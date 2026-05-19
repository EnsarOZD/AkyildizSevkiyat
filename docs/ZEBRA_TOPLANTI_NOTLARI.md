# Zebra El Terminali — Toplantı Notları

**Tarih:** 2026-05-07  
**Konu:** Depo ve şoför operasyonları için Zebra handheld terminal değerlendirmesi  
**Sistem:** Vue 3 PWA — Native uygulama yok, web tabanlı

---

## Bağlam: Sistemimiz Hakkında Bilgi

Toplantıya girmeden önce Zebra temsilcisine şunu özetle anlat:

- Vue 3 tabanlı web uygulaması, PWA olarak çalışıyor
- Şu an şoför aracı başlatma/bitiş için QR tarama var (kamera tabanlı)
- İleride: raf adresi QR, ürün barkodu (EAN-13/Code128), koli barkodu entegrasyonu planlanıyor
- Native uygulama yazmıyoruz, tarayıcı üzerinden çalışıyoruz

---

## 1. Cihaz Seçimi

### Tablet mi, Handheld mi?

> *"Bizim senaryomuzda depo picking operasyonu ve araç şoförü kullanımı var. Her ikisi için aynı cihaz mı, yoksa farklı cihazlar mı önerirsiniz?"*

- Depoda tek elle kullanım şart — iki el boş olmaz
- Şoför için daha kompakt, cep telefonu boyutu yeterli olabilir (TC26/TC27 serisi?)
- Gerçek saha örnekleri iste — katalog değil, benzer müşteri deneyimi

> *"Benzer ölçekteki depo müşterilerinizde picking için hangi cihazı kullanıyor, ne kadar memnunlar?"*

---

## 2. Barkod Tarama — En Kritik Konu

### 2D Imager Şartı

> *"Önerdiğiniz cihazlarda scanner tipi nedir — 2D imager mi, laser mı?"*

**Neden önemli:** Laser scanner sadece 1D barkod okur. Bizim sistemimizde QR kod (raf, araç) ve ileride 2D barkodlar da olacak. 2D imager zorunlu.

### Keyboard Wedge Stabilitesi

> *"Web uygulamalarında DataWedge keyboard wedge olarak kullanımda stabil çalışan referans müşteriniz var mı?"*

> *"DataWedge'in Browser Output veya Browser Wedge özelliğini web uygulamaları için önerir misiniz?"*

> *"Keyboard wedge'de prefix/suffix konfigürasyonu için öneriniz nedir? Bazı kurulumlar wrapper karakter ekliyor, bunları nasıl önlüyorsunuz?"*

### Intent-Based Gerekli mi?

> *"Web uygulamasında Intent-based integration için Enterprise Browser şart mı, yoksa standart Chrome'da çalışan bir yöntem var mı?"*

Beklenen cevap: Keyboard wedge yeterli. Eğer intent öneriyorlarsa Enterprise Browser lisansı, yönetim maliyeti ve kısıtlamaları ayrıca sor.

---

## 3. Barkod Format Stratejisi — Bizim Sistemimize Özel

Toplantıda netleştirmemiz gereken konu:

> *"Sistemimizde birden fazla barkod tipi olacak: araç QR, raf adresi QR, ürün EAN barkodu, koli barkodu. DataWedge aynı cihazda farklı profiller kullanarak bağlama göre farklı davranabilir mi?"*

**Planladığımız format:**
- `AKY_VH_{plaka}` — Araç
- `AKY_LC_{adres}` — Raf/Lokasyon
- `AKY_KO_{id}` — Koli (bizim etiketimiz)
- Ham EAN-13/Code128 — Ürün (tedarikçi barkodu)

> *"Zebra'nın kendi etiket yazıcı çözümü var mı? ZPL yazıcılarla raf ve koli etiketi basımını da sisteme entegre edebilir miyiz?"*

---

## 4. Offline Kullanım

> *"Depo içinde Wi-Fi koptuğunda cihaz ne yapıyor? Tarama işlemleri kuyrukta bekler mi?"*

> *"Şoför senaryosunda 4G kesintisinde teslimat onayı nasıl çalışmalı?"*

**Bizim sistemimizin durumu:** Şu an offline kuyruğumuz yok, işlemler anlık API çağrısı gerektiriyor. Zebra tarafında herhangi bir offline buffer mekanizması var mı öğren.

---

## 5. Wi-Fi ve Ağ Performansı

> *"Depo içinde roaming performansı nasıl — access point değişiminde bağlantı kopuyor mu?"*

> *"Wi-Fi 6 bizim senaryoda gerçek fark yaratır mı, yoksa Wi-Fi 5 yeterli mi?"*

> *"Batarya gerçek kullanımda kaç saat gidiyor? Hot swap destekliyor mu?"*

---

## 6. Yönetim ve Kiosk Modu

> *"MDM ile Chrome'u kiosk moduna alıp sadece bizim uygulamamıza kilitleyebilir miyiz?"*

> *"Hangi MDM çözümünü öneriyorsunuz — SOTI, Ivanti? Yönetim maliyeti nedir?"*

> *"Başlangıçta web app ile ilerleyip ileride native uygulamaya geçmek istersek MDM konfigürasyonu sorun çıkarır mı?"*

---

## 7. Sonuç Sorusu

> *"Bizim operasyonumuz için siz olsanız hangi cihazı seçerdiniz ve neden?"*

Ardından:

> *"Handheld önermenizin tablet karşısındaki en güçlü 2 sebebi nedir?"*

---

## Toplantı Sonrası Değerlendirme Kriterleri

| Kriter | Kabul Şartı |
|--------|-------------|
| Scanner tipi | 2D imager zorunlu |
| Keyboard wedge | Chrome'da referans müşteri var |
| DataWedge | Browser output veya stable wedge konfigürasyonu |
| Kiosk mode | MDM ile Chrome kilitleme mümkün |
| Batarya | Vardiya süresi (8 saat) tek şarjla karşılanmalı |
| Hot swap | Varsa büyük artı |

---

## Teknik Notlar (İç Kullanım)

- Şu anki QR format: `AKYILDIZ_VEHICLE_*` → yeni standarda geçilecek: `AKY_VH_*`
- `jsQR` sadece QR okur; ürün barkodları 1D ise `@zxing/browser` gerekebilir (hardware scanner varsa sorun yok)
- Offline queue için: Workbox BackgroundSync + IndexedDB — henüz yapılmadı
- Intent-based entegrasyon için Enterprise Browser veya Capacitor wrapper gerekiyor — şimdilik planlanmıyor
