using System.Collections.Concurrent;

namespace CustSearch.Infrastructure.AlertsRealtime;

/// <summary>Serializes matching in-process alert keys while the database unique index protects cross-process races.</summary>
public sealed class AlertDeduplicationCoordinator
{
    private sealed class Entry{public readonly SemaphoreSlim Gate=new(1,1);public int References;}
    private readonly ConcurrentDictionary<string,Entry> entries=new(StringComparer.Ordinal);
    public async Task<T> ExecuteAsync<T>(long tenantId,string deduplicationKey,Func<Task<T>> action,CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentException.ThrowIfNullOrWhiteSpace(deduplicationKey);ArgumentNullException.ThrowIfNull(action);var key=$"{tenantId}:{deduplicationKey.Trim()}";Entry entry;
        while(true){entry=entries.GetOrAdd(key,_=>new());Interlocked.Increment(ref entry.References);if(entries.TryGetValue(key,out var current)&&ReferenceEquals(current,entry))break;Interlocked.Decrement(ref entry.References);}
        await entry.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try{return await action().ConfigureAwait(false);}
        finally{entry.Gate.Release();if(Interlocked.Decrement(ref entry.References)==0)entries.TryRemove(new KeyValuePair<string,Entry>(key,entry));}
    }
}
