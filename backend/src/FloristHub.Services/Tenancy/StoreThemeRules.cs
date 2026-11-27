using System.Globalization;
using System.Text.RegularExpressions;

namespace FloristHub.Services.Tenancy;

public static partial class StoreThemeRules
{
    public static readonly IReadOnlyList<string> AllowedFontKeys =
        ["system", "serif", "rounded", "classic"];

    [GeneratedRegex(@"^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex HexColorPattern();

    public static void EnsureValidTheme(string primaryHex, string accentHex, string? fontKey)
    {
        EnsureHexColor(primaryHex, "cor principal");
        EnsureHexColor(accentHex, "cor de destaque");

        if (!string.IsNullOrWhiteSpace(fontKey) && !AllowedFontKeys.Contains(fontKey, StringComparer.OrdinalIgnoreCase))
            throw new InvalidOperationException("Tipo de letra não suportado.");

        EnsureContrast(primaryHex, accentHex);
    }

    public static void EnsureHexColor(string value, string fieldLabel)
    {
        if (!HexColorPattern().IsMatch(value))
            throw new InvalidOperationException($"A {fieldLabel} tem de ser um código hex válido (#RRGGBB).");
    }

    public static void EnsureContrast(string primaryHex, string accentHex)
    {
        var l1 = RelativeLuminance(primaryHex);
        var l2 = RelativeLuminance(accentHex);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        var ratio = (lighter + 0.05) / (darker + 0.05);
        if (ratio < 2.5)
            throw new InvalidOperationException("As cores principal e de destaque não têm contraste suficiente.");
    }

    private static double RelativeLuminance(string hex)
    {
        var r = int.Parse(hex.AsSpan(1, 2), NumberStyles.HexNumber) / 255.0;
        var g = int.Parse(hex.AsSpan(3, 2), NumberStyles.HexNumber) / 255.0;
        var b = int.Parse(hex.AsSpan(5, 2), NumberStyles.HexNumber) / 255.0;
        return 0.2126 * Channel(r) + 0.7152 * Channel(g) + 0.0722 * Channel(b);
    }

    private static double Channel(double c) =>
        c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
}
