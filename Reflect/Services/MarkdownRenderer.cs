using Markdig;
using Reflect.Services.Interfaces;

namespace Reflect.Services;

/// <summary>
/// Markdig-backed Markdown rendering.
/// </summary>
/// <remarks>
/// Raw HTML is disabled in the pipeline. Entry content is rendered straight into
/// the BlazorWebView, so markup pasted into a journal - deliberately or from the
/// clipboard - would otherwise execute as script inside the app. Disabling HTML
/// means such input is shown as literal text instead.
///
/// The pipeline is built once and reused: constructing it per render would parse
/// the extension set on every keystroke of the live preview.
/// </remarks>
public sealed class MarkdownRenderer : IMarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    /// <inheritdoc />
    public string ToHtml(string? markdown) =>
        string.IsNullOrWhiteSpace(markdown)
            ? string.Empty
            : Markdown.ToHtml(markdown, _pipeline);

    /// <inheritdoc />
    public string ToPlainText(string? markdown) =>
        string.IsNullOrWhiteSpace(markdown)
            ? string.Empty
            : Markdown.ToPlainText(markdown, _pipeline);
}
