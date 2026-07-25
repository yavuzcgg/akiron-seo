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

// Register MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetWebsitesQuery).Assembly));

// Database Context (InMemory for dev reliability / fallback)
builder.Services.AddDbContext<AkironDbContext>((sp, options) =>
{
    options.UseInMemoryDatabase("AkironDevDb");
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

// Seed Database on Startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AkironDbContext>();
        await DbInitializer.SeedAsync(db);
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while seeding the database.");
    }
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

app.Run();
