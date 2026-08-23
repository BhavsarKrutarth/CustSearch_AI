using CustSearch.Application.Authentication;
using CustSearch.Application.Abstractions.Data;
using CustSearch.Application.Tenancy;
using CustSearch.Application.PlatformTenancy;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Infrastructure.Data;
using CustSearch.Infrastructure.Persistence;
using CustSearch.Infrastructure.PlatformTenancy;
using CustSearch.Infrastructure.Security;
using CustSearch.Infrastructure.Tenancy;
using CustSearch.Infrastructure.TenantOperations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CustSearch.Infrastructure;

/// <summary>Registers SQL Server, EF Core, Dapper, authentication and tenant operations.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddDbContext<CustSearchDbContext>(options => options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));
        services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
        services.AddSingleton<JwtTokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ITenantUserRepository, TenantUserRepository>();
        services.AddScoped<IPlatformTenantManagementService, PlatformTenantManagementService>();

        // Phase 5 — tenant user/store/staff/category/voice implementation.
        services.AddScoped<ITenantOperationsRepository, TenantOperationsRepository>();
        services.AddScoped<ITenantOperationsService, TenantOperationsService>();
        services.AddScoped<IClaimsTransformation, PhaseFiveStoreClaimsTransformation>();
        return services;
    }
}
