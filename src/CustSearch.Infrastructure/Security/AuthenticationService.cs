using System.Data;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CustSearch.Infrastructure.Security;

/// <summary>
/// Implements credential validation, short-lived JWT issuance and rotating refresh sessions.
/// </summary>
/// <remarks>
/// Raw refresh tokens are returned only to the API cookie boundary. The database stores
/// SHA-256 hashes, and reuse of a rotated/revoked token revokes its entire token family.
/// </remarks>
public sealed class AuthenticationService(
    CustSearchDbContext dbContext,
    IPasswordHasher<UserAccount> passwordHasher,
    JwtTokenService jwtTokenService,
    IOptions<JwtOptions> jwtOptions,
    TimeProvider timeProvider) : IAuthenticationService
{
    private static readonly SemaphoreSlim SqliteRefreshGate = new(1, 1);
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private readonly string _dummyPasswordHash = passwordHasher.HashPassword(null!, Guid.NewGuid().ToString("N"));

    public async Task<AuthenticationResult> LoginAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.UserName);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.Password);
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var normalizedUserName = command.UserName.Trim().ToUpperInvariant();
        var normalizedTenantCode = command.TenantCode?.Trim().ToUpperInvariant();

        var query = dbContext.UserAccounts.Include(user => user.Tenant).AsQueryable();
        UserAccount? user;
        if (string.IsNullOrWhiteSpace(normalizedTenantCode))
        {
            user = await query.SingleOrDefaultAsync(
                candidate => candidate.Scope == UserScope.Platform
                    && candidate.TenantId == null
                    && candidate.NormalizedUserName == normalizedUserName,
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            user = await query.SingleOrDefaultAsync(
                candidate => candidate.Scope == UserScope.Tenant
                    && candidate.Tenant != null
                    && candidate.Tenant.TenantCode == normalizedTenantCode
                    && candidate.NormalizedUserName == normalizedUserName,
                cancellationToken).ConfigureAwait(false);
        }

        var passwordResult = user is null
            ? passwordHasher.VerifyHashedPassword(null!, _dummyPasswordHash, command.Password)
            : passwordHasher.VerifyHashedPassword(user, user.PasswordHash, command.Password);
        if (user is null || passwordResult == PasswordVerificationResult.Failed)
        {
            throw await CreateFailureAsync(
                user,
                "LoginFailed",
                AuthenticationFailure.InvalidCredentials,
                command.IpAddress,
                command.CorrelationId,
                utcNow,
                cancellationToken).ConfigureAwait(false);
        }

        if (GetAvailabilityFailure(user) is { } availabilityFailure)
        {
            throw await CreateFailureAsync(
                user,
                "LoginFailed",
                availabilityFailure,
                command.IpAddress,
                command.CorrelationId,
                utcNow,
                cancellationToken).ConfigureAwait(false);
        }

        if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.SetPasswordHash(passwordHasher.HashPassword(user, command.Password));
        }

        user.RecordSuccessfulLogin(utcNow);
        var authorization = await LoadAuthorizationProfileAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var result = IssueSession(user, authorization, Guid.NewGuid(), command.IpAddress, utcNow);
        dbContext.RefreshTokens.Add(result.StoredRefreshToken);
        dbContext.AuthenticationEvents.Add(AuthenticationEvent.Record(
            user.Id,
            user.TenantId,
            "LoginSucceeded",
            true,
            null,
            utcNow,
            command.IpAddress,
            command.CorrelationId));
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result.Result;
    }

    public async Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        var useLocalProviderGate = string.Equals(
            dbContext.Database.ProviderName,
            "Microsoft.EntityFrameworkCore.Sqlite",
            StringComparison.Ordinal);
        if (useLocalProviderGate)
        {
            await SqliteRefreshGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            return await RefreshCoreAsync(
                refreshToken,
                ipAddress,
                correlationId,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (useLocalProviderGate)
            {
                SqliteRefreshGate.Release();
            }
        }
    }

    private async Task<AuthenticationResult> RefreshCoreAsync(
        string refreshToken,
        string? ipAddress,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var tokenHash = RefreshTokenFactory.Hash(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .ThenInclude(user => user.Tenant)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);

        if (storedToken is null)
        {
            throw await CreateFailureAsync(
                null,
                "RefreshFailed",
                AuthenticationFailure.InvalidRefreshToken,
                ipAddress,
                correlationId,
                utcNow,
                cancellationToken).ConfigureAwait(false);
        }

        if (storedToken.RevokedUtc is not null)
        {
            await RevokeFamilyAsync(
                storedToken.UserId,
                storedToken.FamilyId,
                utcNow,
                "ReuseDetected",
                ipAddress,
                cancellationToken).ConfigureAwait(false);
            dbContext.AuthenticationEvents.Add(AuthenticationEvent.Record(
                storedToken.UserId,
                storedToken.User.TenantId,
                "RefreshTokenReuseDetected",
                false,
                AuthenticationFailure.ReusedRefreshToken.ToString(),
                utcNow,
                ipAddress,
                correlationId));
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new AuthenticationFailureException(AuthenticationFailure.ReusedRefreshToken);
        }

        if (storedToken.IsExpired(utcNow))
        {
            storedToken.Revoke(utcNow, "Expired", ipAddress);
            dbContext.AuthenticationEvents.Add(AuthenticationEvent.Record(
                storedToken.UserId,
                storedToken.User.TenantId,
                "RefreshFailed",
                false,
                AuthenticationFailure.ExpiredRefreshToken.ToString(),
                utcNow,
                ipAddress,
                correlationId));
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new AuthenticationFailureException(AuthenticationFailure.ExpiredRefreshToken);
        }

        if (!string.Equals(
                storedToken.IssuedSecurityStamp,
                storedToken.User.SecurityStamp,
                StringComparison.Ordinal))
        {
            // Refresh cookies are bound to the security stamp present at issuance. A password
            // or security change therefore invalidates refresh directly, without needing /me first.
            await RevokeAllUserSessionsAsync(
                storedToken.UserId,
                utcNow,
                AuthenticationFailure.SessionRevoked.ToString(),
                ipAddress,
                cancellationToken).ConfigureAwait(false);
            dbContext.AuthenticationEvents.Add(AuthenticationEvent.Record(
                storedToken.UserId,
                storedToken.User.TenantId,
                "RefreshFailed",
                false,
                AuthenticationFailure.SessionRevoked.ToString(),
                utcNow,
                ipAddress,
                correlationId));
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new AuthenticationFailureException(AuthenticationFailure.SessionRevoked);
        }

        if (GetAvailabilityFailure(storedToken.User) is { } availabilityFailure)
        {
            await RevokeFamilyAsync(
                storedToken.UserId,
                storedToken.FamilyId,
                utcNow,
                availabilityFailure.ToString(),
                ipAddress,
                cancellationToken).ConfigureAwait(false);
            dbContext.AuthenticationEvents.Add(AuthenticationEvent.Record(
                storedToken.UserId,
                storedToken.User.TenantId,
                "RefreshFailed",
                false,
                availabilityFailure.ToString(),
                utcNow,
                ipAddress,
                correlationId));
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new AuthenticationFailureException(availabilityFailure);
        }

        var authorization = await LoadAuthorizationProfileAsync(storedToken.UserId, cancellationToken)
            .ConfigureAwait(false);
        var result = IssueSession(storedToken.User, authorization, storedToken.FamilyId, ipAddress, utcNow);
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            await using var transaction = await dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                // This single conditional UPDATE is the consume point. SQL Server takes an
                // update/exclusive row lock; a competing request waits and then affects zero rows.
                var consumed = await dbContext.RefreshTokens
                    .Where(token => token.Id == storedToken.Id
                        && token.RevokedUtc == null
                        && token.ExpiresUtc > utcNow)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(token => token.RevokedUtc, utcNow)
                            .SetProperty(token => token.RevokedReason, "Rotated")
                            .SetProperty(token => token.RevokedByIp, ipAddress)
                            .SetProperty(token => token.ReplacedByTokenHash, result.StoredRefreshToken.TokenHash),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (consumed == 0)
                {
                    await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                    dbContext.ChangeTracker.Clear();
                    await RecordReuseAndRevokeFamilyAsync(
                        storedToken.UserId,
                        storedToken.User.TenantId,
                        storedToken.FamilyId,
                        utcNow,
                        ipAddress,
                        correlationId,
                        cancellationToken).ConfigureAwait(false);
                    throw new AuthenticationFailureException(AuthenticationFailure.ReusedRefreshToken);
                }

                dbContext.RefreshTokens.Add(result.StoredRefreshToken);
                dbContext.AuthenticationEvents.Add(AuthenticationEvent.Record(
                    storedToken.UserId,
                    storedToken.User.TenantId,
                    "TokenRefreshed",
                    true,
                    null,
                    utcNow,
                    ipAddress,
                    correlationId));
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result.Result;
            }
            catch (SqlException exception) when (exception.Number == 1205)
            {
                await TryRollbackDeadlockVictimAsync(transaction, cancellationToken).ConfigureAwait(false);
                dbContext.ChangeTracker.Clear();
                if (attempt == 3)
                {
                    throw new AuthenticationFailureException(AuthenticationFailure.SessionRevoked);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(25 * attempt), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException("Refresh-token consume retry limit was reached.");
    }

    private static async Task TryRollbackDeadlockVictimAsync(
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            // SQL Server already rolled back a deadlock victim.
        }
        catch (SqlException rollbackException) when (rollbackException.Number is 3902 or 3903)
        {
            // There is no active transaction left to roll back.
        }
    }

    private async Task RecordReuseAndRevokeFamilyAsync(
        long userId,
        long? tenantId,
        Guid familyId,
        DateTime utcNow,
        string? ipAddress,
        string correlationId,
        CancellationToken cancellationToken)
    {
        await RevokeFamilyAsync(
            userId,
            familyId,
            utcNow,
            "ReuseDetected",
            ipAddress,
            cancellationToken).ConfigureAwait(false);
        dbContext.AuthenticationEvents.Add(AuthenticationEvent.Record(
            userId,
            tenantId,
            "RefreshTokenReuseDetected",
            false,
            AuthenticationFailure.ReusedRefreshToken.ToString(),
            utcNow,
            ipAddress,
            correlationId));
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task LogoutAsync(
        string refreshToken,
        string? ipAddress,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var tokenHash = RefreshTokenFactory.Hash(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken)
            .ConfigureAwait(false);
        if (storedToken is not null && storedToken.RevokedUtc is null)
        {
            storedToken.Revoke(utcNow, "Logout", ipAddress);
        }

        dbContext.AuthenticationEvents.Add(AuthenticationEvent.Record(
            storedToken?.UserId,
            storedToken?.User.TenantId,
            "Logout",
            true,
            null,
            utcNow,
            ipAddress,
            correlationId));
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<AuthenticatedUser> GetCurrentUserAsync(
        long userId,
        string securityStamp,
        string? ipAddress,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        var user = await dbContext.UserAccounts
            .Include(candidate => candidate.Tenant)
            .SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null)
        {
            throw new AuthenticationFailureException(AuthenticationFailure.SessionRevoked);
        }

        var failure = GetAvailabilityFailure(user)
            ?? (!string.Equals(user.SecurityStamp, securityStamp, StringComparison.Ordinal)
                ? AuthenticationFailure.SessionRevoked
                : null);
        if (failure is { } sessionFailure)
        {
            var utcNow = timeProvider.GetUtcNow().UtcDateTime;
            var activeTokens = await dbContext.RefreshTokens
                .Where(token => token.UserId == user.Id && token.RevokedUtc == null)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
            foreach (var activeToken in activeTokens)
            {
                activeToken.Revoke(utcNow, sessionFailure.ToString(), ipAddress);
            }

            // A replayed invalid JWT must not create an unbounded audit row on every request.
            // One matching rejection per user and five-minute window preserves evidence safely.
            var rejectionWindowStart = utcNow.AddMinutes(-5);
            var rejectionAlreadyRecorded = await dbContext.AuthenticationEvents.AnyAsync(
                audit => audit.UserId == user.Id
                    && audit.EventType == "AccessSessionRejected"
                    && audit.FailureCode == sessionFailure.ToString()
                    && audit.OccurredUtc >= rejectionWindowStart,
                cancellationToken).ConfigureAwait(false);
            if (!rejectionAlreadyRecorded)
            {
                dbContext.AuthenticationEvents.Add(AuthenticationEvent.Record(
                    user.Id,
                    user.TenantId,
                    "AccessSessionRejected",
                    false,
                    sessionFailure.ToString(),
                    utcNow,
                    ipAddress,
                    correlationId));
            }
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new AuthenticationFailureException(sessionFailure);
        }

        var authorization = await LoadAuthorizationProfileAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return MapUser(user, authorization);
    }

    private IssuedSession IssueSession(
        UserAccount user,
        AuthorizationProfile authorization,
        Guid familyId,
        string? ipAddress,
        DateTime utcNow)
    {
        var accessToken = jwtTokenService.Create(user, authorization, utcNow);
        var generatedRefreshToken = RefreshTokenFactory.Create();
        var refreshExpiresUtc = utcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays);
        var storedRefreshToken = RefreshToken.Issue(
            user.Id,
            generatedRefreshToken.TokenHash,
            familyId,
            user.SecurityStamp,
            utcNow,
            refreshExpiresUtc,
            ipAddress);
        var result = new AuthenticationResult(
            accessToken.Token,
            accessToken.ExpiresUtc,
            generatedRefreshToken.RawToken,
            refreshExpiresUtc,
            MapUser(user, authorization));
        return new IssuedSession(result, storedRefreshToken);
    }

    private async Task<AuthenticationFailureException> CreateFailureAsync(
        UserAccount? user,
        string eventType,
        AuthenticationFailure failure,
        string? ipAddress,
        string correlationId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        dbContext.AuthenticationEvents.Add(AuthenticationEvent.Record(
            user?.Id,
            user?.TenantId,
            eventType,
            false,
            failure.ToString(),
            utcNow,
            ipAddress,
            correlationId));
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new AuthenticationFailureException(failure);
    }

    private async Task RevokeFamilyAsync(
        long userId,
        Guid familyId,
        DateTime utcNow,
        string reason,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var activeFamilyTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.FamilyId == familyId && token.RevokedUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var familyToken in activeFamilyTokens)
        {
            familyToken.Revoke(utcNow, reason, ipAddress);
        }
    }

    private async Task RevokeAllUserSessionsAsync(
        long userId,
        DateTime utcNow,
        string reason,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedUtc == null)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        foreach (var activeToken in activeTokens)
        {
            activeToken.Revoke(utcNow, reason, ipAddress);
        }
    }

    private static AuthenticationFailure? GetAvailabilityFailure(UserAccount user)
    {
        if (!user.IsActive)
        {
            return AuthenticationFailure.UserDisabled;
        }

        if (user.Scope == UserScope.Tenant
            && (user.Tenant is null || !user.Tenant.IsActive || user.Tenant.IsSuspended))
        {
            return AuthenticationFailure.TenantUnavailable;
        }

        return null;
    }

    private async Task<AuthorizationProfile> LoadAuthorizationProfileAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        var activeAssignments = dbContext.UserRoles
            .AsNoTracking()
            .Where(assignment => assignment.UserId == userId
                && assignment.Role.IsActive
                && assignment.Role.Scope == assignment.User.Scope
                && assignment.Role.TenantId == assignment.User.TenantId);
        var roles = await activeAssignments
            .Select(assignment => assignment.Role.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var permissions = await activeAssignments
            .SelectMany(assignment => assignment.Role.RolePermissions)
            .Where(grant => grant.Permission.IsActive && grant.Permission.Scope == grant.Role.Scope)
            .Select(grant => grant.Permission.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        // Store assignments are introduced in Phase 5. Keeping the server-owned list empty
        // prevents clients from inventing access until that authoritative relation exists.
        return new AuthorizationProfile(roles, permissions, []);
    }

    private static AuthenticatedUser MapUser(UserAccount user, AuthorizationProfile authorization)
    {
        var isPlatformAdmin = user.Scope == UserScope.Platform;
        return new AuthenticatedUser(
            user.Id,
            user.TenantId,
            user.Tenant?.TenantCode,
            user.UserName,
            user.DisplayName,
            user.Email,
            isPlatformAdmin,
            authorization.Roles,
            authorization.Permissions,
            authorization.StoreIds);
    }

    private sealed record IssuedSession(AuthenticationResult Result, RefreshToken StoredRefreshToken);
}
