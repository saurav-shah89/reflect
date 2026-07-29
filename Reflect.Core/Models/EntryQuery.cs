namespace Reflect.Models;

/// <summary>
/// Search and filter criteria for listing entries. Every property is optional;
/// an empty query matches all entries.
/// </summary>
/// <remarks>
/// Grouping the criteria into one object keeps service signatures stable as
/// filters are added, instead of growing a long parameter list.
/// </remarks>
public sealed class EntryQuery
{
    /// <summary>Free text matched against entry title and content.</summary>
    public string? SearchText { get; set; }

    /// <summary>Inclusive lower bound on <see cref="JournalEntry.EntryDate"/>.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Inclusive upper bound on <see cref="JournalEntry.EntryDate"/>.</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Mood ids to match. An entry matches if any of its three mood slots
    /// contains one of these ids.
    /// </summary>
    public IReadOnlyCollection<int> MoodIds { get; set; } = Array.Empty<int>();

    /// <summary>Tag ids to match. An entry matches if it carries any of these tags.</summary>
    public IReadOnlyCollection<int> TagIds { get; set; } = Array.Empty<int>();

    /// <summary>Restrict to a single category.</summary>
    public int? CategoryId { get; set; }

    /// <summary>True when no criteria have been supplied.</summary>
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(SearchText) &&
        FromDate is null &&
        ToDate is null &&
        MoodIds.Count == 0 &&
        TagIds.Count == 0 &&
        CategoryId is null;
}
