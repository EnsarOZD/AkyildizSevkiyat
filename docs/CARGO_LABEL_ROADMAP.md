# Kargo Etiketi Entegrasyon Yol Haritası

## Genel Bakış

YurtiKargo entegrasyonunu tamamlayarak sistemin kargo kaydı oluşturmasını, barkodu saklamasını ve operatörlerin pakete yapıştırılabilir kargo etiketi basabilmesini sağlamak.

**Mevcut durum:**
- `DispatchZoneAsCargo` komutu YurtiKargo SOAP API'sine `createShipment` çağrısı yapıyor ✓
- YurtiKargo yanıtındaki `barcode` alanı entity'e kaydedilmiyor ✗
- Kargo etiketi baskı ekranı yok ✗
- Self-servis kargo akışı (sistem dışında) kullanılmak istenmiyor

**Mimari karar:**
- Netsis e-irsaliyesi (GIB belgesi) değiştirilemez → dokunulmaz
- Kargo etiketi **ayrı bir baskı çıktısı** olarak pakete yapıştırılır
- İrsaliye içine konur, kargo etiketi dışına yapıştırılır

---

## Faz 1 — Backend: YkBarcode Alanı

### 1.1 Domain — Shipment Entity

**Dosya:** `Akyildiz.Sevkiyat.Domain/Entities/Shipment.cs`

- `YkBarcode` (`string?`) alanı ekle
- `SetYkCargoInfo()` metodunu barkodu parametre olarak alacak şekilde güncelle
- `UpdateYkBarcode(string? barcode)` metodu ekle (sonraki sorgulardan barcode güncellenebilmesi için)

```csharp
public string? YkBarcode { get; private set; }

public void SetYkCargoInfo(string cargoKey, string? invoiceKey, int? jobId, string? barcode, string? opStatus, string? opMessage, string? errCode, string? errMsg)
{
    YkCargoKey      = cargoKey;
    YkInvoiceKey    = invoiceKey;
    YkJobId         = jobId;
    YkBarcode       = barcode;          // YENİ
    YkOperationStatus  = opStatus;
    YkOperationMessage = opMessage;
    YkErrorCode     = errCode;
    YkErrorMessage  = errMsg;
}
```

### 1.2 Infrastructure — YurtiKargoClient

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/ExternalServices/YurtiKargo/YurtiKargoClient.cs`

- `CreateShipmentAsync` dönüş DTO'suna `Barcode` alanı ekle
- SOAP yanıtından `barcode` elementini parse et ve DTO'ya yaz

```csharp
public record YkCreateShipmentResult(
    bool Success,
    string? DocId,
    int? JobId,
    string? Barcode,    // YENİ — SOAP yanıtındaki <barcode> elementi
    string? ErrorCode,
    string? ErrorMessage
);
```

### 1.3 Application — DispatchZoneAsCargoCommand

**Dosya:** `Akyildiz.Sevkiyat.Application/Warehouse/Commands/DispatchZoneAsCargo/DispatchZoneAsCargoCommandHandler.cs`

- `SetYkCargoInfo()` çağrısına `result.Barcode` parametresini ekle

### 1.4 Infrastructure — EF Core Migration

```bash
dotnet ef migrations add AddYkBarcodeToShipment \
  --project Akyildiz.Sevkiyat.Infrastructure \
  --startup-project Akyildiz.Sevkiyat.WebApi
```

Şema değişikliği: `Shipments` tablosuna `YkBarcode nvarchar(100) NULL` kolonu eklenir.

### 1.5 Application — Shipment DTO

**Dosya:** `Akyildiz.Sevkiyat.Application/Shipments/Queries/GetShipmentDetail/` (veya ilgili DTO)

- `YkBarcode` alanını DTO'ya ve mapping'e ekle
- YurtiKargo rapor DTO'suna (`YkCargoReportItemDto`) da ekle

---

## Faz 2 — Frontend: Kargo Etiketi Baskı Ekranı

### 2.1 Bağımlılık

```bash
cd client
npm install jsbarcode
# veya: npm install bwip-js
```

`JsBarcode` tercih edilir — Code128 formatını destekler, hafif, Vue 3 uyumlu.

### 2.2 Baskı Route'u

**Dosya:** `client/src/router/index.ts`

```typescript
{
  path: '/shipments/:id/cargo-label',
  name: 'CargoLabel',
  component: () => import('@/views/CargoLabelPrintView.vue'),
  meta: { requiresAuth: true, roles: ['Admin', 'Manager', 'Accounting', 'Dispatcher'] }
}
```

### 2.3 CargoLabelPrintView.vue

**Dosya:** `client/src/views/CargoLabelPrintView.vue`

Baskı ekranı içeriği:

```
┌─────────────────────────────────────────────┐
│  AKYILDIZ LOJİSTİK                          │
│  ─────────────────────────────────────────  │
│  Alıcı:  [ProjectName]                      │
│  Adres:  [Project.Address]                  │
│  Tel:    [ContactPhone]                     │
│  ─────────────────────────────────────────  │
│                                             │
│  ┌─────────────────────────────────────┐   │
│  │ ||||||||||||||||||||||||||||||||||| │   │  ← JsBarcode (Code128)
│  └─────────────────────────────────────┘   │
│       [YkBarcode değeri — text olarak]      │
│                                             │
│  ─────────────────────────────────────────  │
│  Gönderici: Akyıldız Lojistik              │
│  İrsaliye:  [IrsaliyeNo]                   │
│  Takip No:  [YkCargoKey]                   │
│  Tarih:     [DeliveryDate]                  │
│                                             │
│  YURTİÇİ KARGO                             │
└─────────────────────────────────────────────┘
```

- `window.print()` ile native browser baskı
- `@media print` CSS — sadece etiket alanı, navbar/header gizlenir
- Sayfa yüklendiğinde otomatik `window.print()` tetiklenir (ShipmentOrderPrintView gibi)
- Tek sevkiyat için: `/shipments/123/cargo-label`

### 2.4 Toplu Etiket Baskısı (Opsiyonel — Faz 3)

Zone sevkiyatları için tüm etikетleri tek seferde basmak:
- `/zones/:zoneId/cargo-labels` route'u
- Her sevkiyat için ayrı sayfa (`page-break-after: always`)

### 2.5 "Kargo Etiketi Bas" Butonu

**Birincil konum:** Zone dispatch ekranı — kargo ile sevk işlemi tamamlandıktan sonra "Etiket Bas" butonu çıkar

**İkincil konum:** Sevkiyat detay sayfası — `CargoProvider == YurticiKargo` ve `YkBarcode != null` ise buton görünür

```typescript
// Yeni sekmede aç — baskı sonrası kapanır
window.open(`/shipments/${shipmentId}/cargo-label`, '_blank');
```

---

## Faz 3 — Ek Geliştirmeler (Sonraki Aşama)

| # | Geliştirme | Öncelik | Notlar |
|---|---|---|---|
| 3.1 | Zone toplu etiket baskısı (`/zones/:id/cargo-labels`) | ORTA | Tüm zone'u tek print |
| 3.2 | YurtiKargo durum sorgulama butonu (manuel tetik) | ORTA | `QueryYkShipmentStatusCommand` frontend'e bağlanmadı |
| 3.3 | YK kargo raporu ekranı (`/yk-cargo-report`) | DÜŞÜK | Backend endpoint var, frontend yok |
| 3.4 | Barcode'u PDF olarak indirme (jsPDF) | DÜŞÜK | Gerekirse `window.print` yerine |
| 3.5 | YK API prod URL geçişi (`testws` → `ws`) | KRİTİK | Deploy öncesi config'de yapılmalı |

---

## Teknik Notlar

### YurtiKargo SOAP Yanıt Yapısı

`createShipment` başarılı yanıtında:
```xml
<outFlag>0</outFlag>         <!-- 0 = başarı, 1 = hata -->
<docId>...</docId>
<jobId>12345</jobId>
<barcode>9876543210</barcode> <!-- Kargo etiketi barkodu — Code128 formatı -->
<invoiceKey>2025/001234</invoiceKey>
```

### Barcode Formatı

YurtiKargo barkodları **Code128** formatındadır. JsBarcode kullanımı:

```typescript
import JsBarcode from 'jsbarcode';

JsBarcode('#barcode-svg', shipment.ykBarcode, {
  format: 'CODE128',
  width: 2,
  height: 80,
  displayValue: true,
  fontSize: 14,
});
```

### Idempotency Notu

`DispatchZoneAsCargo` handler'ı zaten idempotency kontrolü yapıyor:
- `YkOperationStatus` "0", "S" veya "SUCCESS" ise `createShipment` tekrar çağrılmaz
- `YkBarcode` zaten doluysa üzerine yazılmaz (bu davranış entity metodunda korunmalı)

### Config Uyarısı

`appsettings.json` içindeki YurtiKargo BaseUrl test ortamına işaret ediyor:
```
https://testws.yurticikargo.com/...
```
Canlıya geçmeden önce prod URL'e alınmalı ve `WsUserName` / `WsPassword` env değişkenleri set edilmeli.

---

## Uygulama Sırası

```
Faz 1.1 → 1.2 → 1.3 → 1.4 (migration) → 1.5   [Backend, ~2-3 saat]
     ↓
Faz 2.1 (npm install) → 2.3 (view) → 2.2 (route) → 2.5 (buton)   [Frontend, ~2-3 saat]
     ↓
Deploy → Faz 3 gereksinimlere göre
```

---

*Oluşturulma: 2026-05-14*
