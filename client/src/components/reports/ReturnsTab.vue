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
      <button @click="loadReturns" class="bg-indigo-600 text-white px-4 py-2 rounded hover:bg-indigo-700">Filtrele</button>
      <button v-if="retData" @click="exportReturns" class="ml-auto flex items-center gap-1.5 px-4 py-2 text-sm border border-green-600 text-green-700 dark:text-green-400 dark:border-green-600 rounded hover:bg-green-50 dark:hover:bg-green-900/20 transition-colors">
        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" /></svg>
        Excel İndir
      </button>
    </div>

    <template v-if="retData">
      <div class="grid grid-cols-2 gap-4">
        <div class="bg-white dark:bg-gray-900 rounded shadow p-4 text-center">
          <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ retData.totalReturnedLines }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam İade Kalemi</p>
        </div>
        <div class="bg-orange-50 dark:bg-orange-900/20 rounded shadow p-4 text-center">
          <p class="text-3xl font-bold text-orange-700 dark:text-orange-400">{{ retData.totalReturnedQty }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam İade Miktarı</p>
        </div>
      </div>

      <!-- By reason -->
      <div v-if="retData.byReason.length > 0" class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
        <div class="px-4 py-3 border-b dark:border-gray-700">
          <h3 class="font-medium text-gray-900 dark:text-gray-100">Neden Bazında Özet</h3>
        </div>
        <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Neden</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Adet</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Toplam Miktar</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
            <tr v-for="r in retData.byReason" :key="r.reason" class="hover:bg-gray-50 dark:hover:bg-gray-800">
              <td class="px-4 py-3 font-medium text-gray-900 dark:text-gray-100">{{ returnReasonLabel(r.reason) }}</td>
              <td class="px-4 py-3 text-right text-gray-700 dark:text-gray-300">{{ r.count }}</td>
              <td class="px-4 py-3 text-right text-gray-700 dark:text-gray-300">{{ r.totalQty }}</td>
            </tr>
          </tbody>
        </table>
        </div>
      </div>

      <!-- Detail -->
      <div class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
        <div class="px-4 py-3 border-b dark:border-gray-700">
          <h3 class="font-medium text-gray-900 dark:text-gray-100">İade Detayları ({{ retData.rows.length }})</h3>
        </div>
        <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Proje</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Bölge</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Stok Kodu</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Stok Adı</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Miktar</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Neden</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Tarih</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
            <tr v-if="retData.rows.length === 0"><td colspan="7" class="px-4 py-6 text-center text-gray-400">Bu tarih aralığında iade bulunamadı.</td></tr>
            <tr v-for="(r, i) in retData.rows" :key="i" class="hover:bg-gray-50 dark:hover:bg-gray-800">
              <td class="px-4 py-3 font-medium text-gray-900 dark:text-gray-100">{{ r.projectName }}</td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400">{{ r.zoneName || '-' }}</td>
              <td class="px-4 py-3 font-mono text-xs text-gray-700 dark:text-gray-300">{{ r.stockCode }}</td>
              <td class="px-4 py-3 text-gray-700 dark:text-gray-300">{{ r.stockName }}</td>
              <td class="px-4 py-3 text-right font-semibold text-orange-700 dark:text-orange-400">{{ r.returnedQty }}</td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 text-xs">{{ returnReasonLabel(r.returnReason) }}</td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400 text-xs">{{ r.returnedAt ? fmtDate(r.returnedAt) : '-' }}</td>
            </tr>
          </tbody>
        </table>
        </div>
      </div>
    </template>
    <div v-if="!retData && !loading" class="text-center py-10 text-gray-400">Tarih aralığı seçip "Filtrele" butonuna tıklayın.</div>
    <div v-if="loading" class="text-center py-10 text-gray-400">Yükleniyor...</div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import reportService from '../../services/reportService';
import type { ReturnsReportDto } from '../../services/reportService';
import { ApiErrorUtils } from '../../utils/apiError';
import { useNotification } from '../../composables/useNotification';
import { exportToExcel } from '../../utils/exportExcel';

const { notify } = useNotification();

const today = new Date().toISOString().slice(0, 10);
const monthAgo = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10);

const filter = ref({ startDate: monthAgo, endDate: today });
const retData = ref<ReturnsReportDto | null>(null);
const loading = ref(false);

const fmtDate = (d: string) => new Date(d).toLocaleDateString('tr-TR');

const returnReasonLabelMap: Record<string, string> = {
  CustomerRejected: 'Müşteri Reddi',
  Damaged: 'Hasarlı',
  ExcessLoading: 'Fazla Yükleme',
  WrongItem: 'Yanlış Ürün',
  ProjectNotFound: 'Proje Bulunamadı',
  Other: 'Diğer',
};

const returnReasonLabel = (r?: string) =>
  r ? (returnReasonLabelMap[r] ?? r) : 'Belirtilmemiş';

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
