using Microsoft.Extensions.Options;
using ModelContextProtocol;

namespace DotnetAgentTools.Workspace;

/// <summary>Turns the paths an agent sends into safe, full paths inside the workspace.</summary>
internal sealed class WorkspacePaths(IOptions<WorkspaceOptions> options)
{
    private static readonly string[] SolutionPatterns = ["*.slnx", "*.sln"];
    private const string ProjectPattern = "*.*proj";

    public string Root { get; } = Path.TrimEndingDirectorySeparator(
        Path.GetFullPath(options.Value.Root ?? Directory.GetCurrentDirectory()));

    /// <summary>
    /// Resolves the solution or project to build. With no path, uses the only solution in the root, or else its only
    /// project. Rejects anything outside the root.
    /// </summary>
    public string ResolveTarget(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return FindDefaultTarget();
        }

        var fullPath = Path.GetFullPath(path, Root);
        if (!IsInsideRoot(fullPath))
        {
            throw new McpException($"The path '{path}' is outside the workspace. Use a path inside '{Root}'.");
        }

        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            throw new McpException($"The path '{path}' does not exist.");
        }

        return fullPath;
    }

    /// <summary>Gives a path relative to the root with forward slashes, or the path unchanged if it is outside.</summary>
    public string ToRelative(string fullPath)
    {
        return IsInsideRoot(fullPath)
            ? Path.GetRelativePath(Root, fullPath).Replace('\\', '/')
            : fullPath;
    }

    private bool IsInsideRoot(string fullPath)
    {
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return string.Equals(fullPath, Root, comparison)
            || fullPath.StartsWith(Root + Path.DirectorySeparatorChar, comparison);
    }

    private string FindDefaultTarget()
    {
        var solutions = SolutionPatterns.SelectMany(pattern => Directory.GetFiles(Root, pattern)).ToArray();
        if (solutions.Length == 1)
        {
            return solutions[0];
        }

        if (solutions.Length > 1)
        {
            throw new McpException(
                $"The workspace has {solutions.Length} solutions. Say which one with 'path': " +
                string.Join(", ", solutions.Select(Path.GetFileName)));
        }

        var projects = Directory.GetFiles(Root, ProjectPattern);
        return projects.Length == 1
            ? projects[0]
            : throw new McpException("No single solution or project found in the workspace root. Say which one with 'path'.");
    }
}
