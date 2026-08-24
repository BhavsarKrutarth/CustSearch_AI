namespace CustSearch.Domain.Entities;

/// <summary>Phase 10 store/tenant-scoped phrase that resolves staff voice text to an existing ProductCategory. The parser never creates categories from arbitrary speech.</summary>
public sealed class ProductCategoryAlias
{
    private ProductCategoryAlias() { }

    private ProductCategoryAlias(long tenantId, long? storeId, long productCategoryId, string aliasText, string languageCode, long createdByUserId, DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        if (storeId.HasValue) ArgumentOutOfRangeException.ThrowIfNegativeOrZero(storeId.Value);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(productCategoryId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(createdByUserId);
        TenantId = tenantId;
        StoreId = storeId;
        ProductCategoryId = productCategoryId;
        AliasText = Require(aliasText, nameof(aliasText), 150);
        NormalizedAliasText = Normalize(AliasText);
        LanguageCode = Require(languageCode, nameof(languageCode), 20);
        IsActive = true;
        CreatedByUserId = createdByUserId;
        CreatedUtc = RequireUtc(utcNow, nameof(utcNow));
        UpdatedUtc = CreatedUtc;
    }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public long? StoreId { get; private set; }
    public long ProductCategoryId { get; private set; }
    public string AliasText { get; private set; } = string.Empty;
    public string NormalizedAliasText { get; private set; } = string.Empty;
    public string LanguageCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public long CreatedByUserId { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static ProductCategoryAlias Create(long tenantId, long? storeId, long productCategoryId, string aliasText, string languageCode, long createdByUserId, DateTime utcNow) =>
        new(tenantId, storeId, productCategoryId, aliasText, languageCode, createdByUserId, utcNow);

    public void SetActive(bool active, DateTime utcNow)
    {
        IsActive = active;
        UpdatedUtc = RequireUtc(utcNow, nameof(utcNow));
    }

    public static string Normalize(string value) => string.Join(' ', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static string Require(string value, string parameterName, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maximumLength) throw new ArgumentException($"Value cannot exceed {maximumLength} characters.", parameterName);
        return normalized;
    }

    private static DateTime RequireUtc(DateTime value, string parameterName) =>
        value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException("Timestamp must be UTC.", parameterName);
}
