using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CustSearch.Application.Authentication;
using CustSearch.Contracts.Common;
using CustSearch.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace CustSearch.API.Controllers;

[ApiController]
[Route("api/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController(
    IAuthenticationService authenticationService,
    ICurrentUserContext currentUser,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await authenticationService.LoginAsync(
                new LoginCommand(
                    request.TenantCode,
                    request.UserName,
                    request.Password,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    HttpContext.TraceIdentifier),
                cancellationToken).ConfigureAwait(false);
            WriteRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresUtc);
            return Ok(ToResponse(result));
        }
        catch (AuthenticationFailureException exception)
        {
            return AuthenticationError(exception.Failure);
        }
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(_jwtOptions.RefreshCookie.Name, out var refreshToken)
            || string.IsNullOrWhiteSpace(refreshToken))
        {
            return AuthenticationError(AuthenticationFailure.InvalidRefreshToken);
        }

        try
        {
            var result = await authenticationService.RefreshAsync(
                refreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.TraceIdentifier,
                cancellationToken).ConfigureAwait(false);
            WriteRefreshCookie(result.RefreshToken, result.RefreshTokenExpiresUtc);
            return Ok(ToResponse(result));
        }
        catch (AuthenticationFailureException exception)
        {
            DeleteRefreshCookie();
            return AuthenticationError(exception.Failure);
        }
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (Request.Cookies.TryGetValue(_jwtOptions.RefreshCookie.Name, out var refreshToken)
            && !string.IsNullOrWhiteSpace(refreshToken))
        {
            await authenticationService.LogoutAsync(
                refreshToken,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.TraceIdentifier,
                cancellationToken).ConfigureAwait(false);
        }

        DeleteRefreshCookie();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [DisableRateLimiting]
    [ProducesResponseType<CurrentSessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorResponse>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentSessionResponse>> Me(CancellationToken cancellationToken)
    {
        try
        {
            var user = await authenticationService.GetCurrentUserAsync(
                currentUser.UserId,
                currentUser.SecurityStamp,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                HttpContext.TraceIdentifier,
                cancellationToken).ConfigureAwait(false);
            return Ok(new CurrentSessionResponse(user, GetAccessTokenExpiryUtc()));
        }
        catch (AuthenticationFailureException exception)
        {
            return AuthenticationError(exception.Failure);
        }
    }

    private void WriteRefreshCookie(string token, DateTime expiresUtc) =>
        Response.Cookies.Append(_jwtOptions.RefreshCookie.Name, token, CreateCookieOptions(expiresUtc));

    private void DeleteRefreshCookie() => Response.Cookies.Delete(
        _jwtOptions.RefreshCookie.Name,
        CreateCookieOptions(DateTime.UnixEpoch));

    private CookieOptions CreateCookieOptions(DateTime expiresUtc) => new()
    {
        HttpOnly = _jwtOptions.RefreshCookie.HttpOnly,
        Secure = _jwtOptions.RefreshCookie.Secure,
        SameSite = Enum.Parse<SameSiteMode>(_jwtOptions.RefreshCookie.SameSite, true),
        Path = _jwtOptions.RefreshCookie.Path,
        Expires = new DateTimeOffset(expiresUtc),
        IsEssential = true,
    };

    private UnauthorizedObjectResult AuthenticationError(AuthenticationFailure failure) => Unauthorized(
        new ApiErrorResponse(
            failure.ToString(),
            "Authentication could not be completed.",
            HttpContext.TraceIdentifier));

    private DateTime GetAccessTokenExpiryUtc()
    {
        var expiryClaim = User.FindFirstValue(JwtRegisteredClaimNames.Exp);
        if (!long.TryParse(expiryClaim, NumberStyles.None, CultureInfo.InvariantCulture, out var unixSeconds))
        {
            throw new InvalidOperationException("The validated access token has no valid expiry claim.");
        }

        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime;
    }

    private static AuthResponse ToResponse(AuthenticationResult result) =>
        new(result.AccessToken, result.AccessTokenExpiresUtc, result.User);
}

public sealed record LoginRequest(
    string? TenantCode,
    [param: Required, MinLength(1), MaxLength(100)] string UserName,
    [param: Required, MinLength(1), MaxLength(500)] string Password);

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresUtc,
    AuthenticatedUser User);

public sealed record CurrentSessionResponse(
    AuthenticatedUser User,
    DateTime AccessTokenExpiresUtc);
