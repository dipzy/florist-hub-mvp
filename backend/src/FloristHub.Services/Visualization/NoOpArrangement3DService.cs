using FloristHub.DAL.Enums;
using FloristHub.Services.Interfaces.Visualization;

namespace FloristHub.Services.Visualization;

public class NoOpArrangement3DService : IArrangement3DService
{
    public Task<string?> GeneratePreviewTokenAsync(
        Guid orderId,
        int variationSeed,
        ArrangementShape shape,
        ArrangementBaseType baseType,
        CancellationToken ct = default) =>
        Task.FromResult<string?>(ArrangementPreviewToken.Format(orderId, variationSeed, shape, baseType));
}
