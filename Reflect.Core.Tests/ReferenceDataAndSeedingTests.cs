using Reflect.Models;

namespace Reflect.Core.Tests;

/// <summary>
/// Schema creation, reference-data seeding and custom tag handling.
/// </summary>
public sealed class ReferenceDataAndSeedingTests
{
    [Fact]
    public async Task The_specification_reference_data_is_seeded_on_first_use()
    {
        using var journal = new TestJournal();

        var moods = await journal.Reference.GetMoodsAsync();
        var tags = await journal.Reference.GetTagsAsync();
        var categories = await journal.Reference.GetCategoriesAsync();

        Assert.Equal(15, moods.Count);
        Assert.Equal(31, tags.Count);
        Assert.Equal(6, categories.Count);
    }

    [Theory]
    [InlineData(MoodCategory.Positive, "Happy", "Excited", "Relaxed", "Grateful", "Confident")]
    [InlineData(MoodCategory.Neutral, "Calm", "Thoughtful", "Curious", "Nostalgic", "Bored")]
    [InlineData(MoodCategory.Negative, "Sad", "Angry", "Stressed", "Lonely", "Anxious")]
    public async Task Each_mood_category_holds_exactly_the_specified_moods(
        MoodCategory category, params string[] expected)
    {
        using var journal = new TestJournal();

        var actual = (await journal.Reference.GetMoodsAsync())
            .Where(mood => mood.Category == category)
            .Select(mood => mood.Name)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task Opening_the_database_again_does_not_duplicate_reference_data()
    {
        using var journal = new TestJournal();
        await journal.Reference.GetMoodsAsync();

        // A second database over the same file re-runs schema creation and seeding.
        var second = new Data.JournalDatabase(journal.Database.DatabasePath,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Data.JournalDatabase>.Instance);
        var connection = await second.GetConnectionAsync();

        Assert.Equal(15, await connection.Table<Mood>().CountAsync());
        Assert.Equal(31, await connection.Table<Tag>().CountAsync());
        Assert.Equal(6, await connection.Table<Category>().CountAsync());
        Assert.Equal(1, await connection.Table<AppSettings>().CountAsync());
    }

    [Fact]
    public async Task An_existing_tag_name_resolves_rather_than_creating_a_duplicate()
    {
        using var journal = new TestJournal();
        var before = (await journal.Reference.GetTagsAsync()).Count;

        // Different case from the seeded "Work".
        var tag = await journal.Reference.GetOrCreateTagAsync("work");

        Assert.Equal("Work", tag.Name);
        Assert.False(tag.IsCustom);
        Assert.Equal(before, (await journal.Reference.GetTagsAsync()).Count);
    }

    [Fact]
    public async Task A_genuinely_new_tag_is_created_and_marked_custom()
    {
        using var journal = new TestJournal();
        var before = (await journal.Reference.GetTagsAsync()).Count;

        var tag = await journal.Reference.GetOrCreateTagAsync("Woodworking");

        Assert.True(tag.IsCustom);
        Assert.Equal(before + 1, (await journal.Reference.GetTagsAsync()).Count);
    }

    [Fact]
    public async Task Tag_names_are_trimmed()
    {
        using var journal = new TestJournal();

        var tag = await journal.Reference.GetOrCreateTagAsync("  Spaced Out  ");

        Assert.Equal("Spaced Out", tag.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Blank_tag_names_are_rejected(string name)
    {
        using var journal = new TestJournal();

        await Assert.ThrowsAsync<ArgumentException>(
            () => journal.Reference.GetOrCreateTagAsync(name));
    }

    [Fact]
    public async Task An_overlong_tag_name_is_rejected()
    {
        using var journal = new TestJournal();

        await Assert.ThrowsAsync<ArgumentException>(
            () => journal.Reference.GetOrCreateTagAsync(new string('x', 41)));
    }

    [Fact]
    public async Task An_existing_category_name_resolves_rather_than_duplicating()
    {
        using var journal = new TestJournal();
        var before = (await journal.Reference.GetCategoriesAsync()).Count;

        var category = await journal.Reference.GetOrCreateCategoryAsync("PERSONAL");

        Assert.Equal("Personal", category.Name);
        Assert.Equal(before, (await journal.Reference.GetCategoriesAsync()).Count);
    }

    [Fact]
    public async Task The_mood_lookup_covers_every_mood()
    {
        using var journal = new TestJournal();

        var moods = await journal.Reference.GetMoodsAsync();
        var lookup = await journal.Reference.GetMoodLookupAsync();

        Assert.Equal(moods.Count, lookup.Count);
        Assert.All(moods, mood => Assert.True(lookup.ContainsKey(mood.Id)));
    }

    [Fact]
    public async Task A_database_path_is_required()
    {
        Assert.Throws<ArgumentException>(() => new Data.JournalDatabase(
            "  ",
            Microsoft.Extensions.Logging.Abstractions.NullLogger<Data.JournalDatabase>.Instance));
    }
}
