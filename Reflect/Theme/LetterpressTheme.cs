using MudBlazor;

namespace Reflect.Theme;

// Colours and sizes for the Letterpress design.
//
// Only colour and geometry are here. The fonts, rules, grain and folio numbers
// are in app.css instead, because they need selectors and pseudo-elements that
// you can't set from a theme object - and keeping them in one place stops the
// same values being written twice.
//
// The main rule of the design is that nothing is rounded and nothing floats, so
// corners are square and there are no shadows anywhere.
public static class LetterpressTheme
{
    // Light — warm paper stock under a deep press red.
    public const string LightBackdrop = "#C9C6BE";
    public const string LightPaper = "#FCFBF8";
    public const string LightPaper2 = "#F5F3EE";
    public const string LightPaper3 = "#EBE8E1";
    public const string LightInk = "#16150F";
    public const string LightMuted = "#5E5A50";
    public const string LightFaint = "#6E6A5F";
    public const string LightRule = "#DFDBD1";
    public const string LightRuleStrong = "#C4BFB2";
    public const string LightAccent = "#A81E12";
    public const string LightAccentSoft = "#F6E7E4";

    // Dark — the same press, inked on near-black.
    public const string DarkBackdrop = "#080807";
    public const string DarkPaper = "#111110";
    public const string DarkPaper2 = "#181816";
    public const string DarkPaper3 = "#20201D";
    public const string DarkSide = "#0D0D0C";
    public const string DarkInk = "#EFEDE6";
    public const string DarkMuted = "#A6A199";
    public const string DarkFaint = "#8E8A80";
    public const string DarkRule = "#252523";
    public const string DarkRuleStrong = "#35342F";
    public const string DarkAccent = "#E0483A";
    public const string DarkAccentSoft = "#241413";

    // Font stacks. Each falls back to a face present on every desktop.
    public static readonly string[] DisplayFont = { "Bodoni Moda", "Didot", "Georgia", "serif" };

    public static readonly string[] ProseFont = { "Newsreader", "Georgia", "serif" };

    public static readonly string[] ChromeFont = { "IBM Plex Mono", "ui-monospace", "Consolas", "monospace" };

    // Keyed by name rather than id, because the ids come from the database and
    // could be different on another install.
    private static readonly Dictionary<string, string> MoodColours = new(StringComparer.OrdinalIgnoreCase)
    {
        // Positive
        ["Happy"] = "#E3A93F",
        ["Excited"] = "#E4763B",
        ["Relaxed"] = "#7FB069",
        ["Grateful"] = "#57A773",
        ["Confident"] = "#C98A3F",
        // Neutral
        ["Calm"] = "#7BA6C9",
        ["Thoughtful"] = "#6C8EAD",
        ["Curious"] = "#8E9BAE",
        ["Nostalgic"] = "#A89BB0",
        ["Bored"] = "#9CA39F",
        // Negative
        ["Sad"] = "#8494BE",
        ["Angry"] = "#CB6553",
        ["Stressed"] = "#C87C60",
        ["Lonely"] = "#A487A5",
        ["Anxious"] = "#B287B4",
    };

    // Category colours used for grouped figures and legends.
    public const string PositiveColour = "#57A773";
    public const string NeutralColour = "#6E97BE";
    public const string NegativeColour = "#9B6E93";

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

    // The MudBlazor theme both palettes are wired into.
    public static MudTheme Build() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = LightAccent,
            Secondary = LightMuted,
            Background = LightBackdrop,
            BackgroundGray = LightPaper2,
            Surface = LightPaper,
            AppbarBackground = LightPaper,
            AppbarText = LightInk,
            DrawerBackground = LightPaper2,
            DrawerText = LightMuted,
            DrawerIcon = LightMuted,
            TextPrimary = LightInk,
            TextSecondary = LightMuted,
            TextDisabled = LightFaint,
            ActionDefault = LightMuted,
            ActionDisabled = LightFaint,
            Divider = LightRule,
            DividerLight = LightRule,
            LinesDefault = LightRule,
            LinesInputs = LightRuleStrong,
            TableLines = LightRule,
            Success = PositiveColour,
            Info = NeutralColour,
            Warning = "#C98A3F",
            Error = LightAccent,
        },
        PaletteDark = new PaletteDark
        {
            Primary = DarkAccent,
            Secondary = DarkMuted,
            Background = DarkBackdrop,
            BackgroundGray = DarkPaper2,
            Surface = DarkPaper,
            AppbarBackground = DarkPaper,
            AppbarText = DarkInk,
            DrawerBackground = DarkSide,
            DrawerText = DarkMuted,
            DrawerIcon = DarkMuted,
            TextPrimary = DarkInk,
            TextSecondary = DarkMuted,
            TextDisabled = DarkFaint,
            ActionDefault = DarkMuted,
            ActionDisabled = DarkFaint,
            Divider = DarkRule,
            DividerLight = DarkRule,
            LinesDefault = DarkRule,
            LinesInputs = DarkRuleStrong,
            TableLines = DarkRule,
            Success = PositiveColour,
            Info = NeutralColour,
            Warning = "#C98A3F",
            Error = DarkAccent,
        },
        LayoutProperties = new LayoutProperties
        {
            // Square corners are the whole point of the style.
            DefaultBorderRadius = "0",
            DrawerWidthLeft = "236px",
            AppbarHeight = "0px",
        }
    };
}
