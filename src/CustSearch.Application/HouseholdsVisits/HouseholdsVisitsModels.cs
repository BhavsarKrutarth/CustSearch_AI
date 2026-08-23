using CustSearch.Application.ShopperCustomers;
using CustSearch.Domain.Enums;

namespace CustSearch.Application.HouseholdsVisits;

public sealed record HouseholdSearchQuery(int PageNumber=1,int PageSize=25,string? Search=null,bool ActiveOnly=false);
public sealed record HouseholdListItem(long Id,string HouseholdCode,string Name,int VisibleMemberCount,bool IsActive,DateTime UpdatedUtc);
public sealed record HouseholdMemberView(long CustomerId,string CustomerCode,string FirstName,string? LastName,string RelationshipType,
    HouseholdRelationshipSource RelationshipSource,bool IsVerified,long VerifiedByUserId,DateTime VerifiedUtc,bool IsActive);
public sealed record HouseholdDetail(long Id,string HouseholdCode,string Name,string? Notes,bool IsActive,IReadOnlyList<HouseholdMemberView> Members,DateTime CreatedUtc,DateTime UpdatedUtc);
public sealed record CreateHouseholdCommand(string? HouseholdCode,string Name,string? Notes);
public sealed record UpdateHouseholdCommand(string Name,string? Notes,bool IsActive);
public sealed record SaveHouseholdMemberCommand(long CustomerId,string RelationshipType,HouseholdRelationshipSource RelationshipSource,bool IsActive=true);

public sealed record CustomerVisitSearchQuery(int PageNumber=1,int PageSize=25,string? Search=null,long? StoreId=null,long? CustomerId=null,DateTime? FromUtc=null,DateTime? ToUtc=null);
public sealed record CustomerVisitListItem(long Id,string VisitCode,long CustomerId,string CustomerCode,string CustomerName,long StoreId,long? VisitPartyId,
    DateTime EnteredUtc,DateTime? ExitedUtc,CustomerVisitSource Source,CustomerVisitStatus Status);
public sealed record CustomerVisitDetail(long Id,string VisitCode,long CustomerId,string CustomerCode,string CustomerName,long StoreId,long? VisitPartyId,
    DateTime EnteredUtc,DateTime? ExitedUtc,CustomerVisitSource Source,CustomerVisitStatus Status,DateTime CreatedUtc,DateTime UpdatedUtc);
public sealed record CreateCustomerVisitCommand(long StoreId,long CustomerId,long? VisitPartyId,DateTime? EnteredUtc,CustomerVisitSource Source);
public sealed record CompleteCustomerVisitCommand(DateTime? ExitedUtc);

public sealed record VisitPartySearchQuery(int PageNumber=1,int PageSize=25,string? Search=null,long? StoreId=null,VisitPartyStatus? Status=null,DateTime? FromUtc=null,DateTime? ToUtc=null);
public sealed record VisitPartyListItem(long Id,string PartyCode,long StoreId,DateTime StartedUtc,DateTime? EndedUtc,VisitPartySource Source,VisitPartyStatus Status,int MemberCount);
public sealed record VisitPartyMemberView(long Id,VisitPartyMemberIdentityType IdentityType,long? CustomerId,string? CustomerCode,long? AnonymousVisitorId,string? VisitorCode,DateTime JoinedUtc);
public sealed record VisitPartyDetail(long Id,string PartyCode,long StoreId,DateTime StartedUtc,DateTime? EndedUtc,VisitPartySource Source,VisitPartyStatus Status,IReadOnlyList<VisitPartyMemberView> Members,DateTime CreatedUtc,DateTime UpdatedUtc);

public sealed record HouseholdSearchRow(long Id,string HouseholdCode,string Name,int VisibleMemberCount,bool IsActive,DateTime UpdatedUtc,long TotalCount);
public sealed record CustomerVisitSearchRow(long Id,string VisitCode,long CustomerId,string CustomerCode,string CustomerName,long StoreId,long? VisitPartyId,
    DateTime EnteredUtc,DateTime? ExitedUtc,CustomerVisitSource Source,CustomerVisitStatus Status,long TotalCount);
public sealed record VisitPartySearchRow(long Id,string PartyCode,long StoreId,DateTime StartedUtc,DateTime? EndedUtc,VisitPartySource Source,VisitPartyStatus Status,int MemberCount,long TotalCount);

/// <summary>Alias keeps Phase 7 contracts on the existing shared pagination model rather than duplicating paging infrastructure.</summary>
public static class PhaseSevenPaging
{
    public static PagedResult<T> Empty<T>(int pageNumber,int pageSize)=>new([],pageNumber,pageSize,0);
}