namespace CustSearch.Domain.Entities;

/// <summary>
/// Defines a reusable commercial plan and its default tenant resource limits.
/// </summary>
public sealed class SubscriptionPlan
{
    private SubscriptionPlan()
    {
    }

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
        ValidateLimits(maxStores, maxUsers, maxCameras, maxMonthlyRecognitions, maxMonthlyApiCalls);
        MonthlyPrice = monthlyPrice;
        AnnualPrice = annualPrice;
        MaxStores = maxStores;
        MaxUsers = maxUsers;
        MaxCameras = maxCameras;
        MaxMonthlyRecognitions = maxMonthlyRecognitions;
        MaxMonthlyApiCalls = maxMonthlyApiCalls;
        IsActive = true;
        CreatedUtc = RequireUtc(createdUtc, nameof(createdUtc));
        UpdatedUtc = CreatedUtc;
        RowVersion = NewRowVersion();
    }

    public long Id { get; private set; }

    public string PlanCode { get; private set; } = string.Empty;

    public string PlanName { get; private set; } = string.Empty;

    public decimal MonthlyPrice { get; private set; }

    public decimal? AnnualPrice { get; private set; }

    public int MaxStores { get; private set; }

    public int MaxUsers { get; private set; }

    public int MaxCameras { get; private set; }

    public long? MaxMonthlyRecognitions { get; private set; }

    public long? MaxMonthlyApiCalls { get; private set; }

    public bool IsActive { get; private set; }

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
        new(
            planCode,
            planName,
            monthlyPrice,
            annualPrice,
            maxStores,
            maxUsers,
            maxCameras,
            maxMonthlyRecognitions,
            maxMonthlyApiCalls,
            createdUtc);

    /// <summary>Updates plan pricing and limits while retaining its stable plan code.</summary>
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
        ValidateLimits(maxStores, maxUsers, maxCameras, maxMonthlyRecognitions, maxMonthlyApiCalls);
        PlanName = Require(planName, nameof(planName), 100);
        MonthlyPrice = monthlyPrice;
        AnnualPrice = annualPrice;
        MaxStores = maxStores;
        MaxUsers = maxUsers;
        MaxCameras = maxCameras;
        MaxMonthlyRecognitions = maxMonthlyRecognitions;
        MaxMonthlyApiCalls = maxMonthlyApiCalls;
        UpdatedUtc = RequireUtc(updatedUtc, nameof(updatedUtc));
        RowVersion = NewRowVersion();
    }

    public void Deactivate(DateTime updatedUtc)
    {
        IsActive = false;
        UpdatedUtc = RequireUtc(updatedUtc, nameof(updatedUtc));
        RowVersion = NewRowVersion();
    }

    /// <summary>Activates or deactivates a plan and advances its concurrency version.</summary>
    public void SetActive(bool isActive, DateTime updatedUtc)
    {
        IsActive = isActive;
        UpdatedUtc = RequireUtc(updatedUtc, nameof(updatedUtc));
        RowVersion = NewRowVersion();
    }

    public void Activate(DateTime updatedUtc) => SetActive(true, updatedUtc);

    private static void ValidatePrices(decimal monthlyPrice, decimal? annualPrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(monthlyPrice);
        if (annualPrice < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(annualPrice));
        }
    }

    private static void ValidateLimits(
        int maxStores,
        int maxUsers,
        int maxCameras,
        long? maxMonthlyRecognitions,
        long? maxMonthlyApiCalls)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxStores);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxUsers);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCameras);
        if (maxMonthlyRecognitions is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMonthlyRecognitions));
        }

        if (maxMonthlyApiCalls is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxMonthlyApiCalls));
        }
    }

    private static string Require(string value, string parameterName, int maximumLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        return normalized.Length <= maximumLength
            ? normalized
            : throw new ArgumentOutOfRangeException(parameterName, $"Value cannot exceed {maximumLength} characters.");
    }

    private static DateTime RequireUtc(DateTime value, string parameterName) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : throw new ArgumentException("Timestamp must be UTC.", parameterName);

    private static byte[] NewRowVersion() => Guid.NewGuid().ToByteArray();
}
