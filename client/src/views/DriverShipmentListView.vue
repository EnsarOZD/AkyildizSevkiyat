<template>
  <div class="space-y-3 pb-2" style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">

    <!-- ── Banners ─────────────────────────────────────────────────── -->
    <div
      v-if="!loading && !activeSession"
      class="rounded-2xl bg-amber-500/10 border border-amber-400/30 p-4 flex items-center justify-between gap-3"
    >
      <div>
        <p class="text-sm font-bold text-amber-300">Seferinizi başlatmayı unutmayın!</p>
        <p class="text-xs text-amber-200/70 mt-0.5">QR kod okutarak seferi başlatın.</p>
      </div>
      <router-link
        to="/driver/qr-scan"
        class="shrink-0 px-3.5 h-9 inline-flex items-center bg-amber-500 hover:bg-amber-400 text-[#0a1626] text-xs font-bold rounded-xl"
      >QR Okut</router-link>
    </div>

    <div
      v-if="!loading && canEndSession && activeSession"
      class="rounded-2xl bg-emerald-500/10 border border-emerald-400/40 p-4 flex items-center justify-between gap-3"
    >
      <div>
        <p class="text-sm font-bold text-emerald-300">Tüm teslimatlar tamamlandı!</p>
        <p class="text-xs text-emerald-200/70 mt-0.5">Aracınıza giderek seferi kapatın.</p>
      </div>
      <router-link
        to="/driver/qr-scan?mode=end"
        class="shrink-0 px-3.5 h-9 inline-flex items-center bg-emerald-500 hover:bg-emerald-400 text-[#0a1626] text-xs font-bold rounded-xl"
      >QR Okut</router-link>
    </div>

    <!-- ── Loading ────────────────────────────────────────────────── -->
    <div v-if="loading" class="flex justify-center py-20">
      <div class="w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- ── Error ──────────────────────────────────────────────────── -->
    <div
      v-else-if="error"
      class="bg-red-500/10 border border-red-400/30 rounded-2xl p-4 text-red-300 text-sm"
    >
      {{ error }}
    </div>

    <template v-else-if="routeData">

      <!-- ── Stats / gradient header ──────────────────────────────── -->
      <div class="relative rounded-3xl overflow-hidden p-4"
        style="background: linear-gradient(155deg,#1b3e74 0%,#15315c 50%,#0f2748 100%); box-shadow: 0 14px 40px rgba(0,0,0,0.45);">
        <div class="absolute -top-10 -right-8 w-44 h-44 rounded-full pointer-events-none"
          style="background: radial-gradient(circle, rgba(59,130,246,0.35), transparent 70%);"></div>

        <div class="relative flex items-center justify-between gap-3">
          <div class="grid grid-cols-2 gap-x-5 gap-y-2 flex-1">
            <div>
              <p class="text-[10px] font-bold uppercase tracking-wider text-white/45 mb-0.5">Tamamlanan</p>
              <p class="text-xl font-extrabold text-white leading-none">
                {{ routeData.completedStops }}<span class="text-sm font-bold text-white/40">/{{ routeData.totalStops }}</span>
                <span class="text-[11px] font-semibold text-white/40 ml-1">durak</span>
              </p>
            </div>
            <div>
              <p class="text-[10px] font-bold uppercase tracking-wider text-white/45 mb-0.5">İrsaliye</p>
              <p class="text-xl font-extrabold text-white leading-none">
                {{ routeData.completedShipments }}<span class="text-sm font-bold text-white/40">/{{ routeData.totalShipments }}</span>
              </p>
            </div>
          </div>

          <!-- Progress ring -->
          <div class="relative shrink-0" style="width:66px;height:66px;">
            <svg width="66" height="66" viewBox="0 0 66 66" style="transform: rotate(-90deg);">
              <circle cx="33" cy="33" r="27" fill="none" stroke="rgba(255,255,255,0.14)" stroke-width="6" />
              <circle cx="33" cy="33" r="27" fill="none" stroke="#60a5fa" stroke-width="6" stroke-linecap="round"
                :stroke-dasharray="2 * Math.PI * 27"
                :stroke-dashoffset="(2 * Math.PI * 27) * (1 - progressPct / 100)"
                style="filter: drop-shadow(0 0 5px rgba(96,165,250,0.7)); transition: stroke-dashoffset .7s;" />
            </svg>
            <div class="absolute inset-0 flex flex-col items-center justify-center">
              <span class="text-[15px] font-extrabold text-white leading-none">{{ progressPct }}%</span>
            </div>
          </div>
        </div>

        <!-- Rota aç -->
        <button
          v-if="routeData.mapsRouteUrl"
          @click="openMaps(routeData.mapsRouteUrl)"
          class="relative mt-3 w-full h-10 rounded-xl bg-white/[0.1] hover:bg-white/[0.16] text-white text-[13px] font-bold flex items-center justify-center gap-1.5 transition-colors"
        >
          <MapIcon class="w-4 h-4" />
          Rotanın Tamamını Haritada Aç
        </button>
      </div>

      <!-- ── Tab switcher ────────────────────────────────────────── -->
      <div class="bg-[#0f2038] rounded-2xl border border-white/[0.06] p-1 flex gap-1">
        <button
          v-for="tab in tabs"
          :key="tab.key"
          @click="activeTab = tab.key"
          class="flex-1 py-2 rounded-xl text-sm font-bold transition-colors"
          :class="activeTab === tab.key
            ? 'bg-gradient-to-br from-blue-500 to-blue-600 text-white'
            : 'text-white/45 hover:bg-white/[0.04]'"
        >
          <span class="flex items-center justify-center gap-1.5">
            <component :is="tab.icon" class="w-4 h-4" />
            {{ tab.label }}
          </span>
        </button>
      </div>

      <!-- ═══════════════════════ TAB: LİSTE ══════════════════════ -->
      <template v-if="activeTab === 'list'">

        <!-- Embedded map for active stop -->
        <div
          v-if="activeStop && (activeStop.projectLatitude || activeStop.projectAddress)"
          class="bg-[#0f2038] rounded-2xl border border-white/[0.06] overflow-hidden"
        >
          <button
            @click="mapExpanded = !mapExpanded"
            class="w-full flex items-center justify-between px-4 py-3 text-sm font-semibold text-white hover:bg-white/[0.04] transition-colors"
          >
            <div class="flex items-center gap-2">
              <MapPinIcon class="w-4 h-4 text-blue-400" />
              <span>{{ activeStop.projectName }} — Harita</span>
            </div>
            <ChevronDownIcon
              class="w-4 h-4 text-white/40 transition-transform duration-200"
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
          <TruckIcon class="w-14 h-14 mx-auto text-white/15 mb-3" />
          <p class="text-white/55 font-semibold">Aktif teslimat yok</p>
          <p class="text-xs text-white/35 mt-1">Bugün size atanmış teslimat bulunmuyor.</p>
        </div>

        <!-- Reorder mode toolbar -->
        <div
          v-if="routeData.stops.length > 1 && routeData.zonePreparationId"
          class="flex items-center justify-between gap-2"
        >
          <template v-if="!reorderMode">
            <button
              @click="enterReorderMode"
              class="flex items-center gap-1.5 px-3 h-10 rounded-xl border border-white/[0.1] text-xs font-semibold text-white/70 hover:bg-white/[0.05] transition-colors"
            >
              <Bars3Icon class="w-4 h-4" />
              Sırayı Düzenle
            </button>
          </template>
          <template v-else>
            <button
              @click="cancelReorder"
              class="flex-1 h-10 rounded-xl border border-white/[0.1] text-xs font-semibold text-white/70"
            >Vazgeç</button>
            <button
              @click="saveReorder"
              :disabled="savingReorder"
              class="flex-1 h-10 rounded-xl bg-gradient-to-br from-blue-500 to-blue-600 text-white text-xs font-bold disabled:opacity-50"
            >
              {{ savingReorder ? 'Kaydediliyor…' : 'Kaydet' }}
            </button>
          </template>
        </div>

        <!-- "Şimdi" etiketi -->
        <div v-if="activeStop && !reorderMode" class="flex items-center gap-2 pt-1 pl-1">
          <span class="text-[11px] font-extrabold uppercase tracking-wider text-amber-400">Şimdi Teslim Edilecek</span>
          <span class="flex-1 h-px bg-white/10"></span>
        </div>

        <!-- Stop list -->
        <div v-if="routeData.stops.length > 0" class="space-y-2.5">
          <div
            v-for="(stop, idx) in reorderMode ? reorderList : sortedStops"
            :key="stop.projectId"
            @click="!isActiveStop(stop) && !reorderMode && goToStop(stop)"
            class="rounded-2xl border transition-all duration-150"
            :class="[stopCardClass(stop), reorderMode ? 'cursor-default' : '']"
            :style="isActiveStop(stop) && !reorderMode ? activeStopStyle : {}"
          >
            <div class="p-4">
              <!-- Reorder controls -->
              <div v-if="reorderMode" class="flex items-start gap-3">
                <div class="flex flex-col gap-0.5 shrink-0 pt-0.5">
                  <button
                    @click.stop="moveStop(idx, -1)"
                    :disabled="idx === 0"
                    class="w-7 h-7 flex items-center justify-center rounded-lg bg-white/10 disabled:opacity-30 text-white/70"
                  ><ChevronUpIcon class="w-4 h-4" /></button>
                  <button
                    @click.stop="moveStop(idx, 1)"
                    :disabled="idx === reorderList.length - 1"
                    class="w-7 h-7 flex items-center justify-center rounded-lg bg-white/10 disabled:opacity-30 text-white/70"
                  ><ChevronDownIcon class="w-4 h-4" /></button>
                </div>
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2 mb-1">
                    <span class="text-xs font-bold text-blue-300 bg-blue-500/15 rounded-full w-6 h-6 flex items-center justify-center shrink-0">{{ idx + 1 }}</span>
                    <span class="font-bold text-sm text-white break-words">{{ stop.projectName }}</span>
                  </div>
                  <p v-if="stop.projectAddress" class="text-xs text-white/40 break-words">{{ stop.projectAddress }}</p>
                </div>
              </div>

              <!-- Normal stop card -->
              <StopCard v-else :stop="stop" :is-active="isActiveStop(stop)" />
            </div>
          </div>
        </div>
      </template>

      <!-- ═══════════════════════ TAB: HARİTA ══════════════════════ -->
      <template v-if="activeTab === 'map'">
        <div
          class="bg-[#0f2038] rounded-2xl border border-white/[0.06] overflow-hidden"
          :style="{ height: mapTabHeight }"
        >
          <div v-if="stopsWithCoords.length === 0" class="flex flex-col items-center justify-center h-full text-white/40 gap-2">
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

      <!-- ═══════════════════════ TAB: TAMAMLANAN ══════════════════════ -->
      <template v-if="activeTab === 'done'">
        <div v-if="completedStops.length === 0" class="text-center py-20">
          <CheckCircleIcon class="w-14 h-14 mx-auto text-white/15 mb-3" />
          <p class="text-white/55 font-semibold">Henüz tamamlanan teslimat yok</p>
          <p class="text-xs text-white/35 mt-1">Teslim ettiğiniz duraklar burada görünecek.</p>
        </div>
        <div v-else class="space-y-2.5">
          <div
            v-for="stop in completedStops"
            :key="stop.projectId"
            @click="goToStop(stop)"
            class="rounded-2xl border bg-emerald-500/[0.06] border-emerald-400/20 cursor-pointer active:opacity-60 transition-opacity"
          >
            <div class="p-4">
              <StopCard :stop="stop" :is-active="false" />
            </div>
          </div>
        </div>
      </template>

    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  TruckIcon,
  MapIcon,
  MapPinIcon,
  ChevronDownIcon,
  ChevronUpIcon,
  Bars3Icon,
  ListBulletIcon,
  CheckCircleIcon,
} from '@heroicons/vue/24/outline';
import driverService, { type DriverRouteDto, type DeliveryStopDto } from '../services/driverService';
import driverSessionService, { type ActiveSessionDto } from '../services/driverSessionService';
import StopCard from '../components/driver/StopCard.vue';
import RouteMapView from '../components/driver/RouteMapView.vue';
import { useDriverRouteStore } from '../stores/driverRoute';
import { useNotificationStore } from '../stores/notification';
import { useOpenMaps } from '../composables/useOpenMaps';

const route = useRoute();
const router = useRouter();
const driverRouteStore = useDriverRouteStore();
const notif = useNotificationStore();
const { openMaps } = useOpenMaps();
const routeData = ref<DriverRouteDto | null>(null);
const loading = ref(false);
const error = ref('');
const activeSession = ref<ActiveSessionDto | null>(null);
const mapExpanded = ref(false);
const activeTab = ref<'list' | 'map' | 'done'>('list');
const reorderMode = ref(false);
const reorderList = ref<DeliveryStopDto[]>([]);
const savingReorder = ref(false);

const tabs = [
  { key: 'list' as const, label: 'Liste', icon: ListBulletIcon },
  { key: 'map' as const, label: 'Harita', icon: MapIcon },
  { key: 'done' as const, label: 'Tamamlanan', icon: CheckCircleIcon },
];

// Alt menüdeki "Tamamlanan" sekmesi ?view=done query'si ile bu sekmeyi açar
watch(
  () => route.query.view,
  (v) => { activeTab.value = v === 'done' ? 'done' : 'list'; },
  { immediate: true },
);

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

const completedStops = computed((): DeliveryStopDto[] =>
  sortedStops.value.filter(s => s.isFullyDelivered)
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
  // Teslim edilmiş duraklar sıralanamaz (pasif) — yalnızca bekleyenler düzenlenir
  reorderList.value = sortedStops.value.filter(s => !s.isFullyDelivered);
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
    // Teslim edilmiş duraklar mevcut sıralarını korusun; bekleyenler yeni sıraya göre
    // arkalarına eklensin → global routeOrder tutarlı kalır
    const ordered = [...completedStops.value, ...reorderList.value];
    const items = ordered.map((stop, idx) => ({
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
    return 'bg-[#0f2038] border-white/10';
  }
  if (isActiveStop(stop)) {
    return 'border-blue-400/40 shadow-lg shadow-blue-600/20';
  }
  if (stop.isFullyDelivered) {
    return 'bg-emerald-500/[0.06] border-emerald-400/20 opacity-80 cursor-pointer active:opacity-50';
  }
  return 'bg-[#0f2038] border-white/[0.06] cursor-pointer active:scale-[0.98]';
}

// Aktif durak kartı için gradyan zemin (floating kart hissi)
const activeStopStyle = { background: 'linear-gradient(160deg,#16335f,#0f2240)' };

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
    driverRouteStore.hasActiveSession = activeSession.value !== null;
    driverRouteStore.canEndSession = canEndSession.value;
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
