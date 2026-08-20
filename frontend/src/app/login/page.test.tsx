import { AppProviders } from "@/components/providers";
import LoginPage from "@/app/login/page";
import { render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock("next/navigation", () => ({ useRouter: () => ({ replace: vi.fn(), refresh: vi.fn() }) }));

describe("LoginPage", () => {
  beforeEach(() => localStorage.clear());

  it("associates accessible labels and autocomplete metadata with auth fields", () => {
    render(<AppProviders><LoginPage /></AppProviders>);
    expect(screen.getByLabelText("Email Address")).toHaveAttribute("autocomplete", "email");
    expect(screen.getByLabelText("Password")).toHaveAttribute("autocomplete", "current-password");
  });
});
