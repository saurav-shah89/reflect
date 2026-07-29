using MudBlazor;

namespace Reflect.Theme;

/// <summary>
/// The Letterpress visual language, taken from the DailyScribe v3 design.
/// </summary>
/// <remarks>
/// Only colour and geometry live here. Typography, rules, grain and the folio
/// numerals are expressed in <c>app.css</c> instead: they depend on selectors
/// and pseudo-elements that a theme object cannot reach, and keeping them in one
/// stylesheet avoids the same values being defined in two places.
///
/// The defining geometry is that nothing is rounded and nothing floats. Corners
/// are square and shadows are absent, so depth comes from hairline rules and the
/// weight of the type rather than from elevation.
/// </remarks>
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

    /// <summary>Font stacks. Each falls back to a face present on every desktop.</summary>
    public static readonly string[] DisplayFont = { "Bodoni Moda", "Didot", "Georgia", "serif" };

    public static readonly string[] ProseFont = { "Newsreader", "Georgia", "serif" };

    public static readonly string[] ChromeFont = { "IBM Plex Mono", "ui-monospace", "Consolas", "monospace" };

    /// <summary>
    /// Mood colours from the design, keyed by the mood names the specification
    /// fixes. Looked up by name because the seeded ids are assigned by the
    /// database and are not stable across installs.
    /// </summary>
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

    /// <summary>Category colours used for grouped figures and legends.</summary>
    public const string PositiveColour = "#57A773";
    public const string NeutralColour = "#6E97BE";
    public const string NegativeColour = "#9B6E93";

    /// <summary>
    /// Returns the design's colour for a mood, falling back to its category
    /// colour so a mood added later still renders in the right family.
    /// </summary>
    public static string ColourFor(string moodName, Models.MoodCategory category) =>
        MoodColours.TryGetValue(moodName, out var colour) ? colour : ColourFor(category);

    public static string ColourFor(Models.MoodCategory category) => category switch
    {
        Models.MoodCategory.Positive => PositiveColour,
        Models.MoodCategory.Negative => NegativeColour,
        _ => NeutralColour
    };

    /// <summary>The MudBlazor theme both palettes are wired into.</summary>
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
