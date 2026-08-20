"use client";

import Modal from "@/components/ui/Modal";
import { useApp } from "@/components/providers";
import { AiContentPlan, apiClient } from "@/lib/apiClient";
import { getErrorMessage } from "@/lib/errors";
import { queryKeys } from "@/lib/queryKeys";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, Copy, Download, History, PenLine, Sparkles, Zap } from "lucide-react";
import { useState } from "react";

interface ModalProps {
  websiteId: string;
  websiteName: string;
  initialKeyword?: string;
  initialPath?: string;
  onClose: () => void;
}

export default function AiContentWriterModal({
  websiteId,
  initialKeyword = "",
  initialPath = "",
  onClose,
}: ModalProps) {
  const { t } = useApp();
  const queryClient = useQueryClient();
  const [keyword, setKeyword] = useState(initialKeyword);
  const [missingPath, setMissingPath] = useState(initialPath);
  const [generatedContent, setGeneratedContent] = useState<AiContentPlan | null>(null);
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<"editor" | "history">("editor");
  const historyQuery = useQuery({
    queryKey: queryKeys.content(websiteId),
    queryFn: () => apiClient.content.list(websiteId),
  });
  const history = historyQuery.data ?? [];
  const generateMutation = useMutation({
    mutationFn: (body: { targetKeyword: string; missingPath: string | null }) => apiClient.content.generate(websiteId, body),
    onSuccess: (result) => {
      setGeneratedContent(result);
      queryClient.invalidateQueries({ queryKey: queryKeys.content(websiteId) });
    },
    onError: (err) => setError(getErrorMessage(err, t("contentGenerationFailed"))),
  });

  const handleGenerate = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!keyword.trim()) return;

    setError(null);
    generateMutation.mutate({
        targetKeyword: keyword.trim(),
        missingPath: missingPath.trim() || null,
    });
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

  const tabClass = (active: boolean) =>
    `flex cursor-pointer items-center gap-1.5 rounded-t-lg border-b-2 px-4 py-2 transition-colors ${
      active ? "border-primary text-primary" : "border-transparent text-muted hover:text-foreground"
    }`;

  return (
    <Modal
      onClose={onClose}
      title={t("writerTitle")}
      subtitle={t("writerSubtitle")}
      icon={<Sparkles size={18} aria-hidden />}
      maxWidthClass="max-w-4xl"
      footer={
        <button
          onClick={onClose}
          className="cursor-pointer rounded-lg bg-elevated px-5 py-2 text-xs font-bold text-foreground transition-colors hover:opacity-80"
        >
          {t("close")}
        </button>
      }
    >
      {/* Tabs */}
      <div className="mb-5 flex gap-2 border-b border-border text-xs font-bold">
        <button onClick={() => setActiveTab("editor")} className={tabClass(activeTab === "editor")}>
          <PenLine size={14} aria-hidden /> {t("editorTab")}
        </button>
        <button onClick={() => setActiveTab("history")} className={tabClass(activeTab === "history")}>
          <History size={14} aria-hidden /> {t("history")} ({history.length})
        </button>
      </div>

      {activeTab === "editor" ? (
        <div className="space-y-5">
          {/* Input Form */}
          <form onSubmit={handleGenerate} className="space-y-3 rounded-xl border border-border bg-bg p-4">
            <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
              <div>
                <label htmlFor={`content-keyword-${websiteId}`} className="mb-1 block text-xs font-semibold text-foreground">{t("targetKeyword")}</label>
                <input
                  id={`content-keyword-${websiteId}`}
                  type="text"
                  placeholder="e.g. lightweight touring helmets"
                  value={keyword}
                  onChange={(e) => setKeyword(e.target.value)}
                  required
                  maxLength={200}
                  className="w-full rounded-lg border border-border bg-surface px-3 py-2 text-xs font-medium text-foreground placeholder:text-subtle focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>

              <div>
                <label htmlFor={`content-path-${websiteId}`} className="mb-1 block text-xs font-semibold text-foreground">
                  {t("missingPath")}
                </label>
                <input
                  id={`content-path-${websiteId}`}
                  type="text"
                  placeholder="e.g. /missing-catalog-page"
                  value={missingPath}
                  onChange={(e) => setMissingPath(e.target.value)}
                  maxLength={2048}
                  className="w-full rounded-lg border border-border bg-surface px-3 py-2 font-mono text-xs text-accent focus:outline-none focus:ring-2 focus:ring-ring"
                />
              </div>
            </div>

            <div className="flex justify-end pt-1">
              <button
                type="submit"
                disabled={generateMutation.isPending}
                className="flex cursor-pointer items-center gap-2 rounded-lg bg-primary px-5 py-2.5 text-xs font-extrabold text-on-primary transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
              >
                <Zap size={14} aria-hidden />
                {generateMutation.isPending ? t("generating") : t("generateArticle")}
              </button>
            </div>
          </form>

          {error && (
            <div className="rounded-lg border border-danger/20 bg-danger/10 p-3 text-xs font-semibold text-danger">
              {error}
            </div>
          )}

          {/* Generated Article Editor */}
          {generatedContent && (
            <div className="space-y-3">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <span className="text-xs font-bold text-foreground">{t("generatedArticle")}</span>
                  <span className="rounded border border-primary/30 bg-primary/10 px-2 py-0.5 font-mono text-[10px] text-primary">
                    {generatedContent.tokensSpent} tokens spent
                  </span>
                </div>

                <div className="flex items-center gap-2">
                  <button
                    onClick={handleCopy}
                    className="flex cursor-pointer items-center gap-1.5 rounded-lg bg-primary/15 px-3 py-1.5 text-xs font-semibold text-primary transition-colors hover:bg-primary/25"
                  >
                    {copied ? <Check size={13} aria-hidden /> : <Copy size={13} aria-hidden />}
                    {copied ? t("copied") : t("copyMarkdown")}
                  </button>

                  <button
                    onClick={handleDownload}
                    className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3 py-1.5 text-xs font-semibold text-muted transition-colors hover:text-foreground"
                  >
                    <Download size={13} aria-hidden /> {t("downloadMarkdown")}
                  </button>
                </div>
              </div>

              <label htmlFor={`content-editor-${websiteId}`} className="sr-only">{t("generatedArticle")}</label>
              <textarea
                id={`content-editor-${websiteId}`}
                aria-label={t("generatedArticle")}
                value={generatedContent.generatedMarkdownContent}
                onChange={(e) => setGeneratedContent({ ...generatedContent, generatedMarkdownContent: e.target.value })}
                rows={14}
                className="w-full rounded-xl border border-border bg-elevated p-4 font-mono text-xs leading-relaxed text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>
          )}
        </div>
      ) : (
        <div className="space-y-3">
          {historyQuery.isPending ? (
            <p className="py-8 text-center text-xs text-muted" role="status">{t("loadingHistory")}</p>
          ) : historyQuery.isError ? (
            <p className="rounded-lg border border-danger/20 bg-danger/10 p-3 text-xs text-danger" role="alert">{t("historyLoadFailed")}</p>
          ) : history.length === 0 ? (
            <p className="py-8 text-center text-xs text-muted">{t("noHistory")}</p>
          ) : (
            <div className="space-y-2.5">
              {history.map((plan) => (
                <div
                  key={plan.id}
                  className="flex items-center justify-between gap-3 rounded-xl border border-border bg-bg p-4 text-xs"
                >
                  <div>
                    <div className="flex items-center gap-2">
                      <span className="text-sm font-bold text-foreground">{plan.targetKeyword}</span>
                      <span className="font-mono text-[10px] text-muted">
                        {new Date(plan.createdAt).toLocaleDateString()}
                      </span>
                    </div>
                    <p className="mt-1 line-clamp-1 text-[11px] text-muted">
                      {plan.generatedMarkdownContent.substring(0, 120)}…
                    </p>
                  </div>

                  <button
                    onClick={() => {
                      setGeneratedContent(plan);
                      setKeyword(plan.targetKeyword);
                      setActiveTab("editor");
                    }}
                    className="cursor-pointer whitespace-nowrap rounded-lg bg-primary/15 px-3 py-1.5 text-xs font-semibold text-primary transition-colors hover:bg-primary/25"
                  >
                    {t("viewEdit")}
                  </button>
                </div>
              ))}
            </div>
          )}
        </div>
      )}
    </Modal>
  );
}
