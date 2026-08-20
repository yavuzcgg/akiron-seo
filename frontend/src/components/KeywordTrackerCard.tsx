"use client";

import DataSourceBadge from "@/components/DataSourceBadge";
import { useApp } from "@/components/providers";
import { apiClient, DataSource } from "@/lib/apiClient";
import { getErrorMessage } from "@/lib/errors";
import { queryKeys } from "@/lib/queryKeys";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { LineChart, Zap } from "lucide-react";
import { useState } from "react";

export default function KeywordTrackerCard({ websiteId }: { websiteId: string }) {
  const { t } = useApp();
  const queryClient = useQueryClient();
  const [keywordText, setKeywordText] = useState("");
  const [language, setLanguage] = useState("en");
  const [country, setCountry] = useState("US");
  const [error, setError] = useState<string | null>(null);

  const keywordsQuery = useQuery({
    queryKey: queryKeys.keywords(websiteId),
    queryFn: () => apiClient.keywords.list(websiteId),
  });
  const keywords = keywordsQuery.data ?? [];
  const rankDataSource: DataSource | undefined = keywords[0]?.rankDataSource;

  const addMutation = useMutation({
    mutationFn: () => apiClient.keywords.add({ websiteId, keywordText: keywordText.trim(), language, targetCountry: country }),
    onSuccess: () => {
      setKeywordText("");
      queryClient.invalidateQueries({ queryKey: queryKeys.keywords(websiteId) });
    },
    onError: (err) => setError(getErrorMessage(err, t("addKeywordFailed"))),
  });
  const checkMutation = useMutation({
    mutationFn: apiClient.keywords.checkRank,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.keywords(websiteId) }),
    onError: (err) => setError(getErrorMessage(err, t("checkRankFailed"))),
  });

  const handleAdd = (event: React.FormEvent) => {
    event.preventDefault();
    if (!keywordText.trim()) return;
    setError(null);
    addMutation.mutate();
  };

  return (
    <section className="space-y-4 rounded-xl border border-border bg-bg p-5" aria-labelledby={`keywords-${websiteId}`}>
      <div className="flex flex-wrap items-start justify-between gap-3 border-b border-border pb-3">
        <div>
          <h3 id={`keywords-${websiteId}`} className="flex items-center gap-2 text-sm font-bold text-foreground"><LineChart size={16} className="text-primary" aria-hidden /> {t("keywordTracker")} <DataSourceBadge source={rankDataSource} /></h3>
          <p className="text-[11px] text-muted">{rankDataSource && rankDataSource !== "Live" ? t("simulatedRanks") : t("rankDescription")}</p>
        </div>

        <form onSubmit={handleAdd} className="grid w-full grid-cols-2 gap-2 sm:w-auto sm:grid-cols-[minmax(180px,1fr)_72px_72px_auto]">
          <label htmlFor={`keyword-${websiteId}`} className="sr-only">{t("keyword")}</label>
          <input id={`keyword-${websiteId}`} value={keywordText} onChange={(event) => setKeywordText(event.target.value)} required maxLength={200} placeholder={t("targetKeyword")} className="min-h-11 rounded-lg border border-border bg-surface px-3 text-xs text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
          <label htmlFor={`language-${websiteId}`} className="sr-only">{t("language")}</label>
          <select id={`language-${websiteId}`} value={language} onChange={(event) => setLanguage(event.target.value)} className="min-h-11 rounded-lg border border-border bg-surface px-2 text-xs text-foreground focus:outline-none focus:ring-2 focus:ring-ring"><option value="en">EN</option><option value="tr">TR</option></select>
          <label htmlFor={`country-${websiteId}`} className="sr-only">{t("country")}</label>
          <input id={`country-${websiteId}`} value={country} onChange={(event) => setCountry(event.target.value.toUpperCase())} required maxLength={2} pattern="[A-Za-z]{2}" aria-describedby={`country-help-${websiteId}`} className="min-h-11 rounded-lg border border-border bg-surface px-2 text-xs uppercase text-foreground focus:outline-none focus:ring-2 focus:ring-ring" />
          <span id={`country-help-${websiteId}`} className="sr-only">{t("countryCodeHelp")}</span>
          <button type="submit" disabled={addMutation.isPending} className="min-h-11 rounded-lg bg-primary px-3 text-xs font-bold text-on-primary transition-colors hover:bg-primary-hover disabled:opacity-50">{addMutation.isPending ? t("adding") : t("track")}</button>
        </form>
      </div>

      {error && <p className="rounded-lg border border-danger/20 bg-danger/10 p-3 text-xs font-semibold text-danger" role="alert">{error}</p>}
      {keywordsQuery.isPending ? (
        <p className="py-2 text-xs text-muted" role="status">{t("loadingKeywords")}</p>
      ) : keywordsQuery.isError ? (
        <p className="rounded-lg border border-danger/20 bg-danger/10 p-3 text-xs text-danger" role="alert">{getErrorMessage(keywordsQuery.error, t("keywordLoadFailed"))}</p>
      ) : keywords.length === 0 ? (
        <p className="py-2 text-xs text-muted">{t("noKeywords")}</p>
      ) : (
        <div className="space-y-2">
          {keywords.map((keyword) => (
            <div key={keyword.id} className="flex flex-wrap items-center justify-between gap-3 rounded-lg border border-border bg-surface p-3 text-xs">
              <div><div className="flex items-center gap-2"><span className="font-bold text-foreground">{keyword.keywordText}</span><span className="rounded bg-elevated px-1.5 py-0.5 font-mono text-[10px] text-muted">{keyword.targetLanguage.toUpperCase()} / {keyword.targetCountry}</span></div>{keyword.targetUrl && <p className="mt-0.5 max-w-xs truncate text-[10px] text-subtle">{keyword.targetUrl}</p>}</div>
              <div className="flex items-center gap-3">
                {keyword.currentPosition == null ? <span className="text-[11px] text-subtle">{t("unchecked")}</span> : <span className="font-extrabold text-foreground">#{keyword.currentPosition} <small className={keyword.positionChange >= 0 ? "text-success" : "text-danger"}>{keyword.positionChange === 0 ? "—" : keyword.positionChange > 0 ? `↑ ${keyword.positionChange}` : `↓ ${Math.abs(keyword.positionChange)}`}</small></span>}
                <button type="button" onClick={() => { setError(null); checkMutation.mutate(keyword.id); }} disabled={checkMutation.isPending && checkMutation.variables === keyword.id} className="flex min-h-11 cursor-pointer items-center gap-1 rounded bg-elevated px-3 text-[11px] font-semibold text-foreground disabled:opacity-50"><Zap size={11} aria-hidden /> {checkMutation.isPending && checkMutation.variables === keyword.id ? t("checking") : t("checkRank")}</button>
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}
