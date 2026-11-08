using DotnetAgentTools.Build;
using DotnetAgentTools.Processes;
using DotnetAgentTools.Workspace;

namespace DotnetAgentTools.Testing;

/// <summary>
/// Builds first, and only if the build is clean and the workspace uses Microsoft.Testing.Platform runs dotnet test with a TRX report in a temporary folder, reads it and
/// deletes it. The TRX file names hold the user and machine names, so they never leave that folder.
/// </summary>
internal sealed class TestRunner(BuildRunner buildRunner, IDotnetCli dotnet, WorkspacePaths paths, TimeProvider timeProvider)
{
    private const int BuildItemsOnFailure = 20;

    public async Task<TestReport> RunAsync(TestRequest request, CancellationToken cancellationToken)
    {
        var started = timeProvider.GetTimestamp();
        var target = paths.ResolveTarget(request.Path);

        // Build errors are useful whatever runs the tests, so the build comes first.
        var buildRequest = new BuildRequest(request.Path, request.Configuration, IncludeWarnings: false, BuildItemsOnFailure);
        var build = await buildRunner.RunAsync(buildRequest, cancellationToken);
        if (!build.Ok)
        {
            return NotRun(started, "The build failed, so the tests did not run.", build);
        }

        if (!TestPlatform.UsesMtp(paths.Root))
        {
            return NotRun(
                started,
                "This workspace runs dotnet test with VSTest. The test tool supports Microsoft.Testing.Platform only: " +
                "add \"test\": { \"runner\": \"Microsoft.Testing.Platform\" } to global.json, or run dotnet test yourself.");
        }

        var tempFolder = Directory.CreateTempSubdirectory("dotnet-agent-tools-");
        try
        {
            var command = await dotnet.RunAsync(
                BuildArguments(target, request, tempFolder.FullName), paths.Root, englishMessages: false, cancellationToken);
            var results = tempFolder.GetFiles("*.trx", SearchOption.AllDirectories)
                .SelectMany(trx => TrxResults.Read(trx.FullName))
                .ToList();
            return TestReportBuilder.Create(results, command, request, paths, timeProvider.GetElapsedTime(started));
        }
        finally
        {
            tempFolder.Delete(recursive: true);
        }
    }

    private static List<string> BuildArguments(string target, TestRequest request, string resultsFolder)
    {
        List<string> arguments =
        [
            "test", TargetOption(target), target, "-c", request.Configuration, "--no-build",
            "--report-xunit-trx", "--results-directory", resultsFolder,
        ];
        if (!string.IsNullOrWhiteSpace(request.Filter))
        {
            arguments.AddRange(["--filter", request.Filter]);
        }

        return arguments;
    }

    /// <summary>On Microsoft.Testing.Platform, dotnet test needs to be told what kind of target it gets.</summary>
    private static string TargetOption(string target)
    {
        if (Directory.Exists(target))
        {
            return "--directory";
        }

        return Path.GetExtension(target).ToUpperInvariant() is ".SLN" or ".SLNX" or ".SLNF" ? "--solution" : "--project";
    }

    private TestReport NotRun(long started, string note, BuildReport? build = null) => new(
        Ok: false, Total: null, Passed: null, Failed: null, Skipped: null, Failures: null, Truncated: false,
        (long)timeProvider.GetElapsedTime(started).TotalMilliseconds, build, note);
}
