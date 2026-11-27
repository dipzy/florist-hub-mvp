using System.Net;
using FloristHub.Api.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace FloristHub.Api.Tests;

public class CorsErrorPipelineTests
{
    private const string Origin = "https://florist-hub-26m.pages.dev";

    [Fact]
    public async Task Respostas_de_erro_e_preflight_mantem_cors_da_origem_de_producao()
    {
        var port = 19000 + Random.Shared.Next(2000);
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.Services.AddCors();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("once", limiter =>
            {
                limiter.PermitLimit = 1;
                limiter.Window = TimeSpan.FromMinutes(1);
                limiter.QueueLimit = 0;
            });
        });

        var app = builder.Build();
        app.UseCors(policy => policy
            .WithOrigins(Origin)
            .AllowAnyHeader()
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"));
        app.UseRateLimiter();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.MapPost("/api/orders/{id}/confirm", (string id) =>
        {
            throw new InvalidOperationException("não confirmada");
        });
        app.MapGet("/boom", () =>
        {
            throw new Exception("boom");
        });
        app.MapGet("/limited", () => Results.Ok()).RequireRateLimiting("once");

        await app.StartAsync();
        try
        {
            var client = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}") };

            var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/orders/abc/confirm");
            preflight.Headers.Add("Origin", Origin);
            preflight.Headers.Add("Access-Control-Request-Method", "POST");
            var preflightResponse = await client.SendAsync(preflight);
            AssertCors(preflightResponse);

            var confirm = new HttpRequestMessage(HttpMethod.Post, "/api/orders/abc/confirm");
            confirm.Headers.Add("Origin", Origin);
            var confirmResponse = await client.SendAsync(confirm);
            Assert.Equal(HttpStatusCode.BadRequest, confirmResponse.StatusCode);
            AssertCors(confirmResponse);

            var crash = new HttpRequestMessage(HttpMethod.Get, "/boom");
            crash.Headers.Add("Origin", Origin);
            var crashResponse = await client.SendAsync(crash);
            Assert.Equal(HttpStatusCode.InternalServerError, crashResponse.StatusCode);
            AssertCors(crashResponse);

            var first = new HttpRequestMessage(HttpMethod.Get, "/limited");
            first.Headers.Add("Origin", Origin);
            var firstResponse = await client.SendAsync(first);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
            AssertCors(firstResponse);

            var limited = new HttpRequestMessage(HttpMethod.Get, "/limited");
            limited.Headers.Add("Origin", Origin);
            var limitedResponse = await client.SendAsync(limited);
            Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
            AssertCors(limitedResponse);
        }
        finally
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private static void AssertCors(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var values));
        Assert.Equal(Origin, Assert.Single(values));
    }
}
