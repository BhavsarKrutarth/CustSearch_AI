using CustSearch.Domain.Enums;

namespace CustSearch.Domain.Entities;

/// <summary>
/// Phase 5C — canonical tenant-owned physical store used for user assignment, quota enforcement and later CCTV/reporting features.
/// </summary>
/// <remarks>
/// Coordinates are optional but must be supplied as a pair. Persistence remains UTC while the configured time zone is used for local reporting.
/// Location verification and every administrative change must be audited by the application service.
/// </remarks>
public sealed class Store
{
    private Store() { }

    private Store(long tenantId, string storeCode, string storeName, string addressLine1, string? addressLine2,
        string? landmark, string city, string? district, string stateOrProvince, string postalCode, string countryCode,
        decimal? latitude, decimal? longitude, decimal? geoFenceRadiusMeters, string? externalPlaceId,
        StoreLocationSource locationSource, string timeZone, string? contactEmail, string? contactMobile, DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tenantId);
        ValidateCoordinates(latitude, longitude);
        if (geoFenceRadiusMeters is <= 0) throw new ArgumentOutOfRangeException(nameof(geoFenceRadiusMeters));
        TenantId = tenantId;
        StoreCode = Require(storeCode, nameof(storeCode), 30).ToUpperInvariant();
        StoreName = Require(storeName, nameof(storeName), 150);
        AddressLine1 = Require(addressLine1, nameof(addressLine1), 250);
        AddressLine2 = Optional(addressLine2, nameof(addressLine2), 250);
        Landmark = Optional(landmark, nameof(landmark), 150);
        City = Require(city, nameof(city), 100);
        District = Optional(district, nameof(district), 100);
        StateOrProvince = Require(stateOrProvince, nameof(stateOrProvince), 100);
        PostalCode = Require(postalCode, nameof(postalCode), 20);
        CountryCode = Require(countryCode, nameof(countryCode), 2).ToUpperInvariant();
        Latitude = latitude;
        Longitude = longitude;
        GeoFenceRadiusMeters = geoFenceRadiusMeters;
        ExternalPlaceId = Optional(externalPlaceId, nameof(externalPlaceId), 200);
        LocationSource = locationSource;
        TimeZone = Require(timeZone, nameof(timeZone), 100);
        ContactEmail = Optional(contactEmail, nameof(contactEmail), 254);
        ContactMobile = Optional(contactMobile, nameof(contactMobile), 30);
        IsActive = true;
        CreatedUtc = RequireUtc(utcNow, nameof(utcNow));
        UpdatedUtc = CreatedUtc;
    }

    public long Id { get; private set; }
    public long TenantId { get; private set; }
    public Tenant Tenant { get; private set; } = null!;
    public string StoreCode { get; private set; } = string.Empty;
    public string StoreName { get; private set; } = string.Empty;
    public string AddressLine1 { get; private set; } = string.Empty;
    public string? AddressLine2 { get; private set; }
    public string? Landmark { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string? District { get; private set; }
    public string StateOrProvince { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string CountryCode { get; private set; } = string.Empty;
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public decimal? GeoFenceRadiusMeters { get; private set; }
    public string? ExternalPlaceId { get; private set; }
    public StoreLocationSource LocationSource { get; private set; }
    public bool IsLocationVerified { get; private set; }
    public DateTime? LocationVerifiedUtc { get; private set; }
    public long? LocationVerifiedByUserId { get; private set; }
    public string TimeZone { get; private set; } = string.Empty;
    public string? ContactEmail { get; private set; }
    public string? ContactMobile { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedUtc { get; private set; }
    public DateTime UpdatedUtc { get; private set; }

    public static Store Create(long tenantId, string storeCode, string storeName, string addressLine1, string? addressLine2,
        string? landmark, string city, string? district, string stateOrProvince, string postalCode, string countryCode,
        decimal? latitude, decimal? longitude, decimal? geoFenceRadiusMeters, string? externalPlaceId,
        StoreLocationSource locationSource, string timeZone, string? contactEmail, string? contactMobile, DateTime utcNow) =>
        new(tenantId, storeCode, storeName, addressLine1, addressLine2, landmark, city, district, stateOrProvince, postalCode,
            countryCode, latitude, longitude, geoFenceRadiusMeters, externalPlaceId, locationSource, timeZone, contactEmail, contactMobile, utcNow);

    public void Update(string storeName, string addressLine1, string? addressLine2, string? landmark, string city,
        string? district, string stateOrProvince, string postalCode, string countryCode, decimal? latitude,
        decimal? longitude, decimal? geoFenceRadiusMeters, string? externalPlaceId, StoreLocationSource locationSource,
        string timeZone, string? contactEmail, string? contactMobile, DateTime utcNow)
    {
        ValidateCoordinates(latitude, longitude);
        if (geoFenceRadiusMeters is <= 0) throw new ArgumentOutOfRangeException(nameof(geoFenceRadiusMeters));
        StoreName = Require(storeName, nameof(storeName), 150);
        AddressLine1 = Require(addressLine1, nameof(addressLine1), 250);
        AddressLine2 = Optional(addressLine2, nameof(addressLine2), 250);
        Landmark = Optional(landmark, nameof(landmark), 150);
        City = Require(city, nameof(city), 100);
        District = Optional(district, nameof(district), 100);
        StateOrProvince = Require(stateOrProvince, nameof(stateOrProvince), 100);
        PostalCode = Require(postalCode, nameof(postalCode), 20);
        CountryCode = Require(countryCode, nameof(countryCode), 2).ToUpperInvariant();
        Latitude = latitude;
        Longitude = longitude;
        GeoFenceRadiusMeters = geoFenceRadiusMeters;
        ExternalPlaceId = Optional(externalPlaceId, nameof(externalPlaceId), 200);
        LocationSource = locationSource;
        TimeZone = Require(timeZone, nameof(timeZone), 100);
        ContactEmail = Optional(contactEmail, nameof(contactEmail), 254);
        ContactMobile = Optional(contactMobile, nameof(contactMobile), 30);
        IsLocationVerified = false;
        LocationVerifiedUtc = null;
        LocationVerifiedByUserId = null;
        UpdatedUtc = RequireUtc(utcNow, nameof(utcNow));
    }

    public void VerifyLocation(long userId, DateTime utcNow)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
        IsLocationVerified = true;
        LocationVerifiedByUserId = userId;
        LocationVerifiedUtc = RequireUtc(utcNow, nameof(utcNow));
        UpdatedUtc = LocationVerifiedUtc.Value;
    }

    public void SetActive(bool isActive, DateTime utcNow)
    {
        IsActive = isActive;
        UpdatedUtc = RequireUtc(utcNow, nameof(utcNow));
    }

    private static void ValidateCoordinates(decimal? latitude, decimal? longitude)
    {
        if (latitude.HasValue != longitude.HasValue) throw new ArgumentException("Latitude and longitude must be supplied together.");
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
    }

    private static string Require(string value, string name, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        var v = value.Trim();
        return v.Length <= max ? v : throw new ArgumentOutOfRangeException(name);
    }
    private static string? Optional(string? value, string name, int max) => string.IsNullOrWhiteSpace(value) ? null : Require(value, name, max);
    private static DateTime RequireUtc(DateTime value, string name) => value.Kind == DateTimeKind.Utc ? value : throw new ArgumentException("Timestamp must be UTC.", name);
}
