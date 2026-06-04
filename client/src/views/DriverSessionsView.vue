<template>
  <div class="space-y-5">

    <!-- Header -->
    <div class="flex items-center justify-between">
      <h1 class="text-xl font-bold text-gray-900 dark:text-gray-100">Şoför Puantajı</h1>
    </div>

    <!-- Özet Kartlar -->
    <div class="grid grid-cols-3 gap-3">
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 text-center">
        <p class="text-2xl font-bold text-yellow-600 dark:text-yellow-400">{{ todayOpenCount }}</p>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Bugün Aktif</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 text-center">
        <p class="text-2xl font-bold text-green-600 dark:text-green-400">{{ todayClosedCount }}</p>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Bugün Tamamlanan</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 text-center">
        <p class="text-2xl font-bold text-blue-600 dark:text-blue-400">{{ weekAvgDuration }}</p>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Ort. Süre (dk)</p>
      </div>
    </div>

    <!-- Filtreler -->
    <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 space-y-3">
      <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <div>
          <label class="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">Başlangıç</label>
          <input type="date" v-model="filters.fromDate"
            class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-1.5 text-sm bg-white dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">Bitiş</label>
          <input type="date" v-model="filters.toDate"
            class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-1.5 text-sm bg-white dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">Durum</label>
          <select v-model="filters.status"
            class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-1.5 text-sm bg-white dark:bg-gray-800 dark:text-gray-100">
            <option value="">Tümü</option>
            <option value="0">Açık</option>
            <option value="1">Kapalı</option>
            <option value="2">Zorla Kapatılmış</option>
          </select>
        </div>
        <div class="flex items-end">
          <button @click="loadSessions" :disabled="loading"
            class="w-full py-1.5 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg disabled:opacity-50">
            Filtrele
          </button>
        </div>
      </div>
    </div>

    <!-- Tablo -->
    <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div v-if="loading" class="flex justify-center py-10">
        <div class="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
      </div>

      <div v-else-if="sessions.length === 0" class="text-center py-10 text-gray-500 dark:text-gray-400 text-sm">
        Sefer kaydı bulunamadı.
      </div>

      <div v-else class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Şoför</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Araç</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Başlangıç</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden md:table-cell">Bitiş</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden md:table-cell">Süre</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden lg:table-cell">Baş. KM</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden lg:table-cell">Bitiş KM</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden lg:table-cell">Gidilen KM</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Sevkiyat</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Durum</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">İşlem</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
            <template v-for="s in sessions" :key="String(s.id)">
            <tr
              :class="{
                'bg-yellow-50 dark:bg-yellow-900/10': s.status === 0,
                'bg-red-50 dark:bg-red-900/10': s.status === 2,
              }"
            >
              <td class="px-4 py-3 font-medium text-gray-900 dark:text-gray-100">{{ s.driverFullName }}</td>
              <td class="px-4 py-3 text-gray-700 dark:text-gray-300">{{ s.plateNumber }}</td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400">{{ fmtDateTime(s.startTime) }}</td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 hidden md:table-cell">{{ s.endTime ? fmtDateTime(s.endTime) : '—' }}</td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 hidden md:table-cell">{{ s.totalDurationMinutes != null ? s.totalDurationMinutes + ' dk' : '—' }}</td>
              <td class="px-4 py-3 text-right text-gray-600 dark:text-gray-400 hidden lg:table-cell">{{ s.startOdometerKm != null ? s.startOdometerKm.toLocaleString('tr-TR') : '—' }}</td>
              <td class="px-4 py-3 text-right text-gray-600 dark:text-gray-400 hidden lg:table-cell">{{ s.endOdometerKm != null ? s.endOdometerKm.toLocaleString('tr-TR') : '—' }}</td>
              <td class="px-4 py-3 text-right font-medium hidden lg:table-cell" :class="distanceKm(s) != null ? 'text-blue-600 dark:text-blue-400' : 'text-gray-400'">{{ distanceKm(s) != null ? distanceKm(s)!.toLocaleString('tr-TR') + ' km' : '—' }}</td>
              <td class="px-4 py-3">
                <button v-if="s.shipments && s.shipments.length"
                  @click="toggleExpand(s.id)"
                  class="inline-flex items-center gap-1 text-xs font-semibold text-blue-600 dark:text-blue-400 hover:underline">
                  {{ s.shipments.length }} sevkiyat
                  <span class="text-[10px]">{{ expandedId === s.id ? '▲' : '▼' }}</span>
                </button>
                <span v-else class="text-xs text-gray-400">—</span>
              </td>
              <td class="px-4 py-3">
                <span class="px-2 py-0.5 rounded-full text-xs font-semibold"
                  :class="{
                    'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/30 dark:text-yellow-300': s.status === 0,
                    'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300': s.status === 1,
                    'bg-red-100 text-red-800 dark:bg-red-900/30 dark:text-red-300': s.status === 2,
                  }">
                  {{ ['Açık', 'Kapalı', 'Zorla Kapatılmış'][s.status] }}
                </span>
              </td>
              <td class="px-4 py-3 text-right">
                <button
                  v-if="s.status === 0"
                  @click="openForceClose(s)"
                  class="text-xs text-red-600 hover:text-red-800 dark:text-red-400 dark:hover:text-red-300 font-medium"
                >Zorla Kapat</button>
              </td>
            </tr>
            <!-- Sefer manifesti (genişletilmiş) -->
            <tr v-if="expandedId === s.id">
              <td colspan="11" class="px-4 py-3 bg-gray-50 dark:bg-gray-800/50">
                <p class="text-xs text-gray-500 dark:text-gray-400 mb-2">
                  Bu seferde taşınan sevkiyatlar
                  <span v-if="s.startOdometerKm != null || s.endOdometerKm != null">
                    · KM: {{ s.startOdometerKm ?? '—' }} → {{ s.endOdometerKm ?? '—' }}
                  </span>
                </p>
                <ul class="space-y-1">
                  <li v-for="sh in s.shipments" :key="sh.id" class="flex justify-between gap-3 text-sm">
                    <span class="text-gray-800 dark:text-gray-200 truncate">{{ sh.projectName }}</span>
                    <span class="font-mono text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap">
                      {{ sh.irsaliyeNo || '—' }}<span v-if="sh.talepNo"> · {{ sh.talepNo }}</span> · {{ sh.status }}
                    </span>
                  </li>
                </ul>
              </td>
            </tr>
            </template>
          </tbody>
        </table>

        <!-- Pagination -->
        <div class="px-4 py-3 flex items-center justify-between border-t border-gray-200 dark:border-gray-700">
          <p class="text-xs text-gray-500 dark:text-gray-400">Toplam {{ totalCount }} kayıt</p>
          <div class="flex gap-2">
            <button @click="prevPage" :disabled="pageNumber === 1 || loading"
              class="px-3 py-1 text-xs border dark:border-gray-700 rounded disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-gray-800">‹ Önceki</button>
            <span class="px-3 py-1 text-xs">{{ pageNumber }} / {{ totalPages }}</span>
            <button @click="nextPage" :disabled="pageNumber >= totalPages || loading"
              class="px-3 py-1 text-xs border dark:border-gray-700 rounded disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-gray-800">Sonraki ›</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Force Close Modal -->
    <div v-if="forceCloseTarget" class="fixed inset-0 bg-gray-500 bg-opacity-75 flex items-center justify-center z-50">
      <div class="bg-white dark:bg-gray-900 rounded-xl p-6 max-w-sm w-full mx-4">
        <h3 class="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-3">Seferi Zorla Kapat</h3>
        <p class="text-sm text-gray-600 dark:text-gray-400 mb-4">
          <strong>{{ forceCloseTarget.driverFullName }}</strong> — {{ forceCloseTarget.plateNumber }}
        </p>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Not <span class="text-red-500">*</span></label>
          <textarea v-model="forceCloseNotes" rows="3"
            class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100"
            placeholder="Neden zorla kapatılıyor?"></textarea>
        </div>
        <div class="mt-4 flex justify-end gap-2">
          <button @click="forceCloseTarget = null; forceCloseNotes = ''"
            class="px-4 py-2 border dark:border-gray-700 rounded-lg text-sm text-gray-700 dark:text-gray-300">İptal</button>
          <button @click="submitForceClose" :disabled="!forceCloseNotes.trim() || forceCloseLoading"
            class="px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg text-sm font-medium disabled:opacity-50">
            {{ forceCloseLoading ? 'İşleniyor...' : 'Zorla Kapat' }}
          </button>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import apiClient from '../services/apiClient';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';

const notificationStore = useNotificationStore();

interface SessionDto {
  id: string;
  driverId: number;
  driverFullName: string;
  vehicleId: number;
  plateNumber: string;
  startTime: string;
  endTime: string | null;
  totalDurationMinutes: number | null;
  startLatitude: number;
  startLongitude: number;
  endLatitude: number | null;
  endLongitude: number | null;
  status: 0 | 1 | 2;
  notes: string | null;
  startOdometerKm: number | null;
  endOdometerKm: number | null;
  shipments: SessionShipment[];
}

interface SessionShipment {
  id: number;
  projectName: string;
  talepNo: string | null;
  irsaliyeNo: string | null;
  status: string;
}

const today = new Date().toISOString().slice(0, 10);
const weekAgo = new Date(Date.now() - 7 * 86400000).toISOString().slice(0, 10);

const filters = ref({ fromDate: weekAgo, toDate: today, status: '' });
const sessions = ref<SessionDto[]>([]);
const totalCount = ref(0);
const pageNumber = ref(1);
const pageSize = 50;
const loading = ref(false);

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize)));

const expandedId = ref<string | null>(null);
const toggleExpand = (id: string) => { expandedId.value = expandedId.value === id ? null : id; };

// Özet hesapları
const todayOpenCount = computed(() =>
  sessions.value.filter(s => s.status === 0 && s.startTime.slice(0, 10) === today).length);
const todayClosedCount = computed(() =>
  sessions.value.filter(s => s.status === 1 && s.startTime.slice(0, 10) === today).length);
const weekAvgDuration = computed(() => {
  const closed = sessions.value.filter(s => s.status === 1 && s.totalDurationMinutes != null);
  if (closed.length === 0) return '—';
  const avg = closed.reduce((sum, s) => sum + (s.totalDurationMinutes ?? 0), 0) / closed.length;
  return Math.round(avg);
});

const loadSessions = async () => {
  loading.value = true;
  try {
    const params: Record<string, string | number> = {
      fromDate: filters.value.fromDate,
      toDate: filters.value.toDate,
      pageNumber: pageNumber.value,
      pageSize,
    };
    if (filters.value.status !== '') params.status = Number(filters.value.status);
    const res = await apiClient.get<{ items: SessionDto[]; totalCount: number }>('/admin/driver-sessions', { params });
    sessions.value = res.data.items;
    totalCount.value = res.data.totalCount;
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Veriler yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
};

const prevPage = () => { if (pageNumber.value > 1) { pageNumber.value--; loadSessions(); } };
const nextPage = () => { if (pageNumber.value < totalPages.value) { pageNumber.value++; loadSessions(); } };

// Force Close
const forceCloseTarget = ref<SessionDto | null>(null);
const forceCloseNotes = ref('');
const forceCloseLoading = ref(false);

const openForceClose = (s: SessionDto) => {
  forceCloseTarget.value = s;
  forceCloseNotes.value = '';
};

const submitForceClose = async () => {
  if (!forceCloseTarget.value) return;
  forceCloseLoading.value = true;
  try {
    await apiClient.post(`/admin/driver-sessions/${forceCloseTarget.value.id}/force-close`, {
      notes: forceCloseNotes.value,
    });
    notificationStore.add('Sefer kapatıldı.', 'success');
    forceCloseTarget.value = null;
    forceCloseNotes.value = '';
    await loadSessions();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
  } finally {
    forceCloseLoading.value = false;
  }
};

// Gidilen toplam km — başlangıç ve bitiş km'si girildiyse fark
const distanceKm = (s: SessionDto): number | null =>
  s.startOdometerKm != null && s.endOdometerKm != null && s.endOdometerKm >= s.startOdometerKm
    ? s.endOdometerKm - s.startOdometerKm
    : null;

const fmtDateTime = (iso: string) =>
  new Date(iso).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });

onMounted(loadSessions);
</script>
