namespace CustSearch.Infrastructure.Security;

/// <summary>Controls whether new and reset passwords are also stored in dbo.Users.DisplayPassword.</summary>
public sealed class PasswordStorageOptions
{
    public const string SectionName = "PasswordStorage";
    public bool StoreDisplayPassword { get; init; }
}
