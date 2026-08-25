namespace CustSearch.Infrastructure.AlertsRealtime;

/// <summary>Serializes matching in-process alert keys while the database unique index protects cross-process races.</summary>
public sealed class AlertDeduplicationCoordinator
{
    private sealed class Entry:IDisposable{public readonly SemaphoreSlim Gate=new(1,1);public int References;public void Dispose()=>Gate.Dispose();}
    private readonly object sync=new();
    private readonly Dictionary<string,Entry> entries=new(StringComparer.Ordinal);
    public async Task<T> ExecuteAsync<T>(long tenantId,string deduplicationKey,Func<Task<T>> action,CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);ArgumentException.ThrowIfNullOrWhiteSpace(deduplicationKey);ArgumentNullException.ThrowIfNull(action);var key=$"{tenantId}:{deduplicationKey.Trim()}";Entry entry;
        lock(sync){if(!entries.TryGetValue(key,out entry!)){entry=new();entries.Add(key,entry);}entry.References++;}
        var entered=false;
        try{await entry.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);entered=true;return await action().ConfigureAwait(false);}
        finally
        {
            if(entered)entry.Gate.Release();var dispose=false;lock(sync){entry.References--;if(entry.References==0){entries.Remove(key);dispose=true;}}if(dispose)entry.Dispose();
        }
    }
}
