using System.Globalization;
using CustSearch.Application.Authentication;
using CustSearch.Application.Operations;
using CustSearch.Application.ShopperCustomers;
using CustSearch.Application.TenantOperations;

namespace CustSearch.Infrastructure.Operations;

public sealed class OperationalPlatformService(IOperationalPlatformRepository repository,ICurrentUserContext currentUser):IOperationalPlatformService
{
    private static readonly HashSet<string> LockedTenantKeys=new(["AutoLinkHouseholdFromFaceSimilarity"],StringComparer.OrdinalIgnoreCase);
    public Task<IReadOnlyList<SystemSettingView>>ListPlatformSettingsAsync(CancellationToken ct=default){RequirePlatform();return repository.ListSettingsAsync(null,null,true,ct);}
    public Task<IReadOnlyList<SystemSettingView>>ListTenantSettingsAsync(long?storeId,bool effective,CancellationToken ct=default){var tenant=RequireTenant();ValidateStore(storeId);return repository.ListSettingsAsync(tenant,storeId,effective,ct);}
    public Task<SystemSettingView>SavePlatformSettingAsync(SaveSystemSettingCommand command,TenantAuditContext audit,CancellationToken ct=default){RequirePlatform();Validate(command);ArgumentNullException.ThrowIfNull(audit);return repository.SaveSettingAsync(null,null,command,audit,ct);}
    public Task<SystemSettingView>SaveTenantSettingAsync(long?storeId,SaveSystemSettingCommand command,TenantAuditContext audit,CancellationToken ct=default){var tenant=RequireTenant();ValidateStore(storeId);Validate(command);ArgumentNullException.ThrowIfNull(audit);if(LockedTenantKeys.Contains(command.SettingKey))throw new OperationalException("This safety setting is platform-controlled.",OperationalFailureKind.Forbidden);return repository.SaveSettingAsync(tenant,storeId,command,audit,ct);}
    public Task<AuditLogPage>SearchPlatformAuditAsync(AuditLogQuery query,CancellationToken ct=default){RequirePlatform();Validate(query,false);return repository.SearchAuditAsync(null,Array.Empty<long>(),true,query,ct);}
    public Task<AuditLogPage>SearchTenantAuditAsync(AuditLogQuery query,CancellationToken ct=default){var tenant=RequireTenant();Validate(query,true);ValidateStore(query.StoreId);return repository.SearchAuditAsync(tenant,currentUser.StoreIds,TenantWide(),query,ct);}
    public Task<SystemHealthView>GetSystemHealthAsync(CancellationToken ct=default){RequirePlatform();return repository.GetHealthAsync(120,ct);}
    private void RequirePlatform(){if(!currentUser.IsAuthenticated||!currentUser.IsPlatformAdmin||currentUser.TenantId is not null)throw new OperationalException("Platform context is required.",OperationalFailureKind.Forbidden);}
    private long RequireTenant()=>currentUser.IsAuthenticated&&!currentUser.IsPlatformAdmin&&currentUser.TenantId is>0 and var id?id:throw new OperationalException("Tenant context is required.",OperationalFailureKind.Forbidden);
    private bool TenantWide()=>PhaseSixAccessRules.IsTenantWide(currentUser.Roles);
    private void ValidateStore(long?storeId){if(storeId.HasValue&&!PhaseSixAccessRules.CanAccessStore(storeId.Value,currentUser.StoreIds,TenantWide()))throw new OperationalException("Store was not found.",OperationalFailureKind.NotFound);}
    private static void Validate(SaveSystemSettingCommand command){ArgumentNullException.ThrowIfNull(command);if(string.IsNullOrWhiteSpace(command.SettingKey)||command.SettingKey.Length>100||!command.SettingKey.All(x=>char.IsAsciiLetterOrDigit(x)||x is '.' or '_' or '-')||!Enum.IsDefined(command.ValueType)||command.SettingValue is null||command.SettingValue.Length>1000)throw new OperationalException("Setting input is invalid.",OperationalFailureKind.Validation);_ = command.ValueType switch{SystemSettingValueType.Toggle when command.SettingValue is "true" or "false"=>true,SystemSettingValueType.WholeNumber when long.TryParse(command.SettingValue,NumberStyles.Integer,CultureInfo.InvariantCulture,out _)=>true,SystemSettingValueType.Numeric when decimal.TryParse(command.SettingValue,NumberStyles.Number,CultureInfo.InvariantCulture,out _)=>true,SystemSettingValueType.Text=>true,_=>throw new OperationalException("Setting value does not match its declared type.",OperationalFailureKind.Validation)};}
    private static void Validate(AuditLogQuery query,bool tenant){ArgumentNullException.ThrowIfNull(query);if(query.PageNumber<1||query.PageSize is<1 or>200||(query.FromUtc.HasValue&&query.ToUtc.HasValue&&query.FromUtc>=query.ToUtc)||(!tenant&&query.StoreId.HasValue))throw new OperationalException("Audit query is invalid.",OperationalFailureKind.Validation);}
}
