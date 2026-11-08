using System.Text.Json.Serialization;
using DotnetAgentTools.Build;

namespace DotnetAgentTools.Testing;

/// <summary>
/// What the test tool returns to the agent. When the build fails, only <see cref="Build"/> is filled: the tests did
/// not run, so there is nothing to count.
/// </summary>
internal sealed record TestReport(
    bool Ok,
    int? Total,
    int? Passed,
    int? Failed,
    int? Skipped,
    IReadOnlyList<TestFailure>? Failures,
    bool Truncated,
    long DurationMs,
    BuildReport? Build = null,
    string? Note = null);

/// <summary>One failed test: its name, the start of its message and where it failed in the workspace's own code.</summary>
internal sealed record TestFailure(
    string Test,
    [property: JsonPropertyName("msg")] string Message,
    string? File,
    int? Line);
