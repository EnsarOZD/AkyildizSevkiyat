# Akyıldız Sevkiyat — Derin Ürün ve Mimari Analizi

> **Tarih:** 2026-03-24
> **Kapsam:** Operasyonel doğruluk, domain model, planlama motoru, teknik borç, ürün stratejisi
> **Amaç:** Gerçek hayatta kırılacak noktaları tespit et, çözüm yollarını tanımla

---

## İÇİNDEKİLER

1. [Operasyonel Doğruluk](#1-operasyonel-doğruluk)
2. [Domain Model & İş Akışı Eleştirisi](#2-domain-model--i̇ş-akışı-eleştirisi)
3. [Planning Engine Tasarımı](#3-planning-engine-tasarımı)
4. [Teknik Borç & Ölçeklenebilirlik](#4-teknik-borç--ölçeklenebilirlik)
5. [Ürün Stratejisi Eleştirisi](#5-ürün-stratejisi-eleştirisi)
6. [En Kritik 10 Geliştirme](#6-en-kritik-10-geliştirme)

---

## 1. OPERASYONEL DOĞRULUK

### 1.1 Stok Bütünlüğü: Gerçek Durum

Stok rezervasyon mekanizması var ve mantık doğru kurulmuş. Ancak implementasyonda üç ciddi açık var:

#### A — Negatif OnHandQty Koruması Yok

`AssignVehicleCommandHandler` içinde:

```csharp
stock.OnHandQty -= deliveredQty;
stock.ReservedQty -= line.OrderedQty;
```

`OnHandQty`'nin negatife düşmesini engelleyen hiçbir kontrol yok. Şu senaryo gerçekleşebilir:

1. Depoda 10 kutu stok var.
2. Şoför A için 8 kutu rezerve edildi, şoför B için 5 kutu rezerve edildi → toplam rezervasyon 13, available -3.
3. Sistem bunu `AssignToWarehouse` anında engellemeli: `AvailableQty >= line.OrderedQty` kontrolü **yok**.
4. İki araç aynı anda yüklenirse: her ikisi de `OnHandQty -= ...` yapar, biri -3 ile biter.

**Fix — Domain Level:**

```csharp
// Domain service veya AssignVehicleCommandHandler içinde:
if (stock.OnHandQty < deliveredQty)
    throw new DomainException(
        $"{stock.StockCode}: Yeterli stok yok. " +
        $"Mevcut: {stock.OnHandQty}, İstenen: {deliveredQty}");

stock.OnHandQty -= deliveredQty;
stock.ReservedQty = Math.Max(0, stock.ReservedQty - line.OrderedQty);
```

#### B — Non-Atomic Stok Güncellemesi (Race Condition)

Birden fazla shipment aynı anda `AssignVehicle` alırsa, EF Core her biri için `stock.OnHandQty` değerini context'e yükler (read), modifiye eder, kaydeder. İki thread aynı değeri okursa stok yanlış hesaplanır.

**Fix — SQL-level Atomic UPDATE:**

```csharp
// Raw SQL ile atomic update — hem güvenli hem hızlı:
var affected = await _context.Database.ExecuteSqlRawAsync(
    @"UPDATE StockMasters
      SET OnHandQty    = OnHandQty    - {0},
          ReservedQty  = ReservedQty  - {1}
      WHERE Id = {2}
        AND OnHandQty >= {0}",
    deliveredQty, orderedQty, stock.Id);

if (affected == 0)
    throw new DomainException($"{stock.StockCode}: Yeterli stok yok.");
```

#### C — ReservedQty Double-Reserve Riski

`AssignToWarehouse` handler'ı ağ timeout'u nedeniyle iki kez çalışırsa, aynı shipment için stok iki kez rezerve edilebilir. Handler başında status kontrolü var ama idempotency garantisi zayıf.

**Fix:**

```csharp
// AssignToWarehouseCommandHandler başında:
if (shipment.Status != ShipmentStatus.Created)
    return; // Idempotent — zaten işlenmiş, tekrar rezervasyon yapma
```

---

### 1.2 Eksik Teslimat Kontrolü

`MarkShipmentDeliveredCommandHandler` içinde teslimat kanıtı için **hiçbir validation yok:**

```csharp
// Mevcut kod — her şey nullable, hiçbir şey zorunlu değil:
shipment.DeliveredAt        = DateTime.UtcNow;
shipment.DeliveryNote       = request.DeliveryNote;           // nullable
shipment.DeliveryRecipient  = request.DeliveryRecipient;      // nullable
shipment.DeliveryPhotoBase64 = request.DeliveryPhotoBase64;   // nullable
```

Bu şu anlama geliyor: boş alıcı adı, açıklama yok, fotoğraf yok → sistem "teslim edildi" diyor. Gerçek hayatta müşteri "teslim almadım" derse elinizde hiçbir kanıt yok.

Ayrıca eksik teslimat (bazı satırlar teslim edilmedi) kontrolü yok. `DeliveredQty = 0` olan satırlar varken `MarkShipmentDelivered` çalışabiliyor.

**Fix — Domain + Application Level:**

```csharp
// MarkShipmentDeliveredCommandHandler içinde:

// 1. Alıcı adı zorunlu
if (string.IsNullOrWhiteSpace(request.DeliveryRecipient))
    throw new DomainException("Teslim alan kişi adı zorunludur.");

// 2. Açıklanmamış eksik satır kontrolü
var unexplainedLines = shipment.Lines
    .Where(l => l.OrderedQty > 0
             && l.DeliveredQty == 0
             && string.IsNullOrEmpty(l.DifferenceReason))
    .ToList();

if (unexplainedLines.Any())
    throw new DomainException(
        $"{unexplainedLines.Count} kalem teslim edilmedi ve açıklama girilmedi. " +
        "Satırları güncelleyin.");

// 3. Kısmi teslimat → not zorunlu
bool isPartial = shipment.Lines.Any(l => l.DeliveredQty < l.OrderedQty);
if (isPartial && string.IsNullOrWhiteSpace(request.DeliveryNote))
    throw new DomainException("Kısmi teslimat için açıklama zorunludur.");
```

---

### 1.3 Netsis'e Yanlış Veri Gitmesini Engelleyen Mekanizmalar

Mevcut koruma: `IrsaliyeFetched` flag'i ve `ReadyForDriverInfo` durum kontrolü. Yaklaşım doğru ama üç boşluk var:

**A — İrsaliye numarası format validasyonu yok.**
`UpdateIrsaliyeNo` command'i sadece string alıyor. Netsis irsaliye formatı neyse regex ile validate edilmeli.

**B — Double export riski.**
`FetchZoneIrsaliye` her çağrıda tüm shipment'ları export etmeye çalışıyor. Kısmi başarı durumunda ikinci çağrı zaten aktarılanları tekrar gönderir.

```csharp
// FetchZoneIrsaliyeCommandHandler içinde:
var unExported = shipments
    .Where(s => s.NetsisTransferredAt == null)  // Sadece aktarılmayanlar
    .ToList();
```

**C — Partial failure sessiz geçiyor.**
Handler errors listesi dolduruyor ama kullanıcı "irsaliye çekildi" sanıp araç atayabiliyor. Hangi shipment'ın aktarılamadığı frontend'de açıkça gösterilmeli.

---

## 2. DOMAIN MODEL & İŞ AKIŞI ELEŞTİRİSİ

### 2.1 Yapısal Sorunlar

**Shipment ile ZonePreparation bağı gevşek ve kırılgandır.**

`Shipment.ZonePreparationId` nullable. `ZonePreparationProject` proje bazlı çalışıyor — bir projenin tüm shipment'larını kapsadığı varsayılıyor ama bu explicitly enforce edilmiyor. Bir proje o gün 3 farklı sipariş taşıyorsa, hangi shipment'ların bu hazırlığa dahil olduğu muğlak.

**Stok çıkışının timing'i tartışmalıdır.**

Stok `AssignVehicle` (araç yükleme) anında düşülüyor, teslim anında değil. Bu şu problemi yaratıyor:

- Araç çıktı, teslim yapamadı (yol kazası, proje kapalı), geri döndü.
- `RecordVehicleReturn` ile stok geri alınıyor.
- Aradaki süre boyunca stok kayıtlarda sıfır görünüyor.
- Bu süre içinde başka sipariş gelirse "stok yok" sinyali verebilir.

> Bu yaklaşım pragmatik ve savunulabilir — ama hiçbir yerde belgelenmemiş. Yeni geliştirici bu mantığı anlayamaz.

**ZonePreparation BatchNo anlamsız bir sayaç.**

Ankara-1, Ankara-2 gibi bölme mantığını temsil etmesi gerekiyor ama bu otomatik uygulanmıyor. Planning Engine olmadan batch kavramı dekoratif kalıyor.

### 2.2 Tehlikeli State Transition'lar

| Geçiş | Risk | Açıklama |
|-------|------|----------|
| `AssignedToVehicle → Delivered` | Orta | Şoför GPS onayı veya dış doğrulama olmadan direkt geçiş yapılabiliyor |
| `Created → Cancelled` | Yüksek | Stok rezervasyonu serbest bırakılıyor mu? Cancel handler'da kontrol edilmeli |
| `ReadyForDispatch → AssignedToVehicle` | Orta | Bölgedeki bir shipment iptal edilirse diğerleri zaten vehicle'a atanıyor; iptal edilen shipment akıştan temiz çıkmıyor |

### 2.3 Eksik Business Rule'lar

```
1. Minimum stok uyarısı tetikleyicisi yok.
   MinStockQty alanı var ama hiçbir handler kontrol etmiyor.
   "Ekmek stoğu kritik seviyeye düştü" bildirimi sisteme yoktur.

2. Shipment deadline kontrolü yok.
   DeliveryDate geçmiş bir shipment için sistem uyarı vermiyor.
   "Bugün teslim edilmesi gereken ama edilmemiş" sorgusu yok.

3. Aynı projeye aynı gün çift shipment koruması yok.
   Kullanıcı hatasıyla proje A için iki sevkiyat oluşturulabilir,
   sistem bunu engellemez.

4. GoodsReceipt Post → stok artışı doğrulanmamış.
   PostGoodsReceiptCommandHandler'da OnHandQty güncelleniyor mu?
   Eğer sadece status değişiyorsa, satınalma modülü stokla entegre değildir.
```

---

## 3. PLANNING ENGINE TASARIMI

### 3.1 Neden Gerekli

Şu an planlama tamamen insan kararına bırakılmış:
- Hangi araç hangi bölgeye gidecek → elle seçim
- Araç dolup dolmayacağı → bilinmiyor
- Aynı bölgenin 2 araca bölünmesi gerekip gerekmediği → manuel tahmin

15+ şoför, 50+ günlük teslimat noktasına ulaşıldığında bu yapı kırılır.

### 3.2 Gerekli Domain Değişiklikleri

```csharp
// Vehicle.cs — sayısal kapasite ekle:
public int?     MaxPallets    { get; set; }
public decimal? MaxWeightKg   { get; set; }
public string?  Capacity      { get; set; }  // Mevcut metin açıklama korunur

// ShipmentLine.cs — fiziksel boyut (opsiyonel, sonraki adım):
public int?     PalletCount   { get; set; }
public decimal? WeightKg      { get; set; }

// Yeni Entity: PlanningBatch.cs
public class PlanningBatch
{
    public int              Id              { get; set; }
    public int              ZoneId          { get; set; }
    public DateOnly         DeliveryDate    { get; set; }
    public int              VehicleId       { get; set; }
    public int?             DriverId        { get; set; }
    public int              BatchSequence   { get; set; }   // 1, 2, 3...
    public PlanningStatus   Status          { get; set; }

    public Zone                         Zone    { get; set; } = null!;
    public Vehicle                      Vehicle { get; set; } = null!;
    public ICollection<PlanningBatchStop> Stops  { get; set; } = new List<PlanningBatchStop>();
}

public class PlanningBatchStop
{
    public int PlanningBatchId { get; set; }
    public int ProjectId       { get; set; }
    public int StopOrder       { get; set; }
}
```

### 3.3 Planlama Algoritması

**Input:**
- Seçilen tarih için tüm `ReadyForDispatch` shipment'lar (bölge bazlı)
- Projelerin `DeliveryOrder` değerleri (sürükle-bırak ile belirlenen)
- Mevcut araçların `MaxPallets` değerleri

**Greedy Bin Packing — Pseudo-code:**

```python
def plan_zone(zone_id, delivery_date, available_vehicles):

    # 1. Shipment'ları proje bazlı grupla
    projects = group_shipments_by_project(
        get_ready_shipments(zone_id, delivery_date)
    )
    # Her proje: toplam palet, ağırlık, shipment listesi

    # 2. DeliveryOrder'a göre sırala (null = sona)
    projects.sort(key=lambda p: p.delivery_order or 9999)

    # 3. Araçlara dağıt — büyük araç önce (Greedy First Fit Decreasing)
    batches = []
    for vehicle in sorted(available_vehicles, key=lambda v: v.max_pallets, reverse=True):
        batch = PlanningBatch(vehicle=vehicle, stops=[], sequence=len(batches)+1)
        remaining = vehicle.max_pallets

        for project in projects:
            if project.allocated:
                continue
            if project.pallet_count <= remaining:
                batch.stops.append(project)
                remaining -= project.pallet_count
                project.allocated = True

        if batch.stops:
            batches.append(batch)

    # 4. Sığmayan projeler → uyarı
    unallocated = [p for p in projects if not p.allocated]

    return PlanningResult(
        batches=batches,
        unallocated=unallocated,
        warnings=generate_warnings(batches)
    )

def generate_warnings(batches):
    warnings = []
    for batch in batches:
        utilization = batch.used_pallets / batch.vehicle.max_pallets
        if utilization > 0.95:
            warnings.append(f"{batch.vehicle.plate}: %{utilization*100:.0f} dolu — risk")
        if len(batch.stops) > 12:
            warnings.append(f"{batch.vehicle.plate}: {len(batch.stops)} durak fazla olabilir")
    return warnings
```

**Günlük kapasite tahmini:**

```python
def estimate_daily_capacity(zone_id, date):
    avg_stop_minutes   = 30    # Konfigürasyondan alınabilir
    working_hours      = 10
    max_stops_per_vehicle = (working_hours * 60) / avg_stop_minutes  # = 20

    vehicles       = get_available_vehicles(date)
    total_capacity = sum(
        min(v.max_pallets or 20, max_stops_per_vehicle)
        for v in vehicles
    )
    pending = get_pending_projects(zone_id, date)

    return {
        "pending_projects":   len(pending),
        "vehicle_capacity":   total_capacity,
        "can_complete_today": total_capacity >= len(pending),
        "overflow_count":     max(0, len(pending) - total_capacity)
    }
```

### 3.4 Domain Entegrasyonu

```
Yeni MediatR Command: CreatePlanningBatchesCommand
  Input:  ZoneId, DeliveryDate, VehicleIds[]
  Output: List<PlanningBatchDto> (öneri, henüz kaydedilmez)

Kullanıcı onaylarsa → ConfirmPlanningCommand
  → Her batch için ZonePreparation (BatchNo otomatik) oluşturur
  → BatchNo = batch sequence numarası

Yeni Frontend Sayfası: /zones/planning
  - Tarih + araç seçimi
  - Otomatik dağıtım önerisi
  - Sürükle-bırak manuel düzenleme
  - "Bu planı onayla" → ZonePrep oluşturur
```

---

## 4. TEKNİK BORÇ & ÖLÇEKLENEBİLİRLİK

### 4.1 Test Olmaması

**Şu an sorun değil:** Ekip küçük, değişiklikler elle test ediliyor, hacim düşük.

**Şu noktada patlayacak:** İlk ciddi stok refactor'ında. `AssignToWarehouse → AssignVehicle → RevertToDraft` zinciri 4 farklı handler'ı etkiliyor. Bunlardan birini değiştirince diğerleri sessizce kırılabilir.

**Minimum viable test stratejisi:**

```
1. Domain entity testleri (saf C#, hızlı, bağımsız):
   - Shipment state machine — tüm geçişler
   - ShipmentLine.SetDeliveredQty edge case'leri
   - StockMaster negatif qty koruması (fix'ten sonra)

2. Application handler integration testleri (test DB):
   - AssignToWarehouse → ReservedQty doğru artıyor mu?
   - AssignVehicle → OnHandQty doğru düşüyor mu?
   - RevertToDraft → stok serbest bırakma doğru mu?

Bunlar olmadan stok mantığına dokunmak kör ameliyattır.
```

### 4.2 API Versioning Olmaması

**Şu an sorun değil:** Frontend ve backend aynı repo'da, birlikte deploy ediliyor.

**Şu noktada patlayacak:** PWA cache eski versiyon çalışırken backend yeni bir alan ekler veya kaldırırsa. `autoUpdate` var ama sadece sayfa yenilendiğinde çalışır.

> Şoför sabah uygulamayı açtı → gün içinde deploy yapıldı → şoför uygulamayı kapatmadı → 10 saat eski kodla çalıştı → yeni field'lar eksik gönderildi → sessiz hata.

**Minimum fix:** `/api/v1/` prefix + backend'de eski ve yeni versiyonu birlikte tutma planı.

### 4.3 Rate Limiting Eksikliği

**Şu an sorun değil:** Dahili kullanım, kısıtlı kullanıcı sayısı.

**Şu noktada patlayacak:** `POST /api/orders/import` her çağrıda ISS-IP'ye N×HTTP isteği atıyor. Kullanıcı sabırsızlığıyla 5 dakikada 10 kez tıklarsa ISS-IP servisi throttle yapar → tüm import kırılır.

**Fix:**

```csharp
// Program.cs:
builder.Services.AddRateLimiter(options => {
    options.AddFixedWindowLimiter("external-api", opt => {
        opt.Window             = TimeSpan.FromSeconds(30);
        opt.PermitLimit        = 1;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

// Controller:
[HttpPost("import")]
[RateLimiting(policyName: "external-api")]
public async Task<IActionResult> ImportOrders(...)
```

### 4.4 Base64 Fotoğraf Storage

**Şu an sorun değil:** Teslimat fotoğrafı zorunlu değil, az kullanılıyor.

**Şu noktada patlayacak:** Günde 50 teslimat fotoğrafı = ~10MB/gün DB büyümesi. Yılda 3.6GB sadece fotoğraf. Daha kritik: bazı query handler'lar tüm Shipment alanlarını SELECT ediyor — `DeliveryPhotoBase64` 200KB'lık string her satırda dashboard'a geliyor olabilir.

**Kademeli fix:**

```csharp
// Adım 1 (hemen): Tüm list query'lerinde fotoğrafı SELECT dışında bırak
.Select(s => new ShipmentListDto {
    Id             = s.Id,
    // DeliveryPhotoBase64 = s.DeliveryPhotoBase64  ← BU SATIRI YAZMA
})

// Adım 2: Ayrı endpoint
// GET /api/shipments/{id}/delivery-photo

// Adım 3 (uzun vade): Azure Blob Storage veya MinIO
```

---

## 5. ÜRÜN STRATEJİSİ ELEŞTİRİSİ

### 5.1 Dürüst Değerlendirme

Bu sistem aynı anda WMS + TMS + ERP entegrasyonu + Satınalma + Mobil uygulama yapmaya çalışıyor. Her modülü yarım bitirmek, hiçbirini tam bitirmekten kötüdür.

Şu andaki durum:

| Modül | Olgunluk | Sorun |
|-------|----------|-------|
| Sevkiyat akışı | ✅ İyi | Teslimat kanıtı zorunlu değil |
| Depo hazırlık | ✅ İyi | Planning yok, batch manuel |
| Şoför paneli | ✅ İyi | Offline çalışmıyor |
| Stok yönetimi | ⚠️ Orta | Negatif qty riski, min stok uyarısı yok |
| WMS (depo adresleme) | ⚠️ Yeni | Henüz olgunlaşmamış |
| Satınalma | ❓ Belirsiz | Stok akışıyla entegre mi? |
| Raporlama | ⚠️ Temel | Analitik yok, gecikme takibi yok |

### 5.2 Core Ürün Ne Olmalı?

Bu sistemin asıl problemi şudur: **Sabah sipariş gelir, öğleden önce araç çıkar, akşam teslim edilir. Bu döngüyü güvenilir şekilde yönetmek.**

```
CORE — Her şeyden önce mükemmel olmalı:
  1. ISS-IP → Shipment oluşturma akışı
  2. Depo hazırlık (picking) akışı
  3. Şoför teslimat akışı (mobil, offline)
  4. Teslimat kanıtı ve eksik teslimat yönetimi
  5. Netsis irsaliye entegrasyonu

SECONDARY — Çalışıyor ama derinleştirilmeli:
  6. Stok yönetimi (atomic işlemler + uyarılar)
  7. Bölge/proje yönetimi + Planning Engine

TERTIARY — Sonra, veya kapsam dışına alınabilir:
  8. Satınalma modülü (ISS ile entegre değil, hangi problemi çözüyor?)
  9. WMS depo adresleme (kim kullanıyor, hacim ne zaman gerektirir?)
  10. Gelişmiş raporlama
```

### 5.3 Satınalma Modülü Hakkında Net Yorum

Satınalma modülü şu an stok akışına entegre değil. `GoodsReceipt Post` edilince `OnHandQty` artıyor mu doğrulanmamış. Tedarikçi yönetimi ISS-IP ile konuşmuyor. Bu modülün iş gereksinimi net değilse, geliştirme kapasitesini tüketmemeli.

**Karar:** Ya tam entegre et (`GoodsReceipt → OnHandQty`, `PO → stok planlaması`) ya da şimdilik dondur.

---

## 6. EN KRİTİK 10 GELİŞTİRME

Öncelik sırası: operasyonel risk azaltma → güvenilirlik → büyüme.

---

### #1 — Stok Negatif Koruması ve Atomic Güncelleme
**Dosyalar:** `AssignToWarehouseCommandHandler.cs`, `AssignVehicleCommandHandler.cs`
**Risk:** Stok negatife düşerse muhasebe ve Netsis verileri kirli olur. Geri dönüşü zordur.
**Ne yapılacak:**
- `AssignToWarehouse`'da: `AvailableQty >= line.OrderedQty` kontrolü
- `AssignVehicle`'da: SQL atomic UPDATE + `OnHandQty >= deliveredQty` garantisi

---

### #2 — Teslimat Zorunlu Alanları
**Dosya:** `MarkShipmentDeliveredCommandHandler.cs`
**Risk:** Teslim edildi deyip kanıt olmayan her sipariş müşteri itirazına açık.
**Ne yapılacak:**
- `DeliveryRecipient` zorunlu
- Açıklanmamış eksik satır varsa hata
- Kısmi teslimat → zorunlu not

---

### #3 — GoodsReceipt Post → Stok Güncellemesi Doğrulaması
**Dosya:** `PostGoodsReceiptCommandHandler.cs`
**Risk:** Satınalma modülü stokla entegre değilse tamamen dekoratif.
**Ne yapılacak:** Handler'da her satır için `OnHandQty += acceptedQty` ve `StockTransaction.GoodsIn` oluşturulduğunu doğrula/uygula.

---

### #4 — MinStockQty Uyarı Mekanizması
**Dosya:** `AssignVehicleCommandHandler.cs` (tetikleme noktası)
**Risk:** Stok bitmesi ancak fiziksel sayımla fark ediliyor.
**Ne yapılacak:** `AssignVehicle` sonrası `OnHandQty < MinStockQty` olan stokları in-app bildirim olarak yöneticiye ilet.

---

### #5 — Planning Engine MVP
**Yeni dosyalar:** `CreatePlanningBatchesCommand`, `Vehicle.MaxPallets`, `/zones/planning` sayfası
**Risk:** 3+ araçlı operasyonda günlük plan elle yapılıyor, hata oranı yüksek.
**Ne yapılacak:** Bölüm 3'te tanımlanan Greedy Bin Packing algoritması + araç kapasite alanları + frontend planlama ekranı.

---

### #6 — Domain Testleri (Shipment SM + Stok Mantığı)
**Neden:** #1 ve #3 fix'leri için stok mantığına dokunulacak. Test yoksa bu değişiklikler kör.
**Ne yapılacak:**
- Shipment state machine — tüm geçişler
- `SetDeliveredQty` edge case'leri
- Negatif qty koruması (fix'ten sonra doğrulama)

---

### #7 — Geciken Teslimat Takibi
**Yeni dosya:** `GetOverdueShipmentsQuery`
**Risk:** `DeliveryDate` geçmiş ama hâlâ `AssignedToWarehouse` durumunda olan shipment'lar sessizce birikebilir.
**Ne yapılacak:** Dashboard'a "Geciken Sevkiyatlar" widget'ı. Opsiyonel: günlük özet bildirimi.

---

### #8 — Fotoğraf Storage Ayrışması
**Dosyalar:** Tüm list query handler'ları
**Risk:** Büyüyen DB boyutu, yavaşlayan sorgular.
**Ne yapılacak:**
1. List query'lerden `DeliveryPhotoBase64` çıkar (hemen)
2. `GET /api/shipments/{id}/delivery-photo` ayrı endpoint (kısa vade)
3. Blob storage (uzun vade)

---

### #9 — Rate Limiting (Dış Servis Endpoint'leri)
**Dosyalar:** `OrdersController`, `NetsisController`
**Risk:** ISS-IP veya Netsis servisi throttle yaparsa tüm import/irsaliye akışı kırılır.
**Ne yapılacak:** `/api/orders/import` ve `/api/netsis/*` için per-user 1 req/30sn rate limiting. Frontend'de çift tıklama koruması.

---

### #10 — PWA Offline Güvenilirliği
**Dosyalar:** `vite.config.ts`, `DriverStopView.vue`
**Risk:** Şoför sahada bağlantısız kalırsa hangi sayfalar çalışır belirsiz.
**Ne yapılacak:**
- Şoför paneli (`/driver/*`) sayfaları için offline-first strateji
- Günlük rota Workbox'a cache'lenir
- Teslimat formu offline çalışır, bağlantı gelince senkronize edilir
- Kare 512×512 PWA icon

---

## ÖZET TABLO

| # | Geliştirme | Aciliyet | Etki | Efor |
|---|-----------|----------|------|------|
| 1 | Stok negatif koruması + atomic update | 🔴 Kritik | Yüksek | Orta |
| 2 | Teslimat zorunlu alanları | 🔴 Kritik | Yüksek | Düşük |
| 3 | GoodsReceipt → stok doğrulaması | 🔴 Kritik | Yüksek | Düşük |
| 4 | MinStockQty uyarı mekanizması | 🟠 Önemli | Orta | Düşük |
| 5 | Planning Engine MVP | 🟠 Önemli | Yüksek | Yüksek |
| 6 | Domain testleri | 🟠 Önemli | Yüksek | Orta |
| 7 | Geciken teslimat takibi | 🟡 Faydalı | Orta | Düşük |
| 8 | Fotoğraf storage ayrışması | 🟡 Faydalı | Orta | Düşük |
| 9 | Rate limiting | 🟡 Faydalı | Orta | Düşük |
| 10 | PWA offline güvenilirliği | 🟡 Faydalı | Orta | Yüksek |

---

*Bu döküman `ARCHITECTURE_REVIEW.md` olarak proje kök dizinine kaydedilmiştir.*
