using System.Diagnostics;
using DotnetAgentTools.Workspace;
using Microsoft.Extensions.Options;

namespace DotnetAgentTools.Processes;

/// <summary>Starts the real dotnet process, with a time limit, and never lets it touch the MCP channel on stdin/stdout.</summary>
internal sealed class DotnetCli(IOptions<WorkspaceOptions> options) : IDotnetCli
{
    private const string EnglishLanguageId = "1033";

    public async Task<CommandResult> RunAsync(
        IReadOnlyList<string> arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // English messages whatever the machine's language, so agents and tests see the same text.
        startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "en";
        startInfo.Environment["VSLANG"] = EnglishLanguageId;
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["MSBUILDTERMINALLOGGER"] = "off";

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The dotnet process could not be started.");
        process.StandardInput.Close();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(options.Value.CommandTimeout);

        var output = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var error = process.StandardError.ReadToEndAsync(CancellationToken.None);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            cancellationToken.ThrowIfCancellationRequested();
            return new CommandResult(process.ExitCode, await output, await error, TimedOut: true);
        }

        return new CommandResult(process.ExitCode, await output, await error, TimedOut: false);
    }
}
