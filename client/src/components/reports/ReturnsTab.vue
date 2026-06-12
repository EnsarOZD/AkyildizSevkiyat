<template>
  <div class="space-y-4" style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">
    <ReportToolbar
      v-model:start-date="filter.startDate"
      v-model:end-date="filter.endDate"
      :loading="loading"
      :can-export="!!retData"
      @apply="loadReturns"
      @export="exportReturns"
    />

    <template v-if="retData">
      <div class="grid grid-cols-2 gap-3">
        <ReportStat label="Toplam İade Kalemi" :value="retData.totalReturnedLines" />
        <ReportStat label="Toplam İade Miktarı" :value="retData.totalReturnedQty" tone="amber" />
      </div>

      <!-- Neden bazında -->
      <div v-if="retData.byReason.length > 0" class="rounded-2xl border border-gray-200 dark:border-white/10 bg-white dark:bg-[#0f2238] overflow-hidden">
        <div class="px-4 py-3.5 border-b border-gray-100 dark:border-white/5">
          <h3 class="text-[14px] font-bold text-gray-900 dark:text-white">Neden Bazında Özet</h3>
        </div>
        <div class="overflow-x-auto">
          <table class="min-w-full text-sm">
            <thead>
              <tr class="bg-gray-50 dark:bg-white/5 text-gray-500 dark:text-white/55">
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Neden</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Adet</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Toplam Miktar</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-white/5">
              <tr v-for="r in retData.byReason" :key="r.reason" class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
                <td class="px-4 py-3 font-semibold text-gray-900 dark:text-white">{{ returnReasonLabel(r.reason) }}</td>
                <td class="px-4 py-3 text-right text-gray-700 dark:text-white/75">{{ r.count }}</td>
                <td class="px-4 py-3 text-right text-gray-700 dark:text-white/75">{{ r.totalQty }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Detay -->
      <div class="rounded-2xl border border-gray-200 dark:border-white/10 bg-white dark:bg-[#0f2238] overflow-hidden">
        <div class="px-4 py-3.5 border-b border-gray-100 dark:border-white/5">
          <h3 class="text-[14px] font-bold text-gray-900 dark:text-white">İade Detayları ({{ retData.rows.length }})</h3>
        </div>
        <div class="overflow-x-auto">
          <table class="min-w-full text-sm">
            <thead>
              <tr class="bg-gray-50 dark:bg-white/5 text-gray-500 dark:text-white/55">
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Proje</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Bölge</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Stok Kodu</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Stok Adı</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Miktar</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Neden</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Tarih</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-white/5">
              <tr v-if="retData.rows.length === 0"><td colspan="7" class="px-4 py-8 text-center text-gray-400 dark:text-white/40">Bu tarih aralığında iade bulunamadı.</td></tr>
              <tr v-for="(r, i) in retData.rows" :key="i" class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
                <td class="px-4 py-3 font-semibold text-gray-900 dark:text-white">{{ r.projectName }}</td>
                <td class="px-4 py-3 text-gray-500 dark:text-white/55">{{ r.zoneName || '—' }}</td>
                <td class="px-4 py-3 font-mono text-xs text-gray-700 dark:text-white/75">{{ r.stockCode }}</td>
                <td class="px-4 py-3 text-gray-700 dark:text-white/75">{{ r.stockName }}</td>
                <td class="px-4 py-3 text-right font-bold text-amber-600 dark:text-amber-300">{{ r.returnedQty }}</td>
                <td class="px-4 py-3 text-gray-600 dark:text-white/65 text-xs">{{ returnReasonLabel(r.returnReason) }}</td>
                <td class="px-4 py-3 text-gray-400 dark:text-white/40 text-xs">{{ r.returnedAt ? fmtDate(r.returnedAt) : '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>
    <div v-if="!retData && !loading" class="text-center py-12 text-gray-400 dark:text-white/40">Tarih aralığı seçip "Filtrele" butonuna tıklayın.</div>
    <div v-if="loading" class="flex justify-center py-12"><div class="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></div></div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import reportService from '../../services/reportService';
import type { ReturnsReportDto } from '../../services/reportService';
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
const retData = ref<ReturnsReportDto | null>(null);
const loading = ref(false);

const returnReasonLabelMap: Record<string, string> = {
  CustomerRejected: 'Müşteri Reddi',
  Damaged: 'Hasarlı',
  ExcessLoading: 'Fazla Yükleme',
  WrongItem: 'Yanlış Ürün',
  ProjectNotFound: 'Proje Bulunamadı',
  Other: 'Diğer',
};
const returnReasonLabel = (r?: string) => (r ? (returnReasonLabelMap[r] ?? r) : 'Belirtilmemiş');

const loadReturns = async () => {
  loading.value = true;
  try {
    retData.value = await reportService.getReturns(filter.value);
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Rapor yüklenemedi.'));
  } finally {
    loading.value = false;
  }
};

const exportReturns = () => {
  if (!retData.value) return;
  const rows = retData.value.rows.map(r => ({
    'Sevkiyat ID': r.shipmentId,
    'Talep No': r.talepNo || '',
    'Proje': r.projectName,
    'Bölge': r.zoneName || '',
    'İade Tarihi': r.returnedAt ? fmtDate(r.returnedAt) : '',
    'Stok Kodu': r.stockCode,
    'Stok Adı': r.stockName,
    'İade Miktarı': r.returnedQty,
    'Neden': returnReasonLabel(r.returnReason),
    'Not': r.returnNote || '',
  }));
  exportToExcel(rows, 'İade Analizi', `iade-analizi-${filter.value.startDate}_${filter.value.endDate}`);
};
</script>
