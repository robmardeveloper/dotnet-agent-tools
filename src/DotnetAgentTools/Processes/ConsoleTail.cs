namespace DotnetAgentTools.Processes;

/// <summary>
/// The end of a command's console output in one line. Used only when a command fails without a result the tool can
/// read, so the agent still sees why.
/// </summary>
internal static class ConsoleTail
{
    private const int MaxLines = 15;

    public static string LastLines(string output)
    {
        var lastLines = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .TakeLast(MaxLines);
        return string.Join(" | ", lastLines);
    }
}
