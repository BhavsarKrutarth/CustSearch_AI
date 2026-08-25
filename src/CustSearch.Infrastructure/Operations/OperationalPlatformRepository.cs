using System.Data;
using System.Text.Json;
using CustSearch.Application.Abstractions.Data;
using CustSearch.Application.Operations;
using Dapper;

namespace CustSearch.Infrastructure.Operations;

public sealed class OperationalPlatformRepository(IDbConnectionFactory connectionFactory):IOperationalPlatformRepository
{
    public async Task<IReadOnlyList<SystemSettingView>>ListSettingsAsync(long?tenantId,long?storeId,bool effective,CancellationToken ct=default)
    {await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);var rows=await c.QueryAsync<SystemSettingView>(new CommandDefinition("dbo.SystemSetting_List",new{TenantId=tenantId,StoreId=storeId,IncludeInherited=effective},commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);return rows.AsList();}
    public async Task<SystemSettingView>SaveSettingAsync(long?tenantId,long?storeId,SaveSystemSettingCommand command,CustSearch.Application.TenantOperations.TenantAuditContext audit,CancellationToken ct=default)
    {await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);return await c.QuerySingleAsync<SystemSettingView>(new CommandDefinition("dbo.SystemSetting_Upsert",new{TenantId=tenantId,StoreId=storeId,command.SettingKey,ValueType=(byte)command.ValueType,command.SettingValue,command.Description,UpdatedByUserId=audit.ActorUserId,audit.IpAddress,audit.UserAgent,audit.CorrelationId},commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);}
    public async Task<AuditLogPage>SearchAuditAsync(long?tenantId,IReadOnlyCollection<long>allowedStoreIds,bool tenantWide,AuditLogQuery query,CancellationToken ct=default)
    {await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);var rows=(await c.QueryAsync<AuditLogItem>(new CommandDefinition("dbo.AuditLog_Search",new{TenantId=tenantId,AllowedStoreIdsJson=tenantId is null?null:JsonSerializer.Serialize(allowedStoreIds.OrderBy(x=>x)),TenantWide=tenantWide,query.Action,query.EntityType,query.FromUtc,query.ToUtc,query.PageNumber,query.PageSize},commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false)).AsList();return new(rows,rows.FirstOrDefault()?.TotalCount??0,query.PageNumber,query.PageSize);}
    public async Task<SystemHealthView>GetHealthAsync(int workerWarningSeconds,CancellationToken ct=default)
    {await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);using var multi=await c.QueryMultipleAsync(new CommandDefinition("dbo.SystemHealth_Get",new{WorkerWarningSeconds=workerWarningSeconds},commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);var database=await multi.ReadSingleAsync<DatabaseHealth>().ConfigureAwait(false);var workers=(await multi.ReadAsync<WorkerHealth>().ConfigureAwait(false)).AsList();var queues=await multi.ReadSingleAsync<QueueHealth>().ConfigureAwait(false);var cameras=await multi.ReadSingleAsync<CameraHealth>().ConfigureAwait(false);return new(database,workers,queues,cameras);}
    public async Task WriteHeartbeatAsync(WorkerHeartbeat heartbeat,CancellationToken ct=default)
    {await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);await c.ExecuteAsync(new CommandDefinition("dbo.WorkerHeartbeat_Upsert",heartbeat,commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);}
    public async Task<RetentionRunResult>RunRetentionAsync(int batchSize,int recognitionMetadataRetentionDays,CancellationToken ct=default)
    {await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);return await c.QuerySingleAsync<RetentionRunResult>(new CommandDefinition("dbo.OperationalRetention_Run",new{BatchSize=batchSize,RecognitionMetadataRetentionDays=recognitionMetadataRetentionDays},commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);}
}
