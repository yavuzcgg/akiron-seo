"use client";

import { apiClient, GeoAnalysisResult } from "@/lib/apiClient";
import { useEffect, useState } from "react";

interface ComponentProps {
  websiteId: string;
  websiteName: string;
  tenantId: string;
}

export default function GeoIntelligenceCard({ websiteId, websiteName, tenantId }: ComponentProps) {
  const [geoData, setGeoData] = useState<GeoAnalysisResult | null>(null);
  const [customPrompt, setCustomPrompt] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchGeoAnalysis = async () => {
    try {
      const data = await apiClient.geo.getAnalysis(websiteId, tenantId);
      setGeoData(data);
    } catch {
      // Offline fallback
    }
  };

  useEffect(() => {
    fetchGeoAnalysis();
  }, [websiteId, tenantId]);

  const handleAnalyzeCustomPrompt = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!customPrompt.trim()) return;

    setLoading(true);
    setError(null);
    try {
      const data = await apiClient.geo.analyzePrompt(websiteId, tenantId, customPrompt.trim());
      setGeoData(data);
    } catch (err: any) {
      setError(err.message || "Failed to analyze prompt.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="p-6 rounded-2xl border border-purple-500/20 bg-purple-500/5 space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-purple-500/20 pb-4">
        <div>
          <span className="text-[10px] font-extrabold uppercase tracking-wider text-purple-400">
            GENERATIVE ENGINE OPTIMIZATION (GEO)
          </span>
          <h3 className="text-lg font-extrabold text-white flex items-center gap-2 mt-0.5">
            🤖 AI Share of Voice & Citation Intelligence
          </h3>
          <p className="text-xs text-slate-400">
            Measure how ChatGPT, Perplexity, Claude & Gemini cite {websiteName} in AI search answers.
          </p>
        </div>

        {geoData && (
          <div className="px-4 py-2 rounded-xl bg-purple-500/10 border border-purple-500/30 flex items-center space-x-3">
            <span className="text-xs font-bold text-slate-300">AI Share of Voice:</span>
            <span className="text-2xl font-extrabold text-purple-400">{geoData.shareOfVoiceScore}%</span>
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
            {loading ? "Analyzing AI Citations..." : "⚡ Analyze Prompt"}
          </button>
        </div>
      </form>

      {/* AI Model Citations Breakdown */}
      {geoData && (
        <div className="space-y-4 pt-2">
          <h4 className="text-xs font-bold text-slate-300 uppercase tracking-wider">AI Search Engine Citation Status</h4>
          
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {geoData.engineCitations.map((item, idx) => (
              <div
                key={idx}
                className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] space-y-2"
              >
                <div className="flex items-center justify-between">
                  <span className="font-bold text-sm text-white">{item.engineName}</span>
                  <span
                    className={`px-2.5 py-0.5 rounded-full text-[10px] font-bold ${
                      item.isMentioned
                        ? "bg-emerald-500/10 text-emerald-400 border border-emerald-500/20"
                        : "bg-rose-500/10 text-rose-400 border border-rose-500/20"
                    }`}
                  >
                    {item.isMentioned ? "✓ Cited & Recommended" : "✕ Not Cited"}
                  </span>
                </div>

                <p className="text-xs text-slate-300 italic leading-relaxed">
                  "{item.sampleAiResponseSnippet}"
                </p>

                {item.citationUrl && (
                  <div className="text-[11px] font-mono text-purple-400 truncate">
                    🔗 <a href={item.citationUrl} target="_blank" rel="noreferrer" className="hover:underline">{item.citationUrl}</a>
                  </div>
                )}
              </div>
            ))}
          </div>

          {/* GEO Recommendations */}
          {geoData.optimizationRecommendations.length > 0 && (
            <div className="p-4 rounded-xl border border-purple-500/30 bg-black/40 space-y-2">
              <span className="text-xs font-bold text-purple-300 block">💡 GEO Citation Optimization Steps:</span>
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
