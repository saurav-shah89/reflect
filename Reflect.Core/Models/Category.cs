using SQLite;

namespace Reflect.Models;

/// <summary>
/// A broad grouping an entry can be filed under. Unlike tags, an entry has at
/// most one category.
/// </summary>
[Table("categories")]
public class Category
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Name = "idx_category_name", Unique = true), MaxLength(40), NotNull]
    public string Name { get; set; } = string.Empty;
}
