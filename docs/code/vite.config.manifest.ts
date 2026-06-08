// ════════ vite.config.ts — manifest GÜNCELLEMESİ ════════
// VitePWA({ ... manifest: { ... } }) bloğunda SADECE renkleri güncelle.
// (icons aynı kalıyor; logo-icon.png'i bu paketteki yenisiyle değiştir.)
//
// ÖNCE:                              SONRA:
//   theme_color: '#2563eb',    →       theme_color: '#0a1626',
//   background_color: '#ffffff', →     background_color: '#0a1626',
//
// Tam blok:

manifest: {
  name: 'Akyıldız Lojistik',
  short_name: 'Sevkiyat',
  description: 'Akyıldız Sevkiyat ve Lojistik Yönetim Sistemi',
  theme_color: '#0a1626',
  background_color: '#0a1626',
  display: 'standalone',
  orientation: 'any',
  start_url: '/',
  scope: '/',
  icons: [
    { src: '/logo-icon.png', sizes: '192x192', type: 'image/png', purpose: 'any' },
    { src: '/logo-icon.png', sizes: '512x512', type: 'image/png', purpose: 'any' },
    { src: '/logo-icon-maskable.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
  ],
},
