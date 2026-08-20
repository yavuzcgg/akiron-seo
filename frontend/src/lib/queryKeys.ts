export const queryKeys = {
  session: ["session"] as const,
  websites: ["websites"] as const,
  quota: ["tenant", "quota"] as const,
  keywords: (websiteId: string) => ["websites", websiteId, "keywords"] as const,
  geo: (websiteId: string) => ["websites", websiteId, "geo"] as const,
  competitors: (websiteId: string) => ["websites", websiteId, "competitors"] as const,
  opportunities: (websiteId: string) => ["websites", websiteId, "opportunities"] as const,
  gsc: (websiteId: string) => ["websites", websiteId, "gsc"] as const,
  content: (websiteId: string) => ["websites", websiteId, "content"] as const,
  aeo: (websiteId: string) => ["websites", websiteId, "aeo"] as const,
  adminTenants: ["admin", "tenants"] as const,
  adminUsage: ["admin", "usage"] as const,
};
