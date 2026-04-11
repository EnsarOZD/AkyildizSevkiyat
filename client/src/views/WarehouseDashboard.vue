<template>
  <div class="space-y-4">

    <!-- ── Header ── -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-gray-900 dark:text-gray-100">Depo Hazırlık</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
          <span v-if="!loading">
            <span v-if="activePickingCount > 0" class="text-orange-600 dark:text-orange-400 font-medium">{{ activePickingCount }} bekleyen hazırlık</span>
            <span v-else class="text-green-600 dark:text-green-400 font-medium">Bekleyen hazırlık yok</span>
            <span v-if="overdueCount > 0" class="ml-2 text-red-600 dark:text-red-400 font-medium">• {{ overdueCount }} gecikmiş</span>
          </span>
          <span v-else class="italic">Yükleniyor...</span>
        </p>
      </div>
      <div class="flex items-center gap-2">
        <!-- 3-sekme filtre -->
        <div class="flex rounded-lg border border-gray-300 dark:border-gray-700 overflow-hidden text-xs">
          <button
            v-for="tab in tabOptions" :key="tab.key"
            @click="activeTab = tab.key"
            class="relative px-3 py-1.5 transition-colors"
            :class="activeTab === tab.key
              ? 'bg-blue-600 text-white'
              : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700'"
          >
            {{ tab.label }}
            <span
              v-if="tab.key === 'irsaliye' && irsaliyePendingCount > 0"
              class="ml-1 bg-red-500 text-white rounded-full px-1.5 py-0.5 text-[10px] font-bold"
            >{{ irsaliyePendingCount }}</span>
            <span
              v-if="tab.key === 'vehicle' && vehiclePendingCount > 0"
              class="ml-1 bg-purple-500 text-white rounded-full px-1.5 py-0.5 text-[10px] font-bold"
            >{{ vehiclePendingCount }}</span>
            <span
              v-if="tab.key === 'loading' && loadingPendingCount > 0"
              class="ml-1 bg-emerald-500 text-white rounded-full px-1.5 py-0.5 text-[10px] font-bold"
            >{{ loadingPendingCount }}</span>
          </button>
        </div>
        <!-- Refresh -->
        <button
          @click="fetchAll"
          :disabled="loading"
          class="p-2 rounded-lg border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors disabled:opacity-50"
          title="Yenile"
        >
          <svg :class="loading ? 'animate-spin' : ''" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
        </button>
      </div>
    </div>

    <!-- ── Kritik Stok Widget ── -->
    <CriticalStockWidget :items="criticalStocks" :loading="criticalStocksLoading" />

    <!-- ── Error state ── -->
    <div v-if="error" class="rounded-lg bg-red-50 border border-red-200 p-6 text-center">
      <p class="text-red-700 font-medium">{{ error }}</p>
      <button @click="fetchAll" class="mt-3 text-sm text-red-600 underline hover:text-red-800">
        Tekrar dene
      </button>
    </div>

    <!-- ── Skeleton ── -->
    <div v-if="loading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 animate-pulse">
        <div class="flex items-center justify-between mb-3">
          <div class="h-4 bg-gray-200 dark:bg-gray-700 rounded w-32"></div>
          <div class="h-6 bg-gray-200 dark:bg-gray-700 rounded-full w-20"></div>
        </div>
        <div class="h-3 bg-gray-200 dark:bg-gray-700 rounded w-48 mb-4"></div>
        <div class="h-10 bg-gray-200 dark:bg-gray-700 rounded w-full"></div>
      </div>
    </div>

    <!-- ── Empty state ── -->
    <div
      v-else-if="visibleZones.length === 0"
      class="text-center py-20 bg-white dark:bg-gray-900 rounded-xl border border-dashed border-gray-300 dark:border-gray-700"
    >
      <svg class="mx-auto h-12 w-12 text-gray-300 dark:text-gray-600 mb-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
      </svg>
      <p class="text-gray-500 dark:text-gray-400 font-medium">Son 14 günde bekleyen hazırlık bulunamadı</p>
      <p class="text-gray-400 dark:text-gray-600 text-sm mt-1">Tamamlananları görmek için "Aktifler" butonunu kapatın</p>
    </div>

    <!-- ── Zone Preparation Cards ── -->
    <div v-else class="space-y-3">

      <!-- Date group headers + cards -->
      <template v-for="group in groupedByDate" :key="group.date">

        <!-- Date header -->
        <div class="flex items-center gap-2 pt-1">
          <span class="text-xs font-bold uppercase tracking-wider"
                :class="group.isPast ? 'text-red-500 dark:text-red-400' : group.isToday ? 'text-orange-500 dark:text-orange-400' : 'text-gray-500 dark:text-gray-400'">
            {{ group.label }}
          </span>
          <div class="flex-1 h-px bg-gray-200 dark:bg-gray-800"></div>
          <span v-if="group.isPast" class="text-[10px] font-bold bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 px-1.5 py-0.5 rounded uppercase">GECİKMİŞ</span>
          <span v-else-if="group.isToday" class="text-[10px] font-bold bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 px-1.5 py-0.5 rounded uppercase">BUGÜN</span>
        </div>

        <!-- Zone cards for this date -->
        <div v-for="zonePrep in group.zones" :key="zonePrep.id"
             class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden shadow-sm"
             :class="group.isPast && zonePrep.statusId < 5 ? 'border-l-4 border-l-red-400' : group.isToday ? 'border-l-4 border-l-orange-400' : ''">

          <!-- Card header -->
          <div class="px-4 py-3 flex items-start justify-between gap-2 cursor-pointer select-none"
               @click="toggleExpanded(zonePrep.id)">
            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-2 flex-wrap">
                <span class="font-bold text-gray-900 dark:text-gray-100 text-sm truncate">{{ zonePrep.zoneName }}</span>
                <span class="text-[10px] font-bold px-1.5 py-0.5 rounded border flex-shrink-0"
                      :class="zonePrep.batchNo === 1
                        ? 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400 border-gray-300 dark:border-gray-700'
                        : 'bg-orange-50 dark:bg-orange-900/20 text-orange-700 dark:text-orange-400 border-orange-200 dark:border-orange-800'">
                  {{ zonePrep.batchLabel }}
                </span>
                <span v-if="zonePrep.isFrozen" class="text-[10px] font-bold bg-blue-50 dark:bg-blue-900/20 text-blue-600 dark:text-blue-400 border border-blue-200 dark:border-blue-800 px-1.5 py-0.5 rounded">🔒</span>
                <span v-if="zonePrep.openErrorCount > 0" class="text-[10px] font-bold bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400 border border-red-200 dark:border-red-800 px-1.5 py-0.5 rounded flex items-center gap-0.5">
                  ⚠ {{ zonePrep.openErrorCount }} hata
                </span>
                <span v-else-if="zonePrep.openWarningCount > 0" class="text-[10px] font-bold bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 border border-amber-200 dark:border-amber-800 px-1.5 py-0.5 rounded flex items-center gap-0.5">
                  ⚠ {{ zonePrep.openWarningCount }} uyarı
                </span>
              </div>
              <div class="flex items-center gap-2 mt-1">
                <span class="text-[11px] font-semibold px-2 py-0.5 rounded-full"
                      :class="statusChipClass(zonePrep.statusId)">
                  {{ getStatusLabel(zonePrep.statusId) }}
                </span>
                <span v-if="zonePrep.projects?.length" class="text-xs text-gray-400 dark:text-gray-500">
                  {{ readyCount(zonePrep) }}/{{ zonePrep.projects.length }} proje hazır
                </span>
              </div>
            </div>
            <!-- Chevron -->
            <svg class="h-4 w-4 text-gray-400 dark:text-gray-600 flex-shrink-0 mt-1 transition-transform"
                 :class="expandedIds.has(zonePrep.id) ? 'rotate-180' : ''"
                 fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
            </svg>
          </div>

          <!-- Action buttons row (always visible) -->
          <div class="px-4 pb-3 flex flex-wrap gap-2">

            <button
              v-if="zonePrep.statusId === 0 && !zonePrep.isFrozen"
              @click.stop="startPreparation(zonePrep)"
              class="flex-1 md:flex-none bg-blue-600 hover:bg-blue-700 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
            >
              🚀 <span>Toplamayı Başlat</span>
            </button>

            <button
              v-if="canStartMacro(zonePrep)"
              @click.stop="openMacroModal(zonePrep)"
              class="flex-1 md:flex-none bg-orange-500 hover:bg-orange-600 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
            >
              📦 <span>Macro Hazırlık</span>
            </button>

            <!-- Fetch irsaliye from Netsis (shown at status 4, before irsaliye fetched) -->
            <button
              v-if="zonePrep.statusId === 4 && !zonePrep.irsaliyeFetched && zonePrep.isFrozen"
              @click.stop="fetchIrsaliye(zonePrep)"
              :disabled="fetchingIrsaliyeId === zonePrep.id"
              class="flex-1 md:flex-none bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
            >
              <svg v-if="fetchingIrsaliyeId === zonePrep.id" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
              </svg>
              <span v-else>📄</span>
              <span>Netsisten İrsaliye Çek</span>
            </button>

            <!-- Assign vehicle (shown at status 4, after irsaliye fetched) -->
            <button
              v-if="zonePrep.statusId === 4 && zonePrep.irsaliyeFetched && zonePrep.isFrozen"
              @click.stop="openDriverModal(zonePrep)"
              class="flex-1 md:flex-none bg-purple-600 hover:bg-purple-700 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
            >
              🚚 <span>Araca Ata</span>
            </button>

            <!-- Confirm loading (shown at status 5 = ReadyForTransfer) -->
            <button
              v-if="zonePrep.statusId === 5"
              @click.stop="confirmLoading(zonePrep)"
              :disabled="confirmingLoadingId === zonePrep.id"
              class="flex-1 md:flex-none bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
            >
              <svg v-if="confirmingLoadingId === zonePrep.id" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
              </svg>
              <span v-else>🚛</span>
              <span>Yüklemeyi Onayla</span>
            </button>
          </div>

          <!-- Expanded: project grid -->
          <Transition name="expand">
            <div v-if="expandedIds.has(zonePrep.id)" class="border-t border-gray-100 dark:border-gray-800 px-4 py-4 space-y-4 bg-gray-50 dark:bg-gray-800/50">

              <!-- Original projects -->
              <div v-if="zonePrep.projects?.some((p: any) => !p.isAddedLater)">
                <p class="text-[11px] font-bold uppercase tracking-wider text-gray-400 dark:text-gray-500 mb-2">Planlanan Projeler</p>
                <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
                  <div v-for="proj in zonePrep.projects.filter((p: any) => !p.isAddedLater)" :key="proj.id"
                       class="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 p-3 relative overflow-hidden">
                    <div v-if="!zonePrep.isFrozen && zonePrep.statusId === 0"
                         class="absolute inset-0 bg-white/60 dark:bg-gray-900/60 flex items-center justify-center z-10 pointer-events-none">
                      <span class="text-gray-400 dark:text-gray-600 text-xs font-bold border border-gray-300 dark:border-gray-700 px-2 py-1 rounded rotate-[-10deg]">BEKLİYOR</span>
                    </div>
                    <div class="flex items-start justify-between gap-1 mb-1">
                      <span class="font-bold text-sm text-blue-900 dark:text-blue-300 truncate">{{ proj.projectCode }}</span>
                      <span class="text-[10px] font-bold px-1.5 py-0.5 rounded-full flex-shrink-0"
                            :class="proj.isMicroReady ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' : 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400'">
                        {{ proj.isMicroReady ? 'HAZIR' : 'BEKLER' }}
                      </span>
                    </div>
                    <p class="text-xs text-gray-500 dark:text-gray-400 truncate mb-2">{{ proj.projectName }}</p>
                    <button
                      @click="openMicroModal(proj)"
                      :disabled="!zonePrep.isFrozen"
                      class="w-full py-1.5 rounded-lg text-xs font-bold transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                      :class="proj.isMicroReady
                        ? 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-400 border border-green-200 dark:border-green-800'
                        : 'bg-blue-600 text-white hover:bg-blue-700'"
                    >
                      {{ proj.isMicroReady ? 'İncele / Düzenle' : 'Micro Topla' }}
                    </button>
                  </div>
                </div>
              </div>

              <!-- Late-added projects -->
              <div v-if="zonePrep.projects?.some((p: any) => p.isAddedLater)"
                   class="bg-red-50 dark:bg-red-900/10 border border-red-200 dark:border-red-800 rounded-lg p-3">
                <p class="text-[11px] font-bold uppercase tracking-wider text-red-600 dark:text-red-400 mb-2 flex items-center gap-1">
                  ⚡ Sonradan Eklenenler
                  <span class="bg-red-200 dark:bg-red-800 text-red-800 dark:text-red-200 px-1.5 rounded-full text-[9px]">DİKKAT</span>
                </p>
                <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
                  <div v-for="proj in zonePrep.projects.filter((p: any) => p.isAddedLater)" :key="proj.id"
                       class="bg-white dark:bg-gray-900 rounded-lg border border-red-200 dark:border-red-800 p-3 relative overflow-hidden">
                    <div class="absolute top-0 right-0 bg-red-600 text-white text-[9px] font-bold px-1.5 py-0.5 rounded-bl">YENİ</div>
                    <div class="flex items-start justify-between gap-1 mb-1">
                      <span class="font-bold text-sm text-red-900 dark:text-red-300 truncate">{{ proj.projectCode }}</span>
                      <span class="text-[10px] font-bold px-1.5 py-0.5 rounded-full flex-shrink-0 mt-3"
                            :class="proj.isMicroReady ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' : 'bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400'">
                        {{ proj.isMicroReady ? 'HAZIR' : 'BEKLER' }}
                      </span>
                    </div>
                    <p class="text-xs text-gray-500 dark:text-gray-400 truncate mb-2">{{ proj.projectName }}</p>
                    <button
                      @click="openMicroModal(proj)"
                      :disabled="!zonePrep.isFrozen"
                      class="w-full py-1.5 rounded-lg text-xs font-bold transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                      :class="proj.isMicroReady
                        ? 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-400 border border-green-200 dark:border-green-800'
                        : 'bg-red-600 text-white hover:bg-red-700'"
                    >
                      {{ proj.isMicroReady ? 'İncele / Düzenle' : 'Micro Topla' }}
                    </button>
                  </div>
                </div>
              </div>

            </div>
          </Transition>
        </div>

      </template>
    </div>

    <!-- ── Modals ── -->
    <MicroPickingModal
      v-if="showMicroModal"
      :zp-project-id="selectedProjectId"
      :project-name="selectedProjectName"
      @close="closeMicroModal"
      @completed="fetchAll"
    />
    <MacroPickingModal
      v-if="showMacroModal && selectedZonePrep"
      :zone-preparation-id="selectedZonePrep.id"
      @close="closeMacroModal"
      @completed="fetchAll"
    />
    <DriverAssignmentModal
      v-if="showDriverModal && selectedZonePrep"
      :zone-preparation-id="selectedZonePrep.id"
      @close="closeDriverModal"
      @completed="fetchAll"
    />

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import warehouseService, { type DashboardZoneDto } from '../services/warehouseService';
import { dashboardService, type CriticalStockItem } from '../services/dashboardService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import MicroPickingModal from '../components/MicroPickingModal.vue';
import MacroPickingModal from '../components/MacroPickingModal.vue';
import DriverAssignmentModal from '../components/DriverAssignmentModal.vue';
import CriticalStockWidget from '../components/CriticalStockWidget.vue';

const notificationStore = useNotificationStore();
const loading = ref(false);
const error = ref('');
const isStarting = ref(false);
const fetchingIrsaliyeId = ref<number | null>(null);
const confirmingLoadingId = ref<number | null>(null);
type TabKey = 'active' | 'irsaliye' | 'vehicle' | 'loading' | 'all';
const activeTab = ref<TabKey>('active');
const tabOptions: { key: TabKey; label: string }[] = [
  { key: 'active',    label: 'Aktif Hazırlık' },
  { key: 'irsaliye',  label: 'İrsaliye Bekleyen' },
  { key: 'vehicle',   label: 'Araç Atama Bekleyen' },
  { key: 'loading',   label: 'Yükleme Bekleyen' },
  { key: 'all',       label: 'Tümü' },
];
const expandedIds = ref<Set<number>>(new Set());

interface ZonePrepWithDate extends DashboardZoneDto {
  deliveryDateStr: string;
}

// All fetched zone preps across date range
const allZones = ref<ZonePrepWithDate[]>([]);

// Modal state
const showMicroModal = ref(false);
const selectedProjectId = ref(0);
const selectedProjectName = ref('');
const showMacroModal = ref(false);
const showDriverModal = ref(false);
const selectedZonePrep = ref<DashboardZoneDto | null>(null);
const criticalStocks = ref<CriticalStockItem[]>([]);
const criticalStocksLoading = ref(false);

// ── Tüm aktif zone hazırlıklarını tek çağrıyla getir (tarih filtresi yok) ──
const fetchAll = async () => {
  loading.value = true;
  error.value = '';
  try {
    const zones = await warehouseService.getDashboardAll();
    const withDate = zones.map(z => ({
      ...z,
      deliveryDateStr: new Date(z.deliveryDate).toISOString().split('T')[0] as string,
    }));
    // Tarihe göre sırala
    withDate.sort((a, b) => a.deliveryDateStr.localeCompare(b.deliveryDateStr));
    allZones.value = withDate;

    // Tamamlanmamış zone'ları otomatik aç
    expandedIds.value = new Set(
      withDate.filter(z => z.statusId < 5).map(z => z.id)
    );
  } catch (e) {
    error.value = 'Veriler yüklenirken bir hata oluştu.';
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Veriler alınamadı.', 'error');
  } finally {
    loading.value = false;
  }
};

// ── Filtered / grouped data ──
const visibleZones = computed(() => {
  if (activeTab.value === 'irsaliye')
    return allZones.value.filter(z => z.statusId === 4 && !z.irsaliyeFetched);
  if (activeTab.value === 'vehicle')
    return allZones.value.filter(z => z.statusId === 4 && z.irsaliyeFetched);
  if (activeTab.value === 'loading')
    return allZones.value.filter(z => z.statusId === 5);
  if (activeTab.value === 'active')
    return allZones.value.filter(z => z.statusId < 4);
  return allZones.value; // 'all'
});

const irsaliyePendingCount = computed(
  () => allZones.value.filter(z => z.statusId === 4 && !z.irsaliyeFetched).length
);

const vehiclePendingCount = computed(
  () => allZones.value.filter(z => z.statusId === 4 && z.irsaliyeFetched).length
);

const loadingPendingCount = computed(
  () => allZones.value.filter(z => z.statusId === 5).length
);

const activePickingCount = computed(() => allZones.value.filter(z => z.statusId < 4).length);
const overdueCount = computed(() =>
  allZones.value.filter(z => z.statusId < 4 && z.deliveryDateStr < todayStr()).length
);

interface DateGroup {
  date: string;
  label: string;
  isToday: boolean;
  isPast: boolean;
  zones: ZonePrepWithDate[];
}

const groupedByDate = computed((): DateGroup[] => {
  const map = new Map<string, DateGroup>();
  const today = todayStr();

  for (const zone of visibleZones.value) {
    const d = zone.deliveryDateStr;
    if (!map.has(d)) {
      const isToday = d === today;
      const isPast = d < today;
      const dateObj = new Date(d + 'T12:00:00');
      const label = isToday
        ? `Bugün — ${dateObj.toLocaleDateString('tr-TR', { day: 'numeric', month: 'long' })}`
        : dateObj.toLocaleDateString('tr-TR', { weekday: 'long', day: 'numeric', month: 'long' });
      map.set(d, { date: d, label, isToday, isPast, zones: [] });
    }
    map.get(d)!.zones.push(zone);
  }

  return Array.from(map.values());
});

// ── UI helpers ──
const toggleExpanded = (id: number) => {
  const s = new Set(expandedIds.value);
  if (s.has(id)) s.delete(id);
  else s.add(id);
  expandedIds.value = s;
};

const readyCount = (zone: DashboardZoneDto) =>
  zone.projects?.filter((p: any) => p.isMicroReady).length ?? 0;

const statusChipClass = (statusId: number) => {
  if (statusId === 6) return 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400';
  if (statusId === 5) return 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400';
  if (statusId === 4) return 'bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-400';
  if (statusId === 3) return 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400';
  if (statusId >= 1) return 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400';
  return 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400';
};

const getStatusLabel = (statusId: number) => {
  switch (statusId) {
    case 0: return 'Başlamadı';
    case 1: return 'Micro Toplama';
    case 2: return 'Macro Bekliyor';
    case 3: return 'Macro Toplama';
    case 4: return 'Sevke Hazır';
    case 5: return 'Araca Atandı';
    case 6: return 'Sevk Edildi';
    default: return 'Bilinmiyor';
  }
};

const canStartMacro = (zone: DashboardZoneDto) =>
  zone.isFrozen && zone.projects?.length > 0 && zone.statusId < 4;

// ── Modal handlers ──
const openMicroModal = (proj: any) => {
  selectedProjectId.value = proj.id;
  selectedProjectName.value = proj.projectName;
  showMicroModal.value = true;
};
const closeMicroModal = () => { showMicroModal.value = false; };

const openMacroModal = (zone: DashboardZoneDto) => {
  selectedZonePrep.value = zone;
  showMacroModal.value = true;
};
const closeMacroModal = () => { showMacroModal.value = false; };

const openDriverModal = (zone: DashboardZoneDto) => {
  selectedZonePrep.value = zone;
  showDriverModal.value = true;
};
const closeDriverModal = () => { showDriverModal.value = false; };

const fetchIrsaliye = async (zonePrep: DashboardZoneDto) => {
  if (fetchingIrsaliyeId.value) return;
  fetchingIrsaliyeId.value = zonePrep.id;
  try {
    const result = await warehouseService.fetchIrsaliye(zonePrep.id);
    if (result.fetched > 0) {
      notificationStore.add(`${result.fetched} sevkiyatın irsaliye numarası çekildi.`, 'success');
    }
    if (result.warnings.length > 0) {
      result.warnings.forEach(w => notificationStore.add(w, 'warning'));
    }
    if (result.errors.length > 0) {
      result.errors.forEach(e => notificationStore.add(e, 'error'));
    }
    if (result.fetched === 0 && result.warnings.length === 0 && result.errors.length === 0) {
      notificationStore.add('Tüm irsaliyeler zaten çekilmiş.', 'info');
    }
    await fetchAll();
  } catch (e: any) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İrsaliye çekilemedi.', 'error');
  } finally {
    fetchingIrsaliyeId.value = null;
  }
};

const startPreparation = async (zonePrep: DashboardZoneDto) => {
  if (isStarting.value) return;
  const ok = await notificationStore.promptConfirm({
    title: 'Toplamayı Başlat',
    message: 'Toplamayı başlatmak istediğinize emin misiniz? Bu işlemden sonra yeni siparişler ayrı bir batch olarak takip edilecektir.',
    confirmText: 'Başlat',
    type: 'warning'
  });
  if (!ok) return;

  isStarting.value = true;
  try {
    await warehouseService.startZonePreparation({ zonePreparationId: zonePrep.id });
    await fetchAll();
    notificationStore.add('Toplama başarıyla başlatıldı.', 'success');
  } catch (e: any) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Başlatılamadı.', 'error');
  } finally {
    isStarting.value = false;
  }
};

const confirmLoading = async (zonePrep: DashboardZoneDto) => {
  if (confirmingLoadingId.value) return;

  const ok = await notificationStore.promptConfirm({
    title: 'Yüklemeyi Onayla',
    message: `${zonePrep.zoneName} için araç yükleme tamamlandı mı? Bu işlem sevkiyatları "Sevk Edildi" statüsüne geçirecektir.`,
    confirmText: 'Evet, Yükleme Tamamlandı',
    type: 'warning'
  });
  if (!ok) return;

  confirmingLoadingId.value = zonePrep.id;
  try {
    await warehouseService.confirmLoading(zonePrep.id);
    await fetchAll();
    notificationStore.add('Yükleme onaylandı. Sevkiyatlar "Sevk Edildi" statüsüne geçirildi.', 'success');
  } catch (e: any) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Yükleme onaylanamadı.', 'error');
  } finally {
    confirmingLoadingId.value = null;
  }
};

onMounted(async () => {
  fetchAll();
  criticalStocksLoading.value = true;
  try {
    criticalStocks.value = await dashboardService.getCriticalStocks();
  } catch {
    // sessiz hata — widget empty state gösterir
  } finally {
    criticalStocksLoading.value = false;
  }
});
</script>

<style scoped>
.expand-enter-active,
.expand-leave-active {
  transition: all 0.2s ease;
  overflow: hidden;
}
.expand-enter-from,
.expand-leave-to {
  opacity: 0;
  max-height: 0;
}
.expand-enter-to,
.expand-leave-from {
  opacity: 1;
  max-height: 2000px;
}
</style>
