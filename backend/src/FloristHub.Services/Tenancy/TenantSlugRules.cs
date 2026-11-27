using System.Text.RegularExpressions;

namespace FloristHub.Services.Tenancy;

public static partial class TenantSlugRules
{
    [GeneratedRegex(@"^[a-z0-9](?:[a-z0-9-]{0,98}[a-z0-9])?$", RegexOptions.CultureInvariant)]
    private static partial Regex SlugPattern();

    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "www", "api", "admin", "superadmin", "app", "mail", "static", "assets"
    };

    public static void EnsureValidSlug(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new InvalidOperationException("O slug da loja é obrigatório.");

        var normalized = slug.Trim().ToLowerInvariant();
        if (normalized.Length is < 2 or > 100)
            throw new InvalidOperationException("O slug deve ter entre 2 e 100 caracteres.");

        if (Reserved.Contains(normalized))
            throw new InvalidOperationException("Este slug está reservado.");

        if (!SlugPattern().IsMatch(normalized))
            throw new InvalidOperationException("O slug só pode conter letras minúsculas, números e hífens.");
    }

    public static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant();
}
