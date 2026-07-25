const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5248/api/v1";

export async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const url = `${API_BASE_URL}${endpoint}`;
  
  const headers = {
    "Content-Type": "application/json",
    ...options.headers,
  };

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
    list: (tenantId: string) =>
      apiRequest<Array<{ id: string; name: string; domainUrl: string; isVerified: boolean; verificationToken: string; createdAt: string }>>(
        `/websites?tenantId=${tenantId}`
      ),
    create: (tenantId: string, body: { name: string; domainUrl: string }) =>
      apiRequest<{ success: boolean; websiteId: string }>(
        `/websites?tenantId=${tenantId}`,
        { method: "POST", body: JSON.stringify(body) }
      ),
    verify: (websiteId: string, tenantId: string, method: number = 1) =>
      apiRequest<{ success: boolean; verified: boolean }>(
        `/websites/${websiteId}/verify?tenantId=${tenantId}&method=${method}`,
        { method: "POST" }
      ),
    crawl: (websiteId: string, tenantId: string) =>
      apiRequest<{ success: boolean; auditId: string; score: number }>(
        `/websites/${websiteId}/crawl?tenantId=${tenantId}`,
        { method: "POST" }
      ),
    getLatestAudit: (websiteId: string, tenantId: string) =>
      apiRequest<any>(
        `/websites/${websiteId}/latest-audit?tenantId=${tenantId}`
      ),
    getAiSuggestions: (websiteId: string, tenantId: string) =>
      apiRequest<AiSeoRecommendation>(
        `/websites/${websiteId}/ai-suggestions?tenantId=${tenantId}`,
        { method: "POST" }
      ),
    getRobotsTxtAudit: (websiteId: string, tenantId: string) =>
      apiRequest<RobotsTxtAudit>(
        `/websites/${websiteId}/robots-txt-audit?tenantId=${tenantId}`
      ),
    getAeoSchemas: (websiteId: string, tenantId: string) =>
      apiRequest<AeoSchemas>(
        `/websites/${websiteId}/aeo-schemas?tenantId=${tenantId}`
      ),
  },
  keywords: {
    list: (websiteId: string, tenantId: string) =>
      apiRequest<TrackedKeyword[]>(
        `/websites/${websiteId}/keywords?tenantId=${tenantId}`
      ),
    add: (tenantId: string, body: { websiteId: string; keywordText: string; language?: string; cronExpression?: string }) =>
      apiRequest<{ success: boolean; keywordId: string }>(
        `/keywords?tenantId=${tenantId}`,
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
    checkRank: (keywordId: string, tenantId: string) =>
      apiRequest<TrackedKeyword>(
        `/keywords/${keywordId}/check-rank?tenantId=${tenantId}`,
        { method: "POST" }
      ),
  },
  geo: {
    getAnalysis: (websiteId: string, tenantId: string) =>
      apiRequest<GeoAnalysisResult>(
        `/websites/${websiteId}/geo-analysis?tenantId=${tenantId}`
      ),
    analyzePrompt: (websiteId: string, tenantId: string, promptText: string) =>
      apiRequest<GeoAnalysisResult>(
        `/websites/${websiteId}/analyze-prompt?tenantId=${tenantId}`,
        {
          method: "POST",
          body: JSON.stringify({ promptText }),
        }
      ),
  },
  tenant: {
    saveApiKey: (body: { tenantId: string; provider: number; apiKey: string }) =>
      apiRequest<{ success: boolean; message: string }>(
        "/tenant/api-keys",
        { method: "POST", body: JSON.stringify(body) }
      ),
  },
};
