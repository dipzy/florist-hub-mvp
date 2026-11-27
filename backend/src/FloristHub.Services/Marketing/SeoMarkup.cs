using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace FloristHub.Services.Marketing;

public sealed record SeoPage(
    string Title,
    string Description,
    string CanonicalUrl,
    string? ImageUrl,
    string SiteName,
    string JsonLd);

public static class SeoMarkup
{
    public static string Robots(string sitemapUrl) =>
        "User-agent: *\nAllow: /\nDisallow: /admin\nDisallow: /login\nDisallow: /registo\nDisallow: /superadmin\nSitemap: " + sitemapUrl + "\n";

    public static string Sitemap(IEnumerable<string> urls)
    {
        var builder = new StringBuilder();
        builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
        builder.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");
        foreach (var url in urls)
        {
            builder.Append("  <url><loc>").Append(WebUtility.HtmlEncode(url)).Append("</loc></url>\n");
        }

        builder.Append("</urlset>\n");
        return builder.ToString();
    }

    public static string JsonLdFlorist(
        string name,
        string? description,
        string url,
        string? imageUrl,
        string? street,
        string? city,
        string? postalCode,
        decimal? rating,
        int reviewCount)
    {
        var payload = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = new[] { "Florist", "LocalBusiness" },
            ["name"] = name,
            ["description"] = description,
            ["url"] = url,
            ["image"] = imageUrl,
            ["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["streetAddress"] = street,
                ["addressLocality"] = city,
                ["postalCode"] = postalCode,
                ["addressCountry"] = "PT"
            }
        };
        if (reviewCount > 0 && rating is decimal value)
        {
            payload["aggregateRating"] = new Dictionary<string, object?>
            {
                ["@type"] = "AggregateRating",
                ["ratingValue"] = value.ToString("0.00", CultureInfo.InvariantCulture),
                ["reviewCount"] = reviewCount,
                ["bestRating"] = "5",
                ["worstRating"] = "1"
            };
        }

        return JsonSerializer.Serialize(payload).Replace("<", "\\u003c", StringComparison.Ordinal);
    }

    public static string Html(SeoPage page)
    {
        var title = Encode(page.Title);
        var description = Encode(Truncate(page.Description, 300));
        var canonical = Encode(page.CanonicalUrl);
        var image = string.IsNullOrWhiteSpace(page.ImageUrl) ? "" : Encode(page.ImageUrl);
        var imageTags = string.IsNullOrWhiteSpace(page.ImageUrl)
            ? ""
            : $"""
              <meta property="og:image" content="{image}" />
              <meta name="twitter:image" content="{image}" />
              """;
        return $"""
            <!DOCTYPE html>
            <html lang="pt">
            <head>
            <meta charset="utf-8" />
            <title>{title}</title>
            <meta name="description" content="{description}" />
            <link rel="canonical" href="{canonical}" />
            <meta property="og:type" content="website" />
            <meta property="og:locale" content="pt_PT" />
            <meta property="og:site_name" content="{Encode(page.SiteName)}" />
            <meta property="og:title" content="{title}" />
            <meta property="og:description" content="{description}" />
            <meta property="og:url" content="{canonical}" />
            {imageTags}
            <meta name="twitter:card" content="summary_large_image" />
            <meta name="twitter:title" content="{title}" />
            <meta name="twitter:description" content="{description}" />
            <script type="application/ld+json">{page.JsonLd}</script>
            </head>
            <body>
            <h1>{title}</h1>
            <p>{description}</p>
            <p><a href="{canonical}">Abrir a loja</a></p>
            </body>
            </html>
            """;
    }

    public static string Truncate(string? value, int max)
    {
        var text = (value ?? "").Trim();
        if (text.Length <= max)
            return text;
        return text[..(max - 1)].TrimEnd() + "…";
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? "");
}
