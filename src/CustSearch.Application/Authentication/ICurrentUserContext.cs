namespace CustSearch.Application.Authentication;

/// <summary>
/// Exposes only server-validated identity claims to tenant-aware application services.
/// </summary>
public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    long UserId { get; }

    long? TenantId { get; }

    bool IsPlatformAdmin { get; }

    string SecurityStamp { get; }

    IReadOnlySet<string> Roles { get; }

    IReadOnlySet<string> Permissions { get; }

    IReadOnlySet<long> StoreIds { get; }
}
