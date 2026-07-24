# Project Changelog & Dev Progress Log

All notable progress, phase updates, and development milestones for **AkironSeo** will be documented here.

---

## [Phase 0 - Initiated] - 2026-07-24

### 🎯 Objective
Establish the core .NET 10 Clean Architecture foundation, EF Core 10 Multi-Tenant DbContext with Global Query Filters, QuotaReservations Ledger Engine, Scoped Hangfire Job Filter, Testcontainers integration tests, and Next.js Auth UI Shell (Dual-Language & Light/Dark Theme).

### 📋 Planned Milestones for Phase 0
- [ ] 0.1: Add NuGet dependencies (`MediatR`, `Npgsql.EFCore.PostgreSQL`, `FluentValidation`, `Cronos`).
- [ ] 0.2: Implement 22 Domain Entities & Global Interfaces (`IMultiTenant`, `ISoftDelete`) in English.
- [ ] 0.3: Configure `AkironDbContext` with Global Query Filters and Partial Indexes.
- [ ] 0.4: Implement Atomic Quota Ledger Engine (`QuotaReservations`).
- [ ] 0.5: Implement Scoped Hangfire `TenantJobFilter`.
- [ ] 0.6: Create `AkironSeo.IntegrationTests` with `Testcontainers` PostgreSQL tenant isolation test suite.
- [ ] 0.7: Implement Next.js App Router Auth Shell with HttpOnly Refresh Cookie (`SameSite=Lax`), EN/TR i18n, and Light/Dark themes.
