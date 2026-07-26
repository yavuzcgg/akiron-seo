"use client";

import { apiClient, GscMetrics } from "@/lib/apiClient";
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
          <span className="text-[10px] font-extrabold uppercase tracking-wider text-blue-400">
            GOOGLE SEARCH CONSOLE (GSC) ANALYTICS
          </span>
          <h3 className="text-lg font-extrabold text-white flex items-center gap-2 mt-0.5">
            📈 Organic Google Search Performance
          </h3>
          <p className="text-xs text-slate-400">
            Real-time organic search clicks, impressions, CTR %, and avg position for {websiteName}.
          </p>
        </div>

        <button
          onClick={fetchGscAnalytics}
          className="px-3 py-1.5 rounded-lg border border-blue-500/30 text-blue-300 hover:bg-blue-500/20 text-xs font-semibold transition"
        >
          🔄 Refresh Metrics
        </button>
      </div>

      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <div className="p-3.5 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] space-y-1">
          <span className="text-[10px] font-bold text-slate-400 uppercase">Organic Clicks</span>
          <div className="text-xl font-extrabold text-emerald-400">{metrics.totalClicks.toLocaleString()}</div>
          <span className="text-[10px] text-slate-500">Google SERP Clicks</span>
        </div>

        <div className="p-3.5 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] space-y-1">
          <span className="text-[10px] font-bold text-slate-400 uppercase">Total Impressions</span>
          <div className="text-xl font-extrabold text-blue-400">{metrics.totalImpressions.toLocaleString()}</div>
          <span className="text-[10px] text-slate-500">Search Views</span>
        </div>

        <div className="p-3.5 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] space-y-1">
          <span className="text-[10px] font-bold text-slate-400 uppercase">Average CTR</span>
          <div className="text-xl font-extrabold text-purple-400">{metrics.averageCtrPercentage}%</div>
          <span className="text-[10px] text-slate-500">Click-Through Rate</span>
        </div>

        <div className="p-3.5 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] space-y-1">
          <span className="text-[10px] font-bold text-slate-400 uppercase">Avg Google Position</span>
          <div className="text-xl font-extrabold text-amber-400">#{metrics.averagePosition}</div>
          <span className="text-[10px] text-slate-500">Organic SERP Rank</span>
        </div>
      </div>
    </div>
  );
}
