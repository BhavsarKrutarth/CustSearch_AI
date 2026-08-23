using CustSearch.Domain.Entities;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace CustSearch.IntegrationTests;

public sealed class PhaseSixDataModelTests
{
    [Fact]
    public void PhaseSixEntitiesMapToExpectedTablesAndKeys()
    {
        using var context = CreateContext();
        var model = context.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;

        Assert.Equal("Customers", model.FindEntityType(typeof(Customer))!.GetTableName());
        Assert.Equal("CustomerStoreAssignments", model.FindEntityType(typeof(CustomerStoreAssignment))!.GetTableName());
        Assert.Equal("AnonymousVisitors", model.FindEntityType(typeof(AnonymousVisitor))!.GetTableName());

        var assignment = model.FindEntityType(typeof(CustomerStoreAssignment))!;
        Assert.Equal(new[] { nameof(CustomerStoreAssignment.CustomerId), nameof(CustomerStoreAssignment.StoreId) },
            assignment.FindPrimaryKey()!.Properties.Select(x => x.Name));
    }

    [Fact]
    public void CustomerBusinessKeyIsUniqueInsideTenant()
    {
        using var context = CreateContext();
        var model = context.GetService<Microsoft.EntityFrameworkCore.Metadata.IDesignTimeModel>().Model;
        var customer = model.FindEntityType(typeof(Customer))!;
        var index = customer.GetIndexes().Single(x => x.Properties.Select(p => p.Name)
            .SequenceEqual(new[] { nameof(Customer.TenantId), nameof(Customer.CustomerCode) }));
        Assert.True(index.IsUnique);
    }

    [Fact]
    public void PhaseSixDbSetsArePartOfContextModel()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(Customer)));
        Assert.NotNull(context.Model.FindEntityType(typeof(CustomerStoreAssignment)));
        Assert.NotNull(context.Model.FindEntityType(typeof(AnonymousVisitor)));
    }

    private static CustSearchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CustSearchDbContext>()
            .UseSqlServer("Server=(local);Database=CustSearch_AI;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        return new CustSearchDbContext(options);
    }
}
