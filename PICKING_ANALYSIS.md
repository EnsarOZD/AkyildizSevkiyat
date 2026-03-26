# Picking Sistemi Analizi

> **Tarih:** 2026-03-24
> **Kapsam:** Mevcut picking yapısının derinlemesine analizi — kırılma noktaları, validation eksikleri ve sağlamlaştırma planı
> **Durum:** Tüm kritik eksikler giderildi. Kıyafet operasyonu altyapısı hazır.

---

## 1. MEVCUT PICKING AKIŞI

### Entity / Status Katmanları

Picking iki ayrı durum makinesi üzerinde koşuyor:

```
Shipment.Status:        Created → AssignedToWarehouse → Picking → ReadyForDispatch
ZonePreparation.Status: Draft   → MicroPicking        → MacroPicking → ReadyForDriverInfo → ReadyForTransfer
```

İkisi arasındaki senkronizasyon **otomatik** — Zone durumu geçişleri Shipment durumunu da sürüklüyor.

### Shipment → ZonePreparation İlişkisi

`Shipment.ZonePreparationId` FK ile bağlı. `AssignToWarehouse` çağrıldığında bu bağ kuruluyor. `ZonePreparation.IsFrozen = true` olduğunda yeni sevkiyat o batch'e bağlanamıyor; yeni Batch (BatchNo+1) oluşuyor.

### MicroPicking / MacroPicking Ayrımı

Ayrımın temeli `StockMaster.PickingType` enum'u:

```
PickingType.Micro  → Proje bazında, satır bazında toplama (GetProjectMicroPickListQuery)
PickingType.Macro  → Zone bazında, stok kodu bazında gruplu toplama (GetZoneMacroPickListQuery)
```

**Micro flow (proje bazında):**
```
StartZonePreparation
→ Her proje için: UpdateMicroLinesBulkCommand (ShipmentLine.DeliveredQty set edilir)
→ Her proje için: MarkProjectMicroReadyCommand (ZonePreparationProject.IsMicroReady = true)
→ Tüm projeler ready → ZonePreparation.Status = MacroPicking (otomatik)
```

**Macro flow (zone bazında, gruplu):**
```
GetZoneMacroPickListQuery → Stok koduna göre tüm projelerin satırlarını grupla
→ UpdateAggregatedLinesCommand (tek komutla N satıra FIFO dağıtım)
→ MarkZoneMacroReadyCommand → Status = ReadyForDriverInfo
→ Tüm Shipment'lar: Picking → ReadyForDispatch (otomatik)
```

### PickedQty Nerede Tutuluyor?

`ShipmentLine.DeliveredQty` — hem picking hem teslim miktarı bu tek alanda. Ayrı bir `PickedQty` alanı yok. `MicroPickItemDto.PickedQty` bir alias:

```csharp
new MicroPickItemDto(..., line.DeliveredQty, isCompleted)
```

### Komut → Entity → Status Haritası

| Komut | Tetikleyen Değişiklik |
|-------|----------------------|
| `AssignToWarehouseCommand` | Shipment → AssignedToWarehouse, StockReserve, ZonePrep bağlantısı |
| `StartZonePreparationCommand` | ZP → MicroPicking, IsFrozen=true, Shipment → Picking |
| `UpdateMicroLinesBulkCommand` | ShipmentLine.DeliveredQty set (sahiplik doğrulamalı) |
| `MarkProjectMicroReadyCommand` | ZPProject.IsMicroReady=true → (tümü ready ise) ZP → MacroPicking |
| `UpdateAggregatedLinesCommand` | ShipmentLine.DeliveredQty FIFO dağıtım |
| `MarkZoneMacroReadyCommand` | ZP → ReadyForDriverInfo, Shipment → ReadyForDispatch |
| `SetZoneDriverInfoCommand` | ZP → ReadyForTransfer |
| `RevertToDraftCommand` | Shipment → Created, StockRelease, **DeliveredQty sıfırlanır** ✅ |

---

## 2. FAZLA TOPLAMA KURALI

Depo, sipariş miktarını **aşan** toplama yapabilir. Koli tamamlama bunun tipik örneğidir:

> Proje 12 adet eldiven istedi → depo tam koli olsun diye 20 adet gönderiyor.

**Kural:**

| Durum | Davranış |
|-------|----------|
| `DeliveredQty < 0` | ❌ Blokla |
| `DeliveredQty = 0` ve MarkReady basıldı | ❌ Blokla (`ForceComplete` olmadan) |
| `DeliveredQty < OrderedQty` | ⚠️ İzin ver — açıklama zorunlu |
| `DeliveredQty > OrderedQty` | ⚠️ İzin ver — açıklama zorunlu (koli tamamlama) |
| `DeliveredQty = OrderedQty` | ✅ Serbest |

---

## 3. VALİDASYON MATRİSİ (GÜNCEL)

| Durum | Blokla | Açıklama Zorunlu | Durum |
|-------|--------|-----------------|-------|
| `DeliveredQty < 0` | ✅ | — | ✅ Uygulandı |
| `DeliveredQty > OrderedQty` | — | ✅ | ✅ Uygulandı |
| `DeliveredQty < OrderedQty` | — | ✅ | ✅ Uygulandı |
| `DeliveredQty = 0` ve Micro Ready basıldı | ✅ (ForceComplete olmadan) | — | ✅ Uygulandı |
| `DeliveredQty = 0` ve Macro Ready basıldı | ✅ (ForceComplete olmadan) | — | ✅ Uygulandı |
| Mapping eksik satır var, Micro Ready basıldı | ⚠️ Uyarı (UnmappedLineCount) | — | ✅ Uygulandı |
| ShipmentLine başka Zone/Proje'ye ait | ✅ | — | ✅ Uygulandı |
| `NewTotalPickedQty < 0` (Macro) | ✅ | — | ✅ Uygulandı |
| `NewTotalPickedQty > TotalOrderedQty` (Macro) | — | ✅ | ✅ Uygulandı |
| RevertToDraft sonrası DeliveredQty kirli | — | — | ✅ ResetPickingData ile temizleniyor |

---

## 4. PICKING GRANULARITY

| Aşama | Çalışma Seviyesi |
|-------|-----------------|
| Micro pick list görüntüleme | Satır (ShipmentLine) bazında — proje bazında gruplu |
| Micro güncelleme | Satır bazında (`UpdateMicroLinesBulkCommand`) |
| Micro hazır işareti | Proje bazında — satır doluluğu kontrol edilerek |
| Macro pick list görüntüleme | Stok kodu bazında — zone genelinde gruplu |
| Macro güncelleme | Stok kodu grubu bazında (FIFO dağıtım) |
| Macro hazır işareti | Zone bazında — unfilled satır sayısı kontrol edilerek |

---

## 5. SAĞLAMLAŞTIRMA TASARIMI

### ShipmentLine Domain Değişiklikleri

```csharp
// SetDeliveredQty — fazla toplama geçerli, fark varsa açıklama zorunlu
public void SetDeliveredQty(decimal qty, string? reason = null, string? note = null)
{
    if (qty < 0)
        throw new DomainException("Toplama miktarı negatif olamaz.");

    // Fazla toplama geçerli (koli tamamlama vb.) — blok YOK
    if (qty != OrderedQty && string.IsNullOrWhiteSpace(reason) && string.IsNullOrWhiteSpace(DifferenceReason))
        throw new DomainException("Toplanan miktar sipariş miktarından farklıysa açıklama zorunludur.");

    DeliveredQty = qty;
    if (!string.IsNullOrWhiteSpace(reason)) DifferenceReason = reason;
    if (!string.IsNullOrWhiteSpace(note)) Note = note;
}

// ResetPickingData — RevertToDraft'ta çağrılır
public void ResetPickingData()
{
    DeliveredQty = 0;
    DifferenceReason = null;
}
```

### ForceComplete Pattern

Kasıtlı eksik toplama senaryolarında (ürün yok, hasar, vb.) operatör override yapabilir:

```csharp
public record MarkProjectMicroReadyCommand(
    int ZonePreparationProjectId,
    bool ForceComplete = false,   // default=false → geriye dönük uyumlu
    string? ForceReason = null
) : IRequest<MarkProjectMicroReadyResult>;

public class MarkProjectMicroReadyResult
{
    public bool Success { get; set; }
    public int UnfilledLineCount { get; set; }  // 0 ise tüm satırlar toplandı
    public int UnmappedLineCount { get; set; }  // Mapping eksik satır sayısı
}
```

Aynı pattern `MarkZoneMacroReadyCommand` için de geçerli.

### UI Tarafında Zorunlu Davranışlar

| Ekran | Alan | Kural |
|-------|------|-------|
| Micro pick list | DeliveredQty | `qty >= 0` |
| Micro pick list | DifferenceReason | `DeliveredQty ≠ OrderedQty` ise zorunlu |
| Micro Ready butonu | Onay dialog | `UnfilledLineCount > 0` ise "X satır sıfır, devam edilsin mi?" |
| Micro Ready butonu | Uyarı | `UnmappedLineCount > 0` ise "X ürünün mapping'i eksik" |
| Macro pick list | NewTotalPickedQty | `qty >= 0` |
| Macro pick list | DifferenceReason | `NewTotalPickedQty ≠ TotalOrderedQty` ise zorunlu |
| Macro Ready butonu | Onay dialog | `UnfilledMacroLineCount > 0` ise uyarı |

### History Kayıtları

| Olay | Ne Yazılıyor |
|------|-------------|
| `UpdateMicroLinesBulkCommand` | Kullanıcı, zaman, stok adı, önceki qty → yeni qty |
| `UpdateAggregatedLinesCommand` | Kullanıcı, zaman, toplam girilen, sipariş miktarı |
| `MarkProjectMicroReady` | ForceComplete bilgisi (UnfilledLineCount result'ta) |
| `RevertToDraft` | Temizlenen satır sayısı ve toplam sıfırlanan qty |

---

## 6. İKİ OPERASYon TİPİ — CATERING vs KIYAFet

### Mevcut Durum

Sistem **Catering** (ikram) operasyonu için tasarlanmış: Micro + Macro zone picking.
`StockCategory.Kiyafet = 3` zaten var ama picking akışı tanımlı değil.

### Yapılan Altyapı Hazırlığı (2026-03-24)

| Değişiklik | Detay |
|-----------|-------|
| `OperationType` enum | `Catering = 0` (varsayılan), `Clothing = 1` |
| `Project.OperationType` | Mevcut projeler Catering default |
| Migration | `AddProjectOperationType` |
| `StartZonePreparation` guard | Clothing sevkiyat varsa → DomainException (`TODO: CLOTHING_PICKING` işareti) |

### Kıyafet Operasyonu — İleride Yapılacaklar

| Kriter | Catering | Kıyafet (TBD) |
|--------|----------|----------------|
| Gruplama | Stok kodu | Beden / renk varyantı |
| Toplama yöntemi | Zone bazlı bulk | Muhtemelen ürün bazlı |
| Fazla toplama | Koli tamamlama (geçerli) | TBD |
| Barkod tarama | Yok | TBD |
| Proje bazlı mı? | Evet (Micro) | TBD |

Kıyafet akışı tasarlandığında `// TODO: CLOTHING_PICKING` etiketli yerler başlangıç noktası.

---

## 7. UYGULAMA DURUMU

| # | Değişiklik | Durum | Tarih |
|---|-----------|-------|-------|
| 1a | `SetDeliveredQty`: negatif blok, fark → açıklama zorunlu, fazla toplama geçerli | ✅ | 2026-03-24 |
| 1b | `MicroLineUpdateDto`: `DifferenceReason` alanı | ✅ | 2026-03-24 |
| 1c | `UpdateMicroLinesBulkCommand`: negatif → DomainException | ✅ | 2026-03-24 |
| 1d | `UpdateAggregatedLinesCommand`: negatif → DomainException, `DifferenceReason` alanı | ✅ | 2026-03-24 |
| 1e | `ShipmentLine.ResetPickingData()` domain metodu | ✅ | 2026-03-24 |
| 2  | `RevertToDraft`: `ResetPickingData` çağrısı + history kaydı | ✅ | 2026-03-24 |
| 3  | `UpdateMicroLinesBulkCommand`: `validSet` hash-set sahiplik doğrulaması | ✅ | 2026-03-24 |
| 4  | `MarkProjectMicroReady`: `ForceComplete` + `UnfilledLineCount` + `UnmappedLineCount` | ✅ | 2026-03-24 |
| 4  | `MarkZoneMacroReady`: `ForceComplete` + `UnfilledMacroLineCount` | ✅ | 2026-03-24 |
| 5  | `MicroPickItemDto.IsCompleted`: `false` (hard-coded) → `DeliveredQty > 0` | ✅ | 2026-03-24 |
| 6  | History: `userId`, önceki qty, stok adı (Micro + Macro + RevertToDraft) | ✅ | 2026-03-24 |
| 7  | `MarkProjectMicroReady` result: `UnmappedLineCount` hesaplaması | ✅ | 2026-03-24 |
| A  | `OperationType` enum + `Project.OperationType` + migration | ✅ | 2026-03-24 |
| A  | `StartZonePreparation`: Clothing guard (`TODO: CLOTHING_PICKING`) | ✅ | 2026-03-24 |
