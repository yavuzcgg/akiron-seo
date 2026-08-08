"use client";

import AeoGeneratorModal from "@/components/AeoGeneratorModal";
import AiBotAuditorCard from "@/components/AiBotAuditorCard";
import AuthGuard from "@/components/AuthGuard";
import Header from "@/components/Header";
import {
  BarChart3,
  CheckCircle2,
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
import { isSuperAdmin, logout } from "@/lib/session";
import Link from "next/link";
import { useEffect, useState } from "react";

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
  const [websites, setWebsites] = useState<Website[]>([]);
  const [loading, setLoading] = useState(false);
  const [showAdminLink, setShowAdminLink] = useState(false);

  // Role is only known in the browser, so this is resolved after mount.
  useEffect(() => {
    setShowAdminLink(isSuperAdmin());
  }, []);
  
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

  const fetchWebsites = async () => {
    try {
      const data = await apiClient.websites.list();
      setWebsites(data);
    } catch {
      // API Offline fallback
    }
  };

  useEffect(() => {
    fetchWebsites();
  }, []);

  const fetchLatestAudit = async (websiteId: string) => {
    try {
      const report = await apiClient.websites.getLatestAudit(websiteId);
      if (report) {
        setActiveAuditReport(report);
      } else {
        setError("No audit report available yet for this website. Click '⚡ Run Audit' to start!");
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Failed to fetch audit report."));
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
        setMessage(`Website '${newSiteName}' added successfully!`);
        setNewSiteName("");
        setNewDomainUrl("");
        fetchWebsites();
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Failed to add website."));
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
        setMessage("Website ownership verified successfully!");
        fetchWebsites();
      } else {
        setError("Verification pending. Please check DNS TXT or Meta tag.");
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Verification check failed."));
    }
  };

  const handleRunCrawl = async (websiteId: string) => {
    setMessage(null);
    setError(null);
    try {
      const data = await apiClient.websites.crawl(websiteId);
      if (data.success) {
        setMessage(`Crawl completed! Audit Score: ${data.score}/100`);
        fetchLatestAudit(websiteId);
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Crawler service connection failed."));
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
        setMessage(data.message || "BYOK API key encrypted with AES-256-GCM and saved!");
        setApiKeyValue("");
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err, "API key service failed to connect."));
    }
  };

  return (
    <div className="mx-auto flex min-h-screen max-w-7xl flex-col justify-between space-y-6 p-4 sm:p-6">
      <Header label="Akiron SEO Dashboard">
        {showAdminLink && (
          <Link
            href="/admin"
            className="flex h-9 items-center gap-1.5 rounded-lg border border-primary/30 px-3 text-xs font-semibold text-primary transition-colors hover:bg-primary/10"
          >
            <Shield size={15} aria-hidden />
            Admin
          </Link>
        )}
        <button
          onClick={logout}
          className="flex h-9 cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3 text-xs font-semibold text-muted transition-colors hover:text-foreground"
        >
          <LogOut size={15} aria-hidden />
          Logout
        </button>
      </Header>

      {/* Main Content Grid */}
      <main className="space-y-6">
        {/* Status Messages */}
        {message && (
          <div className="flex animate-fadeIn items-center justify-between rounded-xl border border-success/20 bg-success/10 p-4 text-sm font-semibold text-success">
            <span>{message}</span>
            {message.includes("Audit Score") && (
              <button className="cursor-pointer text-xs font-bold underline" onClick={() => activeAuditReport && fetchLatestAudit(activeAuditReport.websiteId)}>
                View Report ↓
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
                <Globe size={18} className="text-primary" aria-hidden /> Add New Website
              </h2>
              <form onSubmit={handleAddWebsite} className="grid grid-cols-1 gap-3 sm:grid-cols-5">
                <input
                  type="text"
                  placeholder="Site Name (e.g. My Shop)"
                  value={newSiteName}
                  onChange={(e) => setNewSiteName(e.target.value)}
                  required
                  className="rounded-lg border border-border bg-bg px-4 py-2 text-sm text-foreground placeholder:text-subtle focus:outline-none focus:ring-2 focus:ring-ring sm:col-span-2"
                />
                <input
                  type="text"
                  placeholder="Domain (e.g. myshop.com)"
                  value={newDomainUrl}
                  onChange={(e) => setNewDomainUrl(e.target.value)}
                  required
                  className="rounded-lg border border-border bg-bg px-4 py-2 text-sm text-foreground placeholder:text-subtle focus:outline-none focus:ring-2 focus:ring-ring sm:col-span-2"
                />
                <button
                  type="submit"
                  disabled={loading}
                  className="cursor-pointer rounded-lg bg-primary px-4 py-2 text-sm font-bold text-on-primary transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {loading ? "Adding..." : "Add Site"}
                </button>
              </form>
            </div>

            {/* Registered Websites Table */}
            <div className="space-y-4 rounded-2xl border border-border bg-surface p-6">
              <h2 className="flex items-center gap-2 text-lg font-bold text-foreground">
                <ListChecks size={18} className="text-primary" aria-hidden /> Registered Websites
              </h2>
              {websites.length === 0 ? (
                <p className="text-sm text-muted">No websites added yet. Add a website to start crawling!</p>
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
                              <CheckCircle2 size={13} aria-hidden /> Verified
                            </span>
                          ) : (
                            <button
                              onClick={() => handleVerifyWebsite(site.id)}
                              className="cursor-pointer rounded-lg border border-warning/30 px-3 py-1 text-xs font-semibold text-warning transition-colors hover:bg-warning/10"
                            >
                              Verify Ownership
                            </button>
                          )}

                          <button
                            onClick={() => fetchLatestAudit(site.id)}
                            className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-primary/30 px-3 py-1 text-xs font-semibold text-primary transition-colors hover:bg-primary/10"
                          >
                            <BarChart3 size={13} aria-hidden /> Audit Report
                          </button>

                          <button
                            onClick={() => {
                              const token = localStorage.getItem("akiron_token");
                              const url = apiClient.reports.getExecutiveReportUrl(site.id);
                              const win = window.open(url, "_blank");
                              if (win && token) {
                                fetch(url, { headers: { Authorization: `Bearer ${token}` } })
                                  .then((res) => res.text())
                                  .then((html) => {
                                    win.document.open();
                                    win.document.write(html);
                                    win.document.close();
                                  });
                              }
                            }}
                            className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3 py-1 text-xs font-semibold text-muted transition-colors hover:text-foreground"
                          >
                            <FileText size={13} aria-hidden /> Executive HTML
                          </button>

                          <button
                            onClick={() => setAeoModalSite({ id: site.id, name: site.name })}
                            className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3 py-1 text-xs font-semibold text-muted transition-colors hover:text-foreground"
                          >
                            <FileCode2 size={13} aria-hidden /> AEO & Schemas
                          </button>

                          <button
                            onClick={() => setAiWriterSite({ id: site.id, name: site.name })}
                            className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3 py-1 text-xs font-semibold text-muted transition-colors hover:text-foreground"
                          >
                            <PenLine size={13} aria-hidden /> AI Writer
                          </button>

                          <button
                            onClick={() => handleRunCrawl(site.id)}
                            className="flex cursor-pointer items-center gap-1.5 rounded-lg bg-primary px-3 py-1 text-xs font-bold text-on-primary transition-colors hover:bg-primary-hover"
                          >
                            <Zap size={13} aria-hidden /> Run Audit
                          </button>
                        </div>
                      </div>

                      {/* Gold GEO Opportunity Alerts Panel */}
                      <GoldOpportunityPanel
                        websiteId={site.id}
                        websiteName={site.name}
                        onOpenWriter={(kw, path) => setAiWriterSite({ id: site.id, name: site.name, keyword: kw, path: path })}
                      />

                      {/* Google Search Console Analytics Card */}
                      <GscAnalyticsCard
                        websiteId={site.id}
                        websiteName={site.name}
                      />

                      {/* GEO Intelligence Engine Card */}
                      <GeoIntelligenceCard
                        websiteId={site.id}
                        websiteName={site.name}
                      />

                      {/* Competitor Intelligence & SERP Gap Card */}
                      <CompetitorAnalysisCard
                        websiteId={site.id}
                        websiteName={site.name}
                      />

                      {/* Keyword Rank Tracker Card */}
                      <KeywordTrackerCard
                        websiteId={site.id}
                      />

                      {/* AI Bot Auditor Sub-card */}
                      <AiBotAuditorCard
                        websiteId={site.id}
                        websiteName={site.name}
                      />
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
                <KeyRound size={18} className="text-primary" aria-hidden /> BYOK (Bring Your Own Key)
              </h2>
              <p className="text-xs leading-relaxed text-muted">
                Save your private LLM API keys, encrypted at rest with <strong className="text-foreground">AES-256-GCM</strong>.
              </p>

              <form onSubmit={handleSaveApiKey} className="space-y-3">
                <div>
                  <label className="mb-1 block text-xs font-semibold text-muted">Provider</label>
                  <select
                    value={apiKeyProvider}
                    onChange={(e) => setApiKeyProvider(e.target.value)}
                    className="w-full cursor-pointer rounded-lg border border-border bg-bg px-3 py-2 text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
                  >
                    <option value="3">Google Gemini</option>
                    <option value="2">Perplexity AI</option>
                    <option value="1">OpenAI (not yet used)</option>
                  </select>
                </div>

                <div>
                  <label className="mb-1 block text-xs font-semibold text-muted">API Key</label>
                  <input
                    type="password"
                    placeholder="AIzaSy••••••••••••••••"
                    value={apiKeyValue}
                    onChange={(e) => setApiKeyValue(e.target.value)}
                    required
                    className="w-full rounded-lg border border-border bg-bg px-3 py-2 font-mono text-sm text-foreground placeholder:text-subtle focus:outline-none focus:ring-2 focus:ring-ring"
                  />
                </div>

                <button
                  type="submit"
                  className="flex w-full cursor-pointer items-center justify-center gap-1.5 rounded-lg bg-primary py-2.5 text-sm font-bold text-on-primary transition-colors hover:bg-primary-hover"
                >
                  <Lock size={15} aria-hidden /> Encrypt & Save Key
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
      <AeoGeneratorModal
        websiteId={aeoModalSite?.id || null}
        websiteName={aeoModalSite?.name || ""}
        onClose={() => setAeoModalSite(null)}
      />

      {/* AI Content Writer & Gold Opportunity Fixer Modal */}
      <AiContentWriterModal
        websiteId={aiWriterSite?.id || null}
        websiteName={aiWriterSite?.name || ""}
        initialKeyword={aiWriterSite?.keyword || ""}
        initialPath={aiWriterSite?.path || ""}
        onClose={() => setAiWriterSite(null)}
      />

      {/* Footer */}
      <footer className="border-t border-border py-4 text-center text-xs text-subtle">
        {t("rightsReserved")}
      </footer>
    </div>
  );
}
