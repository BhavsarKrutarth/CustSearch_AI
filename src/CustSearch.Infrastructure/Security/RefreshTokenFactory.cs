using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CustSearch.Infrastructure.Security;

/// <summary>
/// Generates cryptographically random refresh tokens and one-way SHA-256 storage hashes.
/// </summary>
public static class RefreshTokenFactory
{
    public static GeneratedRefreshToken Create()
    {
        var rawToken = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return new GeneratedRefreshToken(rawToken, Hash(rawToken));
    }

    public static string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}

public sealed record GeneratedRefreshToken(string RawToken, string TokenHash);
