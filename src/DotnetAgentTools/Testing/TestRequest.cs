namespace DotnetAgentTools.Testing;

/// <summary>What the agent asked for.</summary>
internal sealed record TestRequest(string? Path, string Configuration, string? Filter, int MaxFailures);
