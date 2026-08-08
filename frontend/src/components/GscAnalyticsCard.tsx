"use client";

import DataSourceBadge from "@/components/DataSourceBadge";
import { apiClient, GscMetrics } from "@/lib/apiClient";
import { RefreshCw, TrendingUp } from "lucide-react";
import { useEffect, useState } from "react";

interface ComponentProps {
  websiteId: string;
  websiteName: string;
}

export default function GscAnalyticsCard({ websiteId, websiteName }: ComponentProps) {
  const [metrics, setMetrics] = useState<GscMetrics | null>(null);
  const [loading, setLoading] = useState(false);

  const fetchGscAnalytics = async () => {
    setLoading(true);
    try {
      const data = await apiClient.gsc.getAnalytics(websiteId);
      setMetrics(data);
    } catch {
      // Offline fallback
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchGscAnalytics();
  }, [websiteId]);

  if (loading && !metrics) return null;
  if (!metrics) return null;

  return (
    <div className="p-6 rounded-2xl border border-blue-500/20 bg-blue-500/5 space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-blue-500/20 pb-3">
        <div>
          <span className="text-[10px] font-extrabold uppercase tracking-wider text-blue-400 flex items-center gap-2">
            GOOGLE SEARCH CONSOLE (GSC) ANALYTICS
            <DataSourceBadge source={metrics.dataSource} />
          </span>
          <h3 className="mt-0.5 flex items-center gap-2 text-lg font-extrabold text-foreground">
            <TrendingUp size={18} className="text-primary" aria-hidden /> Organic Google Search Performance
          </h3>
          <p className="text-xs text-muted">
            {metrics.dataSource && metrics.dataSource !== "Live"
              ? "Placeholder figures — Search Console is not connected yet, so these are generated locally and are not organic search data."
              : `Organic search clicks, impressions, CTR %, and avg position for ${websiteName}.`}
          </p>
        </div>

        <button
          onClick={fetchGscAnalytics}
          className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-muted transition-colors hover:text-foreground"
        >
          <RefreshCw size={13} aria-hidden /> Refresh
        </button>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <div className="p-3.5 rounded-xl border border-border bg-bg space-y-1">
          <span className="text-[10px] font-bold text-muted uppercase">Organic Clicks</span>
          <div className="text-xl font-extrabold text-emerald-400">{metrics.totalClicks.toLocaleString()}</div>
          <span className="text-[10px] text-subtle">Google SERP Clicks</span>
        </div>

        <div className="p-3.5 rounded-xl border border-border bg-bg space-y-1">
          <span className="text-[10px] font-bold text-muted uppercase">Total Impressions</span>
          <div className="text-xl font-extrabold text-blue-400">{metrics.totalImpressions.toLocaleString()}</div>
          <span className="text-[10px] text-subtle">Search Views</span>
        </div>

        <div className="p-3.5 rounded-xl border border-border bg-bg space-y-1">
          <span className="text-[10px] font-bold text-muted uppercase">Average CTR</span>
          <div className="text-xl font-extrabold text-purple-400">{metrics.averageCtrPercentage}%</div>
          <span className="text-[10px] text-subtle">Click-Through Rate</span>
        </div>

        <div className="p-3.5 rounded-xl border border-border bg-bg space-y-1">
          <span className="text-[10px] font-bold text-muted uppercase">Avg Google Position</span>
          <div className="text-xl font-extrabold text-amber-400">#{metrics.averagePosition}</div>
          <span className="text-[10px] text-subtle">Organic SERP Rank</span>
        </div>
      </div>
    </div>
  );
}
