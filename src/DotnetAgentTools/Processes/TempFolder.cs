namespace DotnetAgentTools.Processes;

/// <summary>
/// A temporary folder for the binary logs and TRX files of one command, deleted when disposed. Deleting is best
/// effort: on Windows a process that was just stopped can still hold a file for a moment, and that must not hide the
/// result the agent is waiting for.
/// </summary>
internal sealed class TempFolder : IDisposable
{
    private readonly DirectoryInfo folder = Directory.CreateTempSubdirectory("dotnet-agent-tools-");

    public string FullName => folder.FullName;

    public IEnumerable<string> FindFiles(string pattern) =>
        Directory.EnumerateFiles(folder.FullName, pattern, SearchOption.AllDirectories);

    public void Dispose()
    {
        try
        {
            folder.Delete(recursive: true);
        }
        catch (IOException)
        {
            // Left for the system's temporary-file cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Same.
        }
    }
}
