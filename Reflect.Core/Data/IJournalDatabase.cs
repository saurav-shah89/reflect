using SQLite;

namespace Reflect.Data;

// The services take this rather than a SQLiteAsyncConnection directly, so the
// database can be swapped without changing them.
public interface IJournalDatabase
{
    // Creates the tables and seeds the reference data the first time it's
    // called. Safe to call from more than one place at once.
    Task<SQLiteAsyncConnection> GetConnectionAsync();
}
