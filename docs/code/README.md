# Akyıldız — Splash & Login (gerçek kod paketi)

Seçilen yön: **Splash A · Yükselen** + **Login C · Tam Ekran Minimal** + **İleri (chevron) logo** (üst beyaz, alt mavi aksan). Tema: koyu lacivert `#0a1626`.

Bu klasördeki dosyaları `client/` projene şöyle yerleştir:

| Bu paket | Hedef (client/) | İşlem |
|---|---|---|
| `LoginView.vue` | `src/views/LoginView.vue` | **Değiştir** (eskisinin yerine) |
| `index.html` | `index.html` | **Değiştir** |
| `main.ts` | `src/main.ts` | **Değiştir** (tek eklenti: en alttaki splash kaldırma bloğu) |
| `public/logo-icon.svg` | `public/logo-icon.svg` | **Ekle** |
| `public/logo-icon.png` | `public/logo-icon.png` | **Değiştir** (yeni chevron ikon, 512×512) |
| `public/logo-icon-maskable.png` | `public/logo-icon-maskable.png` | **Ekle** (Android maskable) |
| `public/logo.svg` | `public/logo.svg` | **Ekle** (yatay lockup — fatura/e-posta/doküman için) |
| `vite.config.manifest.ts` | — | Sadece **referans**: `vite.config.ts` içindeki manifest renklerini ve maskable ikonu güncelle |

---

## 1) Açılış (Splash) ekranı — nasıl çalışır?

- `index.html` içine, `#app`'in **yanına** `#ak-splash` overlay'i eklendi. Sayfa açılır açılmaz (Vue/JS yüklenmeden **önce**) anında görünür — beyaz ekran/flaş olmaz.
- Animasyon saf CSS: chevron'lar sırayla yükselir → üst chevron hafifçe parlar → **AKYILDIZ** belirir → ince ilerleme çubuğu akar.
- `main.ts` en altındaki blok, `app.mount()` sonrası overlay'i (en az ~700ms gösterip) yumuşakça kaldırır.
- `prefers-reduced-motion` açıksa animasyonlar kapanır, içerik direkt görünür.

> İnce ayar: minimum gösterim süresi `main.ts` içinde `MIN_MS = 700`. Daha uzun istersen artır.

## 2) Login — `LoginView.vue`

- `<script setup>` mantığı **birebir korundu** (username/password/rememberMe/showPassword, `authStore.login`, rol bazlı yönlendirme, `ApiErrorUtils`). Sadece şablon + stil yenilendi.
- Tailwind kullanır (projede mevcut). Ek bağımlılık yok. Plus Jakarta Sans `index.html`'den global geliyor.
- Tam ekran lacivert gradyan + soluk rota çizgileri + cam input'lar + mavi gradyan buton.
- `viewport-fit=cover` + `env(safe-area-inset-bottom)` ile çentikli telefonlarda footer güvenli alanda.

## 3) PWA / ikon

`vite.config.ts` → `VitePWA({ manifest: { … } })` içinde:

```diff
- theme_color: '#2563eb',
- background_color: '#ffffff',
+ theme_color: '#0a1626',
+ background_color: '#0a1626',
```

ve maskable ikonu ayrı dosyaya çevir (detay: `vite.config.manifest.ts`). `includeAssets` listesine `logo-icon.svg` eklemen iyi olur.

---

### Notlar
- Renkler: lacivert `#0a1626`/`#1b3e74`, mavi `#3b82f6`/`#2563eb`/`#60a5fa`.
- Logo tek renk de çalışır: `logo-icon.svg` açık zeminde (favicon/tarayıcı sekmesi) görünür koyu chevron; uygulama içi koyu zeminlerde beyaz+mavi inline SVG kullanıldı.
- favicon.ico'yu da yenilemek istersen `logo-icon.png`'den 32×32 üret.
```
