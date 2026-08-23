using System.Data;
using CustSearch.Application.Abstractions.Data;
using CustSearch.Application.ShopperCustomers;
using Dapper;

namespace CustSearch.Infrastructure.ShopperCustomers;

/// <summary>
/// Phase 6C stored-procedure search implementation. TenantId and allowed StoreIds come only from the authenticated
/// server context; SQL applies those predicates before paging so inaccessible records never enter the result set.
/// </summary>
public sealed class ShopperCustomerRepository(IDbConnectionFactory connectionFactory) : IShopperCustomerRepository
{
    public async Task<IReadOnlyList<CustomerSearchRow>> SearchCustomersAsync(long tenantId, IReadOnlySet<long> allowedStoreIds,
        bool tenantWide, CustomerSearchQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var parameters = new DynamicParameters();
        parameters.Add("TenantId", tenantId);
        parameters.Add("AllowedStoreIdsCsv", tenantWide ? null : string.Join(',', allowedStoreIds.OrderBy(x => x)));
        parameters.Add("StoreId", query.StoreId);
        parameters.Add("Search", NormalizeSearch(query.Search));
        parameters.Add("ActiveOnly", query.ActiveOnly);
        parameters.Add("PageNumber", query.PageNumber);
        parameters.Add("PageSize", query.PageSize);
        var command = new CommandDefinition("dbo.Customer_Search", parameters, commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<CustomerSearchRow>(command).ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<AnonymousVisitorSearchRow>> SearchVisitorsAsync(long tenantId, IReadOnlySet<long> allowedStoreIds,
        bool tenantWide, AnonymousVisitorSearchQuery query, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var parameters = new DynamicParameters();
        parameters.Add("TenantId", tenantId);
        parameters.Add("AllowedStoreIdsCsv", tenantWide ? null : string.Join(',', allowedStoreIds.OrderBy(x => x)));
        parameters.Add("StoreId", query.StoreId);
        parameters.Add("Search", NormalizeSearch(query.Search));
        parameters.Add("ActiveOnly", query.ActiveOnly);
        parameters.Add("PageNumber", query.PageNumber);
        parameters.Add("PageSize", query.PageSize);
        var command = new CommandDefinition("dbo.AnonymousVisitor_Search", parameters, commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<AnonymousVisitorSearchRow>(command).ConfigureAwait(false);
        return rows.AsList();
    }

    private static string? NormalizeSearch(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
