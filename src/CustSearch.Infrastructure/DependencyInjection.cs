using CustSearch.Application.Authentication;
using CustSearch.Application.Abstractions.Data;
using CustSearch.Application.AlertsRealtime;
using CustSearch.Application.HouseholdsVisits;
using CustSearch.Application.Integrations;
using CustSearch.Application.CamerasTracking;
using CustSearch.Application.Recognition;
using CustSearch.Application.ReportsExports;
using CustSearch.Application.Operations;
using CustSearch.Application.PlatformBilling;
using CustSearch.Application.PlatformTenancy;
using CustSearch.Application.PreferencesVoice;
using CustSearch.Application.RetailBilling;
using CustSearch.Application.ShopperCustomers;
using CustSearch.Application.Tenancy;
using CustSearch.Application.TenantOperations;
using CustSearch.Domain.Entities;
using CustSearch.Infrastructure.Data;
using CustSearch.Infrastructure.AlertsRealtime;
using CustSearch.Infrastructure.HouseholdsVisits;
using CustSearch.Infrastructure.Integrations;
using CustSearch.Infrastructure.CamerasTracking;
using CustSearch.Infrastructure.Recognition;
using CustSearch.Infrastructure.ReportsExports;
using CustSearch.Infrastructure.Operations;
using CustSearch.Infrastructure.Persistence;
using CustSearch.Infrastructure.PlatformBilling;
using CustSearch.Infrastructure.PlatformTenancy;
using CustSearch.Infrastructure.PreferencesVoice;
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
    public static IServiceCollection AddInfrastructure(this IServiceCollection services,string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddDbContext<CustSearchDbContext>(options=>options.UseSqlServer(connectionString,sqlOptions=>sqlOptions.EnableRetryOnFailure(5,TimeSpan.FromSeconds(10),null)));
        services.AddSingleton<IDbConnectionFactory>(_=>new SqlConnectionFactory(connectionString));services.TryAddSingleton(TimeProvider.System);services.AddScoped<IPasswordHasher<UserAccount>,PasswordHasher<UserAccount>>();services.AddSingleton<JwtTokenService>();
        services.AddScoped<AuthenticationService>();services.AddScoped<IAuthenticationService,PhaseFiveAuthenticationServiceDecorator>();
        services.AddScoped<ITenantUserRepository,TenantUserRepository>();services.AddScoped<IPlatformTenantManagementService,PlatformTenantManagementService>();services.AddScoped<IPlatformBillingService,PlatformBillingService>();services.AddScoped<ITenantOperationsRepository,TenantOperationsRepository>();services.AddScoped<TenantOperationsService>();services.AddScoped<ITenantOperationsService,TenantOperationsSecurityDecorator>();
        services.AddScoped<IShopperCustomerRepository,ShopperCustomerRepository>();services.AddScoped<IShopperCustomerService,ShopperCustomerService>();services.AddScoped<IHouseholdsVisitsRepository,HouseholdsVisitsRepository>();services.AddScoped<IHouseholdsVisitsService,HouseholdsVisitsService>();
        services.AddScoped<IRetailBillingRepository,RetailBillingRepository>();services.AddScoped<IRetailBillingService,RetailBillingService>();services.AddScoped<IPreferencesVoiceService,PreferencesVoiceService>();
        services.AddSingleton<AlertDeduplicationCoordinator>();services.AddScoped<IAlertsRealtimeService,AlertsRealtimeService>();services.AddScoped<INotificationOutboxProcessor,NotificationOutboxProcessor>();
        services.AddScoped<IIntegrationManagementService,IntegrationManagementService>();services.AddScoped<IInboundIntegrationService,InboundIntegrationService>();services.AddScoped<IIntegrationOutboxProcessor,IntegrationOutboxProcessor>();
        services.AddScoped<ICameraTrackingService,CameraTrackingService>();
        services.AddScoped<IRecognitionService,RecognitionService>();services.AddSingleton<IRecognitionTemplateProtector,AesGcmRecognitionTemplateProtector>();
        services.AddScoped<IReportQueryRepository,DapperReportQueryRepository>();services.AddScoped<IReportsExportsService,ReportsExportsService>();services.AddScoped<IExportJobProcessor,ExportJobProcessor>();services.AddSingleton<IExportFileStore,LocalExportFileStore>();services.AddSingleton<IExportDownloadTokenService,ExportDownloadTokenService>();
        services.AddScoped<IOperationalPlatformService,OperationalPlatformService>();services.AddScoped<IWorkerRuntimeGate,WorkerRuntimeGate>();services.AddScoped<IRetentionProcessor,OperationalRetentionProcessor>();
        return services;
    }
}
