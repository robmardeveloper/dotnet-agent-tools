namespace DotnetAgentTools.Processes;

/// <summary>
/// Lets one tool call run dotnet at a time. Agents can call tools in parallel, and two builds of the same project at
/// once fight over the same obj and bin files.
/// </summary>
internal sealed class OneAtATime : IDisposable
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task<T> RunAsync<T>(Func<Task<T>> work, CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await work();
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose() => gate.Dispose();
}
