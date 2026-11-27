using System.Security.Cryptography;

namespace FloristHub.Services.Orders;

public static class OrderTrackingToken
{
    public static string Create()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    public static bool LooksValid(string? token) =>
        !string.IsNullOrWhiteSpace(token) && token.Length is >= 40 and <= 64;

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
