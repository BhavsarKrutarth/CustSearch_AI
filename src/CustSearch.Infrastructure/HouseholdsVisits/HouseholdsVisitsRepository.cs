using System.Data;
using CustSearch.Application.Abstractions.Data;
using CustSearch.Application.HouseholdsVisits;
using Dapper;

namespace CustSearch.Infrastructure.HouseholdsVisits;

/// <summary>Phase 7 stored-procedure read repository. SQL applies TenantId/allowed StoreIds before paging.</summary>
public sealed class HouseholdsVisitsRepository(IDbConnectionFactory connectionFactory) : IHouseholdsVisitsRepository
{
    public async Task<IReadOnlyList<HouseholdSearchRow>> SearchHouseholdsAsync(long tenantId,IReadOnlySet<long> allowedStoreIds,bool tenantWide,HouseholdSearchQuery query,CancellationToken cancellationToken=default)
    {
        await using var connection=await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var p=new DynamicParameters(); p.Add("TenantId",tenantId); p.Add("AllowedStoreIdsCsv",tenantWide?null:Csv(allowedStoreIds));
        p.Add("Search",Normalize(query.Search)); p.Add("ActiveOnly",query.ActiveOnly); p.Add("PageNumber",query.PageNumber); p.Add("PageSize",query.PageSize);
        var cmd=new CommandDefinition("dbo.Household_Search",p,commandType:CommandType.StoredProcedure,cancellationToken:cancellationToken);
        return (await connection.QueryAsync<HouseholdSearchRow>(cmd).ConfigureAwait(false)).AsList();
    }

    public async Task<IReadOnlyList<CustomerVisitSearchRow>> SearchVisitsAsync(long tenantId,IReadOnlySet<long> allowedStoreIds,bool tenantWide,CustomerVisitSearchQuery query,CancellationToken cancellationToken=default)
    {
        await using var connection=await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var p=new DynamicParameters(); p.Add("TenantId",tenantId); p.Add("AllowedStoreIdsCsv",tenantWide?null:Csv(allowedStoreIds));
        p.Add("StoreId",query.StoreId); p.Add("CustomerId",query.CustomerId); p.Add("Search",Normalize(query.Search)); p.Add("FromUtc",query.FromUtc); p.Add("ToUtc",query.ToUtc);
        p.Add("PageNumber",query.PageNumber); p.Add("PageSize",query.PageSize);
        var cmd=new CommandDefinition("dbo.CustomerVisit_Search",p,commandType:CommandType.StoredProcedure,cancellationToken:cancellationToken);
        return (await connection.QueryAsync<CustomerVisitSearchRow>(cmd).ConfigureAwait(false)).AsList();
    }

    public async Task<IReadOnlyList<VisitPartySearchRow>> SearchVisitPartiesAsync(long tenantId,IReadOnlySet<long> allowedStoreIds,bool tenantWide,VisitPartySearchQuery query,CancellationToken cancellationToken=default)
    {
        await using var connection=await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var p=new DynamicParameters(); p.Add("TenantId",tenantId); p.Add("AllowedStoreIdsCsv",tenantWide?null:Csv(allowedStoreIds));
        p.Add("StoreId",query.StoreId); p.Add("Search",Normalize(query.Search)); p.Add("Status",query.Status is null?null:(byte?)query.Status.Value); p.Add("FromUtc",query.FromUtc); p.Add("ToUtc",query.ToUtc);
        p.Add("PageNumber",query.PageNumber); p.Add("PageSize",query.PageSize);
        var cmd=new CommandDefinition("dbo.VisitParty_Search",p,commandType:CommandType.StoredProcedure,cancellationToken:cancellationToken);
        return (await connection.QueryAsync<VisitPartySearchRow>(cmd).ConfigureAwait(false)).AsList();
    }

    private static string Csv(IReadOnlySet<long> values)=>string.Join(',',values.OrderBy(x=>x));
    private static string? Normalize(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
}