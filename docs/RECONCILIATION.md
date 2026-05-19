# Operasyonel Uzlaştırma Katmanı

> **Tarih:** 2026-03-24
> **Kapsam:** ISS → Sevkiyat → Toplama → Netsis zincirinin tutarlılık kontrolleri
> **Durum:** Backend tamamlandı. Frontend admin ekranı TBD.

---

## 1. AMAÇ

Şu 4 sistemin birbiriyle uyumlu kalmasını garanti etmek:

| Katman | Kaynak | Ne Kontrol Ediliyor |
|--------|--------|---------------------|
| ISS Sipariş | `IssOrder / IssOrderLine` | Sipariş miktarları doğru kopyalandı mı? |
| Sevkiyat | `Shipment / ShipmentLine` | Her ISS satırının bir ShipmentLine'ı var mı? |
| Toplama | `ShipmentLine.DeliveredQty` | Teslim edilen sevkiyatlarda tüm satırlar toplandı mı? |
| Netsis | `NetsisTransferredAt`, `IrsaliyeNo` | Aktarıldı mı? İrsaliye no geldi mi? |

---

## 2. KONTROL TÜRLERİ

### 2.1 IssQtyMismatch — ISS Miktar Uyumsuzluğu

**Tetikleyici:** `ShipmentLine.OrderedQty ≠ IssOrderLine.OrderedQty`

ISS siparişi import edilirken `ShipmentLine.OrderedQty`, `IssOrderLine.OrderedQty`'nin kopyasıdır. Eğer bunlar farklıysa veri bütünlüğü bozulmuş demektir.

| Özellik | Değer |
|---------|-------|
| Şiddet | Warning |
| Referans | ShipmentId + ShipmentLineId |
| Beklenen | IssOrderLine.OrderedQty |
| Gerçek | ShipmentLine.OrderedQty |

---

### 2.2 PickingIncomplete — Toplanmamış Satır

**Tetikleyici:** `Shipment.Status == Delivered AND ShipmentLine.DeliveredQty == 0 AND OrderedQty > 0`

Sevkiyat teslim edildi ama satırda toplama miktarı sıfır kalmış. İade (`ReturnedQty`) de yok.

| Özellik | Değer |
|---------|-------|
| Şiddet | Error |
| Referans | ShipmentId + ShipmentLineId |
| Beklenen | DeliveredQty > 0 |
| Gerçek | 0 |

> **Not:** `ForceComplete` ile kasıtlı atlanan satırlar için `DifferenceReason` dolu olur — bu kontrol yalnızca `DeliveredQty = 0` olan satırları yakalamak içindir.

---

### 2.3 NetsisTransferMissing — Netsis Aktarımı Eksik

**Tetikleyici:** `Status == Delivered AND NetsisTransferredAt == null AND DeliveredAt < (şimdi - 24 saat)`

Sevkiyat teslim edileli 24 saatten fazla geçmiş ama Netsis'e Müşteri Siparişi gönderilmemiş.

| Özellik | Değer |
|---------|-------|
| Şiddet | Warning |
| Referans | ShipmentId |
| Grace Period | 24 saat (yoğun gün sonrası aktarım için tolerans) |

---

### 2.4 IrsaliyeMissing — İrsaliye No Bekleniyor

**Tetikleyici:** `NetsisTransferredAt != null AND IrsaliyeNo == null AND NetsisTransferredAt < (şimdi - 24 saat)`

Netsis'e aktarım yapıldı, ancak `SyncIrsaliyeFromNetsis` komutu çalışmasına rağmen irsaliye no henüz gelmedi.

| Özellik | Değer |
|---------|-------|
| Şiddet | Warning |
| Referans | ShipmentId |
| Grace Period | 24 saat (Netsis işlem gecikmesi toleransı) |

---

### 2.5 IssCoverageGap — ISS Satırı Karşılıksız

**Tetikleyici:** `IssOrder.IsTransferred == true AND IssOrderLine.Id IN (ShipmentLine.IssOrderLineId) = FALSE`

Transfer edilmiş bir ISS siparişinde satır var ama bu satıra bağlı aktif ShipmentLine bulunamıyor.

| Özellik | Değer |
|---------|-------|
| Şiddet | Error |
| Referans | IssOrderLineId |
| Kapsam | Cancelled olmayan aktif ISS siparişleri |

---

## 3. MİMARİ

### Entity: ReconciliationIssue

```
Id              PK
IssueKey        varchar(100) UNIQUE — upsert anahtarı
CheckType       enum (IssQtyMismatch=1, PickingIncomplete=2, ...)
Severity        enum (Warning=1, Error=2)
Status          enum (Open=0, Acknowledged=1, AutoResolved=2)

ShipmentId?     FK nullable
ShipmentLineId? nullable
IssOrderLineId? nullable

Description     varchar(500)
ExpectedValue   varchar(200)
ActualValue     varchar(200)

DetectedAt      datetime
AcknowledgedAt? datetime
AcknowledgedByUserId? int
AcknowledgementNote?  varchar(500)
```

**IssueKey formatı:** `"{CheckType}:{EntityRef}"`

| CheckType | IssueKey Örneği |
|-----------|-----------------|
| IssQtyMismatch | `IssQtyMismatch:SL123` |
| PickingIncomplete | `PickingIncomplete:SL456` |
| NetsisTransferMissing | `NetsisTransferMissing:S789` |
| IrsaliyeMissing | `IrsaliyeMissing:S789` |
| IssCoverageGap | `IssCoverageGap:IOL101` |

### Application Katmanı

```
Application/Reconciliation/
  Commands/
    RunReconciliationChecks/       → tüm kontrolleri çalıştır, upsert et
    AcknowledgeReconciliationIssue/ → görüldü işaretle
  Queries/
    GetReconciliationIssues/        → filtreli liste + OpenSummary
```

### API Endpointleri

| Method | Path | Açıklama |
|--------|------|----------|
| `POST` | `/api/reconciliation/run` | Tüm kontrolleri çalıştır |
| `GET` | `/api/reconciliation/issues` | Sorunları listele (filtrelenebilir) |
| `POST` | `/api/reconciliation/issues/{id}/acknowledge` | Acknowledge et |

**GET parametreleri:** `checkType`, `status`, `severity`, `fromDate`, `toDate`, `page`, `pageSize`

---

## 4. UPSERT MANTIĞI

Her `RunReconciliationChecks` çalışmasında:

```
1. Mevcut Open sorunları yükle (IssueKey → Id haritası)
2. 5 kontrolü çalıştır → findings listesi
3. findings içinde olmayan Open sorunlar → Status = AutoResolved
4. existingOpenMap içinde olmayan findings → INSERT yeni kayıt
5. Her ikisinde de olan → dokunma (Open kalır, DetectedAt değişmez)
```

Acknowledged sorunlar hiçbir zaman etkilenmez — operatörün notu korunur.

---

## 5. RİSK SENARYOLARI

### Risk 1: IssQtyMismatch yanlış pozitif
**Senaryo:** Depo bir ürünü başka bir stokla değiştirdi (`UpdateMicroLinesBulk` ile `NewLocalStockId` kullandı). ShipmentLine.StockCode değişti ama `IssOrderLineId` hâlâ bağlı ve `OrderedQty` farklı olabilir.
**Mitigation:** Bu check `OrderedQty` karşılaştırması yapar. Stok değişimi sırasında `ShipmentLine.OrderedQty` güncellenmiyorsa uyarı çıkar. Acknowledge ile kapatılabilir.

### Risk 2: PickingIncomplete yanlış pozitif
**Senaryo:** `ForceComplete=true` ile bilinçli sıfır bırakılan satırlar. Bu satırlarda `DifferenceReason` dolu olur.
**Mitigation:** Bu kontrol sadece `DeliveredQty=0` olanları yakalar. ForceComplete ile geçilen satırlar için `DifferenceReason != null` kontrolü eklenebilir (ileride).

### Risk 3: NetsisTransferMissing geç teslim
**Senaryo:** Geç teslim saati (örn. gece yarısı), ertesi sabah aktarım yapılacak.
**Mitigation:** 24 saatlik grace period bunu karşılar. Yine de bir Warning çıkarsa Acknowledge edilir.

### Risk 4: IssCoverageGap phantom satır
**Senaryo:** ISS siparişinde bir satır Cancelled ShipmentLine'a bağlı ama bu `coveredLineIds` içine girmez (Cancelled filtre dışı). Sonuç: yanlış gap tespiti.
**Mitigation:** `Cancelled` şipmanlara ait satırlar `coveredLineIds`'tan hariç tutulur. ISS satırı gerçekten karşılıksız ise Error doğrudur.

### Risk 5: Reconciliation tablosu şişme
**Senaryo:** Binlerce eski AutoResolved kayıt birikir.
**Mitigation:** Gelecekte: `DELETE FROM ReconciliationIssues WHERE Status = AutoResolved AND DetectedAt < 90 gün önce` temizleme job'ı. Şu an: tablo küçük kalır.

---

## 6. AUDIT LOGGING (MEVCUT DURUM)

| Olay | Kayıt Nerede |
|------|-------------|
| Status geçişi | `ShipmentHistory` (OldStatus → NewStatus) |
| Picking miktarı değişti | `ShipmentHistory` (Description: "Micro Toplama: ...") |
| Macro dağıtım | `ShipmentHistory` (Description: "Macro Dağıtım: ...") |
| Revert to Draft | `ShipmentHistory` (Description: "Picking verileri temizlendi: N satır") |
| **Netsis aktarımı** *(YENİ)* | `ShipmentHistory` (Description: "Netsis'e aktarıldı. Belge No: ...") |
| ISS import | `ImportBatch / ImportBatchOrder` |

---

## 7. ADMIN EKRANI — TASARIM ÖNERİSİ

### Dashboard Kartları (üst bölüm)

| Kart | İçerik |
|------|--------|
| Error sayısı | Open Error tipindeki sorun sayısı (kırmızı) |
| Warning sayısı | Open Warning tipindeki sorun sayısı (sarı) |
| Son kontrol | RunReconciliationChecks son çalışma zamanı |
| Auto-resolved | Son çalışmada kendiliğinden kapanan sorun sayısı |

### Sorun Listesi (orta bölüm)

Filtreler: CheckType, Severity, Status, Tarih aralığı

| Sütun | Açıklama |
|-------|----------|
| Şiddet | Error / Warning ikonu |
| Kontrol Tipi | IssQtyMismatch, PickingIncomplete vb. |
| Açıklama | Description alanı |
| Beklenen / Gerçek | ExpectedValue / ActualValue |
| Sevkiyat | Sevkiyat #ID linki (ShipmentId varsa) |
| Tespit Tarihi | DetectedAt |
| Durum | Open / Acknowledged |
| Aksiyon | "Acknowledge" butonu (not giriş alanıyla) |

### Periyodik Çalıştırma

Günlük `POST /api/reconciliation/run` çağrısı (ör. sabah 07:00) önerilir.
Manuel tetikleme için admin panelinde "Kontrolleri Çalıştır" butonu yeterli.

---

## 8. UYGULAMA DURUMU

| # | Değişiklik | Durum | Tarih |
|---|-----------|-------|-------|
| 1 | `ReconciliationCheckType`, `ReconciliationSeverity`, `ReconciliationStatus` enum'ları | ✅ | 2026-03-24 |
| 2 | `ReconciliationIssue` entity | ✅ | 2026-03-24 |
| 3 | `IApplicationDbContext` + `SevkiyatDbContext` güncelleme | ✅ | 2026-03-24 |
| 4 | `AddReconciliationIssues` migration | ✅ | 2026-03-24 |
| 5 | `RunReconciliationChecksCommand` (5 check + upsert) | ✅ | 2026-03-24 |
| 6 | `GetReconciliationIssuesQuery` (filtrelenebilir, sayfalı) | ✅ | 2026-03-24 |
| 7 | `AcknowledgeReconciliationIssueCommand` | ✅ | 2026-03-24 |
| 8 | `ReconciliationController` (3 endpoint) | ✅ | 2026-03-24 |
| 9 | `ExportShipmentToNetsis` → ShipmentHistory audit kaydı | ✅ | 2026-03-24 |
| 10 | `WarehouseController` — MarkMicro/MacroReady dönüş tipi düzeltmesi | ✅ | 2026-03-24 |
| F1 | Frontend admin ekranı | ⬜ TBD | — |
| F2 | Günlük otomatik çalıştırma (cronjob / hangfire) | ⬜ TBD | — |
| F3 | AutoResolved temizleme job'ı (90 gün) | ⬜ TBD | — |
