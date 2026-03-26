# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Akyildiz Sevkiyat** is a shipment/logistics management system for Akyildiz, a facility services company. It integrates with an external ERP system called **ISS-IP** to import orders. The application is built as:

- **Backend**: ASP.NET Core 10 Web API (Clean Architecture)
- **Frontend**: Vue 3 + TypeScript SPA (in `client/`)
- **Database**: SQL Server (via Entity Framework Core 10)

## Common Commands

### Backend (run from solution root or project directory)

```bash
# Build the solution
dotnet build Akyildiz.Sevkiyat.sln

# Run the API (development)
dotnet run --project Akyildiz.Sevkiyat.WebApi

# EF Core migrations (run from solution root)
dotnet ef migrations add <MigrationName> --project Akyildiz.Sevkiyat.Infrastructure --startup-project Akyildiz.Sevkiyat.WebApi
dotnet ef database update --project Akyildiz.Sevkiyat.Infrastructure --startup-project Akyildiz.Sevkiyat.WebApi
```

### Frontend (run from `client/`)

```bash
cd client
npm install
npm run dev      # Vite dev server (http://localhost:5173)
npm run build    # Type-check + production build
npm run preview  # Preview production build
```

## Architecture

### Backend — Clean Architecture (4 layers)

```
Domain       → Entities, Enums, Exceptions, Domain Events (no dependencies)
Application  → MediatR Commands/Queries, Validators, Interfaces, DTOs
Infrastructure → EF Core DbContext, Migrations, JwtTokenService, ISSIpClient, Seeders
WebApi       → Controllers, Middlewares, Program.cs
```

**Key patterns:**
- Every feature follows CQRS via **MediatR** — commands and queries are in `Application/<Feature>/Commands|Queries/<Name>/`
- **FluentValidation** validators are co-located with their command/query and wired into a MediatR `ValidationBehavior` pipeline
- `IApplicationDbContext` is the Application layer's abstraction over EF Core — handlers depend on this interface, not `SevkiyatDbContext` directly
- Domain exceptions (`NotFoundException`, `ConflictException`, `DomainException`, `UnauthorizedException`, `ForbiddenException`) are thrown in handlers and caught by `GlobalExceptionMiddleware`, which serializes them to a consistent JSON error format: `{ type, message, errors?, traceId }`
- JWT auth: `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()` is set so the `sub` claim is NOT remapped to `nameidentifier`

### Configuration & Secrets

All sensitive config values use `SET_BY_ENV_` placeholders in `appsettings.json`. The app validates these at startup and shuts down if any placeholder is still present. For local dev use `appsettings.Development.json` (gitignored in production workflows).

Environment variable format uses double-underscore for nesting: e.g., `ConnectionStrings__SevkiyatConnection`, `Jwt__Key`, `ISSIp__KullaniciAdi`.

### External Integration — ISS-IP

`ISSIpClient` (Infrastructure) fetches orders from `http://isstr-dmz1.tr.issworld.com:88/`. It uses Basic Auth + form-based credentials. Configuration in `appsettings.json` under `ISSIp` section. The client is registered as a typed `HttpClient`.

### Frontend — Vue 3 SPA

```
client/src/
  services/     # Axios-based API service files (one per domain)
  stores/       # Pinia stores (auth, notification)
  views/        # Page-level components (mapped to routes)
  components/   # Reusable UI components
  composables/  # Vue composables (useNotification)
  directives/   # Custom directives (v-role for RBAC)
  router/       # Vue Router with auth + role guards
  layouts/      # DefaultLayout wraps all authenticated views
```

**API communication:** All HTTP calls go through `src/services/apiClient.ts` (Axios instance). The base URL is `VITE_API_BASE_URL` env var (defaults to `/api`). The request interceptor injects the JWT from `localStorage`. The response interceptor normalizes errors to `ApiError` and dispatches DOM custom events (`api:unauthorized`, `api:forbidden`, `api:server-error`).

**Auth flow:** JWT stored in `localStorage`. `authStore.init()` listens for `api:unauthorized` events to auto-logout. RBAC is enforced in the router `beforeEach` guard via `meta.roles` on route definitions, and also via the `v-role` directive for UI elements.

**User roles:** `Admin`, `Manager`, `Accounting`, `Warehouse`, `Dispatcher`

### Key Domain Entities

- **Shipment / ShipmentLine** — core entity with a status workflow (Draft → Warehouse → Picking → Ready → Preparing → Delivered)
- **IssOrder / IssOrderLine** — imported orders from ISS-IP ERP
- **Zone / ZonePreparation / ZonePreparationProject** — warehouse zone-based picking preparation
- **StockMaster / StockMapping** — internal stock catalog with mapping to ISS stock codes
- **PurchaseOrder / GoodsReceipt** — procurement module
- **Driver / Vehicle** — transport management

### Database

- Migrations live in `Akyildiz.Sevkiyat.Infrastructure/Migrations/`
- `context.Database.Migrate()` runs automatically on startup
- `UserSeeder` and `ShipmentSeeder` run after migration on every startup
- Admin password for seeding comes from `SeedData:AdminPassword` config

## Agent Sistemi

Bu proje çoklu agent mimarisiyle yönetilir. Agent tanımları `agents/` klasöründedir.

### Agent Rolleri

| Agent | Dosya | Sorumluluk |
|-------|-------|------------|
| Manager | `agents/manager.md` | Orkestratör — direktif alır, planlar, koordine eder |
| Backend | `agents/backend.md` | Tüm backend layer'lar, migration |
| Frontend | `agents/frontend.md` | Vue 3 SPA, component/view/store/router |
| Architect | `agents/architect.md` | Mimari inceleme ve onay, kod yazmaz |
| QA/UAT | `agents/qa.md` | Kalite güvence, UAT checklist, regression analizi |
| Operations | `agents/operations.md` | Operasyonel doğruluk, iş akışı analizi, saha uygunluğu |

### Çalışma Kuralı

1. **Her görev Manager Agent ile başlar** — doğrudan backend/frontend agent'a geçme
2. **Mimari değişiklikler** önce Architect Agent'tan onay alır
3. **Kullanıcıdan tek onay** alınır: feature planı sunulur, onaylanır, uygulanır
4. **Yeni frontend component/view** plan aşamasında kullanıcıya listelenir
5. **Backend migration** otomatik eklenir, onay gerekmez
6. **Execution sırası**: Backend → Frontend (backend tamamlanmadan frontend başlamaz)

Detaylı kurallar için: `agents/workflow.md`

## CORS

Development allows `http://localhost:5173` and `http://localhost:3000`. Production allows only `https://sevkiyat.akyildiz.com`. Origins are read from `Cors:AllowedOrigins` config — missing/empty config causes a hard startup failure.
