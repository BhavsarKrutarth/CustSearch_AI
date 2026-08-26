using System.Globalization;
using System.Text.Json;
using CustSearch.Application.Authentication;
using CustSearch.Application.CamerasTracking;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Domain.Enums;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CustSearch.Infrastructure.CamerasTracking;

/// <summary>Authorizes every preview operation from authoritative tenant, store and user-camera data.</summary>
public sealed class CameraPreviewService(CustSearchDbContext db,ICurrentUserContext currentUser,ICameraFrameSource frames,TimeProvider clock,IOptions<CctvPreviewOptions>options):ICameraPreviewService
{
    private static readonly string[] TenantWideRoles=["TENANTADMIN","TENANTOWNER","SHOPOWNER"];
    private readonly CctvPreviewOptions preview=options.Value;

    public async Task<IReadOnlyList<CameraPreviewGrantView>> ListGrantsAsync(long cameraId,CancellationToken ct=default)
    {
        var camera=await RequireCameraAsync(cameraId,ct).ConfigureAwait(false);RequireStore(camera.StoreId);
        return await(from grant in db.CameraUserPreviewGrants.AsNoTracking() join user in db.UserAccounts.AsNoTracking() on grant.UserId equals user.Id where grant.TenantId==camera.TenantId&&grant.CameraId==camera.Id orderby user.UserName select new CameraPreviewGrantView(grant.CameraId,grant.StoreId,grant.UserId,user.UserName,user.DisplayName,grant.CanViewLive,grant.CanViewTracking,grant.CanControl,grant.ValidUntilUtc,grant.IsActive)).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<CameraPreviewGrantView> SaveGrantAsync(long cameraId,long userId,SaveCameraPreviewGrantCommand command,TenantAuditContext audit,CancellationToken ct=default)
    {
        ArgumentNullException.ThrowIfNull(command);ArgumentNullException.ThrowIfNull(audit);var camera=await RequireCameraAsync(cameraId,ct).ConfigureAwait(false);RequireStore(camera.StoreId);var tenant=RequireTenant();var target=await db.UserAccounts.SingleOrDefaultAsync(x=>x.Id==userId&&x.TenantId==tenant&&x.IsActive,ct).ConfigureAwait(false)??throw new CameraTrackingException("Tenant user was not found.",CameraTrackingFailureKind.NotFound);
        var targetTenantWide=await(from assignment in db.UserRoles join role in db.Roles on assignment.RoleId equals role.Id where assignment.UserId==userId&&role.TenantId==tenant&&role.IsActive&&TenantWideRoles.Contains(role.NormalizedName) select role.Id).AnyAsync(ct).ConfigureAwait(false);
        var targetStore=targetTenantWide||await db.UserStoreAssignments.AnyAsync(x=>x.TenantId==tenant&&x.UserId==userId&&x.StoreId==camera.StoreId,ct).ConfigureAwait(false);if(!targetStore)throw new CameraTrackingException("User is not assigned to the camera store.",CameraTrackingFailureKind.Forbidden);
        var now=clock.GetUtcNow().UtcDateTime;if(command.ValidUntilUtc is not null&&(command.ValidUntilUtc.Value.Kind!=DateTimeKind.Utc||command.ValidUntilUtc<=now))throw new CameraTrackingException("Preview grant expiry must be a future UTC timestamp.",CameraTrackingFailureKind.Validation);if((command.CanViewTracking||command.CanControl)&&!command.CanViewLive)throw new CameraTrackingException("Tracking/control access requires live preview access.",CameraTrackingFailureKind.Validation);
        var grant=await db.CameraUserPreviewGrants.SingleOrDefaultAsync(x=>x.TenantId==tenant&&x.CameraId==cameraId&&x.UserId==userId,ct).ConfigureAwait(false);if(grant is null){grant=CameraUserPreviewGrant.Create(tenant,camera.StoreId,cameraId,userId,command.CanViewTracking,command.CanControl,command.ValidUntilUtc,audit.ActorUserId,now);grant.Update(command.CanViewLive,command.CanViewTracking,command.CanControl,command.ValidUntilUtc,command.IsActive,now);db.CameraUserPreviewGrants.Add(grant);}else grant.Update(command.CanViewLive,command.CanViewTracking,command.CanControl,command.ValidUntilUtc,command.IsActive,now);
        Audit(audit,tenant,camera.StoreId,"CameraPreviewGrantSaved","CameraUserPreviewGrant",userId,new{cameraId,userId,command.CanViewLive,command.CanViewTracking,command.CanControl,command.ValidUntilUtc,command.IsActive},now);await db.SaveChangesAsync(ct).ConfigureAwait(false);return new(cameraId,camera.StoreId,userId,target.UserName,target.DisplayName,grant.CanViewLive,grant.CanViewTracking,grant.CanControl,grant.ValidUntilUtc,grant.IsActive);
    }

    public async Task RemoveGrantAsync(long cameraId,long userId,TenantAuditContext audit,CancellationToken ct=default)
    {
        var camera=await RequireCameraAsync(cameraId,ct).ConfigureAwait(false);RequireStore(camera.StoreId);var tenant=RequireTenant();var grant=await db.CameraUserPreviewGrants.SingleOrDefaultAsync(x=>x.TenantId==tenant&&x.CameraId==cameraId&&x.UserId==userId,ct).ConfigureAwait(false)??throw new CameraTrackingException("Preview grant was not found.",CameraTrackingFailureKind.NotFound);db.CameraUserPreviewGrants.Remove(grant);var now=clock.GetUtcNow().UtcDateTime;var sessions=await db.CameraPreviewSessions.Where(x=>x.TenantId==tenant&&x.CameraId==cameraId&&x.UserId==userId&&x.Status==CameraPreviewSessionStatus.Active).ToListAsync(ct).ConfigureAwait(false);foreach(var session in sessions)session.End(now);Audit(audit,tenant,camera.StoreId,"CameraPreviewGrantRemoved","CameraUserPreviewGrant",userId,new{cameraId,userId},now);await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<CameraPreviewSessionView> StartSessionAsync(long cameraId,TenantAuditContext audit,CancellationToken ct=default)
    {
        if(!preview.Enabled)throw new CameraTrackingException("Live camera preview is not enabled on this server.",CameraTrackingFailureKind.Unavailable);var camera=await RequireCameraAsync(cameraId,ct).ConfigureAwait(false);RequireStore(camera.StoreId);if(!camera.IsActive)throw new CameraTrackingException("Camera is inactive.",CameraTrackingFailureKind.Conflict);await RequireGrantAsync(camera,currentUser.UserId,ct).ConfigureAwait(false);var now=clock.GetUtcNow().UtcDateTime;var session=CameraPreviewSession.Start(camera.TenantId,camera.StoreId,camera.Id,currentUser.UserId,now,TimeSpan.FromMinutes(preview.SessionLifetimeMinutes));db.CameraPreviewSessions.Add(session);Audit(audit,camera.TenantId,camera.StoreId,"CameraPreviewStarted","CameraPreviewSession",camera.Id,new{CameraId=camera.Id,SessionId=session.Id,session.ExpiresUtc},now);await db.SaveChangesAsync(ct).ConfigureAwait(false);return new(session.Id,camera.Id,session.ExpiresUtc,$"/api/tenant/cameras/{camera.Id}/preview-sessions/{session.Id}/frame",preview.FrameRefreshMilliseconds);
    }

    public async Task<CameraPreviewFrame> GetFrameAsync(long cameraId,Guid sessionId,CancellationToken ct=default)
    {
        if(!preview.Enabled)throw new CameraTrackingException("Live camera preview is not enabled on this server.",CameraTrackingFailureKind.Unavailable);var camera=await RequireCameraAsync(cameraId,ct).ConfigureAwait(false);RequireStore(camera.StoreId);var now=clock.GetUtcNow().UtcDateTime;var session=await db.CameraPreviewSessions.SingleOrDefaultAsync(x=>x.Id==sessionId&&x.TenantId==camera.TenantId&&x.CameraId==camera.Id&&x.UserId==currentUser.UserId,ct).ConfigureAwait(false)??throw new CameraTrackingException("Preview session was not found.",CameraTrackingFailureKind.NotFound);if(session.Status!=CameraPreviewSessionStatus.Active||session.ExpiresUtc<=now)throw new CameraTrackingException("Preview session has expired.",CameraTrackingFailureKind.Unauthorized);await RequireGrantAsync(camera,currentUser.UserId,ct).ConfigureAwait(false);if((now-session.LastAccessedUtc).TotalSeconds>=15){session.Touch(now);await db.SaveChangesAsync(ct).ConfigureAwait(false);}return await frames.GetLatestFrameAsync(camera.RtspConfigurationReference,ct).ConfigureAwait(false);
    }

    public async Task EndSessionAsync(long cameraId,Guid sessionId,TenantAuditContext audit,CancellationToken ct=default)
    {
        var camera=await RequireCameraAsync(cameraId,ct).ConfigureAwait(false);RequireStore(camera.StoreId);var session=await db.CameraPreviewSessions.SingleOrDefaultAsync(x=>x.Id==sessionId&&x.TenantId==camera.TenantId&&x.CameraId==camera.Id&&x.UserId==currentUser.UserId,ct).ConfigureAwait(false)??throw new CameraTrackingException("Preview session was not found.",CameraTrackingFailureKind.NotFound);var now=clock.GetUtcNow().UtcDateTime;session.End(now);Audit(audit,camera.TenantId,camera.StoreId,"CameraPreviewEnded","CameraPreviewSession",camera.Id,new{CameraId=camera.Id,SessionId=session.Id},now);await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    private async Task<Camera> RequireCameraAsync(long cameraId,CancellationToken ct){var tenant=RequireTenant();return await db.Cameras.SingleOrDefaultAsync(x=>x.Id==cameraId&&x.TenantId==tenant,ct).ConfigureAwait(false)??throw new CameraTrackingException("Camera was not found.",CameraTrackingFailureKind.NotFound);}
    private async Task RequireGrantAsync(Camera camera,long userId,CancellationToken ct){var now=clock.GetUtcNow().UtcDateTime;var allowed=await db.CameraUserPreviewGrants.AsNoTracking().AnyAsync(x=>x.TenantId==camera.TenantId&&x.StoreId==camera.StoreId&&x.CameraId==camera.Id&&x.UserId==userId&&x.IsActive&&x.CanViewLive&&(x.ValidUntilUtc==null||x.ValidUntilUtc>now),ct).ConfigureAwait(false);if(!allowed)throw new CameraTrackingException("Live preview is not assigned to this user.",CameraTrackingFailureKind.Forbidden);}
    private long RequireTenant()=>currentUser.IsAuthenticated&&!currentUser.IsPlatformAdmin&&currentUser.TenantId is>0?currentUser.TenantId.Value:throw new CameraTrackingException("Tenant session is required.",CameraTrackingFailureKind.Forbidden);
    private bool TenantWide()=>PhaseFiveAccessRules.ContainsTenantWideRole(currentUser.Roles);
    private void RequireStore(long storeId){if(!TenantWide()&&!currentUser.StoreIds.Contains(storeId))throw new CameraTrackingException("Store is outside the server-authorized scope.",CameraTrackingFailureKind.Forbidden);}
    private void Audit(TenantAuditContext audit,long tenant,long store,string action,string entity,long id,object value,DateTime now)=>db.AuditLogs.Add(AuditLog.Record(tenant,store,audit.ActorUserId,"User",action,entity,id.ToString(CultureInfo.InvariantCulture),null,JsonSerializer.Serialize(value),audit.IpAddress,audit.UserAgent,audit.CorrelationId,now));
}
