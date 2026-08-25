using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CustSearch.Application.ReportsExports;
using Microsoft.Extensions.Options;

namespace CustSearch.Infrastructure.ReportsExports;

public sealed class ExportDownloadTokenService(IOptions<ReportsExportsOptions>options):IExportDownloadTokenService
{
    private readonly byte[]key=Encoding.UTF8.GetBytes(options.Value.DownloadSigningKey);
    public ExportDownloadTicketView Create(long jobId,long requestedByUserId,long?tenantId,DateTime expiresUtc){RequireConfigured();var expiry=new DateTimeOffset(expiresUtc).ToUnixTimeSeconds();var payload=$"{jobId}.{requestedByUserId}.{tenantId?.ToString(CultureInfo.InvariantCulture)??"platform"}.{expiry}";var signature=Convert.ToHexString(HMACSHA256.HashData(key,Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();return new($"{payload}.{signature}",expiresUtc);}
    public void Validate(string token,long jobId,long requestedByUserId,long?tenantId,DateTime utcNow){RequireConfigured();ArgumentException.ThrowIfNullOrWhiteSpace(token);var parts=token.Split('.',StringSplitOptions.None);if(parts.Length!=5||!long.TryParse(parts[0],NumberStyles.None,CultureInfo.InvariantCulture,out var tokenJob)||!long.TryParse(parts[1],NumberStyles.None,CultureInfo.InvariantCulture,out var tokenUser)||!long.TryParse(parts[3],NumberStyles.None,CultureInfo.InvariantCulture,out var expiry))throw Forbidden();var tenantPart=tenantId?.ToString(CultureInfo.InvariantCulture)??"platform";var payload=string.Join('.',parts[..4]);var expected=Convert.ToHexString(HMACSHA256.HashData(key,Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();byte[]actualBytes;byte[]expectedBytes;try{actualBytes=Convert.FromHexString(parts[4]);expectedBytes=Convert.FromHexString(expected);}catch(FormatException){throw Forbidden();}if(tokenJob!=jobId||tokenUser!=requestedByUserId||!string.Equals(parts[2],tenantPart,StringComparison.Ordinal)||!CryptographicOperations.FixedTimeEquals(actualBytes,expectedBytes)||DateTimeOffset.FromUnixTimeSeconds(expiry).UtcDateTime<=utcNow)throw Forbidden();}
    private void RequireConfigured(){if(key.Length<32)throw new ReportExportException("Export download signing secret is not configured.",ReportExportFailureKind.Unavailable);}
    private static ReportExportException Forbidden()=>new("Export download access is invalid or expired.",ReportExportFailureKind.Forbidden);
}
