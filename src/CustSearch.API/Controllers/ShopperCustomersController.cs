using System.ComponentModel.DataAnnotations;
using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.ShopperCustomers;
using CustSearch.Application.TenantOperations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;

/// <summary>
/// Phase 6 tenant-scoped shopper customer and anonymous visitor APIs. TenantId is never accepted from request payloads;
/// tenant/store authorization comes from the validated session and is rechecked in the service.
/// </summary>
[ApiController]
[Route("api/tenant")]
[Authorize(Policy = AuthorizationPolicyNames.TenantScope)]
public sealed class ShopperCustomersController(IShopperCustomerService service, ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet("customers")]
    [HasPermission(PermissionCatalog.Operations.CustomersView)]
    public Task<PagedResult<CustomerListItem>> SearchCustomers([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null, [FromQuery] long? storeId = null, [FromQuery] bool activeOnly = false,
        CancellationToken ct = default) =>
        service.SearchCustomersAsync(new(pageNumber, pageSize, search, storeId, activeOnly), ct);

    [HttpGet("customers/{customerId:long}")]
    [HasPermission(PermissionCatalog.Operations.CustomersView)]
    public Task<CustomerDetail> GetCustomer(long customerId, CancellationToken ct) => service.GetCustomerAsync(customerId, ct);

    [HttpGet("customers/{customerId:long}/smart-profile")]
    [HasPermission(PermissionCatalog.Operations.CustomersView)]
    public Task<CustomerSmartProfile> GetSmartProfile(long customerId, CancellationToken ct) => service.GetSmartProfileAsync(customerId, ct);

    [HttpPost("customers")]
    [HasPermission(PermissionCatalog.Operations.CustomersCreate)]
    public Task<CustomerDetail> CreateCustomer(CreateCustomerRequest request, CancellationToken ct) =>
        service.CreateCustomerAsync(request.ToCommand(), Audit(), ct);

    [HttpPut("customers/{customerId:long}")]
    [HasPermission(PermissionCatalog.Operations.CustomersEdit)]
    public Task<CustomerDetail> UpdateCustomer(long customerId, UpdateCustomerRequest request, CancellationToken ct) =>
        service.UpdateCustomerAsync(customerId, request.ToCommand(), Audit(), ct);

    [HttpPut("customers/{customerId:long}/stores")]
    [HasPermission(PermissionCatalog.Operations.CustomersEdit)]
    public Task<CustomerDetail> SetCustomerStores(long customerId, SetCustomerStoresRequest request, CancellationToken ct) =>
        service.SetCustomerStoresAsync(customerId, new(request.StoreIds ?? [], request.PrimaryStoreId), Audit(), ct);

    [HttpGet("visitors")]
    [HasPermission(PermissionCatalog.Operations.VisitorsView)]
    public Task<PagedResult<AnonymousVisitorListItem>> SearchVisitors([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null, [FromQuery] long? storeId = null, [FromQuery] bool activeOnly = false,
        CancellationToken ct = default) =>
        service.SearchVisitorsAsync(new(pageNumber, pageSize, search, storeId, activeOnly), ct);

    [HttpGet("visitors/{visitorId:long}")]
    [HasPermission(PermissionCatalog.Operations.VisitorsView)]
    public Task<AnonymousVisitorDetail> GetVisitor(long visitorId, CancellationToken ct) => service.GetVisitorAsync(visitorId, ct);

    // Manual/demo registration is intentionally restricted to the stronger conversion permission. Normal CCTV creation
    // will later call the service from a trusted backend workflow rather than granting browser write access via Visitors.View.
    [HttpPost("visitors")]
    [HasPermission(PermissionCatalog.Operations.VisitorsConvert)]
    public Task<AnonymousVisitorDetail> CreateVisitor(CreateAnonymousVisitorRequest request, CancellationToken ct) =>
        service.CreateVisitorAsync(new(request.StoreId, request.VisitorCode, request.SeenUtc), Audit(), ct);

    [HttpPost("visitors/{visitorId:long}/convert")]
    [HasPermission(PermissionCatalog.Operations.VisitorsConvert)]
    public Task<CustomerDetail> ConvertVisitor(long visitorId, ConvertAnonymousVisitorRequest request, CancellationToken ct) =>
        service.ConvertVisitorAsync(visitorId, request.ToCommand(), Audit(), ct);

    private TenantAuditContext Audit() => new(currentUser.UserId, HttpContext.Connection.RemoteIpAddress?.ToString(),
        Request.Headers.UserAgent.ToString(), HttpContext.TraceIdentifier);
}

public sealed record CreateCustomerRequest(
    [param: StringLength(50)] string? CustomerCode,
    [param: Required, StringLength(100)] string FirstName,
    [param: StringLength(100)] string? LastName,
    [param: StringLength(30)] string? Mobile,
    [param: EmailAddress, StringLength(254)] string? Email,
    [param: StringLength(1000)] string? Notes,
    IReadOnlyList<long>? StoreIds,
    long? PrimaryStoreId)
{
    public CreateCustomerCommand ToCommand() => new(CustomerCode, FirstName, LastName, Mobile, Email, Notes, StoreIds ?? [], PrimaryStoreId);
}

public sealed record UpdateCustomerRequest(
    [param: Required, StringLength(100)] string FirstName,
    [param: StringLength(100)] string? LastName,
    [param: StringLength(30)] string? Mobile,
    [param: EmailAddress, StringLength(254)] string? Email,
    [param: StringLength(1000)] string? Notes,
    bool IsActive)
{
    public UpdateCustomerCommand ToCommand() => new(FirstName, LastName, Mobile, Email, Notes, IsActive);
}

public sealed record SetCustomerStoresRequest(IReadOnlyList<long>? StoreIds, long? PrimaryStoreId);

public sealed record CreateAnonymousVisitorRequest(long StoreId, [param: StringLength(50)] string? VisitorCode, DateTime? SeenUtc);

public sealed record ConvertAnonymousVisitorRequest(
    long? CustomerId,
    [param: StringLength(100)] string? FirstName,
    [param: StringLength(100)] string? LastName,
    [param: StringLength(30)] string? Mobile,
    [param: EmailAddress, StringLength(254)] string? Email,
    [param: StringLength(1000)] string? Notes)
{
    public ConvertAnonymousVisitorCommand ToCommand() => new(CustomerId, FirstName, LastName, Mobile, Email, Notes);
}
