using CustSearch.Domain.Entities;
using CustSearch.Infrastructure.Persistence;
using CustSearch.Infrastructure.Tenancy;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.IntegrationTests;

public sealed class TenantUserRepositoryTests
{
    [Fact]
    public async Task GetByIdIncludesTenantInDatabasePredicate()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new CustSearchDbContext(
            new DbContextOptionsBuilder<CustSearchDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        var now = new DateTime(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
        var firstTenant = Tenant.Create("FIRST", "First Pvt Ltd", "First", "Asia/Kolkata", now);
        var secondTenant = Tenant.Create("SECOND", "Second Pvt Ltd", "Second", "Asia/Kolkata", now);
        context.Tenants.AddRange(firstTenant, secondTenant);
        await context.SaveChangesAsync();
        var secondUser = UserAccount.CreateTenant(
            secondTenant.Id,
            "owner",
            "owner@second.example",
            "Second Owner",
            "test-hash",
            now);
        context.UserAccounts.Add(secondUser);
        await context.SaveChangesAsync();
        var repository = new TenantUserRepository(context);

        Assert.Null(await repository.GetByIdAsync(firstTenant.Id, secondUser.Id));
        Assert.Equal(secondUser.Id, (await repository.GetByIdAsync(secondTenant.Id, secondUser.Id))?.Id);
    }
}
