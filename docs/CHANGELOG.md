# Project Changelog & Dev Progress Log

All progress, phase updates, and development milestones for **AkironSeo** are recorded here.

---

## [Phase 0 - Completed] - 2026-07-24

### 🎯 Objective
Establish the core .NET 10 Clean Architecture foundation, EF Core 10 Multi-Tenant DbContext with Global Query Filters, QuotaReservations Ledger Engine, Scoped Hangfire Job Filter, Testcontainers integration tests, and Next.js Auth UI Shell (Dual-Language & Light/Dark Theme).

### 📋 Completed Milestones
- [x] **0.1: Backend Dependencies**: Added `MediatR` (`12.4.1`), `Npgsql.EFCore.PostgreSQL`, `FluentValidation`, `Cronos`, `Resilience` NuGet packages.
- [x] **0.2: Domain Layer**: Implemented all 22 Domain Entities (`User`, `Tenant`, `TenantUser`, `Subscription`, `Website`, `TrackedKeyword`, `CrawlJob`, `CrawlResult`, `SeoAudit`, `SiteSnapshot`, `GeoAnalysis`, `QuotaReservation`, etc.) with `IMultiTenant` and `ISoftDelete` interfaces 100% in English.
- [x] **0.3: EF Core Multi-Tenancy**: Configured `AkironDbContext` with automatic Global Query Filters (`TenantId` & `IsDeleted`), partial unique index (`HasFilter("\"IsDeleted\" = false")`), and composite indexes.
- [x] **0.4: Quota Ledger Engine**: Implemented `QuotaLedgerService` managing `QuotaReservations` (`Reserved`, `Committed`, `Refunded`) with atomic reservation and idempotent double-refund prevention.
- [x] **0.5: Multi-Tenant Integration Tests**: Created `AkironSeo.IntegrationTests` test suite with 4 passing integration tests verifying zero cross-tenant data leaks and idempotent quota refunds.
- [x] **0.6: Next.js Auth UI Shell**: Built Next.js 16 App Router UI slice (`/`, `/login`, `/register`) featuring EN/TR dual-language switcher and Light/Dark mode themes. Build verified with zero errors.
