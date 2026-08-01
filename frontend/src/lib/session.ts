const TOKEN_KEY = "akiron_token";
const TENANT_KEY = "akiron_tenant_id";
const ROLE_KEY = "akiron_role";

export const SUPER_ADMIN_ROLE = "SuperAdmin";

export interface SessionData {
  accessToken: string;
  tenantId: string;
  role: string;
}

export function saveSession({ accessToken, tenantId, role }: SessionData): void {
  localStorage.setItem(TOKEN_KEY, accessToken);
  localStorage.setItem(TENANT_KEY, tenantId);
  localStorage.setItem(ROLE_KEY, role);
}

export function clearSession(): void {
  if (typeof window === "undefined") return;
  localStorage.removeItem(TOKEN_KEY);
  localStorage.removeItem(TENANT_KEY);
  localStorage.removeItem(ROLE_KEY);
}

export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TOKEN_KEY);
}

export function getTenantId(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(TENANT_KEY);
}

export function getRole(): string | null {
  if (typeof window === "undefined") return null;
  return localStorage.getItem(ROLE_KEY);
}

export function isSuperAdmin(): boolean {
  return getRole() === SUPER_ADMIN_ROLE;
}

/**
 * Clears the session and sends the browser to the login page. Used both by the
 * logout control and by the API client when the server rejects a token.
 */
export function logout(): void {
  clearSession();
  if (typeof window !== "undefined") {
    window.location.href = "/login";
  }
}
