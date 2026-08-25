using System.Data;
using CustSearch.Application.Abstractions.Data;
using CustSearch.Application.ReportsExports;
using Dapper;

namespace CustSearch.Infrastructure.ReportsExports;

/// <summary>Dapper-only Phase 15 data path. Stored procedures own report filters and export state transitions.</summary>
public sealed class ReportsExportsRepository(IDbConnectionFactory connectionFactory):IReportsExportsRepository
{
    public Task<ReportDataView> QueryTenantAsync(long tenantId,IReadOnlyCollection<long>allowedStoreIds,bool tenantWide,string reportType,ReportFilter filter,int take,CancellationToken ct=default)
    {
        var p=new DynamicParameters();p.Add("TenantId",tenantId);p.Add("AllowedStoreIdsCsv",tenantWide?null:string.Join(',',allowedStoreIds.OrderBy(x=>x)));p.Add("ReportType",reportType);p.Add("StoreId",filter.StoreId);p.Add("FromUtc",filter.FromUtc);p.Add("ToUtc",filter.ToUtc);p.Add("Take",take);
        return QueryTableAsync("dbo.TenantReport_Get",p,ct);
    }
    public Task<ReportDataView> QueryPlatformAsync(string reportType,ReportFilter filter,int take,CancellationToken ct=default)
    {
        var p=new DynamicParameters();p.Add("ReportType",reportType);p.Add("TenantId",filter.TenantId);p.Add("FromUtc",filter.FromUtc);p.Add("ToUtc",filter.ToUtc);p.Add("Take",take);return QueryTableAsync("dbo.PlatformReport_Get",p,ct);
    }
    public async Task<ReportExportJobView>CreateJobAsync(long?tenantId,long requesterId,string reportType,string filterJson,ReportExportFormat format,ReportRequestContext request,CancellationToken ct=default)
    {
        await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);return await c.QuerySingleAsync<ReportExportJobView>(new CommandDefinition("dbo.ReportExportJob_Create",new{TenantId=tenantId,RequestedByUserId=requesterId,ReportType=reportType,FilterJson=filterJson,Format=(byte)format,request.IpAddress,request.UserAgent,request.CorrelationId},commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);
    }
    public async Task<IReadOnlyList<ReportExportJobView>>ListJobsAsync(long?tenantId,long requesterId,bool platform,ReportExportStatus?status,int take,CancellationToken ct=default)
    {
        await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);var rows=await c.QueryAsync<ReportExportJobView>(new CommandDefinition("dbo.ReportExportJob_List",new{RequestedByUserId=requesterId,TenantId=tenantId,IsPlatform=platform,Status=status is null?null:(byte?)status.Value,Take=take},commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);return rows.AsList();
    }
    public async Task<ReportExportJobView?>GetJobAsync(long jobId,long?tenantId,long requesterId,bool platform,CancellationToken ct=default)
    {
        await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);return await c.QuerySingleOrDefaultAsync<ReportExportJobView>(new CommandDefinition("dbo.ReportExportJob_Get",new{JobId=jobId,RequestedByUserId=requesterId,TenantId=tenantId,IsPlatform=platform},commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);
    }
    public async Task<ClaimedReportExportJob?>ClaimAsync(int leaseSeconds,CancellationToken ct=default)
    {
        await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);return await c.QuerySingleOrDefaultAsync<ClaimedReportExportJob>(new CommandDefinition("dbo.ReportExportJob_Claim",new{LeaseSeconds=leaseSeconds},commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);
    }
    public Task SetProgressAsync(long jobId,Guid leaseToken,byte progress,CancellationToken ct=default)=>ExecuteAsync("dbo.ReportExportJob_Progress",new{JobId=jobId,LeaseToken=leaseToken,ProgressPercent=progress},ct);
    public Task CompleteAsync(long jobId,Guid leaseToken,ReportArtifactMetadata a,int retentionHours,CancellationToken ct=default)=>ExecuteAsync("dbo.ReportExportJob_Complete",new{JobId=jobId,LeaseToken=leaseToken,a.StorageReference,a.DownloadFileName,a.ContentType,a.ContentLength,a.Sha256,RetentionHours=retentionHours},ct);
    public Task FailAsync(long jobId,Guid leaseToken,string safeError,CancellationToken ct=default)=>ExecuteAsync("dbo.ReportExportJob_Fail",new{JobId=jobId,LeaseToken=leaseToken,ErrorMessage=safeError},ct);
    public async Task<IReadOnlyList<ExpiredReportArtifact>>ExpireArtifactsAsync(int take,CancellationToken ct=default){await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);var rows=await c.QueryAsync<ExpiredReportArtifact>(new CommandDefinition("dbo.ReportExportJob_Expire",new{Take=take},commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);return rows.AsList();}
    public Task AcknowledgeArtifactDeletedAsync(long jobId,string storageReference,CancellationToken ct=default)=>ExecuteAsync("dbo.ReportExportJob_ArtifactDeleted",new{JobId=jobId,StorageReference=storageReference},ct);
    public async Task<ReportRequesterScope>GetRequesterScopeAsync(long?tenantId,long requesterId,string reportType,CancellationToken ct=default)
    {
        await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);using var multi=await c.QueryMultipleAsync(new CommandDefinition("dbo.ReportExportRequesterScope_Get",new{TenantId=tenantId,RequestedByUserId=requesterId,ReportType=reportType},commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);var header=await multi.ReadSingleOrDefaultAsync<RequesterScopeHeader>().ConfigureAwait(false)??throw new UnauthorizedAccessException("The report requester is no longer authorized.");var stores=(await multi.ReadAsync<long>().ConfigureAwait(false)).AsList();return new(header.TenantWide,stores);
    }
    public Task RecordAuditAsync(long?tenantId,long?storeId,long actorUserId,string action,string entityType,string?entityId,string afterJson,ReportRequestContext request,CancellationToken ct=default)=>ExecuteAsync("dbo.ReportAudit_Write",new{TenantId=tenantId,StoreId=storeId,ActorUserId=actorUserId,Action=action,EntityType=entityType,EntityId=entityId,AfterJson=afterJson,request.IpAddress,request.UserAgent,request.CorrelationId},ct);
    private async Task<ReportDataView>QueryTableAsync(string procedure,DynamicParameters parameters,CancellationToken ct)
    {
        await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);
        await using var reader=await c.ExecuteReaderAsync(new CommandDefinition(procedure,parameters,commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);
        var columns=Enumerable.Range(0,reader.FieldCount).Select(reader.GetName).ToArray();
        var rows=new List<IReadOnlyDictionary<string,object?>>();
        while(await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var row=new Dictionary<string,object?>(columns.Length,StringComparer.Ordinal);
            for(var ordinal=0;ordinal<columns.Length;ordinal++)row[columns[ordinal]]=await reader.IsDBNullAsync(ordinal,ct).ConfigureAwait(false)?null:reader.GetValue(ordinal);
            rows.Add(row);
        }
        return new(columns,rows);
    }
    private async Task ExecuteAsync(string procedure,object parameters,CancellationToken ct){await using var c=await connectionFactory.OpenConnectionAsync(ct).ConfigureAwait(false);await c.ExecuteAsync(new CommandDefinition(procedure,parameters,commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false);}
    private sealed record RequesterScopeHeader(bool TenantWide);
}
