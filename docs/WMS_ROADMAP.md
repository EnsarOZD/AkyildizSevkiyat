# Depo Yönetim Sistemi (WMS) — Yol Haritası

> Son Güncelleme: 2026-05-17
> Durum: **Phase 1 TAMAMLANDI · Phase 1b (Adres Şeması & UI) TAMAMLANDI · Phase 2 PLANLAMADA**

---

## Genel Bakış

WMS üç katmanlı bir yapıda inşa edilmektedir:

| Katman | Açıklama | Durum |
|--------|----------|-------|
| **Temel Altyapı** | Lokasyon yapısı, StockLocation, LocationTransfer | ✅ Tamamlandı (önceki sprint) |
| **Barkod & Toplama Gözü (Phase 1)** | Barkod entity'leri, feature flag'ler, QR, PickingFace | ✅ Tamamlandı (2026-05-15) |
| **Adres Şeması & UI (Phase 1b)** | ContainerType, iç adres, harita, silme, toplu etiket | ✅ Tamamlandı (2026-05-17) |
| **İş Akışları (Phase 2)** | Putaway, barkodlu toplama, lokasyon bazlı stok düşümü | 🟡 Planlamada |

---

## Tamamlananlar

### Temel Altyapı (Önceki Sprint)

- ✅ `WarehouseLocation` entity — kod formatı `{KoridorNo}{Taraf}-{ModulNo:D3}-{Kat:D2}` (örn. `1K-001-03`)
- ✅ `StockLocation` — StockMasterId + WarehouseLocationId başına lokasyon bazlı miktar takibi
- ✅ `LocationTransfer` — lokasyonlar arası hareket kaydı
- ✅ `StockTransaction` — genel stok hareket logu (GoodsIn, ShipmentOut, ManualAdjust, vb.)
- ✅ `StockCount` — fiziksel sayım + düzeltme fişi
- ✅ Frontend: Depo Adresleri yönetim sayfası (toplu/tekli oluşturma)
- ✅ Frontend: Stok Lokasyonları sayfası (lokasyon bazlı stok görüntüleme + transfer)

---

### Adres Şeması & UI İyileştirmeleri — Phase 1b (2026-05-17)

#### Yeni Adres Formatı

`WarehouseLocation.BuildCode()` genişletildi. Artık üç farklı fiziksel yapıyı destekler:

| Tip | Format | Örnek |
|-----|--------|-------|
| **Palet** (raf veya toplama gözü) | `{K}{T}-{M:3}-{kat:2}` | `2K-001-00` |
| **Koli** (kayar raf — A=zemin, B=orta, C=üst) | `{K}{T}-{M:3}-00-{harf}{pos:2}` | `2K-001-00-A01` |
| **Kutu** (kol harfi + göz numarası) | `{K}{T}-{M:3}-00-{harf}{pos:2}` | `2K-020-00-B03` |

Koli ve Kutu aynı kod formatını kullanır; farkı `ContainerType` alanı taşır.
`InnerPosition` tek başına (level=null) kullanılırsa salt sayısal iç adres üretir: `-01` (şimdilik kullanılmıyor, ileride genişletilebilir).

#### Yeni Enum

**`ContainerType`** (`Domain/Enums/ContainerType.cs`)
```
Pallet = 0   — Palet
Case   = 1   — Koli (kayar raf)
Box    = 2   — Kutu (iç adresleme)
```

#### Mevcut Entity Güncellemeleri

**`WarehouseLocation`** — 3 yeni alan
```
InnerLevel    string?       — Harf bazlı iç seviye (Koli: A/B/C kat, Kutu: kol)
InnerPosition int?          — Pozisyon numarası (01–06 gibi)
ContainerType ContainerType — Pallet / Case / Box
```

#### Migration

`WmsPhase2_ContainerTypeAndInnerAddress`

#### Backend Değişiklikleri

**Güncellenen endpoint'ler:**

| Method | URL | Değişiklik |
|--------|-----|-----------|
| `POST` | `/api/warehouse-locations/picking-face` | Artık Palet, Koli, Kutu üç tipi de destekliyor; Koli/Kutu için `InnerLevels`+`PositionsPerLevel`, Palet için `KatFrom`/`KatTo` |
| `PUT` | `/api/warehouse-locations/{id}` | `ContainerType`, `InnerLevel`, `InnerPosition` parametreleri eklendi |
| `POST` | `/api/warehouse-locations/bulk` | `ContainerType` parametresi eklendi |

**Yeni endpoint'ler:**

| Method | URL | Açıklama |
|--------|-----|----------|
| `GET` | `/api/warehouse-locations/map` | Modül bazında depo haritası (`MapModuleDto[]`) |
| `DELETE` | `/api/warehouse-locations/{id}` | Tekil hard delete |
| `DELETE` | `/api/warehouse-locations/bulk` | Toplu hard delete (`List<int>` body) |

**Yeni komutlar/query'ler:**
- `DeleteWarehouseLocationCommand` — tekil silme
- `BulkDeleteWarehouseLocationsCommand` — toplu silme
- `GetWarehouseMapQuery` + `MapModuleDto` — modül gruplandırma (KoridorNo, Taraf, ModulNo, DominantTypeId, HasActive, AllActive, TotalLocations)

#### `CreatePickingFaceCommand` — Tüm Tipler

```
Palet  → modül × kat aralığı, iç adres yok
         Örn: KatFrom=0, KatTo=0 → 2K-001-00

Koli   → modül × innerLevels × positionsPerLevel (Kutu ile aynı algoritma, ContainerType=Case)
         Örn: innerLevels=["A","B","C"], positionsPerLevel=6 → 2K-001-00-A01 … C06

Kutu   → modül × innerLevels × positionsPerLevel, ContainerType=Box
         Örn: innerLevels=["A","B"], positionsPerLevel=8 → 2K-020-00-A01 … B08
```

#### Frontend Değişiklikleri

**`WarehouseLocationsView.vue`** — kapsamlı yenileme:
- ✅ Raflar / Toplama Gözleri sekme ayrımı (LocationType=6 filtresi)
- ✅ Checkbox çoklu seçim + `allPageSelected` computed
- ✅ Hızlı Aktif/Pasif toggle (satır üzerinde, modal açmadan)
- ✅ Tekil silme + toplu silme (hard delete, confirm dialog ile)
- ✅ Toplu etiket basma — seçili lokasyonlar için QR'ları yükleyip yazdır
- ✅ "Toplu Oluştur" modali: ContainerType seçici (Palet/Koli)
- ✅ "Toplama Gözü Ekle" modali: üç tip desteği
  - Palet: kat sabit 00, modül aralığı
  - Koli: Kat Harfleri (A, B, C) + Göz Sayısı / Kat
  - Kutu: İç Kollar + Göz Sayısı / Kol
- ✅ Düzenleme formuna ContainerType + InnerLevel + InnerPosition eklendi
- ✅ Tüm form elemanlarında `dark:bg-gray-800` (dark mode dropdown tutarsızlığı giderildi)

**`WarehouseMapView.vue`** — YENİ sayfa:
- 2D depo haritası: koridor × modül ızgarası
- Renk kodlaması: Raf=mavi, Toplama Gözü=yeşil, inaktif=soluk
- Hücreye tıklayınca modül detay paneli
- `GET /api/warehouse-locations/map` endpoint'ini kullanır

**`router/index.ts`** — `/warehouse/map` → `WarehouseMapView` rotası eklendi

**`navigation.ts`** — "Depo Haritası" nav linki eklendi

**`warehouseLocationService.ts`** — yeni alanlar ve metodlar:
- `WarehouseLocation`: `innerLevel`, `innerPosition`, `containerTypeId`
- `CreatePickingFaceRequest`: `katFrom?`, `katTo?`, `innerLevels?`, `positionsPerLevel?`, `containerType`
- `BulkCreateRequest`: `containerType`
- `MapModuleDto` interface + `getMap()` metodu
- `delete(id)` ve `bulkDelete(ids[])` metodları
- `CONTAINER_TYPE_LABELS` sabiti

#### Kodun Bulunduğu Yerler (Yeni / Güncellenen)

```
Domain/
  Enums/ContainerType.cs                  — YENİ enum
  Entities/WarehouseLocation.cs           — InnerLevel, InnerPosition, ContainerType; BuildCode() güncellendi

Application/WarehouseLocations/
  Commands/CreatePickingFace/             — tüm 3 tip desteği
  Commands/DeleteWarehouseLocation/       — DeleteWarehouseLocationCommand (YENİ)
                                            BulkDeleteWarehouseLocationsCommand (YENİ)
  Commands/UpdateWarehouseLocation/       — ContainerType, InnerLevel, InnerPosition eklendi
  Commands/BulkCreateWarehouseLocations/  — ContainerType eklendi
  Queries/GetWarehouseMap/               — GetWarehouseMapQuery + MapModuleDto (YENİ)

WebApi/Controllers/WarehouseLocationsController.cs
  — DELETE /{id}, DELETE /bulk, GET /map endpoint'leri eklendi
  — UpdateWarehouseLocationRequest: ContainerType, InnerLevel, InnerPosition

Infrastructure/Migrations/
  WmsPhase2_ContainerTypeAndInnerAddress

client/src/
  services/warehouseLocationService.ts   — interface güncellemeleri + yeni metodlar
  views/WarehouseLocationsView.vue       — kapsamlı yenileme (bkz. yukarısı)
  views/WarehouseMapView.vue             — YENİ
  router/index.ts                        — /warehouse/map rotası
  navigation.ts                          — Depo Haritası linki
```

---

### Barkod & Toplama Gözü — Phase 1 (2026-05-15)

#### Yeni Entity'ler

**`StockBarcode`** — Bir ürüne ait ek barkodlar (farklı ambalaj/tedarikçi)
```
Id, StockMasterId, Barcode (EAN13/Code128/QR), Description, CreatedAt
```

**`PutawayTask`** — Mal kabul sonrası oluşturulan dağıtım görevi
```
Id, GoodsReceiptId, GoodsReceiptLineId, StockMasterId,
TotalQty, DistributedQty, RemainingQty (computed),
Status (Pending/PartiallyDistributed/Completed),
CreatedAt, CompletedAt
Lines: ICollection<PutawayLine>
```

**`PutawayLine`** — Bir PutawayTask içinde tek lokasyon satırı
```
Id, PutawayTaskId, WarehouseLocationId, Qty, CreatedAt, CreatedByUserId
```

#### Mevcut Entity Güncellemeleri

**`WarehouseLocation`** — 3 yeni alan + 1 yeni tip
```
Alan         string?  — Açıklama alanı (⚠️ Phase 1b'den itibaren PickingFace kod üretiminde kullanılmıyor; sadece bilgi amaçlı korundu)
QrCode       string?  — Rafa yapıştırılan QR değeri (katsız: "1K-001")
TotalFloors  int?     — Raf toplam kat sayısı (terminal kat seçim listesi için)
```
`LocationType` enum'una `PickingFace = 6` eklendi.

**`StockMaster`** — 2 yeni alan
```
Barcode              string?   — Birincil tedarikçi barkodu
DefaultPickingFaceId int? FK   — Varsayılan toplama gözü
```

**`SystemSettings`** — 3 WMS feature flag (hepsi varsayılan false)
```
WmsPutawayEnabled         bool  — Mal kabul → lokasyon dağıtım akışı
WmsLocationPickingEnabled bool  — Lokasyon bazlı picking + StockLocation düşümü
WmsBarcodePickingEnabled  bool  — Toplama sırasında barkod tarama zorunluluğu
```

#### Migration
`20260514224602_WmsPhase1_BarcodeAndPutaway` — Local'e uygulandı, production'a uygulanacak.

#### Backend Endpoints (Yeni)

| Method | URL | Açıklama |
|--------|-----|----------|
| GET | `/api/system-settings/wms` | WMS flag'lerini getir |
| PUT | `/api/system-settings/wms` | WMS flag'lerini kaydet |
| POST | `/api/warehouse-locations/picking-face` | Toplama gözü toplu oluştur |
| GET | `/api/warehouse-locations/{id}/qr` | Lokasyon QR görüntüsü üret |

#### Frontend (Yeni/Güncellenen)

- ✅ **Ayarlar → Depo Yönetimi** sekmesi — 3 WMS toggle switch
- ✅ **Depo Adresleri** sayfası tamamen güncellendi:
  - "Raflar / Toplama Gözleri" sekme seçimi (Phase 1b'de kapsamlı yenilendi)
  - Her lokasyon için QR yazdırma (browser print, 60×80mm etiket)
  - Raf düzenleme: QrCode ve TotalFloors alanları
- ✅ **Stok Yönetimi** → stok formu: Birincil Barkod alanı eklendi

#### Kodun Bulunduğu Yerler

```
Domain/Entities/
  PutawayTask.cs           — PutawayTask + PutawayLine + PutawayTaskStatus enum
  StockBarcode.cs          — StockBarcode entity
  WarehouseLocation.cs     — Alan, QrCode, TotalFloors + BuildPickingFaceCode() (⚠️ eski format — Phase 1b'de değiştirildi)
  StockMaster.cs           — Barcode, DefaultPickingFaceId
  SystemSettings.cs        — WmsPutawayEnabled, WmsLocationPickingEnabled, WmsBarcodePickingEnabled

Application/SystemSettings/
  Queries/GetWmsSettingsQuery.cs
  Commands/SaveWmsSettingsCommand.cs

Application/WarehouseLocations/
  Commands/CreatePickingFace/CreatePickingFaceCommand.cs
  Queries/GenerateLocationQr/GenerateLocationQrQuery.cs

Application/Stocks/
  Queries/GetStocks/GetStocksQuery.cs     — StockDto'ya Barcode eklendi
  Commands/UpdateStock/UpdateStockCommand.cs — Barcode param eklendi

Infrastructure/Persistence/SevkiyatDbContext.cs
  — StockBarcodes, PutawayTasks, PutawayLines DbSet'leri
  — WarehouseLocation, StockMaster OnModelCreating config'leri

client/src/
  services/systemSettingsService.ts  — WmsSettingsDto + getWmsSettings/saveWmsSettings
  services/warehouseLocationService.ts — alan,qrCode,totalFloors + createPickingFace/getQr
  services/stockService.ts           — barcode alanı eklendi
  views/WarehouseLocationsView.vue   — Raflar/PickingFace sekmeleri, QR modal
  views/SettingsView.vue             — WMS toggle'ları (Depo Yönetimi sekmesi)
  views/StockManagementView.vue      — Birincil Barkod input alanı
```

---

## Phase 2 — İş Akışları (Sıradaki)

### Ön Koşullar

Phase 2'ye başlamadan önce şunlar yapılmış olmalı:
1. ✅ Toplama gözleri tanımlandı (WarehouseLocations → PickingFace tipi)
2. ⬜ Stok lokasyonlarına ilk yükleme yapıldı (StockLocations ekranı üzerinden)
3. ⬜ Ürünlere barkod girildi (Stok yönetimi → Birincil Barkod)
4. ⬜ Production deploy tamamlandı (migration + kod)

---

### 2A · Putaway İş Akışı (`WmsPutawayEnabled`)

**Tetikleyici:** Mal kabul onaylandığında (`GoodsReceipt` status → Posted)

**Akış:**
1. Her GoodsReceiptLine için otomatik `PutawayTask` oluşturulur (status: Pending)
2. Depocu terminal ekranında "Bekleyen Dağıtımlar" listesini görür
3. Her görev için: ürünü tara → hedef lokasyonu tara/seç → miktar gir
4. `PutawayLine` oluşturulur, `DistributedQty` artırılır
5. `TotalQty` dolduğunda task status → Completed, `StockLocation.OnHandQty` güncellenir

**Yapılacaklar:**
- Backend: `GoodsReceiptPostedEvent` handler → `PutawayTask` oluşturma
- Backend: `DistributePutawayCommand` — line ekle, miktar güncelle
- Backend: `GetPendingPutawayTasksQuery` — terminale görev listesi
- Frontend: `PutawayView.vue` — handheld-uyumlu dağıtım ekranı

---

### 2B · Barkodlu Toplama (`WmsBarcodePickingEnabled`)

**Tetikleyici:** Micro veya Macro picking akışında, flag aktifse

**Akış (mevcut flow üzerine eklenti):**
1. Toplama listesi açılır (mevcut davranış)
2. Her ürün için: barkod taranır → sistem doğrular
3. Yanlış ürün → hata sesi (`useSoundFeedback.error()`) + kırmızı uyarı
4. Doğru ürün → onaylanır, miktar girilir
5. Tamamlanınca → `useSoundFeedback.complete()`

**Yapılacaklar:**
- Backend: `ValidateBarcodeForShipmentLineQuery` — barkod + sevkiyat satırı eşleşme kontrolü
- Frontend: Picking modal'a barkod input alanı eklenir (DataWedge keyboard input)
- Frontend: Barkod doğrulama state machine (scanning → validating → confirmed/error)

**Not:** DataWedge (Zebra TC15) barkodu klavye girişi olarak gönderir, özel SDK gerekmez.

---

### 2C · Lokasyon Bazlı Toplama (`WmsLocationPickingEnabled`)

**Tetikleyici:** Picking sırasında, flag aktifse

**Akış:**
1. Toplama listesinde her ürün için sistem lokasyon önerir:
   - Önce: `StockMaster.DefaultPickingFaceId` (atanmışsa)
   - Sonra: `StockLocation` → PickingFace tipi → en çok stoku olan
   - Son çare: Rack lokasyonu
2. Depocu önerilen lokasyonu tara veya manuel değiştirir
3. Ürün toplandıktan sonra `StockLocation.OnHandQty` düşülür (sadece StockMaster değil)
4. Toplama gözü stoku sıfıra düşerse → uyarı

**Yapılacaklar:**
- Backend: `SuggestPickingLocationQuery` — lokasyon öneri algoritması
- Backend: `ConfirmPickingFromLocationCommand` — StockLocation düşümü
- Backend: Gap fix — `StockMaster.OnHandQty ↔ SUM(StockLocation.OnHandQty)` senkronizasyonu
- Frontend: Picking'e lokasyon önerisi paneli eklenir

---

### 2D · Raf → Toplama Gözü Transfer Ekranı

**Bağımsız iş akışı — flag gerektirmez**

Depocunun raf → toplama gözüne ürün aktarmasını kayıt altına alır.

**Akış:**
1. Kaynak adresi tara (raf QR)
2. Kat seç (terminal dropdown, TotalFloors'a göre)
3. Hedef toplama gözünü tara
4. Miktarı gir (veya tüm paleti)
5. `LocationTransfer` kaydı oluşur, `StockLocation` güncellenir

**Yapılacaklar:**
- Frontend: Yeni sayfa veya modal `LocationTransferView.vue` (handheld-uyumlu)
- Backend: Mevcut `TransferStock` command'ı yeterli — sadece frontend gerekiyor

---

### 2E · Toplama Gözü Doluluk Uyarısı

Bir toplama gözündeki stok tanımlanan bir eşiğin altına düştüğünde bildirim:
- `StockLocation.OnHandQty < eşik` → SSE bildirimi + notification kaydı
- Eşik: `StockMaster.MinStockQty` veya `StockLocation`'a özel alan (ileride)

---

## Phase 2 Öncelik Matrisi

| Görev | Flag | Öncelik | Karmaşıklık | Bağımlılık |
|-------|------|---------|-------------|-----------|
| Production deploy | — | 🔴 Kritik | Düşük | Phase 1 tamamı |
| 2D · Raf → Toplama gözü transfer ekranı | Yok | 🔴 Yüksek | Düşük | Lokasyon verisi |
| 2A · Putaway iş akışı | WmsPutawayEnabled | 🟡 Orta | Orta | 2D |
| 2B · Barkodlu toplama | WmsBarcodePickingEnabled | 🟡 Orta | Düşük | Barkod girişi |
| 2E · Doluluk uyarısı | WmsLocationPickingEnabled | 🟡 Orta | Düşük | 2C |
| 2C · Lokasyon bazlı toplama | WmsLocationPickingEnabled | 🟢 İleride | Yüksek | 2A + 2D |

---

## Bilinen Gap'ler (İzleme)

| Gap | Öncelik | Not |
|-----|---------|-----|
| `StockMaster.OnHandQty ↔ SUM(StockLocation.OnHandQty)` uyumsuzluğu | Yüksek | Picking zone'dan düşüm StockMaster'ı günceller ama StockLocation'ı güncellemiyor |
| `DefaultPickingFaceId` frontend atama ekranı yok | Orta | Stok kartından varsayılan toplama gözü seçilemiyor henüz |
| Depo Haritası'nda `WarehouseMapView` — modül detayı basit | Düşük | Tıklayınca lokasyon listesi açılmıyor, sadece özet gösteriyor |
| Mevcut lokasyonların `ContainerType` alanı boş | Orta | Phase 1 öncesi oluşturulan lokasyonlarda `ContainerType=0 (Pallet)` varsayılan, doğrulama yapılmadı |
| Lot/Batch/Expiry (FEFO) takibi | Düşük | Gıda ürünleri için kritik, ama ayrı bir büyük çalışma gerektirir |
| Döngüsel sayım (cycle count) | Düşük | Şu an sadece tam sayım var |
| Replenishment otomasyonu | Düşük | `MinStockQty`/`ReorderPoint` var ama otomatik PO önerisi yok |

---

## Cihaz Notu

- **RT112 tablet** → Müdür kullanımı (depo yönetim ekranları, raporlar)
- **TC15 handheld terminal** → Depo personeli + şoförler
  - DataWedge: barkodu klavye girişi olarak gönderir → PWA ile native app gerekmez
  - Tüm WMS akışları PWA üzerinden çalışır, ayrı native uygulama gerekmez
