import AuthGuard from "@/components/AuthGuard";
import { AppProviders } from "@/components/providers";
import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const replace = vi.fn();
const useSession = vi.fn();

vi.mock("next/navigation", () => ({ useRouter: () => ({ replace, refresh: vi.fn() }) }));
vi.mock("@/hooks/useSession", () => ({
  SUPER_ADMIN_ROLE: "SuperAdmin",
  useSession: () => useSession(),
}));

describe("AuthGuard", () => {
  beforeEach(() => {
    replace.mockReset();
    localStorage.clear();
  });

  it("renders protected content for an authenticated session", () => {
    useSession.mockReturnValue({ isPending: false, isError: false, isSuccess: true, data: { role: "Owner" } });
    render(<AppProviders><AuthGuard><div>Protected content</div></AuthGuard></AppProviders>);
    expect(screen.getByText("Protected content")).toBeInTheDocument();
  });

  it("redirects a failed session to login", async () => {
    useSession.mockReturnValue({ isPending: false, isError: true, isSuccess: false, data: undefined });
    render(<AppProviders><AuthGuard><div>Protected content</div></AuthGuard></AppProviders>);
    await waitFor(() => expect(replace).toHaveBeenCalledWith("/login"));
    expect(screen.queryByText("Protected content")).not.toBeInTheDocument();
  });
});
