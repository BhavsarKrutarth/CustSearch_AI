using CustSearch.Application.Authentication;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.Security;

/// <summary>
/// Phase 5B — enriches the established authentication/session implementation with authoritative
/// active store assignments. Token rotation, password verification and reuse detection remain in
/// <see cref="AuthenticationService"/>; this decorator only completes the Phase 5 StoreIds contract.
/// </summary>
public sealed class PhaseFiveAuthenticationServiceDecorator(
    AuthenticationService inner,
    CustSearchDbContext dbContext) : IAuthenticationService
{
    public async Task<AuthenticationResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.LoginAsync(command, cancellationToken).ConfigureAwait(false);
        return result with { User = await EnrichAsync(result.User, cancellationToken).ConfigureAwait(false) };
    }

    public async Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.RefreshAsync(refreshToken, ipAddress, correlationId, cancellationToken).ConfigureAwait(false);
        return result with { User = await EnrichAsync(result.User, cancellationToken).ConfigureAwait(false) };
    }

    public Task LogoutAsync(
        string refreshToken,
        string? ipAddress,
        string correlationId,
        CancellationToken cancellationToken = default) =>
        inner.LogoutAsync(refreshToken, ipAddress, correlationId, cancellationToken);

    public async Task<AuthenticatedUser> GetCurrentUserAsync(
        long userId,
        string securityStamp,
        string? ipAddress,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var user = await inner.GetCurrentUserAsync(
            userId,
            securityStamp,
            ipAddress,
            correlationId,
            cancellationToken).ConfigureAwait(false);
        return await EnrichAsync(user, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AuthenticatedUser> EnrichAsync(
        AuthenticatedUser user,
        CancellationToken cancellationToken)
    {
        if (user.IsPlatformAdmin || user.TenantId is null)
        {
            return user with { StoreIds = [] };
        }

        var storeIds = await dbContext.UserStoreAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == user.UserId
                && assignment.TenantId == user.TenantId.Value
                && assignment.Store.IsActive
                && assignment.User.IsActive)
            .OrderByDescending(assignment => assignment.IsPrimary)
            .ThenBy(assignment => assignment.StoreId)
            .Select(assignment => assignment.StoreId)
            .Distinct()
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return user with { StoreIds = storeIds };
    }
}
