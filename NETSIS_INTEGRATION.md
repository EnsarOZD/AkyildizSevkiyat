# Netsis Entegrasyon Planı

> **Tarih:** 2026-03-24
> **Kapsam:** 3 entegrasyon noktası — Satınalma Siparişi Export, Müşteri Siparişi Export, İrsaliye Okuma
> **Ön koşul:** Netsis REST API dokümantasyonu alınana kadar endpoint path'leri ve alan adları `PENDING_NETSIS_API_DOCS` olarak işaretli kalır.

---

## MEVCUT DURUM

| Özellik | Durum | Dosya |
|---------|-------|-------|
| Login + Token Cache | ✅ Yapılandırıldı | `NetsisClient.cs`, `NetsisTokenCache.cs` |
| Müşteri Siparişi Export | ⏳ Scaffold var — API docs bekliyor | `ExportShipmentToNetsisCommand.cs` |
| Stok Bakiye Sync | ✅ Çalışıyor | `SyncNetsisStockBalanceCommand.cs` |
| Satınalma Siparişi Export | ❌ Yok | — |
| İrsaliye Okuma | ❌ Yok | — |

---

## 3 ENTEGRASYON NOKTASI

---

### #1 — Satınalma Siparişi Export (PurchaseOrder → Netsis)

**Tetikleyici:** PO `Approved` durumuna geçtiğinde (manuel buton veya otomatik)
**İdempotency:** `PurchaseOrder.NetsisTransferredAt` — dolu ise yeniden aktarmaz

#### Netsis'e Gönderilecek Alanlar

| Alan | Kaynak | Not |
|------|--------|-----|
| BelgeNo | `PurchaseOrder.OrderNumber` | Bizim sipariş numaramız |
| TedarikciKodu | `Supplier.SupplierCode` | Netsis cari kodu |
| SiparisDate | `PurchaseOrder.OrderDate` | |
| TeslimTarihi | `PurchaseOrder.ExpectedDeliveryDate` | Opsiyonel |
| Aciklama | `PurchaseOrder.Note` | Opsiyonel |
| **Satır:** StokKodu | `StockMaster.NetsisStockCode` | **Kritik: NetsisStockCode dolu olmalı** |
| **Satır:** Miktar | `PurchaseOrderLine.OrderedQty` | |
| **Satır:** Birim | `PurchaseOrderLine.Unit` | StockUnit enum → string |
| **Satır:** BirimFiyati | — | Netsis kabul ediyorsa opsiyonel |

#### Saklanacak Geri Bilgiler

| Netsis Response | Entity Alanı |
|-----------------|--------------|
| Netsis PO No | `PurchaseOrder.ExternalRef` |
| Aktarım zamanı | `PurchaseOrder.NetsisTransferredAt` ← **yeni alan gerekiyor** |

#### Eksik Alan

```
PurchaseOrder: NetsisTransferredAt DateTime? — eklenecek
```

#### Bloklay ıcı Validasyonlar

- `Supplier.SupplierCode` boşsa → DomainException
- Herhangi satırda `StockMaster.NetsisStockCode` boşsa → DomainException (satır listesiyle)
- Zaten aktarılmışsa (`NetsisTransferredAt` dolu) → ConflictException
- PO status `Approved` değilse → DomainException

#### Yeni Dosyalar / Değişiklikler

| Dosya | Değişiklik |
|-------|------------|
| `Domain/Entities/PurchaseOrder.cs` | `NetsisTransferredAt DateTime?` ekle |
| `Application/External/Netsis/Dtos/NetsisDto.cs` | `NetsisPoRequest`, `NetsisPoLine`, `NetsisPoResult` ekle |
| `Application/Interfaces/INetsisClient.cs` | `CreateSatinalmaSiparisAsync(NetsisPoRequest)` imzası ekle |
| `Infrastructure/ExternalServices/Netsis/NetsisClient.cs` | `CreateSatinalmaSiparisAsync` implement et |
| `Infrastructure/ExternalServices/Netsis/NetsisOptions.cs` | `SatinalmaSiparisPath` ekle |
| `Application/Netsis/Commands/ExportPurchaseOrderToNetsis/` | Yeni command + handler + validator |
| Migration | `NetsisTransferredAt` kolonu |

---

### #2 — Müşteri Siparişi Export (Shipment → Netsis)

**Tetikleyici:** `ReadyForDispatch` veya `AssignedToVehicle` durumunda manuel aktarım
**İdempotency:** `Shipment.NetsisTransferredAt` — zaten var
**Scaffold:** `ExportShipmentToNetsisCommand` — API docs gelince endpoint path güncellenir

#### Netsis'e Gönderilecek Alanlar

| Alan | Kaynak | Not |
|------|--------|-----|
| BelgeNo | `Shipment.TalepNo` → `IssOrder.TalepNo` → `IssOrder.ExternalOrderNumber` | Öncelik sırası |
| CariKodu | `Project.NetsisCariKodu` | Boşsa blokla |
| ProjeKodu | `Project.Code` | |
| DepoKodu | `NetsisOptions.DepoKodu` | TODO: Project'e özel DepoKodu gerekirse |
| TeslimTarihi | `Shipment.DeliveryDate` | |
| **Satır:** StokKodu | ⚠️ `IssOrderLine.StockCode` mi, `StockMaster.NetsisStockCode` mi? | **Karar gerekiyor** |
| **Satır:** Miktar | `ShipmentLine.OrderedQty` | |
| **Satır:** Birim | `IssOrderLine.Unit` | |
| **Satır:** BirimFiyati | `IssOrderLine.BirimFiyati` | Opsiyonel |
| **Satır:** KdvOrani | `IssOrderLine.KDVOrani` | Opsiyonel |

#### Saklanacak Geri Bilgiler

| Netsis Response | Entity Alanı |
|-----------------|--------------|
| Netsis Sipariş No | `IssOrder.NetsisOrderNumber` |
| Aktarım zamanı | `Shipment.NetsisTransferredAt` |

#### Açık Kararlar

1. **StokKodu:** ISS-IP kodu (`IssOrderLine.StockCode`) mu, Netsis stok kodu (`StockMaster.NetsisStockCode`) mu?
   - Netsis'in kendi stok kodunu bekliyorsa → `StockMapping` üzerinden `StockMaster.NetsisStockCode` alınmalı
   - ISS-IP kodu geçerliyse → mevcut kod doğru

2. **DepoKodu:** Global config mi (`NetsisOptions.DepoKodu`), yoksa proje bazlı mı?
   - Proje bazlıysa `Project.NetsisDepoKodu` alanı eklenebilir

---

### #3 — İrsaliye Okuma (Netsis → Shipment)

**Tetikleyici:** Shipment `Delivered` olduğunda otomatik veya zamanlanmış polling (saatlik)
**Amaç:** Netsis'te oluşturulan irsaliye numarası ve tarihini sevkiyata bağlamak

#### Netsis'ten Alınacak Alanlar

| Netsis Alanı | Entity Alanı | Not |
|--------------|--------------|-----|
| IrsaliyeNo | `Shipment.IrsaliyeNo` | Alan zaten var |
| IrsaliyeTarihi | `Shipment.IrsaliyeDate` | Alan zaten var |
| NetsisOrderNo (sorgu key) | `IssOrder.NetsisOrderNumber` | Önce sipariş aktarımı yapılmış olmalı |

#### Sorgu Yöntemi (API docs gelince netleşecek)

```
Seçenek A: Sipariş numarasına göre irsaliye sorgula
  GET /irsaliyeler?siparisNo={NetsisOrderNo}

Seçenek B: Tarih aralığına göre bekleyen irsaliyeleri çek
  GET /irsaliyeler?baslangic={date}&bitis={date}
```

#### Yeni Dosyalar / Değişiklikler

| Dosya | Değişiklik |
|-------|------------|
| `Application/External/Netsis/Dtos/NetsisDto.cs` | `NetsisIrsaliyeQuery`, `NetsisIrsaliyeDto` ekle |
| `Application/Interfaces/INetsisClient.cs` | `GetIrsaliyeAsync(...)` imzası ekle |
| `Infrastructure/ExternalServices/Netsis/NetsisClient.cs` | `GetIrsaliyeAsync` implement et |
| `Infrastructure/ExternalServices/Netsis/NetsisOptions.cs` | `IrsaliyePath` ekle |
| `Application/Netsis/Commands/SyncIrsaliyeFromNetsis/` | Yeni command + handler |

#### Sync Stratejisi

```
SyncIrsaliyeFromNetsisCommand
  → Shipped & NetsisTransferredAt dolu & IrsaliyeNo boş olan Shipment'ları bul
  → Her biri için GetIrsaliyeAsync(NetsisOrderNo) çağır
  → IrsaliyeNo + IrsaliyeDate doldur
  → SyncedCount / NotFoundCount döndür
```

---

## EKSİK ALANLAR ÖZETI

### Entity Değişiklikleri

| Entity | Yeni Alan | Tip | Amaç |
|--------|-----------|-----|------|
| `PurchaseOrder` | `NetsisTransferredAt` | `DateTime?` | PO export idempotency |

### NetsisOptions Yeni Path'ler

| Config Key | Başlangıç Değeri | Amaç |
|------------|------------------|------|
| `SatinalmaSiparisPath` | `PENDING_NETSIS_API_DOCS` | PO export endpoint |
| `IrsaliyePath` | `PENDING_NETSIS_API_DOCS` | Waybill query endpoint |

---

## API DOCS GELİNCE CEVAPLANACAK SORULAR

| # | Soru | Etkisi |
|---|------|--------|
| 1 | Login endpoint path ve alan adları | `NetsisOptions.LoginPath`, `NetsisLoginRequest` |
| 2 | Token field adı (Token / AccessToken / Result) | `NetsisLoginResponse.Token` |
| 3 | Auth header schema (Bearer / Token / custom) | `SetAuthHeader()` |
| 4 | Müşteri siparişi endpoint path ve method | `NetsisOptions.SiparisPath` |
| 5 | Satınalma siparişi endpoint path ve method | `NetsisOptions.SatinalmaSiparisPath` |
| 6 | İrsaliye sorgu endpoint ve parametreler | `NetsisOptions.IrsaliyePath` |
| 7 | Response wrapper var mı? (`{ data: [...] }` vs direkt array) | `ReadFromJsonAsync<>` type arg |
| 8 | Stok kodu: ISS-IP kodu mu, Netsis kodu mu? | `ExportShipmentToNetsis` satır mapping |
| 9 | DepoKodu global mı, proje bazlı mı? | `NetsisSiparisRequest.DepoKodu` kaynağı |
| 10 | İrsaliye sorgusu: sipariş no bazlı mı, tarih bazlı mı? | Sync stratejisi |

---

## UYGULAMA SIRASI

```
API Docs beklemeden yapılabilecekler:
  [x] Bu dokümanı yaz
  [ ] PurchaseOrder.NetsisTransferredAt alanı + migration
  [ ] NetsisOptions'a SatinalmaSiparisPath + IrsaliyePath ekle
  [ ] Satınalma Siparişi DTO'larını (NetsisPoRequest/Line/Result) yaz
  [ ] İrsaliye DTO'larını (NetsisIrsaliyeQuery/Dto) yaz
  [ ] INetsisClient'a imzaları ekle
  [ ] ExportPurchaseOrderToNetsis command + handler + validator (PENDING guard ile)
  [ ] SyncIrsaliyeFromNetsis command + handler (PENDING guard ile)

API Docs gelince:
  [ ] Tüm PENDING_NETSIS_API_DOCS placeholder'larını güncelle
  [ ] Alan adlarını Netsis'in döndürdüğü gerçek adlarla eşleştir
  [ ] ExportShipmentToNetsis StokKodu mapping kararını uygula
  [ ] DepoKodu kaynağını netleştir
  [ ] Entegrasyon testleri
```

---

## UYGULAMA DURUMU

| # | Değişiklik | Durum | Tarih |
|---|-----------|-------|-------|
| 1 | Satınalma PO NetsisTransferredAt alanı | ✅ Tamamlandı | 2026-03-24 |
| 2 | NetsisOptions: SatinalmaSiparisPath + IrsaliyePath | ✅ Tamamlandı | 2026-03-24 |
| 3 | Satınalma DTO'ları (NetsisPoRequest/Line/Result) | ✅ Tamamlandı | 2026-03-24 |
| 4 | İrsaliye DTO'ları (NetsisIrsaliyeQuery/Dto) | ✅ Tamamlandı | 2026-03-24 |
| 5 | INetsisClient: CreateSatinalmaSiparisAsync + GetIrsaliyelerAsync | ✅ Tamamlandı | 2026-03-24 |
| 6 | ExportPurchaseOrderToNetsis command | ✅ Tamamlandı | 2026-03-24 |
| 7 | SyncIrsaliyeFromNetsis command | ✅ Tamamlandı | 2026-03-24 |
| 8 | Müşteri siparişi: StokKodu → StockMaster.NetsisStockCode | ✅ Tamamlandı | 2026-03-24 |
| 9 | Migration: PurchaseOrders.NetsisTransferredAt | ✅ Tamamlandı | 2026-03-24 |
| 10 | Müşteri siparişi: endpoint path güncelle | ⏳ API docs bekliyor | |
| 11 | Satınalma: endpoint path güncelle | ⏳ API docs bekliyor | |
| 12 | İrsaliye: endpoint path güncelle | ⏳ API docs bekliyor | |
