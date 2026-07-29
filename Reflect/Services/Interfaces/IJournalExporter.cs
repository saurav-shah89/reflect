namespace Reflect.Services.Interfaces;

/// <summary>
/// Renders a range of entries to a PDF document.
/// </summary>
public interface IJournalExporter
{
    /// <summary>
    /// Writes entries between the two dates, inclusive, to <paramref name="destination"/>
    /// as a PDF. Oldest first.
    /// </summary>
    /// <remarks>
    /// Takes a stream rather than a file path so the caller decides where the
    /// document lands - a file on desktop, a share sheet on mobile, or a buffer
    /// in a test.
    /// </remarks>
    /// <returns>The number of entries written.</returns>
    Task<int> ExportRangeAsync(DateTime from, DateTime to, Stream destination);

    /// <summary>Suggested file name for a range, without a directory.</summary>
    string SuggestFileName(DateTime from, DateTime to);
}
