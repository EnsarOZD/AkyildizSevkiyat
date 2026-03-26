<template>
  <div class="p-6 space-y-6">
    <!-- Header -->
    <div class="flex justify-between items-center bg-white dark:bg-gray-900 p-6 rounded-lg shadow-sm border dark:border-gray-700">
      <div>
        <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-200">Bölge Malzeme Raporu</h1>
        <p class="text-gray-500 dark:text-gray-400 text-sm">Araca atanan sevkiyatların bölge bazlı malzeme özetleri.</p>
      </div>
      <div class="flex gap-2">
        <button
          @click="fetchReport"
          class="bg-blue-600 text-white px-6 py-2 rounded-lg font-bold hover:bg-blue-700 transition shadow-lg shadow-blue-200 flex items-center gap-2"
          :disabled="loading"
        >
          <span v-if="loading" class="animate-spin">🌀</span>
          <span>{{ loading ? 'Yükleniyor...' : 'Raporu Getir' }}</span>
        </button>
      </div>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-900 p-6 rounded-lg shadow-sm border dark:border-gray-700 grid grid-cols-1 md:grid-cols-4 gap-4 items-end">
      <div>
        <label class="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase mb-2">Teslimat Tarihi</label>
        <input
          type="date"
          v-model="filters.deliveryDate"
          class="w-full border rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
        />
      </div>
      <div>
        <label class="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase mb-2">Bölge</label>
        <select v-model="filters.zoneId" class="w-full border rounded-lg px-3 py-2 focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
          <option :value="null">Tüm Bölgeler</option>
          <option v-for="zone in zones" :key="zone.id" :value="zone.id">{{ zone.name }}</option>
        </select>
      </div>
      <div>
          <label class="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase mb-2">Miktar Modu</label>
          <div class="flex border dark:border-gray-700 rounded-lg overflow-hidden">
              <button
                @click="filters.qtyMode = 'Ordered'"
                class="flex-1 py-2 text-sm font-bold transition"
                :class="filters.qtyMode === 'Ordered' ? 'bg-blue-600 text-white' : 'bg-gray-50 dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'"
              >
                Sipariş
              </button>
              <button
                @click="filters.qtyMode = 'Delivered'"
                class="flex-1 py-2 text-sm font-bold transition"
                :class="filters.qtyMode === 'Delivered' ? 'bg-blue-600 text-white' : 'bg-gray-50 dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'"
              >
                Sevkiyat
              </button>
          </div>
      </div>
      <div class="flex items-center gap-4 h-[42px]">
          <label class="flex items-center gap-2 cursor-pointer group">
              <input type="checkbox" v-model="filters.includeDelivered" class="w-5 h-5 rounded border-gray-300 text-blue-600 focus:ring-blue-500" />
              <span class="text-sm font-medium text-gray-700 dark:text-gray-300 group-hover:text-blue-600">Teslim Edilenleri Dahil Et</span>
          </label>
      </div>
    </div>

    <!-- Stats summary (Optional) -->
    <div v-if="reportData.length > 0" class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div class="bg-blue-50 border border-blue-100 p-4 rounded-lg">
            <p class="text-blue-600 text-xs font-bold uppercase">Toplam Kalem</p>
            <p class="text-2xl font-black text-blue-900">{{ totalMaterialRows }}</p>
        </div>
        <div class="bg-indigo-50 border border-indigo-100 p-4 rounded-lg">
            <p class="text-indigo-600 text-xs font-bold uppercase">Toplam Miktar</p>
            <p class="text-2xl font-black text-indigo-900">{{ formatQty(totalQtySum) }}</p>
        </div>
        <div class="bg-gray-50 dark:bg-gray-800 border border-gray-100 dark:border-gray-700 p-4 rounded-lg">
            <p class="text-gray-600 dark:text-gray-400 text-xs font-bold uppercase">Miktar Modu</p>
            <p class="text-2xl font-black text-gray-900 dark:text-gray-100">{{ filters.qtyMode === 'Ordered' ? 'Sipariş (Plan)' : 'Sevkiyat (Fiili)' }}</p>
        </div>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-gray-900 rounded-lg shadow-sm border dark:border-gray-700 overflow-hidden">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
                <tr>
                    <th class="px-6 py-3 text-left text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Bölge</th>
                    <th class="px-6 py-3 text-left text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Stok Kodu</th>
                    <th class="px-6 py-3 text-left text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Stok Adı</th>
                    <th class="px-6 py-3 text-left text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider text-center">Birim</th>
                    <th class="px-6 py-3 text-right text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Toplam Miktar</th>
                    <th class="px-6 py-3 text-right text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sevkiyat Sayısı</th>
                </tr>
            </thead>
            <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                <template v-if="loading">
                    <tr v-for="i in 5" :key="i">
                        <td colspan="6" class="px-6 py-4">
                            <div class="h-4 bg-gray-100 dark:bg-gray-800 rounded animate-pulse"></div>
                        </td>
                    </tr>
                </template>
                <template v-else-if="reportData.length === 0">
                    <tr>
                        <td colspan="6" class="px-6 py-20 text-center">
                            <div class="flex flex-col items-center">
                                <span class="text-4xl mb-4">📊</span>
                                <p class="text-gray-400 font-bold">Veri bulunamadı.</p>
                                <p class="text-gray-300 text-sm">Seçili kriterlerde sevkiyat olmayabilir.</p>
                            </div>
                        </td>
                    </tr>
                </template>
                <template v-else>
                    <tr v-for="(row, idx) in reportData" :key="idx" class="hover:bg-blue-50/50 transition-colors">
                        <td class="px-6 py-4 whitespace-nowrap">
                            <span class="px-2 py-1 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 rounded text-xs font-bold">
                                {{ row.zoneName }}
                            </span>
                        </td>
                        <td class="px-6 py-4 whitespace-nowrap text-sm font-mono text-blue-700 font-bold">
                            {{ row.stockCode }}
                        </td>
                        <td class="px-6 py-4 text-sm text-gray-700 dark:text-gray-300 font-medium">
                            {{ row.stockName }}
                        </td>
                        <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 text-center">
                            {{ row.unit }}
                        </td>
                        <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-black text-gray-900 dark:text-gray-100 tabular-nums">
                            {{ formatQty(row.totalQty) }}
                        </td>
                        <td class="px-6 py-4 whitespace-nowrap text-right text-sm">
                            <span class="px-2 py-0.5 bg-blue-50 text-blue-600 rounded-full border border-blue-100 font-bold">
                                {{ row.shipmentCount }}
                            </span>
                        </td>
                    </tr>
                </template>
            </tbody>
        </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import projectService from '../services/projectService';
import reportService from '../services/reportService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';

const notificationStore = useNotificationStore();

interface ReportRow {
    zoneId: number;
    zoneName: string;
    stockCode: string;
    stockName: string;
    unit: string;
    totalQty: number;
    shipmentCount: number;
}

interface Zone {
    id: number;
    name: string;
}

const loading = ref(false);
const zones = ref<Zone[]>([]);
const reportData = ref<ReportRow[]>([]);

const filters = ref({
    deliveryDate: new Date().toISOString().split('T')[0],
    zoneId: null as number | null,
    includeDelivered: false,
    qtyMode: 'Ordered' as 'Ordered' | 'Delivered'
});

const totalMaterialRows = computed(() => reportData.value.length);
const totalQtySum = computed(() => reportData.value.reduce((acc, row) => acc + row.totalQty, 0));

const fetchZones = async () => {
    try {
        zones.value = await projectService.getZones();
    } catch (e) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Bölgeler yüklenemedi.', 'error');
    }
};

const fetchReport = async () => {
    loading.value = true;
    try {
        reportData.value = await reportService.getZoneMaterialReport({
            deliveryDate: filters.value.deliveryDate ?? '',
            zoneId: filters.value.zoneId,
            includeDelivered: filters.value.includeDelivered,
            qtyMode: filters.value.qtyMode
        });
    } catch (e) {
        const msg = ApiErrorUtils.getErrorMessage(e) || 'Rapor yüklenemedi.';
        notificationStore.add(msg, 'error');
        reportData.value = [];
    } finally {
        loading.value = false;
    }
};

const formatQty = (val: number) => {
    if (val === null || val === undefined) return '0.00';
    return val.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
};

onMounted(() => {
    fetchZones();
    fetchReport();
});
</script>

<style scoped>
.font-mono {
    font-family: 'JetBrains Mono', 'Fira Code', monospace;
}
</style>
