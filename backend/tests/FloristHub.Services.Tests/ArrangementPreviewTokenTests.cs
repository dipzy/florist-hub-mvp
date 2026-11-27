using FloristHub.DAL.Enums;
using FloristHub.Services.Visualization;

namespace FloristHub.Services.Tests;

public class ArrangementPreviewTokenTests
{
    [Fact]
    public void Format_includes_seed_shape_and_base()
    {
        var orderId = Guid.Parse("55555555-5555-5555-5555-555555555501");
        var token = ArrangementPreviewToken.Format(orderId, 48291, ArrangementShape.Sponge, ArrangementBaseType.Sponge);

        Assert.Equal("v1;order=55555555555555555555555555555501;seed=48291;shape=0;base=1", token);
    }

    [Fact]
    public void Format_is_stable_for_the_same_variation()
    {
        var orderId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var first = ArrangementPreviewToken.Format(orderId, -7, ArrangementShape.Short, ArrangementBaseType.Wrapped);
        var second = ArrangementPreviewToken.Format(orderId, -7, ArrangementShape.Short, ArrangementBaseType.Wrapped);
        Assert.Equal(first, second);
        Assert.Contains("shape=2", first, StringComparison.Ordinal);
        Assert.Contains("base=2", first, StringComparison.Ordinal);
    }
}
