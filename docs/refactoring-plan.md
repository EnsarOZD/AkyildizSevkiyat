# Güvenli Adım Adım Refactoring Planı

**Kaynak:** architecture-audit.md
**Tarih:** 2026-03-13
**Hedef:** Üretim davranışını bozmadan mimari ve güvenlik sorunlarını gidermek

---

## Genel Strateji

Her değişiklik aşağıdaki kurallara uyar:

1. **Küçük commit'ler** — her adım bağımsız olarak deploy edilebilir
2. **Davranış değişmez** — mevcut iş mantığı korunur, yalnızca güvenlik/yapı düzeltilir
3. **Önce güvenlik** — kritik açıklar ilk aşamada kapatılır
4. **Migration gereksinimini ertele** — DB migration gerektiren adımlar ayrı bir aşamaya alınır
5. **Frontend ve backend değişiklikleri birlikte deploy edilir**

---

## Aşama 1 — Kritik Güvenlik Açıklarını Kapat (Migration yok, davranış değişmez)

Bu adımların tamamı attribute ekleme veya string düzeltmesidir. Rolleri doğru olan kullanıcılar için hiçbir şey değişmez; yalnızca yetkisiz erişim engellenir.

---

### Adım 1.1 — `IssOrdersController`'a `[Authorize]` ekle

**Dosya:** `Akyildiz.Sevkiyat.WebApi/Controllers/IssOrdersController.cs`

```csharp
// ÖNCE
[ApiController]
[Route("api/[controller]")]
public class IssOrdersController : ControllerBase

// SONRA
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class IssOrdersController : ControllerBase
```

**Etki:** Anonim erişim kalıcı olarak engellenir. Tüm mevcut kullanıcılar token gönderdikleri sürece etkilenmez.

**Import eklenmesi gerekebilir:**
```csharp
using Microsoft.AspNetCore.Authorization;
```

---

### Adım 1.2 — `PurchaseOrdersController`'daki rol yazım hatasını düzelt

**Dosya:** `Akyildiz.Sevkiyat.WebApi/Controllers/PurchaseOrdersController.cs`

```csharp
// ÖNCE — "WarehouseManager" diye bir rol yok, Warehouse kullanıcıları erişemez
[Authorize(Roles = "Admin,WarehouseManager")]

// SONRA
[Authorize(Roles = "Admin,Accounting,Warehouse")]
```

**Etki:** `Warehouse` kullanıcıları ilk kez satın alma siparişlerine erişebilir (zaten bekledikleri davranış bu). `Admin` erişimi değişmez.

---

### Adım 1.3 — Backend'e `Manager` rolünü ekle

`Manager`, `Accounting` ve `Warehouse`'un birleşimi olan bir üst yönetici rolüdür. Backend enum'una eklenmesi gerekiyor.

**Dosya:** `Akyildiz.Sevkiyat.Domain/Enums/UserRole.cs`

```csharp
// ÖNCE
public enum UserRole
{
    Admin      = 0,
    Accounting = 1,
    Warehouse  = 2,
    Dispatcher = 3
}

// SONRA
public enum UserRole
{
    Admin      = 0,
    Accounting = 1,
    Warehouse  = 2,
    Dispatcher = 3,
    Manager    = 4   // Warehouse + Accounting yetkilerini kapsar
}
```

**Etki:** Mevcut kullanıcıların JWT claim'leri değişmez. Sadece artık `Manager` rolüne atanabilen kullanıcılar oluşturulabilir.

**Not:** Bu değişiklikten sonra `PurchaseOrdersController`'ı da güncelle:

```csharp
[Authorize(Roles = "Admin,Accounting,Warehouse,Manager")]
```

---

### Adım 1.4 — `ShipmentsController`'da eksik rol kısıtlamalarını ekle

**Dosya:** `Akyildiz.Sevkiyat.WebApi/Controllers/ShipmentsController.cs`

```csharp
// ÖNCE — hiç rol kısıtlaması yok
[HttpPost]
public async Task<IActionResult> Create(...)

[HttpPost("bulk")]
public async Task<IActionResult> CreateBulk(...)

[HttpPost("{id:int}/mark-preparing")]
public async Task<IActionResult> MarkPreparing(int id)

[HttpPost("{id:int}/mark-delivered")]
public async Task<IActionResult> MarkDelivered(int id)

[HttpPatch("{id:int}/lines/{lineId:int}/delivered")]
public async Task<IActionResult> UpdateDeliveredQty(...)

// SONRA
[HttpPost]
[Authorize(Roles = "Admin,Accounting,Manager")]
public async Task<IActionResult> Create(...)

[HttpPost("bulk")]
[Authorize(Roles = "Admin,Accounting,Manager")]
public async Task<IActionResult> CreateBulk(...)

[HttpPost("{id:int}/mark-preparing")]
[Authorize(Roles = "Admin,Accounting,Warehouse,Manager")]
public async Task<IActionResult> MarkPreparing(int id)

[HttpPost("{id:int}/mark-delivered")]
[Authorize(Roles = "Admin,Dispatcher,Manager")]
public async Task<IActionResult> MarkDelivered(int id)

[HttpPatch("{id:int}/lines/{lineId:int}/delivered")]
[Authorize(Roles = "Admin,Warehouse,Dispatcher,Manager")]
public async Task<IActionResult> UpdateDeliveredQty(...)
```

---

### Adım 1.5 — `WarehouseController`'a rol kısıtlamaları ekle

**Dosya:** `Akyildiz.Sevkiyat.WebApi/Controllers/WarehouseController.cs`

```csharp
// Okuma endpoint'leri — Warehouse + üstü
[HttpGet("dashboard")]
[Authorize(Roles = "Admin,Warehouse,Manager")]
public async Task<...> GetDashboard(...)

[HttpGet("dashboard-all")]
[Authorize(Roles = "Admin,Warehouse,Manager")]
public async Task<...> GetDashboardAll(...)

[HttpGet("micro-pick-list")]
[Authorize(Roles = "Admin,Warehouse,Manager")]
public async Task<...> GetMicroPickList(...)

[HttpGet("macro-pick-list")]
[Authorize(Roles = "Admin,Warehouse,Manager")]
public async Task<...> GetMacroPickList(...)

// Yazma endpoint'leri — Warehouse + üstü
[HttpPost("dashboard/sync")]
[Authorize(Roles = "Admin,Warehouse,Manager")]
public async Task<...> SyncDashboard(...)

[HttpPost("zone-preparation/initialize")]
[Authorize(Roles = "Admin,Warehouse,Manager")]
public async Task<...> InitializeZonePreparation(...)

[HttpPost("mark-micro-ready")]
[Authorize(Roles = "Admin,Warehouse,Manager")]
public async Task<...> MarkMicroReady(...)

[HttpPost("update-micro-lines-bulk")]
[Authorize(Roles = "Admin,Warehouse,Manager")]
public async Task<...> UpdateMicroLinesBulk(...)

[HttpPost("update-aggregated-lines")]
[Authorize(Roles = "Admin,Warehouse,Manager")]
public async Task<...> UpdateAggregatedLines(...)

[HttpPost("allocate-macro-shortage")]
[Authorize(Roles = "Admin,Warehouse,Manager")]
public async Task<...> AllocateMacroShortage(...)

[HttpPost("mark-macro-ready")]
[Authorize(Roles = "Admin,Warehouse,Manager")]
public async Task<...> MarkMacroReady(...)

// Sürücü bilgisi — Dispatcher da yapabilmeli
[HttpPost("set-driver-info")]
[Authorize(Roles = "Admin,Warehouse,Dispatcher,Manager")]
public async Task<...> SetDriverInfo(...)

[HttpPost("start-zone-preparation")]
[Authorize(Roles = "Admin,Warehouse,Manager")]
public async Task<...> StartZonePreparation(...)
```

---

### Adım 1.6 — Frontend rol tanımını backend ile senkronize et

**Dosya:** `client/src/navigation.ts` — zaten `Manager` var, değişiklik yok.

**Dosya:** `client/src/router/index.ts` — zaten `Manager` var, değişiklik yok.

**Dosya:** `client/src/directives/vRole.ts` — zaten `Manager` var, değişiklik yok.

> **Not:** Backend'e `Manager` rolü eklendiği anda (Adım 1.3) frontend otomatik olarak doğru çalışır. Frontend tarafında değişiklik gerekmez.

---

**✅ Aşama 1 tamamlandıktan sonra deploy edilebilir. Mevcut hiçbir kullanıcının çalışan işlevi kırılmaz.**

---

## Aşama 2 — Altyapı Güvenliği (Migration yok, yeniden başlatma yeterli)

---

### Adım 2.1 — `JwtTokenService`'i `IOptions<JwtOptions>` kullanacak şekilde güncelle

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/Security/JwtTokenService.cs`

```csharp
// ÖNCE
public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        ...
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(_configuration["Jwt:ExpiresInMinutes"]!)),
            ...
        );
    }
}

// SONRA
public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public string GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.Key));
        ...
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.ExpiresInMinutes),
            ...
        );
    }
}
```

**Gerekli import:**
```csharp
using Microsoft.Extensions.Options;
```

**Etki:** Uygulama başlangıcındaki doğrulama artık token üretimini de güvence altına alır. Token üretimi davranışı değişmez.

---

### Adım 2.2 — `JwtOptions` üzerindeki `ExpiresInMinutes` tipini `double` yap

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/Security/JwtOptions.cs`

```csharp
// ÖNCE (muhtemelen string veya int)
public string ExpiresInMinutes { get; set; }

// SONRA
public double ExpiresInMinutes { get; set; } = 60;
```

Bu değişiklik `double.Parse()` çağrısını ortadan kaldırır ve tip güvenliği sağlar.

---

**✅ Aşama 2 tamamlandıktan sonra deploy edilebilir. Token davranışı değişmez.**

---

## Aşama 3 — Mantık Hatalarını Düzelt (Migration yok)

---

### Adım 3.1 — `MarkShipmentPreparingCommand` yinelenmesini çöz

`POST /api/shipments/{id}/mark-preparing` ve `POST /api/shipments/{id}/assign-to-warehouse` aynı durum geçişini (`AssignedToWarehouse`) yapıyor. İki endpoint'in tek farkı rol kısıtlamasıydı; Adım 1.4'te bu giderildi.

Şimdi `MarkShipmentPreparingCommand`'ı kaldırıp controller'ı `AssignToWarehouseCommand`'ı yeniden kullanacak şekilde güncelle.

**Dosya:** `Akyildiz.Sevkiyat.WebApi/Controllers/ShipmentsController.cs`

```csharp
// ÖNCE
[HttpPost("{id:int}/mark-preparing")]
[Authorize(Roles = "Admin,Accounting,Warehouse,Manager")]
public async Task<IActionResult> MarkPreparing(int id)
{
    await _mediator.Send(new MarkShipmentPreparingCommand(id));
    return NoContent();
}

// SONRA — artık mevcut AssignToWarehouseCommand kullanılıyor
[HttpPost("{id:int}/mark-preparing")]
[Authorize(Roles = "Admin,Accounting,Warehouse,Manager")]
public async Task<IActionResult> MarkPreparing(int id)
{
    await _mediator.Send(new AssignToWarehouseCommand(id));
    return NoContent();
}
```

Ardından `MarkShipmentPreparingCommand.cs` dosyasını sil:
```
Akyildiz.Sevkiyat.Application/Shipments/Commands/MarkShipmentPreparing/MarkShipmentPreparingCommand.cs
```

**Etki:** HTTP endpoint adresi aynı kalır. Frontend değişikliği gerekmez. Behavior aynıdır çünkü her iki command da `AssignedToWarehouse` statüsüne geçiyordu.

---

### Adım 3.2 — `MarkShipmentDelivered`'daki gereksiz `Include` kaldır

**Dosya:** `Akyildiz.Sevkiyat.Application/Shipments/Commands/MarkShipmentDelivered/MarkShipmentDeliveredCommand.cs`

```csharp
// ÖNCE
var shipment = await _context.Shipments
    .Include(s => s.Lines)   // hiç kullanılmıyor
    .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken);

// SONRA
var shipment = await _context.Shipments
    .FirstOrDefaultAsync(s => s.Id == request.ShipmentId, cancellationToken);
```

**Etki:** Teslimat işlemi sırasında gereksiz JOIN ve veri transferi ortadan kalkar. Davranış değişmez.

---

**✅ Aşama 3 tamamlandıktan sonra deploy edilebilir.**

---

## Aşama 4 — Veri Bütünlüğü (Migration gerekli)

Bu aşama veritabanı şemasında değişiklik yapar. Staging ortamında test et, production deploy öncesi yedek al.

---

### Adım 4.1 — `Shipments.IssOrderId` üzerine unique index ekle

Yarış koşulunu veritabanı seviyesinde çözer.

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/Persistence/SevkiyatDbContext.cs`

`OnModelCreating` içindeki Shipment konfigürasyonuna ekle:

```csharp
modelBuilder.Entity<Shipment>(entity =>
{
    entity.HasIndex(s => s.DeliveryDate);
    entity.HasIndex(s => s.Status);
    entity.HasIndex(s => s.IssOrderId).IsUnique(); // ← YENİ

    entity.HasOne(s => s.ZonePreparation)
        ...
});
```

Ardından migration oluştur:
```bash
dotnet ef migrations add AddUniqueIndexShipmentIssOrderId \
  --project Akyildiz.Sevkiyat.Infrastructure \
  --startup-project Akyildiz.Sevkiyat.WebApi
```

> **Önemli:** Migration'ı çalıştırmadan önce veritabanında yinelenen `IssOrderId` değeri olmadığını doğrula:
> ```sql
> SELECT IssOrderId, COUNT(*)
> FROM Shipments
> GROUP BY IssOrderId
> HAVING COUNT(*) > 1;
> ```
> Sonuç dönerse önce veri temizliği gerekir.

---

### Adım 4.2 — Eksik FK index'lerini ekle

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/Persistence/SevkiyatDbContext.cs`

```csharp
// IssOrder -> Project
modelBuilder.Entity<IssOrder>(entity =>
{
    entity.HasIndex(o => o.ProjectId); // ← YENİ
    ...
});

// ShipmentLine -> IssOrderLine
modelBuilder.Entity<ShipmentLine>(entity =>
{
    entity.HasIndex(sl => sl.IssOrderLineId); // ← YENİ
    ...
});

// StockMapping -> LocalStock
modelBuilder.Entity<StockMapping>(entity =>
{
    entity.HasIndex(m => m.ExternalStockCode);
    entity.HasIndex(m => m.LocalStockId); // ← YENİ
    ...
});
```

```bash
dotnet ef migrations add AddMissingForeignKeyIndexes \
  --project Akyildiz.Sevkiyat.Infrastructure \
  --startup-project Akyildiz.Sevkiyat.WebApi
```

---

**✅ Aşama 4: Migration'ları staging'de test et, ardından production'a uygula.**

---

## Aşama 5 — Clean Architecture Düzeltmeleri

Bu aşama dikkatli refactoring gerektirir. Her adım ayrı commit olmalı.

---

### Adım 5.1 — Domain'den MediatR bağımlılığını kopar

Bu, en önemli mimari düzeltmedir. İki adımda yapılır.

**Adım 5.1a — `IDomainEvent`'i saf interface yap**

**Dosya:** `Akyildiz.Sevkiyat.Domain/Common/IDomainEvent.cs`

```csharp
// ÖNCE
using MediatR;

namespace Akyildiz.Sevkiyat.Domain.Common
{
    public interface IDomainEvent : INotification { }
}

// SONRA
namespace Akyildiz.Sevkiyat.Domain.Common
{
    public interface IDomainEvent { }  // MediatR bağımlılığı yok
}
```

**Adım 5.1b — `SevkiyatDbContext`'teki dispatch mantığını güncelle**

`IDomainEvent` artık `INotification` değil. `_publisher.Publish()` doğrudan çağrılamaz. Infrastructure katmanında bir adapter wrap et:

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/Persistence/SevkiyatDbContext.cs`

```csharp
// DispatchDomainEvents metodunu güncelle
private async Task DispatchDomainEvents(CancellationToken cancellationToken)
{
    var entitiesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
        .Where(e => e.Entity.DomainEvents.Any())
        .Select(e => e.Entity)
        .ToList();

    foreach (var entity in entitiesWithEvents)
    {
        var events = entity.DomainEvents.ToArray();
        entity.ClearDomainEvents();

        foreach (var domainEvent in events)
        {
            // IDomainEvent -> INotification dönüşümü burada yapılır
            // Domain katmanı MediatR'dan habersiz kalır
            if (domainEvent is INotification notification)
            {
                await _publisher.Publish(notification, cancellationToken);
            }
        }
    }
}
```

**Not:** Bu geçiş döneminde çalışır çünkü `ShipmentStatusChangedEvent` gibi concrete event sınıfları hem `IDomainEvent` hem de `INotification`'ı implemente edebilir — bu kez sadece MediatR bağımlılığı Infrastructure'da tutulur, Domain'de değil:

**Dosya:** `Akyildiz.Sevkiyat.Domain/Events/ShipmentStatusChangedEvent.cs`

```csharp
// ÖNCE — MediatR.INotification Domain katmanına sızıyor
public record ShipmentStatusChangedEvent(...) : IDomainEvent;
// IDomainEvent : INotification sayesinde dolaylı olarak MediatR'a bağımlı

// Geçiş Sonrası — Domain tamamen bağımsız
// Infrastructure'da ayrı bir wrapper oluşturulur
// Ya da event sınıfları Application katmanına taşınır (ileride)
```

> **Alternatif uzun vadeli çözüm:** `ShipmentStatusChangedEvent` gibi event sınıflarını Domain'den Application katmanına taşımak. Ancak bu daha büyük bir refactoring olduğu için ayrı bir aşamaya bırakılır.

---

### Adım 5.2 — `AuditableEntity` alanlarını otomatik doldur

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/Persistence/SevkiyatDbContext.cs`

`ICurrentUserService`'i constructor'a ekle ve `SaveChangesAsync`'te alanları otomatik doldur:

```csharp
public class SevkiyatDbContext : DbContext, IApplicationDbContext
{
    private readonly IPublisher _publisher;
    private readonly ICurrentUserService _currentUserService; // ← YENİ

    public SevkiyatDbContext(
        DbContextOptions<SevkiyatDbContext> options,
        IPublisher publisher,
        ICurrentUserService currentUserService) // ← YENİ
        : base(options)
    {
        _publisher = publisher;
        _currentUserService = currentUserService;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit alanlarını otomatik doldur
        var now = DateTime.UtcNow;
        var userEmail = _currentUserService.Email ?? "system";

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.CreatedBy = userEmail;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModified = now;
                    entry.Entity.LastModifiedBy = userEmail;
                    break;
            }
        }

        var result = await base.SaveChangesAsync(cancellationToken);
        await DispatchDomainEvents(cancellationToken);
        return result;
    }
}
```

**Etki:** `AuditableEntity`'den türeyen tüm entity'lerde `CreatedAt`, `CreatedBy`, `LastModified`, `LastModifiedBy` otomatik dolar. Handler'lardaki manuel atamalar gereksiz hale gelir (silinmesi zorunlu değil, sadece override edilir).

> **Uyarı:** `UserSeeder` ve `ShipmentSeeder` gibi seeding işlemlerinde `ICurrentUserService.Email` null döner. Yukarıdaki `?? "system"` fallback bunu karşılar.

---

### Adım 5.3 — `Shipment` entity'sini `AuditableEntity`'den türet

**Dosya:** `Akyildiz.Sevkiyat.Domain/Entities/Shipment.cs`

```csharp
// ÖNCE
public class Shipment : IHasDomainEvents
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    // LastModified yok
    // LastModifiedBy yok
    ...
}

// SONRA
public class Shipment : AuditableEntity, IHasDomainEvents
{
    // CreatedAt ve CreatedBy kaldırıldı — AuditableEntity'den geliyor
    // LastModified ve LastModifiedBy de otomatik
    ...
}
```

**Handler'larda temizlik:**
```csharp
// CreateShipmentCommandHandler'da artık gerekli değil
// CreatedAt = DateTime.UtcNow  ← bu satırı kaldır
```

---

**✅ Aşama 5 tamamlandıktan sonra ayrı bir deploy ile yayına alınabilir.**

---

## Aşama 6 — Infrastructure Kalitesi (Refactoring, migration gerekmez)

---

### Adım 6.1 — `OnModelCreating`'i `IEntityTypeConfiguration<T>` sınıflarına böl

Mevcut 290 satırlık metodu ayrı konfigürasyon sınıflarına taşı. `PurchaseOrderConfiguration.cs` dosyası zaten bu pattern'i gösteriyor — diğerleri de aynı şekilde yapılmalı.

**Oluşturulacak dosyalar:**
```
Infrastructure/Persistence/Configurations/
  ShipmentConfiguration.cs
  ShipmentLineConfiguration.cs
  IssOrderConfiguration.cs
  IssOrderLineConfiguration.cs
  ZoneConfiguration.cs
  ZonePreparationConfiguration.cs
  ZonePreparationProjectConfiguration.cs
  StockMasterConfiguration.cs
  StockMappingConfiguration.cs
  UserConfiguration.cs
  GoodsReceiptConfiguration.cs
  GoodsReceiptLineConfiguration.cs
  DriverConfiguration.cs
  VehicleConfiguration.cs
```

**Örnek — ShipmentConfiguration.cs:**
```csharp
public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.HasIndex(s => s.DeliveryDate);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.IssOrderId).IsUnique();

        builder.HasOne(s => s.ZonePreparation)
            .WithMany()
            .HasForeignKey(s => s.ZonePreparationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

**`OnModelCreating` sonucu:**
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(SevkiyatDbContext).Assembly);
    // Seed verileri burada kalabilir veya ayrı bir seeder'a taşınır
}
```

---

### Adım 6.2 — `OnModelCreating`'deki `PendingModelChangesWarning` susturmasını kaldır

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/Persistence/SevkiyatDbContext.cs`

```csharp
// ÖNCE
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.ConfigureWarnings(warnings =>
        warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
    base.OnConfiguring(optionsBuilder);
}

// SONRA — OnConfiguring'i tamamen kaldır
// (Eğer bu uyarı kaldırılınca gerçek migration eksikliği ortaya çıkarsa,
//  önce eksik migration oluştur, sonra bu satırı kaldır)
```

---

### Adım 6.3 — Seed verilerindeki `DateTime.Today`'i sabit tarihle değiştir

**Dosya:** `Akyildiz.Sevkiyat.Infrastructure/Persistence/SevkiyatDbContext.cs`

```csharp
// ÖNCE
modelBuilder.Entity<IssOrder>().HasData(new IssOrder
{
    OrderDate    = DateTime.Today,
    DeliveryDate = DateTime.Today.AddDays(1),
    ...
});

// SONRA
modelBuilder.Entity<IssOrder>().HasData(new IssOrder
{
    OrderDate    = new DateTime(2026, 1, 14, 0, 0, 0, DateTimeKind.Utc),
    DeliveryDate = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
    ...
});
```

Bu değişiklik yeni bir migration gerektirir çünkü snapshot değişir:
```bash
dotnet ef migrations add FixSeedDateTimeValues \
  --project Akyildiz.Sevkiyat.Infrastructure \
  --startup-project Akyildiz.Sevkiyat.WebApi
```

---

**✅ Aşama 6 tamamlandıktan sonra deploy edilebilir. Davranış değişmez.**

---

## Aşama 7 — Teknik Borç (Yeni özellikler, ayrı sprint)

Bu aşama mevcut sorunları çözmez, sisteme yeni güvenlik katmanları ekler.

---

### Adım 7.1 — Test projesi oluştur

```bash
dotnet new xunit -n Akyildiz.Sevkiyat.Application.Tests
dotnet sln add Akyildiz.Sevkiyat.Application.Tests
```

Öncelikli test edilecekler:

```
Tests/
  Domain/
    ShipmentStatusMachineTests.cs     ← ValidateTransition tüm geçişler
    ShipmentLineSetDeliveredQtyTests.cs
  Application/
    CreateShipmentCommandHandlerTests.cs
    AssignToWarehouseCommandHandlerTests.cs
    LoginCommandHandlerTests.cs
  Infrastructure/
    PasswordHasherTests.cs
```

**Örnek test:**
```csharp
public class ShipmentStatusMachineTests
{
    [Fact]
    public void ChangeStatus_FromCreated_ToAssignedToWarehouse_Succeeds()
    {
        var shipment = new Shipment { /* ... */ };
        shipment.ChangeStatus(ShipmentStatus.AssignedToWarehouse, userId: 1);
        Assert.Equal(ShipmentStatus.AssignedToWarehouse, shipment.Status);
    }

    [Fact]
    public void ChangeStatus_FromDelivered_ToAny_ThrowsDomainException()
    {
        var shipment = new Shipment { /* ... */ };
        // Delivered'a getir
        Assert.Throws<DomainException>(() =>
            shipment.ChangeStatus(ShipmentStatus.Created, userId: 1));
    }

    [Fact]
    public void ChangeStatus_SkipTransition_WithoutReason_ThrowsDomainException()
    {
        var shipment = new Shipment { /* Created */ };
        Assert.Throws<DomainException>(() =>
            shipment.ChangeStatus(ShipmentStatus.Picking, userId: 1, reason: null));
    }
}
```

---

### Adım 7.2 — Şifre değiştirme endpoint'i ekle

**Application:**
```csharp
// Akyildiz.Sevkiyat.Application/Auth/Commands/ChangePassword/
public record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;
```

**Handler:** Mevcut `LoginCommandHandler` ile aynı pattern — şifreyi doğrula, yeni hash üret, kaydet.

**Controller:**
```csharp
[HttpPost("change-password")]
[Authorize]
public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
{
    await _mediator.Send(command);
    return NoContent();
}
```

---

### Adım 7.3 — Refresh token endpoint'i ekle

Mevcut `LoginResponse`'a `RefreshToken` ekle ve `/api/auth/refresh` endpoint'i oluştur. Refresh token'lar veritabanında saklanır (yeni `UserRefreshToken` tablosu).

Bu adım kendi başına bir mini sprint gerektirir — Adım 7.1 ve 7.2 tamamlandıktan sonra ele alınmalı.

---

### Adım 7.4 — `HelloWorld.vue` ve stale dizini kaldır

```bash
# Frontend temizliği
rm client/src/components/HelloWorld.vue

# Kök dizindeki gereksiz dizin
rm -rf AkyildizSevkiyat/
```

---

## Özet Tablosu

| Aşama | Kapsam | Migration | Davranış Değişikliği | Risk |
|---|---|---|---|---|
| **1** | Kritik güvenlik açıkları | ❌ | Yok (yetkisiz erişim engellenir) | 🟢 Çok Düşük |
| **2** | Altyapı güvenliği | ❌ | Yok | 🟢 Çok Düşük |
| **3** | Mantık hataları | ❌ | Yok (endpoint aynı kalır) | 🟢 Düşük |
| **4** | Veri bütünlüğü index'leri | ✅ | Yok | 🟡 Orta (veri temizliği gerekebilir) |
| **5** | Clean Architecture | ❌ | Yok | 🟡 Orta (dikkatli test gerekir) |
| **6** | Infrastructure kalitesi | Kısmen ✅ | Yok | 🟢 Düşük |
| **7** | Teknik borç / yeni özellik | ✅ | Yeni özellikler eklenir | 🟡 Orta |

---

## Önerilen Deploy Sırası

```
Aşama 1 → Aşama 2 → Aşama 3    (tek deploy, aynı gün)
    ↓
Aşama 4                          (migration — staging test sonrası)
    ↓
Aşama 5 → Aşama 6               (tek deploy, refactoring sprint)
    ↓
Aşama 7                          (feature sprint)
```

Aşama 1-3 aynı pull request içinde birleştirilebilir; kritik güvenlik açıkları için bu tercih edilen yaklaşımdır.
