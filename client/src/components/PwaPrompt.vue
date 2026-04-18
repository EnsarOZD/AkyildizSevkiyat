<template>
  <div v-if="needRefresh" class="fixed bottom-4 right-4 z-[9999] bg-white dark:bg-gray-800 p-4 rounded-xl shadow-2xl border border-gray-200 dark:border-gray-700 max-w-sm flex flex-col gap-3 animate-in slide-in-from-bottom-5">
    <div>
      <p class="font-bold text-gray-900 dark:text-gray-100 flex items-center gap-2">
        <svg class="h-5 w-5 text-indigo-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
        </svg>
        Yeni Versiyon Mevcut
      </p>
      <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">Uygulamanın yeni bir sürümü yüklendi. Uygulamayı güncelleyerek en son özelliklere erişebilirsiniz.</p>
    </div>
    <div class="flex items-center gap-2 mt-1">
      <button @click="updateServiceWorker()" class="flex-1 bg-indigo-600 text-white px-4 py-2.5 rounded-lg text-sm font-bold shadow-lg shadow-indigo-200 dark:shadow-none hover:bg-indigo-700 transition-colors">Yenile ve Güncelle</button>
      <button @click="close" class="px-4 py-2.5 text-sm font-bold text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors">Sonra</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useRegisterSW } from 'virtual:pwa-register/vue'

const {
  offlineReady,
  needRefresh,
  updateServiceWorker,
} = useRegisterSW({
  onRegisteredSW(_swUrl, r) {
    if (r) {
      setInterval(() => {
        r.update()
      }, 60 * 60 * 1000) // Her saat başı yeni sürüm kontrolü yap
    }
  }
})

const close = () => {
  offlineReady.value = false
  needRefresh.value = false
}
</script>
