using ModelContextProtocol;
using Xunit;

namespace DotnetAgentTools.Tests;

public sealed class ToolArgumentsTests
{
    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    [InlineData("Release-Linux_x64.2")]
    public void Configuration_PlainName_IsAccepted(string configuration) =>
        Assert.Equal(configuration, ToolArguments.Configuration(configuration));

    [Theory]
    [InlineData("Debug;OutputPath=C:\\elsewhere")]
    [InlineData("Debug /p:Extra=1")]
    [InlineData("")]
    public void Configuration_WithSpecialCharacters_IsRejected(string configuration) =>
        Assert.Throws<McpException>(() => ToolArguments.Configuration(configuration));

    [Fact]
    public void Filter_ThatLooksLikeAnOption_IsRejected() =>
        Assert.Throws<McpException>(() => ToolArguments.Filter("--results-directory C:\\elsewhere"));

    [Fact]
    public void Filter_Blank_MeansNoFilter() => Assert.Null(ToolArguments.Filter("  "));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(20, 20)]
    [InlineData(100_000, ToolArguments.MaxListed)]
    public void Limit_StaysBetweenOneAndTheMaximum(int value, int expected) =>
        Assert.Equal(expected, ToolArguments.Limit(value));
}
