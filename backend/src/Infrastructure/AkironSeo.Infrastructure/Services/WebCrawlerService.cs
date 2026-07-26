using System.Text.Json;
using System.Text.RegularExpressions;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Entities.TenantScoped;
using AkironSeo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Services;

public class WebCrawlerService : IWebCrawlerService
{
    private readonly IAkironDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly IRobotsTxtAuditorService _robotsTxtAuditorService;

    public WebCrawlerService(
        IAkironDbContext dbContext,
        HttpClient httpClient,
        IRobotsTxtAuditorService robotsTxtAuditorService)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _robotsTxtAuditorService = robotsTxtAuditorService;
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

        int totalPages = 0;

        try
        {
            // 2. Perform HTTP Fetch
            var targetUrl = website.DomainUrl.StartsWith("http")
                ? website.DomainUrl
                : $"https://{website.DomainUrl}";

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
            int overallScore = CalculateWeightedScore(
                title, metaDesc, canonicalUrl, h1Tags, ogTags,
                robotsMeta, (int)response.StatusCode);

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
                IssuesJson = JsonSerializer.Serialize(issues)
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

            return seoAudit;
        }
        catch
        {
            crawlJob.Status = CrawlStatusEnum.Failed;
            crawlJob.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    // ────────────────────────────────────────────────────────────
    // SEO Scoring Engine (100-point weighted system)
    // ────────────────────────────────────────────────────────────

    private static int CalculateWeightedScore(
        string title, string metaDesc, string canonicalUrl,
        List<string> h1Tags, Dictionary<string, string> ogTags,
        string robotsMeta, int statusCode)
    {
        int score = 0;

        // HTTP Status (15 pts)
        if (statusCode >= 200 && statusCode < 300) score += 15;

        // Title length: 30-60 chars ideal (15 pts)
        if (title.Length >= 30 && title.Length <= 60) score += 15;
        else if (title.Length >= 20 && title.Length <= 65) score += 10;
        else if (title.Length > 0) score += 5;

        // Meta description: 120-160 chars ideal (15 pts)
        if (metaDesc.Length >= 120 && metaDesc.Length <= 160) score += 15;
        else if (metaDesc.Length >= 50 && metaDesc.Length <= 165) score += 10;
        else if (metaDesc.Length > 0 && metaDesc != "No meta description configured.") score += 5;

        // H1 presence: exactly 1 H1 (10 pts)
        if (h1Tags.Count == 1) score += 10;
        else if (h1Tags.Count > 1) score += 5;

        // Canonical URL present (10 pts)
        if (!string.IsNullOrEmpty(canonicalUrl)) score += 10;

        // OpenGraph tags: og:title + og:image (10 pts)
        bool hasOgTitle = ogTags.ContainsKey("og:title");
        bool hasOgImage = ogTags.ContainsKey("og:image");
        if (hasOgTitle && hasOgImage) score += 10;
        else if (hasOgTitle || hasOgImage) score += 5;

        // Robots meta: not noindex (10 pts)
        bool isNoIndex = robotsMeta.Contains("noindex", StringComparison.OrdinalIgnoreCase);
        if (!isNoIndex) score += 10;

        // Title not too long (5 pts)
        if (title.Length <= 65) score += 5;

        // Meta desc not too long (5 pts)
        if (metaDesc.Length <= 165 || metaDesc == "No meta description configured.") score += 5;

        // No multiple H1s (5 pts)
        if (h1Tags.Count <= 1) score += 5;

        return Math.Min(score, 100);
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
