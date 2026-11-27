namespace FloristHub.Services.Invoicing;

public static class VatRates
{
    public const decimal StandardMainland = 23m;
    public static readonly decimal[] Allowed = [23m, 22m, 16m, 13m, 6m, 0m];

    public static bool IsAllowed(decimal rate) => Allowed.Contains(rate);

    public static string InvoiceXpressName(decimal rate) => rate switch
    {
        23m => "IVA23",
        22m => "IVA22",
        16m => "IVA16",
        13m => "IVA13",
        6m => "IVA6",
        0m => "IVA0",
        _ => throw new InvalidOperationException($"Taxa de IVA {rate}% não é suportada.")
    };
}

public static class VatCalculator
{
    public static decimal NetUnit(decimal grossUnit, decimal vatPercent)
    {
        if (vatPercent <= 0)
            return decimal.Round(grossUnit, 4, MidpointRounding.AwayFromZero);
        return decimal.Round(grossUnit / (1 + vatPercent / 100m), 4, MidpointRounding.AwayFromZero);
    }

    public static (decimal Net, decimal Vat, decimal Gross) Summarize(IReadOnlyList<InvoiceLine> lines)
    {
        decimal net = 0;
        decimal gross = 0;
        foreach (var line in lines)
        {
            gross += decimal.Round(line.GrossUnitPrice * line.Quantity, 2, MidpointRounding.AwayFromZero);
            net += decimal.Round(line.NetUnitPrice * line.Quantity, 2, MidpointRounding.AwayFromZero);
        }

        return (net, gross - net, gross);
    }
}

public sealed record InvoiceLine(string Name, string? Description, decimal Quantity, decimal GrossUnitPrice, decimal VatRate)
{
    public decimal NetUnitPrice => VatCalculator.NetUnit(GrossUnitPrice, VatRate);
}
