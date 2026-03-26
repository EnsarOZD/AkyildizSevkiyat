# Akyildiz Sevkiyat — UI/UX Analiz & İyileştirme Yol Haritası

> Hazırlanma: 2026-03-18 | Son Güncelleme: 2026-03-22
> Kapsam: Mevcut Vue 3 SPA'nın kullanılabilirlik analizi ve önceliklendirilmiş iyileştirme planı.
> **Sprint 1 TAMAMLANDI** | **Sprint 2 TAMAMLANDI** | **Sprint 3 TAMAMLANDI** | **Sprint 4 TAMAMLANDI** | **Sprint 5 kısmen (U21/U24 API bekleniyor)** | **Sprint 6 TAMAMLANDI** | **Sprint 7 TAMAMLANDI**

---

## İçindekiler

1. [Mevcut Durum Analizi](#1-mevcut-durum-analizi)
2. [Kritik Sorunlar (Hemen Düzeltilmeli)](#2-kritik-sorunlar)
3. [Önemli İyileştirmeler](#3-önemli-i̇yileştirmeler)
4. [Orta Vadeli Geliştirmeler](#4-orta-vadeli-geliştirmeler)
5. [Uzun Vadeli / Vizyon](#5-uzun-vadeli--vizyon)
6. [Öncelik Sırası & Uygulama Planı](#6-öncelik-sırası--uygulama-planı)

---

## 1. Mevcut Durum Analizi

### Güçlü Yönler

- ✅ Responsive layout (sidebar + main content)
- ✅ Rol tabanlı menü filtrelemesi çalışıyor
- ✅ Toast bildirimleri (NotificationContainer) mevcut
- ✅ Modal tabanlı form yapısı tutarlı (BaseModal)
- ✅ TailwindCSS ile tutarlı bir renk paleti var
- ✅ Route-based lazy loading uygulanmış

### Tespit Edilen Sorunlar

| Kategori | Sorun | Şiddet | Durum |
|----------|-------|--------|-------|
| Navigasyon | "Depo Yönetimi" grubunda 10 madde — aşırı kalabalık | 🔴 Kritik | ✅ Çözüldü (U01) |
| Dashboard | HomeView sadece "Hoşgeldiniz" + logout butonu — içerik yok | 🔴 Kritik | ✅ Çözüldü (U02) |
| İkonlar | Emoji kullanımı (📦, 🗺️, 🔢) — OS'a göre farklı render, düşük profesyonellik | 🟠 Önemli | ✅ Çözüldü (U03) |
| Form Validasyon | Hata mesajları alert() ile gösteriliyor (bazı yerlerde) | 🟡 Orta | ✅ Çözüldü (U06) |
| Sayfa Başlıkları | document.title güncellenmez | 🟡 Orta | ✅ Çözüldü (U11) |
| Görsel Geri Bildirim | Yükleme durumu sadece metin ("Yükleniyor...") — skeleton yok | 🟠 Önemli | ✅ Çözüldü (U04) |
| Tablo UX | Tablolarda sıralama, satır başına aksiyon menüsü yok | 🟠 Önemli | ⬜ Sprint 3 |
| Breadcrumb | İç sayfalarda (sevkiyat detay, mal girişi) konum bilgisi yok | 🟡 Orta | ✅ Çözüldü (U07) |
| Durum Renkleri | Status badge renkleri görünümler arasında tutarsız | 🟡 Orta | ✅ Çözüldü (U08) |
| Boş Durum | Tablolarda sadece metin — resimli/açıklamalı empty state yok | 🟡 Orta | ✅ Çözüldü (U09) |
| Header | Üst header minimum bilgi taşıyor, aksiyon alanı yok | 🟡 Orta | ⬜ Sprint 3 |
| Mobil | Tablolar mobilde yatay scroll gerektiriyor, optimize değil | 🟡 Orta | ⬜ Sprint 4 |
| Klavye | Tab gezinme, Enter ile onay kısayolları eksik | 🟢 Düşük | ⬜ Sprint 4 |

---

## 2. Kritik Sorunlar

### ✅ U01 — Navigasyon Yeniden Yapılandırması 🔴 — TAMAMLANDI

**Mevcut durum:**
"Depo Yönetimi" grubu 10 madde içeriyor. Admin kullanıcısı tüm menüyü gördüğünde sidebar scroll gerektiriyor ve hangi gruba gittiği belirsizleşiyor.

**Mevcut grup yapısı:**
```
[Grup 1 - Başlıksız]
  Dashboard, Sevkiyatlar

[Grup 2 - Sistem]
  Kullanıcı Yönetimi

[Grup 3 - Satınalma & Giriş]
  Satınalma, Mal Kabul, Tedarikçiler

[Grup 4 - Depo Yönetimi]  ← 10 madde! 🚨
  Depo Hazırlık, Şoför & Araç, Bölge Yönetimi, Proje-Bölge,
  Stok Yönetimi, Raporlar, Bölge Malzeme Raporu,
  Belirsiz İadeler, Stok Sayımı, ISS Entegrasyon
```

**Önerilen yeni yapı (6 grup, max 5 madde/grup):**

```
[Grup 1 - Başlıksız]
  🏠 Dashboard
  📋 Sevkiyatlar

[Grup 2 - Operasyon]
  🏭 Depo Hazırlık           (Admin, Warehouse, Manager)
  🚚 Şoför & Araç            (Admin, Dispatcher, Manager)
  🔄 Belirsiz İadeler        (Admin, Manager, Warehouse, Dispatcher)

[Grup 3 - Stok & Depo]
  📦 Stok Yönetimi           (Admin, Accounting, Manager)
  🔢 Stok Sayımı             (Admin, Manager, Warehouse)
  🗺️  Bölge Yönetimi         (Admin, Manager)
  🧩 Proje - Bölge           (Admin, Manager)

[Grup 4 - Satınalma]
  🛒 Satınalma Siparişleri   (Admin, Accounting, Manager)
  📥 Mal Kabul İrsaliyeleri  (Admin, Warehouse, Manager)
  🏢 Tedarikçiler            (Admin, Accounting, Manager)

[Grup 5 - Raporlar]
  📊 Raporlar                (Admin, Accounting, Warehouse, Manager)
  📋 Bölge Malzeme Raporu    (Tüm roller)

[Grup 6 - Sistem]
  👥 Kullanıcı Yönetimi      (Admin)
  📡 ISS Entegrasyon         (Admin, Accounting, Manager) [Beta]
```

**Faydaları:**
- Her grup max 4-5 madde → scroll azalır
- "Stok & Depo" grubu semantik olarak tutarlı
- "Raporlar" ayrı grup → bulunabilirlik artar
- Roller aynı kalır, sadece gruplama değişir

---

### ✅ U02 — Dashboard (HomeView) Yeniden Tasarımı 🔴 — TAMAMLANDI

**Mevcut durum:** HomeView sadece "Hoşgeldiniz" mesajı ve logout butonu. Warehouse kullanıcısı otomatik `/warehouse`'a yönlendiriliyor ama diğer roller için anlamlı içerik yok.

**Önerilen içerik (role göre kişiselleştirilmiş KPI kartları):**

```
┌─────────────────────────────────────────────────────┐
│  Bugün: 18 Mart 2026, Salı                          │
│  Hoşgeldiniz, Ensar 👋                              │
├──────────┬──────────┬──────────┬────────────────────┤
│ Aktif    │ Araçta   │ Kritik   │ Bekleyen           │
│ Sevkiyat │ Sevkiyat │ Stok     │ Mal Girişi         │
│   12     │    3     │    5     │    2               │
├──────────┴──────────┴──────────┴────────────────────┤
│ Bugünkü Sevkiyatlar (durum bazlı mini tablo)        │
│ + Son Aktiviteler (history feed)                    │
└─────────────────────────────────────────────────────┘
```

**Role göre gösterilecek kartlar:**
- **Admin/Manager:** Tüm KPI'lar
- **Warehouse:** Hazırlanacak sevkiyat sayısı, depo hazırlık durumu
- **Dispatcher:** Araçtaki sevkiyatlar, teslim bekleyenler
- **Accounting:** Bekleyen PO'lar, onay bekleyen mal girişleri

---

## 3. Önemli İyileştirmeler

### ✅ U03 — SVG İkon Sistemi 🟠 — TAMAMLANDI

**Sorun:** Emoji ikonlar (📦, 🗺️, 🔢, 🔄) farklı işletim sistemlerinde farklı görünür, bazı tarayıcılarda renksiz render olur, animasyon kısıtlıdır.

**Öneri:** [Heroicons](https://heroicons.com/) (Tailwind Labs'ın ikonu, MIT lisanslı) kullanımı.
- `npm install @heroicons/vue`
- Her nav item için tutarlı 20×20px outline veya solid SVG
- Hover animasyonları CSS ile kolayca yapılabilir

**Örnek dönüşüm:**
```
📋 Sevkiyatlar      →  ClipboardDocumentListIcon
📦 Stok Yönetimi   →  ArchiveBoxIcon
🚚 Şoför & Araç    →  TruckIcon
📊 Raporlar        →  ChartBarIcon
👥 Kullanıcılar    →  UsersIcon
🔢 Stok Sayımı     →  CalculatorIcon
🔄 Belirsiz İadeler → ArrowPathIcon
```

---

### U04 — Skeleton Loading 🟠

**Sorun:** Tüm listeler/tablolar yüklenirken sadece "Yükleniyor..." metni gösteriliyor. Bu hem görsel açıdan zayıf hem de sayfanın ne zaman hazır olduğu belirsizleştiriyor.

**Öneri:** Basit bir `SkeletonLoader` bileşeni:
```vue
<!-- Tablo satırı skeleton -->
<div class="animate-pulse bg-gray-200 h-10 rounded mb-2" />
<div class="animate-pulse bg-gray-200 h-10 rounded mb-2 opacity-75" />
<div class="animate-pulse bg-gray-200 h-10 rounded mb-2 opacity-50" />
```

**Uygulanacak yerler:** Tüm liste ve tablo yüklemeleri (ShipmentList, StockManagement, PurchaseOrders, GoodsReceipts, vb.)

---

### U05 — Tablo İyileştirmeleri 🟠

**Mevcut sorunlar:**
- Tablolarda sütun sıralaması (sort) yok
- Satır başına aksiyon menüsü yok (her yer farklı yapıda)
- Mobilde tablo horizontal scroll oluyor, card view yok

**Önerilen değişiklikler:**

**a) Sütun sıralama:**
```vue
<th @click="sortBy('status')" class="cursor-pointer select-none">
  Durum
  <ChevronUpDownIcon class="inline w-4 h-4 ml-1" />
</th>
```

**b) Satır aksiyonları — tutarlı pattern:**
Şu an bazı tablolarda inline buton, bazılarında modal açma var. Standart:
- Birincil aksiyon: satıra tıklayınca detaya git
- İkincil aksiyonlar: satır sonundaki "..." dropdown menü

**c) Mobil card view:**
Küçük ekranlarda tablo yerine kart görünümü:
```vue
<div v-for="item in items">
  <!-- Desktop: tablo satırı -->
  <tr class="hidden md:table-row">...</tr>
  <!-- Mobile: kart -->
  <div class="md:hidden bg-white rounded-lg shadow p-4 mb-2">...</div>
</div>
```

---

### ✅ U06 — Form Validasyon Görünümü 🟠 — TAMAMLANDI

**Sorun:** Bazı yerlerde `alert()` kullanılıyor (özellikle assign vehicle), bazı yerlerde toast notification, bazılarında inline hata. Tutarsız.

**Standart öneri:**
- `alert()` → kaldır, tüm hatalar toast notification üzerinden
- FluentValidation hata mesajları backend'den geliyor → bunları form alanı altında inline göster
- `ApiErrorUtils.getErrorMessage()` zaten var ama bazı yerlerde `alert()` ile kullanılıyor

**Uygulama:**
```vue
<!-- Inline hata gösterimi -->
<div v-if="errors.driverName" class="text-red-500 text-xs mt-1">
  {{ errors.driverName }}
</div>
```

---

### U07 — Breadcrumb Navigasyonu 🟡

**Sorun:** ShipmentDetailView gibi iç sayfalarda kullanıcı nerede olduğunu göremez.

**Öneri:** `AppHeader.vue` içine route'a göre breadcrumb:
```
Sevkiyatlar > Sevkiyat #142 > Detay
Mal Kabul İrsaliyeleri > GR-2024-001
```

**Uygulama:** `route.meta.breadcrumb` array'i tanımlamak ve header'da göstermek.

---

### U08 — Durum Badge Standardizasyonu 🟡

**Sorun:** Sevkiyat durumları (Created, AssignedToWarehouse, vb.), PO durumları, GoodsReceipt durumları — her görünümde farklı renk/stil kullanılıyor.

**Öneri:** Global `StatusBadge` bileşeni:
```vue
<StatusBadge :status="shipment.status" type="shipment" />
<StatusBadge :status="po.status" type="purchaseOrder" />
```

Merkezi renk haritası:
```typescript
// Sevkiyat
Created: gray, AssignedToWarehouse: yellow, Picking: blue,
ReadyForDispatch: purple, AssignedToVehicle: indigo,
Delivered: green, ReturnedToWarehouse: orange, Cancelled: red

// PurchaseOrder
Draft: gray, Approved: blue, PartiallyReceived: yellow,
FullyReceived: green, Closed: gray/dark, Cancelled: red

// FloatingReturn
Pending: orange, MatchedToShipment: blue,
AddedToStock: green, WrittenOff: gray
```

---

### U09 — Boş Durum (Empty State) İyileştirmesi 🟡

**Sorun:** Veri yokken sadece "Kayıt bulunamadı." gibi kısa metinler. Kullanıcı ne yapacağını bilemiyor.

**Öneri:** Her boş durum için:
- SVG illüstrasyon veya büyük ikon
- Açıklayıcı metin
- Call-to-action butonu

```
📋
Henüz sevkiyat oluşturulmamış.
ISS siparişlerini import ederek sevkiyat oluşturabilirsiniz.
[Sipariş İmport Et →]
```

---

## 4. Orta Vadeli Geliştirmeler

### ✅ U10 — URL Filter State — TAMAMLANDI (kısmen)

**Mevcut:** Her sayfada farklı filtre yapısı, bazılarında yok.

**Tamamlanan:**
- `ShipmentListView`: `tab`, `date`, `status`, `search`, `page`, `sortKey`, `sortDir` URL query params'a yazılıyor. `router.replace()` ile deep link + back/forward navigasyonu çalışıyor.

**Kalan (opsiyonel):**
- Global arama (Cmd+K / Ctrl+K) — sevkiyat, proje, stok kodu arama
- "Aktif filtreler" chip'leri (her filtre X ile kaldırılabilir)
- Diğer listelere URL sync uygulanabilir (PurchaseOrders, GoodsReceipts, FloatingReturns)

```
[Durum: Araçta ✕] [Tarih: Bu hafta ✕] [Bölge: Anadolu-1 ✕]   [Tümünü Temizle]
```

---

### ✅ U11 — Sayfa Başlıkları & Meta Tutarlılığı — TAMAMLANDI

**Sorun:** `document.title` güncellenmez — tarayıcı sekmesinde hep "Akyıldız Sevkiyat" yazar.

**Öneri:** Router `afterEach` hook:
```typescript
router.afterEach((to) => {
  document.title = to.meta.title
    ? `${to.meta.title} | Akyıldız Sevkiyat`
    : 'Akyıldız Sevkiyat';
});
```

---

### ✅ U12 — Keyboard Shortcuts — TAMAMLANDI

| Kısayol | Aksiyon | Durum |
|---------|---------|-------|
| `Esc` | Açık modalı kapat | ✅ BaseModal'da `@keydown.esc` |
| `N` | Yeni oluştur (PurchaseOrders, StockManagement, GoodsReceipts) | ✅ `useKeyboardShortcut` composable |
| `←` / `→` | Sayfalama (ShipmentListView) | ✅ `useKeyboardShortcut` composable |
| `Ctrl+K` | Global arama | ⬜ Sprint 7 (karmaşık) |

`useKeyboardShortcut(key, handler)` composable oluşturuldu — input/textarea içindeyken otomatik ignore eder, `onMounted`/`onUnmounted` lifecycle'a bağlı.

---

### U13 — Sevkiyat Listesi Akış İyileştirmeleri

**Mevcut sorunlar:**
- Hangi sevkiyatların "acil" olduğu belli değil (gecikenler vurgulanmıyor)
- Teslim tarihi geçmiş sevkiyatlar kırmızı değil

**Öneri:**
- Bugünkü sevkiyatlar → mavi arka plan veya "Bugün" badge
- Teslim tarihi geçmiş + teslim edilmemiş → kırmızı kenarlık + "Gecikmiş" badge
- Durum sütununda ileriye dönük sıralama (önce acililer)

---

### U14 — Sevkiyat Detay Sayfa Yeniden Düzeni

**Mevcut sorun:** Bilgi kartları dikey sıralı, aksiyon butonları ayrı bir panelde, uzun sayfalarda scroll gerekiyor.

**Öneri:**
```
┌─────────────────────────┬────────────────────┐
│ Sevkiyat Başlığı +      │ Durum & Aksiyonlar │
│ Proje / Tarih Bilgisi   │ (sticky sidebar)   │
├─────────────────────────┤                    │
│ Ürün Listesi (tab)      │                    │
│ Geçmiş (tab)            │                    │
│ Sürücü Bilgisi (tab)    │                    │
└─────────────────────────┴────────────────────┘
```
- Aksiyon butonları sağ panelde sticky kalır
- Bilgiler tab'lara ayrılır → sayfa daha kısa ve okunabilir

---

### U15 — Bildirim & Onay Akışları

**Sorun:** Yıkıcı aksiyonlar (sevkiyat iptal etme, stok sayımı tamamlama) için bazı yerlerde onay modal var, bazılarında yok.

**Standart:**
- Geri dönülemez her aksiyon → `ConfirmDialog` bileşeni (zaten var, tutarlı kullanılmalı)
- `ConfirmDialog` içinde aksiyon açıklaması + ne olacağının özeti
- Yıkıcı aksiyonlar için buton rengi kırmızı, onay metni "Evet, İptal Et" gibi

---

### U16 — Print / Export Desteği

**Öneri:**
- Sevkiyat detay → "Yazdır" (browser print CSS ile yönlendirme kağıdı formatı)
- Stok listesi → Excel export (mevcut import var, export eklenebilir)
- Raporlar → PDF export (tarayıcı print → PDF yeterli, özel CSS ile)

---

## 5. Uzun Vadeli / Vizyon

### ✅ U17 — Dark Mode — TAMAMLANDI

Tailwind `darkMode: 'class'` konfigürasyonu, `useThemeStore` (Pinia), localStorage persistance, `window.matchMedia('prefers-color-scheme')` system default. Toggle butonu AppHeader'da — güneş/ay ikonu.

---

### ✅ U18 — Accessibility (a11y) — TAMAMLANDI

- [x] Global `:focus-visible` ring stili (`style.css`) — klavye navigasyonu görünür
- [x] Skip-to-content linki (`index.html` + `DefaultLayout main#main-content`)
- [x] `aria-label` → AppHeader (hamburger, dark mode toggle, logout), AppSidebar (close, `aria-label="Ana menü"`), DriverLayout (geri, çıkış), BaseModal (kapat → Türkçe), NotificationContainer (kapat)
- [x] `aria-hidden="true"` dekoratif ikonlara eklendi
- [x] `inputmode="decimal/numeric"` sayısal input'lara eklendi (mobil klavye optimize)

---

### ✅ U19 — Animasyonlar & Geçişler — TAMAMLANDI

- [x] Sayfa geçiş animasyonu — `DefaultLayout` `<router-view>` → `<Transition name="page" mode="out-in">` (150ms fade + 6px slide)
- [x] Modal animasyonu — `BaseModal.vue` enter/leave transitions (scale + opacity)
- [x] Toast animasyonu — `NotificationContainer.vue` `TransitionGroup name="toast"` (slide-in sağdan)
- [x] Confirm dialog — `ConfirmDialog.vue` `<Transition name="fade">`

---

### U20 — Mobil Deneyim Paketi

Şoför rolü ileride mobil kullanacak (Faz 3). Şimdiden:
- Tablolarda touch-friendly row height (min 48px)
- Tüm form input'larında `inputmode` attribute (sayısal alanlarda `inputmode="decimal"`)
- Bottom navigation bar opsiyonu (mobil için sidebar yerine)

---

## 6. Öncelik Sırası & Uygulama Planı

### ✅ Sprint 1 — TAMAMLANDI

| # | Madde | Etki | Durum |
|---|-------|------|-------|
| U01 | Navigasyon yeniden yapılandırması | ⭐⭐⭐⭐⭐ | ✅ |
| U02 | Dashboard KPI kartları | ⭐⭐⭐⭐⭐ | ✅ |
| U03 | Heroicons SVG ikon sistemi | ⭐⭐⭐⭐ | ✅ |
| U06 | alert() → toast dönüşümü | ⭐⭐⭐ | ✅ |
| U11 | Sayfa başlıkları (document.title) | ⭐⭐ | ✅ |

**Backend eklentisi:** `GET /api/dashboard/stats` endpoint'i (`DashboardController` + `GetDashboardStatsQuery`)

### ✅ Sprint 2 — TAMAMLANDI

| # | Madde | Etki | Durum |
|---|-------|------|-------|
| U04 | Skeleton loading bileşeni | ⭐⭐⭐⭐ | ✅ |
| U08 | StatusBadge bileşeni | ⭐⭐⭐ | ✅ |
| U09 | Empty state bileşeni | ⭐⭐⭐ | ✅ |
| U13 | Sevkiyat listesi gecikme vurgusu | ⭐⭐⭐⭐ | ✅ |

**Oluşturulan bileşenler:** `StatusBadge.vue`, `SkeletonTable.vue`, `EmptyState.vue`
**Güncellenen sayfalar:** `ShipmentListView`, `PurchaseOrdersView`, `GoodsReceiptsView`, `FloatingReturnsView`

### ✅ Sprint 3 — TAMAMLANDI

| # | Madde | Etki | Durum |
|---|-------|------|-------|
| U07 | Breadcrumb navigasyonu | ⭐⭐⭐ | ✅ |
| U15 | Onay akışı standardizasyonu | ⭐⭐⭐ | ✅ |
| U05 | Tablo sütun sıralama (ShipmentList) | ⭐⭐⭐ | ✅ |

**U07 detay:** `AppHeader.vue` route'dan breadcrumb oluşturuyor. Router'a `meta.breadcrumb` eklendi (ShipmentDetail, PurchaseOrderSelection, ZoneMaterialReport).
**U15 detay:** 9 dosyada `confirm()` / inline modal → `notificationStore.promptConfirm()` ile değiştirildi (ZoneList, UserManagement, TransportManagement, GoodsReceipts, WarehouseDashboard, PurchaseOrders, ShipmentList).
**U05 detay:** ShipmentList'te No, Tarih, Durum sütunlarına tıklanabilir sort indikatörleri eklendi.

### ✅ Sprint 4 — TAMAMLANDI

| # | Madde | Etki | Durum |
|---|-------|------|-------|
| U10 | Filtreler URL params'a yazılsın (ShipmentList) | ⭐⭐⭐ | ✅ |
| U14 | Sevkiyat detay yeniden düzeni (sticky sidebar + tab'lar) | ⭐⭐⭐ | ✅ |

**U10 detay:** `ShipmentListView.vue` — tab/date/status/search/page/sortKey/sortDir `router.replace()` ile URL'e yazılıyor. Sayfa yenileme ve back/forward navigasyonu korunuyor.
**U14 detay:** `ShipmentDetailView.vue` komple yeniden yapılandırıldı. Sol: info kartı + 3 sekme (Ürünler, Sürücü & Teslimat, Tarihçe). Sağ: `lg:sticky` sidebar (StatusBadge + aksiyon butonları). `prompt()` → "Taslağa Geri Çek" modalı. `getStatusClass` → `StatusBadge` ile değiştirildi.

### Sprint 5 — Netsis & Faz 2 Tamamlamaları

Netsis API bilgileri gelince + Faz 2 artık iş mantığı yerli yerinde.

| # | Madde | Etki | Durum |
|---|-------|------|-------|
| U21 | Netsis aktarım butonu — ShipmentDetailView | ⭐⭐⭐⭐ | ⬜ API bekleniyor |
| U22 | Proje `NetsisCariKodu` düzenleme alanı | ⭐⭐⭐ | ✅ Tamamlandı |
| U23 | Stok `NetsisStockCode` düzenleme kolonu | ⭐⭐⭐ | ✅ Tamamlandı |
| U24 | Stok senkronizasyon aksiyonu (Netsis → StockMaster) | ⭐⭐⭐ | ⬜ API bekleniyor |
| U16 | Sevkiyat irsaliye yazdır / PDF export | ⭐⭐⭐ | ~~İptal — Netsis halleder~~ |

**U21 detay:** `ShipmentDetailView` sidebar'ına "Netsis'e Aktar" butonu eklenecek. `NetsisTransferredAt` doluysa "Aktarıldı ✓" badge göster, doluysa buton pasif. `POST /api/netsis/shipments/{id}/export` çağrısı.

**U22 detay:** `/projects` listesinde veya proje detay modalında `NetsisCariKodu` düzenlenebilir input alanı eklenecek.

**U23 detay:** `/stocks` sayfasına `NetsisStockCode` kolonu eklenecek — inline düzenleme veya toplu import.

**U24 detay:** Stok sayfasına "Netsis'ten Senkronize Et" butonu — `POST /api/netsis/stock/sync` çağrısı, sonuç özeti toast ile gösterilecek.

**U16:** İptal edildi — irsaliye oluşturma ve yazdırma Netsis tarafında yönetilecek.

---

### ✅ Sprint 6 — Şoför Paneli (Faz 3 kapsamı) — TAMAMLANDI

| # | Madde | Etki | Durum |
|---|-------|------|-------|
| U25 | `/driver` rotası — Dispatcher rolüne özel | ⭐⭐⭐⭐ | ✅ Tamamlandı |
| U26 | Günlük araç-sevkiyat listesi (mobil önce tasarım) | ⭐⭐⭐⭐ | ✅ Tamamlandı |
| U27 | Teslimat noktası detay + navigasyon deep link | ⭐⭐⭐⭐ | ✅ Tamamlandı |
| U20 | Mobil tablo → kart view dönüşümü | ⭐⭐⭐ | ✅ Tamamlandı (ShipmentList zaten `md:hidden` kart view'a sahip) |
| U28 | PWA manifest + offline desteği (temel) | ⭐⭐⭐ | ✅ Tamamlandı |

**U25-27 detay:** Dispatcher kullanıcısı giriş yapınca `/driver` sayfasına yönlendirilir. Günün yüklü araçları listelenir. Her teslimat noktası için adres + Google Maps / Yandex deep link. Teslimat fotoğrafı zaten hazır (F2-04).

---

### Sprint 7 — Uzun Vadeli

| # | Madde | Etki | Durum |
|---|-------|------|-------|
| U12 | Keyboard shortcuts | ⭐⭐ | ✅ Tamamlandı (N, ←/→, Esc) |
| U17 | Dark mode toggle | ⭐⭐ | ✅ Tamamlandı |
| U18 | Accessibility (a11y) — ARIA, contrast | ⭐⭐⭐ | ✅ Tamamlandı |
| U19 | Animasyon standardizasyonu | ⭐ | ✅ Tamamlandı |
| U12+ | Ctrl+K global arama | ⭐⭐ | ✅ Tamamlandı |

---

## Ek: Navigasyon Değişikliği için navigation.ts Taslağı

```typescript
export const NAV_ITEMS: NavGroup[] = [
  {
    // Başlıksız — her rol görür
    items: [
      { label: 'Dashboard',   to: '/',         icon: 'HomeIcon' },
      { label: 'Sevkiyatlar', to: '/shipments', icon: 'ClipboardDocumentListIcon' },
    ],
  },
  {
    title: 'Operasyon',
    items: [
      { label: 'Depo Hazırlık',    to: '/warehouse',       icon: 'BuildingStorefrontIcon', roles: ['Admin', 'Warehouse', 'Manager'] },
      { label: 'Şoför & Araç',     to: '/transport',       icon: 'TruckIcon',              roles: ['Admin', 'Dispatcher', 'Manager'] },
      { label: 'Belirsiz İadeler', to: '/floating-returns',icon: 'ArrowPathIcon',          roles: ['Admin', 'Manager', 'Warehouse', 'Dispatcher'] },
    ],
  },
  {
    title: 'Stok & Depo',
    items: [
      { label: 'Stok Yönetimi', to: '/stocks',            icon: 'ArchiveBoxIcon',   roles: ['Admin', 'Accounting', 'Manager'] },
      { label: 'Stok Sayımı',   to: '/stock-counts',      icon: 'CalculatorIcon',   roles: ['Admin', 'Manager', 'Warehouse'] },
      { label: 'Bölge Yönetimi',to: '/zones',             icon: 'MapIcon',          roles: ['Admin', 'Manager'] },
      { label: 'Proje - Bölge', to: '/projects/zone-mapping', icon: 'PuzzlePieceIcon', roles: ['Admin', 'Manager'] },
    ],
  },
  {
    title: 'Satınalma',
    items: [
      { label: 'Satınalma Siparişleri',  to: '/purchase-orders', icon: 'ShoppingCartIcon', roles: ['Admin', 'Accounting', 'Manager'] },
      { label: 'Mal Kabul İrsaliyeleri', to: '/goods-receipts',  icon: 'InboxArrowDownIcon', roles: ['Admin', 'Warehouse', 'Manager'] },
      { label: 'Tedarikçiler',           to: '/suppliers',        icon: 'BuildingOfficeIcon', roles: ['Admin', 'Accounting', 'Manager'] },
    ],
  },
  {
    title: 'Raporlar',
    items: [
      { label: 'Raporlar',             to: '/reports',              icon: 'ChartBarIcon', roles: ['Admin', 'Accounting', 'Warehouse', 'Manager'] },
      { label: 'Bölge Malzeme Raporu', to: '/reports/zone-material',icon: 'TableCellsIcon' },
    ],
  },
  {
    title: 'Sistem',
    items: [
      { label: 'Kullanıcı Yönetimi', to: '/users',         icon: 'UsersIcon',       roles: ['Admin'] },
      { label: 'ISS Entegrasyon',    to: '/orders/import', icon: 'SignalIcon',       roles: ['Admin', 'Accounting', 'Manager'], badge: 'Beta' },
    ],
  },
];
```

---

---

## Tamamlananlar Özeti

| Sprint | Madde | Dosyalar |
|--------|-------|---------|
| Sprint 1 | U01 Nav yeniden yapılandırması | `navigation.ts` |
| Sprint 1 | U02 Dashboard KPI | `HomeView.vue`, `dashboardService.ts`, `DashboardController.cs`, `GetDashboardStatsQuery.cs` |
| Sprint 1 | U03 Heroicons | `navigation.ts`, `AppSidebar.vue` |
| Sprint 1 | U06 alert→toast | `ShipmentDetailView.vue`, `PurchaseOrdersView.vue`, `MacroPickingModal.vue` |
| Sprint 1 | U11 document.title | `router/index.ts` |
| Sprint 2 | U04 Skeleton loading | `SkeletonTable.vue` → ShipmentListView, PurchaseOrdersView, GoodsReceiptsView |
| Sprint 2 | U08 StatusBadge | `StatusBadge.vue` → ShipmentListView, PurchaseOrdersView, GoodsReceiptsView, FloatingReturnsView |
| Sprint 2 | U09 EmptyState | `EmptyState.vue` → ShipmentListView, PurchaseOrdersView, GoodsReceiptsView |
| Sprint 2 | U13 Gecikmiş vurgu | `ShipmentListView.vue` — kırmızı satır + "Gecikmiş" badge, "Bugün" badge |
| Sprint 3 | U07 Breadcrumb | `AppHeader.vue`, `router/index.ts` (meta.breadcrumb) |
| Sprint 3 | U15 Confirm standardizasyon | 9 view → `promptConfirm()` (tüm native `confirm()` kaldırıldı) |
| Sprint 3 | U05 Sütun sıralama | `ShipmentListView.vue` — No, Tarih, Durum sort |
| Sprint 4 | U10 URL filter state | `ShipmentListView.vue` — tab/date/status/search/page/sortKey/sortDir → URL query params |
| Sprint 4 | U14 Sevkiyat detay yeniden düzeni | `ShipmentDetailView.vue` — 2 sütun layout, sticky sidebar, 3 sekme, prompt()→modal |
| Faz 2 | Teslimat fotoğrafı upload | `ShipmentDetailView.vue` — kamera/dosya input, canvas sıkıştırma, thumbnail + lightbox |
| Faz 2 | İrsaliye no — araç atama uyarısı | `ShipmentDetailView.vue` — araç modal'ında irsaliye eksikse amber uyarı banner |
| Sprint 5 | U22 NetsisCariKodu inline edit | `ProjectMappingView.vue`, `UpdateProjectNetsisCariKoduCommand.cs` |
| Sprint 5 | U23 NetsisStockCode kolonu | `StockManagementView.vue`, `UpdateStockCommand.cs` |
| Sprint 6 | U25 `/driver` rotası | `DriverLayout.vue`, `router/index.ts` |
| Sprint 6 | U26 Şoför sevkiyat listesi | `DriverShipmentListView.vue`, `ShipmentCard.vue`, `StatusBadge.vue` (driver) |
| Sprint 6 | U27 Teslimat detay + navigasyon | `DriverDeliveryView.vue` — Google Maps deep link, teslimat formu, fotoğraf |
| Sprint 6 | U28 PWA manifest | `vite.config.ts` — vite-plugin-pwa, workbox, `index.html` Apple meta tags |
| Sprint 6 | U20 Mobil kart view | `ShipmentListView.vue` — `md:hidden` kart view zaten mevcut ✅ |
| Sprint 6 | U17 Dark mode | `stores/theme.ts`, `AppHeader.vue` — localStorage + system preference |
| Sprint 7 | U12 Klavye kısayolları | `useKeyboardShortcut.ts` composable — N (PO/Stok/MalGirişi), ←/→ (ShipmentList) |
| Sprint 7 | U18 Accessibility | `style.css` focus-visible, `index.html` skip-link, `AppHeader/Sidebar/DriverLayout` aria-label |
| Sprint 7 | U19 Animasyonlar | `DefaultLayout.vue` page transition, BaseModal/Toast/Confirm zaten mevcut |
| Sprint 7 | U12+ Ctrl+K Global Arama | `GlobalSearchModal.vue`, `searchService.ts`, `SearchController.cs`, `GetGlobalSearchQuery.cs` |
| F4-05 | Excel export | `exportExcel.ts` (SheetJS), `ReportsDashboardView.vue` — tüm 6 sekmede buton |

---

*Bu döküman UI/UX iyileştirmeleri tamamlandıkça güncellenir.*
