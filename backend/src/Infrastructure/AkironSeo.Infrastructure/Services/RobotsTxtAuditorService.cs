using System.Text.RegularExpressions;
using AkironSeo.Application.Common.Interfaces;

namespace AkironSeo.Infrastructure.Services;

public class RobotsTxtAuditorService : IRobotsTxtAuditorService
{
    private readonly HttpClient _httpClient;

    public RobotsTxtAuditorService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RobotsTxtAuditDto> AuditRobotsTxtAsync(string domainUrl, CancellationToken cancellationToken = default)
    {
        var cleanDomain = domainUrl.Trim();
        if (!cleanDomain.StartsWith("http://") && !cleanDomain.StartsWith("https://"))
        {
            cleanDomain = "https://" + cleanDomain;
        }

        var robotsUri = new Uri(new Uri(cleanDomain), "/robots.txt");
        string rawRobotsTxt = "";
        bool hasRobotsTxt = false;

        try
        {
            var response = await _httpClient.GetAsync(robotsUri, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                rawRobotsTxt = await response.Content.ReadAsStringAsync(cancellationToken);
                hasRobotsTxt = true;
            }
        }
        catch
        {
            // robots.txt unreachable
        }

        var botsToCheck = new Dictionary<string, (string Agent, string Description)>
        {
            { "OpenAI GPTBot", ("GPTBot", "Used by OpenAI to crawl web content for ChatGPT AI models.") },
            { "ChatGPT User", ("ChatGPT-User", "Used by ChatGPT when users perform live web browsing.") },
            { "Anthropic Claude", ("ClaudeBot", "Used by Anthropic to train and fetch web content for Claude AI.") },
            { "Perplexity AI", ("PerplexityBot", "Used by Perplexity AI search engine for live answers.") },
            { "Google Extended", ("Google-Extended", "Used by Google to train Gemini AI models.") },
            { "ByteDance Spider", ("Bytespider", "Used by ByteDance / TikTok AI models.") }
        };

        var botStatuses = new List<AiBotStatusDto>();

        foreach (var (botName, (agent, desc)) in botsToCheck)
        {
            var status = CheckBotStatus(rawRobotsTxt, agent);
            botStatuses.Add(new AiBotStatusDto(
                BotName: botName,
                UserAgent: agent,
                Status: status,
                Description: desc
            ));
        }

        return new RobotsTxtAuditDto(
            DomainUrl: cleanDomain,
            HasRobotsTxt: hasRobotsTxt,
            BotStatuses: botStatuses,
            RawRobotsTxt: rawRobotsTxt
        );
    }

    private static string CheckBotStatus(string robotsTxt, string userAgent)
    {
        if (string.IsNullOrWhiteSpace(robotsTxt)) return "NotSpecified";

        var lines = robotsTxt.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        bool currentSectionApplies = false;
        bool wildcardSectionApplies = false;
        bool isDisallowedForBot = false;
        bool isDisallowedForWildcard = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("#")) continue;

            if (trimmed.StartsWith("User-agent:", StringComparison.OrdinalIgnoreCase))
            {
                var agentVal = trimmed["User-agent:".Length..].Trim();
                currentSectionApplies = agentVal.Equals(userAgent, StringComparison.OrdinalIgnoreCase);
                if (agentVal == "*") wildcardSectionApplies = true;
                else if (!currentSectionApplies) wildcardSectionApplies = false;
            }
            else if (trimmed.StartsWith("Disallow:", StringComparison.OrdinalIgnoreCase))
            {
                var path = trimmed["Disallow:".Length..].Trim();
                if (path == "/")
                {
                    if (currentSectionApplies) isDisallowedForBot = true;
                    if (wildcardSectionApplies) isDisallowedForWildcard = true;
                }
            }
        }

        if (isDisallowedForBot) return "Disallowed";
        if (isDisallowedForWildcard && !isDisallowedForBot) return "Disallowed";
        return "Allowed";
    }
}
