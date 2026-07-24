# AkironSeo - AI Coding Assistant & Developer Guidelines

## 🚨 Mandatory Project Rules

1. **Language Standard**:
   - **ALL SOURCE CODE, COMMENTS, DATABASE COLUMN NAMES, VARIABLES, DTOS, COMMIT MESSAGES, AND TECHNICAL DOCUMENTATION MUST BE WRITTEN 100% IN ENGLISH.**
   - No Turkish words in code identifiers or database schemas.

2. **Backend Architecture (.NET 10 LTS Clean Architecture)**:
   - Solution structure: `Domain`, `Application`, `Infrastructure`, `API`.
   - Use CQRS with `MediatR` (version range `[12.4.0, 13.0.0)`).
   - EF Core 10 PostgreSQL with `Npgsql`.
   - Multi-Tenancy: Denormalized `TenantId` column across all tenant-scoped tables (`IMultiTenant`). Global Query Filter automatically applied.
   - Idempotent Quota Ledger via `QuotaReservations` table.
   - Use `Cronos` library for parsing CronExpressions.

3. **Frontend Architecture (Next.js 15/16 App Router)**:
   - App Router structure under `src/app/`.
   - Tailwind CSS for styling.
   - Dual-Language Support (English & Turkish UI options).
   - Theme Toggle Support (Light Mode & Dark Mode).
   - Authentication via HttpOnly Cookies (`SameSite=Lax`).

4. **Git & Commit Standard**:
   - Commit messages must follow conventional commits standard in English (e.g. `feat: implement tenant entity`, `fix: quota reservation ledger`).
