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

- The audit uses the real crawler and shows its score breakdown.
- The executive report opens directly in a new tab using the cookie session; no token-reading script or `document.write` flow is involved.
- The six detail areas load only after expansion.
- Simulated GSC, rank, or competitor data is labeled with its data-source badge and explanatory copy.
- The quota card shows only the real subscription plan, monthly token limit, used tokens, remaining tokens, and period dates. It states that enforcement is not yet connected to every job flow.

## 5. Language, theme, and accessibility

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

## 6. Refresh, logout, and revocation

1. Leave the dashboard open until the short access token expires, then perform one action.
2. In Network, inspect the `401`, `/auth/refresh`, and retried request sequence.
3. Log out, then press the browser Back button.

Expected results:

- Concurrent `401` responses share one refresh request.
- Each original request is retried at most once.
- Refresh rotates both cookies and returns `204` with no token in the response body.
- Logout clears both cookies, revokes the refresh family, and the Back button cannot restore an authenticated dashboard.

## 7. Current-role and tenant checks

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

## 8. Automated acceptance gates

```powershell
cd backend
dotnet build --warnaserror
dotnet test

cd ..\frontend
npm run lint
npm run test -- --run
npx tsc --noEmit
npm run build

cd ..
docker compose build
```

All commands must finish successfully. Report unexpected API behavior with the URL, expected and actual results, and the response `correlationId`.
