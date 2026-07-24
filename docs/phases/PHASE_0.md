# Phase 0: Core Platform Foundation & Multi-Tenancy Engine

## 🎯 Phase Overview
Phase 0 establishes the foundational Clean Architecture (.NET 10 LTS), PostgreSQL EF Core multi-tenant database context with automatic global query filters, atomic quota reservation ledger engine, integration test suite, and Next.js 16 App Router UI shell.

---

## 🛠️ Implemented Technical Features

### 1. Backend Clean Architecture (.NET 10 LTS)
* **Domain Layer (`AkironSeo.Domain`)**: 22 domain entities and 13 enums defined 100% in English with `IMultiTenant` and `ISoftDelete` interfaces.
* **Application Layer (`AkironSeo.Application`)**: MediatR CQRS infrastructure, `ITenantContext`, `IAkironDbContext`, and `IQuotaLedgerService`.
* **Infrastructure Layer (`AkironSeo.Infrastructure`)**: EF Core 10 `AkironDbContext` with automatic Global Query Filters (`TenantId` and `IsDeleted`), partial unique index (`HasFilter("\"IsDeleted\" = false")`), and `QuotaLedgerService`.
* **Cross-Cutting Concerns**: Serilog structured logging and `GlobalExceptionHandlerMiddleware` returning RFC 7807 `ProblemDetails` with unique `CorrelationId`.

### 2. Next.js 16 Auth & Shell UI
* Built UI slice for `/`, `/login`, and `/register`.
* Supported EN/TR dual-language switcher and Light/Dark mode with `localStorage` preference persistence and automatic browser language detection.

---

## 🧪 How to Test & Verify Phase 0

### Test 1: Run Multi-Tenant & Quota Integration Tests
Run the automated test suite in terminal:
```powershell
cd c:\Users\Lenovo\Desktop\AkironSeo
dotnet test backend/AkironSeo.sln
```
**Expected Result**: All 4 integration tests pass with 100% success (`TenantIsolationTests` verifying zero cross-tenant data leaks & `QuotaLedgerTests` verifying atomic quota deduction and double-refund prevention).

### Test 2: Verify SuperAdmin Seeded Account & Auth Login
1. Open **[http://localhost:3000/login](http://localhost:3000/login)** in your browser.
2. The form will be pre-filled with the seeded SuperAdmin credentials:
   * **Email**: `admin@akironseo.com`
   * **Password**: `Admin123!`
3. Click **Sign In to Workspace**.
4. **Expected Result**: Green success alert: *"Success! Signed in as admin@akironseo.com (Role: SuperAdmin). Workspace TenantId: 33333333-3333-3333-3333-333333333333"*.
