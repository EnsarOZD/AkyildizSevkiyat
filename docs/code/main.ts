import { createApp } from 'vue'
import { createPinia } from 'pinia'
import router from './router'
import './style.css'
import App from './App.vue'
import { vRole } from './directives/vRole'
import { useAuthStore } from './stores/auth'
import { useThemeStore } from './stores/theme'

const pinia = createPinia()
const app = createApp(App)

app.use(pinia)
useAuthStore().init()
useThemeStore().init()
app.use(router)
app.directive('role', vRole)

// Vue component error handler — component render/setup hatalarını yakalar.
// Hata olmadan içerik beyaz kalmasın diye console'a yazar.
app.config.errorHandler = (err, _instance, info) => {
  console.error('[Vue Error]', info, err)
}

// Yakalanmamış Promise rejection'ları logla
window.addEventListener('unhandledrejection', (event) => {
  console.error('[Unhandled Rejection]', event.reason)
})

// Lazy-load chunk yükleme hatası → yeni deployment sonrası eski hash'ler 404 verir.
// Kullanıcıyı aynı URL'e hard-redirect yaparak yeni versiyonu yükletir.
router.onError((error, to) => {
  const isChunkError =
    error.message.includes('Failed to fetch dynamically imported module') ||
    error.message.includes('Loading chunk') ||
    error.message.includes('Loading CSS chunk') ||
    error.message.includes('error loading dynamically imported module')
  if (isChunkError) {
    window.location.assign(to.fullPath)
  }
})

app.mount('#app')

// ════════ Açılış (splash) ekranını kaldır ════════
// index.html'deki #ak-splash anında görünür; app mount olduktan sonra
// (en az ~700ms gösterip) yumuşakça kaybolur ve DOM'dan silinir.
;(() => {
  const splash = document.getElementById('ak-splash')
  if (!splash) return
  const MIN_MS = 700            // çok hızlı açılışlarda flaş gibi geçmesin
  const start = Number((window as any).__akSplashStart || performance.timing?.navigationStart || Date.now())
  const elapsed = Date.now() - start
  const wait = Math.max(0, MIN_MS - elapsed)
  window.setTimeout(() => {
    splash.classList.add('ak-hide')
    window.setTimeout(() => splash.remove(), 500)  // fade süresi kadar bekle
  }, wait)
})()
