using DotnetAgentTools.Build;
using Xunit;

namespace DotnetAgentTools.Tests.Build;

/// <summary>Runs the real dotnet build on fixture projects and checks what the agent would receive.</summary>
public sealed class BuildRunnerTests
{
    private static readonly BuildRequest DefaultRequest = new(Path: null, "Debug", IncludeWarnings: false, MaxItems: 20);

    [Fact]
    public async Task RunAsync_CompileError_ReportsTheErrorWithFileLineAndCode()
    {
        using var workspace = new FixtureWorkspace("CompileError");

        var report = await CreateRunner(workspace).RunAsync(DefaultRequest, TestContext.Current.CancellationToken);

        Assert.False(report.Ok);
        Assert.Equal(1, report.Errors);
        var error = Assert.Single(report.Items!);
        Assert.Equal(new ReportedDiagnostic("error", "CS0103", "Program.cs", 2, 19, error.Message, "CompileError"), error);
        Assert.Contains("totl", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_CompileErrorWithWarning_CountsTheWarningByCodeWithoutListingIt()
    {
        using var workspace = new FixtureWorkspace("CompileError");

        var report = await CreateRunner(workspace).RunAsync(DefaultRequest, TestContext.Current.CancellationToken);

        Assert.Equal(1, report.Warnings);
        Assert.Equal(1, report.WarningsByCode!["CS8602"]);
        Assert.DoesNotContain(report.Items!, item => item.Severity == "warning");
    }

    [Fact]
    public async Task RunAsync_IncludeWarnings_ListsWarningsAfterErrors()
    {
        using var workspace = new FixtureWorkspace("CompileError");
        var request = DefaultRequest with { IncludeWarnings = true };

        var report = await CreateRunner(workspace).RunAsync(request, TestContext.Current.CancellationToken);

        Assert.Collection(
            report.Items!,
            item => Assert.Equal("CS0103", item.Code),
            item => Assert.Equal(("CS8602", 4), (item.Code, item.Line)));
    }

    [Fact]
    public async Task RunAsync_MaxItemsBelowTheDiagnostics_MarksTheReportAsTruncated()
    {
        using var workspace = new FixtureWorkspace("CompileError");
        var request = DefaultRequest with { IncludeWarnings = true, MaxItems = 1 };

        var report = await CreateRunner(workspace).RunAsync(request, TestContext.Current.CancellationToken);

        Assert.Single(report.Items!);
        Assert.True(report.Truncated);
    }

    [Fact]
    public async Task RunAsync_MissingPackage_ReportsTheNuGetError()
    {
        using var workspace = new FixtureWorkspace("MissingPackage");

        var report = await CreateRunner(workspace).RunAsync(DefaultRequest, TestContext.Current.CancellationToken);

        Assert.False(report.Ok);
        var error = Assert.Single(report.Items!);
        Assert.Equal(("NU1101", "MissingPackage.csproj"), (error.Code, error.File));
    }

    [Fact]
    public async Task RunAsync_CleanProject_ReturnsOkWithNothingListed()
    {
        using var workspace = new FixtureWorkspace("Clean");

        var report = await CreateRunner(workspace).RunAsync(DefaultRequest, TestContext.Current.CancellationToken);

        Assert.True(report.Ok);
        Assert.Equal((0, 0), (report.Errors, report.Warnings));
        Assert.Null(report.Items);
        Assert.Null(report.WarningsByCode);
        Assert.Null(report.Note);
    }

    private static BuildRunner CreateRunner(FixtureWorkspace workspace) =>
        new(workspace.CreateDotnetCli(), workspace.Paths, TimeProvider.System);
}
