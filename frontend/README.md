# Akiron SEO Frontend

Next.js 16 App Router client for the Akiron SEO tenant dashboard.

## Runtime model

- Authentication is API-managed through `akiron_access` and `akiron_refresh` HttpOnly cookies.
- The browser stores no token, tenant identifier, or role. All authorization UI derives from `GET /auth/session`.
- Every API request includes credentials. Concurrent `401` responses share one refresh request, then retry each original request once.
- TanStack Query v5 owns server state, stable query keys, caching, and targeted mutation invalidation.
- Website insight cards load only after the user expands a website, avoiding the former per-site startup request burst.
- English and Turkish copy uses the typed catalog in `src/lib/i18n.ts`; light and dark themes use semantic CSS tokens.

## Development

```powershell
npm install
npm run dev
```

The frontend runs at <http://localhost:3000>. `NEXT_PUBLIC_API_URL` defaults to <http://localhost:5248/api/v1>.

## Quality gates

```powershell
npm run lint
npm run test -- --run
npx tsc --noEmit
npm run build
```

Vitest and React Testing Library cover the credentialed API client, single-flight refresh, session guards, role-based admin visibility, translation-key alignment, and accessible authentication fields.
