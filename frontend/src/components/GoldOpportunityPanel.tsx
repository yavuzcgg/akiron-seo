"use client";

import { apiClient } from "@/lib/apiClient";
import { useApp } from "@/components/providers";
import { getErrorMessage } from "@/lib/errors";
import { queryKeys } from "@/lib/queryKeys";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { RefreshCw, Sparkles, X, Zap } from "lucide-react";

interface PanelProps {
  websiteId: string;
  websiteName: string;
  onOpenWriter?: (keyword: string, missingPath: string) => void;
}

function extractKeywordAndPath(message: string) {
  let keyword = "target page content";
  let path = "";
  const keywordMatch = message.match(/keyword '([^']+)'/i);
  if (keywordMatch) keyword = keywordMatch[1];
  const urlMatch = message.match(/'(https?:\/\/[^']+)'/i);
  if (urlMatch) {
    try { path = new URL(urlMatch[1]).pathname; } catch { path = urlMatch[1]; }
  }
  return { keyword, path };
}

export default function GoldOpportunityPanel({ websiteId, websiteName, onOpenWriter }: PanelProps) {
  const { t } = useApp();
  const queryClient = useQueryClient();
  const opportunitiesQuery = useQuery({
    queryKey: queryKeys.opportunities(websiteId),
    queryFn: () => apiClient.websites.getGoldOpportunities(websiteId),
  });
  const dismissMutation = useMutation({
    mutationFn: apiClient.notifications.markRead,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.opportunities(websiteId) }),
  });

  if (opportunitiesQuery.isPending) {
    return <div className="rounded-xl border border-border bg-bg p-4 text-xs text-muted" role="status">{t("loadingOpportunities")}</div>;
  }
  if (opportunitiesQuery.isError) {
    return <div className="rounded-xl border border-danger/20 bg-danger/10 p-4 text-xs text-danger" role="alert">{getErrorMessage(opportunitiesQuery.error, t("opportunityLoadFailed"))}</div>;
  }
  if (opportunitiesQuery.data.length === 0) {
    return <div className="rounded-xl border border-border bg-bg p-4 text-xs text-muted">{t("noOpportunities")}</div>;
  }

  return (
    <section className="space-y-3 rounded-2xl border border-warning/30 bg-warning/10 p-5 shadow-lg" aria-labelledby={`opportunities-${websiteId}`}>
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          <Sparkles size={20} className="text-warning" aria-hidden />
          <div>
            <h3 id={`opportunities-${websiteId}`} className="text-sm font-extrabold uppercase tracking-wider text-warning">{t("goldOpportunities")} ({opportunitiesQuery.data.length})</h3>
            <p className="text-xs text-foreground">{t("goldOpportunityHelp")} ({websiteName})</p>
          </div>
        </div>
        <button type="button" onClick={() => opportunitiesQuery.refetch()} disabled={opportunitiesQuery.isFetching} className="flex min-h-11 cursor-pointer items-center gap-1 px-2 text-xs font-semibold text-muted hover:text-foreground disabled:opacity-50"><RefreshCw size={12} aria-hidden /> {t("refresh")}</button>
      </div>

      <div className="space-y-2.5">
        {opportunitiesQuery.data.map((opportunity) => (
          <div key={opportunity.notificationId} className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-border bg-surface p-3.5 text-xs">
            <div className="max-w-xl space-y-1"><div className="flex items-center gap-2"><span className="rounded border border-warning/30 bg-warning/20 px-2 py-0.5 text-[10px] font-extrabold text-warning">{t("missingCitation")}</span><span className="font-mono text-[11px] text-muted">{new Date(opportunity.detectedAt).toLocaleTimeString()}</span></div><p className="font-semibold leading-relaxed text-foreground">{opportunity.message}</p></div>
            <div className="flex items-center gap-2">
              <button type="button" onClick={() => { const target = extractKeywordAndPath(opportunity.message); onOpenWriter?.(target.keyword, target.path); }} className="flex min-h-11 cursor-pointer items-center gap-1.5 rounded-lg bg-warning px-3.5 text-xs font-extrabold text-bg hover:opacity-90"><Zap size={13} aria-hidden /> {t("createPageWithAi")}</button>
              <button type="button" onClick={() => dismissMutation.mutate(opportunity.notificationId)} disabled={dismissMutation.isPending} className="min-h-11 min-w-11 cursor-pointer rounded-lg bg-elevated text-muted hover:text-foreground disabled:opacity-50" aria-label={t("dismissAlert")}><X size={14} className="mx-auto" aria-hidden /></button>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}
