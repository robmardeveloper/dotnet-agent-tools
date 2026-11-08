using System.Text.RegularExpressions;
using ModelContextProtocol;

namespace DotnetAgentTools;

/// <summary>
/// Checks the values an agent sends before they reach the dotnet command line. Each value goes to dotnet as one
/// argument, never through a shell, but dotnet itself still reads some characters in a special way.
/// </summary>
internal static partial class ToolArguments
{
    public const int MaxListed = 200;
    private const int MaxFilterLength = 1000;

    /// <summary>
    /// dotnet turns "-c X" into the MSBuild property Configuration=X, and MSBuild reads ';' in it as the start of another
    /// property: "Debug;OutputPath=..." would set any property. Configuration names are plain words, so allow only those.
    /// </summary>
    public static string Configuration(string configuration) =>
        ConfigurationPattern().IsMatch(configuration)
            ? configuration
            : throw new McpException(
                $"'{configuration}' is not a valid configuration. Use a name such as Debug or Release.");

    /// <summary>A filter that starts with '-' would be read by dotnet test as another option, not as the filter.</summary>
    public static string? Filter(string? filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
        {
            return null;
        }

        return filter.StartsWith('-') || filter.Length > MaxFilterLength
            ? throw new McpException(
                $"The filter must not start with '-' and must be at most {MaxFilterLength} characters long.")
            : filter;
    }

    /// <summary>At least one item, and never so many that the answer stops being short.</summary>
    public static int Limit(int value) => Math.Clamp(value, 1, MaxListed);

    [GeneratedRegex("^[A-Za-z0-9_.-]{1,64}$")]
    private static partial Regex ConfigurationPattern();
}
