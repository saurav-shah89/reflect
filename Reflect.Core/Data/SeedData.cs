using Reflect.Models;

namespace Reflect.Data;

// The reference data from the coursework spec. Inserted once on first run.
internal static class SeedData
{
    // Fifteen moods, five in each category.
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

    // Starting tags. Anything the user adds is saved with IsCustom set.
    public static IReadOnlyList<string> TagNames { get; } = new[]
    {
        "Work", "Career", "Studies", "Family", "Friends", "Relationships",
        "Health", "Fitness", "Personal Growth", "Self-care", "Hobbies", "Travel",
        "Nature", "Finance", "Spirituality", "Birthday", "Holiday", "Vacation",
        "Celebration", "Exercise", "Reading", "Writing", "Cooking", "Meditation",
        "Yoga", "Music", "Shopping", "Parenting", "Projects", "Planning",
        "Reflection"
    };

    // The spec says entries need categories but doesn't say which, so these are
    // just sensible ones to start with.
    public static IReadOnlyList<string> CategoryNames { get; } = new[]
    {
        "Personal", "Work", "Health", "Travel", "Learning", "Relationships"
    };
}
