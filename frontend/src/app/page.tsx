"use client";

import { useApp } from "@/components/providers";
import Link from "next/link";

export default function Home() {
  const { theme, toggleTheme, lang, setLang, t } = useApp();

  return (
    <div className="min-h-screen flex flex-col justify-between p-8 max-w-6xl mx-auto">
      {/* Header Bar */}
      <header className="flex justify-between items-center py-4 border-b border-[var(--border-color)]">
        <div className="flex items-center space-x-3">
          <div className="w-9 h-9 bg-blue-600 rounded-lg flex items-center justify-center font-bold text-white text-xl">
            A
          </div>
          <span className="font-extrabold text-xl tracking-tight">AkironSeo</span>
        </div>

        <div className="flex items-center space-x-4 text-sm font-medium">
          {/* Language Switcher */}
          <button
            onClick={() => setLang(lang === "en" ? "tr" : "en")}
            className="px-3 py-1.5 rounded-md border border-[var(--border-color)] bg-[var(--card-bg)] hover:opacity-80 transition"
          >
            🌐 {lang.toUpperCase()}
          </button>

          {/* Theme Switcher */}
          <button
            onClick={toggleTheme}
            className="px-3 py-1.5 rounded-md border border-[var(--border-color)] bg-[var(--card-bg)] hover:opacity-80 transition"
          >
            {theme === "dark" ? "☀️ Light" : "🌙 Dark"}
          </button>

          <Link
            href="/login"
            className="px-4 py-2 rounded-md bg-blue-600 text-white hover:bg-blue-700 transition"
          >
            {t("login")}
          </Link>
        </div>
      </header>

      {/* Hero Section */}
      <main className="my-auto py-16 text-center space-y-8">
        <div className="inline-block px-4 py-1.5 rounded-full bg-blue-500/10 border border-blue-500/20 text-blue-500 text-sm font-semibold">
          {t("verifiedIsolation")}
        </div>

        <h1 className="text-4xl sm:text-6xl font-black tracking-tight leading-tight max-w-4xl mx-auto">
          {t("title")}
        </h1>

        <p className="text-lg text-slate-400 max-w-2xl mx-auto">
          {t("subtitle")}
        </p>

        <div className="flex justify-center space-x-4">
          <Link
            href="/register"
            className="px-6 py-3 rounded-lg bg-blue-600 text-white font-bold hover:bg-blue-700 transition shadow-lg shadow-blue-600/20"
          >
            {t("register")}
          </Link>
          <Link
            href="/login"
            className="px-6 py-3 rounded-lg border border-[var(--border-color)] bg-[var(--card-bg)] font-bold hover:opacity-80 transition"
          >
            {t("login")}
          </Link>
        </div>

        {/* Feature Highlights Grid */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6 pt-12 text-left">
          <div className="p-6 rounded-xl border border-[var(--border-color)] bg-[var(--card-bg)]">
            <div className="text-2xl mb-2">🔍</div>
            <h3 className="font-bold text-lg mb-1">SEO Engine</h3>
            <p className="text-xs text-slate-400">Classical on-page crawling, PageSpeed & meta analysis.</p>
          </div>
          <div className="p-6 rounded-xl border border-[var(--border-color)] bg-[var(--card-bg)]">
            <div className="text-2xl mb-2">🤖</div>
            <h3 className="font-bold text-lg mb-1">AIO Engine</h3>
            <p className="text-xs text-slate-400">Google AI Overviews & SGE optimization insights.</p>
          </div>
          <div className="p-6 rounded-xl border border-[var(--border-color)] bg-[var(--card-bg)]">
            <div className="text-2xl mb-2">⚡</div>
            <h3 className="font-bold text-lg mb-1">GEO Engine</h3>
            <p className="text-xs text-slate-400">Perplexity, ChatGPT & Gemini native citation tracking & sampling.</p>
          </div>
          <div className="p-6 rounded-xl border border-[var(--border-color)] bg-[var(--card-bg)]">
            <div className="text-2xl mb-2">💬</div>
            <h3 className="font-bold text-lg mb-1">AEO Engine</h3>
            <p className="text-xs text-slate-400">Automated JSON-LD schemas, robots.txt AI auditor & llms.txt.</p>
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer className="py-6 border-t border-[var(--border-color)] text-center text-xs text-slate-500">
        © 2026 AkironSeo Inc. Built with .NET 10 LTS & Next.js 16 App Router.
      </footer>
    </div>
  );
}
