import DashboardPage from "@/app/dashboard/page";
import { AppProviders } from "@/components/providers";
import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const useSession = vi.fn();
vi.mock("next/navigation", () => ({ useRouter: () => ({ replace: vi.fn(), refresh: vi.fn() }) }));
vi.mock("@/hooks/useSession", () => ({
  SUPER_ADMIN_ROLE: "SuperAdmin",
  useSession: () => useSession(),
  useLogout: () => ({ mutate: vi.fn(), isPending: false }),
}));
vi.mock("@/lib/apiClient", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/lib/apiClient")>();
  return {
    ...actual,
    apiClient: {
      ...actual.apiClient,
      websites: { ...actual.apiClient.websites, list: vi.fn().mockResolvedValue([]) },
      tenant: { ...actual.apiClient.tenant, getQuota: vi.fn().mockResolvedValue({ planName: "Free", monthlyTokenLimit: 1000, usedTokens: 100, remainingTokens: 900, periodStart: "2026-08-01T00:00:00Z", periodEnd: "2026-09-01T00:00:00Z", enforcementEnabled: false }) },
    },
  };
});

describe("DashboardPage", () => {
  beforeEach(() => localStorage.clear());

  it.each([
    ["SuperAdmin", true],
    ["Owner", false],
  ])("shows the admin link only for the current SuperAdmin role", async (role, visible) => {
    useSession.mockReturnValue({ isPending: false, isError: false, isSuccess: true, data: { role } });
    const { unmount } = render(<AppProviders><DashboardPage /></AppProviders>);
    const link = await screen.findByRole("link", { name: "Admin" }).catch(() => null);
    expect(Boolean(link)).toBe(visible);
    unmount();
  });
});
