using System.ComponentModel.DataAnnotations;

namespace CustSearch.Infrastructure.Security;

/// <summary>
/// Controls JWT and refresh-session lifetime behavior through validated configuration.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required, MinLength(3), MaxLength(200)]
    public string Issuer { get; init; } = string.Empty;

    [Required, MinLength(3), MaxLength(200)]
    public string Audience { get; init; } = string.Empty;

    [Required, MinLength(32)]
    public string SigningKey { get; init; } = string.Empty;

    [Range(1, 60)]
    public int AccessTokenLifetimeMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenLifetimeDays { get; init; } = 7;

    [Range(0, 300)]
    public int ClockSkewSeconds { get; init; } = 30;

    [Required]
    public RefreshCookieOptions RefreshCookie { get; init; } = new();
}

public sealed class RefreshCookieOptions
{
    [Required, MinLength(3), MaxLength(100)]
    public string Name { get; init; } = "custsearch_refresh";

    public bool Secure { get; init; } = true;

    public bool HttpOnly { get; init; } = true;

    [Required]
    public string SameSite { get; init; } = "Strict";

    [Required]
    public string Path { get; init; } = "/api/auth";
}
