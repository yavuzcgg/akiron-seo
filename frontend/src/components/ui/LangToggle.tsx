"use client";

import { useApp } from "@/components/providers";
import { Languages } from "lucide-react";

export default function LangToggle() {
  const { lang, setLang, t } = useApp();

  return (
    <button
      onClick={() => setLang(lang === "en" ? "tr" : "en")}
      aria-label={`${t("switchLanguage")} ${lang.toUpperCase()}`}
      className="flex h-9 cursor-pointer items-center gap-1.5 rounded-lg border border-border bg-surface px-2.5 text-xs font-semibold text-muted transition-colors hover:text-foreground"
    >
      <Languages size={15} aria-hidden />
      {lang.toUpperCase()}
    </button>
  );
}
