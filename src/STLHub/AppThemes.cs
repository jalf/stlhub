using Avalonia.Styling;

namespace STLHub;

/// <summary>
/// Defines custom theme variants used by the application.
/// </summary>
public static class AppThemes
{
    public static readonly ThemeVariant Nord = new("Nord", ThemeVariant.Dark);
    public static readonly ThemeVariant Dracula = new("Dracula", ThemeVariant.Dark);
    public static readonly ThemeVariant SolarizedDark = new("SolarizedDark", ThemeVariant.Dark);
    public static readonly ThemeVariant SolarizedLight = new("SolarizedLight", ThemeVariant.Light);

    public static ThemeVariant GetVariant(string key) => key switch
    {
        "Light" => ThemeVariant.Light,
        "Nord" => Nord,
        "Dracula" => Dracula,
        "SolarizedDark" => SolarizedDark,
        "SolarizedLight" => SolarizedLight,
        _ => ThemeVariant.Dark,
    };
}
