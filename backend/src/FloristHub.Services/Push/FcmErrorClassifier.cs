using System.Text.Json;

namespace FloristHub.Services.Push;

public static class FcmErrorClassifier
{
    public static PushDeliveryOutcome Classify(int statusCode, string? body)
    {
        if (statusCode is >= 200 and < 300)
            return PushDeliveryOutcome.Sent;

        var errorCode = ReadErrorCode(body);
        var message = ReadMessage(body);

        if (statusCode == 404 || errorCode is "UNREGISTERED" or "NOT_FOUND")
            return PushDeliveryOutcome.InvalidToken;

        if (errorCode == "INVALID_ARGUMENT" && MentionsToken(message))
            return PushDeliveryOutcome.InvalidToken;

        if (statusCode == 429 || statusCode >= 500 || errorCode is "UNAVAILABLE" or "INTERNAL" or "QUOTA_EXCEEDED")
            return PushDeliveryOutcome.Retry;

        return PushDeliveryOutcome.Drop;
    }

    private static bool MentionsToken(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;
        return message.Contains("token", StringComparison.OrdinalIgnoreCase)
            || message.Contains("registration", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadErrorCode(string? body)
    {
        if (!TryRoot(body, out var error))
            return null;

        if (error.TryGetProperty("details", out var details) && details.ValueKind == JsonValueKind.Array)
        {
            foreach (var detail in details.EnumerateArray())
            {
                if (detail.TryGetProperty("errorCode", out var code) && code.ValueKind == JsonValueKind.String)
                    return code.GetString();
            }
        }

        if (error.TryGetProperty("status", out var fallback) && fallback.ValueKind == JsonValueKind.String)
            return fallback.GetString();

        return null;
    }

    private static string? ReadMessage(string? body)
    {
        if (!TryRoot(body, out var error))
            return null;
        return error.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String
            ? message.GetString()
            : null;
    }

    private static bool TryRoot(string? body, out JsonElement error)
    {
        error = default;
        if (string.IsNullOrWhiteSpace(body))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("error", out var found))
                return false;
            error = found.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
