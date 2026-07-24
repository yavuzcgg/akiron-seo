# AkironSeo - Sistem Mimarısı ve Teknik Tasarım Dokümanı (System Design Document v3.0)

> [!IMPORTANT]
> **v3.0 Mimari ve Prod-Ready Güncellemeleri**:
> - **MediatR Sürümü**: `MediatR 12.4.1` (MIT lisanslı 12.x serisinin en güncel kararlı sürümü).
> - **Hangfire + Tenant Context Tuzağı Çözümü**: Background Job'lar HTTP Context'e sahip olmadığından `TenantJobFilter` / `TenantJobActivator` ile `TenantId` scope'u job başlangıcında set edilecek. SuperAdmin için `IgnoreQueryFilters()` tanımlandı.
> - **Atomik Kota Rezervasyonu (Race Condition Çözümü)**: Pre-flight kontrolü yerine DB seviyesinde koşullu atomik update (`UPDATE Subscriptions SET UsedTokens = UsedTokens + @cost WHERE UsedTokens + @cost <= Limit`) veya Redis Lua script.
> - **BrandAliases Temizliği**: `BrandAliasesJson` sadece `Websites` tablosunda saklanır (Tenants altından kaldırıldı).
> - **Soft Delete Partial Index**: Unique indeksler için EF Core `.HasFilter("\"IsDeleted\" = false")` partial index tanımı.
> - **Crawler & Snapshot Ayrımı**: `CrawlJob`, `CrawlQueue`, `CrawlResult` ve zaman serisi kıyaslaması için `SiteSnapshot` & `SnapshotDiff` yapıları.
> - **AI Prompt Library & Cache**: DB tabanlı `PromptTemplate`, `AiCache` ve `ICostCalculator` servis ayrımı.

---

## 📌 1. Yüksek Seviye Sistem Topolojisi (High-Level Architecture)

```
                                  +------------------------------------------------+
                                  |            Next.js 15/16 Client (App Router)   |
                                  |  - HttpOnly Cookie (Auth & Refresh Token: Lax) |
                                  |  - TanStack Query v5 & Recharts                |
                                  +-----------------------+------------------------+
                                                          | REST API / JSON
                                                          v
                                  +------------------------------------------------+
                                  |   .NET 10 Web API Gateway / Global Controllers |
                                  |  - Tenant Resolver Middleware                  |
                                  |  - Rate Limiting Middleware (Redis / In-Memory)|
                                  +-----------------------+------------------------+
                                                          |
                 +----------------------------------------+----------------------------------------+
                 |                                        |                                        |
                 v                                        v                                        v
  +------------------------------+        +------------------------------+        +------------------------------+
  |    Application Layer (CQRS)  |        |    Hangfire Worker Engine    |        |    Infrastructure Services   |
  |  - MediatR 12.4.1            |        |  - TenantJobFilter Scope     |        |  - Redis Cache & Quotas      |
  |  - FluentValidation          |        |  - Atomic Quota Reservation |        |  - Structured AI Adapters    |
  |  - Tenant Pipeline Behavior  |        |  - Resilience (Polly/Http)   |        |  - Serilog / OpenTelemetry   |
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

## 🏛️ 2. .NET 10 Clean Architecture & CQRS Yapısı

Backend 4 katmandan oluşur. Katmanlar arasındaki bağımlılık oku sadece **içe (Domain'e)** doğrudur:

```
AkironSeo.API (Presentation)
    ├──> AkironSeo.Infrastructure
    │       └──> AkironSeo.Application
    │               └──> AkironSeo.Domain
    └──> AkironSeo.Application
```

### A. Domain Katmanı (`AkironSeo.Domain`)
- **Entities**: 
  - Multi-Tenancy & Auth: `Tenant`, `User`, `TenantUser`, `Role`, `RefreshToken`, `Plan`, `Subscription`, `EncryptedTenantApiKey` (BYOK), `TenantFeature` (Feature Flags).
  - Core Business & Crawler: `Website`, `TrackedKeyword`, `CrawlJob`, `CrawlResult`, `SiteSnapshot`, `SnapshotDiff`, `SeoAudit`, `SeoAuditPage`, `GeoAnalysis`, `AeoSchema`, `AiContentPlan`, `Notification`.
  - AI Engine Support: `PromptTemplate`, `AiCache`, `ApiUsageLog`.
- **Value Objects**: `DomainUrl`, `ScoreValue`, `GeoCitation`.
- **Enums**: `UserRoleEnum`, `TargetLlmEnum` (Perplexity, OpenAiSearch, GeminiGrounding, Anthropic, GoogleAIO), `SchemaTypeEnum`, `AuditStatusEnum`, `MentionTypeEnum`, `CrawlStatusEnum`.
- **Global Interfaces**: `ISoftDelete` (`IsDeleted`, `DeletedAt`), `IMultiTenant` (`TenantId`).

### B. Application Katmanı (`AkironSeo.Application`)
- **Commands & Queries** (MediatR 12.4.1):
  - **Commands**: `CreateTenantCommand`, `AddWebsiteCommand`, `RunGeoAnalysisGroupCommand`, `GenerateAeoSchemaCommand`, `CreateSiteSnapshotCommand`.
  - **Queries**: `GetWebsiteAuditsQuery`, `GetSnapshotDiffQuery`, `GetGeoVisibilityTrendQuery`, `GetTenantQuotaUsageQuery`.
- **Pipeline Behaviors**:
  1. `TenantContextBehavior`: İsteği yapan kullanıcının `TenantId` bilgisini context'e enjekte eder.
  2. `AtomicQuotaBehavior`: DB seviyesinde koşullu atomik güncellemeyle kota rezervasyonu yapar.
  3. `ValidationBehavior`: FluentValidation doğrulaması.
  4. `LoggingBehavior`: Serilog ile korelasyon takibi (TenantId, UserId, JobId).

### C. Infrastructure Katmanı (`AkironSeo.Infrastructure`)
- **PostgreSQL & EF Core 10**:
  - `AkironDbContext`: Global Query Filter (`x => !x.IsDeleted && x.TenantId == _tenantContext.CurrentTenantId`).
  - Partial Unique Indexes (`.HasFilter("\"IsDeleted\" = false")`).
- **Hangfire Job Scope & Tenant Filter**:
  - `TenantJobFilter`: Job parametrelerindeki `TenantId`'yi okuyup job çalışırken `ITenantContext` scope'unu set eder.
- **Structured AI Engine Adapters (`IGeoEngineClient`) & Cost Engine**:
  - `ICostCalculator`: OpenAI, Perplexity, Gemini, Anthropic için ayrı maliyet hesaplama.
  - `PerplexitySonarAdapter`, `OpenAiSearchAdapter`, `GeminiGroundingAdapter`, `AnthropicWebSearchAdapter`.
  - Concurrency jitter & 2-3 paralellik sınırı (BYOK rate limit koruması).
- **Resilience**: `Microsoft.Extensions.Http.Resilience` ile Polly retry + exponential backoff + circuit breaker.

### D. Presentation Katmanı (`AkironSeo.API`)
- JWT Access Token + HttpOnly Refresh Cookie (`SameSite=Lax`).
- `/hangfire` Dashboard SuperAdmin Authorization Middleware koruması.
- RFC 7807 `ProblemDetails` standart hata yanıtları.

---

## 🗄️ 3. Veritabanı Şeması (PostgreSQL ER Modeli v3.0)

```
[Tenants] 1 --- * [TenantUsers] * --- 1 [Users]
   |
   1 --- * [Subscriptions] * --- 1 [Plans]
   |
   1 --- * [TenantFeatures]
   |
   1 --- * [EncryptedTenantApiKeys]
   |
   1 --- * [Websites] 1 --- * [TrackedKeywords] 1 --- * [GeoAnalyses]
               |                    |
               |                    + --- * [PromptTemplates]
               |
               1 --- * [CrawlJobs] 1 --- * [CrawlResults]
               |
               1 --- * [SiteSnapshots] 1 --- 1 [SnapshotDiffs]
               |
               1 --- * [SeoAudits] 1 --- * [SeoAuditPages]
               |
               1 --- * [AeoSchemas]
               |
               1 --- * [AiContentPlans]
```

### Detaylı Tablo Yapıları:

1. **Tenants**: `Id (Guid)`, `Name`, `Slug`, `CreatedAt`, `IsDeleted`
2. **TenantUsers**: `TenantId`, `UserId`, `Role (Owner, Admin, Member)`, `JoinedAt`
3. **Users**: `Id`, `Email`, `PasswordHash`, `FullName`, `IsActive`, `CreatedAt`
4. **EncryptedTenantApiKeys (BYOK)**: `Id`, `TenantId`, `Provider (OpenAI, Perplexity)`, `EncryptedKey (AES-256-GCM)`, `IsActive`, `CreatedAt`
5. **Plans**: `Id`, `Name`, `PriceMonthly`, `LimitsJson (jsonb)`
6. **Subscriptions**: `Id`, `TenantId`, `PlanId`, `Status`, `MonthlyLimitTokens`, `UsedTokens`, `CurrentPeriodStart`, `CurrentPeriodEnd`
7. **TenantFeatures**: `Id`, `TenantId`, `FeatureKey (GeoEnabled, AeoEnabled, WhiteLabel)`, `IsEnabled`
8. **Websites**: `Id`, `TenantId`, `DomainUrl`, `Name`, `VerificationToken`, `VerificationMethod`, `IsVerified`, `BrandAliasesJson (jsonb: ["akironseo.com", "Akiron SEO"])`, `CreatedAt`, `IsDeleted` *(Partial unique index on TenantId + DomainUrl WHERE IsDeleted=false)*
9. **TrackedKeywords**: `Id`, `WebsiteId`, `Keyword`, `Language`, `TargetEnginesJson (jsonb)`, `FrequencyDays`, `CronExpression`, `NextScheduledRun`, `IsActive`, `CreatedAt`
10. **CrawlJobs**: `Id`, `WebsiteId`, `Status (Pending, Running, Completed, Failed)`, `PagesDiscovered`, `StartedAt`, `CompletedAt`
11. **CrawlResults**: `Id`, `CrawlJobId`, `PageUrl`, `StatusCode`, `Title`, `MetaDesc`, `H1Json`, `CanonicalUrl`, `IssuesJson`
12. **SiteSnapshots**: `Id`, `WebsiteId`, `Score`, `TotalPagesCount`, `TotalIssuesCount`, `CreatedAt`
13. **SnapshotDiffs**: `Id`, `SnapshotId`, `PreviousSnapshotId`, `NewIssuesCount`, `FixedIssuesCount`, `ScoreChange`, `CreatedAt`
14. **SeoAudits**: `Id`, `WebsiteId`, `OverallScore`, `RobotsTxtAiStatusJson (jsonb: GPTBot, ClaudeBot, PerplexityBot, Google-Extended)`, `CreatedAt`
15. **SeoAuditPages**: `Id`, `SeoAuditId`, `PageUrl`, `StatusCode`, `Title`, `MetaDescription`, `PageSpeedMetricsJson (Nullable - sadece anasayfa + N kritik sayfa)`, `IssuesJson`
16. **GeoAnalyses**: `Id`, `TrackedKeywordId`, `RunGroupId (Guid)`, `TargetEngine`, `ModelUsed`, `PromptTemplateId`, `IsMentioned (bool)`, `Position (int?)`, `MentionType (Text, Citation, Both, Fabricated)`, `CitationUrl`, `CompetitorsJson (jsonb)`, `RawResponseJson (jsonb - 30-90 gün pruning politikası)`, `CreatedAt`
17. **PromptTemplates**: `Id`, `Type (Geo, Seo, Aeo, Content)`, `Version`, `PromptText`, `VariablesJson`
18. **AiCaches**: `Id`, `HashKey`, `Provider`, `PromptHash`, `ResponseText`, `ExpiresAt`
19. **AeoSchemas**: `Id`, `WebsiteId`, `PageUrl`, `SchemaType`, `JsonLdOutput (text)`, `LlmsTxtOutput (text)`, `IsValid`, `CreatedAt`
20. **AiContentPlans**: `Id`, `WebsiteId`, `TargetKeyword`, `GeneratedMarkdownContent`, `Status`, `TokensSpent`, `CreatedAt`
21. **Notifications**: `Id`, `TenantId`, `Type (CrawlFinished, QuotaAlert, GeoScoreChanged)`, `Title`, `Message`, `IsRead`, `CreatedAt`
22. **ApiUsageLogs**: `Id`, `TenantId`, `UserId`, `JobId`, `ServiceName`, `TokensUsed`, `EstimatedCostUsd`, `Timestamp`

---

## 🤖 4. GEO Analiz Boru Hattı & Sampling Engine (v3.0)

1. **Jitter & Paralellik Sınırlı Sampling**:
   - 3-5 örneklem grubu çağrısı yaparken BYOK anahtarlarının rate limit yememesi için çağrılar **2-3 paralellik sınırı ve jitter (rastgele gecikme)** ile yürütülür.
2. **AI Cache Check**:
   - Aynı kelime & provider son 24 saat içinde sorgulanmışsa `AiCaches` üzerinden dönülür, API harcaması engellenir.
3. **Mention Rate % & Position**:
   - $\text{Mention Rate} = \left( \frac{\text{Markanın anıldığı çalıştırma sayısı}}{\text{Toplam Örneklem (3-5)}} \right) \times 100$
4. **Halüsinasyon Tespiti (`Fabricated`)**:
   - LLM marka adını anıp geçersiz/uydurma bir URL vermişse `MentionType = Fabricated` olarak işaretlenir.

---

## ⚡ 5. Atomik Kota Rezervasyonu & Hangfire Scope

- **Atomik Kota Rezervasyonu**:
  ```sql
  UPDATE "Subscriptions"
  SET "UsedTokens" = "UsedTokens" + @estimatedCost
  WHERE "Id" = @subId AND ("UsedTokens" + @estimatedCost) <= "MonthlyLimitTokens";
  ```
  Etkilenen satır 0 ise işlem reddedilir (Race condition sıfırlanır).
- **Hangfire Job Scope**:
  - `TenantJobFilter` sınıfı `IClientFilter` ve `IServerFilter` arayüzlerini uygulayarak `TenantId` context'ini job başlatılırken aktif eder.

---

## 🔒 6. Güvenlik & Auth

- **SameSite=Lax Cookie**: Refresh token cookie `SameSite=Lax` ayarlanarak sosyal login / OAuth dönüşlerinde oturum kaybı engellenir.
- **Hangfire Dashboard Auth**: `/hangfire` adresi `SuperAdminAuthorizeFilter` ile yetkilendirilir.
- **BYOK Encryption**: Envanterdeki API key'ler `AES-256-GCM` ile env/KeyVault master key kullanılarak şifrelenir.
