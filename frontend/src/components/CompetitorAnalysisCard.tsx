"use client";

import DataSourceBadge from "@/components/DataSourceBadge";
import { useApp } from "@/components/providers";
import { apiClient, DataSource } from "@/lib/apiClient";
import { queryKeys } from "@/lib/queryKeys";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Target, Zap } from "lucide-react";
import { useState } from "react";
import { getErrorMessage } from "@/lib/errors";

interface ComponentProps {
  websiteId: string;
  websiteName: string;
}

export default function CompetitorAnalysisCard({ websiteId, websiteName }: ComponentProps) {
  const { t } = useApp();
  const queryClient = useQueryClient();
  const [competitorInput, setCompetitorInput] = useState("");
  const [error, setError] = useState<string | null>(null);
  const competitorsQuery = useQuery({ queryKey: queryKeys.competitors(websiteId), queryFn: () => apiClient.competitors.list(websiteId) });
  const analyzeMutation = useMutation({
    mutationFn: (domain: string) => apiClient.competitors.analyze(websiteId, domain),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.competitors(websiteId) }),
    onError: (err) => setError(getErrorMessage(err, t("competitorAnalysisFailed"))),
  });
  const competitorData = analyzeMutation.data ?? competitorsQuery.data?.[0];
  const gapDataSource: DataSource | undefined = competitorData?.dataSource;

  const handleAnalyzeCompetitor = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!competitorInput.trim()) return;

    setError(null);
    analyzeMutation.mutate(competitorInput.trim());
  };

  return (
    <div className="p-5 rounded-xl border border-border bg-bg space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border pb-3">
        <div>
          <h4 className="flex items-center gap-2 text-sm font-bold text-foreground">
            <Target size={16} className="text-primary" aria-hidden /> {t("competitorIntelligence")}
            <DataSourceBadge source={gapDataSource} />
          </h4>
          <p className="text-[11px] text-muted">
            {gapDataSource && gapDataSource !== "Live"
              ? t("simulatedGap")
              : `${t("competitorDescription")} (${websiteName})`}
          </p>
        </div>

        <form onSubmit={handleAnalyzeCompetitor} className="flex items-center space-x-2">
          <label htmlFor={`competitor-${websiteId}`} className="sr-only">{t("competitorDomain")}</label>
          <input
            id={`competitor-${websiteId}`}
            type="text"
            placeholder={t("competitorDomain")}
            value={competitorInput}
            onChange={(e) => setCompetitorInput(e.target.value)}
            required
            maxLength={2048}
            className="px-3 py-1.5 rounded-lg border border-border bg-surface text-xs font-medium focus:outline-none focus:ring-1 focus:ring-amber-500"
          />
          <button
            type="submit"
            disabled={analyzeMutation.isPending}
            className="flex cursor-pointer items-center gap-1.5 rounded-lg bg-primary px-3 py-1.5 text-xs font-bold text-on-primary transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Zap size={13} aria-hidden /> {analyzeMutation.isPending ? t("analyzing") : t("gapAnalysis")}
          </button>
        </form>
      </div>

      {(error || competitorsQuery.isError) && (
        <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-xs font-semibold">
          {error || getErrorMessage(competitorsQuery.error, t("competitorLoadFailed"))}
        </div>
      )}

      {competitorsQuery.isPending ? (
        <p className="text-xs text-muted" role="status">{t("loadingCompetitors")}</p>
      ) : !competitorData ? (
        <p className="text-xs text-muted">{t("noCompetitors")}</p>
      ) : (
        <div className="space-y-3">
          <div className="flex items-center justify-between p-3 rounded-lg bg-surface border border-border">
            <span className="text-xs font-bold text-foreground">
              {competitorData.yourDomain} vs <span className="text-amber-400">{competitorData.competitorDomain}</span>
            </span>
            <span className="px-2.5 py-0.5 rounded-full bg-amber-500/10 text-amber-400 text-xs font-extrabold border border-amber-500/20">
              {competitorData.overlapScore}% {t("competitorOverlap")}
            </span>
          </div>

          <h5 className="text-xs font-bold text-foreground uppercase tracking-wider">{t("topKeywordGaps")}</h5>

          <div className="space-y-2">
            {competitorData.missingKeywordOpportunities.map((item, i) => (
              <div
                key={i}
                className="p-3 rounded-lg border border-border bg-surface flex flex-wrap items-center justify-between gap-2 text-xs"
              >
                <div>
                  <span className="font-bold text-foreground">{item.keyword}</span>
                  <div className="text-[10px] text-muted mt-0.5">
                    {t("estimatedVolume")}: <strong className="text-foreground">{item.estimatedSearchVolume.toLocaleString()} / mo</strong>
                  </div>
                </div>

                <div className="flex items-center space-x-3 text-[11px]">
                  <span className="px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-400 font-extrabold">
                    {t("competitor")}: #{item.competitorRank}
                  </span>
                  <span className="px-2 py-0.5 rounded bg-rose-500/10 text-rose-400 font-extrabold">
                    {t("you")}: {item.yourRank > 0 ? `#${item.yourRank}` : t("notRanking")}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
