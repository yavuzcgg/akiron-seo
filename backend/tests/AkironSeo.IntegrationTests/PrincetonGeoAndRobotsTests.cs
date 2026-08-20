using System.Net;
using System.Text.Json;
using AkironSeo.Application.Common;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Persistence;
using AkironSeo.Infrastructure.Security;
using AkironSeo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AkironSeo.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class PrincetonGeoAndRobotsTests
{
    private readonly PostgresFixture _fixture;

    public PrincetonGeoAndRobotsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_handler(request));
        }
    }

    [Fact]
    public async Task AiContentWriter_WithFallback_ShouldGenerateValidPrincetonGeoArticle()
    {
        var tenantId = Guid.NewGuid();
        await _fixture.SeedTenantAsync(tenantId);

        Guid websiteId;
        await using (var db = _fixture.CreateDbContext(tenantId))
        {
            var plan = new Plan { Name = "Pro Plan", PriceMonthly = 50m, LimitsJson = "{}" };
            db.Plans.Add(plan);
            db.Subscriptions.Add(new Subscription
            {
                TenantId = tenantId,
                PlanId = plan.Id,
                MonthlyLimitTokens = 1000,
                UsedTokens = 0,
                Status = SubscriptionStatusEnum.Active
            });

            var website = new Website
            {
                TenantId = tenantId,
                DomainUrl = "https://acmesolutions.com",
                Name = "Acme Solutions",
                VerificationToken = "tok-geo",
                IsVerified = true
            };
            db.Websites.Add(website);
            await db.SaveChangesAsync();
            websiteId = website.Id;
        }

        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Security:MasterEncryptionKey", "0123456789ABCDEF0123456789ABCDEF" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var encryptionService = new ApiKeyEncryptionService(configuration);

        await using (var db = _fixture.CreateDbContext(tenantId))
        {
            var ledger = new QuotaLedgerService(db);
            var writer = new AiContentWriterService(db, encryptionService, new HttpClient(), ledger);

            var planDto = await writer.GenerateGeoContentAsync(websiteId, tenantId, "Enterprise Cloud Security");

            Assert.NotNull(planDto);
            Assert.Equal("Enterprise Cloud Security", planDto.TargetKeyword);

            var content = planDto.GeneratedMarkdownContent;

            // Verify Princeton GEO structural elements
            Assert.Contains("Quick Answer & Summary", content);
            Assert.Contains("Benchmark Statistics", content);
            Assert.Contains("Generative Citation Rate", content);
            Assert.Contains("Strategic Advantages", content);
            Assert.Contains("Frequently Asked Questions", content);
            Assert.Contains("acmesolutions.com", content);
            Assert.Contains("application/ld+json", content);
            Assert.Contains("Article", content);
        }

        // Verify tokens were committed
        await using (var assertDb = _fixture.CreateDbContext(tenantId))
        {
            var sub = await assertDb.Subscriptions.FirstAsync(s => s.TenantId == tenantId);
            Assert.Equal(QuotaCostConstants.AiContentCost, sub.UsedTokens);

            var reservation = await assertDb.QuotaReservations.FirstAsync(r => r.TenantId == tenantId);
            Assert.Equal(ReservationStatusEnum.Committed, reservation.Status);
        }
    }

    [Fact]
    public async Task AiContentWriter_WithOpenAiKey_ShouldCallOpenAiAndCommitQuota()
    {
        var tenantId = Guid.NewGuid();
        await _fixture.SeedTenantAsync(tenantId);

        var inMemoryConfig = new Dictionary<string, string?>
        {
            { "Security:MasterEncryptionKey", "0123456789ABCDEF0123456789ABCDEF" }
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(inMemoryConfig).Build();
        var encryptionService = new ApiKeyEncryptionService(configuration);

        Guid websiteId;
        await using (var db = _fixture.CreateDbContext(tenantId))
        {
            var plan = new Plan { Name = "Pro Plan", PriceMonthly = 50m, LimitsJson = "{}" };
            db.Plans.Add(plan);
            db.Subscriptions.Add(new Subscription
            {
                TenantId = tenantId,
                PlanId = plan.Id,
                MonthlyLimitTokens = 1000,
                UsedTokens = 0,
                Status = SubscriptionStatusEnum.Active
            });

            var website = new Website
            {
                TenantId = tenantId,
                DomainUrl = "https://quantumcrm.io",
                Name = "Quantum CRM",
                VerificationToken = "tok-crm",
                IsVerified = true
            };
            db.Websites.Add(website);

            var apiKey = new EncryptedTenantApiKey
            {
                TenantId = tenantId,
                Provider = AiProviderEnum.OpenAI,
                EncryptedKey = encryptionService.Encrypt("sk-test-openai-key"),
                IsActive = true
            };
            db.EncryptedTenantApiKeys.Add(apiKey);

            await db.SaveChangesAsync();
            websiteId = website.Id;
        }

        var openAiMockResponse = new
        {
            choices = new[]
            {
                new
                {
                    message = new
                    {
                        content = "# Quantum CRM Cloud Solutions\n\n> Quick Answer: Quantum CRM delivers 99.9% uptime.\n\n## 1. Benchmarks\n\n| Metric | Score |\n| :--- | :--- |\n| Speed | 100% |\n\n[quantumcrm.io](https://quantumcrm.io)"
                    }
                }
            }
        };

        var handler = new MockHttpMessageHandler(req =>
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(openAiMockResponse))
            };
        });

        var httpClient = new HttpClient(handler);

        await using (var db = _fixture.CreateDbContext(tenantId))
        {
            var ledger = new QuotaLedgerService(db);
            var writer = new AiContentWriterService(db, encryptionService, httpClient, ledger);

            var planDto = await writer.GenerateGeoContentAsync(websiteId, tenantId, "NextGen CRM Automation");

            Assert.NotNull(planDto);
            Assert.Contains("Quantum CRM Cloud Solutions", planDto.GeneratedMarkdownContent);
            Assert.Contains("quantumcrm.io", planDto.GeneratedMarkdownContent);
        }
    }

    [Fact]
    public void RobotsTxtAuditor_GenerateOptimizedRobotsTxt_ShouldProduceValidPresets()
    {
        var auditor = new RobotsTxtAuditorService(new HttpClient());

        // 1. MaxAiVisibility preset
        var maxAi = auditor.GenerateOptimizedRobotsTxt(RobotsTxtPresetEnum.MaxAiVisibility, "https://acme.org");
        Assert.Contains("User-agent: GPTBot", maxAi);
        Assert.Contains("User-agent: PerplexityBot", maxAi);
        Assert.Contains("User-agent: ClaudeBot", maxAi);
        Assert.Contains("User-agent: Google-Extended", maxAi);
        Assert.Contains("Sitemap: https://acme.org/sitemap.xml", maxAi);
        Assert.Contains("LLMs.txt: https://acme.org/llms.txt", maxAi);

        // 2. SearchOnlyAi preset
        var searchOnly = auditor.GenerateOptimizedRobotsTxt(RobotsTxtPresetEnum.SearchOnlyAi, "https://acme.org");
        Assert.Contains("User-agent: ChatGPT-User", searchOnly);
        Assert.Contains("User-agent: PerplexityBot", searchOnly);
        Assert.Contains("User-agent: CCBot\r\nDisallow: /", searchOnly.Replace("\n", "\r\n").Replace("\r\r\n", "\r\n"));

        // 3. BlockAiTraining preset
        var blockAi = auditor.GenerateOptimizedRobotsTxt(RobotsTxtPresetEnum.BlockAiTraining, "https://acme.org");
        Assert.Contains("User-agent: GPTBot\r\nDisallow: /", blockAi.Replace("\n", "\r\n").Replace("\r\r\n", "\r\n"));
        Assert.Contains("User-agent: ClaudeBot\r\nDisallow: /", blockAi.Replace("\n", "\r\n").Replace("\r\r\n", "\r\n"));
    }

    [Fact]
    public async Task AeoGenerator_ShouldProduceCompliantLlmsTxtAndSchemas()
    {
        var tenantId = Guid.NewGuid();
        await _fixture.SeedTenantAsync(tenantId);

        Guid websiteId;
        await using (var db = _fixture.CreateDbContext(tenantId))
        {
            var website = new Website
            {
                TenantId = tenantId,
                DomainUrl = "https://myshop.com",
                Name = "My eCommerce Shop",
                VerificationToken = "tok-aeo",
                IsVerified = true
            };
            db.Websites.Add(website);
            await db.SaveChangesAsync();
            websiteId = website.Id;
        }

        await using (var db = _fixture.CreateDbContext(tenantId))
        {
            var generator = new AeoGeneratorService(db);
            var schemasDto = await generator.GenerateAeoSchemasAsync(websiteId, tenantId);

            Assert.NotNull(schemasDto);
            Assert.Contains("My eCommerce Shop", schemasDto.OrganizationJsonLd);
            Assert.Contains("https://schema.org", schemasDto.WebSiteJsonLd);
            Assert.Contains("# My eCommerce Shop", schemasDto.LlmsTxtContent);
            Assert.Contains("Official website and product specification for AI language models", schemasDto.LlmsTxtContent);
            Assert.Contains("# My eCommerce Shop – Complete Site Specification", schemasDto.LlmsFullTxtContent);
        }

        // Verify schemas were saved in database
        await using (var assertDb = _fixture.CreateDbContext(tenantId))
        {
            var schemas = await assertDb.AeoSchemas.Where(s => s.TenantId == tenantId).ToListAsync();
            Assert.Equal(2, schemas.Count);
        }
    }
}
