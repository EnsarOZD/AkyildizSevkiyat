<template>
  <div v-if="!loading && sessions.length === 0" />
  <div v-else class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
    <div class="flex items-center justify-between mb-4">
      <div>
        <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">Aktif Seferler</h2>
        <p class="text-xs text-gray-400 mt-0.5">Yolda olan şoförler ve teslimat ilerlemesi</p>
      </div>
      <router-link to="/admin/active-operations" class="text-xs text-blue-600 hover:underline">Operasyonlar</router-link>
    </div>

    <div v-if="loading && sessions.length === 0" class="flex justify-center py-8">
      <div class="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-3">
      <div
        v-for="s in sessions"
        :key="s.sessionId"
        class="border border-gray-200 dark:border-gray-700 rounded-lg p-4"
      >
        <div class="flex items-center justify-between gap-2 mb-2">
          <div class="min-w-0">
            <p class="text-sm font-semibold text-gray-800 dark:text-gray-200 truncate">{{ s.driverFullName }}</p>
            <p class="text-xs text-gray-400 font-mono">{{ s.plateNumber }}</p>
          </div>
          <button
            @click="openMap(s)"
            :disabled="!hasCoords(s)"
            class="shrink-0 flex items-center gap-1 text-xs px-2.5 py-1.5 rounded-lg border border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 disabled:opacity-40 disabled:cursor-not-allowed"
            :title="hasCoords(s) ? 'Haritada gör' : 'Koordinat bilgisi yok'"
          >
            <MapPinIcon class="w-4 h-4" /> Harita
          </button>
        </div>

        <div class="flex items-baseline justify-between text-xs mb-1">
          <span class="text-gray-500 dark:text-gray-400">
            {{ s.deliveredProjects }}/{{ s.totalProjects }} proje teslim
          </span>
          <span class="text-gray-400">{{ s.totalProjects - s.deliveredProjects }} kaldı</span>
        </div>
        <div class="h-2 rounded-full bg-gray-100 dark:bg-gray-800 overflow-hidden">
          <div
            class="h-full bg-green-500 transition-all duration-500"
            :style="{ width: pct(s) + '%' }"
          />
        </div>
      </div>
    </div>

    <!-- Harita modalı -->
    <Teleport to="body">
      <div v-if="mapSession" class="fixed inset-0 z-50 flex items-center justify-center p-4">
        <div class="absolute inset-0 bg-black/60" @click="mapSession = null"></div>
        <div class="relative w-full max-w-3xl bg-white dark:bg-gray-900 rounded-xl shadow-xl overflow-hidden">
          <div class="flex items-center justify-between px-4 py-3 border-b border-gray-200 dark:border-gray-700">
            <div>
              <h3 class="text-sm font-semibold text-gray-800 dark:text-gray-200">{{ mapSession.driverFullName }} · {{ mapSession.plateNumber }}</h3>
              <p class="text-xs text-gray-400">{{ mapSession.deliveredProjects }}/{{ mapSession.totalProjects }} proje teslim</p>
            </div>
            <button @click="mapSession = null" class="p-1.5 rounded-lg text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800">
              <XMarkIcon class="w-5 h-5" />
            </button>
          </div>
          <div class="h-[60vh]">
            <RouteMapView :stops="mapStops" class="w-full h-full" />
          </div>
          <!-- Koordinatı olmayan noktalar haritada gösterilemez -->
          <div v-if="noCoordStops.length" class="px-4 py-3 border-t border-gray-200 dark:border-gray-700 bg-amber-50 dark:bg-amber-900/10">
            <p class="text-xs font-medium text-amber-700 dark:text-amber-400 mb-1">
              {{ noCoordStops.length }} nokta koordinatsız — haritada gösterilemiyor:
            </p>
            <div class="flex flex-wrap gap-1.5">
              <span v-for="st in noCoordStops" :key="st.projectId"
                class="text-[11px] px-2 py-0.5 rounded bg-white dark:bg-gray-800 border border-amber-200 dark:border-amber-800 text-gray-700 dark:text-gray-300"
                :class="{ 'line-through opacity-60': st.isDelivered }">
                {{ st.projectName }}
              </span>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { MapPinIcon, XMarkIcon } from '@heroicons/vue/24/outline';
import { type ActiveSessionWithShipmentsDto } from '../../services/adminService';
import { dashboardService } from '../../services/dashboardService';
import RouteMapView from '../driver/RouteMapView.vue';
import type { DeliveryStopDto } from '../../services/driverService';

const sessions = ref<ActiveSessionWithShipmentsDto[]>([]);
const loading = ref(false);
const mapSession = ref<ActiveSessionWithShipmentsDto | null>(null);

let refreshTimer: ReturnType<typeof setInterval> | null = null;

const pct = (s: ActiveSessionWithShipmentsDto) =>
  s.totalProjects === 0 ? 0 : Math.round((s.deliveredProjects / s.totalProjects) * 100);

const hasCoords = (s: ActiveSessionWithShipmentsDto) =>
  s.stops.some(st => st.latitude != null && st.longitude != null);

// Açık haritadaki, koordinatı olmadığı için çizilemeyen noktalar
const noCoordStops = computed(() =>
  mapSession.value ? mapSession.value.stops.filter(st => st.latitude == null || st.longitude == null) : []
);

// ActiveSessionStopDto → RouteMapView'in beklediği DeliveryStopDto şekline eşle
const mapStops = computed<DeliveryStopDto[]>(() => {
  if (!mapSession.value) return [];
  return mapSession.value.stops.map((st, idx) => ({
    stopNumber: idx + 1,
    projectId: st.projectId,
    projectName: st.projectName,
    projectAddress: st.projectAddress,
    projectLatitude: st.latitude,
    projectLongitude: st.longitude,
    isFullyDelivered: st.isDelivered,
  })) as unknown as DeliveryStopDto[];
});

function openMap(s: ActiveSessionWithShipmentsDto) {
  if (!hasCoords(s)) return;
  mapSession.value = s;
}

async function load() {
  loading.value = true;
  try {
    sessions.value = await dashboardService.getActiveTrips();
  } catch {
    sessions.value = [];
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  load();
  refreshTimer = setInterval(load, 60000); // 60 sn'de bir tazele
});
onUnmounted(() => { if (refreshTimer) clearInterval(refreshTimer); });
</script>
