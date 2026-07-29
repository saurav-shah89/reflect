using System.Text;

namespace Reflect.Core.Tests;

/// <summary>
/// Markdown rendering and PDF export.
/// </summary>
public sealed class ExportAndRenderingTests
{
    [Fact]
    public void Markdown_renders_formatting_to_html()
    {
        var renderer = new Services.MarkdownRenderer();

        var html = renderer.ToHtml("# Title\n\nSome **bold** text.");

        Assert.Contains("<h1", html);
        Assert.Contains("<strong>bold</strong>", html);
    }

    [Fact]
    public void Raw_html_in_content_is_not_passed_through()
    {
        var renderer = new Services.MarkdownRenderer();

        var html = renderer.ToHtml("<script>alert('x')</script>");

        // Content renders straight into the WebView, so markup pasted into a
        // journal must not become executable script.
        Assert.DoesNotContain("<script>", html);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_content_renders_to_an_empty_string(string? content)
    {
        var renderer = new Services.MarkdownRenderer();

        Assert.Equal(string.Empty, renderer.ToHtml(content));
        Assert.Equal(string.Empty, renderer.ToPlainText(content));
    }

    [Fact]
    public void Plain_text_strips_markdown_syntax()
    {
        var renderer = new Services.MarkdownRenderer();

        var text = renderer.ToPlainText("# Heading\n\nSome **bold** text.");

        Assert.DoesNotContain("#", text);
        Assert.DoesNotContain("**", text);
        Assert.Contains("Heading", text);
        Assert.Contains("bold", text);
    }

    [Fact]
    public async Task Export_produces_a_structurally_valid_pdf()
    {
        using var journal = new TestJournal();
        await journal.WriteAsync(DateTime.Today, "Today's entry");
        await journal.WriteAsync(DateTime.Today.AddDays(-1), "Yesterday's entry");

        using var buffer = new MemoryStream();
        var count = await journal.Exporter.ExportRangeAsync(
            DateTime.Today.AddDays(-1), DateTime.Today, buffer);

        var bytes = buffer.ToArray();

        Assert.Equal(2, count);
        Assert.True(bytes.Length > 1000, $"only {bytes.Length} bytes");
        Assert.Equal(new byte[] { 0x25, 0x50, 0x44, 0x46 }, bytes[..4]);   // %PDF
        Assert.Contains("%%EOF", Encoding.ASCII.GetString(bytes[^1024..]));
    }

    [Fact]
    public async Task Export_of_an_empty_range_still_produces_a_valid_pdf()
    {
        using var journal = new TestJournal();
        await journal.WriteAsync(DateTime.Today);

        using var buffer = new MemoryStream();
        var count = await journal.Exporter.ExportRangeAsync(
            DateTime.Today.AddDays(-400), DateTime.Today.AddDays(-390), buffer);

        Assert.Equal(0, count);
        Assert.Equal(new byte[] { 0x25, 0x50, 0x44, 0x46 }, buffer.ToArray()[..4]);
    }

    [Fact]
    public async Task Export_tolerates_reversed_bounds()
    {
        using var journal = new TestJournal();
        await journal.WriteAsync(DateTime.Today);
        await journal.WriteAsync(DateTime.Today.AddDays(-1));

        using var buffer = new MemoryStream();
        var count = await journal.Exporter.ExportRangeAsync(
            DateTime.Today, DateTime.Today.AddDays(-1), buffer);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Export_rejects_a_null_destination()
    {
        using var journal = new TestJournal();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            journal.Exporter.ExportRangeAsync(DateTime.Today, DateTime.Today, null!));
    }

    [Fact]
    public void Suggested_file_name_carries_both_dates_and_ignores_bound_order()
    {
        using var journal = new TestJournal();
        var from = new DateTime(2026, 3, 1);
        var to = new DateTime(2026, 3, 31);

        var name = journal.Exporter.SuggestFileName(from, to);

        Assert.EndsWith(".pdf", name);
        Assert.Contains("2026-03-01", name);
        Assert.Contains("2026-03-31", name);
        Assert.Equal(name, journal.Exporter.SuggestFileName(to, from));
    }
}
