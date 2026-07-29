using SQLite;

namespace Reflect.Models;

/// <summary>
/// One day's journal entry.
/// </summary>
/// <remarks>
/// The specification allows exactly one entry per calendar day. That rule is
/// enforced at the schema level by the unique index on <see cref="EntryDate"/>
/// rather than in application code alone, so a race or a bug cannot produce a
/// duplicate day. <see cref="EntryDate"/> is always normalised to midnight.
/// </remarks>
[Table("entries")]
public class JournalEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>The day this entry belongs to, normalised to 00:00:00.</summary>
    [Indexed(Name = "idx_entry_date", Unique = true), NotNull]
    public DateTime EntryDate { get; set; }

    [MaxLength(200), NotNull]
    public string Title { get; set; } = string.Empty;

    /// <summary>Raw Markdown as typed by the user. Rendered to HTML for display.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Set once when the entry is first saved. Never modified afterwards.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Refreshed on every save.</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Required primary mood. Drives mood-distribution analytics.</summary>
    [Indexed]
    public int PrimaryMoodId { get; set; }

    /// <summary>
    /// Optional additional moods. Modelled as two nullable columns rather than a
    /// join table because the specification caps secondary moods at two - the
    /// schema then enforces that limit for free.
    /// </summary>
    public int? SecondaryMoodOneId { get; set; }

    public int? SecondaryMoodTwoId { get; set; }

    [Indexed]
    public int? CategoryId { get; set; }

    /// <summary>
    /// Word count of <see cref="Content"/>, stored rather than recomputed so the
    /// word-count-trend chart does not have to parse every entry body on load.
    /// Kept in sync by the entry service on save.
    /// </summary>
    public int WordCount { get; set; }
}
