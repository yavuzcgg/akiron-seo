"use client";

import Modal from "@/components/ui/Modal";
import { useApp } from "@/components/providers";
import { apiClient } from "@/lib/apiClient";
import { getErrorMessage } from "@/lib/errors";
import { queryKeys } from "@/lib/queryKeys";
import { useQuery } from "@tanstack/react-query";
import { Bot, Braces, Check, Copy, FileCode2, FileText, Lightbulb } from "lucide-react";
import { useState } from "react";

interface ModalProps {
  websiteId: string;
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
  const { t } = useApp();
  const [copiedType, setCopiedType] = useState<string | null>(null);
  const [activeTab, setActiveTab] = useState<TabKey>("schema");
  const schemasQuery = useQuery({
    queryKey: queryKeys.aeo(websiteId),
    queryFn: () => apiClient.websites.getAeoSchemas(websiteId),
  });
  const schemas = schemasQuery.data;

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
      {copiedType === type ? t("copied") : label}
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
      title={t("aeoGeneratorTitle")}
      subtitle={`${t("aeoGeneratorSubtitle")} ${websiteName}`}
      icon={<FileCode2 size={18} aria-hidden />}
      maxWidthClass="max-w-3xl"
      footer={
        <div className="flex w-full items-center justify-between">
          <span className="text-[10px] text-subtle">{t("schemasSaved")}</span>
          <button
            onClick={onClose}
            className="cursor-pointer rounded-lg bg-elevated px-5 py-2 text-xs font-bold text-foreground transition-colors hover:opacity-80"
          >
            {t("close")}
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

      {schemasQuery.isPending ? (
        <div className="py-12 text-center text-sm text-muted">{t("generatingSchemas")}</div>
      ) : schemasQuery.isError ? (
        <div className="py-12 text-center text-sm font-semibold text-danger" role="alert">{getErrorMessage(schemasQuery.error, t("aeoLoadFailed"))}</div>
      ) : schemas ? (
        <div className="space-y-4">
          {activeTab === "schema" && (
            <div className="space-y-5">
              <div>
                <div className="mb-1.5 flex items-center justify-between text-xs">
                  <span className="font-bold text-foreground">1. {t("organizationSchema")}</span>
                  {copyButton(schemas.organizationJsonLd, "org", t("copyScript"))}
                </div>
                {codeBlock(schemas.organizationJsonLd)}
              </div>

              <div>
                <div className="mb-1.5 flex items-center justify-between text-xs">
                  <span className="font-bold text-foreground">2. {t("websiteSchema")}</span>
                  {copyButton(schemas.webSiteJsonLd, "site", t("copyScript"))}
                </div>
                {codeBlock(schemas.webSiteJsonLd)}
              </div>

              <div>
                <div className="mb-1.5 flex items-center justify-between text-xs">
                  <span className="font-bold text-foreground">3. {t("faqSchema")}</span>
                  {copyButton(schemas.faqJsonLd, "faq", t("copyScript"))}
                </div>
                <p className="mb-1.5 text-[11px] text-muted">
                  {t("faqSchemaHelp")}
                </p>
                {codeBlock(schemas.faqJsonLd, "max-h-56")}
              </div>
            </div>
          )}

          {activeTab === "llmstxt" && (
            <div className="space-y-3">
              <div className="flex items-center justify-between text-xs">
                <span className="font-bold text-foreground">{t("standardLlms")}</span>
                {copyButton(schemas.llmsTxtContent, "llms", "Copy llms.txt")}
              </div>
              <div className="rounded-xl border border-border bg-elevated p-3.5">
                <p className="text-xs leading-relaxed text-foreground">
                  {t("llmsSaveHelp")} <code className="text-foreground">{`https://${websiteName}/llms.txt`}</code>
                </p>
              </div>
              {codeBlock(schemas.llmsTxtContent, "max-h-72")}
            </div>
          )}

          {activeTab === "llmsfull" && (
            <div className="space-y-3">
              <div className="flex items-center justify-between text-xs">
                <span className="font-bold text-foreground">{t("extendedLlms")}</span>
                {copyButton(schemas.llmsFullTxtContent, "llmsfull", "Copy llms-full.txt")}
              </div>
              <div className="space-y-2 rounded-xl border border-border bg-elevated p-3.5">
                <p className="text-xs leading-relaxed text-foreground">
                  {t("llmsFullHelp")} <code className="rounded bg-surface px-1 py-0.5 text-accent">llms-full.txt</code>
                </p>
                <p className="flex items-center gap-1.5 text-[11px] text-muted">
                  <Lightbulb size={13} aria-hidden /> {t("auditInventoryHelp")}
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
