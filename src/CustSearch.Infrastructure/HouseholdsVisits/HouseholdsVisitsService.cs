using System.Globalization;
using System.Text.Json;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.HouseholdsVisits;
using CustSearch.Application.ShopperCustomers;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.HouseholdsVisits;

/// <summary>Phase 7 tenant/store-safe household, factual visit and co-visit party implementation.</summary>
public sealed class HouseholdsVisitsService(
    CustSearchDbContext db,
    IHouseholdsVisitsRepository repository,
    ICurrentUserContext currentUser,
    TimeProvider timeProvider) : IHouseholdsVisitsService
{
    public async Task<PagedResult<HouseholdListItem>> SearchHouseholdsAsync(HouseholdSearchQuery query,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(query); var tenantWide=IsTenantWide(); var paging=PhaseSixAccessRules.NormalizePaging(query.PageNumber,query.PageSize);
        var normalized=query with{PageNumber=paging.PageNumber,PageSize=paging.PageSize};
        var rows=await repository.SearchHouseholdsAsync(RequireTenantId(),currentUser.StoreIds,tenantWide,normalized,cancellationToken).ConfigureAwait(false);
        if(rows.Count==0)return PhaseSevenPaging.Empty<HouseholdListItem>(normalized.PageNumber,normalized.PageSize);
        return new(rows.Select(x=>new HouseholdListItem(x.Id,x.HouseholdCode,x.Name,x.VisibleMemberCount,x.IsActive,x.UpdatedUtc)).ToArray(),normalized.PageNumber,normalized.PageSize,rows[0].TotalCount);
    }

    public async Task<HouseholdDetail> GetHouseholdAsync(long householdId,CancellationToken cancellationToken=default)=>
        await MapHouseholdAsync(await RequireVisibleHouseholdAsync(householdId,false,cancellationToken).ConfigureAwait(false),cancellationToken).ConfigureAwait(false);

    public async Task<HouseholdDetail> CreateHouseholdAsync(CreateHouseholdCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command); ValidateAudit(audit);
        // Empty households have no store visibility relation yet. Restrict creation to tenant-wide owners/admins so a store-scoped user cannot create an immediately inaccessible orphan.
        if(!IsTenantWide())throw new TenantBusinessRuleException("Creating a new household requires a tenant-wide owner/admin. Store-scoped users may manage visible household members.");
        var tenantId=RequireTenantId(); var code=string.IsNullOrWhiteSpace(command.HouseholdCode)?NewHouseholdCode():command.HouseholdCode.Trim().ToUpperInvariant();
        if(await db.Households.AnyAsync(x=>x.TenantId==tenantId&&x.HouseholdCode==code,cancellationToken).ConfigureAwait(false))throw new TenantBusinessRuleException("Household code already exists in this tenant.");
        Household household; try{household=Household.Create(tenantId,code,command.Name,command.Notes,UtcNow());}catch(ArgumentException ex){throw new TenantBusinessRuleException(ex.Message);}
        db.Households.Add(household); await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId,null,audit,"HouseholdCreated","Household",household.Id,null,new{household.HouseholdCode,household.Name},UtcNow()); await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await MapHouseholdAsync(household,cancellationToken).ConfigureAwait(false);
    }

    public async Task<HouseholdDetail> UpdateHouseholdAsync(long householdId,UpdateHouseholdCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command); ValidateAudit(audit); var household=await RequireVisibleHouseholdAsync(householdId,true,cancellationToken).ConfigureAwait(false);
        var before=new{household.Name,household.Notes,household.IsActive}; try{household.Update(command.Name,command.Notes,command.IsActive,UtcNow());}catch(ArgumentException ex){throw new TenantBusinessRuleException(ex.Message);}
        RecordAudit(RequireTenantId(),null,audit,"HouseholdUpdated","Household",household.Id,before,new{household.Name,household.Notes,household.IsActive},UtcNow()); await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await MapHouseholdAsync(household,cancellationToken).ConfigureAwait(false);
    }

    public async Task<HouseholdDetail> SaveHouseholdMemberAsync(long householdId,SaveHouseholdMemberCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command); ValidateAudit(audit); var tenantId=RequireTenantId(); var household=await RequireVisibleOrTenantWideHouseholdAsync(householdId,true,cancellationToken).ConfigureAwait(false);
        await RequireVisibleCustomerForHouseholdAsync(command.CustomerId,cancellationToken).ConfigureAwait(false);
        var now=UtcNow(); var member=await db.HouseholdMembers.SingleOrDefaultAsync(x=>x.TenantId==tenantId&&x.HouseholdId==householdId&&x.CustomerId==command.CustomerId,cancellationToken).ConfigureAwait(false);
        object? before=null;
        try
        {
            if(member is null){member=HouseholdMember.Link(tenantId,householdId,command.CustomerId,command.RelationshipType,command.RelationshipSource,audit.ActorUserId,now);db.HouseholdMembers.Add(member);}
            else{before=new{member.RelationshipType,member.RelationshipSource,member.IsActive};member.Update(command.RelationshipType,command.RelationshipSource,command.IsActive,audit.ActorUserId,now);}
        }
        catch(ArgumentException ex){throw new TenantBusinessRuleException(ex.Message);}
        RecordAudit(tenantId,null,audit,"HouseholdMemberSaved","HouseholdMember",command.CustomerId,before,new{HouseholdId=householdId,command.CustomerId,command.RelationshipType,command.RelationshipSource,command.IsActive},now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); return await MapHouseholdAsync(household,cancellationToken).ConfigureAwait(false);
    }

    public async Task RemoveHouseholdMemberAsync(long householdId,long customerId,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ValidateAudit(audit); var tenantId=RequireTenantId(); await RequireVisibleHouseholdAsync(householdId,true,cancellationToken).ConfigureAwait(false);
        var member=await db.HouseholdMembers.SingleOrDefaultAsync(x=>x.TenantId==tenantId&&x.HouseholdId==householdId&&x.CustomerId==customerId,cancellationToken).ConfigureAwait(false)
            ??throw new TenantResourceNotFoundException("Household member");
        await RequireVisibleCustomerForHouseholdAsync(customerId,cancellationToken).ConfigureAwait(false);
        var before=new{member.RelationshipType,member.RelationshipSource,member.IsActive}; member.Update(member.RelationshipType,member.RelationshipSource,false,audit.ActorUserId,UtcNow());
        RecordAudit(tenantId,null,audit,"HouseholdMemberRemoved","HouseholdMember",customerId,before,new{HouseholdId=householdId,CustomerId=customerId,IsActive=false},UtcNow()); await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<CustomerVisitListItem>> SearchVisitsAsync(CustomerVisitSearchQuery query,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(query); var tenantWide=IsTenantWide(); EnsureStoreFilterAllowed(query.StoreId,tenantWide); var paging=PhaseSixAccessRules.NormalizePaging(query.PageNumber,query.PageSize);
        var normalized=query with{PageNumber=paging.PageNumber,PageSize=paging.PageSize}; var rows=await repository.SearchVisitsAsync(RequireTenantId(),currentUser.StoreIds,tenantWide,normalized,cancellationToken).ConfigureAwait(false);
        if(rows.Count==0)return PhaseSevenPaging.Empty<CustomerVisitListItem>(normalized.PageNumber,normalized.PageSize);
        return new(rows.Select(MapVisitRow).ToArray(),normalized.PageNumber,normalized.PageSize,rows[0].TotalCount);
    }

    public async Task<CustomerVisitDetail> GetVisitAsync(long visitId,CancellationToken cancellationToken=default)=>MapVisit(await RequireVisibleVisitAsync(visitId,false,cancellationToken).ConfigureAwait(false));

    public async Task<CustomerVisitDetail> CreateVisitAsync(CreateCustomerVisitCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command); ValidateAudit(audit); var tenantId=RequireTenantId(); await RequireAuthorizedStoreAsync(command.StoreId,cancellationToken).ConfigureAwait(false);
        var customer=await db.Customers.AsNoTracking().SingleOrDefaultAsync(x=>x.TenantId==tenantId&&x.Id==command.CustomerId&&x.IsActive,cancellationToken).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Customer");
        var assigned=await db.CustomerStoreAssignments.AsNoTracking().AnyAsync(x=>x.TenantId==tenantId&&x.CustomerId==customer.Id&&x.StoreId==command.StoreId,cancellationToken).ConfigureAwait(false);
        if(!assigned)throw new TenantBusinessRuleException("Customer must be assigned to the visit store.");
        if(command.VisitPartyId.HasValue&&!await db.VisitParties.AsNoTracking().AnyAsync(x=>x.TenantId==tenantId&&x.StoreId==command.StoreId&&x.Id==command.VisitPartyId.Value,cancellationToken).ConfigureAwait(false))throw new TenantBusinessRuleException("Visit party is invalid or belongs to another store/tenant.");
        var entered=NormalizeUtc(command.EnteredUtc??UtcNow()); CustomerVisit visit; try{visit=CustomerVisit.Create(tenantId,command.StoreId,command.CustomerId,NewVisitCode(),entered,command.Source,command.VisitPartyId);}catch(ArgumentException ex){throw new TenantBusinessRuleException(ex.Message);}
        db.CustomerVisits.Add(visit); await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); RecordAudit(tenantId,command.StoreId,audit,"CustomerVisitCreated","CustomerVisit",visit.Id,null,new{visit.VisitCode,visit.CustomerId,visit.VisitPartyId,visit.EnteredUtc,visit.Source},UtcNow()); await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); return MapVisit(visit);
    }

    public async Task<CustomerVisitDetail> CompleteVisitAsync(long visitId,CompleteCustomerVisitCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(command); ValidateAudit(audit); var visit=await RequireVisibleVisitAsync(visitId,true,cancellationToken).ConfigureAwait(false); if(visit.Status!=CustomerVisitStatus.Open)throw new TenantBusinessRuleException("Only open visits can be completed.");
        var before=new{visit.ExitedUtc,visit.Status}; try{visit.Complete(NormalizeUtc(command.ExitedUtc??UtcNow()));}catch(ArgumentException ex){throw new TenantBusinessRuleException(ex.Message);}
        RecordAudit(RequireTenantId(),visit.StoreId,audit,"CustomerVisitCompleted","CustomerVisit",visit.Id,before,new{visit.ExitedUtc,visit.Status},UtcNow()); await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false); return MapVisit(visit);
    }

    public async Task<PagedResult<VisitPartyListItem>> SearchVisitPartiesAsync(VisitPartySearchQuery query,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(query); var tenantWide=IsTenantWide(); EnsureStoreFilterAllowed(query.StoreId,tenantWide); var paging=PhaseSixAccessRules.NormalizePaging(query.PageNumber,query.PageSize);
        var normalized=query with{PageNumber=paging.PageNumber,PageSize=paging.PageSize}; var rows=await repository.SearchVisitPartiesAsync(RequireTenantId(),currentUser.StoreIds,tenantWide,normalized,cancellationToken).ConfigureAwait(false);
        if(rows.Count==0)return PhaseSevenPaging.Empty<VisitPartyListItem>(normalized.PageNumber,normalized.PageSize);
        return new(rows.Select(x=>new VisitPartyListItem(x.Id,x.PartyCode,x.StoreId,x.StartedUtc,x.EndedUtc,x.Source,x.Status,x.MemberCount)).ToArray(),normalized.PageNumber,normalized.PageSize,rows[0].TotalCount);
    }

    public async Task<VisitPartyDetail> GetVisitPartyAsync(long partyId,CancellationToken cancellationToken=default)
    {
        var party=await RequireVisiblePartyAsync(partyId,cancellationToken).ConfigureAwait(false); var tenantId=RequireTenantId();
        var members=await db.VisitPartyMembers.AsNoTracking().Where(x=>x.TenantId==tenantId&&x.VisitPartyId==party.Id)
            .OrderBy(x=>x.JoinedUtc).Select(x=>new VisitPartyMemberView(x.Id,x.IdentityType,x.CustomerId,x.Customer!=null?x.Customer.CustomerCode:null,x.AnonymousVisitorId,x.AnonymousVisitor!=null?x.AnonymousVisitor.VisitorCode:null,x.JoinedUtc))
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);
        return new(party.Id,party.PartyCode,party.StoreId,party.StartedUtc,party.EndedUtc,party.Source,party.Status,members,party.CreatedUtc,party.UpdatedUtc);
    }

    private async Task<Household> RequireVisibleHouseholdAsync(long householdId,bool tracked,CancellationToken ct)
    {
        var tenantId=RequireTenantId(); var q=db.Households.Where(x=>x.TenantId==tenantId&&x.Id==householdId); var h=await(tracked?q:q.AsNoTracking()).SingleOrDefaultAsync(ct).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Household");
        if(!IsTenantWide())
        {
            var visible=await db.HouseholdMembers.AsNoTracking().AnyAsync(m=>m.TenantId==tenantId&&m.HouseholdId==householdId&&m.IsActive&&db.CustomerStoreAssignments.Any(cs=>cs.TenantId==tenantId&&cs.CustomerId==m.CustomerId&&currentUser.StoreIds.Contains(cs.StoreId)),ct).ConfigureAwait(false);
            if(!visible)throw new TenantResourceNotFoundException("Household");
        }
        return h;
    }

    private async Task<Household> RequireVisibleOrTenantWideHouseholdAsync(long householdId,bool tracked,CancellationToken ct)=>await RequireVisibleHouseholdAsync(householdId,tracked,ct).ConfigureAwait(false);

    private async Task<Customer> RequireVisibleCustomerForHouseholdAsync(long customerId,CancellationToken ct)
    {
        var tenantId=RequireTenantId(); var customer=await db.Customers.AsNoTracking().SingleOrDefaultAsync(x=>x.TenantId==tenantId&&x.Id==customerId&&x.IsActive,ct).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Customer");
        if(!IsTenantWide()&&!await db.CustomerStoreAssignments.AsNoTracking().AnyAsync(x=>x.TenantId==tenantId&&x.CustomerId==customerId&&currentUser.StoreIds.Contains(x.StoreId),ct).ConfigureAwait(false))throw new TenantResourceNotFoundException("Customer"); return customer;
    }

    private async Task<HouseholdDetail> MapHouseholdAsync(Household h,CancellationToken ct)
    {
        var tenantId=RequireTenantId(); var q=db.HouseholdMembers.AsNoTracking().Where(x=>x.TenantId==tenantId&&x.HouseholdId==h.Id);
        if(!IsTenantWide())q=q.Where(x=>db.CustomerStoreAssignments.Any(cs=>cs.TenantId==tenantId&&cs.CustomerId==x.CustomerId&&currentUser.StoreIds.Contains(cs.StoreId)));
        var members=await q.OrderByDescending(x=>x.IsActive).ThenBy(x=>x.Customer.FirstName).Select(x=>new HouseholdMemberView(x.CustomerId,x.Customer.CustomerCode,x.Customer.FirstName,x.Customer.LastName,x.RelationshipType,x.RelationshipSource,x.IsVerified,x.VerifiedByUserId,x.VerifiedUtc,x.IsActive)).ToArrayAsync(ct).ConfigureAwait(false);
        return new(h.Id,h.HouseholdCode,h.Name,h.Notes,h.IsActive,members,h.CreatedUtc,h.UpdatedUtc);
    }

    private async Task<CustomerVisit> RequireVisibleVisitAsync(long id,bool tracked,CancellationToken ct)
    {
        var tenantId=RequireTenantId(); var q=db.CustomerVisits.Where(x=>x.TenantId==tenantId&&x.Id==id); var v=await(tracked?q:q.AsNoTracking()).SingleOrDefaultAsync(ct).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Customer visit");
        if(!PhaseSixAccessRules.CanAccessStore(v.StoreId,currentUser.StoreIds,IsTenantWide()))throw new TenantResourceNotFoundException("Customer visit"); return v;
    }
    private async Task<VisitParty> RequireVisiblePartyAsync(long id,CancellationToken ct)
    {
        var tenantId=RequireTenantId(); var p=await db.VisitParties.AsNoTracking().SingleOrDefaultAsync(x=>x.TenantId==tenantId&&x.Id==id,ct).ConfigureAwait(false)??throw new TenantResourceNotFoundException("Visit party");
        if(!PhaseSixAccessRules.CanAccessStore(p.StoreId,currentUser.StoreIds,IsTenantWide()))throw new TenantResourceNotFoundException("Visit party"); return p;
    }
    private async Task RequireAuthorizedStoreAsync(long storeId,CancellationToken ct)
    {
        if(!PhaseSixAccessRules.CanAccessStore(storeId,currentUser.StoreIds,IsTenantWide()))throw new TenantResourceNotFoundException("Store");
        if(!await db.Stores.AsNoTracking().AnyAsync(x=>x.TenantId==RequireTenantId()&&x.Id==storeId&&x.IsActive,ct).ConfigureAwait(false))throw new TenantResourceNotFoundException("Store");
    }
    private void EnsureStoreFilterAllowed(long? storeId,bool tenantWide){if(storeId.HasValue&&!PhaseSixAccessRules.CanAccessStore(storeId.Value,currentUser.StoreIds,tenantWide))throw new TenantResourceNotFoundException("Store");}
    private bool IsTenantWide()=>PhaseSixAccessRules.IsTenantWide(currentUser.Roles);
    private long RequireTenantId()=>currentUser.TenantId is>0 and var id?id:throw new UnauthorizedAccessException("Tenant context is required.");
    private DateTime UtcNow()=>timeProvider.GetUtcNow().UtcDateTime;
    private static DateTime NormalizeUtc(DateTime value)=>value.Kind switch{DateTimeKind.Utc=>value,DateTimeKind.Local=>value.ToUniversalTime(),_=>DateTime.SpecifyKind(value,DateTimeKind.Utc)};
    private static string NewHouseholdCode()=>$"HH-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
    private static string NewVisitCode()=>$"VST-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
    private static CustomerVisitListItem MapVisitRow(CustomerVisitSearchRow x)=>new(x.Id,x.VisitCode,x.CustomerId,x.CustomerCode,x.CustomerName,x.StoreId,x.VisitPartyId,x.EnteredUtc,x.ExitedUtc,x.Source,x.Status);
    private static CustomerVisitDetail MapVisit(CustomerVisit x)=>new(x.Id,x.VisitCode,x.CustomerId,x.Customer.CustomerCode,$"{x.Customer.FirstName} {x.Customer.LastName}".Trim(),x.StoreId,x.VisitPartyId,x.EnteredUtc,x.ExitedUtc,x.Source,x.Status,x.CreatedUtc,x.UpdatedUtc);
    private static void ValidateAudit(TenantAuditContext audit){if(audit.ActorUserId<=0||string.IsNullOrWhiteSpace(audit.CorrelationId))throw new ArgumentException("Valid audit context is required.",nameof(audit));}
    private void RecordAudit(long tenantId,long? storeId,TenantAuditContext audit,string action,string entityType,long entityId,object? before,object? after,DateTime now)=>db.AuditLogs.Add(AuditLog.Record(tenantId,storeId,audit.ActorUserId,"User",action,entityType,entityId.ToString(CultureInfo.InvariantCulture),before is null?null:JsonSerializer.Serialize(before),after is null?null:JsonSerializer.Serialize(after),audit.IpAddress,audit.UserAgent,audit.CorrelationId,now));
}