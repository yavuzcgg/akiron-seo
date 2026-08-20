using System.Text.Json;
using System.Text.RegularExpressions;
using AkironSeo.Application.Common;
using AkironSeo.Application.Common.Exceptions;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Application.Common.Security;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Services;

public class WebCrawlerService : IWebCrawlerService
{
    private readonly IAkironDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly IRobotsTxtAuditorService _robotsTxtAuditorService;
    private readonly IQuotaLedgerService _quotaLedgerService;

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

        int totalPages = 0;

        try
        {
            // 2. Perform HTTP Fetch. The domain comes from tenant input, so it is screened
            // against loopback/private/link-local ranges before the server connects.
            var targetUrl = (await OutboundUrlGuard.EnsureSafeAsync(website.DomainUrl, cancellationToken)).ToString();

            var response = await _httpClient.GetAsync(targetUrl, cancellationToken);
            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            // 3. Extract rich metadata from HTML
            var title = ExtractTag(html, "<title>", "</title>") ?? website.Name;
            var metaDesc = ExtractMetaContent(html, "description") ?? "No meta description configured.";
            var canonicalUrl = ExtractCanonicalUrl(html) ?? string.Empty;
            var h1Tags = ExtractAllH1Tags(html);
            var ogTags = ExtractOpenGraphTags(html);
            var robotsMeta = ExtractMetaContent(html, "robots") ?? string.Empty;

            totalPages = 1;

            // 4. Build issues list from extracted data
            var issues = AnalyzePageIssues(
                title, metaDesc, canonicalUrl, h1Tags, ogTags,
                robotsMeta, (int)response.StatusCode, targetUrl);

            // 5. Calculate weighted score
            var scoreBreakdown = CalculateScoreBreakdown(
                title, metaDesc, canonicalUrl, h1Tags, ogTags,
                robotsMeta, (int)response.StatusCode);

            int overallScore = Math.Min(scoreBreakdown.Sum(c => c.EarnedPoints), 100);

            // 6. Create CrawlResult with rich data
            var crawlResult = new CrawlResult
            {
                TenantId = tenantId,
                CrawlJobId = crawlJob.Id,
                PageUrl = targetUrl,
                StatusCode = (int)response.StatusCode,
                Title = title,
                MetaDescription = metaDesc,
                CanonicalUrl = canonicalUrl,
                H1Json = JsonSerializer.Serialize(h1Tags),
                IssuesJson = JsonSerializer.Serialize(issues),
                ScoreBreakdownJson = JsonSerializer.Serialize(scoreBreakdown)
            };
            _dbContext.CrawlResults.Add(crawlResult);

            // Complete CrawlJob
            crawlJob.Status = CrawlStatusEnum.Completed;
            crawlJob.CompletedAt = DateTime.UtcNow;
            crawlJob.PagesDiscovered = totalPages;

            // 7. Run robots.txt audit and persist results
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

            // 8. Create SeoAudit linked 1-to-1 to CrawlJob
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

            // 9. Create SiteSnapshot summary
            var snapshot = new SiteSnapshot
            {
                TenantId = tenantId,
                WebsiteId = websiteId,
                SeoAuditId = seoAudit.Id,
                TotalPagesCount = totalPages,
                TotalIssuesCount = issues.Count,
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

    // ────────────────────────────────────────────────────────────
    // SEO Scoring Engine (100-point weighted system)
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Produces the per-component score contributions. The overall score is their sum, so
    /// the two can never disagree — the client renders these rather than reimplementing the
    /// rules, which is how the previous UI ended up reporting fixed values for the two
    /// components it had no data for.
    /// </summary>
    private static List<ScoreComponent> CalculateScoreBreakdown(
        string title, string metaDesc, string canonicalUrl,
        List<string> h1Tags, Dictionary<string, string> ogTags,
        string robotsMeta, int statusCode)
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

            new ScoreComponent("Title Length", 5, title.Length <= 65 ? 5 : 0),

            new ScoreComponent("Meta Length", 5,
                metaDesc.Length <= 165 || hasDefaultMetaDesc ? 5 : 0),

            new ScoreComponent("Heading Hierarchy", 5, h1Tags.Count <= 1 ? 5 : 0)
        ];
    }

    private static List<CrawlIssue> AnalyzePageIssues(
        string title, string metaDesc, string canonicalUrl,
        List<string> h1Tags, Dictionary<string, string> ogTags,
        string robotsMeta, int statusCode, string pageUrl)
    {
        var issues = new List<CrawlIssue>();

        // HTTP status check
        if (statusCode < 200 || statusCode >= 300)
        {
            issues.Add(new CrawlIssue("HTTP_ERROR", "Critical",
                $"Page returned HTTP {statusCode} status code.",
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

    // ────────────────────────────────────────────────────────────
    // HTML Extraction Helpers
    // ────────────────────────────────────────────────────────────

    private static string? ExtractTag(string html, string startTag, string endTag)
    {
        int startIndex = html.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1) return null;
        startIndex += startTag.Length;

        int endIndex = html.IndexOf(endTag, startIndex, StringComparison.OrdinalIgnoreCase);
        if (endIndex == -1) return null;

        return html.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private static string? ExtractMetaContent(string html, string name)
    {
        // Match both name="X" content="Y" and content="Y" name="X" orderings
        var patterns = new[]
        {
            $@"<meta\s+name\s*=\s*[""']{name}[""']\s+content\s*=\s*[""']([^""']*)[""']",
            $@"<meta\s+content\s*=\s*[""']([^""']*)[""']\s+name\s*=\s*[""']{name}[""']"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
        }

        return null;
    }

    private static string? ExtractCanonicalUrl(string html)
    {
        var match = Regex.Match(html,
            @"<link\s+[^>]*rel\s*=\s*[""']canonical[""'][^>]*href\s*=\s*[""']([^""']*)[""']",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (match.Success)
        {
            return match.Groups[1].Value.Trim();
        }

        // Also try href before rel
        match = Regex.Match(html,
            @"<link\s+[^>]*href\s*=\s*[""']([^""']*)[""'][^>]*rel\s*=\s*[""']canonical[""']",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private static List<string> ExtractAllH1Tags(string html)
    {
        var h1Tags = new List<string>();
        var matches = Regex.Matches(html, @"<h1[^>]*>(.*?)</h1>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            // Strip inner HTML tags to get plain text
            var text = Regex.Replace(match.Groups[1].Value, @"<[^>]+>", "").Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                h1Tags.Add(text);
            }
        }

        return h1Tags;
    }

    private static Dictionary<string, string> ExtractOpenGraphTags(string html)
    {
        var ogTags = new Dictionary<string, string>();
        var ogNames = new[] { "og:title", "og:description", "og:image", "og:url", "og:type" };

        foreach (var ogName in ogNames)
        {
            var patterns = new[]
            {
                $@"<meta\s+property\s*=\s*[""']{Regex.Escape(ogName)}[""']\s+content\s*=\s*[""']([^""']*)[""']",
                $@"<meta\s+content\s*=\s*[""']([^""']*)[""']\s+property\s*=\s*[""']{Regex.Escape(ogName)}[""']"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (match.Success)
                {
                    ogTags[ogName] = match.Groups[1].Value.Trim();
                    break;
                }
            }
        }

        return ogTags;
    }
}

/// <summary>
/// Serializable crawl issue record stored in CrawlResult.IssuesJson.
/// </summary>
public record CrawlIssue(string Code, string Severity, string Description, string Recommendation);

/// <summary>One weighted contribution to the overall SEO score.</summary>
public record ScoreComponent(string Label, int MaxPoints, int EarnedPoints);
