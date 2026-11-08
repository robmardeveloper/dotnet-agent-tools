using System.Text.Json.Serialization;

namespace DotnetAgentTools.Build;

/// <summary>What the build tool returns to the agent: as little as possible, but enough to fix the build.</summary>
internal sealed record BuildReport(
    bool Ok,
    int Errors,
    int Warnings,
    IReadOnlyList<ReportedDiagnostic>? Items,
    IReadOnlyDictionary<string, int>? WarningsByCode,
    bool Truncated,
    long DurationMs,
    string? Note = null);

/// <summary>One cleaned-up diagnostic: path relative to the workspace, short field names to save tokens.</summary>
internal sealed record ReportedDiagnostic(
    [property: JsonPropertyName("sev")] string Severity,
    string? Code,
    string? File,
    int? Line,
    [property: JsonPropertyName("col")] int? Column,
    [property: JsonPropertyName("msg")] string Message,
    string? Project);
