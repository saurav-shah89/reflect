namespace Reflect.Services;

/// <summary>
/// Whether the journal is unlocked for this session.
/// </summary>
/// <remarks>
/// Held in memory only and never persisted, so closing the app always returns
/// to a locked state. A plain state container rather than an interface-backed
/// service: it carries no behaviour worth substituting, only a flag and a
/// notification, which is the conventional shape for shared Blazor state.
/// </remarks>
public sealed class AppLockState
{
    /// <summary>True once the correct passphrase has been entered this session.</summary>
    public bool IsUnlocked { get; private set; }

    /// <summary>Raised whenever the lock state changes, so layouts can re-render.</summary>
    public event Action? Changed;

    /// <summary>
    /// Raised when a passphrase is set or removed. Distinct from
    /// <see cref="Changed"/>: that reports locking and unlocking within a
    /// session, this reports that whether a lock exists at all has changed, which
    /// the layout must re-read from storage.
    /// </summary>
    public event Action? ConfigurationChanged;

    /// <summary>Signals that the stored credential has been added or removed.</summary>
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
