using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Reflect.Data;
using Reflect.Models;
using Reflect.Services.Interfaces;

namespace Reflect.Services;

/// <summary>
/// App preferences and the journal lock credential.
/// </summary>
/// <remarks>
/// The passphrase is never stored. What is persisted is a PBKDF2-HMAC-SHA256
/// hash, a random per-install salt, and the iteration count used to produce it.
/// Storing the iteration count alongside the hash means the work factor can be
/// raised later without invalidating credentials people already set - an old
/// hash still verifies against its own recorded count.
///
/// Comparison uses <see cref="CryptographicOperations.FixedTimeEquals"/> rather
/// than a byte-by-byte loop, so how long verification takes does not leak how
/// much of the hash matched.
/// </remarks>
public sealed class SettingsService : ISettingsService
{
    /// <summary>
    /// PBKDF2 work factor for new passphrases, following the OWASP Password
    /// Storage guidance for PBKDF2-HMAC-SHA256. Measured at roughly 90ms per
    /// verification on a desktop - imperceptible when unlocking, but costly
    /// enough to make offline guessing expensive. Existing credentials keep the
    /// count they were created with, so this can be raised again later without
    /// invalidating anyone's passphrase.
    /// </summary>
    private const int CurrentIterations = 600_000;

    /// <summary>Length of the derived key and of the salt, in bytes.</summary>
    private const int KeySize = 32;

    /// <summary>
    /// Minimum passphrase length. Deliberately short enough to allow a numeric
    /// PIN, which the specification names as an acceptable option.
    /// </summary>
    public const int MinimumLength = 4;

    private readonly IJournalDatabase _database;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(IJournalDatabase database, ILogger<SettingsService> logger)
    {
        _database = database;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AppSettings> GetAsync()
    {
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

        var settings = await connection.Table<AppSettings>()
            .Where(row => row.Id == AppSettings.SingletonId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (settings is null)
        {
            // The seed should have created this row; recreate rather than fail so
            // a damaged database still opens.
            settings = new AppSettings();
            await connection.InsertAsync(settings).ConfigureAwait(false);
            _logger.LogWarning("Settings row was missing and has been recreated");
        }

        return settings;
    }

    /// <inheritdoc />
    public async Task<bool> IsLockEnabledAsync()
    {
        var settings = await GetAsync().ConfigureAwait(false);
        return settings.IsLockEnabled && settings.PasswordHash.Length > 0;
    }

    /// <inheritdoc />
    public async Task SetPassphraseAsync(string passphrase)
    {
        Validate(passphrase);

        var salt = RandomNumberGenerator.GetBytes(KeySize);
        var hash = DeriveKey(passphrase, salt, CurrentIterations);

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var settings = await GetAsync().ConfigureAwait(false);

        settings.PasswordSalt = Convert.ToBase64String(salt);
        settings.PasswordHash = Convert.ToBase64String(hash);
        settings.Iterations = CurrentIterations;
        settings.IsLockEnabled = true;

        await connection.UpdateAsync(settings).ConfigureAwait(false);
        _logger.LogInformation("Journal lock enabled");
    }

    /// <inheritdoc />
    public async Task<bool> VerifyPassphraseAsync(string passphrase)
    {
        if (string.IsNullOrEmpty(passphrase))
        {
            return false;
        }

        var settings = await GetAsync().ConfigureAwait(false);

        if (!settings.IsLockEnabled ||
            settings.PasswordHash.Length == 0 ||
            settings.PasswordSalt.Length == 0)
        {
            return false;
        }

        byte[] expected;
        byte[] salt;

        try
        {
            expected = Convert.FromBase64String(settings.PasswordHash);
            salt = Convert.FromBase64String(settings.PasswordSalt);
        }
        catch (FormatException ex)
        {
            // Corrupted credential material must not be treated as a match.
            _logger.LogError(ex, "Stored credential is not valid base64");
            return false;
        }

        // Fall back to the current work factor for rows written before the
        // iteration count was recorded.
        var iterations = settings.Iterations > 0 ? settings.Iterations : CurrentIterations;
        var actual = DeriveKey(passphrase, salt, iterations);

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    /// <inheritdoc />
    public async Task<bool> DisableLockAsync(string currentPassphrase)
    {
        if (!await VerifyPassphraseAsync(currentPassphrase).ConfigureAwait(false))
        {
            return false;
        }

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var settings = await GetAsync().ConfigureAwait(false);

        settings.PasswordHash = string.Empty;
        settings.PasswordSalt = string.Empty;
        settings.Iterations = 0;
        settings.IsLockEnabled = false;

        await connection.UpdateAsync(settings).ConfigureAwait(false);
        _logger.LogInformation("Journal lock disabled");

        return true;
    }

    /// <inheritdoc />
    public async Task SetThemeAsync(string theme)
    {
        var normalised = string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase)
            ? "dark"
            : "light";

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);
        var settings = await GetAsync().ConfigureAwait(false);

        settings.Theme = normalised;
        await connection.UpdateAsync(settings).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> GetThemeAsync()
    {
        var settings = await GetAsync().ConfigureAwait(false);
        return string.IsNullOrWhiteSpace(settings.Theme) ? "light" : settings.Theme;
    }

    private static void Validate(string passphrase)
    {
        if (string.IsNullOrWhiteSpace(passphrase))
        {
            throw new ArgumentException("A passphrase is required.", nameof(passphrase));
        }

        if (passphrase.Length < MinimumLength)
        {
            throw new ArgumentException(
                $"Use at least {MinimumLength} characters.", nameof(passphrase));
        }
    }

    private static byte[] DeriveKey(string passphrase, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(passphrase),
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            KeySize);
}
