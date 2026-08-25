using System.Data;
using System.Text.Json;
using CustSearch.Application.Abstractions.Data;
using CustSearch.Application.ReportsExports;
using CustSearch.Domain.Enums;
using Dapper;

namespace CustSearch.Infrastructure.ReportsExports;

/// <summary>Executes allowlisted report procedures with server-derived tenant and store scope.</summary>
public sealed class DapperReportQueryRepository(IDbConnectionFactory connections):IReportQueryRepository
{
    public async Task<ReportResultView>QueryTenantAsync(long tenantId,ReportType reportType,long[]authorizedStoreIds,ReportFilter filter,CancellationToken ct=default)
    {ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);if(reportType==ReportType.PlatformTenants)throw new ReportExportException("Platform reports require platform scope.",ReportExportFailureKind.Forbidden);var normalized=filter.Normalize();var parameters=new DynamicParameters();parameters.Add("TenantId",tenantId,DbType.Int64);parameters.Add("ReportType",(byte)reportType,DbType.Byte);parameters.Add("StoreIdsJson",JsonSerializer.Serialize(authorizedStoreIds),DbType.String);parameters.Add("FromUtc",normalized.FromUtc,DbType.DateTime2);parameters.Add("ToUtc",normalized.ToUtc,DbType.DateTime2);parameters.Add("PageNumber",normalized.Page,DbType.Int32);parameters.Add("PageSize",normalized.PageSize,DbType.Int32);await using var connection=await connections.OpenConnectionAsync(ct).ConfigureAwait(false);var rows=(await connection.QueryAsync<ProcedureRow>(new CommandDefinition("dbo.Report_TenantOperationalSummary",parameters,commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false)).ToList();return Map(reportType,normalized,rows);}
    public async Task<ReportResultView>QueryPlatformAsync(ReportType reportType,ReportFilter filter,CancellationToken ct=default)
    {if(reportType!=ReportType.PlatformTenants)throw new ReportExportException("Tenant reports require tenant scope.",ReportExportFailureKind.Forbidden);var normalized=filter.Normalize();var parameters=new DynamicParameters();parameters.Add("FromUtc",normalized.FromUtc,DbType.DateTime2);parameters.Add("ToUtc",normalized.ToUtc,DbType.DateTime2);parameters.Add("PageNumber",normalized.Page,DbType.Int32);parameters.Add("PageSize",normalized.PageSize,DbType.Int32);await using var connection=await connections.OpenConnectionAsync(ct).ConfigureAwait(false);var rows=(await connection.QueryAsync<ProcedureRow>(new CommandDefinition("dbo.Report_PlatformTenantSummary",parameters,commandType:CommandType.StoredProcedure,cancellationToken:ct)).ConfigureAwait(false)).ToList();return Map(reportType,normalized,rows);}
    private static ReportResultView Map(ReportType type,ReportFilter filter,List<ProcedureRow>rows)=>new(type,filter.FromUtc,filter.ToUtc,filter.Page,filter.PageSize,rows.FirstOrDefault()?.TotalRows??0,rows.Select(x=>new ReportDataRow(x.Domain,x.StoreId,x.Metric,x.Value,x.Label,x.OccurredUtc)).ToList());
    private sealed class ProcedureRow{public long TotalRows{get;set;}public string Domain{get;set;}=string.Empty;public long?StoreId{get;set;}public string Metric{get;set;}=string.Empty;public decimal Value{get;set;}public string?Label{get;set;}public DateTime?OccurredUtc{get;set;}}
}
