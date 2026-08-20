const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || "http://localhost:5248/api/v1";
export const SESSION_EXPIRED_EVENT = "akiron:session-expired";

export interface SessionDto {
  userId: string;
  userEmail: string;
  tenantId: string;
  role: string;
}

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly correlationId?: string,
    public readonly fieldErrors?: Record<string, string[]>,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

/**
 * Reads the body as JSON when there is one. Endpoints that answer 204 or send a
 * non-JSON error page would otherwise fail here with an opaque SyntaxError.
 */
async function readBody(response: Response): Promise<unknown> {
  if (response.status === 204) return null;

  const text = await response.text();
  if (!text) return null;

  try {
    return JSON.parse(text);
  } catch {
    return { message: text };
  }
}

function createApiError(body: unknown, status: number): ApiError {
  if (body && typeof body === "object") {
    const record = body as Record<string, unknown>;
    const detail = record.detail ?? record.message ?? record.title;
    const correlationId = typeof record.correlationId === "string" ? record.correlationId : undefined;
    const fieldErrors = record.errors && typeof record.errors === "object"
      ? record.errors as Record<string, string[]>
      : undefined;
    if (typeof detail === "string" && detail.length > 0) {
      return new ApiError(detail, status, correlationId, fieldErrors);
    }
  }
  return new ApiError(`API request failed with status ${status}`, status);
}

let refreshPromise: Promise<boolean> | null = null;

async function refreshSession(): Promise<boolean> {
  if (!refreshPromise) {
    refreshPromise = fetch(`${API_BASE_URL}/auth/refresh`, {
      method: "POST",
      credentials: "include",
    })
      .then((response) => response.ok)
      .catch(() => false)
      .finally(() => {
        refreshPromise = null;
      });
  }

  return refreshPromise;
}

function notifySessionExpired(): void {
  if (typeof window !== "undefined") {
    window.dispatchEvent(new Event(SESSION_EXPIRED_EVENT));
  }
}

export async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {},
  allowRefresh = true,
): Promise<T> {
  const url = `${API_BASE_URL}${endpoint}`;
  const headers = new Headers(options.headers);
  if (options.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(url, {
    ...options,
    headers,
    credentials: "include",
  });

  const skipsRefresh = ["/auth/login", "/auth/register", "/auth/refresh", "/auth/logout"].includes(endpoint);
  if (response.status === 401 && allowRefresh && !skipsRefresh) {
    if (await refreshSession()) {
      return apiRequest<T>(endpoint, options, false);
    }
    notifySessionExpired();
  }

  const data = await readBody(response);

  if (!response.ok) {
    throw createApiError(data, response.status);
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

export interface SeoIssue {
  code: string;
  severity: "Critical" | "Warning" | "Info";
  description: string;
  recommendation: string;
}

export interface AuditReportData {
  auditId: string;
  websiteId: string;
  websiteName: string;
  domainUrl: string;
  overallScore: number;
  statusCode: number;
  title: string;
  metaDescription: string;
  canonicalUrl: string;
  h1Tags: string[];
  openGraphTags: Record<string, string>;
  robotsMeta: string;
  issues: SeoIssue[];
  robotsTxtAudit: RobotsTxtAudit | null;
  crawledAt: string;
  /** Computed by the crawler and summing to overallScore. Null for older audits. */
  scoreBreakdown: ScoreComponent[] | null;
}

export interface ScoreComponent {
  label: string;
  maxPoints: number;
  earnedPoints: number;
}

export interface AeoSchemas {
  organizationJsonLd: string;
  webSiteJsonLd: string;
  faqJsonLd: string;
  llmsTxtContent: string;
  llmsFullTxtContent: string;
}

/**
 * Where a reported metric came from. Mirrors the backend DataSources constants.
 * Anything other than "Live" means the value is not a measurement.
 */
export type DataSource = "Live" | "NotConfigured" | "Unavailable" | "Simulated";

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
  rankDataSource?: DataSource;
}

export interface AiEngineCitation {
  engineName: string;
  isMentioned: boolean;
  sentiment: string;
  citationUrl: string;
  sampleAiResponseSnippet: string;
  mentionRatePercentage?: number;
  citationStatus?: string;
  isGoldOpportunity?: boolean;
  dataSource?: DataSource;
}

export interface GeoAnalysisResult {
  websiteId: string;
  domainUrl: string;
  shareOfVoiceScore: number;
  overallMentionRatePercentage?: number;
  engineCitations: AiEngineCitation[];
  optimizationRecommendations: string[];
  analyzedAt: string;
  isCached?: boolean;
  /** How many engines actually answered. Zero means nothing was measured. */
  liveEngineCount?: number;
}

export interface GoldOpportunity {
  notificationId: string;
  websiteId: string;
  websiteName: string;
  domainUrl: string;
  title: string;
  message: string;
  detectedAt: string;
  isRead: boolean;
}

export interface NotificationItem {
  id: string;
  type: number;
  title: string;
  message: string;
  isRead: boolean;
  createdAt: string;
}

export interface AiContentPlan {
  id: string;
  websiteId: string;
  targetKeyword: string;
  missingPath?: string | null;
  generatedMarkdownContent: string;
  status: number;
  tokensSpent: number;
  createdAt: string;
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
  dataSource?: DataSource;
}

export interface TenantQuotaStatus {
  planName: string;
  monthlyTokenLimit: number;
  usedTokens: number;
  remainingTokens: number;
  periodStart: string;
  periodEnd: string;
  enforcementEnabled: boolean;
}

export interface AdminTenantDto {
  tenantId: string;
  tenantName: string;
  slug: string;
  planName: string;
  monthlyLimitTokens: number;
  usedTokens: number;
  registeredWebsitesCount: number;
  isActive: boolean;
  createdAt: string;
}

export interface AdminUsageLogDto {
  logId: string;
  tenantId: string;
  tenantName: string;
  serviceName: string;
  tokensUsed: number;
  estimatedCostUsd: number;
  timestamp: string;
}

export interface GscMetrics {
  websiteId: string;
  domainUrl: string;
  totalClicks: number;
  totalImpressions: number;
  averageCtrPercentage: number;
  averagePosition: number;
  topKeywordsCount: number;
  analyzedAt: string;
  dataSource?: DataSource;
}

export const apiClient = {
  auth: {
    login: (body: { email: string; password: string }) =>
      apiRequest<SessionDto>(
        "/auth/login",
        { method: "POST", body: JSON.stringify(body) }
      ),
    register: (body: { tenantName: string; fullName: string; email: string; password: string }) =>
      apiRequest<SessionDto>(
        "/auth/register",
        { method: "POST", body: JSON.stringify(body) }
      ),
    session: () => apiRequest<SessionDto>("/auth/session"),
    refresh: () => apiRequest<null>("/auth/refresh", { method: "POST" }, false),
    logout: () => apiRequest<null>("/auth/logout", { method: "POST" }, false),
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
      apiRequest<AuditReportData>(
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
    getGoldOpportunities: (websiteId: string) =>
      apiRequest<GoldOpportunity[]>(
        `/websites/${websiteId}/gold-opportunities`
      ),
  },
  keywords: {
    list: (websiteId: string) =>
      apiRequest<TrackedKeyword[]>(
        `/websites/${websiteId}/keywords`
      ),
    add: (body: { websiteId: string; keywordText: string; language?: string; targetCountry?: string; cronExpression?: string }) =>
      apiRequest<{ success: boolean; keywordId: string }>(
        "/keywords",
        {
          method: "POST",
          body: JSON.stringify({
            websiteId: body.websiteId,
            keyword: body.keywordText,
            language: body.language || "en",
            targetCountry: body.targetCountry || "US",
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
    getAnalysis: (websiteId: string, forceRefresh = false) =>
      apiRequest<GeoAnalysisResult>(
        `/websites/${websiteId}/geo-analysis${forceRefresh ? "?forceRefresh=true" : ""}`
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
  notifications: {
    list: () =>
      apiRequest<NotificationItem[]>(
        "/notifications"
      ),
    markRead: (id: string) =>
      apiRequest<{ success: boolean }>(
        `/notifications/${id}/read`,
        { method: "POST" }
      ),
  },
  content: {
    generate: (websiteId: string, body: { targetKeyword: string; missingPath?: string | null }) =>
      apiRequest<AiContentPlan>(
        `/websites/${websiteId}/ai-content/generate`,
        { method: "POST", body: JSON.stringify(body) }
      ),
    list: (websiteId: string) =>
      apiRequest<AiContentPlan[]>(
        `/websites/${websiteId}/ai-content`
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
  admin: {
    getTenants: () =>
      apiRequest<AdminTenantDto[]>(
        "/admin/tenants"
      ),
    updateQuota: (tenantId: string, body: { newMonthlyLimitTokens: number; resetUsedTokens?: boolean }) =>
      apiRequest<{ success: boolean }>(
        `/admin/tenants/${tenantId}/quota`,
        { method: "POST", body: JSON.stringify(body) }
      ),
    toggleStatus: (tenantId: string) =>
      apiRequest<{ success: boolean; isActive: boolean }>(
        `/admin/tenants/${tenantId}/toggle-status`,
        { method: "POST" }
      ),
    getUsageLogs: () =>
      apiRequest<AdminUsageLogDto[]>(
        "/admin/usage-logs"
      ),
    pruneLogs: (olderThanDays = 30) =>
      apiRequest<{ success: boolean; prunedRecordsCount: number }>(
        "/admin/prune-logs",
        { method: "POST", body: JSON.stringify({ olderThanDays }) }
      ),
  },
  reports: {
    getExecutiveReportUrl: (websiteId: string) =>
      `${API_BASE_URL}/websites/${websiteId}/export-report`,
  },
  gsc: {
    getAnalytics: (websiteId: string) =>
      apiRequest<GscMetrics>(
        `/websites/${websiteId}/gsc-analytics`
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
