# Secrets Rotation Runbook — 2026-05-19

**Trigger:** `audit/AUDIT_REPORT_2026-05-19.md` §4.1 — live credentials were committed to git in `appsettings.Development.json` and `appsettings.json`, and remain in `git log` history even after the working-copy scrub.

**Assumption about disclosure scope:** the repository is private but should still be treated as exposed. Any team-mate with `git fetch` history, any CI cache, any cloned laptop backup, and any forked or mirrored remote retains the secrets. Rotation is mandatory.

**Order:** lowest blast radius first, so you can practice the procedure before touching anything that pages users mid-shift.

After every rotation:
1. Set the new value in the deployment environment (server env vars, not in any file). The exact env-var name is in each section.
2. Set the new value locally in `Akyildiz.Sevkiyat.WebApi/appsettings.Development.json` (which is now gitignored).
3. Run the verification step in that section.
4. Tick the box at the bottom of this file.

The historical values are intentionally not duplicated here — they live in the audit report. Don't propagate them into more files.

---

## 1. VAPID key pair (lowest blast radius)

- **What it does:** signs Web Push payloads. Browsers verify the signature.
- **Where to rotate:** generate locally with `npx web-push generate-vapid-keys` (the `web-push` npm package). Or any VAPID key generator that yields the standard URL-safe base64 EC P-256 keys.
- **What breaks temporarily:** all existing browser push subscriptions will start failing once the server begins signing with the new key. Users have to re-subscribe (they'll be prompted on next page load, or you can clear the `PushSubscriptions` table to force it).
- **Env vars:**
  - `Vapid__PublicKey`
  - `Vapid__PrivateKey`
  - (`Vapid__Subject` stays as the operator email; no rotation needed.)
- **Frontend follow-up:** `client/.env.local` → `VITE_VAPID_PUBLIC_KEY` must match the new public key, or the service worker subscription request will use the old key and fail. Rebuild + redeploy the SPA.
- **Verify:**
  1. After deploy, open the app in a fresh browser, accept notification permission.
  2. Confirm a new row appears in the `PushSubscriptions` table.
  3. Trigger any notification path (e.g. assign a shipment to a vehicle) and confirm the push arrives.

---

## 2. YurtiKargo test credentials

- **What it does:** authenticates against the YK *test* sandbox (`testws.yurticikargo.com`). The committed values (`YKTEST` / `YK`) are widely-known YK demo creds, but should not be in our repo.
- **Where to rotate:** request a project-specific test account from YurtiKargo (your YK account manager). For now you can also rotate to per-developer test creds.
- **What breaks temporarily:** `YkStatusSyncBackgroundService` will fail polling and any `DispatchZoneAsCargoCommand` against the test endpoint will return auth errors until env vars are updated.
- **Env vars:**
  - `YurtiKargo__WsUserName`
  - `YurtiKargo__WsPassword`
  - `YurtiKargo__BaseUrl` (production value differs from the test URL — set this in the prod deploy)
- **Verify:**
  1. `curl -s -u "$WsUserName:$WsPassword" "$YurtiKargo__BaseUrl?wsdl" | head -5` — should return valid WSDL.
  2. In the app, hit the admin `POST /api/warehouse/shipments/{id}/yk-register` retry endpoint against a known-test shipment and confirm a barcode is returned.

---

## 3. JWT signing key (`Jwt__Key`)

- **What it does:** HMAC-SHA256 signs every issued access + refresh token.
- **Where to rotate:** generate a fresh ≥32-character random string, e.g. `openssl rand -base64 48`.
- **What breaks temporarily:** **all** active sessions are invalidated the moment the new key is loaded. Every user (and the driver app on TC15 handhelds) has to log in again. Plan to do this during low-traffic.
- **Env var:** `Jwt__Key`
- **Verify:**
  1. POST `/api/auth/login` with a valid user, confirm 200.
  2. Decode the returned `accessToken` at jwt.io — only the signature should fail to verify, since the key is server-side.
  3. Send an old (pre-rotation) JWT in `Authorization: Bearer …` against any authenticated endpoint and confirm 401.

---

## 4. Seed admin password (`SeedData__AdminPassword`)

- **What it does:** `UserSeeder` writes/refreshes the bootstrap admin user on every startup using this value.
- **Where to rotate:** pick a new strong password (treat it like a service password; store in your password manager). Verify behavior on existing admin user before rotating in production — `UserSeeder` may or may not re-hash an existing admin's password from this value. If it doesn't, you must also update the admin's password via the admin UI or by re-seeding against a clean DB.
- **What breaks temporarily:** none, **provided** there is at least one other Admin-role user who can log in. If admin is the only Admin user and the seeder doesn't update existing rows, you'll be locked out.
- **Env var:** `SeedData__AdminPassword`
- **Pre-flight check (do this first):**
  1. Create a second Admin user via the admin UI as a break-glass account.
  2. Verify you can log in as that user.
- **Verify after rotation:**
  1. Restart the app, watch logs for `UserSeeder` output.
  2. Log out, log in as admin with the new password.

---

## 5. ISS-IP credentials (vendor coordination required)

- **What it does:** authenticates the order-import + project-detail calls to `https://generalapi.issturkiye.com/`.
- **Where to rotate:** contact ISS-IP vendor support — there are two credential layers and *both* need new values:
  1. `KullaniciAdi` / `Sifre` — the application-level user.
  2. `BasicAuthUsername` / `BasicAuthPassword` — the HTTP Basic Auth wrapper.
- **What breaks temporarily:** `IssOrderImportBackgroundService` stops importing orders until env vars are updated. No outbound damage, but new ISS orders won't appear in the app — coordinate with operations.
- **Env vars:**
  - `ISSIp__KullaniciAdi`
  - `ISSIp__Sifre`
  - `ISSIp__BasicAuthUsername`
  - `ISSIp__BasicAuthPassword`
- **Verify:**
  1. Trigger a manual import (`POST /api/orders/import` from the UI as Admin).
  2. Confirm a new `ImportBatch` row appears in the DB with a non-zero order count.
  3. Tail the application logs for any 401/403 from ISS.

---

## 6. Netsis credentials + DB user

- **What it does:** the Netsis ERP integration uses both:
  1. `Netsis__KullaniciAdi` / `Netsis__Sifre` — Netsis REST API token-issuing endpoint.
  2. `Netsis__DbUser` (was `TEMELSET`) / `Netsis__DbPassword` — direct DB read for irsaliye / stock-balance queries.
- **Where to rotate:** Netsis admin console (Logo Netsis user management) for the application user. The DB user is typically rotated in SQL Server directly (`ALTER LOGIN [TEMELSET] WITH PASSWORD = N'…'`) by your DB admin.
- **What breaks temporarily:**
  - Netsis token-cache must refresh — `NetsisTokenCache` singleton holds the old token for up to ~55 min. Restart the app or wait it out.
  - `NetsisIrsaliyeSyncBackgroundService` will fail until env vars are updated.
  - `ExportShipmentToNetsisCommand` will fail.
  - `VerifyNetsisShipmentTransfersCommand` will fail.
- **Env vars:**
  - `Netsis__KullaniciAdi`
  - `Netsis__Sifre`
  - `Netsis__DbUser`
  - `Netsis__DbPassword`
- **Pre-flight:** confirm with operations that no in-flight shipment is mid-export. Pick a window where the `MarkReady` auto-export path is quiet.
- **Verify:**
  1. Restart the app.
  2. Confirm the startup config validation does not throw.
  3. Trigger one export against a low-value test shipment (`POST /api/netsis/export-shipment/{id}` or via the UI).
  4. Confirm `NetsisTransferredAt` is set and no exception in logs.

---

## 7. Google Maps API key (highest blast radius — billing)

- **What it does:** authenticates calls from the server (Geocoding + Distance Matrix via `RouteOptimizationService` and `SystemSettingsController.Geocode`) **and** from the browser (Maps JS for `RouteOptimizationView.vue` / `WarehouseMapView.vue` if any client-side Maps usage exists).
- **Where to rotate:**
  1. **Google Cloud Console → APIs & Services → Credentials.**
  2. Create a *new* API key with **HTTP referrer restriction** (production domain) + **API restrictions** (only Maps JavaScript API, Geocoding API, Distance Matrix API).
  3. Delete the old key after the new one is deployed and verified.
  4. While you're there, set a **daily quota** + a **billing budget alert** at e.g. $50/day so quota theft has a cap.
- **What breaks temporarily:** route optimization, address validation, and any map tile loads on the frontend will fail with `REQUEST_DENIED` until the new key is in place at both server (env var) and frontend (build-time env var or runtime config).
- **Env vars (server-side):**
  - `GoogleMaps__ApiKey` (note: the prod `appsettings.json` placeholder is `SET_BY_ENV_GOOGLE_MAPS_API_KEY` — the `.NET` configuration system reads `GoogleMaps:ApiKey` from this env var via the standard `__` → `:` mapping. If your existing deploy already uses `GoogleMaps__ApiKey`, keep that; if you need to point at the explicit `GOOGLE_MAPS_API_KEY` placeholder name, update `Program.cs` config binding accordingly **out of scope of this rotation**.)
- **Frontend follow-up:** if the SPA embeds the key (`client/.env.production` or similar), update + redeploy the SPA build.
- **Verify:**
  1. `curl -s "https://maps.googleapis.com/maps/api/geocode/json?address=Kocaeli&key=$NEW_KEY" | jq .status` — should return `"OK"`.
  2. With the *old* key, the same call should return `REQUEST_DENIED` (proves the old key is disabled).
  3. In the app, run a route optimization request and confirm a result returns.
  4. Open the GCP billing dashboard the next day and confirm spend on the new key only.

---

## After all rotations

- Run the audit query again: `git log -p --all -- '*/appsettings*.json' | grep -E '(KullaniciAdi|Sifre|Password|ApiKey|PrivateKey)' | head` should show only historical placeholders, no new live values being introduced.
- Decide whether to history-rewrite (`git filter-repo` over the relevant paths) or accept the disclosure of the old credentials now that every one is rotated. With rotation complete, history rewrite is a hygiene step, not a security one.

## Checklist

- [ ] 1. VAPID key pair rotated + frontend `VITE_VAPID_PUBLIC_KEY` updated + push verified
- [ ] 2. YurtiKargo test creds rotated + WSDL fetch returns 200
- [ ] 3. JWT signing key rotated + all sessions force-expired + new login works
- [ ] 4. Seed admin password rotated + break-glass admin verified before + after
- [ ] 5. ISS-IP credentials (both layers) rotated + import batch succeeds
- [ ] 6. Netsis app + DB credentials rotated + one test export succeeds
- [ ] 7. Google Maps API key rotated + restrictions applied + quota cap set + old key deleted
- [ ] 8. `.debug_bin/` and `_build_check/` removed from index, deploy verified
- [ ] 9. Decision recorded on history-rewrite vs. accept disclosure
