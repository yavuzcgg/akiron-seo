"use client";

import { apiClient, RobotsTxtAudit } from "@/lib/apiClient";
import { useApp } from "@/components/providers";
import { getErrorMessage } from "@/lib/errors";
import { Bot, Search } from "lucide-react";
import { useState } from "react";

interface CardProps {
  websiteId: string;
  websiteName: string;
}

export default function AiBotAuditorCard({ websiteId, websiteName }: CardProps) {
  const { t } = useApp();
  const [audit, setAudit] = useState<RobotsTxtAudit | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleRunAudit = async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await apiClient.websites.getRobotsTxtAudit(websiteId);
      setAudit(data);
    } catch (err: unknown) {
      setError(getErrorMessage(err, t("robotsAuditFailed")));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-6 rounded-2xl border border-border bg-surface space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="flex items-center gap-2 text-base font-bold text-foreground">
            <Bot size={16} className="text-primary" aria-hidden /> {t("aiBotAuditor")}
          </h3>
          <p className="text-xs text-muted">
            {t("aiBotDescription")} ({websiteName})
          </p>
        </div>

        <button
          onClick={handleRunAudit}
          disabled={loading}
          className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3.5 py-1.5 text-xs font-semibold text-muted transition-colors hover:text-foreground disabled:cursor-not-allowed disabled:opacity-50"
        >
          <Search size={13} aria-hidden /> {loading ? t("checkingCrawlers") : t("checkCrawlers")}
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
                className="p-3.5 rounded-xl border border-border bg-bg flex items-center justify-between gap-3"
              >
                <div>
                  <div className="flex items-center space-x-2">
                    <span className="font-bold text-sm text-foreground">{bot.botName}</span>
                    <span className="text-[10px] font-mono text-muted">({bot.userAgent})</span>
                  </div>
                  <p className="text-[11px] text-muted mt-0.5 leading-tight">{bot.description}</p>
                </div>

                <span
                  className={`px-2.5 py-1 rounded-full text-[11px] font-bold ${
                    bot.status === "Allowed"
                      ? "bg-emerald-500/10 text-emerald-400 border border-emerald-500/20"
                      : bot.status === "Disallowed"
                      ? "bg-rose-500/10 text-rose-400 border border-rose-500/20"
                      : "bg-slate-500/10 text-muted border border-slate-500/20"
                  }`}
                >
                  {bot.status === "Allowed" ? `✓ ${t("allowed")}` : bot.status === "Disallowed" ? `✕ ${t("blocked")}` : `⚪ ${t("defaultStatus")}`}
                </span>
              </div>
            ))}
          </div>

          <div className="text-[11px] text-subtle text-right pt-1">
            {t("robotsStatusBasedOn")} <code className="text-muted">{audit.domainUrl}/robots.txt</code>
          </div>
        </div>
      )}
    </div>
  );
}
