"use client";

import AeoGeneratorModal from "@/components/AeoGeneratorModal";
import AiBotAuditorCard from "@/components/AiBotAuditorCard";
import AuthGuard from "@/components/AuthGuard";
import Header from "@/components/Header";
import {
  BarChart3,
  CheckCircle2,
  ChevronDown,
  ChevronUp,
  FileCode2,
  FileText,
  Globe,
  KeyRound,
  ListChecks,
  Lock,
  LogOut,
  PenLine,
  Shield,
  Zap,
} from "lucide-react";
import AiContentWriterModal from "@/components/AiContentWriterModal";
import AuditDetailsModal from "@/components/AuditDetailsModal";
import { AuditReportData } from "@/lib/apiClient";
import CompetitorAnalysisCard from "@/components/CompetitorAnalysisCard";
import GeoIntelligenceCard from "@/components/GeoIntelligenceCard";
import GoldOpportunityPanel from "@/components/GoldOpportunityPanel";
import GscAnalyticsCard from "@/components/GscAnalyticsCard";
import KeywordTrackerCard from "@/components/KeywordTrackerCard";
import TenantQuotaCard from "@/components/TenantQuotaCard";
import { useApp } from "@/components/providers";
import { apiClient } from "@/lib/apiClient";
import { getErrorMessage } from "@/lib/errors";
import { SUPER_ADMIN_ROLE, useLogout, useSession } from "@/hooks/useSession";
import { queryKeys } from "@/lib/queryKeys";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useState } from "react";

interface Website {
  id: string;
  name: string;
  domainUrl: string;
  isVerified: boolean;
  verificationToken: string;
  createdAt: string;
}

export default function DashboardPage() {
  return (
    <AuthGuard>
      <DashboardContent />
    </AuthGuard>
  );
}

function DashboardContent() {
  const { t } = useApp();
  const queryClient = useQueryClient();
  const session = useSession();
  const logout = useLogout();
  const websitesQuery = useQuery<Website[]>({
    queryKey: queryKeys.websites,
    queryFn: apiClient.websites.list,
  });
  const websites = websitesQuery.data ?? [];
  const [loading, setLoading] = useState(false);
  const [expandedSiteIds, setExpandedSiteIds] = useState<Set<string>>(() => new Set());
  
  // Modals state
  const [activeAuditReport, setActiveAuditReport] = useState<AuditReportData | null>(null);
  const [aeoModalSite, setAeoModalSite] = useState<{ id: string; name: string } | null>(null);
  const [aiWriterSite, setAiWriterSite] = useState<{ id: string; name: string; keyword?: string; path?: string } | null>(null);

  // Form states
  const [newSiteName, setNewSiteName] = useState("");
  const [newDomainUrl, setNewDomainUrl] = useState("");
  const [apiKeyProvider, setApiKeyProvider] = useState("3"); // Gemini = 3
  const [apiKeyValue, setApiKeyValue] = useState("");

  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const fetchLatestAudit = async (websiteId: string) => {
    try {
      const report = await apiClient.websites.getLatestAudit(websiteId);
      if (report) {
        setActiveAuditReport(report);
      } else {
        setError(t("noAuditReport"));
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err, t("auditLoadFailed")));
    }
  };

  const handleAddWebsite = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setMessage(null);
    setError(null);

    try {
      const data = await apiClient.websites.create({ name: newSiteName, domainUrl: newDomainUrl });
      if (data.success) {
        setMessage(`${t("websiteAdded")} ${newSiteName}`);
        setNewSiteName("");
        setNewDomainUrl("");
        await queryClient.invalidateQueries({ queryKey: queryKeys.websites });
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err, t("websiteAddFailed")));
    } finally {
      setLoading(false);
    }
  };

  const handleVerifyWebsite = async (websiteId: string) => {
    setMessage(null);
    setError(null);
    try {
      const data = await apiClient.websites.verify(websiteId, 1);
      if (data.verified) {
        setMessage(t("ownershipVerified"));
        await queryClient.invalidateQueries({ queryKey: queryKeys.websites });
      } else {
        setError(t("verificationPending"));
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err, t("verificationFailed")));
    }
  };

  const handleRunCrawl = async (websiteId: string) => {
    setMessage(null);
    setError(null);
    try {
      const data = await apiClient.websites.crawl(websiteId);
      if (data.success) {
        setMessage(`${t("crawlCompleted")} ${data.score}/100`);
        fetchLatestAudit(websiteId);
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err, t("crawlFailed")));
    }
  };

  const handleSaveApiKey = async (e: React.FormEvent) => {
    e.preventDefault();
    setMessage(null);
    setError(null);
    try {
      const data = await apiClient.tenant.saveApiKey({
        provider: parseInt(apiKeyProvider),
        apiKey: apiKeyValue,
      });

      if (data.success) {
        setMessage(t("apiKeySaved"));
        setApiKeyValue("");
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err, t("apiKeyFailed")));
    }
  };

  return (
    <div className="mx-auto flex min-h-dvh max-w-7xl flex-col justify-between space-y-6 p-4 sm:p-6">
      <Header label={`Akiron SEO ${t("dashboard")}`}>
        {session.data?.role === SUPER_ADMIN_ROLE && (
          <Link
            href="/admin"
            className="flex h-9 items-center gap-1.5 rounded-lg border border-primary/30 px-3 text-xs font-semibold text-primary transition-colors hover:bg-primary/10"
          >
            <Shield size={15} aria-hidden />
            {t("admin")}
          </Link>
        )}
        <button
          onClick={() => logout.mutate()}
          disabled={logout.isPending}
          className="flex h-9 cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3 text-xs font-semibold text-muted transition-colors hover:text-foreground"
        >
          <LogOut size={15} aria-hidden />
          {t("logout")}
        </button>
      </Header>

      {/* Main Content Grid */}
      <main id="main-content" tabIndex={-1} className="space-y-6">
        {/* Status Messages */}
        {message && (
          <div className="flex animate-fadeIn items-center justify-between rounded-xl border border-success/20 bg-success/10 p-4 text-sm font-semibold text-success">
            <span>{message}</span>
            {message.startsWith(t("crawlCompleted")) && (
              <button className="cursor-pointer text-xs font-bold underline" onClick={() => activeAuditReport && fetchLatestAudit(activeAuditReport.websiteId)}>
                {t("viewReport")} ↓
              </button>
            )}
          </div>
        )}
        {error && (
          <div className="animate-fadeIn rounded-xl border border-danger/20 bg-danger/10 p-4 text-sm font-semibold text-danger">
            {error}
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Column 1: Websites List & Verification */}
          <div className="lg:col-span-2 space-y-6">
            {/* Add Website Form */}
            <div className="space-y-4 rounded-2xl border border-border bg-surface p-6">
              <h2 className="flex items-center gap-2 text-lg font-bold text-foreground">
                <Globe size={18} className="text-primary" aria-hidden /> {t("addWebsite")}
              </h2>
              <form onSubmit={handleAddWebsite} className="grid grid-cols-1 gap-3 sm:grid-cols-5">
                <label htmlFor="site-name" className="sr-only">{t("siteName")}</label>
                <input
                  id="site-name"
                  name="siteName"
                  autoComplete="organization"
                  type="text"
                  placeholder={`${t("siteName")} (e.g. My Shop)`}
                  value={newSiteName}
                  onChange={(e) => setNewSiteName(e.target.value)}
                  required
                  minLength={2}
                  maxLength={100}
                  className="rounded-lg border border-border bg-bg px-4 py-2 text-sm text-foreground placeholder:text-subtle focus:outline-none focus:ring-2 focus:ring-ring sm:col-span-2"
                />
                <label htmlFor="site-domain" className="sr-only">{t("domain")}</label>
                <input
                  id="site-domain"
                  name="domain"
                  autoComplete="url"
                  type="text"
                  placeholder={`${t("domain")} (e.g. myshop.com)`}
                  value={newDomainUrl}
                  onChange={(e) => setNewDomainUrl(e.target.value)}
                  required
                  maxLength={2048}
                  className="rounded-lg border border-border bg-bg px-4 py-2 text-sm text-foreground placeholder:text-subtle focus:outline-none focus:ring-2 focus:ring-ring sm:col-span-2"
                />
                <button
                  type="submit"
                  disabled={loading}
                  className="cursor-pointer rounded-lg bg-primary px-4 py-2 text-sm font-bold text-on-primary transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {loading ? t("adding") : t("addSite")}
                </button>
              </form>
            </div>

            {/* Registered Websites Table */}
            <div className="space-y-4 rounded-2xl border border-border bg-surface p-6">
              <h2 className="flex items-center gap-2 text-lg font-bold text-foreground">
                <ListChecks size={18} className="text-primary" aria-hidden /> {t("registeredWebsites")}
              </h2>
              {websitesQuery.isPending ? (
                <p className="text-sm text-muted" role="status">{t("loadingWebsites")}</p>
              ) : websitesQuery.isError ? (
                <div className="rounded-lg border border-danger/20 bg-danger/10 p-3 text-sm text-danger" role="alert">
                  {t("websiteLoadFailed")}
                </div>
              ) : websites.length === 0 ? (
                <p className="text-sm text-muted">{t("noWebsites")}</p>
              ) : (
                <div className="space-y-4">
                  {websites.map((site) => (
                    <div
                      key={site.id}
                      className="space-y-4 rounded-xl border border-border bg-bg p-4"
                    >
                      <div className="flex flex-wrap items-center justify-between gap-3">
                        <div>
                          <h4 className="text-base font-bold text-foreground">{site.name}</h4>
                          <p className="text-xs text-muted">{site.domainUrl}</p>
                        </div>

                        <div className="flex flex-wrap items-center gap-2">
                          {site.isVerified ? (
                            <span className="flex items-center gap-1 rounded-full bg-success/10 px-2.5 py-1 text-xs font-semibold text-success">
                              <CheckCircle2 size={13} aria-hidden /> {t("verified")}
                            </span>
                          ) : (
                            <button
                              onClick={() => handleVerifyWebsite(site.id)}
                              className="cursor-pointer rounded-lg border border-warning/30 px-3 py-1 text-xs font-semibold text-warning transition-colors hover:bg-warning/10"
                            >
                              {t("verifyOwnership")}
                            </button>
                          )}

                          <button
                            onClick={() => fetchLatestAudit(site.id)}
                            className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-primary/30 px-3 py-1 text-xs font-semibold text-primary transition-colors hover:bg-primary/10"
                          >
                            <BarChart3 size={13} aria-hidden /> {t("auditReport")}
                          </button>

                          <button
                            onClick={() => {
                              const url = apiClient.reports.getExecutiveReportUrl(site.id);
                              window.open(url, "_blank", "noopener,noreferrer");
                            }}
                            className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3 py-1 text-xs font-semibold text-muted transition-colors hover:text-foreground"
                          >
                            <FileText size={13} aria-hidden /> {t("executiveReport")}
                          </button>

                          <button
                            onClick={() => setAeoModalSite({ id: site.id, name: site.name })}
                            className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3 py-1 text-xs font-semibold text-muted transition-colors hover:text-foreground"
                          >
                            <FileCode2 size={13} aria-hidden /> {t("aeoSchemas")}
                          </button>

                          <button
                            onClick={() => setAiWriterSite({ id: site.id, name: site.name })}
                            className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3 py-1 text-xs font-semibold text-muted transition-colors hover:text-foreground"
                          >
                            <PenLine size={13} aria-hidden /> {t("aiWriter")}
                          </button>

                          <button
                            onClick={() => handleRunCrawl(site.id)}
                            className="flex cursor-pointer items-center gap-1.5 rounded-lg bg-primary px-3 py-1 text-xs font-bold text-on-primary transition-colors hover:bg-primary-hover"
                          >
                            <Zap size={13} aria-hidden /> {t("runAudit")}
                          </button>
                        </div>
                      </div>

                      <button
                        type="button"
                        onClick={() => setExpandedSiteIds((current) => {
                          const next = new Set(current);
                          if (next.has(site.id)) next.delete(site.id);
                          else next.add(site.id);
                          return next;
                        })}
                        aria-expanded={expandedSiteIds.has(site.id)}
                        className="flex min-h-11 w-full cursor-pointer items-center justify-center gap-2 rounded-lg border border-border bg-surface px-4 py-2 text-sm font-semibold text-foreground transition-colors hover:bg-elevated"
                      >
                        {expandedSiteIds.has(site.id) ? <ChevronUp size={16} aria-hidden /> : <ChevronDown size={16} aria-hidden />}
                        {expandedSiteIds.has(site.id) ? t("hideInsights") : t("showInsights")}
                      </button>

                      {expandedSiteIds.has(site.id) && (
                        <div className="space-y-4" data-testid={`site-insights-${site.id}`}>
                          <GoldOpportunityPanel
                            websiteId={site.id}
                            websiteName={site.name}
                            onOpenWriter={(kw, path) => setAiWriterSite({ id: site.id, name: site.name, keyword: kw, path })}
                          />
                          <GscAnalyticsCard websiteId={site.id} websiteName={site.name} />
                          <GeoIntelligenceCard websiteId={site.id} websiteName={site.name} />
                          <CompetitorAnalysisCard websiteId={site.id} websiteName={site.name} />
                          <KeywordTrackerCard websiteId={site.id} />
                          <AiBotAuditorCard websiteId={site.id} websiteName={site.name} />
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* Column 2: BYOK Key Settings & Quota Ledger */}
          <div className="space-y-6">
            {/* Tenant Quota Meter Card */}
            <TenantQuotaCard />

            {/* BYOK API Key Settings */}
            <div className="space-y-4 rounded-2xl border border-border bg-surface p-6">
              <h2 className="flex items-center gap-2 text-lg font-bold text-foreground">
                <KeyRound size={18} className="text-primary" aria-hidden /> {t("byokTitle")}
              </h2>
              <p className="text-xs leading-relaxed text-muted">
                {t("byokDescription")}
              </p>

              <form onSubmit={handleSaveApiKey} className="space-y-3">
                <div>
                  <label htmlFor="api-provider" className="mb-1 block text-xs font-semibold text-muted">{t("provider")}</label>
                  <select
                    id="api-provider"
                    name="provider"
                    value={apiKeyProvider}
                    onChange={(e) => setApiKeyProvider(e.target.value)}
                    className="w-full cursor-pointer rounded-lg border border-border bg-bg px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                  >
                    <option value="3">Google Gemini</option>
                    <option value="2">Perplexity AI</option>
                    <option value="1">OpenAI ({t("notYetUsed")})</option>
                  </select>
                </div>

                <div>
                  <label htmlFor="api-key" className="mb-1 block text-xs font-semibold text-muted">{t("apiKey")}</label>
                  <input
                    id="api-key"
                    name="apiKey"
                    autoComplete="off"
                    type="password"
                    placeholder="AIzaSy••••••••••••••••"
                    value={apiKeyValue}
                    onChange={(e) => setApiKeyValue(e.target.value)}
                    required
                    minLength={16}
                    maxLength={4096}
                    className="w-full rounded-lg border border-border bg-bg px-3 py-2 font-mono text-sm text-foreground placeholder:text-subtle focus:outline-none focus:ring-2 focus:ring-ring"
                  />
                </div>

                <button
                  type="submit"
                  className="flex w-full cursor-pointer items-center justify-center gap-1.5 rounded-lg bg-primary py-2.5 text-sm font-bold text-on-primary transition-colors hover:bg-primary-hover"
                >
                  <Lock size={15} aria-hidden /> {t("saveKey")}
                </button>
              </form>
            </div>
          </div>
        </div>
      </main>

      {/* Detailed SEO Audit Report Modal */}
      <AuditDetailsModal
        report={activeAuditReport}
        onClose={() => setActiveAuditReport(null)}
      />

      {/* AEO & Schemas Modal */}
      {aeoModalSite && (
        <AeoGeneratorModal
          key={aeoModalSite.id}
          websiteId={aeoModalSite.id}
          websiteName={aeoModalSite.name}
          onClose={() => setAeoModalSite(null)}
        />
      )}

      {/* AI Content Writer & Gold Opportunity Fixer Modal */}
      {aiWriterSite && (
        <AiContentWriterModal
          key={`${aiWriterSite.id}:${aiWriterSite.keyword ?? ""}:${aiWriterSite.path ?? ""}`}
          websiteId={aiWriterSite.id}
          websiteName={aiWriterSite.name}
          initialKeyword={aiWriterSite.keyword || ""}
          initialPath={aiWriterSite.path || ""}
          onClose={() => setAiWriterSite(null)}
        />
      )}

      {/* Footer */}
      <footer className="border-t border-border py-4 text-center text-xs text-subtle">
        {t("rightsReserved")}
      </footer>
    </div>
  );
}
