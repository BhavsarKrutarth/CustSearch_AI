using CustSearch.Application.TenantOperations;

namespace CustSearch.Application.CamerasTracking;

public interface ICameraPreviewService
{
    Task<IReadOnlyList<CameraPreviewGrantView>> ListGrantsAsync(long cameraId,CancellationToken ct=default);
    Task<CameraPreviewGrantView> SaveGrantAsync(long cameraId,long userId,SaveCameraPreviewGrantCommand command,TenantAuditContext audit,CancellationToken ct=default);
    Task RemoveGrantAsync(long cameraId,long userId,TenantAuditContext audit,CancellationToken ct=default);
    Task<CameraPreviewSessionView> StartSessionAsync(long cameraId,TenantAuditContext audit,CancellationToken ct=default);
    Task<CameraPreviewFrame> GetFrameAsync(long cameraId,Guid sessionId,CancellationToken ct=default);
    Task EndSessionAsync(long cameraId,Guid sessionId,TenantAuditContext audit,CancellationToken ct=default);
}

public interface ICameraFrameSource
{
    Task<CameraPreviewFrame> GetLatestFrameAsync(string configurationReference,CancellationToken ct=default);
}

public sealed record SaveCameraPreviewGrantCommand(bool CanViewLive,bool CanViewTracking,bool CanControl,DateTime?ValidUntilUtc,bool IsActive);
public sealed record CameraPreviewGrantView(long CameraId,long StoreId,long UserId,string UserName,string DisplayName,bool CanViewLive,bool CanViewTracking,bool CanControl,DateTime?ValidUntilUtc,bool IsActive);
public sealed record CameraPreviewSessionView(Guid SessionId,long CameraId,DateTime ExpiresUtc,string FrameUrl,int RefreshMilliseconds);
public sealed record CameraPreviewFrame(byte[] Content,string ContentType,DateTime CapturedUtc,int Width,int Height);

public sealed class CctvPreviewOptions
{
    public const string SectionName="CctvPreview";
    public bool Enabled{get;set;}
    public string AiServiceBaseUrl{get;set;}="http://127.0.0.1:8000";
    public string ApiKey{get;set;}=string.Empty;
    public int SessionLifetimeMinutes{get;set;}=10;
    public int FrameRefreshMilliseconds{get;set;}=750;
    public int RequestTimeoutSeconds{get;set;}=10;
    public bool IsValid()=>SessionLifetimeMinutes is>=1 and<=60&&FrameRefreshMilliseconds is>=250 and<=5000&&RequestTimeoutSeconds is>=1 and<=30&&Uri.TryCreate(AiServiceBaseUrl,UriKind.Absolute,out var uri)&&uri.Scheme is "http" or "https"&&(!Enabled||!string.IsNullOrWhiteSpace(ApiKey));
}
