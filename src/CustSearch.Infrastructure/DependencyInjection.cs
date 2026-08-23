using CustSearch.Application.Authentication;
using CustSearch.Application.Abstractions.Data;
using CustSearch.Application.HouseholdsVisits;
using CustSearch.Application.PlatformTenancy;
using CustSearch.Application.RetailBilling;
using CustSearch.Application.ShopperCustomers;
using CustSearch.Application.Tenancy;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Infrastructure.Data;
using CustSearch.Infrastructure.HouseholdsVisits;
using CustSearch.Infrastructure.Persistence;
using CustSearch.Infrastructure.PlatformTenancy;
using CustSearch.Infrastructure.RetailBilling;
using CustSearch.Infrastructure.Security;
using CustSearch.Infrastructure.ShopperCustomers;
using CustSearch.Infrastructure.Tenancy;
using CustSearch.Infrastructure.TenantOperations;
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
        services.AddDbContext<CustSearchDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null)));
        services.AddSingleton<IDbConnectionFactory>(_ => new SqlConnectionFactory(connectionString));
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
        services.AddSingleton<JwtTokenService>();

        services.AddScoped<AuthenticationService>();
        services.AddScoped<IAuthenticationService, PhaseFiveAuthenticationServiceDecorator>();

        services.AddScoped<ITenantUserRepository, TenantUserRepository>();
        services.AddScoped<IPlatformTenantManagementService, PlatformTenantManagementService>();
        services.AddScoped<ITenantOperationsRepository, TenantOperationsRepository>();
        services.AddScoped<TenantOperationsService>();
        services.AddScoped<ITenantOperationsService, TenantOperationsSecurityDecorator>();

        services.AddScoped<IShopperCustomerRepository, ShopperCustomerRepository>();
        services.AddScoped<IShopperCustomerService, ShopperCustomerService>();

        services.AddScoped<IHouseholdsVisitsRepository, HouseholdsVisitsRepository>();
        services.AddScoped<IHouseholdsVisitsService, HouseholdsVisitsService>();

        // Phase 8 keeps transactional catalog/invoice writes in the EF unit-of-work and uses Dapper stored procedures
        // for tenant/store filtered search, purchase-history and retail report read models.
        services.AddScoped<IRetailBillingRepository, RetailBillingRepository>();
        services.AddScoped<IRetailBillingService, RetailBillingService>();
        return services;
    }
}
