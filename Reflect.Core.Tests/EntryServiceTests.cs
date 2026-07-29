using Reflect.Models;
using Reflect.Services;

namespace Reflect.Core.Tests;

/// <summary>
/// Entry creation, the one-entry-per-day rule, tag synchronisation and querying.
/// </summary>
public sealed class EntryServiceTests
{
    [Fact]
    public async Task Save_assigns_an_id_and_timestamps()
    {
        using var journal = new TestJournal();

        var entry = await journal.WriteAsync(DateTime.Today, "First day");

        Assert.True(entry.Id > 0);
        Assert.NotEqual(default, entry.CreatedAt);
        Assert.NotEqual(default, entry.UpdatedAt);
    }

    [Fact]
    public async Task Save_normalises_the_entry_date_to_midnight()
    {
        using var journal = new TestJournal();

        var entry = await journal.Entries.SaveAsync(new JournalEntry
        {
            EntryDate = DateTime.Today.AddHours(14).AddMinutes(37),
            Title = "Afternoon",
            Content = "Written after lunch.",
            PrimaryMoodId = await journal.MoodIdAsync("Happy")
        }, Array.Empty<int>());

        Assert.Equal(DateTime.Today, entry.EntryDate);
    }

    [Fact]
    public async Task Save_trims_the_title()
    {
        using var journal = new TestJournal();

        var entry = await journal.WriteAsync(DateTime.Today, "   Padded   ");

        Assert.Equal("Padded", entry.Title);
    }

    [Fact]
    public async Task A_second_entry_on_the_same_day_is_rejected()
    {
        using var journal = new TestJournal();
        await journal.WriteAsync(DateTime.Today, "First");

        var failure = await Assert.ThrowsAsync<DuplicateEntryDateException>(() =>
            journal.WriteAsync(DateTime.Today, "Second"));

        Assert.Equal(DateTime.Today, failure.EntryDate);
    }

    [Fact]
    public async Task Updating_preserves_CreatedAt_and_advances_UpdatedAt()
    {
        using var journal = new TestJournal();
        var original = await journal.WriteAsync(DateTime.Today, "Before");
        var createdAt = original.CreatedAt;

        await Task.Delay(15);
        original.Title = "After";
        var updated = await journal.Entries.SaveAsync(original, Array.Empty<int>());

        Assert.Equal(createdAt, updated.CreatedAt);
        Assert.True(updated.UpdatedAt > createdAt);
    }

    [Fact]
    public async Task GetByDate_ignores_the_time_component()
    {
        using var journal = new TestJournal();
        var written = await journal.WriteAsync(DateTime.Today);

        var found = await journal.Entries.GetByDateAsync(DateTime.Today.AddHours(9));

        Assert.Equal(written.Id, found?.Id);
    }

    [Fact]
    public async Task ExistsForDate_reflects_whether_a_day_is_written()
    {
        using var journal = new TestJournal();
        await journal.WriteAsync(DateTime.Today);

        Assert.True(await journal.Entries.ExistsForDateAsync(DateTime.Today));
        Assert.False(await journal.Entries.ExistsForDateAsync(DateTime.Today.AddDays(-5)));
    }

    [Theory]
    [InlineData("", "An entry needs a title")]
    [InlineData("   ", "whitespace title")]
    public async Task Entries_without_a_title_are_rejected(string title, string _)
    {
        using var journal = new TestJournal();

        await Assert.ThrowsAsync<ArgumentException>(() => journal.Entries.SaveAsync(new JournalEntry
        {
            EntryDate = DateTime.Today,
            Title = title,
            PrimaryMoodId = 1
        }, Array.Empty<int>()));
    }

    [Fact]
    public async Task Entries_without_a_primary_mood_are_rejected()
    {
        using var journal = new TestJournal();

        await Assert.ThrowsAsync<ArgumentException>(() => journal.Entries.SaveAsync(new JournalEntry
        {
            EntryDate = DateTime.Today,
            Title = "No mood chosen",
            PrimaryMoodId = 0
        }, Array.Empty<int>()));
    }

    [Fact]
    public async Task Word_count_skips_tokens_that_are_only_markdown_syntax()
    {
        using var journal = new TestJournal();

        // "Heading", "Hello", "world,", "this", "has", "seven", "words", "here."
        // The "#" and "**" markers must not be counted.
        var entry = await journal.WriteAsync(DateTime.Today, "Counting",
            "# Heading\n\nHello world, this has **seven** words here.");

        Assert.Equal(8, entry.WordCount);
    }

    [Fact]
    public async Task A_secondary_mood_matching_the_primary_is_dropped()
    {
        using var journal = new TestJournal();
        var happy = await journal.MoodIdAsync("Happy");
        var calm = await journal.MoodIdAsync("Calm");

        var entry = await journal.Entries.SaveAsync(new JournalEntry
        {
            EntryDate = DateTime.Today,
            Title = "Mood rules",
            Content = "Testing.",
            PrimaryMoodId = happy,
            SecondaryMoodOneId = happy,  // same as primary
            SecondaryMoodTwoId = calm    // shifts up
        }, Array.Empty<int>());

        Assert.Equal(calm, entry.SecondaryMoodOneId);
        Assert.Null(entry.SecondaryMoodTwoId);
    }

    [Fact]
    public async Task Duplicate_secondary_moods_collapse_to_one()
    {
        using var journal = new TestJournal();
        var anxious = await journal.MoodIdAsync("Anxious");

        var entry = await journal.Entries.SaveAsync(new JournalEntry
        {
            EntryDate = DateTime.Today,
            Title = "Doubled",
            Content = "Testing.",
            PrimaryMoodId = await journal.MoodIdAsync("Happy"),
            SecondaryMoodOneId = anxious,
            SecondaryMoodTwoId = anxious
        }, Array.Empty<int>());

        Assert.Equal(anxious, entry.SecondaryMoodOneId);
        Assert.Null(entry.SecondaryMoodTwoId);
    }

    [Fact]
    public async Task Saving_replaces_the_tag_set_rather_than_adding_to_it()
    {
        using var journal = new TestJournal();
        var entry = await journal.WriteAsync(DateTime.Today, "Tagged", tags: new[] { "Work", "Travel" });

        Assert.Equal(2, (await journal.Entries.GetTagIdsAsync(entry.Id)).Count);

        var health = await journal.TagIdAsync("Health");
        await journal.Entries.SaveAsync(entry, new[] { health });

        var after = await journal.Entries.GetTagIdsAsync(entry.Id);
        Assert.Equal(new[] { health }, after);
    }

    [Fact]
    public async Task Deleting_an_entry_removes_its_tag_links_and_frees_the_day()
    {
        using var journal = new TestJournal();
        var entry = await journal.WriteAsync(DateTime.Today, "Doomed", tags: new[] { "Work" });

        await journal.Entries.DeleteAsync(entry.Id);

        Assert.Null(await journal.Entries.GetByIdAsync(entry.Id));
        Assert.Empty(await journal.Entries.GetTagIdsAsync(entry.Id));
        Assert.False(await journal.Entries.ExistsForDateAsync(DateTime.Today));
    }

    [Fact]
    public async Task Deleting_an_id_that_does_not_exist_is_a_no_op()
    {
        using var journal = new TestJournal();

        var exception = await Record.ExceptionAsync(() => journal.Entries.DeleteAsync(999_999));

        Assert.Null(exception);
    }

    [Fact]
    public async Task GetRange_tolerates_reversed_bounds_and_returns_oldest_first()
    {
        using var journal = new TestJournal();
        for (var i = 0; i < 4; i++)
        {
            await journal.WriteAsync(DateTime.Today.AddDays(-i));
        }

        var range = await journal.Entries.GetRangeAsync(DateTime.Today, DateTime.Today.AddDays(-3));

        Assert.Equal(4, range.Count);
        Assert.True(range[0].EntryDate < range[^1].EntryDate);
    }

    [Fact]
    public async Task GetEntryDates_returns_midnight_normalised_days()
    {
        using var journal = new TestJournal();
        await journal.WriteAsync(DateTime.Today);
        await journal.WriteAsync(DateTime.Today.AddDays(-2));

        var dates = await journal.Entries.GetEntryDatesAsync(DateTime.Today.AddDays(-5), DateTime.Today);

        Assert.Equal(2, dates.Count);
        Assert.All(dates, date => Assert.Equal(TimeSpan.Zero, date.TimeOfDay));
    }
}
