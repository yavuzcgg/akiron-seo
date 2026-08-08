"use client";

import { useApp } from "@/components/providers";
import { apiClient } from "@/lib/apiClient";
import { getErrorMessage } from "@/lib/errors";
import { saveSession } from "@/lib/session";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";

export default function RegisterPage() {
  const { t } = useApp();
  const router = useRouter();

  const [tenantName, setTenantName] = useState("");
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setMessage(null);
    setError(null);

    try {
      const data = await apiClient.auth.register({ tenantName, fullName, email, password });

      if (data.success && data.accessToken) {
        saveSession({
          accessToken: data.accessToken,
          tenantId: data.tenantId ?? "",
          role: data.role ?? "Owner",
        });

        setMessage("Account created! Redirecting to dashboard...");
        setTimeout(() => {
          router.push("/dashboard");
        }, 800);
      } else {
        setError(data.message || "Failed to register organization.");
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Could not connect to server. Please try again."));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center p-4">
      <div className="w-full max-w-md space-y-6 rounded-2xl border border-border bg-surface p-6 shadow-xl sm:p-8">
        <div className="space-y-2 text-center">
          <Link href="/" className="inline-block text-2xl font-extrabold tracking-tight text-primary">
            Akiron SEO
          </Link>
          <h2 className="text-xl font-bold text-foreground">{t("register")}</h2>
        </div>

        {message && (
          <div className="rounded-lg border border-success/20 bg-success/10 p-3.5 text-center text-xs font-semibold text-success">
            {message}
          </div>
        )}

        {error && (
          <div className="rounded-lg border border-danger/20 bg-danger/10 p-3.5 text-center text-xs font-semibold text-danger">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="mb-1 block text-xs font-semibold text-muted">{t("tenantName")}</label>
            <input
              type="text"
              value={tenantName}
              onChange={(e) => setTenantName(e.target.value)}
              placeholder="Acme Digital Agency"
              required
              className="w-full rounded-lg border border-border bg-bg px-4 py-2.5 text-sm text-foreground placeholder:text-subtle focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-semibold text-muted">{t("fullName")}</label>
            <input
              type="text"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              placeholder="John Doe"
              required
              className="w-full rounded-lg border border-border bg-bg px-4 py-2.5 text-sm text-foreground placeholder:text-subtle focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-semibold text-muted">{t("email")}</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="admin@agency.com"
              required
              className="w-full rounded-lg border border-border bg-bg px-4 py-2.5 text-sm text-foreground placeholder:text-subtle focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>

          <div>
            <label className="mb-1 block text-xs font-semibold text-muted">{t("password")}</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
              className="w-full rounded-lg border border-border bg-bg px-4 py-2.5 text-sm text-foreground placeholder:text-subtle focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full cursor-pointer rounded-lg bg-primary py-3 font-bold text-on-primary transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
          >
            {loading ? "Registering..." : t("submitRegister")}
          </button>
        </form>

        <div className="border-t border-border pt-2 text-center text-xs text-muted">
          Already registered?{" "}
          <Link href="/login" className="font-semibold text-primary hover:underline">
            {t("login")}
          </Link>
        </div>
      </div>
    </div>
  );
}
