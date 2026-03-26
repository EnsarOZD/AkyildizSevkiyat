# Netsis Entegrasyonu — Yapılacaklar

> Durum: Altyapı hazır. Bu dosya, Netsis tarafından bilgiler gelince yapılacak adımları listeler.
> Her adım tamamlandıkça kutucuk işaretlenir.

---

## 1. Config Güncellemeleri

Bilgiler gelince `appsettings.Development.json` ve prod ortam değişkenleri doldurulur.

| Alan | Nerede | Açıklama |
|------|--------|---------|
| `Netsis:BaseUrl` | env var `Netsis__BaseUrl` | REST servis base URL (örn. `http://server_ip:port/`) |
| `Netsis:KullaniciAdi` | env var `Netsis__KullaniciAdi` | Entegrasyon kullanıcısı |
| `Netsis:Sifre` | env var `Netsis__Sifre` | Şifre |
| `Netsis:FirmaKodu` | env var `Netsis__FirmaKodu` | Firma / DB adı |
| `Netsis:SubeKodu` | env var `Netsis__SubeKodu` | Şube kodu |
| `Netsis:IsletmeKodu` | `appsettings.json` | Varsa doldur, yoksa `null` bırak |
| `Netsis:DepoKodu` | `appsettings.json` | Varsayılan depo kodu |
| `Netsis:LoginPath` | `appsettings.json` | Login endpoint path (örn. `api/v1/auth/login`) |
| `Netsis:SiparisPath` | `appsettings.json` | Müşteri siparişi oluşturma endpoint'i |
| `Netsis:StokBakiyePath` | `appsettings.json` | Stok bakiye sorgulama endpoint'i |
| `Netsis:TokenExpiryMinutes` | `appsettings.json` | Token geçerlilik süresi (dakika - 5 pay bırak) |

---

## 2. DTO Alan Adları — `NetsisDto.cs`

Dosya: `Application/External/Netsis/Dtos/NetsisDto.cs`

Tüm `// TODO: NETSIS_API` satırları güncellenmeli.

### 2a. Login

- [ ] `NetsisLoginRequest` field adlarını API'ye göre düzelt
- [ ] `NetsisLoginResponse.Token` — dönen token field adını düzelt (örn. `token`, `accessToken`, `result`)
- [ ] `NetsisLoginResponse.ExpireDate` — expiry field adını düzelt (yoksa kaldır)

### 2b. Müşteri Siparişi

- [ ] `NetsisSiparisRequest` root-level field adlarını düzelt
  - `BelgeNo` → Netsis'teki karşılığı (sipariş/belge no field adı)
  - `CariKodu` → Netsis cari kod field adı
  - `ProjeKodu` → Netsis proje kod field adı
  - `DepoKodu` → Netsis depo kod field adı
  - `TeslimTarihi` → tarih format + field adı
- [ ] `NetsisSiparisLine` field adlarını düzelt
  - `SatirNo`, `StokKodu`, `Miktar`, `Birim`, `BirimFiyati`, `KdvOrani`
- [ ] `NetsisSiparisResult` field adlarını düzelt
  - Başarı/hata alanı (bool mu, status code mu?)
  - Dönen Netsis sipariş/belge no alanı

### 2c. Stok Bakiye

- [ ] `NetsisStockBalanceQuery` field adlarını düzelt (sorgu parametreleri)
- [ ] `NetsisStockBalanceDto` field adlarını düzelt
  - `StokKodu`, `DepoKodu`, `MevcutStok`, `RezerveStok`, `SerbstStok`
- [ ] Response wrapper var mı kontrol et — varsa `NetsisClient.GetStockBalancesAsync`'ta unwrap ekle
  - Örn. `{ "data": [...] }` şeklinde geliyorsa ara bir wrapper DTO gerekir

---

## 3. NetsisClient — HTTP Detayları

Dosya: `Infrastructure/ExternalServices/Netsis/NetsisClient.cs`

- [ ] `SetAuthHeader` — auth schema doğru mu? (`Bearer` mi, başka bir scheme mi, custom header mı?)
- [ ] `CreateSiparisAsync` — HTTP method doğru mu? (POST varsayıldı, PUT/PATCH olabilir)
- [ ] `GetStockBalancesAsync` — GET mi POST mu? Query string mi, body mi?
  - GET + query string ise `PostAsJsonAsync` → `GetAsync` + `QueryHelpers.AddQueryString` ile değiştir

---

## 4. ExportShipmentToNetsisHandler — İş Kuralları

Dosya: `Application/Netsis/Commands/ExportShipmentToNetsis/ExportShipmentToNetsisCommandHandler.cs`

- [ ] **Stok kodu hangisi gönderilmeli?**
  - Şu an `IssOrderLine.StockCode` (ISS-IP kodu) gönderiliyor
  - Netsis kendi stok kodunu bekliyorsa `StockMaster.NetsisStockCode` kullanılmalı
  - `BuildSiparisRequest` içindeki `// TODO: NETSIS_API` satırını güncelle

- [ ] **Hangi durumda export izin verilmeli?**
  - Şu an: `ReadyForDispatch` veya `AssignedToVehicle`
  - Netsis'in beklentisine göre genişletilebilir/daraltılabilir

- [ ] **Yeniden aktarıma izin verilecek mi?**
  - Şu an `NetsisTransferredAt` doluysa `ConflictException` fırlatıyor
  - Netsis sipariş güncellemeyi destekliyorsa bu kontrolü gevşet

- [ ] **DepoKodu kaynağı:**
  - Şu an `NetsisOptions.DepoKodu` (global) kullanılıyor
  - Proje bazlı farklı depo varsa `Project`'e `NetsisDepoKodu` alanı eklenebilir

---

## 5. SyncNetsisStockBalanceHandler — Eşleşme Stratejisi

Dosya: `Application/Netsis/Commands/SyncNetsisStockBalance/SyncNetsisStockBalanceCommandHandler.cs`

- [ ] `StockMaster.NetsisStockCode` alanlarını veriyle doldur (bkz. Adım 6)
- [ ] Stok senkronizasyonu sadece `OnHandQty`'yi mi güncellemeli, `ReservedQty`'yi de mi?
  - Şu an sadece `OnHandQty` güncelleniyor (`ReservedQty` bizim sistemimizden geliyor)
  - Netsis'in `RezerveStok` alanına güvenilecekse burayı da güncelle

---

## 6. Master Veri Doldurma (Tek Seferlik)

Netsis bilgileri gelince mevcut kayıtlara Netsis kodu eşleştirmesi yapılacak.

### 6a. `Project.NetsisCariKodu`

Her projeye Netsis'teki karşılığı olan cari kodu ekle.

```sql
-- Örnek — gerçek değerler Netsis'ten alınacak
UPDATE Projects SET NetsisCariKodu = 'CARI_001' WHERE Code = 'PRJ_X';
```

Ya da frontend üzerinden proje düzenleme ekranına `NetsisCariKodu` alanı eklenerek UI'dan girilebilir.

### 6b. `StockMaster.NetsisStockCode`

Her stok kaydına Netsis'teki stok kodunu ekle.

```sql
-- Örnek
UPDATE StockMasters SET NetsisStockCode = 'NTS_STK_001' WHERE StockCode = 'AKY_001';
```

Eğer ISS-IP stok kodu = Netsis stok kodu ise toplu güncelleme yapılabilir:
```sql
-- ISS-IP kodu ile Netsis kodu aynıysa
UPDATE StockMasters SET NetsisStockCode = StockCode WHERE NetsisStockCode IS NULL;
```

---

## 7. Frontend (İsteğe Bağlı)

- [ ] Sevkiyat detay sayfasına "Netsis'e Aktar" butonu ekle
  - `POST /api/netsis/shipments/{id}/export` çağrısı
  - `NetsisTransferredAt` doluysa butonu pasif/bilgi göster
- [ ] Stok sayfasına `NetsisStockCode` kolonu ekle (düzenlenebilir)
- [ ] Proje düzenleme formuna `NetsisCariKodu` alanı ekle
- [ ] Raporlar veya Ayarlar altına "Netsis Stok Senkronize Et" aksiyonu ekle

---

## 8. Test Adımları (Bilgiler Geldikten Sonra)

- [ ] Login endpoint'ini test et — token alındığını doğrula
- [ ] Tek bir sevkiyat için export dene — Netsis'te sipariş oluştuğunu doğrula
- [ ] `BelgeNo` = `TalepNo` eşleşmesini Netsis tarafında kontrol et
- [ ] Stok bakiye sorgusunu test et — dönen veriyi incele, DTO field adlarını karşılaştır
- [ ] Sync komutunu çalıştır — `StockMaster.OnHandQty` değerlerini doğrula

---

## Referans

- Proje dokümantasyon talebi: `docs/Netsis_Integration_Request.md`
- Altyapı kodu:
  - `Application/Interfaces/INetsisClient.cs`
  - `Application/External/Netsis/Dtos/NetsisDto.cs`
  - `Infrastructure/ExternalServices/Netsis/NetsisClient.cs`
  - `Infrastructure/ExternalServices/Netsis/NetsisOptions.cs`
  - `WebApi/Controllers/NetsisController.cs`
