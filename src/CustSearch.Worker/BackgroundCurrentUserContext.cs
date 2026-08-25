using CustSearch.Application.Authentication;
using CustSearch.Application.AlertsRealtime;

namespace CustSearch.Worker;

/// <summary>
/// Fail-closed identity for background processes. Worker jobs receive their scope from persisted,
/// server-owned job records and must never impersonate a browser user or invent a tenant/store.
/// </summary>
internal sealed class BackgroundCurrentUserContext : ICurrentUserContext
{
    public bool IsAuthenticated => false;
    public long UserId => 0;
    public long? TenantId => null;
    public bool IsPlatformAdmin => false;
    public string SecurityStamp => string.Empty;
    public IReadOnlySet<string> Roles { get; } = new HashSet<string>(StringComparer.Ordinal);
    public IReadOnlySet<string> Permissions { get; } = new HashSet<string>(StringComparer.Ordinal);
    public IReadOnlySet<long> StoreIds { get; } = new HashSet<long>();
}

/// <summary>
/// Worker processes have no browser SignalR connections. Returning zero keeps shared operational
/// services observable without pretending that this non-HTTP process owns any tenant connection.
/// </summary>
internal sealed class BackgroundAlertConnectionMetrics : IAlertConnectionMetrics
{
    public long ActiveConnections(long tenantId) => 0;
    public long Reconnects(long tenantId) => 0;
    public void Connected(string connectionId, long tenantId) { }
    public void Disconnected(string connectionId) { }
    public void Reconnected(string connectionId, long tenantId) { }
}
