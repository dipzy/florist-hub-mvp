using FloristHub.Services.Localization;

namespace FloristHub.Api.Localization;

public sealed class AcceptLanguageMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var header = context.Request.Headers.AcceptLanguage.ToString();
        var culture = header.AsSpan().TrimStart().StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : "pt-PT";
        context.Items[ApiLocalization.CultureItemKey] = culture;
        RequestCultureScope.SetCulture(culture);
        try
        {
            await next(context);
        }
        finally
        {
            RequestCultureScope.SetCulture(null);
        }
    }
}
