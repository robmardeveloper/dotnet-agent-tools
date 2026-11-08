using DotnetAgentTools.Build;
using DotnetAgentTools.Processes;
using DotnetAgentTools.Testing;
using Xunit;

namespace DotnetAgentTools.Tests.Testing;

public sealed class TestRunnerEnvironmentTests
{
    [Fact]
    public async Task RunAsync_DotnetTest_KeepsTheMachineLanguage()
    {
        // Forcing English reaches the test processes too and changed the result of a test that checks Spanish text.
        using var workspace = new FixtureWorkspace("FailingTests");
        var dotnet = new RecordingDotnetCli();
        var runner = new TestRunner(
            new BuildRunner(dotnet, workspace.Paths, TimeProvider.System), dotnet, workspace.Paths, TimeProvider.System);

        await runner.RunAsync(new TestRequest(null, "Debug", null, 10), TestContext.Current.CancellationToken);

        Assert.Equal([("build", true), ("test", false)], dotnet.Calls);
    }

    /// <summary>Answers every command with success and remembers which command asked for English messages.</summary>
    private sealed class RecordingDotnetCli : IDotnetCli
    {
        public List<(string Command, bool EnglishMessages)> Calls { get; } = [];

        public Task<CommandResult> RunAsync(
            IReadOnlyList<string> arguments, string workingDirectory, bool englishMessages, CancellationToken cancellationToken)
        {
            Calls.Add((arguments[0], englishMessages));
            return Task.FromResult(new CommandResult(0, string.Empty, string.Empty, TimedOut: false));
        }
    }
}
