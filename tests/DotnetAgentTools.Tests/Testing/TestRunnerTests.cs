using DotnetAgentTools.Build;
using DotnetAgentTools.Testing;
using Xunit;

namespace DotnetAgentTools.Tests.Testing;

/// <summary>Runs the real dotnet build and dotnet test on fixture projects and checks what the agent would receive.</summary>
public sealed class TestRunnerTests
{
    private static readonly TestRequest DefaultRequest = new(Path: null, "Debug", Filter: null, MaxFailures: 10);

    [Fact]
    public async Task RunAsync_FailingTests_CountsEveryOutcome()
    {
        using var workspace = new FixtureWorkspace("FailingTests");

        var report = await CreateRunner(workspace).RunAsync(DefaultRequest, TestContext.Current.CancellationToken);

        Assert.False(report.Ok);
        Assert.Equal((4, 1, 2, 1), (report.Total, report.Passed, report.Failed, report.Skipped));
        Assert.Null(report.Build);
        Assert.Null(report.Note);
    }

    [Fact]
    public async Task RunAsync_FailedAssertion_ReportsTheMessageFileAndLine()
    {
        using var workspace = new FixtureWorkspace("FailingTests");

        var report = await CreateRunner(workspace).RunAsync(DefaultRequest, TestContext.Current.CancellationToken);

        var failure = Assert.Single(report.Failures!, f => f.Test.EndsWith("Total_WithDiscount_IsLower", StringComparison.Ordinal));
        Assert.Equal(("PriceTests.cs", 11), (failure.File, failure.Line));
        Assert.Contains("Expected: 4", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_ExceptionInCodeUnderTest_PointsAtTheLineThatThrew()
    {
        using var workspace = new FixtureWorkspace("FailingTests");

        var report = await CreateRunner(workspace).RunAsync(DefaultRequest, TestContext.Current.CancellationToken);

        var failure = Assert.Single(report.Failures!, f => f.Test.EndsWith("Parse_Empty_Throws", StringComparison.Ordinal));
        Assert.Equal(("PriceTests.cs", 25), (failure.File, failure.Line));
        Assert.Contains("The price is empty.", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_Filter_RunsOnlyTheMatchingTests()
    {
        using var workspace = new FixtureWorkspace("FailingTests");
        var request = DefaultRequest with { Filter = "FullyQualifiedName~Total" };

        var report = await CreateRunner(workspace).RunAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal((2, 1, 1), (report.Total, report.Passed, report.Failed));
    }

    [Fact]
    public async Task RunAsync_FilterThatMatchesNothing_SaysNoTestRan()
    {
        using var workspace = new FixtureWorkspace("FailingTests");
        var request = DefaultRequest with { Filter = "FullyQualifiedName~DoesNotExist" };

        var report = await CreateRunner(workspace).RunAsync(request, TestContext.Current.CancellationToken);

        Assert.False(report.Ok);
        Assert.Equal(0, report.Total);
        Assert.StartsWith("No test ran.", report.Note, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_MaxFailuresBelowTheFailures_MarksTheReportAsTruncated()
    {
        using var workspace = new FixtureWorkspace("FailingTests");
        var request = DefaultRequest with { MaxFailures = 1 };

        var report = await CreateRunner(workspace).RunAsync(request, TestContext.Current.CancellationToken);

        Assert.Single(report.Failures!);
        Assert.Equal(2, report.Failed);
        Assert.True(report.Truncated);
    }

    [Fact]
    public async Task RunAsync_BuildFails_ReturnsTheBuildErrorsWithoutRunningTests()
    {
        using var workspace = new FixtureWorkspace("CompileError");

        var report = await CreateRunner(workspace).RunAsync(DefaultRequest, TestContext.Current.CancellationToken);

        Assert.False(report.Ok);
        Assert.Null(report.Total);
        Assert.Equal("CS0103", Assert.Single(report.Build!.Items!).Code);
    }

    [Fact]
    public async Task RunAsync_WorkspaceOnVSTest_SaysSoInsteadOfAnEmptyResult()
    {
        using var workspace = new FixtureWorkspace("Clean");

        var report = await CreateRunner(workspace).RunAsync(DefaultRequest, TestContext.Current.CancellationToken);

        Assert.False(report.Ok);
        Assert.Null(report.Total);
        Assert.Contains("VSTest", report.Note, StringComparison.Ordinal);
    }

    private static TestRunner CreateRunner(FixtureWorkspace workspace)
    {
        var dotnet = workspace.CreateDotnetCli();
        var buildRunner = new BuildRunner(dotnet, workspace.Paths, TimeProvider.System);
        return new TestRunner(buildRunner, dotnet, workspace.Paths, TimeProvider.System);
    }
}
