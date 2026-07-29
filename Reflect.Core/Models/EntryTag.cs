using SQLite;

namespace Reflect.Models;

// Join table between entries and tags.
[Table("entry_tags")]
public class EntryTag
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Name = "idx_entrytag_pair", Order = 1, Unique = true)]
    public int EntryId { get; set; }

    [Indexed(Name = "idx_entrytag_pair", Order = 2, Unique = true)]
    public int TagId { get; set; }
}
