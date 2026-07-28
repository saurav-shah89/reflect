using SQLite;

namespace Reflect.Models;

/// <summary>
/// A single selectable mood, e.g. "Happy" (Positive) or "Anxious" (Negative).
/// The full set is fixed by the specification and seeded on first run.
/// </summary>
[Table("moods")]
public class Mood
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Name = "idx_mood_name", Unique = true), MaxLength(40), NotNull]
    public string Name { get; set; } = string.Empty;

    [Indexed]
    public MoodCategory Category { get; set; }

    /// <summary>Display order within the category, so pickers list moods predictably.</summary>
    public int SortOrder { get; set; }
}
