namespace FloristHub.Services.Payments;

public static class CommissionCalculator
{
    public static decimal ResolvePercent(decimal globalPercent, decimal? tenantOverride)
    {
        var percent = tenantOverride ?? globalPercent;
        EnsurePercent(percent);
        return percent;
    }

    public static void EnsurePercent(decimal percent)
    {
        if (percent < 0 || percent > 100 || percent != decimal.Round(percent, 2))
            throw new InvalidOperationException("A comissão tem de estar entre 0 e 100, com no máximo duas casas decimais.");
    }

    public static void EnsureMonthlyFee(decimal amount)
    {
        if (amount < 0 || amount != decimal.Round(amount, 2))
            throw new InvalidOperationException("A mensalidade tem de ser zero ou positiva, com no máximo duas casas decimais.");
    }

    public static long ToCents(decimal euros) =>
        (long)decimal.Round(euros * 100m, 0, MidpointRounding.AwayFromZero);

    public static decimal FromCents(long cents) => cents / 100m;

    public static long ApplicationFeeCents(decimal amountEuros, decimal percent)
    {
        var cents = ToCents(amountEuros);
        return (long)decimal.Round(cents * percent / 100m, 0, MidpointRounding.AwayFromZero);
    }
}
