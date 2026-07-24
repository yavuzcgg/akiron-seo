# AkironSeo - Sistem Mimarısı ve Teknik Tasarım Dokümanı (System Design Document v2.0)

> [!IMPORTANT]
> **Mimari Güncellemeler (v2.0)**:
> - **Hedef Framework**: .NET 10 LTS (Kasım 2025 çıkışlı, 2028'e kadar destekli LTS).
> - **Lisans Uyumluluğu**: MediatR `v12.1.1` (Son ücretsiz MIT lisanslı sürüm) veya özel `IMediator` dispatcher soyutlaması.
> - **Multi-Tenancy**: Baştan entegre edilmiş `Tenants` -> `TenantUsers` -> `Websites` zinciri ve EF Core Global Query Filter izolasyonu.
> - **Abonelik & Kota**: `Plans` + `Subscriptions` veritabanı modelleri + Redis sayaç takibi.
> - **GEO Pipeline**: Provider-native adapter kalıbı (`IGeoEngineClient`), 3-5 örneklem grubu (Mention Rate %), marka varyantları (`BrandAliases`) ve dinamik prompt şablonları.
> - **AIO vs GEO Netleştirmesi**: Gemini grounding GEO için; Google AI Overviews takibi SERP API (DataForSEO / SerpApi) ile yapılması.

---

## 📌 1. Yüksek Seviye Sistem Topolojisi (High-Level Architecture)

```
                                  +------------------------------------------------+
                                  |            Next.js 15/16 Client (App Router)   |
                                  |  - HttpOnly Cookie (Auth & Refresh Token)      |
                                  |  - TanStack Query v5 & Recharts                |
                                  +-----------------------+------------------------+
                                                          | REST API / JSON
                                                          v
                                  +------------------------------------------------+
                                  |   .NET 10 Web API Gateway / Global Controllers |
                                  |  - Tenant Resolver Middleware                  |
                                  |  - Rate Limiting Middleware (Redis)            |
                                  +-----------------------+------------------------+
                                                          |
                 +----------------------------------------+----------------------------------------+
                 |                                        |                                        |
                 v                                        v                                        v
  +------------------------------+        +------------------------------+        +------------------------------+
  |    Application Layer (CQRS)  |        |    Background Worker Engine  |        |    Infrastructure Services   |
  |  - MediatR 12.x / Custom     |        |  - Hangfire (Postgres Storage)|       |  - Redis Cache & Quotas      |
  |  - FluentValidation          |        |  - Scheduled TrackedKeywords |        |  - Structured AI Adapters    |
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
  - Multi-Tenancy & Auth: `Tenant`, `User`, `TenantUser`, `Role`, `RefreshToken`, `Plan`, `Subscription`.
  - Core Business: `Website`, `TrackedKeyword`, `SeoAudit`, `SeoAuditPage`, `GeoAnalysis`, `AeoSchema`, `AiContentPlan`, `ApiUsageLog`, `EncryptedTenantApiKey` (BYOK).
- **Value Objects**: `DomainUrl`, `ScoreValue`, `GeoCitation`.
- **Enums**: `UserRoleEnum`, `TargetLlmEnum` (Perplexity, OpenAiSearch, GeminiGrounding, Anthropic), `SchemaTypeEnum`, `AuditStatusEnum`, `MentionTypeEnum`.
- **Global Interfaces**: `ISoftDelete` (`IsDeleted`, `DeletedAt`), `IMultiTenant` (`TenantId`).

### B. Application Katmanı (`AkironSeo.Application`)
- **Commands & Queries** (MediatR v12.1.1):
  - **Commands**: `CreateTenantCommand`, `AddWebsiteCommand`, `RunGeoAnalysisGroupCommand`, `GenerateAeoSchemaCommand`.
  - **Queries**: `GetWebsiteAuditsQuery`, `GetGeoVisibilityTrendQuery`, `GetTenantQuotaUsageQuery`.
- **Pipeline Behaviors**:
  1. `TenantContextBehavior`: İsteği yapan kullanıcının `TenantId` bilgisini context'e enjekte eder.
  2. `PreFlightQuotaBehavior`: AI / Crawl işlemi öncesinde veritabanı ve Redis kotasını doğrular.
  3. `ValidationBehavior`: FluentValidation ile otomatik DTO doğrulaması.
  4. `LoggingBehavior`: Serilog ile korelasyon takibi (TenantId, UserId, JobId).

### C. Infrastructure Katmanı (`AkironSeo.Infrastructure`)
- **PostgreSQL & EF Core 10**:
  - `AkironDbContext`: Global Query Filter (`x => !x.IsDeleted && x.TenantId == _tenantContext.CurrentTenantId`).
  - GIN Index konfigürasyonları (JSONB alanlar için).
- **Structured AI Engine Adapters (`IGeoEngineClient`)**:
  - `PerplexitySonarAdapter`: `citations` & `search_results` JSON parse.
  - `OpenAiSearchAdapter`: Responses API `web_search` + `url_citation` annotations.
  - `GeminiGroundingAdapter`: Google Search grounding `groundingMetadata` parse.
  - `AnthropicWebSearchAdapter`: Claude web search tool adapter.
- **Resilience**: `Microsoft.Extensions.Http.Resilience` ile AI API çağrılarına Idempotency key destekli retry + exponential backoff + circuit breaker.
- **Background Jobs**: Hangfire (PostgreSQL Storage üzerinde çalışır, Pooled connection string ayrıştırılmıştır).

### D. Presentation Katmanı (`AkironSeo.API`)
- JWT Access Token + HttpOnly Refresh Cookie akışı.
- RFC 7807 `ProblemDetails` standart hata dönütleri.
- Next.js Route Handlers ile entegre Auth proxy.

---

## 🗄️ 3. Veritabanı Şeması (PostgreSQL ER Modeli)

```
[Tenants] 1 --- * [TenantUsers] * --- 1 [Users]
   |
   1 --- * [Subscriptions] * --- 1 [Plans]
   |
   1 --- * [Websites] 1 --- * [TrackedKeywords] 1 --- * [GeoAnalyses]
               |                                
               1 --- * [SeoAudits] 1 --- * [SeoAuditPages]
               |
               1 --- * [AeoSchemas]
               |
               1 --- * [AiContentPlans]
```

### Detaylı Entity Şeması:

1. **Tenants**: `Id (Guid)`, `Name`, `Slug`, `BrandAliases (jsonb)`, `CreatedAt`, `IsDeleted`
2. **TenantUsers**: `TenantId`, `UserId`, `Role (Owner, Admin, Member)`, `JoinedAt`
3. **Users**: `Id`, `Email`, `PasswordHash`, `FullName`, `IsActive`, `CreatedAt`
4. **Plans**: `Id`, `Name`, `PriceMonthly`, `LimitsJson (jsonb: max_websites, max_keywords, monthly_ai_tokens, geo_sampling_count)`
5. **Subscriptions**: `Id`, `TenantId`, `PlanId`, `Status (Active, Cancelled, PastDue)`, `CurrentPeriodStart`, `CurrentPeriodEnd`
6. **Websites**: `Id`, `TenantId`, `DomainUrl`, `Name`, `VerificationToken`, `VerificationMethod (DnsTxt, MetaTag)`, `IsVerified`, `BrandAliasesJson (jsonb)`, `CreatedAt`, `IsDeleted`
7. **TrackedKeywords**: `Id`, `WebsiteId`, `Keyword`, `Language`, `TargetEnginesJson (jsonb)`, `FrequencyDays`, `IsActive`, `CreatedAt`
8. **SeoAudits**: `Id`, `WebsiteId`, `OverallScore`, `PagesCrawledCount`, `RobotsTxtAiStatusJson (jsonb: GPTBot, ClaudeBot, PerplexityBot, Google-Extended)`, `CreatedAt`
9. **SeoAuditPages**: `Id`, `SeoAuditId`, `PageUrl`, `StatusCode`, `Title`, `MetaDescription`, `H1StructureJson`, `PageSpeedMetricsJson (PageSpeed Insights API)`, `IssuesJson (jsonb)`
10. **GeoAnalyses**: `Id`, `TrackedKeywordId`, `RunGroupId (Guid)`, `TargetEngine`, `ModelUsed`, `PromptVariantId`, `IsMentioned (bool)`, `Position (int?)`, `MentionType (Text, Citation, Both)`, `CitationUrl`, `CompetitorsJson (jsonb)`, `RawResponseJson (jsonb)`, `CreatedAt`
11. **AeoSchemas**: `Id`, `WebsiteId`, `PageUrl`, `SchemaType`, `JsonLdOutput (text)`, `LlmsTxtOutput (text)`, `IsValid`, `CreatedAt`
12. **ApiUsageLogs**: `Id`, `TenantId`, `UserId`, `JobId`, `ServiceName`, `TokensUsed`, `EstimatedCostUsd`, `Timestamp`

---

## 🤖 4. GEO & AIO Analiz Boru Hattı (Pipeline v2.0)

1. **Keyword Gruplama & Örnekleme (Sampling)**:
   - Bir anahtar kelime için stokastik LLM gürültüsünü engellemek adına **3 ile 5 örneklem çağrısı** (RunGroup) eşzamanlı olarak yapılır.
2. **Provider-Native Adapter Çağrısı**:
   - **Perplexity**: `citations` array'i doğrudan çekilir.
   - **OpenAI**: `web_search` `url_citation` annotation'ları ayrıştırılır.
   - **Gemini**: `groundingMetadata` içindeki search chunks listesi okunur.
3. **Mention Rate % ve Ortalama Sıra (Position) Hesabı**:
   - $\text{Mention Rate} = \left( \frac{\text{Ağızdan Kaçırılmayan/Citation Alınan Çalıştırma Sayısı}}{\text{Toplam Örneklem (3-5)}} \right) \times 100$
   - LLM cevabında kullanıcının sırası ($Position$) hesaplanır.
4. **Rakip İstihbaratı (Competitor Intelligence)**:
   - LLM'in öne çıkardığı rakipler `CompetitorsJson` içine kaydedilir.
5. **AI Bot Auditing & Quick Wins**:
   - `robots.txt` taraması yapılarak `GPTBot`, `ClaudeBot`, `PerplexityBot`, `Google-Extended` gibi yapay zeka tarayıcılarının engellenip engellenmediği müşteriye doğrudan raporlanır.
   - AEO modülünde **`llms.txt`** dosyası otomatik üretilir.

---

## ⚡ 5. Resilience, Quota ve Asenkron Mimari

- **Resilience Engine**: AI API çağrılarına `Microsoft.Extensions.Http.Resilience` ile retry, circuit breaker uygulanır. Idempotency key ile tekrarlı harcama önlenir.
- **Pre-Flight Quota Check**: Her job başlamadan veritabanı ve Redis üzerinden kullanıcının `Subscription` ve `Plan` limitleri kontrol edilir; yetersiz bakiyede job `QuotaExceeded` statüsüne alınır.
- **Polling & Job Tracker**: `GET /api/v1/jobs/{jobId}` polling mekanizması.

---

## 🔒 6. Güvenlik & BYOK (Bring Your Own Key)

- **JWT Güvenliği**: Refresh token `httpOnly`, `SameSite=Strict`, `Secure` cookie olarak saklanır. İstemci tarafında Next.js Route Handler üzerinden tazeleme yapılır.
- **BYOK (Kendi API Anahtarını Getir)**: Ajansların kendi OpenAI veya Perplexity API anahtarlarını şifreli (`AES-256-GCM`) olarak saklamalarına izin verilir.

---

## 🧪 7. Test Stratejisi

- **Integration Tests**: `Testcontainers` ile gerçek Docker PostgreSQL konteynerine karşı multi-tenancy izolasyonu ve EF Core Global Query Filter testleri.
- **AI Engine Mocks**: `IGeoEngineClient` arayüzü arkasında deterministik JSON fixture'ları ile birim testler.
