namespace DotnetAgentTools.Processes;

/// <summary>What a finished dotnet command left behind.</summary>
internal sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut);
