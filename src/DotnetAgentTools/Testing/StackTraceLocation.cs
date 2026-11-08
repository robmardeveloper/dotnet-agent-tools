using System.Globalization;
using System.Text.RegularExpressions;
using DotnetAgentTools.Workspace;

namespace DotnetAgentTools.Testing;

/// <summary>
/// Finds where a test failed: the first frame of the stack trace that is in the workspace's own code, skipping the
/// frames of the test framework and of .NET itself.
/// </summary>
internal static partial class StackTraceLocation
{
    public static (string File, int Line)? FindFirstInWorkspace(string? stackTrace, WorkspacePaths paths)
    {
        if (string.IsNullOrEmpty(stackTrace))
        {
            return null;
        }

        foreach (Match frame in FramePattern().Matches(stackTrace))
        {
            var file = frame.Groups["file"].Value;
            if (Path.IsPathFullyQualified(file) && paths.IsInsideRoot(file))
            {
                return (paths.ToRelative(file), int.Parse(frame.Groups["line"].Value, CultureInfo.InvariantCulture));
            }
        }

        return null;
    }

    // "   at Namespace.Class.Method() in C:\src\File.cs:line 12"
    [GeneratedRegex(@"^\s*at .+ in (?<file>.+):line (?<line>\d+)\s*$", RegexOptions.Multiline)]
    private static partial Regex FramePattern();
}
