namespace DotnetAgentTools.Processes;

/// <summary>Runs the dotnet command line. Behind an interface so the tools can be tested without real builds.</summary>
internal interface IDotnetCli
{
    Task<CommandResult> RunAsync(IReadOnlyList<string> arguments, string workingDirectory, CancellationToken cancellationToken);
}
