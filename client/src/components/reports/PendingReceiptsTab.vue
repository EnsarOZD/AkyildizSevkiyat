<template>
  <div class="space-y-4">
    <div class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
      <div class="px-4 py-3 border-b dark:border-gray-700 flex flex-col sm:flex-row justify-between items-start sm:items-center gap-3">
        <h3 class="font-medium text-gray-900 dark:text-gray-100 mb-2 sm:mb-0">Bekleyen Mal Girişleri — Taslak ({{ pendingGRs.length }})</h3>
        <div class="flex flex-wrap items-center gap-3">
          <button @click="loadPendingGRs" class="text-sm text-indigo-600 hover:underline">Yenile</button>
          <button v-if="pendingGRs.length > 0" @click="exportGRs" class="flex items-center gap-1.5 px-3 py-1.5 text-sm border border-green-600 text-green-700 dark:text-green-400 dark:border-green-600 rounded hover:bg-green-50 dark:hover:bg-green-900/20 transition-colors">
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" /></svg>
            Excel
          </button>
        </div>
      </div>
      <div class="overflow-x-auto">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">İrsaliye No</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Tedarikçi</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Tarih</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Bağlı Sipariş</th>
            <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Kalem</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-if="loading"><td colspan="5" class="px-4 py-6 text-center text-gray-400">Yükleniyor...</td></tr>
          <tr v-else-if="pendingGRs.length === 0"><td colspan="5" class="px-4 py-6 text-center text-gray-400">Bekleyen mal girişi yok.</td></tr>
          <tr v-for="gr in pendingGRs" :key="gr.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
            <td class="px-4 py-3 font-mono font-medium text-gray-900 dark:text-gray-100">{{ gr.waybillNo }}</td>
            <td class="px-4 py-3 text-gray-900 dark:text-gray-100">{{ gr.supplierName }}</td>
            <td class="px-4 py-3 text-gray-500 dark:text-gray-400">{{ gr.receiptDate }}</td>
            <td class="px-4 py-3 text-blue-700 font-mono text-xs">{{ gr.linkedOrderNumber || '-' }}</td>
            <td class="px-4 py-3 text-right text-gray-600 dark:text-gray-400">{{ gr.lineCount }}</td>
          </tr>
        </tbody>
      </table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import reportService from '../../services/reportService';
import type { PendingGoodsReceiptRow } from '../../services/reportService';
import { ApiErrorUtils } from '../../utils/apiError';
import { useNotification } from '../../composables/useNotification';
import { exportToExcel } from '../../utils/exportExcel';

const { notify } = useNotification();

const pendingGRs = ref<PendingGoodsReceiptRow[]>([]);
const loading = ref(false);

const loadPendingGRs = async () => {
  loading.value = true;
  try {
    pendingGRs.value = await reportService.getPendingGoodsReceipts();
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Yüklenemedi.'));
  } finally {
    loading.value = false;
  }
};

const exportGRs = () => {
  if (!pendingGRs.value.length) return;
  const rows = pendingGRs.value.map(g => ({
    'İrsaliye No': g.waybillNo,
    'Tarih': g.receiptDate,
    'Tedarikçi': g.supplierName,
    'Bağlı Sipariş': g.linkedOrderNumber || '',
    'Kalem Sayısı': g.lineCount,
    'Durum': g.status,
  }));
  exportToExcel(rows, 'Bekleyen Mal Girişi', 'bekleyen-mal-girisi');
};

onMounted(() => {
  loadPendingGRs();
});
</script>
