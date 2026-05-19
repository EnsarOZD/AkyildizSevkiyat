<template>
  <div class="space-y-4">
    <div class="bg-white dark:bg-gray-900 p-4 rounded shadow flex flex-wrap gap-4 items-end">
      <div>
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Başlangıç</label>
        <input v-model="filter.startDate" type="date" class="border dark:border-gray-700 p-2 rounded dark:bg-gray-800 dark:text-gray-100" />
      </div>
      <div>
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Bitiş</label>
        <input v-model="filter.endDate" type="date" class="border dark:border-gray-700 p-2 rounded dark:bg-gray-800 dark:text-gray-100" />
      </div>
      <button @click="loadPerformance" class="bg-indigo-600 text-white px-4 py-2 rounded hover:bg-indigo-700 mt-auto">Filtrele</button>
      <button v-if="perfData" @click="exportPerformance" class="md:ml-auto w-full md:w-auto justify-center flex items-center gap-1.5 px-4 py-2 text-sm border border-green-600 text-green-700 dark:text-green-400 dark:border-green-600 rounded hover:bg-green-50 dark:hover:bg-green-900/20 transition-colors">
        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" /></svg>
        Excel İndir
      </button>
    </div>

    <template v-if="perfData">
      <!-- KPI cards -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div class="bg-white dark:bg-gray-900 rounded shadow p-4 text-center">
          <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ perfData.totalDelivered }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam Teslim</p>
        </div>
        <div class="bg-green-50 dark:bg-green-900/20 rounded shadow p-4 text-center">
          <p class="text-3xl font-bold text-green-700 dark:text-green-400">{{ perfData.onTime }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Zamanında</p>
        </div>
        <div class="bg-red-50 dark:bg-red-900/20 rounded shadow p-4 text-center">
          <p class="text-3xl font-bold text-red-700 dark:text-red-400">{{ perfData.late }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Gecikmiş</p>
        </div>
        <div class="rounded shadow p-4 text-center" :class="perfData.onTimeRate >= 80 ? 'bg-green-50 dark:bg-green-900/20' : 'bg-red-50 dark:bg-red-900/20'">
          <p class="text-3xl font-bold" :class="perfData.onTimeRate >= 80 ? 'text-green-700 dark:text-green-400' : 'text-red-700 dark:text-red-400'">
            %{{ perfData.onTimeRate }}
          </p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Zamanında Teslimat Oranı</p>
        </div>
      </div>

      <!-- By zone -->
      <div v-if="perfData.byZone.length > 0" class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
        <div class="px-4 py-3 border-b dark:border-gray-700">
          <h3 class="font-medium text-gray-900 dark:text-gray-100">Bölge Bazında Performans</h3>
        </div>
        <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Bölge</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Toplam</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Zamanında</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Gecikmiş</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Oran</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
            <tr v-for="z in perfData.byZone" :key="z.zoneName" class="hover:bg-gray-50 dark:hover:bg-gray-800">
              <td class="px-4 py-3 font-medium text-gray-900 dark:text-gray-100">{{ z.zoneName }}</td>
              <td class="px-4 py-3 text-right text-gray-600 dark:text-gray-400">{{ z.total }}</td>
              <td class="px-4 py-3 text-right text-green-700 dark:text-green-400 font-medium">{{ z.onTime }}</td>
              <td class="px-4 py-3 text-right text-red-700 dark:text-red-400 font-medium">{{ z.late }}</td>
              <td class="px-4 py-3 text-right font-semibold" :class="z.onTimeRate >= 80 ? 'text-green-700 dark:text-green-400' : 'text-red-700 dark:text-red-400'">
                %{{ z.onTimeRate }}
              </td>
            </tr>
          </tbody>
        </table>
        </div>
      </div>

      <!-- Detail rows -->
      <div class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
        <div class="px-4 py-3 border-b dark:border-gray-700 flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3">
          <h3 class="font-medium text-gray-900 dark:text-gray-100">Teslim Detayları ({{ perfData.rows.length }})</h3>
          <label class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400 cursor-pointer mt-2 sm:mt-0">
            <input v-model="perfLateOnly" type="checkbox" class="rounded" />
            Sadece gecikmişler
          </label>
        </div>
        <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Proje</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Bölge</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Planlanan</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Teslim Edildi</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Gecikme</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
            <tr v-if="filteredPerfRows.length === 0"><td colspan="5" class="px-4 py-6 text-center text-gray-400">Kayıt bulunamadı.</td></tr>
            <tr v-for="r in filteredPerfRows" :key="r.id" class="hover:bg-gray-50 dark:hover:bg-gray-800" :class="r.isLate ? 'bg-red-50/30 dark:bg-red-900/10' : ''">
              <td class="px-4 py-3 font-medium text-gray-900 dark:text-gray-100">{{ r.projectName }}</td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400">{{ r.zoneName || '-' }}</td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400">{{ fmtDate(r.deliveryDate) }}</td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400">{{ fmtDate(r.deliveredAt) }}</td>
              <td class="px-4 py-3 text-right font-semibold" :class="r.isLate ? 'text-red-600 dark:text-red-400' : 'text-green-600 dark:text-green-400'">
                {{ r.isLate ? `+${r.delayDays} gün` : 'Zamanında' }}
              </td>
            </tr>
          </tbody>
        </table>
        </div>
      </div>
    </template>
    <div v-if="!perfData && !loading" class="text-center py-10 text-gray-400">Tarih aralığı seçip "Filtrele" butonuna tıklayın.</div>
    <div v-if="loading" class="text-center py-10 text-gray-400">Yükleniyor...</div>
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
