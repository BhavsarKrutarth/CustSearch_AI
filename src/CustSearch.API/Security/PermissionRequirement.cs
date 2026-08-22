using Microsoft.AspNetCore.Authorization;

namespace CustSearch.API.Security;

/// <summary>
/// Represents one exact server-issued permission required by an API operation.
/// </summary>
public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;
