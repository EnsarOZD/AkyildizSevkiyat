<template>
  <div class="dark min-h-screen bg-[#0a1626] text-white flex flex-col" style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">
    <!-- Header -->
    <header class="bg-[#0f2038] border-b border-white/[0.06] sticky top-0 z-10">
      <div class="flex items-center justify-between px-4 py-3 max-w-2xl mx-auto w-full">
        <div class="flex items-center gap-3 min-w-0">
          <button
            v-if="showBack"
            @click="router.back()"
            aria-label="Geri dön"
            class="w-9 h-9 rounded-xl flex items-center justify-center bg-white/[0.07] text-white"
          >
            <ChevronLeftIcon class="w-5 h-5" aria-hidden="true" />
          </button>
          <div v-else class="w-9 h-9 rounded-xl bg-white/[0.08] flex items-center justify-center overflow-hidden flex-shrink-0">
            <img :src="logoUrl" alt="Akyıldız" class="w-7 h-7 object-contain" />
          </div>
          <div class="min-w-0">
            <p class="text-[11px] text-white/45 leading-none mb-1 truncate">
              Şoför · {{ userName }}
            </p>
            <h1 class="text-base font-bold text-white leading-tight truncate">
              {{ pageTitle }}
            </h1>
          </div>
        </div>
        <!-- Right: online/offline status -->
        <div class="flex items-center gap-2 flex-shrink-0">
          <template v-if="!isOnline">
            <span class="flex items-center gap-1.5 text-xs text-amber-400 font-semibold">
              <SignalSlashIcon class="w-4 h-4" />
              Çevrimdışı
            </span>
          </template>
          <template v-else-if="queue.length > 0">
            <button @click="flushQueue" :disabled="flushing"
              class="flex items-center gap-1.5 text-xs text-blue-300 font-semibold disabled:opacity-60">
              <span class="w-4 h-4 border-2 border-blue-400 border-t-transparent rounded-full"
                :class="flushing ? 'animate-spin' : ''"></span>
              {{ queue.length }} bekliyor
            </button>
          </template>
          <template v-else>
            <span class="flex items-center gap-1.5 text-xs text-emerald-400 font-semibold">
              <span class="w-2 h-2 rounded-full bg-emerald-400 animate-pulse" style="box-shadow:0 0 8px #4ade80"></span>
              Aktif
            </span>
          </template>
        </div>
      </div>
    </header>

    <!-- Offline banner -->
    <div v-if="!isOnline"
      class="bg-amber-500 text-[#0a1626] text-xs text-center py-1.5 px-4 font-bold">
      Çevrimdışısınız — teslimatlar kaydedilecek ve bağlantı gelince gönderilecek
    </div>

    <!-- Location permission denied banner -->
    <div
      v-if="locationDenied"
      class="bg-red-500 text-white text-xs py-2 px-4 flex items-center justify-between gap-2 font-medium"
    >
      <span>Konum izni gerekli — QR sefer işlemleri çalışmaz.</span>
      <router-link
        to="/driver/settings"
        class="flex-shrink-0 font-bold underline"
        @click.native="locationDenied = false"
      >Ayarlar</router-link>
    </div>

    <!-- Main content — extra bottom padding for nav bar -->
    <main class="flex-1 p-4 pb-28 max-w-2xl mx-auto w-full">
      <router-view :key="$route.fullPath" />
    </main>

    <!-- Bottom navigation -->
    <nav
      class="fixed bottom-0 inset-x-0 z-30 bg-[#0f2038] border-t border-white/[0.07]"
      :style="{ paddingBottom: 'env(safe-area-inset-bottom, 0px)' }"
    >
      <div class="flex h-16 max-w-2xl mx-auto relative">

        <!-- Ana -->
        <router-link
          to="/driver"
          class="flex-1 flex flex-col items-center justify-center gap-1 transition-colors"
          :class="isTab('DriverShipments') && route.query.view !== 'done' ? 'text-blue-300' : 'text-white/40'"
        >
          <span class="flex items-center justify-center transition-all"
            :class="isTab('DriverShipments') && route.query.view !== 'done' ? 'w-10 h-7 rounded-full bg-blue-500/18' : ''">
            <HomeIcon class="w-5 h-5" />
          </span>
          <span class="text-[10px] font-semibold">Ana</span>
        </router-link>

        <!-- Rota — Google Maps çoklu durak URL'ini açar (onaylı) -->
        <button
          v-if="driverRouteStore.mapsRouteUrl"
          @click="openMaps(driverRouteStore.mapsRouteUrl)"
          class="flex-1 flex flex-col items-center justify-center gap-1 text-white/40 transition-colors"
        >
          <MapIcon class="w-5 h-5" />
          <span class="text-[10px] font-semibold">Rota</span>
        </button>
        <router-link
          v-else
          to="/driver"
          class="flex-1 flex flex-col items-center justify-center gap-1 text-white/40 transition-colors"
        >
          <MapIcon class="w-5 h-5" />
          <span class="text-[10px] font-semibold">Rota</span>
        </router-link>

        <!-- Center QR button — yalnızca sefer başında (başlat) ve sonunda (kapat) aktif -->
        <div class="flex-1 flex items-center justify-center relative">
          <router-link
            v-if="qrActive"
            :to="qrTarget"
            class="absolute -top-5 w-14 h-14 rounded-full flex items-center justify-center shadow-lg transition-colors border-4 border-[#0a1626]"
            :class="driverRouteStore.canEndSession
              ? 'bg-gradient-to-br from-emerald-500 to-green-600 shadow-emerald-600/40'
              : 'bg-gradient-to-br from-blue-500 to-blue-600 shadow-blue-600/40'"
            :aria-label="driverRouteStore.canEndSession ? 'Seferi Kapat (QR)' : 'Sefer Başlat (QR)'"
          >
            <QrCodeIcon class="w-6 h-6 text-white" />
          </router-link>
          <div
            v-else
            class="absolute -top-5 w-14 h-14 rounded-full bg-white/10 flex items-center justify-center shadow-lg opacity-50 cursor-not-allowed border-4 border-[#0a1626]"
            aria-label="Sefer sürüyor — teslimatlar tamamlanınca seferi kapatabilirsiniz"
            title="Sefer sürüyor — teslimatlar tamamlanınca seferi kapatabilirsiniz"
          >
            <QrCodeIcon class="w-6 h-6 text-white/70" />
          </div>
        </div>

        <!-- Tamamlanan -->
        <router-link
          :to="{ name: 'DriverShipments', query: { view: 'done' } }"
          class="flex-1 flex flex-col items-center justify-center gap-1 transition-colors"
          :class="route.query.view === 'done' ? 'text-blue-300' : 'text-white/40'"
        >
          <span class="flex items-center justify-center transition-all"
            :class="route.query.view === 'done' ? 'w-10 h-7 rounded-full bg-blue-500/18' : ''">
            <CheckCircleIcon class="w-5 h-5" />
          </span>
          <span class="text-[10px] font-semibold">Tamamlanan</span>
        </router-link>

        <!-- Diğer -->
        <button
          @click="menuOpen = true"
          class="flex-1 flex flex-col items-center justify-center gap-1 text-white/40 transition-colors"
        >
          <EllipsisHorizontalIcon class="w-5 h-5" />
          <span class="text-[10px] font-semibold">Diğer</span>
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
          class="fixed inset-0 z-50 bg-black/60"
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
              class="absolute bottom-0 inset-x-0 bg-[#0f2038] rounded-t-[28px] border-t border-white/[0.08] shadow-[0_-20px_50px_rgba(0,0,0,0.5)]"
              :style="{ paddingBottom: 'max(1.5rem, env(safe-area-inset-bottom))' }"
              @click.stop
            >
              <!-- Handle -->
              <div class="flex justify-center pt-3 pb-4">
                <div class="w-10 h-1.5 bg-white/20 rounded-full"></div>
              </div>

              <!-- User info -->
              <div class="px-5 pb-4 border-b border-white/[0.07] flex items-center gap-3">
                <div class="w-12 h-12 rounded-full bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center flex-shrink-0">
                  <span class="text-base font-extrabold text-white">{{ userInitial }}</span>
                </div>
                <div class="flex-1 min-w-0">
                  <p class="font-bold text-white text-[15px] leading-tight truncate">{{ userName }}</p>
                  <p class="text-xs text-white/45 truncate">{{ authStore.userEmail }}</p>
                </div>
                <span class="px-2.5 py-1 rounded-full text-[10px] font-bold bg-emerald-500/16 text-emerald-300 flex-shrink-0">Şoför</span>
              </div>

              <!-- Actions -->
              <div class="px-3 py-3 space-y-1">
                <button
                  @click="router.push('/driver/qr-scan?mode=end'); menuOpen = false"
                  class="w-full flex items-center gap-3 px-4 py-3.5 rounded-2xl text-white font-semibold hover:bg-white/[0.05] transition-colors text-sm"
                >
                  <span class="w-9 h-9 rounded-xl bg-white/[0.07] text-white/70 flex items-center justify-center"><QrCodeIcon class="w-5 h-5" /></span>
                  Seferi Kapat (QR)
                </button>
                <button
                  @click="router.push('/driver/settings'); menuOpen = false"
                  class="w-full flex items-center gap-3 px-4 py-3.5 rounded-2xl text-white font-semibold hover:bg-white/[0.05] transition-colors text-sm"
                >
                  <span class="w-9 h-9 rounded-xl bg-white/[0.07] text-white/70 flex items-center justify-center"><ShieldCheckIcon class="w-5 h-5" /></span>
                  İzinler & Ayarlar
                </button>
                <button
                  @click="handleLogout"
                  class="w-full flex items-center gap-3 px-4 py-3.5 rounded-2xl text-red-400 font-semibold bg-red-500/[0.08] hover:bg-red-500/[0.14] transition-colors text-sm"
                >
                  <span class="w-9 h-9 rounded-xl bg-red-500/14 text-red-400 flex items-center justify-center"><ArrowRightOnRectangleIcon class="w-5 h-5" /></span>
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
  CheckCircleIcon,
  EllipsisHorizontalIcon,
  SignalSlashIcon,
  ShieldCheckIcon,
} from '@heroicons/vue/24/outline';
import { useAuthStore } from '../stores/auth';
import { useDeliveryQueue } from '../composables/useDeliveryQueue';
import { useDriverRouteStore } from '../stores/driverRoute';
import { useOpenMaps } from '../composables/useOpenMaps';
import logoUrl from '../assets/logo.png';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const { isOnline, queue, flushing, flushQueue } = useDeliveryQueue();
const driverRouteStore = useDriverRouteStore();
const { openMaps } = useOpenMaps();
const menuOpen = ref(false);
const locationDenied = ref(false);

// QR butonu yalnızca sefer başında (başlat) ve sonunda (kapat) aktif olur;
// sefer sürerken pasif kalır (teslimat sırasında işe yaramaz).
const qrActive = computed(() => !driverRouteStore.hasActiveSession || driverRouteStore.canEndSession);
const qrTarget = computed(() => driverRouteStore.canEndSession ? '/driver/qr-scan?mode=end' : '/driver/qr-scan');

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
