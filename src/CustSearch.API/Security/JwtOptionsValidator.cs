using CustSearch.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace CustSearch.API.Security;

/// <summary>Validates security-sensitive JWT settings that data annotations cannot express.</summary>
public sealed class JwtOptionsValidator(IHostEnvironment environment) : IValidateOptions<JwtOptions>
{
    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();
        if (EncodingLength(options.SigningKey) < 32)
        {
            failures.Add("Jwt:SigningKey must contain at least 32 UTF-8 bytes.");
        }

        if (!Enum.TryParse<SameSiteMode>(options.RefreshCookie.SameSite, true, out _))
        {
            failures.Add("Jwt:RefreshCookie:SameSite must be Strict, Lax, None, or Unspecified.");
        }

        if (!options.RefreshCookie.HttpOnly)
        {
            failures.Add("Jwt:RefreshCookie:HttpOnly must remain enabled.");
        }

        if (!environment.IsDevelopment() && !options.RefreshCookie.Secure)
        {
            failures.Add("Jwt:RefreshCookie:Secure must be enabled outside Development.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static int EncodingLength(string? value) =>
        string.IsNullOrEmpty(value) ? 0 : System.Text.Encoding.UTF8.GetByteCount(value);
}
