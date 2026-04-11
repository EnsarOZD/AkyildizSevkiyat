<template>
  <div class="space-y-4">
    <div class="bg-white dark:bg-gray-900 p-4 rounded shadow flex items-center gap-4">
      <label class="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 cursor-pointer">
        <input v-model="stockCriticalOnly" type="checkbox" class="rounded" @change="loadStockStatus" />
        Sadece kritik stoklar
      </label>
      <button @click="loadStockStatus" class="bg-indigo-600 text-white px-4 py-2 rounded hover:bg-indigo-700 text-sm">Yenile</button>
      <button v-if="stockData" @click="exportStock" class="ml-auto flex items-center gap-1.5 px-4 py-2 text-sm border border-green-600 text-green-700 dark:text-green-400 dark:border-green-600 rounded hover:bg-green-50 dark:hover:bg-green-900/20 transition-colors">
        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" /></svg>
        Excel İndir
      </button>
    </div>

    <template v-if="stockData">
      <div class="grid grid-cols-3 gap-4">
        <div class="bg-white dark:bg-gray-900 rounded shadow p-4 text-center">
          <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stockData.totalStocks }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam Stok</p>
        </div>
        <div class="bg-orange-50 dark:bg-orange-900/20 rounded shadow p-4 text-center">
          <p class="text-3xl font-bold text-orange-700 dark:text-orange-400">{{ stockData.criticalCount }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Kritik Seviye</p>
        </div>
        <div class="bg-red-50 dark:bg-red-900/20 rounded shadow p-4 text-center">
          <p class="text-3xl font-bold text-red-700 dark:text-red-400">{{ stockData.outOfStockCount }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Stok Tükendi</p>
        </div>
      </div>

      <div class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
        <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Kod</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Ad</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Lokasyon</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Elde</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Rezerve</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Kullanılabilir</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Min</th>
              <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Durum</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
            <tr v-if="stockData.rows.length === 0"><td colspan="8" class="px-4 py-6 text-center text-gray-400">Kayıt bulunamadı.</td></tr>
            <tr v-for="s in stockData.rows" :key="s.id" class="hover:bg-gray-50 dark:hover:bg-gray-800"
                :class="s.isOutOfStock ? 'bg-red-50/40 dark:bg-red-900/10' : s.isCritical ? 'bg-orange-50/40 dark:bg-orange-900/10' : ''">
              <td class="px-4 py-3 font-mono text-xs font-medium text-gray-900 dark:text-gray-100">{{ s.stockCode }}</td>
              <td class="px-4 py-3 text-gray-900 dark:text-gray-100">{{ s.stockName }}</td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400 text-xs font-mono">{{ s.warehouseLocation || '-' }}</td>
              <td class="px-4 py-3 text-right text-gray-700 dark:text-gray-300">{{ s.onHandQty }}</td>
              <td class="px-4 py-3 text-right text-yellow-700 dark:text-yellow-400">{{ s.reservedQty }}</td>
              <td class="px-4 py-3 text-right font-semibold" :class="s.isOutOfStock ? 'text-red-600 dark:text-red-400' : s.isCritical ? 'text-orange-600 dark:text-orange-400' : 'text-green-700 dark:text-green-400'">
                {{ s.availableQty }}
              </td>
              <td class="px-4 py-3 text-right text-gray-500 dark:text-gray-400">{{ s.minStockQty ?? '-' }}</td>
              <td class="px-4 py-3 text-center">
                <span v-if="s.isOutOfStock" class="px-2 py-0.5 text-xs rounded-full bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300 font-medium">Tükendi</span>
                <span v-else-if="s.isCritical" class="px-2 py-0.5 text-xs rounded-full bg-orange-100 text-orange-800 dark:bg-orange-900/40 dark:text-orange-300 font-medium">Kritik</span>
                <span v-else class="px-2 py-0.5 text-xs rounded-full bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300">Normal</span>
              </td>
            </tr>
          </tbody>
        </table>
        </div>
      </div>
    </template>
    <div v-if="!stockData && !loading" class="text-center py-10 text-gray-400">Yükleniyor...</div>
    <div v-if="loading" class="text-center py-10 text-gray-400">Yükleniyor...</div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import reportService from '../../services/reportService';
import type { StockStatusReportDto } from '../../services/reportService';
import { ApiErrorUtils } from '../../utils/apiError';
import { useNotification } from '../../composables/useNotification';
import { exportToExcel } from '../../utils/exportExcel';

const { notify } = useNotification();

const stockData = ref<StockStatusReportDto | null>(null);
const loading = ref(false);
const stockCriticalOnly = ref(false);

const loadStockStatus = async () => {
  loading.value = true;
  try {
    stockData.value = await reportService.getStockStatus({ criticalOnly: stockCriticalOnly.value });
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Yüklenemedi.'));
  } finally {
    loading.value = false;
  }
};

const exportStock = () => {
  if (!stockData.value) return;
  const rows = stockData.value.rows.map(s => ({
    'Stok Kodu': s.stockCode,
    'Stok Adı': s.stockName,
    'Kategori': s.category || '',
    'Lokasyon': s.warehouseLocation || '',
    'Elde (Adet)': s.onHandQty,
    'Rezerve': s.reservedQty,
    'Kullanılabilir': s.availableQty,
    'Min Stok': s.minStockQty ?? '',
    'Durum': s.isOutOfStock ? 'Tükendi' : s.isCritical ? 'Kritik' : 'Normal',
  }));
  exportToExcel(rows, 'Stok Durumu', 'stok-durumu');
};

onMounted(() => {
  loadStockStatus();
});
</script>
