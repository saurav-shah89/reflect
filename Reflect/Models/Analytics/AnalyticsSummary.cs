namespace Reflect.Models.Analytics;

/// <summary>Share of entries falling in one mood category.</summary>
public sealed class MoodCategoryShare
{
    public MoodCategory Category { get; init; }
    public int Count { get; init; }

    /// <summary>Share of entries in the range, 0-100.</summary>
    public double Percentage { get; init; }
}

/// <summary>How often a single mood was recorded.</summary>
public sealed class MoodUsage
{
    public int MoodId { get; init; }
    public string Name { get; init; } = string.Empty;
    public MoodCategory Category { get; init; }
    public int Count { get; init; }
}

/// <summary>How often a tag was applied.</summary>
public sealed class TagUsage
{
    public int TagId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }

    /// <summary>Share of entries in the range carrying this tag, 0-100.</summary>
    public double PercentageOfEntries { get; init; }
}

/// <summary>How many entries were filed under a category.</summary>
public sealed class CategoryUsage
{
    public string Name { get; init; } = string.Empty;
    public int Count { get; init; }
    public double Percentage { get; init; }
}

/// <summary>One bucket on the word-count trend.</summary>
public sealed class WordCountPoint
{
    public DateTime PeriodStart { get; init; }

    /// <summary>Axis label for the bucket, e.g. "12 Mar" or "Mar 2026".</summary>
    public string Label { get; init; } = string.Empty;

    public double AverageWords { get; init; }
    public int EntryCount { get; init; }
}

/// <summary>Granularity chosen for the word-count trend.</summary>
public enum TrendGrouping
{
    Daily,
    Weekly,
    Monthly
}

/// <summary>
/// Everything the dashboard shows for one date range.
/// </summary>
/// <remarks>
/// Assembled in a single service call rather than one call per widget, so the
/// page issues a fixed number of queries no matter how many charts it draws.
/// </remarks>
public sealed class AnalyticsSummary
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }

    /// <summary>Entries within the range.</summary>
    public int EntryCount { get; init; }

    /// <summary>Days in the range that could hold an entry, excluding the future.</summary>
    public int EligibleDays { get; init; }

    public int TotalWords { get; init; }
    public double AverageWords { get; init; }

    /// <summary>
    /// Distribution across Positive, Neutral and Negative. Based on each entry's
    /// primary mood, which the specification designates as the one "for
    /// analytics"; secondary moods colour an entry but do not classify it.
    /// </summary>
    public IReadOnlyList<MoodCategoryShare> MoodDistribution { get; init; } = Array.Empty<MoodCategoryShare>();

    /// <summary>Most frequently recorded primary mood, or null when the range is empty.</summary>
    public MoodUsage? MostFrequentMood { get; init; }

    /// <summary>Every primary mood used in the range, most frequent first.</summary>
    public IReadOnlyList<MoodUsage> MoodCounts { get; init; } = Array.Empty<MoodUsage>();

    /// <summary>Most used tags, most frequent first.</summary>
    public IReadOnlyList<TagUsage> TopTags { get; init; } = Array.Empty<TagUsage>();

    /// <summary>Entries per category, largest first.</summary>
    public IReadOnlyList<CategoryUsage> CategoryBreakdown { get; init; } = Array.Empty<CategoryUsage>();

    public TrendGrouping Grouping { get; init; }

    /// <summary>Average words per entry over time, oldest bucket first.</summary>
    public IReadOnlyList<WordCountPoint> WordCountTrend { get; init; } = Array.Empty<WordCountPoint>();

    /// <summary>Days in the range with no entry, excluding future days.</summary>
    public IReadOnlyList<DateTime> MissedDays { get; init; } = Array.Empty<DateTime>();
}
