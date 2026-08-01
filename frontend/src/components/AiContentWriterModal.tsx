"use client";

import { AiContentPlan, apiClient } from "@/lib/apiClient";
import { useEffect, useState } from "react";
import { getErrorMessage } from "@/lib/errors";

interface ModalProps {
  websiteId: string | null;
  websiteName: string;
  initialKeyword?: string;
  initialPath?: string;
  onClose: () => void;
}

export default function AiContentWriterModal({
  websiteId,
  websiteName,
  initialKeyword = "",
  initialPath = "",
  onClose,
}: ModalProps) {
  const [keyword, setKeyword] = useState(initialKeyword);
  const [missingPath, setMissingPath] = useState(initialPath);
  const [generatedContent, setGeneratedContent] = useState<AiContentPlan | null>(null);
  const [history, setHistory] = useState<AiContentPlan[]>([]);
  const [loading, setLoading] = useState(false);
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<"editor" | "history">("editor");

  useEffect(() => {
    setKeyword(initialKeyword);
    setMissingPath(initialPath);
  }, [initialKeyword, initialPath]);

  const fetchHistory = async () => {
    if (!websiteId) return;
    try {
      const data = await apiClient.content.list(websiteId);
      setHistory(data);
    } catch {
      // History fetch offline
    }
  };

  useEffect(() => {
    fetchHistory();
  }, [websiteId]);

  if (!websiteId) return null;

  const handleGenerate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!keyword.trim()) return;

    setLoading(true);
    setError(null);
    try {
      const result = await apiClient.content.generate(websiteId, {
        targetKeyword: keyword.trim(),
        missingPath: missingPath.trim() || null,
      });
      setGeneratedContent(result);
      fetchHistory();
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Failed to generate AI content."));
    } finally {
      setLoading(false);
    }
  };

  const handleCopy = () => {
    if (!generatedContent) return;
    navigator.clipboard.writeText(generatedContent.generatedMarkdownContent);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };

  const handleDownload = () => {
    if (!generatedContent) return;
    const blob = new Blob([generatedContent.generatedMarkdownContent], { type: "text/markdown" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `${keyword.replace(/\s+/g, "_")}_GEO_Article.md`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-fadeIn">
      <div className="relative w-full max-w-4xl max-h-[90vh] overflow-y-auto rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] shadow-2xl p-6 sm:p-8 space-y-5">
        
        {/* Header */}
        <div className="flex items-center justify-between border-b border-[var(--border-color)] pb-4">
          <div>
            <span className="text-xs uppercase tracking-wider text-blue-400 font-bold">Princeton GEO Optimization Engine</span>
            <h2 className="text-2xl font-extrabold text-white mt-1">✍️ AI Content Writer & Gold Fixer</h2>
            <p className="text-xs text-slate-400">Generate high-fact-density articles tailored for AI search citations (Perplexity, ChatGPT, Claude, Gemini).</p>
          </div>
          
          <button
            onClick={onClose}
            className="p-2 text-slate-400 hover:text-white rounded-lg hover:bg-slate-800 transition"
          >
            ✕
          </button>
        </div>

        {/* Navigation Tabs */}
        <div className="flex border-b border-[var(--border-color)] gap-2 text-xs font-bold">
          <button
            onClick={() => setActiveTab("editor")}
            className={`px-4 py-2 transition border-b-2 rounded-t-lg ${
              activeTab === "editor"
                ? "border-blue-500 text-blue-400 bg-blue-500/5"
                : "border-transparent text-slate-400 hover:text-slate-200"
            }`}
          >
            ⚡ Content Generator & Editor
          </button>
          <button
            onClick={() => setActiveTab("history")}
            className={`px-4 py-2 transition border-b-2 rounded-t-lg ${
              activeTab === "history"
                ? "border-purple-500 text-purple-400 bg-purple-500/5"
                : "border-transparent text-slate-400 hover:text-slate-200"
            }`}
          >
            📚 Generated History ({history.length})
          </button>
        </div>

        {activeTab === "editor" ? (
          <div className="space-y-5">
            {/* Input Form */}
            <form onSubmit={handleGenerate} className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] space-y-3">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1">Hedef Anahtar Kelime (Keyword)</label>
                  <input
                    type="text"
                    placeholder="e.g. motosiklet kaskı çeşitleri"
                    value={keyword}
                    onChange={(e) => setKeyword(e.target.value)}
                    required
                    className="w-full px-3 py-2 rounded-lg border border-[var(--border-color)] bg-[var(--card-bg)] text-xs font-medium text-white focus:outline-none focus:ring-1 focus:ring-blue-500"
                  />
                </div>

                <div>
                  <label className="block text-xs font-semibold text-slate-300 mb-1">
                    Gold Opportunity 404 Path (Opsiyonel)
                  </label>
                  <input
                    type="text"
                    placeholder="e.g. /missing-catalog-page"
                    value={missingPath}
                    onChange={(e) => setMissingPath(e.target.value)}
                    className="w-full px-3 py-2 rounded-lg border border-[var(--border-color)] bg-[var(--card-bg)] text-xs font-mono text-amber-300 focus:outline-none focus:ring-1 focus:ring-amber-500"
                  />
                </div>
              </div>

              <div className="flex justify-end pt-1">
                <button
                  type="submit"
                  disabled={loading}
                  className="px-5 py-2.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-extrabold text-xs transition disabled:opacity-50 shadow-md flex items-center gap-2"
                >
                  {loading ? "Generating GEO Article with AI..." : "⚡ Generate Princeton GEO Article"}
                </button>
              </div>
            </form>

            {error && (
              <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-xs font-semibold">
                {error}
              </div>
            )}

            {/* Generated Article Editor & Previewer */}
            {generatedContent && (
              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-2">
                    <span className="text-xs font-bold text-slate-300">Generated Markdown Article</span>
                    <span className="px-2 py-0.5 rounded text-[10px] font-mono bg-blue-500/20 text-blue-300 border border-blue-500/30">
                      {generatedContent.tokensSpent} tokens spent
                    </span>
                  </div>

                  <div className="flex items-center gap-2">
                    <button
                      onClick={handleCopy}
                      className="px-3 py-1.5 rounded-lg bg-blue-600/20 text-blue-300 hover:bg-blue-600/30 text-xs font-semibold transition"
                    >
                      {copied ? "✓ Copied!" : "📋 Copy Markdown"}
                    </button>

                    <button
                      onClick={handleDownload}
                      className="px-3 py-1.5 rounded-lg bg-emerald-600/20 text-emerald-300 hover:bg-emerald-600/30 text-xs font-semibold transition"
                    >
                      📥 Download .md
                    </button>
                  </div>
                </div>

                <textarea
                  value={generatedContent.generatedMarkdownContent}
                  onChange={(e) => setGeneratedContent({ ...generatedContent, generatedMarkdownContent: e.target.value })}
                  rows={14}
                  className="w-full p-4 rounded-xl bg-black/70 border border-slate-800 font-mono text-xs text-slate-200 focus:outline-none focus:ring-1 focus:ring-blue-500 leading-relaxed"
                />
              </div>
            )}
          </div>
        ) : (
          <div className="space-y-3">
            {history.length === 0 ? (
              <p className="py-8 text-center text-xs text-slate-400">No content plans generated yet for this site.</p>
            ) : (
              <div className="space-y-2.5">
                {history.map((plan) => (
                  <div
                    key={plan.id}
                    className="p-4 rounded-xl border border-[var(--border-color)] bg-[var(--bg-primary)] flex items-center justify-between gap-3 text-xs"
                  >
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-bold text-white text-sm">{plan.targetKeyword}</span>
                        <span className="text-[10px] font-mono text-slate-400">
                          {new Date(plan.createdAt).toLocaleDateString()}
                        </span>
                      </div>
                      <p className="text-[11px] text-slate-400 mt-1 line-clamp-1">
                        {plan.generatedMarkdownContent.substring(0, 120)}...
                      </p>
                    </div>

                    <button
                      onClick={() => {
                        setGeneratedContent(plan);
                        setKeyword(plan.targetKeyword);
                        setActiveTab("editor");
                      }}
                      className="px-3 py-1.5 rounded-lg bg-blue-600/20 text-blue-300 hover:bg-blue-600/30 font-semibold text-xs transition whitespace-nowrap"
                    >
                      View & Edit
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

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
