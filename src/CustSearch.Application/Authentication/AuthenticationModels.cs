namespace CustSearch.Application.Authentication;

public sealed record LoginCommand(
    string? TenantCode,
    string UserName,
    string Password,
    string? IpAddress,
    string CorrelationId);

public sealed record AuthenticatedUser(
    long UserId,
    long? TenantId,
    string? TenantCode,
    string UserName,
    string DisplayName,
    string Email,
    bool IsPlatformAdmin,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<long> StoreIds);

public sealed record AuthenticationResult(
    string AccessToken,
    DateTime AccessTokenExpiresUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresUtc,
    AuthenticatedUser User);
