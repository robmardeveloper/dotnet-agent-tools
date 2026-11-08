using DotnetAgentTools.Processes;
using DotnetAgentTools.Workspace;

namespace DotnetAgentTools.Build;

/// <summary>Turns the raw diagnostics of a build into the short report the agent receives.</summary>
internal static class BuildReportBuilder
{
    private const int MaxMessageLength = 300;

    public static BuildReport Create(
        BuildOutcome outcome, IReadOnlyList<Diagnostic> diagnostics, BuildRequest request, WorkspacePaths paths)
    {
        // The same diagnostic is reported once per target framework and per build pass: keep one.
        var distinct = diagnostics
            .Select(d => d with { Message = Shorten(d.Message) })
            .Distinct()
            .ToList();
        var errors = distinct.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        var warnings = distinct.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();

        var shown = request.IncludeWarnings ? [.. errors, .. warnings] : errors;
        var items = shown.Take(request.MaxItems).Select(d => ToReported(d, paths)).ToList();
        var warningsByCode = warnings
            .GroupBy(w => w.Code ?? "none")
            .OrderByDescending(g => g.Count())
            .ToDictionary(g => g.Key, g => g.Count());

        var ok = outcome.ExitCode == 0 && !outcome.TimedOut && errors.Count == 0;
        return new BuildReport(
            ok,
            errors.Count,
            warnings.Count,
            items.Count == 0 ? null : items,
            warningsByCode.Count == 0 ? null : warningsByCode,
            shown.Count > items.Count,
            (long)outcome.Duration.TotalMilliseconds,
            ExplainWithoutDiagnostics(outcome, ok, errors.Count));
    }

    private static ReportedDiagnostic ToReported(Diagnostic diagnostic, WorkspacePaths paths) => new(
        diagnostic.Severity == DiagnosticSeverity.Error ? "error" : "warning",
        diagnostic.Code,
        diagnostic.File is null ? null : paths.ToRelative(diagnostic.File),
        diagnostic.Line > 0 ? diagnostic.Line : null,
        diagnostic.Column > 0 ? diagnostic.Column : null,
        diagnostic.Message,
        diagnostic.ProjectFile is null ? null : Path.GetFileNameWithoutExtension(diagnostic.ProjectFile));

    /// <summary>A failed build with no error to show (a time-out, a bad argument): give the agent the end of the output.</summary>
    private static string? ExplainWithoutDiagnostics(BuildOutcome outcome, bool ok, int errorCount)
    {
        if (outcome.TimedOut)
        {
            return "The build was stopped because it took longer than the time limit.";
        }

        if (ok || errorCount > 0)
        {
            return null;
        }

        return $"dotnet build failed (exit code {outcome.ExitCode}) without reporting an error. Last lines: "
            + ConsoleTail.LastLines(outcome.ConsoleOutput);
    }

    private static string Shorten(string message)
    {
        var firstLine = message.Split('\n', 2)[0].Trim();
        return firstLine.Length <= MaxMessageLength
            ? firstLine
            : string.Concat(firstLine.AsSpan(0, MaxMessageLength), "…");
    }
}
