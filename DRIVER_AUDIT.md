# Driver Delivery Pipeline — Güvenlik ve Doğruluk Denetimi

**Tarih:** 2026-03-25
**Kapsam:** Backend (C# / ASP.NET Core 10) + Frontend (Vue 3 / TypeScript)
**Odak:** Şoför teslimat akışı, iade akışı, güvenlik, izlenebilirlik, edge case'ler

---

## 1. Backend Güvenlik Denetimi

### BULGU 1.1 — KRİTİK: `mark-delivered` endpoint'inde rol kısıtı yok

**Dosya:** `ShipmentsController.cs:119`

```csharp
[HttpPost("{id:int}/mark-delivered")]
// [Authorize(Roles = "...")] YOK
public async Task<IActionResult> MarkDelivered(int id, [FromBody] MarkDeliveredRequest? request)
```

Sınıf seviyesinde `[Authorize]` var, ancak hiçbir rol filtresi uygulanmamış.
Bu durumda `Accounting` veya `Warehouse` rolündeki kullanıcılar bu endpoint'i çağırabilir.
Handler, `Driver` olmayan herkesi "override" koluna düşürür ve `OverrideNote` ister — ancak bu kolu **kimin kullanabileceğine dair hiçbir kontrol yoktur**.
Muhasebe çalışanı `Accounting` rolüyle `OverrideNote` girerek herhangi bir sevkiyatı teslim edildi olarak işaretleyebilir.

**Beklenen:** `[Authorize(Roles = "Admin,Manager,Dispatcher,Driver")]`

---

### BULGU 1.2 — ORTA: `mark-preparing` endpoint'inde de rol kısıtı yok

**Dosya:** `ShipmentsController.cs:95`

```csharp
[HttpPost("{id:int}/mark-preparing")]
// [Authorize(Roles = "...")] YOK
public async Task<IActionResult> MarkPreparing(int id)
```

Aynı sorun. Tüm authentikasyonlu kullanıcılar çağırabilir.

---

### BULGU 1.3 — ORTA: `GET /shipments/{id}/detail` herkese açık

**Dosya:** `ShipmentsController.cs:184`

```csharp
[HttpGet("{id:int}/detail")]
// [Authorize(Roles = "...")] YOK
public async Task<IActionResult> GetDetail(int id)
```

`Driver` rolündeki şoför, URL'deki ID'yi değiştirerek **başka bir şoföre atanmış** sevkiyatın tüm detaylarını (teslimat fotoğrafı, alıcı adı, adres, stok kalemleri) görebilir.
Handler'da ownership filtresi yok.

---

### BULGU 1.4 — DÜŞÜK: `GetDriverShipmentsQuery` ownership filtresi koşullu

**Dosya:** `GetDriverShipmentsQuery.cs:64`

```csharp
if (driverId.HasValue)
{
    query = query.Where(s => s.AssignedDriverId == driverId.Value);
}
```

`driverId` yalnızca `UserRole.Driver` rolünde set edilir. Admin/Dispatcher bu endpoint'i çağırırsa tüm aktif sevkiyatlar döner — bu kasıtlı tasarım gibi görünse de dokümantasyon yok ve yan etkilere neden olabilir.
`/driver/shipments` legacy endpoint'i Admin rolüyle çağrıldığında binlerce sevkiyat dönebilir.

---

### BULGU 1.5 — DÜŞÜK: `record-vehicle-return` endpoint'i Warehouse rolüne açık

**Dosya:** `ShipmentsController.cs:192`

```csharp
[Authorize(Roles = "Admin,Manager,Warehouse,Dispatcher,Driver")]
```

`Warehouse` çalışanı override notu girerek herhangi bir sevkiyatı iade edebilir.
Bu kasıtlı mı? `MarkDelivered`'dan farklı bir politika uygulanmış.

---

## 2. Teslimat Akışı Tutarlılığı

### BULGU 2.1 — KRİTİK: Bulk teslimatda alıcı zorunluluğu kontrolü yok

**Dosya:** `DriverStopView.vue:470`

```typescript
async function markAllDelivered() {
  bulkSubmitting.value = true;
  try {
    for (const shipment of pendingShipments.value) {
      await shipmentService.markDelivered(
        shipment.id,
        bulkForm.deliveryNote || undefined,
        bulkForm.deliveryRecipient || undefined,  // ← boş olabilir, kontrol yok!
        bulkForm.photoBase64 || undefined,
      );
    }
```

`bulkForm.deliveryRecipient` boş olduğunda backend her irsaliye için `DomainException` fırlatır. Şoför "Tümünü Teslim Et" butonuna basıp modal'ı onaylarsa hiçbir irsaliye teslim edilmez ama hata mesajı "Bir hata oluştu" der. Nedenini anlamak için tekrar denemesi gerekir.

Tek irsaliye teslimatında (`DriverDeliveryView.vue:280`) kontrol **var**:
```typescript
if (!form.value.deliveryRecipient?.trim()) {
  notify.add('Lütfen teslim alan kişi bilgisini giriniz.', 'warning');
  return;
}
```
Bulk modalde bu kontrol yoktur.

---

### BULGU 2.2 — ORTA: Bulk teslimat döngüsünde kısmi başarısızlık sessiz kalır

**Dosya:** `DriverStopView.vue:470`

```typescript
for (const shipment of pendingShipments.value) {
  await shipmentService.markDelivered(...);  // 3. irsaliye başarısız olursa
}
// catch bloğunda: notify.add('Bir hata oluştu...', 'error');
```

10 irsaliyeli bir durağın ilk 7'si teslim edilip 8.'si hata verirse (`Dispatched` değil, vs.), catch bloğu devreye girer. Kullanıcı 7 irsaliyenin teslim edildiğini görmez; `load()` çağrılır ve UI güncellenir ama "bir hata oluştu" mesajı hangi irsaliyenin sorun çıkardığını söylemez.

---

### BULGU 2.3 — ORTA: `pendingShipments` computed'ı `ReturnedToWarehouse` durumunu içeriyor

**Dosya:** `DriverStopView.vue:363`

```typescript
const pendingShipments = computed(() =>
  stop.value?.shipments.filter(s => s.status !== 'Delivered') ?? []
);
```

İade edilmiş irsaliyeler (`ReturnedToWarehouse`) bu listeye dahil olur.
"Tümünü Teslim Et (3 irsaliye)" butonu 3 gösterebilir ama bunlardan 1'i zaten iade edilmiştir.
Backend reddeder, hata çıkar.

**Beklenen filtre:**
```typescript
s.status !== 'Delivered' && s.status !== 'ReturnedToWarehouse'
```

---

### BULGU 2.4 — DÜŞÜK: `DriverDeliveryView` veriyi eski endpoint'ten yüklüyor

**Dosya:** `DriverDeliveryView.vue:228`

```typescript
const all = await driverService.getShipments();  // /driver/shipments (legacy)
shipment.value = all.find(s => s.id === shipmentId) ?? null;
```

`DriverStopView` artık `driverService.getRoute()` kullanıyor. Bu view eski `/driver/shipments` endpoint'ini kullanmaya devam ediyor.
Sonuç:
- `DriverShipmentDto` arayüzünde `irsaliyeNo` alanı yok — bilgi gösterilemiyor
- İki farklı endpoint'ten tutarsız veri gösterimi

---

### BULGU 2.5 — DÜŞÜK: Teslimat formu `AssignedToVehicle` durumunda da açık

**Dosya:** `DriverDeliveryView.vue:84`

```html
<!-- Delivery form (only when AssignedToVehicle) -->
<template v-else>
```

Yorum yanlış. Aslında `status !== 'Delivered'` olan her durumda form açılıyor.
`AssignedToVehicle` statüsündeki irsaliye için form doldurup gönderilirse backend "Only dispatched shipments can be delivered" hatası verir. Kullanıcı neden olmadığını anlayamaz.

---

## 3. İade Akışı Doğrulaması

### BULGU 3.1 — ORTA: Kısmi iade sonrası tekrar iade mümkün — UI bunu engellemiyor

`RecordVehicleReturnCommandHandler` kümülatif iade destekliyor (`line.ReturnedQty += dto.ReturnedQty`) ve `maxReturnable` validation ile fazla iade koruması var.
Ancak frontend `DriverStopView.vue`'da iade butonu her zaman görünür:

```html
<button @click.stop="router.push({ name: 'DriverReturn', params: { id: shipment.id } })">
  İade Et
</button>
```

Bu buton hem `Dispatched` hem `Delivered` durumunda aktif. Backend ikinci çağrıda hata vermez (kümülatif), bu doğru davranış. Ancak şoförün ekranda ne kadar iade ettiğini görmesi için UI'da kalan iade edilebilir miktar gösterilmiyor.

---

### BULGU 3.2 — ORTA: Frontend `RecordVehicleReturnRequest` arayüzünde `overrideNote` yok

**Dosya:** `shipmentService.ts:83`

```typescript
export interface RecordVehicleReturnRequest {
  lines: ReturnLineRequest[];
  returnNote?: string;
  // overrideNote eksik!
}
```

Backend `Dispatcher`/`Admin` rolünden gelen iade isteklerinde `OverrideNote` zorunlu tutuyor. Frontend bu alanı hiç göndermediği için:
- `Dispatcher` rolüyle UI üzerinden yapılan her iade isteği backend'de `DomainException` ile ret alır
- "Yönetici/Operasyon işlemi için Override Notu girmek zorunludur" hatası çıkar
- Dispatcher'ın UI'da override notu girebileceği bir alan da yok

---

### BULGU 3.3 — DÜŞÜK: `RecordVehicleReturn` döngüsünde `Dispatched` durumu henüz doğru işleniyor mu?

**Dosya:** `RecordVehicleReturnCommandHandler.cs:122`

```csharp
bool allReturned = shipment.Lines.All(l =>
{
    var delivered = l.DeliveredQty > 0 ? l.DeliveredQty : l.OrderedQty;
    return (l.ReturnedQty ?? 0) >= delivered;
});
```

`Dispatched` sevkiyat için `DeliveredQty` = 0, bu yüzden `OrderedQty` kullanılıyor. Bu mantıklı.
Tam iade yapılırsa durum `ReturnedToWarehouse`'a geçiyor. Stok geri ekleniyor.
Ancak bu sevkiyat için teslim edildiğinde daha önce `ShipmentOut` stok çıkışı yapılmamıştı (`Dispatched`'dan teslim ediliyordu), iade anında stok artışı yapılıyor. Stok dengesi **sıfır değişim** yerine **net artı** olur.

**Senaryo:**
1. Sevkiyat `Dispatched` olur → stok değişimi yok
2. Teslim olmadan doğrudan iade edilir → stok `Increase(orderedQty)` yapılır
3. Stok `orderedQty` kadar artar ama bu ürünler hiç çıkmamıştı

Bu muhtemelen bir mantık hatası. İade akışı sadece `Delivered` sevkiyatlar için tasarlanmış gibi görünüyor.

---

## 4. Audit / İzlenebilirlik

### BULGU 4.1 — ORTA: Kısmi iade için driver rolünde history kaydı yok

**Dosya:** `RecordVehicleReturnCommandHandler.cs:133`

```csharp
else if (isOverride)
{
    // Kısmi iade -> write audit history even if status doesn't change
    shipment.Histories.Add(...);
}
```

`isOverride = false` (yani şoför kendi iadesi) ve kısmi iade yapılmışsa history kaydı **hiç yazılmıyor**.
Şoför hangi kalemleri ne zaman iade ettiği izlenemiyor.

---

### BULGU 4.2 — DÜŞÜK: `DeliveredByUserId` ve `DeliveredByRole` tam olarak güvenilir mi?

**Dosya:** `MarkShipmentDeliveredCommand.cs:98`

```csharp
shipment.DeliveredByUserId = _currentUserService.UserId;
shipment.DeliveredByRole = _currentUserService.Role?.ToString();
```

`ICurrentUserService.Role` null gelebilir (`?.ToString()`). Null durumunda `DeliveredByRole` null olarak kaydediliyor.
Kimin override yaptığını araştırmak gerektiğinde bu alan bilgi veremiyor.

---

### BULGU 4.3 — DÜŞÜK: Override notu shipment history'de var ama shipment entity'de de tutuluyor — çift kayıt

`shipment.DeliveryOverrideNote` set ediliyor ve ayrıca `shipment.ChangeStatus(..., statusReason)` içinde history'e yazılıyor.
Bu tutarsızlığa neden olmaz ama sorgulama tarafında kafa karışıklığı yaratabilir.

---

## 5. Frontend ↔ Backend Kontrat

| Alan | Frontend | Backend | Sonuç |
|------|----------|---------|-------|
| `markDelivered` → `deliveryRecipient` | `string \| undefined` (optional) | Zorunlu (DomainException) | Backend guard var, frontend'de sadece tek teslimat formu validate ediyor |
| `recordVehicleReturn` → `overrideNote` | **Alan yok** | Dispatcher için zorunlu | Dispatcher UI'dan iade yapamaz |
| `markDelivered` → `overrideNote` | `shipmentService.markDelivered()` imzasında yok | Dispatcher için zorunlu | Dispatcher frontend'den teslim yapamaz |
| `StopShipmentDto.deliveryRecipient` | Tanımlı | Backend DTO'da mevcut | Eşleşiyor |
| `DriverShipmentDto.irsaliyeNo` | **Tanımsız** | `GetDriverShipmentsQuery`'de yok | Eski endpoint eksik |

**Kritik:** `shipmentService.markDelivered()` imzası:
```typescript
async markDelivered(id, deliveryNote?, deliveryRecipient?, deliveryPhotoBase64?): Promise<void>
```
`overrideNote` parametresi **yok**. Dispatcher veya Admin rolündeki kullanıcı herhangi bir araç üzerinden (Postman değil UI üzerinden) teslim kaydı yapamaz.

---

## 6. Edge Case'ler

### EC-1: Ağ kesintisinde çift gönderim

**Senaryo:** Şoför "Teslim Edildi" butonuna basar → istek sunucuya ulaşır ve işlenir → yanıt dönmeden ağ kesilir → uygulama "hata" gösterir → şoför tekrar basar.

**Mevcut durum:**
- `submitting` flag'i koruma sağlıyor: `if (submitting.value) return;`
- Ancak bu yalnızca aynı oturum içinde koruma sağlar. Sayfa yenilenirse flag sıfırlanır.
- Backend `if (shipment.Status == Delivered) return;` ile idempotent. Stok çift düşürülmez.
- **Sonuç: Backend korumalı, Frontend tek-oturum korumalı — kabul edilebilir.**

### EC-2: Aynı durağa iki şoför aynı anda teslim yapmaya çalışıyor

**Senaryo:** Aynı proje'ye atanmış iki irsaliye için iki farklı şoför (veya aynı şoför iki cihazdan) aynı anda form gönderiyor.

**Mevcut durum:**
Her sevkiyat ayrı ID'yle yönetiliyor. Bu senaryo olası değil (sevkiyat tek şoföre atanır), ancak `DbUpdateConcurrencyException` catch bloğu stok çakışmalarını yakalıyor.
**Sonuç: Düşük risk.**

### EC-3: Dispatcher teslim kaydı yaparken şoför de aynı anda teslim yapıyor

**Senaryo:** Şoför ekranda formu doldurmuş, dispatcher aynı anda override ile teslim ediyor.
Şoföre gönderim yanıtı geldiğinde `if (shipment.Status == Delivered) return;` devreye girer.
Stok çift düşürülmez. Şoförün ekranı güncellenmez (Vue reactive olmayan) ama `router.back()` sonrası DriverStopView yeniden yükler.
**Sonuç: Backend sağlam, frontend biraz stale kalabilir.**

### EC-4: Şoför oturum süresi biterken form gönderimi

**Senaryo:** Şoför formu doldurdu, butona bastı, JWT token tam bu anda expired.

**Mevcut durum:**
`apiClient.ts` response interceptor `api:unauthorized` eventi dispatch ediyor → `authStore.init()` dinliyor → logout yapıyor.
Form state kaybolur, şoför login sayfasına yönlendirilir.
Teslimat kaydedilmemiştir. Şoför login sonra tekrar formu doldurmak zorunda.
**Sonuç: UX kötü ama güvenlik açısından kabul edilebilir. Token süresi uzatılabilir.**

### EC-5: `AssignedToVehicle` durumundaki sevkiyata teslim formu açmak

**Senaryo:** Şoför route'u yükler, `ConfirmLoading` henüz yapılmamış (zone `ReadyForTransfer`'da), bir irsaliye `AssignedToVehicle` durumunda. Şoför formu doldurur, gönderir.

**Mevcut durum:**
Backend: `if (shipment.Status != ShipmentStatus.Dispatched) throw new DomainException("Only dispatched shipments can be delivered.");`
Frontend: `v-else` — form `AssignedToVehicle` için de açılıyor.
Sonuç: Backend 400 hatası, toast "bir hata oluştu" — şoför nedenini bilmiyor.
**Düzeltme:** Frontend'de `Dispatched` durumu olmayan irsaliyeler için form yerine "Sevkiyat yüklemesi onaylanmamış" mesajı gösterilmeli.

### EC-6: Fotoğraf sıkıştırılırken sayfa kapatılır

**Senaryo:** Şoför fotoğraf seçti, canvas sıkıştırma devam ederken sayfayı kapatır / geri gider.

**Mevcut durum:**
`photoCompressing` flag var ama sayfa kapanmasını engelleyen `beforeunload` yok.
Sıkıştırma async değil (senkron image callback). Sayfa kapanınca tamamlanmaz.
**Sonuç: Veri kaybı olabilir ama kritik değil — şoför tekrar fotoğraf çekebilir.**

### EC-7: Stale UI sonrası gereksiz API çağrısı

**Senaryo:** Şoför Stop sayfasındayken aynı stop'un tüm irsaliyeleri dispatcher tarafından teslim edildi olarak işaretlendi. Şoför "Tümünü Teslim Et" butonuna basar.

**Mevcut durum:**
Backend idempotency guard var → her irsaliye için `return;` döner, stok tekrar düşürülmez.
Ama loop'taki her istek backend'e gidiyor (gereksiz API çağrısı).
`load()` sonrası UI güncellenecek.
**Sonuç: Fonksiyonel olarak güvenli, performans açısından suboptimal.**

---

## 7. Final Değerlendirme

### Top 5 Risk

| # | Risk | Önem | Etki |
|---|------|------|------|
| **R1** | `mark-delivered` endpoint'inde rol kısıtı yok — Accounting rolü teslim kaydı yapabilir | **KRİTİK** | Yetkisiz teslimat kaydı, muhasebe manipülasyonu |
| **R2** | Bulk teslimat modalda `deliveryRecipient` zorunluluğu yok — backend hatası şeffaf değil | **KRİTİK** | Tüm irsaliyeler teslim edilemez, kullanıcı neden anlayamaz |
| **R3** | `overrideNote` frontend'den hiç gönderilemiyor (`shipmentService`, `RecordVehicleReturnRequest`) — Dispatcher rolü teslim/iade yapamaz | **YÜKSEK** | Dispatcher'ın UI üzerinden override işlemi tamamen kırık |
| **R4** | `GET /shipments/{id}/detail` herkese açık — Driver ID manipülasyonuyla başka şoförlerin teslimat fotoğraflarına erişilebilir | **YÜKSEK** | Veri sızıntısı, GDPR riski |
| **R5** | `Dispatched` olmayan irsaliye teslim formuna erişilebiliyor — backend reddediyor ama hata mesajı anlaşılır değil | **ORTA** | Kötü UX, şoför iş akışını tamamlayamadığını düşünebilir |

---

### Düzeltme Listesi (Öncelik Sırasına Göre)

#### Hemen (Kritik)
```csharp
// ShipmentsController.cs — mark-delivered endpoint'ine rol kısıtı ekle
[HttpPost("{id:int}/mark-delivered")]
[Authorize(Roles = "Admin,Manager,Dispatcher,Driver")]
```

```typescript
// DriverStopView.vue — markAllDelivered() başına ekle
if (!bulkForm.deliveryRecipient?.trim()) {
  notify.add('Lütfen teslim alan kişi bilgisini giriniz.', 'warning');
  return;
}
```

#### Kısa vadede (Yüksek)
```typescript
// shipmentService.ts — markDelivered imzasına overrideNote ekle
async markDelivered(id, deliveryNote?, deliveryRecipient?, deliveryPhotoBase64?, overrideNote?): Promise<void>

// RecordVehicleReturnRequest arayüzüne overrideNote ekle
export interface RecordVehicleReturnRequest {
  lines: ReturnLineRequest[];
  returnNote?: string;
  overrideNote?: string;  // ekle
}
```

```csharp
// ShipmentsController.cs
[HttpGet("{id:int}/detail")]
[Authorize(Roles = "Admin,Manager,Dispatcher,Warehouse,Accounting,Driver")]
// Driver için handler'a ownership check ekle
```

#### Orta vadede (Orta)
```typescript
// DriverStopView.vue — pendingShipments filtresini düzelt
const pendingShipments = computed(() =>
  stop.value?.shipments.filter(
    s => s.status !== 'Delivered' && s.status !== 'ReturnedToWarehouse'
  ) ?? []
);
```

```typescript
// DriverDeliveryView.vue — getShipments yerine getRoute kullan
// ve Dispatched olmayan irsaliye için form yerine bilgi mesajı göster
```

```csharp
// RecordVehicleReturnCommandHandler.cs — Driver kısmi iadesi için history kaydı ekle
if (!isOverride && dto.ReturnedQty > 0)
{
    shipment.Histories.Add(new ShipmentHistory { ... });
}
```

```csharp
// RecordVehicleReturnCommandHandler.cs — Dispatched sevkiyat iadesinde stok artışını engelle
// Stok, sadece daha önce ShipmentOut kaydı olan sevkiyatlar için artırılmalı
if (shipment.Status == ShipmentStatus.Delivered && line.StockMasterId.HasValue)
{
    stock.Increase(dto.ReturnedQty);
    // ...
}
```

---

**Sonuç:** Pipeline genel olarak mantıklı inşa edilmiş. Double-delivery koruması backend'de sağlam. Ownership modeli doğru uygulanmış. Tespit edilen kritik sorunlar birkaç satır kod ile giderilebilir ve öncelikli olarak ele alınmalıdır.
