using FloristHub.Services.Orders;

namespace FloristHub.Services.Tests.Orders;

public class OrderTrackingTokenTests
{
    [Fact]
    public void Create_produces_url_safe_token_of_expected_length()
    {
        var token = OrderTrackingToken.Create();
        Assert.True(OrderTrackingToken.LooksValid(token));
        Assert.DoesNotContain('+', token);
        Assert.DoesNotContain('/', token);
        Assert.DoesNotContain('=', token);
    }

    [Fact]
    public void Create_tokens_are_unique()
    {
        var a = OrderTrackingToken.Create();
        var b = OrderTrackingToken.Create();
        Assert.NotEqual(a, b);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short")]
    public void LooksValid_rejects_bad_tokens(string? value)
    {
        Assert.False(OrderTrackingToken.LooksValid(value));
    }
}
