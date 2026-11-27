namespace FloristHub.Services.Invoicing;

public static class PortugueseNif
{
    public static bool IsValid(string? value)
    {
        if (!TryNormalize(value, out _))
            return false;
        return true;
    }

    public static string Normalize(string value)
    {
        if (!TryNormalize(value, out var nif))
            throw new InvalidOperationException("NIF inválido.");
        return nif;
    }

    public static bool TryNormalize(string? value, out string nif)
    {
        nif = "";
        if (string.IsNullOrWhiteSpace(value))
            return false;

        var text = value.Trim().Replace(" ", "", StringComparison.Ordinal);
        if (text.StartsWith("PT", StringComparison.OrdinalIgnoreCase))
            text = text[2..];
        if (text.Length != 9 || text.Any(c => !char.IsDigit(c)))
            return false;

        var sum = 0;
        for (var i = 0; i < 8; i++)
            sum += (text[i] - '0') * (9 - i);
        var mod = sum % 11;
        var check = mod < 2 ? 0 : 11 - mod;
        if (check != text[8] - '0')
            return false;

        nif = text;
        return true;
    }
}
