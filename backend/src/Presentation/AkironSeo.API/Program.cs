using System.Text;
using AkironSeo.API.Endpoints;
using AkironSeo.API.Middleware;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Application.Websites.Queries;
using AkironSeo.Infrastructure.Persistence;
using AkironSeo.Infrastructure.Security;
using AkironSeo.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

builder.Services.AddAuthorization();

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
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TenantResolverMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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

app.Run();
