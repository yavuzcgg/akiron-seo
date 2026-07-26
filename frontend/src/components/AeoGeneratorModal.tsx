"use client";

import { AeoSchemas, apiClient } from "@/lib/apiClient";
import { useState } from "react";

interface ModalProps {
  websiteId: string | null;
  websiteName: string;
  tenantId?: string;
  onClose: () => void;
}

export default function AeoGeneratorModal({ websiteId, websiteName, tenantId, onClose }: ModalProps) {
  const [schemas, setSchemas] = useState<AeoSchemas | null>(null);
  const [loading, setLoading] = useState(false);
  const [copiedType, setCopiedType] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<"schema" | "llmstxt">("schema");

  if (!websiteId) return null;

  const fetchSchemas = async () => {
    setLoading(true);
    try {
      const data = await apiClient.websites.getAeoSchemas(websiteId);
      setSchemas(data);
    } catch {
      // Handle error
    } finally {
      setLoading(false);
    }
  };

  if (!schemas && !loading) {
    fetchSchemas();
  }

  const handleCopy = (text: string, type: string) => {
    navigator.clipboard.writeText(text);
    setCopiedType(type);
    setTimeout(() => setCopiedType(null), 2000);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-fadeIn">
      <div className="relative w-full max-w-3xl max-h-[90vh] overflow-y-auto rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] shadow-2xl p-6 sm:p-8 space-y-6">
        
        {/* Header */}
        <div className="flex items-center justify-between border-b border-[var(--border-color)] pb-4">
          <div>
            <span className="text-xs uppercase tracking-wider text-purple-400 font-bold">Answer Engine Optimization (AEO)</span>
            <h2 className="text-xl font-extrabold text-white mt-1">Schema.org & llms.txt Generator</h2>
            <p className="text-xs text-slate-400">Generate structured data & LLM specifications for {websiteName}.</p>
          </div>
          
          <button
            onClick={onClose}
            className="p-2 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 transition"
          >
            ✕
          </button>
        </div>

        {/* Tabs */}
        <div className="flex border-b border-[var(--border-color)] space-x-4 text-xs font-bold">
          <button
            onClick={() => setActiveTab("schema")}
            className={`pb-3 transition border-b-2 ${
              activeTab === "schema"
                ? "border-blue-500 text-blue-400"
                : "border-transparent text-slate-400 hover:text-slate-200"
            }`}
          >
            🏷️ Schema.org JSON-LD
          </button>
          <button
            onClick={() => setActiveTab("llmstxt")}
            className={`pb-3 transition border-b-2 ${
              activeTab === "llmstxt"
                ? "border-purple-500 text-purple-400"
                : "border-transparent text-slate-400 hover:text-slate-200"
            }`}
          >
            🤖 llms.txt File Standard
          </button>
        </div>

        {loading ? (
          <div className="py-12 text-center text-xs text-slate-400">Generating AEO schemas...</div>
        ) : schemas ? (
          <div className="space-y-4">
            {activeTab === "schema" && (
              <div className="space-y-4">
                <div>
                  <div className="flex items-center justify-between text-xs mb-1.5">
                    <span className="font-bold text-slate-300">1. Organization Schema (JSON-LD)</span>
                    <button
                      onClick={() => handleCopy(schemas.organizationJsonLd, "org")}
                      className="px-2.5 py-1 rounded bg-blue-600/20 text-blue-300 hover:bg-blue-600/30 text-[11px] font-semibold transition"
                    >
                      {copiedType === "org" ? "✓ Copied!" : "📋 Copy Script"}
                    </button>
                  </div>
                  <pre className="p-3.5 rounded-xl bg-black/60 border border-slate-800 text-xs font-mono text-emerald-400 overflow-x-auto max-h-48">
                    {schemas.organizationJsonLd}
                  </pre>
                </div>

                <div>
                  <div className="flex items-center justify-between text-xs mb-1.5">
                    <span className="font-bold text-slate-300">2. WebSite Search Action Schema</span>
                    <button
                      onClick={() => handleCopy(schemas.webSiteJsonLd, "site")}
                      className="px-2.5 py-1 rounded bg-blue-600/20 text-blue-300 hover:bg-blue-600/30 text-[11px] font-semibold transition"
                    >
                      {copiedType === "site" ? "✓ Copied!" : "📋 Copy Script"}
                    </button>
                  </div>
                  <pre className="p-3.5 rounded-xl bg-black/60 border border-slate-800 text-xs font-mono text-emerald-400 overflow-x-auto max-h-48">
                    {schemas.webSiteJsonLd}
                  </pre>
                </div>
              </div>
            )}

            {activeTab === "llmstxt" && (
              <div className="space-y-3">
                <div className="flex items-center justify-between text-xs">
                  <span className="font-bold text-slate-300">Standard llms.txt Markdown File</span>
                  <button
                    onClick={() => handleCopy(schemas.llmsTxtContent, "llms")}
                    className="px-2.5 py-1 rounded bg-purple-600/20 text-purple-300 hover:bg-purple-600/30 text-[11px] font-semibold transition"
                  >
                    {copiedType === "llms" ? "✓ Copied!" : "📋 Copy llms.txt"}
                  </button>
                </div>
                <p className="text-xs text-slate-400 leading-relaxed">
                  Save this content as <code className="text-purple-300">llms.txt</code> in your website's root directory (<code className="text-slate-300">https://yourdomain.com/llms.txt</code>) so AI search engines (Perplexity, SearchGPT, Claude) can parse your brand instantly.
                </p>
                <pre className="p-4 rounded-xl bg-black/60 border border-slate-800 text-xs font-mono text-purple-300 overflow-x-auto max-h-64 whitespace-pre-wrap">
                  {schemas.llmsTxtContent}
                </pre>
              </div>
            )}
          </div>
        ) : null}

        {/* Footer */}
        <div className="flex justify-end pt-2 border-t border-[var(--border-color)]">
          <button
            onClick={onClose}
            className="px-5 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-200 font-bold text-xs transition"
          >
            Close
          </button>
        </div>

      </div>
    </div>
  );
}
