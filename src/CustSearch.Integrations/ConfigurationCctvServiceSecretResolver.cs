using CustSearch.Application.CamerasTracking;
using Microsoft.Extensions.Configuration;

namespace CustSearch.Integrations;

/// <summary>Resolves service secrets from environment/vault-backed configuration; values never enter SQL or API output.</summary>
public sealed class ConfigurationCctvServiceSecretResolver(IConfiguration configuration):ICctvServiceSecretResolver
{
    public ValueTask<CctvServiceCredential?>ResolveAsync(string serviceId,CancellationToken cancellationToken=default)
    {cancellationToken.ThrowIfCancellationRequested();if(string.IsNullOrWhiteSpace(serviceId)||serviceId.Length>100||serviceId.Any(x=>!(char.IsLetterOrDigit(x)||x is '-' or '_' or '.')))return ValueTask.FromResult<CctvServiceCredential?>(null);var prefix=$"CctvServices:{serviceId}";var secret=configuration[$"{prefix}:Secret"]??configuration[$"CCTV_SERVICE_{serviceId.ToUpperInvariant().Replace('-','_')}_SECRET"];if(string.IsNullOrWhiteSpace(secret)||!long.TryParse(configuration[$"{prefix}:TenantId"],out var tenantId)||tenantId<=0)return ValueTask.FromResult<CctvServiceCredential?>(null);var stores=(configuration[$"{prefix}:StoreIds"]??string.Empty).Split(',',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Select(x=>long.TryParse(x,out var id)?id:0).Where(x=>x>0).ToHashSet();var allowAll=configuration.GetValue<bool>($"{prefix}:AllowAllStores");if(stores.Count==0&&!allowAll)return ValueTask.FromResult<CctvServiceCredential?>(null);return ValueTask.FromResult<CctvServiceCredential?>(new(secret,tenantId,stores,allowAll));}
}
