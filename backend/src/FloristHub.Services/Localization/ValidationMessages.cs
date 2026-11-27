namespace FloristHub.Services.Localization;

/// <summary>Mensagens FluentValidation e afins, bilingues.</summary>
public static class ValidationMessages
{
    public static string OrderNeedsItems =>
        RequestCultureScope.Pick(
            "A encomenda precisa de pelo menos um item.",
            "The order must include at least one item.");

    public static string DeliveryAddressRequired =>
        RequestCultureScope.Pick("Indique a morada de entrega.", "Enter a delivery address.");

    public static string InvalidNif =>
        RequestCultureScope.Pick("NIF inválido.", "Invalid tax ID (NIF).");

    public static string InvalidArrangementPreviewUrl =>
        RequestCultureScope.Pick(
            "O endereço da pré-visualização do arranjo não é válido.",
            "The arrangement preview URL is not valid.");

    public static string DeliveryTimeMustBeFuture =>
        RequestCultureScope.Pick(
            "A data/hora pretendida deve ser no futuro (ou muito recente).",
            "The requested delivery time must be in the future (or very recent).");

    public static string InvalidProposedItemsJson =>
        RequestCultureScope.Pick("JSON de itens propostos inválido.", "Invalid proposed items JSON.");

    public static string RejectReasonRequired =>
        RequestCultureScope.Pick("Indique o motivo da recusa.", "Provide a reason for rejection.");

    public static string RejectReasonTooLong =>
        RequestCultureScope.Pick("O motivo da recusa é demasiado longo.", "The rejection reason is too long.");

    public static string InvalidVatRate =>
        RequestCultureScope.Pick("Taxa de IVA inválida.", "Invalid VAT rate.");

    public static string SpongeShapeCannotUseJar =>
        RequestCultureScope.Pick(
            "O formato esponja não usa jarra; escolha base de esponja ou embrulho.",
            "The sponge shape cannot use a jar base.");
}
