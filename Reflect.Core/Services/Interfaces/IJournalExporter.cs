namespace Reflect.Services.Interfaces;

// Writes entries out as a PDF.
public interface IJournalExporter
{
    // Takes a Stream instead of a file path so the caller decides where it ends
    // up - a file on Windows, the share sheet on mobile. Returns how many
    // entries were written.
    Task<int> ExportRangeAsync(DateTime from, DateTime to, Stream destination);

    string SuggestFileName(DateTime from, DateTime to);
}
