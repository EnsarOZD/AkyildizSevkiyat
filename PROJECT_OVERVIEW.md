# Akyıldız Sevkiyat — Proje Teknik Dökümanı

> **Son güncelleme:** 2026-03-23
> **Versiyon:** MVP → Aktif Geliştirme
> **Ortam:** Production → `https://sevkiyat.akyildiz.com`

---

## 1. Projenin Amacı

**Akyıldız Sevkiyat**, bir tesis hizmetleri şirketi olan Akyıldız'ın lojistik ve sevkiyat operasyonlarını yönetmek için geliştirilmiş dahili bir ERP/WMS uygulamasıdır.

### Temel Problemler (Çözülenler)
1. Dış ERP sistemi (ISS-IP) siparişlerinin manuel takibi → **otomatik import + onay akışı**
2. Depo hazırlık sürecinin kağıt tabanlı yönetimi → **dijital picking akışı**
3. Şoför rotaları ve teslimat kanıtının takibi → **mobil şoför paneli (PWA)**
4. Stok yönetiminin ayrı sistemlerde olması → **entegre stok + depo adresleme**
5. Netsis muhasebe entegrasyonunun manuel olması → **otomatik irsaliye + transfer**

---

## 2. Teknoloji Stack'i

### Backend
| Katman | Teknoloji |
|--------|-----------|
| Framework | ASP.NET Core 10 Web API |
| Mimari | Clean Architecture (4 katman) |
| ORM | Entity Framework Core 10 (Code-First) |
| DB | SQL Server |
| Pattern | CQRS (MediatR), Repository yok (DbContext direkt) |
| Validation | FluentValidation (MediatR pipeline'a entegre) |
| Auth | JWT Bearer Token |
| External | ISS-IP ERP (HTTP/Basic Auth), Netsis API |

### Frontend
| Teknoloji | Versiyon/Detay |
|-----------|----------------|
| Framework | Vue 3 + TypeScript |
| Build | Vite |
| State | Pinia |
| UI | TailwindCSS + Heroicons |
| HTTP | Axios (interceptor ile JWT + hata normalizasyonu) |
| Router | Vue Router 4 (RBAC guard) |
| PWA | vite-plugin-pwa + Workbox |

### Infrastructure
```
çözüm root/
├── Akyildiz.Sevkiyat.Domain/          ← Entity'ler, Enum'lar, Exception'lar
├── Akyildiz.Sevkiyat.Application/     ← MediatR Command/Query Handler'ları, DTO'lar
├── Akyildiz.Sevkiyat.Infrastructure/  ← EF Core, Migration'lar, Ext. Servisler
├── Akyildiz.Sevkiyat.WebApi/          ← Controller'lar, Middleware, Program.cs
└── client/                            ← Vue 3 SPA
```

---

## 3. Kullanıcı Rolleri ve Yetki Matrisi

| Rol | Açıklama | Erişim |
|-----|----------|--------|
| **Admin** | Tam yetki | Her şey |
| **Manager** | Operasyon yöneticisi | Raporlar hariç hemen her şey |
| **Warehouse** | Depo personeli | Depo hazırlık, stok, mal kabul |
| **Dispatcher** | Sevkiyat planlayıcısı | Şoför/araç, iade, sevkiyat |
| **Accounting** | Muhasebe | Satınalma, stok, raporlar |

RBAC hem **router guard** (`meta.roles`) hem de **backend `[Authorize(Roles=...)]`** ile çift katmanlı uygulanır.

---

## 4. Modüller ve Yetenekler

### 4.1 ISS-IP Entegrasyonu
- Tarih aralığına göre sipariş çekme (`POST /api/orders/import`)
- Çift-serialize JSON parsing (ISS API özelliği)
- Stok kodları otomatik eşleştirme + bilinmeyen stok kuyruğa alma
- Tekrar import = self-healing (mevcut siparişler güncellenir)

### 4.2 Sevkiyat Yönetimi
**Durum Akışı:**
```
Created → AssignedToWarehouse → Picking → ReadyForDispatch → AssignedToVehicle → Delivered
                                                           ↘ ReturnedToWarehouse
                                                           ↘ Cancelled
                                 ↕ Passive (arşiv)
```
- Toplu sevkiyat oluşturma (ISS siparişlerinden)
- Her geçişte history kaydı + zorunlu açıklama (geri dönüşlerde)
- İrsaliye no girişi (Netsis'ten veya manuel)
- Teslimat kanıtı: alıcı adı + not + fotoğraf (Base64)
- Satır bazlı teslim miktarı ve fark nedeni
- Araç dönüş kaydı (kısmi iade)

### 4.3 Depo Hazırlık (Zone Preparation)
**Durum Akışı:**
```
Draft → MicroPicking → MicroReady → MacroPicking → ReadyForDriverInfo → ReadyForTransfer
```
1. **Draft:** Bölge + tarih seçilip hazırlık başlatılır
2. **MicroPicking:** Her proje için ayrı picking listesi (stok bazlı)
3. **MicroReady:** Proje picking tamamlandı işareti
4. **MacroPicking:** Tüm projeler hazır, makro picking başlar
5. **ReadyForDriverInfo:** `IrsaliyeFetched = false` → "Netsisten İrsaliye Çek" butonu aktif; `IrsaliyeFetched = true` → "Araca Ata" butonu aktif
6. **ReadyForTransfer:** Şoför + araç atandı, teslimat başlayabilir

### 4.4 Şoför Paneli (PWA - Mobile)
- Ayrı layout (`/driver` prefix) — masaüstü menüsüz, mobil odaklı
- Günlük sevkiyat listesi + bölge bazlı gruplandırma
- Stop detayı: proje adresi, teslim edilecek malzeme listesi
- **GPS konum kaydetme:** Şoförun mevcut konumunu projeye kaydeder
- **Google Maps navigasyon:** GPS koordinatı varsa lat/lng, yoksa adres üzerinden
- Çok duraklı rota linki (tüm durakları tek Maps URL'inde)
- Teslimat tamamlama: alıcı + not + fotoğraf
- Araç dönüş kaydı

### 4.5 Stok Yönetimi
- Stok kataloğu (CRUD + Excel import)
- Kategori, birim, KDV oranı, picking tipi (Micro/Macro)
- Netsis stok kodu eşleştirmesi
- Stok bakiye takibi (OnHand / Reserved / Available)
- Stok hareketleri (GoodsIn, ShipmentOut, ManualAdjust, VehicleReturn...)

### 4.6 Depo Adresleme (WMS)
- Koridor / Taraf (K-G) / Modül / Kat hiyerarşisi
- Lokasyon kodu otomatik üretimi (örn: `1K-003-02`)
- Stok → Lokasyon ataması
- Lokasyonlar arası transfer + geçmiş
- **Stok Haritası:** Hangi stok nerede, anlık miktar görünümü

### 4.7 Proje & Bölge Yönetimi
- **Bölge (Zone):** Coğrafi/operasyonel teslimat bölgesi (İzmir, Ankara...)
- **Proje:** ISS kodlu müşteri projesi (yemekhanesi, tesisi vb.)
- Proje-Bölge eşleştirmesi (bulk atama UI)
- **Teslimat sırası:** Bölge içindeki projeler sürükle-bırak ile sıralanır
- GPS koordinatı kaydı (şoför panelinden)
- Netsis Cari Kodu eşleştirmesi

### 4.8 Satınalma Modülü
**Durum Akışı:** `Draft → Approved → PartiallyReceived → Closed / Cancelled`
- PO oluştur → onayla → mal kabul et → kapat
- Otomatik sipariş numarası (Yıl/Ay/Sıra)
- Tedarikçi yönetimi (Excel import desteği)

### 4.9 Mal Kabul
**Durum Akışı:** `Draft → Posted / Cancelled`
- PO'ya bağlı veya bağımsız mal kabul
- Satır bazlı: sipariş miktarı vs. gelen miktar, kabul/red
- "Post" işleminde stok OnHand artışı tetiklenir

### 4.10 Stok Sayımı
- Sayım başlat (tüm aktif stoklar çekilir)
- Gerçek miktar girişi
- Tamamla → fark analizi

### 4.11 Belirsiz İadeler
- Araç dönüşünde hangi sevkiyata ait olduğu bilinmeyen iadeler
- Durumlar: Pending → MatchedToShipment / AddedToStock / WrittenOff
- Sevkiyata bağla veya stoka ekle veya sil

### 4.12 Raporlar
- **Dashboard:** Günlük sevkiyat sayıları, durum dağılımı, performans
- **Bölge Malzeme Raporu:** Bölge + tarih bazlı stok ihtiyacı
- **İade Raporu**
- **Stok Durum Raporu**

### 4.13 Netsis Entegrasyonu
- Sevkiyatı Netsis'e aktarma (irsaliye oluşturma)
- Stok bakiyesi senkronizasyonu
- Bölge bazlı toplu irsaliye çekme (`FetchZoneIrsaliye`)

### 4.14 Kullanıcı Yönetimi
- CRUD + şifre sıfırlama
- Aktif/pasif toggle
- Role assignment

---

## 5. Domain Model (Varlık İlişkileri)

```
ISS-IP ERP
    ↓ import
IssOrder ──────────────── IssOrderLine
    │                           │
    ↓                           ↓
Project ◄─── Zone          StockMapping ─── StockMaster
    │                                            │
    ↓                                        StockLocation ─── WarehouseLocation
Shipment ─── ShipmentLine (IssOrderLine bağlı)
    │
    └── ZonePreparation ─── ZonePreparationProject
              │
           Driver / Vehicle
```

**Kritik ilişkiler:**
- Bir `Project` → birden fazla `IssOrder`, birden fazla `Shipment`
- Bir `Zone` → birden fazla `Project` + birden fazla `ZonePreparation`
- Bir `ZonePreparation` → birden fazla `ZonePreparationProject` (M:N köprü)
- `ShipmentLine` hem `IssOrderLine`'a hem `StockMaster`'a bağlı

---

## 6. Durum Matrisi (Tüm Workflow'lar)

### Shipment (ShipmentStatus)
| Değer | Ad | Tetikleyen |
|-------|----|------------|
| 0 | Created | Sevkiyat oluşturuldu |
| 1 | AssignedToWarehouse | Depoya atandı |
| 2 | Picking | Picking başlatıldı |
| 3 | ReadyForDispatch | Hazır işaretlendi |
| 4 | AssignedToVehicle | Araca atandı |
| 5 | Delivered | Teslim edildi |
| 6 | Cancelled | İptal edildi |
| 7 | ReturnedToWarehouse | Araç iade |
| 10 | Passive | Arşivlendi |

### ZonePreparation (ZonePreparationStatus)
| Değer | Ad | Açıklama |
|-------|----|----------|
| 0 | Draft | Oluşturuldu |
| 1 | MicroPicking | Proje bazlı picking sürüyor |
| 2 | MicroReady | Tüm projeler micro-ready |
| 3 | MacroPicking | Makro picking sürüyor |
| 4 | ReadyForDriverInfo | İrsaliye + araç atama bekliyor |
| 5 | ReadyForTransfer | Araç + şoför atandı, teslimata hazır |

### PurchaseOrder (PurchaseOrderStatus)
`Draft → Approved → PartiallyReceived → Closed / Cancelled`

### GoodsReceipt (GoodsReceiptStatus)
`Draft → Posted / Cancelled`

---

## 7. API Endpoint Özeti

**Base URL:** `/api`
**Auth:** `Authorization: Bearer <JWT>`

| Controller | Prefix | Başlıca İşlemler |
|------------|--------|-----------------|
| Auth | `/auth` | POST /login |
| Shipments | `/shipments` | CRUD + 10+ durum geçiş endpoint'i |
| Orders | `/orders` | ISS sipariş sorgulama |
| IssOrders | `/iss-orders` | Import, eşleştirme |
| PurchaseOrders | `/purchase-orders` | CRUD + onay/kapatma |
| GoodsReceipts | `/goods-receipts` | CRUD + post/iptal |
| Stocks | `/stocks` | CRUD + Excel import |
| StockCounts | `/stock-counts` | Sayım yönetimi |
| StockLocations | `/stock-locations` | Lokasyon transferleri |
| WarehouseLocations | `/warehouse/locations` | Depo adres yönetimi |
| Warehouse | `/warehouse` | Dashboard + sync |
| Zones | `/zones` | CRUD + hazırlık akışı (6 endpoint) |
| Projects | `/projects` | CRUD + bulk delivery order |
| Suppliers | `/suppliers` | CRUD + import |
| Transport | `/transport` | Şoför + Araç CRUD |
| Driver | `/driver` | Şoför paneli (sevkiyatlar, rota) |
| FloatingReturns | `/floating-returns` | İade yönetimi |
| Reports | `/reports` | 4 rapor endpoint'i |
| Search | `/search` | Global arama |
| Users | `/users` | Kullanıcı yönetimi (Admin only) |
| Netsis | `/netsis` | Stok sync + irsaliye aktarım |
| Dashboard | `/dashboard` | İstatistikler |

**Toplam tahmini endpoint sayısı: ~90+**

---

## 8. Frontend Sayfa Listesi

### Yönetim Paneli (DefaultLayout)
| URL | Sayfa | Roller |
|-----|-------|--------|
| `/` | Dashboard | Hepsi |
| `/shipments` | Sevkiyat Listesi | Hepsi |
| `/shipments/:id` | Sevkiyat Detayı | Hepsi |
| `/warehouse` | Depo Hazırlık | Admin, Warehouse, Manager |
| `/transport` | Şoför & Araç | Admin, Manager, Dispatcher |
| `/floating-returns` | Belirsiz İadeler | Admin, Manager, Warehouse, Dispatcher |
| `/stocks` | Stok Yönetimi | Admin, Accounting, Manager |
| `/stock-counts` | Stok Sayımı | Admin, Manager, Warehouse |
| `/warehouse/locations` | Depo Adresleri | Admin, Manager, Warehouse, Accounting |
| `/warehouse/stock-locations` | Stok Haritası | Admin, Manager, Warehouse, Accounting |
| `/zones` | Bölge Yönetimi | Admin, Manager |
| `/projects/zone-mapping` | Proje-Bölge Eşleştirme | Admin, Manager |
| `/zones/project-order` | Teslimat Sırası | Admin, Manager |
| `/orders/import` | ISS Entegrasyon | Admin, Accounting, Manager |
| `/purchase-orders` | Satınalma | Admin, Accounting, Manager |
| `/goods-receipts` | Mal Kabul | Admin, Warehouse, Manager |
| `/suppliers` | Tedarikçiler | Admin, Accounting, Manager |
| `/reports` | Raporlar | Admin, Accounting, Warehouse, Manager |
| `/reports/zone-material` | Bölge Malzeme Raporu | Hepsi (auth) |
| `/users` | Kullanıcı Yönetimi | Admin |

### Şoför Paneli (DriverLayout — PWA odaklı)
| URL | Sayfa |
|-----|-------|
| `/driver` | Şoförün sevkiyatları |
| `/driver/stop/:projectId` | Teslimat noktası detayı |
| `/driver/:id` | Sevkiyat teslim ekranı |

---

## 9. Harici Entegrasyonlar

### ISS-IP ERP
- **URL:** `http://isstr-dmz1.tr.issworld.com:88/`
- **Auth:** Basic Auth + form credentials
- **Veri:** Sipariş listesi + detayı (çift-serialize JSON)
- **Senkronizasyon:** Manuel (admin/manager tetikler)
- **Proje Sync:** ISS'ten proje listesi çekme (`POST /projects/sync`)

### Netsis ERP
- Stok bakiyesi senkronizasyonu
- Sevkiyat → İrsaliye aktarımı
- Bölge hazırlık → Toplu irsaliye çekme
- ⚠️ İrsaliye çekilmeden araç ataması **engellenmiş** (domain rule)

---

## 10. PWA Durumu

| Özellik | Durum |
|---------|-------|
| Manifest (install prompt) | ✅ |
| Service Worker (Workbox) | ✅ |
| Offline static assets | ✅ |
| API NetworkFirst cache | ✅ |
| Apple (iOS) meta tag'leri | ✅ |
| autoUpdate | ✅ |
| GPS (geolocation) | ✅ HTTPS ile çalışır |
| Icon (kare 512px) | ⚠️ Mevcut logo kare değil |

---

## 11. Veritabanı Yapısı

### Tablo Sayısı: ~25+
```
Projects, Zones
IssOrders, IssOrderLines
Shipments, ShipmentLines, ShipmentHistories
ZonePreparations, ZonePreparationProjects
StockMasters, StockMappings, StockTransactions
StockCounts, StockCountLines
StockLocations, WarehouseLocations, LocationTransfers
PurchaseOrders, PurchaseOrderLines, PurchaseOrderNumberCounters
GoodsReceipts, GoodsReceiptLines
Suppliers
Drivers, Vehicles
FloatingReturns
Users
```

### EF Core Önemli Config'ler
- `User.Email` → unique index
- `StockMaster.StockCode` → unique index
- `Project.Code` → unique index (nvarchar(100))
- `ZonePreparation (ZoneId, DeliveryDate, BatchNo)` → unique
- `WarehouseLocation.Code` → unique index
- `StockLocation (StockMasterId, WarehouseLocationId)` → unique
- Decimal alanlar: `decimal(18,2)` veya `decimal(18,4)`

### Migration Sayısı: ~35
Tüm migration'lar `Akyildiz.Sevkiyat.Infrastructure/Migrations/` altında, startup'ta otomatik çalışır.

---

## 12. Bilinen Kısıtlar / Teknik Borç

| Konu | Durum | Not |
|------|-------|-----|
| ISS-IP JSON parsing | Çalışıyor | Çift-serialize, kırılgan yapı |
| Netsis entegrasyonu | Kısmi | İrsaliye çekme var, bakiye sync var |
| Fotoğraf storage | Base64 DB'de | Büyük ölçekte blob storage gerekir |
| Proje sync | Manuel tetikli | Otomatik scheduling yok |
| Test coverage | Yok | Unit/integration test eksik |
| API versioning | Yok | v1 prefix yok |
| Rate limiting | Yok | Throttle yok |
| Audit log | Kısmi | Sadece ShipmentHistory tablosu |
| Push notification | Yok | Real-time için WebSocket/SSE yok |

---

## 13. Geliştirme Ortamı

```bash
# Backend (port 5087)
dotnet run --project Akyildiz.Sevkiyat.WebApi

# Frontend (port 5173, /api proxy → 5087)
cd client && npm run dev

# Yeni migration ekle
dotnet ef migrations add <MigrationName> \
  --project Akyildiz.Sevkiyat.Infrastructure \
  --startup-project Akyildiz.Sevkiyat.WebApi

# Build kontrol
cd client && npm run build
```

### Env Değişkenleri (appsettings.Development.json'da)
```json
{
  "ConnectionStrings": { "SevkiyatConnection": "..." },
  "Jwt": { "Key": "...", "Issuer": "...", "Audience": "..." },
  "ISSIp": { "BaseUrl": "...", "KullaniciAdi": "...", "Sifre": "..." },
  "Cors": { "AllowedOrigins": ["http://localhost:5173"] },
  "SeedData": { "AdminPassword": "..." }
}
```
Placeholder değerleri (`SET_BY_ENV_...`) startup'ta validate edilir, eksikse uygulama başlamaz.

---

## 14. Sistem Akışı (End-to-End Örnek)

```
1. Admin ISS'ten siparişleri import eder (/orders/import)
   ↓
2. Stok eşleştirmesi yapılır (bilinmeyen stoklar eşleştirilir)
   ↓
3. Siparişlerden Sevkiyatlar oluşturulur (bulk create)
   ↓
4. Sevkiyatlar depoya atanır (AssignedToWarehouse)
   ↓
5. Depo, bölge hazırlığı başlatır (/warehouse dashboard)
   - MicroPicking: Her proje için ayrı stok listesi
   - MacroPicking: Tüm bölge malzemeleri toplanır
   ↓
6. Netsis'ten irsaliye numaraları çekilir
   ↓
7. Araç + şoför atanır (ReadyForTransfer)
   ↓
8. Şoför PWA üzerinden teslimata çıkar
   - GPS ile konum kaydeder
   - Google Maps navigasyon açar
   - Teslimatı tamamlar (fotoğraf + imza)
   ↓
9. Araç depoya döner, kalan malzemeler iade kaydedilir
```

---

*Bu döküman `PROJECT_OVERVIEW.md` olarak proje kök dizinine kaydedilmiştir.*
