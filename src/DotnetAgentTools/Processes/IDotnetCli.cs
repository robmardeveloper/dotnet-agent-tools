namespace DotnetAgentTools.Processes;

/// <summary>Runs the dotnet command line. Behind an interface so the tools can be tested without real builds.</summary>
internal interface IDotnetCli
{
    /// <param name="arguments">The dotnet arguments, for example build and the project.</param>
    /// <param name="workingDirectory">The folder the command runs in.</param>
    /// <param name="englishMessages">
    /// Ask dotnet for English messages. Only for commands that run no user code, such as build: the language setting
    /// also reaches the processes dotnet starts, and would change how the user's tests behave.
    /// </param>
    /// <param name="cancellationToken">Stops the command.</param>
    Task<CommandResult> RunAsync(
        IReadOnlyList<string> arguments, string workingDirectory, bool englishMessages, CancellationToken cancellationToken);
}
