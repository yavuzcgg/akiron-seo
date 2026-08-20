"use client";

import AuthGuard from "@/components/AuthGuard";
import Header from "@/components/Header";
import { useApp } from "@/components/providers";
import Modal from "@/components/ui/Modal";
import { AdminTenantDto, AdminUsageLogDto, apiClient } from "@/lib/apiClient";
import { getErrorMessage } from "@/lib/errors";
import { queryKeys } from "@/lib/queryKeys";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Building2, ScrollText, Trash2, X, Zap } from "lucide-react";
import Link from "next/link";
import { useState } from "react";

export default function AdminDashboardPage() {
  return (
    <AuthGuard requireSuperAdmin>
      <AdminDashboardContent />
    </AuthGuard>
  );
}

function AdminDashboardContent() {
  const { t } = useApp();
  const queryClient = useQueryClient();
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Quota Adjust Modal State
  const [selectedTenant, setSelectedTenant] = useState<AdminTenantDto | null>(null);
  const [newLimit, setNewLimit] = useState<number>(1000000);
  const [resetUsage, setResetUsage] = useState(false);
  const tenantsQuery = useQuery<AdminTenantDto[]>({ queryKey: queryKeys.adminTenants, queryFn: apiClient.admin.getTenants });
  const usageQuery = useQuery<AdminUsageLogDto[]>({ queryKey: queryKeys.adminUsage, queryFn: apiClient.admin.getUsageLogs });
  const tenants = tenantsQuery.data ?? [];
  const usageLogs = usageQuery.data ?? [];
  const refreshAdminData = () => Promise.all([
    queryClient.invalidateQueries({ queryKey: queryKeys.adminTenants }),
    queryClient.invalidateQueries({ queryKey: queryKeys.adminUsage }),
  ]);

  const quotaMutation = useMutation({
    mutationFn: ({ tenantId, limit, reset }: { tenantId: string; limit: number; reset: boolean }) => apiClient.admin.updateQuota(tenantId, { newMonthlyLimitTokens: limit, resetUsedTokens: reset }),
    onSuccess: async (_, variables) => {
      const tenant = tenants.find((item) => item.tenantId === variables.tenantId);
      setMessage(`${t("quotaUpdated")} ${tenant?.tenantName ?? variables.tenantId}.`);
      setSelectedTenant(null);
      await refreshAdminData();
    },
    onError: (err) => setError(getErrorMessage(err, t("quotaUpdateFailed"))),
  });
  const statusMutation = useMutation({
    mutationFn: ({ tenantId }: { tenantId: string; tenantName: string }) => apiClient.admin.toggleStatus(tenantId),
    onSuccess: async (result, variables) => {
      setMessage(`${t("tenantStatusChanged")}: ${variables.tenantName} — ${result.isActive ? t("active") : t("disabled")}.`);
      await refreshAdminData();
    },
    onError: (err) => setError(getErrorMessage(err, t("statusUpdateFailed"))),
  });
  const pruneMutation = useMutation({
    mutationFn: () => apiClient.admin.pruneLogs(30),
    onSuccess: async (result) => {
      setMessage(`${t("cleanupComplete")} ${result.prunedRecordsCount}.`);
      await refreshAdminData();
    },
    onError: (err) => setError(getErrorMessage(err, t("pruneFailed"))),
  });

  const handleUpdateQuota = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedTenant) return;

    setMessage(null);
    setError(null);
    quotaMutation.mutate({ tenantId: selectedTenant.tenantId, limit: newLimit, reset: resetUsage });
  };

  const handleToggleStatus = async (tenantId: string, tenantName: string) => {
    setMessage(null);
    setError(null);
    statusMutation.mutate({ tenantId, tenantName });
  };

  const handlePruneLogs = async () => {
    setMessage(null);
    setError(null);
    pruneMutation.mutate();
  };

  // KPI Calculations
  const totalTenants = tenants.length;
  const totalWebsites = tenants.reduce((acc, t) => acc + t.registeredWebsitesCount, 0);
  const totalTokens = tenants.reduce((acc, t) => acc + t.usedTokens, 0);
  const totalCostUsd = usageLogs.reduce((acc, l) => acc + l.estimatedCostUsd, 0);

  return (
    <div className="mx-auto flex min-h-dvh max-w-7xl flex-col justify-between space-y-6 p-4 sm:p-6">
      <Header label={t("adminPanel")}>
        <Link
          href="/dashboard"
          className="flex h-9 items-center gap-1.5 rounded-lg border border-border px-3 text-xs font-semibold text-muted transition-colors hover:text-foreground"
        >
          <ArrowLeft size={15} aria-hidden /> {t("backToDashboard")}
        </Link>
      </Header>

      {/* Main Content */}
      <main id="main-content" tabIndex={-1} className="space-y-6">
        {/* Status Alerts */}
        {message && (
          <div className="flex animate-fadeIn items-center justify-between rounded-xl border border-success/20 bg-success/10 p-4 text-sm font-semibold text-success">
            <span>{message}</span>
            <button onClick={() => setMessage(null)} aria-label={t("dismissAlert")} className="cursor-pointer rounded p-0.5 text-muted hover:text-foreground"><X size={14} aria-hidden /></button>
          </div>
        )}
        {error && (
          <div className="flex animate-fadeIn items-center justify-between rounded-xl border border-danger/20 bg-danger/10 p-4 text-sm font-semibold text-danger">
            <span>{error}</span>
            <button onClick={() => setError(null)} aria-label={t("dismissAlert")} className="cursor-pointer rounded p-0.5 text-muted hover:text-foreground"><X size={14} aria-hidden /></button>
          </div>
        )}

        {/* System Overview KPI Cards */}
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
          <div className="space-y-1 rounded-2xl border border-border bg-surface p-5">
            <span className="text-xs font-semibold uppercase text-muted">{t("totalTenants")}</span>
            <div className="text-3xl font-extrabold text-foreground">{totalTenants}</div>
            <span className="text-[11px] text-subtle">{t("b2bAccounts")}</span>
          </div>

          <div className="space-y-1 rounded-2xl border border-border bg-surface p-5">
            <span className="text-xs font-semibold uppercase text-muted">{t("registeredSites")}</span>
            <div className="text-3xl font-extrabold text-primary">{totalWebsites}</div>
            <span className="text-[11px] text-subtle">{t("monitoredDomains")}</span>
          </div>

          <div className="space-y-1 rounded-2xl border border-border bg-surface p-5">
            <span className="text-xs font-semibold uppercase text-muted">{t("totalTokensSpent")}</span>
            <div className="text-3xl font-extrabold text-accent">{totalTokens.toLocaleString()}</div>
            <span className="text-[11px] text-subtle">{t("llmTokensConsumed")}</span>
          </div>

          <div className="space-y-1 rounded-2xl border border-border bg-surface p-5">
            <span className="text-xs font-semibold uppercase text-muted">{t("estimatedApiCost")}</span>
            <div className="text-3xl font-extrabold text-success">${totalCostUsd.toFixed(4)}</div>
            <span className="text-[11px] text-subtle">{t("usdProviderCharges")}</span>
          </div>
        </div>

        {/* Tenant B2B Management Table */}
        <div className="space-y-4 rounded-2xl border border-border bg-surface p-6">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2 className="flex items-center gap-2 text-lg font-bold text-foreground">
                <Building2 size={18} className="text-primary" aria-hidden /> {t("tenantManagement")}
              </h2>
              <p className="text-xs text-muted">{t("tenantManagementDescription")}</p>
            </div>

            <button
              onClick={handlePruneLogs}
              className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3.5 py-1.5 text-xs font-semibold text-muted transition-colors hover:text-foreground"
            >
              <Trash2 size={14} aria-hidden /> {t("pruneLogs")}
            </button>
          </div>

          {tenantsQuery.isPending ? (
            <div className="py-8 text-center text-xs text-muted">{t("loadingTenants")}</div>
          ) : tenantsQuery.isError ? (
            <div className="rounded-lg border border-danger/20 bg-danger/10 p-4 text-xs text-danger" role="alert">{getErrorMessage(tenantsQuery.error, t("tenantLoadFailed"))}</div>
          ) : tenants.length === 0 ? (
            <div className="py-8 text-center text-xs text-muted">{t("noTenants")}</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead>
                  <tr className="border-b border-border text-[10px] font-semibold uppercase text-muted">
                    <th className="px-2 pb-3">{t("tenantNameHeader")}</th>
                    <th className="px-2 pb-3">{t("plan")}</th>
                    <th className="px-2 pb-3">{t("tokenUsageQuota")}</th>
                    <th className="px-2 pb-3 text-center">{t("sites")}</th>
                    <th className="px-2 pb-3 text-center">{t("status")}</th>
                    <th className="px-2 pb-3 text-right">{t("actions")}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border font-medium">
                  {tenants.map((tenant) => {
                    const usagePct = tenant.monthlyLimitTokens > 0
                      ? Math.min((tenant.usedTokens / tenant.monthlyLimitTokens) * 100, 100)
                      : 0;

                    return (
                      <tr key={tenant.tenantId} className="transition-colors hover:bg-elevated">
                        <td className="px-2 py-3">
                          <div className="font-bold text-foreground">{tenant.tenantName}</div>
                          <div className="font-mono text-[10px] text-subtle">{tenant.slug}</div>
                        </td>

                        <td className="py-3 px-2">
                          <span className="rounded border border-primary/30 bg-primary/15 px-2 py-0.5 text-[10px] font-bold text-primary">
                            {tenant.planName}
                          </span>
                        </td>

                        <td className="py-3 px-2 min-w-[200px]">
                          <div className="space-y-1">
                            <div className="flex justify-between text-[10px] font-mono">
                              <span className="text-foreground">{tenant.usedTokens.toLocaleString()}</span>
                              <span className="text-subtle">/ {tenant.monthlyLimitTokens.toLocaleString()}</span>
                            </div>
                            <div className="h-1.5 w-full overflow-hidden rounded-full bg-elevated">
                              <div
                                className={`h-full rounded-full transition-[width] motion-reduce:transition-none ${usagePct >= 90 ? "bg-danger" : usagePct >= 60 ? "bg-warning" : "bg-primary"}`}
                                style={{ width: `${usagePct}%` }}
                              />
                            </div>
                          </div>
                        </td>

                        <td className="px-2 py-3 text-center font-bold text-foreground">
                          {tenant.registeredWebsitesCount}
                        </td>

                        <td className="px-2 py-3 text-center">
                          <span className={`rounded-full border px-2 py-0.5 text-[10px] font-bold ${tenant.isActive ? "border-success/20 bg-success/10 text-success" : "border-danger/20 bg-danger/10 text-danger"}`}>
                            {tenant.isActive ? t("active") : t("disabled")}
                          </span>
                        </td>

                        <td className="space-x-2 px-2 py-3 text-right">
                          <button
                            onClick={() => {
                              setSelectedTenant(tenant);
                              setNewLimit(tenant.monthlyLimitTokens);
                              setResetUsage(false);
                            }}
                            className="cursor-pointer rounded bg-primary/15 px-2.5 py-1 text-[11px] font-semibold text-primary transition-colors hover:bg-primary/25"
                          >
                            {t("quota")}
                          </button>

                          <button
                            onClick={() => handleToggleStatus(tenant.tenantId, tenant.tenantName)}
                            className={`cursor-pointer rounded px-2.5 py-1 text-[11px] font-semibold transition-colors ${tenant.isActive ? "bg-danger/15 text-danger hover:bg-danger/25" : "bg-success/15 text-success hover:bg-success/25"}`}
                          >
                            {tenant.isActive ? t("disable") : t("enable")}
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>

        {/* API Usage & Token Audit Logs Table */}
        <div className="space-y-4 rounded-2xl border border-border bg-surface p-6">
          <div>
            <h2 className="flex items-center gap-2 text-lg font-bold text-foreground">
              <ScrollText size={18} className="text-primary" aria-hidden /> {t("usageAudit")}
            </h2>
            <p className="text-xs text-muted">{t("usageAuditDescription")}</p>
          </div>

          {usageQuery.isPending ? (
            <div className="py-8 text-center text-xs text-muted" role="status">{t("loadingUsage")}</div>
          ) : usageQuery.isError ? (
            <div className="rounded-lg border border-danger/20 bg-danger/10 p-4 text-xs text-danger" role="alert">{getErrorMessage(usageQuery.error, t("usageLoadFailed"))}</div>
          ) : usageLogs.length === 0 ? (
            <div className="py-8 text-center text-xs text-muted">{t("noUsage")}</div>
          ) : (
            <div className="max-h-80 overflow-x-auto">
              <table className="w-full text-left font-mono text-xs">
                <thead>
                  <tr className="border-b border-border text-[10px] font-semibold uppercase text-muted">
                    <th className="px-2 pb-3">{t("timestamp")}</th>
                    <th className="px-2 pb-3">{t("tenant")}</th>
                    <th className="px-2 pb-3">{t("service")}</th>
                    <th className="px-2 pb-3 text-right">{t("tokens")}</th>
                    <th className="px-2 pb-3 text-right">{t("estimatedCost")}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border">
                  {usageLogs.map((log) => (
                    <tr key={log.logId} className="transition-colors hover:bg-elevated">
                      <td className="px-2 py-2.5 text-muted">
                        {new Date(log.timestamp).toLocaleString()}
                      </td>
                      <td className="px-2 py-2.5 font-bold text-foreground">
                        {log.tenantName}
                      </td>
                      <td className="px-2 py-2.5 text-accent">
                        {log.serviceName}
                      </td>
                      <td className="px-2 py-2.5 text-right font-bold text-foreground">
                        {log.tokensUsed.toLocaleString()}
                      </td>
                      <td className="px-2 py-2.5 text-right font-bold text-success">
                        ${log.estimatedCostUsd.toFixed(5)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </main>

      {/* Quota Adjust Modal */}
      {selectedTenant && (
        <Modal
          onClose={() => setSelectedTenant(null)}
          title={t("adjustQuota")}
          icon={<Zap size={18} aria-hidden />}
          subtitle={`${t("updatingQuota")} ${selectedTenant.tenantName}`}
          maxWidthClass="max-w-md"
          footer={
            <>
              <button
                type="button"
                onClick={() => setSelectedTenant(null)}
                className="cursor-pointer rounded-lg bg-elevated px-4 py-2 text-xs font-bold text-foreground transition-colors hover:opacity-80"
              >
                {t("cancel")}
              </button>
              <button
                type="submit"
                form="quota-form"
                disabled={quotaMutation.isPending}
                className="cursor-pointer rounded-lg bg-primary px-4 py-2 text-xs font-bold text-on-primary transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
              >
                {quotaMutation.isPending ? t("saving") : t("saveQuota")}
              </button>
            </>
          }
        >
          <form id="quota-form" onSubmit={handleUpdateQuota} className="space-y-4">
            <div>
              <label htmlFor="monthly-token-limit" className="mb-1 block text-xs font-semibold text-muted">{t("newMonthlyLimit")}</label>
              <input
                id="monthly-token-limit"
                type="number"
                value={newLimit}
                onChange={(e) => setNewLimit(parseInt(e.target.value) || 0)}
                required
                min={0}
                max={1000000000}
                step={50000}
                className="w-full rounded-lg border border-border bg-bg px-3 py-2 font-mono text-sm text-foreground focus:outline-none focus:ring-2 focus:ring-ring"
              />
            </div>

            <label className="flex cursor-pointer items-center gap-2">
              <input
                type="checkbox"
                checked={resetUsage}
                onChange={(e) => setResetUsage(e.target.checked)}
                className="h-4 w-4 cursor-pointer accent-primary"
              />
              <span className="text-xs font-medium text-muted">{t("resetUsage")}</span>
            </label>
          </form>
        </Modal>
      )}

      {/* Footer */}
      <footer className="border-t border-border py-4 text-center text-xs text-subtle">
        {t("adminFooter")}
      </footer>
    </div>
  );
}
