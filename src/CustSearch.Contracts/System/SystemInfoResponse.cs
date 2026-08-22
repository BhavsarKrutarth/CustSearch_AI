namespace CustSearch.Contracts.System;

/// <summary>
/// Describes the running API without exposing infrastructure secrets.
/// </summary>
public sealed record SystemInfoResponse(
    string Application,
    string Environment,
    string Framework,
    DateTime UtcNow,
    string CorrelationId);
