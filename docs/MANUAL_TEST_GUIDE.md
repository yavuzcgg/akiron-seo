# Manual Test Guide

This walkthrough reflects the current cookie-session and lazy-dashboard architecture. It takes about 15 minutes.

## 1. Start the stack

```powershell
docker compose up -d --build --wait
docker compose ps
Invoke-RestMethod http://localhost:5248/health
```

Expected results:

- PostgreSQL and backend report healthy; frontend is running and returns HTTP 200.
- The health response contains `status: Healthy`.
- The frontend is available at <http://localhost:3000>.

## 2. Register and inspect the session

1. Open <http://localhost:3000/register>.
2. Register with a unique email and a password containing at least 12 characters, a letter, a number, and a special character.
3. Confirm that the dashboard opens.
4. Open DevTools, then Application, Cookies, and `http://localhost:3000` or the API origin shown by the browser.

Expected results:

- `akiron_access` and `akiron_refresh` exist and are marked `HttpOnly` and `SameSite=Lax`.
- Both cookies are host-only: no `Domain` attribute is present.
- Local Storage contains only optional UI preferences such as theme or language. It contains no token, tenant identifier, or role.
- A password such as `weak` produces an inline validation error and an API `400 application/problem+json` response with a `correlationId`.

`Secure` is disabled only for the local loopback HTTP compose setup. It is required by default outside Development.

## 3. Verify dashboard request behavior

1. Open DevTools Network and reload `/dashboard`.
2. Filter requests by `/api/v1`.

Expected results before expanding a website:

- One session request, one website-list request, and one quota request are sufficient.
- Detail requests for keywords, GEO, competitors, GSC, content, or opportunities do not run yet.
- Loading, empty, and failed states render distinctly; API failures are not silently hidden.

## 4. Add and inspect a website

1. Add `Example` with domain `example.com`.
2. Run the audit and open the audit report.
3. Open the executive report.
4. Expand **Show insights** for the website.

Expected results:

- The audit uses the AngleSharp HTML5 DOM crawler, auto-discovers `/sitemap.xml`, and crawls discovered internal links (up to 5 pages).
- The executive report opens directly in a new tab using the cookie session; no token-reading script or `document.write` flow is involved.
- The six detail areas load only after expansion.
- Simulated GSC, rank, or competitor data is labeled with its data-source badge and explanatory copy.
- The quota card shows the real subscription plan, monthly token limit, used tokens, remaining tokens, and period dates. It displays the **Active** green checkmark indicating live quota enforcement (`EnforcementEnabled: true`).
- Running a crawl debits exactly 5 tokens from the tenant ledger; running GEO prompt analysis debits 10 tokens; generating AI content debits 25 tokens.

## 5. Test 4-Engine GEO Intelligence & BYOK Keys

1. Open website insights and expand **GEO Search Citations**.
2. Click **Run Multi-Engine GEO Analysis**.
3. Inspect citations returned for all 4 major engines: **Perplexity**, **Gemini**, **ChatGPT**, and **Claude**.
4. In Settings -> API Keys, configure a valid BYOK key (e.g. OpenAI or Gemini).
5. Re-run GEO Analysis to verify that the configured provider status switches from `NotConfigured` to `Live` with verified snippet and citation URLs.

## 6. Test Princeton GEO AI Content Writer

1. In website insights, open **AI Content Writer**.
2. Enter a target keyword (e.g. `NextGen CRM Automation`) and optional path.
3. Click **Generate GEO-Optimized Content**.
4. Inspect the generated Markdown content:
   - **Quick Answer Block**: First 40-50 words contain concise direct summary.
   - **Benchmark Statistics**: Includes formatted comparison table.
   - **Authoritative Quotations**: Embeds expert citation commentary.
   - **Structured FAQ**: Schema-ready 3 questions and answers.
   - **Schema.org Article**: Embedded JSON-LD script block.
5. Confirm that 25 tokens were debited from the quota card.

## 7. Test AI Robots.txt & LLMs.txt Generation

1. Open **AEO & Schemas** modal for any crawled website.
2. Verify that **llms.txt** and **llms-full.txt** are generated, containing the site description, documentation links, and crawled page inventory.
3. In API or Swagger, trigger `POST /api/v1/websites/{id}/robots-txt/generate` with preset `MaxAiVisibility`, `SearchOnlyAi`, or `BlockAiTraining`.
4. Inspect generated `robots.txt` output verifying `GPTBot`, `ClaudeBot`, `PerplexityBot`, `Google-Extended`, and `llms.txt` directives.

## 8. Test Background Scheduled Keyword Worker

1. In website insights, add a tracked keyword with Cron expression `0 * * * *` (Hourly).
2. The `ScheduledKeywordWorker` runs every 30 seconds, automatically evaluates due keywords (`NextScheduledRun <= UtcNow`), sets scoped `ITenantContext`, updates rank positions, and advances `NextScheduledRun` using `Cronos`.
3. In PostgreSQL, manually set `NextScheduledRun = NOW() - INTERVAL '5 minutes'`:
   ```sql
   UPDATE "TrackedKeywords" SET "NextScheduledRun" = NOW() - INTERVAL '5 minutes' WHERE "IsActive" = true;
   ```
4. Within 30 seconds, verify that `LastCheckedAt` is updated and `NextScheduledRun` is advanced to the next hour.

## 9. Language, theme, and accessibility

1. Switch between EN and TR.
2. Switch between light and dark themes.
3. Repeat at a narrow mobile width.
4. Navigate forms and dialogs with only the keyboard.

Expected results:

- Core navigation, authentication, dashboard, and quota copy changes language.
- Text and controls remain readable in both themes, including the admin table.
- Controls do not overflow the viewport.
- Every form control has an associated label and visible keyboard focus.
- Dialog focus stays inside the dialog, Escape closes it, and focus returns to the launch control.
- Reduced-motion preferences suppress non-essential transitions.

## 10. Refresh, logout, and revocation

1. Leave the dashboard open until the short access token expires, then perform one action.
2. In Network, inspect the `401`, `/auth/refresh`, and retried request sequence.
3. Log out, then press the browser Back button.

Expected results:

- Concurrent `401` responses share one refresh request.
- Each original request is retried at most once.
- Refresh rotates both cookies and returns `204` with no token in the response body.
- Logout clears both cookies, revokes the refresh family, and the Back button cannot restore an authenticated dashboard.

## 11. Current-role and tenant checks

For an admin smoke test, promote a test membership in PostgreSQL and sign in again:

```sql
UPDATE "TenantUsers" SET "Role" = 99
WHERE "UserId" = (SELECT "Id" FROM "Users" WHERE "Email" = 'your@email.com');
```

Expected results:

- The admin link is visible only when `/auth/session` reports `SuperAdmin`.
- Changing the stored role immediately invalidates an access token with the old role.
- Disabling a tenant immediately invalidates its current access tokens.
- A SuperAdmin cannot disable the tenant that owns the current session.

## 12. Automated acceptance gates

Run the entire automated verification suite:

```powershell
# 1. Backend warning-as-error compilation & 32 PostgreSQL integration tests
cd backend
dotnet build --warnaserror
dotnet test

# 2. Frontend lint, Vitest suite, TypeScript check & Next.js 16 build
cd ..\frontend
npm run lint
npm run test -- --run
npx tsc --noEmit
npm run build

# 3. Docker Compose multi-container production build
cd ..
docker compose build
```

All 32 backend PostgreSQL tests and 8 frontend Vitest tests must finish with zero failures.
