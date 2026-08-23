using System.Reflection;
using CustSearch.API.Controllers;
using CustSearch.API.Security;
using CustSearch.Application.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.IntegrationTests;

/// <summary>Phase 6 API contract regressions keep tenant identity server-owned and write routes permission-protected.</summary>
public sealed class PhaseSixApiContractTests
{
    [Fact]
    public void AnonymousVisitorTouchRouteRequiresVisitorsConvertPermission()
    {
        var method = typeof(ShopperCustomersController).GetMethod(nameof(ShopperCustomersController.TouchVisitor))!;
        var route = method.GetCustomAttribute<HttpPostAttribute>();
        var permission = method.GetCustomAttribute<HasPermissionAttribute>();

        Assert.Equal("visitors/{visitorId:long}/touch", route!.Template);
        Assert.Equal(AuthorizationPolicyNames.ForPermission(PermissionCatalog.Operations.VisitorsConvert), permission!.Policy);
    }

    [Fact]
    public void PhaseSixBrowserRequestContractsNeverExposeTenantId()
    {
        var requestTypes = new[]
        {
            typeof(CreateCustomerRequest),
            typeof(UpdateCustomerRequest),
            typeof(SetCustomerStoresRequest),
            typeof(CreateAnonymousVisitorRequest),
            typeof(TouchAnonymousVisitorRequest),
            typeof(ConvertAnonymousVisitorRequest),
        };

        foreach (var type in requestTypes)
            Assert.DoesNotContain(type.GetProperties(), property => string.Equals(property.Name, "TenantId", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(nameof(ShopperCustomersController.CreateCustomer), PermissionCatalog.Operations.CustomersCreate)]
    [InlineData(nameof(ShopperCustomersController.UpdateCustomer), PermissionCatalog.Operations.CustomersEdit)]
    [InlineData(nameof(ShopperCustomersController.SetCustomerStores), PermissionCatalog.Operations.CustomersEdit)]
    [InlineData(nameof(ShopperCustomersController.CreateVisitor), PermissionCatalog.Operations.VisitorsConvert)]
    [InlineData(nameof(ShopperCustomersController.TouchVisitor), PermissionCatalog.Operations.VisitorsConvert)]
    [InlineData(nameof(ShopperCustomersController.ConvertVisitor), PermissionCatalog.Operations.VisitorsConvert)]
    public void PhaseSixWriteEndpointsRequireExpectedPermission(string methodName, string permissionName)
    {
        var method = typeof(ShopperCustomersController).GetMethod(methodName)!;
        var permission = method.GetCustomAttribute<HasPermissionAttribute>();

        Assert.NotNull(permission);
        Assert.Equal(AuthorizationPolicyNames.ForPermission(permissionName), permission.Policy);
    }
}
