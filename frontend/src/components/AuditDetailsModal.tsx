"use client";

import { apiClient, AiSeoRecommendation, AuditReportData } from "@/lib/apiClient";
import { useState } from "react";

interface ModalProps {
  report: AuditReportData | null;
  tenantId?: string;
  onClose: () => void;
}

export default function AuditDetailsModal({ report, tenantId, onClose }: ModalProps) {
  const [aiAnalysis, setAiAnalysis] = useState<AiSeoRecommendation | null>(null);
  const [loadingAi, setLoadingAi] = useState(false);
  const [errorAi, setErrorAi] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<"overview" | "issues" | "robots" | "ai">("overview");

  if (!report) return null;

  const getScoreColor = (score: number) => {
    if (score >= 90) return "text-emerald-400 border-emerald-500/30 bg-emerald-500/10";
    if (score >= 60) return "text-amber-400 border-amber-500/30 bg-amber-500/10";
    return "text-rose-400 border-rose-500/30 bg-rose-500/10";
  };

  const getScoreLabel = (score: number) => {
    if (score >= 90) return "Excellent";
    if (score >= 70) return "Good";
    if (score >= 50) return "Needs Work";
    return "Critical";
  };

  const getSeverityStyle = (severity: string) => {
    switch (severity) {
      case "Critical": return "bg-rose-500/20 text-rose-300 border-rose-500/30";
      case "Warning": return "bg-amber-500/20 text-amber-300 border-amber-500/30";
      default: return "bg-blue-500/20 text-blue-300 border-blue-500/30";
    }
  };

  const criticalCount = report.issues.filter(i => i.severity === "Critical").length;
  const warningCount = report.issues.filter(i => i.severity === "Warning").length;
  const infoCount = report.issues.filter(i => i.severity === "Info").length;

  const handleGenerateAiSuggestions = async () => {
    setLoadingAi(true);
    setErrorAi(null);
    try {
      const data = await apiClient.websites.getAiSuggestions(report.websiteId);
      setAiAnalysis(data);
    } catch (err: any) {
      setErrorAi(err.message || "Failed to generate AI recommendations.");
    } finally {
      setLoadingAi(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-fadeIn">
      <div className="relative w-full max-w-4xl max-h-[90vh] overflow-y-auto rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] shadow-2xl p-6 sm:p-8 space-y-5">
        
        {/* Header */}
        <div className="flex items-center justify-between border-b border-[var(--border-color)] pb-4">
          <div>
            <span className="text-xs uppercase tracking-wider text-blue-400 font-bold">SEO Audit Report</span>
            <h2 className="text-2xl font-extrabold text-white mt-1">{report.websiteName}</h2>
            <p className="text-xs text-slate-400">{report.domainUrl} • Crawled {new Date(report.crawledAt).toLocaleString()}</p>
          </div>
          
          <div className="flex items-center gap-2">
            <button
              onClick={() => {
                const token = localStorage.getItem("akiron_token");
                const url = apiClient.reports.getExecutiveReportUrl(report.websiteId);
                const win = window.open(url, "_blank");
                if (win && token) {
                  // Fetch and render HTML with auth header
                  fetch(url, { headers: { Authorization: `Bearer ${token}` } })
                    .then((res) => res.text())
                    .then((html) => {
                      win.document.open();
                      win.document.write(html);
                      win.document.close();
                    });
                }
              }}
              className="px-3.5 py-1.5 rounded-lg bg-blue-600/20 text-blue-300 hover:bg-blue-600/30 font-bold text-xs transition border border-blue-500/30 flex items-center gap-1.5"
            >
              📄 Executive Report
            </button>

            <button
              onClick={onClose}
              className="p-2 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 transition"
            >
              ✕
            </button>
          </div>
        </div>

        {/* Score Overview Cards */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <div className={`p-4 rounded-xl border flex flex-col items-center justify-center text-center ${getScoreColor(report.overallScore)}`}>
            <span className="text-[10px] font-semibold uppercase tracking-wider opacity-80">Score</span>
            <span className="text-3xl font-extrabold my-0.5">{report.overallScore}<span className="text-sm font-normal">/100</span></span>
            <span className="text-[10px] font-bold">{getScoreLabel(report.overallScore)}</span>
          </div>

          <div className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] flex flex-col items-center justify-center text-center">
            <span className="text-[10px] font-semibold text-slate-400 uppercase">HTTP</span>
            <div className="flex items-center gap-1.5 my-0.5">
              <span className={`w-2 h-2 rounded-full ${report.statusCode === 200 ? "bg-emerald-500" : "bg-rose-500"}`}></span>
              <span className="text-lg font-bold text-white">{report.statusCode}</span>
            </div>
            <span className="text-[10px] text-slate-400">{report.statusCode === 200 ? "OK" : "Error"}</span>
          </div>

          <div className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] flex flex-col items-center justify-center text-center">
            <span className="text-[10px] font-semibold text-slate-400 uppercase">Issues</span>
            <span className="text-lg font-bold text-amber-400 my-0.5">{report.issues.length}</span>
            <span className="text-[10px] text-slate-400">
              {criticalCount > 0 && <span className="text-rose-400">{criticalCount} critical</span>}
              {criticalCount > 0 && warningCount > 0 && " · "}
              {warningCount > 0 && <span className="text-amber-400">{warningCount} warn</span>}
            </span>
          </div>

          <div className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] flex flex-col items-center justify-center text-center">
            <span className="text-[10px] font-semibold text-slate-400 uppercase">AI Bots</span>
            <span className="text-lg font-bold text-purple-400 my-0.5">
              {report.robotsTxtAudit ? report.robotsTxtAudit.botStatuses.filter(b => b.status === "Allowed").length : "—"}
            </span>
            <span className="text-[10px] text-slate-400">Allowed</span>
          </div>
        </div>

        {/* Tabs */}
        <div className="flex border-b border-[var(--border-color)] gap-1 text-xs font-bold">
          {(["overview", "issues", "robots", "ai"] as const).map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`px-4 py-2.5 transition border-b-2 rounded-t-lg ${
                activeTab === tab
                  ? "border-blue-500 text-blue-400 bg-blue-500/5"
                  : "border-transparent text-slate-400 hover:text-slate-200 hover:bg-slate-800/50"
              }`}
            >
              {tab === "overview" && "📊 Overview"}
              {tab === "issues" && `⚠️ Issues (${report.issues.length})`}
              {tab === "robots" && "🤖 AI Bots"}
              {tab === "ai" && "✨ AI Engine"}
            </button>
          ))}
        </div>

        {/* Tab Content */}
        <div className="space-y-4">

          {/* OVERVIEW TAB */}
          {activeTab === "overview" && (
            <div className="space-y-4">
              {/* Extracted Meta Tags */}
              <div className="space-y-3">
                <h3 className="text-sm font-bold uppercase text-slate-300 tracking-wider">Extracted Meta Tags</h3>
                
                <div className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] space-y-3">
                  <div>
                    <div className="flex items-center justify-between text-xs mb-1">
                      <span className="font-semibold text-blue-400">&lt;title&gt; Tag</span>
                      <span className={`font-mono ${report.title.length >= 30 && report.title.length <= 60 ? "text-emerald-400" : "text-amber-400"}`}>
                        {report.title.length} chars
                      </span>
                    </div>
                    <div className="p-2.5 rounded-lg bg-black/40 text-sm font-mono text-slate-200 border border-slate-800">
                      {report.title}
                    </div>
                  </div>

                  <div>
                    <div className="flex items-center justify-between text-xs mb-1">
                      <span className="font-semibold text-blue-400">&lt;meta name=&quot;description&quot;&gt;</span>
                      <span className={`font-mono ${report.metaDescription.length >= 120 && report.metaDescription.length <= 160 ? "text-emerald-400" : "text-amber-400"}`}>
                        {report.metaDescription.length} chars
                      </span>
                    </div>
                    <div className="p-2.5 rounded-lg bg-black/40 text-sm font-mono text-slate-200 border border-slate-800">
                      {report.metaDescription}
                    </div>
                  </div>
                </div>
              </div>

              {/* H1 Tags */}
              <div className="space-y-2">
                <h3 className="text-sm font-bold uppercase text-slate-300 tracking-wider">H1 Headings</h3>
                <div className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)]">
                  {report.h1Tags && report.h1Tags.length > 0 ? (
                    <div className="space-y-2">
                      {report.h1Tags.map((h1, idx) => (
                        <div key={idx} className="flex items-center gap-2">
                          <span className={`px-1.5 py-0.5 rounded text-[10px] font-bold ${report.h1Tags.length === 1 ? "bg-emerald-500/20 text-emerald-400" : "bg-amber-500/20 text-amber-400"}`}>
                            H1#{idx + 1}
                          </span>
                          <span className="text-sm text-slate-200 font-mono">{h1}</span>
                        </div>
                      ))}
                      {report.h1Tags.length > 1 && (
                        <p className="text-[11px] text-amber-400 mt-1">⚠ Multiple H1 tags detected. Best practice is exactly one H1 per page.</p>
                      )}
                    </div>
                  ) : (
                    <p className="text-xs text-rose-400">✕ No H1 heading found on this page.</p>
                  )}
                </div>
              </div>

              {/* Canonical URL */}
              <div className="space-y-2">
                <h3 className="text-sm font-bold uppercase text-slate-300 tracking-wider">Canonical URL</h3>
                <div className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)]">
                  {report.canonicalUrl ? (
                    <div className="flex items-center gap-2">
                      <span className="px-1.5 py-0.5 rounded text-[10px] font-bold bg-emerald-500/20 text-emerald-400">✓</span>
                      <code className="text-sm text-slate-200 font-mono">{report.canonicalUrl}</code>
                    </div>
                  ) : (
                    <p className="text-xs text-amber-400">⚠ No canonical URL specified. Consider adding &lt;link rel=&quot;canonical&quot;&gt; to prevent duplicate content.</p>
                  )}
                </div>
              </div>

              {/* Score Breakdown */}
              <div className="space-y-2">
                <h3 className="text-sm font-bold uppercase text-slate-300 tracking-wider">Score Breakdown</h3>
                <div className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] space-y-2">
                  {[
                    { label: "HTTP Status", max: 15, earned: report.statusCode >= 200 && report.statusCode < 300 ? 15 : 0 },
                    { label: "Title Tag", max: 15, earned: report.title.length >= 30 && report.title.length <= 60 ? 15 : report.title.length >= 20 ? 10 : report.title.length > 0 ? 5 : 0 },
                    { label: "Meta Description", max: 15, earned: report.metaDescription.length >= 120 && report.metaDescription.length <= 160 ? 15 : report.metaDescription.length >= 50 ? 10 : 5 },
                    { label: "H1 Heading", max: 10, earned: (report.h1Tags?.length === 1) ? 10 : (report.h1Tags?.length ?? 0) > 1 ? 5 : 0 },
                    { label: "Canonical URL", max: 10, earned: report.canonicalUrl ? 10 : 0 },
                    { label: "OpenGraph Tags", max: 10, earned: 5 },
                    { label: "Robots Meta", max: 10, earned: 10 },
                    { label: "Title Length", max: 5, earned: report.title.length <= 65 ? 5 : 0 },
                    { label: "Meta Length", max: 5, earned: report.metaDescription.length <= 165 ? 5 : 0 },
                    { label: "Heading Hierarchy", max: 5, earned: (report.h1Tags?.length ?? 0) <= 1 ? 5 : 0 },
                  ].map((item, idx) => (
                    <div key={idx} className="flex items-center gap-3">
                      <span className="text-xs text-slate-400 w-36 shrink-0">{item.label}</span>
                      <div className="flex-1 h-2 rounded-full bg-slate-800 overflow-hidden">
                        <div
                          className={`h-full rounded-full transition-all ${item.earned >= item.max ? "bg-emerald-500" : item.earned > 0 ? "bg-amber-500" : "bg-rose-500"}`}
                          style={{ width: `${(item.earned / item.max) * 100}%` }}
                        />
                      </div>
                      <span className={`text-xs font-mono w-12 text-right ${item.earned >= item.max ? "text-emerald-400" : item.earned > 0 ? "text-amber-400" : "text-rose-400"}`}>
                        {item.earned}/{item.max}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* ISSUES TAB */}
          {activeTab === "issues" && (
            <div className="space-y-3">
              {report.issues.length === 0 ? (
                <div className="p-6 text-center text-emerald-400 text-sm font-semibold">
                  ✓ No SEO issues detected. Great job!
                </div>
              ) : (
                report.issues.map((issue, idx) => (
                  <div key={idx} className={`p-4 rounded-xl border space-y-1.5 ${
                    issue.severity === "Critical" ? "border-rose-500/20 bg-rose-500/5" :
                    issue.severity === "Warning" ? "border-amber-500/20 bg-amber-500/5" :
                    "border-blue-500/20 bg-blue-500/5"
                  }`}>
                    <div className="flex items-center gap-2">
                      <span className={`px-2 py-0.5 rounded text-[10px] font-extrabold uppercase tracking-wide border ${getSeverityStyle(issue.severity)}`}>
                        {issue.severity}
                      </span>
                      <code className="text-[10px] text-slate-500 font-mono">{issue.code}</code>
                    </div>
                    <p className="text-sm font-semibold text-slate-200">{issue.description}</p>
                    <p className="text-xs text-slate-300 leading-relaxed">
                      💡 <strong className="text-amber-300">Fix:</strong> {issue.recommendation}
                    </p>
                  </div>
                ))
              )}
            </div>
          )}

          {/* ROBOTS.TXT AI BOT STATUS TAB */}
          {activeTab === "robots" && (
            <div className="space-y-4">
              {report.robotsTxtAudit ? (
                <>
                  <div className="flex items-center gap-2 text-xs">
                    <span className={`px-2 py-1 rounded-full font-bold ${report.robotsTxtAudit.hasRobotsTxt ? "bg-emerald-500/10 text-emerald-400 border border-emerald-500/20" : "bg-rose-500/10 text-rose-400 border border-rose-500/20"}`}>
                      {report.robotsTxtAudit.hasRobotsTxt ? "✓ robots.txt Found" : "✕ No robots.txt"}
                    </span>
                    <code className="text-slate-400">{report.robotsTxtAudit.domainUrl}/robots.txt</code>
                  </div>

                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                    {report.robotsTxtAudit.botStatuses.map((bot, idx) => (
                      <div
                        key={idx}
                        className="p-3.5 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] flex items-center justify-between gap-3"
                      >
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="font-bold text-sm text-slate-200">{bot.botName}</span>
                            <span className="text-[10px] font-mono text-slate-500">({bot.userAgent})</span>
                          </div>
                          <p className="text-[11px] text-slate-400 mt-0.5 leading-tight">{bot.description}</p>
                        </div>

                        <span
                          className={`px-2.5 py-1 rounded-full text-[11px] font-bold shrink-0 ${
                            bot.status === "Allowed"
                              ? "bg-emerald-500/10 text-emerald-400 border border-emerald-500/20"
                              : bot.status === "Disallowed"
                              ? "bg-rose-500/10 text-rose-400 border border-rose-500/20"
                              : "bg-slate-500/10 text-slate-400 border border-slate-500/20"
                          }`}
                        >
                          {bot.status === "Allowed" ? "✓ Allowed" : bot.status === "Disallowed" ? "✕ Blocked" : "⚪ Default"}
                        </span>
                      </div>
                    ))}
                  </div>
                </>
              ) : (
                <div className="p-6 text-center text-slate-400 text-sm">
                  No robots.txt audit data available. Run a new audit to capture AI bot status.
                </div>
              )}
            </div>
          )}

          {/* AI ENGINE TAB */}
          {activeTab === "ai" && (
            <div className="p-5 rounded-xl border border-blue-500/20 bg-blue-500/5 space-y-4">
              <div className="flex items-center justify-between">
                <div>
                  <h4 className="text-sm font-bold text-blue-400 flex items-center gap-1.5">
                    🤖 Live AI Optimization Engine (BYOK Powered)
                  </h4>
                  <p className="text-xs text-slate-400">Generate high-converting Title & Meta Descriptions using your saved Gemini key.</p>
                </div>
                
                <button
                  onClick={handleGenerateAiSuggestions}
                  disabled={loadingAi}
                  className="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs transition disabled:opacity-50 shadow-md"
                >
                  {loadingAi ? "Analyzing with Gemini..." : "⚡ Generate AI Fixes"}
                </button>
              </div>

              {errorAi && (
                <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-xs font-semibold">
                  {errorAi}
                </div>
              )}

              {aiAnalysis && (
                <div className="p-4 rounded-xl bg-black/70 border border-blue-500/30 text-xs space-y-3 text-slate-200">
                  <div>
                    <span className="text-slate-400 font-semibold block mb-1">🎯 Optimized Title:</span>
                    <div className="p-2.5 rounded-lg bg-blue-950/40 border border-blue-800/50 text-blue-300 font-bold text-sm">
                      {aiAnalysis.optimizedTitle}
                    </div>
                  </div>

                  <div>
                    <span className="text-slate-400 font-semibold block mb-1">📝 Optimized Meta Description:</span>
                    <div className="p-2.5 rounded-lg bg-blue-950/40 border border-blue-800/50 text-slate-200 text-xs leading-relaxed">
                      {aiAnalysis.optimizedMetaDescription}
                    </div>
                  </div>

                  <div>
                    <span className="text-slate-400 font-semibold block mb-1">🔑 Target Keywords:</span>
                    <div className="flex flex-wrap gap-1.5">
                      {aiAnalysis.targetKeywords.map((kw, i) => (
                        <span key={i} className="px-2 py-0.5 rounded bg-blue-500/20 text-blue-300 text-[11px] font-mono border border-blue-500/30">
                          #{kw}
                        </span>
                      ))}
                    </div>
                  </div>

                  {aiAnalysis.actionableTips.length > 0 && (
                    <div>
                      <span className="text-slate-400 font-semibold block mb-1">💡 Actionable Improvements:</span>
                      <ul className="list-disc pl-4 space-y-1 text-slate-300">
                        {aiAnalysis.actionableTips.map((tip, i) => (
                          <li key={i}>{tip}</li>
                        ))}
                      </ul>
                    </div>
                  )}
                </div>
              )}
            </div>
          )}
        </div>

        {/* Close Button */}
        <div className="flex justify-end pt-2 border-t border-[var(--border-color)]">
          <button
            onClick={onClose}
            className="px-5 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 font-bold text-xs transition"
          >
            Close Report
          </button>
        </div>

      </div>
    </div>
  );
}
