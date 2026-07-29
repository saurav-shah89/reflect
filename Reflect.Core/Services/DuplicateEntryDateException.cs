namespace Reflect.Services;

/// <summary>
/// Thrown when a save would put a second entry on a day that already has one.
/// </summary>
/// <remarks>
/// A dedicated exception type lets the UI show a specific, actionable message
/// ("you already wrote on this day") instead of surfacing a raw SQLite
/// constraint error.
/// </remarks>
public sealed class DuplicateEntryDateException : Exception
{
    public DuplicateEntryDateException(DateTime entryDate)
        : base($"A journal entry already exists for {entryDate:d MMMM yyyy}.")
    {
        EntryDate = entryDate;
    }

    /// <summary>The day that was already taken.</summary>
    public DateTime EntryDate { get; }
}
