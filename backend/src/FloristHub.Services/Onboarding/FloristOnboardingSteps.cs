namespace FloristHub.Services.Onboarding;

public static class FloristOnboardingSteps
{
    public const string Loja = "loja";
    public const string Cores = "cores";
    public const string Dominio = "dominio";
    public const string ContactosHorario = "contactos-horario";
    public const string Entrega = "entrega";
    public const string Capacidade = "capacidade";
    public const string Catalogo = "catalogo";
    public const string Fiscal = "fiscal";
    public const string Pagamentos = "pagamentos";
    public const string Termos = "termos";

    public static readonly string[] Ordered =
    [
        Loja,
        Cores,
        Dominio,
        ContactosHorario,
        Entrega,
        Capacidade,
        Catalogo,
        Fiscal,
        Pagamentos,
        Termos,
    ];

    public static string DefaultFirst => Loja;
}
