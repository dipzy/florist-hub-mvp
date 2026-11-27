using FloristHub.Api.Localization;
using Microsoft.AspNetCore.Http;

namespace FloristHub.Api.Tests.Localization;

public class ApiLocalizationTests
{
    [Fact]
    public void TranslateMessage_returns_english_for_known_validation_text()
    {
        var en = ApiLocalization.TranslateMessage("NIF inválido.", "en");
        Assert.Equal("Invalid tax ID (NIF).", en);
    }

    [Fact]
    public void ResolveCulture_reads_accept_language_header()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.AcceptLanguage = "en-GB";
        Assert.Equal("en", ApiLocalization.ResolveCulture(context));
    }
}
