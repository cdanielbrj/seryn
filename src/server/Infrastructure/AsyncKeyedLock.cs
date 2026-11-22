namespace Seryn.Server.Infrastructure;

public sealed class AsyncKeyedLock
{
    private sealed class Entry
    {
        public SemaphoreSlim Semaphore { get; } = new(1, 1);
        public int References;
    }

    private readonly object registryLock = new();
    private readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal);
    internal int ActiveKeyCount { get { lock (registryLock) return entries.Count; } }

    public async Task<IDisposable> Acquire(string key, CancellationToken cancellationToken = default)
    {
        Entry entry;
        lock (registryLock)
        {
            if (!entries.TryGetValue(key, out entry!))
            {
                entry = new Entry();
                entries.Add(key, entry);
            }
            entry.References++;
        }
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
        lock (registryLock)
        {
            entry.References--;
            if (entry.References == 0)
            {
                entries.Remove(key);
                entry.Semaphore.Dispose();
            }
        }
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
