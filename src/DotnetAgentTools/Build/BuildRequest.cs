namespace DotnetAgentTools.Build;

/// <summary>What the agent asked for.</summary>
internal sealed record BuildRequest(string? Path, string Configuration, bool IncludeWarnings, int MaxItems);

/// <summary>How the dotnet build process ended.</summary>
internal sealed record BuildOutcome(int ExitCode, bool TimedOut, TimeSpan Duration, string ConsoleOutput);
