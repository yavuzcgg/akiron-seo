"use client";

import Header from "@/components/Header";
import { useApp } from "@/components/providers";
import { FileCode2, Radar, Search, Sparkles } from "lucide-react";
import Link from "next/link";

const features = [
  {
    icon: Search,
    title: "SEO Engine",
    body: "On-page crawling with weighted, transparent scoring and actionable issues.",
  },
  {
    icon: Sparkles,
    title: "GEO Engine",
    body: "Track whether Perplexity and Gemini cite your brand, with share-of-voice sampling.",
  },
  {
    icon: FileCode2,
    title: "AEO Engine",
    body: "Generate JSON-LD schemas and llms.txt, and audit robots.txt for AI crawlers.",
  },
  {
    icon: Radar,
    title: "Gold Opportunities",
    body: "When an engine cites a 404 on your domain, that gap becomes a content brief.",
  },
];

export default function Home() {
  const { t } = useApp();

  return (
    <div className="mx-auto flex min-h-screen max-w-7xl flex-col justify-between p-4 sm:p-6">
      <Header>
        <Link
          href="/login"
          className="rounded-lg bg-primary px-4 py-2 text-sm font-semibold text-on-primary transition-colors hover:bg-primary-hover"
        >
          {t("login")}
        </Link>
      </Header>

      <main className="my-auto space-y-6 py-12 text-center">
        <h1 className="mx-auto max-w-4xl px-2 text-3xl font-black leading-tight tracking-tight text-foreground sm:text-5xl md:text-6xl">
          {t("title")}
        </h1>

        <p className="mx-auto max-w-2xl px-4 text-base text-muted sm:text-lg">{t("subtitle")}</p>

        <div className="flex flex-col justify-center gap-3 px-4 pt-2 sm:flex-row sm:gap-4">
          <Link
            href="/register"
            className="w-full rounded-lg bg-primary px-6 py-3 font-bold text-on-primary shadow-lg transition-colors hover:bg-primary-hover sm:w-auto"
          >
            {t("register")}
          </Link>
          <Link
            href="/login"
            className="w-full rounded-lg border border-border bg-surface px-6 py-3 font-bold text-foreground transition-colors hover:bg-elevated sm:w-auto"
          >
            {t("login")}
          </Link>
        </div>

        <div className="grid grid-cols-1 gap-4 px-2 pt-10 text-left sm:grid-cols-2 sm:gap-6 lg:grid-cols-4">
          {features.map(({ icon: Icon, title, body }) => (
            <div
              key={title}
              className="rounded-xl border border-border bg-surface p-5 shadow-sm transition-colors hover:border-primary/40 sm:p-6"
            >
              <Icon className="mb-3 text-primary" size={24} aria-hidden />
              <h3 className="mb-1 text-base font-bold text-foreground sm:text-lg">{title}</h3>
              <p className="text-xs text-muted sm:text-sm">{body}</p>
            </div>
          ))}
        </div>
      </main>

      <footer className="border-t border-border py-4 text-center text-xs text-subtle">
        {t("rightsReserved")}
      </footer>
    </div>
  );
}
