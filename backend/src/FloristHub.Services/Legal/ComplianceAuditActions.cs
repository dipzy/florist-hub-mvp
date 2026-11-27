namespace FloristHub.Services.Legal;

public static class ComplianceAuditActions
{
    public const string TermsAcceptedRegistration = "Legal.TermsAccepted.Registration";
    public const string PrivacyAcceptedRegistration = "Legal.PrivacyAccepted.Registration";
    public const string MarketingConsentRegistration = "Legal.MarketingConsent.Registration";
    public const string OrderLegalConsent = "Legal.OrderConsent";
    public const string PaymentLegalConsent = "Legal.PaymentConsent";
    public const string DataExport = "Legal.DataExport";
    public const string AccountAnonymized = "Legal.AccountAnonymized";

    public static readonly string[] All =
    [
        TermsAcceptedRegistration,
        PrivacyAcceptedRegistration,
        MarketingConsentRegistration,
        OrderLegalConsent,
        PaymentLegalConsent,
        DataExport,
        AccountAnonymized,
    ];
}
