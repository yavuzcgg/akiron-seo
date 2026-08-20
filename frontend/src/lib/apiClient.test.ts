import { apiRequest } from "@/lib/apiClient";
import { afterEach, describe, expect, it, vi } from "vitest";

afterEach(() => vi.unstubAllGlobals());

describe("apiRequest", () => {
  it("includes cookies without adding an authorization header", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(JSON.stringify({ ok: true }), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    }));
    vi.stubGlobal("fetch", fetchMock);

    await apiRequest<{ ok: boolean }>("/test");

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const options = fetchMock.mock.calls[0][1] as RequestInit;
    expect(options.credentials).toBe("include");
    expect(new Headers(options.headers).has("Authorization")).toBe(false);
  });

  it("shares one refresh and retries each failed request only once", async () => {
    const attempts = new Map<string, number>();
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith("/auth/refresh")) {
        await new Promise((resolve) => setTimeout(resolve, 5));
        return new Response(null, { status: 204 });
      }
      const nextAttempt = (attempts.get(url) ?? 0) + 1;
      attempts.set(url, nextAttempt);
      return nextAttempt === 1
        ? new Response(JSON.stringify({ title: "Unauthorized" }), { status: 401 })
        : new Response(JSON.stringify({ ok: true }), { status: 200, headers: { "Content-Type": "application/json" } });
    });
    vi.stubGlobal("fetch", fetchMock);

    await Promise.all([apiRequest("/protected-a"), apiRequest("/protected-b")]);

    expect(fetchMock.mock.calls.filter(([input]) => String(input).endsWith("/auth/refresh"))).toHaveLength(1);
    expect(attempts.get("http://localhost:5248/api/v1/protected-a")).toBe(2);
    expect(attempts.get("http://localhost:5248/api/v1/protected-b")).toBe(2);
  });
});
