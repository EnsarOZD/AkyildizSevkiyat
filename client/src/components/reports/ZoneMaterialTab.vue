<template>
  <div class="space-y-4" style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">
    <ReportToolbar :dates="false" :loading="loading" :can-export="false" apply-label="Raporu Getir" @apply="fetchReport">
      <template #filters>
        <div>
          <label class="block text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-white/55 mb-1.5">Teslimat Tarihi</label>
          <input type="date" v-model="filters.deliveryDate" :class="inputCls" />
        </div>
        <div>
          <label class="block text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-white/55 mb-1.5">Bölge</label>
          <select v-model="filters.zoneId" :class="inputCls">
            <option :value="null">Tüm Bölgeler</option>
            <option v-for="zone in zones" :key="zone.id" :value="zone.id">{{ zone.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-white/55 mb-1.5">Miktar Modu</label>
          <div class="flex h-[42px] rounded-xl border border-gray-200 dark:border-white/15 overflow-hidden">
            <button @click="filters.qtyMode = 'Ordered'" class="px-4 text-sm font-bold transition-colors" :class="filters.qtyMode === 'Ordered' ? 'bg-blue-600 text-white' : 'bg-white dark:bg-[#13294a] text-gray-600 dark:text-white/70'">Sipariş</button>
            <button @click="filters.qtyMode = 'Delivered'" class="px-4 text-sm font-bold transition-colors" :class="filters.qtyMode === 'Delivered' ? 'bg-blue-600 text-white' : 'bg-white dark:bg-[#13294a] text-gray-600 dark:text-white/70'">Sevkiyat</button>
          </div>
        </div>
        <label class="flex items-center gap-2 h-[42px] text-[13px] text-gray-700 dark:text-white/80 cursor-pointer">
          <input type="checkbox" v-model="filters.includeDelivered" class="rounded accent-blue-600" />
          Teslim Edilenleri Dahil Et
        </label>
      </template>
    </ReportToolbar>

    <div v-if="reportData.length > 0" class="grid grid-cols-1 md:grid-cols-3 gap-3">
      <ReportStat label="Toplam Kalem" :value="totalMaterialRows" tone="blue" />
      <ReportStat label="Toplam Miktar" :value="formatQty(totalQtySum)" tone="blue" />
      <ReportStat label="Miktar Modu" :value="filters.qtyMode === 'Ordered' ? 'Sipariş (Plan)' : 'Sevkiyat (Fiili)'" />
    </div>

    <div class="rounded-2xl border border-gray-200 dark:border-white/10 bg-white dark:bg-[#0f2238] overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full text-sm">
          <thead>
            <tr class="bg-gray-50 dark:bg-white/5 text-gray-500 dark:text-white/55">
              <th class="px-5 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Bölge</th>
              <th class="px-5 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Stok Kodu</th>
              <th class="px-5 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Stok Adı</th>
              <th class="px-5 py-3 text-center text-[11px] font-bold uppercase tracking-wide">Birim</th>
              <th class="px-5 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Toplam Miktar</th>
              <th class="px-5 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Sevkiyat</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-white/5">
            <tr v-if="loading"><td colspan="6" class="px-5 py-10 text-center"><div class="w-6 h-6 mx-auto border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></div></td></tr>
            <tr v-else-if="reportData.length === 0"><td colspan="6" class="px-5 py-16 text-center text-gray-400 dark:text-white/40">Veri bulunamadı. Seçili kriterlerde sevkiyat olmayabilir.</td></tr>
            <tr v-else v-for="(row, idx) in reportData" :key="idx" class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
              <td class="px-5 py-3"><span class="px-2 py-0.5 rounded text-xs font-bold bg-gray-100 dark:bg-white/10 text-gray-700 dark:text-white/75">{{ row.zoneName }}</span></td>
              <td class="px-5 py-3 font-mono text-xs font-bold text-blue-600 dark:text-blue-300">{{ row.stockCode }}</td>
              <td class="px-5 py-3 text-gray-700 dark:text-white/75 font-medium">{{ row.stockName }}</td>
              <td class="px-5 py-3 text-center text-gray-500 dark:text-white/55">{{ row.unit }}</td>
              <td class="px-5 py-3 text-right font-extrabold text-gray-900 dark:text-white tabular-nums">{{ formatQty(row.totalQty) }}</td>
              <td class="px-5 py-3 text-right"><span class="px-2 py-0.5 rounded-full text-xs font-bold bg-blue-50 dark:bg-blue-500/15 text-blue-600 dark:text-blue-300">{{ row.shipmentCount }}</span></td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import projectService from '../../services/projectService';
import reportService from '../../services/reportService';
import { ApiErrorUtils } from '../../utils/apiError';
import { useNotificationStore } from '../../stores/notification';
import ReportToolbar from './ReportToolbar.vue';
import ReportStat from './ReportStat.vue';

const notificationStore = useNotificationStore();

interface ReportRow { zoneId: number; zoneName: string; stockCode: string; stockName: string; unit: string; totalQty: number; shipmentCount: number; }
interface Zone { id: number; name: string; }

const loading = ref(false);
const zones = ref<Zone[]>([]);
const reportData = ref<ReportRow[]>([]);

const filters = ref({
  deliveryDate: new Date().toISOString().split('T')[0],
  zoneId: null as number | null,
  includeDelivered: false,
  qtyMode: 'Ordered' as 'Ordered' | 'Delivered',
});

const totalMaterialRows = computed(() => reportData.value.length);
const totalQtySum = computed(() => reportData.value.reduce((acc, row) => acc + row.totalQty, 0));

const inputCls =
  'h-[42px] px-3 rounded-xl border border-gray-200 dark:border-white/15 bg-white dark:bg-[#13294a] ' +
  'text-sm text-gray-800 dark:text-white outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/12 transition-all';

const fetchZones = async () => {
  try { zones.value = await projectService.getZones(); }
  catch (e) { notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Bölgeler yüklenemedi.', 'error'); }
};

const fetchReport = async () => {
  loading.value = true;
  try {
    reportData.value = await reportService.getZoneMaterialReport({
      deliveryDate: filters.value.deliveryDate ?? '',
      zoneId: filters.value.zoneId,
      includeDelivered: filters.value.includeDelivered,
      qtyMode: filters.value.qtyMode,
    });
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Rapor yüklenemedi.', 'error');
    reportData.value = [];
  } finally {
    loading.value = false;
  }
};

const formatQty = (val: number) => (val == null ? '0,00' : val.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }));

onMounted(() => { fetchZones(); fetchReport(); });
</script>
