using Reflect.Models;

namespace Reflect.Services.Interfaces;

/// <summary>
/// Reads and writes app preferences, and owns the journal lock credential.
/// </summary>
public interface ISettingsService
{
    /// <summary>Returns the settings row, creating it if it is somehow missing.</summary>
    Task<AppSettings> GetAsync();

    /// <summary>True once a passphrase has been set and the lock is enabled.</summary>
    Task<bool> IsLockEnabledAsync();

    /// <summary>
    /// Sets or replaces the passphrase and enables the lock. Generates a fresh
    /// salt, so the same passphrase produces a different hash on every install.
    /// </summary>
    /// <exception cref="ArgumentException">The passphrase is too short.</exception>
    Task SetPassphraseAsync(string passphrase);

    /// <summary>
    /// Checks a passphrase against the stored hash. Returns false rather than
    /// throwing when no lock is set, so callers need not special-case it.
    /// </summary>
    Task<bool> VerifyPassphraseAsync(string passphrase);

    /// <summary>
    /// Turns the lock off and clears the stored hash and salt. Requires the
    /// current passphrase, so an unlocked session cannot be used to strip
    /// protection without knowing it.
    /// </summary>
    /// <returns>False when the supplied passphrase is wrong; nothing changes.</returns>
    Task<bool> DisableLockAsync(string currentPassphrase);

    /// <summary>Persists the chosen theme, "light" or "dark".</summary>
    Task SetThemeAsync(string theme);

    /// <summary>Returns the persisted theme, defaulting to "light".</summary>
    Task<string> GetThemeAsync();
}
