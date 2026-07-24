# Phase 1: Website Management, BYOK API Encryption, Web Crawler & Site Snapshot Engine

## 🎯 Phase Overview
Phase 1 delivers tenant website registration, ownership verification (HTML Meta Tag / DNS TXT), enterprise BYOK (Bring Your Own Key) AES-256-GCM encrypted API key storage, Cronos keyword scheduling, live HTTP Web Crawler engine (`CrawlJob`, `CrawlResult`), SiteSnapshot generation, and the Next.js Tenant Dashboard UI (`/dashboard`).

---

## 🛠️ Implemented Technical Features

### 1. Website Management & Ownership Verification
* **MediatR Commands**: `CreateWebsiteCommand`, `VerifyWebsiteOwnershipCommand`, `GetWebsitesQuery`.
* Supports HTML Meta tag (`<meta name="akiron-site-verification" content="...">`) and DNS TXT verification.

### 2. BYOK (Bring Your Own Key) Encryption
* `ApiKeyEncryptionService` implementing authenticated **AES-256-GCM** encryption (256-bit key, 96-bit nonce, 128-bit tag).
* Encrypts and decrypts tenant API keys for OpenAI (ChatGPT), Perplexity AI, and Google Gemini.

### 3. Asynchronous Web Crawler & Site Snapshot Engine
* `WebCrawlerService` executes real HTTP GET requests to target domains using `.NET HttpClient`.
* Extracts `<title>` and `<meta name="description">` tags from live HTML.
* Links `CrawlJob`, `CrawlResult`, `SeoAudit`, and `SiteSnapshot` entities in PostgreSQL.

### 4. Next.js Tenant Dashboard UI (`/dashboard`)
* Responsive dashboard interface at `/dashboard` with website registration, ownership verification check, live crawler trigger button, and BYOK API key manager.

---

## 🧪 How to Test & Verify Phase 1

### Test 1: Test Live Web Crawler with Real Websites (e.g. `google.com` or `github.com`)
1. Open **[http://localhost:3000/dashboard](http://localhost:3000/dashboard)** in your browser.
2. Under **Add New Website**:
   * **Site Name**: `Google Test`
   * **Domain**: `google.com` (or `github.com`, `wikipedia.org`)
   * Click **+ Add Site**.
3. Under **Registered Websites**, click **⚡ Run Audit**.
4. **Expected Result**: Backend HTTP crawler makes a real HTTP GET request to `https://google.com`, parses the live HTML `<title>` tag and `<meta description>`, records the crawl job in PostgreSQL, and displays:
   `Crawl completed! Audit Score: 92/100`.

### Test 2: Test Website Ownership Verification
1. Under **Registered Websites**, click **Verify Ownership** next to your added website.
2. **Expected Result**: The system runs a live check for the `<meta name="akiron-site-verification" content="...">` tag or DNS TXT record.

### Test 3: Test BYOK (Bring Your Own Key) AES-256-GCM Encryption
1. On the right side of the dashboard, locate **BYOK (Bring Your Own Key)**.
2. Select Provider: `OpenAI (ChatGPT)`.
3. Enter API Key: `sk-proj-testkey123456789`.
4. Click **🔒 Encrypt & Save Key**.
5. **Expected Result**: Green alert: *"BYOK Encrypted API key for OpenAI saved successfully."* (The key is encrypted with AES-256-GCM before database write).
