using Reflect.Models;
using Reflect.Models.Analytics;

namespace Reflect.Core.Tests;

/// <summary>
/// Streaks and the dashboard aggregates. Streak arithmetic has the most edge
/// cases of anything in the project, so it carries the most tests.
/// </summary>
public sealed class AnalyticsServiceTests
{
    [Fact]
    public async Task An_empty_journal_reports_no_streak()
    {
        using var journal = new TestJournal();

        var streaks = await journal.Analytics.GetStreaksAsync();

        Assert.Equal(0, streaks.CurrentStreak);
        Assert.Equal(0, streaks.LongestStreak);
        Assert.Null(streaks.LastEntryDate);
        Assert.False(streaks.StreakAtRisk);
    }

    [Fact]
    public async Task Current_streak_counts_back_from_today()
    {
        using var journal = new TestJournal();
        foreach (var offset in new[] { 0, 1, 2, 5, 6 })
        {
            await journal.WriteAsync(DateTime.Today.AddDays(-offset));
        }

        var streaks = await journal.Analytics.GetStreaksAsync();

        Assert.Equal(3, streaks.CurrentStreak);
        Assert.Equal(3, streaks.LongestStreak);
        Assert.Equal(DateTime.Today, streaks.LastEntryDate);
        Assert.Equal(5, streaks.TotalEntries);
        Assert.False(streaks.StreakAtRisk);
    }

    [Fact]
    public async Task A_streak_survives_a_today_that_is_not_written_yet()
    {
        using var journal = new TestJournal();
        foreach (var offset in new[] { 1, 2, 3 })
        {
            await journal.WriteAsync(DateTime.Today.AddDays(-offset));
        }

        var streaks = await journal.Analytics.GetStreaksAsync();

        // Showing zero at nine in the morning would be both wrong and dispiriting.
        Assert.Equal(3, streaks.CurrentStreak);
        Assert.True(streaks.StreakAtRisk);
    }

    [Fact]
    public async Task A_streak_ends_once_a_whole_day_passes_unwritten()
    {
        using var journal = new TestJournal();
        foreach (var offset in new[] { 2, 3, 4, 5 })
        {
            await journal.WriteAsync(DateTime.Today.AddDays(-offset));
        }

        var streaks = await journal.Analytics.GetStreaksAsync();

        Assert.Equal(0, streaks.CurrentStreak);
        Assert.Equal(4, streaks.LongestStreak);
        Assert.False(streaks.StreakAtRisk);
    }

    [Fact]
    public async Task A_run_spanning_a_month_boundary_counts_as_one_streak()
    {
        using var journal = new TestJournal();

        // Four consecutive days ending on the last day of last month.
        var endOfLastMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddDays(-1);
        for (var i = 0; i < 4; i++)
        {
            await journal.WriteAsync(endOfLastMonth.AddDays(-i));
        }

        var streaks = await journal.Analytics.GetStreaksAsync();

        Assert.Equal(4, streaks.LongestStreak);
    }

    [Fact]
    public async Task Mood_distribution_is_based_on_the_primary_mood()
    {
        using var journal = new TestJournal();
        await journal.WriteAsync(DateTime.Today, mood: "Happy");
        await journal.WriteAsync(DateTime.Today.AddDays(-1), mood: "Happy");
        await journal.WriteAsync(DateTime.Today.AddDays(-2), mood: "Calm");
        await journal.WriteAsync(DateTime.Today.AddDays(-3), mood: "Sad");

        var summary = await journal.Analytics.GetSummaryAsync(DateTime.Today.AddDays(-3), DateTime.Today);

        double Share(MoodCategory category) =>
            summary.MoodDistribution.First(m => m.Category == category).Percentage;

        Assert.Equal(50.0, Share(MoodCategory.Positive), 1);
        Assert.Equal(25.0, Share(MoodCategory.Neutral), 1);
        Assert.Equal(25.0, Share(MoodCategory.Negative), 1);
        Assert.Equal(100.0, summary.MoodDistribution.Sum(m => m.Percentage), 1);
    }

    [Fact]
    public async Task All_three_mood_categories_are_present_even_when_unused()
    {
        using var journal = new TestJournal();
        await journal.WriteAsync(DateTime.Today, mood: "Happy");

        var summary = await journal.Analytics.GetSummaryAsync(DateTime.Today, DateTime.Today);

        // A chart legend that changes shape as the range changes is worse than
        // one showing a zero.
        Assert.Equal(3, summary.MoodDistribution.Count);
    }

    [Fact]
    public async Task Most_frequent_mood_is_the_commonest_primary()
    {
        using var journal = new TestJournal();
        await journal.WriteAsync(DateTime.Today, mood: "Happy");
        await journal.WriteAsync(DateTime.Today.AddDays(-1), mood: "Happy");
        await journal.WriteAsync(DateTime.Today.AddDays(-2), mood: "Sad");

        var summary = await journal.Analytics.GetSummaryAsync(DateTime.Today.AddDays(-2), DateTime.Today);

        Assert.Equal("Happy", summary.MostFrequentMood?.Name);
        Assert.Equal(2, summary.MostFrequentMood?.Count);
    }

    [Fact]
    public async Task Tag_usage_counts_and_percentages_are_reported()
    {
        using var journal = new TestJournal();
        await journal.WriteAsync(DateTime.Today, tags: new[] { "Work", "Health" });
        await journal.WriteAsync(DateTime.Today.AddDays(-1), tags: new[] { "Work" });
        await journal.WriteAsync(DateTime.Today.AddDays(-2), tags: new[] { "Work" });
        await journal.WriteAsync(DateTime.Today.AddDays(-3));

        var summary = await journal.Analytics.GetSummaryAsync(DateTime.Today.AddDays(-3), DateTime.Today);
        var work = summary.TopTags.First(tag => tag.Name == "Work");

        Assert.Equal(3, work.Count);
        Assert.Equal(75.0, work.PercentageOfEntries, 1);
        Assert.True(summary.TopTags[0].Count >= summary.TopTags[^1].Count);
    }

    [Fact]
    public async Task Entries_with_no_category_are_grouped_as_uncategorised()
    {
        using var journal = new TestJournal();
        await journal.WriteAsync(DateTime.Today, category: "Work");
        await journal.WriteAsync(DateTime.Today.AddDays(-1));

        var summary = await journal.Analytics.GetSummaryAsync(DateTime.Today.AddDays(-1), DateTime.Today);

        Assert.Contains(summary.CategoryBreakdown, c => c.Name == "Uncategorised");
        Assert.Equal(100.0, summary.CategoryBreakdown.Sum(c => c.Percentage), 1);
    }

    [Fact]
    public async Task Missed_days_are_the_gaps_in_the_range()
    {
        using var journal = new TestJournal();
        foreach (var offset in new[] { 0, 2, 4 })
        {
            await journal.WriteAsync(DateTime.Today.AddDays(-offset));
        }

        var summary = await journal.Analytics.GetSummaryAsync(DateTime.Today.AddDays(-4), DateTime.Today);

        Assert.Equal(2, summary.MissedDays.Count);
        Assert.Contains(DateTime.Today.AddDays(-1), summary.MissedDays);
        Assert.Contains(DateTime.Today.AddDays(-3), summary.MissedDays);
        Assert.Equal(5, summary.EligibleDays);
    }

    [Fact]
    public async Task Days_that_have_not_happened_are_neither_eligible_nor_missed()
    {
        using var journal = new TestJournal();
        foreach (var offset in new[] { 0, 2, 4 })
        {
            await journal.WriteAsync(DateTime.Today.AddDays(-offset));
        }

        var summary = await journal.Analytics.GetSummaryAsync(
            DateTime.Today.AddDays(-4), DateTime.Today.AddDays(10));

        Assert.Equal(5, summary.EligibleDays);
        Assert.Equal(2, summary.MissedDays.Count);
    }

    [Theory]
    [InlineData(10, TrendGrouping.Daily)]
    [InlineData(90, TrendGrouping.Weekly)]
    [InlineData(400, TrendGrouping.Monthly)]
    public async Task Trend_granularity_follows_the_range_length(int days, TrendGrouping expected)
    {
        using var journal = new TestJournal();
        await journal.WriteAsync(DateTime.Today);

        var summary = await journal.Analytics.GetSummaryAsync(DateTime.Today.AddDays(-days), DateTime.Today);

        Assert.Equal(expected, summary.Grouping);
    }

    [Fact]
    public async Task Trend_buckets_omit_days_with_no_entries()
    {
        using var journal = new TestJournal();
        await journal.WriteAsync(DateTime.Today, content: "one two three");
        await journal.WriteAsync(DateTime.Today.AddDays(-2), content: "one two three");

        var summary = await journal.Analytics.GetSummaryAsync(DateTime.Today.AddDays(-10), DateTime.Today);

        // Plotting an empty day as zero would read as "wrote nothing" rather
        // than "did not write".
        Assert.Equal(2, summary.WordCountTrend.Count);
        Assert.All(summary.WordCountTrend, point => Assert.True(point.EntryCount > 0));
    }

    [Fact]
    public async Task Reversed_range_bounds_are_tolerated()
    {
        using var journal = new TestJournal();
        for (var i = 0; i < 3; i++)
        {
            await journal.WriteAsync(DateTime.Today.AddDays(-i));
        }

        var summary = await journal.Analytics.GetSummaryAsync(DateTime.Today, DateTime.Today.AddDays(-2));

        Assert.Equal(3, summary.EntryCount);
    }

    [Fact]
    public async Task An_empty_range_yields_zeroes_rather_than_NaN()
    {
        using var journal = new TestJournal();
        await journal.WriteAsync(DateTime.Today);

        var summary = await journal.Analytics.GetSummaryAsync(
            DateTime.Today.AddDays(-400), DateTime.Today.AddDays(-390));

        Assert.Equal(0, summary.EntryCount);
        Assert.Equal(0, summary.AverageWords);
        Assert.Null(summary.MostFrequentMood);
        Assert.All(summary.MoodDistribution, share => Assert.Equal(0, share.Percentage));
    }
}
