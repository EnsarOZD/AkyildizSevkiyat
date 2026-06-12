<template>
  <div class="space-y-4" style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">
    <ReportToolbar
      v-model:start-date="filter.startDate"
      v-model:end-date="filter.endDate"
      :loading="loading"
      :can-export="!!perfData"
      @apply="loadPerformance"
      @export="exportPerformance"
    />

    <template v-if="perfData">
      <!-- KPI -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-3">
        <ReportStat label="Toplam Teslim" :value="perfData.totalDelivered" />
        <ReportStat label="Zamanında" :value="perfData.onTime" tone="green" />
        <ReportStat label="Gecikmiş" :value="perfData.late" tone="red" />
        <ReportStat label="Zamanında Oranı" :value="`%${perfData.onTimeRate}`" :tone="perfData.onTimeRate >= 80 ? 'green' : 'red'" />
      </div>

      <!-- Bölge bazında -->
      <div v-if="perfData.byZone.length > 0" class="rounded-2xl border border-gray-200 dark:border-white/10 bg-white dark:bg-[#0f2238] overflow-hidden">
        <div class="px-4 py-3.5 border-b border-gray-100 dark:border-white/5">
          <h3 class="text-[14px] font-bold text-gray-900 dark:text-white">Bölge Bazında Performans</h3>
        </div>
        <div class="overflow-x-auto">
          <table class="min-w-full text-sm">
            <thead>
              <tr class="bg-gray-50 dark:bg-white/5 text-gray-500 dark:text-white/55">
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Bölge</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Toplam</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Zamanında</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Gecikmiş</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Oran</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-white/5">
              <tr v-for="z in perfData.byZone" :key="z.zoneName" class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
                <td class="px-4 py-3 font-semibold text-gray-900 dark:text-white">{{ z.zoneName }}</td>
                <td class="px-4 py-3 text-right text-gray-600 dark:text-white/65">{{ z.total }}</td>
                <td class="px-4 py-3 text-right text-emerald-600 dark:text-emerald-300 font-bold">{{ z.onTime }}</td>
                <td class="px-4 py-3 text-right text-red-600 dark:text-red-300 font-bold">{{ z.late }}</td>
                <td class="px-4 py-3 text-right font-bold" :class="z.onTimeRate >= 80 ? 'text-emerald-600 dark:text-emerald-300' : 'text-red-600 dark:text-red-300'">%{{ z.onTimeRate }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Teslim detayları -->
      <div class="rounded-2xl border border-gray-200 dark:border-white/10 bg-white dark:bg-[#0f2238] overflow-hidden">
        <div class="px-4 py-3.5 border-b border-gray-100 dark:border-white/5 flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <h3 class="text-[14px] font-bold text-gray-900 dark:text-white">Teslim Detayları ({{ perfData.rows.length }})</h3>
          <label class="flex items-center gap-2 text-[13px] text-gray-500 dark:text-white/55 cursor-pointer">
            <input v-model="perfLateOnly" type="checkbox" class="rounded accent-blue-600" />
            Sadece gecikmişler
          </label>
        </div>
        <div class="overflow-x-auto">
          <table class="min-w-full text-sm">
            <thead>
              <tr class="bg-gray-50 dark:bg-white/5 text-gray-500 dark:text-white/55">
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Proje</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Bölge</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Planlanan</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Teslim Edildi</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Gecikme</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-white/5">
              <tr v-if="filteredPerfRows.length === 0"><td colspan="5" class="px-4 py-8 text-center text-gray-400 dark:text-white/40">Kayıt bulunamadı.</td></tr>
              <tr v-for="r in filteredPerfRows" :key="r.id" class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors" :class="r.isLate ? 'bg-red-50/40 dark:bg-red-500/5' : ''">
                <td class="px-4 py-3 font-semibold text-gray-900 dark:text-white">{{ r.projectName }}</td>
                <td class="px-4 py-3 text-gray-500 dark:text-white/55">{{ r.zoneName || '—' }}</td>
                <td class="px-4 py-3 text-gray-600 dark:text-white/65">{{ fmtDate(r.deliveryDate) }}</td>
                <td class="px-4 py-3 text-gray-600 dark:text-white/65">{{ fmtDate(r.deliveredAt) }}</td>
                <td class="px-4 py-3 text-right font-bold" :class="r.isLate ? 'text-red-600 dark:text-red-300' : 'text-emerald-600 dark:text-emerald-300'">
                  {{ r.isLate ? `+${r.delayDays} gün` : 'Zamanında' }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>

    <div v-if="!perfData && !loading" class="text-center py-12 text-gray-400 dark:text-white/40">Tarih aralığı seçip "Filtrele" butonuna tıklayın.</div>
    <div v-if="loading" class="flex justify-center py-12"><div class="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></div></div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import reportService from '../../services/reportService';
import type { ShipmentPerformanceDto } from '../../services/reportService';
import { ApiErrorUtils } from '../../utils/apiError';
import { formatDate as fmtDate } from '../../utils/dateFormat';
import { useNotification } from '../../composables/useNotification';
import { exportToExcel } from '../../utils/exportExcel';
import ReportToolbar from './ReportToolbar.vue';
import ReportStat from './ReportStat.vue';

const { notify } = useNotification();

const today = new Date().toISOString().slice(0, 10);
const monthAgo = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10);

const filter = ref({ startDate: monthAgo, endDate: today });
const perfData = ref<ShipmentPerformanceDto | null>(null);
const loading = ref(false);
const perfLateOnly = ref(false);

const filteredPerfRows = computed(() =>
  perfLateOnly.value ? (perfData.value?.rows.filter(r => r.isLate) ?? []) : (perfData.value?.rows ?? [])
);

const loadPerformance = async () => {
  loading.value = true;
  try {
    perfData.value = await reportService.getShipmentPerformance(filter.value);
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Rapor yüklenemedi.'));
  } finally {
    loading.value = false;
  }
};

const exportPerformance = () => {
  if (!perfData.value) return;
  const rows = perfData.value.rows.map(r => ({
    'Proje': r.projectName,
    'Bölge': r.zoneName || '',
    'Talep No': r.talepNo || '',
    'Planlanan Tarih': fmtDate(r.deliveryDate),
    'Teslim Tarihi': fmtDate(r.deliveredAt),
    'Gecikme (Gün)': r.delayDays,
    'Durum': r.isLate ? 'Gecikmiş' : 'Zamanında',
    'Sürücü': r.driverName || '',
  }));
  exportToExcel(rows, 'Performans', `teslimat-performansi-${filter.value.startDate}_${filter.value.endDate}`);
};
</script>
