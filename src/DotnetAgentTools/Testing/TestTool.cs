using System.ComponentModel;
using ModelContextProtocol.Server;

namespace DotnetAgentTools.Testing;

[McpServerToolType]
internal sealed class TestTool(TestRunner runner)
{
    private const int DefaultMaxFailures = 10;

    [McpServerTool(Name = "test", Idempotent = true, OpenWorld = false)]
    [Description(
        "Builds and runs the tests of a .NET solution or project and returns the counts and each failed test with its " +
        "message, file and line. If the build fails, returns the build errors instead. " +
        "Needs Microsoft.Testing.Platform and xUnit v3. Use it instead of running 'dotnet test' and reading its output.")]
    public async Task<string> TestAsync(
        [Description("Solution or project to test, relative to the workspace. Default: the only solution in the workspace root.")]
        string? path = null,
        [Description("Test filter passed to 'dotnet test --filter', for example 'FullyQualifiedName~BookingTests'. Default: all tests.")]
        string? filter = null,
        [Description("Build configuration. Default: Debug.")]
        string configuration = "Debug",
        [Description("Most failed tests to list. Default: 10.")]
        int maxFailures = DefaultMaxFailures,
        CancellationToken cancellationToken = default)
    {
        var request = new TestRequest(path, configuration, filter, Math.Max(1, maxFailures));
        var report = await runner.RunAsync(request, cancellationToken);
        return AgentJson.Serialize(report);
    }
}
