# Project Changelog & Dev Progress Log

All progress, phase updates, and development milestones for **Akiron SEO** are recorded here.

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
