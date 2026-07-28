using Reflect.Models;

namespace Reflect.Services.Interfaces;

/// <summary>
/// Supplies the moods, tags and categories that entries are built from, and
/// creates new tags and categories on demand.
/// </summary>
public interface IReferenceDataService
{
    /// <summary>Returns all moods ordered by category then display order.</summary>
    Task<IReadOnlyList<Mood>> GetMoodsAsync();

    /// <summary>Returns moods keyed by id, for resolving an entry's mood slots.</summary>
    Task<IReadOnlyDictionary<int, Mood>> GetMoodLookupAsync();

    /// <summary>Returns all tags, pre-built and custom, ordered by name.</summary>
    Task<IReadOnlyList<Tag>> GetTagsAsync();

    /// <summary>Returns all categories ordered by name.</summary>
    Task<IReadOnlyList<Category>> GetCategoriesAsync();

    /// <summary>
    /// Returns the tag with this name, creating a custom one if it does not
    /// exist. Matching ignores case, so "work" resolves to the pre-built "Work"
    /// rather than creating a near-duplicate.
    /// </summary>
    Task<Tag> GetOrCreateTagAsync(string name);

    /// <summary>
    /// Returns the category with this name, creating it if absent. Matching
    /// ignores case, as for tags.
    /// </summary>
    Task<Category> GetOrCreateCategoryAsync(string name);
}
