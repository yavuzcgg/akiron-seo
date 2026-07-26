# Project Changelog & Dev Progress Log

All progress, phase updates, and development milestones for **Akiron SEO** are recorded here.

---

## [SuperAdmin Dashboard & B2B Tenant Management - Completed] - 2026-07-26

### 🎯 Objective
Implement SuperAdmin Dashboard & B2B Tenant Management (`/admin` route): Tenant listing, manual token quota adjustments, B2B account lifecycle control, system-wide API token consumption audit logs, and log pruning maintenance.

### 📋 Completed Milestones
- [x] **A.1: Admin CQRS Queries & Commands**: Created `GetAdminTenantsQuery` (bypasses tenant query filters to retrieve system-wide tenant information), `UpdateTenantQuotaCommand` (manual token limit adjustment & reset), `ToggleTenantStatusCommand` (soft-delete toggle), `GetApiUsageLogsQuery` (token audit log aggregation), and `PruneSystemLogsCommand` (30-day log cleanup).
- [x] **A.2: Admin Modular API Endpoints**: Created `AdminEndpoints` (`GET /api/v1/admin/tenants`, `POST /api/v1/admin/tenants/{id}/quota`, `POST /api/v1/admin/tenants/{id}/toggle-status`, `GET /api/v1/admin/usage-logs`, `POST /api/v1/admin/prune-logs`).
- [x] **A.3: Frontend /admin Dashboard Route**: Built `src/app/admin/page.tsx` featuring KPI Cards (Total Tenants, Total Sites, Total Tokens Spent, Estimated Cost USD), B2B Tenant Management Table, Quota Adjust Modal, API Usage Logs Table, and Prune Logs Trigger.
- [x] **A.4: Dashboard Navigation**: Added `"🛡️ Admin"` shortcut link in tenant dashboard header.

---

## [Phase 4 - GEO AI Content Writer & Reporting Engine - Completed] - 2026-07-26

### 🎯 Objective
Implement Phase 4: Princeton GEO-backed AI Content Writer Engine (`AiContentWriterService`), 1-click Gold Opportunity missing 404 page content generation, DB persistence to `AiContentPlans` table, CQRS commands/queries, and interactive Markdown Content Writer Modal UI with live editor, copy, and `.md` file download.

### 📋 Completed Milestones
- [x] **4.1: GEO-Optimized Content Writer Service**: Implemented `AiContentWriterService` generating Princeton GEO-backed high-fact-density articles (high data density, statistics, direct quotations, Schema.org Article JSON-LD) and saving `AiContentPlan` entities.
- [x] **4.2: CQRS & Modular Endpoints**: Created `GenerateAiContentCommand`, `GetAiContentPlansQuery`, and `ContentEndpoints` (`POST /api/v1/websites/{id}/ai-content/generate`, `GET /api/v1/websites/{id}/ai-content`).
- [x] **4.3: Frontend AI Content Writer Modal**: Built `AiContentWriterModal` featuring live Markdown editor/previewer, token usage counter, 1-click Markdown copy, `.md` file download, and historical generated content list.
- [x] **4.4: 1-Click Gold Opportunity Fix Integration**: Connected Gold Opportunity 404 alerts to launch `AiContentWriterModal` pre-populated with target keyword & 404 missing path.
- [x] **4.5: Dashboard Actions**: Added `"✍️ AI Writer"` action button on registered website cards.

---

## [Phase 3 - GEO Pipeline & Gold Opportunity Engine - Completed] - 2026-07-26

### 🎯 Objective
Implement Phase 3: Structured AI Engine Adapters (`PerplexitySonarAdapter`, `GeminiGroundingAdapter`), 3-Sample Iteration Engine with Mention Rate % computation, Automated HTTP Citation Verification & 404 Gold Opportunity Alert Triggering, 24-Hour Analysis Caching, and UI Components (`GoldOpportunityPanel` & `GeoIntelligenceCard`).

### 📋 Completed Milestones
- [x] **3.1: Citation Verification Service**: Implemented `CitationVerificationService` performing HTTP HEAD/GET verification with 5-second timeout, categorizing URLs into `Valid`, `NonExistentPage` (404), `WrongDomain`, or `Unreachable`.
- [x] **3.2: 404 Gold Opportunity Alert Trigger**: Automatically generates `Notification` entries (`Type = NotificationTypeEnum.GoldOpportunityAlert`) when AI engines cite 404 missing pages on tenant domain.
- [x] **3.3: Structured AI Adapters**: Created `IGeoEngineAdapter` interface and implemented `PerplexitySonarAdapter` (native citations parsing) and `GeminiGroundingAdapter` (`google_search` grounding).
- [x] **3.4: Multi-Sample Iteration Engine & Caching**: 3 sample query runs with 200-400ms random jitter for precise Mention Rate % computation, with 24-hour analysis caching and "Force Refresh" override.
- [x] **3.5: API Endpoints & CQRS Queries**: Added `GetGoldOpportunitiesQuery`, updated `GeoEndpoints` for `forceRefresh=true`, and added `NotificationEndpoints` (`GET /notifications`, `POST /notifications/{id}/read`).
- [x] **3.6: Frontend Gold Opportunity Panel & GEO UI**: Built `GoldOpportunityPanel` with 1-click CTA "Create Page with AI", updated `GeoIntelligenceCard` with Mention Rate % badge, Citation Status pills, and Force Refresh.

---

## [Phase 2 Completion - Completed] - 2026-07-26

### 🎯 Objective
Complete Phase 2: Enhanced SEO Audit Scoring Engine, AI Bot Auditor persistence, AEO Generator with `llms-full.txt` & FAQ JSON-LD, and enriched frontend audit reporting.

### 📋 Completed Milestones
- [x] **2.1: Enhanced Web Crawler Engine**: `WebCrawlerService` now extracts H1 tags, canonical URL, OpenGraph tags (og:title, og:description, og:image), and robots meta directives (noindex/nofollow) from crawled HTML.
- [x] **2.2: Weighted SEO Scoring Engine**: 100-point scoring system across 10 weighted checks: HTTP status (15), title (15), meta description (15), H1 heading (10), canonical URL (10), OpenGraph (10), robots meta (10), title length (5), meta length (5), heading hierarchy (5).
- [x] **2.3: AI Bot Auditor Persistence**: `robots.txt` audit is now automatically executed during crawl and persisted to `SeoAudit.RobotsTxtAiStatusJson`. No separate manual trigger needed.
- [x] **2.4: AEO Generator Enhancement**: Added FAQ JSON-LD generation from crawled page data, `llms-full.txt` with complete page inventory, and DB persistence to `AeoSchemas` table.
- [x] **2.5: Frontend Audit Modal Redesign**: Tabbed interface with Overview (meta tags, H1, canonical, score breakdown), Issues (severity-coded), AI Bots (inline robots.txt status), and AI Engine (BYOK suggestions).
- [x] **2.6: Frontend AEO Modal Enhancement**: Three tabs – Schema.org (Organization + WebSite + FAQ JSON-LD), llms.txt, and llms-full.txt with copy buttons and contextual AI engine badges.

### 📌 Known Deviations
- PageSpeed Insights API integration deferred (requires Google Cloud API key).
- JSON columns remain `text` type; `jsonb` migration planned as separate pass before Phase 3.

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
