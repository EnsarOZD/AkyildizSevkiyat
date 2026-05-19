# Stok Sistemi Mimari Analizi ve Uygulama Planı

> **Tarih:** 2026-03-24
> **Kapsam:** Flat Stock Model — OnHandQty / ReservedQty / AvailableQty
> **Hedef:** Çalışan değil, güvenilir sistem

---

## STOK AKIŞI — DOĞRU MODEL

| Adım | OnHandQty | ReservedQty | StockTransaction |
|------|-----------|-------------|-----------------|
| GoodsReceipt Posted | ↑ +AcceptedQty | — | GoodsIn |
| AssignToWarehouse | — | ↑ +OrderedQty | Reserve |
| AssignVehicle | — | — | — |
| **Delivered** | ↓ -DeliveredQty | ↓ -OrderedQty | ShipmentOut |
| Return | ↑ +ReturnedQty | — | VehicleReturn |
| Shipment İptal | — | ↓ -OrderedQty | ReservationReleased |

`AvailableQty = OnHandQty - ReservedQty` (computed, DB'ye yazılmaz)

---

## MEVCUT KOD vs DOĞRU MODEL

| # | Sorun | Dosya | Risk |
|---|-------|-------|------|
| ❌ | AssignVehicle stok düşürüyor (Delivered yerine) | `AssignVehicleCommandHandler.cs` | Araçtaki mal depoda görünmüyor |
| ❌ | Delivered'da hiçbir stok işlemi yok | `MarkShipmentDelivered*.cs` | Teslim kaydı stoka yansımıyor |
| ❌ | AvailableQty kontrolü yok (Reserve sırasında) | `AssignToWarehouseCommandHandler.cs` | Negatif available ile rezervasyon yapılıyor |
| ❌ | Double-reserve koruması yok | `AssignToWarehouseCommandHandler.cs` | Retry → ReservedQty 2x artıyor |
| ❌ | RowVersion yok (concurrency) | `StockMaster.cs` | Race condition → phantom reserve |
| ❌ | İptal edilen sevkiyat rezervasyonu serbest bırakmıyor | Cancel handler | Reserved stok kilitli kalıyor |
| ⚠️ | StockTransaction'da ShipmentId FK yok | `StockTransaction.cs` | Sadece string reference, sorgulama zor |
| ⚠️ | AcceptedQty > ReceivedQty validasyonu yok | `PostGoodsReceiptCommandHandler.cs` | Veri tutarsızlığı |
| ⚠️ | PO fully received → Closed geçişi yok | `PostGoodsReceiptCommandHandler.cs` | PO PartiallyReceived'da kalıyor |

---

## 5 KRİTİK DEĞİŞİKLİK

### #1 — AssignVehicle stok çıkışını Delivered'a taşı
**Neden:** Araçtaki mal fiziksel olarak depoda. Çıkış teslimatta olmalı.
**Dosyalar:** `AssignVehicleCommandHandler.cs`, `MarkShipmentDeliveredCommandHandler.cs`
**Süre:** ~2 saat

### #2 — StockMaster domain metodları + AvailableQty kontrolü
**Neden:** Stok dışı rezervasyon önlenir. Setter'lar private olur, handler bypass edemez.
**Dosyalar:** `StockMaster.cs`, `AssignToWarehouseCommandHandler.cs`
**Süre:** ~1 saat

### #3 — Double-reserve koruması (Shipment.StockReserved flag)
**Neden:** Retry veya duplicate çağrıda ReservedQty iki kez artmaz.
**Dosyalar:** `Shipment.cs` + migration, `AssignToWarehouseCommandHandler.cs`
**Süre:** ~1 saat

### #4 — RowVersion + DB check constraints
**Neden:** Concurrent reserve race condition önlenir. Negatif stok DB seviyesinde engellenir.
**Dosyalar:** `StockMaster.cs` + migration, `SevkiyatDbContext.cs`
**Süre:** ~2 saat

### #5 — İptal edilen sevkiyatlarda rezervasyon serbest bırak
**Neden:** İptal → stok kilitleniyor, başka sevkiyat için kullanılamıyor.
**Dosyalar:** Cancel/revert handler(ları)
**Süre:** ~2 saat

---

## UYGULAMA TAKVİMİ

```
Gün 1 — Operasyonel kritik:
  [ ] #1: AssignVehicle fix
  [ ] #2: Domain metodları + AvailableQty kontrolü

Gün 2 — Güvenlik:
  [ ] #3: Double-reserve flag
  [ ] #5: İptal → rezervasyon serbest bırak

Gün 3 — Altyapı:
  [ ] #4: RowVersion + DB constraints + migration
```

---

## DOMAIN METOD TASARIMI

```csharp
// StockMaster.cs
public void Reserve(decimal qty)
{
    if (qty <= 0)
        throw new DomainException($"{StockCode}: rezervasyon miktarı pozitif olmalı.");
    if (AvailableQty < qty)
        throw new DomainException(
            $"{StockCode}: yetersiz stok. Mevcut: {AvailableQty}, Talep: {qty}");
    ReservedQty += qty;
}

public void ReleaseReservation(decimal qty)
{
    ReservedQty = Math.Max(0, ReservedQty - qty);
}

public void Deduct(decimal qty)
{
    if (OnHandQty < qty)
        throw new DomainException(
            $"{StockCode}: OnHandQty yetersiz. Mevcut: {OnHandQty}, Çıkış: {qty}");
    OnHandQty  -= qty;
    ReservedQty = Math.Max(0, ReservedQty - qty);
}

public void Increase(decimal qty)
{
    if (qty <= 0)
        throw new DomainException($"{StockCode}: artış miktarı pozitif olmalı.");
    OnHandQty += qty;
}
```

---

## GÜVENLİK KURALLARI — ENFORCEMENT KATMANLARI

### Domain
- `StockMaster.Reserve()` → AvailableQty kontrolü
- `StockMaster.Deduct()` → OnHandQty < qty kontrolü
- `Shipment.MarkStockReserved()` → double-reserve koruması

### EF Core / DB
```csharp
entity.Property(s => s.RowVersion).IsRowVersion();
entity.ToTable(t => {
    t.HasCheckConstraint("CK_StockMaster_OnHandQty",    "[OnHandQty] >= 0");
    t.HasCheckConstraint("CK_StockMaster_ReservedQty",  "[ReservedQty] >= 0");
    t.HasCheckConstraint("CK_StockMaster_ReservedLte",  "[ReservedQty] <= [OnHandQty]");
});
```

### Transaction / Concurrency
```csharp
catch (DbUpdateConcurrencyException)
{
    throw new ConflictException(
        $"Stok aynı anda başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin.");
}
```

---

## GOODS RECEIPT — TAMAMLANMASI GEREKEN

```csharp
// Eksik validation — AcceptedQty > ReceivedQty
if (line.AcceptedQty.HasValue && line.AcceptedQty > line.ReceivedQty)
    throw new DomainException(
        $"Kabul miktarı ({line.AcceptedQty}) teslim alınan miktarı ({line.ReceivedQty}) aşamaz.");

// Eksik — PO Closed geçişi
bool fullyReceived = allLines.All(l => l.ReceivedQty >= l.OrderedQty);
if (fullyReceived) po.Status = PurchaseOrderStatus.Closed;
```

---

## SATIN ALMA — DEPO GÖRÜNÜRLÜĞü (minimal)

Karmaşık satınalma akışı YOK. Sadece:

1. **Bekleyen Mallar** — `PO.Status = Approved | PartiallyReceived`
2. **Geciken PO** — `ExpectedDeliveryDate < today`
3. **Kısmi Takip** — `PurchaseOrderLine.PendingQty = OrderedQty - ReceivedQty`

---

## UYGULAMA DURUMU

| # | Değişiklik | Durum | Tarih |
|---|-----------|-------|-------|
| #1 | AssignVehicle fix → Delivered'a taşı | ✅ Tamamlandı | 2026-03-24 |
| #2 | StockMaster domain metodları | ✅ Tamamlandı | 2026-03-24 |
| #3 | Double-reserve flag | ✅ Tamamlandı | 2026-03-24 |
| #4 | RowVersion + DB constraints | ✅ Tamamlandı | 2026-03-24 |
| #5 | İptal → rezervasyon serbest bırak | ✅ Tamamlandı | 2026-03-24 |
| +  | GoodsReceipt AcceptedQty validation | ✅ Tamamlandı | 2026-03-24 |
| +  | PO Closed geçişi | ✅ Tamamlandı | 2026-03-24 |
