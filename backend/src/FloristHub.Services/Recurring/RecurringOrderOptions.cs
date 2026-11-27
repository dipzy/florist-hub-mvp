namespace FloristHub.Services.Recurring;

public sealed class RecurringOrderOptions
{
    public const string SectionName = "RecurringOrders";

    /// <summary>Dias de antecedência para gerar a encomenda antes da entrega.</summary>
    public int GenerationAdvanceDays { get; set; } = 3;

    /// <summary>Intervalo do worker em segundos.</summary>
    public int PollIntervalSeconds { get; set; } = 60;

    /// <summary>Duração do lease de geração por instância.</summary>
    public int GenerationLeaseSeconds { get; set; } = 120;
}
