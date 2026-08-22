using CustSearch.Domain.Entities;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CustSearch.IntegrationTests;

public sealed class CustSearchDbContextModelTests
{
    [Fact]
    public void DatabaseVersionMatchesVersionedSqlTable()
    {
        var options = new DbContextOptionsBuilder<CustSearchDbContext>()
            .UseSqlServer("Server=(local);Database=CustSearch_AI;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        using var context = new CustSearchDbContext(options);

        var entity = context.Model.FindEntityType(typeof(DatabaseVersion));

        Assert.NotNull(entity);
        Assert.Equal("DatabaseVersions", entity.GetTableName());
        Assert.Equal("dbo", entity.GetSchema());
        Assert.Equal(nameof(DatabaseVersion.VersionId), entity.FindPrimaryKey()!.Properties.Single().Name);
        Assert.True(entity.GetIndexes().Single(index =>
            index.Properties.Single().Name == nameof(DatabaseVersion.VersionNumber)).IsUnique);
    }

    [Fact]
    public void RefreshTokenMatchesPhaseTwoSecuritySchema()
    {
        using var context = CreateContext();
        var entity = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType>(
            context.Model.FindEntityType(typeof(RefreshToken)));

        Assert.Equal("RefreshTokens", entity.GetTableName());
        Assert.Equal("dbo", entity.GetSchema());
        Assert.Equal(64, entity.FindProperty(nameof(RefreshToken.TokenHash))!.GetMaxLength());
        Assert.Equal(64, entity.FindProperty(nameof(RefreshToken.IssuedSecurityStamp))!.GetMaxLength());
        Assert.False(entity.FindProperty(nameof(RefreshToken.IssuedSecurityStamp))!.IsNullable);
        Assert.Equal(100, entity.FindProperty(nameof(RefreshToken.RevokedReason))!.GetMaxLength());
        Assert.True(FindIndex(entity, nameof(RefreshToken.TokenHash)).IsUnique);
        Assert.False(FindIndex(entity, nameof(RefreshToken.UserId), nameof(RefreshToken.FamilyId)).IsUnique);
        Assert.False(FindIndex(entity, nameof(RefreshToken.ExpiresUtc)).IsUnique);
    }

    [Fact]
    public void AuthenticationEventMatchesAuditLookupSchema()
    {
        using var context = CreateContext();
        var entity = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType>(
            context.Model.FindEntityType(typeof(AuthenticationEvent)));

        Assert.Equal("AuthenticationEvents", entity.GetTableName());
        Assert.Equal("dbo", entity.GetSchema());
        Assert.Equal(60, entity.FindProperty(nameof(AuthenticationEvent.EventType))!.GetMaxLength());
        Assert.Equal(64, entity.FindProperty(nameof(AuthenticationEvent.CorrelationId))!.GetMaxLength());
        Assert.NotNull(FindIndex(entity, nameof(AuthenticationEvent.TenantId), nameof(AuthenticationEvent.OccurredUtc)));
        Assert.NotNull(FindIndex(entity, nameof(AuthenticationEvent.UserId), nameof(AuthenticationEvent.OccurredUtc)));
    }

    [Fact]
    public void UserScopeTenantConstraintAndIdentityIndexesAreModeled()
    {
        using var context = CreateContext();
        var designTimeModel = context.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;
        var entity = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType>(
            designTimeModel.FindEntityType(typeof(UserAccount)));

        var constraint = Assert.Single(entity.GetCheckConstraints(), item => item.Name == "CK_Users_ScopeTenant");
        Assert.Contains("[Scope] = 1", constraint.Sql, StringComparison.Ordinal);
        Assert.Contains("[TenantId] IS NOT NULL", constraint.Sql, StringComparison.Ordinal);
        Assert.True(FindIndex(entity, nameof(UserAccount.TenantId), nameof(UserAccount.NormalizedUserName)).IsUnique);
        Assert.True(FindIndex(entity, nameof(UserAccount.TenantId), nameof(UserAccount.NormalizedEmail)).IsUnique);
        Assert.Equal(64, entity.FindProperty(nameof(UserAccount.SecurityStamp))!.GetMaxLength());
    }

    [Fact]
    public void AuthorizationEntitiesMatchPhaseThreeSqlSchema()
    {
        using var context = CreateContext();
        var designTimeModel = context.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;
        var role = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType>(
            designTimeModel.FindEntityType(typeof(Role)));
        var permission = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType>(
            designTimeModel.FindEntityType(typeof(Permission)));
        var userRole = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType>(
            designTimeModel.FindEntityType(typeof(UserRole)));
        var rolePermission = Assert.IsAssignableFrom<Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType>(
            designTimeModel.FindEntityType(typeof(RolePermission)));

        Assert.Equal("Roles", role.GetTableName());
        Assert.Contains(role.GetCheckConstraints(), item => item.Name == "CK_Roles_ScopeTenant");
        Assert.True(FindIndex(role, nameof(Role.TenantId), nameof(Role.NormalizedName)).IsUnique);
        Assert.Equal(100, role.FindProperty(nameof(Role.NormalizedName))!.GetMaxLength());

        Assert.Equal("Permissions", permission.GetTableName());
        Assert.True(FindIndex(permission, nameof(Permission.Name)).IsUnique);
        Assert.Equal(150, permission.FindProperty(nameof(Permission.Name))!.GetMaxLength());

        Assert.Equal(new[] { nameof(UserRole.UserId), nameof(UserRole.RoleId) },
            userRole.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal(new[] { nameof(RolePermission.RoleId), nameof(RolePermission.PermissionId) },
            rolePermission.FindPrimaryKey()!.Properties.Select(property => property.Name));
    }

    private static CustSearchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CustSearchDbContext>()
            .UseSqlServer("Server=(local);Database=CustSearch_AI;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        return new CustSearchDbContext(options);
    }

    private static Microsoft.EntityFrameworkCore.Metadata.IReadOnlyIndex FindIndex(
        Microsoft.EntityFrameworkCore.Metadata.IReadOnlyEntityType entity,
        params string[] propertyNames) => entity.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
}
