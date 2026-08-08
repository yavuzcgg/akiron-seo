"use client";

import { apiClient, GoldOpportunity } from "@/lib/apiClient";
import { RefreshCw, Sparkles, X, Zap } from "lucide-react";
import { useEffect, useState } from "react";

interface PanelProps {
  websiteId: string;
  websiteName: string;
  onOpenWriter?: (keyword: string, missingPath: string) => void;
}

export default function GoldOpportunityPanel({ websiteId, websiteName, onOpenWriter }: PanelProps) {
  const [opportunities, setOpportunities] = useState<GoldOpportunity[]>([]);
  const [loading, setLoading] = useState(false);

  const fetchOpportunities = async () => {
    setLoading(true);
    try {
      const data = await apiClient.websites.getGoldOpportunities(websiteId);
      setOpportunities(data);
    } catch {
      // API Offline
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchOpportunities();
  }, [websiteId]);

  const handleDismiss = async (notificationId: string) => {
    try {
      await apiClient.notifications.markRead(notificationId);
      setOpportunities((prev) => prev.filter((o) => o.notificationId !== notificationId));
    } catch {
      // Error
    }
  };

  const extractKeywordAndPath = (message: string) => {
    // Message format: "... cited 'https://domain.com/path' for keyword 'XYZ'..."
    let keyword = "hedef sayfa içeriği";
    let path = "";

    const kwMatch = message.match(/keyword '([^']+)'/i);
    if (kwMatch) keyword = kwMatch[1];

    const urlMatch = message.match(/'(https?:\/\/[^']+)'/i);
    if (urlMatch) {
      try {
        const u = new URL(urlMatch[1]);
        path = u.pathname;
      } catch {
        path = urlMatch[1];
      }
    }

    return { keyword, path };
  };

  if (loading && opportunities.length === 0) return null;
  if (opportunities.length === 0) return null;

  return (
    <div className="p-5 rounded-2xl border border-amber-500/30 bg-gradient-to-r from-amber-500/10 via-purple-500/10 to-blue-500/10 space-y-3 animate-fadeIn shadow-lg">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Sparkles size={20} className="text-accent" aria-hidden />
          <div>
            <h3 className="text-sm font-extrabold uppercase tracking-wider text-accent">
              Gold GEO Opportunities ({opportunities.length})
            </h3>
            <p className="text-xs text-foreground">
              AI engines cited missing 404 pages on {websiteName}. Create these pages for instant GEO traffic.
            </p>
          </div>
        </div>

        <button
          onClick={fetchOpportunities}
          className="flex cursor-pointer items-center gap-1 text-xs font-semibold text-muted transition-colors hover:text-foreground"
        >
          <RefreshCw size={12} aria-hidden /> Refresh
        </button>
      </div>

      <div className="space-y-2.5">
        {opportunities.map((opp) => (
          <div
            key={opp.notificationId}
            className="flex flex-wrap items-center justify-between gap-3 rounded-xl border border-border bg-surface p-3.5 text-xs"
          >
            <div className="space-y-1 max-w-xl">
              <div className="flex items-center gap-2">
                <span className="px-2 py-0.5 rounded text-[10px] font-extrabold bg-amber-500/20 text-amber-300 border border-amber-500/30">
                  404 Missing Citation
                </span>
                <span className="text-muted font-mono text-[11px]">
                  {new Date(opp.detectedAt).toLocaleTimeString()}
                </span>
              </div>
              <p className="text-foreground font-semibold leading-relaxed">{opp.message}</p>
            </div>

            <div className="flex items-center gap-2">
              <button
                onClick={() => {
                  const { keyword, path } = extractKeywordAndPath(opp.message);
                  if (onOpenWriter) {
                    onOpenWriter(keyword, path);
                  } else {
                    alert(`Target Keyword: ${keyword}`);
                  }
                }}
                className="flex cursor-pointer items-center gap-1.5 rounded-lg bg-accent px-3.5 py-1.5 text-xs font-extrabold text-white transition-opacity hover:opacity-90"
              >
                <Zap size={13} aria-hidden /> Create Page with AI
              </button>

              <button
                onClick={() => handleDismiss(opp.notificationId)}
                className="cursor-pointer rounded-lg bg-elevated px-2 py-1.5 text-muted transition-colors hover:text-foreground"
                aria-label="Dismiss alert"
              >
                <X size={14} aria-hidden />
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
