# Project Changelog & Dev Progress Log

All progress, phase updates, and development milestones for **Akiron SEO** are recorded here.

---

## [Persistence Migration - Completed] - 2026-07-26

### 🎯 Objective
Move the runtime database from SQLite to the PostgreSQL 16 instance the project always specified, put the schema under EF Core migrations, and make the quota ledger correct under real concurrency.

### 📋 Completed Milestones
- [x] **P.1: PostgreSQL Provider**: Replaced `UseSqlite` with `UseNpgsql` (retry-on-failure enabled) reading `ConnectionStrings:DefaultConnection`; removed the `Microsoft.EntityFrameworkCore.Sqlite` package.
- [x] **P.2: EF Core Migrations**: Added the `InitialCreate` migration and an `IDesignTimeDbContextFactory`. Startup now runs `MigrateAsync` and fails fast; `EnsureCreatedAsync` and the exception-swallowing schema fallback in `DbInitializer` are gone (Compose pre-creates the database, so `EnsureCreated` would have skipped table creation entirely).
- [x] **P.3: Schema Constraints**: Unique indexes on `User.Email`, `Tenant.Slug`, and the partial `Website (TenantId, DomainUrl)` index; explicit decimal precision on `Plan.PriceMonthly` and `ApiUsageLog.EstimatedCostUsd`.
- [x] **P.4: UTC Normalization**: Added a global `DateTime` value converter, required by Npgsql's `timestamptz` mapping.
- [x] **P.5: Quota Ledger Correctness**: `ReserveQuotaAsync` is idempotent per job id, enforces `MonthlyLimitTokens`, and debits via a conditional `ExecuteUpdate` inside a transaction; `RefundQuotaAsync` claims the reservation atomically and no longer depends on an ambient tenant context.
- [x] **P.6: Testcontainers Suite**: Integration tests run against a real PostgreSQL container (10 tests), covering duplicate job ids, concurrent overdraw, concurrent refunds, soft-delete filtering, and the partial unique index.
- [x] **P.7: Compose Healthcheck**: Added a `pg_isready` healthcheck so `docker compose up -d --wait` blocks until the database accepts connections.

### 📌 Known Deviations
- The 11 JSON string columns remain `text`; `SYSTEM_ARCHITECTURE.md` specifies `jsonb`. Entity defaults are `string.Empty`, which is not valid `jsonb`, so the conversion needs a dedicated pass over every write site.

---

## [Phase 1 - Completed] - 2026-07-24

### 🎯 Objective
Build Website Management (CRUD & Ownership Verification via DNS TXT / Meta Tag), BYOK (Bring Your Own Key) AES-256-GCM Encrypted Key Storage, Tracked Keywords Scheduling with Cronos, Asynchronous Web Crawler Engine (`CrawlJob`, `CrawlResult`), SiteSnapshot Generation, and Next.js Tenant Dashboard UI (`/dashboard`).

### 📋 Completed Milestones
- [x] **1.1: Security & BYOK**: Implemented `ApiKeyEncryptionService` with enterprise **AES-256-GCM** authenticated encryption for OpenAI, Perplexity, and Gemini keys.
- [x] **1.2: Website Management**: Built MediatR CQRS commands (`CreateWebsiteCommand`, `VerifyWebsiteOwnershipCommand`, `GetWebsitesQuery`) supporting HTML Meta tag and DNS TXT verification.
- [x] **1.3: Tracked Keywords & Cronos**: Built `AddTrackedKeywordCommand` with `Cronos` library for dynamic cron expression validation and `NextScheduledRun` computation.
- [x] **1.4: Web Crawler & Site Snapshot**: Created `WebCrawlerService` executing HTTP page fetches, creating `CrawlJob`, `CrawlResult`, `SeoAudit`, and generating `SiteSnapshot` summaries.
- [x] **1.5: Global Exception & Serilog Infrastructure**: Added `GlobalExceptionHandlerMiddleware` (RFC 7807 `ProblemDetails` with unique `CorrelationId`) and `Serilog` structured logging.
- [x] **1.6: Next.js Tenant Dashboard**: Built `/dashboard` route featuring Site Add/Verify modal, Web Crawler audit launcher, and BYOK API Key management panel.

---

## [Phase 0 - Completed] - 2026-07-24

### 🎯 Objective
Establish the core .NET 10 Clean Architecture foundation, EF Core 10 Multi-Tenant DbContext with Global Query Filters, QuotaReservations Ledger Engine, Scoped Hangfire Job Filter, Testcontainers integration tests, and Next.js Auth UI Shell (Dual-Language & Light/Dark Theme).

### 📋 Completed Milestones
- [x] **0.1: Backend Dependencies**: Added `MediatR` (`12.4.1`), `Npgsql.EFCore.PostgreSQL`, `FluentValidation`, `Cronos`, `Resilience` NuGet packages.
- [x] **0.2: Domain Layer**: Implemented all 22 Domain Entities with `IMultiTenant` and `ISoftDelete` interfaces 100% in English.
- [x] **0.3: EF Core Multi-Tenancy**: Configured `AkironDbContext` with automatic Global Query Filters (`TenantId` & `IsDeleted`), partial unique index, and composite indexes.
- [x] **0.4: Quota Ledger Engine**: Implemented `QuotaLedgerService` managing `QuotaReservations`.
- [x] **0.5: Multi-Tenant Integration Tests**: Created `AkironSeo.IntegrationTests` test suite with 4 passing integration tests.
- [x] **0.6: Next.js Auth UI Shell**: Built Next.js 16 App Router UI slice (`/`, `/login`, `/register`).
