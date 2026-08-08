"use client";

import DataSourceBadge from "@/components/DataSourceBadge";
import { apiClient, DataSource, TrackedKeyword } from "@/lib/apiClient";
import { LineChart, Zap } from "lucide-react";
import { useEffect, useState } from "react";
import { getErrorMessage } from "@/lib/errors";

interface ComponentProps {
  websiteId: string;
  tenantId?: string;
}

export default function KeywordTrackerCard({ websiteId, tenantId }: ComponentProps) {
  const [keywords, setKeywords] = useState<TrackedKeyword[]>([]);
  const [newKeywordText, setNewKeywordText] = useState("");
  const [loading, setLoading] = useState(false);
  const [checkingId, setCheckingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Every keyword carries the same provenance, so the first one describes the card.
  const rankDataSource: DataSource | undefined = keywords[0]?.rankDataSource;

  const fetchKeywords = async () => {
    try {
      const data = await apiClient.keywords.list(websiteId);
      setKeywords(data);
    } catch {
      // Offline fallback
    }
  };

  useEffect(() => {
    fetchKeywords();
  }, [websiteId, tenantId]);

  const handleAddKeyword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newKeywordText.trim()) return;

    setLoading(true);
    setError(null);
    try {
      const res = await apiClient.keywords.add({
        websiteId,
        keywordText: newKeywordText.trim(),
        language: "tr",
      });

      if (res.success) {
        setNewKeywordText("");
        fetchKeywords();
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Failed to add keyword."));
    } finally {
      setLoading(false);
    }
  };

  const handleCheckRank = async (keywordId: string) => {
    setCheckingId(keywordId);
    setError(null);
    try {
      await apiClient.keywords.checkRank(keywordId);
      fetchKeywords();
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Failed to check rank position."));
    } finally {
      setCheckingId(null);
    }
  };

  return (
    <div className="p-5 rounded-xl border border-border bg-bg space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border pb-3">
        <div>
          <h4 className="flex items-center gap-2 text-sm font-bold text-foreground">
            <LineChart size={16} className="text-primary" aria-hidden /> Keyword Rank Tracker
            <DataSourceBadge source={rankDataSource} />
          </h4>
          <p className="text-[11px] text-muted">
            {rankDataSource && rankDataSource !== "Live"
              ? "Placeholder positions — no SERP provider is connected yet, so these are not real rankings."
              : "Search engine positioning and rank deltas."}
          </p>
        </div>

        <form onSubmit={handleAddKeyword} className="flex items-center space-x-2">
          <input
            type="text"
            placeholder="Keyword (e.g. touring helmets)"
            value={newKeywordText}
            onChange={(e) => setNewKeywordText(e.target.value)}
            className="px-3 py-1.5 rounded-lg border border-border bg-surface text-xs font-medium focus:outline-none focus:ring-1 focus:ring-blue-500"
          />
          <button
            type="submit"
            disabled={loading}
            className="px-3 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-on-primary font-bold text-xs transition disabled:opacity-50"
          >
            {loading ? "Adding..." : "+ Track"}
          </button>
        </form>
      </div>

      {error && (
        <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-xs font-semibold">
          {error}
        </div>
      )}

      {keywords.length === 0 ? (
        <p className="text-xs text-muted py-2">
          No keywords tracked yet. Type a target keyword above and click &quot;+ Track&quot; to begin rank tracking!
        </p>
      ) : (
        <div className="space-y-2">
          {keywords.map((kw) => (
            <div
              key={kw.id}
              className="p-3 rounded-lg border border-border bg-surface flex flex-wrap items-center justify-between gap-3 text-xs"
            >
              <div>
                <div className="flex items-center space-x-2">
                  <span className="font-bold text-foreground">{kw.keywordText}</span>
                  <span className="px-1.5 py-0.5 rounded bg-elevated text-[10px] text-muted font-mono">
                    {kw.targetLanguage.toUpperCase()} / {kw.targetCountry}
                  </span>
                </div>
                {kw.targetUrl && (
                  <p className="text-[10px] text-subtle mt-0.5 truncate max-w-xs">{kw.targetUrl}</p>
                )}
              </div>

              <div className="flex items-center space-x-3">
                {kw.currentPosition !== null && kw.currentPosition !== undefined ? (
                  <div className="flex items-center space-x-2">
                    <span className="font-extrabold text-sm text-foreground">#{kw.currentPosition}</span>
                    {kw.positionChange > 0 ? (
                      <span className="px-2 py-0.5 rounded-full bg-emerald-500/10 text-emerald-400 text-[10px] font-bold border border-emerald-500/20">
                        ↑ {kw.positionChange}
                      </span>
                    ) : kw.positionChange < 0 ? (
                      <span className="px-2 py-0.5 rounded-full bg-rose-500/10 text-rose-400 text-[10px] font-bold border border-rose-500/20">
                        ↓ {Math.abs(kw.positionChange)}
                      </span>
                    ) : (
                      <span className="px-2 py-0.5 rounded-full bg-slate-500/10 text-muted text-[10px] font-bold border border-slate-500/20">
                        - Unchanged
                      </span>
                    )}
                  </div>
                ) : (
                  <span className="text-[11px] text-subtle">Unchecked</span>
                )}

                <button
                  onClick={() => handleCheckRank(kw.id)}
                  disabled={checkingId === kw.id}
                  className="flex cursor-pointer items-center gap-1 rounded bg-elevated px-2.5 py-1 text-[11px] font-semibold text-foreground transition-colors hover:opacity-80 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  <Zap size={11} aria-hidden /> {checkingId === kw.id ? "Checking…" : "Check Rank"}
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
