using System.Net.Http.Headers;
using System.Text.Json;
using System.Xml.Linq;
using AkironSeo.Application.Common;
using AkironSeo.Application.Common.Exceptions;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Application.Common.Security;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Services;

public class WebCrawlerService : IWebCrawlerService
{
    private const int DefaultMaxCrawlPages = 5;
    private readonly IAkironDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly IRobotsTxtAuditorService _robotsTxtAuditorService;
    private readonly IQuotaLedgerService _quotaLedgerService;
    private readonly HtmlParser _htmlParser;

    public WebCrawlerService(
        IAkironDbContext dbContext,
        HttpClient httpClient,
        IRobotsTxtAuditorService robotsTxtAuditorService,
        IQuotaLedgerService quotaLedgerService)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _robotsTxtAuditorService = robotsTxtAuditorService;
        _quotaLedgerService = quotaLedgerService;
        _htmlParser = new HtmlParser();
    }

    public async Task<SeoAudit> CrawlAndAuditWebsiteAsync(Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId && w.TenantId == tenantId, cancellationToken);

        if (website == null)
        {
            throw new KeyNotFoundException("Website not found.");
        }

        // 1. Create CrawlJob
        var crawlJob = new CrawlJob
        {
            TenantId = tenantId,
            WebsiteId = websiteId,
            Status = CrawlStatusEnum.Running,
            StartedAt = DateTime.UtcNow
        };
        _dbContext.CrawlJobs.Add(crawlJob);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var jobId = $"crawl-{crawlJob.Id}";
        var reserved = await _quotaLedgerService.ReserveQuotaAsync(tenantId, jobId, QuotaCostConstants.CrawlCost, cancellationToken);
        if (!reserved)
        {
            crawlJob.Status = CrawlStatusEnum.Failed;
            crawlJob.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new QuotaExceededException($"Insufficient quota for website crawl. Required tokens: {QuotaCostConstants.CrawlCost}.");
        }

        try
        {
            // 2. Validate and normalize root target URL
            var rootUri = await OutboundUrlGuard.EnsureSafeAsync(website.DomainUrl, cancellationToken);
            var rootUrl = rootUri.ToString();
            var targetHost = rootUri.Host.ToLowerInvariant();

            var urlQueue = new Queue<string>();
            var visitedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            urlQueue.Enqueue(rootUrl);

            // 3. Sitemap.xml discovery
            var sitemapUrls = await DiscoverSitemapUrlsAsync(rootUri, targetHost, cancellationToken);
            foreach (var sUrl in sitemapUrls)
            {
                if (!visitedUrls.Contains(sUrl) && !urlQueue.Contains(sUrl))
                {
                    urlQueue.Enqueue(sUrl);
                }
            }

            var crawlResults = new List<CrawlResult>();
            var allIssues = new List<CrawlIssue>();
            var pageScores = new List<int>();

            // 4. Multi-page Crawl Loop using AngleSharp
            while (urlQueue.Count > 0 && visitedUrls.Count < DefaultMaxCrawlPages)
            {
                var currentUrl = urlQueue.Dequeue();
                var normalizedCurrent = NormalizeUrl(currentUrl);

                if (visitedUrls.Contains(normalizedCurrent))
                {
                    continue;
                }

                visitedUrls.Add(normalizedCurrent);

                try
                {
                    // Screen every outbound URL for SSRF
                    var safeUri = await OutboundUrlGuard.EnsureSafeAsync(currentUrl, cancellationToken);
                    using var response = await _httpClient.GetAsync(safeUri, cancellationToken);
                    var statusCode = (int)response.StatusCode;
                    var html = await response.Content.ReadAsStringAsync(cancellationToken);

                    // Parse HTML using AngleSharp DOM parser
                    using var document = await _htmlParser.ParseDocumentAsync(html, cancellationToken);

                    var title = document.Title?.Trim() ?? website.Name;
                    var metaDesc = document.QuerySelector("meta[name='description' i]")?.GetAttribute("content")?.Trim()
                                   ?? "No meta description configured.";
                    var canonicalUrl = document.QuerySelector("link[rel='canonical' i]")?.GetAttribute("href")?.Trim() ?? string.Empty;
                    var robotsMeta = document.QuerySelector("meta[name='robots' i]")?.GetAttribute("content")?.Trim() ?? string.Empty;

                    var h1Tags = document.QuerySelectorAll("h1")
                        .Select(h => h.TextContent.Trim())
                        .Where(t => !string.IsNullOrWhiteSpace(t))
                        .ToList();

                    var ogTags = ExtractOpenGraphTags(document);
                    var images = document.QuerySelectorAll("img");
                    int missingAltCount = images.Count(img => string.IsNullOrWhiteSpace(img.GetAttribute("alt")));

                    // Discover internal links to crawl next
                    var links = document.QuerySelectorAll("a[href]")
                        .Select(a => a.GetAttribute("href"))
                        .Where(h => !string.IsNullOrWhiteSpace(h))
                        .ToList();

                    foreach (var link in links)
                    {
                        if (Uri.TryCreate(safeUri, link, out var resolvedUri) &&
                            (resolvedUri.Scheme == Uri.UriSchemeHttp || resolvedUri.Scheme == Uri.UriSchemeHttps) &&
                            resolvedUri.Host.Equals(targetHost, StringComparison.OrdinalIgnoreCase))
                        {
                            var cleanResolved = NormalizeUrl(resolvedUri.ToString());
                            if (!visitedUrls.Contains(cleanResolved) && !urlQueue.Contains(cleanResolved))
                            {
                                urlQueue.Enqueue(cleanResolved);
                            }
                        }
                    }

                    // Analyze Issues & Calculate Score
                    var pageIssues = AnalyzePageIssues(
                        title, metaDesc, canonicalUrl, h1Tags, ogTags,
                        robotsMeta, missingAltCount, statusCode, currentUrl);

                    var scoreBreakdown = CalculateScoreBreakdown(
                        title, metaDesc, canonicalUrl, h1Tags, ogTags,
                        robotsMeta, missingAltCount, statusCode);

                    int pageScore = Math.Min(scoreBreakdown.Sum(c => c.EarnedPoints), 100);
                    pageScores.Add(pageScore);
                    allIssues.AddRange(pageIssues);

                    var crawlResult = new CrawlResult
                    {
                        TenantId = tenantId,
                        CrawlJobId = crawlJob.Id,
                        PageUrl = currentUrl,
                        StatusCode = statusCode,
                        Title = title,
                        MetaDescription = metaDesc,
                        CanonicalUrl = canonicalUrl,
                        H1Json = JsonSerializer.Serialize(h1Tags),
                        IssuesJson = JsonSerializer.Serialize(pageIssues),
                        ScoreBreakdownJson = JsonSerializer.Serialize(scoreBreakdown)
                    };

                    _dbContext.CrawlResults.Add(crawlResult);
                    crawlResults.Add(crawlResult);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Record failed page fetch and continue with remaining pages
                    var failedResult = new CrawlResult
                    {
                        TenantId = tenantId,
                        CrawlJobId = crawlJob.Id,
                        PageUrl = currentUrl,
                        StatusCode = 500,
                        Title = website.Name,
                        MetaDescription = "Fetch failed: " + ex.Message,
                        CanonicalUrl = string.Empty,
                        H1Json = "[]",
                        IssuesJson = JsonSerializer.Serialize(new[]
                        {
                            new CrawlIssue("FETCH_ERROR", "Critical", $"Failed to fetch page: {ex.Message}", "Ensure page is online and reachable.")
                        }),
                        ScoreBreakdownJson = "[]"
                    };
                    _dbContext.CrawlResults.Add(failedResult);
                    pageScores.Add(0);
                }
            }

            int totalPages = visitedUrls.Count;
            int overallScore = pageScores.Count > 0 ? (int)Math.Round(pageScores.Average()) : 0;

            // Complete CrawlJob
            crawlJob.Status = CrawlStatusEnum.Completed;
            crawlJob.CompletedAt = DateTime.UtcNow;
            crawlJob.PagesDiscovered = totalPages;

            // 5. Run robots.txt audit and persist results
            string robotsTxtAiStatusJson = "{}";
            try
            {
                var robotsAudit = await _robotsTxtAuditorService.AuditRobotsTxtAsync(website.DomainUrl, cancellationToken);
                robotsTxtAiStatusJson = JsonSerializer.Serialize(robotsAudit);
            }
            catch
            {
                // robots.txt audit is non-critical; continue with empty status
            }

            // 6. Create SeoAudit linked 1-to-1 to CrawlJob
            var seoAudit = new SeoAudit
            {
                TenantId = tenantId,
                WebsiteId = websiteId,
                CrawlJobId = crawlJob.Id,
                OverallScore = overallScore,
                RobotsTxtAiStatusJson = robotsTxtAiStatusJson,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.SeoAudits.Add(seoAudit);

            // 7. Create SiteSnapshot summary
            var snapshot = new SiteSnapshot
            {
                TenantId = tenantId,
                WebsiteId = websiteId,
                SeoAuditId = seoAudit.Id,
                TotalPagesCount = totalPages,
                TotalIssuesCount = allIssues.Count,
                Score = overallScore
            };
            _dbContext.SiteSnapshots.Add(snapshot);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _quotaLedgerService.CommitQuotaAsync(jobId, QuotaCostConstants.CrawlCost, cancellationToken);

            return seoAudit;
        }
        catch
        {
            await _quotaLedgerService.RefundQuotaAsync(jobId, CancellationToken.None);
            crawlJob.Status = CrawlStatusEnum.Failed;
            crawlJob.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private async Task<List<string>> DiscoverSitemapUrlsAsync(Uri baseUri, string targetHost, CancellationToken cancellationToken)
    {
        var discovered = new List<string>();
        var candidatePaths = new[] { "/sitemap.xml", "/sitemap_index.xml" };

        foreach (var path in candidatePaths)
        {
            try
            {
                var sitemapUri = new Uri(baseUri, path);
                var safeUri = await OutboundUrlGuard.EnsureSafeAsync(sitemapUri.ToString(), cancellationToken);

                using var request = new HttpRequestMessage(HttpMethod.Get, safeUri);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var xml = await response.Content.ReadAsStringAsync(cancellationToken);
                    var xdoc = XDocument.Parse(xml);

                    var locElements = xdoc.Descendants().Where(e => e.Name.LocalName == "loc");
                    foreach (var loc in locElements)
                    {
                        var url = loc.Value?.Trim();
                        if (!string.IsNullOrEmpty(url) &&
                            Uri.TryCreate(url, UriKind.Absolute, out var parsed) &&
                            parsed.Host.Equals(targetHost, StringComparison.OrdinalIgnoreCase))
                        {
                            discovered.Add(NormalizeUrl(url));
                            if (discovered.Count >= 20) break;
                        }
                    }

                    if (discovered.Count > 0)
                    {
                        break; // Successfully parsed primary sitemap
                    }
                }
            }
            catch
            {
                // Ignore sitemap fetch errors and continue crawl
            }
        }

        return discovered;
    }

    private static string NormalizeUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var clean = $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}".TrimEnd('/');
            if (string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/")
            {
                clean = $"{uri.Scheme}://{uri.Authority}";
            }
            return clean;
        }
        return url.TrimEnd('/');
    }

    // ────────────────────────────────────────────────────────────
    // SEO Scoring Engine (100-point weighted system)
    // ────────────────────────────────────────────────────────────

    private static List<ScoreComponent> CalculateScoreBreakdown(
        string title, string metaDesc, string canonicalUrl,
        List<string> h1Tags, Dictionary<string, string> ogTags,
        string robotsMeta, int missingAltCount, int statusCode)
    {
        bool hasOgTitle = ogTags.ContainsKey("og:title");
        bool hasOgImage = ogTags.ContainsKey("og:image");
        bool isNoIndex = robotsMeta.Contains("noindex", StringComparison.OrdinalIgnoreCase);
        bool hasDefaultMetaDesc = metaDesc == "No meta description configured.";

        return
        [
            new ScoreComponent("HTTP Status", 15,
                statusCode >= 200 && statusCode < 300 ? 15 : 0),

            new ScoreComponent("Title Tag", 15,
                title.Length >= 30 && title.Length <= 60 ? 15
                : title.Length >= 20 && title.Length <= 65 ? 10
                : title.Length > 0 ? 5 : 0),

            new ScoreComponent("Meta Description", 15,
                metaDesc.Length >= 120 && metaDesc.Length <= 160 ? 15
                : metaDesc.Length >= 50 && metaDesc.Length <= 165 ? 10
                : metaDesc.Length > 0 && !hasDefaultMetaDesc ? 5 : 0),

            new ScoreComponent("H1 Heading", 10,
                h1Tags.Count == 1 ? 10 : h1Tags.Count > 1 ? 5 : 0),

            new ScoreComponent("Canonical URL", 10,
                string.IsNullOrEmpty(canonicalUrl) ? 0 : 10),

            new ScoreComponent("OpenGraph Tags", 10,
                hasOgTitle && hasOgImage ? 10 : hasOgTitle || hasOgImage ? 5 : 0),

            new ScoreComponent("Robots Meta", 10, isNoIndex ? 0 : 10),

            new ScoreComponent("Image Alt Tags", 10, missingAltCount == 0 ? 10 : missingAltCount <= 2 ? 5 : 0),

            new ScoreComponent("Title Length", 5, title.Length <= 65 ? 5 : 0),

            new ScoreComponent("Meta Length", 5,
                metaDesc.Length <= 165 || hasDefaultMetaDesc ? 5 : 0),

            new ScoreComponent("Heading Hierarchy", 5, h1Tags.Count <= 1 ? 5 : 0)
        ];
    }

    private static List<CrawlIssue> AnalyzePageIssues(
        string title, string metaDesc, string canonicalUrl,
        List<string> h1Tags, Dictionary<string, string> ogTags,
        string robotsMeta, int missingAltCount, int statusCode, string pageUrl)
    {
        var issues = new List<CrawlIssue>();

        // HTTP status check
        if (statusCode < 200 || statusCode >= 300)
        {
            issues.Add(new CrawlIssue("HTTP_ERROR", "Critical",
                $"Page returned HTTP {statusCode} status code at {pageUrl}.",
                "Investigate server errors or redirects. Ensure the page returns a 200 OK status."));
        }

        // Title checks
        if (string.IsNullOrWhiteSpace(title))
        {
            issues.Add(new CrawlIssue("TITLE_MISSING", "Critical",
                "No <title> tag found on the page.",
                "Add a unique, descriptive title tag between 30-60 characters including your primary keyword."));
        }
        else if (title.Length < 20)
        {
            issues.Add(new CrawlIssue("TITLE_TOO_SHORT", "Warning",
                $"Title tag is too short ({title.Length} characters): \"{title}\"",
                "Expand title tag to 30-60 characters including your main target keyword for better CTR."));
        }
        else if (title.Length > 65)
        {
            issues.Add(new CrawlIssue("TITLE_TOO_LONG", "Warning",
                $"Title tag is too long ({title.Length} characters) and may be truncated in SERPs.",
                "Shorten your title to under 60 characters. Place the most important keywords at the beginning."));
        }

        // Meta description checks
        if (string.IsNullOrWhiteSpace(metaDesc) || metaDesc == "No meta description configured.")
        {
            issues.Add(new CrawlIssue("META_DESC_MISSING", "Critical",
                "No meta description found on the page.",
                "Add a compelling meta description between 120-160 characters with a clear call-to-action."));
        }
        else if (metaDesc.Length < 50)
        {
            issues.Add(new CrawlIssue("META_DESC_TOO_SHORT", "Warning",
                $"Meta description is too short ({metaDesc.Length} characters).",
                "Write a compelling meta description between 120-160 characters for maximum search CTR."));
        }
        else if (metaDesc.Length > 165)
        {
            issues.Add(new CrawlIssue("META_DESC_TOO_LONG", "Info",
                $"Meta description is too long ({metaDesc.Length} characters) and may be truncated.",
                "Keep meta description under 160 characters to avoid truncation in search results."));
        }

        // H1 checks
        if (h1Tags.Count == 0)
        {
            issues.Add(new CrawlIssue("H1_MISSING", "Warning",
                "No <h1> heading found on the page.",
                "Add exactly one H1 heading per page containing your primary keyword for SEO best practices."));
        }
        else if (h1Tags.Count > 1)
        {
            issues.Add(new CrawlIssue("H1_MULTIPLE", "Warning",
                $"Multiple H1 headings found ({h1Tags.Count}). Best practice is exactly one H1 per page.",
                "Use a single H1 for the main page heading and H2-H6 for subheadings."));
        }

        // Image Alt check
        if (missingAltCount > 0)
        {
            issues.Add(new CrawlIssue("IMAGES_MISSING_ALT", missingAltCount > 3 ? "Warning" : "Info",
                $"Found {missingAltCount} image(s) missing an 'alt' attribute.",
                "Add descriptive alt text to all informative images for improved web accessibility and image SEO indexing."));
        }

        // Canonical URL check
        if (string.IsNullOrEmpty(canonicalUrl))
        {
            issues.Add(new CrawlIssue("CANONICAL_MISSING", "Info",
                "No canonical URL specified. This may cause duplicate content issues.",
                "Add <link rel=\"canonical\"> pointing to the preferred URL to prevent duplicate content penalties."));
        }

        // OpenGraph checks
        bool hasOgTitle = ogTags.ContainsKey("og:title");
        bool hasOgImage = ogTags.ContainsKey("og:image");
        bool hasOgDesc = ogTags.ContainsKey("og:description");
        if (!hasOgTitle || !hasOgImage)
        {
            issues.Add(new CrawlIssue("OPENGRAPH_INCOMPLETE", "Info",
                $"OpenGraph tags incomplete: {(hasOgTitle ? "✓" : "✕")} og:title, {(hasOgImage ? "✓" : "✕")} og:image, {(hasOgDesc ? "✓" : "✕")} og:description.",
                "Add og:title, og:description, and og:image meta tags for rich social media previews on LinkedIn, Twitter, and WhatsApp."));
        }

        // Robots meta check
        if (robotsMeta.Contains("noindex", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new CrawlIssue("ROBOTS_NOINDEX", "Critical",
                "Page has 'noindex' robots meta tag. Search engines will NOT index this page.",
                "Remove the noindex directive unless you intentionally want to exclude this page from search results."));
        }
        if (robotsMeta.Contains("nofollow", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new CrawlIssue("ROBOTS_NOFOLLOW", "Warning",
                "Page has 'nofollow' robots meta tag. Search engines will NOT follow links on this page.",
                "Remove the nofollow directive to allow link equity to flow through this page."));
        }

        return issues;
    }

    private static Dictionary<string, string> ExtractOpenGraphTags(IHtmlDocument document)
    {
        var ogTags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var metaTags = document.QuerySelectorAll("meta[property^='og:' i], meta[name^='og:' i]");

        foreach (var meta in metaTags)
        {
            var prop = meta.GetAttribute("property") ?? meta.GetAttribute("name");
            var content = meta.GetAttribute("content");

            if (!string.IsNullOrWhiteSpace(prop) && !string.IsNullOrWhiteSpace(content))
            {
                ogTags[prop.ToLowerInvariant()] = content.Trim();
            }
        }

        return ogTags;
    }
}

public record CrawlIssue(string Code, string Severity, string Description, string Recommendation);
public record ScoreComponent(string Label, int MaxPoints, int EarnedPoints);
