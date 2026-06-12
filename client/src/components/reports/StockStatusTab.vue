<template>
  <div class="space-y-4" style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">
    <ReportToolbar :dates="false" :loading="loading" :can-export="!!stockData" apply-label="Yenile" @apply="loadStockStatus" @export="exportStock">
      <template #filters>
        <label class="flex items-center gap-2 h-[42px] text-[13px] text-gray-700 dark:text-white/80 cursor-pointer">
          <input v-model="stockCriticalOnly" type="checkbox" class="rounded accent-blue-600" @change="loadStockStatus" />
          Sadece kritik stoklar
        </label>
      </template>
    </ReportToolbar>

    <template v-if="stockData">
      <div class="grid grid-cols-3 gap-3">
        <ReportStat label="Toplam Stok" :value="stockData.totalStocks" />
        <ReportStat label="Kritik Seviye" :value="stockData.criticalCount" tone="amber" />
        <ReportStat label="Stok Tükendi" :value="stockData.outOfStockCount" tone="red" />
      </div>

      <div class="rounded-2xl border border-gray-200 dark:border-white/10 bg-white dark:bg-[#0f2238] overflow-hidden">
        <div class="overflow-x-auto">
          <table class="min-w-full text-sm">
            <thead>
              <tr class="bg-gray-50 dark:bg-white/5 text-gray-500 dark:text-white/55">
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Kod</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Ad</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Lokasyon</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Elde</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Rezerve</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Kullanılabilir</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Min</th>
                <th class="px-4 py-3 text-center text-[11px] font-bold uppercase tracking-wide">Durum</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-white/5">
              <tr v-if="stockData.rows.length === 0"><td colspan="8" class="px-4 py-8 text-center text-gray-400 dark:text-white/40">Kayıt bulunamadı.</td></tr>
              <tr v-for="s in stockData.rows" :key="s.id" class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors"
                  :class="s.isOutOfStock ? 'bg-red-50/40 dark:bg-red-500/5' : s.isCritical ? 'bg-amber-50/40 dark:bg-amber-500/5' : ''">
                <td class="px-4 py-3 font-mono text-xs font-semibold text-gray-900 dark:text-white">{{ s.stockCode }}</td>
                <td class="px-4 py-3 text-gray-900 dark:text-white">{{ s.stockName }}</td>
                <td class="px-4 py-3 text-gray-400 dark:text-white/40 text-xs font-mono">{{ s.warehouseLocation || '—' }}</td>
                <td class="px-4 py-3 text-right text-gray-700 dark:text-white/75">{{ s.onHandQty }}</td>
                <td class="px-4 py-3 text-right text-amber-600 dark:text-amber-300">{{ s.reservedQty }}</td>
                <td class="px-4 py-3 text-right font-bold" :class="s.isOutOfStock ? 'text-red-600 dark:text-red-300' : s.isCritical ? 'text-amber-600 dark:text-amber-300' : 'text-emerald-600 dark:text-emerald-300'">{{ s.availableQty }}</td>
                <td class="px-4 py-3 text-right text-gray-500 dark:text-white/55">{{ s.minStockQty ?? '—' }}</td>
                <td class="px-4 py-3 text-center">
                  <span v-if="s.isOutOfStock" class="px-2 py-0.5 text-[11px] rounded-full font-bold bg-red-100 text-red-700 dark:bg-red-500/15 dark:text-red-300">Tükendi</span>
                  <span v-else-if="s.isCritical" class="px-2 py-0.5 text-[11px] rounded-full font-bold bg-amber-100 text-amber-700 dark:bg-amber-500/15 dark:text-amber-300">Kritik</span>
                  <span v-else class="px-2 py-0.5 text-[11px] rounded-full font-bold bg-emerald-100 text-emerald-700 dark:bg-emerald-500/15 dark:text-emerald-300">Normal</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>
    <div v-if="loading" class="flex justify-center py-12"><div class="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></div></div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import reportService from '../../services/reportService';
import type { StockStatusReportDto } from '../../services/reportService';
import { ApiErrorUtils } from '../../utils/apiError';
import { useNotification } from '../../composables/useNotification';
import { exportToExcel } from '../../utils/exportExcel';
import ReportToolbar from './ReportToolbar.vue';
import ReportStat from './ReportStat.vue';

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

onMounted(loadStockStatus);
</script>
