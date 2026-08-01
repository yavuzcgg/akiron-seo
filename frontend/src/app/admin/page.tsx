"use client";

import AuthGuard from "@/components/AuthGuard";
import { useApp } from "@/components/providers";
import { AdminTenantDto, AdminUsageLogDto, apiClient } from "@/lib/apiClient";
import Link from "next/link";
import { useEffect, useState } from "react";
import { getErrorMessage } from "@/lib/errors";

export default function AdminDashboardPage() {
  return (
    <AuthGuard requireSuperAdmin>
      <AdminDashboardContent />
    </AuthGuard>
  );
}

function AdminDashboardContent() {
  const { theme, toggleTheme, lang, setLang } = useApp();
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
    <div className="min-h-screen flex flex-col justify-between p-4 sm:p-6 max-w-7xl mx-auto space-y-6">
      {/* Header */}
      <header className="flex flex-wrap items-center justify-between gap-4 py-3 border-b border-[var(--border-color)]">
        <div className="flex items-center space-x-3">
          <Link href="/dashboard" className="w-9 h-9 bg-purple-600 rounded-lg flex items-center justify-center font-extrabold text-white text-xl shadow-md">
            🛡️
          </Link>
          <div>
            <h1 className="font-extrabold text-xl tracking-tight text-white">SuperAdmin Control Panel</h1>
            <p className="text-xs text-slate-400">Enterprise B2B Tenant Management & API Token Audit Log</p>
          </div>
        </div>

        <div className="flex items-center space-x-3 text-sm font-medium">
          <button
            onClick={() => setLang(lang === "en" ? "tr" : "en")}
            className="px-3 py-1.5 rounded-lg border border-[var(--border-color)] bg-[var(--card-bg)] hover:opacity-80 transition"
          >
            🌐 {lang.toUpperCase()}
          </button>
          <button
            onClick={toggleTheme}
            className="px-3 py-1.5 rounded-lg border border-[var(--border-color)] bg-[var(--card-bg)] hover:opacity-80 transition"
          >
            {theme === "dark" ? "☀️ Light" : "🌙 Dark"}
          </button>
          <Link href="/dashboard" className="px-3.5 py-1.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs shadow transition">
            ← Tenant Dashboard
          </Link>
        </div>
      </header>

      {/* Main Content */}
      <main className="space-y-6">
        {/* Status Alerts */}
        {message && (
          <div className="p-4 rounded-xl bg-emerald-500/10 border border-emerald-500/20 text-emerald-400 text-sm font-semibold flex items-center justify-between">
            <span>{message}</span>
            <button onClick={() => setMessage(null)} className="text-xs font-bold text-slate-400 hover:text-white">✕</button>
          </div>
        )}
        {error && (
          <div className="p-4 rounded-xl bg-red-500/10 border border-red-500/20 text-red-400 text-sm font-semibold flex items-center justify-between">
            <span>{error}</span>
            <button onClick={() => setError(null)} className="text-xs font-bold text-slate-400 hover:text-white">✕</button>
          </div>
        )}

        {/* System Overview KPI Cards */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          <div className="p-5 rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] space-y-1">
            <span className="text-xs font-semibold text-slate-400 uppercase">Total Tenants</span>
            <div className="text-3xl font-extrabold text-white">{totalTenants}</div>
            <span className="text-[11px] text-slate-400">B2B Accounts</span>
          </div>

          <div className="p-5 rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] space-y-1">
            <span className="text-xs font-semibold text-slate-400 uppercase">Registered Sites</span>
            <div className="text-3xl font-extrabold text-blue-400">{totalWebsites}</div>
            <span className="text-[11px] text-slate-400">Monitored Domains</span>
          </div>

          <div className="p-5 rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] space-y-1">
            <span className="text-xs font-semibold text-slate-400 uppercase">Total Tokens Spent</span>
            <div className="text-3xl font-extrabold text-purple-400">{totalTokens.toLocaleString()}</div>
            <span className="text-[11px] text-slate-400">LLM Tokens Consumed</span>
          </div>

          <div className="p-5 rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] space-y-1">
            <span className="text-xs font-semibold text-slate-400 uppercase">Estimated API Cost</span>
            <div className="text-3xl font-extrabold text-emerald-400">${totalCostUsd.toFixed(4)}</div>
            <span className="text-[11px] text-slate-400">USD Provider Charges</span>
          </div>
        </div>

        {/* Tenant B2B Management Table */}
        <div className="p-6 rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h2 className="text-lg font-bold text-white flex items-center gap-2">
                🏢 B2B Tenant & Subscription Quota Control
              </h2>
              <p className="text-xs text-slate-400">Manage tenant monthly token limits, reset usage, and control account active status.</p>
            </div>

            <button
              onClick={handlePruneLogs}
              className="px-3.5 py-1.5 rounded-lg border border-purple-500/30 text-purple-300 hover:bg-purple-500/10 text-xs font-semibold transition"
            >
              🧹 Prune System Logs (&gt;30 Days)
            </button>
          </div>

          {loading ? (
            <div className="py-8 text-center text-xs text-slate-400">Loading tenant data...</div>
          ) : tenants.length === 0 ? (
            <div className="py-8 text-center text-xs text-slate-400">No tenants registered yet.</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead>
                  <tr className="border-b border-[var(--border-color)] text-slate-400 font-semibold uppercase text-[10px]">
                    <th className="pb-3 px-2">Tenant Name</th>
                    <th className="pb-3 px-2">Plan</th>
                    <th className="pb-3 px-2">Token Usage & Quota</th>
                    <th className="pb-3 px-2 text-center">Sites</th>
                    <th className="pb-3 px-2 text-center">Status</th>
                    <th className="pb-3 px-2 text-right">Actions</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[var(--border-color)] font-medium">
                  {tenants.map((tenant) => {
                    const usagePct = tenant.monthlyLimitTokens > 0
                      ? Math.min((tenant.usedTokens / tenant.monthlyLimitTokens) * 100, 100)
                      : 0;

                    return (
                      <tr key={tenant.tenantId} className="hover:bg-slate-800/30 transition">
                        <td className="py-3 px-2">
                          <div className="font-bold text-slate-200">{tenant.tenantName}</div>
                          <div className="text-[10px] font-mono text-slate-500">{tenant.slug}</div>
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

                        <td className="py-3 px-2 text-center font-bold text-slate-300">
                          {tenant.registeredWebsitesCount}
                        </td>

                        <td className="py-3 px-2 text-center">
                          <span className={`px-2 py-0.5 rounded-full text-[10px] font-bold ${tenant.isActive ? "bg-emerald-500/10 text-emerald-400 border border-emerald-500/20" : "bg-rose-500/10 text-rose-400 border border-rose-500/20"}`}>
                            {tenant.isActive ? "✓ Active" : "✕ Disabled"}
                          </span>
                        </td>

                        <td className="py-3 px-2 text-right space-x-2">
                          <button
                            onClick={() => {
                              setSelectedTenant(tenant);
                              setNewLimit(tenant.monthlyLimitTokens);
                              setResetUsage(false);
                            }}
                            className="px-2.5 py-1 rounded bg-purple-600/20 text-purple-300 hover:bg-purple-600/30 font-semibold text-[11px] transition"
                          >
                            ⚡ Quota
                          </button>

                          <button
                            onClick={() => handleToggleStatus(tenant.tenantId, tenant.tenantName)}
                            className={`px-2.5 py-1 rounded font-semibold text-[11px] transition ${tenant.isActive ? "bg-rose-600/20 text-rose-300 hover:bg-rose-600/30" : "bg-emerald-600/20 text-emerald-300 hover:bg-emerald-600/30"}`}
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
        <div className="p-6 rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] space-y-4">
          <div>
            <h2 className="text-lg font-bold text-white flex items-center gap-2">
              📜 System API Token Audit Logs
            </h2>
            <p className="text-xs text-slate-400">Real-time LLM token consumption and estimated provider cost breakdown.</p>
          </div>

          {usageLogs.length === 0 ? (
            <div className="py-8 text-center text-xs text-slate-400">No usage logs recorded yet.</div>
          ) : (
            <div className="overflow-x-auto max-h-80">
              <table className="w-full text-left text-xs font-mono">
                <thead>
                  <tr className="border-b border-[var(--border-color)] text-slate-400 font-semibold uppercase text-[10px]">
                    <th className="pb-3 px-2">Timestamp</th>
                    <th className="pb-3 px-2">Tenant</th>
                    <th className="pb-3 px-2">Service</th>
                    <th className="pb-3 px-2 text-right">Tokens</th>
                    <th className="pb-3 px-2 text-right">Est. Cost ($)</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[var(--border-color)]">
                  {usageLogs.map((log) => (
                    <tr key={log.logId} className="hover:bg-slate-800/30 transition">
                      <td className="py-2.5 px-2 text-slate-400">
                        {new Date(log.timestamp).toLocaleString()}
                      </td>
                      <td className="py-2.5 px-2 font-bold text-slate-200">
                        {log.tenantName}
                      </td>
                      <td className="py-2.5 px-2 text-purple-300">
                        {log.serviceName}
                      </td>
                      <td className="py-2.5 px-2 text-right font-bold text-slate-200">
                        {log.tokensUsed.toLocaleString()}
                      </td>
                      <td className="py-2.5 px-2 text-right text-emerald-400 font-bold">
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
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-fadeIn">
          <div className="relative w-full max-w-md rounded-2xl border border-[var(--border-color)] bg-[var(--card-bg)] shadow-2xl p-6 space-y-4">
            <div className="flex items-center justify-between border-b border-[var(--border-color)] pb-3">
              <h3 className="text-base font-bold text-white">⚡ Adjust Tenant Quota</h3>
              <button onClick={() => setSelectedTenant(null)} className="text-slate-400 hover:text-white">✕</button>
            </div>

            <p className="text-xs text-slate-400">
              Updating quota for tenant <strong className="text-white">{selectedTenant.tenantName}</strong>.
            </p>

            <form onSubmit={handleUpdateQuota} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-300 mb-1">New Monthly Limit Tokens</label>
                <input
                  type="number"
                  value={newLimit}
                  onChange={(e) => setNewLimit(parseInt(e.target.value) || 0)}
                  required
                  step={50000}
                  className="w-full px-3 py-2 rounded-lg border border-[var(--border-color)] bg-[var(--bg-primary)] text-sm font-mono"
                />
              </div>

              <div className="flex items-center space-x-2">
                <input
                  type="checkbox"
                  id="resetCheck"
                  checked={resetUsage}
                  onChange={(e) => setResetUsage(e.target.checked)}
                  className="rounded border-slate-700 bg-slate-900"
                />
                <label htmlFor="resetCheck" className="text-xs text-slate-300 font-medium">
                  Reset current token consumption to 0
                </label>
              </div>

              <div className="flex justify-end gap-2 pt-2 border-t border-[var(--border-color)]">
                <button
                  type="button"
                  onClick={() => setSelectedTenant(null)}
                  className="px-4 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 font-bold text-xs"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={submittingQuota}
                  className="px-4 py-2 rounded-lg bg-purple-600 hover:bg-purple-700 text-white font-bold text-xs disabled:opacity-50"
                >
                  {submittingQuota ? "Saving..." : "Save Quota"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Footer */}
      <footer className="py-4 border-t border-[var(--border-color)] text-center text-xs text-slate-500">
        Akiron SEO SuperAdmin Management Platform © 2026
      </footer>
    </div>
  );
}
