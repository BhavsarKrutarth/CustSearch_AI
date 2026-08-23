using System.Reflection;
using CustSearch.API.Controllers;
using CustSearch.API.Security;
using CustSearch.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.IntegrationTests;

public sealed class PhaseNineApiContractTests
{
    [Fact]
    public void PhaseNineRequestDtosNeverExposeTenantId()
    {
        var types = new[]
        {
            typeof(SavePlatformPlanRequest),
            typeof(CreatePlatformSubscriptionRequest),
            typeof(ChangePlatformPlanRequest),
            typeof(GeneratePlatformInvoiceRequest),
            typeof(RecordPlatformPaymentRequest),
        };

        foreach (var type in types)
            Assert.DoesNotContain(type.GetProperties(), property =>
                string.Equals(property.Name, "TenantId", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(nameof(PlatformBillingController.Plans), PermissionCatalog.PlatformBilling.PlansView)]
    [InlineData(nameof(PlatformBillingController.CreatePlan), PermissionCatalog.PlatformBilling.PlansManage)]
    [InlineData(nameof(PlatformBillingController.UpdatePlan), PermissionCatalog.PlatformBilling.PlansManage)]
    [InlineData(nameof(PlatformBillingController.Subscriptions), PermissionCatalog.PlatformBilling.SubscriptionsView)]
    [InlineData(nameof(PlatformBillingController.CreateSubscription), PermissionCatalog.PlatformBilling.SubscriptionsManage)]
    [InlineData(nameof(PlatformBillingController.Renew), PermissionCatalog.PlatformBilling.SubscriptionsManage)]
    [InlineData(nameof(PlatformBillingController.ChangePlan), PermissionCatalog.PlatformBilling.SubscriptionsManage)]
    [InlineData(nameof(PlatformBillingController.Cancel), PermissionCatalog.PlatformBilling.SubscriptionsManage)]
    [InlineData(nameof(PlatformBillingController.Invoices), PermissionCatalog.PlatformBilling.InvoicesView)]
    [InlineData(nameof(PlatformBillingController.GenerateInvoice), PermissionCatalog.PlatformBilling.SubscriptionsManage)]
    [InlineData(nameof(PlatformBillingController.Payments), PermissionCatalog.PlatformBilling.PaymentsView)]
    [InlineData(nameof(PlatformBillingController.RecordPayment), PermissionCatalog.PlatformBilling.SubscriptionsManage)]
    public void PlatformAdminEndpointsRequireExactPhaseNinePermission(string methodName, string permission)
    {
        var method = typeof(PlatformBillingController).GetMethod(methodName)!;
        var permissionAttribute = method.GetCustomAttribute<HasPermissionAttribute>();

        Assert.NotNull(permissionAttribute);
        Assert.Equal(AuthorizationPolicyNames.ForPermission(permission), permissionAttribute.Policy);
    }

    [Fact]
    public void TenantPlatformBillingControllerIsReadOnly()
    {
        var methods = typeof(TenantPlatformBillingController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        Assert.NotEmpty(methods);
        foreach (var method in methods)
        {
            Assert.Null(method.GetCustomAttribute<HttpPostAttribute>());
            Assert.Null(method.GetCustomAttribute<HttpPutAttribute>());
            Assert.Null(method.GetCustomAttribute<HttpDeleteAttribute>());
        }
    }

    [Theory]
    [InlineData(nameof(TenantPlatformBillingController.Summary), PermissionCatalog.PlatformBilling.SubscriptionsView)]
    [InlineData(nameof(TenantPlatformBillingController.Subscription), PermissionCatalog.PlatformBilling.SubscriptionsView)]
    [InlineData(nameof(TenantPlatformBillingController.Invoices), PermissionCatalog.PlatformBilling.InvoicesView)]
    [InlineData(nameof(TenantPlatformBillingController.Payments), PermissionCatalog.PlatformBilling.PaymentsView)]
    public void TenantBillingReadsRequirePhaseNineViewPermission(string methodName, string permission)
    {
        var method = typeof(TenantPlatformBillingController).GetMethod(methodName)!;
        var permissionAttribute = method.GetCustomAttribute<HasPermissionAttribute>();

        Assert.NotNull(permissionAttribute);
        Assert.Equal(AuthorizationPolicyNames.ForPermission(permission), permissionAttribute.Policy);
    }

    [Fact]
    public void PlatformAndRetailControllersRemainDifferentApiDomains()
    {
        var platformRoute = typeof(PlatformBillingController).GetCustomAttribute<RouteAttribute>()!.Template;
        var retailRoute = typeof(RetailBillingController).GetCustomAttribute<RouteAttribute>()!.Template;

        Assert.Equal("api/platform/billing", platformRoute);
        Assert.Equal("api/tenant", retailRoute);
        Assert.NotEqual(platformRoute, retailRoute);
    }

    [Fact]
    public void PlatformBillingRequestsDoNotContainRetailCustomerStoreOrProductFields()
    {
        var protectedTypes = new[]
        {
            typeof(SavePlatformPlanRequest),
            typeof(CreatePlatformSubscriptionRequest),
            typeof(ChangePlatformPlanRequest),
            typeof(GeneratePlatformInvoiceRequest),
            typeof(RecordPlatformPaymentRequest),
        };
        var forbidden = new[] { "CustomerId", "HouseholdId", "VisitPartyId", "ProductId", "StoreId", "RetailInvoiceId" };

        foreach (var type in protectedTypes)
        {
            var names = type.GetProperties().Select(x => x.Name).ToArray();
            foreach (var name in forbidden)
                Assert.DoesNotContain(name, names, StringComparer.OrdinalIgnoreCase);
        }
    }
}
