<template>
  <div class="space-y-4" style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">
    <h1 class="text-xl font-extrabold text-gray-900 dark:text-white tracking-tight">Şoför Puantajı</h1>

    <!-- Özet -->
    <div class="grid grid-cols-3 gap-3">
      <ReportStat label="Bugün Aktif" :value="todayOpenCount" tone="amber" />
      <ReportStat label="Bugün Tamamlanan" :value="todayClosedCount" tone="green" />
      <ReportStat label="Ort. Süre (dk)" :value="weekAvgDuration" tone="blue" />
    </div>

    <!-- Filtreler -->
    <ReportToolbar
      v-model:start-date="filters.fromDate"
      v-model:end-date="filters.toDate"
      :loading="loading"
      :can-export="false"
      @apply="loadSessions"
    >
      <template #filters>
        <div>
          <label class="block text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-white/55 mb-1.5">Durum</label>
          <select v-model="filters.status" :class="inputCls">
            <option value="">Tümü</option>
            <option value="0">Açık</option>
            <option value="1">Kapalı</option>
            <option value="2">Zorla Kapatılmış</option>
          </select>
        </div>
      </template>
    </ReportToolbar>

    <!-- Tablo -->
    <div class="rounded-2xl border border-gray-200 dark:border-white/10 bg-white dark:bg-[#0f2238] overflow-hidden">
      <div v-if="loading" class="flex justify-center py-10"><div class="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></div></div>
      <div v-else-if="sessions.length === 0" class="text-center py-10 text-gray-400 dark:text-white/40 text-sm">Sefer kaydı bulunamadı.</div>
      <div v-else class="overflow-x-auto">
        <table class="min-w-full text-sm">
          <thead>
            <tr class="bg-gray-50 dark:bg-white/5 text-gray-500 dark:text-white/55">
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Şoför</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Araç</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Başlangıç</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide hidden md:table-cell">Bitiş</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide hidden md:table-cell">Süre</th>
              <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide hidden lg:table-cell">Baş. KM</th>
              <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide hidden lg:table-cell">Bitiş KM</th>
              <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide hidden lg:table-cell">Gidilen</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Sevkiyat</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Durum</th>
              <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">İşlem</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-white/5">
            <template v-for="s in sessions" :key="String(s.id)">
              <tr :class="s.status === 0 ? 'bg-amber-50/40 dark:bg-amber-500/5' : s.status === 2 ? 'bg-red-50/40 dark:bg-red-500/5' : ''">
                <td class="px-4 py-3 font-semibold text-gray-900 dark:text-white">{{ s.driverFullName }}</td>
                <td class="px-4 py-3 text-gray-700 dark:text-white/75 font-mono text-xs">{{ s.plateNumber }}</td>
                <td class="px-4 py-3 text-gray-600 dark:text-white/65">{{ fmtDateTime(s.startTime) }}</td>
                <td class="px-4 py-3 text-gray-600 dark:text-white/65 hidden md:table-cell">{{ s.endTime ? fmtDateTime(s.endTime) : '—' }}</td>
                <td class="px-4 py-3 text-gray-600 dark:text-white/65 hidden md:table-cell">{{ s.totalDurationMinutes != null ? s.totalDurationMinutes + ' dk' : '—' }}</td>
                <td class="px-4 py-3 text-right text-gray-600 dark:text-white/65 hidden lg:table-cell">{{ s.startOdometerKm != null ? s.startOdometerKm.toLocaleString('tr-TR') : '—' }}</td>
                <td class="px-4 py-3 text-right text-gray-600 dark:text-white/65 hidden lg:table-cell">{{ s.endOdometerKm != null ? s.endOdometerKm.toLocaleString('tr-TR') : '—' }}</td>
                <td class="px-4 py-3 text-right font-bold hidden lg:table-cell" :class="distanceKm(s) != null ? 'text-blue-600 dark:text-blue-300' : 'text-gray-400 dark:text-white/40'">{{ distanceKm(s) != null ? distanceKm(s)?.toLocaleString('tr-TR') + ' km' : '—' }}</td>
                <td class="px-4 py-3">
                  <button v-if="s.shipments && s.shipments.length" @click="toggleExpand(s.id)" class="inline-flex items-center gap-1 text-xs font-bold text-blue-600 dark:text-blue-300 hover:underline">
                    {{ s.shipments.length }} sevkiyat <span class="text-[10px]">{{ expandedId === s.id ? '▲' : '▼' }}</span>
                  </button>
                  <span v-else class="text-xs text-gray-400 dark:text-white/40">—</span>
                </td>
                <td class="px-4 py-3"><span class="px-2 py-0.5 rounded-full text-[11px] font-bold" :class="statusChip(s.status)">{{ ['Açık', 'Kapalı', 'Zorla Kapatılmış'][s.status] }}</span></td>
                <td class="px-4 py-3 text-right">
                  <button v-if="s.status === 0" @click="openForceClose(s)" class="text-xs font-bold text-red-600 hover:text-red-700 dark:text-red-300">Zorla Kapat</button>
                </td>
              </tr>
              <tr v-if="expandedId === s.id">
                <td colspan="11" class="px-4 py-3 bg-gray-50 dark:bg-white/5">
                  <p class="text-xs text-gray-500 dark:text-white/55 mb-2">Bu seferde taşınan sevkiyatlar
                    <span v-if="s.startOdometerKm != null || s.endOdometerKm != null">· KM: {{ s.startOdometerKm ?? '—' }} → {{ s.endOdometerKm ?? '—' }}</span>
                  </p>
                  <ul class="space-y-1">
                    <li v-for="sh in s.shipments" :key="sh.id" class="flex justify-between gap-3 text-sm">
                      <span class="text-gray-800 dark:text-white/85 truncate">{{ sh.projectName }}</span>
                      <span class="font-mono text-xs text-gray-500 dark:text-white/55 whitespace-nowrap">{{ sh.irsaliyeNo || '—' }}<span v-if="sh.talepNo"> · {{ sh.talepNo }}</span> · {{ sh.status }}</span>
                    </li>
                  </ul>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
        <div class="px-4 py-3 flex items-center justify-between border-t border-gray-100 dark:border-white/10">
          <p class="text-xs text-gray-500 dark:text-white/55">Toplam {{ totalCount }} kayıt</p>
          <div class="flex items-center gap-2">
            <button @click="prevPage" :disabled="pageNumber === 1 || loading" class="px-3 py-1 text-xs font-semibold rounded-lg border border-gray-200 dark:border-white/15 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-white/5">‹ Önceki</button>
            <span class="px-2 text-xs text-gray-500 dark:text-white/55">{{ pageNumber }} / {{ totalPages }}</span>
            <button @click="nextPage" :disabled="pageNumber >= totalPages || loading" class="px-3 py-1 text-xs font-semibold rounded-lg border border-gray-200 dark:border-white/15 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-white/5">Sonraki ›</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Force Close Modal -->
    <div v-if="forceCloseTarget" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div class="bg-white dark:bg-[#0f2238] rounded-2xl p-6 max-w-sm w-full border border-gray-200 dark:border-white/10">
        <h3 class="text-lg font-extrabold text-gray-900 dark:text-white mb-3">Seferi Zorla Kapat</h3>
        <p class="text-sm text-gray-600 dark:text-white/65 mb-4"><strong class="text-gray-900 dark:text-white">{{ forceCloseTarget.driverFullName }}</strong> — {{ forceCloseTarget.plateNumber }}</p>
        <label class="block text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-white/55 mb-1.5">Not <span class="text-red-500">*</span></label>
        <textarea v-model="forceCloseNotes" rows="3" :class="inputCls + ' w-full !h-auto py-2'" placeholder="Neden zorla kapatılıyor?"></textarea>
        <div class="mt-4 flex justify-end gap-2">
          <button @click="forceCloseTarget = null; forceCloseNotes = ''" class="px-4 py-2 rounded-xl border border-gray-200 dark:border-white/15 text-sm font-semibold text-gray-700 dark:text-white/80">İptal</button>
          <button @click="submitForceClose" :disabled="!forceCloseNotes.trim() || forceCloseLoading" class="px-4 py-2 rounded-xl bg-red-600 hover:bg-red-700 text-white text-sm font-bold disabled:opacity-50">{{ forceCloseLoading ? 'İşleniyor…' : 'Zorla Kapat' }}</button>
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
import ReportToolbar from '../components/reports/ReportToolbar.vue';
import ReportStat from '../components/reports/ReportStat.vue';

const notificationStore = useNotificationStore();

interface SessionShipment { id: number; projectName: string; talepNo: string | null; irsaliyeNo: string | null; status: string; }
interface SessionDto {
  id: string; driverId: number; driverFullName: string; vehicleId: number; plateNumber: string;
  startTime: string; endTime: string | null; totalDurationMinutes: number | null;
  startLatitude: number; startLongitude: number; endLatitude: number | null; endLongitude: number | null;
  status: 0 | 1 | 2; notes: string | null; startOdometerKm: number | null; endOdometerKm: number | null; shipments: SessionShipment[];
}

const inputCls =
  'h-[42px] px-3 rounded-xl border border-gray-200 dark:border-white/15 bg-white dark:bg-[#13294a] ' +
  'text-sm text-gray-800 dark:text-white outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/12 transition-all';

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

const todayOpenCount = computed(() => sessions.value.filter(s => s.status === 0 && s.startTime.slice(0, 10) === today).length);
const todayClosedCount = computed(() => sessions.value.filter(s => s.status === 1 && s.startTime.slice(0, 10) === today).length);
const weekAvgDuration = computed(() => {
  const closed = sessions.value.filter(s => s.status === 1 && s.totalDurationMinutes != null);
  if (closed.length === 0) return '—';
  return Math.round(closed.reduce((sum, s) => sum + (s.totalDurationMinutes ?? 0), 0) / closed.length);
});

function statusChip(status: number) {
  if (status === 0) return 'bg-amber-100 text-amber-700 dark:bg-amber-500/15 dark:text-amber-300';
  if (status === 1) return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-300';
  return 'bg-red-100 text-red-700 dark:bg-red-500/15 dark:text-red-300';
}

const loadSessions = async () => {
  loading.value = true;
  try {
    const params: Record<string, string | number> = { fromDate: filters.value.fromDate, toDate: filters.value.toDate, pageNumber: pageNumber.value, pageSize };
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

const forceCloseTarget = ref<SessionDto | null>(null);
const forceCloseNotes = ref('');
const forceCloseLoading = ref(false);

const openForceClose = (s: SessionDto) => { forceCloseTarget.value = s; forceCloseNotes.value = ''; };

const submitForceClose = async () => {
  if (!forceCloseTarget.value) return;
  forceCloseLoading.value = true;
  try {
    await apiClient.post(`/admin/driver-sessions/${forceCloseTarget.value.id}/force-close`, { notes: forceCloseNotes.value });
    notificationStore.add('Sefer kapatıldı.', 'success');
    forceCloseTarget.value = null; forceCloseNotes.value = '';
    await loadSessions();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
  } finally {
    forceCloseLoading.value = false;
  }
};

const distanceKm = (s: SessionDto): number | null =>
  s.startOdometerKm != null && s.endOdometerKm != null && s.endOdometerKm >= s.startOdometerKm ? s.endOdometerKm - s.startOdometerKm : null;

const fmtDateTime = (iso: string) =>
  new Date(iso).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });

onMounted(loadSessions);
</script>
