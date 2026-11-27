namespace FloristHub.Services.DTOs.Platform;

public record PlatformDailyCountDto(string Date, int Count);
public record PlatformDailyAmountDto(string Date, decimal Amount);

public record PlatformTopStoreDto(
    Guid TenantId,
    string Name,
    string Slug,
    int OrderCount,
    decimal Gmv);

public record PlatformMetricsDto(
    int Days,
    int ActiveStores,
    int OrdersInPeriod,
    decimal GmvInPeriod,
    decimal CommissionEarned,
    int StoresWithStripeConnected,
    int NewStoresThisMonth,
    IReadOnlyList<PlatformDailyCountDto> OrdersByDay,
    IReadOnlyList<PlatformDailyAmountDto> GmvByDay,
    IReadOnlyList<PlatformTopStoreDto> TopStores,
    IReadOnlyList<PlatformDailyCountDto> NewStoresByMonth);
