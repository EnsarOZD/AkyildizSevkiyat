# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Akyildiz Sevkiyat** is a shipment/logistics management system for Akyildiz, a facility services company. It integrates with two external ERPs:
- **ISS-IP** — source of customer orders (imported automatically on a configurable interval)
- **Netsis** — accounting ERP (shipments, purchase orders, and goods receipts are exported to it)

Stack:
- **Backend**: ASP.NET Core 10 Web API (Clean Architecture)
- **Frontend**: Vue 3 + TypeScript SPA (in `client/`)
- **Database**: SQL Server via Entity Framework Core 10

## Common Commands

### Backend (run from solution root)

```bash
dotnet build Akyildiz.Sevkiyat.sln
dotnet run --project Akyildiz.Sevkiyat.WebApi

# EF Core migrations
dotnet ef migrations add <MigrationName> --project Akyildiz.Sevkiyat.Infrastructure --startup-project Akyildiz.Sevkiyat.WebApi
dotnet ef database update --project Akyildiz.Sevkiyat.Infrastructure --startup-project Akyildiz.Sevkiyat.WebApi
```

Swagger UI is available at `/swagger` in Development mode.

### Frontend (run from `client/`)

```bash
npm run dev      # Vite dev server → http://localhost:5173
npm run build    # Type-check + production build
```

## Architecture

### Backend — Clean Architecture (4 layers)

```
Domain          → Entities, Enums, Exceptions, Domain Events (no dependencies)
Application     → MediatR Commands/Queries, Validators, Interfaces, DTOs
Infrastructure  → EF Core DbContext, Migrations, JwtTokenService, ISSIpClient, NetsisClient, Seeders
WebApi          → Controllers, Middlewares, Program.cs
```

**MediatR pipeline order (Program.cs):**
1. `AuthorizationBehavior` — role/claim checks before the handler runs
2. `ValidationBehavior` — FluentValidation; validators are co-located with their command/query

**Key patterns:**
- Every feature follows CQRS via **MediatR** — handlers live in `Application/<Feature>/Commands|Queries/<Name>/`
- `IApplicationDbContext` is the Application layer's abstraction over EF Core; handlers depend on this interface, not `SevkiyatDbContext` directly
- Domain exceptions (`NotFoundException`, `ConflictException`, `DomainException`, `UnauthorizedException`, `ForbiddenException`) are thrown in handlers and caught by `GlobalExceptionMiddleware` → consistent JSON: `{ type, message, errors?, traceId }`
- JWT auth: `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()` is set so the `sub` claim is **not** remapped to `nameidentifier`
- Rate limiting: login endpoint 5 req/15 min; route-optimization endpoint 5 req/min

### Configuration & Secrets

All sensitive config values use `SET_BY_ENV_` placeholders in `appsettings.json`. The app validates these at startup and shuts down if any placeholder is still present. For local dev use `appsettings.Development.json` (gitignored in production workflows).

Environment variable format uses double-underscore for nesting: e.g., `ConnectionStrings__SevkiyatConnection`, `Jwt__Key`, `ISSIp__KullaniciAdi`.

### External Integrations

**ISS-IP** (`Infrastructure/ExternalServices/ISSIpClient`):
- Fetches orders from `http://isstr-dmz1.tr.issworld.com:88/`
- Basic Auth + form-based credentials; config under `ISSIp` section
- Polly retry policy: 3 retries with exponential back-off (2s, 4s, 8s) on transient HTTP errors
- `IssOrderImportBackgroundService` runs on `IssImport:IntervalMinutes` interval to auto-import

**Netsis** (`Infrastructure/ExternalServices/NetsisClient`):
- Internal ERP at `http://192.168.1.200:7071`; config under `Netsis` section
- Token-based auth with `NetsisTokenCache` (singleton, token refreshed every ~55 min)
- In Development, TLS validation is skipped (self-signed cert on internal IP)
- Used to export: shipments (irsaliye), purchase orders, and to sync stock balances/irsaliye

### Frontend — Vue 3 SPA

**Two layouts:**
- `DefaultLayout` — all authenticated staff routes under `/`
- `DriverLayout` — mobile-optimized driver interface under `/driver/*`

```
client/src/
  services/     # Axios-based API service files (one per domain)
  stores/       # Pinia stores: auth, notification, theme
  views/        # Page-level components (mapped to routes)
  components/   # Reusable UI components
  composables/  # useNotification, useKeyboardShortcut
  directives/   # v-role (RBAC), others
  router/       # Vue Router with auth + role guards
  layouts/      # DefaultLayout, DriverLayout
  utils/        # apiError, exportExcel, turkishSearch
  navigation.ts # Sidebar nav items grouped by section with role filters
```

**API client** (`src/services/apiClient.ts`):
- Base URL: `VITE_API_BASE_URL` env var (defaults to `/api`)
- Request interceptor injects Bearer token from `localStorage`
- Response interceptor: normalizes errors to `ApiError`, dispatches DOM events (`api:unauthorized`, `api:forbidden`, `api:server-error`)
- Implements a **concurrent token refresh queue** — parallel 401s all wait for a single refresh call before being retried

**Auth flow:** JWT + refresh token stored in `localStorage`. `authStore.init()` listens for `api:unauthorized` to auto-logout. RBAC enforced in router `beforeEach` via `meta.roles` and via the `v-role` directive for UI elements.

**User roles:** `Admin`, `Manager`, `Accounting`, `Warehouse`, `Dispatcher`, `Driver`

### Key Domain Entities & Status Workflows

**Shipment / ShipmentLine** — core entity:
```
Created → AssignedToWarehouse → Picking → ReadyForDispatch → AssignedToVehicle → Dispatched → Delivered
                                                                                              ↓
                                                                               ReturnedToWarehouse | Cancelled | Passive
```

**IssOrder / IssOrderLine** — imported from ISS-IP; grouped into `ImportBatch` records per import run

**Zone / ZonePreparation / ZonePreparationProject** — warehouse zone-based picking preparation workflow

**StockMaster / StockMapping** — internal stock catalog mapped to ISS stock codes; StockLocation tracks bin-level position

**PurchaseOrder / GoodsReceipt** — procurement module; GoodsReceipts can be exported to Netsis

**Driver / Vehicle / DriverSession** — transport management; driver sessions track time on duty

**StockCount** — periodic physical inventory counts with Excel template export/import

**FloatingReturn** — unmatched returns awaiting resolution

**ReconciliationIssue** — data integrity checks (5 check types) with acknowledge workflow

### Database

- Migrations: `Akyildiz.Sevkiyat.Infrastructure/Migrations/`
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
