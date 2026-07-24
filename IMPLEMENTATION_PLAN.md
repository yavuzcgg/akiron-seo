# AkironSeo: Yapay Zeka Destekli SEO, AIO, GEO ve AEO Optimizasyon Platformu (Implementation Plan v4.0)

Bu proje; klasik Arama Motoru Optimizasyonu (**SEO**), Yapay Zeka Optimizasyonu (**AIO**), Üretken Motor Optimizasyonu (**GEO**) ve Cevap Motoru Optimizasyonu (**AEO**) süreçlerini otomatize eden, veritabanı düzeyinde multi-tenant olan ve yapay zeka ile içerik/teknik strateji üreten enterprise düzeyde bir SaaS platformudur.

---

## 🛠️ Mimari & Teknolojik Altyapı (Güncellendi v4.0)

- **Backend**: **.NET 10 LTS** Web API (**Clean Architecture**: Domain, Application, Infrastructure, API katmanları)
- **CQRS & Pipeline**: **MediatR [12.4.0, 13.0.0)** (MIT Lisanslı 12.x serisi) + FluentValidation
- **Frontend**: **Next.js 15/16 (Latest)** App Router + React 19 + TypeScript + Tailwind CSS + TanStack Query v5 + Recharts
- **Veritabanı**: **PostgreSQL / NeonDB** (Entity Framework Core 10, Multi-Tenancy Global Query Filters, Partial Unique Indexes)
- **Yalın MVP Topolojisi**: v1 MVP için Redis bağımlılığı kaldırıldı. Kota ve Analiz Deduplication Postgres'te, Rate Limiting .NET native `RateLimiter` middleware'inde çalışır.
- **Atomik Kota & İade Engine**: Postgres koşullu UPDATE + Job Fail iade (refund) + Job Success mutabakat (reconciliation)
- **Yapay Zeka Adapte Motorları (`IGeoEngineClient`)**:
  - **Perplexity Sonar**: `citations` & `search_results` JSON adapter
  - **OpenAI**: Responses API + `web_search` `url_citation` adapter
  - **Gemini**: Google Search `groundingMetadata` adapter
  - **Anthropic**: Claude web search tool adapter
- **Arka Plan Görevleri**: Hangfire (PostgreSQL Storage) + Scoped `TenantJobFilter` Activator
- **Güvenlik & Auth**: Multi-Tenant RBAC + JWT Access Token + HttpOnly Refresh Cookie (`SameSite=Lax`) + BYOK (AES-256-GCM)

---

## 📐 Clean Architecture Yapısı (.NET 10 Backend)

```
AkironSeo.Backend/
├── src/
│   ├── Core/
│   │   ├── AkironSeo.Domain/         # Multi-Tenant & Global Entity'ler, CrawlJob, Snapshots, PromptTemplates
│   │   └── AkironSeo.Application/    # CQRS (MediatR 12.x), Atomic Quota Behavior, Validation
│   ├── Infrastructure/
│   │   └── AkironSeo.Infrastructure/ # DbContext + Global Filters, AI Adapters, Cost Engine, Scoped Hangfire Filter
│   └── Presentation/
│       └── AkironSeo.API/            # Controllers, Tenant Middleware, HttpOnly Cookie Auth, SuperAdmin Hangfire Auth
└── tests/
    └── AkironSeo.IntegrationTests/   # Testcontainers (Real PostgreSQL Tenant Isolation & Quota Tests)
```

---

## 🚀 Faz Bazlı Uygulama Planı (Roadmap v4.0 - Demo Odaklı & Tamamlanmış)

### ⚪ Faz 0: Multi-Tenancy, Plan, Atomik Kota & İzolasyon Testleri (Temel İskelet)
- [ ] `.NET 10 Web API Clean Architecture` projelerini `.NET 10` ve `MediatR [12.4.0, 13.0.0)` paketleriyle yapılandırma
- [ ] `Tenant`, `User`, `TenantUser`, `RefreshToken`, `Plan`, `Subscription`, `TenantFeature` Entity'lerinin yazılması
- [ ] EF Core 10 `AkironDbContext` üzerinde **Global Query Filter** ve `.HasFilter("\"IsDeleted\" = false")` partial index'leri
- [ ] **Hangfire Scoped Tenant Filter**: Job DI scope'u içinde `ITenantContext` enjekte eden `TenantJobFilter`
- [ ] Atomik Kota Rezervasyon & İade/Mutabakat mekanizması (`UPDATE Subscriptions SET UsedTokens = ...`)
- [ ] **İzolasyon & Sızıntı Testleri**: `Testcontainers` ile gerçek Postgres konteynerinde eşzamanlı job izolasyonu doğrulaması
- [ ] **UI Slice**: Next.js App Router üzerinde `HttpOnly Cookie (SameSite=Lax)` tabanlı Login/Register ve Tenant Dashboard İskeleti

### 🔴 Faz 1: Website Yönetimi, Site Crawler & Snapshot Engine
- [ ] `Website` (VerificationToken ile DNS/Meta Tag doğrulaması) ve `TrackedKeyword` (CronExpression + NextScheduledRun) modülleri
- [ ] BYOK (AES-256-GCM ile şifreli OpenAI / Perplexity API anahtarı saklama)
- [ ] **Site Tarayıcısı & Özet Yapısı**: `CrawlJob` → `SeoAudit` (1-1) → `CrawlResults` zinciri
- [ ] **Site Snapshot & Diff Engine**: `SiteSnapshot` özet satırları ve PostgreSQL `LAG()` pencere fonksiyonuyla score/issue diff hesabı
- [ ] **UI Slice**: Website Ekleme/Doğrulama ekranı ve Site Taraması / Snapshot Karşılaştırma UI bileşenleri

### 🟡 Faz 2: SEO Audits, AI Bot Denetimi & AEO Engine
- [ ] SEO Audit skorlaması (`CrawlResults` üzerinden skorlama, PageSpeed Insights API entegrasyonu: Sadece anasayfa + N kritik sayfa ile sınırlı)
- [ ] **Quick-Win Audit**: `robots.txt` AI Bot Denetleyici (`GPTBot`, `ClaudeBot`, `PerplexityBot`, `Google-Extended` erişim analizi)
- [ ] AEO Generator: JSON-LD SSS/Makale şeması ve otomatik **`llms.txt`** / **`llms-full.txt`** dosyası üretici
- [ ] **UI Slice**: SEO Audit Rapor Sayfası, AI Bot Uyum Paneli ve 1-Tıkla AEO Şema Üretici UI

### 🟢 Faz 3: GEO (Generative Engine Optimization) Structured Pipeline & Opportunity Engine
- [ ] `IGeoEngineClient` Provider Adapter'ları (`PerplexitySonarAdapter`, `OpenAiSearchAdapter`, `GeminiGroundingAdapter`, `AnthropicAdapter`)
- [ ] **Sampling Engine**: Jitter & paralellik sınırlı (2-3 concurrent) 3-5 örneklem çağrısı ile **Mention Rate %** ve Ortalama Sıra (Position) hesabı
- [ ] **Analiz Seviyesi Deduplication**: 24 saat içindeki aynı kelime sorgularında mevcut analiz sonucunu gösterip "Zorla Yenile" seçeneği sunma
- [ ] **CitationStatus & Fırsat Bildirimi**: `CitationStatus = NonExistentPage` durumunda *"AI seni kaynak göstermek istiyor ama sayfa yok — hemen oluştur!"* fırsat bildirimi üretimi
- [ ] `PromptTemplate` DB kütüphanesi ve `AiCache` (AEO ve İçerik üretici için)
- [ ] `CompetitorsJson` rakip istihbaratı üreticisi ve GEO İyileştirme Tavsiye motoru
- [ ] **UI Slice**: GEO Görünürlük Haritası, Mention Rate Trend Grafikleri (Recharts), Fırsat Bildirim Paneli ve Rakip Kıyaslama UI

### 🔵 Faz 4: AI Content Writer, Raporlama & Admin Yönetimi
- [ ] **GEO-Optimized AI Content Writer**: `AiContentPlan` ile alıntı yapmaya meyilli içerik ve makale motoru
- [ ] SuperAdmin Paneli (Tenant yönetimi, B2B Havale/Manuel abonelik onay CRUD, API harcamaları, token maliyet takibi, `/hangfire` auth)
- [ ] RawResponseJson 30-90 gün otomatik budama (Pruning job)
- [ ] **UI Slice**: AI Content Writer Editörü, SuperAdmin Yönetim Paneli ve PDF Rapor Çıktı Motoru

### 🟣 Faz 5 / Backlog (Gelecek Vizyonu)
- [ ] Google AI Overviews için SERP API (DataForSEO / SerpApi) entegrasyonu (`TargetEngine = GoogleAIO`)
- [ ] Google Search Console & Bing Webmaster Connectors
- [ ] White-Label Agency Portal (`agency.com` / `seo.agency.com`)
- [ ] Webhook & Email Bildirim Sistemi (`Notification` entegrasyonu)

---

## 🧪 Verification Plan

### Automated Tests
- `Testcontainers` PostgreSQL entegrasyon testleri (`dotnet test`) - **Faz 0'da eşzamanlı multi-tenant izolasyonu ile çalıştırılır**
- EF Core Global Query Filter izolasyon doğrulaması
- Scoped `TenantJobFilter` scope doğrulama testleri
- Atomik Kota Rezervasyon yarış durumu (Concurrency) testleri

### Manual Verification
- Tenant kaydı, kullanıcı rol doğrulaması ve SameSite=Lax HttpOnly Cookie JWT kontrolü
- `robots.txt` AI bot engeli tarama testi
- Perplexity Sonar API canlı citation ve Mention Rate % hesaplama doğrulaması
