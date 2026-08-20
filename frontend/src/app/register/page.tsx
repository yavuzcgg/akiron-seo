"use client";

import { useApp } from "@/components/providers";
import { ApiError, apiClient } from "@/lib/apiClient";
import { queryKeys } from "@/lib/queryKeys";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";

export default function RegisterPage() {
  const { t } = useApp();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [tenantName, setTenantName] = useState("");
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const register = useMutation({
    mutationFn: apiClient.auth.register,
    onSuccess: (session) => {
      queryClient.setQueryData(queryKeys.session, session);
      router.replace("/dashboard");
    },
  });
  const error = register.error instanceof ApiError ? register.error : null;
  const fieldError = (field: string) => error?.fieldErrors?.[field]?.[0];

  return (
    <main id="main-content" tabIndex={-1} className="flex min-h-dvh items-center justify-center p-4">
      <div className="w-full max-w-md space-y-6 rounded-2xl border border-border bg-surface p-6 shadow-xl sm:p-8">
        <div className="space-y-2 text-center">
          <Link href="/" className="inline-block text-2xl font-extrabold tracking-tight text-primary">
            Akiron SEO
          </Link>
          <h1 className="text-xl font-bold text-foreground">{t("register")}</h1>
        </div>

        {error && (
          <div role="alert" className="rounded-lg border border-danger/20 bg-danger/10 p-3.5 text-center text-sm font-semibold text-danger">
            {error.message}
          </div>
        )}

        <form
          onSubmit={(event) => {
            event.preventDefault();
            register.mutate({ tenantName, fullName, email, password });
          }}
          className="space-y-4"
        >
          <FormField
            id="register-tenant"
            label={t("tenantName")}
            value={tenantName}
            onChange={setTenantName}
            autoComplete="organization"
            placeholder="Acme Digital Agency"
            error={fieldError("tenantName")}
          />
          <FormField
            id="register-name"
            label={t("fullName")}
            value={fullName}
            onChange={setFullName}
            autoComplete="name"
            placeholder="John Doe"
            error={fieldError("fullName")}
          />
          <FormField
            id="register-email"
            label={t("email")}
            value={email}
            onChange={setEmail}
            type="email"
            autoComplete="email"
            placeholder="admin@agency.com"
            error={fieldError("email")}
          />

          <div>
            <label htmlFor="register-password" className="mb-1 block text-sm font-semibold text-muted">
              {t("password")}
            </label>
            <input
              id="register-password"
              name="password"
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              minLength={12}
              maxLength={128}
              required
              aria-invalid={Boolean(fieldError("password"))}
              aria-describedby="password-help password-error"
              className="min-h-11 w-full rounded-lg border border-border bg-bg px-4 py-2.5 text-base text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
            />
            <p id="password-help" className="mt-1 text-xs leading-relaxed text-subtle">
              {t("passwordRequirements")}
            </p>
            {fieldError("password") && <p id="password-error" className="mt-1 text-xs text-danger">{fieldError("password")}</p>}
          </div>

          <button
            type="submit"
            disabled={register.isPending}
            className="min-h-11 w-full cursor-pointer rounded-lg bg-primary py-3 font-bold text-on-primary transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
          >
            {register.isPending ? t("registering") : t("submitRegister")}
          </button>
        </form>

        <div className="border-t border-border pt-4 text-center text-sm text-muted">
          {t("alreadyRegistered")} {" "}
          <Link href="/login" className="font-semibold text-primary hover:underline">
            {t("login")}
          </Link>
        </div>
      </div>
    </main>
  );
}

interface FormFieldProps {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  type?: "text" | "email";
  autoComplete: string;
  placeholder: string;
  error?: string;
}

function FormField({ id, label, value, onChange, type = "text", autoComplete, placeholder, error }: FormFieldProps) {
  return (
    <div>
      <label htmlFor={id} className="mb-1 block text-sm font-semibold text-muted">{label}</label>
      <input
        id={id}
        name={id}
        type={type}
        autoComplete={autoComplete}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        required
        aria-invalid={Boolean(error)}
        aria-describedby={`${id}-error`}
        className="min-h-11 w-full rounded-lg border border-border bg-bg px-4 py-2.5 text-base text-foreground placeholder:text-subtle focus:outline-none focus:ring-2 focus:ring-ring"
      />
      {error && <p id={`${id}-error`} className="mt-1 text-xs text-danger">{error}</p>}
    </div>
  );
}
