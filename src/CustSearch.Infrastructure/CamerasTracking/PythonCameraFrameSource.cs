using System.Net.Http.Json;
using CustSearch.Application.CamerasTracking;
using Microsoft.Extensions.Options;

namespace CustSearch.Infrastructure.CamerasTracking;

/// <summary>Calls the internal Python frame boundary; configuration references and service keys stay server-side.</summary>
public sealed class PythonCameraFrameSource(HttpClient client,IOptions<CctvPreviewOptions>options):ICameraFrameSource
{
    private readonly CctvPreviewOptions preview=options.Value;
    public async Task<CameraPreviewFrame> GetLatestFrameAsync(string configurationReference,CancellationToken ct=default)
    {
        using var request=new HttpRequestMessage(HttpMethod.Post,"v1/cctv/cameras/frame"){Content=JsonContent.Create(new{configuration_reference=configurationReference,max_age_seconds=5})};request.Headers.Add("X-CustSearch-AI-Key",preview.ApiKey);using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,ct).ConfigureAwait(false);if(!response.IsSuccessStatusCode)throw new CameraTrackingException(response.StatusCode==System.Net.HttpStatusCode.ServiceUnavailable?"Camera frame is temporarily unavailable.":"Camera preview service rejected the frame request.",CameraTrackingFailureKind.Unavailable);var bytes=await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);if(bytes.Length==0||bytes.Length>2_000_000)throw new CameraTrackingException("Camera preview returned an invalid frame.",CameraTrackingFailureKind.Unavailable);var width=int.TryParse(response.Headers.GetValues("X-Frame-Width").FirstOrDefault(),out var w)?w:0;var height=int.TryParse(response.Headers.GetValues("X-Frame-Height").FirstOrDefault(),out var h)?h:0;var captured=DateTime.TryParse(response.Headers.GetValues("X-Frame-Captured-Utc").FirstOrDefault(),null,System.Globalization.DateTimeStyles.RoundtripKind,out var timestamp)?timestamp.ToUniversalTime():DateTime.UtcNow;return new(bytes,response.Content.Headers.ContentType?.MediaType??"image/jpeg",captured,width,height);
    }
}
