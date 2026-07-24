using AkironSeo.API.Middleware;
using AkironSeo.Application.Auth.Dtos;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Application.Keywords.Commands;
using AkironSeo.Application.Websites.Commands;
using AkironSeo.Application.Websites.Queries;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Persistence;
using AkironSeo.Infrastructure.Security;
using AkironSeo.Infrastructure.Services;
using MediatR;
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
// AUTH ENDPOINTS
// ----------------------------------------------------

app.MapPost("/api/v1/auth/login", async (LoginRequestDto request, AkironDbContext db) =>
{
    var user = await db.Users
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(u => u.Email == request.Email);

    if (user == null || !DbInitializer.VerifyPassword(request.Password, user.PasswordHash))
    {
        return Results.BadRequest(new AuthResponseDto(false, "Invalid email or password.", null, null, null, null));
    }

    var tenantUser = await db.TenantUsers
        .IgnoreQueryFilters()
        .Include(tu => tu.Tenant)
        .FirstOrDefaultAsync(tu => tu.UserId == user.Id);

    var role = tenantUser?.Role.ToString() ?? "Member";
    var tenantId = tenantUser?.TenantId ?? Guid.Empty;

    return Results.Ok(new AuthResponseDto(
        Success: true,
        Message: "Login successful.",
        AccessToken: $"mock_jwt_token_{user.Id}",
        TenantId: tenantId,
        UserEmail: user.Email,
        Role: role
    ));
});

app.MapPost("/api/v1/auth/register", async (RegisterRequestDto request, AkironDbContext db) =>
{
    var existingUser = await db.Users
        .IgnoreQueryFilters()
        .AnyAsync(u => u.Email == request.Email);

    if (existingUser)
    {
        return Results.BadRequest(new AuthResponseDto(false, "Email address already registered.", null, null, null, null));
    }

    var user = new User
    {
        Email = request.Email,
        PasswordHash = DbInitializer.HashPassword(request.Password),
        FullName = request.FullName,
        IsActive = true
    };
    db.Users.Add(user);

    var tenant = new Tenant
    {
        Name = request.TenantName,
        Slug = request.TenantName.ToLowerInvariant().Replace(" ", "-")
    };
    db.Tenants.Add(tenant);

    var tenantUser = new TenantUser
    {
        TenantId = tenant.Id,
        UserId = user.Id,
        Role = UserRoleEnum.Owner
    };
    db.TenantUsers.Add(tenantUser);

    var defaultPlan = await db.Plans.IgnoreQueryFilters().FirstOrDefaultAsync();
    if (defaultPlan != null)
    {
        var subscription = new Subscription
        {
            TenantId = tenant.Id,
            PlanId = defaultPlan.Id,
            Status = SubscriptionStatusEnum.Active,
            MonthlyLimitTokens = 100000,
            UsedTokens = 0
        };
        db.Subscriptions.Add(subscription);
    }

    await db.SaveChangesAsync();

    return Results.Ok(new AuthResponseDto(
        Success: true,
        Message: "Account and Organization created successfully.",
        AccessToken: $"mock_jwt_token_{user.Id}",
        TenantId: tenant.Id,
        UserEmail: user.Email,
        Role: "Owner"
    ));
});

// ----------------------------------------------------
// WEBSITE & CRAWLER ENDPOINTS
// ----------------------------------------------------

app.MapGet("/api/v1/websites", async (Guid tenantId, ITenantContext tenantContext, IMediator mediator) =>
{
    tenantContext.SetTenantId(tenantId);
    var result = await mediator.Send(new GetWebsitesQuery());
    return Results.Ok(result);
});

app.MapPost("/api/v1/websites", async (Guid tenantId, CreateWebsiteCommand command, ITenantContext tenantContext, IMediator mediator) =>
{
    tenantContext.SetTenantId(tenantId);
    var websiteId = await mediator.Send(command);
    return Results.Ok(new { Success = true, WebsiteId = websiteId });
});

app.MapPost("/api/v1/websites/{id}/verify", async (Guid id, Guid tenantId, VerificationMethodEnum method, ITenantContext tenantContext, IMediator mediator) =>
{
    tenantContext.SetTenantId(tenantId);
    var verified = await mediator.Send(new VerifyWebsiteOwnershipCommand(id, method));
    return Results.Ok(new { Success = verified, Verified = verified });
});

app.MapPost("/api/v1/websites/{id}/crawl", async (Guid id, Guid tenantId, ITenantContext tenantContext, IWebCrawlerService crawlerService) =>
{
    tenantContext.SetTenantId(tenantId);
    var audit = await crawlerService.CrawlAndAuditWebsiteAsync(id, tenantId);
    return Results.Ok(new { Success = true, AuditId = audit.Id, Score = audit.OverallScore });
});

app.MapPost("/api/v1/keywords", async (Guid tenantId, AddTrackedKeywordCommand command, ITenantContext tenantContext, IMediator mediator) =>
{
    tenantContext.SetTenantId(tenantId);
    var keywordId = await mediator.Send(command);
    return Results.Ok(new { Success = true, KeywordId = keywordId });
});

// ----------------------------------------------------
// BYOK API KEY ENDPOINT (AES-256-GCM Encrypted)
// ----------------------------------------------------

app.MapPost("/api/v1/tenant/api-keys", async (SaveApiKeyDto request, ITenantContext tenantContext, AkironDbContext db, IApiKeyEncryptionService encryptionService) =>
{
    tenantContext.SetTenantId(request.TenantId);

    var encryptedKey = encryptionService.Encrypt(request.ApiKey);
    var existing = await db.EncryptedTenantApiKeys
        .FirstOrDefaultAsync(k => k.TenantId == request.TenantId && k.Provider == request.Provider);

    if (existing != null)
    {
        existing.EncryptedKey = encryptedKey;
        existing.IsActive = true;
    }
    else
    {
        db.EncryptedTenantApiKeys.Add(new EncryptedTenantApiKey
        {
            TenantId = request.TenantId,
            Provider = request.Provider,
            EncryptedKey = encryptedKey,
            IsActive = true
        });
    }

    await db.SaveChangesAsync();
    return Results.Ok(new { Success = true, Message = $"BYOK Encrypted API key for {request.Provider} saved successfully." });
});

app.Run();
