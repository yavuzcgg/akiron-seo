"use client";

import DataSourceBadge from "@/components/DataSourceBadge";
import { useApp } from "@/components/providers";
import { apiClient } from "@/lib/apiClient";
import { queryKeys } from "@/lib/queryKeys";
import { useQuery } from "@tanstack/react-query";
import { RefreshCw, TrendingUp } from "lucide-react";

interface ComponentProps {
  websiteId: string;
  websiteName: string;
}

export default function GscAnalyticsCard({ websiteId, websiteName }: ComponentProps) {
  const { t } = useApp();
  const metricsQuery = useQuery({ queryKey: queryKeys.gsc(websiteId), queryFn: () => apiClient.gsc.getAnalytics(websiteId) });

  return (
    <section className="space-y-4 rounded-2xl border border-primary/20 bg-primary/5 p-6" aria-labelledby={`gsc-${websiteId}`}>
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-primary/20 pb-3">
        <div>
          <span className="flex items-center gap-2 text-[10px] font-extrabold uppercase tracking-wider text-primary">{t("gscAnalytics")} <DataSourceBadge source={metricsQuery.data?.dataSource} /></span>
          <h3 id={`gsc-${websiteId}`} className="mt-0.5 flex items-center gap-2 text-lg font-extrabold text-foreground"><TrendingUp size={18} className="text-primary" aria-hidden /> {t("organicPerformance")}</h3>
          <p className="text-xs text-muted">
            {metricsQuery.data?.dataSource && metricsQuery.data.dataSource !== "Live"
              ? t("gscSimulated")
              : `${t("organicPerformanceFor")} ${websiteName}.`}
          </p>
        </div>
        <button type="button" onClick={() => metricsQuery.refetch()} disabled={metricsQuery.isFetching} className="flex min-h-11 cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3 text-xs font-semibold text-muted transition-colors hover:text-foreground disabled:opacity-50">
          <RefreshCw size={13} aria-hidden /> {metricsQuery.isFetching ? t("refreshing") : t("refresh")}
        </button>
      </div>

      {metricsQuery.isPending ? (
        <p className="text-xs text-muted" role="status">{t("loadingGsc")}</p>
      ) : metricsQuery.isError ? (
        <p className="rounded-lg border border-danger/20 bg-danger/10 p-3 text-xs text-danger" role="alert">{t("gscLoadFailed")}</p>
      ) : (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          {[
            [t("organicClicks"), metricsQuery.data.totalClicks.toLocaleString(), t("searchClicks")],
            [t("totalImpressions"), metricsQuery.data.totalImpressions.toLocaleString(), t("searchViews")],
            [t("averageCtr"), `${metricsQuery.data.averageCtrPercentage}%`, t("clickThroughRate")],
            [t("averagePosition"), `#${metricsQuery.data.averagePosition}`, t("organicRank")],
          ].map(([label, value, help]) => (
            <div key={label} className="space-y-1 rounded-xl border border-border bg-bg p-3.5">
              <span className="text-[10px] font-bold uppercase text-muted">{label}</span>
              <div className="text-xl font-extrabold text-primary">{value}</div>
              <span className="text-[10px] text-subtle">{help}</span>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}
