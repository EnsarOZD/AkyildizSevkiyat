<template>
  <div class="space-y-4 pb-6">

    <!-- Banner: Sefer başlatılmadı -->
    <div
      v-if="!loading && !activeSession"
      class="rounded-xl bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-300 dark:border-yellow-700 p-4 flex items-center justify-between gap-3"
    >
      <div>
        <p class="text-sm font-semibold text-yellow-800 dark:text-yellow-200">Seferinizi başlatmayı unutmayın!</p>
        <p class="text-xs text-yellow-700 dark:text-yellow-300 mt-0.5">QR kod okutarak seferi başlatın.</p>
      </div>
      <router-link
        to="/driver/qr-scan"
        class="shrink-0 px-3 py-1.5 bg-yellow-500 hover:bg-yellow-600 text-white text-xs font-semibold rounded-lg"
      >QR Okut</router-link>
    </div>

    <!-- Banner: Tüm teslimatlar tamamlandı, seferi kapat -->
    <div
      v-if="!loading && allDelivered && activeSession"
      class="rounded-xl bg-orange-50 dark:bg-orange-900/20 border-2 border-orange-400 dark:border-orange-600 p-4 flex items-center justify-between gap-3"
    >
      <div>
        <p class="text-sm font-semibold text-orange-800 dark:text-orange-200">Tüm teslimatlar tamamlandı!</p>
        <p class="text-xs text-orange-700 dark:text-orange-300 mt-0.5">Seferi kapatmak için aracınızın yanına giderek QR kodunu okutunuz.</p>
      </div>
      <router-link
        to="/driver/qr-scan?mode=end"
        class="shrink-0 px-3 py-1.5 bg-orange-500 hover:bg-orange-600 text-white text-xs font-semibold rounded-lg"
      >QR Okut</router-link>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-16">
      <div class="w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- Error -->
    <div v-else-if="error"
         class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl p-4 text-red-700 dark:text-red-400 text-sm">
      {{ error }}
    </div>

    <template v-else-if="routeData">

      <!-- Progress bar -->
      <div class="bg-white dark:bg-[#0f2744] rounded-xl shadow-sm border border-gray-200 dark:border-white/10 p-4">
        <div class="flex items-center justify-between mb-2 text-sm">
          <span class="font-medium text-gray-900 dark:text-white">Bugünkü Rota</span>
          <span class="text-gray-500 dark:text-gray-400">
            {{ routeData.completedStops }} / {{ routeData.totalStops }} nokta tamamlandı
          </span>
        </div>
        <div class="h-2 bg-gray-100 dark:bg-white/10 rounded-full overflow-hidden">
          <div
            class="h-full bg-green-500 rounded-full transition-all duration-500"
            :style="{ width: progressPct + '%' }"
          />
        </div>
        <a
          v-if="routeData.mapsRouteUrl"
          :href="routeData.mapsRouteUrl"
          target="_blank"
          rel="noopener"
          class="mt-3 flex items-center justify-center gap-2 w-full py-2 bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40 text-blue-700 dark:text-blue-400 text-sm font-medium rounded-lg transition-colors"
        >
          <MapIcon class="w-4 h-4" aria-hidden="true" />
          Tüm Rotayı Haritada Aç
        </a>
      </div>

      <!-- Empty -->
      <div v-if="routeData.stops.length === 0" class="text-center py-16">
        <TruckIcon class="w-14 h-14 mx-auto text-gray-300 dark:text-gray-600 mb-3" />
        <p class="text-gray-500 dark:text-gray-400 font-medium">Aktif teslimat yok</p>
        <p class="text-sm text-gray-400 dark:text-gray-500 mt-1">Bugün size atanmış teslimat bulunmuyor.</p>
      </div>

      <template v-else>
        <!-- Active stops -->
        <div v-if="activeStops.length > 0">
          <h2 class="text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400 px-1 mb-2">
            Bekleyen ({{ activeStops.length }})
          </h2>
          <div
            v-for="stop in activeStops"
            :key="stop.projectId"
            @click="goToStop(stop)"
            class="bg-white dark:bg-[#0f2744] rounded-xl shadow-sm border border-gray-200 dark:border-white/10 p-4 cursor-pointer active:scale-[0.98] transition-transform"
          >
            <StopCard :stop="stop" />
          </div>
        </div>

        <!-- Completed stops -->
        <div v-if="completedStops.length > 0" class="mt-2">
          <h2 class="text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400 px-1 mb-2">
            Tamamlanan ({{ completedStops.length }})
          </h2>
          <div
            v-for="stop in completedStops"
            :key="stop.projectId"
            @click="goToStop(stop)"
            class="bg-white dark:bg-[#0f2744] rounded-xl shadow-sm border border-gray-200 dark:border-white/10 p-4 cursor-pointer opacity-60 active:scale-[0.98] transition-transform"
          >
            <StopCard :stop="stop" />
          </div>
        </div>
      </template>

    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { TruckIcon, MapIcon } from '@heroicons/vue/24/outline';
import driverService, { type DriverRouteDto, type DeliveryStopDto } from '../services/driverService';
import driverSessionService, { type ActiveSessionDto } from '../services/driverSessionService';
import StopCard from '../components/driver/StopCard.vue';

const router    = useRouter();
const routeData = ref<DriverRouteDto | null>(null);
const loading   = ref(false);
const error     = ref('');
const activeSession = ref<ActiveSessionDto | null>(null);

const activeStops    = computed(() => routeData.value?.stops.filter(s => !s.isFullyDelivered) ?? []);
const completedStops = computed(() => routeData.value?.stops.filter(s => s.isFullyDelivered) ?? []);
const progressPct    = computed(() => {
  if (!routeData.value || routeData.value.totalStops === 0) return 0;
  return Math.round((routeData.value.completedStops / routeData.value.totalStops) * 100);
});
const allDelivered = computed(() =>
  routeData.value !== null &&
  routeData.value.totalStops > 0 &&
  routeData.value.completedStops === routeData.value.totalStops
);

async function load() {
  loading.value = true;
  error.value   = '';
  try {
    [routeData.value, activeSession.value] = await Promise.all([
      driverService.getRoute(),
      driverSessionService.getActiveSession(),
    ]);
  } catch {
    error.value = 'Rota yüklenemedi. Lütfen tekrar deneyin.';
  } finally {
    loading.value = false;
  }
}

function goToStop(stop: DeliveryStopDto) {
  router.push({ name: 'DriverStop', params: { projectId: stop.projectId } });
}

onMounted(load);
</script>
