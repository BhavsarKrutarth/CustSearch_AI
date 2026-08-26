namespace CustSearch.Application.Authentication;

/// <summary>
/// Coordinates login and secure refresh-token session lifecycle operations.
/// </summary>
public interface IAuthenticationService
{
    Task<AuthenticationResult> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);

    Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        string refreshToken,
        string? ipAddress,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthenticatedUser> GetCurrentUserAsync(
        long userId,
        string securityStamp,
        string? ipAddress,
        string correlationId,
        CancellationToken cancellationToken = default);
}
