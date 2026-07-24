# AkironSeo: Yapay Zeka Destekli SEO, AIO, GEO ve AEO Optimizasyon Platformu (Implementation Plan v2.0)

Bu proje; klasik Arama Motoru Optimizasyonu (**SEO**), Yapay Zeka Optimizasyonu (**AIO**), Üretken Motor Optimizasyonu (**GEO**) ve Cevap Motoru Optimizasyonu (**AEO**) süreçlerini otomatize eden, veritabanı düzeyinde multi-tenant olan ve yapay zeka ile içerik/teknik strateji üreten enterprise düzeyde bir SaaS platformudur.

---

## 🛠️ Mimari & Teknolojik Altyapı (Güncellendi)

- **Backend**: **.NET 10 LTS** Web API (**Clean Architecture**: Domain, Application, Infrastructure, API katmanları)
- **CQRS & Pipeline**: **MediatR v12.1.1** (Son ücretsiz MIT lisanslı sürüm) + FluentValidation
- **Frontend**: **Next.js 15/16 (Latest)** App Router + React 19 + TypeScript + Tailwind CSS + TanStack Query v5 + Recharts
- **Veritabanı**: **PostgreSQL / NeonDB** (Entity Framework Core 10, Multi-Tenancy Global Query Filters)
- **Resilience & Http**: `Microsoft.Extensions.Http.Resilience` (Polly retry, exponential backoff, circuit breaker, idempotency key)
- **Yapay Zeka Adapte Motorları (`IGeoEngineClient`)**:
  - **Perplexity Sonar**: `citations` & `search_results` JSON adapter
  - **OpenAI**: Responses API + `web_search` `url_citation` adapter
  - **Gemini**: Google Search `groundingMetadata` adapter
  - **Anthropic**: Claude web search tool adapter
- **Arka Plan Görevleri**: Hangfire (PostgreSQL Storage) + .NET BackgroundService
- **Güvenlik & Auth**: Multi-Tenant RBAC + JWT Access Token + HttpOnly Refresh Cookie + BYOK (Bring Your Own Key AES-256-GCM)

---

## 📐 Clean Architecture Yapısı (.NET 10 Backend)

```
AkironSeo.Backend/
├── src/
│   ├── Core/
│   │   ├── AkironSeo.Domain/         # Multi-Tenant Entity'ler, Value Object'ler, Enum'lar, Interfaces
│   │   └── AkironSeo.Application/    # CQRS (MediatR 12.x), Pre-Flight Quota Behavior, Validation
│   ├── Infrastructure/
│   │   └── AkironSeo.Infrastructure/ # DbContext + Global Filters, AI Provider Adapters, Resilience, Hangfire
│   └── Presentation/
│       └── AkironSeo.API/            # Controllers, Tenant Middleware, HttpOnly Cookie Auth
└── tests/
    └── AkironSeo.IntegrationTests/   # Testcontainers (Real PostgreSQL Integration Tests)
```

---

## 📊 Veritabanı Modelleri (Core Schema v2.0)

1. **Multi-Tenancy & Auth**: `Tenant`, `TenantUser`, `User`, `Role`, `RefreshToken`, `EncryptedTenantApiKey` (BYOK)
2. **Subscriptions & Quotas**: `Plan` (limits_jsonb), `Subscription` (TenantId, PlanId, CurrentPeriod)
3. **Websites & Verification**: `Website` (TenantId, DomainUrl, VerificationToken, IsVerified, BrandAliasesJson)
4. **Tracked Keywords**: `TrackedKeyword` (WebsiteId, Keyword, Language, TargetEnginesJson, FrequencyDays)
5. **SEO & AI Bot Audits**: `SeoAudit` (OverallScore, RobotsTxtAiStatusJson) -> `SeoAuditPage` (PageUrl, StatusCode, Title, MetaDesc, H1Json, PageSpeedMetricsJson, IssuesJson)
6. **GEO Engine Analyses**: `GeoAnalysis` (TrackedKeywordId, RunGroupId, TargetEngine, ModelUsed, IsMentioned, Position, MentionType, CitationUrl, CompetitorsJson, RawResponseJson)
7. **AEO & Schema Data**: `AeoSchema` (WebsiteId, PageUrl, SchemaType, JsonLdOutput, LlmsTxtOutput, IsValid)
8. **AI Content Strategy**: `AiContentPlan` (WebsiteId, TargetKeyword, GeneratedMarkdown, Status, TokensSpent)
9. **Telemetry & Logs**: `ApiUsageLogs` (TenantId, UserId, JobId, ServiceName, TokensUsed, EstimatedCostUsd)

---

## 🚀 Faz Bazlı Uygulama Planı (Roadmap v2.0)

### ⚪ Faz 0: Multi-Tenancy, Plan & Kota Altyapısı (Öncelikli Temel)
- [ ] `.NET 10 Web API Clean Architecture` projelerini `.NET 10` hedefleyecek şekilde güncelleme
- [ ] `Tenant`, `User`, `TenantUser`, `Plan` ve `Subscription` Entity'lerinin yazılması
- [ ] EF Core 10 `AkironDbContext` üzerinde **Global Query Filter** (`TenantId` ve `IsDeleted` izolasyonu)
- [ ] `MediatR v12.1.1` paketinin eklenmesi ve `TenantContextBehavior` yazılması
- [ ] Next.js 15/16 App Router üzerinde **HttpOnly Cookie** tabanlı Login/Register ve Tenant Router yapısı

### 🔴 Faz 1: Proje Yönetimi, TrackedKeywords ve Site Doğrulama
- [ ] `Website` (Domain doğrulama: DNS TXT veya Meta Tag) ve `TrackedKeyword` modüllerinin yazılması
- [ ] BYOK (Kendi OpenAI / Perplexity API anahtarını tanımlama ve AES-256 şifreleme)
- [ ] Postgres storage ile **Hangfire** kurulumu (Connection pooling ayrıştırması)

### 🟡 Faz 2: SEO Tarayıcısı, AI Bot Denetimi & AEO Engine
- [ ] Parent-Child SEO Crawler (`SeoAudit` -> `SeoAuditPage` ilişkisi, PageSpeed Insights API entegrasyonu)
- [ ] **Quick-Win Audit**: `robots.txt` AI Bot Denetleyici (`GPTBot`, `ClaudeBot`, `PerplexityBot`, `Google-Extended` erişim analizi)
- [ ] AEO Generator: JSON-LD SSS/Makale şeması ve otomatik **`llms.txt`** dosyası üretici

### 🟢 Faz 3: GEO (Generative Engine Optimization) Structured Pipeline
- [ ] `IGeoEngineClient` Provider Adapter'ları (`PerplexitySonarAdapter`, `OpenAiSearchAdapter`, `GeminiGroundingAdapter`, `AnthropicAdapter`)
- [ ] **Sampling Engine**: Bir kelime için 3-5 örneklem grubu çağrısı yapıp **Mention Rate %** ve Ortalama Sıra (Position) hesabı
- [ ] `CompetitorsJson` rakip istihbaratı üreticisi ve GEO İyileştirme Tavsiye motoru

### 🔵 Faz 4: Dashboard, Raporlama & Integration Tests
- [ ] Next.js SuperAdmin Paneli (Tenant yönetimi, API harcamaları, token maliyet takibi)
- [ ] Next.js Tenant Dashboard (GEO Görünürlük Trend Grafikleri, AEO Şema İndirici, SEO Raporları)
- [ ] `Testcontainers` ile gerçek PostgreSQL konteyneri üzerinde izolasyon ve multi-tenant entegrasyon testleri

---

## 🧪 Verification Plan

### Automated Tests
- `Testcontainers` PostgreSQL entegrasyon testleri (`dotnet test`)
- EF Core Global Query Filter izolasyon doğrulaması (Tenant A verisine Tenant B erişememeli)
- MediatR Pre-Flight Quota ve Validation unit testleri

### Manual Verification
- Tenant kaydı, kullanıcı rol doğrulaması ve HttpOnly Cookie JWT kontrolü
- `robots.txt` AI bot engeli tarama testi
- Perplexity Sonar API canlı citation ve Mention Rate % hesaplama doğrulaması
