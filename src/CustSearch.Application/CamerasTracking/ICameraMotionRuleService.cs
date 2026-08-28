using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Enums;

namespace CustSearch.Application.CamerasTracking;

public interface ICameraMotionRuleService
{
    IReadOnlyList<MotionRuleCatalogItem>GetCatalog();
    Task<CameraMotionSettingsView>GetSettingsAsync(long cameraId,CancellationToken ct=default);
    Task<CameraMotionSettingsView>SetSettingsAsync(long cameraId,bool enabled,TenantAuditContext audit,CancellationToken ct=default);
    Task<IReadOnlyList<CameraMotionRuleView>>ListAsync(long cameraId,CancellationToken ct=default);
    Task<CameraMotionRuleView>SaveAsync(long cameraId,long?ruleId,SaveCameraMotionRuleCommand command,TenantAuditContext audit,CancellationToken ct=default);
}

public sealed record MotionRuleCatalogItem(string RuleCode,string Name,string Group,bool ZoneRequired,bool IsAvailable,string Description);
public sealed record CameraMotionSettingsView(long CameraId,bool MotionRulesEnabled);
public sealed record CameraMotionRuleView(long Id,long CameraId,string RuleCode,string RuleName,bool IsEnabled,decimal MinimumConfidence,int Sensitivity,int MinimumDurationSeconds,int CooldownSeconds,TimeOnly?StartTime,TimeOnly?EndTime,string DaysOfWeek,bool EvidenceSnapshotEnabled,bool EvidenceClipEnabled,int EvidencePreEventSeconds,int EvidencePostEventSeconds,AlertSeverity Severity,bool CreateAlert,bool RealtimeNotificationEnabled,bool ZoneRequired,long?ZoneId,DateTime CreatedUtc,DateTime UpdatedUtc);
public sealed record SaveCameraMotionRuleCommand(string RuleCode,string RuleName,bool IsEnabled,decimal MinimumConfidence,int Sensitivity,int MinimumDurationSeconds,int CooldownSeconds,TimeOnly?StartTime,TimeOnly?EndTime,string DaysOfWeek,bool EvidenceSnapshotEnabled,bool EvidenceClipEnabled,int EvidencePreEventSeconds,int EvidencePostEventSeconds,AlertSeverity Severity,bool CreateAlert,bool RealtimeNotificationEnabled,long?ZoneId);
