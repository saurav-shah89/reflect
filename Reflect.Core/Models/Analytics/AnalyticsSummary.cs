namespace Reflect.Models.Analytics;

public sealed class MoodCategoryShare
{
    public MoodCategory Category { get; init; }
    public int Count { get; init; }

    // 0-100
    public double Percentage { get; init; }
}

public sealed class MoodUsage
{
    public int MoodId { get; init; }
    public string Name { get; init; } = string.Empty;
    public MoodCategory Category { get; init; }
    public int Count { get; init; }
}

public sealed class TagUsage
{
    public int TagId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }

    public double PercentageOfEntries { get; init; }
}

public sealed class CategoryUsage
{
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
    public double Percentage { get; init; }
}

// One point on the word count chart.
public sealed class WordCountPoint
{
    public DateTime PeriodStart { get; init; }

    // What goes on the axis, e.g. "12 Mar" or "Mar 2026".
    public string Label { get; init; } = string.Empty;

    public double AverageWords { get; init; }
    public int EntryCount { get; init; }
}

public enum TrendGrouping
{
    Daily,
    Weekly,
    Monthly
}

// Everything the dashboard needs for one date range, built in a single call so
// the page doesn't run a separate query per panel.
public sealed class AnalyticsSummary
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }

    public int EntryCount { get; init; }

    // Days that could have an entry - doesn't count future days.
    public int EligibleDays { get; init; }

    public int TotalWords { get; init; }
    public double AverageWords { get; init; }

    // Based on the primary mood only. The spec calls that the one "for
    // analytics", so the secondary moods don't affect this.
    public IReadOnlyList<MoodCategoryShare> MoodDistribution { get; init; } = Array.Empty<MoodCategoryShare>();

    // Null if there are no entries in the range.
    public MoodUsage? MostFrequentMood { get; init; }

    public IReadOnlyList<MoodUsage> MoodCounts { get; init; } = Array.Empty<MoodUsage>();

    public IReadOnlyList<TagUsage> TopTags { get; init; } = Array.Empty<TagUsage>();

    public IReadOnlyList<CategoryUsage> CategoryBreakdown { get; init; } = Array.Empty<CategoryUsage>();

    public TrendGrouping Grouping { get; init; }

    // Oldest first.
    public IReadOnlyList<WordCountPoint> WordCountTrend { get; init; } = Array.Empty<WordCountPoint>();

    public IReadOnlyList<DateTime> MissedDays { get; init; } = Array.Empty<DateTime>();
}
