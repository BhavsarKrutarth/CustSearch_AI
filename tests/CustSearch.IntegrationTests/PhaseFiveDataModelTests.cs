using CustSearch.Domain.Entities;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CustSearch.IntegrationTests;

public sealed class PhaseFiveDataModelTests
{
    [Fact]
    public void PhaseFiveEntitiesMapToExpectedTablesAndKeys()
    {
        using var context = CreateContext();
        var model = context.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;

        Assert.Equal("Stores", model.FindEntityType(typeof(Store))!.GetTableName());
        Assert.Equal("UserStoreAssignments", model.FindEntityType(typeof(UserStoreAssignment))!.GetTableName());
        Assert.Equal("StaffProfiles", model.FindEntityType(typeof(StaffProfile))!.GetTableName());
        Assert.Equal("StaffShifts", model.FindEntityType(typeof(StaffShift))!.GetTableName());
        Assert.Equal("StaffPresenceSessions", model.FindEntityType(typeof(StaffPresenceSession))!.GetTableName());
        Assert.Equal("ProductCategories", model.FindEntityType(typeof(ProductCategory))!.GetTableName());
        Assert.Equal("StoreVoiceCommandSettings", model.FindEntityType(typeof(StoreVoiceCommandSetting))!.GetTableName());
        Assert.Equal("StoreVoiceCommandAliases", model.FindEntityType(typeof(StoreVoiceCommandAlias))!.GetTableName());

        var assignment = model.FindEntityType(typeof(UserStoreAssignment))!;
        Assert.Equal(new[] { nameof(UserStoreAssignment.UserId), nameof(UserStoreAssignment.StoreId) }, assignment.FindPrimaryKey()!.Properties.Select(x => x.Name));

        var voice = model.FindEntityType(typeof(StoreVoiceCommandSetting))!;
        Assert.Equal(nameof(StoreVoiceCommandSetting.StoreId), voice.FindPrimaryKey()!.Properties.Single().Name);
    }

    [Fact]
    public void StoreModelIncludesLocationSafetyConstraintsAndIndexes()
    {
        using var context = CreateContext();
        var model = context.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;
        var store = model.FindEntityType(typeof(Store))!;

        Assert.Contains(store.GetCheckConstraints(), x => x.Name == "CK_Stores_Latitude");
        Assert.Contains(store.GetCheckConstraints(), x => x.Name == "CK_Stores_Longitude");
        Assert.Contains(store.GetCheckConstraints(), x => x.Name == "CK_Stores_CoordinatesPair");
        Assert.Contains(store.GetCheckConstraints(), x => x.Name == "CK_Stores_GeoFence");
        Assert.True(store.GetIndexes().Single(x => x.Properties.Select(p => p.Name).SequenceEqual(new[] { nameof(Store.TenantId), nameof(Store.StoreCode) })).IsUnique);
    }

    [Fact]
    public void PhaseFiveDbSetsArePartOfContextModel()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(Store)));
        Assert.NotNull(context.Model.FindEntityType(typeof(UserStoreAssignment)));
        Assert.NotNull(context.Model.FindEntityType(typeof(StaffProfile)));
        Assert.NotNull(context.Model.FindEntityType(typeof(StaffShift)));
        Assert.NotNull(context.Model.FindEntityType(typeof(ProductCategory)));
        Assert.NotNull(context.Model.FindEntityType(typeof(StoreVoiceCommandSetting)));
    }

    private static CustSearchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CustSearchDbContext>()
            .UseSqlServer("Server=(local);Database=CustSearch_AI;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        return new CustSearchDbContext(options);
    }
}
