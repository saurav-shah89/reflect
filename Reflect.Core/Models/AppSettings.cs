using SQLite;

namespace Reflect.Models;

// Settings table - only ever has one row.
//
// The passphrase itself isn't saved anywhere, just the PBKDF2 hash and salt.
// Iterations is stored as well so the count can be raised later without
// breaking journals that were locked with the old value.
[Table("app_settings")]
public class AppSettings
{
    public const int SingletonId = 1;

    [PrimaryKey]
    public int Id { get; set; } = SingletonId;

    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;

    public int Iterations { get; set; }

    public bool IsLockEnabled { get; set; }

    // "light" or "dark"
    [MaxLength(10)]
    public string Theme { get; set; } = "light";
}
