"use client";

import { apiClient, AiSeoRecommendation } from "@/lib/apiClient";
import { useState } from "react";

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
  issues: SeoIssue[];
  crawledAt: string;
}

interface ModalProps {
  report: AuditReportData | null;
  tenantId: string;
  onClose: () => void;
}

export default function AuditDetailsModal({ report, tenantId, onClose }: ModalProps) {
  const [aiAnalysis, setAiAnalysis] = useState<AiSeoRecommendation | null>(null);
  const [loadingAi, setLoadingAi] = useState(false);
  const [errorAi, setErrorAi] = useState<string | null>(null);

  if (!report) return null;

  const getScoreColor = (score: number) => {
    if (score >= 90) return "text-emerald-400 border-emerald-500/30 bg-emerald-500/10";
    if (score >= 60) return "text-amber-400 border-amber-500/30 bg-amber-500/10";
    return "text-rose-400 border-rose-500/30 bg-rose-500/10";
  };

  const handleGenerateAiSuggestions = async () => {
    setLoadingAi(true);
    setErrorAi(null);
    try {
      const data = await apiClient.websites.getAiSuggestions(report.websiteId, tenantId);
      setAiAnalysis(data);
    } catch (err: any) {
      setErrorAi(err.message || "Failed to generate AI recommendations.");
    } finally {
      setLoadingAi(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-fadeIn">
      <div className="relative w-full max-w-3xl max-h-[90vh] overflow-y-auto rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] shadow-2xl p-6 sm:p-8 space-y-6">
        
        {/* Header */}
        <div className="flex items-center justify-between border-b border-[var(--border-color)] pb-4">
          <div>
            <span className="text-xs uppercase tracking-wider text-blue-400 font-bold">SEO & Crawler Audit Report</span>
            <h2 className="text-2xl font-extrabold text-white mt-1">{report.websiteName}</h2>
            <p className="text-xs text-slate-400">{report.domainUrl} • Crawled {new Date(report.crawledAt).toLocaleTimeString()}</p>
          </div>
          
          <button
            onClick={onClose}
            className="p-2 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 transition"
          >
            ✕
          </button>
        </div>

        {/* Score Overview Cards */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div className={`p-4 rounded-xl border flex flex-col items-center justify-center text-center ${getScoreColor(report.overallScore)}`}>
            <span className="text-xs font-semibold uppercase tracking-wider opacity-80">Audit Score</span>
            <span className="text-4xl font-extrabold my-1">{report.overallScore}<span className="text-base font-normal">/100</span></span>
            <span className="text-[11px] font-bold">
              {report.overallScore >= 90 ? "Excellent Performance" : report.overallScore >= 60 ? "Needs Improvement" : "Critical SEO Issues"}
            </span>
          </div>

          <div className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] flex flex-col justify-center space-y-1">
            <span className="text-xs font-semibold text-slate-400">HTTP Response Status</span>
            <div className="flex items-center space-x-2">
              <span className="w-2.5 h-2.5 rounded-full bg-emerald-500"></span>
              <span className="text-lg font-bold text-white">{report.statusCode} OK</span>
            </div>
            <span className="text-[11px] text-slate-400">Page accessible by search crawlers</span>
          </div>

          <div className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] flex flex-col justify-center space-y-1">
            <span className="text-xs font-semibold text-slate-400">Detected Issues</span>
            <span className="text-lg font-bold text-amber-400">{report.issues.length} Items</span>
            <span className="text-[11px] text-slate-400">Title, Meta Description, Schema</span>
          </div>
        </div>

        {/* Extracted Page Meta Tags */}
        <div className="space-y-3">
          <h3 className="text-sm font-bold uppercase text-slate-300 tracking-wider">Extracted Meta Tags</h3>
          
          <div className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] space-y-3">
            <div>
              <div className="flex items-center justify-between text-xs mb-1">
                <span className="font-semibold text-blue-400">&lt;title&gt; Tag</span>
                <span className="text-slate-400">{report.title.length} characters</span>
              </div>
              <div className="p-2.5 rounded-lg bg-black/40 text-sm font-mono text-slate-200 border border-slate-800">
                {report.title}
              </div>
            </div>

            <div>
              <div className="flex items-center justify-between text-xs mb-1">
                <span className="font-semibold text-blue-400">&lt;meta name="description"&gt;</span>
                <span className="text-slate-400">{report.metaDescription.length} characters</span>
              </div>
              <div className="p-2.5 rounded-lg bg-black/40 text-sm font-mono text-slate-200 border border-slate-800">
                {report.metaDescription}
              </div>
            </div>
          </div>
        </div>

        {/* Identified SEO Issues & Recommendations */}
        <div className="space-y-3">
          <h3 className="text-sm font-bold uppercase text-slate-300 tracking-wider">SEO Warnings & Recommendations</h3>
          
          <div className="space-y-2.5">
            {report.issues.map((issue, idx) => (
              <div key={idx} className="p-4 rounded-xl border border-amber-500/20 bg-amber-500/5 space-y-1.5">
                <div className="flex items-center space-x-2">
                  <span className="px-2 py-0.5 rounded text-[10px] font-extrabold uppercase tracking-wide bg-amber-500/20 text-amber-300 border border-amber-500/30">
                    {issue.severity}
                  </span>
                  <span className="text-sm font-bold text-slate-200">{issue.description}</span>
                </div>
                <p className="text-xs text-slate-300 pl-1 leading-relaxed">
                  💡 <strong className="text-amber-300">Recommendation:</strong> {issue.recommendation}
                </p>
              </div>
            ))}
          </div>
        </div>

        {/* AI Recommendations Action (BYOK Powered) */}
        <div className="p-5 rounded-xl border border-blue-500/20 bg-blue-500/5 space-y-4">
          <div className="flex items-center justify-between">
            <div>
              <h4 className="text-sm font-bold text-blue-400 flex items-center gap-1.5">
                🤖 Live AI Optimization Engine (BYOK Powered)
              </h4>
              <p className="text-xs text-slate-400">Generate high-converting Turkish Title & Meta Descriptions using your saved Gemini key.</p>
            </div>
            
            <button
              onClick={handleGenerateAiSuggestions}
              disabled={loadingAi}
              className="px-4 py-2 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs transition disabled:opacity-50 shadow-md"
            >
              {loadingAi ? "Analyzing with Gemini..." : "⚡ Generate Live AI Fixes"}
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
                <span className="text-slate-400 font-semibold block mb-1">🎯 Önerilen Türkçe Başlık (Title):</span>
                <div className="p-2.5 rounded-lg bg-blue-950/40 border border-blue-800/50 text-blue-300 font-bold text-sm">
                  {aiAnalysis.optimizedTitle}
                </div>
              </div>

              <div>
                <span className="text-slate-400 font-semibold block mb-1">📝 Önerilen Türkçe Meta Açıklama:</span>
                <div className="p-2.5 rounded-lg bg-blue-950/40 border border-blue-800/50 text-slate-200 text-xs leading-relaxed">
                  {aiAnalysis.optimizedMetaDescription}
                </div>
              </div>

              <div>
                <span className="text-slate-400 font-semibold block mb-1">🔑 Hedef Anahtar Kelimeler:</span>
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
                  <span className="text-slate-400 font-semibold block mb-1">💡 Kritik İyileştirme Adımları:</span>
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
