namespace Reflect.Services.Interfaces;

public interface IMarkdownRenderer
{
    string ToHtml(string? markdown);

    // Plain text for the preview lines in lists, where HTML would just show up
    // as tags.
    string ToPlainText(string? markdown);
}
