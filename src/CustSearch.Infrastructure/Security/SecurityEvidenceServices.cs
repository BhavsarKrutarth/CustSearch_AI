using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CustSearch.Application.Security;
using Microsoft.Extensions.Options;

namespace CustSearch.Infrastructure.Security;

public sealed class SecurityEvidenceTokenService(IOptions<SecurityEvidenceOptions>options):ISecurityEvidenceTokenService
{
    private readonly byte[]key=Encoding.UTF8.GetBytes(options.Value.DownloadSigningKey);
    public SecurityEvidenceTicket Create(long evidenceId,long incidentId,long userId,long tenantId,bool isExport,DateTime expiresUtc)
    {var payload=string.Join('.',tenantId,incidentId,evidenceId,userId,isExport?1:0,new DateTimeOffset(expiresUtc).ToUnixTimeSeconds());var signature=Convert.ToHexString(HMACSHA256.HashData(key,Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();return new($"{payload}.{signature}",expiresUtc);}
    public void Validate(string token,long evidenceId,long incidentId,long userId,long tenantId,bool isExport,DateTime utcNow)
    {var parts=token.Split('.');if(parts.Length!=7||!long.TryParse(parts[0],NumberStyles.None,CultureInfo.InvariantCulture,out var t)||!long.TryParse(parts[1],out var i)||!long.TryParse(parts[2],out var e)||!long.TryParse(parts[3],out var u)||!int.TryParse(parts[4],out var x)||!long.TryParse(parts[5],out var expiry)||t!=tenantId||i!=incidentId||e!=evidenceId||u!=userId||x!=(isExport?1:0)||DateTimeOffset.FromUnixTimeSeconds(expiry).UtcDateTime<utcNow)throw new SecurityException("Evidence access token is invalid or expired.",SecurityFailureKind.Unauthorized);var payload=string.Join('.',parts[..6]);var expected=HMACSHA256.HashData(key,Encoding.UTF8.GetBytes(payload));byte[]supplied;try{supplied=Convert.FromHexString(parts[6]);}catch(FormatException){throw new SecurityException("Evidence access token is invalid or expired.",SecurityFailureKind.Unauthorized);}if(supplied.Length!=expected.Length||!CryptographicOperations.FixedTimeEquals(supplied,expected))throw new SecurityException("Evidence access token is invalid or expired.",SecurityFailureKind.Unauthorized);}
}

/// <summary>Reads AES-GCM evidence envelopes: 12-byte nonce, 16-byte tag, then ciphertext.</summary>
public sealed class LocalEncryptedSecurityEvidenceStore(IOptions<SecurityEvidenceOptions>options):ISecurityEvidenceStore
{
    private readonly string root=Path.GetFullPath(options.Value.StoragePath);
    private readonly byte[]key=Convert.FromBase64String(options.Value.EncryptionKeyBase64);
    public async Task<Stream>OpenDecryptedAsync(string objectKey,CancellationToken ct=default)
    {var path=Resolve(objectKey);if(!File.Exists(path))throw new SecurityException("Evidence file is unavailable.",SecurityFailureKind.NotFound);var envelope=await File.ReadAllBytesAsync(path,ct).ConfigureAwait(false);if(envelope.Length<29)throw new SecurityException("Evidence envelope is invalid.",SecurityFailureKind.Unavailable);var plaintext=new byte[envelope.Length-28];try{using var aes=new AesGcm(key,16);aes.Decrypt(envelope.AsSpan(0,12),envelope.AsSpan(28),envelope.AsSpan(12,16),plaintext);}catch(CryptographicException){throw new SecurityException("Evidence integrity validation failed.",SecurityFailureKind.Unavailable);}return new MemoryStream(plaintext,writable:false);}
    public Task DeleteAsync(string objectKey,CancellationToken ct=default){ct.ThrowIfCancellationRequested();var path=Resolve(objectKey);if(File.Exists(path))File.Delete(path);return Task.CompletedTask;}
    private string Resolve(string objectKey){if(string.IsNullOrWhiteSpace(objectKey)||Path.IsPathRooted(objectKey)||objectKey.Contains("..",StringComparison.Ordinal)||objectKey.Contains(':'))throw new SecurityException("Evidence object key is invalid.",SecurityFailureKind.Validation);var path=Path.GetFullPath(Path.Combine(root,objectKey.Replace('/',Path.DirectorySeparatorChar)));var prefix=root.EndsWith(Path.DirectorySeparatorChar)?root:root+Path.DirectorySeparatorChar;if(!path.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new SecurityException("Evidence path is outside protected storage.",SecurityFailureKind.Forbidden);return path;}
}

public sealed class NullSecurityRealtimePublisher:ISecurityRealtimePublisher{public Task PublishAsync(SecurityRealtimeEvent message,CancellationToken ct=default)=>Task.CompletedTask;}
