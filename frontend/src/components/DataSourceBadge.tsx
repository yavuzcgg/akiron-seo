"use client";

import { DataSource } from "@/lib/apiClient";
import { useApp } from "@/components/providers";

const STYLES: Record<Exclude<DataSource, "Live">, string> = {
  Simulated: "bg-amber-500/10 text-amber-400 border-amber-500/30",
  NotConfigured: "bg-slate-500/10 text-muted border-slate-500/30",
  Unavailable: "bg-rose-500/10 text-rose-400 border-rose-500/30",
};

interface DataSourceBadgeProps {
  source?: DataSource;
  className?: string;
}

/**
 * Marks a value that is not a real measurement. Renders nothing for live data, so
 * placing it next to any metric is safe — it only appears when there is something
 * the user needs to know.
 */
export default function DataSourceBadge({ source, className = "" }: DataSourceBadgeProps) {
  const { t } = useApp();
  if (!source || source === "Live") return null;

  const labels = {
    Simulated: { text: t("demoData"), title: t("demoDataHelp") },
    NotConfigured: { text: t("notConfigured"), title: t("notConfiguredHelp") },
    Unavailable: { text: t("unavailable"), title: t("unavailableHelp") },
  } as const;
  const label = labels[source];

  return (
    <span
      title={label.title}
      className={`inline-flex items-center px-1.5 py-0.5 rounded border text-[9px] font-bold tracking-wide cursor-help ${STYLES[source]} ${className}`}
    >
      {label.text}
    </span>
  );
}
