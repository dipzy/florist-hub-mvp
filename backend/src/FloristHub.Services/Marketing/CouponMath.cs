using System.Globalization;
using System.Text;
using FloristHub.DAL.Enums;
using FloristHub.Services.Payments;

namespace FloristHub.Services.Marketing;

public readonly record struct CouponLine(Guid? FlowerId, string? Category, decimal LineGross);

public static class CouponMath
{
    public static decimal Round(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal LineGross(decimal unitPrice, int quantity) =>
        Round(unitPrice * quantity);

    public static string NormalizeCode(string? code)
    {
        var value = (code ?? "").Trim().ToUpperInvariant();
        if (value.Length is < 3 or > 32 || !value.All(c => char.IsAsciiLetterOrDigit(c) || c == '-'))
            throw new InvalidOperationException("O código do cupão tem de ter 3 a 32 letras, algarismos ou hífens.");
        return value;
    }

    public static string NormalizeCategory(string? category)
    {
        var value = (category ?? "").Trim();
        if (value.Length == 0 || value.Length > 80)
            throw new InvalidOperationException("A categoria tem de ter entre 1 e 80 caracteres.");
        return value.ToLowerInvariant();
    }

    public static void EnsureMoney(decimal value, string label)
    {
        if (value < 0 || value != Round(value))
            throw new InvalidOperationException($"{label} tem de ser zero ou positivo, com no máximo duas casas decimais.");
    }

    public static void EnsureDiscountValue(CouponDiscountType type, decimal value)
    {
        EnsureMoney(value, "O desconto");
        if (type == CouponDiscountType.Percent && (value <= 0 || value > 100))
            throw new InvalidOperationException("A percentagem do cupão tem de estar entre 0,01 e 100.");
        if (type == CouponDiscountType.FixedAmount && value <= 0)
            throw new InvalidOperationException("O valor fixo do cupão tem de ser superior a zero.");
    }

    public static decimal Discount(CouponDiscountType type, decimal value, decimal eligible)
    {
        if (eligible <= 0)
            return 0;
        var raw = type == CouponDiscountType.Percent ? eligible * value / 100m : value;
        var discount = Round(raw);
        var cap = Round(eligible);
        if (discount > cap)
            discount = cap;
        return discount < 0 ? 0 : discount;
    }

    public static decimal Eligible(
        IReadOnlyList<CouponLine> lines,
        bool restricted,
        IReadOnlySet<Guid> flowerIds,
        IReadOnlySet<string> categories)
    {
        decimal sum = 0;
        foreach (var line in lines)
        {
            if (!restricted)
            {
                sum += line.LineGross;
                continue;
            }

            var flowerHit = line.FlowerId is Guid id && flowerIds.Contains(id);
            var categoryHit = !string.IsNullOrWhiteSpace(line.Category)
                && categories.Contains(line.Category.Trim().ToLowerInvariant());
            if (flowerHit || categoryHit)
                sum += line.LineGross;
        }

        return Round(sum);
    }

    public static decimal Payable(decimal merchandise, decimal deliveryFee, decimal discount) =>
        Round(Math.Max(0, merchandise + deliveryFee - discount));

    public static long PayableCents(decimal payable) => CommissionCalculator.ToCents(payable);

    public static string FormatEuro(decimal value) =>
        value.ToString("0.00", CultureInfo.GetCultureInfo("pt-PT")) + " €";
}

public static class SeasonMonths
{
    public static int ToMask(IEnumerable<int>? months)
    {
        if (months is null)
            return 0;
        var mask = 0;
        foreach (var month in months.Distinct())
        {
            if (month is < 1 or > 12)
                throw new InvalidOperationException("O mês da época tem de estar entre 1 e 12.");
            mask |= 1 << (month - 1);
        }

        return mask;
    }

    public static int[] FromMask(int mask)
    {
        var months = new List<int>();
        for (var month = 1; month <= 12; month++)
        {
            if ((mask & (1 << (month - 1))) != 0)
                months.Add(month);
        }

        return months.ToArray();
    }

    public static bool Includes(int mask, int month) =>
        month is >= 1 and <= 12 && (mask & (1 << (month - 1))) != 0;
}

public static class LisbonClock
{
    public static DateOnly Today(DateTimeOffset? utcNow = null)
    {
        var utc = (utcNow ?? DateTimeOffset.UtcNow).UtcDateTime;
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");
        }
        catch (TimeZoneNotFoundException)
        {
            zone = TimeZoneInfo.Utc;
        }

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, zone));
    }
}

public static class PublicSlug
{
    public static string From(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "pagina";
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(character))
                builder.Append(char.ToLowerInvariant(character));
            else if (builder.Length > 0 && builder[^1] != '-')
                builder.Append('-');
        }

        var slug = builder.ToString().Trim('-');
        if (slug.Length > 80)
            slug = slug[..80].Trim('-');
        return slug.Length == 0 ? "pagina" : slug;
    }
}
