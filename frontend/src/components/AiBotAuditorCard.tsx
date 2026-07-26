"use client";

import { apiClient, RobotsTxtAudit } from "@/lib/apiClient";
import { useState } from "react";

interface CardProps {
  websiteId: string;
  websiteName: string;
  tenantId?: string;
}

export default function AiBotAuditorCard({ websiteId, websiteName, tenantId }: CardProps) {
  const [audit, setAudit] = useState<RobotsTxtAudit | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleRunAudit = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await apiClient.websites.getRobotsTxtAudit(websiteId);
      setAudit(data);
    } catch (err: any) {
      setError(err.message || "Failed to audit robots.txt.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-6 rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-base font-bold text-white flex items-center gap-2">
            🤖 AI Bot Auditor (robots.txt)
          </h3>
          <p className="text-xs text-slate-400">
            Check if LLM web crawlers (ChatGPT, Claude, Perplexity) are allowed to index {websiteName}.
          </p>
        </div>

        <button
          onClick={handleRunAudit}
          disabled={loading}
          className="px-3.5 py-1.5 rounded-lg border border-purple-500/30 text-purple-400 hover:bg-purple-500/10 font-semibold text-xs transition disabled:opacity-50"
        >
          {loading ? "Checking..." : "🔍 Check AI Crawlers"}
        </button>
      </div>

      {error && (
        <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-xs font-semibold">
          {error}
        </div>
      )}

      {audit && (
        <div className="space-y-3 pt-2">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {audit.botStatuses.map((bot, idx) => (
              <div
                key={idx}
                className="p-3.5 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] flex items-center justify-between gap-3"
              >
                <div>
                  <div className="flex items-center space-x-2">
                    <span className="font-bold text-sm text-slate-200">{bot.botName}</span>
                    <span className="text-[10px] font-mono text-slate-400">({bot.userAgent})</span>
                  </div>
                  <p className="text-[11px] text-slate-400 mt-0.5 leading-tight">{bot.description}</p>
                </div>

                <span
                  className={`px-2.5 py-1 rounded-full text-[11px] font-bold ${
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

          <div className="text-[11px] text-slate-500 text-right pt-1">
            Status based on <code className="text-slate-400">{audit.domainUrl}/robots.txt</code>
          </div>
        </div>
      )}
    </div>
  );
}
