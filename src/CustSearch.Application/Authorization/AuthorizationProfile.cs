namespace CustSearch.Application.Authorization;

/// <summary>
/// Carries the active roles, permissions and allowed stores loaded from authoritative server data.
/// </summary>
public sealed record AuthorizationProfile(
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<long> StoreIds)
{
    public static AuthorizationProfile Empty { get; } = new([], [], []);
}
