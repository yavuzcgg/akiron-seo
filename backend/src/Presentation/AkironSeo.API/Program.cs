using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using AkironSeo.API.Endpoints;
using AkironSeo.API.Middleware;
using AkironSeo.API.Security;
using AkironSeo.Application.Auth.Dtos;
using AkironSeo.Application.Common.Behaviors;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Application.Websites.Queries;
using AkironSeo.Infrastructure.Persistence;
using AkironSeo.Infrastructure.Security;
using AkironSeo.Infrastructure.Services;
using AkironSeo.Infrastructure.Services.GeoAdapters;
using DnsClient;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Refuse to boot with missing or development-placeholder secrets.
builder.Configuration.ValidateSecrets(builder.Environment);

// Configure Serilog Structured Logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add Application & Infrastructure Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IApiKeyEncryptionService, ApiKeyEncryptionService>();
builder.Services.AddScoped<IWebCrawlerService, WebCrawlerService>();
builder.Services.AddScoped<IAiOptimizationService, GeminiAiService>();
builder.Services.AddScoped<IRobotsTxtAuditorService, RobotsTxtAuditorService>();
builder.Services.AddScoped<IAeoGeneratorService, AeoGeneratorService>();
builder.Services.AddScoped<IKeywordRankTrackerService, KeywordRankTrackerService>();
// GEO adapters are resolved as a set; adding a provider here is all a new engine needs.
builder.Services.AddScoped<IGeoEngineAdapter, PerplexitySonarAdapter>();
builder.Services.AddScoped<IGeoEngineAdapter, GeminiGroundingAdapter>();
builder.Services.AddScoped<IGeoEngineAdapter, OpenAiSearchAdapter>();
builder.Services.AddScoped<IGeoEngineAdapter, AnthropicAdapter>();
builder.Services.AddScoped<IGeoEngineService, GeoEngineService>();
builder.Services.AddScoped<IQuotaLedgerService, QuotaLedgerService>();
builder.Services.AddScoped<ICompetitorService, CompetitorService>();
builder.Services.AddScoped<ICitationVerificationService, CitationVerificationService>();
builder.Services.AddScoped<IAiContentWriterService, AiContentWriterService>();
builder.Services.AddScoped<IReportExportService, ReportExportService>();
builder.Services.AddScoped<INotificationDispatcherService, NotificationDispatcherService>();
builder.Services.AddScoped<ISearchConsoleService, SearchConsoleService>();
builder.Services.AddScoped<IDnsLookupService, DnsLookupService>();
builder.Services.AddSingleton<ILookupClient>(_ => new LookupClient());
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<AuthCookieManager>();
builder.Services.AddSingleton<IBackgroundJobQueue, BackgroundJobQueue>();
builder.Services.AddHostedService<ScheduledKeywordWorker>();

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetWebsitesQuery).Assembly));
builder.Services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<UpdateQuotaRequestValidator>();

// Database Context (PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<AkironDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());
});

builder.Services.AddScoped<IAkironDbContext>(sp => sp.GetRequiredService<AkironDbContext>());

// JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AkironSeo.API",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AkironSeo.Client",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (string.IsNullOrWhiteSpace(context.Token) &&
                context.Request.Cookies.TryGetValue(AuthCookieManager.AccessCookieName, out var cookieToken))
            {
                context.Token = cookieToken;
            }

            return Task.CompletedTask;
        },
        OnTokenValidated = async context =>
        {
            var principal = context.Principal;
            var userIdValue = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                              ?? principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var tenantIdValue = principal?.FindFirstValue("tenant_id");
            var presentedRole = principal?.FindFirstValue(ClaimTypes.Role);

            if (!Guid.TryParse(userIdValue, out var userId) ||
                !Guid.TryParse(tenantIdValue, out var tenantId) ||
                string.IsNullOrWhiteSpace(presentedRole))
            {
                context.Fail("Required session claims are invalid.");
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<AkironDbContext>();
            var membership = await db.TenantUsers
                .IgnoreQueryFilters()
                .Where(candidate => candidate.UserId == userId && candidate.TenantId == tenantId)
                .Select(candidate => new
                {
                    candidate.Role,
                    UserIsActive = candidate.User.IsActive,
                    TenantIsDeleted = candidate.Tenant.IsDeleted
                })
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

            if (membership is null ||
                !membership.UserIsActive ||
                membership.TenantIsDeleted ||
                !string.Equals(membership.Role.ToString(), presentedRole, StringComparison.Ordinal))
            {
                context.Fail("The user, tenant, or role is no longer active.");
            }
        },
        OnChallenge = async context =>
        {
            context.HandleResponse();
            await WriteAuthenticationProblemAsync(
                context.HttpContext,
                StatusCodes.Status401Unauthorized,
                "Authentication required",
                "A valid active session is required.");
        },
        OnForbidden = context => WriteAuthenticationProblemAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            "Access forbidden",
            "The current session does not have permission to access this resource.")
    };
});

builder.Services.AddAkironAuthorization();

// Configure CORS for Next.js Frontend
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) => new ValueTask(WriteAuthenticationProblemAsync(
        context.HttpContext,
        StatusCodes.Status429TooManyRequests,
        "Too many requests",
        "The authentication request limit has been exceeded. Try again later."));

    options.AddPolicy("auth-login", context => CreateAuthLimiter(
        context,
        builder.Configuration.GetValue("RateLimiting:Login:PermitLimit", 5),
        TimeSpan.FromMinutes(builder.Configuration.GetValue("RateLimiting:Login:WindowMinutes", 1))));
    options.AddPolicy("auth-register", context => CreateAuthLimiter(
        context,
        builder.Configuration.GetValue("RateLimiting:Register:PermitLimit", 3),
        TimeSpan.FromMinutes(builder.Configuration.GetValue("RateLimiting:Register:WindowMinutes", 60))));
    options.AddPolicy("auth-refresh", context => CreateAuthLimiter(
        context,
        builder.Configuration.GetValue("RateLimiting:Refresh:PermitLimit", 30),
        TimeSpan.FromMinutes(builder.Configuration.GetValue("RateLimiting:Refresh:WindowMinutes", 1))));
});

builder.Services.AddOpenApi();

var app = builder.Build();

// CORS is registered ahead of the exception handler so that error responses still carry
// the headers the browser needs to surface the real status code instead of an opaque failure.
app.UseCors("FrontendCors");
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseSerilogRequestLogging();
app.UseRateLimiter();

// Apply Pending Migrations & Seed Data on Startup.
// Failures are intentionally fatal: booting with an incomplete schema would fail every request.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AkironDbContext>();
        Log.Information("Applying pending migrations...");
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied successfully.");

        // Seeds are deliberately restricted: test/demo accounts exist only in Development.
        await DbInitializer.SeedAsync(db, seedSuperAdmin: app.Environment.IsDevelopment());
        Log.Information("Database baseline seed complete.");
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Database migration/seed failed. The application will terminate.");
        throw;
    }
}

// Health Check Probe
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Service = "AkironSeo.API"
}));

// Map All Endpoint Modules
app.MapAuthEndpoints();
app.MapWebsiteEndpoints();
app.MapGeoEndpoints();
app.MapAiEndpoints();
app.MapCompetitorEndpoints();
app.MapContentEndpoints();
app.MapAdminEndpoints();
app.MapNotificationEndpoints();
app.MapReportEndpoints();
app.MapGscEndpoints();
app.MapKeywordEndpoints();
app.MapTenantEndpoints();
app.MapQuotaEndpoints();

app.Run();

static RateLimitPartition<string> CreateAuthLimiter(HttpContext context, int permitLimit, TimeSpan window)
{
    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = permitLimit,
        Window = window,
        QueueLimit = 0
    });
}

static async Task WriteAuthenticationProblemAsync(HttpContext context, int statusCode, string title, string detail)
{
    if (context.Response.HasStarted)
    {
        return;
    }

    context.Response.StatusCode = statusCode;
    context.Response.ContentType = "application/problem+json";

    var problem = new ProblemDetails
    {
        Status = statusCode,
        Title = title,
        Detail = detail,
        Instance = context.Request.Path,
        Extensions =
        {
            ["correlationId"] = context.Response.Headers.TryGetValue("X-Correlation-ID", out var cid)
                ? cid.ToString()
                : Guid.NewGuid().ToString("N"),
            ["timestamp"] = DateTime.UtcNow.ToString("o")
        }
    };

    await context.Response.WriteAsJsonAsync(problem, context.RequestAborted);
}

// Make Program public for Testcontainers Integration Tests (WebApplicationFactory)
public partial class Program { }
