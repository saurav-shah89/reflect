using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Reflect.Data;
using Reflect.Models;
using Reflect.Services.Interfaces;

namespace Reflect.Services;

// App settings and the journal passphrase.
//
// The passphrase isn't stored anywhere. What gets saved is a PBKDF2-HMAC-SHA256
// hash, a random salt, and the iteration count that was used. Saving the count
// as well means it can be raised later without locking out anyone who set their
// passphrase under the old value.
//
// The comparison uses CryptographicOperations.FixedTimeEquals instead of
// comparing byte by byte, so how long it takes doesn't give away how much of
// the hash was right.
public sealed class SettingsService : ISettingsService
{
    // Iteration count for new passphrases. This is the figure OWASP recommends
    // for PBKDF2-HMAC-SHA256. It works out at about 90ms to unlock, which you
    // don't notice, but makes guessing at scale slow.
    private const int CurrentIterations = 600_000;

    // Size of both the hash and the salt, in bytes.
    private const int KeySize = 32;

    // Short on purpose - the spec allows a PIN as well as a passphrase.
    public const int MinimumLength = 4;

    private readonly IJournalDatabase _database;
    private readonly ILogger<SettingsService> _logger;

    public SettingsService(IJournalDatabase database, ILogger<SettingsService> logger)
    {
        _database = database;
        _logger = logger;
    }

    public async Task<AppSettings> GetAsync()
    {
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

        var settings = await connection.Table<AppSettings>()
            .Where(row => row.Id == AppSettings.SingletonId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (settings is null)
        {
            // The seeding should have made this row already, but make it again
            // rather than crash so a damaged database still opens.
            settings = new AppSettings();
            await connection.InsertAsync(settings).ConfigureAwait(false);
            _logger.LogWarning("Settings row was missing and has been recreated");
        }

        return settings;
    }

    public async Task<bool> IsLockEnabledAsync()
    {
        var settings = await GetAsync().ConfigureAwait(false);
        return settings.IsLockEnabled && settings.PasswordHash.Length > 0;
    }

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

        // Older rows didn't save the iteration count, so use the current one.
        var iterations = settings.Iterations > 0 ? settings.Iterations : CurrentIterations;
        var actual = DeriveKey(passphrase, salt, iterations);

        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

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
