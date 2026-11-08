using DotnetAgentTools.Testing;
using Xunit;

namespace DotnetAgentTools.Tests.Testing;

public sealed class StackTraceLocationTests
{
    [Fact]
    public void FindFirstInWorkspace_FramesOutsideTheWorkspace_AreSkipped()
    {
        using var workspace = new FixtureWorkspace("Clean");
        var outside = Path.Combine(Path.GetTempPath(), "framework", "Assert.cs");
        var inside = Path.Combine(workspace.Root, "Tests", "BookingTests.cs");
        var stackTrace = string.Join(
            '\n',
            $"   at Xunit.Assert.Equal() in {outside}:line 40",
            "   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj)",
            $"   at Tests.BookingTests.Cancel() in {inside}:line 57",
            $"   at Tests.BookingTests.Other() in {inside}:line 90");

        var location = StackTraceLocation.FindFirstInWorkspace(stackTrace, workspace.Paths);

        Assert.Equal(("Tests/BookingTests.cs", 57), location);
    }

    [Fact]
    public void FindFirstInWorkspace_NoFrameWithAFile_ReturnsNull()
    {
        using var workspace = new FixtureWorkspace("Clean");

        var location = StackTraceLocation.FindFirstInWorkspace(
            "   at System.Reflection.MethodBaseInvoker.InvokeWithNoArgs(Object obj)", workspace.Paths);

        Assert.Null(location);
    }
}
