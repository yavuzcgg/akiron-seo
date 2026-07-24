"use client";

import { useApp } from "@/components/providers";
import Link from "next/link";

export default function Home() {
  const { theme, toggleTheme, lang, setLang, t } = useApp();

  return (
    <div className="min-h-screen flex flex-col justify-between p-4 sm:p-6 max-w-7xl mx-auto">
      {/* Header Bar */}
      <header className="flex flex-wrap items-center justify-between gap-4 py-2 border-b border-[var(--border-color)]">
        <Link href="/" className="flex items-center space-x-2.5">
          <div className="w-9 h-9 bg-blue-600 rounded-lg flex items-center justify-center font-extrabold text-white text-xl shadow-md">
            A
          </div>
          <span className="font-extrabold text-xl tracking-tight">Akiron SEO</span>
        </Link>

        <div className="flex items-center space-x-2 sm:space-x-3 text-sm font-medium">
          {/* Language Switcher */}
          <button
            onClick={() => setLang(lang === "en" ? "tr" : "en")}
            className="px-3 py-1.5 rounded-lg border border-[var(--border-color)] bg-[var(--card-bg)] hover:opacity-80 transition cursor-pointer"
            aria-label="Switch Language"
          >
            🌐 {lang.toUpperCase()}
          </button>

          {/* Theme Switcher */}
          <button
            onClick={toggleTheme}
            className="px-3 py-1.5 rounded-lg border border-[var(--border-color)] bg-[var(--card-bg)] hover:opacity-80 transition cursor-pointer"
            aria-label="Toggle Theme"
          >
            {theme === "dark" ? "☀️ Light" : "🌙 Dark"}
          </button>

          <Link
            href="/login"
            className="px-4 py-2 rounded-lg bg-blue-600 text-white font-semibold hover:bg-blue-700 transition"
          >
            {t("login")}
          </Link>
        </div>
      </header>

      {/* Hero Section */}
      <main className="my-auto py-12 text-center space-y-6">
        <h1 className="text-3xl sm:text-5xl md:text-6xl font-black tracking-tight leading-tight max-w-4xl mx-auto px-2">
          {t("title")}
        </h1>

        <p className="text-base sm:text-lg text-slate-400 max-w-2xl mx-auto px-4">
          {t("subtitle")}
        </p>

        <div className="flex flex-col sm:flex-row justify-center gap-3 sm:gap-4 px-4 pt-2">
          <Link
            href="/register"
            className="w-full sm:w-auto px-6 py-3 rounded-lg bg-blue-600 text-white font-bold hover:bg-blue-700 transition shadow-lg shadow-blue-600/20"
          >
            {t("register")}
          </Link>
          <Link
            href="/login"
            className="w-full sm:w-auto px-6 py-3 rounded-lg border border-[var(--border-color)] bg-[var(--card-bg)] font-bold hover:opacity-80 transition"
          >
            {t("login")}
          </Link>
        </div>

        {/* Feature Highlights Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 sm:gap-6 pt-10 text-left px-2">
          <div className="p-5 sm:p-6 rounded-xl border border-[var(--border-color)] bg-[var(--card-bg)] shadow-sm">
            <div className="text-2xl mb-2">🔍</div>
            <h3 className="font-bold text-base sm:text-lg mb-1">SEO Engine</h3>
            <p className="text-xs sm:text-sm text-slate-400">Classical on-page crawling, PageSpeed & meta analysis.</p>
          </div>
          <div className="p-5 sm:p-6 rounded-xl border border-[var(--border-color)] bg-[var(--card-bg)] shadow-sm">
            <div className="text-2xl mb-2">🤖</div>
            <h3 className="font-bold text-base sm:text-lg mb-1">AIO Engine</h3>
            <p className="text-xs sm:text-sm text-slate-400">Google AI Overviews & SGE optimization insights.</p>
          </div>
          <div className="p-5 sm:p-6 rounded-xl border border-[var(--border-color)] bg-[var(--card-bg)] shadow-sm">
            <div className="text-2xl mb-2">⚡</div>
            <h3 className="font-bold text-base sm:text-lg mb-1">GEO Engine</h3>
            <p className="text-xs sm:text-sm text-slate-400">Perplexity, ChatGPT & Gemini native citation tracking & sampling.</p>
          </div>
          <div className="p-5 sm:p-6 rounded-xl border border-[var(--border-color)] bg-[var(--card-bg)] shadow-sm">
            <div className="text-2xl mb-2">💬</div>
            <h3 className="font-bold text-base sm:text-lg mb-1">AEO Engine</h3>
            <p className="text-xs sm:text-sm text-slate-400">Automated JSON-LD schemas, robots.txt AI auditor & llms.txt.</p>
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer className="py-4 border-t border-[var(--border-color)] text-center text-xs text-slate-500">
        {t("rightsReserved")}
      </footer>
    </div>
  );
}
