using SQLite;

namespace Reflect.Models;

/// <summary>
/// Single-row table holding app-wide preferences and the credential used to
/// unlock the journal.
/// </summary>
/// <remarks>
/// The passphrase is never stored. Only a PBKDF2 hash and its per-install random
/// salt are persisted, so reading the database file does not reveal the
/// credential. <see cref="Iterations"/> is stored alongside the hash so the work
/// factor can be raised later without invalidating existing credentials.
/// </remarks>
[Table("app_settings")]
public class AppSettings
{
    /// <summary>Always 1 - this table holds exactly one row.</summary>
    public const int SingletonId = 1;

    [PrimaryKey]
    public int Id { get; set; } = SingletonId;

    /// <summary>Base64 PBKDF2-SHA256 hash of the passphrase. Empty until one is set.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Base64 random salt generated when the passphrase is set.</summary>
    public string PasswordSalt { get; set; } = string.Empty;

    /// <summary>PBKDF2 work factor used to produce <see cref="PasswordHash"/>.</summary>
    public int Iterations { get; set; }

    /// <summary>True once the user has chosen a passphrase or PIN.</summary>
    public bool IsLockEnabled { get; set; }

    /// <summary>Selected theme: "light" or "dark".</summary>
    [MaxLength(10)]
    public string Theme { get; set; } = "light";
}
