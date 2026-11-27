namespace FloristHub.Services.Payments;

public sealed class StripeSignatureException : Exception
{
    public StripeSignatureException(string message) : base(message) { }
}
