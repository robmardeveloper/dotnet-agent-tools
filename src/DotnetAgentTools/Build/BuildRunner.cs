using DotnetAgentTools.Processes;
using DotnetAgentTools.Workspace;

namespace DotnetAgentTools.Build;

/// <summary>Runs dotnet build with a binary log in a temporary folder, reads the log and deletes it.</summary>
internal sealed class BuildRunner(IDotnetCli dotnet, WorkspacePaths paths, TimeProvider timeProvider)
{
    private const string BinlogFileName = "build.binlog";

    public async Task<BuildReport> RunAsync(BuildRequest request, CancellationToken cancellationToken)
    {
        var target = paths.ResolveTarget(request.Path);
        var tempFolder = Directory.CreateTempSubdirectory("dotnet-agent-tools-");
        try
        {
            var binlogPath = Path.Combine(tempFolder.FullName, BinlogFileName);
            string[] arguments = ["build", target, "-c", request.Configuration, "-nologo", "-v:q", $"-bl:{binlogPath}"];

            var started = timeProvider.GetTimestamp();
            var result = await dotnet.RunAsync(arguments, paths.Root, englishMessages: true, cancellationToken);
            var outcome = new BuildOutcome(
                result.ExitCode,
                result.TimedOut,
                timeProvider.GetElapsedTime(started),
                result.StandardOutput + result.StandardError);

            var diagnostics = File.Exists(binlogPath) ? BinlogDiagnostics.Read(binlogPath) : [];
            return BuildReportBuilder.Create(outcome, diagnostics, request, paths);
        }
        finally
        {
            tempFolder.Delete(recursive: true);
        }
    }
}
