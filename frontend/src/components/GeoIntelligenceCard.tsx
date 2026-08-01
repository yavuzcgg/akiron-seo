"use client";

import { apiClient, GeoAnalysisResult } from "@/lib/apiClient";
import { useEffect, useState } from "react";
import { getErrorMessage } from "@/lib/errors";

interface ComponentProps {
  websiteId: string;
  websiteName: string;
  tenantId?: string;
}

export default function GeoIntelligenceCard({ websiteId, websiteName, tenantId }: ComponentProps) {
  const [geoData, setGeoData] = useState<GeoAnalysisResult | null>(null);
  const [customPrompt, setCustomPrompt] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchGeoAnalysis = async (forceRefresh = false) => {
    setLoading(true);
    setError(null);
    try {
      const data = await apiClient.geo.getAnalysis(websiteId, forceRefresh);
      setGeoData(data);
    } catch {
      // Offline fallback
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchGeoAnalysis(false);
  }, [websiteId, tenantId]);

  const handleAnalyzeCustomPrompt = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!customPrompt.trim()) return;

    setLoading(true);
    setError(null);
    try {
      const data = await apiClient.geo.analyzePrompt(websiteId, customPrompt.trim());
      setGeoData(data);
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Failed to analyze prompt."));
    } finally {
      setLoading(false);
    }
  };

  const getCitationStatusBadge = (status?: string, isGold?: boolean) => {
    if (isGold || status === "NonExistentPage") {
      return {
        text: "🌟 404 Missing Page",
        style: "bg-amber-500/20 text-amber-300 border-amber-500/40 font-extrabold animate-pulse",
      };
    }
    if (status === "Valid") {
      return {
        text: "✓ Valid Link",
        style: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20 font-bold",
      };
    }
    if (status === "WrongDomain") {
      return {
        text: "🌐 External Domain",
        style: "bg-blue-500/10 text-blue-400 border-blue-500/20 font-semibold",
      };
    }
    return {
      text: "⚪ Unreachable",
      style: "bg-slate-500/10 text-slate-400 border-slate-500/20 font-semibold",
    };
  };

  return (
    <div className="p-6 rounded-2xl border border-purple-500/20 bg-purple-500/5 space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-purple-500/20 pb-4">
        <div>
          <span className="text-[10px] font-extrabold uppercase tracking-wider text-purple-400">
            GENERATIVE ENGINE OPTIMIZATION (GEO)
          </span>
          <h3 className="text-lg font-extrabold text-white flex items-center gap-2 mt-0.5">
            🤖 AI Share of Voice & Mention Rate Engine
          </h3>
          <p className="text-xs text-slate-400">
            Multi-sample iteration testing for ChatGPT, Perplexity, Claude & Gemini citations on {websiteName}.
          </p>
        </div>

        {geoData && (
          <div className="flex items-center gap-3">
            <div className="px-3.5 py-1.5 rounded-xl bg-purple-500/10 border border-purple-500/30 flex items-center space-x-2 text-right">
              <div>
                <span className="text-[10px] uppercase font-bold text-slate-400 block">Mention Rate</span>
                <span className="text-xl font-extrabold text-purple-300">
                  {geoData.overallMentionRatePercentage ?? geoData.shareOfVoiceScore}%
                </span>
              </div>
            </div>

            <button
              onClick={() => fetchGeoAnalysis(true)}
              disabled={loading}
              className="px-3 py-1.5 rounded-lg border border-purple-500/30 text-purple-300 hover:bg-purple-500/20 text-xs font-semibold transition disabled:opacity-50"
              title="Force re-sampling and URL verification"
            >
              🔄 Refresh
            </button>
          </div>
        )}
      </div>

      {error && (
        <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-xs font-semibold">
          {error}
        </div>
      )}

      {/* Live Prompt Intelligence Tester */}
      <form onSubmit={handleAnalyzeCustomPrompt} className="space-y-2">
        <label className="block text-xs font-semibold text-slate-300">Prompt Intelligence Tester</label>
        <div className="flex items-center space-x-2">
          <input
            type="text"
            placeholder={`e.g. Türkiye'deki en iyi ${websiteName} alternatifleri nelerdir?`}
            value={customPrompt}
            onChange={(e) => setCustomPrompt(e.target.value)}
            className="w-full px-4 py-2 rounded-lg border border-[var(--border-color)] bg-[var(--bg-primary)] text-xs focus:outline-none focus:ring-1 focus:ring-purple-500"
          />
          <button
            type="submit"
            disabled={loading}
            className="px-4 py-2 rounded-lg bg-purple-600 hover:bg-purple-700 text-white font-bold text-xs transition disabled:opacity-50 whitespace-nowrap shadow-md"
          >
            {loading ? "Analyzing..." : "⚡ Analyze Prompt"}
          </button>
        </div>
      </form>

      {/* AI Model Citations Breakdown */}
      {geoData && (
        <div className="space-y-4 pt-2">
          <div className="flex items-center justify-between text-xs">
            <h4 className="font-bold text-slate-300 uppercase tracking-wider">AI Search Engine Citation Status</h4>
            {geoData.isCached && (
              <span className="text-[10px] text-slate-500 font-mono">⚡ 24h Cached Result</span>
            )}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {geoData.engineCitations.map((item, idx) => {
              const statusBadge = getCitationStatusBadge(item.citationStatus, item.isGoldOpportunity);

              return (
                <div
                  key={idx}
                  className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] space-y-2.5"
                >
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span className="font-bold text-sm text-white">{item.engineName}</span>
                      {item.mentionRatePercentage !== undefined && (
                        <span className="text-[10px] font-mono text-purple-300 font-bold bg-purple-500/10 px-1.5 py-0.5 rounded border border-purple-500/20">
                          {item.mentionRatePercentage}% mention
                        </span>
                      )}
                    </div>

                    <span
                      className={`px-2.5 py-0.5 rounded-full text-[10px] border ${
                        item.isMentioned
                          ? "bg-emerald-500/10 text-emerald-400 border-emerald-500/20 font-bold"
                          : "bg-rose-500/10 text-rose-400 border-rose-500/20 font-semibold"
                      }`}
                    >
                      {item.isMentioned ? "✓ Cited" : "✕ Not Cited"}
                    </span>
                  </div>

                  <p className="text-xs text-slate-300 italic leading-relaxed">
                    &quot;{item.sampleAiResponseSnippet}&quot;
                  </p>

                  {item.citationUrl && (
                    <div className="flex items-center justify-between text-[11px] font-mono gap-2 border-t border-slate-800/80 pt-2">
                      <div className="truncate text-purple-400">
                        🔗 <a href={item.citationUrl} target="_blank" rel="noreferrer" className="hover:underline">{item.citationUrl}</a>
                      </div>

                      <span className={`px-2 py-0.5 rounded text-[10px] border shrink-0 ${statusBadge.style}`}>
                        {statusBadge.text}
                      </span>
                    </div>
                  )}
                </div>
              );
            })}
          </div>

          {/* GEO Recommendations */}
          {geoData.optimizationRecommendations.length > 0 && (
            <div className="p-4 rounded-xl border border-purple-500/30 bg-black/40 space-y-2">
              <span className="text-xs font-bold text-purple-300 block">💡 Actionable GEO Optimization Steps:</span>
              <ul className="list-disc pl-4 text-xs text-slate-300 space-y-1">
                {geoData.optimizationRecommendations.map((rec, i) => (
                  <li key={i}>{rec}</li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
