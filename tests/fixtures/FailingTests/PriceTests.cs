using Xunit;

namespace FailingTests;

public class PriceTests
{
    [Fact]
    public void Total_AddsBothPrices() => Assert.Equal(5, Price.Total(2, 3));

    [Fact]
    public void Total_WithDiscount_IsLower() => Assert.Equal(4, Price.Total(2, 3));

    [Fact]
    public void Parse_Empty_Throws() => Price.Parse(string.Empty);

    [Fact(Skip = "Not ready yet")]
    public void Round_ToCents() => Assert.Fail();
}

public static class Price
{
    public static int Total(int first, int second) => first + second;

    public static int Parse(string text) =>
        text.Length == 0 ? throw new FormatException("The price is empty.") : int.Parse(text);
}
