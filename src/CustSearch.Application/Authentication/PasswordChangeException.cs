namespace CustSearch.Application.Authentication;

/// <summary>
/// Represents a safe, expected self-service password validation failure. It deliberately carries
/// no hash, credential or account-discovery data and is mapped to HTTP 400 by the auth boundary.
/// </summary>
public sealed class PasswordChangeException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}
