"use client";

import { useApp } from "@/components/providers";
import Link from "next/link";
import { useState } from "react";

export default function LoginPage() {
  const { t } = useApp();
  const [email, setEmail] = useState("admin@akironseo.com");
  const [password, setPassword] = useState("Admin123!");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setMessage(null);
    setError(null);

    try {
      const res = await fetch("http://localhost:5248/api/v1/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ email, password }),
      });

      const data = await res.json();

      if (res.ok && data.success) {
        setMessage(`Success! Signed in as ${data.userEmail} (Role: ${data.role}). Workspace TenantId: ${data.tenantId}`);
      } else {
        setError(data.message || "Failed to sign in.");
      }
    } catch (err: any) {
      setError("Could not connect to API server at http://localhost:5248");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center p-4">
      <div className="w-full max-w-md p-6 sm:p-8 rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] shadow-xl space-y-6">
        <div className="text-center space-y-2">
          <Link href="/" className="inline-block font-extrabold text-2xl tracking-tight text-blue-500">
            Akiron SEO
          </Link>
          <h2 className="text-xl font-bold">{t("login")}</h2>
        </div>

        {message && (
          <div className="p-3 rounded-lg bg-green-500/10 border border-green-500/20 text-green-400 text-xs font-semibold">
            {message}
          </div>
        )}

        {error && (
          <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/20 text-red-400 text-xs font-semibold">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-semibold text-slate-400 mb-1">{t("email")}</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="admin@akironseo.com"
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
            disabled={loading}
            className="w-full py-3 rounded-lg bg-blue-600 font-bold text-white hover:bg-blue-700 transition disabled:opacity-50"
          >
            {loading ? "Signing In..." : t("submitLogin")}
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
