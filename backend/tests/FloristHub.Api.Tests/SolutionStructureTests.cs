using Xunit;

namespace FloristHub.Api.Tests;

public class SolutionStructureTests
{
    [Fact]
    public void Api_layer_is_discoverable()
    {
        Assert.NotNull(typeof(FloristHub.Api.Controllers.OrdersController).Assembly);
    }
}
