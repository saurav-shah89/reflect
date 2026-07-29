using SQLite;

namespace Reflect.Data;

/// <summary>
/// Provides the initialised SQLite connection used by the service layer.
/// </summary>
/// <remarks>
/// Services depend on this abstraction rather than on a concrete connection so
/// the storage mechanism can be swapped (for example for an in-memory database
/// during testing) without touching business logic.
/// </remarks>
public interface IJournalDatabase
{
    /// <summary>
    /// Returns the shared connection, creating the schema and seeding reference
    /// data on first call. Safe to call concurrently; initialisation runs once.
    /// </summary>
    Task<SQLiteAsyncConnection> GetConnectionAsync();
}
