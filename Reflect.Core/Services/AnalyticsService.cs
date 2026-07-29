using Microsoft.Extensions.Logging;
using Reflect.Data;
using Reflect.Models;
using Reflect.Models.Analytics;
using Reflect.Services.Interfaces;

namespace Reflect.Services;

// Works out the dashboard numbers.
//
// It only runs two queries - one for the entries in the range and one for the
// tag counts - and everything else is worked out from those in memory, so
// adding another panel doesn't mean another trip to the database. The word
// count buckets are done in C# because week and month boundaries are a pain to
// write in SQLite.
public sealed class AnalyticsService : IAnalyticsService
{
    // Short ranges are charted per day, medium ones per week, longer per month.
    private const int DailyThresholdDays = 31;

    private const int WeeklyThresholdDays = 180;

    // The tag tail is long and doesn't tell you much, so only chart the top few.
    private const int TopTagCount = 10;

    private readonly IJournalDatabase _database;
    private readonly IReferenceDataService _referenceData;
    private readonly ILogger<AnalyticsService> _logger;

    public AnalyticsService(
        IJournalDatabase database,
        IReferenceDataService referenceData,
        ILogger<AnalyticsService> logger)
    {
        _database = database;
        _referenceData = referenceData;
        _logger = logger;
    }

    public async Task<StreakSummary> GetStreaksAsync()
    {
        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

        var rows = await connection
            .QueryAsync<DateRow>("SELECT EntryDate FROM entries ORDER BY EntryDate")
            .ConfigureAwait(false);

        if (rows.Count == 0)
        {
            return StreakSummary.Empty;
        }

        var days = rows.Select(row => row.EntryDate.Date).ToList();
        var daySet = days.ToHashSet();
        var today = DateTime.Today;

        // Don't show the streak as broken just because today isn't written yet -
        // there's still time. Count from yesterday and mark it as at risk.
        var hasToday = daySet.Contains(today);
        var anchor = hasToday
            ? today
            : daySet.Contains(today.AddDays(-1)) ? today.AddDays(-1) : (DateTime?)null;

        var current = 0;
        if (anchor is not null)
        {
            var cursor = anchor.Value;
            while (daySet.Contains(cursor))
            {
                current++;
                cursor = cursor.AddDays(-1);
            }
        }

        return new StreakSummary
        {
            CurrentStreak = current,
            LongestStreak = LongestRun(days),
            LastEntryDate = days[^1],
            TotalEntries = days.Count,
            StreakAtRisk = current > 0 && !hasToday
        };
    }

    public async Task<AnalyticsSummary> GetSummaryAsync(DateTime from, DateTime to)
    {
        var start = from.Date;
        var end = to.Date;

        if (start > end)
        {
            (start, end) = (end, start);
        }

        var connection = await _database.GetConnectionAsync().ConfigureAwait(false);

        var entries = await connection.QueryAsync<SummaryRow>(
                "SELECT EntryDate, WordCount, PrimaryMoodId, CategoryId FROM entries " +
                "WHERE EntryDate >= ? AND EntryDate <= ? ORDER BY EntryDate",
                start, end)
            .ConfigureAwait(false);

        var tagCounts = await connection.QueryAsync<TagCountRow>(
                "SELECT entry_tags.TagId AS TagId, COUNT(*) AS UsageCount " +
                "FROM entry_tags " +
                "JOIN entries ON entries.Id = entry_tags.EntryId " +
                "WHERE entries.EntryDate >= ? AND entries.EntryDate <= ? " +
                "GROUP BY entry_tags.TagId ORDER BY UsageCount DESC",
                start, end)
            .ConfigureAwait(false);

        var moodLookup = await _referenceData.GetMoodLookupAsync().ConfigureAwait(false);
        var tags = await _referenceData.GetTagsAsync().ConfigureAwait(false);
        var categories = await _referenceData.GetCategoriesAsync().ConfigureAwait(false);

        var entryCount = entries.Count;
        var eligibleDays = CountEligibleDays(start, end);
        var grouping = ChooseGrouping(start, end);

        // Already sorted by count, so the most frequent mood is just the first
        // one - no need to work it out again.
        var moodCounts = BuildMoodCounts(entries, moodLookup);

        _logger.LogInformation(
            "Analytics for {From:yyyy-MM-dd} to {To:yyyy-MM-dd}: {Count} entries",
            start, end, entryCount);

        return new AnalyticsSummary
        {
            From = start,
            To = end,
            EntryCount = entryCount,
            EligibleDays = eligibleDays,
            TotalWords = entries.Sum(entry => entry.WordCount),
            AverageWords = entryCount == 0 ? 0 : entries.Average(entry => entry.WordCount),
            MoodDistribution = BuildMoodDistribution(entries, moodLookup),
            MoodCounts = moodCounts,
            MostFrequentMood = moodCounts.FirstOrDefault(),
            TopTags = BuildTagUsage(tagCounts, tags, entryCount),
            CategoryBreakdown = BuildCategoryBreakdown(entries, categories, entryCount),
            Grouping = grouping,
            WordCountTrend = BuildWordCountTrend(entries, grouping),
            MissedDays = FindMissedDays(entries, start, end)
        };
    }

    // Longest run of consecutive days. Expects the list sorted with no repeats.
    private static int LongestRun(IReadOnlyList<DateTime> orderedDays)
    {
        var longest = 1;
        var run = 1;

        for (var i = 1; i < orderedDays.Count; i++)
        {
            if (orderedDays[i] == orderedDays[i - 1].AddDays(1))
            {
                run++;
                longest = Math.Max(longest, run);
            }
            else
            {
                run = 1;
            }
        }

        return longest;
    }

    // Days that could have an entry. Future days don't count as missed because
    // they haven't happened yet.
    private static int CountEligibleDays(DateTime start, DateTime end)
    {
        var lastCountable = end > DateTime.Today ? DateTime.Today : end;
        return lastCountable < start ? 0 : (lastCountable - start).Days + 1;
    }

    private static TrendGrouping ChooseGrouping(DateTime start, DateTime end)
    {
        var days = (end - start).Days + 1;

        return days switch
        {
            <= DailyThresholdDays => TrendGrouping.Daily,
            <= WeeklyThresholdDays => TrendGrouping.Weekly,
            _ => TrendGrouping.Monthly
        };
    }

    private static IReadOnlyList<MoodCategoryShare> BuildMoodDistribution(
        IReadOnlyList<SummaryRow> entries,
        IReadOnlyDictionary<int, Mood> moodLookup)
    {
        var total = entries.Count;

        // Include categories with no entries too, otherwise the legend keeps
        // changing as you switch ranges.
        return Enum.GetValues<MoodCategory>()
            .Select(category =>
            {
                var count = entries.Count(entry =>
                    moodLookup.TryGetValue(entry.PrimaryMoodId, out var mood) && mood.Category == category);

                return new MoodCategoryShare
                {
                    Category = category,
                    Count = count,
                    Percentage = total == 0 ? 0 : Math.Round(count * 100.0 / total, 1)
                };
            })
            .ToArray();
    }

    private static IReadOnlyList<MoodUsage> BuildMoodCounts(
        IReadOnlyList<SummaryRow> entries,
        IReadOnlyDictionary<int, Mood> moodLookup) =>
        entries
            .GroupBy(entry => entry.PrimaryMoodId)
            .Where(group => moodLookup.ContainsKey(group.Key))
            .Select(group => new MoodUsage
            {
                MoodId = group.Key,
                Name = moodLookup[group.Key].Name,
                Category = moodLookup[group.Key].Category,
                Count = group.Count()
            })
            .OrderByDescending(usage => usage.Count)
            .ThenBy(usage => usage.Name)
            .ToArray();

    private static IReadOnlyList<TagUsage> BuildTagUsage(
        IReadOnlyList<TagCountRow> counts,
        IReadOnlyList<Tag> tags,
        int entryCount)
    {
        var names = tags.ToDictionary(tag => tag.Id, tag => tag.Name);

        return counts
            .Where(row => names.ContainsKey(row.TagId))
            .Take(TopTagCount)
            .Select(row => new TagUsage
            {
                TagId = row.TagId,
                Name = names[row.TagId],
                Count = row.UsageCount,
                PercentageOfEntries = entryCount == 0
                    ? 0
                    : Math.Round(row.UsageCount * 100.0 / entryCount, 1)
            })
            .ToArray();
    }

    private static IReadOnlyList<CategoryUsage> BuildCategoryBreakdown(
        IReadOnlyList<SummaryRow> entries,
        IReadOnlyList<Category> categories,
        int entryCount)
    {
        var names = categories.ToDictionary(category => category.Id, category => category.Name);

        return entries
            .GroupBy(entry => entry.CategoryId)
            .Select(group => new CategoryUsage
            {
                Name = group.Key is int id && names.TryGetValue(id, out var name) ? name : "Uncategorised",
                Count = group.Count(),
                Percentage = entryCount == 0 ? 0 : Math.Round(group.Count() * 100.0 / entryCount, 1)
            })
            .OrderByDescending(usage => usage.Count)
            .ThenBy(usage => usage.Name)
            .ToArray();
    }

    // Average words per entry in each bucket. Empty buckets are left out rather
    // than plotted as zero, which would look like writing nothing rather than
    // not writing at all.
    private static IReadOnlyList<WordCountPoint> BuildWordCountTrend(
        IReadOnlyList<SummaryRow> entries,
        TrendGrouping grouping) =>
        entries
            .GroupBy(entry => BucketStart(entry.EntryDate.Date, grouping))
            .OrderBy(group => group.Key)
            .Select(group => new WordCountPoint
            {
                PeriodStart = group.Key,
                Label = BucketLabel(group.Key, grouping),
                AverageWords = Math.Round(group.Average(entry => entry.WordCount), 1),
                EntryCount = group.Count()
            })
            .ToArray();

    private static DateTime BucketStart(DateTime date, TrendGrouping grouping) => grouping switch
    {
        TrendGrouping.Daily => date,
        // Weeks start on Monday, matching the calendar view.
        TrendGrouping.Weekly => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
        _ => new DateTime(date.Year, date.Month, 1)
    };

    private static string BucketLabel(DateTime bucketStart, TrendGrouping grouping) => grouping switch
    {
        TrendGrouping.Daily => bucketStart.ToString("d MMM"),
        TrendGrouping.Weekly => bucketStart.ToString("d MMM"),
        _ => bucketStart.ToString("MMM yyyy")
    };

    private static IReadOnlyList<DateTime> FindMissedDays(
        IReadOnlyList<SummaryRow> entries,
        DateTime start,
        DateTime end)
    {
        var written = entries.Select(entry => entry.EntryDate.Date).ToHashSet();
        var lastCountable = end > DateTime.Today ? DateTime.Today : end;
        var missed = new List<DateTime>();

        for (var day = start; day <= lastCountable; day = day.AddDays(1))
        {
            if (!written.Contains(day))
            {
                missed.Add(day);
            }
        }

        return missed;
    }

    // The streak query only needs the dates.
    private sealed class DateRow
    {
        public DateTime EntryDate { get; set; }
    }

    // Only the columns the totals actually need.
    private sealed class SummaryRow
    {
        public DateTime EntryDate { get; set; }
        public int WordCount { get; set; }
        public int PrimaryMoodId { get; set; }
        public int? CategoryId { get; set; }
    }

    private sealed class TagCountRow
    {
        public int TagId { get; set; }
        public int UsageCount { get; set; }
    }
}
