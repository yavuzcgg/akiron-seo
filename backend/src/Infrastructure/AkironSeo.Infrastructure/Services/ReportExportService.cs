using System.Text;
using AkironSeo.Application.Common.Interfaces;
using AkironSeo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AkironSeo.Infrastructure.Services;

public class ReportExportService : IReportExportService
{
    private readonly IAkironDbContext _dbContext;
    private readonly IGeoEngineService _geoEngineService;

    public ReportExportService(IAkironDbContext dbContext, IGeoEngineService geoEngineService)
    {
        _dbContext = dbContext;
        _geoEngineService = geoEngineService;
    }

    public async Task<ExecutiveReportDto> GenerateExecutiveReportAsync(
        Guid websiteId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var website = await _dbContext.Websites
            .FirstOrDefaultAsync(w => w.Id == websiteId && w.TenantId == tenantId, cancellationToken);

        if (website == null)
        {
            throw new InvalidOperationException("Website not found.");
        }

        // Fetch audit data
        var audit = await _dbContext.SeoAudits
            .Where(a => a.WebsiteId == websiteId && a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        var crawlResult = audit != null
            ? await _dbContext.CrawlResults.FirstOrDefaultAsync(cr => cr.CrawlJobId == audit.CrawlJobId, cancellationToken)
            : null;

        // Fetch GEO data
        var geoData = await _geoEngineService.AnalyzeBrandGeoVisibilityAsync(websiteId, tenantId, forceRefresh: false, cancellationToken);

        // Fetch Gold Opportunities
        var goldOpportunitiesCount = await _dbContext.Notifications
            .CountAsync(n => n.TenantId == tenantId && n.Type == NotificationTypeEnum.GoldOpportunityAlert, cancellationToken);

        // Fetch Tracked Keywords count
        var keywordCount = await _dbContext.TrackedKeywords
            .CountAsync(k => k.WebsiteId == websiteId && k.TenantId == tenantId && k.IsActive, cancellationToken);

        // Generate HTML Executive Report Document
        var html = BuildExecutiveHtmlReport(website.Name, website.DomainUrl, audit?.OverallScore ?? 100,
            crawlResult?.Title ?? website.Name, crawlResult?.MetaDescription ?? "", crawlResult?.CanonicalUrl ?? "",
            geoData, goldOpportunitiesCount, keywordCount);

        return new ExecutiveReportDto(
            WebsiteId: websiteId,
            WebsiteName: website.Name,
            DomainUrl: website.DomainUrl,
            SeoAuditScore: audit?.OverallScore ?? 100,
            AiShareOfVoiceScore: geoData.ShareOfVoiceScore,
            GoldOpportunitiesCount: goldOpportunitiesCount,
            TrackedKeywordsCount: keywordCount,
            HtmlReportDocument: html,
            GeneratedAt: DateTime.UtcNow
        );
    }

    private static string BuildExecutiveHtmlReport(
        string name, string domain, int score, string title, string metaDesc, string canonical,
        GeoAnalysisResultDto geoData, int goldCount, int keywordCount)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<title>Executive SEO & GEO Audit Report – " + name + "</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("  body { font-family: 'Segoe UI', system-ui, -apple-system, sans-serif; background: #0f172a; color: #f8fafc; margin: 0; padding: 40px; line-height: 1.5; }");
        sb.AppendLine("  .container { max-width: 900px; margin: 0 auto; background: #1e293b; border-radius: 16px; border: 1px solid #334155; padding: 40px; box-shadow: 0 20px 25px -5px rgba(0,0,0,0.5); }");
        sb.AppendLine("  .header { border-bottom: 2px solid #334155; padding-bottom: 20px; margin-bottom: 30px; display: flex; justify-content: space-between; align-items: center; }");
        sb.AppendLine("  .title { font-size: 28px; font-weight: 800; color: #38bdf8; margin: 0; }");
        sb.AppendLine("  .subtitle { font-size: 14px; color: #94a3b8; margin-top: 4px; }");
        sb.AppendLine("  .kpi-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 16px; margin-bottom: 30px; }");
        sb.AppendLine("  .kpi-card { background: #0f172a; border: 1px solid #334155; border-radius: 12px; padding: 20px; text-align: center; }");
        sb.AppendLine("  .kpi-val { font-size: 32px; font-weight: 800; margin: 8px 0; }");
        sb.AppendLine("  .kpi-label { font-size: 11px; font-weight: 700; text-transform: uppercase; color: #94a3b8; }");
        sb.AppendLine("  .section { margin-bottom: 30px; background: #0f172a; border: 1px solid #334155; border-radius: 12px; padding: 24px; }");
        sb.AppendLine("  .section-title { font-size: 18px; font-weight: 700; color: #c084fc; margin-top: 0; margin-bottom: 16px; border-bottom: 1px solid #334155; padding-bottom: 8px; }");
        sb.AppendLine("  .tag-box { background: #1e293b; border: 1px solid #334155; border-radius: 8px; padding: 12px; font-family: monospace; font-size: 13px; color: #34d399; margin-bottom: 12px; }");
        sb.AppendLine("  .citation-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }");
        sb.AppendLine("  .citation-card { background: #1e293b; border: 1px solid #334155; border-radius: 8px; padding: 16px; font-size: 12px; }");
        sb.AppendLine("  .badge { display: inline-block; padding: 2px 8px; border-radius: 9999px; font-size: 10px; font-weight: 700; }");
        sb.AppendLine("  .badge-success { background: rgba(52,211,153,0.1); color: #34d399; border: 1px solid rgba(52,211,153,0.3); }");
        sb.AppendLine("  .badge-warn { background: rgba(251,191,36,0.1); color: #fbbf24; border: 1px solid rgba(251,191,36,0.3); }");
        sb.AppendLine("  .print-btn { background: #0284c7; color: #ffffff; border: none; padding: 10px 20px; border-radius: 8px; font-weight: 700; cursor: pointer; float: right; }");
        sb.AppendLine("  @media print { body { background: #ffffff; color: #000000; padding: 0; } .container { border: none; box-shadow: none; padding: 0; background: #ffffff; color: #000000; } .kpi-card, .section, .citation-card, .tag-box { background: #f8fafc; border-color: #cbd5e1; color: #000000; } .print-btn { display: none; } }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine("<div class=\"container\">");

        // Header
        sb.AppendLine("<div class=\"header\">");
        sb.AppendLine("  <div>");
        sb.AppendLine("    <h1 class=\"title\">" + name + "</h1>");
        sb.AppendLine("    <div class=\"subtitle\">Executive SEO & Generative Engine Optimization (GEO) Summary Report • " + domain + "</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <button class=\"print-btn\" onclick=\"window.print()\">🖨️ Print PDF</button>");
        sb.AppendLine("</div>");

        // KPI Grid
        sb.AppendLine("<div class=\"kpi-grid\">");
        sb.AppendLine("  <div class=\"kpi-card\"><div class=\"kpi-label\">SEO Score</div><div class=\"kpi-val\" style=\"color:#34d399;\">" + score + "/100</div></div>");
        sb.AppendLine("  <div class=\"kpi-card\"><div class=\"kpi-label\">AI Share of Voice</div><div class=\"kpi-val\" style=\"color:#c084fc;\">" + geoData.ShareOfVoiceScore + "%</div></div>");
        sb.AppendLine("  <div class=\"kpi-card\"><div class=\"kpi-label\">Gold Opportunities</div><div class=\"kpi-val\" style=\"color:#fbbf24;\">" + goldCount + "</div></div>");
        sb.AppendLine("  <div class=\"kpi-card\"><div class=\"kpi-label\">Tracked Keywords</div><div class=\"kpi-val\" style=\"color:#60a5fa;\">" + keywordCount + "</div></div>");
        sb.AppendLine("</div>");

        // Section 1: Meta Tags
        sb.AppendLine("<div class=\"section\">");
        sb.AppendLine("  <h2 class=\"section-title\">1. Extracted Technical Meta Tags</h2>");
        sb.AppendLine("  <div style=\"font-size:12px; color:#94a3b8; margin-bottom:6px;\">&lt;title&gt; Tag:</div>");
        sb.AppendLine("  <div class=\"tag-box\">" + title + "</div>");
        sb.AppendLine("  <div style=\"font-size:12px; color:#94a3b8; margin-bottom:6px;\">&lt;meta name=\"description\"&gt;:</div>");
        sb.AppendLine("  <div class=\"tag-box\">" + metaDesc + "</div>");
        if (!string.IsNullOrEmpty(canonical))
        {
            sb.AppendLine("  <div style=\"font-size:12px; color:#94a3b8; margin-bottom:6px;\">Canonical URL:</div>");
            sb.AppendLine("  <div class=\"tag-box\">" + canonical + "</div>");
        }
        sb.AppendLine("</div>");

        // Section 2: AI Citations
        sb.AppendLine("<div class=\"section\">");
        sb.AppendLine("  <h2 class=\"section-title\">2. Generative Engine Optimization (GEO) Citation Matrix</h2>");
        sb.AppendLine("  <div class=\"citation-grid\">");

        foreach (var c in geoData.EngineCitations)
        {
            sb.AppendLine("    <div class=\"citation-card\">");
            sb.AppendLine("      <div style=\"display:flex; justify-content:space-between; align-items:center; margin-bottom:8px;\">");
            sb.AppendLine("        <strong style=\"font-size:14px;\">" + c.EngineName + "</strong>");
            sb.AppendLine("        <span class=\"badge " + (c.IsMentioned ? "badge-success" : "badge-warn") + "\">" + (c.IsMentioned ? "✓ Cited" : "✕ Not Cited") + "</span>");
            sb.AppendLine("      </div>");
            sb.AppendLine("      <p style=\"color:#cbd5e1; font-style:italic;\">\"" + c.SampleAiResponseSnippet + "\"</p>");
            sb.AppendLine("    </div>");
        }

        sb.AppendLine("  </div>");
        sb.AppendLine("</div>");

        // Footer
        sb.AppendLine("<div style=\"text-align:center; font-size:11px; color:#64748b; margin-top:40px;\">");
        sb.AppendLine("  Generated automatically by <strong>Akiron SEO AI Platform</strong> on " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC");
        sb.AppendLine("</div>");

        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }
}
