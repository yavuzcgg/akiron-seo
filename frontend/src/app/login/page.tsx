"use client";

import { useApp } from "@/components/providers";
import { ApiError, apiClient } from "@/lib/apiClient";
import { queryKeys } from "@/lib/queryKeys";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";

export default function LoginPage() {
  const { t } = useApp();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const login = useMutation({
    mutationFn: apiClient.auth.login,
    onSuccess: (session) => {
      queryClient.setQueryData(queryKeys.session, session);
      router.replace("/dashboard");
    },
  });

  const error = login.error instanceof ApiError ? login.error : null;

  return (
    <main id="main-content" tabIndex={-1} className="flex min-h-dvh items-center justify-center p-4">
      <div className="w-full max-w-md space-y-6 rounded-2xl border border-border bg-surface p-6 shadow-xl sm:p-8">
        <div className="space-y-2 text-center">
          <Link href="/" className="inline-block text-2xl font-extrabold tracking-tight text-primary">
            Akiron SEO
          </Link>
          <h1 className="text-xl font-bold text-foreground">{t("login")}</h1>
        </div>

        {error && (
          <div role="alert" className="rounded-lg border border-danger/20 bg-danger/10 p-3.5 text-center text-sm font-semibold text-danger">
            {error.message}
          </div>
        )}

        <form
          onSubmit={(event) => {
            event.preventDefault();
            login.mutate({ email, password });
          }}
          className="space-y-4"
        >
          <div>
            <label htmlFor="login-email" className="mb-1 block text-sm font-semibold text-muted">
              {t("email")}
            </label>
            <input
              id="login-email"
              name="email"
              type="email"
              autoComplete="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              placeholder="admin@akironseo.com"
              required
              className="min-h-11 w-full rounded-lg border border-border bg-bg px-4 py-2.5 text-base text-foreground placeholder:text-subtle focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>

          <div>
            <label htmlFor="login-password" className="mb-1 block text-sm font-semibold text-muted">
              {t("password")}
            </label>
            <input
              id="login-password"
              name="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              required
              className="min-h-11 w-full rounded-lg border border-border bg-bg px-4 py-2.5 text-base text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            />
          </div>

          <button
            type="submit"
            disabled={login.isPending}
            className="min-h-11 w-full cursor-pointer rounded-lg bg-primary py-3 font-bold text-on-primary transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
          >
            {login.isPending ? t("signingIn") : t("submitLogin")}
          </button>
        </form>

        <div className="border-t border-border pt-4 text-center text-sm text-muted">
          {t("noWorkspace")} {" "}
          <Link href="/register" className="font-semibold text-primary hover:underline">
            {t("register")}
          </Link>
        </div>
      </div>
    </main>
  );
}
