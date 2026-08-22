namespace CustSearch.Application.Authentication;

/// <summary>
/// Carries a safe authentication failure code without exposing credential details.
/// </summary>
public sealed class AuthenticationFailureException(AuthenticationFailure failure)
    : Exception("Authentication could not be completed.")
{
    public AuthenticationFailure Failure { get; } = failure;
}
