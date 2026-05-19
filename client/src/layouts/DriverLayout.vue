<template>
  <div class="min-h-screen bg-gray-50 dark:bg-[#0c1a2e] flex flex-col">
    <!-- Header -->
    <header class="bg-white dark:bg-[#0f2744] shadow-sm sticky top-0 z-10">
      <div class="flex items-center justify-between px-4 py-3 max-w-2xl mx-auto w-full">
        <div class="flex items-center gap-3">
          <button
            v-if="showBack"
            @click="router.back()"
            aria-label="Geri dön"
            class="p-1.5 rounded-lg text-gray-500 hover:bg-gray-100 dark:hover:bg-white/10"
          >
            <ChevronLeftIcon class="w-5 h-5" aria-hidden="true" />
          </button>
          <div>
            <p class="text-xs text-gray-400 dark:text-gray-500 leading-none mb-0.5">
              Şoför · {{ userName }}
            </p>
            <h1 class="text-base font-bold text-gray-900 dark:text-white leading-tight">
              {{ pageTitle }}
            </h1>
          </div>
        </div>
        <!-- Right: online/offline status -->
        <div class="flex items-center gap-2">
          <template v-if="!isOnline">
            <span class="flex items-center gap-1.5 text-xs text-amber-500 dark:text-amber-400 font-medium">
              <SignalSlashIcon class="w-4 h-4" />
              Çevrimdışı
            </span>
          </template>
          <template v-else-if="queue.length > 0">
            <button @click="flushQueue" :disabled="flushing"
              class="flex items-center gap-1.5 text-xs text-blue-600 dark:text-blue-400 font-medium disabled:opacity-60">
              <span class="w-4 h-4 border-2 border-blue-500 border-t-transparent rounded-full"
                :class="flushing ? 'animate-spin' : ''"></span>
              {{ queue.length }} bekliyor
            </button>
          </template>
          <template v-else>
            <span class="flex items-center gap-1.5 text-xs text-gray-400">
              <span class="w-2 h-2 rounded-full bg-green-400 animate-pulse"></span>
              Aktif
            </span>
          </template>
        </div>
      </div>
    </header>

    <!-- Offline banner -->
    <div v-if="!isOnline"
      class="bg-amber-500 text-white text-xs text-center py-1.5 px-4 font-medium">
      Çevrimdışısınız — teslimatlar kaydedilecek ve bağlantı gelince gönderilecek
    </div>

    <!-- Location permission denied banner -->
    <div
      v-if="locationDenied"
      class="bg-red-500 text-white text-xs py-2 px-4 flex items-center justify-between gap-2"
    >
      <span>Konum izni gerekli — QR sefer işlemleri çalışmaz.</span>
      <router-link
        to="/driver/settings"
        class="flex-shrink-0 font-semibold underline"
        @click.native="locationDenied = false"
      >Ayarlar</router-link>
    </div>

    <!-- Main content — extra bottom padding for nav bar -->
    <main class="flex-1 p-4 pb-24 max-w-2xl mx-auto w-full">
      <router-view :key="$route.fullPath" />
    </main>

    <!-- Bottom navigation -->
    <nav
      class="fixed bottom-0 inset-x-0 z-30 bg-white dark:bg-[#0f2744] border-t border-gray-200 dark:border-gray-700"
      :style="{ paddingBottom: 'env(safe-area-inset-bottom, 0px)' }"
    >
      <div class="flex h-16 max-w-2xl mx-auto relative">

        <!-- Ana -->
        <router-link
          to="/driver"
          class="flex-1 flex flex-col items-center justify-center gap-0.5 transition-colors"
          :class="isTab('DriverShipments') ? 'text-blue-600 dark:text-blue-400' : 'text-gray-400 dark:text-gray-500'"
        >
          <HomeIcon class="w-5 h-5" />
          <span class="text-[10px] font-medium">Ana</span>
        </router-link>

        <!-- Rota — Google Maps çoklu durak URL'ini açar -->
        <a
          v-if="driverRouteStore.mapsRouteUrl"
          :href="driverRouteStore.mapsRouteUrl"
          target="_blank"
          rel="noopener"
          class="flex-1 flex flex-col items-center justify-center gap-0.5 text-gray-400 dark:text-gray-500 transition-colors"
        >
          <MapIcon class="w-5 h-5" />
          <span class="text-[10px] font-medium">Rota</span>
        </a>
        <router-link
          v-else
          to="/driver"
          class="flex-1 flex flex-col items-center justify-center gap-0.5 transition-colors"
          :class="isTab('DriverShipments') ? 'text-blue-600 dark:text-blue-400' : 'text-gray-400 dark:text-gray-500'"
        >
          <MapIcon class="w-5 h-5" />
          <span class="text-[10px] font-medium">Rota</span>
        </router-link>

        <!-- Center QR button -->
        <div class="flex-1 flex items-center justify-center relative">
          <router-link
            to="/driver/qr-scan"
            class="absolute -top-5 w-14 h-14 rounded-full bg-blue-600 hover:bg-blue-700 active:bg-blue-800 flex items-center justify-center shadow-lg shadow-blue-600/30 transition-colors"
            aria-label="QR Tarat"
          >
            <QrCodeIcon class="w-6 h-6 text-white" />
          </router-link>
        </div>

        <!-- Sevkiyat -->
        <router-link
          to="/driver"
          class="flex-1 flex flex-col items-center justify-center gap-0.5 text-gray-400 dark:text-gray-500 transition-colors"
        >
          <ClipboardDocumentListIcon class="w-5 h-5" />
          <span class="text-[10px] font-medium">Sevkiyat</span>
        </router-link>

        <!-- Diğer -->
        <button
          @click="menuOpen = true"
          class="flex-1 flex flex-col items-center justify-center gap-0.5 text-gray-400 dark:text-gray-500 transition-colors"
        >
          <EllipsisHorizontalIcon class="w-5 h-5" />
          <span class="text-[10px] font-medium">Diğer</span>
        </button>
      </div>
    </nav>

    <!-- "Diğer" bottom sheet -->
    <Teleport to="body">
      <Transition
        enter-active-class="transition ease-out duration-200"
        enter-from-class="opacity-0"
        enter-to-class="opacity-100"
        leave-active-class="transition ease-in duration-150"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0"
      >
        <div
          v-if="menuOpen"
          class="fixed inset-0 z-50 bg-black/40"
          @click="menuOpen = false"
        >
          <Transition
            enter-active-class="transition ease-out duration-250"
            enter-from-class="translate-y-full"
            enter-to-class="translate-y-0"
            leave-active-class="transition ease-in duration-200"
            leave-from-class="translate-y-0"
            leave-to-class="translate-y-full"
          >
            <div
              v-if="menuOpen"
              class="absolute bottom-0 inset-x-0 bg-white dark:bg-[#0f2744] rounded-t-2xl pb-safe"
              :style="{ paddingBottom: 'max(1.5rem, env(safe-area-inset-bottom))' }"
              @click.stop
            >
              <!-- Handle -->
              <div class="flex justify-center pt-3 pb-4">
                <div class="w-10 h-1 bg-gray-300 dark:bg-gray-600 rounded-full"></div>
              </div>

              <!-- User info -->
              <div class="px-5 pb-4 border-b border-gray-100 dark:border-gray-700 flex items-center gap-3">
                <div class="w-10 h-10 rounded-full bg-blue-100 dark:bg-blue-900/40 flex items-center justify-center flex-shrink-0">
                  <span class="text-sm font-bold text-blue-700 dark:text-blue-300">{{ userInitial }}</span>
                </div>
                <div>
                  <p class="font-semibold text-gray-900 dark:text-white text-sm">{{ userName }}</p>
                  <p class="text-xs text-gray-400">{{ authStore.userEmail }}</p>
                </div>
              </div>

              <!-- Actions -->
              <div class="px-3 py-3 space-y-1">
                <button
                  @click="router.push('/driver/qr-scan?mode=end'); menuOpen = false"
                  class="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-white/5 transition-colors text-sm"
                >
                  <QrCodeIcon class="w-5 h-5 text-gray-400" />
                  Seferi Kapat (QR)
                </button>
                <button
                  @click="router.push('/driver/settings'); menuOpen = false"
                  class="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-white/5 transition-colors text-sm"
                >
                  <ShieldCheckIcon class="w-5 h-5 text-gray-400" />
                  İzinler & Ayarlar
                </button>
                <button
                  @click="handleLogout"
                  class="w-full flex items-center gap-3 px-4 py-3 rounded-xl text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors text-sm"
                >
                  <ArrowRightOnRectangleIcon class="w-5 h-5" />
                  Çıkış Yap
                </button>
              </div>
            </div>
          </Transition>
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  ChevronLeftIcon,
  ArrowRightOnRectangleIcon,
  HomeIcon,
  MapIcon,
  QrCodeIcon,
  ClipboardDocumentListIcon,
  EllipsisHorizontalIcon,
  SignalSlashIcon,
  ShieldCheckIcon,
} from '@heroicons/vue/24/outline';
import { useAuthStore } from '../stores/auth';
import { useDeliveryQueue } from '../composables/useDeliveryQueue';
import { useDriverRouteStore } from '../stores/driverRoute';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const { isOnline, queue, flushing, flushQueue } = useDeliveryQueue();
const driverRouteStore = useDriverRouteStore();
const menuOpen = ref(false);
const locationDenied = ref(false);

onMounted(async () => {
  if (!navigator.geolocation) return;
  try {
    const result = await navigator.permissions.query({ name: 'geolocation' });
    if (result.state === 'denied') {
      locationDenied.value = true;
    } else if (result.state === 'prompt') {
      // Uygulama açılışında izin diyaloğunu tetikle
      navigator.geolocation.getCurrentPosition(
        () => { locationDenied.value = false; },
        () => { locationDenied.value = true; }
      );
    }
    result.addEventListener('change', () => {
      locationDenied.value = result.state === 'denied';
    });
  } catch {
    // permissions API yoksa sessizce geç
  }
});

const pageTitle = computed(() => (route.meta.title as string) || 'Bugünün Rotası');
const showBack = computed(() => route.name !== 'DriverShipments');
const userName = computed(() => authStore.userName);
const userInitial = computed(() => authStore.userInitial);

function isTab(name: string): boolean {
  return route.name === name;
}

function handleLogout() {
  menuOpen.value = false;
  authStore.logout();
}
</script>
