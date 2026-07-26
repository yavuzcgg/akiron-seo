"use client";

import { AeoSchemas, apiClient } from "@/lib/apiClient";
import { useState } from "react";

interface ModalProps {
  websiteId: string | null;
  websiteName: string;
  tenantId?: string;
  onClose: () => void;
}

type TabKey = "schema" | "llmstxt" | "llmsfull";

export default function AeoGeneratorModal({ websiteId, websiteName, tenantId, onClose }: ModalProps) {
  const [schemas, setSchemas] = useState<AeoSchemas | null>(null);
  const [loading, setLoading] = useState(false);
  const [copiedType, setCopiedType] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<TabKey>("schema");

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

  const tabs: { key: TabKey; label: string; icon: string; color: string }[] = [
    { key: "schema", label: "Schema.org JSON-LD", icon: "🏷️", color: "blue" },
    { key: "llmstxt", label: "llms.txt", icon: "🤖", color: "purple" },
    { key: "llmsfull", label: "llms-full.txt", icon: "📄", color: "emerald" },
  ];

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
        <div className="flex border-b border-[var(--border-color)] gap-1 text-xs font-bold">
          {tabs.map((tab) => (
            <button
              key={tab.key}
              onClick={() => setActiveTab(tab.key)}
              className={`px-4 py-2.5 transition border-b-2 rounded-t-lg ${
                activeTab === tab.key
                  ? `border-${tab.color}-500 text-${tab.color}-400 bg-${tab.color}-500/5`
                  : "border-transparent text-slate-400 hover:text-slate-200 hover:bg-slate-800/50"
              }`}
              style={activeTab === tab.key ? {
                borderBottomColor: tab.color === "blue" ? "#3b82f6" : tab.color === "purple" ? "#a855f7" : "#10b981",
                color: tab.color === "blue" ? "#60a5fa" : tab.color === "purple" ? "#c084fc" : "#34d399",
              } : {}}
            >
              {tab.icon} {tab.label}
            </button>
          ))}
        </div>

        {loading ? (
          <div className="py-12 text-center">
            <div className="inline-flex items-center gap-2 text-sm text-slate-400">
              <span className="animate-spin">⏳</span> Generating AEO schemas...
            </div>
          </div>
        ) : schemas ? (
          <div className="space-y-4">

            {/* SCHEMA TAB */}
            {activeTab === "schema" && (
              <div className="space-y-5">
                {/* Organization JSON-LD */}
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

                {/* WebSite Search JSON-LD */}
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

                {/* FAQ JSON-LD */}
                <div>
                  <div className="flex items-center justify-between text-xs mb-1.5">
                    <span className="font-bold text-slate-300">3. FAQ Page Schema (JSON-LD)</span>
                    <button
                      onClick={() => handleCopy(schemas.faqJsonLd, "faq")}
                      className="px-2.5 py-1 rounded bg-blue-600/20 text-blue-300 hover:bg-blue-600/30 text-[11px] font-semibold transition"
                    >
                      {copiedType === "faq" ? "✓ Copied!" : "📋 Copy Script"}
                    </button>
                  </div>
                  <p className="text-[11px] text-slate-400 mb-1.5">
                    Auto-generated FAQ schema from your crawled page data. Add this to your homepage for rich search snippets.
                  </p>
                  <pre className="p-3.5 rounded-xl bg-black/60 border border-slate-800 text-xs font-mono text-emerald-400 overflow-x-auto max-h-56">
                    {schemas.faqJsonLd}
                  </pre>
                </div>
              </div>
            )}

            {/* LLMS.TXT TAB */}
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
                <div className="p-3.5 rounded-xl bg-purple-500/5 border border-purple-500/20 space-y-2">
                  <p className="text-xs text-slate-300 leading-relaxed">
                    Save this content as <code className="text-purple-300 bg-purple-500/10 px-1 py-0.5 rounded">llms.txt</code> in your website&apos;s root directory 
                    (<code className="text-slate-300">{`https://${websiteName}/llms.txt`}</code>) so AI search engines can parse your brand instantly.
                  </p>
                  <div className="flex items-center gap-2 text-[11px]">
                    <span className="px-2 py-0.5 rounded bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 font-bold">Perplexity</span>
                    <span className="px-2 py-0.5 rounded bg-blue-500/10 text-blue-400 border border-blue-500/20 font-bold">ChatGPT</span>
                    <span className="px-2 py-0.5 rounded bg-orange-500/10 text-orange-400 border border-orange-500/20 font-bold">Claude</span>
                    <span className="px-2 py-0.5 rounded bg-cyan-500/10 text-cyan-400 border border-cyan-500/20 font-bold">Gemini</span>
                  </div>
                </div>
                <pre className="p-4 rounded-xl bg-black/60 border border-slate-800 text-xs font-mono text-purple-300 overflow-x-auto max-h-72 whitespace-pre-wrap">
                  {schemas.llmsTxtContent}
                </pre>
              </div>
            )}

            {/* LLMS-FULL.TXT TAB */}
            {activeTab === "llmsfull" && (
              <div className="space-y-3">
                <div className="flex items-center justify-between text-xs">
                  <span className="font-bold text-slate-300">Extended llms-full.txt File</span>
                  <button
                    onClick={() => handleCopy(schemas.llmsFullTxtContent, "llmsfull")}
                    className="px-2.5 py-1 rounded bg-emerald-600/20 text-emerald-300 hover:bg-emerald-600/30 text-[11px] font-semibold transition"
                  >
                    {copiedType === "llmsfull" ? "✓ Copied!" : "📋 Copy llms-full.txt"}
                  </button>
                </div>
                <div className="p-3.5 rounded-xl bg-emerald-500/5 border border-emerald-500/20 space-y-2">
                  <p className="text-xs text-slate-300 leading-relaxed">
                    The extended version includes your <strong className="text-emerald-300">complete crawled page inventory</strong> with titles and descriptions. 
                    Save as <code className="text-emerald-300 bg-emerald-500/10 px-1 py-0.5 rounded">llms-full.txt</code> at your site root for comprehensive AI engine coverage.
                  </p>
                  <p className="text-[11px] text-slate-400">
                    💡 <strong>Tip:</strong> Run a site audit first to populate the page inventory. The more pages crawled, the richer this file becomes.
                  </p>
                </div>
                <pre className="p-4 rounded-xl bg-black/60 border border-slate-800 text-xs font-mono text-emerald-300 overflow-x-auto max-h-80 whitespace-pre-wrap">
                  {schemas.llmsFullTxtContent}
                </pre>
              </div>
            )}
          </div>
        ) : null}

        {/* Footer */}
        <div className="flex items-center justify-between pt-2 border-t border-[var(--border-color)]">
          <span className="text-[10px] text-slate-500">Generated schemas are saved to your database for versioning.</span>
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
