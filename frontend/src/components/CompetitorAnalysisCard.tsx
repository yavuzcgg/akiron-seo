"use client";

import DataSourceBadge from "@/components/DataSourceBadge";
import { apiClient, CompetitorGapResult, DataSource } from "@/lib/apiClient";
import { Target, Zap } from "lucide-react";
import { useEffect, useState } from "react";
import { getErrorMessage } from "@/lib/errors";

interface ComponentProps {
  websiteId: string;
  websiteName: string;
  tenantId?: string;
}

export default function CompetitorAnalysisCard({ websiteId, websiteName, tenantId }: ComponentProps) {
  const [competitorData, setCompetitorData] = useState<CompetitorGapResult | null>(null);
  const [competitorInput, setCompetitorInput] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const gapDataSource: DataSource | undefined = competitorData?.dataSource;

  const fetchCompetitors = async () => {
    try {
      const data = await apiClient.competitors.list(websiteId);
      if (data && data.length > 0) {
        setCompetitorData(data[0]);
      }
    } catch {
      // Offline fallback
    }
  };

  useEffect(() => {
    fetchCompetitors();
  }, [websiteId, tenantId]);

  const handleAnalyzeCompetitor = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!competitorInput.trim()) return;

    setLoading(true);
    setError(null);
    try {
      const res = await apiClient.competitors.analyze(websiteId, competitorInput.trim());
      setCompetitorData(res);
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Failed to analyze competitor."));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-5 rounded-xl border border-border bg-bg space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border pb-3">
        <div>
          <h4 className="flex items-center gap-2 text-sm font-bold text-foreground">
            <Target size={16} className="text-primary" aria-hidden /> Competitor Intelligence &amp; Gap
            <DataSourceBadge source={gapDataSource} />
          </h4>
          <p className="text-[11px] text-muted">
            {gapDataSource && gapDataSource !== "Live"
              ? "Placeholder gap analysis — no SERP provider is connected yet, so these keywords are a fixed sample, not competitor research."
              : "Discover missing keywords where competitors rank on Google Page 1."}
          </p>
        </div>

        <form onSubmit={handleAnalyzeCompetitor} className="flex items-center space-x-2">
          <input
            type="text"
            placeholder="Competitor (e.g. yamaha-motor.com.tr)"
            value={competitorInput}
            onChange={(e) => setCompetitorInput(e.target.value)}
            className="px-3 py-1.5 rounded-lg border border-border bg-surface text-xs font-medium focus:outline-none focus:ring-1 focus:ring-amber-500"
          />
          <button
            type="submit"
            disabled={loading}
            className="flex cursor-pointer items-center gap-1.5 rounded-lg bg-primary px-3 py-1.5 text-xs font-bold text-on-primary transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Zap size={13} aria-hidden /> {loading ? "Analyzing…" : "Gap Analysis"}
          </button>
        </form>
      </div>

      {error && (
        <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-xs font-semibold">
          {error}
        </div>
      )}

      {competitorData && (
        <div className="space-y-3">
          <div className="flex items-center justify-between p-3 rounded-lg bg-surface border border-border">
            <span className="text-xs font-bold text-foreground">
              {competitorData.yourDomain} vs <span className="text-amber-400">{competitorData.competitorDomain}</span>
            </span>
            <span className="px-2.5 py-0.5 rounded-full bg-amber-500/10 text-amber-400 text-xs font-extrabold border border-amber-500/20">
              {competitorData.overlapScore}% Competitor Overlap
            </span>
          </div>

          <h5 className="text-xs font-bold text-foreground uppercase tracking-wider">Top Keyword Gap Opportunities</h5>

          <div className="space-y-2">
            {competitorData.missingKeywordOpportunities.map((item, i) => (
              <div
                key={i}
                className="p-3 rounded-lg border border-border bg-surface flex flex-wrap items-center justify-between gap-2 text-xs"
              >
                <div>
                  <span className="font-bold text-foreground">{item.keyword}</span>
                  <div className="text-[10px] text-muted mt-0.5">
                    Est. Search Volume: <strong className="text-foreground">{item.estimatedSearchVolume.toLocaleString()} / mo</strong>
                  </div>
                </div>

                <div className="flex items-center space-x-3 text-[11px]">
                  <span className="px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-400 font-extrabold">
                    Competitor: #{item.competitorRank}
                  </span>
                  <span className="px-2 py-0.5 rounded bg-rose-500/10 text-rose-400 font-extrabold">
                    You: {item.yourRank > 0 ? `#${item.yourRank}` : "Not Ranking"}
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
