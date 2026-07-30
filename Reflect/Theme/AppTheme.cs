using MudBlazor;

namespace Reflect.Theme;

// Theme for the app plus the colours used for moods.
//
// Most of it is just MudBlazor's defaults - I only changed the primary colour
// and left the rest alone so light and dark mode both keep working.
public static class AppTheme
{
    // Keyed by name rather than id, because the ids come from the database and
    // could be different on another install.
    private static readonly Dictionary<string, string> MoodColours = new(StringComparer.OrdinalIgnoreCase)
    {
        // Positive
        ["Happy"] = "#F9A825",
        ["Excited"] = "#F4511E",
        ["Relaxed"] = "#7CB342",
        ["Grateful"] = "#43A047",
        ["Confident"] = "#C0891F",
        // Neutral
        ["Calm"] = "#42A5F5",
        ["Thoughtful"] = "#5C86AD",
        ["Curious"] = "#7E8CA0",
        ["Nostalgic"] = "#9575CD",
        ["Bored"] = "#90A4AE",
        // Negative
        ["Sad"] = "#5C6BC0",
        ["Angry"] = "#E53935",
        ["Stressed"] = "#EF6C00",
        ["Lonely"] = "#8E6BA8",
        ["Anxious"] = "#AB47BC",
    };

    // Used for the legend and the mood breakdown.
    public const string PositiveColour = "#43A047";
    public const string NeutralColour = "#1E88E5";
    public const string NegativeColour = "#8E24AA";

    // Falls back to the category colour, so a mood added later still comes out
    // roughly the right shade.
    public static string ColourFor(string moodName, Models.MoodCategory category) =>
        MoodColours.TryGetValue(moodName, out var colour) ? colour : ColourFor(category);

    public static string ColourFor(Models.MoodCategory category) => category switch
    {
        Models.MoodCategory.Positive => PositiveColour,
        Models.MoodCategory.Negative => NegativeColour,
        _ => NeutralColour
    };

    public static MudTheme Build() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#4A6FA5",
            AppbarBackground = "#4A6FA5",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#7C9DD4",
        }
    };
}
