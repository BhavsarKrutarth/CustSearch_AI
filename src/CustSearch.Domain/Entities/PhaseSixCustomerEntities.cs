namespace CustSearch.Domain.Entities;

/// <summary>
/// Phase 6A — tenant-owned shopper customer identity/profile foundation. Later phases enrich this record with visits,
/// households, purchases and preferences without changing the tenant boundary established here.
/// </summary>
public sealed class Customer
{
    private Customer() { }

    private Customer(long tenantId, string customerCode, string firstName, string? lastName, string? mobile,
        string? email, string? notes, DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        TenantId = tenantId;
        CustomerCode = Require(customerCode, nameof(customerCode), 50).ToUpperInvariant();
        FirstName = Require(firstName, nameof(firstName), 100);
        LastName = Optional(lastName, nameof(lastName), 100);
        Mobile = Optional(mobile, nameof(mobile), 30);
        Email = Optional(email, nameof(email), 254)?.ToLowerInvariant();
        Notes = Optional(notes, nameof(notes), 1000);
        IsActive = true;
        CreatedUtc = RequireUtc(utcNow, nameof(utcNow));
        UpdatedUtc = CreatedUtc;
    }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public string CustomerCode { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string? LastName { get; private set; }
    public string? Mobile { get; private set; }
    public string? Email { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static Customer Create(long tenantId, string customerCode, string firstName, string? lastName,
        string? mobile, string? email, string? notes, DateTime utcNow) =>
        new(tenantId, customerCode, firstName, lastName, mobile, email, notes, utcNow);

    public void Update(string firstName, string? lastName, string? mobile, string? email, string? notes, bool isActive, DateTime utcNow)
    {
        FirstName = Require(firstName, nameof(firstName), 100);
        LastName = Optional(lastName, nameof(lastName), 100);
        Mobile = Optional(mobile, nameof(mobile), 30);
        Email = Optional(email, nameof(email), 254)?.ToLowerInvariant();
        Notes = Optional(notes, nameof(notes), 1000);
        IsActive = isActive;
        UpdatedUtc = RequireUtc(utcNow, nameof(utcNow));
    }

    private static string Require(string value, string name, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var normalized = value.Trim();
        return normalized.Length <= max ? normalized : throw new ArgumentOutOfRangeException(name);
    }

    private static string? Optional(string? value, string name, int max) =>
        string.IsNullOrWhiteSpace(value) ? null : Require(value, name, max);

    private static DateTime RequireUtc(DateTime value, string name) =>
        value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException("Timestamp must be UTC.", name);
}

/// <summary>
/// Phase 6G — explicit customer-to-store visibility relation. Store-scoped administrators can access only customers
/// related to one of their server-authorized StoreIds; tenant-wide owners/admins can access all tenant customers.
/// </summary>
public sealed class CustomerStoreAssignment
{
    private CustomerStoreAssignment() { }

    private CustomerStoreAssignment(long tenantId, long customerId, long storeId, bool isPrimary, DateTime assignedUtc, long assignedByUserId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(customerId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assignedByUserId);
        if (assignedUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("AssignedUtc must be UTC.", nameof(assignedUtc));
        TenantId = tenantId;
        CustomerId = customerId;
        StoreId = storeId;
        IsPrimary = isPrimary;
        AssignedUtc = assignedUtc;
        AssignedByUserId = assignedByUserId;
    }

    public long TenantId { get; private set; }
    public long CustomerId { get; private set; }
    public Customer Customer { get; private set; } = null!;
    public long StoreId { get; private set; }
    public Store Store { get; private set; } = null!;
    public bool IsPrimary { get; private set; }
    public DateTime AssignedUtc { get; private set; }
    public long AssignedByUserId { get; private set; }
    public UserAccount AssignedByUser { get; private set; } = null!;

    public static CustomerStoreAssignment Assign(long tenantId, long customerId, long storeId, bool isPrimary,
        DateTime assignedUtc, long assignedByUserId) =>
        new(tenantId, customerId, storeId, isPrimary, assignedUtc, assignedByUserId);
}

/// <summary>
/// Phase 6B — store-bound anonymous visitor record. This intentionally contains no face embedding or externally
/// resolved identity. Conversion to a shopper customer is explicit, permission-controlled and audited.
/// </summary>
public sealed class AnonymousVisitor
{
    private AnonymousVisitor() { }

    private AnonymousVisitor(long tenantId, long storeId, string visitorCode, DateTime firstSeenUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId);
        if (firstSeenUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("FirstSeenUtc must be UTC.", nameof(firstSeenUtc));
        TenantId = tenantId;
        StoreId = storeId;
        VisitorCode = Require(visitorCode, nameof(visitorCode), 50).ToUpperInvariant();
        FirstSeenUtc = firstSeenUtc;
        LastSeenUtc = firstSeenUtc;
        IsActive = true;
        CreatedUtc = firstSeenUtc;
        UpdatedUtc = firstSeenUtc;
    }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public long StoreId { get; private set; }
    public Store Store { get; private set; } = null!;
    public string VisitorCode { get; private set; } = string.Empty;
    public DateTime FirstSeenUtc { get; private set; }
    public DateTime LastSeenUtc { get; private set; }
    public bool IsActive { get; private set; }
    public long? ConvertedCustomerId { get; private set; }
    public Customer? ConvertedCustomer { get; private set; }
    public DateTime? ConvertedUtc { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static AnonymousVisitor Create(long tenantId, long storeId, string visitorCode, DateTime firstSeenUtc) =>
        new(tenantId, storeId, visitorCode, firstSeenUtc);

    public void Touch(DateTime seenUtc)
    {
        if (seenUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Seen timestamp must be UTC.", nameof(seenUtc));
        ArgumentOutOfRangeException.ThrowIfLessThan(seenUtc, FirstSeenUtc);
        LastSeenUtc = seenUtc;
        UpdatedUtc = seenUtc;
    }

    public void ConvertToCustomer(long customerId, DateTime convertedUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(customerId);
        if (convertedUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("ConvertedUtc must be UTC.", nameof(convertedUtc));
        if (ConvertedCustomerId.HasValue) throw new InvalidOperationException("Anonymous visitor has already been converted.");
        ConvertedCustomerId = customerId;
        ConvertedUtc = convertedUtc;
        IsActive = false;
        UpdatedUtc = convertedUtc;
    }

    private static string Require(string value, string name, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var normalized = value.Trim();
        return normalized.Length <= max ? normalized : throw new ArgumentOutOfRangeException(name);
    }
}
