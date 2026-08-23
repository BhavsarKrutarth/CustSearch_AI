global using static CustSearch.Infrastructure.ShopperCustomers.PhaseSixVisitorMappings;

using CustSearch.Application.ShopperCustomers;
using CustSearch.Domain.Entities;

namespace CustSearch.Infrastructure.ShopperCustomers;

/// <summary>Central Phase 6 visitor projections shared by search and detail service paths.</summary>
internal static class PhaseSixVisitorMappings
{
    internal static AnonymousVisitorListItem MapVisitorRow(AnonymousVisitorSearchRow row) =>
        new(row.Id, row.VisitorCode, row.StoreId, row.FirstSeenUtc, row.LastSeenUtc, row.IsActive,
            row.ConvertedCustomerId, row.ConvertedUtc);

    internal static AnonymousVisitorDetail MapVisitor(AnonymousVisitor visitor) =>
        new(visitor.Id, visitor.VisitorCode, visitor.StoreId, visitor.FirstSeenUtc, visitor.LastSeenUtc,
            visitor.IsActive, visitor.ConvertedCustomerId, visitor.ConvertedUtc, visitor.CreatedUtc, visitor.UpdatedUtc);
}
