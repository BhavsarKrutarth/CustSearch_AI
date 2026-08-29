using CustSearch.Application.Abstractions.Data;
using CustSearch.Application.CamerasTracking;
using Dapper;

namespace CustSearch.Infrastructure.Security;

public sealed class SecurityIncidentEvidenceLinkValidator(IDbConnectionFactory connections)
    : ISecurityIncidentEvidenceLinkValidator
{
    public async Task<bool> ExistsInScopeAsync(
        long tenantId,long storeId,long incidentId,CancellationToken ct=default)
    {
        await using var db=await connections.OpenConnectionAsync(ct).ConfigureAwait(false);
        return await db.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM dbo.SecurityIncidents WHERE TenantId=@TenantId AND StoreId=@StoreId AND Id=@IncidentId",
            new{TenantId=tenantId,StoreId=storeId,IncidentId=incidentId},cancellationToken:ct)).ConfigureAwait(false)>0;
    }
}
