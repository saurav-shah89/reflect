using SQLite;

namespace Reflect.Models;

// One entry per day. The unique index on EntryDate is what actually stops
// duplicates - I don't rely on the check in EntryService alone.
[Table("entries")]
public class JournalEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    // Always saved at midnight so date comparisons work.
    [Indexed(Name = "idx_entry_date", Unique = true), NotNull]
    public DateTime EntryDate { get; set; }

    [MaxLength(200), NotNull]
    public string Title { get; set; } = string.Empty;

    // Markdown as typed. Turned into HTML when it's displayed.
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    [Indexed]
    public int PrimaryMoodId { get; set; }

    // Two columns instead of a join table. The spec only allows two secondary
    // moods, so this way the schema enforces the limit.
    public int? SecondaryMoodOneId { get; set; }

    public int? SecondaryMoodTwoId { get; set; }

    [Indexed]
    public int? CategoryId { get; set; }

    // Saved instead of counted each time, otherwise the word count chart has to
    // read every entry body to draw itself.
    public int WordCount { get; set; }
}
