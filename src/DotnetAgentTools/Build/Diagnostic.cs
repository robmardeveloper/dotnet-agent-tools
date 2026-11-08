namespace DotnetAgentTools.Build;

internal enum DiagnosticSeverity
{
    Error,
    Warning,
}

/// <summary>One error or warning as MSBuild reported it, before it is cleaned up for the agent.</summary>
internal sealed record Diagnostic(
    DiagnosticSeverity Severity,
    string? Code,
    string? File,
    int Line,
    int Column,
    string Message,
    string? ProjectFile);
