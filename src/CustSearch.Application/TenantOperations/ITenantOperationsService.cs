namespace CustSearch.Application.TenantOperations;

/// <summary>Phase 5 application boundary for tenant users, stores, staff, categories and store voice configuration.</summary>
public interface ITenantOperationsService
{
    Task<TenantDashboardSummary> GetDashboardAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantUserListItem>> ListUsersAsync(CancellationToken cancellationToken = default);
    Task<TenantUserDetail> GetUserAsync(long userId, CancellationToken cancellationToken = default);
    Task<TenantUserDetail> CreateUserAsync(CreateTenantUserCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<TenantUserDetail> UpdateUserAsync(long userId, UpdateTenantUserCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<TenantUserDetail> SetUserRolesAsync(long userId, SetTenantUserRolesCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<TenantUserDetail> SetUserStoresAsync(long userId, SetTenantUserStoresCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<TenantUserDetail> ResetUserPasswordAsync(long userId, ResetTenantUserPasswordCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StoreView>> ListStoresAsync(CancellationToken cancellationToken = default);
    Task<StoreView> GetStoreAsync(long storeId, CancellationToken cancellationToken = default);
    Task<StoreView> CreateStoreAsync(SaveStoreCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<StoreView> UpdateStoreAsync(long storeId, SaveStoreCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<StoreView> SetStoreActiveAsync(long storeId, bool active, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<StoreView> VerifyStoreLocationAsync(long storeId, TenantAuditContext audit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StaffView>> ListStaffAsync(CancellationToken cancellationToken = default);
    Task<StaffView> GetStaffAsync(long staffId, CancellationToken cancellationToken = default);
    Task<StaffView> CreateStaffAsync(CreateStaffCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<StaffView> UpdateStaffAsync(long staffId, UpdateStaffCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<StaffShiftView> CreateShiftAsync(long staffId, CreateStaffShiftCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<StaffShiftView> StartShiftAsync(long shiftId, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<StaffShiftView> CompleteShiftAsync(long shiftId, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<StaffPresenceView> StartPresenceAsync(long staffId, StartStaffPresenceCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<StaffPresenceView> ClosePresenceAsync(long presenceId, TenantAuditContext audit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductCategoryView>> ListCategoriesAsync(long? storeId, CancellationToken cancellationToken = default);
    Task<ProductCategoryView> CreateCategoryAsync(SaveProductCategoryCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
    Task<ProductCategoryView> UpdateCategoryAsync(long categoryId, SaveProductCategoryCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);

    Task<StoreVoiceCommandSettingView> GetVoiceSettingAsync(long storeId, CancellationToken cancellationToken = default);
    Task<StoreVoiceCommandSettingView> SaveVoiceSettingAsync(long storeId, SaveStoreVoiceCommandSettingCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default);
}
