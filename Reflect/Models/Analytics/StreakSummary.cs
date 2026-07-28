namespace Reflect.Models.Analytics;

/// <summary>
/// Journalling consistency across the whole journal.
/// </summary>
/// <remarks>
/// Streaks are deliberately not date-range filtered. "Current streak" only has
/// meaning relative to today, and "longest streak achieved" is a lifetime
/// figure - recomputing either inside an arbitrary window would report numbers
/// that look like achievements but are not.
/// </remarks>
public sealed class StreakSummary
{
    /// <summary>Consecutive days written up to today.</summary>
    public int CurrentStreak { get; init; }

    /// <summary>Longest run of consecutive days ever recorded.</summary>
    public int LongestStreak { get; init; }

    /// <summary>Most recent day with an entry, or null if the journal is empty.</summary>
    public DateTime? LastEntryDate { get; init; }

    /// <summary>Total entries in the journal.</summary>
    public int TotalEntries { get; init; }

    /// <summary>
    /// True when today has no entry but the streak is still alive because
    /// yesterday does. Lets the UI prompt rather than imply the streak is lost.
    /// </summary>
    public bool StreakAtRisk { get; init; }

    public static StreakSummary Empty { get; } = new();
}
