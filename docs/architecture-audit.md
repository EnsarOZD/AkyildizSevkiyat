# Mimari Denetim Raporu — Akyildiz Sevkiyat

**Tarih:** 2026-03-13
**Denetçi:** Kıdemli Yazılım Mimarı (Claude Code)
**Kapsam:** Tüm kod tabanı — Backend (ASP.NET Core 10 / Clean Architecture) + Frontend (Vue 3 / TypeScript)

---

## Önem Dereceleri

| Seviye | Açıklama |
|---|---|
| 🔴 KRİTİK | Üretim ortamında güvenlik riski veya veri bütünlüğü ihlali |
| 🟠 YÜKSEK | İşlevsel hata veya önemli mimari ihlal |
| 🟡 ORTA | Pattern ihlali, teknik borç veya bakım sorunu |
| 🟢 DÜŞÜK | Küçük tutarsızlık veya stil sorunu |

---

## 1. Güvenlik Açıkları

### 🔴 KRİTİK — `IssOrdersController` üzerinde `[Authorize]` attribute yok

**Dosya:** `Akyildiz.Sevkiyat.WebApi/Controllers/IssOrdersController.cs:12`

```csharp
[ApiController]
[Route("api/[controller]")]
public class IssOrdersController : ControllerBase  // ← [Authorize] yok
```

Diğer tüm controller'lar sınıf veya metot seviyesinde `[Authorize]` kullanıyor. Bu controller'da hiç yok. Beş endpoint'in tamamı kimlik doğrulama olmadan erişilebilir durumda:

- `POST /api/issorders/import` — kimlik doğrulaması olmadan dış ISS-IP API çağrısı ve toplu veritabanı yazımı tetikler
- `GET /api/issorders` — proje, stok ve fiyat bilgilerini içeren tüm siparişleri açığa çıkarır
- `POST /api/issorders/{id}/toggle-active` — herhangi bir siparişi pasif yapabilir
- `POST /api/issorders/check-mappings` — stok eşleştirme mutasyonunu tetikler
- `GET /api/issorders/counts` — operasyonel metrikleri açığa çıkarır

API'ye ağ erişimi olan herkes bu işlemlerin tamamını anonim olarak gerçekleştirebilir.

---

### 🔴 KRİTİK — `PurchaseOrdersController` var olmayan bir rol kullanıyor

**Dosya:** `Akyildiz.Sevkiyat.WebApi/Controllers/PurchaseOrdersController.cs:16`

```csharp
[Authorize(Roles = "Admin,WarehouseManager")]
```

`UserRole` enum'u şunları içeriyor: `Admin`, `Accounting`, `Warehouse`, `Dispatcher`. `WarehouseManager` diye bir rol yok. ASP.NET Core'un rol yetkilendirmesi, JWT claim'indeki değerle string karşılaştırması yapar. Claim değeri her zaman `Warehouse` olacağından (asla `WarehouseManager` olmaz), depo çalışanları satın alma siparişlerine hiçbir zaman erişemez. **Sonuç olarak yalnızca Admin bu modülü kullanabilir** ve asıl hedef kullanıcılar tamamen kilitlenir.

---

### 🔴 KRİTİK — Frontend'de kullanılan `Manager` rolü backend'de mevcut değil

**Dosya:** `client/src/router/index.ts:42,48,54,59`

```typescript
meta: { roles: ['Admin', 'Manager'] }                    // Zones, ProjectMapping
meta: { roles: ['Admin', 'Accounting', 'Manager'] }      // Orders, Stocks, Suppliers, POs
meta: { roles: ['Admin', 'Warehouse', 'Manager'] }       // Warehouse, GoodsReceipts
```

Backend `UserRole` enum'unda `Manager` değeri yok. `v-role` directive'i ve router guard'ı, `authStore.userRole`'dan JWT claim string'ini okur. Hiçbir kullanıcı `role = "Manager"` değerine sahip olamayacağından bu sayfalar ve UI elementleri `Manager` için kalıcı olarak engellenmiş durumda. Frontend ve backend rol sistemleri birbiriyle uyumsuz.

---

### 🟠 YÜKSEK — `JwtTokenService`, doğrulanmış `IOptions<JwtOptions>` kullanmıyor

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/Security/JwtTokenService.cs:12-38`

```csharp
public JwtTokenService(IConfiguration _configuration)  // ham IConfiguration okuyor
...
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
```

`JwtOptions`, `[Required]` ve uzunluk doğrulamasıyla uygulama başlangıcında validate ediliyor. Ancak `JwtTokenService` bunu tamamen atlayarak `IConfiguration`'ı doğrudan okuyor. `Jwt:Key` null veya kısa olursa `!` operatörü, uygulama başarıyla başladıktan sonra token üretimi sırasında `NullReferenceException` fırlatır. `IOptions<JwtOptions>` inject edilmesi gerekiyor.

---

### 🟠 YÜKSEK — Tüm seed kullanıcıları aynı admin şifresini paylaşıyor

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/Persistence/Seeding/UserSeeder.cs:16-21`

```csharp
CreateUser("Fatma", "Yılmaz", "accounting@akyildiz.com", UserRole.Accounting, passwordHasher, adminPassword),
CreateUser("Mehmet", "Depo",  "warehouse@akyildiz.com",  UserRole.Warehouse,  passwordHasher, adminPassword),
CreateUser("Ali",   "Dağıtıcı","dispatcher@akyildiz.com",UserRole.Dispatcher, passwordHasher, adminPassword)
```

Dört kullanıcının tamamı config'deki aynı `adminPassword` ile oluşturuluyor. Bu şifre zayıf veya paylaşımlıysa tüm hesaplar aynı ölçüde risk altında. `Warehouse` ve `Dispatcher` rollerinin daha düşük ayrıcalıklı şifreleri veya kullanıcı başına ilk şifre mekanizması olmalı. Sistemde şifre değiştirme/sıfırlama endpoint'i de bulunmuyor.

---

### 🟠 YÜKSEK — JWT token'larının iptal mekanizması yok

`JwtTokenService` stateless token üretiyor. Token blacklist, refresh token rotasyonu veya oturum geçersizleştirme mekanizması yok. Çalınan bir token süresi dolana kadar geçerliliğini koruyor. Şifre veya rol değişikliği sonrasında zorla çıkış yaptırmanın süresi dolmayı beklemek dışında yolu yok.

---

## 2. Yetkilendirme Sorunları

### 🔴 KRİTİK — Kritik sevkiyat endpoint'lerinde rol kısıtlaması yok

**Dosya:** `Akyildiz.Sevkiyat.WebApi/Controllers/ShipmentsController.cs`

Sınıfta `[Authorize]` var, ancak aşağıdaki endpoint'lerde "herhangi bir kimlik doğrulanmış kullanıcı" ötesinde **rol kısıtlaması yok**:

| Endpoint | Etki |
|---|---|
| `POST /shipments` | Herhangi bir rol sevkiyat oluşturabilir |
| `POST /shipments/bulk` | Herhangi bir rol toplu oluşturma yapabilir |
| `POST /{id}/mark-preparing` | Herhangi bir rol depo statüsüne alabilir |
| `POST /{id}/mark-delivered` | Herhangi bir rol teslim edildi işaretleyebilir |
| `PATCH /{id}/lines/{lineId}/delivered` | Herhangi bir rol teslim miktarlarını güncelleyebilir |

`Dispatcher` sevkiyat oluşturamamalı. `Warehouse` çalışanı sevkiyatı dispatch sürecinden geçmeden teslim olarak işaretleyememeli. Rol modeli var ama bu kritik yazma işlemlerine uygulanmamış.

---

### 🟠 YÜKSEK — `WarehouseController` hiçbir endpoint'te rol kısıtlaması yok

**Dosya:** `Akyildiz.Sevkiyat.WebApi/Controllers/WarehouseController.cs:19`

Sınıf seviyesinde `[Authorize]` var ama hiçbir endpoint'te rol kısıtlaması yok. `mark-macro-ready`, `set-driver-info`, `allocate-macro-shortage`, `update-aggregated-lines` ve `start-zone-preparation` gibi işlemler `Dispatcher` ve `Accounting` dahil herhangi bir kimlik doğrulanmış kullanıcı tarafından yapılabilir.

---

## 3. Clean Architecture İhlalleri

### 🔴 KRİTİK — Domain katmanı MediatR'a (dış kütüphane) bağımlı

**Dosya:** `Akyildiz.Sevkiyat.Domain/Common/IDomainEvent.cs`

```csharp
using MediatR;

public interface IDomainEvent : INotification  // ← Domain, MediatR'a referans veriyor
```

Domain katmanı en içteki halka olup **sıfır dış bağımlılığa** sahip olmalıdır. `INotification`'ı extend ederek Domain, `MediatR.Contracts` NuGet paketine bağımlı hale geldi. MediatR değiştirilirse veya breaking change içeren bir sürüme geçilirse Domain katmanının da değişmesi gerekir — bu, Clean Architecture'ın özündeki Bağımlılık Tersine Çevirme İlkesi'ni (DIP) ihlal eder.

`IDomainEvent` kalıtım içermeyen sade bir marker interface olmalı. Infrastructure/Application katmanı bunu sınır noktasında `INotification`'a adapte edebilir.

---

### 🟠 YÜKSEK — `Shipment` entity'si `AuditableEntity` kullanmıyor

**Dosya:** `Akyildiz.Sevkiyat.Domain/Entities/Shipment.cs:20-21`

```csharp
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // AuditableEntity'yi tekrarlıyor
public string? CreatedBy { get; set; }
// LastModified, LastModifiedBy yok
```

`AuditableEntity` yerleşik base class olarak mevcut, ancak `Shipment` onu kullanmıyor; dört alanından ikisini manuel olarak kopyalamış, diğer ikisini atlamış. Pek çok başka entity de `AuditableEntity`'yi extend etmiyor. Denetim pattern'i tanımlanmış ama uygulanmamış.

---

### 🟡 ORTA — `AuditableEntity` alanları hiçbir zaman otomatik doldurulmuyor

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/Persistence/SevkiyatDbContext.cs`

`AuditableEntity`; `CreatedAt`, `CreatedBy`, `LastModified`, `LastModifiedBy` alanlarını tanımlıyor. Ancak `SevkiyatDbContext`'teki `SaveChangesAsync`, `ChangeTracker` girdilerini `ICurrentUserService`'ten bu alanları otomatik doldurmak için yakalamıyor. Handler'lar bunları manuel olarak set etmek zorunda ve bir kısmı bunu yapmıyor. Denetim izi eksik ve güvenilmez.

---

### 🟡 ORTA — `OnModelCreating`'deki seed verisi `DateTime.Today` kullanıyor

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/Persistence/SevkiyatDbContext.cs:224-232`

```csharp
modelBuilder.Entity<IssOrder>().HasData(new IssOrder
{
    OrderDate    = DateTime.Today,           // ← migration anında değerlendiriliyor
    DeliveryDate = DateTime.Today.AddDays(1) // ← migration'larda belirsiz değer
});
```

`HasData` seed değerleri migration snapshot'larına gömülür. `DateTime.Today`, kodun son yeniden oluşturulduğu tarihi yakalar — bu değer artık geçersiz ve belirsiz. Sabit bir tarih sabiti kullanılmalı ya da seed veritabanı migration'lardan tamamen kaldırılmalı.

---

### 🟡 ORTA — Birincil anahtar tipi tutarsızlığı: `int` ve `Guid` karışımı

`Shipment`, `ShipmentLine`, `IssOrder`, `Zone`, `User` — hepsi `int` PK kullanıyor. `PurchaseOrder`, `GoodsReceipt`, `Supplier` ise `Guid` kullanıyor. Bu tutarsızlık (büyük ihtimalle sonradan ayrı bir modül olarak eklenmesinden kaynaklanıyor) sorgu, ilişki ve frontend servis tiplemesinde sürtüşme yaratıyor. Karışık kullanım için mimari bir gerekçe yok.

---

## 4. Mantık / Doğruluk Hataları

### 🟠 YÜKSEK — `MarkShipmentPreparingCommand` yanlış statüye geçiyor

**Dosya:** `Akyildiz.Sevkiyat.Application/Shipments/Commands/MarkShipmentPreparing/MarkShipmentPreparingCommand.cs:32`

```csharp
// Command "MarkShipmentPreparing" adında ama AssignedToWarehouse statüsüne geçiyor
shipment.ChangeStatus(ShipmentStatus.AssignedToWarehouse, _currentUserService.UserId);
```

Command adı, controller endpoint'i (`POST /{id}/mark-preparing`) ve frontend çağrısının tamamı ileri bir adımı çağrıştırıyor. Ancak handler statüyü `AssignedToWarehouse`'a — iş akışının 2. adımına — alıyor. Bu ya `AssignToWarehouseCommandHandler`'dan copy-paste hatası ya da temel bir isimlendirme karışıklığıdır. Şu an **iki ayrı command** (`AssignToWarehouseCommand` ve `MarkShipmentPreparingCommand`) tam olarak aynı işi yapıyor ve aynı durum geçişi için yinelenen bir kod yolu oluşturuyor.

---

### 🟠 YÜKSEK — Domain event'ler veritabanı commit'inden SONRA yayınlanıyor

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/Persistence/SevkiyatDbContext.cs:298-305`

```csharp
public override async Task<int> SaveChangesAsync(...)
{
    var result = await base.SaveChangesAsync(cancellationToken); // ← DB burada commit edildi
    await DispatchDomainEvents(cancellationToken);               // ← Event'ler commit'ten sonra
    return result;
}
```

`base.SaveChangesAsync` başarıyla tamamlandıktan sonra bir event handler exception fırlatırsa veritabanı değişiklikleri kalıcı olarak commit edilmiş ama event yan etkileri hiçbir zaman tamamlanmamış olur. Bu atomikliği bozar. Standart yaklaşım event'leri save'den önce yayınlamak (transaction geri almayı sağlamak için) ya da transactional outbox pattern kullanmaktır.

---

### 🟠 YÜKSEK — `CreateShipmentCommandHandler`'da yarış koşulu

**Dosya:** `Akyildiz.Sevkiyat.Application/Shipments/Commands/CreateShipmentCommandHandler.cs:37-52`

```csharp
if (order.IsTransferred)          // Kontrol 1: bellekteki flag
    throw new ConflictException();

var existingShipment = await _context.Shipments.AnyAsync(...); // Kontrol 2: DB sorgusu
if (existingShipment) { ... }
```

`Shipment.IssOrderId` üzerinde veritabanı seviyesinde unique constraint yok ve iki ardışık kontrol yapılıyor. Eş zamanlı istekler her iki kontrolü de geçebilir (her ikisi de `IsTransferred = false` okur) ve her ikisi de sevkiyat oluşturmaya devam eder. `Shipments.IssOrderId` üzerine unique index eklemek bu sorunu kod değişikliği gerektirmeden veritabanı seviyesinde çözer.

---

### 🟡 ORTA — `MarkShipmentDelivered`, `Lines`'ı gereksiz yere yüklüyor

**Dosya:** `Akyildiz.Sevkiyat.Application/Shipments/Commands/MarkShipmentDelivered/MarkShipmentDeliveredCommand.cs:27`

```csharp
var shipment = await _context.Shipments
    .Include(s => s.Lines)  // ← yükleniyor ama hiç erişilmiyor
    .FirstOrDefaultAsync(...);
```

Handler yalnızca `shipment.ChangeStatus()` çağırıyor; bu metot sadece `Status` üzerinde çalışıyor. `Lines` include'u gereksiz bir performans maliyeti.

---

### 🟡 ORTA — `CreateShipmentCommandHandler`, exception fırlatmadan önce kayıt yapıyor

**Dosya:** `Akyildiz.Sevkiyat.Application/Shipments/Commands/CreateShipmentCommandHandler.cs:49-52`

```csharp
order.IsTransferred = true;
await _context.SaveChangesAsync(cancellationToken);  // ← kaydediyor, aşağıda exception fırlatıyor
throw new ConflictException("...(Veri bütünlüğü sağlandı)");
```

Exception fırlatmadan önce `SaveChangesAsync` çağrılıyor. Çağıran `ConflictException` alıyor ama veritabanına yazma zaten gerçekleşmiş. Bu durum belgelenmeli veya yeniden düzenlenmeli.

---

## 5. Performans Riskleri

### 🟡 ORTA — `SevkiyatDbContext`, EF Core model uyarısını susturuyor

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/Persistence/SevkiyatDbContext.cs:22-24`

```csharp
optionsBuilder.ConfigureWarnings(w =>
    w.Ignore(RelationalEventId.PendingModelChangesWarning));
```

`PendingModelChangesWarning`, uygulanmamış migration'lar olduğunda tetiklenir. Düzeltmek yerine susturmak, sistemin uyumsuz bir model ile çalışıyor olabileceği ve geliştiricilerin uyarıyı asla göremeyeceği anlamına gelir.

---

### 🟡 ORTA — Yüksek frekanslı FK sütunlarında index eksikliği

Handler'lardaki sorgu örüntüleri incelendiğinde, birçok join/filtre sütununun açık index'ten yoksun olduğu görülüyor:

- `IssOrders.ProjectId` — neredeyse her sipariş sorgusunda kullanılıyor
- `Shipments.IssOrderId` — `CreateShipmentCommandHandler`'da sorgulanıyor (yarış koşulu için unique constraint da buraya eklenmeli)
- `ShipmentLines.IssOrderLineId` — toplama/teslimat işlemlerinde kullanılıyor
- `StockMappings.LocalStockId` — import işlemlerinde kullanılıyor

---

### 🟡 ORTA — `OnModelCreating` 290 satırlık bir monolit

Tüm entity konfigürasyonları, ilişki fluent API'leri, decimal hassasiyeti ve seed verisi tek bir metotta toplanmış. Yalnızca `PurchaseOrderConfiguration` ayrı bir dosyaya çıkarılmış. EF Core'un `IEntityTypeConfiguration<T>` pattern'i tüm entity'lere tutarlı biçimde uygulanmalı.

---

## 6. Teknik Borç

### 🟡 ORTA — Hiç test projesi yok

Solution'da hiç test projesi bulunmuyor. Karmaşık domain mantığı göz önüne alındığında (Shipment durum makinesi, PurchaseOrder numarası üretimi, stok import ayrıştırma, yinelenen irsaliye tespiti) bu önemli bir risk. `Shipment.ValidateTransition`'daki durum makinesi ve import handler özellikle birim test kapsamına ihtiyaç duyuyor.

---

### 🟡 ORTA — Token yenileme mekanizması yok

Auth sistemi 60 dakikalık JWT (varsayılan) üretiyor. Refresh token endpoint'i, sliding expiration veya `POST /api/auth/refresh` yok. Token süresi dolduğunda frontend otomatik çıkış yapıyor ve yeniden giriş gerekiyor. Gün boyu sürekli kullanılan bir lojistik iş akışı uygulaması için bu kötü bir kullanıcı deneyimi ve operasyonel bir eksiklik.

---

### 🟡 ORTA — Şifre değiştirme / sıfırlama endpoint'i yok

`UserSeeder` kullanıcıları paylaşılan bir şifreyle oluşturuyor. Sistemde giriş var ama `ChangePassword` komutu, `ForgotPassword` akışı veya admin tarafından yönetilen şifre sıfırlama yok. Kullanıcılar veritabanında doğrudan değişiklik yapılmadıkça seed şifrelerine sonsuza kadar kilitli kalıyor.

---

### 🟢 DÜŞÜK — `HelloWorld.vue` scaffold şablon bileşeni üretim kaynak kodunda mevcut

**Dosya:** `client/src/components/HelloWorld.vue`

Kullanılmayan Vite şablon artifact'ı. Hiçbir view veya bileşen tarafından import edilmiyor.

---

### 🟢 DÜŞÜK — Proje kökünde gereksiz `AkyildizSevkiyat/` dizini var

**Dosya:** `AkyildizSevkiyat/README.md`

Yalnızca bir `README.md` içeren iç içe geçmiş bu dizin, amaçsız bir scaffold kalıntısı gibi görünüyor.

---

## 7. Bağımlılık Yönü Özeti

```
Domain
  └── ❌ MediatR'a bağımlı (IDomainEvent : INotification)
       Sıfır dış bağımlılığa sahip olmalı

Application
  └── ✅ Domain'e bağımlı (doğru)
  └── ✅ FluentValidation'a bağımlı (bu katmanda kabul edilebilir)
  └── ✅ MediatR'a bağımlı (doğru)
  └── ✅ EF Core'a bağımlı (yalnızca interface üzerinden, doğru)

Infrastructure
  └── ✅ Domain'e bağımlı (doğru)
  └── ✅ Application'a bağımlı (doğru — interface'leri implemente ediyor)
  └── ❌ JwtTokenService, IOptions<JwtOptions> yerine IConfiguration kullanıyor
       (Infrastructure'ın kendi tanımladığı doğrulanmış options modelini atlıyor)

WebApi
  └── ✅ Application'a bağımlı (doğru)
  └── ✅ Infrastructure'a bağımlı (composition root, doğru)

Frontend
  └── ✅ Yalnızca HTTP API üzerinden iletişim kuruyor (doğru izolasyon)
  └── ❌ 'Manager' rol string'i backend enum değerleriyle uyumsuz
```

---

## 8. Öncelikli Düzeltme Planı

| Öncelik | Önem | Sorun | Dosya(lar) |
|---|---|---|---|
| 1 | 🔴 | `IssOrdersController`'a `[Authorize]` ekle | `IssOrdersController.cs` |
| 2 | 🔴 | Rol yazım hatasını düzelt: `WarehouseManager` → `Warehouse` | `PurchaseOrdersController.cs` |
| 3 | 🔴 | `UserRole` enum'una `Manager` ekle veya frontend route'lardan kaldır | `UserRole.cs`, `router/index.ts` |
| 4 | 🔴 | `IDomainEvent : INotification` kalıtımını kaldır — Domain'deki MediatR bağımlılığını kopar | `IDomainEvent.cs`, `SevkiyatDbContext.cs` |
| 5 | 🔴 | `POST /shipments`, `/mark-preparing`, `/mark-delivered`'a rol kısıtlaması ekle | `ShipmentsController.cs` |
| 6 | 🟠 | `MarkShipmentPreparingCommand`'ı düzelt — yanlış durum geçişi veya yinelenen command'ı kaldır | `MarkShipmentPreparingCommand.cs` |
| 7 | 🟠 | `JwtTokenService`'i `IOptions<JwtOptions>` kullanacak şekilde güncelle | `JwtTokenService.cs` |
| 8 | 🟠 | `WarehouseController` endpoint'lerine rol kısıtlaması ekle | `WarehouseController.cs` |
| 9 | 🟠 | Yarış koşulunu çözmek için `Shipments.IssOrderId`'ye unique index ekle | Yeni migration |
| 10 | 🟠 | Domain event'leri `base.SaveChangesAsync`'ten önce yayınla veya outbox pattern uygula | `SevkiyatDbContext.cs` |
| 11 | 🟡 | Tüm entity konfigürasyonlarını `IEntityTypeConfiguration<T>` sınıflarına taşı | Infrastructure/Persistence |
| 12 | 🟡 | `SaveChangesAsync`'te `AuditableEntity` alanlarını otomatik doldur | `SevkiyatDbContext.cs` |
| 13 | 🟡 | Durum makinesi, PO numarası üretimi ve import mantığı için birim testler yaz | Yeni test projesi |
| 14 | 🟡 | Token yenileme endpoint'ini implemente et | Yeni `RefreshTokenCommand` |
| 15 | 🟡 | `HelloWorld.vue` ve `AkyildizSevkiyat/` dizinini kaldır | Temizlik |

---

## Genel Değerlendirme

Kod tabanı sağlam bir mimari niyet sergilıyor — Clean Architecture katmanlaması, MediatR üzerinden CQRS, doğru durum makinesiyle zengin bir domain modeli, tutarlı hata yönetimi ve iyi yapılandırılmış bir Vue frontend. Temeller güçlü.

Ancak üretim ortamında kullanılmadan önce mutlaka giderilmesi gereken **üç kritik güvenlik açığı** var: tamamen kimlik doğrulamasız ISS siparişleri controller'ı, depo kullanıcılarını satın alma siparişlerinden kilitleyen var olmayan `WarehouseManager` rolü ve frontend ile backend arasındaki uyumsuz `Manager` rolü. Bunlar teorik riskler değil — şu an istismar edilebilir durumdalar.

Domain katmanının MediatR bağımlılığı, en önemli mimari ilke ihlalidir ve ileride iç halka bağımlılıkları için kötü bir emsal oluşturuyor.
