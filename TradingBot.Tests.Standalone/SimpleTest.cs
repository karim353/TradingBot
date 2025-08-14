using Xunit;

namespace TradingBot.Tests.Standalone;

public class SimpleTest
{
    [Fact]
    public void SimpleTest_ShouldPass()
    {
        // Arrange
        var expected = 2;
        var actual = 1 + 1;

        // Act & Assert
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(1, 1, 2)]
    [InlineData(2, 3, 5)]
    [InlineData(0, 0, 0)]
    public void SimpleTest_Addition_ShouldWork(int a, int b, int expected)
    {
        // Act
        var actual = a + b;

        // Assert
        Assert.Equal(expected, actual);
    }
}
