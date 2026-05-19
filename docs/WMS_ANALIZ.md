# WMS Faz 2 — Analiz ve Yol Haritası

> Tarih: 2026-05-09  
> Mevcut durum: Faz 1 (Sevkiyat Yönetimi) tamamlandı. Bu belge Faz 2 WMS geliştirmesi için referanstır.

---

## Mevcut WMS Altyapısı (Hazır)

Şu an kodda var olan, üzerine inşa edilebilecek temeller:

### Domain Entities

| Entity | Açıklama |
|--------|----------|
| `StockMaster` | Ana stok kataloğu. `OnHandQty`, `ReservedQty`, domain metodları: `Reserve`, `Deduct`, `Increase`, `AdjustOnHand`, `OverrideOnHand`. RowVersion concurrency. |
| `WarehouseLocation` | Fiziksel raf/lokasyon. Kod formatı: `{KoridorNo}{Taraf}-{ModulNo:D3}-{Kat:D2}` (örn: `1K-001-03`). MaxWeightKg, MaxPallets. |
| `StockLocation` | Bin-level stok miktarı. StockMasterId + WarehouseLocationId (unique). OnHandQty, ReservedQty, LastMovedAt. |
| `LocationTransfer` | Lokasyonlar arası hareket audit logu. FromLocation, ToLocation, Qty, TransferredBy. |
| `StockTransaction` | Genel audit trail. Tipler: GoodsIn, ShipmentOut, ManualAdjust, Reserve, ReleaseReserve, VehicleReturn, GoodsInCorrection. |
| `StockCount` | Periyodik fiziksel sayım. Excel import/export. ExpectedQty vs ActualQty. |
| `StockConsumption` | Zai, dahili kullanım, depo satışı kaydı. |
| `StockMapping` | ISS-IP dış stok kodu → iç StockMaster eşleştirmesi. |

### Mevcut Application Commands/Queries

- `AssignStockToLocationCommand` — Manuel lokasyon ataması (upsert)
- `TransferStockCommand` — Lokasyonlar arası transfer + LocationTransfer log
- `GetStockLocationsQuery` — Stok-lokasyon haritası
- `GetTransferHistoryQuery` — Transfer geçmişi
- `CreateWarehouseLocation`, `BulkCreateWarehouseLocations`, `UpdateWarehouseLocation`
- `GetWarehouseLocationsQuery`
- `CreateStockCount`, `UpdateStockCountLines`, `CompleteStockCount`, `ImportStockCountFromExcel`, `ExportStockCountTemplate`
- `SyncNetsisStockBalance` — Netsis'ten bakiye override

### Mevcut Frontend Views

| View | Route | Durum |
|------|-------|-------|
| `StockManagementView` | `/stocks` | CRUD, Excel import/export |
| `StockLocationsView` | `/warehouse/stock-locations` | Lokasyon haritası, transfer UI, 2 tab |
| `WarehouseLocationsView` | `/warehouse/locations` | Fiziksel lokasyon yönetimi |
| `StockCountView` | `/stock-counts` | Sayım oluştur, Excel import |
| `StockConsumptionView` | `/stock-consumptions` | Zai/dahili/satış kaydı |
| `StockMappingView` | `/stocks/mappings` | ISS eşleştirme |

---

## Kritik Açıklar ve Öncelik Sırası

### 🔴 Yüksek Öncelik

#### 1. StockMaster ↔ StockLocation Senkron Problemi
**Sorun:** İki ayrı bakiye tutulmasına rağmen bunlar otomatik senkronize edilmiyor.
- Zone picking → `StockMaster.Deduct()` çalışıyor ama `StockLocation.OnHandQty` düşmüyor
- GoodsReceipt post → `StockMaster.Increase()` çalışıyor ama hangi lokasyona gittiği bilinmiyor
- `AssignStockToLocationCommand` StockMaster'ı hiç güncellemeden sadece lokasyon kaydını yazıyor

**Çözüm seçenekleri:**
- A) Strict coupling: Her StockMaster hareketi eş zamanlı bir StockLocation hareketini zorunlu kılsın (lokasyon seçimi gerektirir)
- B) Nightly reconciliation job: Lokasyon toplamlarını periyodik olarak StockMaster ile karşılaştır, sapmaları raporla
- C) Soft sync: StockMaster authoritative kalır, StockLocation sadece "nerede var" bilgisini tutar (miktar doğruluğu garanti edilmez)

**Öneri:** Faz 2'de B → Faz 3'te A geçişi. Önce sapma görünürlüğü, sonra strict coupling.

---

#### 2. Picking → Bin Düşümü Yok
**Sorun:** Micro/Macro toplama sırasında `StockMaster.OnHandQty` azalıyor ama hangi raftan alındığı `StockLocation`'a yansımıyor. Raf bazında stok takibi fiilen çalışmıyor.

**Etkilenen flow:** `AssignToWarehouseCommand`, `ConfirmZoneLoadingCommand`

**Çözüm:** Picking sırasında kullanıcı ya da sistem hangi lokasyondan alındığını belirtmeli → `StockLocation.OnHandQty` düşmeli → `LocationTransfer` veya `StockTransaction` logu oluşmalı.

**UI gereksinimi:** Toplama ekranına (MicroPickingModal, MacroPickingModal) lokasyon seçimi veya barkod taraması eklenmesi gerekiyor.

---

#### 3. GoodsReceipt → Putaway Workflow Yok
**Sorun:** GoodsReceipt onaylandığında (`Post` komutu) `StockMaster.Increase()` çalışıyor ama stok hiçbir `StockLocation`'a atanmıyor. Mal depoya geldi ama nerede olduğu bilinmiyor.

**İstenen akış:**
```
GoodsReceipt.Post() 
  → StockMaster.Increase() [mevcut ✅]
  → Putaway listesi oluştur (hangi ürün, kaç adet, önerilen lokasyon)
  → Depo personeli lokasyonu onaylar veya değiştirir
  → StockLocation güncellenir [eksik ❌]
  → LocationTransfer logu [eksik ❌]
```

**Öneri:** Putaway'i ayrı bir adım olarak tasarla, GoodsReceipt'i bloklamasın. "Putaway bekliyor" durumu ekle.

---

#### 4. Lot/Batch + Son Kullanma Tarihi (FEFO)
**Sorun:** Gıda ürünleri için kritik. Hiçbir entity'de `LotNo`, `ExpiryDate`, `ManufactureDate` alanı yok.

**Etkilenen senaryolar:**
- Kısa tarihli ürün önce gönderilmeli (FEFO)
- Recall durumunda hangi müşterilere hangi lot gitti bilinmemeli
- Gıda hazırlık akışında tarih kontrolü yapılamıyor

**Gerekli değişiklikler:**
- `GoodsReceiptLine` → `LotNo`, `ExpiryDate` alanları
- `StockLocation` → Lot bazında ayrı kayıt (StockMasterId + WarehouseLocationId + LotNo)
- Sevkiyat picking → FEFO sırasına göre lot seçimi
- Bu **önemli bir şema değişikliği** — erken planlanmalı

---

### 🟡 Orta Öncelik

#### 5. MinStockQty / ReorderPoint Otomasyonu
**Mevcut:** Alan var, UI'da gösteriliyor ama otomatik tetik yok.

**Eksik:**
- Stok `MinStockQty` altına düştüğünde bildirim
- `ReorderPoint`'e ulaşınca PO taslağı önerisi
- Critical stock dashboard widget

**Çözüm:** `GoodsReceipt.Post()` ve `Shipment.Deliver()` sonrası bakiye kontrolü → `NotificationService` ile ilgili kullanıcılara SSE/push bildirimi.

---

#### 6. Cycle Counting (Döngüsel Sayım)
**Mevcut:** Sadece full physical count mevcut.

**Eksik:**
- ABC analizi (A: her hafta, B: her ay, C: her çeyrek)
- Lokasyon bazında count planı
- Otomatik sayım görevleri

**Çözüm:** `StockCount`'a `CountType` (Full / Zone / ABC) ekle. Scheduler ile otomatik count oluştur.

---

#### 7. Barkod/QR ile Mal Kabul
**Mevcut:** GoodsReceipt satırları manuel girilıyor.

**Eksik:** Barkod tarayıcı ile hızlı mal kabul.

**Çözüm:** `jsqr` zaten bağımlılık olarak var (driver için kullanılıyor). MalKabulDashboard'a QR/barkod scan modu eklenebilir.

---

#### 8. ABC / Hız Analizi ve Dinamik Sıralama
**Mevcut:** `PickingOrder` alanı var ama statik, elle giriliyor.

**Eksik:**
- Stok hareketi hızına (velocity) göre otomatik sıralama
- Yüksek hareketli ürünler erişimi kolay raflara atansın

**Çözüm:** `StockTransaction` logundan hareket frekansı hesapla → `StockMaster.PickingOrder` önerisi üret.

---

### 🟢 Düşük Öncelik

#### 9. Stok Maliyet Takibi (Valuation)
**Mevcut:** `StockMaster.UnitPrice` var.
**Eksik:** FIFO/Weighted-Average maliyet muhasebesi, stok değeri raporu.

#### 10. Tedarikçiye İade (Return to Supplier)
**Mevcut:** `FloatingReturn` var ama sadece belirsiz iadeler için.
**Eksik:** Tedarikçiye iade fişi, iade nedeni, Netsis aktarımı.

#### 11. Mobil WMS Ekranı (Depo Personeli)
**Mevcut:** Driver app tam çalışıyor. Depo personeli için mobil ekran yok.
**Eksik:** Raf konumuna göre picking task listesi, barkod ile doğrulama.

---

## Önerilen Faz 2 Uygulama Sırası

### Sprint 1 — Temel Senkron
1. StockMaster ↔ StockLocation reconciliation raporu (sapma görünürlüğü)
2. GoodsReceipt post → StockLocation assignment (putaway akışı, basit versiyon)
3. MinStockQty aşıldığında SSE bildirimi

### Sprint 2 — Picking Traceability
1. Zone picking sırasında lokasyon seçimi (önce isteğe bağlı, sonra zorunlu)
2. StockLocation.OnHandQty'yi picking'de düş
3. StockTransaction log'una lokasyon bilgisi ekle

### Sprint 3 — Lot Tracking
1. GoodsReceiptLine'a LotNo + ExpiryDate ekle (migration)
2. StockLocation'a lot bazlı ayrım
3. Gıda hazırlık akışında FEFO sıralaması

### Sprint 4 — Otomasyon
1. Cycle counting altyapısı (CountType + zone-based)
2. ReorderPoint tetikleyici + PO taslağı önerisi
3. ABC analizi ve PickingOrder önerisi

---

## Mimari Kararlar (Faz 2 öncesi cevaplanmalı)

1. **Strict coupling mu, soft sync mi?**  
   StockMaster.OnHandQty her zaman StockLocation toplamına eşit olmalı mı? Bu, her picking işlemine lokasyon seçimi ekler — operasyonel yük.

2. **Lot tracking zorunlu mu, isteğe bağlı mı?**  
   Gıda için zorunlu, kıyafet/kırtasiye için gereksiz. OperationType bazında toggle edilebilir.

3. **Putaway ayrı onay adımı mı olacak?**  
   "Mal kabul edildi, putaway bekleniyor" durumu GoodsReceipt'i bloklamasın, arka planda task oluştursun.

4. **Mobil WMS için ayrı layout mı?**  
   DriverLayout'a benzer, Warehouse rolüne özel `WarehouseLayout` düşünülebilir.

---

## Orchestrator Entegrasyonu

`D:\vue\ai-orchestrator` konumundaki agent sistem bu fazda aktif kullanılacak:
- **plan-feature.md** → Sprint planı hazırlamadan önce çalıştır
- **Architect Agent** → Lot tracking şema değişikliği, strict coupling kararı öncesi
- **QA-Reviewer** → Senkron problemi çözümünden önce edge case analizi

---

## Dosya Referansları

| Alan | Dosya |
|------|-------|
| StockMaster domain | `Domain/Entities/StockMaster.cs` |
| StockLocation | `Domain/Entities/StockLocation.cs` |
| WarehouseLocation | `Domain/Entities/WarehouseLocation.cs` |
| LocationTransfer | `Domain/Entities/LocationTransfer.cs` |
| StockTransaction | `Domain/Entities/StockTransaction.cs` |
| StockConsumption | `Domain/Entities/StockConsumption.cs` |
| GoodsReceipt post command | `Application/GoodsReceipts/Commands/PostGoodsReceipt/` |
| AssignToWarehouse (reserve) | `Application/Shipments/Commands/AssignToWarehouse/` |
| ConfirmZoneLoading | `Application/Warehouse/Commands/ConfirmZoneLoading/` |
| StockLocations controller | `WebApi/Controllers/StockLocationsController.cs` |
| StockLocations view | `client/src/views/StockLocationsView.vue` |
| MalKabul dashboard | `client/src/views/MalKabulDashboardView.vue` |
