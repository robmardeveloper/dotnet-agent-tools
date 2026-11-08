using System.ComponentModel;
using DotnetAgentTools.Processes;
using ModelContextProtocol.Server;

namespace DotnetAgentTools.Build;

[McpServerToolType]
internal sealed class BuildTool(BuildRunner runner, OneAtATime queue)
{
    private const int DefaultMaxItems = 20;

    [McpServerTool(Name = "build", Idempotent = true, OpenWorld = false)]
    [Description(
        "Builds a .NET solution or project and returns only what is needed to fix it: each error with file, line, " +
        "column and code (compiler, MSBuild, NuGet and SDK errors), and warnings counted by code. " +
        "Use it instead of running 'dotnet build' and reading its console output.")]
    public async Task<string> BuildAsync(
        [Description("Solution or project to build, relative to the workspace. Default: the only solution in the workspace root.")]
        string? path = null,
        [Description("Build configuration. Default: Debug.")]
        string configuration = "Debug",
        [Description("Also list each warning, not only their count by code. Default: false.")]
        bool includeWarnings = false,
        [Description("Most diagnostics to list, up to 200. Default: 20.")]
        int maxItems = DefaultMaxItems,
        CancellationToken cancellationToken = default)
    {
        var request = new BuildRequest(
            path, ToolArguments.Configuration(configuration), includeWarnings, ToolArguments.Limit(maxItems));
        var report = await queue.RunAsync(() => runner.RunAsync(request, cancellationToken), cancellationToken);
        return AgentJson.Serialize(report);
    }
}
