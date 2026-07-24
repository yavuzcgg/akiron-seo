using AkironSeo.API.Middleware;
using AkironSeo.Application.Auth.Dtos;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Persistence;
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

// Add Services
builder.Services.AddScoped<ITenantContext, TenantContext>();

// Database Context (PostgreSQL or InMemory for fallback)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Host=localhost;Port=5432;Database=akironseo_db;Username=akiron_user;Password=akiron_password";

builder.Services.AddDbContext<AkironDbContext>((sp, options) =>
{
    options.UseNpgsql(connectionString);
});

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

app.Run();
