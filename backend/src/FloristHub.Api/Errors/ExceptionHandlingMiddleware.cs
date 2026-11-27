using System.Net;
using System.Text.Json;
using FloristHub.Api.Localization;

namespace FloristHub.Api.Errors;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or UnauthorizedAccessException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Erro de negócio/pedido");
            await WriteErrorAsync(context, ex, ex switch
            {
                KeyNotFoundException => HttpStatusCode.NotFound,
                UnauthorizedAccessException => HttpStatusCode.Forbidden,
                _ => HttpStatusCode.BadRequest,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro não tratado");
            await WriteErrorAsync(context, ex, HttpStatusCode.InternalServerError, exposeMessage: false);
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        Exception ex,
        HttpStatusCode status,
        bool exposeMessage = true)
    {
        if (context.Response.HasStarted)
            throw ex;

        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json";
        var culture = ApiLocalization.ResolveCulture(context);
        var fallback = status == HttpStatusCode.InternalServerError
            ? "Ocorreu um erro inesperado. Tente novamente."
            : ex.Message;
        var message = exposeMessage ? ApiLocalization.TranslateMessage(ex.Message, culture) : fallback;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { message }));
    }
}
