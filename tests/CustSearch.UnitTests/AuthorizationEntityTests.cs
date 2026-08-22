using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;

namespace CustSearch.UnitTests;

/// <summary>
/// Verifies the domain rejects cross-tenant and cross-scope authorization assignments early.
/// </summary>
public sealed class AuthorizationEntityTests
{
    private static readonly DateTime CreatedUtc = new(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RoleFactoriesEnforcePlatformAndTenantOwnership()
    {
        var platformRole = Role.CreatePlatform("PlatformAuditor", "Read-only platform access.", true, CreatedUtc);
        var tenantRole = Role.CreateTenant(42, "Auditor", "Read-only tenant access.", true, CreatedUtc);

        Assert.Equal(UserScope.Platform, platformRole.Scope);
        Assert.Null(platformRole.TenantId);
        Assert.Equal(UserScope.Tenant, tenantRole.Scope);
        Assert.Equal(42, tenantRole.TenantId);
    }

    [Fact]
    public void UserRoleRejectsRoleOwnedByAnotherTenant()
    {
        var user = UserAccount.CreateTenant(10, "staff", "staff@example.test", "Staff", "hash", CreatedUtc);
        var role = Role.CreateTenant(11, "Manager", "Manages a tenant.", true, CreatedUtc);

        Assert.Throws<ArgumentException>(() => UserRole.Assign(user, role, CreatedUtc));
    }

    [Fact]
    public void RolePermissionRejectsPermissionFromAnotherScope()
    {
        var tenantRole = Role.CreateTenant(10, "Manager", "Manages a tenant.", true, CreatedUtc);
        var platformPermission = Permission.Create(
            UserScope.Platform,
            "Tenants.View",
            "Views platform tenants.",
            CreatedUtc);

        Assert.Throws<ArgumentException>(() => RolePermission.Grant(tenantRole, platformPermission));
    }
}
