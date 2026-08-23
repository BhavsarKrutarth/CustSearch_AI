using System.Reflection;
using CustSearch.API.Controllers;
using CustSearch.API.Security;
using CustSearch.Application.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.IntegrationTests;

public sealed class PhaseEightApiContractTests
{
    [Fact]
    public void PhaseEightBrowserWriteRequestsNeverExposeTenantId()
    {
        var types=new[]{typeof(CreateProductRequest),typeof(UpdateProductRequest),typeof(SetProductStoresRequest),typeof(CreateRetailInvoiceRequest),typeof(UpdateRetailInvoiceRequest),typeof(CancelRetailInvoiceRequest),typeof(AddRetailPaymentRequest),typeof(SaveRetailParticipantRequest),typeof(SaveRetailAttributionRequest)};
        foreach(var type in types)
            Assert.DoesNotContain(type.GetProperties(),p=>string.Equals(p.Name,"TenantId",StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(nameof(RetailBillingController.CreateProduct),PermissionCatalog.Operations.ProductsCreate)]
    [InlineData(nameof(RetailBillingController.UpdateProduct),PermissionCatalog.Operations.ProductsEdit)]
    [InlineData(nameof(RetailBillingController.SetProductStores),PermissionCatalog.Operations.ProductsManageStores)]
    [InlineData(nameof(RetailBillingController.CreateInvoice),PermissionCatalog.Operations.RetailInvoicesCreate)]
    [InlineData(nameof(RetailBillingController.UpdateInvoice),PermissionCatalog.Operations.RetailInvoicesEdit)]
    [InlineData(nameof(RetailBillingController.FinalizeInvoice),PermissionCatalog.Operations.RetailInvoicesFinalize)]
    [InlineData(nameof(RetailBillingController.CancelInvoice),PermissionCatalog.Operations.RetailInvoicesCancel)]
    [InlineData(nameof(RetailBillingController.AddPayment),PermissionCatalog.Operations.RetailPaymentsCreate)]
    [InlineData(nameof(RetailBillingController.SaveAttribution),PermissionCatalog.Operations.RetailSpendAttributionManage)]
    public void PhaseEightWritesRequireExactPermission(string methodName,string permission)
    {
        var method=typeof(RetailBillingController).GetMethod(methodName)!;
        var attr=method.GetCustomAttribute<HasPermissionAttribute>();
        Assert.NotNull(attr);
        Assert.Equal(AuthorizationPolicyNames.ForPermission(permission),attr.Policy);
    }

    [Fact]
    public void InvoiceRequestKeepsVisitPartySeparateFromHousehold()
    {
        var names=typeof(CreateRetailInvoiceRequest).GetProperties().Select(x=>x.Name).ToArray();
        Assert.Contains("HouseholdId",names);
        Assert.Contains("VisitPartyId",names);
        Assert.NotEqual(Array.IndexOf(names,"HouseholdId"),Array.IndexOf(names,"VisitPartyId"));
    }

    [Fact]
    public void AttributionRequestRequiresExplicitKnownCustomerAndItem()
    {
        var names=typeof(SaveRetailAttributionRequest).GetProperties().Select(x=>x.Name).ToArray();
        Assert.Contains("InvoiceItemId",names);
        Assert.Contains("CustomerId",names);
        Assert.DoesNotContain(names,x=>x.Contains("Face",StringComparison.OrdinalIgnoreCase)||x.Contains("AnonymousVisitor",StringComparison.OrdinalIgnoreCase));
    }
}
