using Markdig;
using Reflect.Services.Interfaces;

namespace Reflect.Services;

// Markdown rendering using Markdig.
//
// Raw HTML is turned off. The output goes straight into the WebView, so any
// HTML pasted into an entry would otherwise run as script inside the app. With
// it disabled it just shows up as text.
//
// The pipeline is built once and kept, because building it every render would
// mean rebuilding it on every keystroke in the live preview.
public sealed class MarkdownRenderer : IMarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    public string ToHtml(string? markdown) =>
        string.IsNullOrWhiteSpace(markdown)
            ? string.Empty
            : Markdown.ToHtml(markdown, _pipeline);

    public string ToPlainText(string? markdown) =>
        string.IsNullOrWhiteSpace(markdown)
            ? string.Empty
            : Markdown.ToPlainText(markdown, _pipeline);
}
