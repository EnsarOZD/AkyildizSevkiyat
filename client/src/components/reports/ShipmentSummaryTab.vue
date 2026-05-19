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
      <button @click="loadData" class="bg-indigo-600 text-white px-4 py-2 rounded hover:bg-indigo-700">Filtrele</button>
      <button v-if="summary" @click="exportExcel" class="ml-auto flex items-center gap-1.5 px-4 py-2 text-sm border border-green-600 text-green-700 dark:text-green-400 dark:border-green-600 rounded hover:bg-green-50 dark:hover:bg-green-900/20 transition-colors">
        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" /></svg>
        Excel İndir
      </button>
    </div>

    <div v-if="summary" class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-8 gap-3">
      <div class="bg-white dark:bg-gray-900 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-gray-900 dark:text-gray-100">{{ summary.total }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam</p></div>
      <div class="bg-gray-50 dark:bg-gray-800 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-gray-600 dark:text-gray-400">{{ summary.created }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Taslak</p></div>
      <div class="bg-yellow-50 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-yellow-700">{{ summary.assignedToWarehouse }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Depoda</p></div>
      <div class="bg-blue-50 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-blue-700">{{ summary.picking }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplanıyor</p></div>
      <div class="bg-purple-50 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-purple-700">{{ summary.readyForDispatch }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Hazır</p></div>
      <div class="bg-indigo-50 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-indigo-700">{{ summary.assignedToVehicle }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Araçta</p></div>
      <div class="bg-green-50 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-green-700">{{ summary.delivered }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Teslim Edildi</p></div>
      <div class="bg-red-50 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-red-700">{{ summary.cancelled }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">İptal</p></div>
    </div>

    <div v-if="summary" class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
      <div class="overflow-x-auto">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">#</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Proje</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden sm:table-cell">Bölge</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Durum</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden sm:table-cell">Teslim Tarihi</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden lg:table-cell">Talep No</th>
            <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden lg:table-cell">Kalem</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-if="summary.rows.length === 0"><td colspan="7" class="px-4 py-6 text-center text-gray-400">Bu tarih aralığında sevkiyat bulunamadı.</td></tr>
          <tr v-for="row in summary.rows" :key="row.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
            <td class="px-4 py-3 font-mono text-xs text-gray-500 dark:text-gray-400">{{ row.id }}</td>
            <td class="px-4 py-3 font-medium text-gray-900 dark:text-gray-100">{{ row.projectName }}</td>
            <td class="px-4 py-3 text-gray-500 dark:text-gray-400 hidden sm:table-cell">{{ row.zoneName || '-' }}</td>
            <td class="px-4 py-3"><span class="px-2 py-0.5 text-xs rounded-full font-semibold" :class="statusClass(row.status)">{{ row.status }}</span></td>
            <td class="px-4 py-3 text-gray-600 dark:text-gray-400 hidden sm:table-cell">{{ fmtDate(row.deliveryDate) }}</td>
            <td class="px-4 py-3 text-gray-500 dark:text-gray-400 text-xs hidden lg:table-cell">{{ row.talepNo || '-' }}</td>
            <td class="px-4 py-3 text-right text-gray-600 dark:text-gray-400 hidden lg:table-cell">{{ row.lineCount }}</td>
          </tr>
        </tbody>
      </table>
      </div>
    </div>
    <div v-if="!summary && !loading" class="text-center py-10 text-gray-400">Tarih aralığı seçip "Filtrele" butonuna tıklayın.</div>
    <div v-if="loading" class="text-center py-10 text-gray-400">Yükleniyor...</div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import reportService from '../../services/reportService';
import type { ShipmentSummaryDto } from '../../services/reportService';
import { ApiErrorUtils } from '../../utils/apiError';
import { useNotification } from '../../composables/useNotification';
import { exportToExcel } from '../../utils/exportExcel';

const { notify } = useNotification();

const today = new Date().toISOString().slice(0, 10);
const monthAgo = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10);

const filter = ref({ startDate: monthAgo, endDate: today });
const summary = ref<ShipmentSummaryDto | null>(null);
const loading = ref(false);

const fmtDate = (d: string) => new Date(d).toLocaleDateString('tr-TR');

const statusClass = (status: string) => {
  const map: Record<string, string> = {
    Created: 'bg-gray-100 text-gray-800',
    AssignedToWarehouse: 'bg-yellow-100 text-yellow-800',
    Picking: 'bg-blue-100 text-blue-800',
    ReadyForDispatch: 'bg-purple-100 text-purple-800',
    AssignedToVehicle: 'bg-indigo-100 text-indigo-800',
    Dispatched: 'bg-orange-100 text-orange-800',
    Delivered: 'bg-green-100 text-green-800',
    Cancelled: 'bg-red-100 text-red-800',
    Passive: 'bg-gray-100 text-gray-500',
  };
  return map[status] || 'bg-gray-100 text-gray-800';
};

const loadData = async () => {
  loading.value = true;
  try {
    summary.value = await reportService.getShipmentSummary(filter.value);
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Rapor yüklenemedi.'));
  } finally {
    loading.value = false;
  }
};

const exportExcel = () => {
  if (!summary.value) return;
  const rows = summary.value.rows.map(r => ({
    'ID': r.id,
    'Proje': r.projectName,
    'Bölge': r.zoneName || '',
    'Durum': r.status,
    'Teslim Tarihi': fmtDate(r.deliveryDate),
    'Talep No': r.talepNo || '',
    'Kalem Sayısı': r.lineCount,
  }));
  exportToExcel(rows, 'Sevkiyat Özeti', `sevkiyat-ozeti-${filter.value.startDate}_${filter.value.endDate}`);
};

onMounted(() => {
  loadData();
});
</script>
