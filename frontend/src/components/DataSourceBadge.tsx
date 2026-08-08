"use client";

import { DataSource } from "@/lib/apiClient";

const LABELS: Record<Exclude<DataSource, "Live">, { text: string; title: string; className: string }> = {
  Simulated: {
    text: "DEMO DATA",
    title:
      "Not a measurement. No third-party integration is connected for this metric yet, so these figures are generated locally.",
    className: "bg-amber-500/10 text-amber-400 border-amber-500/30",
  },
  NotConfigured: {
    text: "NOT CONFIGURED",
    title: "No API key is configured for this provider, so nothing was queried.",
    className: "bg-slate-500/10 text-muted border-slate-500/30",
  },
  Unavailable: {
    text: "UNAVAILABLE",
    title: "The provider could not be reached for this run. The result is unknown, not negative.",
    className: "bg-rose-500/10 text-rose-400 border-rose-500/30",
  },
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
  if (!source || source === "Live") return null;

  const label = LABELS[source];
  if (!label) return null;

  return (
    <span
      title={label.title}
      className={`inline-flex items-center px-1.5 py-0.5 rounded border text-[9px] font-bold tracking-wide cursor-help ${label.className} ${className}`}
    >
      {label.text}
    </span>
  );
}
