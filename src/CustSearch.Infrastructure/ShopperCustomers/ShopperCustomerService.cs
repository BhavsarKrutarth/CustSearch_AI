using System.Globalization;
using System.Text.Json;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Application.ShopperCustomers;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CustSearch.Infrastructure.ShopperCustomers;

/// <summary>
/// Phase 6 application implementation for shopper customers and anonymous visitors. TenantId and StoreIds are derived
/// only from the authenticated server context. Unknown visitors remain anonymous until an explicit conversion action.
/// </summary>
public sealed class ShopperCustomerService(
    CustSearchDbContext db,
    IShopperCustomerRepository repository,
    ICurrentUserContext currentUser,
    TimeProvider timeProvider) : IShopperCustomerService
{
    public async Task<PagedResult<CustomerListItem>> SearchCustomersAsync(CustomerSearchQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var tenantId = RequireTenantId();
        var tenantWide = IsTenantWide();
        EnsureStoreFilterAllowed(query.StoreId, tenantWide);
        var paging = PhaseSixAccessRules.NormalizePaging(query.PageNumber, query.PageSize);
        var normalized = query with { PageNumber = paging.PageNumber, PageSize = paging.PageSize };
        var rows = await repository.SearchCustomersAsync(tenantId, currentUser.StoreIds, tenantWide, normalized, cancellationToken).ConfigureAwait(false);
        if (rows.Count == 0) return new([], normalized.PageNumber, normalized.PageSize, 0);

        var ids = rows.Select(x => x.Id).ToArray();
        var assignments = await db.CustomerStoreAssignments.AsNoTracking()
            .Where(x => x.TenantId == tenantId && ids.Contains(x.CustomerId) && x.Store.IsActive)
            .Select(x => new { x.CustomerId, x.StoreId })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var storesByCustomer = assignments
            .Where(x => tenantWide || currentUser.StoreIds.Contains(x.StoreId))
            .GroupBy(x => x.CustomerId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<long>)x.Select(y => y.StoreId).Distinct().OrderBy(y => y).ToArray());

        var data = rows.Select(x => new CustomerListItem(x.Id, x.CustomerCode, x.FirstName, x.LastName, x.Mobile, x.Email,
            x.IsActive, storesByCustomer.GetValueOrDefault(x.Id, []), x.UpdatedUtc)).ToArray();
        return new(data, normalized.PageNumber, normalized.PageSize, rows[0].TotalCount);
    }

    public async Task<CustomerDetail> GetCustomerAsync(long customerId, CancellationToken cancellationToken = default)
    {
        var customer = await RequireVisibleCustomerAsync(customerId, false, cancellationToken).ConfigureAwait(false);
        return await MapCustomerAsync(customer, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CustomerSmartProfile> GetSmartProfileAsync(long customerId, CancellationToken cancellationToken = default)
    {
        var detail = await GetCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
        var tenantId = RequireTenantId();
        var tenantWide = IsTenantWide();
        var visitorQuery = db.AnonymousVisitors.AsNoTracking().Where(x => x.TenantId == tenantId && x.ConvertedCustomerId == customerId);
        if (!tenantWide) visitorQuery = visitorQuery.Where(x => currentUser.StoreIds.Contains(x.StoreId));
        var visitorCount = await visitorQuery.CountAsync(cancellationToken).ConfigureAwait(false);
        var lastSeen = await visitorQuery.OrderByDescending(x => x.LastSeenUtc).Select(x => (DateTime?)x.LastSeenUtc)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return new(detail, visitorCount, lastSeen, !string.IsNullOrWhiteSpace(detail.Mobile), !string.IsNullOrWhiteSpace(detail.Email),
            ["Identity", "Contact", "Store visibility", "Anonymous visitor conversions"],
            ["Households (Phase 7)", "Visits (Phase 7)", "Purchase history (Phase 8)", "Preferences (Phase 10)"]);
    }

    public async Task<CustomerDetail> CreateCustomerAsync(CreateCustomerCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        var stores = NormalizeStoreIds(command.StoreIds);
        await EnsureRequestedStoresAsync(tenantId, stores, command.PrimaryStoreId, requireVisibleStore: !IsTenantWide(), cancellationToken).ConfigureAwait(false);
        if (!IsTenantWide() && stores.Length == 0) throw new TenantBusinessRuleException("Store-scoped users must assign the customer to at least one authorized store.");

        var code = string.IsNullOrWhiteSpace(command.CustomerCode) ? NewCustomerCode() : command.CustomerCode.Trim().ToUpperInvariant();
        if (await db.Customers.AnyAsync(x => x.TenantId == tenantId && x.CustomerCode == code, cancellationToken).ConfigureAwait(false))
            throw new TenantBusinessRuleException("Customer code already exists in this tenant.");

        var now = UtcNow();
        Customer customer;
        try { customer = Customer.Create(tenantId, code, command.FirstName, command.LastName, command.Mobile, command.Email, command.Notes, now); }
        catch (ArgumentException exception) { throw new TenantBusinessRuleException(exception.Message); }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await ReplaceCustomerStoresAsync(tenantId, customer.Id, stores, command.PrimaryStoreId, audit.ActorUserId, now, tenantWide: true, cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, command.PrimaryStoreId ?? stores.FirstOrDefaultOrNull(), audit, "CustomerCreated", "Customer", customer.Id, null,
            new { customer.CustomerCode, customer.FirstName, customer.LastName, customer.Mobile, customer.Email, StoreIds = stores }, now);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return await MapCustomerAsync(customer, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CustomerDetail> UpdateCustomerAsync(long customerId, UpdateCustomerCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateAudit(audit);
        var customer = await RequireVisibleCustomerAsync(customerId, true, cancellationToken).ConfigureAwait(false);
        var before = new { customer.FirstName, customer.LastName, customer.Mobile, customer.Email, customer.Notes, customer.IsActive };
        try { customer.Update(command.FirstName, command.LastName, command.Mobile, command.Email, command.Notes, command.IsActive, UtcNow()); }
        catch (ArgumentException exception) { throw new TenantBusinessRuleException(exception.Message); }
        RecordAudit(RequireTenantId(), null, audit, "CustomerUpdated", "Customer", customer.Id, before,
            new { customer.FirstName, customer.LastName, customer.Mobile, customer.Email, customer.Notes, customer.IsActive }, UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await MapCustomerAsync(customer, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CustomerDetail> SetCustomerStoresAsync(long customerId, SetCustomerStoresCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        var customer = await RequireVisibleCustomerAsync(customerId, false, cancellationToken).ConfigureAwait(false);
        var tenantWide = IsTenantWide();
        var requested = NormalizeStoreIds(command.StoreIds);
        if (!tenantWide && requested.Length == 0) throw new TenantBusinessRuleException("Store-scoped users must keep at least one authorized customer-store assignment.");
        await EnsureRequestedStoresAsync(tenantId, requested, command.PrimaryStoreId, requireVisibleStore: !tenantWide, cancellationToken).ConfigureAwait(false);

        var before = await db.CustomerStoreAssignments.AsNoTracking().Where(x => x.TenantId == tenantId && x.CustomerId == customerId)
            .Select(x => new { x.StoreId, x.IsPrimary }).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        await ReplaceCustomerStoresAsync(tenantId, customerId, requested, command.PrimaryStoreId, audit.ActorUserId, UtcNow(), tenantWide, cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, command.PrimaryStoreId, audit, "CustomerStoresChanged", "Customer", customerId, before,
            new { StoreIds = requested, command.PrimaryStoreId }, UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await MapCustomerAsync(customer, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PagedResult<AnonymousVisitorListItem>> SearchVisitorsAsync(AnonymousVisitorSearchQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var tenantWide = IsTenantWide();
        EnsureStoreFilterAllowed(query.StoreId, tenantWide);
        var paging = PhaseSixAccessRules.NormalizePaging(query.PageNumber, query.PageSize);
        var normalized = query with { PageNumber = paging.PageNumber, PageSize = paging.PageSize };
        var rows = await repository.SearchVisitorsAsync(RequireTenantId(), currentUser.StoreIds, tenantWide, normalized, cancellationToken).ConfigureAwait(false);
        var data = rows.Select(MapVisitorRow).ToArray();
        return new(data, normalized.PageNumber, normalized.PageSize, rows.Count == 0 ? 0 : rows[0].TotalCount);
    }

    public async Task<AnonymousVisitorDetail> GetVisitorAsync(long visitorId, CancellationToken cancellationToken = default) =>
        MapVisitor(await RequireVisibleVisitorAsync(visitorId, false, cancellationToken).ConfigureAwait(false));

    public async Task<AnonymousVisitorDetail> CreateVisitorAsync(CreateAnonymousVisitorCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        await RequireAuthorizedStoreAsync(tenantId, command.StoreId, cancellationToken).ConfigureAwait(false);
        var code = string.IsNullOrWhiteSpace(command.VisitorCode) ? NewVisitorCode() : command.VisitorCode.Trim().ToUpperInvariant();
        if (await db.AnonymousVisitors.AnyAsync(x => x.TenantId == tenantId && x.StoreId == command.StoreId && x.VisitorCode == code, cancellationToken).ConfigureAwait(false))
            throw new TenantBusinessRuleException("Visitor code already exists in this store.");
        var seenUtc = NormalizeUtc(command.SeenUtc ?? UtcNow());
        AnonymousVisitor visitor;
        try { visitor = AnonymousVisitor.Create(tenantId, command.StoreId, code, seenUtc); }
        catch (ArgumentException exception) { throw new TenantBusinessRuleException(exception.Message); }
        db.AnonymousVisitors.Add(visitor);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        RecordAudit(tenantId, command.StoreId, audit, "AnonymousVisitorCreated", "AnonymousVisitor", visitor.Id, null,
            new { visitor.VisitorCode, visitor.StoreId, visitor.FirstSeenUtc }, UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapVisitor(visitor);
    }

    public async Task<AnonymousVisitorDetail> TouchVisitorAsync(long visitorId, TouchAnonymousVisitorCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateAudit(audit);
        var visitor = await RequireVisibleVisitorAsync(visitorId, true, cancellationToken).ConfigureAwait(false);
        if (!visitor.IsActive || visitor.ConvertedCustomerId.HasValue) throw new TenantBusinessRuleException("Converted/inactive visitors cannot receive a new anonymous sighting.");
        var seenUtc = NormalizeUtc(command.SeenUtc ?? UtcNow());
        try { visitor.Touch(seenUtc); }
        catch (ArgumentException exception) { throw new TenantBusinessRuleException(exception.Message); }
        RecordAudit(RequireTenantId(), visitor.StoreId, audit, "AnonymousVisitorSeen", "AnonymousVisitor", visitor.Id, null,
            new { visitor.LastSeenUtc }, UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return MapVisitor(visitor);
    }

    public async Task<CustomerDetail> ConvertVisitorAsync(long visitorId, ConvertAnonymousVisitorCommand command, TenantAuditContext audit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidateAudit(audit);
        var tenantId = RequireTenantId();
        var visitor = await RequireVisibleVisitorAsync(visitorId, true, cancellationToken).ConfigureAwait(false);
        if (visitor.ConvertedCustomerId.HasValue) throw new TenantBusinessRuleException("Anonymous visitor has already been converted.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        Customer customer;
        if (command.CustomerId.HasValue)
        {
            RequirePermission(PermissionCatalog.Operations.CustomersEdit, "Linking an existing customer requires Customers.Edit.");
            customer = await RequireVisibleCustomerAsync(command.CustomerId.Value, true, cancellationToken).ConfigureAwait(false);
            if (!customer.IsActive) throw new TenantBusinessRuleException("Inactive customer cannot be linked to an anonymous visitor.");
        }
        else
        {
            RequirePermission(PermissionCatalog.Operations.CustomersCreate, "Creating a customer during visitor conversion requires Customers.Create.");
            if (string.IsNullOrWhiteSpace(command.FirstName)) throw new TenantBusinessRuleException("First name is required when creating a customer from a visitor.");
            var now = UtcNow();
            customer = Customer.Create(tenantId, NewCustomerCode(), command.FirstName, command.LastName, command.Mobile, command.Email, command.Notes, now);
            db.Customers.Add(customer);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (!await db.CustomerStoreAssignments.AnyAsync(x => x.TenantId == tenantId && x.CustomerId == customer.Id && x.StoreId == visitor.StoreId, cancellationToken).ConfigureAwait(false))
        {
            var hasPrimary = await db.CustomerStoreAssignments.AnyAsync(x => x.TenantId == tenantId && x.CustomerId == customer.Id && x.IsPrimary, cancellationToken).ConfigureAwait(false);
            db.CustomerStoreAssignments.Add(CustomerStoreAssignment.Assign(tenantId, customer.Id, visitor.StoreId, !hasPrimary, UtcNow(), audit.ActorUserId));
        }

        try { visitor.ConvertToCustomer(customer.Id, UtcNow()); }
        catch (InvalidOperationException exception) { throw new TenantBusinessRuleException(exception.Message); }
        RecordAudit(tenantId, visitor.StoreId, audit, "AnonymousVisitorConverted", "AnonymousVisitor", visitor.Id, null,
            new { visitor.VisitorCode, CustomerId = customer.Id }, UtcNow());
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return await MapCustomerAsync(customer, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Customer> RequireVisibleCustomerAsync(long customerId, bool tracked, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var query = db.Customers.Where(x => x.TenantId == tenantId && x.Id == customerId);
        var customer = await (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("Customer");
        if (!IsTenantWide())
        {
            var visible = await db.CustomerStoreAssignments.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.CustomerId == customerId && currentUser.StoreIds.Contains(x.StoreId), cancellationToken).ConfigureAwait(false);
            if (!visible) throw new TenantResourceNotFoundException("Customer");
        }
        return customer;
    }

    private async Task<AnonymousVisitor> RequireVisibleVisitorAsync(long visitorId, bool tracked, CancellationToken cancellationToken)
    {
        var tenantId = RequireTenantId();
        var query = db.AnonymousVisitors.Where(x => x.TenantId == tenantId && x.Id == visitorId);
        var visitor = await (tracked ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new TenantResourceNotFoundException("Anonymous visitor");
        if (!PhaseSixAccessRules.CanAccessStore(visitor.StoreId, currentUser.StoreIds, IsTenantWide()))
            throw new TenantResourceNotFoundException("Anonymous visitor");
        return visitor;
    }

    private async Task<CustomerDetail> MapCustomerAsync(Customer customer, CancellationToken cancellationToken)
    {
        var assignments = await db.CustomerStoreAssignments.AsNoTracking()
            .Where(x => x.TenantId == customer.TenantId && x.CustomerId == customer.Id && x.Store.IsActive)
            .OrderByDescending(x => x.IsPrimary).ThenBy(x => x.StoreId)
            .Select(x => new { x.StoreId, x.IsPrimary })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (!IsTenantWide()) assignments = assignments.Where(x => currentUser.StoreIds.Contains(x.StoreId)).ToList();
        return new(customer.Id, customer.CustomerCode, customer.FirstName, customer.LastName, customer.Mobile, customer.Email,
            customer.Notes, customer.IsActive, assignments.Select(x => x.StoreId).ToArray(),
            assignments.FirstOrDefault(x => x.IsPrimary)?.StoreId, customer.CreatedUtc, customer.UpdatedUtc);
    }

    private async Task ReplaceCustomerStoresAsync(long tenantId, long customerId, long[] requestedStoreIds, long? primaryStoreId,
        long actorUserId, DateTime now, bool tenantWide, CancellationToken cancellationToken)
    {
        var existing = await db.CustomerStoreAssignments.Where(x => x.TenantId == tenantId && x.CustomerId == customerId)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        if (tenantWide)
        {
            db.CustomerStoreAssignments.RemoveRange(existing);
            foreach (var storeId in requestedStoreIds)
                db.CustomerStoreAssignments.Add(CustomerStoreAssignment.Assign(tenantId, customerId, storeId, primaryStoreId == storeId, now, actorUserId));
            return;
        }

        var outOfScopePrimaryExists = existing.Any(x => x.IsPrimary && !currentUser.StoreIds.Contains(x.StoreId));
        if (outOfScopePrimaryExists && primaryStoreId.HasValue)
            throw new TenantBusinessRuleException("The customer's primary store is outside your authorized scope and cannot be changed.");
        var removable = existing.Where(x => currentUser.StoreIds.Contains(x.StoreId)).ToArray();
        db.CustomerStoreAssignments.RemoveRange(removable);
        foreach (var storeId in requestedStoreIds)
            db.CustomerStoreAssignments.Add(CustomerStoreAssignment.Assign(tenantId, customerId, storeId,
                !outOfScopePrimaryExists && primaryStoreId == storeId, now, actorUserId));
    }

    private async Task EnsureRequestedStoresAsync(long tenantId, long[] storeIds, long? primaryStoreId, bool requireVisibleStore, CancellationToken cancellationToken)
    {
        if (primaryStoreId.HasValue && !storeIds.Contains(primaryStoreId.Value))
            throw new TenantBusinessRuleException("Primary store must be included in StoreIds.");
        if (requireVisibleStore && !PhaseSixAccessRules.RequestedStoresWithinScope(storeIds, currentUser.StoreIds, tenantWide: false))
            throw new TenantBusinessRuleException("One or more requested stores are outside your authorized store scope.");
        if (storeIds.Length == 0) return;
        var count = await db.Stores.CountAsync(x => x.TenantId == tenantId && x.IsActive && storeIds.Contains(x.Id), cancellationToken).ConfigureAwait(false);
        if (count != storeIds.Length) throw new TenantBusinessRuleException("One or more stores are invalid, inactive or belong to another tenant.");
    }

    private async Task RequireAuthorizedStoreAsync(long tenantId, long storeId, CancellationToken cancellationToken)
    {
        if (!PhaseSixAccessRules.CanAccessStore(storeId, currentUser.StoreIds, IsTenantWide()))
            throw new TenantResourceNotFoundException("Store");
        if (!await db.Stores.AsNoTracking().AnyAsync(x => x.TenantId == tenantId && x.Id == storeId && x.IsActive, cancellationToken).ConfigureAwait(false))
            throw new TenantResourceNotFoundException("Store");
    }

    private void EnsureStoreFilterAllowed(long? storeId, bool tenantWide)
    {
        if (storeId.HasValue && !PhaseSixAccessRules.CanAccessStore(storeId.Value, currentUser.StoreIds, tenantWide))
            throw new TenantResourceNotFoundException("Store");
    }

    private bool IsTenantWide() => PhaseSixAccessRules.IsTenantWide(currentUser.Roles);
    private long RequireTenantId() => currentUser.TenantId is > 0 and var id ? id : throw new UnauthorizedAccessException("Tenant context is required.");
    private DateTime UtcNow() => timeProvider.GetUtcNow().UtcDateTime;
    private static DateTime NormalizeUtc(DateTime value) => value.Kind switch { DateTimeKind.Utc => value, DateTimeKind.Local => value.ToUniversalTime(), _ => DateTime.SpecifyKind(value, DateTimeKind.Utc) };
    private static long[] NormalizeStoreIds(IReadOnlyList<long>? storeIds) => (storeIds ?? []).Where(x => x > 0).Distinct().OrderBy(x => x).ToArray();
    private static string NewCustomerCode() => $"CUST-{Guid.NewGuid():N}"[..25].ToUpperInvariant();
    private static string NewVisitorCode() => $"VIS-{Guid.NewGuid():N}"[..24].ToUpperInvariant();

    private void RequirePermission(string permission, string message)
    {
        if (!currentUser.Permissions.Contains(permission)) throw new TenantBusinessRuleException(message);
    }

    private static void ValidateAudit(TenantAuditContext audit)
    {
        if (audit.ActorUserId <= 0 || string.IsNullOrWhiteSpace(audit.CorrelationId))
            throw new ArgumentException("Valid audit context is required.", nameof(audit));
    }

    private void RecordAudit(long tenantId, long? storeId, TenantAuditContext audit, string action, string entityType, long entityId,
        object? before, object? after, DateTime now)
    {
        db.AuditLogs.Add(AuditLog.Record(tenantId, storeId, audit.ActorUserId, "User", action, entityType,
            entityId.ToString(CultureInfo.InvariantCulture), before is null ? null : JsonSerializer.Serialize(before),
            after is null ? null : JsonSerializer.Serialize(after), audit.IpAddress, audit.UserAgent, audit.CorrelationId, now));
    }
}

internal static class PhaseSixEnumerableExtensions
{
    public static long? FirstOrDefaultOrNull(this IEnumerable<long> values)
    {
        foreach (var value in values) return value;
        return null;
    }
}
