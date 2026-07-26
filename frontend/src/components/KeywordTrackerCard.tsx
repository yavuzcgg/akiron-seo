"use client";

import { apiClient, TrackedKeyword } from "@/lib/apiClient";
import { useEffect, useState } from "react";

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
    } catch (err: any) {
      setError(err.message || "Failed to add keyword.");
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
    } catch (err: any) {
      setError(err.message || "Failed to check rank position.");
    } finally {
      setCheckingId(null);
    }
  };

  return (
    <div className="p-5 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-[var(--border-color)] pb-3">
        <div>
          <h4 className="text-sm font-bold text-white flex items-center gap-2">
            📈 Keyword Rank Tracker & Position Engine
          </h4>
          <p className="text-[11px] text-slate-400">Track real-time search engine positioning and rank deltas.</p>
        </div>

        <form onSubmit={handleAddKeyword} className="flex items-center space-x-2">
          <input
            type="text"
            placeholder="Keyword (e.g. motosiklet kaskı)"
            value={newKeywordText}
            onChange={(e) => setNewKeywordText(e.target.value)}
            className="px-3 py-1.5 rounded-lg border border-[var(--border-color)] bg-[var(--card-bg)] text-xs font-medium focus:outline-none focus:ring-1 focus:ring-blue-500"
          />
          <button
            type="submit"
            disabled={loading}
            className="px-3 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs transition disabled:opacity-50"
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
        <p className="text-xs text-slate-400 py-2">
          No keywords tracked yet. Type a target keyword above and click "+ Track" to begin rank tracking!
        </p>
      ) : (
        <div className="space-y-2">
          {keywords.map((kw) => (
            <div
              key={kw.id}
              className="p-3 rounded-lg border border-[var(--border-color)] bg-[var(--card-bg)] flex flex-wrap items-center justify-between gap-3 text-xs"
            >
              <div>
                <div className="flex items-center space-x-2">
                  <span className="font-bold text-slate-200">{kw.keywordText}</span>
                  <span className="px-1.5 py-0.5 rounded bg-slate-800 text-[10px] text-slate-400 font-mono">
                    {kw.targetLanguage.toUpperCase()} / {kw.targetCountry}
                  </span>
                </div>
                {kw.targetUrl && (
                  <p className="text-[10px] text-slate-500 mt-0.5 truncate max-w-xs">{kw.targetUrl}</p>
                )}
              </div>

              <div className="flex items-center space-x-3">
                {kw.currentPosition !== null && kw.currentPosition !== undefined ? (
                  <div className="flex items-center space-x-2">
                    <span className="font-extrabold text-sm text-white">#{kw.currentPosition}</span>
                    {kw.positionChange > 0 ? (
                      <span className="px-2 py-0.5 rounded-full bg-emerald-500/10 text-emerald-400 text-[10px] font-bold border border-emerald-500/20">
                        ↑ {kw.positionChange}
                      </span>
                    ) : kw.positionChange < 0 ? (
                      <span className="px-2 py-0.5 rounded-full bg-rose-500/10 text-rose-400 text-[10px] font-bold border border-rose-500/20">
                        ↓ {Math.abs(kw.positionChange)}
                      </span>
                    ) : (
                      <span className="px-2 py-0.5 rounded-full bg-slate-500/10 text-slate-400 text-[10px] font-bold border border-slate-500/20">
                        - Unchanged
                      </span>
                    )}
                  </div>
                ) : (
                  <span className="text-[11px] text-slate-500">Unchecked</span>
                )}

                <button
                  onClick={() => handleCheckRank(kw.id)}
                  disabled={checkingId === kw.id}
                  className="px-2.5 py-1 rounded bg-slate-800 hover:bg-slate-700 text-slate-300 font-semibold text-[11px] transition disabled:opacity-50"
                >
                  {checkingId === kw.id ? "Checking..." : "⚡ Check Rank"}
                </button>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
