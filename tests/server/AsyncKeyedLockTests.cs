using Seryn.Server.Infrastructure;

namespace Seryn.Tests;

public sealed class AsyncKeyedLockTests
{
    [Fact]
    public async Task Same_key_is_exclusive_and_all_references_are_released_under_contention()
    {
        var keyedLock = new AsyncKeyedLock();
        var inside = 0;
        var maximumInside = 0;

        await Task.WhenAll(Enumerable.Range(0, 100).Select(async _ =>
        {
            using (await keyedLock.Acquire("shared"))
            {
                var current = Interlocked.Increment(ref inside);
                InterlockedExtensions.Max(ref maximumInside, current);
                await Task.Yield();
                Interlocked.Decrement(ref inside);
            }
        }));

        Assert.Equal(1, maximumInside);
        Assert.Equal(0, keyedLock.ActiveKeyCount);
    }

    [Fact]
    public async Task Cancelled_waiter_releases_its_reference_without_affecting_the_holder()
    {
        var keyedLock = new AsyncKeyedLock();
        using var holder = await keyedLock.Acquire("shared");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => keyedLock.Acquire("shared", cancellation.Token));
        Assert.Equal(1, keyedLock.ActiveKeyCount);

        holder.Dispose();
        Assert.Equal(0, keyedLock.ActiveKeyCount);
    }
}

internal static class InterlockedExtensions
{
    public static void Max(ref int location, int value)
    {
        int current;
        do
        {
            current = Volatile.Read(ref location);
            if (current >= value) return;
        } while (Interlocked.CompareExchange(ref location, value, current) != current);
    }
}
