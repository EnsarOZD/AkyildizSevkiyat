# Akyıldız Şoför Paneli — Yeniden Tasarım (C + B koyu modern)

Bu klasör, sürücü panelinin onaylanan **"modern düzen + koyu operasyon paleti"** tasarımının
gerçek Vue dosyalarına uygulanmış halidir. Dosyalar **drop-in** — aşağıdaki eşlemeye göre
`client/src/...` altındaki orijinallerin üzerine kopyalayın.

## Dosya eşlemesi

| Bu paket | Hedef (repo içinde) |
|---|---|
| `layouts/DriverLayout.vue` | `client/src/layouts/DriverLayout.vue` |
| `views/DriverShipmentListView.vue` | `client/src/views/DriverShipmentListView.vue` |
| `views/DriverStopView.vue` | `client/src/views/DriverStopView.vue` |
| `views/DriverDeliveryView.vue` | `client/src/views/DriverDeliveryView.vue` |
| `views/DriverQrScanView.vue` | `client/src/views/DriverQrScanView.vue` |
| `views/DriverSettingsView.vue` | `client/src/views/DriverSettingsView.vue` |
| `components/driver/StopCard.vue` | `client/src/components/driver/StopCard.vue` |
| `components/driver/StatusBadge.vue` | `client/src/components/driver/StatusBadge.vue` |
| `components/driver/PermissionCard.vue` | `client/src/components/driver/PermissionCard.vue` |
| `index.html` | `client/index.html` *(yalnızca font `<link>`'leri eklendi)* |

## Önemli notlar

1. **Hiçbir `<script>` mantığı değişmedi.** Tüm store/servis/router çağrıları, prop/emit
   imzaları, `v-if`/`v-for`/binding'ler birebir korundu. Yalnızca `<template>` ve stiller güncellendi.

2. **Sürücü paneli artık varsayılan koyu.** `DriverLayout.vue` kök elemanına `dark` sınıfı
   eklendi; böylece bu pakette tam elden geçmeyen ekranlar bile (örn. mevcut `dark:` sınıfları
   üzerinden) otomatik koyu render olur. Uygulamanın geri kalanının tema mantığı etkilenmez.

3. **Yazı tipi:** Tasarım *Plus Jakarta Sans* ile çizildi. Pakete dahil edilen `index.html`,
   bu fontu Google Fonts'tan yükleyen `<link>`'leri içerir (mevcut `index.html`'inizle tek farkı budur).
   Font yüklenmezse `system-ui`'ye düşer (bozulma olmaz).

4. **Logo:** `DriverLayout` üst başlıkta `import logoUrl from '../assets/logo.png'` kullanır
   (repodaki mevcut desenle aynı).

5. **Tailwind:** Arbitrary value'lar (`bg-[#0f2038]`, `bg-blue-500/15`, gradient `from-*/to-*`)
   mevcut Tailwind JIT yapılandırmanızla çalışır; ek paket gerekmez.

## Palet (referans)

| Token | Değer |
|---|---|
| Zemin | `#0a1626` |
| Kart yüzeyi | `#0f2038` |
| Gradyan başlık | `linear-gradient(155deg,#1b3e74,#15315c,#0f2748)` |
| Birincil aksiyon | `from-blue-500 to-blue-600` |
| Onay/teslim | `from-emerald-500 to-green-600` |
| İade | `from-orange-500 to-orange-600` |
| Vurgu (şimdi) | amber-400 |
| İlerleme halkası | `#60a5fa` |
