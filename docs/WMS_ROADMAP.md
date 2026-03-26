# Depo Yönetim Sistemi (WMS) — Yol Haritası

> Oluşturulma: 2026-03-22 · Son Güncelleme: 2026-03-22
> Durum: **W1 TAMAMLANDI · W2 TAMAMLANDI · W3+ PLANLAMA**
> Bağlı Proje: Akyildiz Sevkiyat

---

## Mevcut Stok Altyapısı (Tamamlananlar)

Faz 1'de tamamlanan temel altyapı:
- ✅ StockMaster CRUD (kod, ad, birim, kategori, fiyat, toplama tipi)
- ✅ StockTransaction (GoodsIn, ShipmentOut, ManualAdjust, Reserve, ReleaseReserve, VehicleReturn)
- ✅ OnHandQty / ReservedQty / AvailableQty otomatik hesaplama
- ✅ MinStockQty eşik uyarısı
- ✅ `WarehouseLocation` string alanı ("A-01-03" gibi metin)
- ✅ StockCount modülü (sayım → düzeltme fişi)
- ✅ Excel import/export
- ✅ Netsis ve ISS-IP entegrasyon noktaları

**Eksik:** WarehouseLocation yalnızca serbest metin — hiyerarşik yapı, palet takibi, lokasyon kapasitesi, transfer kaydı yok.

---

## Faz W1 — Adres / Lokasyon Yapısı ✅ TAMAMLANDI (2026-03-22)

### Hedef
Depodaki fiziksel konumları hiyerarşik bir veri yapısıyla modellemek.
Format: `Bölge – Koridor – Raf – Seviye – Göz` → örn. `A-02-03-2-01`

### Domain Entities

#### `WarehouseLocation` (YENİ)
```
Id           int PK
Code         string  UNIQUE  — örn. "A-02-03-2-01"
Zone         string  — Bölge: "A", "B", "Soğuk"
Row          string  — Koridor: "01", "02"
Rack         string  — Raf: "03"
Level        int     — Seviye (yükseklik): 1, 2, 3
Slot         string  — Göz: "01"
Description  string? — opsiyonel açıklama
LocationType enum    — Rack | FloorStack | Receiving | Shipping | Quarantine | Staging
MaxWeight    decimal?— kg cinsinden maksimum yük kapasitesi
MaxPallets   int?    — kaç palet sığar
IsActive     bool    default true
```

#### Lokasyon Tipleri
| Tip | Açıklama |
|-----|----------|
| `Rack` | Klasik raflı alan |
| `FloorStack` | Zemin istiflemesi |
| `Receiving` | Kabul/giriş alanı — mallar buraya gelir |
| `Shipping` | Sevkiyat/çıkış alanı — mallar buradan çıkar |
| `Quarantine` | Hasarlı / bekleyen mallar |
| `Staging` | Hazırlık alanı — toplama sonrası bekleme |

### Application Layer (CQRS)

**Commands:**
- `CreateWarehouseLocation` — yeni lokasyon oluştur
- `UpdateWarehouseLocation` — güncelle (tip, kapasite, aktiflik)
- `BulkCreateWarehouseLocations` — Excel ile toplu oluştur (A-01-01-1-01'den A-05-10-3-10'a kadar)

**Queries:**
- `GetWarehouseLocations` — liste (zone filtresi, tip filtresi, sayfalama)
- `GetLocationDetail` — lokasyon + içindeki stoklar + paletler

### Frontend
- Yeni sayfa: **Depo Adres Yönetimi** (`/warehouse/locations`)
- Filtrelenebilir lokasyon haritası (Bölge → tablo)
- Toplu oluşturma sihirbazı ("A bölgesinde 5 koridor, her koridorda 10 raf, 3 seviye, 5 göz oluştur")
- Kapasite doluluk göstergesi (progress bar)

### Migration
- Mevcut `StockMaster.WarehouseLocation` string → `WarehouseLocationId int? FK` olarak değiştirilir
- Migration sırasında mevcut string değerler manual olarak eşleştirilir

---

## Faz W2 — Stok-Lokasyon Bağlantısı ✅ TAMAMLANDI (2026-03-22)

### Hedef
Bir stok kalemi birden fazla fiziksel lokasyonda bulunabilsin.
Toplama sırasında hangi lokasyondan ne kadar alınacağı bilinsin.

### Domain Entities

#### `StockLocation` (YENİ)
```
Id                int PK
StockMasterId     int FK
WarehouseLocationId int FK
OnHandQty         decimal
ReservedQty       decimal
AvailableQty      decimal (computed)
LastMovedAt       DateTime?
```

**Index:** UNIQUE (StockMasterId, WarehouseLocationId)

#### `LocationTransfer` (YENİ — hareket kaydı)
```
Id                    int PK
StockMasterId         int FK
FromLocationId        int FK → WarehouseLocation
ToLocationId          int FK → WarehouseLocation
Qty                   decimal
Note                  string?
TransferredByUserId   int FK
TransferredAt         DateTime
PalletId              int? FK → Pallet (palet ile birlikte taşındıysa)
```

### Application Layer

**Commands:**
- `TransferStock` — lokasyonlar arası stok taşı (ör. kabul alanından rafa)
- `PutawayGoods` — GoodsReceipt sonrası malları kabul alanından belirlenen raflara yerleştir

**Queries:**
- `GetStockByLocation` — lokasyon bazlı stok durumu
- `GetStockLocations(stockMasterId)` — bir stokun tüm lokasyonlarını listele
- `GetTransferHistory` — lokasyon transfer geçmişi

### Frontend
- StockManagement sayfasına "Lokasyonlar" sekmesi ekle
- Lokasyon detayında stok listesi
- Transfer formu: kaynak lokasyon → hedef lokasyon → miktar

---

## Faz W3 — Palet Yönetimi

### Hedef
Palet bazlı takip: her paletin nerede olduğu, ne içerdiği, durumu.

### Domain Entities

#### `Pallet` (YENİ)
```
Id                    int PK
PalletCode            string UNIQUE  — otomatik üretilen (PAL-2026-0001) veya barkod
LocationId            int? FK → WarehouseLocation
Status                enum: Empty | Loading | Sealed | InTransit | Delivered | Damaged
WeightKg              decimal?
Note                  string?
CreatedAt             DateTime
CreatedByUserId       int FK
ShipmentId            int? FK → Shipment (sevkiyata atandıysa)
Lines                 1:N PalletLine
```

#### `PalletLine` (YENİ)
```
Id            int PK
PalletId      int FK
StockMasterId int FK
Qty           decimal
Unit          StockUnit (enum)
Note          string?
```

#### Palet Durumları
| Durum | Açıklama |
|-------|----------|
| `Empty` | Boş palet |
| `Loading` | Dolduruluyor |
| `Sealed` | Kapatıldı, sevkiyata hazır |
| `InTransit` | Araçta / yolda |
| `Delivered` | Teslim edildi |
| `Damaged` | Hasarlı, karantinada |

### Application Layer

**Commands:**
- `CreatePallet` — yeni palet oluştur (boş veya lokasyonla)
- `AddStockToPallet` — palete stok ekle
- `RemoveStockFromPallet` — paletten stok çıkar
- `SealPallet` — paleti kapat (sevkiyata hazır)
- `AssignPalletToShipment` — sevkiyata bağla
- `MovePallet` — paleti farklı lokasyona taşı (tüm içerik birlikte)
- `DamagePallet` — hasarlı işaretle + karantina lokasyonuna taşı

**Queries:**
- `GetPallets` — liste (durum, lokasyon, sevkiyat filtresi)
- `GetPalletDetail` — palet + içerik + hareket geçmişi
- `GetPalletsByShipment` — sevkiyata ait paletler

### Frontend
- Yeni sayfa: **Palet Yönetimi** (`/warehouse/pallets`)
- Kart görünümü: palet kodu, lokasyon, durum, içerik özeti, ağırlık
- Palet detay modalı: içerik listesi + hareket geçmişi
- QR kod üretme (PDF yazdırma için)
- Sevkiyat bazlı palet görünümü (sevkiyat detay sayfasına eklenti)

---

## Faz W4 — Süreç İyileştirmeleri

### W4-01 · Putaway Süreci (Mal Kabul → Rafa Yerleştirme)

**Akış:**
1. GoodsReceipt oluşturulur → mallar otomatik `Receiving` lokasyonuna düşer
2. Depo personeli "Putaway Görevi" alır
3. Her satır için hedef lokasyon seçilir
4. `PutawayGoods` komutu çalışır → StockLocation güncellenir + LocationTransfer kaydı oluşur

### W4-02 · Toplama Rotası Optimizasyonu (Picking Route)

Sevkiyat hazırlanırken toplama listesi, lokasyonlara göre sıralanır:
`Zone ASC, Row ASC, Rack ASC, Level ASC` → depo personeli en kısa yolu gider.

- ShipmentLine'lara `PickLocationId FK → WarehouseLocation` eklenir
- Toplama listesi basılabilir PDF olarak çıktı alınabilir

### W4-03 · Döngüsel Sayım (Cycle Count)

Tam sayım yerine belirli bölge/raf sayımı:
- `StockCount` entity'sine `CountScope` enum eklenir: `Full | Zone | Rack | Location`
- Seçilen lokasyondaki stoklar için mini sayım açılır
- Günde X lokasyon sayılacak şekilde otomatik görev oluşturulabilir

### W4-04 · Min Stok Otomasyonu

- `MinStockQty` zaten var → şu an yalnızca dashboard uyarısı
- Eklenti: **otomatik satın alma talebi önerisi** — stok minimum altına düşünce `PurchaseOrderSuggestion` kaydı oluştur
- Muhasebe kullanıcısına bildirim gönder

### W4-05 · Stok Dondurma / Hold

Kalite kontrol veya bekleyen mallar için:
```
StockHold entity:
  Id, StockMasterId, LocationId, HeldQty, Reason, HeldByUserId,
  HeldAt, ReleasedAt, ReleasedByUserId, Status(Active|Released)
```

---

## Faz W5 — Barkod & Mobil

### W5-01 · Barkod Alanları
- `StockMaster.Barcode` string? — EAN-13 / QR
- `Pallet.PalletCode` zaten var (QR oluşturulabilir)
- `WarehouseLocation.Code` → QR basılır rafa yapıştırılır

### W5-02 · Tarayıcı Kamera ile Barkod Okuma
- `vue-qrcode-reader` veya `html5-qrcode` kütüphanesi
- Stok arama modalında "Barkod Tara" butonu
- Palet taşıma formunda "Kaynak Palet Tara" → "Hedef Lokasyon Tara"

### W5-03 · Mobil Ambar Görünümü
- Mevcut Driver modülü pattern'ı baz alınır
- Yeni layout: `WarehouseLayout.vue`
- Sayfalar:
  - Toplama görevi listesi
  - Palet tarama & taşıma
  - Stok sayım (mobil)
  - Mal kabul

---

## Öncelik Matrisi

| Faz | Başlık | Öncelik | Karmaşıklık | Süre (tahmini) |
|-----|--------|---------|-------------|----------------|
| W1 | Adres / Lokasyon Yapısı | ✅ Tamamlandı | — | — |
| W2 | Stok-Lokasyon Bağlantısı | ✅ Tamamlandı | — | — |
| W3 | Palet Yönetimi | 🟡 Orta | Yüksek | 4-5 gün |
| W4-01 | Putaway Süreci | 🟡 Orta | Orta | 2-3 gün |
| W4-02 | Toplama Rotası | 🟢 Düşük | Düşük | 1-2 gün |
| W4-03 | Döngüsel Sayım | 🟢 Düşük | Orta | 2-3 gün |
| W4-04 | Min Stok Otomasyonu | 🟡 Orta | Düşük | 1-2 gün |
| W4-05 | Stok Dondurma | 🟢 Düşük | Düşük | 1-2 gün |
| W5 | Barkod & Mobil | 🟢 Düşük | Yüksek | 5-7 gün |

**Toplam: ~23-33 iş günü (tam WMS)**
**MVP (W1 + W2): ~7-9 iş günü**

---

## Teknik Kararlar ve Notlar

### WarehouseLocation Kodu Formatı
Öneri: `{Zone}-{Row}-{Rack}-{Level}-{Slot}`
Örnek: `A-02-03-2-01`
- Zone: harf (A-Z)
- Row/Rack/Slot: 2 haneli padded integer (01-99)
- Level: 1 haneli integer (1-5)

### Mevcut StockMaster.WarehouseLocation Migration Stratejisi
1. Yeni `WarehouseLocations` tablosu oluştur
2. `StockMasters` tablosuna `WarehouseLocationId int? FK` ekle
3. Mevcut `WarehouseLocation` string değerlerini `Code` kolonuyla eşleştir
4. Eşleşme sağlanamayanlar `NULL` bırakılır (veri kaybı yok)
5. Eski string kolonu silinmez, bir migration sonraki sürümde kaldırılır

### Multi-Location vs Single-Location
Şu an StockMaster başına tek lokasyon var.
Multi-location (W2) ekleme kararı verilirse:
- `StockMaster.OnHandQty` = `SUM(StockLocation.OnHandQty)` olur (hesaplanan)
- Tek lokasyon kullanmak isteyenler için "varsayılan lokasyon" kavramı korunur

### Palet QR Kodu
- Backend: `QRCoder` NuGet paketi ile PNG üret → Base64 döndür
- Frontend: `<img :src="qrBase64" />` ile göster + yazdır
- Format: `https://sevkiyat.akyildiz.com/pallets/{id}` veya sadece `PAL-2026-0001`

---

## Bağımlılıklar

- W2 → W1'e bağımlı (lokasyon olmadan stok-lokasyon olamaz)
- W3 → W1'e bağımlı (palet bir lokasyonda olur)
- W4-01 → W1 + W2'ye bağımlı
- W4-02 → W1'e bağımlı (lokasyon olmadan sıralama olamaz)
- W5-02 → W1 + W3'e bağımlı

---

## Sonuç

Sisteme en değeri katan ilk adımlar:

1. **W1 (Lokasyon yapısı)** — mevcut `WarehouseLocation` stringini gerçek bir entity'ye dönüştür
2. **W2 (Stok-lokasyon)** — aynı stokun farklı lokasyonlarda takibi + transfer kaydı
3. **W3 (Palet)** — toplama ve sevkiyatta palet bazlı iş akışı

Bu üç faz tamamlandığında sistem **tam teşekküllü bir WMS** olur.
