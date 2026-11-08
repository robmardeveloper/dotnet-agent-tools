using DotnetAgentTools.Processes;
using Xunit;

namespace DotnetAgentTools.Tests.Processes;

public sealed class OneAtATimeTests
{
    [Fact]
    public async Task RunAsync_SecondCall_WaitsForTheFirst()
    {
        using var queue = new OneAtATime();
        var firstCanFinish = new TaskCompletionSource();
        var ct = TestContext.Current.CancellationToken;

        var first = queue.RunAsync(async () => { await firstCanFinish.Task; return 1; }, ct);
        var second = queue.RunAsync(() => Task.FromResult(2), ct);

        await Task.Delay(100, ct);
        Assert.False(second.IsCompleted);

        firstCanFinish.SetResult();
        var results = await Task.WhenAll(first, second);
        Assert.Equal([1, 2], results);
    }
}
