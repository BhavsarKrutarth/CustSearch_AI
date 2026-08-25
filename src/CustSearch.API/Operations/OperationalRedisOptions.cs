using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CustSearch.API.Operations;

public sealed class OperationalRedisOptions
{
    public const string SectionName="Redis";
    public bool Enabled{get;set;}
    public string ConnectionString{get;set;}=string.Empty;
    public bool SignalRBackplaneEnabled{get;set;}
    public string InstanceName{get;set;}="CustSearch:";
}

public sealed class OperationalRedisOptionsValidator:IValidateOptions<OperationalRedisOptions>
{
    public ValidateOptionsResult Validate(string?name,OperationalRedisOptions options)
    {
        if((options.Enabled||options.SignalRBackplaneEnabled)&&string.IsNullOrWhiteSpace(options.ConnectionString))return ValidateOptionsResult.Fail("Redis:ConnectionString is required when Redis or its SignalR backplane is enabled.");
        if(options.SignalRBackplaneEnabled&&!options.Enabled)return ValidateOptionsResult.Fail("Redis must be enabled before the SignalR backplane can be enabled.");
        if(options.InstanceName.Length is<1 or>100)return ValidateOptionsResult.Fail("Redis:InstanceName must be between 1 and 100 characters.");
        return ValidateOptionsResult.Success;
    }
}

/// <summary>A read-only connectivity probe. Failure is degraded so optional Redis never hides SQL/API readiness.</summary>
public sealed class RedisDistributedCacheHealthCheck(IDistributedCache cache):IHealthCheck
{
    public async Task<HealthCheckResult>CheckHealthAsync(HealthCheckContext context,CancellationToken cancellationToken=default)
    {
        try{_ = await cache.GetAsync("health:connectivity-probe",cancellationToken).ConfigureAwait(false);return HealthCheckResult.Healthy("Redis is reachable.");}
        catch(Exception exception){return HealthCheckResult.Degraded("Redis is unavailable; uncached paths remain active.",exception);}
    }
}
