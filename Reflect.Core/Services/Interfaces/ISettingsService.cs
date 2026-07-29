using Reflect.Models;

namespace Reflect.Services.Interfaces;

// App preferences and the passphrase used to lock the journal.
public interface ISettingsService
{
    Task<AppSettings> GetAsync();

    Task<bool> IsLockEnabledAsync();

    // Makes a new salt each time, so the same passphrase hashes differently on
    // two installs. Throws ArgumentException if it's too short.
    Task SetPassphraseAsync(string passphrase);

    // Returns false rather than throwing when no lock is set.
    Task<bool> VerifyPassphraseAsync(string passphrase);

    // Needs the current passphrase, otherwise a session left unlocked could be
    // used to take the lock off. Returns false if it's wrong.
    Task<bool> DisableLockAsync(string currentPassphrase);

    Task SetThemeAsync(string theme);

    Task<string> GetThemeAsync();
}
