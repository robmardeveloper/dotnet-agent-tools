using Microsoft.Build.Framework;
using Microsoft.Build.Logging.StructuredLogger;

namespace DotnetAgentTools.Build;

/// <summary>
/// Reads every error and warning from an MSBuild binary log. The binary log, unlike SARIF, also holds the MSBuild,
/// NuGet and SDK diagnostics (MSB, NU, NETSDK), not only the compiler's.
/// </summary>
internal static class BinlogDiagnostics
{
    public static IReadOnlyList<Diagnostic> Read(string binlogPath)
    {
        var diagnostics = new List<Diagnostic>();
        var reader = new BinLogReader();
        reader.ErrorRaised += (_, e) => diagnostics.Add(FromError(e));
        reader.WarningRaised += (_, e) => diagnostics.Add(FromWarning(e));
        reader.Replay(binlogPath);
        return diagnostics;
    }

    private static Diagnostic FromError(BuildErrorEventArgs e) => new(
        DiagnosticSeverity.Error, e.Code, ResolveFile(e.File, e.ProjectFile), e.LineNumber, e.ColumnNumber,
        e.Message ?? string.Empty, e.ProjectFile);

    private static Diagnostic FromWarning(BuildWarningEventArgs e) => new(
        DiagnosticSeverity.Warning, e.Code, ResolveFile(e.File, e.ProjectFile), e.LineNumber, e.ColumnNumber,
        e.Message ?? string.Empty, e.ProjectFile);

    /// <summary>MSBuild sometimes gives no file, or one relative to the project folder; make it a full path.</summary>
    private static string? ResolveFile(string? file, string? projectFile)
    {
        if (string.IsNullOrEmpty(file))
        {
            return projectFile;
        }

        if (Path.IsPathRooted(file))
        {
            return file;
        }

        var projectFolder = Path.GetDirectoryName(projectFile);
        return projectFolder is null ? file : Path.GetFullPath(file, projectFolder);
    }
}
