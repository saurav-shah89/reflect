using SQLite;

namespace Reflect.Models;

/// <summary>
/// A label attached to entries. Tags are either pre-built (seeded from the
/// specification) or created by the user at write time.
/// </summary>
[Table("tags")]
public class Tag
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Name = "idx_tag_name", Unique = true), MaxLength(40), NotNull]
    public string Name { get; set; } = string.Empty;

    /// <summary>False for the pre-built set, true for tags the user added themselves.</summary>
    public bool IsCustom { get; set; }
}
