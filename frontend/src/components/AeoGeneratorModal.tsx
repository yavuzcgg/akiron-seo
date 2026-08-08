"use client";

import Modal from "@/components/ui/Modal";
import { AeoSchemas, apiClient } from "@/lib/apiClient";
import { getErrorMessage } from "@/lib/errors";
import { Bot, Braces, Check, Copy, FileCode2, FileText, Lightbulb } from "lucide-react";
import { useEffect, useState } from "react";

interface ModalProps {
  websiteId: string | null;
  websiteName: string;
  onClose: () => void;
}

type TabKey = "schema" | "llmstxt" | "llmsfull";

const TABS: { key: TabKey; label: string; Icon: typeof Bot }[] = [
  { key: "schema", label: "Schema.org JSON-LD", Icon: Braces },
  { key: "llmstxt", label: "llms.txt", Icon: Bot },
  { key: "llmsfull", label: "llms-full.txt", Icon: FileText },
];

export default function AeoGeneratorModal({ websiteId, websiteName, onClose }: ModalProps) {
  const [schemas, setSchemas] = useState<AeoSchemas | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [copiedType, setCopiedType] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<TabKey>("schema");

  // Keyed on websiteId, clearing prior state, so opening site B after A never shows
  // A's schemas. The cancelled flag drops a slow earlier response.
  useEffect(() => {
    if (!websiteId) return;

    let cancelled = false;
    setSchemas(null);
    setError(null);
    setLoading(true);

    apiClient.websites
      .getAeoSchemas(websiteId)
      .then((data) => {
        if (!cancelled) setSchemas(data);
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(getErrorMessage(err, "Failed to generate AEO schemas."));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, [websiteId]);

  if (!websiteId) return null;

  const handleCopy = (text: string, type: string) => {
    navigator.clipboard.writeText(text);
    setCopiedType(type);
    setTimeout(() => setCopiedType(null), 2000);
  };

  const copyButton = (text: string, type: string, label: string) => (
    <button
      onClick={() => handleCopy(text, type)}
      className="flex cursor-pointer items-center gap-1.5 rounded bg-primary/15 px-2.5 py-1 text-[11px] font-semibold text-primary transition-colors hover:bg-primary/25"
    >
      {copiedType === type ? <Check size={12} aria-hidden /> : <Copy size={12} aria-hidden />}
      {copiedType === type ? "Copied!" : label}
    </button>
  );

  const codeBlock = (content: string, maxH = "max-h-48") => (
    <pre className={`overflow-x-auto rounded-xl border border-border bg-elevated p-3.5 font-mono text-xs text-success ${maxH} whitespace-pre-wrap`}>
      {content}
    </pre>
  );

  return (
    <Modal
      onClose={onClose}
      title="Schema.org & llms.txt Generator"
      subtitle={`Structured data & LLM specifications for ${websiteName}`}
      icon={<FileCode2 size={18} aria-hidden />}
      maxWidthClass="max-w-3xl"
      footer={
        <div className="flex w-full items-center justify-between">
          <span className="text-[10px] text-subtle">Generated schemas are saved for versioning.</span>
          <button
            onClick={onClose}
            className="cursor-pointer rounded-lg bg-elevated px-5 py-2 text-xs font-bold text-foreground transition-colors hover:opacity-80"
          >
            Close
          </button>
        </div>
      }
    >
      {/* Tabs */}
      <div className="mb-5 flex gap-1 border-b border-border text-xs font-bold">
        {TABS.map(({ key, label, Icon }) => (
          <button
            key={key}
            onClick={() => setActiveTab(key)}
            className={`flex cursor-pointer items-center gap-1.5 rounded-t-lg border-b-2 px-4 py-2.5 transition-colors ${
              activeTab === key
                ? "border-primary text-primary"
                : "border-transparent text-muted hover:text-foreground"
            }`}
          >
            <Icon size={14} aria-hidden />
            {label}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="py-12 text-center text-sm text-muted">Generating AEO schemas…</div>
      ) : error ? (
        <div className="py-12 text-center text-sm font-semibold text-danger">{error}</div>
      ) : schemas ? (
        <div className="space-y-4">
          {activeTab === "schema" && (
            <div className="space-y-5">
              <div>
                <div className="mb-1.5 flex items-center justify-between text-xs">
                  <span className="font-bold text-foreground">1. Organization Schema (JSON-LD)</span>
                  {copyButton(schemas.organizationJsonLd, "org", "Copy Script")}
                </div>
                {codeBlock(schemas.organizationJsonLd)}
              </div>

              <div>
                <div className="mb-1.5 flex items-center justify-between text-xs">
                  <span className="font-bold text-foreground">2. WebSite Search Action Schema</span>
                  {copyButton(schemas.webSiteJsonLd, "site", "Copy Script")}
                </div>
                {codeBlock(schemas.webSiteJsonLd)}
              </div>

              <div>
                <div className="mb-1.5 flex items-center justify-between text-xs">
                  <span className="font-bold text-foreground">3. FAQ Page Schema (JSON-LD)</span>
                  {copyButton(schemas.faqJsonLd, "faq", "Copy Script")}
                </div>
                <p className="mb-1.5 text-[11px] text-muted">
                  Auto-generated from your crawled page data. Add it to your homepage for rich snippets.
                </p>
                {codeBlock(schemas.faqJsonLd, "max-h-56")}
              </div>
            </div>
          )}

          {activeTab === "llmstxt" && (
            <div className="space-y-3">
              <div className="flex items-center justify-between text-xs">
                <span className="font-bold text-foreground">Standard llms.txt Markdown File</span>
                {copyButton(schemas.llmsTxtContent, "llms", "Copy llms.txt")}
              </div>
              <div className="rounded-xl border border-border bg-elevated p-3.5">
                <p className="text-xs leading-relaxed text-foreground">
                  Save this as{" "}
                  <code className="rounded bg-surface px-1 py-0.5 text-accent">llms.txt</code> in your
                  site root (<code className="text-foreground">{`https://${websiteName}/llms.txt`}</code>)
                  so answer engines can parse your brand.
                </p>
              </div>
              {codeBlock(schemas.llmsTxtContent, "max-h-72")}
            </div>
          )}

          {activeTab === "llmsfull" && (
            <div className="space-y-3">
              <div className="flex items-center justify-between text-xs">
                <span className="font-bold text-foreground">Extended llms-full.txt File</span>
                {copyButton(schemas.llmsFullTxtContent, "llmsfull", "Copy llms-full.txt")}
              </div>
              <div className="space-y-2 rounded-xl border border-border bg-elevated p-3.5">
                <p className="text-xs leading-relaxed text-foreground">
                  Includes your complete crawled page inventory with titles and descriptions. Save as{" "}
                  <code className="rounded bg-surface px-1 py-0.5 text-accent">llms-full.txt</code> at your
                  site root.
                </p>
                <p className="flex items-center gap-1.5 text-[11px] text-muted">
                  <Lightbulb size={13} aria-hidden /> Run a site audit first to populate the inventory —
                  the more pages crawled, the richer this file.
                </p>
              </div>
              {codeBlock(schemas.llmsFullTxtContent, "max-h-80")}
            </div>
          )}
        </div>
      ) : null}
    </Modal>
  );
}
