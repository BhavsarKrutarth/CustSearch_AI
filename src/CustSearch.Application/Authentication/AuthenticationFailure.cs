namespace CustSearch.Application.Authentication;

public enum AuthenticationFailure
{
    InvalidCredentials,
    InvalidRefreshToken,
    ExpiredRefreshToken,
    ReusedRefreshToken,
    UserDisabled,
    TenantUnavailable,
    SessionRevoked,
}
