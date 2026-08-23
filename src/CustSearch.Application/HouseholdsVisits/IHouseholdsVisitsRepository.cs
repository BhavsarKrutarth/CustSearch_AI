namespace CustSearch.Application.HouseholdsVisits;

/// <summary>Phase 7 Dapper read repository. Tenant/store filters are always supplied by the trusted server context.</summary>
public interface IHouseholdsVisitsRepository
{
    Task<IReadOnlyList<HouseholdSearchRow>> SearchHouseholdsAsync(long tenantId,IReadOnlySet<long> allowedStoreIds,bool tenantWide,HouseholdSearchQuery query,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<CustomerVisitSearchRow>> SearchVisitsAsync(long tenantId,IReadOnlySet<long> allowedStoreIds,bool tenantWide,CustomerVisitSearchQuery query,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<VisitPartySearchRow>> SearchVisitPartiesAsync(long tenantId,IReadOnlySet<long> allowedStoreIds,bool tenantWide,VisitPartySearchQuery query,CancellationToken cancellationToken=default);
}