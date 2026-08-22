namespace CustSearch.Domain.Enums;

/// <summary>
/// Separates platform identities from identities owned by exactly one tenant.
/// </summary>
public enum UserScope : byte
{
    Platform = 1,
    Tenant = 2,
}
