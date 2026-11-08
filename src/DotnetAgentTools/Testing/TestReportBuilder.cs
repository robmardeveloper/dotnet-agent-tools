using DotnetAgentTools.Processes;
using DotnetAgentTools.Workspace;

namespace DotnetAgentTools.Testing;

/// <summary>Turns the test results and the dotnet test exit into the short report the agent receives.</summary>
internal static class TestReportBuilder
{
    private const int MaxMessageLines = 5;
    private const int MaxMessageLength = 500;

    // Microsoft.Testing.Platform exit codes: https://aka.ms/testingplatform/exitcodes
    private const int InvalidCommandLine = 5;
    private const int ZeroTests = 8;

    public static TestReport Create(
        IReadOnlyList<TestResult> results, CommandResult command, TestRequest request, WorkspacePaths paths,
        TimeSpan duration)
    {
        var failed = results.Where(r => r.Outcome == TestOutcome.Failed).ToList();
        var failures = failed.Take(request.MaxFailures).Select(r => ToFailure(r, paths)).ToList();

        var ok = command.ExitCode == 0 && !command.TimedOut && failed.Count == 0 && results.Count > 0;
        return new TestReport(
            ok,
            results.Count,
            results.Count(r => r.Outcome == TestOutcome.Passed),
            failed.Count,
            results.Count(r => r.Outcome == TestOutcome.Skipped),
            failures.Count == 0 ? null : failures,
            failed.Count > failures.Count,
            (long)duration.TotalMilliseconds,
            Note: Explain(command, ok, failed.Count, results.Count));
    }

    private static TestFailure ToFailure(TestResult result, WorkspacePaths paths)
    {
        var location = StackTraceLocation.FindFirstInWorkspace(result.StackTrace, paths);
        return new TestFailure(result.Name, Shorten(result.Message), location?.File, location?.Line);
    }

    /// <summary>When the run went wrong without a failed test to show, say why.</summary>
    private static string? Explain(CommandResult command, bool ok, int failedCount, int resultCount)
    {
        if (command.TimedOut)
        {
            return "The tests were stopped because they took longer than the time limit.";
        }

        if (ok || failedCount > 0)
        {
            return null;
        }

        if (resultCount == 0 && command.ExitCode == ZeroTests)
        {
            return "No test ran. Check the filter, or that the solution has test projects.";
        }

        var output = ConsoleTail.LastLines(command.StandardOutput + command.StandardError);
        return command.ExitCode == InvalidCommandLine
            ? $"dotnet test rejected the command line. The test tool reads results from xUnit v3 test projects only. Last lines: {output}"
            : $"dotnet test failed (exit code {command.ExitCode}) without a failed test to show. Last lines: {output}";
    }

    private static string Shorten(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "(no message)";
        }

        var lines = message.Split('\n', StringSplitOptions.TrimEntries).Take(MaxMessageLines);
        var text = string.Join('\n', lines);
        return text.Length <= MaxMessageLength ? text : string.Concat(text.AsSpan(0, MaxMessageLength), "…");
    }
}
