namespace FloristHub.Services.DTOs.Florist;

public record FloristDashboardDto(
    IReadOnlyDictionary<string, int> OrdersByStatus,
    int OrdersToday,
    int OrdersTomorrow,
    decimal MonthlyRevenue,
    IReadOnlyList<TopCatalogItemDto> TopItems);

public record TopCatalogItemDto(string Name, int OrderLineCount, decimal TotalValue);

public record FloristDashboardMetricsDto(
    int Days,
    int PendingOrders,
    decimal PeriodRevenue,
    IReadOnlyList<DailyCountDto> OrdersByDay,
    IReadOnlyList<DailyRevenueDto> RevenueByDay,
    IReadOnlyList<TopCatalogItemDto> TopFlowers);

public record DailyCountDto(string Date, int Count);

public record DailyRevenueDto(string Date, decimal Revenue);

public record FloristCustomerDto(
    Guid CustomerId,
    string? FullName,
    string? Email,
    int OrderCount,
    decimal TotalSpent);
