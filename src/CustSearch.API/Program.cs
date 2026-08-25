using CustSearch.API.Middleware;
using CustSearch.API.Security;
using CustSearch.API.PlatformTenancy;
using CustSearch.API.AlertsRealtime;
using CustSearch.API.Integrations;
using CustSearch.API.CamerasTracking;
using CustSearch.API.Recognition;
using CustSearch.API.ReportsExports;
using CustSearch.API.Operations;
using CustSearch.API.OpenApi;
using CustSearch.Application.AlertsRealtime;
using CustSearch.Application.Integrations;
using CustSearch.Application.CamerasTracking;
using CustSearch.Application.Recognition;
using CustSearch.Application.ReportsExports;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using CustSearch.Infrastructure;
using CustSearch.Infrastructure.Security;
using CustSearch.Infrastructure.Persistence;
using CustSearch.Infrastructure.ReportsExports;
using CustSearch.Integrations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    var connectionString = builder.Configuration.GetConnectionString("CustSearchDatabase")
        ?? throw new InvalidOperationException("ConnectionStrings:CustSearchDatabase is required.");

    // Controls brute-force protection without hard-coding an environment-specific request budget.
    var authRateLimitPermitCount = builder.Configuration.GetValue<int>("AuthRateLimiting:PermitLimit");
    if (authRateLimitPermitCount is < 1 or > 10_000)
    {
        throw new InvalidOperationException("AuthRateLimiting:PermitLimit must be between 1 and 10000.");
    }

    builder.Services.AddInfrastructure(connectionString);
    builder.Services.AddCustSearchIntegrations();
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
    builder.Services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();
    builder.Services.AddOptions<JwtOptions>()
        .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
        .ValidateDataAnnotations()
        .ValidateOnStart();
    var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
        ?? throw new InvalidOperationException($"Configuration section '{JwtOptions.SectionName}' is required.");
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,
                ClockSkew = TimeSpan.FromSeconds(jwtOptions.ClockSkewSeconds),
                NameClaimType = "unique_name",
                RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    // SignalR browser handshakes may carry the short-lived bearer token in the query.
                    // Normal JWT validation and database-backed session revalidation still run afterwards.
                    var accessToken = context.Request.Query["access_token"];
                    if (!Microsoft.Extensions.Primitives.StringValues.IsNullOrEmpty(accessToken)
                        && (context.HttpContext.Request.Path.StartsWithSegments("/hubs/alerts")
                            || context.HttpContext.Request.Path.StartsWithSegments("/hubs/reports")))
                    {
                        context.Token = accessToken.ToString();
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    // Active sessions are checked server-side so disabling a user, suspending a
                    // tenant or rotating its security stamp takes effect before JWT expiry.
                    var principal = context.Principal;
                    var userIdValue = principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                    var securityStamp = principal?.FindFirst(CustomClaimTypes.SecurityStamp)?.Value;
                    if (!long.TryParse(userIdValue, NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
                        || string.IsNullOrWhiteSpace(securityStamp))
                    {
                        context.Fail("The access token has no valid session identity.");
                        return;
                    }

                    try
                    {
                        var authenticationService = context.HttpContext.RequestServices
                            .GetRequiredService<IAuthenticationService>();
                        var currentUser = await authenticationService.GetCurrentUserAsync(
                            userId,
                            securityStamp,
                            context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                            context.HttpContext.TraceIdentifier,
                            context.HttpContext.RequestAborted).ConfigureAwait(false);

                        // The signed tenant scope must still match the authoritative user row.
                        // This prevents any endpoint from accepting a tenant identifier supplied by request data.
                        var tenantClaimValue = principal!.FindFirst(CustomClaimTypes.TenantId)?.Value;
                        var hasTenantClaim = long.TryParse(
                            tenantClaimValue,
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out var tenantClaimId);
                        var expectedScope = currentUser.IsPlatformAdmin ? "Platform" : "Tenant";
                        if (principal.FindFirst(CustomClaimTypes.UserScope)?.Value != expectedScope
                            || (currentUser.TenantId is null && hasTenantClaim)
                            || (currentUser.TenantId is { } tenantId
                                && (!hasTenantClaim || tenantClaimId != tenantId)))
                        {
                            context.Fail("The access token scope does not match the server identity.");
                            return;
                        }

                        // Roles and permissions are refreshed from the database on every request, so a
                        // revoked grant stops working immediately even if the signed JWT has time left.
                        if (principal.Identity is ClaimsIdentity identity)
                        {
                            foreach (var claim in identity.Claims.Where(claim => claim.Type is ClaimTypes.Role
                                         or CustomClaimTypes.Permission or CustomClaimTypes.StoreId).ToArray())
                            {
                                identity.RemoveClaim(claim);
                            }

                            identity.AddClaims(currentUser.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
                            identity.AddClaims(currentUser.Permissions.Select(permission =>
                                new Claim(CustomClaimTypes.Permission, permission)));
                            identity.AddClaims(currentUser.StoreIds.Select(storeId => new Claim(
                                CustomClaimTypes.StoreId,
                                storeId.ToString(CultureInfo.InvariantCulture))));
                        }
                    }
                    catch (AuthenticationFailureException)
                    {
                        context.Fail("The access session is no longer valid.");
                    }
                },
            };
        });
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(AuthorizationPolicyNames.PlatformScope, policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim(CustomClaimTypes.UserScope, "Platform")
            .RequireAssertion(context => !context.User.HasClaim(claim => claim.Type == CustomClaimTypes.TenantId)));
        options.AddPolicy(AuthorizationPolicyNames.TenantScope, policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim(CustomClaimTypes.UserScope, "Tenant")
            .RequireClaim(CustomClaimTypes.TenantId));
    });
    builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
    builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
    builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, ApiAuthorizationResultHandler>();
    builder.Services.AddSingleton<IValidateOptions<OperationalRedisOptions>,OperationalRedisOptionsValidator>();
    builder.Services.AddOptions<OperationalRedisOptions>().Bind(builder.Configuration.GetSection(OperationalRedisOptions.SectionName)).ValidateOnStart();
    var redisOptions=builder.Configuration.GetSection(OperationalRedisOptions.SectionName).Get<OperationalRedisOptions>()??new();
    if(redisOptions.Enabled)builder.Services.AddStackExchangeRedisCache(options=>{options.Configuration=redisOptions.ConnectionString;options.InstanceName=redisOptions.InstanceName;});else builder.Services.AddDistributedMemoryCache();
    var signalR=builder.Services.AddSignalR(options=>{options.EnableDetailedErrors=false;options.MaximumReceiveMessageSize=32*1024;});
    if(redisOptions.SignalRBackplaneEnabled)signalR.AddStackExchangeRedis(redisOptions.ConnectionString,options=>options.Configuration.AbortOnConnectFail=false);
    builder.Services.AddSingleton<IAlertConnectionMetrics,AlertConnectionMetrics>();
    builder.Services.AddSingleton<INotificationChannelAdapter,SignalRNotificationChannelAdapter>();
    builder.Services.AddScoped<AlertExceptionFilter>();
    builder.Services.AddScoped<IntegrationExceptionFilter>();
    builder.Services.AddScoped<CameraTrackingExceptionFilter>();
    builder.Services.AddScoped<RecognitionExceptionFilter>();
    builder.Services.AddScoped<ReportExportExceptionFilter>();
    builder.Services.AddScoped<OperationalExceptionFilter>();
    builder.Services.AddSingleton<IReportExportRealtimePublisher,SignalRReportExportPublisher>();
    builder.Services.AddScoped<IReportExportEventDispatcher,ReportExportEventDispatcher>();
    builder.Services.AddOptions<ReportExportOptions>().Bind(builder.Configuration.GetSection(ReportExportOptions.SectionName)).Validate(x=>x.RetentionHours is>=1 and<=168,"ReportExports:RetentionHours must be between 1 and 168.").Validate(x=>x.LeaseSeconds is>=30 and<=3600,"ReportExports:LeaseSeconds must be between 30 and 3600.").ValidateOnStart();
    builder.Services.AddOptions<IntegrationSecurityOptions>().Bind(builder.Configuration.GetSection(IntegrationSecurityOptions.SectionName)).Validate(x=>x.AllowedClockSkewSeconds is>=30 and<=900,"IntegrationSecurity:AllowedClockSkewSeconds must be between 30 and 900.").Validate(x=>x.MaximumInboundBodyBytes is>=1024 and<=1048576,"IntegrationSecurity:MaximumInboundBodyBytes must be between 1024 and 1048576.").ValidateOnStart();
    builder.Services.AddOptions<AlertsRealtimeOptions>().Bind(builder.Configuration.GetSection(AlertsRealtimeOptions.SectionName)).Validate(x=>x.PollIntervalSeconds is>=1 and<=60,"AlertsRealtime:PollIntervalSeconds must be between 1 and 60.").Validate(x=>x.BatchSize is>=1 and<=200,"AlertsRealtime:BatchSize must be between 1 and 200.").ValidateOnStart();
    builder.Services.AddOptions<CctvSecurityOptions>().Bind(builder.Configuration.GetSection(CctvSecurityOptions.SectionName)).Validate(x=>x.AllowedClockSkewSeconds is>=30 and<=900,"CctvSecurity:AllowedClockSkewSeconds must be between 30 and 900.").Validate(x=>x.MaximumBodyBytes is>=1024 and<=1048576,"CctvSecurity:MaximumBodyBytes must be between 1024 and 1048576.").ValidateOnStart();
    builder.Services.AddOptions<RecognitionSecurityOptions>().Bind(builder.Configuration.GetSection(RecognitionSecurityOptions.SectionName)).Validate(x=>x.MinimumConfidence is>=0 and<=1&&x.MinimumQuality is>=0 and<=1&&x.AmbiguityDelta is>=0 and<=1,"Recognition thresholds must be between 0 and 1.").Validate(x=>x.RetentionDaysAfterWithdrawal is>=0 and<=3650,"Recognition retention must be between 0 and 3650 days.").Validate(x=>x.HasValidEncryptionConfiguration(),"Enabled recognition requires a secret-supplied 256-bit Base64 encryption key and an opaque key reference.").ValidateOnStart();
    if(builder.Environment.IsProduction()&&builder.Configuration.GetValue<bool>("CctvRuntime:DemoMode"))throw new InvalidOperationException("CCTV Demo Mode cannot be enabled in Production.");
    builder.Services.AddHostedService<NotificationOutboxHostedService>();
    builder.Services.AddHostedService<ReportExportEventHostedService>();
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = authRateLimitPermitCount,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            }));
        options.AddPolicy("integration-inbound",httpContext=>RateLimitPartition.GetFixedWindowLimiter(httpContext.Request.RouteValues["integrationId"]?.ToString()??httpContext.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions{PermitLimit=30,Window=TimeSpan.FromMinutes(1),QueueLimit=0,AutoReplenishment=true}));
        options.AddPolicy("cctv-inbound",httpContext=>RateLimitPartition.GetFixedWindowLimiter(httpContext.Request.Headers["X-CustSearch-Service-Id"].FirstOrDefault()??httpContext.Connection.RemoteIpAddress?.ToString()??"unknown",_=>new FixedWindowRateLimiterOptions{PermitLimit=120,Window=TimeSpan.FromMinutes(1),QueueLimit=0,AutoReplenishment=true}));
    });
    builder.Services.AddControllers(options =>
        options.Filters.Add<PlatformManagementExceptionFilter>());
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options=>
    {
        options.SwaggerDoc("v1",new OpenApiInfo{Title="CustSearch AI Admin API",Version="v1",Description="Multi-tenant administrative API. Tenant and store scope are derived from the authenticated server session."});
        options.AddSecurityDefinition("Bearer",new OpenApiSecurityScheme{Name="Authorization",In=ParameterLocation.Header,Type=SecuritySchemeType.Http,Scheme="bearer",BearerFormat="JWT",Description="Short-lived CustSearch access token. Refresh tokens remain in the secure cookie flow."});
        options.OperationFilter<BearerSecurityOperationFilter>();
    });
    var healthChecks=builder.Services.AddHealthChecks().AddDbContextCheck<CustSearchDbContext>("sql-server",tags:["ready"]);
    if(redisOptions.Enabled)healthChecks.AddCheck<RedisDistributedCacheHealthCheck>("redis",failureStatus:HealthStatus.Degraded,tags:["operational"]);

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnostics, httpContext) =>
        {
            diagnostics.Set("CorrelationId", httpContext.TraceIdentifier);
            diagnostics.Set("RequestHost", httpContext.Request.Host.Value);
        };
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<AlertHub>("/hubs/alerts");
    app.MapHub<ReportExportHub>("/hubs/reports");
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false,
    });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
    });

    app.Run();
}
catch (Exception exception)
{
    Log.Fatal(exception, "CustSearch API terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Exposes the entry point to the integration-test host.
/// </summary>
public partial class Program;
