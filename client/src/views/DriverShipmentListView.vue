<template>
  <div class="space-y-3 pb-2">

    <!-- ── Banners ─────────────────────────────────────────────────── -->
    <div
      v-if="!loading && !activeSession"
      class="rounded-2xl bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 p-4 flex items-center justify-between gap-3"
    >
      <div>
        <p class="text-sm font-semibold text-amber-800 dark:text-amber-200">Seferinizi başlatmayı unutmayın!</p>
        <p class="text-xs text-amber-600 dark:text-amber-300 mt-0.5">QR kod okutarak seferi başlatın.</p>
      </div>
      <router-link
        to="/driver/qr-scan"
        class="shrink-0 px-3 py-1.5 bg-amber-500 hover:bg-amber-600 text-white text-xs font-semibold rounded-xl"
      >QR Okut</router-link>
    </div>

    <div
      v-if="!loading && canEndSession && activeSession"
      class="rounded-2xl bg-green-50 dark:bg-green-900/20 border-2 border-green-400 dark:border-green-600 p-4 flex items-center justify-between gap-3"
    >
      <div>
        <p class="text-sm font-semibold text-green-800 dark:text-green-200">Tüm teslimatlar tamamlandı!</p>
        <p class="text-xs text-green-600 dark:text-green-300 mt-0.5">Aracınıza giderek seferi kapatın.</p>
      </div>
      <router-link
        to="/driver/qr-scan?mode=end"
        class="shrink-0 px-3 py-1.5 bg-green-500 hover:bg-green-600 text-white text-xs font-semibold rounded-xl"
      >QR Okut</router-link>
    </div>

    <!-- ── Loading ────────────────────────────────────────────────── -->
    <div v-if="loading" class="flex justify-center py-20">
      <div class="w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- ── Error ──────────────────────────────────────────────────── -->
    <div
      v-else-if="error"
      class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-700 rounded-2xl p-4 text-red-700 dark:text-red-400 text-sm"
    >
      {{ error }}
    </div>

    <template v-else-if="routeData">

      <!-- ── Stats bar ───────────────────────────────────────────── -->
      <div class="bg-white dark:bg-[#0f2744] rounded-2xl shadow-sm border border-gray-100 dark:border-white/10 overflow-hidden">
        <div class="grid grid-cols-3 divide-x divide-gray-100 dark:divide-white/10">
          <div class="px-3 py-3 text-center">
            <p class="text-[10px] font-bold uppercase tracking-wide text-gray-400 dark:text-gray-500 mb-1">Tamamlanan</p>
            <p class="text-xl font-bold text-blue-600 dark:text-blue-400 leading-none">
              {{ routeData.completedStops }}
              <span class="text-sm font-medium text-gray-400">/{{ routeData.totalStops }}</span>
            </p>
          </div>
          <div class="px-3 py-3 text-center">
            <p class="text-[10px] font-bold uppercase tracking-wide text-gray-400 dark:text-gray-500 mb-1">İrsaliye</p>
            <p class="text-xl font-bold text-gray-700 dark:text-gray-200 leading-none">
              {{ routeData.completedShipments }}
              <span class="text-sm font-medium text-gray-400">/{{ routeData.totalShipments }}</span>
            </p>
          </div>
          <div class="px-3 py-3 text-center bg-amber-50 dark:bg-amber-900/20">
            <p class="text-[10px] font-bold uppercase tracking-wide text-amber-600 dark:text-amber-400 mb-1">Rota</p>
            <a
              v-if="routeData.mapsRouteUrl"
              :href="routeData.mapsRouteUrl"
              target="_blank"
              rel="noopener"
              class="text-sm font-bold text-amber-700 dark:text-amber-400 flex items-center justify-center gap-1"
            >
              <MapIcon class="w-4 h-4" />
              Aç
            </a>
            <p v-else class="text-sm font-medium text-gray-300 dark:text-gray-600">—</p>
          </div>
        </div>
        <div class="h-1.5 bg-gray-100 dark:bg-white/10">
          <div
            class="h-full bg-blue-500 transition-all duration-700"
            :style="{ width: progressPct + '%' }"
          />
        </div>
      </div>

      <!-- ── Tab switcher ────────────────────────────────────────── -->
      <div class="bg-white dark:bg-[#0f2744] rounded-2xl shadow-sm border border-gray-100 dark:border-white/10 p-1 flex gap-1">
        <button
          v-for="tab in tabs"
          :key="tab.key"
          @click="activeTab = tab.key"
          class="flex-1 py-2 rounded-xl text-sm font-semibold transition-colors"
          :class="activeTab === tab.key
            ? 'bg-blue-600 text-white shadow-sm'
            : 'text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-white/5'"
        >
          <span class="flex items-center justify-center gap-1.5">
            <component :is="tab.icon" class="w-4 h-4" />
            {{ tab.label }}
          </span>
        </button>
      </div>

      <!-- ═══════════════════════════════════════════════════════════
           TAB: LİSTE
           ══════════════════════════════════════════════════════════ -->
      <template v-if="activeTab === 'list'">

        <!-- Embedded map for active stop -->
        <div
          v-if="activeStop && (activeStop.projectLatitude || activeStop.projectAddress)"
          class="bg-white dark:bg-[#0f2744] rounded-2xl shadow-sm border border-gray-100 dark:border-white/10 overflow-hidden"
        >
          <button
            @click="mapExpanded = !mapExpanded"
            class="w-full flex items-center justify-between px-4 py-3 text-sm font-medium text-gray-700 dark:text-gray-200 hover:bg-gray-50 dark:hover:bg-white/5 transition-colors"
          >
            <div class="flex items-center gap-2">
              <MapPinIcon class="w-4 h-4 text-blue-500" />
              <span>{{ activeStop.projectName }} — Harita</span>
            </div>
            <ChevronDownIcon
              class="w-4 h-4 text-gray-400 transition-transform duration-200"
              :class="mapExpanded ? 'rotate-180' : ''"
            />
          </button>
          <Transition
            enter-active-class="transition-all duration-300 ease-out overflow-hidden"
            enter-from-class="max-h-0 opacity-0"
            enter-to-class="max-h-64 opacity-100"
            leave-active-class="transition-all duration-200 ease-in overflow-hidden"
            leave-from-class="max-h-64 opacity-100"
            leave-to-class="max-h-0 opacity-0"
          >
            <div v-if="mapExpanded" class="h-52 relative">
              <iframe
                :src="mapEmbedUrl"
                class="w-full h-full border-0"
                loading="lazy"
                referrerpolicy="no-referrer-when-downgrade"
                title="Teslimat noktası haritası"
              ></iframe>
            </div>
          </Transition>
        </div>

        <!-- Empty state -->
        <div v-if="routeData.stops.length === 0" class="text-center py-20">
          <TruckIcon class="w-14 h-14 mx-auto text-gray-200 dark:text-gray-600 mb-3" />
          <p class="text-gray-500 dark:text-gray-400 font-medium">Aktif teslimat yok</p>
          <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">Bugün size atanmış teslimat bulunmuyor.</p>
        </div>

        <!-- Reorder mode toolbar -->
        <div
          v-if="routeData.stops.length > 1 && routeData.zonePreparationId"
          class="flex items-center justify-between gap-2"
        >
          <template v-if="!reorderMode">
            <button
              @click="enterReorderMode"
              class="flex items-center gap-1.5 px-3 py-2 rounded-xl border border-gray-200 dark:border-white/10 text-xs font-medium text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-white/5 transition-colors"
            >
              <Bars3Icon class="w-4 h-4" />
              Sırayı Düzenle
            </button>
          </template>
          <template v-else>
            <button
              @click="cancelReorder"
              class="flex-1 py-2 rounded-xl border border-gray-200 dark:border-white/10 text-xs font-medium text-gray-600 dark:text-gray-300"
            >Vazgeç</button>
            <button
              @click="saveReorder"
              :disabled="savingReorder"
              class="flex-1 py-2 rounded-xl bg-blue-600 text-white text-xs font-semibold disabled:opacity-50"
            >
              {{ savingReorder ? 'Kaydediliyor…' : 'Kaydet' }}
            </button>
          </template>
        </div>

        <!-- Stop list -->
        <div v-if="routeData.stops.length > 0" class="space-y-2">
          <div
            v-for="(stop, idx) in reorderMode ? reorderList : sortedStops"
            :key="stop.projectId"
            @click="!isActiveStop(stop) && !reorderMode && goToStop(stop)"
            class="rounded-2xl border transition-all duration-150"
            :class="[stopCardClass(stop), reorderMode ? 'cursor-default' : '']"
          >
            <div class="p-4">
              <!-- Reorder controls -->
              <div v-if="reorderMode" class="flex items-start gap-3">
                <div class="flex flex-col gap-0.5 shrink-0 pt-0.5">
                  <button
                    @click.stop="moveStop(idx, -1)"
                    :disabled="idx === 0"
                    class="w-7 h-7 flex items-center justify-center rounded-lg bg-gray-100 dark:bg-white/10 disabled:opacity-30 text-gray-600 dark:text-gray-300"
                  ><ChevronUpIcon class="w-4 h-4" /></button>
                  <button
                    @click.stop="moveStop(idx, 1)"
                    :disabled="idx === reorderList.length - 1"
                    class="w-7 h-7 flex items-center justify-center rounded-lg bg-gray-100 dark:bg-white/10 disabled:opacity-30 text-gray-600 dark:text-gray-300"
                  ><ChevronDownIcon class="w-4 h-4" /></button>
                </div>
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2 mb-1">
                    <span class="text-xs font-bold text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/30 rounded-full w-6 h-6 flex items-center justify-center">{{ idx + 1 }}</span>
                    <span class="font-semibold text-sm text-gray-800 dark:text-gray-100 truncate">{{ stop.projectName }}</span>
                  </div>
                  <p v-if="stop.projectAddress" class="text-xs text-gray-400 dark:text-gray-500 truncate">{{ stop.projectAddress }}</p>
                </div>
              </div>

              <!-- Normal stop card -->
              <StopCard v-else :stop="stop" :is-active="isActiveStop(stop)" />
            </div>
          </div>
        </div>
      </template>

      <!-- ═══════════════════════════════════════════════════════════
           TAB: HARİTA
           ══════════════════════════════════════════════════════════ -->
      <template v-if="activeTab === 'map'">
        <div
          class="bg-white dark:bg-[#0f2744] rounded-2xl shadow-sm border border-gray-100 dark:border-white/10 overflow-hidden"
          :style="{ height: mapTabHeight }"
        >
          <div v-if="stopsWithCoords.length === 0" class="flex flex-col items-center justify-center h-full text-gray-400 gap-2">
            <MapIcon class="w-10 h-10" />
            <p class="text-sm">Koordinat bilgisi olmayan duraklar haritada gösterilemez.</p>
          </div>
          <RouteMapView
            v-else
            :stops="sortedStops"
            :active-project-id="activeStop?.projectId"
            class="w-full h-full"
          />
        </div>
      </template>

    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import {
  TruckIcon,
  MapIcon,
  MapPinIcon,
  ChevronDownIcon,
  ChevronUpIcon,
  Bars3Icon,
  ListBulletIcon,
} from '@heroicons/vue/24/outline';
import driverService, { type DriverRouteDto, type DeliveryStopDto } from '../services/driverService';
import driverSessionService, { type ActiveSessionDto } from '../services/driverSessionService';
import StopCard from '../components/driver/StopCard.vue';
import RouteMapView from '../components/driver/RouteMapView.vue';
import { useDriverRouteStore } from '../stores/driverRoute';
import { useNotificationStore } from '../stores/notification';

const router = useRouter();
const driverRouteStore = useDriverRouteStore();
const notif = useNotificationStore();
const routeData = ref<DriverRouteDto | null>(null);
const loading = ref(false);
const error = ref('');
const activeSession = ref<ActiveSessionDto | null>(null);
const mapExpanded = ref(false);
const activeTab = ref<'list' | 'map'>('list');
const reorderMode = ref(false);
const reorderList = ref<DeliveryStopDto[]>([]);
const savingReorder = ref(false);

const tabs = [
  { key: 'list' as const, label: 'Liste', icon: ListBulletIcon },
  { key: 'map' as const, label: 'Harita', icon: MapIcon },
];

// Dynamic map height: fills remaining screen below stats+tabs, clears bottom nav
const windowHeight = ref(window.innerHeight);
function onResize() { windowHeight.value = window.innerHeight; }
onUnmounted(() => window.removeEventListener('resize', onResize));

// Header 56px + optional banner ~70px + stats bar 80px + tab switcher 52px + margins 40px + bottom nav 96px ≈ 394px
const MAP_CHROME_PX = 395;
const mapTabHeight = computed(() => `${Math.max(280, windowHeight.value - MAP_CHROME_PX)}px`);

// ── Computed ──────────────────────────────────────────────────────────────────

const sortedStops = computed((): DeliveryStopDto[] => {
  if (!routeData.value) return [];
  return [...routeData.value.stops].sort((a, b) => a.stopNumber - b.stopNumber);
});

const stopsWithCoords = computed(() =>
  sortedStops.value.filter(s => s.projectLatitude != null && s.projectLongitude != null)
);

const activeStop = computed((): DeliveryStopDto | null =>
  sortedStops.value.find(s => !s.isFullyDelivered) ?? null
);

const progressPct = computed(() => {
  if (!routeData.value || routeData.value.totalStops === 0) return 0;
  return Math.round((routeData.value.completedStops / routeData.value.totalStops) * 100);
});

const allDelivered = computed(() =>
  routeData.value !== null &&
  routeData.value.totalStops > 0 &&
  routeData.value.completedStops === routeData.value.totalStops
);

const canEndSession = computed(() =>
  allDelivered.value ||
  (activeSession.value !== null && routeData.value !== null && routeData.value.totalStops === 0)
);

const mapEmbedUrl = computed(() => {
  const stop = activeStop.value;
  if (!stop) return '';
  if (stop.projectLatitude != null && stop.projectLongitude != null) {
    return `https://maps.google.com/maps?q=${stop.projectLatitude},${stop.projectLongitude}&z=15&output=embed`;
  }
  if (stop.projectAddress) {
    return `https://maps.google.com/maps?q=${encodeURIComponent(stop.projectAddress)}&z=14&output=embed`;
  }
  return '';
});

// ── Reorder ───────────────────────────────────────────────────────────────────

function enterReorderMode() {
  reorderList.value = [...sortedStops.value];
  reorderMode.value = true;
}

function cancelReorder() {
  reorderMode.value = false;
  reorderList.value = [];
}

function moveStop(idx: number, direction: -1 | 1) {
  const newIdx = idx + direction;
  if (newIdx < 0 || newIdx >= reorderList.value.length) return;
  const list = [...reorderList.value];
  const temp = list[idx]!;
  list[idx] = list[newIdx]!;
  list[newIdx] = temp;
  reorderList.value = list;
}

async function saveReorder() {
  if (!routeData.value?.zonePreparationId) return;
  savingReorder.value = true;
  try {
    const items = reorderList.value.map((stop, idx) => ({
      projectId: stop.projectId,
      routeOrder: idx + 1,
    }));
    await driverService.reorderStops(routeData.value.zonePreparationId, items);
    // Reload data to reflect new order
    await load();
    reorderMode.value = false;
    notif.add('Rota sıralaması güncellendi.', 'success');
  } catch {
    notif.add('Sıralama kaydedilemedi.', 'error');
  } finally {
    savingReorder.value = false;
  }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

function isActiveStop(stop: DeliveryStopDto): boolean {
  return activeStop.value?.projectId === stop.projectId;
}

function stopCardClass(stop: DeliveryStopDto): string {
  if (reorderMode.value) {
    return 'bg-white dark:bg-[#0f2744] border-gray-200 dark:border-white/10';
  }
  if (isActiveStop(stop)) {
    return 'bg-white dark:bg-[#0f2744] border-blue-400 dark:border-blue-500 shadow-md shadow-blue-100 dark:shadow-blue-900/20';
  }
  if (stop.isFullyDelivered) {
    return 'bg-gray-50 dark:bg-[#0f2744]/60 border-gray-100 dark:border-white/5 opacity-70 cursor-pointer active:opacity-50';
  }
  return 'bg-white dark:bg-[#0f2744] border-gray-100 dark:border-white/10 shadow-sm cursor-pointer active:scale-[0.98]';
}

// ── Data ──────────────────────────────────────────────────────────────────────

async function load() {
  loading.value = true;
  error.value = '';
  try {
    [routeData.value, activeSession.value] = await Promise.all([
      driverService.getRoute(),
      driverSessionService.getActiveSession(),
    ]);
    driverRouteStore.mapsRouteUrl = routeData.value?.mapsRouteUrl ?? null;
  } catch {
    error.value = 'Rota yüklenemedi. Lütfen tekrar deneyin.';
  } finally {
    loading.value = false;
  }
}

function goToStop(stop: DeliveryStopDto) {
  router.push({ name: 'DriverStop', params: { projectId: stop.projectId } });
}

onMounted(() => {
  load();
  window.addEventListener('resize', onResize);
});
</script>
