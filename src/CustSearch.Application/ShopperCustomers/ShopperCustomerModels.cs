namespace CustSearch.Application.ShopperCustomers;

/// <summary>Consistent paged response used by Phase 6 customer and visitor search APIs.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Data, int PageNumber, int PageSize, long TotalCount)
{
    public long TotalPages => PageSize <= 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;
}

/// <summary>Phase 6C — normalized query model for tenant/store-safe customer search.</summary>
public sealed record CustomerSearchQuery(int PageNumber = 1, int PageSize = 25, string? Search = null, long? StoreId = null, bool ActiveOnly = false);

/// <summary>Phase 6C — normalized query model for tenant/store-safe anonymous visitor search.</summary>
public sealed record AnonymousVisitorSearchQuery(int PageNumber = 1, int PageSize = 25, string? Search = null, long? StoreId = null, bool ActiveOnly = false);

public sealed record CustomerListItem(long Id, string CustomerCode, string FirstName, string? LastName, string? Mobile,
    string? Email, bool IsActive, IReadOnlyList<long> StoreIds, DateTime UpdatedUtc);

public sealed record CustomerDetail(long Id, string CustomerCode, string FirstName, string? LastName, string? Mobile,
    string? Email, string? Notes, bool IsActive, IReadOnlyList<long> StoreIds, long? PrimaryStoreId, DateTime CreatedUtc, DateTime UpdatedUtc);

public sealed record CreateCustomerCommand(string? CustomerCode, string FirstName, string? LastName, string? Mobile,
    string? Email, string? Notes, IReadOnlyList<long> StoreIds, long? PrimaryStoreId);

public sealed record UpdateCustomerCommand(string FirstName, string? LastName, string? Mobile, string? Email, string? Notes, bool IsActive);
public sealed record SetCustomerStoresCommand(IReadOnlyList<long> StoreIds, long? PrimaryStoreId);

/// <summary>
/// Phase 6D smart-profile foundation. Later phases add visits, households, invoices and preference evidence; this phase
/// returns only facts that already exist and never fabricates purchase/behavioral insight.
/// </summary>
public sealed record CustomerSmartProfile(
    CustomerDetail Customer,
    int ConvertedAnonymousVisitorCount,
    DateTime? LastAnonymousVisitorSeenUtc,
    bool HasMobile,
    bool HasEmail,
    IReadOnlyList<string> AvailableSections,
    IReadOnlyList<string> PlannedEnrichmentSections);

public sealed record AnonymousVisitorListItem(long Id, string VisitorCode, long StoreId, DateTime FirstSeenUtc,
    DateTime LastSeenUtc, bool IsActive, long? ConvertedCustomerId, DateTime? ConvertedUtc);

public sealed record AnonymousVisitorDetail(long Id, string VisitorCode, long StoreId, DateTime FirstSeenUtc,
    DateTime LastSeenUtc, bool IsActive, long? ConvertedCustomerId, DateTime? ConvertedUtc, DateTime CreatedUtc, DateTime UpdatedUtc);

public sealed record CreateAnonymousVisitorCommand(long StoreId, string? VisitorCode, DateTime? SeenUtc);
public sealed record TouchAnonymousVisitorCommand(DateTime? SeenUtc);

/// <summary>
/// Phase 6B explicit conversion command. Supply CustomerId to link an authorized existing customer, or omit it and
/// provide FirstName to create a new customer. No automatic face/social identity lookup is permitted.
/// </summary>
public sealed record ConvertAnonymousVisitorCommand(long? CustomerId, string? FirstName, string? LastName, string? Mobile, string? Email, string? Notes);

/// <summary>Internal Dapper projection from dbo.Customer_Search before authorized store assignments are attached.</summary>
public sealed record CustomerSearchRow(long Id, string CustomerCode, string FirstName, string? LastName, string? Mobile,
    string? Email, bool IsActive, DateTime UpdatedUtc, long TotalCount);

/// <summary>Internal Dapper projection from dbo.AnonymousVisitor_Search.</summary>
public sealed record AnonymousVisitorSearchRow(long Id, string VisitorCode, long StoreId, DateTime FirstSeenUtc,
    DateTime LastSeenUtc, bool IsActive, long? ConvertedCustomerId, DateTime? ConvertedUtc, long TotalCount);
