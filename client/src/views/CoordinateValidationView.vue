<template>
  <div class="p-4 sm:p-6 space-y-5">
    <!-- Başlık -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Koordinat Doğrulama</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">Proje adreslerini Google Geocoding ile karşılaştır ve koordinatları doğrula</p>
      </div>
      <div class="flex gap-2 flex-wrap">
        <button
          v-if="selectedIds.size > 0"
          @click="applySelectedGeocodedCoords"
          :disabled="bulkRunning || selectedWithGeocode.length === 0"
          class="px-4 py-2 bg-green-600 text-white rounded font-medium text-sm hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed"
          :title="selectedWithGeocode.length === 0 ? 'Seçili projelerde uygulanacak geocode koordinatı yok' : ''"
        >
          Seçili Koordinatları Uygula ({{ selectedWithGeocode.length }})
        </button>
        <button
          v-if="selectedIds.size > 0"
          @click="validateSelected"
          :disabled="bulkRunning"
          class="px-4 py-2 bg-indigo-600 text-white rounded font-medium text-sm hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Seçilileri Doğrula ({{ selectedIds.size }})
        </button>
        <button
          @click="validateAll"
          :disabled="bulkRunning || filteredProjects.length === 0"
          class="px-4 py-2 bg-indigo-500 text-white rounded font-medium text-sm hover:bg-indigo-600 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Tümünü Doğrula ({{ filteredProjects.length }})
        </button>
        <button
          @click="startBulkGeocode"
          :disabled="bulkRunning || noCoordinateWithAddress.length === 0"
          class="px-4 py-2 bg-orange-500 text-white rounded font-medium text-sm hover:bg-orange-600 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Koordinatsızları Geocode Et ({{ noCoordinateWithAddress.length }})
        </button>
      </div>
    </div>

    <!-- Toplu İşlem Progress -->
    <div v-if="bulkRunning" class="bg-orange-50 dark:bg-orange-900/20 border border-orange-200 dark:border-orange-800 rounded-lg p-4">
      <div class="flex items-center justify-between mb-2">
        <span class="text-sm font-medium text-orange-800 dark:text-orange-200">
          {{ bulkLabel }} devam ediyor... {{ bulkDone }}/{{ bulkTotal }} tamamlandı
        </span>
        <span class="text-xs text-orange-600 dark:text-orange-400">
          Tahmini süre: {{ estimatedMinutes }} dakika
        </span>
      </div>
      <div class="w-full bg-orange-200 dark:bg-orange-800 rounded-full h-2">
        <div
          class="bg-orange-500 h-2 rounded-full transition-all duration-300"
          :style="{ width: bulkTotal > 0 ? `${(bulkDone / bulkTotal) * 100}%` : '0%' }"
        ></div>
      </div>
    </div>

    <!-- Özet Kartlar -->
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 border-l-4 border-gray-300">
        <div class="text-2xl font-bold text-gray-700 dark:text-gray-200">{{ activeProjects.length }}</div>
        <div class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam Aktif Proje</div>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 border-l-4 border-green-400">
        <div class="text-2xl font-bold text-green-600">{{ withCoordinates }}</div>
        <div class="text-xs text-gray-500 dark:text-gray-400 mt-1">Koordinatlı</div>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 border-l-4 border-red-400">
        <div class="text-2xl font-bold text-red-600">{{ withoutCoordinates }}</div>
        <div class="text-xs text-gray-500 dark:text-gray-400 mt-1">Koordinatsız</div>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 border-l-4 border-yellow-400">
        <div class="text-2xl font-bold text-yellow-600">{{ unvalidated }}</div>
        <div class="text-xs text-gray-500 dark:text-gray-400 mt-1">Doğrulanmamış</div>
      </div>
    </div>

    <!-- Proje arama -->
    <div class="relative">
      <input
        v-model="searchTerm"
        type="text"
        placeholder="Proje adı veya kodu ara..."
        class="w-full sm:max-w-md border dark:border-gray-700 rounded-lg pl-9 pr-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100"
      />
      <svg class="absolute left-3 top-2.5 w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-4.35-4.35M11 19a8 8 0 100-16 8 8 0 000 16z" />
      </svg>
    </div>

    <!-- Sekmeler -->
    <div class="flex gap-1 flex-wrap border-b dark:border-gray-700">
      <button
        v-for="tab in tabs"
        :key="tab.key"
        @click="activeTab = tab.key"
        class="px-4 py-2 text-sm font-medium rounded-t transition border-b-2"
        :class="activeTab === tab.key
          ? 'border-indigo-600 text-indigo-600 dark:text-indigo-400'
          : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200'"
      >
        {{ tab.label }}
        <span class="ml-1 text-xs bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 rounded-full px-1.5 py-0.5">
          {{ tab.count }}
        </span>
      </button>
    </div>

    <!-- Tablo — Masaüstü -->
    <div class="hidden sm:block bg-white dark:bg-gray-900 shadow rounded-lg overflow-x-auto">
      <div v-if="loading" class="p-8 text-center text-gray-500 dark:text-gray-400">Projeler yükleniyor...</div>
      <div v-else-if="filteredProjects.length === 0" class="p-8 text-center text-gray-400">Bu kategoride proje yok.</div>
      <table v-else class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-3 py-3 w-8">
              <input type="checkbox" @change="toggleAll" :checked="allSelected" class="rounded" />
            </th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Kod</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Ad</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Adres</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Koordinat</th>
            <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Mesafe</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Durum</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Kaynak / İl-İlçe</th>
            <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">İşlem</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="p in filteredProjects" :key="p.id" class="hover:bg-gray-50 dark:hover:bg-gray-800/50">
            <td class="px-3 py-3">
              <input type="checkbox" :checked="selectedIds.has(p.id)" @change="toggleSelect(p.id)" class="rounded" />
            </td>
            <td class="px-4 py-3 font-mono text-xs font-semibold text-gray-800 dark:text-gray-200">{{ p.code }}</td>
            <td class="px-4 py-3 text-gray-700 dark:text-gray-300 max-w-[180px]">
              <div class="truncate" :title="p.name">{{ p.name }}</div>
              <span v-if="p.locationNeedsRecheck" class="inline-block mt-0.5 text-[10px] px-1.5 py-0.5 rounded bg-amber-100 text-amber-700 dark:bg-amber-900/30 dark:text-amber-300">
                ⚠ Adres değişti — kontrol et
              </span>
            </td>
            <td class="px-4 py-3 text-gray-500 dark:text-gray-400 max-w-[200px]">
              <span v-if="p.address" class="truncate block" :title="p.address">{{ p.address }}</span>
              <span v-else class="text-gray-300 dark:text-gray-600 italic">Adres yok</span>
            </td>
            <td class="px-4 py-3 font-mono text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap">
              <span v-if="p.latitude && p.longitude">{{ p.latitude.toFixed(4) }}, {{ p.longitude.toFixed(4) }}</span>
              <span v-else class="text-gray-300 dark:text-gray-600">—</span>
            </td>
            <td class="px-4 py-3 text-right font-semibold whitespace-nowrap">
              <template v-if="getValidation(p.id)">
                <span :class="distanceClass(getValidation(p.id)!.distanceKm)">
                  {{ getValidation(p.id)!.distanceKm != null ? `${getValidation(p.id)!.distanceKm} km` : '—' }}
                </span>
              </template>
              <span v-else class="text-gray-300 dark:text-gray-600">—</span>
            </td>
            <td class="px-4 py-3">
              <template v-if="rowLoading.has(p.id)">
                <span class="inline-flex items-center gap-1 text-xs text-gray-500">
                  <svg class="animate-spin h-3 w-3" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
                  Doğrulanıyor...
                </span>
              </template>
              <template v-else>
                <StatusBadgeCoord :status="getValidation(p.id)?.status ?? resolveDefaultStatus(p)" />
              </template>
            </td>
            <td class="px-4 py-3 text-xs">
              <div class="space-y-1">
                <span class="inline-block px-2 py-0.5 rounded-full" :class="sourceBadgeClass(getValidation(p.id)?.recordedSource)">
                  {{ sourceLabel(getValidation(p.id)?.recordedSource) }}
                </span>
                <div v-if="getValidation(p.id) && (getValidation(p.id)!.storedCityName || getValidation(p.id)!.detectedCityName)"
                  :class="getValidation(p.id)!.cityDistrictStatus === 'Mismatch' ? 'text-red-600 dark:text-red-400 font-medium' : 'text-gray-500 dark:text-gray-400'">
                  <span :title="'Kayıtlı: ' + ((getValidation(p.id)!.storedCityName || '') + ' ' + (getValidation(p.id)!.storedDistrictName || '')).trim()">
                    {{ (getValidation(p.id)!.storedDistrictName || getValidation(p.id)!.storedCityName) || '—' }}
                  </span>
                  <template v-if="getValidation(p.id)!.cityDistrictStatus === 'Mismatch'">
                    → <span class="font-semibold" :title="'Tespit edilen: ' + ((getValidation(p.id)!.detectedCityName || '') + ' ' + (getValidation(p.id)!.detectedDistrictName || '')).trim()">
                      {{ (getValidation(p.id)!.detectedDistrictName || getValidation(p.id)!.detectedCityName) }}
                    </span>
                  </template>
                </div>
                <div v-if="getValidation(p.id)?.placeLat" class="text-[11px] text-gray-400 dark:text-gray-500">
                  İsim eşleşmesi:
                  <span v-if="getValidation(p.id)!.placeDistanceKm != null">kayıtla {{ getValidation(p.id)!.placeDistanceKm }} km</span>
                  <span v-if="getValidation(p.id)!.placeVsAddressKm != null"> · adresle {{ getValidation(p.id)!.placeVsAddressKm }} km</span>
                </div>
              </div>
            </td>
            <td class="px-4 py-3 text-right">
              <div class="flex gap-1 justify-end flex-wrap">
                <a
                  v-if="p.latitude && p.longitude"
                  :href="`https://www.google.com/maps?q=${p.latitude},${p.longitude}`"
                  target="_blank"
                  class="text-xs px-2 py-1 border border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 rounded hover:bg-gray-100 dark:hover:bg-gray-800"
                  title="Google Maps'te aç"
                >🗺</a>
                <button
                  @click="validateOne(p)"
                  :disabled="rowLoading.has(p.id) || bulkRunning || !p.address"
                  class="text-xs px-2 py-1 bg-indigo-100 dark:bg-indigo-900/40 text-indigo-700 dark:text-indigo-300 rounded hover:bg-indigo-200 disabled:opacity-40 disabled:cursor-not-allowed"
                >Doğrula</button>
                <button
                  v-if="getValidation(p.id)?.geocodedLat"
                  @click="applyGeocodedCoords(p)"
                  class="text-xs px-2 py-1 bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-300 rounded hover:bg-green-200"
                >Adresle Güncelle</button>
                <button
                  v-if="getValidation(p.id)?.placeLat"
                  @click="applyPlaceCoords(p)"
                  class="text-xs px-2 py-1 bg-teal-100 dark:bg-teal-900/40 text-teal-700 dark:text-teal-300 rounded hover:bg-teal-200"
                  title="Proje adından Google Places ile bulunan koordinatı uygula"
                >İsimle Güncelle</button>
                <button
                  v-if="p.latitude || p.longitude"
                  @click="resetCoords(p)"
                  class="text-xs px-2 py-1 bg-red-50 dark:bg-red-900/30 text-red-600 dark:text-red-400 rounded hover:bg-red-100"
                >Sıfırla</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Kart listesi — Mobil -->
    <div class="sm:hidden space-y-3">
      <div v-if="loading" class="text-center text-gray-500 py-8">Yükleniyor...</div>
      <div
        v-for="p in filteredProjects"
        :key="p.id"
        class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 space-y-2"
      >
        <div class="flex items-start justify-between gap-2">
          <div>
            <span class="font-mono text-xs font-bold text-gray-800 dark:text-gray-200">{{ p.code }}</span>
            <p class="text-sm font-medium text-gray-700 dark:text-gray-300 mt-0.5">{{ p.name }}</p>
          </div>
          <StatusBadgeCoord :status="getValidation(p.id)?.status ?? resolveDefaultStatus(p)" />
        </div>
        <p class="text-xs text-gray-500 dark:text-gray-400 truncate">{{ p.address || 'Adres yok' }}</p>
        <div class="flex items-center justify-between text-xs">
          <span class="text-gray-400 font-mono">
            {{ p.latitude && p.longitude ? `${p.latitude.toFixed(4)}, ${p.longitude.toFixed(4)}` : '—' }}
          </span>
          <span v-if="getValidation(p.id)?.distanceKm != null" :class="distanceClass(getValidation(p.id)!.distanceKm)">
            {{ getValidation(p.id)!.distanceKm }} km
          </span>
        </div>
        <div class="flex gap-2 pt-1 flex-wrap">
          <button @click="validateOne(p)" :disabled="!p.address || rowLoading.has(p.id)" class="flex-1 text-xs py-1.5 bg-indigo-100 text-indigo-700 rounded disabled:opacity-40">Doğrula</button>
          <button v-if="getValidation(p.id)?.geocodedLat" @click="applyGeocodedCoords(p)" class="flex-1 text-xs py-1.5 bg-green-100 text-green-700 rounded">Güncelle</button>
          <a v-if="p.latitude && p.longitude" :href="`https://www.google.com/maps?q=${p.latitude},${p.longitude}`" target="_blank" class="px-3 text-xs py-1.5 border border-gray-300 rounded text-gray-500">🗺</a>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, defineComponent, h } from 'vue';
import projectService, { type ProjectCoordinateValidationDto } from '../services/projectService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const notificationStore = useNotificationStore();

// ─── State ───────────────────────────────────────────────────────────────────

interface Project {
  id: number;
  code: string;
  name: string;
  address?: string;
  latitude?: number;
  longitude?: number;
  isActive: boolean;
  locationNeedsRecheck?: boolean;
}

const loading = ref(false);
const allProjects = ref<Project[]>([]);
const validations = ref<Map<number, ProjectCoordinateValidationDto>>(new Map());
const rowLoading = ref<Set<number>>(new Set());
const selectedIds = ref<Set<number>>(new Set());
const activeTab = ref<string>('all');

const bulkRunning = ref(false);
const bulkDone = ref(0);
const bulkTotal = ref(0);
const bulkLabel = ref('İşlem');

// ─── Computed ────────────────────────────────────────────────────────────────

const activeProjects = computed(() => allProjects.value.filter(p => p.isActive));

const withCoordinates = computed(() =>
  activeProjects.value.filter(p => p.latitude != null && p.longitude != null).length
);
const withoutCoordinates = computed(() =>
  activeProjects.value.filter(p => p.latitude == null || p.longitude == null).length
);
const unvalidated = computed(() =>
  activeProjects.value.filter(p =>
    p.latitude != null && p.longitude != null && !validations.value.has(p.id)
  ).length
);

const noCoordinateWithAddress = computed(() =>
  activeProjects.value.filter(p => p.address && (p.latitude == null || p.longitude == null))
);

const estimatedMinutes = computed(() => {
  const remaining = bulkTotal.value - bulkDone.value;
  return Math.ceil(remaining / 10 / 60);
});

const tabs = computed(() => [
  { key: 'all',          label: 'Tümü',           count: activeProjects.value.length },
  { key: 'noCoord',      label: 'Koordinatsız',    count: withoutCoordinates.value },
  { key: 'unvalidated',  label: 'Doğrulanmamış',   count: unvalidated.value },
  { key: 'compatible',   label: 'Uyumlu',          count: countByStatus('Compatible') },
  { key: 'suspicious',   label: 'Şüpheli',         count: countByStatus('Suspicious') },
  { key: 'incompatible', label: 'Uyumsuz',         count: countByStatus('Incompatible') },
  { key: 'cityMismatch', label: 'İl/İlçe Uyumsuz', count: countCityMismatch() },
  { key: 'recheck',      label: 'Adres Değişti',   count: recheckCount() },
]);

const searchTerm = ref('');

const filteredProjects = computed(() => {
  let base: Project[];
  switch (activeTab.value) {
    case 'noCoord':
      base = activeProjects.value.filter(p => p.latitude == null || p.longitude == null);
      break;
    case 'unvalidated':
      base = activeProjects.value.filter(p =>
        p.latitude != null && p.longitude != null && !validations.value.has(p.id)
      );
      break;
    case 'compatible':
      base = activeProjects.value.filter(p => getValidation(p.id)?.status === 'Compatible');
      break;
    case 'suspicious':
      base = activeProjects.value.filter(p => getValidation(p.id)?.status === 'Suspicious');
      break;
    case 'incompatible':
      base = activeProjects.value.filter(p => getValidation(p.id)?.status === 'Incompatible');
      break;
    case 'cityMismatch':
      base = activeProjects.value.filter(p => getValidation(p.id)?.cityDistrictStatus === 'Mismatch');
      break;
    case 'recheck':
      base = activeProjects.value.filter(p => p.locationNeedsRecheck);
      break;
    default:
      base = activeProjects.value;
  }
  const term = searchTerm.value.trim().toLocaleLowerCase('tr');
  if (!term) return base;
  return base.filter(p =>
    (p.name ?? '').toLocaleLowerCase('tr').includes(term) ||
    (p.code ?? '').toLocaleLowerCase('tr').includes(term)
  );
});

const allSelected = computed(() =>
  filteredProjects.value.length > 0 &&
  filteredProjects.value.every(p => selectedIds.value.has(p.id))
);

const selectedWithGeocode = computed(() =>
  [...selectedIds.value]
    .map(id => ({ id, v: validations.value.get(id) }))
    .filter(x => x.v?.geocodedLat && x.v?.geocodedLng)
);

// ─── Helpers ─────────────────────────────────────────────────────────────────

const getValidation = (id: number) => validations.value.get(id);

const resolveDefaultStatus = (p: Project): string => {
  if (!p.address) return 'NoAddress';
  if (p.latitude == null || p.longitude == null) return 'NoCoordinate';
  return '';
};

const countByStatus = (status: string) =>
  activeProjects.value.filter(p => getValidation(p.id)?.status === status).length;

const countCityMismatch = () =>
  activeProjects.value.filter(p => getValidation(p.id)?.cityDistrictStatus === 'Mismatch').length;

const recheckCount = () =>
  activeProjects.value.filter(p => p.locationNeedsRecheck).length;

const sourceLabel = (s?: string) => {
  switch (s) {
    case 'DriverVerified': return 'Şoför doğruladı';
    case 'Geocoded':       return 'Geocode';
    case 'Manual':         return 'Manuel';
    default:               return '—';
  }
};
const sourceBadgeClass = (s?: string) => {
  switch (s) {
    case 'DriverVerified': return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300';
    case 'Geocoded':       return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-300';
    case 'Manual':         return 'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-300';
    default:               return 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400';
  }
};

const distanceClass = (km?: number | null) => {
  if (km == null) return 'text-gray-400';
  if (km < 5) return 'text-green-600 dark:text-green-400';
  if (km < 50) return 'text-yellow-600 dark:text-yellow-400';
  return 'text-red-600 dark:text-red-400';
};

// ─── Load ────────────────────────────────────────────────────────────────────

const load = async () => {
  loading.value = true;
  try {
    const data = await projectService.getProjects({ pageSize: 9999, showInactive: true });
    allProjects.value = data.items.map((p: any) => ({
      id: p.id,
      code: p.code,
      name: p.name,
      address: p.address,
      latitude: p.latitude,
      longitude: p.longitude,
      isActive: p.isActive,
      locationNeedsRecheck: p.locationNeedsRecheck,
    }));
  } catch (e) {
    notificationStore.add('Projeler yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
};

// ─── Tek Proje Doğrulama ─────────────────────────────────────────────────────

const validateOne = async (p: Project) => {
  rowLoading.value = new Set([...rowLoading.value, p.id]);
  try {
    const results = await projectService.validateCoordinates([p.id]);
    if (results.length > 0) {
      validations.value = new Map([...validations.value, [p.id, results[0] as ProjectCoordinateValidationDto]]);
    }
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Doğrulama başarısız.', 'error');
  } finally {
    const next = new Set(rowLoading.value);
    next.delete(p.id);
    rowLoading.value = next;
  }
};

// ─── Seçili Doğrulama ────────────────────────────────────────────────────────

const validateSelected = async () => {
  const targets = [...selectedIds.value];
  if (targets.length === 0) return;

  bulkRunning.value = true;
  bulkLabel.value = 'Doğrulama';
  bulkDone.value = 0;
  bulkTotal.value = targets.length;

  const batchSize = 20;
  for (let i = 0; i < targets.length; i += batchSize) {
    if (!bulkRunning.value) break;
    const batch = targets.slice(i, i + batchSize);
    batch.forEach(id => rowLoading.value = new Set([...rowLoading.value, id]));
    try {
      const results = await projectService.validateCoordinates(batch);
      const map = new Map(validations.value);
      results.forEach(r => map.set(r.projectId, r));
      validations.value = map;
    } catch {
      // Batch hatası — devam et
    } finally {
      const next = new Set(rowLoading.value);
      batch.forEach(id => next.delete(id));
      rowLoading.value = next;
    }
    bulkDone.value = Math.min(i + batchSize, targets.length);
  }

  bulkRunning.value = false;
  notificationStore.add(`${bulkDone.value} proje doğrulandı.`, 'success');
};

// ─── Tümünü Doğrula ──────────────────────────────────────────────────────────

const validateAll = async () => {
  const targets = filteredProjects.value.filter(p => p.address);
  if (targets.length === 0) {
    notificationStore.add('Doğrulanacak proje bulunamadı (adres gerekli).', 'warning');
    return;
  }

  bulkRunning.value = true;
  bulkLabel.value = 'Doğrulama';
  bulkDone.value = 0;
  bulkTotal.value = targets.length;

  const batchSize = 20;
  for (let i = 0; i < targets.length; i += batchSize) {
    if (!bulkRunning.value) break;
    const batch = targets.slice(i, i + batchSize);
    batch.forEach(p => rowLoading.value = new Set([...rowLoading.value, p.id]));
    try {
      const results = await projectService.validateCoordinates(batch.map(p => p.id));
      const map = new Map(validations.value);
      results.forEach(r => map.set(r.projectId, r));
      validations.value = map;
    } catch {
      // Batch hatası — devam et
    } finally {
      const next = new Set(rowLoading.value);
      batch.forEach(p => next.delete(p.id));
      rowLoading.value = next;
    }
    bulkDone.value = Math.min(i + batchSize, targets.length);
  }

  bulkRunning.value = false;
  notificationStore.add(`Toplu doğrulama tamamlandı. ${bulkDone.value} proje işlendi.`, 'success');
};

// ─── Seçili Koordinat Uygula ─────────────────────────────────────────────────

const applySelectedGeocodedCoords = async () => {
  const targets = selectedWithGeocode.value;
  if (targets.length === 0) return;

  let successCount = 0;
  for (const { id, v } of targets) {
    try {
      await projectService.updateLocation(id, v!.geocodedLat!, v!.geocodedLng!, v!.cityName, v!.districtName, 'Geocoded');
      const idx = allProjects.value.findIndex(x => x.id === id);
      if (idx !== -1) {
        allProjects.value[idx] = { ...(allProjects.value[idx] as Project), latitude: v!.geocodedLat!, longitude: v!.geocodedLng! };
      }
      markValidationApplied(id, v!.geocodedLat!, v!.geocodedLng!);
      successCount++;
    } catch {
      // Sessizce geç
    }
  }
  notificationStore.add(`${successCount} projenin koordinatı güncellendi.`, 'success');
};

// ─── Koordinat Güncelle ───────────────────────────────────────────────────────

const applyGeocodedCoords = async (p: Project) => {
  const v = getValidation(p.id);
  if (!v?.geocodedLat || !v?.geocodedLng) return;
  try {
    await projectService.updateLocation(p.id, v.geocodedLat, v.geocodedLng, v.cityName, v.districtName, 'Geocoded');
    // Yerel state güncelle
    const idx = allProjects.value.findIndex(x => x.id === p.id);
    if (idx !== -1) {
      allProjects.value[idx] = { ...(allProjects.value[idx] as Project), latitude: v.geocodedLat, longitude: v.geocodedLng } as Project;
    }
    // Durumu tazele — kayıtlı koordinat artık geocode ile aynı → Uyumlu (mesafe 0)
    markValidationApplied(p.id, v.geocodedLat, v.geocodedLng);
    notificationStore.add(`${p.code} koordinatı güncellendi.`, 'success');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Güncelleme başarısız.', 'error');
  }
};

// İsim (Places) ile bulunan koordinatı uygular
const applyPlaceCoords = async (p: Project) => {
  const v = getValidation(p.id);
  if (!v?.placeLat || !v?.placeLng) return;
  try {
    await projectService.updateLocation(p.id, v.placeLat, v.placeLng, undefined, undefined, 'Geocoded');
    const idx = allProjects.value.findIndex(x => x.id === p.id);
    if (idx !== -1) {
      allProjects.value[idx] = { ...(allProjects.value[idx] as Project), latitude: v.placeLat, longitude: v.placeLng } as Project;
    }
    markValidationApplied(p.id, v.placeLat, v.placeLng);
    notificationStore.add(`${p.code} koordinatı (isimden) güncellendi.`, 'success');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Güncelleme başarısız.', 'error');
  }
};

// Geocode koordinatı uygulandıktan sonra doğrulama sonucunu tazeler:
// kayıtlı koordinat artık geocode ile aynı olduğundan mesafe 0 / Uyumlu olur.
const markValidationApplied = (projectId: number, lat: number, lng: number) => {
  const map = new Map(validations.value);
  const cur = map.get(projectId);
  if (cur) {
    map.set(projectId, {
      ...cur,
      recordedLat: lat,
      recordedLng: lng,
      distanceKm: 0,
      status: 'Compatible',
      recordedSource: 'Geocoded',
    });
    validations.value = map;
  }
};

// ─── Koordinat Sıfırla ────────────────────────────────────────────────────────

const resetCoords = async (p: Project) => {
  try {
    await projectService.resetLocation(p.id);
    const idx = allProjects.value.findIndex(x => x.id === p.id);
    if (idx !== -1) {
      allProjects.value[idx] = { ...(allProjects.value[idx] as Project), latitude: undefined, longitude: undefined } as Project;
    }
    const map = new Map(validations.value);
    map.delete(p.id);
    validations.value = map;
    notificationStore.add(`${p.code} koordinatı sıfırlandı.`, 'success');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Sıfırlama başarısız.', 'error');
  }
};

// ─── Toplu Geocoding ─────────────────────────────────────────────────────────

const startBulkGeocode = async () => {
  const targets = noCoordinateWithAddress.value;
  if (targets.length === 0) return;

  bulkRunning.value = true;
  bulkLabel.value = 'Geocoding';
  bulkDone.value = 0;
  bulkTotal.value = targets.length;

  // 10'luk batch'ler halinde işle (her batch max 100ms × 10 = 1s)
  const batchSize = 10;
  for (let i = 0; i < targets.length; i += batchSize) {
    if (!bulkRunning.value) break;
    const batch = targets.slice(i, i + batchSize);
    try {
      const results = await projectService.validateCoordinates(batch.map(p => p.id));
      const map = new Map(validations.value);
      for (const r of results) {
        map.set(r.projectId, r);
        // Geocoded koordinat varsa otomatik kaydet
        if (r.geocodedLat && r.geocodedLng) {
          try {
            await projectService.updateLocation(r.projectId, r.geocodedLat, r.geocodedLng, r.cityName, r.districtName, 'Geocoded');
            const idx = allProjects.value.findIndex(x => x.id === r.projectId);
            if (idx !== -1) {
              allProjects.value[idx] = { ...(allProjects.value[idx] as Project), latitude: r.geocodedLat, longitude: r.geocodedLng } as Project;
            }
            // Kayıtlı koordinat artık geocode ile aynı → Uyumlu (mesafe 0)
            map.set(r.projectId, { ...r, recordedLat: r.geocodedLat, recordedLng: r.geocodedLng, distanceKm: 0, status: 'Compatible', recordedSource: 'Geocoded' });
          } catch {
            // Kayıt hatası sessizce geç
          }
        }
      }
      validations.value = map;
    } catch {
      // Batch hatası — devam et
    }
    bulkDone.value = Math.min(i + batchSize, targets.length);
  }

  bulkRunning.value = false;
  notificationStore.add(`Toplu geocoding tamamlandı. ${bulkDone.value} proje işlendi.`, 'success');
};

// ─── Seçim ───────────────────────────────────────────────────────────────────

const toggleSelect = (id: number) => {
  const s = new Set(selectedIds.value);
  if (s.has(id)) s.delete(id); else s.add(id);
  selectedIds.value = s;
};

const toggleAll = () => {
  if (allSelected.value) {
    selectedIds.value = new Set();
  } else {
    selectedIds.value = new Set(filteredProjects.value.map(p => p.id));
  }
};

// ─── Inline Badge Bileşeni ────────────────────────────────────────────────────

const StatusBadgeCoord = defineComponent({
  props: { status: String },
  setup(props) {
    return () => {
      const s = props.status;
      if (!s) return h('span');
      const map: Record<string, { label: string; cls: string }> = {
        Compatible:   { label: '✅ Uyumlu',      cls: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300' },
        Suspicious:   { label: '⚠️ Şüpheli',     cls: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-300' },
        Incompatible: { label: '❌ Uyumsuz',      cls: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300' },
        NoCoordinate: { label: '📍 Koordinatsız', cls: 'bg-orange-100 text-orange-800 dark:bg-orange-900/40 dark:text-orange-300' },
        NoAddress:    { label: '🚫 Adres Yok',    cls: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400' },
      };
      const cfg = map[s];
      if (!cfg) return h('span');
      return h('span', {
        class: `inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium ${cfg.cls}`
      }, cfg.label);
    };
  }
});

onMounted(load);
</script>
