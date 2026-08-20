# AkironSeo - System Architecture & Technical Design Document (v5.0 - Definitive Production Architecture)

> [!IMPORTANT]
> **v5.0 Architectural Updates & Core Rules**:
> - **Language Standard**: All source code, comments, database schemas, DTOs, commit messages, and API responses are **100% IN ENGLISH**.
> - **Multi-Tenancy Schema Alignment**: Explicit `TenantId` column denormalized across ALL tenant-scoped tables (`TrackedKeywords`, `CrawlJobs`, `CrawlResults`, `SeoAudits`, `SiteSnapshots`, `GeoAnalyses`, `AeoSchemas`, `AiContentPlans`, `Notifications`, `QuotaReservations`, `EncryptedTenantApiKeys`).
> - **QuotaReservations Ledger Table (Table #22)**: Idempotent quota tracking via `JobId` (Unique), handling `Reserved`, `Committed`, and `Refunded` states cleanly.
> - **Subscription Period Reset**: Nightly job transitions expired subscriptions (`CurrentPeriodEnd < UtcNow`) to `PastDue` state, blocking new jobs until admin renewal.
> - **CitationStatus Verification Pipeline Step**: Automated HEAD/GET HTTP verification. `Valid` (2xx), `NonExistentPage` (404 - Gold Opportunity Trigger), `WrongDomain`, `Unreachable` (5xx/Timeout).
> - **Cron Parsing**: Powered by `Cronos` library for `NextScheduledRun` computation.

---

## 📌 1. High-Level System Topology (MVP Topology)

```
                                  +------------------------------------------------+
                                  |            Next.js 15/16 Client (App Router)   |
                                  |  - HttpOnly Cookie (Auth & Refresh Token: Lax) |
                                  |  - Dual-Language (EN/TR) & Light/Dark Themes   |
                                  |  - TanStack Query v5 & Recharts                |
                                  +-----------------------+------------------------+
                                                          | REST API / JSON (English)
                                                          v
                                  +------------------------------------------------+
                                  |   .NET 10 Web API Gateway / Global Controllers |
                                  |  - Tenant Resolver Middleware                  |
                                  |  - .NET Native RateLimiter Middleware          |
                                  +-----------------------+------------------------+
                                                          |
                 +----------------------------------------+----------------------------------------+
                 |                                        |                                        |
                 v                                        v                                        v
  +------------------------------+        +------------------------------+        +------------------------------+
  |    Application Layer (CQRS)  |        |    Hangfire Worker Engine    |        |    Infrastructure Services   |
  |  - MediatR [12.4.0, 13.0.0)  |        |  - Scoped TenantJobFilter    |        |  - Atomic Quota & Reconcile  |
  |  - FluentValidation          |        |  - Idempotent Job Ledger     |        |  - Structured AI Adapters    |
  |  - Scoped Tenant Behavior    |        |  - Resilience (Polly/Http)   |        |  - Serilog / OpenTelemetry   |
  +--------------+---------------+        +--------------+---------------+        +--------------+---------------+
                 |                                       |                                       |
                 +---------------------------------------+---------------------------------------+
                                                         |
                                                         v
                                  +------------------------------------------------+
                                  |            PostgreSQL / NeonDB                 |
                                  | (Multi-Tenant Isolation via Global Filters)    |
                                  +------------------------------------------------+
```

---

## Authentication and request security

- The browser receives `akiron_access` and `akiron_refresh` as host-only, `HttpOnly`, `SameSite=Lax` cookies. Access is scoped to `/api`; refresh is scoped to `/api/v1/auth`.
- Login and registration return `SessionDto`; tokens never appear in JSON responses or browser storage.
- Refresh tokens are stored only as SHA-256 hashes. Rotation links tokens through `FamilyId` and `ReplacedByTokenHash`; reuse of a revoked token revokes every still-active token in that family.
- A new login revokes prior refresh families for that user, preserving the single-active-session behavior.
- Every validated access token is checked against the current user, tenant membership, role, and tenant deletion state. Role changes and tenant disablement therefore take effect without waiting for token expiry.
- Login, registration, and refresh use IP-partitioned fixed-window rate limits with no queue. Defaults are 5/minute, 3/hour, and 30/minute respectively.
- FluentValidation covers authentication and mutation inputs. API failures use RFC 7807 responses and include the request `correlationId`.
- `Secure=true` is the deployment default. The only supported exception is loopback HTTP development with an explicit `Auth:CookieSecure=false` setting.

The frontend uses TanStack Query for the session and server state. Concurrent `401` responses share one refresh request, and each original request is retried no more than once. A failed refresh clears the session cache and returns the browser to `/login`.

---

## 🏛️ 2. .NET 10 Clean Architecture & Global vs Tenant Entity Classification

Backend consists of 4 layers following Clean Architecture dependencies pointing inward to `Domain`.

### Entity Classification

#### 🌐 A. Global Entities (DO NOT Implement `IMultiTenant`)
EF Core Global Query Filter IS NOT applied to these tables:
- `User`, `RefreshToken`, `Plan`, `PromptTemplate`, `AiCache`, `GlobalSystemLog`

#### 🏢 B. Tenant-Scoped Entities (MUST Implement `IMultiTenant` & `TenantId` Column)
EF Core `x => !x.IsDeleted && x.TenantId == _tenantContext.CurrentTenantId` filter IS AUTOMATICALLY applied to all of these:
- `TenantUser`, `Subscription`, `EncryptedTenantApiKey`, `TenantFeature`, `Website`, `TrackedKeyword`, `CrawlJob`, `CrawlResult`, `SeoAudit`, `SiteSnapshot`, `GeoAnalysis`, `AeoSchema`, `AiContentPlan`, `Notification`, `QuotaReservation`, `ApiUsageLog`

---

## 🗄️ 3. Complete Database Schema (22 PostgreSQL Tables)

```
[Tenants] 1 --- * [TenantUsers] * --- 1 [Users] 1 --- * [RefreshTokens]
   |
   1 --- * [Subscriptions] * --- 1 [Plans]
   |
   1 --- * [TenantFeatures]
   |
   1 --- * [EncryptedTenantApiKeys]
   |
   1 --- * [QuotaReservations]
   |
   1 --- * [Websites] 1 --- * [TrackedKeywords] 1 --- * [GeoAnalyses]
               |                    |                       |
               |                    + --- * [Cronos]        + --- * [PromptTemplates]
               |
               1 --- * [CrawlJobs] 1 --- 1 [SeoAudits] 1 --- 1 [SiteSnapshots]
               |                          |
               + --- * [CrawlResults] <---+
               |
               1 --- * [AeoSchemas]
               |
               1 --- * [AiContentPlans]
               |
               1 --- * [Notifications]
```

### Complete 22 Table Definitions (100% English Schemas):

1. **Tenants**: `Id (Guid)`, `Name (string)`, `Slug (string)`, `CreatedAt (DateTime)`, `IsDeleted (bool)`
2. **TenantUsers**: `TenantId (Guid)`, `UserId (Guid)`, `Role (UserRoleEnum: Owner, Admin, Member)`, `JoinedAt (DateTime)`
3. **Users**: `Id (Guid)`, `Email (string)`, `PasswordHash (string)`, `FullName (string)`, `IsActive (bool)`, `CreatedAt (DateTime)`
4. **RefreshTokens**: `Id (Guid)`, `UserId (Guid)`, `TokenHash (string, unique)`, `FamilyId (Guid)`, `ExpiresAt (DateTime)`, `RevokedAt (DateTime?)`, `ReplacedByTokenHash (string?)`, `CreatedAt (DateTime)`. Raw refresh tokens are never persisted.
5. **EncryptedTenantApiKeys**: `Id (Guid)`, `TenantId (Guid)`, `Provider (AiProviderEnum: OpenAI, Perplexity, Gemini, Anthropic)`, `EncryptedKey (string - AES-256-GCM)`, `IsActive (bool)`, `CreatedAt (DateTime)`
6. **Plans**: `Id (Guid)`, `Name (string)`, `PriceMonthly (decimal)`, `LimitsJson (jsonb)`
7. **Subscriptions**: `Id (Guid)`, `TenantId (Guid)`, `PlanId (Guid)`, `Status (SubscriptionStatusEnum: Active, PastDue, Cancelled)`, `MonthlyLimitTokens (long)`, `UsedTokens (long)`, `CurrentPeriodStart (DateTime)`, `CurrentPeriodEnd (DateTime)`
8. **TenantFeatures**: `Id (Guid)`, `TenantId (Guid)`, `FeatureKey (string: GeoEnabled, AeoEnabled, WhiteLabel)`, `IsEnabled (bool)`
9. **QuotaReservations**: `Id (Guid)`, `TenantId (Guid)`, `SubscriptionId (Guid)`, `JobId (string - Unique Index)`, `EstimatedTokens (long)`, `ActualTokens (long?)`, `Status (ReservationStatusEnum: Reserved, Committed, Refunded)`, `CreatedAt (DateTime)`
10. **Websites**: `Id (Guid)`, `TenantId (Guid)`, `DomainUrl (string)`, `Name (string)`, `VerificationToken (string)`, `VerificationMethod (VerificationMethodEnum: DnsTxt, MetaTag)`, `IsVerified (bool)`, `BrandAliasesJson (jsonb: ["akironseo.com", "Akiron SEO"])`, `CreatedAt (DateTime)`, `IsDeleted (bool)` *(Partial Index: TenantId + DomainUrl WHERE IsDeleted=false)*
11. **TrackedKeywords**: `Id (Guid)`, `TenantId (Guid)`, `WebsiteId (Guid)`, `Keyword (string)`, `Language (string)`, `TargetEnginesJson (jsonb)`, `CronExpression (string)`, `NextScheduledRun (DateTime?)`, `IsActive (bool)`, `IsDeleted (bool)`, `CreatedAt (DateTime)` *(Composite Index: IsActive + NextScheduledRun)*
12. **CrawlJobs**: `Id (Guid)`, `TenantId (Guid)`, `WebsiteId (Guid)`, `Status (CrawlStatusEnum: Pending, Running, Completed, Failed)`, `PagesDiscovered (int)`, `StartedAt (DateTime?)`, `CompletedAt (DateTime?)`
13. **CrawlResults**: `Id (Guid)`, `TenantId (Guid)`, `CrawlJobId (Guid)`, `PageUrl (string)`, `StatusCode (int)`, `Title (string)`, `MetaDescription (string)`, `H1Json (jsonb)`, `CanonicalUrl (string)`, `IssuesJson (jsonb)`, `PageSpeedMetricsJson (jsonb - Nullable: homepage + N critical pages)`
14. **SeoAudits**: `Id (Guid)`, `TenantId (Guid)`, `CrawlJobId (Guid)`, `WebsiteId (Guid)`, `OverallScore (int)`, `RobotsTxtAiStatusJson (jsonb: GPTBot, ClaudeBot, PerplexityBot, Google-Extended)`, `CreatedAt (DateTime)`
15. **SiteSnapshots**: `Id (Guid)`, `TenantId (Guid)`, `SeoAuditId (Guid)`, `WebsiteId (Guid)`, `Score (int)`, `TotalPagesCount (int)`, `TotalIssuesCount (int)`, `CreatedAt (DateTime)`
16. **GeoAnalyses**: `Id (Guid)`, `TenantId (Guid)`, `TrackedKeywordId (Guid)`, `RunGroupId (Guid)`, `TargetEngine (TargetLlmEnum)`, `ModelUsed (string)`, `PromptTemplateId (Guid - FK)`, `IsMentioned (bool)`, `Position (int?)`, `MentionType (MentionTypeEnum: Text, Citation, Both)`, `CitationStatus (CitationStatusEnum: Valid, NonExistentPage, WrongDomain, Unreachable)`, `CitationUrl (string)`, `CompetitorsJson (jsonb)`, `RawResponseJson (jsonb - 30-90 day pruning)`, `CreatedAt (DateTime)`
17. **PromptTemplates**: `Id (Guid - Global)`, `Type (PromptTypeEnum: Geo, Seo, Aeo, Content)`, `Version (int)`, `PromptText (string)`, `VariablesJson (jsonb)`
18. **AiCaches**: `Id (Guid - Global)`, `HashKey (string)`, `TaskType (string)`, `ResponseText (string)`, `ExpiresAt (DateTime)`
19. **GlobalSystemLogs**: `Id (Guid - Global)`, `LogLevel (string)`, `Message (string)`, `Exception (string)`, `CorrelationId (string)`, `Timestamp (DateTime)`
20. **AeoSchemas**: `Id (Guid)`, `TenantId (Guid)`, `WebsiteId (Guid)`, `PageUrl (string)`, `SchemaType (SchemaTypeEnum)`, `JsonLdOutput (string)`, `LlmsTxtOutput (string)`, `IsValid (bool)`, `CreatedAt (DateTime)`
21. **AiContentPlans**: `Id (Guid)`, `TenantId (Guid)`, `WebsiteId (Guid)`, `TargetKeyword (string)`, `GeneratedMarkdownContent (string)`, `Status (ContentStatusEnum)`, `TokensSpent (long)`, `CreatedAt (DateTime)`
22. **Notifications**: `Id (Guid)`, `TenantId (Guid)`, `UserId (Guid?)`, `Type (NotificationTypeEnum)`, `Title (string)`, `Message (string)`, `IsRead (bool)`, `CreatedAt (DateTime)`

---

## 🤖 4. GEO Pipeline & CitationStatus Verification Step

1. **Jitter & Parallelism-Limited Sampling**: 3-5 sample iterations with max 2-3 concurrency + random jitter.
2. **Analysis-Level Deduplication (24 Hours)**: Return existing `GeoAnalysis` if queried within 24h with a "Force Refresh" override.
3. **URL Verification Engine Step**:
   - Send HTTP `HEAD` request (fallback to `GET`) with 5-second timeout and redirect tracking.
   - User's domain + 2xx status -> `CitationStatus.Valid`
   - User's domain + 404 status -> `CitationStatus.NonExistentPage` (**Triggers Gold Opportunity Notification**)
   - External domain -> `CitationStatus.WrongDomain`
   - Timeout / 5xx error -> `CitationStatus.Unreachable`
4. **Gold Opportunity Actionable Remediation**:
   > 💡 **Gold Opportunity Trigger**: *"AI generative engine tried to cite your domain for '[Keyword]', but the target page '/missing-path' returned 404. Create this page now using our AI Content Writer to capture instant GEO traffic!"*

---

## ⚡ 5. Quota Reservations Ledger & Scoped Hangfire Execution

1. **QuotaReservations Ledger Flow**:
   - `AtomicQuotaBehavior` inserts `QuotaReservation` record with `Status = Reserved` and atomically increments `Subscriptions.UsedTokens`.
   - **Job Failure**: `Status = Refunded`, atomically decrements `Subscriptions.UsedTokens`.
   - **Job Success**: `Status = Committed`, adjusts `Subscriptions.UsedTokens` with actual token usage difference.
2. **Hangfire Scoped Tenant Filter**:
   - `TenantJobFilter` activates `ITenantContext` within the job's DI `IServiceScope`.
   - `[AutomaticRetry(Attempts = 2)]` limits retries. Polly handles transient HTTP retries.
3. **Subscription Period Reset**:
   - Nightly Hangfire job checks `CurrentPeriodEnd < DateTime.UtcNow` for active subscriptions and updates status to `PastDue`, blocking new job submissions until manual wire-transfer/admin renewal.

---

## 🎨 6. Frontend Internationalization & Design Standards

- **Language Support**: Dual-language UI (English & Turkish toggle via `next-intl` or React i18n context). All API payloads and standard defaults are in **English**.
- **Theme Support**: Seamless Light Mode & Dark Mode toggle (Tailwind CSS `dark:` class strategy + CSS variables).
