# Manual Test Guide

A sit-down walkthrough of the running application. Written against the state at commit
`957c998`. Each step states what you should see, so a mismatch is a finding rather than a
guess.

Set aside roughly 45 minutes for parts 1–6.

---

## 0. Start the stack

```powershell
cd <repo>
docker compose up -d --wait
docker compose ps
```

All three services must report `healthy`. Then:

| What | Where |
| --- | --- |
| Frontend | <http://localhost:3000> |
| API | <http://localhost:5248> |
| Health probe | <http://localhost:5248/health> → `{"status":"Healthy"}` |
| Database | `docker compose exec postgres psql -U akiron_user -d akironseo_db` |

### Accounts

The compose stack runs as **Production**, which deliberately seeds no SuperAdmin. Your
database volume still holds `admin@akironseo.com` / `Admin123!` from an earlier Development
run, so admin testing works today.

If you ever run `docker compose down -v`, that account is gone. Register a normal user, then
promote it:

```sql
UPDATE "TenantUsers" SET "Role" = 99
WHERE "UserId" = (SELECT "Id" FROM "Users" WHERE "Email" = 'your@email.com');
```

Log out and back in afterwards — the role is baked into the JWT at login.

---

## 1. Security — the fixes that matter most

### 1.1 A normal user cannot reach the admin panel

1. Register a fresh account at <http://localhost:3000/register> (e.g. `test1@example.com`).
2. On the dashboard, look at the header.

**Expect:** no **🛡️ Admin** link. It renders only for SuperAdmin.

3. Now force it — navigate directly to <http://localhost:3000/admin>.

**Expect:** you are bounced back to `/dashboard`. You should never see the tenant table.

4. Confirm the server refuses too, not just the UI. In DevTools → Console:

```js
fetch("http://localhost:5248/api/v1/admin/tenants", {
  headers: { Authorization: `Bearer ${localStorage.getItem("akiron_token")}` }
}).then(r => console.log("status:", r.status));
```

**Expect:** `status: 403`. A 200 here would mean any customer can read and modify every
other customer's account — that was the state before this work.

### 1.2 Logout actually ends the session

1. Click **Logout**.

**Expect:** you land on `/login`, and in DevTools → Application → Local Storage the
`akiron_token`, `akiron_tenant_id` and `akiron_role` keys are **gone**.

2. Press the browser Back button.

**Expect:** you do not get the dashboard; you are sent to `/login`.

### 1.3 An expired or tampered token ends the session

1. Log in. In the Console, corrupt the token:

```js
localStorage.setItem("akiron_token", localStorage.getItem("akiron_token") + "x");
```

2. Trigger any action that calls the API (e.g. **⚡ Run Audit**).

**Expect:** you are redirected to `/login` and storage is cleared — not a red error banner
with a dead session left in place.

### 1.4 The server refuses to fetch internal addresses

Add a website with domain `169.254.169.254` (the cloud metadata endpoint) and click
**⚡ Run Audit**.

**Expect:** a clear `400` error stating the host resolves to a non-public address. Try
`127.0.0.1` and `10.0.0.1` too — all must be refused. A hang or a 200 would mean the API can
be used to reach inside its own network.

### 1.5 Production refuses development secrets

```powershell
docker run --rm -e ASPNETCORE_ENVIRONMENT=Production `
  -e "ConnectionStrings__DefaultConnection=Host=x;Database=y;Username=z;Password=w" `
  akironseo-backend
```

**Expect:** the process aborts with `Startup aborted — invalid secret configuration`,
listing the missing keys. It must never boot with placeholder secrets.

---

## 2. Core flow — website, verification, crawl, audit

1. Log in as your test user. Add a website: name `Example`, domain `example.com`.
2. Click **🔍 Verify** (meta tag method).

**Expect:** verification fails — you do not own `example.com`. This is correct. A success
here would mean ownership checks are meaningless.

3. Click **⚡ Run Audit**.

**Expect:** a score around **55/100** within a few seconds.

4. Open the audit report modal and go through the tabs.

**Expect on the Overview tab:**
- Parsed title, meta description, canonical URL, H1 tags from the real page.
- A **Score Breakdown** whose rows **add up exactly to the score in the header**. Add them
  up by hand once — this is worth verifying, because the bars used to be computed
  separately from the score and could disagree.
- For `example.com` specifically: **OpenGraph Tags 0/10** and **Meta Description 0/15**,
  because that page genuinely has neither. Until recently the UI reported a fixed 5/10 for
  OpenGraph on every site regardless.

**Expect on the Issues tab:** concrete findings with recommendations, not placeholders.

5. Try a richer site — add `github.com` or your own domain and audit it.

**Expect:** a visibly different breakdown, with OpenGraph scoring where the tags exist.

---

## 3. Data honesty — the DEMO DATA badges

This is the part worth looking at closely, since it is what a stranger would judge hardest.

1. On a website row, find the **GSC Analytics** card.

**Expect:** an amber **DEMO DATA** badge next to the heading, and copy stating the figures
are generated locally rather than organic search data. Hover the badge for the tooltip.

2. Look at the **Keyword Rank Tracker**. Add a keyword, then click **⚡ Check Rank**.

**Expect:** a position appears, with a **DEMO DATA** badge on the card. Restart the backend
(`docker compose restart backend`) and check the same keyword again — **the position will
change**, because it is derived from a string hash and .NET randomises hash seeds per
process. That instability is exactly why the badge is there.

3. Look at **Competitor Intelligence**. Analyse any competitor domain.

**Expect:** a **DEMO DATA** badge, and the same fixed keyword set regardless of which
competitor you enter.

**None of these three should ever appear without a badge.** If you find an unbadged metric
that is not a real measurement, that is a bug worth reporting.

---

## 4. GEO — with and without API keys

### 4.1 Without any key configured

Open the **GEO Intelligence** card on a website.

**Expect:**
- Exactly **two** engine rows: Perplexity and Gemini. **No ChatGPT and no Claude** — those
  rows used to be fabricated wholesale and have been removed.
- Both marked **NOT CONFIGURED**, status `— Not measured`, no mention percentage.
- **Share of Voice 0**, not a healthy-looking number. Before this work an unconfigured
  tenant saw a near-perfect score built from invented citations.
- A recommendation telling you to add a key.

### 4.2 With a real key

Google Gemini has a free tier — get a key from <https://aistudio.google.com/apikey>.

1. In the dashboard's BYOK panel choose **Google Gemini**, paste the key, save.
2. Re-run the GEO analysis (use the refresh control to bypass the 24-hour cache).

**Expect:** the Gemini row switches to a real result — badge gone, a genuine mention
percentage, and a response snippet from the model. Perplexity stays **NOT CONFIGURED**.

> The BYOK dropdown also lists OpenAI. A key saved there is stored and encrypted but not
> used yet — no OpenAI adapter is written. That is a gap, not a bug.

### 4.3 Multi-website isolation

1. Add a second website with a different domain.
2. Run GEO analysis on the first, then immediately on the second.

**Expect:** the second site's result shows **its own domain**. If you see the first site's
domain or its citations, the per-website scoping has regressed — this was a real bug until
recently, where the 24-hour cache was shared across a tenant's sites.

### 4.4 AEO generator across two sites

1. Open **AEO Generator** on site A, note the JSON-LD.
2. Close it, open it on site B.

**Expect:** site B's own schemas. Seeing site A's content again means the modal-reset fix
has regressed.

---

## 5. Admin panel

1. Log out, log in as `admin@akironseo.com` / `Admin123!`.

**Expect:** the **🛡️ Admin** link now appears in the header.

2. Open it.

**Expect:** every tenant listed, with token usage and website counts.

3. Adjust a tenant's quota, then toggle a tenant's status.

**Expect:** both succeed and the list refreshes. Toggle it back afterwards so you do not
leave your own test tenant disabled.

---

## 6. Reports and notifications

1. Click **📄 Executive Report** on a website with a completed audit.

**Expect:** a new tab with a branded printable report — SEO score, meta tags, GEO citation
matrix. Ctrl+P should give a clean print layout.

2. **XSS check.** Add a website whose *name* is:

```
<img src=x onerror=alert('xss')>
```

Run an audit, then open its Executive Report.

**Expect:** the name renders as literal text. **No alert dialog.** A popup would mean the
HTML encoding regressed — this document is served from the API origin, so script running
there would be able to act as the API.

---

## 7. Known rough edges — do not file these as new bugs

Things you will notice that are already understood:

| What you will see | Status |
| --- | --- |
| **Light mode is broken** — white text on white cards, especially in the admin panel | Known. Components hardcode `text-white` / `bg-slate-*` with zero `dark:` variants, so the theme toggle is effectively decorative. Part of the design rework below. |
| The **EN/TR toggle** changes almost nothing once you are logged in | Known. Only 12 translation keys exist, covering the landing and auth pages. The dashboard is hardcoded English. |
| Turkish text in a few places (`motosiklet kaskı` placeholders, one form label) | Known. Leftover test data, and a violation of the English-only rule in `AGENTS.md`. |
| Dashboard feels slow with several websites | Known. Each website row mounts ~6 cards that each fetch independently, so N sites means roughly 6N requests on mount. |
| The crawler only ever reports 1 page | Known and by design today — it fetches the homepage only. No sitemap parsing or link following. |
| No password reset, email verification, team management, or billing | Not built. |
| Keyword tracking always says language `tr` | Known. Hardcoded in the API client with no UI control. |
| Scheduled rank checks never fire on their own | Known. Cron expressions are parsed and stored, but no background runner exists — this is the next planned piece of work. |

---

## 8. Notes for the design pass

If you are reworking the UI, these are the structural issues worth fixing while you are in
there, roughly in order of payoff:

1. **Commit to one theme or fix the other.** Right now light mode is unusable. Either
   replace every hardcoded `text-white` / `bg-slate-800` with `dark:` variants and CSS
   variables, or drop the toggle. A half-working toggle reads worse than no toggle.

2. **Extract a shared `<Header>`.** The logo + language + theme + logout bar is duplicated
   in three files (`app/page.tsx`, `app/dashboard/page.tsx`, `app/admin/page.tsx`) and they
   have already drifted apart.

3. **Modals need keyboard and focus handling.** None of them close on `Escape`, trap focus,
   restore focus on close, or dismiss on backdrop click. `role="dialog"` is missing
   throughout. This is the cheapest accessibility win available.

4. **`animate-fadeIn` is used in five places and does not exist** — no `tailwind.config`,
   no keyframes in `globals.css`. Either define it or remove the class.

5. **Dynamic Tailwind classes do not work.** `AeoGeneratorModal` builds
   `` `border-${tab.color}-500` ``, which Tailwind cannot extract statically; it was patched
   over with an inline `style` object. Use a lookup map of complete class names instead.

6. **Emoji are used as icons everywhere.** They render inconsistently across platforms and
   are announced by screen readers. A small SVG icon set would lift the whole product.

7. **Consider collapsing the per-website card stack.** Six always-expanded cards per website
   is a lot of vertical space and a lot of requests. Tabs or accordions per site would fix
   the density and the request storm together.

---

## Reporting what you find

For anything unexpected, capture:

- The URL and what you clicked
- Expected vs actual
- The `correlationId` from the API error response, if there is one
- `docker compose logs backend --tail 50`

Any 5xx status is worth investigating — the API is meant to answer with a `4xx` and a clear
message for anything the caller did wrong.
