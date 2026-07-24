"use client";

import { useApp } from "@/components/providers";
import Link from "next/link";
import { useState } from "react";

export default function LoginPage() {
  const { t } = useApp();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    alert(`Phase 0 Auth Shell Mock: Logging in as ${email}`);
  };

  return (
    <div className="min-h-screen flex items-center justify-center p-4">
      <div className="w-full max-w-md p-8 rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] shadow-xl space-y-6">
        <div className="text-center space-y-2">
          <Link href="/" className="inline-block font-extrabold text-2xl tracking-tight text-blue-500">
            AkironSeo
          </Link>
          <h2 className="text-xl font-bold">{t("login")}</h2>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-semibold text-slate-400 mb-1">{t("email")}</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="user@organization.com"
              required
              className="w-full px-4 py-2.5 rounded-lg border border-[var(--border-color)] bg-[var(--bg-primary)] focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
            />
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-400 mb-1">{t("password")}</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
              className="w-full px-4 py-2.5 rounded-lg border border-[var(--border-color)] bg-[var(--bg-primary)] focus:outline-none focus:ring-2 focus:ring-blue-500 text-sm"
            />
          </div>

          <button
            type="submit"
            className="w-full py-3 rounded-lg bg-blue-600 font-bold text-white hover:bg-blue-700 transition"
          >
            {t("submitLogin")}
          </button>
        </form>

        <div className="text-center text-xs text-slate-400 pt-2 border-t border-[var(--border-color)]">
          Don't have a workspace?{" "}
          <Link href="/register" className="text-blue-500 font-semibold hover:underline">
            {t("register")}
          </Link>
        </div>
      </div>
    </div>
  );
}
