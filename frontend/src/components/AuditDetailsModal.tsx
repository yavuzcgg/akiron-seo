"use client";

import Modal from "@/components/ui/Modal";
import { apiClient, AiSeoRecommendation, AuditReportData } from "@/lib/apiClient";
import { getErrorMessage } from "@/lib/errors";
import { AlertTriangle, BarChart3, Bot, FileText, Search, Sparkles, Zap } from "lucide-react";
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
    } catch (err: unknown) {
      setErrorAi(getErrorMessage(err, "Failed to generate AI recommendations."));
    } finally {
      setLoadingAi(false);
    }
  };

  const openExecutiveReport = () => {
    const token = localStorage.getItem("akiron_token");
    const url = apiClient.reports.getExecutiveReportUrl(report.websiteId);
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
  };

  return (
    <Modal
      onClose={onClose}
      title={report.websiteName}
      subtitle={`${report.domainUrl} • Crawled ${new Date(report.crawledAt).toLocaleString()}`}
      icon={<Search size={18} aria-hidden />}
      maxWidthClass="max-w-4xl"
      footer={
        <div className="flex w-full items-center justify-between">
          <button
            onClick={openExecutiveReport}
            className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3.5 py-1.5 text-xs font-bold text-muted transition-colors hover:text-foreground"
          >
            <FileText size={14} aria-hidden /> Executive Report
          </button>
          <button
            onClick={onClose}
            className="cursor-pointer rounded-lg bg-elevated px-5 py-2 text-xs font-bold text-foreground transition-colors hover:opacity-80"
          >
            Close
          </button>
        </div>
      }
    >
      <div className="space-y-5">

        {/* Score Overview Cards */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <div className={`p-4 rounded-xl border flex flex-col items-center justify-center text-center ${getScoreColor(report.overallScore)}`}>
            <span className="text-[10px] font-semibold uppercase tracking-wider opacity-80">Score</span>
            <span className="text-3xl font-extrabold my-0.5">{report.overallScore}<span className="text-sm font-normal">/100</span></span>
            <span className="text-[10px] font-bold">{getScoreLabel(report.overallScore)}</span>
          </div>

          <div className="p-4 rounded-xl border border-border bg-bg flex flex-col items-center justify-center text-center">
            <span className="text-[10px] font-semibold text-muted uppercase">HTTP</span>
            <div className="flex items-center gap-1.5 my-0.5">
              <span className={`w-2 h-2 rounded-full ${report.statusCode === 200 ? "bg-emerald-500" : "bg-rose-500"}`}></span>
              <span className="text-lg font-bold text-foreground">{report.statusCode}</span>
            </div>
            <span className="text-[10px] text-muted">{report.statusCode === 200 ? "OK" : "Error"}</span>
          </div>

          <div className="p-4 rounded-xl border border-border bg-bg flex flex-col items-center justify-center text-center">
            <span className="text-[10px] font-semibold text-muted uppercase">Issues</span>
            <span className="text-lg font-bold text-amber-400 my-0.5">{report.issues.length}</span>
            <span className="text-[10px] text-muted">
              {criticalCount > 0 && <span className="text-rose-400">{criticalCount} critical</span>}
              {criticalCount > 0 && warningCount > 0 && " · "}
              {warningCount > 0 && <span className="text-amber-400">{warningCount} warn</span>}
            </span>
          </div>

          <div className="p-4 rounded-xl border border-border bg-bg flex flex-col items-center justify-center text-center">
            <span className="text-[10px] font-semibold text-muted uppercase">AI Bots</span>
            <span className="text-lg font-bold text-purple-400 my-0.5">
              {report.robotsTxtAudit ? report.robotsTxtAudit.botStatuses.filter(b => b.status === "Allowed").length : "—"}
            </span>
            <span className="text-[10px] text-muted">Allowed</span>
          </div>
        </div>

        {/* Tabs */}
        <div className="flex gap-1 border-b border-border text-xs font-bold">
          {([
            { key: "overview", label: "Overview", Icon: BarChart3 },
            { key: "issues", label: `Issues (${report.issues.length})`, Icon: AlertTriangle },
            { key: "robots", label: "AI Bots", Icon: Bot },
            { key: "ai", label: "AI Engine", Icon: Sparkles },
          ] as const).map(({ key, label, Icon }) => (
            <button
              key={key}
              onClick={() => setActiveTab(key)}
              className={`flex cursor-pointer items-center gap-1.5 rounded-t-lg border-b-2 px-4 py-2.5 transition-colors ${
                activeTab === key
                  ? "border-primary text-primary"
                  : "border-transparent text-muted hover:text-foreground"
              }`}
            >
              <Icon size={14} aria-hidden />
              {label}
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
                <h3 className="text-sm font-bold uppercase text-foreground tracking-wider">Extracted Meta Tags</h3>
                
                <div className="p-4 rounded-xl border border-border bg-bg space-y-3">
                  <div>
                    <div className="flex items-center justify-between text-xs mb-1">
                      <span className="font-semibold text-blue-400">&lt;title&gt; Tag</span>
                      <span className={`font-mono ${report.title.length >= 30 && report.title.length <= 60 ? "text-emerald-400" : "text-amber-400"}`}>
                        {report.title.length} chars
                      </span>
                    </div>
                    <div className="p-2.5 rounded-lg bg-black/40 text-sm font-mono text-foreground border border-border">
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
                    <div className="p-2.5 rounded-lg bg-black/40 text-sm font-mono text-foreground border border-border">
                      {report.metaDescription}
                    </div>
                  </div>
                </div>
              </div>

              {/* H1 Tags */}
              <div className="space-y-2">
                <h3 className="text-sm font-bold uppercase text-foreground tracking-wider">H1 Headings</h3>
                <div className="p-4 rounded-xl border border-border bg-bg">
                  {report.h1Tags && report.h1Tags.length > 0 ? (
                    <div className="space-y-2">
                      {report.h1Tags.map((h1, idx) => (
                        <div key={idx} className="flex items-center gap-2">
                          <span className={`px-1.5 py-0.5 rounded text-[10px] font-bold ${report.h1Tags.length === 1 ? "bg-emerald-500/20 text-emerald-400" : "bg-amber-500/20 text-amber-400"}`}>
                            H1#{idx + 1}
                          </span>
                          <span className="text-sm text-foreground font-mono">{h1}</span>
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
                <h3 className="text-sm font-bold uppercase text-foreground tracking-wider">Canonical URL</h3>
                <div className="p-4 rounded-xl border border-border bg-bg">
                  {report.canonicalUrl ? (
                    <div className="flex items-center gap-2">
                      <span className="px-1.5 py-0.5 rounded text-[10px] font-bold bg-emerald-500/20 text-emerald-400">✓</span>
                      <code className="text-sm text-foreground font-mono">{report.canonicalUrl}</code>
                    </div>
                  ) : (
                    <p className="text-xs text-amber-400">⚠ No canonical URL specified. Consider adding &lt;link rel=&quot;canonical&quot;&gt; to prevent duplicate content.</p>
                  )}
                </div>
              </div>

              {/* Score Breakdown — rendered from the crawler's own calculation, so these
                  bars always sum to the overall score shown above. Older audits predate the
                  stored breakdown; the section is hidden rather than reconstructed. */}
              {report.scoreBreakdown && report.scoreBreakdown.length > 0 && (
                <div className="space-y-2">
                  <h3 className="text-sm font-bold uppercase text-foreground tracking-wider">Score Breakdown</h3>
                  <div className="p-4 rounded-xl border border-border bg-bg space-y-2">
                    {report.scoreBreakdown.map((item, idx) => (
                      <div key={idx} className="flex items-center gap-3">
                        <span className="text-xs text-muted w-36 shrink-0">{item.label}</span>
                        <div className="flex-1 h-2 rounded-full bg-elevated overflow-hidden">
                          <div
                            className={`h-full rounded-full transition-all ${item.earnedPoints >= item.maxPoints ? "bg-emerald-500" : item.earnedPoints > 0 ? "bg-amber-500" : "bg-rose-500"}`}
                            style={{ width: `${(item.earnedPoints / item.maxPoints) * 100}%` }}
                          />
                        </div>
                        <span className={`text-xs font-mono w-12 text-right ${item.earnedPoints >= item.maxPoints ? "text-emerald-400" : item.earnedPoints > 0 ? "text-amber-400" : "text-rose-400"}`}>
                          {item.earnedPoints}/{item.maxPoints}
                        </span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
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
                      <code className="text-[10px] text-subtle font-mono">{issue.code}</code>
                    </div>
                    <p className="text-sm font-semibold text-foreground">{issue.description}</p>
                    <p className="text-xs text-foreground leading-relaxed">
      <strong className="text-foreground">Fix:</strong> {issue.recommendation}
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
                    <code className="text-muted">{report.robotsTxtAudit.domainUrl}/robots.txt</code>
                  </div>

                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                    {report.robotsTxtAudit.botStatuses.map((bot, idx) => (
                      <div
                        key={idx}
                        className="p-3.5 rounded-xl border border-border bg-bg flex items-center justify-between gap-3"
                      >
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="font-bold text-sm text-foreground">{bot.botName}</span>
                            <span className="text-[10px] font-mono text-subtle">({bot.userAgent})</span>
                          </div>
                          <p className="text-[11px] text-muted mt-0.5 leading-tight">{bot.description}</p>
                        </div>

                        <span
                          className={`px-2.5 py-1 rounded-full text-[11px] font-bold shrink-0 ${
                            bot.status === "Allowed"
                              ? "bg-emerald-500/10 text-emerald-400 border border-emerald-500/20"
                              : bot.status === "Disallowed"
                              ? "bg-rose-500/10 text-rose-400 border border-rose-500/20"
                              : "bg-slate-500/10 text-muted border border-slate-500/20"
                          }`}
                        >
                          {bot.status === "Allowed" ? "✓ Allowed" : bot.status === "Disallowed" ? "✕ Blocked" : "⚪ Default"}
                        </span>
                      </div>
                    ))}
                  </div>
                </>
              ) : (
                <div className="p-6 text-center text-muted text-sm">
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
                  <h4 className="flex items-center gap-1.5 text-sm font-bold text-primary">
                    <Sparkles size={15} aria-hidden /> Live AI Optimization Engine (BYOK)
                  </h4>
                  <p className="text-xs text-muted">Generate Title &amp; Meta Descriptions using your saved Gemini key.</p>
                </div>

                <button
                  onClick={handleGenerateAiSuggestions}
                  disabled={loadingAi}
                  className="flex cursor-pointer items-center gap-1.5 rounded-lg bg-primary px-4 py-2 text-xs font-bold text-on-primary transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
                >
                  <Zap size={14} aria-hidden />
                  {loadingAi ? "Analyzing…" : "Generate AI Fixes"}
                </button>
              </div>

              {errorAi && (
                <div className="rounded-lg border border-danger/20 bg-danger/10 p-3 text-xs font-semibold text-danger">
                  {errorAi}
                </div>
              )}

              {aiAnalysis && (
                <div className="space-y-3 rounded-xl border border-border bg-surface p-4 text-xs text-foreground">
                  <div>
                    <span className="mb-1 block font-semibold text-muted">Optimized Title</span>
                    <div className="rounded-lg border border-border bg-bg p-2.5 text-sm font-bold text-primary">
                      {aiAnalysis.optimizedTitle}
                    </div>
                  </div>

                  <div>
                    <span className="mb-1 block font-semibold text-muted">Optimized Meta Description</span>
                    <div className="rounded-lg border border-border bg-bg p-2.5 text-xs leading-relaxed text-foreground">
                      {aiAnalysis.optimizedMetaDescription}
                    </div>
                  </div>

                  <div>
                    <span className="mb-1 block font-semibold text-muted">Target Keywords</span>
                    <div className="flex flex-wrap gap-1.5">
                      {aiAnalysis.targetKeywords.map((kw, i) => (
                        <span key={i} className="rounded border border-primary/30 bg-primary/10 px-2 py-0.5 font-mono text-[11px] text-primary">
                          #{kw}
                        </span>
                      ))}
                    </div>
                  </div>

                  {aiAnalysis.actionableTips.length > 0 && (
                    <div>
                      <span className="mb-1 block font-semibold text-muted">Actionable Improvements</span>
                      <ul className="list-disc space-y-1 pl-4 text-foreground">
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
      </div>
    </Modal>
  );
}
