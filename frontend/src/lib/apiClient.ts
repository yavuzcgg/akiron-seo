const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5248/api/v1";

export async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const url = `${API_BASE_URL}${endpoint}`;
  
  const token = typeof window !== "undefined" ? localStorage.getItem("akiron_token") : null;

  const headers: Record<string, string> = {
    "Content-Type": "application/json",
    ...(options.headers as Record<string, string>),
  };

  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  const response = await fetch(url, { ...options, headers });
  const data = await response.json();

  if (!response.ok) {
    throw new Error(data?.detail || data?.message || `API request failed with status ${response.status}`);
  }

  return data as T;
}

export interface AiSeoRecommendation {
  optimizedTitle: string;
  optimizedMetaDescription: string;
  targetKeywords: string[];
  actionableTips: string[];
}

export interface AiBotStatus {
  botName: string;
  userAgent: string;
  status: "Allowed" | "Disallowed" | "NotSpecified";
  description: string;
}

export interface RobotsTxtAudit {
  domainUrl: string;
  hasRobotsTxt: boolean;
  botStatuses: AiBotStatus[];
  rawRobotsTxt: string;
}

export interface AeoSchemas {
  organizationJsonLd: string;
  webSiteJsonLd: string;
  llmsTxtContent: string;
}

export interface TrackedKeyword {
  id: string;
  websiteId: string;
  keywordText: string;
  targetCountry: string;
  targetLanguage: string;
  currentPosition: number | null;
  previousPosition: number | null;
  positionChange: number;
  targetUrl: string | null;
  isActive: boolean;
  lastCheckedAt: string | null;
  nextScheduledRun: string | null;
}

export interface AiEngineCitation {
  engineName: string;
  isMentioned: boolean;
  sentiment: string;
  citationUrl: string;
  sampleAiResponseSnippet: string;
}

export interface GeoAnalysisResult {
  websiteId: string;
  domainUrl: string;
  shareOfVoiceScore: number;
  engineCitations: AiEngineCitation[];
  optimizationRecommendations: string[];
  analyzedAt: string;
}

export interface KeywordOpportunity {
  keyword: string;
  competitorRank: number;
  yourRank: number;
  estimatedSearchVolume: number;
  difficulty: string;
}

export interface CompetitorGapResult {
  websiteId: string;
  yourDomain: string;
  competitorDomain: string;
  overlapScore: number;
  missingKeywordOpportunities: KeywordOpportunity[];
  analyzedAt: string;
}

export interface TenantQuotaStatus {
  tenantId: string;
  planName: string;
  crawlQuotaLimit: number;
  crawlQuotaUsed: number;
  aiQuotaLimit: number;
  aiQuotaUsed: number;
  keywordQuotaLimit: number;
  keywordQuotaUsed: number;
  cycleResetsAt: string;
}

export const apiClient = {
  auth: {
    login: (body: { email: string; password: string }) =>
      apiRequest<{ success: boolean; message: string; accessToken?: string; tenantId?: string; userEmail?: string; role?: string }>(
        "/auth/login",
        { method: "POST", body: JSON.stringify(body) }
      ),
    register: (body: { tenantName: string; fullName: string; email: string; password: string }) =>
      apiRequest<{ success: boolean; message: string; accessToken?: string; tenantId?: string; userEmail?: string; role?: string }>(
        "/auth/register",
        { method: "POST", body: JSON.stringify(body) }
      ),
  },
  websites: {
    list: () =>
      apiRequest<Array<{ id: string; name: string; domainUrl: string; isVerified: boolean; verificationToken: string; createdAt: string }>>(
        "/websites"
      ),
    create: (body: { name: string; domainUrl: string }) =>
      apiRequest<{ success: boolean; websiteId: string }>(
        "/websites",
        { method: "POST", body: JSON.stringify(body) }
      ),
    verify: (websiteId: string, method: number = 1) =>
      apiRequest<{ success: boolean; verified: boolean }>(
        `/websites/${websiteId}/verify?method=${method}`,
        { method: "POST" }
      ),
    crawl: (websiteId: string) =>
      apiRequest<{ success: boolean; auditId: string; score: number }>(
        `/websites/${websiteId}/crawl`,
        { method: "POST" }
      ),
    getLatestAudit: (websiteId: string) =>
      apiRequest<any>(
        `/websites/${websiteId}/latest-audit`
      ),
    getAiSuggestions: (websiteId: string) =>
      apiRequest<AiSeoRecommendation>(
        `/websites/${websiteId}/ai-suggestions`,
        { method: "POST" }
      ),
    getRobotsTxtAudit: (websiteId: string) =>
      apiRequest<RobotsTxtAudit>(
        `/websites/${websiteId}/robots-txt-audit`
      ),
    getAeoSchemas: (websiteId: string) =>
      apiRequest<AeoSchemas>(
        `/websites/${websiteId}/aeo-schemas`
      ),
  },
  keywords: {
    list: (websiteId: string) =>
      apiRequest<TrackedKeyword[]>(
        `/websites/${websiteId}/keywords`
      ),
    add: (body: { websiteId: string; keywordText: string; language?: string; cronExpression?: string }) =>
      apiRequest<{ success: boolean; keywordId: string }>(
        "/keywords",
        {
          method: "POST",
          body: JSON.stringify({
            websiteId: body.websiteId,
            keyword: body.keywordText,
            language: body.language || "tr",
            cronExpression: body.cronExpression || "0 0 * * *"
          }),
        }
      ),
    checkRank: (keywordId: string) =>
      apiRequest<TrackedKeyword>(
        `/keywords/${keywordId}/check-rank`,
        { method: "POST" }
      ),
  },
  geo: {
    getAnalysis: (websiteId: string) =>
      apiRequest<GeoAnalysisResult>(
        `/websites/${websiteId}/geo-analysis`
      ),
    analyzePrompt: (websiteId: string, promptText: string) =>
      apiRequest<GeoAnalysisResult>(
        `/websites/${websiteId}/analyze-prompt`,
        {
          method: "POST",
          body: JSON.stringify({ promptText }),
        }
      ),
  },
  competitors: {
    list: (websiteId: string) =>
      apiRequest<CompetitorGapResult[]>(
        `/websites/${websiteId}/competitors`
      ),
    analyze: (websiteId: string, competitorDomain: string) =>
      apiRequest<CompetitorGapResult>(
        `/websites/${websiteId}/analyze-competitor`,
        {
          method: "POST",
          body: JSON.stringify({ competitorDomain }),
        }
      ),
  },
  tenant: {
    getQuota: () =>
      apiRequest<TenantQuotaStatus>(
        "/tenant/quota"
      ),
    saveApiKey: (body: { provider: number; apiKey: string }) =>
      apiRequest<{ success: boolean; message: string }>(
        "/tenant/api-keys",
        { method: "POST", body: JSON.stringify(body) }
      ),
  },
};
