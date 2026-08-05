using System.Text;
using AkironSeo.API.Endpoints;
using AkironSeo.API.Middleware;
using AkironSeo.API.Security;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Application.Websites.Queries;
using AkironSeo.Infrastructure.Persistence;
using AkironSeo.Infrastructure.Security;
using AkironSeo.Infrastructure.Services;
using AkironSeo.Infrastructure.Services.GeoAdapters;
using DnsClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetWebsitesQuery).Assembly));

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

builder.Services.AddOpenApi();

var app = builder.Build();

// CORS is registered ahead of the exception handler so that error responses still carry
// the headers the browser needs to surface the real status code instead of an opaque failure.
app.UseCors("FrontendCors");
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseSerilogRequestLogging();

// Apply Pending Migrations & Seed Data on Startup.
// Failures are intentionally fatal: booting with an incomplete schema would fail every request.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AkironDbContext>();
    await db.Database.MigrateAsync();
    await DbInitializer.SeedAsync(db, seedSuperAdmin: app.Environment.IsDevelopment());
}

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolverMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Readiness probe for container orchestration. Verifies the database is reachable,
// which is what "ready to serve traffic" actually means for this API.
app.MapGet("/health", async (AkironDbContext db, CancellationToken cancellationToken) =>
{
    var canConnect = await db.Database.CanConnectAsync(cancellationToken);
    return canConnect
        ? Results.Ok(new { Status = "Healthy" })
        : Results.Json(new { Status = "Unhealthy" }, statusCode: StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();

// ----------------------------------------------------
// MAP MODULAR ENDPOINTS
// ----------------------------------------------------
app.MapAuthEndpoints();                  // AllowAnonymous (configured inside)
app.MapWebsiteEndpoints();
app.MapTenantEndpoints();
app.MapAiEndpoints();
app.MapKeywordEndpoints();
app.MapGeoEndpoints();
app.MapCompetitorEndpoints();
app.MapQuotaEndpoints();
app.MapNotificationEndpoints();
app.MapContentEndpoints();
app.MapAdminEndpoints();
app.MapReportEndpoints();
app.MapGscEndpoints();

app.Run();
