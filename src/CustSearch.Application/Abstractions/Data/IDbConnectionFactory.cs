using System.Data.Common;

namespace CustSearch.Application.Abstractions.Data;

/// <summary>
/// Opens SQL connections for Dapper queries and stored procedure execution.
/// </summary>
/// <remarks>
/// Callers own and must dispose the returned connection. Tenant-owned operations added
/// in later phases must always pass or resolve a verified tenant scope.
/// </remarks>
public interface IDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}
