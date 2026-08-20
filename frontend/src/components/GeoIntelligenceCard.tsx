"use client";

import DataSourceBadge from "@/components/DataSourceBadge";
import { useApp } from "@/components/providers";
import { apiClient, DataSource, GeoAnalysisResult } from "@/lib/apiClient";
import { getErrorMessage } from "@/lib/errors";
import { queryKeys } from "@/lib/queryKeys";
import { useMutation, useQuery } from "@tanstack/react-query";
import { Clock, Lightbulb, Link as LinkIcon, RefreshCw, Sparkles, Zap } from "lucide-react";
import { useState } from "react";

/** Absent dataSource means the API predates the field; treat it as live. */
function isLive(source?: DataSource): boolean {
  return source === undefined || source === "Live";
}

interface ComponentProps {
  websiteId: string;
  websiteName: string;
}

export default function GeoIntelligenceCard({ websiteId, websiteName }: ComponentProps) {
  const { t } = useApp();
  const [customPrompt, setCustomPrompt] = useState("");
  const [error, setError] = useState<string | null>(null);
  const geoQuery = useQuery({ queryKey: queryKeys.geo(websiteId), queryFn: () => apiClient.geo.getAnalysis(websiteId) });
  const refreshMutation = useMutation({
    mutationFn: () => apiClient.geo.getAnalysis(websiteId, true),
    onError: (err) => setError(getErrorMessage(err, t("geoRefreshFailed"))),
  });
  const promptMutation = useMutation({
    mutationFn: (promptText: string) => apiClient.geo.analyzePrompt(websiteId, promptText),
    onError: (err) => setError(getErrorMessage(err, t("promptAnalysisFailed"))),
  });
  const geoData: GeoAnalysisResult | undefined = promptMutation.data ?? refreshMutation.data ?? geoQuery.data;
  const loading = geoQuery.isPending || refreshMutation.isPending || promptMutation.isPending;

  const handleAnalyzeCustomPrompt = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!customPrompt.trim()) return;

    setError(null);
    promptMutation.mutate(customPrompt.trim());
  };

  const getCitationStatusBadge = (status?: string, isGold?: boolean) => {
    if (isGold || status === "NonExistentPage") {
      return {
        text: t("missingCitation"),
        style: "bg-amber-500/20 text-amber-300 border-amber-500/40 font-extrabold",
      };
    }
    if (status === "Valid") {
      return {
        text: t("validLink"),
        style: "bg-emerald-500/10 text-emerald-400 border-emerald-500/20 font-bold",
      };
    }
    if (status === "WrongDomain") {
      return {
        text: t("externalDomain"),
        style: "bg-blue-500/10 text-blue-400 border-blue-500/20 font-semibold",
      };
    }
    return {
      text: t("unreachable"),
      style: "bg-slate-500/10 text-muted border-slate-500/20 font-semibold",
    };
  };

  return (
    <div className="p-6 rounded-2xl border border-purple-500/20 bg-purple-500/5 space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-purple-500/20 pb-4">
        <div>
          <span className="text-[10px] font-extrabold uppercase tracking-wider text-purple-400">
            {t("geoOptimization")}
          </span>
          <h3 className="mt-0.5 flex items-center gap-2 text-lg font-extrabold text-foreground">
            <Sparkles size={18} className="text-primary" aria-hidden /> {t("shareOfVoice")}
          </h3>
          <p className="text-xs text-muted">
            {geoData && (geoData.liveEngineCount ?? 0) === 0
              ? t("noGeoProvider")
              : `${t("geoSampling")} (${websiteName})`}
          </p>
        </div>

        {geoData && (
          <div className="flex items-center gap-3">
            <div className="px-3.5 py-1.5 rounded-xl bg-purple-500/10 border border-purple-500/30 flex items-center space-x-2 text-right">
              <div>
                <span className="text-[10px] uppercase font-bold text-muted block">{t("mentionRate")}</span>
                <span className="text-xl font-extrabold text-purple-300">
                  {geoData.overallMentionRatePercentage ?? geoData.shareOfVoiceScore}%
                </span>
              </div>
            </div>

            <button
              onClick={() => { setError(null); refreshMutation.mutate(); }}
              disabled={loading}
              className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-muted transition-colors hover:text-foreground disabled:cursor-not-allowed disabled:opacity-50"
              title="Force re-sampling and URL verification"
            >
              <RefreshCw size={13} aria-hidden /> {t("refresh")}
            </button>
          </div>
        )}
      </div>

      {(error || geoQuery.isError) && (
        <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-xs font-semibold">
          {error || getErrorMessage(geoQuery.error, t("geoLoadFailed"))}
        </div>
      )}

      {/* Live Prompt Intelligence Tester */}
      <form onSubmit={handleAnalyzeCustomPrompt} className="space-y-2">
        <label htmlFor={`geo-prompt-${websiteId}`} className="block text-xs font-semibold text-foreground">{t("promptTester")}</label>
        <div className="flex items-center space-x-2">
          <input
            id={`geo-prompt-${websiteId}`}
            type="text"
            placeholder={`e.g. What are the best ${websiteName} alternatives?`}
            value={customPrompt}
            onChange={(e) => setCustomPrompt(e.target.value)}
            required
            maxLength={2000}
            className="w-full rounded-lg border border-border bg-bg px-4 py-2 text-xs text-foreground placeholder:text-subtle focus:outline-none focus:ring-2 focus:ring-ring"
          />
          <button
            type="submit"
            disabled={loading}
            className="flex cursor-pointer items-center gap-1.5 whitespace-nowrap rounded-lg bg-primary px-4 py-2 text-xs font-bold text-on-primary transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
          >
            <Zap size={13} aria-hidden /> {loading ? t("analyzing") : t("analyzePrompt")}
          </button>
        </div>
      </form>

      {/* AI Model Citations Breakdown */}
      {geoData && (
        <div className="space-y-4 pt-2">
          <div className="flex items-center justify-between text-xs">
            <h4 className="font-bold text-foreground uppercase tracking-wider">{t("citationStatus")}</h4>
            {geoData.isCached && (
              <span className="flex items-center gap-1 font-mono text-[10px] text-subtle"><Clock size={11} aria-hidden /> {t("cached24h")}</span>
            )}
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {geoData.engineCitations.map((item, idx) => {
              const statusBadge = getCitationStatusBadge(item.citationStatus, item.isGoldOpportunity);

              return (
                <div
                  key={idx}
                  className="p-4 rounded-xl border border-border bg-bg space-y-2.5"
                >
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <span className="font-bold text-sm text-foreground">{item.engineName}</span>
                      <DataSourceBadge source={item.dataSource} />
                      {/* A mention rate is only meaningful for an engine that answered. */}
                      {item.mentionRatePercentage !== undefined && isLive(item.dataSource) && (
                        <span className="text-[10px] font-mono text-purple-300 font-bold bg-purple-500/10 px-1.5 py-0.5 rounded border border-purple-500/20">
                          {item.mentionRatePercentage}% mention
                        </span>
                      )}
                    </div>

                    <span
                      className={`px-2.5 py-0.5 rounded-full text-[10px] border ${
                        !isLive(item.dataSource)
                          ? "bg-slate-500/10 text-muted border-slate-500/20 font-semibold"
                          : item.isMentioned
                            ? "bg-emerald-500/10 text-emerald-400 border-emerald-500/20 font-bold"
                            : "bg-rose-500/10 text-rose-400 border-rose-500/20 font-semibold"
                      }`}
                    >
                      {!isLive(item.dataSource)
                        ? `— ${t("notMeasured")}`
                        : item.isMentioned
                          ? `✓ ${t("cited")}`
                          : `✕ ${t("notCited")}`}
                    </span>
                  </div>

                  <p className="text-xs text-foreground italic leading-relaxed">
                    &quot;{item.sampleAiResponseSnippet}&quot;
                  </p>

                  {item.citationUrl && (
                    <div className="flex items-center justify-between text-[11px] font-mono gap-2 border-t border-border/80 pt-2">
                      <div className="flex items-center gap-1 truncate text-primary">
                        <LinkIcon size={11} className="shrink-0" aria-hidden />
                        <a href={item.citationUrl} target="_blank" rel="noreferrer" className="truncate hover:underline">{item.citationUrl}</a>
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
            <div className="space-y-2 rounded-xl border border-border bg-elevated p-4">
              <span className="flex items-center gap-1.5 text-xs font-bold text-foreground"><Lightbulb size={14} className="text-accent" aria-hidden /> {t("geoSteps")}</span>
              <ul className="list-disc space-y-1 pl-4 text-xs text-foreground">
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
