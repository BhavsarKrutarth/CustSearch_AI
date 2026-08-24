using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using CustSearch.Application.Integrations;

namespace CustSearch.Integrations;

/// <summary>Generic configured HTTPS adapter with SSRF guards, bounded timeout, bearer references and outbound HMAC.</summary>
public sealed class HttpIntegrationTransport(HttpClient client,IIntegrationSecretResolver secrets):IIntegrationTransport
{
    public async Task<IntegrationTransportResult>SendAsync(IntegrationTransportRequest request,CancellationToken cancellationToken=default)
    {
        ArgumentNullException.ThrowIfNull(request);if(!Uri.TryCreate(request.Destination,UriKind.Absolute,out var destination)||destination.Scheme!=Uri.UriSchemeHttps||destination.IsLoopback||!string.IsNullOrEmpty(destination.UserInfo))return new(false,null,0,"destination","Outbound destination is not an allowed HTTPS endpoint.");if(!await IsPublicDestinationAsync(destination,cancellationToken).ConfigureAwait(false))return new(false,null,0,"destination","Outbound destination resolved to a non-public address.");var credential=request.CredentialReference is null?null:await secrets.ResolveAsync(request.CredentialReference,cancellationToken).ConfigureAwait(false);var signingSecret=request.SigningSecretReference is null?null:await secrets.ResolveAsync(request.SigningSecretReference,cancellationToken).ConfigureAwait(false);if(request.CredentialReference is not null&&string.IsNullOrWhiteSpace(credential))return new(false,null,0,"credential","Configured credential reference is unavailable.");if(request.SigningSecretReference is not null&&string.IsNullOrWhiteSpace(signingSecret))return new(false,null,0,"credential","Configured signing-secret reference is unavailable.");
        using var message=new HttpRequestMessage(HttpMethod.Post,destination);message.Content=new StringContent(request.PayloadJson,Encoding.UTF8,"application/json");message.Headers.TryAddWithoutValidation("X-CustSearch-Event",request.EventType);message.Headers.TryAddWithoutValidation("X-CustSearch-Event-Id",request.IdempotencyKey);message.Headers.TryAddWithoutValidation("X-CustSearch-Delivery-Id",request.OutboxId.ToString(CultureInfo.InvariantCulture));message.Headers.TryAddWithoutValidation("X-CustSearch-Version",request.ContractVersion.ToString(CultureInfo.InvariantCulture));message.Headers.TryAddWithoutValidation("X-Correlation-Id",request.CorrelationId);if(credential is not null)message.Headers.Authorization=new AuthenticationHeaderValue("Bearer",credential);if(signingSecret is not null){var timestamp=DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);var bytes=Encoding.UTF8.GetBytes($"{timestamp}\n{request.IdempotencyKey}\n{request.PayloadJson}");var signature=Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingSecret),bytes)).ToLowerInvariant();message.Headers.TryAddWithoutValidation("X-CustSearch-Timestamp",timestamp);message.Headers.TryAddWithoutValidation("X-CustSearch-Signature",$"sha256={signature}");}
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(Math.Clamp(request.TimeoutSeconds,1,120)));using var linked=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,timeout.Token);var stopwatch=Stopwatch.StartNew();try{using var response=await client.SendAsync(message,HttpCompletionOption.ResponseHeadersRead,linked.Token).ConfigureAwait(false);stopwatch.Stop();var status=(int)response.StatusCode;return response.IsSuccessStatusCode?new(true,status,stopwatch.ElapsedMilliseconds,null,null):new(false,status,stopwatch.ElapsedMilliseconds,status is>=400 and<=499?"client":"server","Remote endpoint rejected the delivery.");}catch(OperationCanceledException)when(timeout.IsCancellationRequested&&!cancellationToken.IsCancellationRequested){stopwatch.Stop();return new(false,408,stopwatch.ElapsedMilliseconds,"timeout","Outbound delivery timed out.");}catch(HttpRequestException){stopwatch.Stop();return new(false,null,stopwatch.ElapsedMilliseconds,"transport","Outbound transport failed.");}
    }
    private static async Task<bool>IsPublicDestinationAsync(Uri destination,CancellationToken cancellationToken){IPAddress[] addresses;try{addresses=await Dns.GetHostAddressesAsync(destination.DnsSafeHost,cancellationToken).ConfigureAwait(false);}catch(System.Net.Sockets.SocketException){return false;}catch(ArgumentException){return false;}return addresses.Length>0&&addresses.All(IsPublicAddress);}
    private static bool IsPublicAddress(IPAddress address)
    {
        if(address.IsIPv4MappedToIPv6)return IsPublicAddress(address.MapToIPv4());if(IPAddress.IsLoopback(address)||address.Equals(IPAddress.Any)||address.Equals(IPAddress.IPv6Any))return false;var bytes=address.GetAddressBytes();
        if(address.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork)return bytes[0] is not 0 and not 10 and not 127&&!(bytes[0]==100&&bytes[1]>=64&&bytes[1]<=127)&&!(bytes[0]==169&&bytes[1]==254)&&!(bytes[0]==172&&bytes[1]>=16&&bytes[1]<=31)&&!(bytes[0]==192&&bytes[1] is 0 or 168)&&!(bytes[0]==198&&bytes[1] is 18 or 19)&&bytes[0]<224;
        return !address.IsIPv6LinkLocal&&!address.IsIPv6SiteLocal&&!address.IsIPv6Multicast&&(bytes[0]&0xfe)!=0xfc;
    }
}
