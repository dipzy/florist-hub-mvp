using System.Net;
using System.Text.Json;
using FloristHub.Services.Email;
using FloristHub.Services.Interfaces.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FloristHub.Services.Tests.Email;

public class ResendEmailSenderTests
{
    [Fact]
    public async Task SendAsync_posts_expected_json_to_resend_api()
    {
        HttpRequestMessage? captured = null;
        string? capturedJson = null;
        var handler = new StubHttpMessageHandler(async (req, _) =>
        {
            captured = req;
            capturedJson = req.Content is null ? null : await req.Content.ReadAsStringAsync();
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.resend.com/") };
        var sender = new ResendEmailSender(
            http,
            Options.Create(new EmailOptions
            {
                From = "Loja <ola@flores.pt>",
                ResendApiKey = "re_test_key"
            }),
            NullLogger<ResendEmailSender>.Instance);

        await sender.SendAsync(new EmailMessage("cliente@demo.pt", "Assunto", "<p>Olá</p>"));

        Assert.NotNull(captured);
        Assert.Equal(HttpMethod.Post, captured!.Method);
        Assert.Equal("https://api.resend.com/emails", captured.RequestUri?.ToString());
        Assert.Equal("Bearer", captured.Headers.Authorization?.Scheme);
        Assert.Equal("re_test_key", captured.Headers.Authorization?.Parameter);

        using var doc = JsonDocument.Parse(capturedJson!);
        Assert.Equal("Loja <ola@flores.pt>", doc.RootElement.GetProperty("from").GetString());
        Assert.Equal("cliente@demo.pt", doc.RootElement.GetProperty("to")[0].GetString());
        Assert.Equal("Assunto", doc.RootElement.GetProperty("subject").GetString());
        Assert.Equal("<p>Olá</p>", doc.RootElement.GetProperty("html").GetString());
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
            _handler = handler;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            await _handler(request, cancellationToken);
    }
}
