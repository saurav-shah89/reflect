using SQLite;

namespace Reflect.Models;

// The moods you can pick from. Fixed list from the spec, seeded on first run.
[Table("moods")]
public class Mood
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Name = "idx_mood_name", Unique = true), MaxLength(40), NotNull]
    public string Name { get; set; } = string.Empty;

    [Indexed]
    public MoodCategory Category { get; set; }

    public int SortOrder { get; set; }
}
