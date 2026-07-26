"use client";

import { apiClient, GoldOpportunity } from "@/lib/apiClient";
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
          <span className="text-xl">🌟</span>
          <div>
            <h3 className="text-sm font-extrabold text-amber-300 uppercase tracking-wider">
              Gold GEO Opportunities ({opportunities.length})
            </h3>
            <p className="text-xs text-slate-300">
              AI generative engines cited missing 404 pages on {websiteName}. Create these pages now for instant GEO traffic!
            </p>
          </div>
        </div>

        <button
          onClick={fetchOpportunities}
          className="text-xs text-amber-400 hover:text-amber-300 underline font-semibold transition"
        >
          🔄 Refresh
        </button>
      </div>

      <div className="space-y-2.5">
        {opportunities.map((opp) => (
          <div
            key={opp.notificationId}
            className="p-3.5 rounded-xl bg-black/60 border border-amber-500/20 flex flex-wrap items-center justify-between gap-3 text-xs"
          >
            <div className="space-y-1 max-w-xl">
              <div className="flex items-center gap-2">
                <span className="px-2 py-0.5 rounded text-[10px] font-extrabold bg-amber-500/20 text-amber-300 border border-amber-500/30">
                  404 Missing Citation
                </span>
                <span className="text-slate-400 font-mono text-[11px]">
                  {new Date(opp.detectedAt).toLocaleTimeString()}
                </span>
              </div>
              <p className="text-slate-200 font-semibold leading-relaxed">{opp.message}</p>
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
                className="px-3.5 py-1.5 rounded-lg bg-amber-500 hover:bg-amber-400 text-black font-extrabold text-xs transition shadow-md flex items-center gap-1"
              >
                ⚡ Create Page with AI
              </button>

              <button
                onClick={() => handleDismiss(opp.notificationId)}
                className="px-2.5 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-400 hover:text-slate-200 transition text-xs"
                title="Dismiss Alert"
              >
                ✕
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
