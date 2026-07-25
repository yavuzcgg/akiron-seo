using AkironSeo.API.Endpoints;
using AkironSeo.API.Middleware;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Application.Websites.Queries;
using AkironSeo.Infrastructure.Persistence;
using AkironSeo.Infrastructure.Security;
using AkironSeo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddScoped<IGeoEngineService, GeoEngineService>();
builder.Services.AddScoped<IQuotaLedgerService, QuotaLedgerService>();
builder.Services.AddScoped<ICompetitorService, CompetitorService>();

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

// Configure CORS for Next.js Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

// Register Global Exception Handling Middleware
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseSerilogRequestLogging();

// Apply Pending Migrations & Seed Data on Startup.
// Failures are intentionally fatal: booting with an incomplete schema would fail every request.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AkironDbContext>();
    await db.Database.MigrateAsync();
    await DbInitializer.SeedAsync(db);
}

app.UseCors("FrontendCors");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ----------------------------------------------------
// MAP MODULAR ENDPOINTS
// ----------------------------------------------------
app.MapAuthEndpoints();
app.MapWebsiteEndpoints();
app.MapTenantEndpoints();
app.MapAiEndpoints();
app.MapKeywordEndpoints();
app.MapGeoEndpoints();
app.MapCompetitorEndpoints();
app.MapQuotaEndpoints();

app.Run();
