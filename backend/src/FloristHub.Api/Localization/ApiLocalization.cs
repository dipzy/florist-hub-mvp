namespace FloristHub.Api.Localization;

/// <summary>Traduções simples de mensagens de validação/negócio para pedidos em inglês.</summary>
public static class ApiLocalization
{
    public const string CultureItemKey = "FloristHub.Locale";

    private static readonly Dictionary<string, string> EnByPt = new(StringComparer.Ordinal)
    {
        ["A encomenda precisa de pelo menos um item."] = "The order must include at least one item.",
        ["Indique a morada de entrega."] = "Enter a delivery address.",
        ["NIF inválido."] = "Invalid tax ID (NIF).",
        ["O endereço da pré-visualização do arranjo não é válido."] = "The arrangement preview URL is not valid.",
        ["A data/hora pretendida deve ser no futuro (ou muito recente)."] = "The requested delivery time must be in the future (or very recent).",
        ["JSON de itens propostos inválido."] = "Invalid proposed items JSON.",
        ["Indique o motivo da recusa."] = "Provide a reason for rejection.",
        ["O motivo da recusa é demasiado longo."] = "The rejection reason is too long.",
        ["Pedido recusado."] = "Request rejected.",
        ["Taxa de IVA inválida."] = "Invalid VAT rate.",
    };

    public static string ResolveCulture(HttpContext context)
    {
        if (context.Items.TryGetValue(CultureItemKey, out var stored) && stored is string s && !string.IsNullOrWhiteSpace(s))
            return s;

        var header = context.Request.Headers.AcceptLanguage.ToString();
        if (header.AsSpan().TrimStart().StartsWith("en", StringComparison.OrdinalIgnoreCase))
            return "en";
        return "pt-PT";
    }

    public static string TranslateMessage(string message, string culture)
    {
        if (string.IsNullOrWhiteSpace(message)) return message;
        if (!culture.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return message;
        if (EnByPt.TryGetValue(message.Trim(), out var en)) return en;

        // Mensagens compostas (FluentValidation junta várias com espaço).
        if (message.Contains(' ', StringComparison.Ordinal))
        {
            var parts = message.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length > 1 && parts.All(p => EnByPt.ContainsKey(p)))
                return string.Join(' ', parts.Select(p => EnByPt[p]));
        }

        return message;
    }
}
