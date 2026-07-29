namespace Reflect.Services.Interfaces;

/// <summary>
/// Converts entry content from Markdown to HTML for display.
/// </summary>
public interface IMarkdownRenderer
{
    /// <summary>
    /// Renders Markdown to HTML. Returns an empty string for null or blank input.
    /// </summary>
    string ToHtml(string? markdown);

    /// <summary>
    /// Strips Markdown formatting and returns readable plain text, for previews
    /// and list summaries where rendered HTML would be noise.
    /// </summary>
    string ToPlainText(string? markdown);
}
