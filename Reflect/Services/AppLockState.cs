namespace Reflect.Services;

// Tracks whether the journal is unlocked for this session.
//
// Kept in memory only, never saved, so closing the app always locks it again.
// It's just a flag and an event rather than a proper service with an interface,
// since there's nothing here worth swapping out.
public sealed class AppLockState
{
    // True once the correct passphrase has been entered this session.
    public bool IsUnlocked { get; private set; }

    // Raised whenever the lock state changes, so layouts can re-render.
    public event Action? Changed;

    // Fires when a passphrase is set or removed. Not the same as Changed -
    // that one is for locking and unlocking during a session, this one means
    // the layout needs to go and re-read whether a lock exists at all.
    public event Action? ConfigurationChanged;

    // Signals that the stored credential has been added or removed.
    public void NotifyConfigurationChanged() => ConfigurationChanged?.Invoke();

    public void Unlock()
    {
        if (IsUnlocked)
        {
            return;
        }

        IsUnlocked = true;
        Changed?.Invoke();
    }

    public void Lock()
    {
        if (!IsUnlocked)
        {
            return;
        }

        IsUnlocked = false;
        Changed?.Invoke();
    }
}
