<template>
  <div class="space-y-4" style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">
    <ReportToolbar :dates="false" :loading="loading" :can-export="pendingGRs.length > 0" apply-label="Yenile" @apply="loadPendingGRs" @export="exportGRs">
      <template #filters>
        <ReportStat label="Bekleyen Mal Girişi (Taslak)" :value="pendingGRs.length" :tone="pendingGRs.length > 0 ? 'amber' : 'neutral'" class="!p-0 !border-0 !bg-transparent" />
      </template>
    </ReportToolbar>

    <div class="rounded-2xl border border-gray-200 dark:border-white/10 bg-white dark:bg-[#0f2238] overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full text-sm">
          <thead>
            <tr class="bg-gray-50 dark:bg-white/5 text-gray-500 dark:text-white/55">
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">İrsaliye No</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Tedarikçi</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Tarih</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Bağlı Sipariş</th>
              <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Kalem</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-white/5">
            <tr v-if="loading"><td colspan="5" class="px-4 py-10 text-center"><div class="w-6 h-6 mx-auto border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></div></td></tr>
            <tr v-else-if="pendingGRs.length === 0"><td colspan="5" class="px-4 py-8 text-center text-gray-400 dark:text-white/40">Bekleyen mal girişi yok.</td></tr>
            <tr v-for="gr in pendingGRs" :key="gr.id" class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
              <td class="px-4 py-3 font-mono font-semibold text-gray-900 dark:text-white">{{ gr.waybillNo }}</td>
              <td class="px-4 py-3 text-gray-900 dark:text-white">{{ gr.supplierName }}</td>
              <td class="px-4 py-3 text-gray-500 dark:text-white/55">{{ gr.receiptDate }}</td>
              <td class="px-4 py-3 text-blue-600 dark:text-blue-300 font-mono text-xs">{{ gr.linkedOrderNumber || '—' }}</td>
              <td class="px-4 py-3 text-right text-gray-600 dark:text-white/65">{{ gr.lineCount }}</td>
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
import ReportToolbar from './ReportToolbar.vue';
import ReportStat from './ReportStat.vue';

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

onMounted(loadPendingGRs);
</script>
