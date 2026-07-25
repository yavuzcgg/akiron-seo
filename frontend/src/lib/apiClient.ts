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
  tenant: {
    saveApiKey: (body: { tenantId: string; provider: number; apiKey: string }) =>
      apiRequest<{ success: boolean; message: string }>(
        "/tenant/api-keys",
        { method: "POST", body: JSON.stringify(body) }
      ),
  },
};
