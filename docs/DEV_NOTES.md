# Developer Notes & Setup Guide

## 🛠️ Environment Prerequisites
- **.NET SDK**: .NET 10.0 (v10.0.302 installed)
- **Node.js**: v22.14.0
- **Database**: PostgreSQL 16 (via Docker Compose `docker-compose.yml`)

## 🚀 Local Running Commands

### 1. Database (Docker PostgreSQL 16)
```powershell
# From project root: c:\Users\Lenovo\Desktop\AkironSeo
docker compose up -d
```

### 2. Backend (.NET 10 Web API)
```powershell
cd c:\Users\Lenovo\Desktop\AkironSeo\backend
dotnet run --project src/Presentation/AkironSeo.API
```

### 3. Frontend (Next.js 15/16 App Router)
```powershell
cd c:\Users\Lenovo\Desktop\AkironSeo\frontend
npm run dev
```

---

## 📐 Architecture Guidelines
- All code, comments, database column names, DTOs, and commit messages must be 100% in English.
- Use `MediatR` range `[12.4.0, 13.0.0)` for CQRS handlers.
- Use `Cronos` library for cron parsing.
- Always verify `IMultiTenant` (`TenantId`) and `ISoftDelete` (`IsDeleted`) interfaces on domain entities.
