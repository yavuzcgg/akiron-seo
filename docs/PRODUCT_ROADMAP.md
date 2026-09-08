# Akiron SEO — Product Roadmap & Feature Gap Analysis

> Written against the actual implementation on 2026-09-08 (30 API endpoints, 23 entities,
> 13 infrastructure services, 4 frontend routes, 15 components, CI, Compose). Claims in
> older planning documents were not trusted; the code was read directly. Where this
> document conflicts with `IMPLEMENTATION_PLAN.md`, this document supersedes it.
>
> Companion document: [`PRICING_STRATEGY.md`](PRICING_STRATEGY.md).

## Guiding principle

The goal is not hundreds of features. The goal is **the first paying customer with the
minimum feature set, then controlled growth**. Every item below is ranked against that.

---

## 1. Current product analysis

### 1.1 Problems we solve today

| Problem | Status | Evidence |
| --- | --- | --- |
| "Do AI engines mention our brand?" (GEO measurement) | Real, 4 engines | `GeoEngineService` + `Services/GeoAdapters/*` (Perplexity `sonar`, Gemini `gemini-2.0-flash` grounded, `gpt-4o-mini`, `claude-3-5-haiku`) |
| "Pages AI believes exist on our domain but return 404" (Gold Opportunity) | Real, most distinctive feature | `CitationVerificationService` HEAD/GET → `NonExistentPage` → `Notification` |
| "Can AI crawlers read our site?" | Real | `RobotsTxtAuditorService` (GPTBot, ClaudeBot, PerplexityBot, Google-Extended) |
| AI content generation (Princeton GEO) | Real, 3-provider cascade | `AiContentWriterService` |
| JSON-LD / `llms.txt` generation | Real (templated) | `AeoGeneratorService` |
| Basic on-page technical audit | Real but shallow (5 pages, 14 issue codes) | `WebCrawlerService` |
| Executive report | Real, HTML (browser print to PDF) | `ReportExportService` |
| Multi-tenant agency core, quota, BYOK | Real, strongest part of the codebase | `AkironDbContext` global filters, `QuotaLedgerService`, AES-256-GCM |

### 1.2 Strongest assets (lead with these in sales)

1. **Closed loop from measurement to action.** GEO citation → 404 detection → one-click AI
   content. Semrush and Ahrefs do not have it; pure GEO tools (Profound, Peec, Otterly) stop
   at measurement.
2. **Four-engine GEO matrix with honest data provenance.** `DataSources`
   (`Live / NotConfigured / Unavailable / Simulated`) on every DTO and the DEMO DATA badge.
3. **Agency-native core.** Tenant isolation at the ORM level, idempotent quota ledger with
   concurrency tests, BYOK so each customer pays its own inference.
4. **Security maturity.** HttpOnly cookies, rotating refresh-token families, SSRF guard,
   PBKDF2 at 600k iterations, `SecretsValidator`. Rare at MVP stage.
5. **AEO bundle.** robots.txt presets (`MaxAiVisibility / SearchOnlyAi / BlockAiTraining`),
   `llms.txt`, JSON-LD in one place.

### 1.3 Missing or half-finished (verified in code)

**Simulated and therefore unsellable**

- `SearchConsoleService`: GSC figures are `score * 350 + keywords * 1250`. No Google API package exists.
- `KeywordRankTrackerService`: position is `hash % 15 + 1`; changes on every process restart. No SERP provider.
- `CompetitorService`: overlap is hash-derived; "missing keywords" are four hardcoded Turkish strings; competitor domain is hardcoded.

**Built but wired to nothing (dead wiring)**

- `BackgroundJobQueue` has a consumer and **zero producers**; crawls run synchronously inside the HTTP request (`WebsiteEndpoints.cs`).
- `Plan.LimitsJson` (`max_websites`, `max_keywords`) is never parsed or enforced. One plan is seeded ("Agency Unlimited Plan", $149).
- `PromptTemplate`, `AiCache`, `TenantFeature`, `ApiUsageLog` tables: zero reads and writes. The admin usage-log screen is always empty.
- `CrawlResult.PageSpeedMetricsJson`, `Website.BrandAliasesJson`, `Website.WebhookUrl` (no UI or command sets it), `GenerateOptimizedRobotsTxt` (no endpoint).
- Four of five `NotificationTypeEnum` values are never produced. `PastDue` does **not** block quota (`ReserveQuotaAsync` ignores status).
- Frontend: `apiClient.notifications.list()` is never called (no bell); the cron expression cannot be entered (hardcoded `"0 0 * * *"`); Anthropic is missing from the BYOK provider select.

**GEO correctness defects**

- `GeoAnalysis.TargetEngine` is written as `GeminiGrounding` for every engine (`GeoEngineService.cs`), so stored data is wrong for three of four engines.
- Sentiment is not analysed: `isMentioned ? "Positive" : "NotMentioned"`.
- Share of voice is `mentionRate + 15`; no competitor share is computed and `CompetitorsJson` is never written.
- The OpenAI and Anthropic adapters attach no web-search tool, so they measure training-data recall rather than live citations. Only Gemini is grounded.
- The default GEO prompt and the `GeminiAiService` system prompt are hardcoded in Turkish, violating the English-only rule and locking the product to one market.
- `AiContentWriterService` fallback template emits **fabricated statistics and a fictional expert quote**. `TokensSpent` is always 1250.
- `AeoGeneratorService` writes "Fast nationwide delivery / official warranty" for every tenant and invents `sameAs` social URLs.

**Product surface**

- No onboarding: register → empty dashboard with a one-sentence empty state. Users discover only by trial that GEO does nothing without a BYOK key.
- No settings, pricing, billing, account, or per-site detail page. Everything lives on `/dashboard`.
- No charts (no charting library installed) and no time series: no rank history, score trend, or mention-rate trend.
- i18n: 291 catalogue keys but roughly 15 hardcoded English strings in components; backend errors and notifications are English-only.

**Operations**

- No deploy target (CI builds images with `push: false`), no TLS or reverse proxy, no Sentry or OpenTelemetry, `/health` does not touch the database.
- No email transport, so password reset, email verification, invites, and alert emails are all impossible.
- No payment provider. Roles are not enforced below SuperAdmin (Owner = Admin = Member).
- `GeoAnalysis.RawResponseJson` (12 full LLM responses per run) is never pruned. No tenant or account deletion endpoint (KVKK/GDPR).
- Crawler sends no User-Agent, ignores robots.txt `Disallow`, has no politeness delay, uses the 100-second default timeout, and has no per-tenant concurrency cap.

### 1.4 Blockers for a real customer today

1. **We cannot take money.** No payment, plan selection, or limit enforcement.
2. **A user without API keys sees no AI feature.** Trials are impossible without platform keys and a credit model.
3. **A user who forgets their password is locked out.** No email, no reset.
4. **Three cards show DEMO DATA.** "Simulated rank tracking" cannot be sold in an SEO tool; make it real or remove it.
5. **Crawl is synchronous and capped at 5 pages.** A 50-page site is not an audit; a slow site blocks the request.
6. **No deployed environment.** No HTTPS, backups, monitoring; the first SuperAdmin must be promoted by hand in the database.
7. **Fabricated content risk.** If a customer publishes the fallback article's fake statistics, it is a reputational and legal problem.

### 1.5 Must-complete list for MVP sale (detailed in §5 P0)

Manual founding-customer billing with enforced plan limits · platform AI key + credit model ·
email + password reset · real GSC integration · real rank tracking (DataForSEO) · remove the
competitor card · async crawl with 100–1,000 pages · tracked GEO prompt library with weekly
sampling · onboarding + settings page · GEO correctness fixes · removal of fabricated fallback
content · deploy + monitoring + backups · KVKK deletion + pruning · pricing / ToS pages.

---

## 2. Future features

Format: **Feature** — value · customer type · difficulty (Low / Medium / High) · revenue or retention potential · stage (MVP / V1 / V2 / V3).

### 2.1 Technical SEO

- **Async, queued, plan-tiered crawler (100–25k pages)** — a real audit; today 5 pages. All · Medium (queue exists, producer missing) · high retention · **MVP**
- **Broken links, redirect chains, duplicate title/meta, hreflang, mixed content** — grows issue coverage from 14 to ~40 codes. SMB + agency · Medium · medium · **V1**
- **Core Web Vitals (PageSpeed Insights + CrUX)** — the first metric customers ask for; the column already exists. All · Low–Medium · medium · **V1**
- **Optional JS rendering (Playwright)** — SPA sites. Enterprise · High · low · **V2+ / not now**
- **Structured-data validator for existing JSON-LD** — synergy with AEO. All · Low–Medium · medium · **V1**
- **Site change tracking (snapshot diff via `LAG()`)** — "why did the score drop?". `SiteSnapshot` exists, diff UI does not. Agency · Low · high retention · **V1**

### 2.2 Content SEO

- **Tracked prompt library (N buying-intent prompts per site, EN/TR)** — the heart of the GEO product; today one hardcoded prompt plus ad-hoc runs. All · Medium · **revenue-critical (the pricing axis)** · **MVP**
- **Content brief generator (GSC queries + GEO gaps → brief)** — plan before writing. SMB / agency · Medium · medium · **V1**
- **Content "AI-readiness" score (fact density, quotable statements, Q&A structure)** — turns Princeton GEO principles into a measurement. All · Medium · differentiator · **V1**
- **CMS publish (WordPress REST, Webflow)** — closes the action loop; Gold Opportunity → publish. SMB · Medium · high retention · **V2**
- **Prompt template management UI** — table exists, unused. Agency · Low · low · **V2**

### 2.3 Keyword & rank tracking

- **Real rank tracking (DataForSEO SERP, Google, daily/weekly)** — worker and cron already exist; only the stub changes. All · Medium · high (hygiene factor for an SEO tool) · **MVP**
- **Rank history chart + SERP features (AI Overview present, featured snippet)** — the AI Overview signal combined with GEO is unique. All · Medium · medium · **V1**
- **Keyword discovery from GSC queries** — free data source. SMB · Low · medium · **V1**
- **Local / mobile / multi-location tracking** — Agency · Medium · medium · **V2**
- Own keyword database (volume/difficulty) — **do not build**; pass through DataForSEO Labs.

### 2.4 Competitor intelligence

- **GEO competitor share of voice (parse competitor brands from answers)** — the real competitive metric Semrush lacks; `CompetitorsJson` is ready. All · Medium · high · **V1**
- **SERP overlap (DataForSEO ranked-keyword intersection)** — the real version of the simulated card. SMB / agency · Medium · medium · **V1** (card removed in MVP)
- **Competitor AEO / robots.txt comparison (who is open to AI bots?)** — cheap and interesting. All · Low · low · **V1**
- Backlink index — **never**; not a fight worth having with Ahrefs.

### 2.5 Google Search Console / Analytics

- **GSC OAuth + Search Analytics (clicks, impressions, CTR, position, query/page)** — free API, replaces the most misleading simulated card, feeds keyword discovery and briefs. All · Medium · **high** · **MVP**
- **GSC-driven "AI cannibalisation" view (flat impressions, falling clicks → AI Overview is absorbing them)** — unique insight. All · Medium · differentiator · **V1**
- **GA4** — only to connect conversions. SMB · Medium · low · **V2**
- Bing Webmaster — **V3**, on demand.

### 2.6 AI search / LLM visibility / GEO

- **Web-search tools on the OpenAI and Anthropic adapters** — today they measure memory. All · Low · accuracy-critical · **MVP**
- **Mention-rate time series + weekly automatic sampling via the scheduler** — no GEO product without a trend. All · Medium · retention-critical · **MVP**
- **Google AI Overviews / AI Mode (DataForSEO SERP `ai_overview` block)** — enum `GoogleAIO` exists, no adapter; the most visible AI surface in Turkey. All · Medium · high · **V1**
- **Real sentiment + position (LLM-as-judge classification)** — today fake. All · Low–Medium · medium · **V1**
- **Citation source analysis (which domains AI cites → PR/backlink targets)** — Ahrefs does not do this. Agency · Medium · high · **V1**
- **Brand alias / entity matching using `BrandAliasesJson`** — fewer false negatives. All · Low · accuracy · **MVP**
- **Multi-language / multi-location prompt sets** — Global · Medium · medium · **V2**

### 2.7 Automated SEO actions

- **Gold Opportunity → content → approval → CMS publish** — full automation chain. SMB / agency · High · high retention · **V2**
- **Hosted schema / `llms.txt` serving (edge snippet or proxy)** — AEO without touching code. SMB · Medium · medium · **V2**
- **Fix suggestion → GitHub PR (meta/title changes)** — developer-first sites. Tech SMB · Medium · low volume · **V3**

### 2.8 Site auditing

- **Scheduled audits (weekly) + score-change alert** — cron infrastructure is ready. All · Low · retention · **V1**
- **Per-page detail view (issues, breakdown, history)** — today a single modal. All · Medium · medium · **V1**
- **Issue priority with estimated impact (AI-ranked)** — Semrush "Site Health" feel. All · Medium · medium · **V2**

### 2.9 Reporting

- **Real server-side branded PDF** — what an agency sends to its client. Agency · Medium · high (justifies the Agency tier) · **V1**
- **Scheduled email reports (weekly/monthly)** — retention engine. Agency · Low once email exists · high · **V1**
- **White-label reports (logo, colour, no "Powered by")** — the Agency tier's main selling point. Agency · Low–Medium · high · **V1**
- **Shareable read-only client portal link** — Agency · Medium · medium · **V2**
- Looker Studio connector — **V3**

### 2.10 Alerts & monitoring

- **In-app notification centre (bell, list)** — API exists, UI does not. All · Low · retention · **MVP**
- **Email alerts (Gold Opportunity, score drop, rank drop, 80% quota)** — keeps the product alive outside the dashboard. All · Low–Medium · high · **V1**
- **Uptime + robots.txt / `llms.txt` change monitoring ("AI access lost" alert)** — cheap and distinctive. All · Low · medium · **V1**
- **Signed, retried webhooks (HMAC)** — today unsigned, single attempt. Agency / dev · Low · low · **V1**
- Slack / Teams — **V2**

### 2.11 Integrations

GSC (MVP), DataForSEO (MVP), email provider (MVP), Paddle or iyzico (V1), PageSpeed Insights (V1), WordPress (V2), Zapier/Make (V2), GA4 (V2), Slack (V2), Looker (V3).

### 2.12 Agency features

- **Client workspace model (tenant → client → site)** — today tenant = agency with a flat site list. Agency · Medium (data model) · **tier-defining** · **V1**
- **White-label reports** (see 2.9) — **V1**
- **Custom-domain white-label portal** — High · low volume · **V3 / not now**
- **Per-client BYOK key and cost breakdown** — easy once `ApiUsageLog` is written. Agency · Low–Medium · medium · **V1**
- **Client-facing read-only user** — **V2**

### 2.13 Team / collaboration

- **User invites + role enforcement (Owner / Admin / Member)** — enum exists, policies do not. All · Medium · tier axis (seat count) · **V1** (requires email)
- **Multi-tenant switch (one user in several tenants)** — login currently picks the first membership. Agency · Low–Medium · medium · **V1**
- **Tasks / comments / assignment on issues** — Agency · Medium · low · **V2**
- **Audit log (who did what)** — Enterprise · Medium · medium · **V3**

### 2.14 AI agents / autonomous SEO (the differentiating vision)

- **Weekly "Agent Brief"**: combines crawl + GSC + GEO into 3–5 prioritised actions, each executable in one click (generate content, propose meta, fix robots). All · Medium · **primary differentiator** · **V1**
- **Approval-gated autonomous loop**: Observe → Diagnose → Propose → (human approval) → Execute (CMS/GitHub) → Verify (re-crawl, re-sample). SMB / agency · High · high · **V2–V3**
- **Specialist sub-agents** (Technical, Content, GEO, Local) under one orchestrator, each consuming credits. **V3**
- **Self-healing**: automatic proposals or fixes for broken links, missing alt text, duplicate meta. **V3**
- **Automatic FAQ + schema from GSC queries, served by the platform.** **V2**
- **Guardrails**: budget caps, change approval, rollback, "changed by AI" labels. Required alongside V2.

**What keeps Akiron from being a Semrush/Ahrefs clone:** (1) AI visibility + classic SEO + action in one product, (2) the Gold Opportunity closed loop, (3) BYOK with transparent AI cost that protects agency margin, (4) data honesty via provenance, (5) Turkey-first but global-ready, (6) an assistant feel from V1 through the Agent Brief.

---

## 3. Improving existing features (grounded in the implementation)

### Crawler / audit

- **Technical**: hand `POST /websites/{id}/crawl` to `BackgroundJobQueue` (write the producer), use `CrawlJob.Pending/Running` with a polling endpoint; turn the `DefaultMaxCrawlPages` constant into a plan entitlement; use `AddStandardResilienceHandler` (package already referenced, unused); send `AkironSeoBot/1.0 (+url)` as User-Agent; obey robots.txt `Disallow`; 500 ms politeness per host; 1–2 concurrent crawls per tenant.
- **Data quality**: replace the plain page average with a weighted score (homepage ×3); normalise the 110-point breakdown instead of clamping to 100; distinguish `FETCH_ERROR` from `HTTP_ERROR` in the UI.
- **UX**: crawl progress bar; the "View Report" button after a crawl does nothing (`fetchLatestAudit` is called with a null report in `dashboard/page.tsx`); page-list view.
- **Performance**: fetch pages with 3–5 concurrency; persist derived fields, never raw HTML.

### GEO engine

- **Accuracy**: fix the `TargetEngine` bug; add OpenAI `web_search` (Responses API) and Anthropic `web_search`; match against `BrandAliasesJson`; either run a cheap judge call for sentiment or drop the field; tie share of voice to competitor parsing (until V1 call it "mention rate", not SoV).
- **Automation**: move prompts to a `TrackedPrompt` entity separate from keywords; add weekly GEO sampling to `ScheduledKeywordWorker`; store results as a time series keyed by `RunGroupId`.
- **AI / cost**: sample count 3 → per plan 1/3/5; write `RawResponseJson` once per run rather than per row; prune after 30 days; actually use `AiCache` for same prompt + engine within 24 h.
- **Data**: English default prompt with a TR/EN prompt set chosen by site language; replace the hardcoded `Position = 1` with the order in the answer.

### AI content writer

Remove fabricated statistics and quotes from the fallback template (use explicit "[ADD DATA]" placeholders); read `TokensSpent` from the provider `usage` field; offer fast vs premium model; score generated content with the AI-readiness metric; add a brief step (title/outline approval before full text).

### AEO / robots

Remove fixed claims ("nationwide delivery"); derive the site description from crawled meta/og tags; stop inventing `sameAs`; expose `GenerateOptimizedRobotsTxt` through an endpoint and UI; validate existing page JSON-LD and show the diff.

### Quota / plans

Parse `LimitsJson` into a typed `PlanLimits` and an `IEntitlementService` (sites, keywords, prompts, pages, users, credits); make `ReserveQuotaAsync` reject `PastDue` / `Cancelled`; write `ApiUsageLog` on every AI call (provider, model, tokens, USD); monthly reset job; 80% / 100% quota notifications (`QuotaAlert` enum already exists).

### Auth / tenant

Role policies (`OwnerOnly` for BYOK and billing, `AdminOrOwner` for site deletion); membership selection at login; email verification; password reset; session listing.

### Frontend / UX

`/settings` (API keys including Anthropic, delete/rotate, test-key); `/sites/[id]` detail page; `/billing`; notification bell; charts (score, rank, mention trend); onboarding checklist (add site → verify → audit → connect GSC → add 5 prompts); make the `DataSourceBadge` explanation visible on touch; move hardcoded English strings into the catalogue; shared `useWebsiteInsight*` hooks to replace 10+ duplicated `useQuery` blocks; convert the dashboard's `useState` + try/catch handlers to mutations; give `GoldOpportunityPanel` real `keyword` and `path` DTO fields instead of regex-parsing prose.

### Operations

Serilog file + Seq/OTLP sink; push the correlation ID into `LogContext`; database ping in `/health` with a `/ready` split; Sentry; `ForwardedHeaders`; per-tenant rate limits on business endpoints; run migrations as a separate job for multi-replica deploys; leader lock (Postgres advisory lock) in the worker.

---

## 4. Positioning

**"Why would a customer pay for Akiron next to, or instead of, Semrush, Ahrefs, or SE Ranking?"**
Semrush and Ahrefs measure where you rank on Google; AI visibility is missing or a $99+ add-on. GEO tools (Profound, Peec, Otterly) only measure; they know nothing about your site's technical state and do not produce content. Akiron combines both in one loop and **turns every measured gap into content**.

- **UVP**: *Measure your visibility in Google and in AI answers from one place, and close every missed page with AI the same day.*
- **Core message (TR)**: "ChatGPT, Gemini ve Perplexity müşterine seni mi öneriyor, rakibini mi? Akiron ölçer, boşluğu bulur, içeriği yazar."
- **Core message (EN)**: "See where AI answers and Google send your customers, and fix the gaps the same day."
- **ICP, first 12 months**: (1) digital/SEO agencies with 5–50 clients, Turkey first then EU (Agency tier); (2) in-house marketing teams at 20–200 person e-commerce or B2B companies (Pro tier). Do not pursue enterprise in year one.
- **Differentiation**: (a) do not fight Semrush, position **beside** it ("the surface Semrush cannot see"); (b) make Gold Opportunity the signature feature (find a 404 in a 3-minute demo); (c) BYOK + transparent cost = agency margin; (d) data honesty (DEMO badge, provenance) earns trust; (e) the Agent Brief makes it an assistant rather than a tool from V1; (f) local advantage through Turkish UI, Turkish payment, and Turkish AI Overview data.

---

## 5. Roadmap

Difficulty: **L** (1–3 days), **M** (1–2 weeks), **H** (3+ weeks). Single-developer assumption.

### P0 — Required before selling (target: 6–8 weeks)

1. **Founding-customer billing (manual)** — payment-provider integration is deliberately **out of P0**. The first 5–10 customers pay by bank transfer against a manual invoice. Build: admin plan-assignment endpoint, `PlanLimits` + `IEntitlementService`, `PastDue` quota rejection, three seeded plans, a billing-status view for the tenant. Automated checkout (Paddle or iyzico) moves to P1, after a sole-proprietorship exists and at least 5 customers pay. — **M**
2. **Platform AI key + credit model** — platform key from configuration, `QuotaCostConstants` → credits, real `TokensSpent`, `ApiUsageLog` writes, 80% / 100% notifications. — **M**
3. **Email (Resend/Postmark) + password reset + email verification** — **M**
4. **GSC OAuth + Search Analytics** — replace the simulated service, return `Live`. — **M**
5. **Real rank tracking (DataForSEO)** — replace the stub, reuse the worker, daily. — **M**
6. **Remove the competitor card** until the real version ships in V1. — **L**
7. **Tracked prompt library + weekly automatic GEO sampling + mention trend (simple line chart)** — **M**
8. **Async crawl** (queue producer, job status, polling) + plan-based page limit (100–1,000) + User-Agent / robots / politeness. — **M**
9. **GEO correctness bundle** — `TargetEngine` bug, OpenAI/Anthropic web search, alias matching, remove fake sentiment, rename SoV to mention rate, English default prompt. — **M**
10. **Fabricated-content cleanup** — fallback template, AEO fixed claims, `sameAs`. — **L**
11. **Onboarding + `/settings`** — checklist, empty states, Anthropic BYOK, key delete/test. — **M**
12. **Deploy** — VPS + Caddy TLS + managed Postgres + daily backups + Sentry + database-aware health check + a CLI command to create the first SuperAdmin. — **M**
13. **KVKK/GDPR minimum** — tenant/account deletion endpoint, 30-day `RawResponseJson` pruning, ToS + privacy + pricing pages. — **L–M**
14. **i18n cleanup** of hardcoded English strings. — **L**

### P1 — After the first customers (target: 2–3 months)

- **Automated billing**: Paddle (global) or iyzico (Turkey) checkout + webhook → `Subscription`, `/billing` page, extra credit packs. Prerequisite: registered business. — **M–H**
- User invites + role policies + multi-tenant switch — **M**
- Notification centre (bell) + email alerts (Gold Opportunity, score/rank drop) — **M**
- Scheduled audits + snapshot diff view — **L–M**
- Server-side PDF + scheduled email reports + white-label (logo/colour) — **M**
- Real competitors: SERP overlap (DataForSEO) + GEO competitor mention parsing (`CompetitorsJson`) — **M**
- Google AI Overview adapter (DataForSEO `ai_overview`) — **M**
- Core Web Vitals (PageSpeed Insights) — **L–M**
- Additional issue checks (broken links, redirects, duplicates, hreflang, structured-data validation) — **M**
- **Agent Brief v1** (weekly 3–5 actions, one-click execution) — **M**
- Client workspace model for agencies — **M**
- Competitor robots/AEO comparison, robots.txt preset endpoint + UI — **L**
- Observability (OTLP/Seq, correlation, leader lock, per-tenant rate limits) — **M**
- Test expansion (crawler scoring, GEO maths, Playwright smoke e2e) — **M**

### P2 — Growth

- CMS publish (WordPress, then Webflow) and full Gold Opportunity automation — **H**
- Hosted `llms.txt` / schema snippet service — **M**
- Content brief generator + AI-readiness score — **M**
- Keyword discovery (GSC) + AI cannibalisation view — **M**
- Client portal (read-only link), Slack/Teams, Zapier — **M**
- Public read API + API-key auth + published OpenAPI — **M**
- Multi-language / multi-location prompt sets, local rank tracking — **M**
- GA4 — **M**

### P3 — Advanced / enterprise

- SSO (SAML/OIDC), audit log, SLA, custom limits, dedicated instance — **H**
- Custom-domain white-label portal — **H**
- JS-rendering crawler (Playwright pool) — **H**
- Task / comment / assignment workflow — **M**
- Bing Webmaster, Looker Studio — **M**

### P4 — Long-term AI agent vision

- Approval-gated autonomous loop (Observe → Diagnose → Propose → Approve → Execute → Verify) — **H**
- Specialist sub-agents + orchestrator with credit-based agent budgets — **H**
- Self-healing technical SEO (automatic meta/alt/link fix PRs) — **H**
- Automatic FAQ/schema from GSC queries, served by the platform — **M**
- Guardrails, rollback, "changed by AI" audit trail — **M** (prerequisite for the rest of P4)

### Explicitly NOT now (unnecessary complexity)

- Migrating to Hangfire (the existing `BackgroundService` plus an advisory lock is enough)
- Microservices / Kubernetes, multi-region, serverless database tuning
- Backlink index, own keyword database (DataForSEO pass-through is enough)
- JS rendering, custom-domain white-label, SSO, public API before there is demand
- Slack / Teams / Zapier before email exists
- Mobile app, browser extension
- Own model fine-tuning; UI languages beyond EN/TR
- Two payment providers at once; enterprise sales
- Prompt-template versioning UI
- Inventing features to "use" the dead `PromptTemplate` / `AiCache` / `TenantFeature` tables: either bind them to a real P0 need (`AiCache` → GEO dedupe) or drop them in a migration

### Superseded items from `IMPLEMENTATION_PLAN.md`

- Hangfire with `TenantJobFilter` — superseded by the custom `ScheduledKeywordWorker` + `BackgroundJobQueue`; not planned.
- `TargetEngine = GoogleAIO` via a dedicated engine adapter — superseded by reading the `ai_overview` block from the DataForSEO SERP response (P1).
- White-label agency portal on a custom domain — moved to P3.
