using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Reflect.Models;
using Reflect.Services.Interfaces;

// MAUI defines its own IContainer and Colors, so name the QuestPDF ones explicitly.
using IPdfContainer = QuestPDF.Infrastructure.IContainer;
using PdfColors = QuestPDF.Helpers.Colors;

namespace Reflect.Services;

/// <summary>
/// Renders journal entries to PDF with QuestPDF.
/// </summary>
/// <remarks>
/// Entry content is Markdown, and rather than reproduce Markdown styling in PDF
/// it is flattened to readable prose through the shared renderer and laid out as
/// paragraphs. A faithful Markdown-to-PDF mapping would be a project of its own;
/// the specification asks for entries to be exportable and readable, which this
/// satisfies without pulling in a second rendering pipeline.
///
/// QuestPDF is used under its Community licence, which covers individuals and
/// small organisations. The licence type must be declared before the first
/// document is generated or the library refuses to render.
/// </remarks>
public sealed class PdfJournalExporter : IJournalExporter
{
    private readonly IEntryService _entries;
    private readonly IReferenceDataService _referenceData;
    private readonly IMarkdownRenderer _markdown;
    private readonly ILogger<PdfJournalExporter> _logger;

    static PdfJournalExporter() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public PdfJournalExporter(
        IEntryService entries,
        IReferenceDataService referenceData,
        IMarkdownRenderer markdown,
        ILogger<PdfJournalExporter> logger)
    {
        _entries = entries;
        _referenceData = referenceData;
        _markdown = markdown;
        _logger = logger;
    }

    /// <inheritdoc />
    public string SuggestFileName(DateTime from, DateTime to)
    {
        var (start, end) = Order(from, to);
        return $"reflect-journal-{start:yyyy-MM-dd}-to-{end:yyyy-MM-dd}.pdf";
    }

    /// <inheritdoc />
    public async Task<int> ExportRangeAsync(DateTime from, DateTime to, Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);

        var (start, end) = Order(from, to);

        var entries = await _entries.GetRangeAsync(start, end).ConfigureAwait(false);
        var moods = await _referenceData.GetMoodLookupAsync().ConfigureAwait(false);
        var categories = await _referenceData.GetCategoriesAsync().ConfigureAwait(false);
        var tags = await _referenceData.GetTagsAsync().ConfigureAwait(false);

        var categoryNames = categories.ToDictionary(category => category.Id, category => category.Name);
        var tagNames = tags.ToDictionary(tag => tag.Id, tag => tag.Name);

        // Tag ids are fetched per entry rather than in one query because the
        // export is an occasional, user-initiated action where clarity beats
        // shaving a few round trips.
        var tagsByEntry = new Dictionary<int, IReadOnlyList<int>>();
        foreach (var entry in entries)
        {
            tagsByEntry[entry.Id] = await _entries.GetTagIdsAsync(entry.Id).ConfigureAwait(false);
        }

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(style => style.FontSize(11).LineHeight(1.4f));

                page.Header().Column(header =>
                {
                    header.Item().Text("Reflect").FontSize(20).SemiBold();
                    header.Item().Text($"Journal entries from {start:d MMMM yyyy} to {end:d MMMM yyyy}")
                        .FontSize(10).FontColor(PdfColors.Grey.Darken1);
                    header.Item().PaddingTop(8).LineHorizontal(1).LineColor(PdfColors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(12).Column(column =>
                {
                    column.Spacing(18);

                    if (entries.Count == 0)
                    {
                        column.Item().Text("No entries in this range.")
                            .Italic().FontColor(PdfColors.Grey.Darken1);
                        return;
                    }

                    foreach (var entry in entries)
                    {
                        column.Item().Element(block => ComposeEntry(
                            block, entry, moods, categoryNames, tagNames,
                            tagsByEntry.GetValueOrDefault(entry.Id, Array.Empty<int>())));
                    }
                });

                page.Footer().AlignCenter().Text(footer =>
                {
                    footer.Span("Page ");
                    footer.CurrentPageNumber();
                    footer.Span(" of ");
                    footer.TotalPages();
                });
            });
        });

        document.GeneratePdf(destination);

        _logger.LogInformation(
            "Exported {Count} entries between {From:yyyy-MM-dd} and {To:yyyy-MM-dd}",
            entries.Count, start, end);

        return entries.Count;
    }

    /// <summary>Lays out a single entry: date, title, metadata line, then the prose.</summary>
    private void ComposeEntry(
        IPdfContainer container,
        JournalEntry entry,
        IReadOnlyDictionary<int, Mood> moods,
        IReadOnlyDictionary<int, string> categoryNames,
        IReadOnlyDictionary<int, string> tagNames,
        IReadOnlyList<int> entryTagIds)
    {
        container.Column(column =>
        {
            column.Spacing(4);

            column.Item().Text(entry.EntryDate.ToString("dddd, d MMMM yyyy"))
                .FontSize(9).FontColor(PdfColors.Grey.Darken1).SemiBold();

            column.Item().Text(entry.Title).FontSize(14).SemiBold();

            var metadata = BuildMetadataLine(entry, moods, categoryNames, tagNames, entryTagIds);
            if (metadata.Length > 0)
            {
                column.Item().Text(metadata).FontSize(9).FontColor(PdfColors.Grey.Darken2);
            }

            var prose = _markdown.ToPlainText(entry.Content);
            if (!string.IsNullOrWhiteSpace(prose))
            {
                foreach (var paragraph in SplitParagraphs(prose))
                {
                    column.Item().PaddingTop(2).Text(paragraph);
                }
            }

            column.Item().PaddingTop(4)
                .Text($"{entry.WordCount} words")
                .FontSize(8).FontColor(PdfColors.Grey.Medium);
        });
    }

    private static string BuildMetadataLine(
        JournalEntry entry,
        IReadOnlyDictionary<int, Mood> moods,
        IReadOnlyDictionary<int, string> categoryNames,
        IReadOnlyDictionary<int, string> tagNames,
        IReadOnlyList<int> entryTagIds)
    {
        var parts = new List<string>();

        var moodNames = new[] { entry.PrimaryMoodId, entry.SecondaryMoodOneId ?? 0, entry.SecondaryMoodTwoId ?? 0 }
            .Where(id => id > 0 && moods.ContainsKey(id))
            .Select(id => moods[id].Name)
            .ToArray();

        if (moodNames.Length > 0)
        {
            parts.Add($"Mood: {string.Join(", ", moodNames)}");
        }

        if (entry.CategoryId is int categoryId && categoryNames.TryGetValue(categoryId, out var categoryName))
        {
            parts.Add($"Category: {categoryName}");
        }

        var names = entryTagIds.Where(tagNames.ContainsKey).Select(id => tagNames[id]).ToArray();
        if (names.Length > 0)
        {
            parts.Add($"Tags: {string.Join(", ", names)}");
        }

        return string.Join("   |   ", parts);
    }

    /// <summary>
    /// Splits flattened content into paragraphs so the writer's structure
    /// survives into the document.
    /// </summary>
    /// <remarks>
    /// Markdig's plain-text renderer emits one line per Markdown block - a
    /// heading, a paragraph, a list item - separated by single newlines rather
    /// than blank lines. Splitting on blank lines therefore collapses an entire
    /// entry onto one line, which is what an earlier version did.
    /// </remarks>
    private static IEnumerable<string> SplitParagraphs(string text) => text
        .Replace("\r\n", "\n")
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(paragraph => paragraph.Trim())
        .Where(paragraph => paragraph.Length > 0);

    private static (DateTime Start, DateTime End) Order(DateTime from, DateTime to)
    {
        var start = from.Date;
        var end = to.Date;

        return start <= end ? (start, end) : (end, start);
    }
}
