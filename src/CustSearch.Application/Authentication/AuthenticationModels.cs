namespace CustSearch.Application.Authentication;

public sealed record LoginCommand(
    string? TenantCode,
    string UserName,
    string Password,
    string? IpAddress,
    string CorrelationId);

/// <summary>
/// Carries a self-service password change. UserId is derived from the validated access token;
/// neither tenant nor store scope is accepted from the browser.
/// </summary>
public sealed record ChangePasswordCommand(
    long UserId,
    string CurrentPassword,
    string NewPassword,
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
