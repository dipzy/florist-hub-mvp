using FloristHub.DAL.Entities;
using FloristHub.DAL.Enums;

namespace FloristHub.Services.Payments;

public static class PaymentRefundRules
{
    public static decimal Remaining(Payment payment) => payment.Amount - payment.RefundedAmount;

    public static bool IsRefundable(PaymentStatus status) =>
        status is PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded;

    public static decimal ResolveRefundAmount(Payment payment, decimal? requested)
    {
        if (!IsRefundable(payment.Status))
            throw new InvalidOperationException("Este pagamento não pode ser reembolsado.");

        var remaining = Remaining(payment);
        if (remaining <= 0)
            throw new InvalidOperationException("Este pagamento já foi reembolsado na totalidade.");

        var amount = requested ?? remaining;
        if (amount <= 0 || amount != decimal.Round(amount, 2) || amount > remaining)
            throw new InvalidOperationException("O valor do reembolso tem de ser positivo e não pode exceder o valor em aberto.");

        return amount;
    }

    public static bool CustomerCancellationRequiresFullRefund(Payment? payment) =>
        payment is not null && IsRefundable(payment.Status) && Remaining(payment) > 0;
}
