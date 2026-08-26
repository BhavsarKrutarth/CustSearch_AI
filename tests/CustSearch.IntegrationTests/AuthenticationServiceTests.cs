using CustSearch.Application.Authentication;
using CustSearch.Domain.Entities;
using CustSearch.Infrastructure.Persistence;
using CustSearch.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CustSearch.IntegrationTests;

public sealed class AuthenticationServiceTests
{
    private static readonly DateTime TestNow = new(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ConcurrentRefreshAllowsOneRotationAndInvalidatesCompetingReuse()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"custsearch-refresh-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={databasePath};Default Timeout=30";
        try
        {
            string rawRefreshToken;
            await using (var setupContext = CreateContext(connectionString))
            {
                await setupContext.Database.EnsureCreatedAsync();
                var (_, setupService) = await SeedAndCreateServiceAsync(setupContext);
                rawRefreshToken = (await setupService.LoginAsync(AuthFixture.CreateLoginCommand())).RefreshToken;
            }

            await using var firstContext = CreateContext(connectionString);
            await using var secondContext = CreateContext(connectionString);
            var (_, firstService) = CreateService(firstContext);
            var (_, secondService) = CreateService(secondContext);
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var first = AttemptRefreshAsync(firstService, rawRefreshToken, "concurrent-1", start.Task);
            var second = AttemptRefreshAsync(secondService, rawRefreshToken, "concurrent-2", start.Task);

            start.SetResult();
            var outcomes = await Task.WhenAll(first, second);

            Assert.Single(outcomes, outcome => outcome.Result is not null);
            Assert.Single(outcomes, outcome => outcome.Failure == AuthenticationFailure.ReusedRefreshToken);
            Assert.DoesNotContain(outcomes, outcome => outcome.UnexpectedException is not null);
            await firstContext.DisposeAsync();
            await secondContext.DisposeAsync();
            await using var verificationContext = CreateContext(connectionString);
            Assert.Equal(2, await verificationContext.RefreshTokens.CountAsync());
            Assert.Equal(0, await verificationContext.RefreshTokens.CountAsync(token => token.RevokedUtc == null));
            await verificationContext.DisposeAsync();
        }
        finally
        {
            DeleteSqliteFiles(databasePath);
        }
    }

    [Fact]
    public async Task RefreshRotatesTokenAndReuseRevokesReplacementFamily()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        var login = await fixture.Service.LoginAsync(AuthFixture.CreateLoginCommand());

        var refreshed = await fixture.Service.RefreshAsync(login.RefreshToken, "127.0.0.1", "refresh-1");
        Assert.NotEqual(login.RefreshToken, refreshed.RefreshToken);

        var reuse = await Assert.ThrowsAsync<AuthenticationFailureException>(() =>
            fixture.Service.RefreshAsync(login.RefreshToken, "127.0.0.1", "reuse-1"));
        Assert.Equal(AuthenticationFailure.ReusedRefreshToken, reuse.Failure);

        var replacementHash = RefreshTokenFactory.Hash(refreshed.RefreshToken);
        var replacement = await fixture.Context.RefreshTokens.SingleAsync(token => token.TokenHash == replacementHash);
        Assert.Equal("ReuseDetected", replacement.RevokedReason);
    }

    [Fact]
    public async Task ExpiredRefreshTokenIsRejectedAndRevoked()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        var login = await fixture.Service.LoginAsync(AuthFixture.CreateLoginCommand());
        fixture.Time.Advance(TimeSpan.FromDays(8));

        var exception = await Assert.ThrowsAsync<AuthenticationFailureException>(() =>
            fixture.Service.RefreshAsync(login.RefreshToken, null, "expired-1"));

        Assert.Equal(AuthenticationFailure.ExpiredRefreshToken, exception.Failure);
        var hash = RefreshTokenFactory.Hash(login.RefreshToken);
        Assert.Equal("Expired", (await fixture.Context.RefreshTokens.SingleAsync(token => token.TokenHash == hash)).RevokedReason);
    }

    [Fact]
    public async Task LogoutRevokesRefreshToken()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        var login = await fixture.Service.LoginAsync(AuthFixture.CreateLoginCommand());

        await fixture.Service.LogoutAsync(login.RefreshToken, "127.0.0.1", "logout-1");

        var hash = RefreshTokenFactory.Hash(login.RefreshToken);
        var token = await fixture.Context.RefreshTokens.SingleAsync(candidate => candidate.TokenHash == hash);
        Assert.Equal("Logout", token.RevokedReason);
        var exception = await Assert.ThrowsAsync<AuthenticationFailureException>(() =>
            fixture.Service.RefreshAsync(login.RefreshToken, null, "after-logout"));
        Assert.Equal(AuthenticationFailure.ReusedRefreshToken, exception.Failure);
    }

    [Fact]
    public async Task RefreshAfterSecurityStampChangeRevokesEveryUserSession()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        var firstSession = await fixture.Service.LoginAsync(AuthFixture.CreateLoginCommand());
        _ = await fixture.Service.LoginAsync(AuthFixture.CreateLoginCommand());
        var user = await fixture.Context.UserAccounts.SingleAsync();
        user.SetPasswordHash(new PasswordHasher<UserAccount>().HashPassword(user, "changed-password"));
        await fixture.Context.SaveChangesAsync();

        var failure = await Assert.ThrowsAsync<AuthenticationFailureException>(() =>
            fixture.Service.RefreshAsync(firstSession.RefreshToken, "127.0.0.1", "stamp-refresh"));

        Assert.Equal(AuthenticationFailure.SessionRevoked, failure.Failure);
        Assert.Equal(0, await fixture.Context.RefreshTokens.CountAsync(token => token.RevokedUtc == null));
        Assert.Contains(
            await fixture.Context.AuthenticationEvents.ToListAsync(),
            audit => audit.EventType == "RefreshFailed" && audit.FailureCode == "SessionRevoked");
    }

    [Fact]
    public async Task ChangePasswordRejectsWrongCurrentPasswordAndAuditsFailure()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        var userId = (await fixture.Context.UserAccounts.SingleAsync()).Id;

        var exception = await Assert.ThrowsAsync<PasswordChangeException>(() =>
            fixture.Service.ChangePasswordAsync(new(
                userId,
                "wrong-password",
                "NewPassword123",
                "127.0.0.1",
                "password-change-failed")));

        Assert.Equal("InvalidCurrentPassword", exception.Code);
        Assert.Contains(
            await fixture.Context.AuthenticationEvents.ToListAsync(),
            audit => audit.EventType == "PasswordChangeFailed"
                && audit.FailureCode == "InvalidCurrentPassword"
                && !audit.IsSuccess);
    }

    [Fact]
    public async Task ChangePasswordRehashesCredentialRevokesSessionsAndRequiresNewPassword()
    {
        await using var fixture = await AuthFixture.CreateAsync();
        var login = await fixture.Service.LoginAsync(AuthFixture.CreateLoginCommand());
        var user = await fixture.Context.UserAccounts.SingleAsync();

        await fixture.Service.ChangePasswordAsync(new(
            user.Id,
            "correct-password",
            "NewPassword123",
            "127.0.0.1",
            "password-change-success"));

        Assert.Equal(0, await fixture.Context.RefreshTokens.CountAsync(token => token.RevokedUtc == null));
        Assert.All(
            await fixture.Context.RefreshTokens.ToListAsync(),
            token => Assert.Equal("PasswordChanged", token.RevokedReason));
        Assert.Contains(
            await fixture.Context.AuthenticationEvents.ToListAsync(),
            audit => audit.EventType == "PasswordChanged" && audit.IsSuccess);
        var refreshFailure = await Assert.ThrowsAsync<AuthenticationFailureException>(() =>
            fixture.Service.RefreshAsync(login.RefreshToken, null, "old-session"));
        Assert.Equal(AuthenticationFailure.ReusedRefreshToken, refreshFailure.Failure);

        await Assert.ThrowsAsync<AuthenticationFailureException>(() =>
            fixture.Service.LoginAsync(AuthFixture.CreateLoginCommand()));
        var newLogin = await fixture.Service.LoginAsync(
            AuthFixture.CreateLoginCommand() with { Password = "NewPassword123" });
        Assert.False(string.IsNullOrWhiteSpace(newLogin.AccessToken));
    }

    private sealed class AuthFixture : IAsyncDisposable
    {
        private AuthFixture(
            SqliteConnection connection,
            CustSearchDbContext context,
            AuthenticationService service,
            MutableTimeProvider time)
        {
            Connection = connection;
            Context = context;
            Service = service;
            Time = time;
        }

        private SqliteConnection Connection { get; }
        public CustSearchDbContext Context { get; }
        public AuthenticationService Service { get; }
        public MutableTimeProvider Time { get; }

        public static LoginCommand CreateLoginCommand() => new(
            "SHOP-ONE", "owner", "correct-password", "127.0.0.1", "login-1");

        public static async Task<AuthFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var context = new CustSearchDbContext(
                new DbContextOptionsBuilder<CustSearchDbContext>().UseSqlite(connection).Options);
            await context.Database.EnsureCreatedAsync();
            var now = TestNow;
            var tenant = Tenant.Create("SHOP-ONE", "Shop One Pvt Ltd", "Shop One", "Asia/Kolkata", now);
            context.Tenants.Add(tenant);
            await context.SaveChangesAsync();
            var hasher = new PasswordHasher<UserAccount>();
            var user = UserAccount.CreateTenant(tenant.Id, "owner", "owner@example.test", "Shop Owner", "temporary", now);
            user.SetPasswordHash(hasher.HashPassword(user, "correct-password"));
            context.UserAccounts.Add(user);
            await context.SaveChangesAsync();
            var jwtOptions = Options.Create(new JwtOptions
            {
                Issuer = "CustSearch.Tests",
                Audience = "CustSearch.TestClient",
                SigningKey = "test-only-signing-key-with-at-least-thirty-two-bytes",
                AccessTokenLifetimeMinutes = 5,
                RefreshTokenLifetimeDays = 7,
                ClockSkewSeconds = 0,
            });
            var time = new MutableTimeProvider(now);
            var service = new AuthenticationService(
                context,
                hasher,
                new JwtTokenService(jwtOptions),
                jwtOptions,
                time);
            return new AuthFixture(connection, context, service, time);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    private static CustSearchDbContext CreateContext(string connectionString) => new(
        new DbContextOptionsBuilder<CustSearchDbContext>().UseSqlite(connectionString).Options);

    private static async Task<(MutableTimeProvider Time, AuthenticationService Service)> SeedAndCreateServiceAsync(
        CustSearchDbContext context)
    {
        var tenant = Tenant.Create("SHOP-ONE", "Shop One Pvt Ltd", "Shop One", "Asia/Kolkata", TestNow);
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();
        var hasher = new PasswordHasher<UserAccount>();
        var user = UserAccount.CreateTenant(
            tenant.Id, "owner", "owner@example.test", "Shop Owner", "temporary", TestNow);
        user.SetPasswordHash(hasher.HashPassword(user, "correct-password"));
        context.UserAccounts.Add(user);
        await context.SaveChangesAsync();
        return CreateService(context, hasher);
    }

    private static (MutableTimeProvider Time, AuthenticationService Service) CreateService(
        CustSearchDbContext context,
        IPasswordHasher<UserAccount>? passwordHasher = null)
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "CustSearch.Tests",
            Audience = "CustSearch.TestClient",
            SigningKey = "test-only-signing-key-with-at-least-thirty-two-bytes",
            AccessTokenLifetimeMinutes = 5,
            RefreshTokenLifetimeDays = 7,
            ClockSkewSeconds = 0,
        });
        var time = new MutableTimeProvider(TestNow);
        var service = new AuthenticationService(
            context,
            passwordHasher ?? new PasswordHasher<UserAccount>(),
            new JwtTokenService(jwtOptions),
            jwtOptions,
            time);
        return (time, service);
    }

    private static async Task<RefreshOutcome> AttemptRefreshAsync(
        AuthenticationService service,
        string refreshToken,
        string correlationId,
        Task start)
    {
        await start;
        try
        {
            return new RefreshOutcome(
                await service.RefreshAsync(refreshToken, "127.0.0.1", correlationId),
                null,
                null);
        }
        catch (AuthenticationFailureException exception)
        {
            return new RefreshOutcome(null, exception.Failure, null);
        }
        catch (Exception exception)
        {
            return new RefreshOutcome(null, null, exception);
        }
    }

    private static void DeleteSqliteFiles(string databasePath)
    {
        SqliteConnection.ClearAllPools();
        foreach (var path in new[] { databasePath, $"{databasePath}-shm", $"{databasePath}-wal" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private sealed record RefreshOutcome(
        AuthenticationResult? Result,
        AuthenticationFailure? Failure,
        Exception? UnexpectedException);

    private sealed class MutableTimeProvider(DateTime utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = new(utcNow);
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan duration) => _utcNow += duration;
    }
}
