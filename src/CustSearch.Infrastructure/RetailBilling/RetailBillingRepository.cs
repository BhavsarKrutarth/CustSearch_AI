using System.Data;
using CustSearch.Application.Abstractions.Data;
using CustSearch.Application.RetailBilling;
using Dapper;

namespace CustSearch.Infrastructure.RetailBilling;

/// <summary>Phase 8 read repository. Stored procedures enforce tenant/store scope before paging or aggregation.</summary>
public sealed class RetailBillingRepository(IDbConnectionFactory connectionFactory):IRetailBillingRepository
{
    public async Task<IReadOnlyList<ProductSearchRow>> SearchProductsAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,ProductSearchQuery query,CancellationToken cancellationToken=default)
    {
        await using var c=await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);var p=Base(tenantId,allowedStoreIds,tenantWide);p.Add("StoreId",query.StoreId);p.Add("CategoryId",query.CategoryId);p.Add("Search",Normalize(query.Search));p.Add("ActiveOnly",query.ActiveOnly);p.Add("PageNumber",query.PageNumber);p.Add("PageSize",query.PageSize);
        return (await c.QueryAsync<ProductSearchRow>(new CommandDefinition("dbo.Product_Search",p,commandType:CommandType.StoredProcedure,cancellationToken:cancellationToken)).ConfigureAwait(false)).AsList();
    }

    public async Task<IReadOnlyList<RetailInvoiceSearchRow>> SearchInvoicesAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,RetailInvoiceSearchQuery query,CancellationToken cancellationToken=default)
    {
        await using var c=await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);var p=Base(tenantId,allowedStoreIds,tenantWide);p.Add("StoreId",query.StoreId);p.Add("CustomerId",query.CustomerId);p.Add("Status",query.Status is null?null:(byte?)query.Status.Value);p.Add("Search",Normalize(query.Search));p.Add("FromUtc",query.FromUtc);p.Add("ToUtc",query.ToUtc);p.Add("PageNumber",query.PageNumber);p.Add("PageSize",query.PageSize);
        return (await c.QueryAsync<RetailInvoiceSearchRow>(new CommandDefinition("dbo.RetailInvoice_Search",p,commandType:CommandType.StoredProcedure,cancellationToken:cancellationToken)).ConfigureAwait(false)).AsList();
    }

    public async Task<CustomerPurchaseHistory> GetCustomerPurchaseHistoryAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,long customerId,int recentCount,CancellationToken cancellationToken=default)
    {
        await using var c=await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);var p=Base(tenantId,allowedStoreIds,tenantWide);p.Add("CustomerId",customerId);p.Add("RecentCount",Math.Clamp(recentCount,1,100));
        using var multi=await c.QueryMultipleAsync(new CommandDefinition("dbo.CustomerPurchaseHistory_Get",p,commandType:CommandType.StoredProcedure,cancellationToken:cancellationToken)).ConfigureAwait(false);
        var s=await multi.ReadSingleOrDefaultAsync<CustomerPurchaseSummaryRow>().ConfigureAwait(false)??new(customerId,0,0,0,null,null);
        var items=(await multi.ReadAsync<CustomerPurchaseHistoryItem>().ConfigureAwait(false)).AsList();return new(s.CustomerId,s.InvoiceCount,s.PayerSpend,s.ExplicitAttributedSpend,s.LastPurchaseUtc,s.LastPurchaseStoreId,items);
    }

    public async Task<HouseholdPurchaseSummary> GetHouseholdPurchaseSummaryAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,long householdId,CancellationToken cancellationToken=default)
    {
        await using var c=await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);var p=Base(tenantId,allowedStoreIds,tenantWide);p.Add("HouseholdId",householdId);
        return await c.QuerySingleOrDefaultAsync<HouseholdPurchaseSummary>(new CommandDefinition("dbo.HouseholdPurchaseSummary_Get",p,commandType:CommandType.StoredProcedure,cancellationToken:cancellationToken)).ConfigureAwait(false)??new(householdId,0,0,null);
    }

    public async Task<RetailSalesSummary> GetSalesSummaryAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,long? storeId,DateTime? fromUtc,DateTime? toUtc,CancellationToken cancellationToken=default)
    {
        await using var c=await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);var p=Report(tenantId,allowedStoreIds,tenantWide,storeId,fromUtc,toUtc);
        return await c.QuerySingleAsync<RetailSalesSummary>(new CommandDefinition("dbo.RetailSalesSummary_Get",p,commandType:CommandType.StoredProcedure,cancellationToken:cancellationToken)).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<RetailBreakdownItem>> GetSalesByProductAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,long? storeId,DateTime? fromUtc,DateTime? toUtc,int top,CancellationToken cancellationToken=default)=>Breakdown("dbo.RetailSalesByProduct_Get",tenantId,allowedStoreIds,tenantWide,storeId,fromUtc,toUtc,top,cancellationToken);
    public Task<IReadOnlyList<RetailBreakdownItem>> GetSalesByCategoryAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,long? storeId,DateTime? fromUtc,DateTime? toUtc,int top,CancellationToken cancellationToken=default)=>Breakdown("dbo.RetailSalesByCategory_Get",tenantId,allowedStoreIds,tenantWide,storeId,fromUtc,toUtc,top,cancellationToken);

    public async Task<IReadOnlyList<RetailPaymentSummaryItem>> GetPaymentSummaryAsync(long tenantId,IReadOnlyCollection<long> allowedStoreIds,bool tenantWide,long? storeId,DateTime? fromUtc,DateTime? toUtc,CancellationToken cancellationToken=default)
    {
        await using var c=await connectionFactory.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);var p=Report(tenantId,allowedStoreIds,tenantWide,storeId,fromUtc,toUtc);
        return (await c.QueryAsync<RetailPaymentSummaryItem>(new CommandDefinition("dbo.RetailPaymentSummary_Get",p,commandType:CommandType.StoredProcedure,cancellationToken:cancellationToken)).ConfigureAwait(false)).AsList();
    }

    private async Task<IReadOnlyList<RetailBreakdownItem>> Breakdown(string sp,long tenantId,IReadOnlyCollection<long> stores,bool tenantWide,long? storeId,DateTime? fromUtc,DateTime? toUtc,int top,CancellationToken ct)
    {
        await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);var p=Report(tenantId,stores,tenantWide,storeId,fromUtc,toUtc);p.Add("Top",Math.Clamp(top,1,100));return (await c.QueryAsync<RetailBreakdownItem>(new CommandDefinition(sp,p,commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false)).AsList();
    }
    private static DynamicParameters Base(long tenantId,IReadOnlyCollection<long> stores,bool tenantWide){var p=new DynamicParameters();p.Add("TenantId",tenantId);p.Add("AllowedStoreIdsCsv",tenantWide?null:Csv(stores));return p;}
    private static DynamicParameters Report(long tenantId,IReadOnlyCollection<long> stores,bool tenantWide,long? storeId,DateTime? fromUtc,DateTime? toUtc){var p=Base(tenantId,stores,tenantWide);p.Add("StoreId",storeId);p.Add("FromUtc",fromUtc);p.Add("ToUtc",toUtc);return p;}
    private static string Csv(IEnumerable<long> values)=>string.Join(',',values.OrderBy(x=>x));private static string? Normalize(string? v)=>string.IsNullOrWhiteSpace(v)?null:v.Trim();
    private sealed record CustomerPurchaseSummaryRow(long CustomerId,long InvoiceCount,decimal PayerSpend,decimal ExplicitAttributedSpend,DateTime? LastPurchaseUtc,long? LastPurchaseStoreId);
}
