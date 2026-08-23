namespace CustSearch.Domain.Entities;

/// <summary>Defines a reusable CustSearch platform subscription plan and its authoritative tenant resource limits.</summary>
public sealed class SubscriptionPlan
{
    private SubscriptionPlan() { }

    private SubscriptionPlan(
        string planCode,
        string planName,
        decimal monthlyPrice,
        decimal? annualPrice,
        int maxStores,
        int maxUsers,
        int maxCameras,
        long? maxMonthlyRecognitions,
        long? maxMonthlyApiCalls,
        DateTime createdUtc)
    {
        PlanCode = Require(planCode, nameof(planCode), 30).ToUpperInvariant();
        PlanName = Require(planName, nameof(planName), 100);
        ValidatePrices(monthlyPrice, annualPrice);
        ValidateLimits(maxStores, maxUsers, maxUsers, maxCameras, maxMonthlyRecognitions, maxMonthlyApiCalls);
        MonthlyPrice = monthlyPrice;
        AnnualPrice = annualPrice;
        MaxStores = maxStores;
        MaxUsers = maxUsers;
        MaxStaff = maxUsers;
        MaxCameras = maxCameras;
        MaxMonthlyRecognitions = maxMonthlyRecognitions;
        MaxMonthlyApiCalls = maxMonthlyApiCalls;
        Currency = "USD";
        Description = string.Empty;
        TrialDays = 0;
        DisplayOrder = 0;
        IsActive = true;
        CreatedUtc = RequireUtc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
        RowVersion = NewRowVersion();
    }

    public long Id { get; private set; }
    public string PlanCode { get; private set; } = string.Empty;
    public string PlanName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public decimal MonthlyPrice { get; private set; }
    public decimal? AnnualPrice { get; private set; }
    public string Currency { get; private set; } = "USD";
    public int TrialDays { get; private set; }
    public int MaxStores { get; private set; }
    public int MaxUsers { get; private set; }
    public int MaxStaff { get; private set; }
    public int MaxCameras { get; private set; }
    public long? MaxMonthlyRecognitions { get; private set; }
    public long? MaxMonthlyApiCalls { get; private set; }
    public string? FeatureLimitsJson { get; private set; }
    public bool IsActive { get; private set; }
    public int DisplayOrder { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static SubscriptionPlan Create(
        string planCode,
        string planName,
        decimal monthlyPrice,
        decimal? annualPrice,
        int maxStores,
        int maxUsers,
        int maxCameras,
        long? maxMonthlyRecognitions,
        long? maxMonthlyApiCalls,
        DateTime createdUtc) =>
        new(planCode, planName, monthlyPrice, annualPrice, maxStores, maxUsers, maxCameras, maxMonthlyRecognitions, maxMonthlyApiCalls, createdUtc);

    public static SubscriptionPlan CreatePlatform(
        string planCode,
        string planName,
        string? description,
        decimal monthlyPrice,
        decimal? annualPrice,
        string currency,
        int trialDays,
        int maxStores,
        int maxUsers,
        int maxStaff,
        int maxCameras,
        long? maxMonthlyRecognitions,
        long? maxMonthlyApiCalls,
        string? featureLimitsJson,
        int displayOrder,
        DateTime createdUtc)
    {
        var plan = new SubscriptionPlan(planCode, planName, monthlyPrice, annualPrice, maxStores, maxUsers, maxCameras, maxMonthlyRecognitions, maxMonthlyApiCalls, createdUtc);
        plan.ConfigurePlatformBilling(description, currency, trialDays, maxStaff, featureLimitsJson, displayOrder, createdUtc);
        return plan;
    }

    /// <summary>Updates legacy/common pricing and limits while retaining the stable plan code.</summary>
    public void Update(
        string planName,
        decimal monthlyPrice,
        decimal? annualPrice,
        int maxStores,
        int maxUsers,
        int maxCameras,
        long? maxMonthlyRecognitions,
        long? maxMonthlyApiCalls,
        DateTime updatedUtc)
    {
        ValidatePrices(monthlyPrice, annualPrice);
        ValidateLimits(maxStores, maxUsers, MaxStaff > 0 ? MaxStaff : maxUsers, maxCameras, maxMonthlyRecognitions, maxMonthlyApiCalls);
        PlanName = Require(planName, nameof(planName), 100);
        MonthlyPrice = monthlyPrice;
        AnnualPrice = annualPrice;
        MaxStores = maxStores;
        MaxUsers = maxUsers;
        MaxCameras = maxCameras;
        MaxMonthlyRecognitions = maxMonthlyRecognitions;
        MaxMonthlyApiCalls = maxMonthlyApiCalls;
        Touch(updatedUtc);
    }

    /// <summary>Configures Phase 9-only plan metadata and staff/feature quotas.</summary>
    public void ConfigurePlatformBilling(
        string? description,
        string currency,
        int trialDays,
        int maxStaff,
        string? featureLimitsJson,
        int displayOrder,
        DateTime updatedUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(trialDays);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxStaff);
        ArgumentOutOfRangeException.ThrowIfNegative(displayOrder);
        Description = Optional(description, 1000) ?? string.Empty;
        Currency = Require(currency, nameof(currency), 3).ToUpperInvariant();
        TrialDays = trialDays;
        MaxStaff = maxStaff;
        FeatureLimitsJson = Optional(featureLimitsJson, 4000);
        DisplayOrder = displayOrder;
        Touch(updatedUtc);
    }

    public void UpdatePlatform(
        string planName,
        string? description,
        decimal monthlyPrice,
        decimal? annualPrice,
        string currency,
        int trialDays,
        int maxStores,
        int maxUsers,
        int maxStaff,
        int maxCameras,
        long? maxMonthlyRecognitions,
        long? maxMonthlyApiCalls,
        string? featureLimitsJson,
        int displayOrder,
        bool isActive,
        DateTime updatedUtc)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(trialDays);
        ArgumentOutOfRangeException.ThrowIfNegative(displayOrder);
        ValidatePrices(monthlyPrice, annualPrice);
        ValidateLimits(maxStores, maxUsers, maxStaff, maxCameras, maxMonthlyRecognitions, maxMonthlyApiCalls);
        PlanName = Require(planName, nameof(planName), 100);
        MonthlyPrice = monthlyPrice;
        AnnualPrice = annualPrice;
        MaxStores = maxStores;
        MaxUsers = maxUsers;
        MaxStaff = maxStaff;
        MaxCameras = maxCameras;
        MaxMonthlyRecognitions = maxMonthlyRecognitions;
        MaxMonthlyApiCalls = maxMonthlyApiCalls;
        Description = Optional(description, 1000) ?? string.Empty;
        Currency = Require(currency, nameof(currency), 3).ToUpperInvariant();
        TrialDays = trialDays;
        FeatureLimitsJson = Optional(featureLimitsJson, 4000);
        DisplayOrder = displayOrder;
        IsActive = isActive;
        Touch(updatedUtc);
    }

    public void Deactivate(DateTime updatedUtc) => SetActive(false, updatedUtc);
    public void Activate(DateTime updatedUtc) => SetActive(true, updatedUtc);

    public void SetActive(bool isActive, DateTime updatedUtc)
    {
        IsActive = isActive;
        Touch(updatedUtc);
    }

    private static void ValidatePrices(decimal monthlyPrice, decimal? annualPrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(monthlyPrice);
        if (annualPrice.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegative(annualPrice.Value, nameof(annualPrice));
    }

    private static void ValidateLimits(
        int maxStores,
        int maxUsers,
        int maxStaff,
        int maxCameras,
        long? maxMonthlyRecognitions,
        long? maxMonthlyApiCalls)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxStores);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxUsers);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxStaff);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCameras);
        if (maxMonthlyRecognitions.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMonthlyRecognitions.Value, nameof(maxMonthlyRecognitions));
        if (maxMonthlyApiCalls.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMonthlyApiCalls.Value, nameof(maxMonthlyApiCalls));
    }

    private void Touch(DateTime utc)
    {
        UpdatedUtc = RequireUtc(utc, nameof(utc));
        RowVersion = NewRowVersion();
    }

    private static string Require(string value, string parameterName, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
            throw new ArgumentException($"Value cannot exceed {maximumLength} characters.", parameterName);
        return normalized;
    }

    private static string? Optional(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
            throw new ArgumentException($"Value cannot exceed {maximumLength} characters.", nameof(value));
        return normalized;
    }

    private static DateTime RequireUtc(DateTime value, string parameterName) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Timestamp must be UTC.", parameterName);

    private static byte[] NewRowVersion() => Guid.NewGuid().ToByteArray();
}
