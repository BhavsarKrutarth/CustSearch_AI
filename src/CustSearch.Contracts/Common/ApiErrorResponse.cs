namespace CustSearch.Contracts.Common;

/// <summary>
/// Provides a stable error envelope that can be correlated with server logs.
/// </summary>
public sealed record ApiErrorResponse(string Code, string Message, string CorrelationId);
