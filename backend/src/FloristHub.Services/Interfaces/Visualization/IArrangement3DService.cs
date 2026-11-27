using FloristHub.DAL.Enums;

namespace FloristHub.Services.Interfaces.Visualization;

/// <summary>
/// Gera o token que identifica a variação 3D guardada na encomenda.
/// A geometria é reconstruída no cliente a partir da semente e das opções.
/// </summary>
public interface IArrangement3DService
{
    Task<string?> GeneratePreviewTokenAsync(
        Guid orderId,
        int variationSeed,
        ArrangementShape shape,
        ArrangementBaseType baseType,
        CancellationToken ct = default);
}
