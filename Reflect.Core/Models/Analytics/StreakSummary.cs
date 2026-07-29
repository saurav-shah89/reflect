namespace Reflect.Models.Analytics;

// Streak numbers for the whole journal.
//
// These aren't filtered by the dashboard's date range on purpose. "Current
// streak" only makes sense compared to today, and the longest streak is a
// lifetime figure - working either out inside a random window would show
// numbers that look like achievements but aren't.
public sealed class StreakSummary
{
    public int CurrentStreak { get; init; }

    public int LongestStreak { get; init; }

    public DateTime? LastEntryDate { get; init; }

    public int TotalEntries { get; init; }

    // Today has no entry yet but yesterday does, so the streak is still alive.
    // Used to nudge instead of showing the streak as already broken.
    public bool StreakAtRisk { get; init; }

    public static StreakSummary Empty { get; } = new();
}
