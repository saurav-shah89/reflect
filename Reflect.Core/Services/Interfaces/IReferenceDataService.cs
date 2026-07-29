using Reflect.Models;

namespace Reflect.Services.Interfaces;

// The moods, tags and categories that entries are built from.
public interface IReferenceDataService
{
    Task<IReadOnlyList<Mood>> GetMoodsAsync();

    // Keyed by id, for turning an entry's mood ids back into names.
    Task<IReadOnlyDictionary<int, Mood>> GetMoodLookupAsync();

    Task<IReadOnlyList<Tag>> GetTagsAsync();

    Task<IReadOnlyList<Category>> GetCategoriesAsync();

    // Case insensitive, so typing "work" finds the existing "Work" instead of
    // creating a second one.
    Task<Tag> GetOrCreateTagAsync(string name);

    Task<Category> GetOrCreateCategoryAsync(string name);
}
