using DotnetAgentTools.Processes;
using DotnetAgentTools.Workspace;
using Microsoft.Extensions.Options;

namespace DotnetAgentTools.Tests;

/// <summary>
/// A copy of one fixture project in its own temporary folder, so tests can build it in parallel and leave nothing in
/// the repository. Deleted when disposed.
/// </summary>
internal sealed class FixtureWorkspace : IDisposable
{
    private static readonly string FixturesFolder = Path.Combine(AppContext.BaseDirectory, "fixtures");
    private static readonly string[] IsolationFiles = ["Directory.Build.props", "Directory.Packages.props"];

    private readonly DirectoryInfo tempFolder;

    public FixtureWorkspace(string fixtureName)
    {
        tempFolder = Directory.CreateTempSubdirectory("dotnet-agent-tools-tests-");
        foreach (var file in IsolationFiles)
        {
            File.Copy(Path.Combine(FixturesFolder, file), Path.Combine(tempFolder.FullName, file));
        }

        Root = Path.Combine(tempFolder.FullName, fixtureName);
        CopyFolder(Path.Combine(FixturesFolder, fixtureName), Root);
        Options = Microsoft.Extensions.Options.Options.Create(new WorkspaceOptions { Root = Root });
        Paths = new WorkspacePaths(Options);
    }

    public string Root { get; }

    public IOptions<WorkspaceOptions> Options { get; }

    public WorkspacePaths Paths { get; }

    public IDotnetCli CreateDotnetCli() => new DotnetCli(Options);

    public void Dispose() => tempFolder.Delete(recursive: true);

    private static void CopyFolder(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)));
        }

        foreach (var folder in Directory.GetDirectories(source))
        {
            CopyFolder(folder, Path.Combine(destination, Path.GetFileName(folder)));
        }
    }
}
