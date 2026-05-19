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

## Repository & Operational Context

This section describes the **operational reality** of this project, not its theoretical architecture. Future audits should interpret findings through this lens — what counts as P0 risk in a public SaaS startup may be P3 here.

**Repository status:**
- Private repository, single developer (Ensar).
- No external collaborators, no fork access, no CI pipeline using the repo as a build source.
- Git is used for version control on the developer machine; production does NOT pull from git. The repository can be treated as a personal workspace, not a shared codebase.

**Production usage:**
- Akyıldız Lojistik uses this system in active production.
- Operational risk tolerance is LOW — any change that could break ISS-IP / Netsis / YurtiKargo integrations during business hours is unacceptable without an explicit rollback plan.
- The system handles real shipments, real driver sessions, and real Netsis exports. Downtime has real-world cost.

**Audit interpretation guidance:**
When auditing this repository, before tagging an item as P0/P1:
1. Verify the risk applies to the *production deployment*, not just the source tree (e.g., secrets in `appsettings.json` are harmless if production loads them from environment variables — check the deployment first).
2. Verify the risk has a realistic exploitation path given the single-developer, private-repo, manual-deploy context.
3. Do not assume standard SaaS attack surfaces (untrusted contributors, CI runners, public clones). State your assumptions explicitly so the developer can correct them.

A finding that is theoretically a security violation but has no realistic exploitation path in this context should be labeled P3 (hygiene, not urgent) with the reasoning stated.

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

### Tests (run from solution root)

```bash
dotnet test Akyildiz.Sevkiyat.Domain.Tests
dotnet test --filter "DisplayName~<MethodName>"   # run a single test by name
```

xUnit 2.9.3 + coverlet; currently covers `StockMaster` domain methods. No integration or API tests.

### Frontend (run from `client/`)

```bash
npm run dev      # Vite dev server → http://localhost:5173 (proxies /api → localhost:5087)
npm run build    # Type-check (vue-tsc) + production build
npm run preview  # Serve the production build locally
vue-tsc -b       # Type-check only, no emit
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

**Base entity:** `AuditableEntity` — abstract base class all domain entities inherit; provides `CreatedAt` (UTC), `CreatedBy`, `LastModified`, `LastModifiedBy`. These columns appear on every table.

**Key patterns:**
- Every feature follows CQRS via **MediatR** — handlers live in `Application/<Feature>/Commands|Queries/<Name>/`
- Commands/Queries that require role checks implement `IRequireRoles`; `AuthorizationBehavior` in the MediatR pipeline enforces this before the handler runs
- `IApplicationDbContext` is the Application layer's abstraction over EF Core; handlers depend on this interface, not `SevkiyatDbContext` directly
- `IApplicationDbContext.WarehouseShipments` — filtered `IQueryable<Shipment>` that excludes `OperationType == Clothing`; **all warehouse pipeline handlers must use this instead of `Shipments`** to keep Clothing shipments out of the depo hazırlık flow. Use raw `Shipments` only when you explicitly need to see Clothing (e.g., `SyncWarehouseDashboard` to unlink them, or the `hasClothingShipments` guard in `StartZonePreparation`)
- Domain exceptions (`NotFoundException`, `ConflictException`, `DomainException`, `UnauthorizedException`, `ForbiddenException`) are thrown in handlers and caught by `GlobalExceptionMiddleware` → consistent JSON: `{ type, message, errors?, traceId }`
- Entities that raise domain events implement `IHasDomainEvents`; `Shipment` raises `ShipmentStatusChangedEvent` on status transitions
- `Shipment` uses `RowVersion` for optimistic concurrency — concurrent updates throw `DbUpdateConcurrencyException`
- JWT auth: `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()` is set so the `sub` claim is **not** remapped to `nameidentifier`; token is read from the `sevkiyat_jwt` HttpOnly cookie first, then the `Authorization: Bearer` header
- All endpoints require auth by default; `[AllowAnonymous]` must be explicit
- Rate limiting: login 5 req/15 min (with `ILoginAttemptTracker`); route-optimization 5 req/min (fixed window); token refresh 10 req/min (sliding window, 2 segments)
- Login lockout: 5 failed attempts → 5-minute lockout tracked by `ILoginAttemptTracker`; lockout is admin-resettable via `AdminController`

### Configuration & Secrets

**Production secrets are stored in environment variables**, loaded by systemd from `/etc/sevkiyat/sevkiyat.env` (root-owned, chmod 600). The `appsettings.json` file in production contains only `SET_BY_ENV_*` placeholders — the .NET `IConfiguration` system overlays environment variables on top of these placeholders at startup.

This means:
- The committed `appsettings.json` is safe to deploy — it contains no real secrets.
- The committed `appsettings.Development.json` IS gitignored (since commit `0a0e776`); a tracked template lives at `appsettings.Development.template.json` for local dev setup.
- The startup validator (`Program.cs`) shuts down the app if any `SET_BY_ENV_*` placeholder is still present, ensuring missing env vars cause a fast, visible failure rather than silent misconfiguration.

**Environment variable format** uses double-underscore for nesting: `ConnectionStrings__SevkiyatConnection`, `Jwt__Key`, `ISSIp__KullaniciAdi`, `Netsis__Sifre`, `Vapid__PrivateKey`, etc.

**Historical note for audits:** Earlier commits in this repository's history contained real secret values in `appsettings.Development.json`. These have been scrubbed in commit `bf863ff` and the file is now gitignored. Because the repository is private and has never been cloned by anyone other than the sole developer, rewriting git history to remove the historical secrets is unnecessary — the realistic exploitation surface is empty. The same secrets in production live exclusively in `/etc/sevkiyat/sevkiyat.env`, not in the source tree.

**Frontend:** `client/.env.example` → `VITE_API_BASE_URL=http://localhost:5087/api`. In dev, Vite proxies `/api` to `http://localhost:5087` so the env var is only needed for production. VAPID public key is set via `VITE_VAPID_PUBLIC_KEY` (also in `client/.env.local` for local dev).

### Deployment

**Production environment:** Hetzner Ubuntu VM running Nginx + systemd-managed .NET service.

**Deploy method:** Manual, via PowerShell script (`deploy.ps1`) on the developer machine. The script:
1. Runs `dotnet publish --configuration Release` locally.
2. Runs `npm run build` for the frontend.
3. Stops the systemd service on the server.
4. Wipes `/var/www/sevkiyat-api/*` and `/var/www/sevkiyat-app/*`.
5. Copies the new publish output and frontend dist over via scp.
6. Restarts the systemd service.

**There is no git-based deploy, no CI/CD, no staging environment.** The production VM does not have a clone of this repository.

**Files that survive deploy** (because they live outside `/var/www/sevkiyat-api/`):
- `/etc/sevkiyat/sevkiyat.env` — production secrets, loaded by systemd via `EnvironmentFile=`
- `/etc/systemd/system/sevkiyat-api.service` — service unit
- Nginx configuration under `/etc/nginx/`

**Files that get overwritten on every deploy:**
- All `.dll`, `.json`, and static asset files in `/var/www/sevkiyat-api/` — including `appsettings.json`
- All frontend assets in `/var/www/sevkiyat-app/`

This means: **never put production secret values into `appsettings.json` in the source tree.** They would be wiped on the next deploy anyway, but more importantly, the deploy script does not preserve them — production relies on env vars through systemd.

**Known deploy-script limitation:** The current `deploy.ps1` uses `rm -rf ${API_PATH}/*` before `scp`, meaning any manually-edited file in `/var/www/sevkiyat-api/` is destroyed on deploy. This is acceptable because production config lives outside `/var/www/`. If a file inside `/var/www/sevkiyat-api/` must persist across deploys, it must be moved to `/etc/` or similar first.

**Operational risk:** Akyıldız uses this system in active production. Run deploys during off-hours when possible, and verify the service is healthy (`systemctl status sevkiyat-api`) before walking away.

### Infrastructure Details

**Kestrel:** Max request body size set to 30 MB (for file/photo uploads).

**Static files:** Photos are served from the `/photos/` path via a static files middleware configured to read from a directory on disk.

**Background services:**
- `IssOrderImportBackgroundService` — auto-imports ISS-IP orders on `IssImport:IntervalMinutes` interval
- `YkStatusSyncBackgroundService` — polls YurtiKargo for cargo status updates
- `NetsisIrsaliyeSyncBackgroundService` — syncs irsaliye records from Netsis on a scheduled interval
- `ReconciliationSummaryEmailBackgroundService` — sends reconciliation summary emails on a scheduled interval
- `BackgroundServiceStatusTracker` (singleton) — records last-run timestamp and result for each background service; exposed via health/admin endpoints

**Domain/application services (scoped, registered in Program.cs):**
- `ReconciliationGuard` — blocks dispatch-critical transitions when open reconciliation issues exist
- `PreDispatchGuard` — validates pre-dispatch checklist before vehicle assignment
- `ZoneAutoCloseService` — auto-closes a zone preparation when all lines are confirmed
- `IPhotoStorageService` / `FilePhotoStorageService` — saves/reads uploaded photos from disk (`PhotoStorage:BasePath` config)
- `IEmailService` / `SmtpEmailService` — SMTP delivery; config under `Smtp` section

**Database:** `context.Database.Migrate()` runs automatically on startup. `UserSeeder` and `ShipmentSeeder` run after migration on every startup. Admin password comes from `SeedData:AdminPassword` config.

### External Integrations

**ISS-IP** (`Infrastructure/ExternalServices/ISSIpClient`):
- Fetches orders from `https://generalapi.issturkiye.com/`
- Basic Auth + form-based credentials; config under `ISSIp` section
- Polly retry policy: 3 retries with exponential back-off (2s, 4s, 8s) on transient HTTP errors

**Netsis** (`Infrastructure/ExternalServices/NetsisClient`):
- Internal ERP at `http://192.168.1.200:7071`; config under `Netsis` section
- Token-based auth with `NetsisTokenCache` (singleton, token refreshed every ~55 min)
- In Development, TLS validation is skipped (self-signed cert on internal IP)
- Used to export: shipments (irsaliye), purchase orders; sync stock balances/irsaliye
- `NetsisTransferredAt` on `Shipment` is the **authoritative** transfer flag; `IssOrder.IsTransferred` is secondary and can drift out of sync

**YurtiKargo** (`Infrastructure/ExternalServices/YurtiKargoClient`):
- Cargo tracking integration; supports proxy configuration (`ProxyUrl` in config)
- Config under `YurtiKargo` section — all three fields (`BaseUrl`, `WsUserName`, `WsPassword`) are `SET_BY_ENV_` placeholders validated at startup
- `YurtiKargoOptions.IsConfigured` gates all API calls; checks both non-empty AND no `SET_BY_ENV_` prefix
- **Barcode flow**: `DispatchZoneAsCargoCommand` calls `createShipment` → response `barcode` element → stored as `Shipment.YkBarcode`; `YkCargoKey` is our reference (`AKY-{id}`), `YkBarcode` is YK's assigned barcode (what gets printed on the cargo label)
- Status synced by `YkStatusSyncBackgroundService` every 30 min; `UpdateYkStatus` also back-fills `YkBarcode` if query response includes it
- `POST /api/warehouse/shipments/{id}/yk-register` — admin endpoint to re-trigger `createShipment` for shipments that were dispatched before YK was configured (`RetryYkShipmentRegistrationCommand`)

**Google APIs:**
- Google Maps (HTTP/1.1 forced) + Google Geocoding — used for route optimization

### Notification System

Two-channel notification architecture:

**SSE (Server-Sent Events)** — real-time in-app:
- `GET /api/notifications/stream` — long-lived SSE connection, 25s heartbeat
- `SseChannelManager` (Infrastructure) manages per-user `Channel<T>` instances
- `DefaultLayout` connects SSE on mount via `notificationsStore.connectSSE()`
- `NotificationBell.vue` shows unread count badge

**Web Push (VAPID)** — push when tab is closed:
- `POST /api/notifications/push-subscribe` / `DELETE /api/notifications/push-subscribe`
- VAPID keys in config (`Vapid:PublicKey`, `Vapid:PrivateKey`, `Vapid:Subject`)
- Frontend: `useWebPush.ts` composable, service worker `sw.ts` handles `push` + `notificationclick` events
- iOS 16.4+ requires PWA to be installed (added to home screen) for push to work; desktop Chrome/Edge/Firefox fully supported

**Sound + Vibration feedback** — `useSoundFeedback.ts` composable:
- `success()` — 880 Hz beep + 60 ms vibration (item picked)
- `error()` — double low square wave + 120-60-120 ms vibration
- `complete()` — C5→E5→G5 melody + long vibration (workflow completed)
- `newAssignment()` — two-tone alert + 100-60-100 ms vibration
- Vibration: Android Chrome only (`navigator.vibrate?.()`); iOS does not support it; desktop silent

### Frontend — Vue 3 SPA

**Two layouts:**
- `DefaultLayout` — all authenticated staff routes under `/`; SSE lifecycle managed here
- `DriverLayout` — mobile-optimized driver interface under `/driver/*`; `Driver` role users are auto-redirected from `/` to `/driver`; uses `<router-view :key="$route.fullPath" />` to force remount on back navigation

```
client/src/
  services/     # Axios-based API service files (one per domain)
  stores/       # Pinia stores: auth, notification, notifications (SSE), theme, driverRoute
  views/        # Page-level components (mapped to routes)
  components/   # Reusable UI components
  composables/  # useNotification, useKeyboardShortcut, useLocationPermission, useSoundFeedback, useWebPush, useDeliveryQueue
  directives/   # v-role (RBAC — removes element from DOM if role not matched)
  router/       # Vue Router with auth + role guards
  layouts/      # DefaultLayout, DriverLayout
  utils/        # apiError, exportExcel, turkishSearch
  sw.ts         # Custom service worker (Workbox injectManifest) — handles push + notificationclick
  navigation.ts # Sidebar nav items grouped by section with role filters
```

**Key frontend dependencies:**
- `xlsx` — Excel template export/import (StockCount, reports)
- `jspdf` + `html2canvas` — PDF generation for shipment orders and goods receipts
- `jsqr` + `tesseract.js` — QR code scanning and OCR (driver QR scan view)
- `vite-plugin-pwa` — PWA with Workbox (`injectManifest` strategy, custom `sw.ts`); network-first for `/api`, cache-first for static assets

**API client** (`src/services/apiClient.ts`):
- Base URL: `VITE_API_BASE_URL` env var (defaults to `/api`)
- `withCredentials: true` — HttpOnly cookies sent automatically; no manual token injection
- Response interceptor: normalizes errors to `ApiError`, dispatches DOM events (`api:unauthorized`, `api:forbidden`, `api:server-error`)
- Implements a **concurrent token refresh queue** — parallel 401s all wait for a single refresh call; after a successful refresh the server sets a new cookie and the request is retried without manual header injection

**Pinia stores:**
- `auth` — user info (name, role, email), login/logout, token refresh (tokens are in HttpOnly cookies, not this store)
- `notification` — toast notifications (`notify(type, msg)`), confirm dialog (`confirm.show()`, `confirm.requireDelete()`)
- `notifications` — SSE connection lifecycle, unread notification list
- `reconciliation` — open reconciliation issue count for sidebar badge
- `driverRoute` — bridges `mapsRouteUrl` from DriverShipmentListView to DriverLayout nav button
- `malKabul` — in-flight session state for the goods intake workflow (MalKabul dashboard); holds `SessionEntry[]` (stock + PO allocations) before posting the GoodsReceipt
- `theme` — dark/light mode

**Auth flow:** JWT and refresh token are stored in **HttpOnly cookies** (`sevkiyat_jwt`) — never in `localStorage`. Only non-sensitive user info (name, role, email) is persisted in `localStorage` under the `user` key. `axios` is configured with `withCredentials: true` so cookies are sent on every request. The server reads the cookie first, then falls back to a `Authorization: Bearer` header (for Postman/non-browser clients). `authStore.init()` listens for `api:unauthorized` to auto-logout. RBAC enforced in router `beforeEach` via `meta.roles` and via the `v-role` directive for UI elements.

**User roles:** `Admin`, `Manager`, `Accounting`, `Warehouse`, `Dispatcher`, `Driver`

**PWA service worker (`src/sw.ts`):** Compiled separately by Vite (excluded from `tsconfig.app.json` to avoid vue-tsc type errors). Uses `ServiceWorkerGlobalScope` globals. Do not add it back to tsconfig.

### Key Domain Entities & Status Workflows

**Shipment / ShipmentLine** — core entity:
```
Created → AssignedToWarehouse → Picking → ReadyForDispatch → AssignedToVehicle → Dispatched → Delivered
                                                                ↓                              ↓
                                                     Dispatched (direct cargo/freight)   ReturnedToWarehouse
                                                                                         Cancelled | Passive
```
- `StockReserved` flag prevents double-reservation in `AssignToWarehouseCommand`
- Skip transitions (Created→Picking, AssignedToWarehouse→ReadyForDispatch) require explicit reason
- `OperationType` (`Catering=0`, `Clothing=1`) on `Shipment` — determines warehouse routing; Clothing shipments skip the `ZonePreparation` / depo hazırlık pipeline entirely and go directly to Netsis export (`DepoKodu="2"`). Use `_context.WarehouseShipments` in warehouse handlers to automatically exclude them.
- `CargoProvider` enum (`YurtiKargo`, etc.) + cargo tracking number stored on `Shipment` when dispatched as cargo
- `ShipmentHistory` — immutable audit log row created on every status transition (Status, Description, ChangedBy, ChangedAt)

**ZonePreparation** — warehouse batch picking lifecycle:
```
Draft → MicroPicking → MicroReady → MacroPicking → GidaHazirlik → ReadyForDriverInfo → ReadyForTransfer → Dispatched
```
- `IsFrozen` must be true before driver assignment; `IrsaliyeFetched` must be true before vehicle assignment
- MacroLock prevents concurrent macro picking; 60-min expiry
- `SetZoneDriverInfoCommand` blocks reassignment if session already Open on the same zone; also blocks if the primary driver has an open session on a **different** zone
- `ConfirmZoneLoadingCommand` blocks if the primary driver's session is Open on any zone

**IssOrder / IssOrderLine** — imported from ISS-IP; grouped into `ImportBatch` records per import run

**Project** — customer/delivery location:
- `OperationType` (`Catering=0`, `Clothing=1`) gates certain workflows
- Two Netsis codes: `NetsisCariKodu` (billing entity) and `NetsisTeslimCariKodu` (delivery entity)
- Coordinates (Latitude/Longitude) for Google Maps integration

**StockMaster** — internal stock catalog:
- `OnHandQty`, `ReservedQty`, `AvailableQty = OnHandQty - ReservedQty`
- Domain methods: `Reserve(qty)`, `ReleaseReservation(qty)`, `Deduct(qty)`, `Increase(qty)`, `AdjustOnHand(diff)`, `OverrideOnHand(newQty)` — only these mutate stock quantities
- `MinStockQty` (critical threshold) and `ReorderPoint` (suggest reorder) — thresholds exist, no automated trigger yet
- `RowVersion` optimistic concurrency token
- `PickingType` (Micro/Macro/Unassigned) and `PickingOrder` control warehouse picking flow
- `WeightKg` for tonnage calculation

**StockLocation / WarehouseLocation** — bin-level tracking:
- `WarehouseLocation` code format: `{KoridorNo}{Taraf}-{ModulNo:D3}-{Kat:D2}` (e.g., `1K-001-03`)
- `StockLocation` = StockMasterId + WarehouseLocationId (unique) → per-bin quantity
- `LocationTransfer` logs all inter-bin movements (FromLocation, ToLocation, Qty, TransferredBy)
- `StockTransaction` is the general audit trail (GoodsIn, ShipmentOut, ManualAdjust, Reserve, ReleaseReserve, VehicleReturn, GoodsInCorrection)
- **Gap**: StockMaster.OnHandQty and sum of StockLocation.OnHandQty can diverge — assignments to locations don't auto-update StockMaster totals; picking from a zone decrements StockMaster but not specific bin locations

**StockMapping** — ISS-IP external stock code → internal `StockMaster` mapping:
- `MatchStatus`: Matched / Unmatched / Partial
- Auto-match capability (fuzzy matching by name)

**StockCount** — periodic physical inventory:
- Excel template export/import (ClosedXML + ExcelDataReader)
- `ExpectedQty` from system vs `ActualQty` from physical count; diff generates adjustment

**StockConsumption** — non-shipment stock out:
- Types: `Zai` (wastage), `DahiliKullanim` (internal use), `DepoSatisi` (depot sale)

**PurchaseOrder / GoodsReceipt** — procurement:
- PO statuses: Draft → Approved → PartiallyReceived → Closed / Cancelled
- GoodsReceipt statuses: Draft → Posted → Cancelled
- Posting a GoodsReceipt calls `StockMaster.Increase()` and creates a `StockTransaction(GoodsIn)`
- Both can be exported to Netsis; `NetsisTransferredAt` is the idempotency guard

**Driver / Vehicle / DriverSession** — transport:
- `DriverSession` tracks time on duty; `ZonePreparationId` links it to a zone batch
- Status: Open / Closed / ForceClosed; only one Open session per driver (DB unique index)
- QR code on vehicle enables scan-to-start-session workflow

**VehicleReturn / VehicleReturnLine** — items returned from vehicle after delivery

**FloatingReturn** — unmatched returns awaiting warehouse resolution

**ReconciliationIssue** — 5 check types, upsert pattern (IssueKey), statuses: Open / Acknowledged / Resolved

**SystemSettings** — singleton table for runtime-configurable app settings; exposed via `SystemSettingsController` (`Admin` only)

**ExternalEmailContact** — stores external email recipients for PO notification emails; exposed via `ExternalEmailContactsController`

**Notification / PushSubscription** — SSE + Web Push notification records

## Documentation

Deep-dive docs live in `/docs/`: `RECONCILIATION.md`, `STOCK_ARCHITECTURE.md`, `Warehouse_Management.md`, `NETSIS_INTEGRATION.md`, `DRIVER_AUDIT.md`, `WMS_ROADMAP.md`, `PICKING_ANALYSIS.md`, and others. Consult these before making changes to their respective subsystems.

## AI Orchestrator

An AI orchestrator is maintained at `D:\vue\ai-orchestrator` (separate repository). It is a **markdown-only** prompt library — no code, no SDK.

**Available agent roles** (prompt files in `agents/`):
- `manager.md` — classifies tasks, selects minimal agent set, merges outputs
- `backend.md` — API, domain, DB, validation, auth, integration changes
- `frontend.md` — UI flow, components, state, API usage
- `architect.md` — cross-layer impact review; does NOT write code
- `qa-reviewer.md` — correctness, edge cases, regression, final quality gate

**Workflows** (in `workflows/`):
- `plan-feature.md` — multi-agent planning: classify → select agents → merge → output plan
- `build-feature.md` — multi-agent implementation: plan → execute (backend ‖ frontend parallel when independent) → QA check
- `review-feature.md` — multi-agent review: target identification → agent review → merge findings

**Usage rule:** Invoke Manager first for non-trivial tasks; it selects only necessary agents. Backend + Frontend can run in parallel when independent. Architect is called only for cross-layer/schema/auth/breaking changes. Never expand scope beyond request.

## CORS

Development allows `http://localhost:5173` and `http://localhost:3000`. Production allows only `https://sevkiyat.akyildizlojistik.com`. Origins are read from `Cors:AllowedOrigins` config — missing/empty config causes a hard startup failure.

## WMS — Mevcut Durum ve Yol Haritası

Full roadmap: `docs/WMS_ROADMAP.md`

### Tamamlanan WMS Altyapısı

**Temel katman (önceki sprint):** `WarehouseLocation`, `StockLocation`, `LocationTransfer`, `StockTransaction`, `StockCount` — lokasyon hiyerarşisi ve stok-lokasyon bağlantısı mevcut.

**Barkod & Toplama Gözü — Phase 1 (2026-05-15):**
- Yeni entity'ler: `StockBarcode`, `PutawayTask`, `PutawayLine`
- `WarehouseLocation`'a: `Alan` (açıklama alanı — artık kod üretiminde kullanılmıyor), `QrCode` (katsız raf QR), `TotalFloors`
- `LocationType` enum'una `PickingFace = 6` eklendi
- `StockMaster`'a: `Barcode` (birincil barkod), `DefaultPickingFaceId` (FK)
- `SystemSettings`'e 3 WMS feature flag (hepsi varsayılan false, mevcut akışı bozmaz):
  - `WmsPutawayEnabled` — mal kabul → lokasyon dağıtım ekranı
  - `WmsLocationPickingEnabled` — picking'de StockLocation bazlı düşüm
  - `WmsBarcodePickingEnabled` — picking'de barkod tarama zorunluluğu
- Migration: `20260514224602_WmsPhase1_BarcodeAndPutaway`
- Endpoint'ler: `GET/PUT /api/system-settings/wms`, `POST /api/warehouse-locations/picking-face`, `GET /api/warehouse-locations/{id}/qr`
- Frontend: WMS toggle ayarları, Raflar/Toplama Gözleri sekme ayrımı, QR baskı, stok barkod alanı

**Adres Şeması & UI — Phase 1b (2026-05-17):**
- `ContainerType` enum eklendi: `Pallet=0`, `Case=1` (koli/kayar raf), `Box=2` (kutu/iç adres)
- `WarehouseLocation`'a: `InnerLevel` (string?), `InnerPosition` (int?), `ContainerType`
- **PickingFace adres formatı değişti** — eski `AKA-001` formatı kaldırıldı; artık raf ile aynı format:
  - Palet: `{K}{T}-{M:3}-{kat:2}` (örn. `2K-001-00`)
  - Koli / Kutu: `{K}{T}-{M:3}-00-{harf}{pos:2}` (örn. `2K-001-00-A01`)
  - `BuildPickingFaceCode()` geriye dönük uyumluluk için korundu ama yeni kodlarda kullanılmaz
- Migration: `WmsPhase2_ContainerTypeAndInnerAddress`
- Endpoint'ler: `GET /api/warehouse-locations/map`, `DELETE /api/warehouse-locations/{id}`, `DELETE /api/warehouse-locations/bulk`
- Frontend: `WarehouseMapView.vue` (depo haritası — koridor × modül ızgarası), toplu silme, ContainerType desteği

### Sıradaki — Phase 2 İş Akışları

Hangi feature flag'in ne açtığı:

| Flag | Açar | Öncelik |
|------|------|---------|
| `WmsPutawayEnabled` | Mal kabul onayı → `PutawayTask` oluşur → depocu terminal ekranından lokasyona dağıtır | YÜKSEK |
| `WmsBarcodePickingEnabled` | Picking sırasında barkod tarama zorunlu olur; yanlış ürün → hata sesi | ORTA |
| `WmsLocationPickingEnabled` | Picking'de sistem lokasyon önerir, `StockLocation.OnHandQty` düşülür | İLERİDE |

Flag bağımsız öncelikli iş: **Raf → Toplama Gözü transfer ekranı** (mevcut `TransferStock` command var, sadece frontend gerekiyor).

### Bilinen Gap'ler

| Gap | Öncelik |
|-----|---------|
| `StockMaster.OnHandQty ↔ SUM(StockLocation.OnHandQty)` uyumsuzluğu | Yüksek |
| `DefaultPickingFaceId` frontend atama ekranı yok (stok kartından seçilemiyor) | Orta |
| Lot/batch + expiry (FEFO) — `LotNo`, `ExpiryDate` entity'lerde yok | Düşük |
| Döngüsel sayım (cycle count) — sadece tam sayım var | Düşük |
| Replenishment otomasyonu — `MinStockQty`/`ReorderPoint` var, otomatik PO önerisi yok | Düşük |

### Cihaz Notu
- **RT112 tablet** → Müdür kullanımı (yönetim ekranları)
- **TC15 handheld (Zebra)** → Depo personeli + şoförler; DataWedge barkodu klavye girişi olarak gönderir — PWA ile native app gerekmez
