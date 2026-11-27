namespace FloristHub.Services.Localization;

/// <summary>Cultura do pedido HTTP (pt-PT por defeito) para mensagens de validação.</summary>
public static class RequestCultureScope
{
    public const string DefaultCulture = "pt-PT";

    private static readonly AsyncLocal<string?> Current = new();

    public static string CurrentCulture => Current.Value ?? DefaultCulture;

    public static bool IsEnglish =>
        CurrentCulture.StartsWith("en", StringComparison.OrdinalIgnoreCase);

    public static void SetCulture(string? culture)
    {
        Current.Value = string.IsNullOrWhiteSpace(culture) ? null : culture;
    }

    public static string Pick(string portuguese, string english) =>
        IsEnglish ? english : portuguese;
}
