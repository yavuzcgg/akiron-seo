"use client";

import AuthGuard from "@/components/AuthGuard";
import Header from "@/components/Header";
import Modal from "@/components/ui/Modal";
import { AdminTenantDto, AdminUsageLogDto, apiClient } from "@/lib/apiClient";
import { getErrorMessage } from "@/lib/errors";
import { ArrowLeft, Building2, ScrollText, Trash2, X, Zap } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";

export default function AdminDashboardPage() {
  return (
    <AuthGuard requireSuperAdmin>
      <AdminDashboardContent />
    </AuthGuard>
  );
}

function AdminDashboardContent() {
  const [tenants, setTenants] = useState<AdminTenantDto[]>([]);
  const [usageLogs, setUsageLogs] = useState<AdminUsageLogDto[]>([]);
  const [loading, setLoading] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Quota Adjust Modal State
  const [selectedTenant, setSelectedTenant] = useState<AdminTenantDto | null>(null);
  const [newLimit, setNewLimit] = useState<number>(1000000);
  const [resetUsage, setResetUsage] = useState(false);
  const [submittingQuota, setSubmittingQuota] = useState(false);

  const fetchData = async () => {
    setLoading(true);
    try {
      const [tenantsData, logsData] = await Promise.all([
        apiClient.admin.getTenants(),
        apiClient.admin.getUsageLogs(),
      ]);
      setTenants(tenantsData);
      setUsageLogs(logsData);
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Failed to fetch admin data."));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleUpdateQuota = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedTenant) return;

    setSubmittingQuota(true);
    setMessage(null);
    setError(null);
    try {
      const res = await apiClient.admin.updateQuota(selectedTenant.tenantId, {
        newMonthlyLimitTokens: newLimit,
        resetUsedTokens: resetUsage,
      });

      if (res.success) {
        setMessage(`Quota updated for tenant '${selectedTenant.tenantName}'!`);
        setSelectedTenant(null);
        fetchData();
      }
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Failed to update quota."));
    } finally {
      setSubmittingQuota(false);
    }
  };

  const handleToggleStatus = async (tenantId: string, tenantName: string) => {
    setMessage(null);
    setError(null);
    try {
      const res = await apiClient.admin.toggleStatus(tenantId);
      setMessage(`Tenant '${tenantName}' status changed to ${res.isActive ? "Active" : "Disabled"}.`);
      fetchData();
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Failed to toggle tenant status."));
    }
  };

  const handlePruneLogs = async () => {
    setMessage(null);
    setError(null);
    try {
      const res = await apiClient.admin.pruneLogs(30);
      setMessage(`System cleanup complete! Pruned ${res.prunedRecordsCount} expired log records.`);
      fetchData();
    } catch (err: unknown) {
      setError(getErrorMessage(err, "Failed to prune logs."));
    }
  };

  // KPI Calculations
  const totalTenants = tenants.length;
  const totalWebsites = tenants.reduce((acc, t) => acc + t.registeredWebsitesCount, 0);
  const totalTokens = tenants.reduce((acc, t) => acc + t.usedTokens, 0);
  const totalCostUsd = usageLogs.reduce((acc, l) => acc + l.estimatedCostUsd, 0);

  return (
    <div className="mx-auto flex min-h-screen max-w-7xl flex-col justify-between space-y-6 p-4 sm:p-6">
      <Header label="SuperAdmin Panel">
        <Link
          href="/dashboard"
          className="flex h-9 items-center gap-1.5 rounded-lg border border-border px-3 text-xs font-semibold text-muted transition-colors hover:text-foreground"
        >
          <ArrowLeft size={15} aria-hidden /> Dashboard
        </Link>
      </Header>

      {/* Main Content */}
      <main className="space-y-6">
        {/* Status Alerts */}
        {message && (
          <div className="flex animate-fadeIn items-center justify-between rounded-xl border border-success/20 bg-success/10 p-4 text-sm font-semibold text-success">
            <span>{message}</span>
            <button onClick={() => setMessage(null)} aria-label="Dismiss" className="cursor-pointer rounded p-0.5 text-muted hover:text-foreground"><X size={14} aria-hidden /></button>
          </div>
        )}
        {error && (
          <div className="flex animate-fadeIn items-center justify-between rounded-xl border border-danger/20 bg-danger/10 p-4 text-sm font-semibold text-danger">
            <span>{error}</span>
            <button onClick={() => setError(null)} aria-label="Dismiss" className="cursor-pointer rounded p-0.5 text-muted hover:text-foreground"><X size={14} aria-hidden /></button>
          </div>
        )}

        {/* System Overview KPI Cards */}
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
          <div className="space-y-1 rounded-2xl border border-border bg-surface p-5">
            <span className="text-xs font-semibold uppercase text-muted">Total Tenants</span>
            <div className="text-3xl font-extrabold text-foreground">{totalTenants}</div>
            <span className="text-[11px] text-subtle">B2B Accounts</span>
          </div>

          <div className="space-y-1 rounded-2xl border border-border bg-surface p-5">
            <span className="text-xs font-semibold uppercase text-muted">Registered Sites</span>
            <div className="text-3xl font-extrabold text-primary">{totalWebsites}</div>
            <span className="text-[11px] text-subtle">Monitored Domains</span>
          </div>

          <div className="space-y-1 rounded-2xl border border-border bg-surface p-5">
            <span className="text-xs font-semibold uppercase text-muted">Total Tokens Spent</span>
            <div className="text-3xl font-extrabold text-accent">{totalTokens.toLocaleString()}</div>
            <span className="text-[11px] text-subtle">LLM Tokens Consumed</span>
          </div>

          <div className="space-y-1 rounded-2xl border border-border bg-surface p-5">
            <span className="text-xs font-semibold uppercase text-muted">Estimated API Cost</span>
            <div className="text-3xl font-extrabold text-success">${totalCostUsd.toFixed(4)}</div>
            <span className="text-[11px] text-subtle">USD Provider Charges</span>
          </div>
        </div>

        {/* Tenant B2B Management Table */}
        <div className="space-y-4 rounded-2xl border border-border bg-surface p-6">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2 className="flex items-center gap-2 text-lg font-bold text-foreground">
                <Building2 size={18} className="text-primary" aria-hidden /> B2B Tenant &amp; Subscription Quota Control
              </h2>
              <p className="text-xs text-muted">Manage tenant monthly token limits, reset usage, and control account active status.</p>
            </div>

            <button
              onClick={handlePruneLogs}
              className="flex cursor-pointer items-center gap-1.5 rounded-lg border border-border px-3.5 py-1.5 text-xs font-semibold text-muted transition-colors hover:text-foreground"
            >
              <Trash2 size={14} aria-hidden /> Prune System Logs (&gt;30 Days)
            </button>
          </div>

          {loading ? (
            <div className="py-8 text-center text-xs text-muted">Loading tenant data...</div>
          ) : tenants.length === 0 ? (
            <div className="py-8 text-center text-xs text-muted">No tenants registered yet.</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead>
                  <tr className="border-b border-border text-[10px] font-semibold uppercase text-muted">
                    <th className="px-2 pb-3">Tenant Name</th>
                    <th className="px-2 pb-3">Plan</th>
                    <th className="px-2 pb-3">Token Usage & Quota</th>
                    <th className="px-2 pb-3 text-center">Sites</th>
                    <th className="px-2 pb-3 text-center">Status</th>
                    <th className="px-2 pb-3 text-right">Actions</th>
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
                          <span className="px-2 py-0.5 rounded text-[10px] font-bold bg-blue-500/20 text-blue-300 border border-blue-500/30">
                            {tenant.planName}
                          </span>
                        </td>

                        <td className="py-3 px-2 min-w-[200px]">
                          <div className="space-y-1">
                            <div className="flex justify-between text-[10px] font-mono">
                              <span className="text-slate-300">{tenant.usedTokens.toLocaleString()}</span>
                              <span className="text-slate-500">/ {tenant.monthlyLimitTokens.toLocaleString()}</span>
                            </div>
                            <div className="w-full h-1.5 bg-slate-800 rounded-full overflow-hidden">
                              <div
                                className={`h-full rounded-full transition-all ${usagePct >= 90 ? "bg-rose-500" : usagePct >= 60 ? "bg-amber-500" : "bg-purple-500"}`}
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
                            {tenant.isActive ? "Active" : "Disabled"}
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
                            Quota
                          </button>

                          <button
                            onClick={() => handleToggleStatus(tenant.tenantId, tenant.tenantName)}
                            className={`cursor-pointer rounded px-2.5 py-1 text-[11px] font-semibold transition-colors ${tenant.isActive ? "bg-danger/15 text-danger hover:bg-danger/25" : "bg-success/15 text-success hover:bg-success/25"}`}
                          >
                            {tenant.isActive ? "Disable" : "Enable"}
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
              <ScrollText size={18} className="text-primary" aria-hidden /> System API Token Audit Logs
            </h2>
            <p className="text-xs text-muted">LLM token consumption and estimated provider cost breakdown.</p>
          </div>

          {usageLogs.length === 0 ? (
            <div className="py-8 text-center text-xs text-muted">No usage logs recorded yet.</div>
          ) : (
            <div className="max-h-80 overflow-x-auto">
              <table className="w-full text-left font-mono text-xs">
                <thead>
                  <tr className="border-b border-border text-[10px] font-semibold uppercase text-muted">
                    <th className="px-2 pb-3">Timestamp</th>
                    <th className="px-2 pb-3">Tenant</th>
                    <th className="px-2 pb-3">Service</th>
                    <th className="px-2 pb-3 text-right">Tokens</th>
                    <th className="px-2 pb-3 text-right">Est. Cost ($)</th>
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
          title="Adjust Tenant Quota"
          icon={<Zap size={18} aria-hidden />}
          subtitle={`Updating quota for ${selectedTenant.tenantName}`}
          maxWidthClass="max-w-md"
          footer={
            <>
              <button
                type="button"
                onClick={() => setSelectedTenant(null)}
                className="cursor-pointer rounded-lg bg-elevated px-4 py-2 text-xs font-bold text-foreground transition-colors hover:opacity-80"
              >
                Cancel
              </button>
              <button
                type="submit"
                form="quota-form"
                disabled={submittingQuota}
                className="cursor-pointer rounded-lg bg-primary px-4 py-2 text-xs font-bold text-on-primary transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50"
              >
                {submittingQuota ? "Saving..." : "Save Quota"}
              </button>
            </>
          }
        >
          <form id="quota-form" onSubmit={handleUpdateQuota} className="space-y-4">
            <div>
              <label className="mb-1 block text-xs font-semibold text-muted">New Monthly Limit Tokens</label>
              <input
                type="number"
                value={newLimit}
                onChange={(e) => setNewLimit(parseInt(e.target.value) || 0)}
                required
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
              <span className="text-xs font-medium text-muted">Reset current token consumption to 0</span>
            </label>
          </form>
        </Modal>
      )}

      {/* Footer */}
      <footer className="border-t border-border py-4 text-center text-xs text-subtle">
        Akiron SEO SuperAdmin Management Platform © 2026
      </footer>
    </div>
  );
}
