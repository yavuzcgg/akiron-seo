# AkironSeo: AI-Powered SEO, AIO, GEO & AEO Optimization Platform (Implementation Plan v5.0)

AkironSeo is an enterprise-grade, multi-tenant SaaS platform that automates, measures, and optimizes websites for Search Engine Optimization (**SEO**), AI Overviews (**AIO**), Generative Engine Optimization (**GEO**), and Answer Engine Optimization (**AEO**).

---

## 🛠️ Architecture & Technology Stack (v5.0 English Standard)

- **Language Standard**: 100% English source code, comments, database schemas, DTOs, commit messages, and API responses.
- **Backend**: **.NET 10 LTS** Web API (**Clean Architecture**: Domain, Application, Infrastructure, API layers)
- **CQRS & Pipeline**: **MediatR [12.4.0, 13.0.0)** (MIT Licensed 12.x series) + FluentValidation
- **Frontend**: **Next.js 15/16 (Latest)** App Router + React 19 + TypeScript + Tailwind CSS + TanStack Query v5 + Recharts (Dual-Language EN/TR + Light/Dark Mode)
- **Database**: **PostgreSQL / NeonDB** (Entity Framework Core 10, Multi-Tenancy Global Query Filters, Denormalized `TenantId`, Partial Unique Indexes)
- **Quota Ledger Engine**: `QuotaReservations` ledger table + Postgres conditional UPDATE + Fail Refund + Success Reconciliation
- **Cron Scheduling**: `Cronos` library for parsing `CronExpression` and computing `NextScheduledRun`
- **Structured AI Adapters (`IGeoEngineClient`)**:
  - **Perplexity Sonar**: `citations` & `search_results` JSON adapter
  - **OpenAI**: Responses API + `web_search` `url_citation` adapter
  - **Gemini**: Google Search `groundingMetadata` adapter
  - **Anthropic**: Claude web search tool adapter
- **Background Jobs**: Hangfire (PostgreSQL Storage) + Scoped `TenantJobFilter` Activator
- **Security & Auth**: Multi-Tenant RBAC + HttpOnly access/refresh cookies (`SameSite=Lax`) + hashed refresh-token families + BYOK (AES-256-GCM)

---

## 🚀 Phase-by-Phase Implementation Roadmap (v5.0 Demo-Driven)

### ⚪ Phase 0: Multi-Tenancy, Quota Ledger, Isolation Tests & Auth Foundation
- [ ] Configure `.NET 10 Web API Clean Architecture` projects targeting `.NET 10` and `MediatR [12.4.0, 13.0.0)`
- [ ] Implement Global Entities (`User`, `RefreshToken`, `Plan`, `PromptTemplate`, `AiCache`, `GlobalSystemLog`)
- [ ] Implement Tenant Entities (`Tenant`, `TenantUser`, `Subscription`, `TenantFeature`, `EncryptedTenantApiKey`, `QuotaReservation`)
- [ ] Configure EF Core 10 `AkironDbContext` **Global Query Filters** and `.HasFilter("\"IsDeleted\" = false")` partial indexes
- [ ] Implement **Scoped Hangfire Tenant Filter** (`TenantJobFilter`)
- [ ] Implement **QuotaReservations Ledger Engine** (`Reserved`, `Committed`, `Refunded` states)
- [ ] **Multi-Tenant Isolation Tests**: `Testcontainers` real PostgreSQL integration tests verifying zero cross-tenant leak under concurrency
- [ ] **UI Slice**: Next.js App Router with `HttpOnly Cookie (SameSite=Lax)` Login/Register, EN/TR Language Switcher, Light/Dark Theme Toggle, and Tenant Dashboard Shell

### 🔴 Phase 1: Website Management, Web Crawler & Site Snapshot Engine
- [ ] Implement `Website` (VerificationToken with DNS/Meta Tag check) and `TrackedKeyword` (Cronos library `CronExpression` + `NextScheduledRun`)
- [ ] Implement BYOK (AES-256-GCM encrypted OpenAI / Perplexity API key management)
- [ ] **Decoupled Crawler Architecture**: `CrawlJob` → `SeoAudit` (1-to-1) → `CrawlResults` chain
- [ ] **Site Snapshot & Diff Engine**: `SiteSnapshot` summary rows and PostgreSQL `LAG()` window function for score/issue diffs
- [ ] **UI Slice**: Website Add/Verify screen, Crawl Trigger UI, and Site Snapshot Comparison View

### 🟡 Phase 2: SEO Audits, AI Bot Auditor & AEO Engine
- [ ] SEO Audit scoring (computed on `CrawlResults`, PageSpeed Insights API integration limited to homepage + N critical pages)
- [ ] **Quick-Win Audit**: `robots.txt` AI Bot Auditor (`GPTBot`, `ClaudeBot`, `PerplexityBot`, `Google-Extended` access verification)
- [ ] AEO Generator: Automated JSON-LD FAQ/Article schema generator + **`llms.txt`** and **`llms-full.txt`** generator
- [ ] **UI Slice**: SEO Audit Report Page, AI Bot Audit Panel, and 1-Click AEO Schema Generator UI

### 🟢 Phase 3: GEO Structured Pipeline & Gold Opportunity Engine
- [ ] Implement `IGeoEngineClient` Provider Adapters (`PerplexitySonarAdapter`, `OpenAiSearchAdapter`, `GeminiGroundingAdapter`, `AnthropicAdapter`)
- [ ] **Sampling Engine**: Jitter & parallelism-limited (2-3 concurrent) 3-5 sample iterations calculating **Mention Rate %** and Average Position
- [ ] **Analysis-Level Deduplication**: 24-hour analysis caching with "Force Refresh" override option
- [ ] **URL Verification & Gold Opportunity Engine**: `CitationStatus` verification (`Valid`, `NonExistentPage`, `WrongDomain`, `Unreachable`). Generate Gold Opportunity Notification when AI cites missing pages!
- [ ] `PromptTemplate` DB library & `AiCache` integration
- [ ] `CompetitorsJson` competitor intelligence and GEO actionable recommendations engine
- [ ] **UI Slice**: GEO Visibility Map, Mention Rate Trend Charts (Recharts), Gold Opportunity Panel, and Competitor Comparison UI

### 🔵 Phase 4: AI Content Writer, Reporting & Admin Management
- [ ] **GEO-Optimized AI Content Writer**: `AiContentPlan` generating Princeton GEO-backed high-fact-density articles
- [ ] SuperAdmin Dashboard (Tenant management, B2B manual subscription CRUD, API usage logs, `/hangfire` auth)
- [ ] Pruning job for `RawResponseJson` (30-90 day retention)
- [ ] **UI Slice**: AI Content Writer Editor, SuperAdmin Management Panel, and PDF Report Export Engine

### 🟣 Phase 5 / Backlog (Future Vision)
- [ ] Google AI Overviews SERP API (DataForSEO / SerpApi) integration (`TargetEngine = GoogleAIO`)
- [ ] Google Search Console & Bing Webmaster Connectors
- [ ] White-Label Agency Portal (`agency.com` / `seo.agency.com`)
- [ ] Webhook & Email Notification Dispatcher

---

## 🧪 Verification Plan

### Automated Tests
- `Testcontainers` PostgreSQL integration tests (`dotnet test`) - **Executed in Phase 0 under concurrent multi-tenant execution**
- EF Core Global Query Filter isolation verification
- Scoped `TenantJobFilter` scope verification tests
- QuotaReservations ledger concurrency and double-refund prevention tests

### Manual Verification
- Tenant registration, live user/tenant/role verification, and SameSite=Lax HttpOnly access/refresh cookie verification
- `robots.txt` AI bot audit verification
- Perplexity Sonar API live citation and Mention Rate % calculation verification
