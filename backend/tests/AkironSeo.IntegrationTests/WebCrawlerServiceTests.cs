using System.Net;
using System.Text.Json;
using AkironSeo.Application.Common;
using AkironSeo.Domain.Entities.Global;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using AkironSeo.Infrastructure.Persistence;
using AkironSeo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AkironSeo.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class WebCrawlerServiceTests
{
    private readonly PostgresFixture _fixture;

    public WebCrawlerServiceTests(PostgresFixture fixture)
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
    public async Task WebCrawler_WithAngleSharp_ShouldParseDomAndAuditMultiplePages()
    {
        var tenantId = Guid.NewGuid();
        await _fixture.SeedTenantAsync(tenantId);

        Guid websiteId;
        await using (var db = _fixture.CreateDbContext(tenantId))
        {
            var plan = new Plan { Name = "Agency Plan", PriceMonthly = 100m, LimitsJson = "{}" };
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
                DomainUrl = "https://example.com",
                Name = "Example Domain",
                VerificationToken = "tok-1",
                IsVerified = true
            };
            db.Websites.Add(website);
            await db.SaveChangesAsync();
            websiteId = website.Id;
        }

        var sitemapXml = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<urlset xmlns=""http://www.sitemaps.org/schemas/sitemap/0.9"">
  <url><loc>https://example.com/blog</loc></url>
</urlset>";

        var homeHtml = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <title>Example Domain - Best SEO and AI Platform</title>
    <meta name=""description"" content=""Supercharge your search rankings with our next generation AI SEO platform. Start optimizing your organic presence today."">
    <link rel=""canonical"" href=""https://example.com"">
    <meta property=""og:title"" content=""Example Domain SEO"">
    <meta property=""og:image"" content=""https://example.com/og.jpg"">
</head>
<body>
    <h1>Next Generation AI SEO Platform</h1>
    <a href=""/features"">Features</a>
    <a href=""https://example.com/pricing"">Pricing</a>
    <img src=""/hero.png""> <!-- Missing alt -->
</body>
</html>";

        var featuresHtml = @"<!DOCTYPE html>
<html>
<head>
    <title>Features - Example Domain</title>
    <meta name=""description"" content=""Explore our enterprise SEO audit and rank tracking features designed for modern digital agencies."">
</head>
<body>
    <h1>Powerful Features</h1>
    <img src=""/feature1.png"" alt=""Keyword Tracker"">
</body>
</html>";

        var pricingHtml = @"<!DOCTYPE html>
<html>
<head>
    <title>Pricing Plans - Example Domain</title>
</head>
<body>
    <h1>Transparent Pricing</h1>
</body>
</html>";

        var blogHtml = @"<!DOCTYPE html>
<html>
<head>
    <title>Blog - Example Domain SEO Insights</title>
    <meta name=""description"" content=""Read the latest search engine optimization articles, generative engine optimization guides, and case studies."">
</head>
<body>
    <h1>SEO Blog & Industry Insights</h1>
</body>
</html>";

        var robotsTxt = @"User-agent: *
Allow: /
Sitemap: https://example.com/sitemap.xml";

        var handler = new MockHttpMessageHandler(req =>
        {
            var path = req.RequestUri!.PathAndQuery;
            if (path == "/sitemap.xml" || path == "/sitemap_index.xml")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(sitemapXml, System.Text.Encoding.UTF8, "application/xml")
                };
            }
            if (path == "/robots.txt")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(robotsTxt, System.Text.Encoding.UTF8, "text/plain")
                };
            }
            if (path == "/features")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(featuresHtml, System.Text.Encoding.UTF8, "text/html")
                };
            }
            if (path == "/pricing")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(pricingHtml, System.Text.Encoding.UTF8, "text/html")
                };
            }
            if (path == "/blog")
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(blogHtml, System.Text.Encoding.UTF8, "text/html")
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(homeHtml, System.Text.Encoding.UTF8, "text/html")
            };
        });

        var httpClient = new HttpClient(handler);

        await using (var db = _fixture.CreateDbContext(tenantId))
        {
            var ledger = new QuotaLedgerService(db);
            var robotsService = new RobotsTxtAuditorService(httpClient);
            var crawler = new WebCrawlerService(db, httpClient, robotsService, ledger);

            var audit = await crawler.CrawlAndAuditWebsiteAsync(websiteId, tenantId);

            Assert.NotNull(audit);
            Assert.True(audit.OverallScore > 0);
        }

        await using (var assertDb = _fixture.CreateDbContext(tenantId))
        {
            var results = await assertDb.CrawlResults
                .Where(r => r.TenantId == tenantId)
                .ToListAsync();

            Assert.True(results.Count >= 3, $"Expected at least 3 crawled pages, but got {results.Count}");

            var homeResult = results.FirstOrDefault(r => r.PageUrl.EndsWith("example.com") || r.PageUrl.EndsWith("example.com/"));
            Assert.NotNull(homeResult);
            Assert.Contains("Example Domain - Best SEO and AI Platform", homeResult.Title);
            Assert.Contains("Next Generation AI SEO Platform", homeResult.H1Json);
            Assert.Contains("IMAGES_MISSING_ALT", homeResult.IssuesJson);

            var snapshot = await assertDb.SiteSnapshots.FirstOrDefaultAsync(s => s.TenantId == tenantId);
            Assert.NotNull(snapshot);
            Assert.True(snapshot.TotalPagesCount >= 3);
            Assert.True(snapshot.Score > 0);
        }
    }
}
