# Developer Notes & Setup Guide

## 🛠️ Environment Prerequisites
- **.NET SDK**: .NET 10.0
- **Node.js**: v22 or newer
- **Docker Desktop**: required for the database and for the integration test suite
- **dotnet-ef**: `dotnet tool install --global dotnet-ef` (keep it aligned with the EF Core package version)

## 🔐 First-Time Secret Setup

No secret is committed to the repository, so two files must be created before anything runs.

**Backend local development** — copy the example and keep the copy out of git (it is already gitignored):
```powershell
cd <repo>\backend\src\Presentation\AkironSeo.API
Copy-Item appsettings.Development.example.json appsettings.Development.json
```

**Docker / deployment** — copy `.env.example` to `.env` and replace every value:
```powershell
cd <repo>
Copy-Item .env.example .env
```
Generate the two keys with `openssl rand -base64 48`. The API refuses to start in any
non-Development environment when `Jwt__SecretKey` or `Security__MasterEncryptionKey` is
missing or still set to an example value.

`Auth__CookieSecure` must remain `true` in deployed environments. The compose example sets
it to `false` only because the documented local stack uses loopback HTTP; startup rejects
that exception when a configured frontend origin is not loopback.

> Rotating `MASTER_ENCRYPTION_KEY` makes every previously stored tenant BYOK API key
> undecryptable — they have to be re-entered.

---

## 🚀 Local Running Commands

Run each of the three from its own terminal. Paths below assume the repository lives at `<repo>`.

### 1. Database (Docker PostgreSQL 16)
```powershell
cd <repo>
docker compose up -d --wait
```
`--wait` blocks until every healthcheck passes, so the API never starts against a database that is not accepting connections yet.

Postgres is not published to the host in `docker-compose.yml`; add a `ports` mapping locally if you want to attach a SQL client directly.

### 2. Backend (.NET 10 Web API)
```powershell
cd <repo>\backend
dotnet run --project src/Presentation/AkironSeo.API
```
Listens on `http://localhost:5248`. On startup it applies any pending EF Core migrations and then seeds the baseline data. Both steps are fatal on failure by design: booting with an incomplete schema would fail every request.

### 3. Frontend (Next.js 16 App Router)
```powershell
cd <repo>\frontend
npm install    # first run only
npm run dev
```
Serves `http://localhost:3000` and talks to the API at `NEXT_PUBLIC_API_URL`, defaulting to `http://localhost:5248/api/v1`.

---

## 🗄️ Database

Local connection settings live in `appsettings.Development.json`; deployments supply
`ConnectionStrings__DefaultConnection` as an environment variable from `.env`.

Open a SQL shell:
```powershell
docker compose exec postgres psql -U akiron_user -d akironseo_db
```

### Seeded Accounts
Plans are seeded in every environment. The SuperAdmin account (`admin@akironseo.com` /
`Admin123!`, tenant "Akiron HQ") is seeded **only when the host runs as Development** —
`Program.cs` passes `seedSuperAdmin: app.Environment.IsDevelopment()`. A deployed
environment therefore has no default account, and the first SuperAdmin must be promoted
manually in the database.

### Migrations
The schema is owned by EF Core migrations in `backend/src/Infrastructure/AkironSeo.Infrastructure/Persistence/Migrations`. Never use `EnsureCreated` alongside them.

```powershell
cd <repo>\backend

# Add a migration after changing the model
dotnet ef migrations add <Name> -p src/Infrastructure/AkironSeo.Infrastructure -s src/Presentation/AkironSeo.API -o Persistence/Migrations

# Apply manually (the API also applies pending migrations on startup)
dotnet ef database update -p src/Infrastructure/AkironSeo.Infrastructure -s src/Presentation/AkironSeo.API
```
`AkironDbContextFactory` supplies the design-time context, so the CLI never executes the API's startup path.

### Reset
```powershell
docker compose down -v
docker compose up -d --wait
```
The volume is dropped and the next API start rebuilds the schema and seed data.

---

## 🧪 Tests
```powershell
cd <repo>\backend
dotnet build --warnaserror
dotnet test

cd <repo>\frontend
npm run lint
npm run test -- --run
npx tsc --noEmit
npm run build
```
The backend integration suite starts a throwaway PostgreSQL container via Testcontainers and applies the real migrations, so unique indexes, foreign keys, transactions and row locking behave as they do in production. Docker must be running; the first run pulls `postgres:16-alpine`. Frontend tests use Vitest, React Testing Library, and jsdom.

---

## 📐 Architecture Guidelines
- All code, comments, database column names, DTOs, and commit messages must be 100% in English.
- Use `MediatR` range `[12.4.0, 13.0.0)` for CQRS handlers.
- Use `Cronos` library for cron parsing.
- Always verify `IMultiTenant` (`TenantId`) and `ISoftDelete` (`IsDeleted`) interfaces on domain entities.
- Persist `DateTime` values as UTC. A global value converter enforces this because Npgsql maps `DateTime` to `timestamptz`, which rejects any other `DateTimeKind`.
