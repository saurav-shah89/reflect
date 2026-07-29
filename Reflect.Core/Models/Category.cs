using SQLite;

namespace Reflect.Models;

// An entry can have several tags but only one category.
[Table("categories")]
public class Category
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Name = "idx_category_name", Unique = true), MaxLength(40), NotNull]
    public string Name { get; set; } = string.Empty;
}
