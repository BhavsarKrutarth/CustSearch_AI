using System.ComponentModel.DataAnnotations;
using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.HouseholdsVisits;
using CustSearch.Application.ShopperCustomers;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustSearch.API.Controllers;

/// <summary>Phase 7 tenant-scoped household, factual visit and Visit Party/Co-Visit APIs. TenantId is never accepted from browser payloads.</summary>
[ApiController]
[Route("api/tenant")]
[Authorize(Policy=AuthorizationPolicyNames.TenantScope)]
public sealed class HouseholdsVisitsController(IHouseholdsVisitsService service,ICurrentUserContext currentUser):ControllerBase
{
    [HttpGet("households")]
    [HasPermission(PermissionCatalog.Operations.HouseholdsView)]
    public Task<PagedResult<HouseholdListItem>> SearchHouseholds([FromQuery]int pageNumber=1,[FromQuery]int pageSize=25,[FromQuery]string? search=null,[FromQuery]bool activeOnly=false,CancellationToken ct=default)=>
        service.SearchHouseholdsAsync(new(pageNumber,pageSize,search,activeOnly),ct);

    [HttpGet("households/{householdId:long}")]
    [HasPermission(PermissionCatalog.Operations.HouseholdsView)]
    public Task<HouseholdDetail> GetHousehold(long householdId,CancellationToken ct)=>service.GetHouseholdAsync(householdId,ct);

    [HttpPost("households")]
    [HasPermission(PermissionCatalog.Operations.HouseholdsCreate)]
    public Task<HouseholdDetail> CreateHousehold(CreateHouseholdRequest request,CancellationToken ct)=>service.CreateHouseholdAsync(new(request.HouseholdCode,request.Name,request.Notes),Audit(),ct);

    [HttpPut("households/{householdId:long}")]
    [HasPermission(PermissionCatalog.Operations.HouseholdsEdit)]
    public Task<HouseholdDetail> UpdateHousehold(long householdId,UpdateHouseholdRequest request,CancellationToken ct)=>service.UpdateHouseholdAsync(householdId,new(request.Name,request.Notes,request.IsActive),Audit(),ct);

    [HttpPost("households/{householdId:long}/members")]
    [HasPermission(PermissionCatalog.Operations.HouseholdsManageMembers)]
    public Task<HouseholdDetail> AddHouseholdMember(long householdId,SaveHouseholdMemberRequest request,CancellationToken ct)=>
        service.SaveHouseholdMemberAsync(householdId,new(request.CustomerId,request.RelationshipType,request.RelationshipSource,true),Audit(),ct);

    [HttpPut("households/{householdId:long}/members/{customerId:long}")]
    [HasPermission(PermissionCatalog.Operations.HouseholdsManageMembers)]
    public Task<HouseholdDetail> UpdateHouseholdMember(long householdId,long customerId,UpdateHouseholdMemberRequest request,CancellationToken ct)=>
        service.SaveHouseholdMemberAsync(householdId,new(customerId,request.RelationshipType,request.RelationshipSource,request.IsActive),Audit(),ct);

    [HttpDelete("households/{householdId:long}/members/{customerId:long}")]
    [HasPermission(PermissionCatalog.Operations.HouseholdsManageMembers)]
    public async Task<IActionResult> RemoveHouseholdMember(long householdId,long customerId,CancellationToken ct){await service.RemoveHouseholdMemberAsync(householdId,customerId,Audit(),ct);return NoContent();}

    [HttpGet("visits")]
    [HasPermission(PermissionCatalog.Operations.VisitsView)]
    public Task<PagedResult<CustomerVisitListItem>> SearchVisits([FromQuery]int pageNumber=1,[FromQuery]int pageSize=25,[FromQuery]string? search=null,[FromQuery]long? storeId=null,[FromQuery]long? customerId=null,[FromQuery]DateTime? fromUtc=null,[FromQuery]DateTime? toUtc=null,CancellationToken ct=default)=>
        service.SearchVisitsAsync(new(pageNumber,pageSize,search,storeId,customerId,fromUtc,toUtc),ct);

    [HttpGet("visits/{visitId:long}")]
    [HasPermission(PermissionCatalog.Operations.VisitsView)]
    public Task<CustomerVisitDetail> GetVisit(long visitId,CancellationToken ct)=>service.GetVisitAsync(visitId,ct);

    // Browser/manual visit creation is explicitly marked Manual. CCTV/system visit writers use the trusted application boundary rather than spoofable request fields.
    [HttpPost("visits")]
    [HasPermission(PermissionCatalog.Operations.VisitsEdit)]
    public Task<CustomerVisitDetail> CreateVisit(CreateCustomerVisitRequest request,CancellationToken ct)=>
        service.CreateVisitAsync(new(request.StoreId,request.CustomerId,request.VisitPartyId,request.EnteredUtc,CustomerVisitSource.Manual),Audit(),ct);

    [HttpPost("visits/{visitId:long}/complete")]
    [HasPermission(PermissionCatalog.Operations.VisitsEdit)]
    public Task<CustomerVisitDetail> CompleteVisit(long visitId,CompleteCustomerVisitRequest request,CancellationToken ct)=>service.CompleteVisitAsync(visitId,new(request.ExitedUtc),Audit(),ct);

    [HttpGet("visit-parties")]
    [HasPermission(PermissionCatalog.Operations.VisitPartiesView)]
    public Task<PagedResult<VisitPartyListItem>> SearchVisitParties([FromQuery]int pageNumber=1,[FromQuery]int pageSize=25,[FromQuery]string? search=null,[FromQuery]long? storeId=null,[FromQuery]VisitPartyStatus? status=null,[FromQuery]DateTime? fromUtc=null,[FromQuery]DateTime? toUtc=null,CancellationToken ct=default)=>
        service.SearchVisitPartiesAsync(new(pageNumber,pageSize,search,storeId,status,fromUtc,toUtc),ct);

    [HttpGet("visit-parties/{partyId:long}")]
    [HasPermission(PermissionCatalog.Operations.VisitPartiesView)]
    public Task<VisitPartyDetail> GetVisitParty(long partyId,CancellationToken ct)=>service.GetVisitPartyAsync(partyId,ct);

    private TenantAuditContext Audit()=>new(currentUser.UserId,HttpContext.Connection.RemoteIpAddress?.ToString(),Request.Headers.UserAgent.ToString(),HttpContext.TraceIdentifier);
}

public sealed record CreateHouseholdRequest([param:StringLength(50)]string? HouseholdCode,[param:Required,StringLength(150)]string Name,[param:StringLength(1000)]string? Notes);
public sealed record UpdateHouseholdRequest([param:Required,StringLength(150)]string Name,[param:StringLength(1000)]string? Notes,bool IsActive);
public sealed record SaveHouseholdMemberRequest(long CustomerId,[param:Required,StringLength(50)]string RelationshipType,HouseholdRelationshipSource RelationshipSource);
public sealed record UpdateHouseholdMemberRequest([param:Required,StringLength(50)]string RelationshipType,HouseholdRelationshipSource RelationshipSource,bool IsActive);
public sealed record CreateCustomerVisitRequest(long StoreId,long CustomerId,long? VisitPartyId,DateTime? EnteredUtc);
public sealed record CompleteCustomerVisitRequest(DateTime? ExitedUtc);