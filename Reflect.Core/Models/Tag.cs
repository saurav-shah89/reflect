using SQLite;

namespace Reflect.Models;

[Table("tags")]
public class Tag
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Name = "idx_tag_name", Unique = true), MaxLength(40), NotNull]
    public string Name { get; set; } = string.Empty;

    // True for tags the user typed in themselves, false for the seeded ones.
    public bool IsCustom { get; set; }
}
