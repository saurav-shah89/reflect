using Microsoft.Extensions.Logging.Abstractions;
using Reflect.Data;
using Reflect.Services;

namespace Reflect.Core.Tests;

/// <summary>
/// A throwaway journal database backed by a temporary file, torn down with the
/// test that created it.
/// </summary>
/// <remarks>
/// Uses the real <see cref="JournalDatabase"/> rather than a stand-in, so schema
/// creation and reference-data seeding are exercised by every test that touches
/// storage. A file rather than <c>:memory:</c> because sqlite-net opens its own
/// connections internally and an in-memory database would not be shared between
/// them.
/// </remarks>
public sealed class TestJournal : IDisposable
{
    private readonly string _path;

    public TestJournal()
    {
        _path = Path.Combine(Path.GetTempPath(), $"reflect-test-{Guid.NewGuid():N}.db3");

        Database = new JournalDatabase(_path, NullLogger<JournalDatabase>.Instance);
        Entries = new EntryService(Database, NullLogger<EntryService>.Instance);
        Reference = new ReferenceDataService(Database, NullLogger<ReferenceDataService>.Instance);
        Analytics = new AnalyticsService(Database, Reference, NullLogger<AnalyticsService>.Instance);
        Settings = new SettingsService(Database, NullLogger<SettingsService>.Instance);
        Markdown = new MarkdownRenderer();
        Exporter = new PdfJournalExporter(Entries, Reference, Markdown,
            NullLogger<PdfJournalExporter>.Instance);
    }

    public JournalDatabase Database { get; }

    public EntryService Entries { get; }

    public ReferenceDataService Reference { get; }

    public AnalyticsService Analytics { get; }

    public SettingsService Settings { get; }

    public MarkdownRenderer Markdown { get; }

    public PdfJournalExporter Exporter { get; }

    /// <summary>Returns the id of a seeded mood by name.</summary>
    public async Task<int> MoodIdAsync(string name)
    {
        var moods = await Reference.GetMoodsAsync();
        return moods.First(mood => mood.Name == name).Id;
    }

    /// <summary>Returns the id of a seeded tag by name.</summary>
    public async Task<int> TagIdAsync(string name)
    {
        var tags = await Reference.GetTagsAsync();
        return tags.First(tag => tag.Name == name).Id;
    }

    /// <summary>Returns the id of a seeded category by name.</summary>
    public async Task<int> CategoryIdAsync(string name)
    {
        var categories = await Reference.GetCategoriesAsync();
        return categories.First(category => category.Name == name).Id;
    }

    /// <summary>Saves a minimal valid entry on the given day, for tests that only care that one exists.</summary>
    public async Task<Models.JournalEntry> WriteAsync(
        DateTime date,
        string title = "Entry",
        string content = "Some content here.",
        string mood = "Happy",
        string? category = null,
        params string[] tags)
    {
        var tagIds = new List<int>();
        foreach (var tag in tags)
        {
            tagIds.Add(await TagIdAsync(tag));
        }

        return await Entries.SaveAsync(new Models.JournalEntry
        {
            EntryDate = date,
            Title = title,
            Content = content,
            PrimaryMoodId = await MoodIdAsync(mood),
            CategoryId = category is null ? null : await CategoryIdAsync(category)
        }, tagIds);
    }

    public void Dispose()
    {
        try
        {
            File.Delete(_path);
        }
        catch (IOException)
        {
            // A held handle only leaves a stray temp file; not worth failing a test.
        }
    }
}
