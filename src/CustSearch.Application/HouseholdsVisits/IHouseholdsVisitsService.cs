using CustSearch.Application.ShopperCustomers;
using CustSearch.Application.TenantOperations;

namespace CustSearch.Application.HouseholdsVisits;

/// <summary>Phase 7 application boundary for verified households, factual visits and co-visit parties.</summary>
public interface IHouseholdsVisitsService
{
    Task<PagedResult<HouseholdListItem>> SearchHouseholdsAsync(HouseholdSearchQuery query,CancellationToken cancellationToken=default);
    Task<HouseholdDetail> GetHouseholdAsync(long householdId,CancellationToken cancellationToken=default);
    Task<HouseholdDetail> CreateHouseholdAsync(CreateHouseholdCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<HouseholdDetail> UpdateHouseholdAsync(long householdId,UpdateHouseholdCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<HouseholdDetail> SaveHouseholdMemberAsync(long householdId,SaveHouseholdMemberCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task RemoveHouseholdMemberAsync(long householdId,long customerId,TenantAuditContext audit,CancellationToken cancellationToken=default);

    Task<PagedResult<CustomerVisitListItem>> SearchVisitsAsync(CustomerVisitSearchQuery query,CancellationToken cancellationToken=default);
    Task<CustomerVisitDetail> GetVisitAsync(long visitId,CancellationToken cancellationToken=default);
    Task<CustomerVisitDetail> CreateVisitAsync(CreateCustomerVisitCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);
    Task<CustomerVisitDetail> CompleteVisitAsync(long visitId,CompleteCustomerVisitCommand command,TenantAuditContext audit,CancellationToken cancellationToken=default);

    Task<PagedResult<VisitPartyListItem>> SearchVisitPartiesAsync(VisitPartySearchQuery query,CancellationToken cancellationToken=default);
    Task<VisitPartyDetail> GetVisitPartyAsync(long partyId,CancellationToken cancellationToken=default);
}