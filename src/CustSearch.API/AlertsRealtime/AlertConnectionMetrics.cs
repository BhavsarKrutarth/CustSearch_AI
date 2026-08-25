using System.Collections.Concurrent;
using CustSearch.Application.AlertsRealtime;

namespace CustSearch.API.AlertsRealtime;

/// <summary>Counts active authenticated hub connections and reconnect reports per tenant.</summary>
public sealed class AlertConnectionMetrics:IAlertConnectionMetrics
{
    private readonly ConcurrentDictionary<string,long> connections=new(StringComparer.Ordinal);private readonly ConcurrentDictionary<long,long> reconnects=new();
    public long ActiveConnections(long tenantId)=>connections.LongCount(x=>x.Value==tenantId);
    public long Reconnects(long tenantId)=>reconnects.TryGetValue(tenantId,out var count)?count:0;
    public long TotalActiveConnections()=>connections.Count;
    public long TotalReconnects()=>reconnects.Values.Sum();
    public void Connected(string connectionId,long tenantId){ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);connections[connectionId]=tenantId;}
    public void Disconnected(string connectionId){if(!string.IsNullOrWhiteSpace(connectionId))connections.TryRemove(connectionId,out _);}
    public void Reconnected(string connectionId,long tenantId){Connected(connectionId,tenantId);reconnects.AddOrUpdate(tenantId,1,(_,value)=>value+1);}
}
