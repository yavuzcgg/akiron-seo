"use client";

import { apiClient, TenantQuotaStatus } from "@/lib/apiClient";
import { Gauge } from "lucide-react";
import { useEffect, useState } from "react";

interface ComponentProps {
  tenantId?: string;
}

export default function TenantQuotaCard({ tenantId }: ComponentProps) {
  const [quota, setQuota] = useState<TenantQuotaStatus | null>(null);

  const fetchQuota = async () => {
    try {
      const data = await apiClient.tenant.getQuota();
      setQuota(data);
    } catch {
      // Offline fallback
    }
  };

  useEffect(() => {
    fetchQuota();
  }, [tenantId]);

  if (!quota) return null;

  const calculatePercent = (used: number, limit: number) => {
    if (limit <= 0) return 0;
    return Math.min(100, Math.round((used / limit) * 100));
  };

  return (
    <div className="p-6 rounded-2xl border border-border bg-surface space-y-4">
      <div className="flex items-center justify-between border-b border-border pb-3">
        <div>
          <span className="text-[10px] font-extrabold uppercase tracking-wider text-blue-400">IDEMPOTENT QUOTA LEDGER</span>
          <h3 className="mt-0.5 flex items-center gap-1.5 text-base font-bold text-foreground"><Gauge size={16} className="text-primary" aria-hidden /> Subscription Quotas</h3>
        </div>
        <span className="px-3 py-1 rounded-full bg-blue-600/10 text-blue-400 border border-blue-500/20 text-xs font-extrabold">
          {quota.planName}
        </span>
      </div>

      <div className="space-y-3">
        {/* Crawl Quota Meter */}
        <div className="space-y-1">
          <div className="flex justify-between text-xs font-semibold">
            <span className="text-foreground">Crawler Page Audits</span>
            <span className="text-muted">{quota.crawlQuotaUsed} / {quota.crawlQuotaLimit}</span>
          </div>
          <div className="w-full h-2 rounded-full bg-elevated overflow-hidden">
            <div
              className="h-full bg-blue-500 rounded-full transition-all duration-500"
              style={{ width: `${calculatePercent(quota.crawlQuotaUsed, quota.crawlQuotaLimit)}%` }}
            />
          </div>
        </div>

        {/* AI Recommendations Meter */}
        <div className="space-y-1">
          <div className="flex justify-between text-xs font-semibold">
            <span className="text-foreground">AI SEO Prompts</span>
            <span className="text-muted">{quota.aiQuotaUsed} / {quota.aiQuotaLimit}</span>
          </div>
          <div className="w-full h-2 rounded-full bg-elevated overflow-hidden">
            <div
              className="h-full bg-purple-500 rounded-full transition-all duration-500"
              style={{ width: `${calculatePercent(quota.aiQuotaUsed, quota.aiQuotaLimit)}%` }}
            />
          </div>
        </div>

        {/* Keyword Tracking Meter */}
        <div className="space-y-1">
          <div className="flex justify-between text-xs font-semibold">
            <span className="text-foreground">Tracked Keywords</span>
            <span className="text-muted">{quota.keywordQuotaUsed} / {quota.keywordQuotaLimit}</span>
          </div>
          <div className="w-full h-2 rounded-full bg-elevated overflow-hidden">
            <div
              className="h-full bg-emerald-500 rounded-full transition-all duration-500"
              style={{ width: `${calculatePercent(quota.keywordQuotaUsed, quota.keywordQuotaLimit)}%` }}
            />
          </div>
        </div>
      </div>

      <div className="text-[10px] text-subtle text-right font-mono">
        Cycle Resets: {new Date(quota.cycleResetsAt).toLocaleDateString()}
      </div>
    </div>
  );
}
