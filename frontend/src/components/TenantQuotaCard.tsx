"use client";

import { useApp } from "@/components/providers";
import { apiClient } from "@/lib/apiClient";
import { queryKeys } from "@/lib/queryKeys";
import { useQuery } from "@tanstack/react-query";
import { Gauge } from "lucide-react";

export default function TenantQuotaCard() {
  const { t } = useApp();
  const quotaQuery = useQuery({ queryKey: queryKeys.quota, queryFn: apiClient.tenant.getQuota });

  if (quotaQuery.isPending) {
    return <div className="rounded-2xl border border-border bg-surface p-6 text-sm text-muted" role="status">{t("loadingQuota")}</div>;
  }
  if (quotaQuery.isError) {
    return <div className="rounded-2xl border border-danger/20 bg-danger/10 p-6 text-sm text-danger" role="alert">{t("quotaLoadFailed")}</div>;
  }

  const quota = quotaQuery.data;
  const usedPercent = quota.monthlyTokenLimit <= 0 ? 0 : Math.min(100, Math.round((quota.usedTokens / quota.monthlyTokenLimit) * 100));

  return (
    <section className="space-y-4 rounded-2xl border border-border bg-surface p-6" aria-labelledby="quota-heading">
      <div className="flex items-center justify-between border-b border-border pb-3">
        <div>
          <span className="text-[10px] font-extrabold uppercase tracking-wider text-primary">{t("subscription")}</span>
          <h2 id="quota-heading" className="mt-0.5 flex items-center gap-1.5 text-base font-bold text-foreground">
            <Gauge size={16} className="text-primary" aria-hidden /> {t("monthlyTokenQuota")}
          </h2>
        </div>
        <span className="rounded-full border border-primary/20 bg-primary/10 px-3 py-1 text-xs font-extrabold text-primary">{quota.planName}</span>
      </div>

      <div className="space-y-2">
        <div className="flex justify-between text-xs font-semibold">
          <span className="text-foreground">{quota.usedTokens.toLocaleString()} {t("used")}</span>
          <span className="text-muted">{quota.remainingTokens.toLocaleString()} {t("remaining")}</span>
        </div>
        <div className="h-2 w-full overflow-hidden rounded-full bg-elevated" role="progressbar" aria-label={t("monthlyTokenQuota")} aria-valuemin={0} aria-valuemax={quota.monthlyTokenLimit} aria-valuenow={quota.usedTokens}>
          <div className="h-full rounded-full bg-primary transition-[width] motion-reduce:transition-none" style={{ width: `${usedPercent}%` }} />
        </div>
        <p className="text-right font-mono text-[10px] text-subtle">{quota.usedTokens.toLocaleString()} / {quota.monthlyTokenLimit.toLocaleString()}</p>
      </div>

      <dl className="grid grid-cols-2 gap-3 text-xs">
        <div><dt className="text-muted">{t("periodStarts")}</dt><dd className="font-semibold text-foreground">{new Date(quota.periodStart).toLocaleDateString()}</dd></div>
        <div><dt className="text-muted">{t("periodEnds")}</dt><dd className="font-semibold text-foreground">{new Date(quota.periodEnd).toLocaleDateString()}</dd></div>
      </dl>

      {!quota.enforcementEnabled && <p className="rounded-lg border border-warning/20 bg-warning/10 p-3 text-xs text-warning" role="note">{t("notEnforced")}</p>}
    </section>
  );
}
