using Reflect.Models;

namespace Reflect.Core.Tests;

/// <summary>
/// Search, filtering and paging. These run against real SQL rather than an
/// in-memory list, so the dynamic WHERE clause is genuinely exercised.
/// </summary>
public sealed class EntrySearchTests
{
    /// <summary>Twelve entries with a predictable spread of moods, tags and categories.</summary>
    private static async Task SeedCorpusAsync(TestJournal journal)
    {
        for (var i = 1; i <= 12; i++)
        {
            await journal.Entries.SaveAsync(new JournalEntry
            {
                EntryDate = DateTime.Today.AddDays(-i),
                Title = i % 2 == 0 ? $"Even day {i}" : $"Odd day {i}",
                Content = i % 3 == 0 ? "Contains the word pineapple." : "Ordinary content.",
                PrimaryMoodId = await journal.MoodIdAsync(i % 2 == 0 ? "Sad" : "Happy"),
                SecondaryMoodOneId = i % 4 == 0 ? await journal.MoodIdAsync("Calm") : null,
                CategoryId = i % 5 == 0 ? await journal.CategoryIdAsync("Personal") : null
            }, i % 3 == 0 ? new[] { await journal.TagIdAsync("Travel") } : Array.Empty<int>());
        }
    }

    [Fact]
    public async Task An_empty_query_returns_everything_newest_first()
    {
        using var journal = new TestJournal();
        await SeedCorpusAsync(journal);

        var page = await journal.Entries.SearchAsync(new EntryQuery(), 1, 100);

        Assert.Equal(12, page.TotalCount);
        Assert.True(page.Items[0].EntryDate > page.Items[^1].EntryDate);
    }

    [Fact]
    public async Task Text_search_matches_content()
    {
        using var journal = new TestJournal();
        await SeedCorpusAsync(journal);

        var page = await journal.Entries.SearchAsync(new EntryQuery { SearchText = "pineapple" }, 1, 50);

        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, entry => Assert.Contains("pineapple", entry.Content));
    }

    [Fact]
    public async Task Text_search_matches_title()
    {
        using var journal = new TestJournal();
        await SeedCorpusAsync(journal);

        var page = await journal.Entries.SearchAsync(new EntryQuery { SearchText = "Even day" }, 1, 50);

        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, entry => Assert.Contains("Even", entry.Title));
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("%%")]
    public async Task Like_wildcards_in_the_search_term_are_matched_literally(string term)
    {
        using var journal = new TestJournal();
        await SeedCorpusAsync(journal);

        // Unescaped, "%" would match every row. None of the seeded entries
        // contain these characters, so a correct implementation finds nothing.
        var page = await journal.Entries.SearchAsync(new EntryQuery { SearchText = term }, 1, 50);

        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task Mood_filter_searches_secondary_slots_as_well_as_primary()
    {
        using var journal = new TestJournal();
        await SeedCorpusAsync(journal);
        var calm = await journal.MoodIdAsync("Calm");

        var page = await journal.Entries.SearchAsync(new EntryQuery { MoodIds = new[] { calm } }, 1, 50);

        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, entry => Assert.True(
            entry.PrimaryMoodId == calm ||
            entry.SecondaryMoodOneId == calm ||
            entry.SecondaryMoodTwoId == calm));
    }

    [Fact]
    public async Task Tag_filter_returns_only_tagged_entries()
    {
        using var journal = new TestJournal();
        await SeedCorpusAsync(journal);
        var travel = await journal.TagIdAsync("Travel");

        var page = await journal.Entries.SearchAsync(new EntryQuery { TagIds = new[] { travel } }, 1, 50);

        Assert.NotEmpty(page.Items);
        foreach (var entry in page.Items)
        {
            Assert.Contains(travel, await journal.Entries.GetTagIdsAsync(entry.Id));
        }
    }

    [Fact]
    public async Task Category_filter_returns_only_that_category()
    {
        using var journal = new TestJournal();
        await SeedCorpusAsync(journal);
        var personal = await journal.CategoryIdAsync("Personal");

        var page = await journal.Entries.SearchAsync(new EntryQuery { CategoryId = personal }, 1, 50);

        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, entry => Assert.Equal(personal, entry.CategoryId));
    }

    [Fact]
    public async Task Date_range_is_inclusive_on_both_bounds()
    {
        using var journal = new TestJournal();
        await SeedCorpusAsync(journal);

        var page = await journal.Entries.SearchAsync(new EntryQuery
        {
            FromDate = DateTime.Today.AddDays(-5),
            ToDate = DateTime.Today.AddDays(-3)
        }, 1, 50);

        Assert.Equal(3, page.TotalCount);
    }

    [Fact]
    public async Task Filters_combine_with_AND_not_OR()
    {
        using var journal = new TestJournal();
        await SeedCorpusAsync(journal);
        var sad = await journal.MoodIdAsync("Sad");

        var page = await journal.Entries.SearchAsync(new EntryQuery
        {
            SearchText = "day",
            FromDate = DateTime.Today.AddDays(-12),
            ToDate = DateTime.Today,
            MoodIds = new[] { sad }
        }, 1, 50);

        Assert.NotEmpty(page.Items);
        Assert.All(page.Items, entry => Assert.Equal(sad, entry.PrimaryMoodId));
    }

    [Fact]
    public async Task Pages_do_not_overlap_and_report_their_position()
    {
        using var journal = new TestJournal();
        await SeedCorpusAsync(journal);

        var first = await journal.Entries.SearchAsync(new EntryQuery(), 1, 5);
        var second = await journal.Entries.SearchAsync(new EntryQuery(), 2, 5);

        Assert.Equal(5, first.Items.Count);
        Assert.Equal(3, first.TotalPages);
        Assert.Empty(first.Items.Select(e => e.Id).Intersect(second.Items.Select(e => e.Id)));
        Assert.True(first.HasNext);
        Assert.False(first.HasPrevious);
        Assert.True(second.HasPrevious);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(999, 3)]
    public async Task Out_of_range_page_numbers_clamp_instead_of_throwing(int requested, int expected)
    {
        using var journal = new TestJournal();
        await SeedCorpusAsync(journal);

        var page = await journal.Entries.SearchAsync(new EntryQuery(), requested, 5);

        Assert.Equal(expected, page.Page);
    }

    [Fact]
    public async Task An_empty_result_set_is_still_well_formed()
    {
        using var journal = new TestJournal();
        await SeedCorpusAsync(journal);

        var page = await journal.Entries.SearchAsync(new EntryQuery { SearchText = "zzzznotfound" }, 1, 5);

        Assert.Equal(0, page.TotalCount);
        Assert.Equal(1, page.TotalPages);
        Assert.False(page.HasNext);
        Assert.Empty(page.Items);
    }
}
