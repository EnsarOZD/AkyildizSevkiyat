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

useAuthStore().init()
useThemeStore().init()

app.mount('#app')
