# ISS-IP Import Güvenilirlik Analizi

> **Tarih:** 2026-03-24
> **Kapsam:** ImportIssOrdersCommand → IssOrder → CreateShipment zinciri
> **Temel dosyalar:**
> - `Application/Orders/Commands/ImportIssOrders/ImportIssOrdersCommand.cs`
> - `Application/Shipments/Commands/CreateShipmentCommandHandler.cs`
> - `Infrastructure/ExternalServices/ISSIpClient.cs`
> - `Domain/Entities/IssOrder.cs`

---

## MEVCUT KODUN GERÇEK DURUMU

Analiz başlamadan önce net olalım: bu kod **çalışıyor** ama kırılgan. Aşağıdaki sorunların tamamı kod okunarak tespit edilmiştir, tahmin değil.

---

## 1. IMPORT GÜVENİLİRLİĞİ

### 1.1 Aynı Sipariş İkinci Kez Import Edilirse Ne Olur?

Mevcut kod `ExternalOrderNumber` üzerinde existence check yapıyor:

```csharp
var existingOrder = await _context.IssOrders
    .Include(o => o.Project)
    .FirstOrDefaultAsync(o => o.ExternalOrderNumber == header.SiparisNo, cancellationToken);
```

`existingOrder != null` ise header alanları güncelleniyor ve `continue` ile geçiliyor. Bu doğru bir self-healing yaklaşımı gibi görünüyor. Ama şu satır kritik:

```csharp
// Continue to next order, lines are complex to update (diffing),
// usually order header update is enough for metadata.
continue;
```

**Bu yorum aslında doğru bir iş kararı, ama yanlış gerekçelendirilmiş.**

ISS-IP iş akışı: Talep oluşturulur → Yetkili onaylar → Bize sipariş düşer. **Onaylı bir sipariş ISS-IP'de değiştirilemez.** Satırlar, miktarlar, tarihler — hiçbiri değişmez. Sadece onay tamamen geri çekilebilir (sipariş silinir). Bu nedenle mevcut siparişin satırlarını diff etmek gereksizdir; ISS-IP onaylı siparişi immutable tutar.

> **Not:** Aynı talep geri çekilip yeniden onaylanırsa **yeni bir sipariş numarasıyla** gelir. Bu son derece nadir (10 yılda ~5 kez) ve özel bir mekanizma gerektirmez.

### 1.2 Duplicate Kayıt Riski

`ExternalOrderNumber` üzerinde **unique index yok.** DB constraint yok.

Şu senaryo gerçekleşebilir:

1. İmport başladı, `SaveChangesAsync` henüz çağrılmadı.
2. Aynı anda ikinci bir import çağrısı yapıldı (kullanıcı çift tıkladı, network timeout'unda tekrar denedi).
3. İkinci çağrı da `FirstOrDefaultAsync` ile kontrol yaptı — DB'de kayıt yok çünkü ilk çağrı henüz kaydetmedi.
4. İkinci çağrı da `_context.IssOrders.Add(newOrder)` yaptı.
5. Her iki çağrı da `SaveChangesAsync` yaptı → aynı `ExternalOrderNumber` ile iki kayıt.

**Sonuç:** Aynı sipariş için iki `IssOrder`, iki `Shipment`, iki kez stok rezervasyonu.

### 1.3 Tek Sipariş Hatası Tüm Import'u Patlatır

```csharp
public async Task<int> Handle(ImportIssOrdersCommand request, CancellationToken cancellationToken)
{
    try
    {
        foreach (var header in orderList)
        {
            // ... her sipariş işlenir
        }
        await _context.SaveChangesAsync(cancellationToken);  // TEK NOKTA
        return importedCount;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "GLOBAL HANDLER ERROR");
        throw;  // HER ŞEY GERİ SARILIR
    }
}
```

100 sipariş import ediliyor. 99. siparişte ISS-IP'den geçersiz JSON geldi. `throw` çalıştı. 98 başarılı sipariş de kaydedilmedi. Kullanıcı tekrar çalıştırdığında bu 98'i de tekrar işler — her biri için ISS-IP'ye ayrı HTTP isteği.

### 1.4 Her Mevcut Sipariş İçin Detail HTTP Çağrısı

```csharp
// Check existence
var existingOrder = await _context.IssOrders...FirstOrDefaultAsync(...);

// Proceed to fetch detail even if exists (Self-Healing)
var detailEnvelope = await _issClient.GetSiparisAsync(header.SiparisNo, cancellationToken);
```

Yorum "Self-Healing" diyor ama yaptığı şey: mevcut siparişler dahil HER sipariş için ISS-IP'ye detail çağrısı. 200 siparişin 180'i zaten DB'de varsa, 180 gereksiz HTTP isteği atılıyor. ISS-IP yavaşlarsa import dakikalarca sürer.

**ISS-IP immutability'si bu sorunu daha da ciddi kılıyor:** Onaylı sipariş değişemediği için mevcut siparişlerde detail çağrısından dönen veri zaten DB'deki ile aynı olacak. Bu çağrıların tamamı sıfır değer üretiyor, sadece ISS-IP'ye yük bindiriyor.

### 1.5 Doğru İdempotent Import Tasarımı

ISS-IP'nin iş kuralları netleştikten sonra doğru tablo:

| Durum | Eylem | Gerekçe |
|-------|-------|---------|
| `ExternalOrderNumber` yok → yeni sipariş | INSERT + detail çağrısı | Normal akış |
| `ExternalOrderNumber` var + `IsTransferred = false` | **SKIP** — detail çağrısı da yok | Sipariş immutable, veri değişmez |
| `ExternalOrderNumber` var + `IsTransferred = true` | **SKIP** | Sevkiyata gitti, dokunma |
| `ExternalOrderNumber` var + `IsActive = false` | **SKIP** | Pasif edilmiş |

**Line Diff gerekli değil.** ISS-IP onaylı siparişi değiştiremiyor. Aynı `ExternalOrderNumber` tekrar gelirse DB'deki veriyle tamamen aynıdır — ne header ne satır değişmiş olabilir. Mevcut sipariş için detail API çağrısı yapılmamalı.

> Bu değişiklik aynı zamanda 1.4'teki N+1 API çağrı sorununu da çözer.

### 1.6 Revocation Gap — İptal Edilen Sipariş Sistemde Kalıyor

ISS-IP'de bir sipariş geri çekildiğinde (onay iptali):
- ISS-IP'de sipariş **silinir** — bir sonraki import listesinde artık **görünmez**
- Bizim DB'mizde `IssOrder` kaydı `ImportStatus.Ready` veya `NeedsMapping` olarak **yaşamaya devam eder**
- Bu sipariş `CreateShipment` ile sevkiyata dönüştürülebilir — ISS-IP'de artık var olmayan bir iş emri için

**Şu an bu durumu yakalamak için hiçbir mekanizma yok.**

Pratik önem:
- Nadir olmayan bir senaryo. Yetkili herhangi bir zamanda siparişi geri çekebilir.
- Sevkiyat hazırlanıp yola çıktıktan sonra fark edilirse ciddi operasyonel sorun.
- Sistem "ISS-IP'de onaylı olmayan sipariş sevk edildi" durumunu kayıt altına almıyor.

**Çözüm yaklaşımı (öneri):**

Import sırasında ISS-IP'den gelen liste ile DB'deki aktif siparişleri karşılaştır:

```csharp
// Import sonunda: ISS'te artık olmayan ama bizde hâlâ aktif olan siparişleri bul
var incomingNumbers = new HashSet<string>(orderList.Select(o => o.SiparisNo));

var orphanedOrders = await _context.IssOrders
    .Where(o => !o.IsTransferred && o.IsActive
                && !incomingNumbers.Contains(o.ExternalOrderNumber))
    .ToListAsync(cancellationToken);

foreach (var orphan in orphanedOrders)
{
    orphan.IsActive = false;
    orphan.CancelledAt = DateTime.UtcNow;
    _logger.LogWarning("Order {OrderNo} no longer in ISS-IP list — marking cancelled", orphan.ExternalOrderNumber);
}
```

> Bu öneri import'un "tam liste" döndürdüğünü varsayar. ISS-IP yalnızca son N günü mi yoksa tüm aktif siparişleri mi döndürüyor? Bu iş kuralı netleştirilmeden implementasyon yapılmamalı.

**Unique Index Ekle:**

```csharp
// SevkiyatDbContext.OnModelCreating:
modelBuilder.Entity<IssOrder>()
    .HasIndex(o => o.ExternalOrderNumber)
    .IsUnique();
```

---

## 2. PAYLOAD / PARSING GÜVENLİĞİ

### 2.1 Çift Serialize JSON — Gerçek Durum

```csharp
private JsonElement GetRefinedRoot(JsonElement root)
{
    if (TryGetProperty(root, out var resProp, "result", "Result")
        && resProp.ValueKind == JsonValueKind.String)
    {
        try
        {
            var doc = JsonDocument.Parse(resProp.GetString());
            return doc.RootElement;
        }
        catch
        {
            // Fallback to original root if parsing fails — SESsizCE geçiyor
        }
    }
    return root;
}
```

Parse hatası `catch { }` ile yutularak orijinal root döndürülüyor. Eğer inner JSON bozuksa sistem devam eder ama yanlış yapıyı parse etmeye çalışır. Bu hata loglanmıyor bile.

### 2.2 Property Key Spekülatif Arama

```csharp
if (TryGetProperty(detailRoot, out var table1, "Table1", "Lines", "Satirlar", "lines"))
```

ISS-IP 4 farklı key ismiyle satır döndürebiliyor ve bunların hepsi deneniyor. Bu defensif programlama değil, **ISS API'sinin tutarsız olduğunun kabulü.** Yarın ISS "Lines2" diye yeni bir key kullanırsa satırlar sessizce boş gelir, sipariş `0 satır` ile kaydedilir.

### 2.3 Satırsız Sipariş Sessizce Kaydediliyor

```csharp
if (detail.Lines == null) detail.Lines = new List<IssSiparisLineDto>();

// ... Lines üzerinde dön, her şey normal

newOrder.ImportStatus = hasUnmapped ? ImportStatus.NeedsMapping : ImportStatus.Ready;
_context.IssOrders.Add(newOrder);
```

`detail.Lines` boş geldiyse (key bulunamadı, parse hatası, ISS yanıt değişti), sipariş `0 satır` ile `ImportStatus.Ready` olarak kaydedilir. Bu siparişten sonra shipment oluşturulabilir — içi boş bir sevkiyat.

### 2.4 Tarih Parse Fallback Sessiz Sorun Yaratır

```csharp
DateTime orderDate    = ParseDate(detail.Header?.Tarih ?? header.Tarih) ?? DateTime.Now;
DateTime deliveryDate = ParseDate(detail.Header?.TeslimTarihi ?? header.TeslimTarihi) ?? DateTime.Now.AddDays(1);
```

ISS'ten tarih gelmezse veya parse edilemezse `DateTime.Now` kullanılıyor. Bu sipariş bugün teslim edilecekmiş gibi görünür. Kullanıcı fark etmeyebilir, sipariş yanlış günde planlama akışına girer.

### 2.5 Ham Response Hiç Saklanmıyor

`ISSIpClient.PostAsync` response body'yi sadece parse etmek için okuyor:

```csharp
var json = await resp.Content.ReadAsStringAsync(ct);
var doc = JsonDocument.Parse(json);
return new ISSIpEnvelope { Root = doc.RootElement.Clone() };
```

Bir parse hatası oluştuğunda ham JSON'a erişim yok. Ne geldiğini göremezsiniz. Debug imkansız.

### 2.6 Güvenli Parsing Tasarımı

```csharp
// ISSIpClient.PostAsync içine ekle:
var rawJson = await resp.Content.ReadAsStringAsync(ct);

// Ham response'u loglama için sakla
_logger.LogDebug("ISS-IP Raw Response [{Method}] ({Bytes} bytes): {Preview}",
    methodName,
    rawJson.Length,
    rawJson[..Math.Min(500, rawJson.Length)]);

// Parse hatası durumunda ham içeriği de logla
try
{
    var doc = JsonDocument.Parse(rawJson);
    return new ISSIpEnvelope { Root = doc.RootElement.Clone(), RawJson = rawJson };
}
catch (JsonException ex)
{
    _logger.LogError(ex, "ISS-IP JSON parse failed [{Method}]. Raw: {Raw}",
        methodName, rawJson[..Math.Min(1000, rawJson.Length)]);
    throw new DomainException($"ISS-IP yanıtı geçersiz JSON [{methodName}]: {ex.Message}");
}
```

```csharp
// ISSIpEnvelope'a RawJson ekle:
public class ISSIpEnvelope
{
    public JsonElement Root   { get; set; }
    public string     RawJson { get; set; } = "";  // ← ekle
}
```

**Satırsız sipariş koruması:**

```csharp
if (!detail.Lines.Any())
{
    _logger.LogWarning(
        "Order {OrderNumber} has no lines. Skipping. Raw detail: {Raw}",
        header.SiparisNo,
        detailEnvelope.RawJson[..Math.Min(500, detailEnvelope.RawJson.Length)]);

    result.AddWarning(header.SiparisNo, "Sipariş satırı bulunamadı, atlandı.");
    continue;
}
```

---

## 3. ZORUNLU ALAN DOĞRULAMA

### 3.1 Mevcut Durum

Şu an yalnızca şu kontrol var:

```csharp
if (string.IsNullOrEmpty(header.SiparisNo)) continue;
```

Geri kalan her alan için fallback değerler kullanılıyor: tarihler `DateTime.Now`, stok adı `"Unknown"`, miktar `0`, proje kodu `"UNKNOWN"`.

### 3.2 Validation Matrisi

| Alan | Kaynak | Kural | İhlalde Eylem |
|------|--------|-------|---------------|
| `SiparisNo` | Header | Zorunlu, boş olamaz | **Tamamen reddet** (mevcut — doğru) |
| `ProjeKodu` veya `KurumKodu` | Header detail | En az biri dolu olmalı | **Problemli kaydet** (`HasProjectWarning = true`) |
| `OrderDate` (Tarih) | Header detail | Parse edilebilir olmalı | **Problemli kaydet**, `DateTime.Now` fallback logla |
| `TeslimTarihi` | Header detail | Parse edilebilir olmalı | **Problemli kaydet**, `DateTime.Now+1` fallback logla |
| `Lines` | Detail | En az 1 satır olmalı | **Tamamen reddet** |
| `MalzemeKodu` (satır) | Line | Boş olamaz | **Satırı atla**, sipariş devam eder |
| `Miktari` (satır) | Line | > 0 olmalı | **Problemli kaydet** (`HasZeroQtyLines = true`) |
| `MalzemeAdi` (satır) | Line | Boş olabilir | `"Unknown"` fallback — kabul |
| `Birimi` (satır) | Line | Boş olabilir | `Adet` fallback — kabul |

### 3.3 Reddet vs Problemli Kaydet Ayrımı

**Tamamen reddet (sisteme girmesin):**
- `SiparisNo` boş
- `Lines` boş (bu sürümde mevcut değil — eklenmeli)

**Problemli kaydet (`ImportStatus.HasWarnings` olarak):**
- Proje kodu yok → stub project "UNKNOWN" yerine açık uyarı
- Tarih parse edilemedi → fallback kullanıldı uyarısı
- Miktar 0 olan satırlar var

**Normal kaydet ama mapping bekle:**
- Stok kodu mapping'de yok → mevcut `NeedsMapping` mantığı doğru

### 3.4 Üçlü ImportStatus Tasarımı

Mevcut `ImportStatus` enum'u: `Ready(0)`, `NeedsMapping(1)`

Önerilen ek değer:

```csharp
public enum ImportStatus
{
    Ready       = 0,   // Tüm kontroller geçti, shipment oluşturulabilir
    NeedsMapping = 1,  // Mapping bekliyor, shipment ENGELLİ
    HasWarnings  = 2,  // Uyarılar var (tarih fallback, proje bilinmiyor vb.), kullanıcı onayı gerekebilir
}
```

---

## 4. STOCK MAPPING BLOKAJI

### 4.1 Mevcut Durumun Gerçeği

`CheckOrderMappingStatusCommand` doğru çalışıyor: tüm satırlar eşleşince `ImportStatus = Ready` yapıyor.

**Ama `CreateShipmentCommandHandler` bu kontrolü yapmıyor:**

```csharp
public async Task<int> Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
{
    var order = await _context.IssOrders...
        .FirstOrDefaultAsync(o => o.Id == request.IssOrderId, cancellationToken);

    if (order == null)        throw new NotFoundException("ISS siparişi bulunamadı.");
    if (order.IsTransferred)  throw new ConflictException("Bu sipariş zaten bir sevkiyata dönüştürülmüş.");

    // BURADA ImportStatus KONTROLÜ YOK!
    // NeedsMapping olan sipariş buraya geliyor, shipment oluşturuluyor.
```

`NeedsMapping` durumundaki bir sipariş shipment'a dönüşebiliyor. O shipment'ın satırlarında `StockMasterId = null`. Depo hazırlıkta picking listesi eksik veya yanlış.

Dahası: shipment satırları oluşturulurken stok eşleştirmesi **hiç yapılmıyor:**

```csharp
var sLine = new ShipmentLine
{
    IssOrderLineId = line.Id,
    StockCode      = line.StockCode,
    StockName      = line.StockName,
    Unit           = line.Unit,
    OrderedQty     = line.OrderedQty,
    // StockMasterId = ???  ← Hiçbir yerde set edilmiyor
};
```

### 4.2 Doğru Blokaj Tasarımı

**Domain Kuralı:** `NeedsMapping` durumundaki sipariş operasyon akışına giremez.

Bu kuralı **iki yerde** enforce et:

**Katman 1 — `IssOrder` domain metodu:**

```csharp
// IssOrder.cs
public Shipment CreateShipment(IReadOnlyDictionary<string, int> stockMappings)
{
    if (ImportStatus == ImportStatus.NeedsMapping)
        throw new DomainException(
            $"Sipariş {ExternalOrderNumber}: tüm stok kalemleri eşleştirilmeden " +
            "sevkiyat oluşturulamaz.");

    var shipment = new Shipment
    {
        ProjectId    = ProjectId,
        DeliveryDate = DeliveryDate,
        IssOrderId   = Id,
        CreatedAt    = DateTime.UtcNow,
        TalepNo      = TalepNo
    };

    foreach (var line in Lines.OrderBy(l => l.LineNumber))
    {
        // Mapping'i burada uygula
        stockMappings.TryGetValue(line.StockCode, out var stockMasterId);

        shipment.Lines.Add(new ShipmentLine
        {
            IssOrderLineId = line.Id,
            StockCode      = line.StockCode,
            StockName      = line.StockName,
            Unit           = line.Unit,
            OrderedQty     = line.OrderedQty,
            StockMasterId  = stockMasterId > 0 ? stockMasterId : null
        });
    }

    IsTransferred = true;
    return shipment;
}
```

**Katman 2 — `CreateShipmentCommandHandler` güncelleme:**

```csharp
// CreateShipmentCommandHandler.cs
if (order.ImportStatus == ImportStatus.NeedsMapping)
    throw new DomainException(
        $"Sipariş {order.ExternalOrderNumber} eşleştirilmemiş stok kalemleri içeriyor. " +
        "Stok eşleştirmelerini tamamlayın.");

// Mapping'leri yükle ve domain metoduna geç
var mappedStocks = await _context.StockMappings
    .Where(m => m.ExternalSystem == "ISS-IP"
             && m.MatchStatus == MatchStatus.Mapped
             && m.LocalStockId != null)
    .ToDictionaryAsync(m => m.ExternalStockCode, m => m.LocalStockId!.Value, cancellationToken);

var shipment = order.CreateShipment(mappedStocks);
_context.Shipments.Add(shipment);
await _context.SaveChangesAsync(cancellationToken);
return shipment.Id;
```

### 4.3 Bulk Create İçin Aynı Kural

`CreateBulkShipmentsCommand` da aynı kontrolü yapmalı. Tek fark: bulk'ta `NeedsMapping` olan siparişler atlanır, atlanma sayısı response'a eklenir.

---

## 5. IMPORT SONUCU GÖRÜNÜRLÜĞÜ

### 5.1 Mevcut Durum

```csharp
public record ImportIssOrdersCommand(DateTime StartDate, DateTime EndDate) : IRequest<int>;
```

Return type `int` — sadece yeni eklenen sipariş sayısı. Güncellenen, atlanan, hatalı olan hiçbiri bilinmiyor.

### 5.2 Yeni Response Modeli

```csharp
// Application/Orders/Commands/ImportIssOrders/ImportResult.cs

public record ImportIssOrdersResult
{
    // Sayılar
    public int TotalFromIss        { get; init; }  // ISS'ten gelen toplam
    public int NewlyCreated        { get; init; }  // Yeni eklenen
    public int Updated             { get; init; }  // Güncellenen (header veya satır)
    public int Skipped             { get; init; }  // Değişmemiş, atlandı
    public int Rejected            { get; init; }  // Tamamen reddedilen (SiparisNo yok, satır yok)
    public int NeedsMapping        { get; init; }  // Mapping bekleyen
    public int HasWarnings         { get; init; }  // Uyarılı kaydedilenler

    // Detay
    public List<OrderImportError>   Errors   { get; init; } = new();
    public List<OrderImportWarning> Warnings { get; init; } = new();
    public List<string>             MappingPendingOrders { get; init; } = new(); // ExternalOrderNumber listesi

    // Meta
    public TimeSpan Duration       { get; init; }
    public DateTime ImportedAt     { get; init; }
}

public record OrderImportError(
    string  ExternalOrderNumber,
    string  Stage,           // "ListParse" | "DetailFetch" | "DetailParse" | "Save"
    string  Message,
    string? RawSnippet       // Ham response'un ilk 300 karakteri
);

public record OrderImportWarning(
    string ExternalOrderNumber,
    string Field,            // "OrderDate" | "ProjectCode" | "ZeroQtyLine"
    string Message
);
```

```csharp
// Command da güncellenir:
public record ImportIssOrdersCommand(DateTime StartDate, DateTime EndDate)
    : IRequest<ImportIssOrdersResult>;
```

### 5.3 Frontend Summary Ekranı

Mevcut `OrderImportView.vue` muhtemelen sadece sayı gösteriyor. Import sonrası gösterilmesi gereken:

```
┌─────────────────────────────────────────────────────────┐
│  Import Tamamlandı  •  3.2 saniye                       │
├─────────────────┬───────────────────────────────────────┤
│ ISS'ten gelen   │ 47                                     │
│ Yeni eklendi    │ 12   ✅                                │
│ Güncellendi     │ 31   🔄                                │
│ Değişmedi       │ 3    ⏭️                                 │
│ Reddedildi      │ 1    ❌                                │
│ Mapping bekliyor│ 8    ⚠️  → [Stok Eşleştirmesine Git]  │
└─────────────────┴───────────────────────────────────────┘

❌ Reddedilen Siparişler (1)
  ORD-2024-0985 — Satır bulunamadı (detay yanıtı boş geldi)

⚠️ Uyarılı Siparişler (2)
  ORD-2024-0978 — Teslim tarihi okunamadı, bugün kullanıldı
  ORD-2024-0981 — Proje kodu boş, "UNKNOWN" atandı

⏳ Mapping Bekleyen (8)
  ORD-2024-0971, 0972, 0973, 0974, 0975, 0976, 0977, 0980
  [Stok eşleştirmesine git →]
```

---

## 6. SHİPMENT OLUŞTURMA ÖNCESİ KORUMA

### 6.1 Mevcut Guard'lar

```csharp
if (order == null)       throw new NotFoundException(...);
if (order.IsTransferred) throw new ConflictException(...);
// Double check: existingShipment AnyAsync kontrolü + IsTransferred self-heal
```

Bu iki kontrol iyi. Ama eksikler:

### 6.2 Eksik Guard'lar ve Fix'leri

**Guard 1 — Mapping kontrolü (kritik, yukarıda detaylandı):**
```csharp
if (order.ImportStatus == ImportStatus.NeedsMapping)
    throw new DomainException("Stok eşleştirmesi tamamlanmadan sevkiyat oluşturulamaz.");
```

**Guard 2 — Satır miktarı kontrolü:**
```csharp
var zeroQtyLines = order.Lines.Where(l => l.OrderedQty <= 0).ToList();
if (zeroQtyLines.Any())
    throw new DomainException(
        $"{zeroQtyLines.Count} satırda miktar sıfır veya negatif. " +
        "Siparişi doğrulayın.");
```

**Guard 3 — Proje bağlantısı kontrolü:**
```csharp
if (order.Project == null || order.Project.Code == "UNKNOWN")
    throw new DomainException(
        "Siparişin bağlı olduğu proje bulunamadı veya tanımsız. " +
        "Proje eşleştirmesini tamamlayın.");
```

**Guard 4 — Sipariş aktif mi:**
```csharp
if (!order.IsActive)
    throw new DomainException("Pasif sipariş sevkiyata dönüştürülemez.");
```

**Guard 5 — Teslim tarihi geçmişte mi (opsiyonel uyarı):**
```csharp
if (order.DeliveryDate.Date < DateTime.UtcNow.Date)
{
    // Hard block değil, uyarı — gerekirse override edilebilir
    // veya log at ve devam et, kullanıcı bilinçli yapıyor olabilir
    _logger.LogWarning(
        "Creating shipment for overdue order {OrderNumber}. " +
        "DeliveryDate: {Date}", order.ExternalOrderNumber, order.DeliveryDate);
}
```

**Guard 6 — `IsTransferred` yarış koşulu koruması (atomic):**

Mevcut iki aşamalı kontrol (`IsTransferred` check + `AnyAsync` check) hala race condition içeriyor.

```csharp
// Atomic update ile çöz:
var updated = await _context.Database.ExecuteSqlRawAsync(
    "UPDATE IssOrders SET IsTransferred = 1 " +
    "WHERE Id = {0} AND IsTransferred = 0",
    order.Id);

if (updated == 0)
    throw new ConflictException("Bu sipariş zaten bir sevkiyata dönüştürülmüş.");
```

### 6.3 Guard Özet Tablosu

| Guard | Mevcut | Önerilen Eylem |
|-------|--------|----------------|
| Sipariş bulunamadı | ✅ | — |
| Zaten transfer edildi | ✅ (ama race condition var) | Atomic SQL update ile güçlendir |
| Mapping eksik | ❌ | Ekle — hard block |
| Satırlar boş | ❌ | Ekle — hard block |
| Sıfır miktar satırları | ❌ | Ekle — hard block veya uyarı |
| Proje "UNKNOWN" | ❌ | Ekle — hard block |
| Sipariş pasif | ❌ | Ekle — hard block |
| Teslim tarihi geçmişte | ❌ | Ekle — log uyarısı |

---

## 7. AUDIT & GÖZLEMLENEBİLİRLİK

### 7.1 Yeni Entity: ImportBatch

```csharp
// Domain/Entities/ImportBatch.cs
public class ImportBatch
{
    public int      Id              { get; set; }
    public string   Source          { get; set; } = "ISS-IP";
    public DateTime StartDate       { get; set; }   // İstenen tarih aralığı
    public DateTime EndDate         { get; set; }
    public DateTime StartedAt       { get; set; }
    public DateTime? CompletedAt    { get; set; }
    public string?  StartedByUserId { get; set; }
    public ImportBatchStatus Status { get; set; }   // Running, Completed, Failed, PartialSuccess

    // Sayılar
    public int TotalFromSource  { get; set; }
    public int NewlyCreated     { get; set; }
    public int Updated          { get; set; }
    public int Skipped          { get; set; }
    public int Rejected         { get; set; }
    public int NeedsMapping     { get; set; }

    // Ham veri
    public string? ListResponseSnippet { get; set; }   // İlk 2000 karakter
    public string? ErrorSummary        { get; set; }   // JSON array of errors
    public int     DurationMs          { get; set; }

    // Navigation
    public ICollection<ImportBatchOrder> Orders { get; set; } = new List<ImportBatchOrder>();
}

public class ImportBatchOrder
{
    public int    Id              { get; set; }
    public int    ImportBatchId   { get; set; }
    public string ExternalOrderNo { get; set; } = null!;
    public string Action          { get; set; } = null!; // "Created" | "Updated" | "Skipped" | "Rejected"
    public string? Warning        { get; set; }
    public string? Error          { get; set; }
    public int?   IssOrderId      { get; set; }  // Başarılı kayıtlar için FK
}

public enum ImportBatchStatus
{
    Running       = 0,
    Completed     = 1,
    Failed        = 2,
    PartialSuccess = 3  // Bazı siparişler başarısız ama genel akış devam etti
}
```

### 7.2 Handler'da Batch Kullanımı

```csharp
public async Task<ImportIssOrdersResult> Handle(ImportIssOrdersCommand request, CancellationToken ct)
{
    var batch = new ImportBatch
    {
        Source      = "ISS-IP",
        StartDate   = request.StartDate,
        EndDate     = request.EndDate,
        StartedAt   = DateTime.UtcNow,
        StartedByUserId = _currentUser.Id,   // ICurrentUserService inject et
        Status      = ImportBatchStatus.Running
    };
    _context.ImportBatches.Add(batch);
    await _context.SaveChangesAsync(ct);  // Batch ID'yi al — import başladığının kaydı

    var sw = Stopwatch.StartNew();
    var result = new ImportResultBuilder();

    try
    {
        // ... import logic (per-order try-catch ile)

        batch.Status     = result.HasErrors ? ImportBatchStatus.PartialSuccess : ImportBatchStatus.Completed;
        batch.CompletedAt = DateTime.UtcNow;
        batch.DurationMs  = (int)sw.ElapsedMilliseconds;
        // batch'e sayıları yaz
        await _context.SaveChangesAsync(ct);

        return result.Build();
    }
    catch (Exception ex)
    {
        batch.Status      = ImportBatchStatus.Failed;
        batch.ErrorSummary = ex.Message;
        batch.CompletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        throw;
    }
}
```

### 7.3 Per-Order Hata İzolasyonu

Import'taki en kritik değişiklik: her sipariş kendi try-catch içinde işlenmeli:

```csharp
foreach (var header in orderList)
{
    var orderResult = new OrderImportAttempt(header.SiparisNo);

    try
    {
        // detail fetch, parse, upsert
        // ...
        orderResult.Action = existingOrder == null ? "Created" : "Updated";
        result.AddSuccess(orderResult);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Order import failed: {OrderNumber}", header.SiparisNo);
        orderResult.Error = ex.Message;
        result.AddError(orderResult);
        // DEVAM ET — bu sipariş başarısız ama diğerleri etkilenmesin
    }
}

// Tüm başarılı siparişleri kaydet
await _context.SaveChangesAsync(ct);
```

### 7.4 Frontend: Import Geçmişi Sayfası

`/orders/import` sayfasına "Import Geçmişi" sekmesi eklenmeli:

```
Tarih          | Başlangıç | Bitiş    | Gelen | Yeni | Güncellendi | Hata | Durum
2026-03-24 09:15 | 03-20    | 03-24   |  47   |  12  |     31      |  1   | ✅
2026-03-23 09:10 | 03-19    | 03-23   |  38   |   8  |     28      |  2   | ⚠️
2026-03-22 09:18 | 03-18    | 03-22   |  42   |   0  |      0      | 42   | ❌ [Detay]
```

---

## 8. EN KRİTİK 5 İYİLEŞTİRME

### #1 — Per-Order Hata İzolasyonu

**Neden kritik:**
Tek bir siparişin parse hatası tüm import'u başarısız kılıyor. 100 siparişten 1 tanesi kötü gelirse 99 tanesi de kaydedilmiyor. Kullanıcı tekrar çalıştırdığında ISS-IP'ye 100 istek daha atılıyor.

**Ne engeller:**
Import'un sessizce başarısız olmasını ve veri kaybını.

**Implementasyon:**
Dış `try-catch`'i koru (fatal hatalar için), ama `foreach` içine per-order `try-catch` ekle. Başarılıları biriktir, tek `SaveChangesAsync` yap. Hatalı siparişleri `result.Errors`'a ekle.

---

### #2 — CreateShipment'ta Mapping Guard

**Neden kritik:**
`NeedsMapping` durumundaki sipariş şu an shipment'a dönüşebiliyor. O shipment'ın satırlarında `StockMasterId = null`. Depo hazırlıkta picking listesi boş ya da eksik stok kodlarıyla geliyor. Şoför yanlış malzeme taşıyor.

**Ne engeller:**
Eşleştirilmemiş stokun operasyon akışına girmesini.

**Implementasyon:**
`CreateShipmentCommandHandler` başına 2 satır:
```csharp
if (order.ImportStatus == ImportStatus.NeedsMapping)
    throw new DomainException("Stok eşleştirmesi tamamlanmadan sevkiyat oluşturulamaz.");
```
Ve shipment satırı oluşturulurken `StockMasterId`'yi mapping'den doldur.

---

### #3 — ImportIssOrdersCommand Return Type Genişletme

**Neden kritik:**
Kullanıcı import'a bastığında "bir şeyler oldu" veya "X sipariş eklendi" görüyor. Kaç tanesi güncellendi, kaç tanesi mapping bekliyor, kaç tanesi reddedildi — bilinmiyor. Operatör körleşiyor.

**Ne engeller:**
Sessiz hataların ve eksik mapping'lerin fark edilmemesini.

**Implementasyon:**
`IRequest<int>` → `IRequest<ImportIssOrdersResult>` değişikliği. `ImportIssOrdersResult` yukarıda tanımlandı. Frontend'de özet ekranı.

---

### #4 — `ExternalOrderNumber` Unique Index + Mevcut Sipariş için Skip

**Neden kritik:**
Double-click veya network retry ile aynı sipariş iki kez kaydedilebiliyor. Unique index olmadan DB constraint yok.

**Ne engeller:**
Mükerrer sipariş → mükerrer stok rezervasyonu → stok negatife düşme.

**Not:** ~~Satır line diff~~ — **gerekli değil.** ISS-IP onaylı siparişler immutable; satırlar hiçbir zaman değişmez. Revoked order yeni sipariş numarasıyla gelir. Mevcut sipariş için detail API çağrısı yapılmamalı, doğrudan skip edilmeli.

**Implementasyon:**
```csharp
// SevkiyatDbContext.OnModelCreating:
modelBuilder.Entity<IssOrder>()
    .HasIndex(o => o.ExternalOrderNumber).IsUnique();
```
Ve `existingOrder != null` ise detail HTTP çağrısı yapmadan `continue` geç — N+1 sorunu da çözülür.

---

### #5 — ImportBatch Audit Entity

**Neden kritik:**
Şu an import yapıldıktan sonra "ne zaman, kim, kaç sipariş, hangileri hata verdi" bilgisine ulaşılamıyor. Sorun çıktığında debug imkansız. ISS-IP'nin hangi yanıtı döndürdüğü hiç saklanmıyor.

**Ne engeller:**
Blind-spot operasyonunu. "Dün import yapıldı mı? Ne geldi? Neden hata var?" sorularını yanıtsız bırakmayı.

**Implementasyon:**
`ImportBatch` entity + migration + `ImportBatches` DbSet. Handler başında `Running` kaydı, sonunda `Completed`/`Failed`/`PartialSuccess` güncellemesi. Frontend'de import geçmişi sekmesi.

---

## UYGULAMA SIRASI

```
Hafta 1 — Operasyonel güvenlik:
  ✓ #2: CreateShipment mapping guard (2 saat)
  ✓ #4: ExternalOrderNumber unique index + existing-skip (1 saat)

Hafta 2 — Import güvenilirliği:
  ✓ #1: Per-order hata izolasyonu (4 saat)
  ✓ #3: Return type genişletme + frontend özet (1 gün)

Hafta 3 — Gözlemlenebilirlik:
  ✓ #5: ImportBatch entity + migration + geçmiş sayfası (2 gün)

Sonrası:
  - Revocation gap (section 1.6) — ISS-IP'nin tam liste mi yoksa delta mı döndürdüğü
    netleştikten sonra implemente et
  - Satırsız sipariş reddi (section 2.3)
  - Tarih fallback uyarıları (section 2.4)
  - ~~Satır line diff~~ — ISS-IP immutability nedeniyle gerekli değil
```

---

## ISS-IP İŞ KURALLARI REFERANSI

> Bu bölüm sonradan öğrenilen iş kurallarını belgelemek için eklendi (2026-03-24).

| Kural | Detay |
|-------|-------|
| Sipariş akışı | Talep oluşturulur → Yetkili onaylar → Sipariş bize düşer |
| Değişmezlik | Onaylı sipariş ISS-IP'de değiştirilemez (satırlar, miktarlar, tarihler) |
| İptal | Yetkili onayı geri çekebilir → sipariş ISS-IP'den silinir, yeni import listesinde görünmez |
| Yeniden onay | Aynı talep geri çekilip onaylanırsa **yeni sipariş numarasıyla** gelir |
| Sıklık | Geri çekme + yeniden onay çok nadir (~10 yılda 5 kez) |
| Etki | Mevcut sipariş için line diff gereksiz; satırlar asla değişmez |
| List endpoint | **Tarih aralığı filtreli delta** — tam aktif sipariş listesi değil |

---

## ISS-IP API REFERANSI

> Kaynak: `ISS Tedarikçi Web Servisi Metodlari https.pdf`
> Base URL: `https://generalapi.issturkiye.com/`
> Auth: KullaniciAdi + Sifre (her request body'de)

### Metodlar

#### 1. `getTedarikciSiparisListesi` — Sipariş Listesi

```json
POST /
{
  "MethodName": "getTedarikciSiparisListesi",
  "MtdParams": {
    "KullaniciAdi": "...", "Sifre": "...",
    "BaslangicTarihi": "2018-12-25",
    "BitisTarihi": "2019-01-02"
  }
}
```

**Çıktı (her sipariş için):** `SiparisNo`, `TalepNo`, `Tarih`, `KurumKodu`, `ProjeKodu`

> **Kritik not:** Bu endpoint **tarih aralığındaki onay kayıtlarını** döndürür, aktif tüm siparişleri değil. Revocation detection bu API ile yapılamaz — geri çekilen sipariş listede zaten görünmez çünkü başlangıçta onay tarihi ile geldi.

#### 2. `getTedarikciSiparis` — Sipariş Detayı

```json
POST /
{ "MethodName": "getTedarikciSiparis", "MtdParams": { ..., "SiparisNo": "201901260265" } }
```

**Header çıktısı (Table):**

| Alan | Tip | Notlar |
|------|-----|--------|
| `SiparisID` | Int | ISS iç ID |
| `SiparisNo` | String | `ExternalOrderNumber` karşılığı |
| `TalepNo` | String | Talep numarası — **şu an saklanmıyor** |
| `Tarih` | DateTime | Sipariş tarihi |
| `KurumKodu` | String | Kurum kodu |
| `TeslimTarihi` | DateTime | Teslimat tarihi |
| `TeslimAlacakKisiler` | String | Teslim alacak kişiler — **şu an saklanmıyor** |
| `TeslimAlacakTelefonNumaralari` | String | Telefon numaraları — **şu an saklanmıyor** |
| `TalepTuru` | String | Talep türü — **şu an saklanmıyor** |
| `Aciklama` | String | Açıklama — **şu an saklanmıyor** |
| `Donem` | String | Dönem — **şu an saklanmıyor** |
| `YoneticiMailAdresleri` | String | Yönetici e-postaları — **şu an saklanmıyor** |

**Satır çıktısı (Table1):**

| Alan | Tip | Notlar |
|------|-----|--------|
| `MalzemeKodu` | String | ISS stok kodu |
| `MalzemeAdi` | String | Malzeme adı |
| `Miktari` | Float | Miktar |
| `Birimi` | String | Birim |
| `ListeFiyati` | Money | Liste fiyatı — **şu an saklanmıyor** |
| `Iskonto` | Money | İskonto — **şu an saklanmıyor** |
| `BirimFiyati` | Money | Birim fiyatı — **şu an saklanmıyor** |
| `KDVOrani` | Float | KDV oranı — **şu an saklanmıyor** |

#### 3. `getTedarikciProje` — Proje Listesi

```json
POST /
{ "MethodName": "getTedarikciProje", "MtdParams": { ..., "ProjeKodu": "" } }
```

**Çıktı:** `ProjeKodu`, `ProjeAdi`, `KurumKodu`, `MalzemeTeslimAdresi`

> **Fırsat:** `ProjeKodu` boş bırakılarak tüm projeler çekilebilir. Şu an `IssOrder` import sırasında proje bulunamazsa `"UNKNOWN"` kodu ile stub kayıt oluşturuluyor. Bunun yerine bu endpoint ile projeler sync edilebilir.

#### 4. `getTedarikciMalzeme` — Malzeme Kataloğu

```json
POST /
{ "MethodName": "getTedarikciMalzeme", "MtdParams": { ..., "MalzemeKodu": "" } }
```

**Çıktı:** `MalzemeKodu`, `MalzemeAdi`, `Birimi`

> **Fırsat:** `MalzemeKodu` boş bırakılarak tüm ISS malzemeleri çekilebilir. Şu an stok mapping tamamen manüel — operatör ISS kodunu aramak zorunda. Bu endpoint ile:
> 1. ISS malzeme listesi DB'ye sync edilebilir
> 2. Mapping ekranında ISS kodu arama otomatikleşir
> 3. `NeedsMapping` siparişlerde malzeme adı zaten bilindiği için öneri sunulabilir

---

## REVOCATION GAP — GÜNCELLENMİŞ DEĞERLENDİRME

Section 1.6'da önerilen "listede olmayan siparişleri iptal et" yaklaşımı **API dokümantasyonu incelendikten sonra uygulanamaz** olarak değerlendirilmiştir.

**Neden:**
`getTedarikciSiparisListesi` tarih aralığı filtreli çalışır. "Bugün çekilen liste" = "bu tarih aralığında onaylanan siparişler". 3 hafta önce onaylanmış ve hâlâ aktif olan bir sipariş bu listede **çıkmaz**. Bunu "iptal edildi" sanmak yanlış olur.

**Gerçek durum:**
Revocation'ı tespit etmenin API üzerinden otomatik bir yolu yok. ISS-IP geri çekilen siparişi bize notify etmiyor.

**Pratik çözüm:**
Manuel işlem. ISS'te sipariş geri çekildiğinde operatör sistemimizdeki siparişi de manuel olarak pasif yapmalı. Bunun için:
1. `IssOrder` üzerinde "İptal Et" butonu (Admin/Manager rolü)
2. İptal edilen siparişin aktif sevkiyatı varsa uyarı
3. `CancelledAt` + `CancelledByUserId` audit alanları

Bu düzeyde bir iptal neredeyse hiç olmayacağından (~10 yılda 5 kez) complex bir mekanizma gereksiz.

---

## API KULLANIM DURUMU — DÜZELTME

> Analiz sırasında "kullanılmıyor/saklanmıyor" olarak işaretlenen bazı alanların aslında implement edildiği kod incelemesiyle teyit edildi (2026-03-24).

### Gerçek Durum

| Alan / Endpoint | Belgelenen | Gerçek Durum |
|-----------------|------------|--------------|
| `TalepNo` | Saklanmıyor | ✅ `IssOrder.TalepNo` entity'de var, import'ta dolduruluyor |
| `TeslimAlacakKisiler` | Saklanmıyor | ✅ Entity'de var, import'ta dolduruluyor |
| `TeslimAlacakTelefonNumaralari` | Saklanmıyor | ✅ Entity'de var, import'ta dolduruluyor |
| `TalepTuru`, `Aciklama`, `Donem`, `YoneticiMailAdresleri` | Saklanmıyor | ✅ Hepsi entity'de var, import'ta dolduruluyor |
| `ListeFiyati`, `Iskonto`, `BirimFiyati`, `KDVOrani` | Saklanmıyor | ✅ `IssOrderLine`'da var, import'ta dolduruluyor |
| `getTedarikciProje` | Kullanılmıyor | ✅ `SyncProjectsCommand` bu endpoint'i kullanıyor |

### Proje Sync Akışı (Doğru Durum)

1. Import sırasında proje bulunamazsa `Code = projeKodu`, `Name = "Bilinmiyor " + code` ile stub proje oluşturulur
2. `SyncProjectsCommand` tetiklendiğinde (manuel "Projeleri Kontrol Et" butonu veya `LastSyncedAt > 24s` kontrolüyle) her proje için `getTedarikciProje` çağrılır
3. `ProjeAdi`, `KurumKodu`, `MalzemeTeslimAdresi` güncellenir, `LastSyncedAt` set edilir
4. Bu akış doğru çalışıyor.

### `getTedarikciMalzeme` — Gerçek Değeri Ne?

Bu endpoint `MalzemeKodu`, `MalzemeAdi`, `Birimi` döndürüyor. **Otomatik stok mapping yapabilmek için**:
- ISS kodunu bizim `StockMaster.StockCode`'una bağlayacak **ortak bir anahtar yok** — iki sistem farklı kodlama kullanıyor
- Eşleştirme ancak isim benzerliğiyle (fuzzy match) yapılabilir
- Risk: ISS `"ÇAMAŞIR SUYU 5LT"` — StockMaster'da `"Çamaşır Suyu 5 Litre"` ve `"Çamaşır Suyu 5 Lt Ekonomik"` varsa yanlış eşleşme

**Gerçekçi kullanım:** Fuzzy match + operatör onayı şeklinde **öneri sistemi**:
1. Import sırasında yeni ISS kodu geldiğinde, StockMaster'ı isim benzerliğine göre sırala
2. En yüksek benzerlikli 3-5 stoku `StockMapping` kaydına `SuggestedStockIds` olarak sakla
3. Mapping ekranında operatör öneri listesinden seçsin, kaydetsin
4. Önerinin yanlışsa manuel arama hâlâ mevcut

Bu **tam otomasyon değil**, ama şu anki tam manuel akışa göre %80 süre tasarrufu sağlar.

**Implementasyon gereklilikleri:**
- `StockMapping` entity'ye `SuggestedStockIds` (JSON kolonu) eklenmesi
- Import'ta fuzzy match algoritması (Levenshtein distance veya NGram)
- Mapping ekranında öneri UI'ı

Efor/etki değerlendirmesi: **Orta efor, yüksek operasyonel etki** — ancak `NeedsMapping` sorunu yüksek hacimde sorun olmaya devam ederse önceliklendirilmeli.

---

*Bu döküman `ISS_IP_RELIABILITY_REVIEW.md` olarak proje kök dizinine kaydedilmiştir.*
