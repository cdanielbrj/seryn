using System.Collections.Concurrent;

namespace Seryn.Server.Infrastructure;

public sealed class AsyncKeyedLock
{
    private sealed class Entry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int References;
    }

    private readonly ConcurrentDictionary<string, Entry> entries = new(StringComparer.Ordinal);

    public async Task<IDisposable> Acquire(string key, CancellationToken cancellationToken = default)
    {
        var entry = entries.AddOrUpdate(key,
            _ => new Entry { References = 1 },
            (_, current) => { Interlocked.Increment(ref current.References); return current; });
        try
        {
            await entry.Semaphore.WaitAsync(cancellationToken);
            return new Releaser(this, key, entry);
        }
        catch
        {
            ReleaseReference(key, entry, releaseSemaphore: false);
            throw;
        }
    }

    private void ReleaseReference(string key, Entry entry, bool releaseSemaphore)
    {
        if (releaseSemaphore) entry.Semaphore.Release();
        if (Interlocked.Decrement(ref entry.References) == 0 &&
            entries.TryRemove(new KeyValuePair<string, Entry>(key, entry)))
            entry.Semaphore.Dispose();
    }

    private sealed class Releaser(AsyncKeyedLock owner, string key, Entry entry) : IDisposable
    {
        private int released;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref released, 1) == 0)
                owner.ReleaseReference(key, entry, releaseSemaphore: true);
        }
    }
}
