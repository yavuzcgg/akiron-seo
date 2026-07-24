# AkironSeo - Sistem Mimarısı ve Teknik Tasarım Dokümanı (System Design Document v4.0 - Production Definitive)

> [!IMPORTANT]
> **v4.0 Üretim (Production) Mimarisi ve Kritik Düzeltmeler**:
> - **MediatR Sürüm Politikası**: NuGet paket tanımı `[12.4.0, 13.0.0)` aralığına çekildi (Tüm 12.x serisi MIT lisanslıdır).
> - **Veri Çakışması Temizliği**: `SeoAuditPages` ve `CrawlQueue` kaldırıldı. Tek veri zinciri: `CrawlJob` → `SeoAudit` (1-1) → `CrawlResults`. `SiteSnapshot` özet satırı olarak `SeoAudit` bitişinde doldurulur. `SnapshotDiff` PostgreSQL `LAG()` pencere fonksiyonuyla hesaplanır.
> - **Sampling Engine & AiCache Ayrımı**: HTTP seviyesinde AI response caching sampling sırasında pasifleşir (stokastik gürültüyü ölçebilmek için). Cache sadece analiz seviyesinde (Deduplication: 24 saat içinde aynı kelime sorgulanırsa bayat analiz uyarısı + "Zorla Yenile" butonu) ve deterministik işlerde (AEO/Content Writer) çalışır.
> - **Kota Yaşam Döngüsü & İade**: Job başarısızlığında rezerve edilen token iade edilir (`UsedTokens -= estimatedCost`). Başarı durumunda gerçek harcamayla düzeltilir. Hangfire `JobId` bazlı idempotent rezervasyon + `[AutomaticRetry(Attempts = 2)]`.
> - **ITenantContext Scoped DI Güvenliği**: `TenantJobFilter` job DI scope'u içinde `ITenantContext`'i set eder (Singleton ezilmesini ve eşzamanlı job'larda cross-tenant veri sızıntısını engeller).
> - **Redis Yalınlaştırması (MVP)**: v1 MVP tek sunucuda çalışacağından Redis çıkarıldı. Kota ve Deduplication Postgres'te, Rate Limiting .NET `RateLimiter` middleware'inde döner.
> - **Özel GEO Özelliği (`CitationStatus`)**: LLM'in anıp sitede bulamadığı linkler `CitationStatus = NonExistentPage` olarak işaretlenip *"AI bu konuda seni kaynak göstermek istiyor ama sayfa yok — hemen oluştur!"* fırsat bildirimi üretilir.

---

## 📌 1. Yüksek Seviye Sistem Topolojisi (High-Level Architecture - MVP v4.0)

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
                                  |  - .NET Native RateLimiter Middleware          |
                                  +-----------------------+------------------------+
                                                          |
                 +----------------------------------------+----------------------------------------+
                 |                                        |                                        |
                 v                                        v                                        v
  +------------------------------+        +------------------------------+        +------------------------------+
  |    Application Layer (CQRS)  |        |    Hangfire Worker Engine    |        |    Infrastructure Services   |
  |  - MediatR [12.4.0, 13.0.0)  |        |  - Scoped TenantJobFilter    |        |  - Atomic Quota & Reconcile  |
  |  - FluentValidation          |        |  - Idempotent Job Reservation|        |  - Structured AI Adapters    |
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

## 🏛️ 2. .NET 10 Clean Architecture & Global vs Tenant Varlık Ayrımı

Backend 4 katmandan oluşur. Katmanlar arasındaki bağımlılık oku sadece **içe (Domain'e)** doğrudur.

### Varlık Katmanlaşması (Entity Classification)

#### 🌐 A. Global Varlıklar (`IMultiTenant` İMPLEMENTE ETMEZ)
EF Core Global Query Filter bu tablolara uygulanmaz:
- `User`, `RefreshToken`, `Plan`, `PromptTemplate`, `GlobalSystemLog`

#### 🏢 B. Tenant-Scoped Varlıklar (`IMultiTenant` İMPLEMENTE EDER)
EF Core `x => !x.IsDeleted && x.TenantId == _tenantContext.CurrentTenantId` filtresi otomatik uygulanır:
- `TenantUser`, `Subscription`, `EncryptedTenantApiKey`, `TenantFeature`, `Website`, `TrackedKeyword`, `CrawlJob`, `CrawlResult`, `SeoAudit`, `SiteSnapshot`, `GeoAnalysis`, `AeoSchema`, `AiContentPlan`, `Notification`, `ApiUsageLog`

---

## 🗄️ 3. Veritabanı Şeması (PostgreSQL ER Modeli v4.0)

```
[Tenants] 1 --- * [TenantUsers] * --- 1 [Users] 1 --- * [RefreshTokens]
   |
   1 --- * [Subscriptions] * --- 1 [Plans]
   |
   1 --- * [TenantFeatures]
   |
   1 --- * [EncryptedTenantApiKeys]
   |
   1 --- * [Websites] 1 --- * [TrackedKeywords] 1 --- * [GeoAnalyses]
               |                                            |
               |                                            + --- * [PromptTemplates]
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

### Detaylı 22 Tablo Şeması (Eksiksiz):

1. **Tenants**: `Id (Guid)`, `Name`, `Slug`, `CreatedAt`, `IsDeleted`
2. **TenantUsers**: `TenantId`, `UserId`, `Role (Enum: Owner, Admin, Member)`, `JoinedAt`
3. **Users**: `Id`, `Email`, `PasswordHash`, `FullName`, `IsActive`, `CreatedAt`
4. **RefreshTokens**: `Id`, `UserId`, `Token`, `ExpiresAt`, `IsRevoked`, `CreatedAt`
5. **EncryptedTenantApiKeys (BYOK)**: `Id`, `TenantId`, `Provider (OpenAI, Perplexity)`, `EncryptedKey (AES-256-GCM)`, `IsActive`, `CreatedAt`
6. **Plans**: `Id`, `Name`, `PriceMonthly`, `LimitsJson (jsonb)`
7. **Subscriptions**: `Id`, `TenantId`, `PlanId`, `Status (Active, PastDue, Cancelled)`, `MonthlyLimitTokens`, `UsedTokens`, `CurrentPeriodStart`, `CurrentPeriodEnd`
8. **TenantFeatures**: `Id`, `TenantId`, `FeatureKey (GeoEnabled, AeoEnabled, WhiteLabel)`, `IsEnabled`
9. **Websites**: `Id`, `TenantId`, `DomainUrl`, `Name`, `VerificationToken`, `VerificationMethod`, `IsVerified`, `BrandAliasesJson (jsonb)`, `CreatedAt`, `IsDeleted` *(Partial Index: TenantId + DomainUrl WHERE IsDeleted=false)*
10. **TrackedKeywords**: `Id`, `WebsiteId`, `Keyword`, `Language`, `TargetEnginesJson (jsonb)`, `CronExpression`, `NextScheduledRun`, `IsActive`, `CreatedAt` *(Composite Index: IsActive + NextScheduledRun)*
11. **CrawlJobs**: `Id`, `WebsiteId`, `Status (Pending, Running, Completed, Failed)`, `PagesDiscovered`, `StartedAt`, `CompletedAt`
12. **CrawlResults**: `Id`, `CrawlJobId`, `PageUrl`, `StatusCode`, `Title`, `MetaDesc`, `H1Json`, `CanonicalUrl`, `IssuesJson`, `PageSpeedMetricsJson (Nullable - sadece anasayfa + N kritik sayfa)`
13. **SeoAudits**: `Id`, `CrawlJobId`, `WebsiteId`, `OverallScore`, `RobotsTxtAiStatusJson (jsonb: GPTBot, ClaudeBot, PerplexityBot, Google-Extended)`, `CreatedAt`
14. **SiteSnapshots**: `Id`, `SeoAuditId`, `WebsiteId`, `Score`, `TotalPagesCount`, `TotalIssuesCount`, `CreatedAt`
15. **GeoAnalyses**: `Id`, `TrackedKeywordId`, `RunGroupId (Guid)`, `TargetEngine`, `ModelUsed`, `PromptTemplateId (FK)`, `IsMentioned (bool)`, `Position (int?)`, `MentionType (Enum: Text, Citation, Both)`, `CitationStatus (Enum: Valid, BrokenPath, WrongDomain, NonExistentPage)`, `CitationUrl`, `CompetitorsJson (jsonb)`, `RawResponseJson (jsonb - 30-90 gün pruning)`, `CreatedAt`
16. **PromptTemplates**: `Id (Global)`, `Type (Geo, Seo, Aeo, Content)`, `Version`, `PromptText`, `VariablesJson`
17. **AiCaches**: `Id (Global)`, `HashKey`, `TaskType`, `ResponseText`, `ExpiresAt`
18. **AeoSchemas**: `Id`, `WebsiteId`, `PageUrl`, `SchemaType`, `JsonLdOutput (text)`, `LlmsTxtOutput (text)`, `IsValid`, `CreatedAt`
19. **AiContentPlans**: `Id`, `WebsiteId`, `TargetKeyword`, `GeneratedMarkdownContent`, `Status`, `TokensSpent`, `CreatedAt`
20. **Notifications**: `Id`, `TenantId`, `UserId (Nullable)`, `Type`, `Title`, `Message`, `IsRead`, `CreatedAt`
21. **ApiUsageLogs**: `Id`, `TenantId`, `UserId`, `JobId`, `ServiceName`, `TokensUsed`, `EstimatedCostUsd`, `Timestamp`

---

## 🤖 4. GEO Pipeline, CitationStatus & Fırsat Bildirimi

1. **Jitter & Paralellik Sınırlı Sampling**:
   - 3-5 örneklem grubu çağrısı yaparken BYOK anahtarlarının rate limit yememesi için çağrılar **2-3 paralellik sınırı ve jitter (rastgele gecikme)** ile yürütülür.
2. **Analiz Seviyesi Deduplication (24 Saat)**:
   - Kullanıcı 24 saat içinde aynı kelimeyi tekrar sorgularsa yeni LLM çağrısı yapılmaz. Mevcut `GeoAnalysis` sonucu *"Son analiz X saat önce yapıldı"* uyarısıyla gösterilir. Kullanıcı isterse *"Zorla Yenile"* butonu ile yeni analiz başlatır.
3. **Fırsat Bildirimi (NonExistentPage Feature)**:
   - LLM marka adınızı kaynak gösterip alan adınızda var olmayan bir URL (`CitationStatus = NonExistentPage`) verdiyse sistem otomatik bildirim üretir:
   > 💡 **Altın Fırsat**: *"Yapay zeka [Kelime] konusunda sitenizi kaynak göstermek istiyor ancak '/ornek-sayfa' adresi sitenizde bulunamadı. Bu sayfayı oluşturarak GEO trafiğini anında yakalayabilirsiniz!"*

---

## ⚡ 5. Atomik Kota Yönetimi, Mutabakat & Scoped Hangfire Scope

1. **Atomik Rezervasyon & Mutabakat**:
   - Job başlangıcında tahmini maliyet atomik olarak düşülür:
     ```sql
     UPDATE "Subscriptions"
     SET "UsedTokens" = "UsedTokens" + @estimatedCost
     WHERE "Id" = @subId AND ("UsedTokens" + @estimatedCost) <= "MonthlyLimitTokens";
     ```
   - **Job Başarısız Olursa (Fail)**: `UsedTokens = UsedTokens - estimatedCost` ile kota iade edilir.
   - **Job Başarılı Olursa (Success)**: `UsedTokens = UsedTokens + (actualCost - estimatedCost)` ile mutabakat yapılır.
2. **Hangfire Scoped Tenant Filter**:
   - `TenantJobFilter` sınıfı `IServerFilter` aşamasında job'ın kendi DI Scope'u (`IServiceScope`) içerisinden `ITenantContext` alıp `TenantId`'yi set eder.
   - `[AutomaticRetry(Attempts = 2)]` ile Hangfire retry adedi sınırlanır; Polly HTTP seviyesindeki anlık hataları çözer.
3. **Dönüşüm Sıfırlama**:
   - Gece çalışan bir Hangfire Job'ı `CurrentPeriodEnd < DateTime.UtcNow` olan aboneliklerin `UsedTokens` değerini `0` yapar.

---

## 💳 6. MVP Ödeme ve Abonelik Kararı

- **v1 MVP Modeli**: **Havale / B2B Manuel Onay + Admin CRUD Paneli**.
  - `Subscriptions` ve `Plans` veritabanı altyapısı iyzico/PayTR/Paddle entegrasyonuna %100 hazırdır. SuperAdmin panelinden manuel abonelik atama/güncelleme yapılır.
