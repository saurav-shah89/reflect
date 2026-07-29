using Reflect.Models;

namespace Reflect.Data;

/// <summary>
/// The fixed reference data defined by the coursework specification. These lists
/// are inserted once, on first run, and are the single source of truth for the
/// built-in moods, tags and categories.
/// </summary>
internal static class SeedData
{
    /// <summary>
    /// The fifteen moods from the specification: five per category.
    /// </summary>
    public static IReadOnlyList<Mood> Moods { get; } = new List<Mood>
    {
        // Positive
        new() { Name = "Happy",      Category = MoodCategory.Positive, SortOrder = 1 },
        new() { Name = "Excited",    Category = MoodCategory.Positive, SortOrder = 2 },
        new() { Name = "Relaxed",    Category = MoodCategory.Positive, SortOrder = 3 },
        new() { Name = "Grateful",   Category = MoodCategory.Positive, SortOrder = 4 },
        new() { Name = "Confident",  Category = MoodCategory.Positive, SortOrder = 5 },

        // Neutral
        new() { Name = "Calm",       Category = MoodCategory.Neutral,  SortOrder = 1 },
        new() { Name = "Thoughtful", Category = MoodCategory.Neutral,  SortOrder = 2 },
        new() { Name = "Curious",    Category = MoodCategory.Neutral,  SortOrder = 3 },
        new() { Name = "Nostalgic",  Category = MoodCategory.Neutral,  SortOrder = 4 },
        new() { Name = "Bored",      Category = MoodCategory.Neutral,  SortOrder = 5 },

        // Negative
        new() { Name = "Sad",        Category = MoodCategory.Negative, SortOrder = 1 },
        new() { Name = "Angry",      Category = MoodCategory.Negative, SortOrder = 2 },
        new() { Name = "Stressed",   Category = MoodCategory.Negative, SortOrder = 3 },
        new() { Name = "Lonely",     Category = MoodCategory.Negative, SortOrder = 4 },
        new() { Name = "Anxious",    Category = MoodCategory.Negative, SortOrder = 5 },
    };

    /// <summary>
    /// The pre-built tag list from the specification. Users may add their own on
    /// top of these; those are stored with <see cref="Tag.IsCustom"/> set.
    /// </summary>
    public static IReadOnlyList<string> TagNames { get; } = new[]
    {
        "Work", "Career", "Studies", "Family", "Friends", "Relationships",
        "Health", "Fitness", "Personal Growth", "Self-care", "Hobbies", "Travel",
        "Nature", "Finance", "Spirituality", "Birthday", "Holiday", "Vacation",
        "Celebration", "Exercise", "Reading", "Writing", "Cooking", "Meditation",
        "Yoga", "Music", "Shopping", "Parenting", "Projects", "Planning",
        "Reflection"
    };

    /// <summary>
    /// Starting categories. The specification requires entries to be organised
    /// under a category but does not fix the list, so these are sensible
    /// defaults the user can build on.
    /// </summary>
    public static IReadOnlyList<string> CategoryNames { get; } = new[]
    {
        "Personal", "Work", "Health", "Travel", "Learning", "Relationships"
    };
}
