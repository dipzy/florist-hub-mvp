using FloristHub.DAL.Enums;

namespace FloristHub.Services.Visualization;

public static class ArrangementPreviewToken
{
    public static string Format(Guid orderId, int seed, ArrangementShape shape, ArrangementBaseType baseType) =>
        $"v1;order={orderId:N};seed={seed};shape={(int)shape};base={(int)baseType}";
}
