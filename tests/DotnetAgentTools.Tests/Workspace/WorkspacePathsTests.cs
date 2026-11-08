using DotnetAgentTools.Workspace;
using ModelContextProtocol;
using Xunit;

namespace DotnetAgentTools.Tests.Workspace;

public sealed class WorkspacePathsTests
{
    [Fact]
    public void ResolveTarget_NoPath_UsesTheOnlyProjectInTheRoot()
    {
        using var workspace = new FixtureWorkspace("Clean");

        var target = workspace.Paths.ResolveTarget(path: null);

        Assert.Equal(Path.Combine(workspace.Root, "Clean.csproj"), target);
    }

    [Fact]
    public void ResolveTarget_PathOutsideTheWorkspace_IsRejected()
    {
        using var workspace = new FixtureWorkspace("Clean");

        var error = Assert.Throws<McpException>(() => workspace.Paths.ResolveTarget(Path.Combine("..", "MissingPackage")));

        Assert.Contains("outside the workspace", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveTarget_FolderThatStartsLikeTheRoot_IsRejected()
    {
        using var workspace = new FixtureWorkspace("Clean");
        var sibling = workspace.Root + "-other";

        Assert.Throws<McpException>(() => workspace.Paths.ResolveTarget(sibling));
    }

    [Fact]
    public void ResolveTarget_PathThatDoesNotExist_IsRejected()
    {
        using var workspace = new FixtureWorkspace("Clean");

        var error = Assert.Throws<McpException>(() => workspace.Paths.ResolveTarget("Missing.csproj"));

        Assert.Contains("does not exist", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveTarget_FileThatIsNotAProject_IsRejected()
    {
        using var workspace = new FixtureWorkspace("Clean");

        var error = Assert.Throws<McpException>(() => workspace.Paths.ResolveTarget("Program.cs"));

        Assert.Contains("is not a solution", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveTarget_Folder_IsAccepted()
    {
        using var workspace = new FixtureWorkspace("Clean");

        Assert.Equal(workspace.Root, workspace.Paths.ResolveTarget("."));
    }

    [Fact]
    public void ToRelative_FileInsideTheRoot_UsesForwardSlashes()
    {
        using var workspace = new FixtureWorkspace("Clean");

        var relative = workspace.Paths.ToRelative(Path.Combine(workspace.Root, "src", "Program.cs"));

        Assert.Equal("src/Program.cs", relative);
    }
}
