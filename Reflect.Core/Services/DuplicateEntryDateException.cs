namespace Reflect.Services;

// Its own exception type so the editor can show "you already wrote on this day"
// instead of a raw SQLite constraint error.
public sealed class DuplicateEntryDateException : Exception
{
    public DuplicateEntryDateException(DateTime entryDate)
        : base($"A journal entry already exists for {entryDate:d MMMM yyyy}.")
    {
        EntryDate = entryDate;
    }

    public DateTime EntryDate { get; }
}
