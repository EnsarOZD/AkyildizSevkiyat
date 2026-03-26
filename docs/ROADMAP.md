# Akyildiz Sevkiyat — Proje Yol Haritası ve Yapılacaklar

> Son Güncelleme: 2026-03-22
> Durum: **Faz 0-4 TAMAMLANDI. WMS Faz W1+W2 TAMAMLANDI. Şoför Rotası Planlaması DEVAM EDİYOR.**

---

## İçindekiler

1. [Mevcut Durum — Tamamlananlar](#1-mevcut-durum--tamamlananlar)
2. [Faz 0 — Mevcut Eksikler ve Düzeltmeler](#2-faz-0--mevcut-eksikler-ve-düzeltmeler) ✅
3. [Faz 1 — Stok Yönetimi Altyapısı](#3-faz-1--stok-yönetimi-altyapısı) ✅
4. [Faz 2 — Sevkiyat Tamamlama & Netsis Entegrasyonu](#4-faz-2--sevkiyat-tamamlama--netsis-entegrasyonu) ✅
5. [Faz 3 — Şoför / Saha Modülü](#5-faz-3--şoför--saha-modülü) ✅
6. [Faz 4 — Gelişmiş Raporlama & Dashboard](#6-faz-4--gelişmiş-raporlama--dashboard) ✅
7. [Faz 5 — WMS (Depo Yönetim Sistemi)](#7-faz-5--wms-depo-yönetim-sistemi) ← **DEVAM EDİYOR**
8. [Faz 6 — Şoför Rota Planlaması](#8-faz-6--şöför-rota-planlaması) ← **DEVAM EDİYOR**
9. [Faz 7 — Kargo Entegrasyonları](#9-faz-7--kargo-entegrasyonları)
10. [Barkod Entegrasyonu — Ne Zaman?](#10-barkod-entegrasyonu--ne-zaman)
11. [Alternatif Mimari Önerileri](#11-alternatif-mimari-önerileri)
12. [Kritik Teknik Borçlar](#12-kritik-teknik-borçlar)

---

## 1. Mevcut Durum — Tamamlananlar

### Backend Altyapı
- [x] Clean Architecture (Domain / Application / Infrastructure / WebApi)
- [x] CQRS via MediatR + FluentValidation pipeline
- [x] JWT kimlik doğrulama (rol tabanlı: Admin, Accounting, Warehouse, Dispatcher, Manager)
- [x] GlobalExceptionMiddleware — tutarlı hata formatı
- [x] EF Core 10 + SQL Server migrations
- [x] SmtpEmailService (`IEmailService` interface + `SmtpEmailService` implementation)

### Sipariş & Entegrasyon
- [x] ISS-IP'den sipariş import (`ISSIpClient`)
- [x] Stok master kataloğu (StockMaster — OnHandQty, ReservedQty, Brand, MinStockQty, WarehouseLocation)
- [x] ISS-IP stok kodu eşleştirmeleri (StockMapping)
- [x] Tedarikçi yönetimi (Supplier)
- [x] Stok hareket kaydı (StockTransaction — GoodsIn, ShipmentOut, ManualAdjust, Reserve, ReleaseReserve, VehicleReturn)

### Sevkiyat Workflow
- [x] Sevkiyat oluşturma (tekil + toplu)
- [x] Durum makinesi: `Created → AssignedToWarehouse → Picking → ReadyForDispatch → AssignedToVehicle → Delivered → ReturnedToWarehouse`
- [x] Sevkiyat geçmişi (ShipmentHistory)
- [x] Araç & sürücü master verisi (Driver / Vehicle)
- [x] Teslimat kanıtı (DeliveredAt, DeliveryNote, DeliveryRecipient)
- [x] Araç iadesi — bilinen (RecordVehicleReturn, ShipmentLine.ReturnedQty/ReturnReason)
- [x] Araç iadesi — belirsiz (FloatingReturn entity, Create + Resolve workflow)
- [x] Stok rezervasyonu/çıkışı (AssignToWarehouse → Reserve, AssignVehicle → ShipmentOut, RevertToDraft → ReleaseReserve)

### Depo Yönetimi
- [x] Bölge (Zone) yönetimi
- [x] Proje-bölge eşleştirme
- [x] Bölge hazırlık süreci (ZonePreparation)
- [x] Micro toplama (proje bazlı)
- [x] Macro toplama (bölge bazlı konsolidasyon)

### Satın Alma
- [x] Satın alma siparişi (PurchaseOrder) — Draft → Approved → Closed
- [x] Mal girişi (GoodsReceipt) — Draft → Posted (stoka GoodsIn transaction kaydedilir)
- [x] Temel raporlama (Bölge malzeme raporu, Sevkiyat özeti, Açık PO, Bekleyen mal girişleri)

### Frontend
- [x] Sevkiyat detay — irsaliye, teslimat kanıtı, araç iadesi modali
- [x] Belirsiz İadeler yönetim sayfası (`/floating-returns`)
- [x] Stok yönetimi (Brand, MinStockQty, WarehouseLocation, import sonuç özeti)
- [x] Kullanıcı yönetimi UI (Admin)
- [x] Raporlar dashboard (6 sekme: Sevkiyat Özeti, Teslimat Performansı, Stok Durumu, İade Analizi, Açık PO, Bekleyen Mal Girişi)
- [x] Excel export (tüm 6 rapor sekmesinde SheetJS/xlsx, client-side)
- [x] Navigasyon yeniden yapılandırması — 6 anlamsal grup, Heroicons SVG ikonlar
- [x] Dashboard KPI kartları (aktif/bugün/gecikmiş/kritik stok/bekleyen mal girişi uyarıları)
- [x] document.title yönetimi (router.afterEach)
- [x] Tüm `alert()` çağrıları → `notificationStore.add()` toast'a dönüştürüldü
- [x] Global arama modalı (Ctrl+K) — sevkiyat, stok, proje, debounce + klavye navigasyonu
- [x] PWA manifest + service worker (vite-plugin-pwa, network-first API cache)
- [x] Klavye kısayolları — `N` yeni oluştur (PO/Stok/MalGirişi), `←/→` sayfalama, `Esc` modal kapat
- [x] Dispatcher rolü → `/driver` otomatik yönlendirme
- [x] Accessibility — `:focus-visible` ring, `aria-label` (tüm ikon butonlar), skip-link, `inputmode`
- [x] Sayfa geçiş animasyonları — `<Transition name="page" mode="out-in">` DefaultLayout'ta

---

## 2. Faz 0 — Mevcut Eksikler ve Düzeltmeler

> ✅ **TAMAMLANDI**

| Madde | Açıklama | Durum |
|-------|----------|-------|
| F0-01 | Manager rolü eksikti | ✅ |
| F0-02 | Stok seviyesi takibi yok (OnHandQty/ReservedQty) | ✅ |
| F0-03 | GoodsReceipt kısmi kabul işlevi (AcceptedQty) | ✅ |
| F0-04 | İrsaliye numarası alanı yok | ✅ |
| F0-05 | Teslimat kanıtı alanları yok | ✅ |
| F0-06 | StockMaster.Brand [NotMapped] sorunus | ✅ |
| F0-07 | Excel import validasyonu zayıf | ✅ |
| F0-08 | Kullanıcı yönetimi UI yok | ✅ |
| F0-09 | ShipmentLine → StockMaster FK yok | ✅ |
| F0-10 | Raporlama eksikleri | ✅ |
| F0-11 | E-posta servisi altyapısı yok | ✅ |

---

## 3. Faz 1 — Stok Yönetimi Altyapısı

> Durum: **Ana gövde tamamlandı.** Kalan minor maddeler aşağıda belirtildi.

### ✅ F1-01 — Stok Hareket Kaydı (StockTransaction) — TAMAMLANDI

`StockTransaction` entity, `StockTransactionType` enum (`GoodsIn, ShipmentOut, ManualAdjust, Reserve, ReleaseReserve, VehicleReturn`).
Tüm tetikleyiciler bağlandı:
- `PostGoodsReceipt` → `GoodsIn` ✅
- `AssignToWarehouse` → `Reserve` ✅
- `AssignVehicle` → `ShipmentOut` + `ReleaseReserve` ✅
- `RevertToDraft` → `ReleaseReserve` ✅
- `RecordVehicleReturn` → `VehicleReturn` ✅
- `ResolveFloatingReturn (AddToStock)` → `VehicleReturn` ✅

---

### ✅ F1-02 — Stok Seviyesi Hesaplama — TAMAMLANDI

`StockMaster.OnHandQty` (denormalized) + `ReservedQty` her hareket sonrası güncelleniyor.
`AvailableQty = OnHandQty - ReservedQty` (computed property, DB'de tutulmuyor).

---

### ✅ F1-03 — Stok Rezervasyonu — TAMAMLANDI

**Akış (uygulandı):**
1. `AssignedToWarehouse` → `ReservedQty += OrderedQty`
2. `AssignedToVehicle` → `OnHandQty -= DeliveredQty`, `ReservedQty -= OrderedQty` + `StockTransaction(ShipmentOut)`
3. `Delivered` → Stok değişimi yok (sadece teslimat kanıtı)
4. `Cancelled / RevertToDraft` → `ReservedQty -= OrderedQty` + `StockTransaction(ReleaseReserve)`
5. `RecordVehicleReturn` → `OnHandQty += ReturnedQty` + `StockTransaction(VehicleReturn)`

**Durum Makinesi (güncel):**
```
Created → AssignedToWarehouse → Picking → ReadyForDispatch → AssignedToVehicle → Delivered
                                                                    ↓                 ↓
                                                           ReturnedToWarehouse  ReturnedToWarehouse
                                                            (tam iade)           (kısmi iade sonrası)
```

---

### ✅ F1-04 — Kritik Stok Eşiği — TAMAMLANDI

`StockMaster.MinStockQty` alanı eklendi. Frontend stok listesinde görüntüleniyor.
Dashboard widget (kritik stok uyarısı) → **F4-01'e bırakıldı**.

---

### ✅ F1-05 — Stok Depo Adresi — TAMAMLANDI

`StockMaster.WarehouseLocation` alanı eklendi, frontend stok tablosunda görüntüleniyor.

---

### ✅ F1-06 — Stok Sayım Modülü (Basit) — TAMAMLANDI

**Uygulananlar:**
- `StockCount` entity (Id, CountDate, Status: Draft/Completed, Note, CreatedAt, CompletedAt)
- `StockCountLine` entity (Id, StockCountId, StockMasterId, ExpectedQty, ActualQty, DifferenceQty computed, Note)
- `CreateStockCountCommand` — aktif stokların OnHandQty snapshot'ıyla sayım oluşturur
- `UpdateStockCountLinesCommand` — toplu ActualQty girişi
- `CompleteStockCountCommand` — fark olan satırlar için `ManualAdjust` StockTransaction oluşturur, OnHandQty günceller
- `GetStockCountsQuery` + `GetStockCountDetailQuery`
- `StockCountsController` — GET, GET/{id}, POST, PUT/{id}/lines, POST/{id}/complete
- EF Migration: `Faz1_StockCount`
- Frontend: `/stock-counts` sayfası — liste + detay, satır satır sayım girişi, ilerleme çubuğu, filtreler (sayılmamış/fark var/fark yok), tamamlama onay modali

---

### ✅ F1-07 — Araç Dönüşü: Bilinen İade (RecordVehicleReturn) — TAMAMLANDI

**Uygulananlar:**
- `ShipmentLine.ReturnedQty (decimal?)` + `ReturnReason (ReturnReason?)`
- `Shipment.ReturnedAt` + `ReturnNote`
- `ReturnReason` enum: CustomerRejected, Damaged, ExcessLoading, WrongItem, ProjectNotFound, Other
- `ShipmentStatus.ReturnedToWarehouse = 7`
- `RecordVehicleReturnCommand` + Handler + Validator
- `POST /api/shipments/{id}/record-vehicle-return` endpoint
- EF Migration: `Faz1_VehicleReturn_FloatingReturn`
- Frontend: ShipmentDetailView'a "Araç İadesi Kaydet" butonu + per-line modal

---

### ✅ F1-08 — Araç Dönüşü: Belirsiz İade (FloatingReturn) — TAMAMLANDI

**Uygulananlar:**
- `FloatingReturn` entity (Id, ReturnDate, StockMasterId?, StockCodeFree?, StockNameFree?, Qty, ReturnReason, Note, Status, LinkedShipmentId?, CreatedByUserId, CreatedAt, ResolvedAt?, ResolvedByUserId?)
- `FloatingReturnStatus` enum: Pending, MatchedToShipment, AddedToStock, WrittenOff
- `CreateFloatingReturnCommand` + Handler + Validator
- `ResolveFloatingReturnCommand` (MatchToShipment / AddToStock / WriteOff) + Handler + Validator
- `GetFloatingReturnsQuery` — status/tarih filtreleme
- `FloatingReturnsController`: `GET /api/floatingreturns`, `POST /api/floatingreturns`, `POST /api/floatingreturns/{id}/resolve`
- `IApplicationDbContext.FloatingReturns` DbSet eklendi
- `SevkiyatDbContext` entity configuration eklendi
- Frontend: `/floating-returns` sayfası — özet kartları, filtrelenebilir tablo, oluşturma modali, çözümleme modali

---

### ✅ F1-09 — İade & Mutabakat Raporlama (Temel) — TAMAMLANDI

**Uygulananlar:**
- Belirsiz İadeler sayfası (`/floating-returns`) özet kartları: Beklemede / Eşleştirildi / Stoğa Eklendi / Hariç Tutuldu
- Bilinen iadeler ShipmentLine bazında ReturnedQty + ReturnReason kaydedildi
- Tüm iadeler `StockTransaction(VehicleReturn)` ile izlenebilir

**Kalan (F5'e bırakıldı):**
- `GET /api/reports/returns` — tarih/proje/stok filtrelenebilir iade analiz raporu
- `GET /api/reports/stock-reconciliation` — beklenen vs sayım fark raporu
- Frontend rapor sekmeleri (İade Analizi, Stok Mutabakat)

---

## 4. Faz 2 — Sevkiyat Tamamlama & Netsis Entegrasyonu

> Ön koşul: Faz 0 + Faz 1 tamamlanmış olmalı. ✅

### ✳️ F2-01 — Netsis İrsaliye Aktarımı — ALTYAPI HAZIR, API BEKLENİYOR

**Karar:** REST API (NetOpenX) yolu seçildi. Toplantı yapıldı, Netsis tarafı credential + endpoint bilgilerini iletecek.

**Tamamlananlar (altyapı):**
- `INetsisClient` interface (Application/Interfaces)
- `NetsisOptions` — tüm credential alanları `SET_BY_ENV_` ile ayarlandı
- `NetsisTokenCache` (singleton) — token in-memory yönetimi
- `NetsisClient` — login, `CreateSiparisAsync`, `GetStockBalancesAsync`
- `ExportShipmentToNetsisCommand` — sevkiyatı Netsis'e Müşteri Siparişi olarak aktarır
- `SyncNetsisStockBalanceCommand` — Netsis'ten stok bakiyelerini çekip StockMaster günceller
- `NetsisController` — `POST /api/netsis/shipments/{id}/export`, `POST /api/netsis/stock/sync`
- `StockMaster.NetsisStockCode` + `Project.NetsisCariKodu` alanları eklendi
- Migration: `Faz2_NetsisIntegrationScaffold`
- `appsettings.json` Netsis section (path'ler `PENDING_NETSIS_API_DOCS` placeholder)

**Netsis bilgileri gelince yapılacak:**
- `appsettings.json` path'lerini güncelle: `LoginPath`, `SiparisPath`, `StokBakiyePath`
- DTO field name'lerini API'ye göre ayarla (`TODO: NETSIS_API` yorum satırları var)
- `Project.NetsisCariKodu` ve `StockMaster.NetsisStockCode` alanlarını veriye göre doldur

---

### ✅ F2-02 — İrsaliye Numarası Geri Okuma — TAMAMLANDI

**Uygulananlar:**
- `UpdateIrsaliyeNo` command + endpoint + frontend modali
- `ShipmentSettings.RequireIrsaliyeNoOnDispatch` (config flag, default: false)
- `AssignVehicleCommandHandler`'da config tabanlı zorunluluk kontrolü
- Frontend araç atama modalında irsaliye no eksikse uyarı banner'ı
- Netsis entegrasyonu aktif olduğunda `appsettings.json`'da `true` yapılır

---

### ✅ F2-03 — Yükleme Sonrası E-posta Bildirimi — TAMAMLANDI

**Uygulananlar:**
- `ShipmentAssignedToVehicleEmailHandler` — `INotificationHandler<ShipmentStatusChangedEvent>`
- `AssignedToVehicle` durumuna geçişte tetiklenir
- `IssOrder.YoneticiMailAdresleri` (virgül/noktalı virgül ayrılmış) alıcı olarak kullanılır
- Mail içeriği: Proje adı, Talep No, teslim tarihi, sürücü adı, araç plakası, kalem sayısı
- Mail hatası sevkiyat akışını bloke etmez (try/catch + loglama)

---

### ✅ F2-04 — Teslimat Onay Akışı — TAMAMLANDI

**Uygulananlar:**
- `Shipment.DeliveryPhotoBase64` alanı eklendi (nvarchar(max))
- `MarkShipmentDeliveredCommand` fotoğraf parametresi destekliyor
- Frontend teslimat modalı: kamera/dosya seçici (`capture="environment"` — mobil uyumlu)
- Canvas ile client-side sıkıştırma (max 1000px, JPEG %75) — büyük fotoğraflarda ağ yükü minimize
- Detay sayfasında fotoğraf thumbnail + lightbox tam ekran görüntüleme
- Migration: `Faz2_DeliveryPhoto`

**Kalan (F5'e bırakıldı):**
- Teslim edilen miktar vs sipariş fark raporu

---

## 5. Faz 3 — Şoför / Saha Modülü

> ✅ **TAMAMLANDI**

### F3-01 — Şoför Paneli ✅
- `Dispatcher` (+ Admin/Manager) rolüne özel `/driver` rotası — `DriverLayout.vue` (sidebar yok, mobil-first)
- `DriverShipmentListView.vue` — aktif sevkiyatlar + bugün teslim edilenler (kart bazlı)
- `DriverDeliveryView.vue` — teslimat formu (teslim alan kişi, not, fotoğraf, teslim et butonu)
- Backend: `GET /api/driver/shipments` — `DriverController` + `GetDriverShipmentsQuery`
- Navigation: Operasyon grubuna "Şoför Paneli" linki (Dispatcher/Admin/Manager)

### F3-02 — Navigasyon Entegrasyonu ✅
- `Project.Address` → Google Maps deep link (API key gerekmez — `maps/search/?api=1&query=...`)
- Hem `ShipmentCard.vue` hem `DriverDeliveryView.vue`'de adres linki aktif

### F3-03 — Teslimat Fotoğrafı & Notu (Mobil) ✅ (Faz 2'de tamamlandı)
- Tarayıcı kamera API (`<input type="file" accept="image/*" capture="environment">`)
- Canvas sıkıştırma (max 1000px, JPEG %75) + Base64 → API upload

---

## 6. Faz 4 — Gelişmiş Raporlama & Dashboard

> ✅ **TAMAMLANDI**

### ✅ F4-01 — Ana Dashboard KPI Widget'ları — TAMAMLANDI
- [x] Bugünkü sevkiyatlar, geciken sevkiyatlar, haftalık teslimat
- [x] Durum dağılımı (Draft/Warehouse/Picking/Ready/OnRoute)
- [x] Bekleyen belirsiz iade sayısı + alert linki
- [x] Kritik stok uyarısı (AvailableQty < MinStockQty) — `DashboardStatsDto.CriticalStockCount`
- [x] Bekleyen mal girişleri widget'ı — `DashboardStatsDto.PendingGoodsReceiptsCount`

### ✅ F4-02 — Sevkiyat Performans Raporu — TAMAMLANDI
- [x] Zamanında/geciken teslimat analizi (DeliveredAt vs DeliveryDate karşılaştırması)
- [x] Bölge bazlı teslimat istatistikleri (`ByZone` gruplaması)
- [x] Gecikme gün sayısı, gecikme oranı
- Backend: `GetShipmentPerformanceQuery` + `GET /api/reports/shipment-performance`
- Frontend: Teslimat Performansı sekmesi — KPI kartları, bölge tablosu, detay filtresi

### ✅ F4-03 — Stok Durumu Raporu — TAMAMLANDI
- [x] OnHandQty / ReservedQty / AvailableQty görünümü
- [x] Kritik (< MinStockQty) ve tükenmiş stoklar renk kodlu
- [x] `criticalOnly` filtresi
- Backend: `GetStockStatusReportQuery` + `GET /api/reports/stock-status`
- Frontend: Stok Durumu sekmesi — 3 KPI kart, renk kodlu tablo

### ✅ F4-04 — İade Analizi Raporu — TAMAMLANDI
- [x] Tarih / bölge filtrelenebilir iade raporu
- [x] Neden bazlı özet (CustomerRejected, Damaged, vb. → Türkçe etiketler)
- [x] Kalem bazlı iade detayları
- Backend: `GetReturnsReportQuery` + `GET /api/reports/returns`
- Frontend: İade Analizi sekmesi — KPI kartlar, neden özeti, detay tablosu

### ✅ F4-05 — Export — TAMAMLANDI
- [x] Tüm 6 rapor sekmesinde Excel export butonu (SheetJS/xlsx, client-side)
- [x] Dosya adına tarih aralığı eklendi (örn. `sevkiyat-ozeti-2026-02-20_2026-03-22.xlsx`)
- Kritik PDF'ler (irsaliye, mal girişi belgesi) — Netsis halleder

---

## 7. Faz 5 — WMS (Depo Yönetim Sistemi)

> Detaylı plan: `docs/WMS_ROADMAP.md`

### W1 — Depo Adres / Lokasyon Yapısı ✅ TAMAMLANDI
- `WarehouseLocation` entity: Zone-Row-Rack-Level-Slot hiyerarşisi (örn. "A-02-03-2-01")
- 6 lokasyon tipi: Rack, FloorStack, Receiving, Shipping, Quarantine, Staging
- Kapasite alanları: MaxWeightKg, MaxPallets
- Toplu oluşturma sihirbazı (aralık bazlı)
- Backend: CreateWarehouseLocation, UpdateWarehouseLocation, BulkCreateWarehouseLocations, GetWarehouseLocations
- Frontend: `/warehouse/locations` — filtreli tablo, oluşturma ve toplu oluşturma modalları

### W2 — Stok-Lokasyon Bağlantısı ✅ TAMAMLANDI
- `StockLocation` entity: bir stok birden fazla lokasyonda takip edilebilir
- `LocationTransfer` entity: lokasyonlar arası transfer kaydı (kim yaptı, ne zaman)
- Backend: AssignStockToLocation, TransferStock, GetStockLocations, GetTransferHistory
- Frontend: `/warehouse/stock-locations` — stok haritası + transfer geçmişi sekmeli görünüm

### W3 — Palet Yönetimi (PLANLANDI)
- `Pallet` + `PalletLine` entity (PAL-2026-XXXX kodu)
- Durum makinesi: Empty → Loading → Sealed → InTransit → Delivered
- QR kod yazdırma (tarayıcı tabanlı)
- Sevkiyata palet atama

### W4+ — Putaway, Rota Optimizasyonu, Döngüsel Sayım (PLANLANDI)
- Detaylar: `docs/WMS_ROADMAP.md`

---

## 8. Faz 6 — Şoför Rota Planlaması

> ← **ŞU AN DEVAM EDİYOR**

### Mevcut Sorunlar
- Şoför ekranı bireysel sevkiyat bazlı — bir teslim noktasına birden fazla sipariş/irsaliye olabiliyor
- Rota sıralaması yok — şoför hangi noktaya önce gideceğini bilmiyor
- `GetDriverShipmentsQuery` araç/şoföre göre filtrelemiyor

### F6-01 — Teslimat Noktası Modeli ← **YAPILACAK**
**Konsept:** Şoför bireysel sevkiyatları değil, **teslimat noktalarını** (proje adresi) görür.
Bir noktada birden fazla irsaliye/sipariş olabilir.

**Backend değişiklikleri:**
- `Project.DeliveryOrder int?` alanı — bölge içinde ziyaret sırası (yönetici belirler)
- `GetDriverRouteQuery` — sevkiyatları proje bazında gruplandırır, sıralı döner
- `DeliveryStopDto` — teslimat noktası DTO: StopNumber, ProjectName, Address, Shipments[]

**Frontend değişiklikleri:**
- `DriverRouteView.vue` — rota ekranı: sıralı teslimat noktaları listesi
- `DriverStopView.vue` — bir noktanın tüm irsaliyeleri, toplam kalem sayısı
- Rota haritası linki: Google Maps'te tüm duraklar sıralı açılır
- "Tümünü Teslim Et" butonu — bir noktadaki tüm irsaliyeleri tek seferde teslim edildi işaretle

### F6-02 — Yönetici Rota Planlama Ekranı ← **YAPILACAK**
- Proje kartı yönetimi sayfasına `DeliveryOrder` alanı (bölge içi sıra)
- Sürükle-bırak ile sıralama (isteğe bağlı)
- Bir araca atanan sevkiyatların bölge + proje sırasına göre önizleme

### F6-03 — Otomatik Rota Önerisi ← **PLANLANDI**
**Seçenek A (basit):** Zone.Order → Project.DeliveryOrder sıralaması — yönetici önceden belirler
**Seçenek B (akıllı):** Google Maps Distance Matrix API ile mesafe bazlı optimizasyon
- Adresler koordinata dönüştürülür (Geocoding API)
- En kısa toplam mesafe hesaplanır (Nearest Neighbor algoritması)
- Yöneticiye öneri sunulur, onay sonrası kayıt edilir

---

## 9. Faz 7 — Kargo Entegrasyonları

> ⏸ **En son yapılacak.** Kargo firmasıyla görüşme gerekiyor.

### F5-01 — Kargo Gönderim Modülü
Yeni entity: `CargoShipment` (Id, ShipmentId, CargoProvider, TrackingNo, SentAt, Status)

### F5-02 — Kargo Firması API Entegrasyonu
`ICargoProviderClient` interface — Yurtiçi / MNG / PTT implementasyonları (Netsis gibi hazırlık → API bilgisi gelince doldurmak)

### F5-03 — Gönderi Durum Sorgulama
Background Service — periyodik durum sorgusu + bildirim

---

## 10. Barkod Entegrasyonu — Ne Zaman?

### Mevcut Durum
Stok hareketi altyapısı (StockTransaction) kuruldu. Barkod eklemek artık kolay.

### Ne Zaman Eklenmeli?
**Faz 1 tamamlandığı için artık uygun zaman.** Küçük bir iterasyon (1-2 gün):
1. `StockMaster`'a `Barcode (string, nullable)` alanı + migration
2. Import/export'a barkod kolonu
3. Frontend: `jsQR` veya `@zxing/library` ile "Barkod Oku" butonu
4. Mal girişi, sayım, toplama ekranlarına entegrasyon

---

## 11. Alternatif Mimari Önerileri

### 9.1 — Stok Yönetimi: Event Sourcing Alternatifi
Mevcut: `StockTransaction` + denormalized `OnHandQty` — şu an için yeterli ve uygulandı.
Event Sourcing günlük 1000+ hareket varsa düşünülebilir.

### 9.2 — E-posta: SendGrid / AWS SES vs. SMTP
Mevcut: SMTP (`SmtpEmailService`). Düşük hacimde yeterli.
Yüksek hacimde veya teslim garantisi gerekiyorsa SendGrid/SES migrate edilebilir.

### 9.3 — Şoför Uygulaması: PWA vs. Native
**Capacitor ile Hybrid önerilir:** Aynı Vue kodu → Android/iOS uygulama, native kamera/GPS.

### 9.4 — Netsis Entegrasyonu
Dosya tabanlı (XML/CSV) ile başlanması önerilir → API hazır olunca geçilir.

### 9.5 — Çoklu Depo Desteği
Şu an tek depo varsayımı. Gerekirse `Warehouse` entity + `Zone → Warehouse` gruplaması.

---

## 10. Kritik Teknik Borçlar

| # | Sorun | Etki | Durum |
|---|-------|------|-------|
| T01 | `UserRole` enum'da `Manager` yoktu | Rol ataması yapılamıyordu | ✅ Çözüldü |
| T02 | Stok seviyesi hesaplanmıyordu | "Stokta var mı?" sorusu cevaplanamıyordu | ✅ Çözüldü |
| T03 | `PostGoodsReceipt` stok güncellemiyordu | Mal girişi stoka yansımıyordu | ✅ Çözüldü |
| T04 | `StockMaster.Brand` `[NotMapped]` idi | Veri kaybı | ✅ Çözüldü |
| T05 | `ShipmentLine` → `StockMaster` FK yoktu | Stok düşme yapılamıyordu | ✅ Çözüldü |
| T06 | İrsaliye no alanı yoktu | Netsis entegrasyonu bloklanıyordu | ✅ Çözüldü |
| T07 | Kullanıcı yönetimi UI yoktu | Elle seed gerekiyordu | ✅ Çözüldü |
| T08 | E-posta servisi yoktu | Bildirimler yapılamıyordu | ✅ Çözüldü |
| T09 | `AcceptedQty` kullanılmıyordu | Kısmi kabul işlemiyordu | ✅ Çözüldü |
| T10 | `Guid` + `int` karışık PK stratejisi | Şema tutarsızlığı | ⬜ Düşük öncelik |
| T11 | Stok sayım modülü yok | Manuel düzeltme yapılamıyordu | ✅ Çözüldü (F1-06) |
| T12 | İade analiz raporu yok | Proje/stok bazlı iade sorgulanamıyor | ✅ Çözüldü (F4-04) |

---

## Faz Sırası Özeti

```
✅  Faz 0 — Mevcut Eksikler (T01–T09 teknik borçlar) — TAMAMLANDI
✅  Faz 1 — Stok Yönetimi Altyapısı — TAMAMLANDI
✅  UI/UX Sprint 1-7 — Navigasyon, Dashboard, Skeleton, StatusBadge, Breadcrumb, URL Filters, Detay layout, Şoför Paneli, PWA, Excel Export, Klavye Kısayolları, Global Arama, Accessibility, Animasyonlar — TAMAMLANDI
✅  Faz 2 — Netsis + Teslimat + Mail — TAMAMLANDI
              ✳️ F2-01 Netsis export — altyapı hazır, API bilgisi bekleniyor
              ✅ F2-02 İrsaliye geri okuma — TAMAMLANDI
              ✅ F2-03 Yükleme e-postası — TAMAMLANDI
              ✅ F2-04 Teslimat fotoğrafı — TAMAMLANDI
         ↓
✅  Faz 3 — Şoför / Saha Modülü — TAMAMLANDI
         ↓
✅  Faz 4 — Gelişmiş Raporlama & Dashboard — TAMAMLANDI
         ↓
🔜  Faz 5 — Kargo Entegrasyonları  ← SIRADA
```

**Barkod:** Faz 1 tamamlandığına göre artık eklenebilir — küçük iterasyon.

---

*Bu döküman proje ilerledikçe güncellenir. Her faz tamamlandığında ilgili checkbox'lar işaretlenir.*
