# Akiron SEO — SaaS Pricing Strategy

> Companion to [`PRODUCT_ROADMAP.md`](PRODUCT_ROADMAP.md). Prices and unit costs are
> planning estimates as of September 2026 and must be re-checked against provider price
> lists before publication. Nothing here is implemented yet; see roadmap P0 item 1 and
> P1 "Automated billing".

## 1. Cost base

| Item | Unit cost (est.) | Note |
| --- | --- | --- |
| GEO prompt, 1 engine, 3 samples (Gemini grounded) | ~$0.10 | ~$35 per 1,000 grounded queries — **the most expensive item** |
| GEO prompt, 1 engine, 3 samples (Perplexity `sonar`) | ~$0.02 | |
| GEO prompt, 1 engine, 3 samples (OpenAI / Anthropic + web search) | ~$0.03–0.04 | search tool ~$10 per 1,000 calls |
| **Full 4-engine GEO run for one prompt** | **~$0.18** | 12 provider calls |
| AI article (Gemini Flash) / (Claude Sonnet class) | ~$0.01 / ~$0.08 | |
| SERP fetch (DataForSEO standard queue) | ~$0.001 | rank tracking is cheap |
| Crawl (own infrastructure) | ~$0.0005 per page | CPU + bandwidth; JS rendering ×20 |
| Google Search Console / PageSpeed Insights | $0 | quota-limited |
| Fixed infrastructure (VPS + managed Postgres + email + Sentry) | ~$150–300 / month | up to the first ~100 customers |

Worked example: a customer sampling 50 prompts across 4 engines **weekly** costs
≈ 50 × 4.3 × $0.18 ≈ **$39 / month** in AI spend. **Daily** sampling would be ≈ $270 / month.
Conclusion: GEO sampling frequency and prompt count must be the primary pricing axis, and
unlimited GEO is impossible.

## 2. Credit model (runs on the existing `QuotaLedgerService`)

Replace the abstract "token" with an **AI Credit**. Target: 1 credit ≈ $0.01 of cost
(≈ 70% gross margin). The ledger mechanics (`Reserve / Commit / Refund`, idempotent `JobId`)
carry this unchanged; only `QuotaCostConstants` and the plan limits change.

| Operation | Credits |
| --- | --- |
| GEO prompt × 1 engine (3 samples) | 3 (uniform across engines for simplicity) |
| GEO prompt × 4 engines | 12 |
| AI article | 10 (fast model) / 25 (premium model) |
| AI meta / title suggestion | 1 |
| Agent Brief (V1) | 5 |
| Crawl | not credits — **page quota** |
| Rank check | not credits — **keyword quota** |

**BYOK rule**: a tenant that supplies its own provider key is not charged AI credits (the
cost is theirs), but prompt / keyword / site limits still apply (infrastructure and fair use).
BYOK is available from Starter upwards and is the strongest argument for agencies.

## 3. Global tiers (USD)

| | **Free** | **Starter** | **Pro** | **Agency** | **Enterprise** |
| --- | --- | --- | --- | --- | --- |
| Monthly / yearly (monthly equivalent) | $0 | $39 / $390 ($32.50) | $99 / $990 ($82.50) | $249 / $2,490 ($207.50) | from $699, custom |
| Websites / projects | 1 | 3 | 10 | 40 | custom |
| Users | 1 | 1 | 3 | 10 | unlimited + SSO |
| Tracked keywords (daily) | 10 (weekly) | 100 | 500 | 2,000 | custom |
| Tracked GEO prompts | 5 | 25 | 100 | 400 | custom |
| GEO sampling frequency | monthly | weekly | weekly (+10 prompts daily) | weekly (+50 prompts daily) | daily |
| Crawl pages / month | 100 | 1,000 | 10,000 | 50,000 | custom + JS rendering |
| AI credits / month | 50 | 300 | 1,500 | 6,000 | custom |
| Competitors per site | 0 | 2 | 5 | 10 | custom |
| Search Console | yes | yes | yes | yes | yes |
| BYOK | no | yes | yes | yes | yes + key vault |
| Automation (scheduled audits, alerts, Agent Brief) | no | email alerts | + Agent Brief, webhooks | + CMS publish (V2) | + autonomous loop |
| Reporting | on-screen | HTML | PDF + scheduled email | white-label PDF + client portal | custom |
| Agency features | no | no | no | client workspaces, white-label, cost breakdown | + custom domain |
| API access | no | no | read (V2) | read/write (V2) | full + SLA |
| Support | community | email, 48 h | email, 24 h | priority + onboarding call | dedicated + SLA |
| Extra credit pack | – | $10 / 500 | $10 / 500 | $8 / 500 | contract |

Rationale: this sits between Otterly ($29–$189), Peec (€89–€199), the Semrush AI Toolkit
add-on ($99) and Semrush Pro ($139). Starter at $39 delivers the message "half of Semrush,
with AI visibility included". Free gives the "you are invisible in AI" shock for free via
GSC + 5 prompts and pulls users into Starter. Two months free on annual is standard.

Margin check (Pro at full use): 100 prompts × 4 engines × weekly ≈ $77 AI + $2 rank + $5
crawl ≈ $84 cost against $99 revenue, which is too thin. Therefore Pro's **1,500-credit cap
(= 125 full GEO runs / month)** is the binding limit; "100 prompts weekly on 4 engines" is
only reachable with BYOK or extra credits. The tier table must carry both the prompt limit
and the credit limit, and the UI must show a credit estimate before a run.

## 4. Turkish market (TRY)

SaaS purchasing power in Turkey is roughly 40–50% below the USD list; even Semrush is
perceived as expensive locally. Recommendation: USD list × ~0.55, pegged to TRY but as a
**fixed price reviewed quarterly** (moving with the daily rate drives customers away).
Shown excluding VAT.

| | Starter | Pro | Agency |
| --- | --- | --- | --- |
| Monthly (TRY, excl. VAT) | ~₺899 | ~₺2,290 | ~₺5,490 |
| Yearly (10 months) | ~₺8,990 | ~₺22,900 | ~₺54,900 |

Notes:

- Turkish cards, instalments, and mandatory e-invoicing point to iyzico or PayTR. Global
  sales point to a Merchant of Record (Paddle or Lemon Squeezy): Stripe does not onboard
  Turkish companies, and an MoR handles VAT / sales tax.
- **Legal prerequisite**: Paddle and iyzico both require a tax-registered business; a sole
  proprietorship is sufficient, an individual account is not. Routing payments through a
  friend's company is not recommended (revenue, invoices, IP, and tax exposure land on the
  other party; the dependency lands on us).
- Sequence: (1) first customers by bank transfer against manual invoices, using the existing
  admin quota controls; (2) register a sole proprietorship (check the young-entrepreneur
  income-tax exemption); (3) one provider only: iyzico if Turkey-heavy, Paddle if global-heavy.
- Known Paddle trade-offs: weekly payouts with a holding period, rare but serious account
  closures with held funds, slow support on the starter plan; against that, very low setup
  friction and full tax compliance.
- Do not put two payment providers in the same phase.
- The agency segment is strong in Turkey (many 5–30 client agencies), so the Agency tier may
  be the main revenue line locally; Starter could allow 2 sites in Turkey to reach freelancers.

## 5. Unlimited plans and the hybrid model

- **Unlimited GEO = loss.** An agency sampling 100 prompts daily across 40 sites would cost
  ≈ $21k / month. Credits and prompt caps are mandatory.
- **Unlimited crawl** is medium risk (bandwidth and time): pages-per-month quota plus 1–2
  concurrent crawls per tenant.
- **Unlimited keywords** is low risk, but still do not say "unlimited"; the limit creates
  tier differentiation.
- Hybrid model: **subscription (limits) + credits (AI) + extra credit packs + BYOK credit
  exemption**. Credits, pages, and keywords are hard caps; they cannot be exceeded without
  buying more; an email goes out at 80%.

## 6. Implementation mapping

| Pricing concept | Existing code | Change |
| --- | --- | --- |
| Credits | `Subscription.MonthlyLimitTokens`, `QuotaLedgerService` | rename semantics, new `QuotaCostConstants` |
| Plan limits | `Plan.LimitsJson` (unused) | typed `PlanLimits` + `IEntitlementService` |
| Billing state | `SubscriptionStatusEnum` (`PastDue` never enforced) | reject reservations when not `Active` |
| Usage / cost reporting | `ApiUsageLog` (never written) | write on every provider call |
| BYOK exemption | `EncryptedTenantApiKey` | skip credit reservation when a tenant key is used |
| Checkout / webhooks | none | P1: Paddle or iyzico adapter behind an `IBillingProvider` |
