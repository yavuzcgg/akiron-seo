# Project Changelog & Dev Progress Log

All progress, phase updates, and development milestones for **Akiron SEO** are recorded here.

---

## [Data Provenance, GEO Correctness & CI - Completed] - 2026-08-05

### 🎯 Objective
Stop presenting synthetic output as measurement, remove the fabricated engine results
entirely, fix two GEO defects found in the audit, and put the existing test suite behind
continuous integration.

### 🧭 Data Provenance
- [x] **`DataSources` provenance model**: every analytics DTO now carries a data source — `Live`, `NotConfigured`, `Unavailable`, or `Simulated`. A single flag per result replaces guesswork, and when a real integration lands its service simply starts returning `Live`.
- [x] **`DataSourceBadge` component**: renders **DEMO DATA**, **NOT CONFIGURED**, or **UNAVAILABLE** with an explanatory tooltip, and renders nothing for live data — so it is safe to place beside any metric. Applied to the GSC, keyword rank, competitor gap, and GEO cards.
- [x] **Fabricated ChatGPT and Claude results deleted**: `AddSimulatedSampleEngineResults` invented citations for two engines that were never queried, including a deliberately-404 URL that manufactured a fake Gold Opportunity and raised a real user notification. Removed outright.
- [x] **Adapter fallbacks no longer claim a citation**: a missing API key or a failed call returned `IsMentioned: true, Sentiment: Positive`, so a tenant who had configured nothing saw a perfect GEO score. They now report the reason and are excluded from the rate calculation instead of counting as evidence.
- [x] **Placeholder services documented at the class level**: `SearchConsoleService`, `KeywordRankTrackerService`, and `CompetitorService` each state plainly that no integration exists and what would replace them.

### 🐛 Defect Fixes
- [x] **`/geo-analysis` returned 500 for any website without a tracked keyword**: `TrackedKeywordId` was set to `Guid.Empty`, violating the foreign key. The column is now nullable, since a brand-visibility run is not tied to a keyword.
- [x] **Cross-website data contamination**: the 24-hour GEO cache and the Gold Opportunity list were scoped to the tenant only, so a multi-website tenant was served another site's results labelled with the requested site's name. Added `WebsiteId` to `GeoAnalysis` and `Notification`, scoped both queries, and added covering indexes.
- [x] **Duplicate shadow column avoided**: the new `GeoAnalysis` → `TrackedKeyword` relationship names its inverse navigation explicitly; without it EF Core scaffolded a second `TrackedKeywordId1` column.

### 🛠️ Quality
- [x] **GEO adapters resolved through DI**: `GeoEngineService` was calling `new PerplexitySonarAdapter(...)` directly, bypassing the abstraction it defined. Adapters are now registered as a set and declare their own `AiProviderEnum`, so the service resolves each tenant key generically and a new engine is one registration line.
- [x] **Silent failures now logged**: exception-swallowing `catch` blocks in the adapters, the GEO cache deserializer, and tenant key decryption were replaced with typed catches that log the cause.
- [x] **GitHub Actions CI**: backend build plus the Testcontainers integration suite, frontend type-check and build, and both Docker images. Lint runs non-gating until the pre-existing `react-hooks` findings are refactored.

### ✅ Verification
`dotnet test` 10/10 passing. `npx tsc --noEmit` clean. `npm run build` succeeds.

---

## [Security Hardening & Migration Repair - Completed] - 2026-07-28

### 🎯 Objective
Close the authorization, secret-handling, and SSRF findings from the full-repository audit,
and repair two migration defects that made every integration test fail and would have broken
any fresh deployment.

### 🔒 Security Fixes
- [x] **Cross-tenant privilege escalation**: `/api/v1/admin/*` was `RequireAuthorization()` with no role check, so any authenticated tenant user could enumerate every tenant and change or disable their accounts. Added the `SuperAdminOnly` policy (`Security/AuthorizationPolicies.cs`) and gated the Admin link behind the role in the dashboard. Verified: a normal Owner now receives `403`.
- [x] **Committed secrets**: removed `Jwt:SecretKey`, `Security:MasterEncryptionKey`, and the database password from the tracked `appsettings.json`. Development values moved to gitignored `appsettings.Development.json` (with a committed `.example`), deployments read environment variables from `.env` (with a committed `.env.example`). `SecretsValidator` aborts startup outside Development when a secret is missing, too short, or still an example value.
- [x] **Default SuperAdmin in production**: `DbInitializer.SeedAsync` now takes `seedSuperAdmin`, which `Program.cs` sets only for Development. Removed the pre-filled `admin@akironseo.com` / `Admin123!` credentials from the login form.
- [x] **SSRF**: added `OutboundUrlGuard`, which resolves the host and rejects loopback, RFC 1918, carrier-grade NAT, and link-local addresses (including the `169.254.169.254` cloud metadata endpoint). Applied to the crawler, the webhook dispatcher, and meta-tag ownership verification. Rejections surface as `400`.
- [x] **DNS ownership verification returned `true` unconditionally**: replaced the stub with a real TXT lookup via `DnsClient` behind `IDnsLookupService`.
- [x] **Stored XSS in the report exporter**: HTML-encoded the website name and the crawled title, meta description, canonical URL, and AI snippets, which were being concatenated into a document served as `text/html` from the API origin.
- [x] **Login accepted deactivated users**: `User.IsActive` is now checked on both login and refresh.

### 🐛 Defect Fixes
- [x] **Missing migration**: `Website.WebhookUrl` existed on the entity with no migration, so the model had pending changes and every integration test failed at fixture setup. Added `AddWebsiteWebhookUrl`.
- [x] **Broken jsonb migration**: `MigrateJsonToNativeJsonb` used bare `AlterColumn` for `text` → `jsonb`, which PostgreSQL rejects (`42804`). Rewrote it with explicit `USING` clauses that also map pre-existing blank values to each column's default shape. This migration had never run successfully against a fresh database.
- [x] **Frontend Docker image could not build**: `next.config.ts` lacked `output: "standalone"` while the Dockerfile copied `.next/standalone`. Also moved `NEXT_PUBLIC_API_URL` to a build arg, since `NEXT_PUBLIC_*` is inlined at build time and had no effect as a runtime variable.

### 🛠️ Infrastructure & Quality
- [x] **Health endpoint** `GET /health` verifying database connectivity, wired to a backend container healthcheck so `frontend` waits for readiness rather than container start.
- [x] **CORS ordering**: `UseCors` now precedes the exception handler so error responses carry CORS headers instead of failing opaquely in the browser.
- [x] **Frontend session handling**: added `lib/session.ts` and `AuthGuard`, a real logout that clears the token, and 401 interception that ends the session and redirects to `/login`. The Logout control was previously a link to `/` that left the token in storage.
- [x] **Backend container** runs as the non-root `$APP_UID`; Postgres is no longer published to the host.
- [x] **Lint**: replaced all 18 `catch (err: any)` blocks with a typed `getErrorMessage` helper; ESLint errors dropped from 32 to 16.

### ✅ Verification
`dotnet test` 10/10 passing (was 0/10). `docker compose build` succeeds for both images and `docker compose up -d --wait` reports all three services healthy.

### ⚠️ Known Remaining
GSC analytics, keyword rank tracking, competitor gap analysis, and the ChatGPT/Claude GEO
citations are still computed from formulas and hardcoded samples rather than live APIs, and
the UI presents them as measurements. Labelling or gating them is the next task. The
`react-hooks/set-state-in-effect` lint errors need the shared data-fetching hook refactor.

---

## [Webhook Dispatcher, GSC Analytics & Production Docker Suite - Completed] - 2026-07-26

### 🎯 Objective
Implement Step 1: Instant Webhook & Email Alert Dispatcher (`NotificationDispatcherService`), Step 2: Google Search Console (GSC) Organic Search Analytics Module (`SearchConsoleService` & `GscAnalyticsCard.tsx`), and Step 3: Full-Stack Production Docker Suite (`backend/Dockerfile`, `frontend/Dockerfile`, `docker-compose.yml`).

### 📋 Completed Milestones
- [x] **W.1: Webhook & Alert Dispatcher**: Implemented `NotificationDispatcherService` firing HTTP POST webhooks with 3s timeout and logging email notification alerts upon Gold Opportunity (404 missing citation page) creation. Added `WebhookUrl` property to `Website` entity.
- [x] **G.1: GSC Analytics Module & Endpoints**: Implemented `SearchConsoleService` and `GscEndpoints` (`GET /api/v1/websites/{id}/gsc-analytics`) calculating Organic Clicks, Impressions, CTR %, and Avg Position.
- [x] **G.2: Frontend GSC Card**: Created `GscAnalyticsCard.tsx` component comparing Organic Google Traffic metrics alongside GEO AI Search Engine Citations on the dashboard.
- [x] **D.1: Production Docker Suite**: Created multi-stage `backend/Dockerfile` for .NET 10 API, `frontend/Dockerfile` for Next.js 16 App Router standalone, and updated `docker-compose.yml` orchestrating PostgreSQL 16, backend, and frontend containers for 1-command deployment (`docker compose up -d`).

---

## [Executive Report Exporter & PostgreSQL jsonb Migration - Completed] - 2026-07-26

### 🎯 Objective
Implement Option 2: Branded Executive SEO & GEO Printable HTML/PDF Report Exporter (`ReportExportService`) and Option 3: Technical Debt Cleanup – PostgreSQL Native `jsonb` Column Migration (`MigrateJsonToNativeJsonb`).

### 📋 Completed Milestones
- [x] **R.1: Executive Report Export Service**: Implemented `ReportExportService` compiling SEO score, Meta tags, GEO Share of Voice %, Gold Opportunities, and AEO Schemas into a responsive, printable HTML document with `@media print` CSS.
- [x] **R.2: Executive Report API & Frontend Buttons**: Added `GET /api/v1/websites/{id}/export-report` endpoint and `"📄 Executive Report"` buttons to `AuditDetailsModal.tsx` and `dashboard/page.tsx`.
- [x] **M.1: Entity Default JSON Normalization**: Normalized string initializers across domain entities (`CrawlResult`, `SeoAudit`, `GeoAnalysis`) to valid JSON (`"{}"` / `"[]"`).
- [x] **M.2: EF Core jsonb Mappings**: Configured `.HasColumnType("jsonb")` mappings for all 11 JSON string columns in `AkironDbContext.cs`.
- [x] **M.3: EF Core Migration**: Scaffolded and generated EF Core migration `MigrateJsonToNativeJsonb`.

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
