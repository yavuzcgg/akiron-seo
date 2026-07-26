"use client";

import AeoGeneratorModal from "@/components/AeoGeneratorModal";
import AiBotAuditorCard from "@/components/AiBotAuditorCard";
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
  const { theme, toggleTheme, lang, setLang, t } = useApp();
  const [websites, setWebsites] = useState<Website[]>([]);
  const [loading, setLoading] = useState(false);
  
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
    } catch (err: any) {
      setError(err.message || "Failed to fetch audit report.");
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
    } catch (err: any) {
      setError(err.message || "Failed to add website.");
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
    } catch (err: any) {
      setError(err.message || "Verification check failed.");
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
    } catch (err: any) {
      setError(err.message || "Crawler service connection failed.");
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
    } catch (err: any) {
      setError(err.message || "API key service failed to connect.");
    }
  };

  return (
    <div className="min-h-screen flex flex-col justify-between p-4 sm:p-6 max-w-7xl mx-auto space-y-6">
      {/* Top Navigation */}
      <header className="flex flex-wrap items-center justify-between gap-4 py-3 border-b border-[var(--border-color)]">
        <Link href="/" className="flex items-center space-x-2.5">
          <div className="w-9 h-9 bg-blue-600 rounded-lg flex items-center justify-center font-extrabold text-white text-xl shadow-md">
            A
          </div>
          <span className="font-extrabold text-xl tracking-tight">Akiron SEO Dashboard</span>
        </Link>

        <div className="flex items-center space-x-3 text-sm font-medium">
          <button
            onClick={() => setLang(lang === "en" ? "tr" : "en")}
            className="px-3 py-1.5 rounded-lg border border-[var(--border-color)] bg-[var(--card-bg)] hover:opacity-80 transition"
          >
            🌐 {lang.toUpperCase()}
          </button>
          <button
            onClick={toggleTheme}
            className="px-3 py-1.5 rounded-lg border border-[var(--border-color)] bg-[var(--card-bg)] hover:opacity-80 transition"
          >
            {theme === "dark" ? "☀️ Light" : "🌙 Dark"}
          </button>
          <Link
            href="/admin"
            className="px-3 py-1.5 rounded-lg border border-purple-500/30 text-purple-400 hover:bg-purple-500/10 transition"
          >
            🛡️ Admin
          </Link>
          <Link href="/" className="px-3 py-1.5 rounded-lg border border-[var(--border-color)] text-slate-400 hover:text-white">
            Logout
          </Link>
        </div>
      </header>

      {/* Main Content Grid */}
      <main className="space-y-6">
        {/* Status Messages */}
        {message && (
          <div className="p-4 rounded-xl bg-green-500/10 border border-green-500/20 text-green-400 text-sm font-semibold flex items-center justify-between">
            <span>{message}</span>
            {message.includes("Audit Score") && (
              <span className="text-xs underline font-bold cursor-pointer" onClick={() => activeAuditReport && fetchLatestAudit(activeAuditReport.websiteId)}>
                View Report Modal ↓
              </span>
            )}
          </div>
        )}
        {error && (
          <div className="p-4 rounded-xl bg-red-500/10 border border-red-500/20 text-red-400 text-sm font-semibold">
            {error}
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Column 1: Websites List & Verification */}
          <div className="lg:col-span-2 space-y-6">
            {/* Add Website Form */}
            <div className="p-6 rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] space-y-4">
              <h2 className="text-lg font-bold">🌐 Add New Website</h2>
              <form onSubmit={handleAddWebsite} className="grid grid-cols-1 sm:grid-cols-5 gap-3">
                <input
                  type="text"
                  placeholder="Site Name (e.g. My Shop)"
                  value={newSiteName}
                  onChange={(e) => setNewSiteName(e.target.value)}
                  required
                  className="sm:col-span-2 px-4 py-2 rounded-lg border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm"
                />
                <input
                  type="text"
                  placeholder="Domain (e.g. myshop.com)"
                  value={newDomainUrl}
                  onChange={(e) => setNewDomainUrl(e.target.value)}
                  required
                  className="sm:col-span-2 px-4 py-2 rounded-lg border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm"
                />
                <button
                  type="submit"
                  disabled={loading}
                  className="px-4 py-2 rounded-lg bg-blue-600 text-white font-bold hover:bg-blue-700 transition text-sm disabled:opacity-50"
                >
                  {loading ? "Adding..." : "+ Add Site"}
                </button>
              </form>
            </div>

            {/* Registered Websites Table */}
            <div className="p-6 rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] space-y-4">
              <h2 className="text-lg font-bold">📋 Registered Websites</h2>
              {websites.length === 0 ? (
                <p className="text-sm text-slate-400">No websites added yet. Add a website to start crawling!</p>
              ) : (
                <div className="space-y-4">
                  {websites.map((site) => (
                    <div
                      key={site.id}
                      className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] space-y-4"
                    >
                      <div className="flex flex-wrap items-center justify-between gap-3">
                        <div>
                          <h4 className="font-bold text-base">{site.name}</h4>
                          <p className="text-xs text-slate-400">{site.domainUrl}</p>
                        </div>

                        <div className="flex items-center space-x-2">
                          {site.isVerified ? (
                            <span className="px-2.5 py-1 rounded-full bg-green-500/10 text-green-400 text-xs font-semibold">
                              ✓ Verified
                            </span>
                          ) : (
                            <button
                              onClick={() => handleVerifyWebsite(site.id)}
                              className="px-3 py-1 rounded-lg border border-amber-500/30 text-amber-400 text-xs font-semibold hover:bg-amber-500/10"
                            >
                              Verify Ownership
                            </button>
                          )}

                          <button
                            onClick={() => fetchLatestAudit(site.id)}
                            className="px-3 py-1 rounded-lg border border-blue-500/30 text-blue-400 text-xs font-semibold hover:bg-blue-500/10"
                          >
                            📊 Audit Report
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
                            className="px-3 py-1 rounded-lg border border-emerald-500/30 text-emerald-300 text-xs font-semibold hover:bg-emerald-500/10"
                          >
                            📄 Executive HTML
                          </button>

                          <button
                            onClick={() => setAeoModalSite({ id: site.id, name: site.name })}
                            className="px-3 py-1 rounded-lg border border-purple-500/30 text-purple-400 text-xs font-semibold hover:bg-purple-500/10"
                          >
                            🤖 AEO & Schemas
                          </button>

                          <button
                            onClick={() => setAiWriterSite({ id: site.id, name: site.name })}
                            className="px-3 py-1 rounded-lg border border-emerald-500/30 text-emerald-400 text-xs font-semibold hover:bg-emerald-500/10"
                          >
                            ✍️ AI Writer
                          </button>

                          <button
                            onClick={() => handleRunCrawl(site.id)}
                            className="px-3 py-1 rounded-lg bg-blue-600 text-white text-xs font-bold hover:bg-blue-700 shadow"
                          >
                            ⚡ Run Audit
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
            <div className="p-6 rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] space-y-4">
              <h2 className="text-lg font-bold">🔐 BYOK (Bring Your Own Key)</h2>
              <p className="text-xs text-slate-400 leading-relaxed">
                Save your private LLM API keys encrypted with enterprise <strong>AES-256-GCM</strong>.
              </p>

              <form onSubmit={handleSaveApiKey} className="space-y-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-400 mb-1">Provider</label>
                  <select
                    value={apiKeyProvider}
                    onChange={(e) => setApiKeyProvider(e.target.value)}
                    className="w-full px-3 py-2 rounded-lg border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm"
                  >
                    <option value="3">Google Gemini</option>
                    <option value="1">OpenAI (ChatGPT)</option>
                    <option value="2">Perplexity AI</option>
                  </select>
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-400 mb-1">API Key</label>
                  <input
                    type="password"
                    placeholder="AIzaSy••••••••••••••••"
                    value={apiKeyValue}
                    onChange={(e) => setApiKeyValue(e.target.value)}
                    required
                    className="w-full px-3 py-2 rounded-lg border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm font-mono"
                  />
                </div>

                <button
                  type="submit"
                  className="w-full py-2.5 rounded-lg bg-blue-600 text-white font-bold hover:bg-blue-700 transition text-sm shadow"
                >
                  🔒 Encrypt & Save Key
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
      <footer className="py-4 border-t border-[var(--border-color)] text-center text-xs text-slate-500">
        {t("rightsReserved")}
      </footer>
    </div>
  );
}
