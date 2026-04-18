<template>
  <div class="space-y-4 shadow rounded-xl bg-white dark:bg-gray-900 overflow-hidden border border-gray-200 dark:border-gray-700 p-5">
    <!-- Filters Header -->
    <div class="flex items-center justify-between mb-2">
      <h3 class="text-sm font-bold text-gray-700 dark:text-gray-300 flex items-center gap-2">
        <svg class="h-4 w-4 text-indigo-500" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2.586a1 1 0 01-.293.707l-6.414 6.414a1 1 0 00-.293.707V17l-4 4v-6.586a1 1 0 00-.293-.707L3.293 7.293A1 1 0 013 6.586V4z" /></svg>
        Rapor Filtreleri
      </h3>
      <button 
        v-if="rows.length > 0"
        @click="exportToExcel" 
        class="flex items-center gap-1.5 px-3 py-1.5 bg-green-600 hover:bg-green-700 text-white rounded-lg text-xs font-bold transition-all shadow-sm"
      >
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" /></svg>
        Excel'e Aktar
      </button>
    </div>

    <!-- Filters Grid -->
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-6">
      <div class="space-y-1">
        <label class="text-[10px] font-bold text-gray-500 uppercase tracking-wider">Tedarikçi</label>
        <select v-model="filters.supplierId" class="w-full rounded-xl border border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-800 px-3 py-2 text-sm focus:ring-2 focus:ring-indigo-500 text-gray-900 dark:text-gray-100 outline-none">
          <option value="">Tümü</option>
          <option v-for="s in suppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
        </select>
      </div>
      
      <div class="space-y-1">
        <label class="text-[10px] font-bold text-gray-500 uppercase tracking-wider">Malzeme Kodu / Adı</label>
        <div class="relative">
          <input 
            type="text" 
            v-model="filters.materialName" 
            @keyup.enter="loadData"
            placeholder="Ara..." 
            class="w-full rounded-xl border border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-800 px-3 py-2 pl-9 text-sm focus:ring-2 focus:ring-indigo-500 text-gray-900 dark:text-gray-100 outline-none"
          />
          <svg class="h-4 w-4 text-gray-400 absolute left-3 top-2.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" /></svg>
        </div>
      </div>

      <div class="flex items-end">
        <button @click="loadData" :disabled="loading" class="px-6 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-bold rounded-xl flex items-center justify-center gap-2 shadow-md shadow-indigo-100 dark:shadow-none transition-all w-full">
          <svg v-if="loading" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
          <span v-else>Raporu Listele</span>
        </button>
      </div>
    </div>

    <!-- Loading overlay -->
    <div v-if="loading" class="py-20 text-center">
       <div class="inline-block h-8 w-8 animate-spin rounded-full border-4 border-solid border-indigo-600 border-r-transparent align-[-0.125em] motion-reduce:animate-[spin_1.5s_linear_infinite]"></div>
       <p class="mt-4 text-gray-500 font-medium">Veriler hazırlanıyor...</p>
    </div>

    <!-- Error/Empty State -->
    <div v-else-if="error" class="text-center py-10 bg-red-50 dark:bg-red-900/10 rounded-xl text-red-600 border border-red-100 dark:border-red-900/30">
      {{ error }}
    </div>
    <div v-else-if="rows.length === 0" class="text-center py-16 bg-gray-50 dark:bg-gray-800/30 rounded-xl border border-dashed border-gray-200 dark:border-gray-700">
      <div class="text-gray-400 dark:text-gray-500 mb-2">
        <svg class="h-10 w-10 mx-auto opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" /></svg>
      </div>
      <p class="text-sm font-medium text-gray-500 dark:text-gray-400">Aradığınız kriterlere uygun satınalma/malzeme kaydı bulunamadı.</p>
    </div>

    <!-- Table -->
    <div v-else class="overflow-x-auto border border-gray-200 dark:border-gray-700 rounded-xl shadow-sm bg-white dark:bg-gray-800">
      <table class="w-full text-xs text-left">
        <thead class="text-[10px] text-gray-500 uppercase bg-gray-50 dark:bg-gray-900/50 border-b border-gray-100 dark:border-gray-800 font-bold">
          <tr>
            <th class="px-4 py-3 border-r border-gray-100 dark:border-gray-800">Tedarikçi Firma</th>
            <th class="px-4 py-3 border-r border-gray-100 dark:border-gray-800">Malz. Kodu</th>
            <th class="px-4 py-3 border-r border-gray-100 dark:border-gray-800 min-w-[200px]">Malzeme Adı</th>
            <th class="px-4 py-3 border-r border-gray-100 dark:border-gray-800 text-center">Birim</th>
            <th class="px-4 py-3 text-right bg-blue-50/50 dark:bg-blue-900/10 text-blue-700 dark:text-blue-400">Sipariş</th>
            <th class="px-4 py-3 text-right bg-green-50/50 dark:bg-green-900/10 text-green-700 dark:text-green-400">Gelen</th>
            <th class="px-4 py-3 text-right bg-orange-50/50 dark:bg-orange-900/10 text-orange-700 dark:text-orange-400">Kalan</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
          <tr v-for="(r, idx) in rows" :key="idx" class="hover:bg-gray-50 dark:hover:bg-gray-900/50 transition-colors">
            <td class="px-4 py-3 font-medium text-gray-900 dark:text-gray-100 border-r border-gray-50 dark:border-gray-800/50">{{ r.supplierName }}</td>
            <td class="px-4 py-3 text-gray-500 dark:text-gray-400 font-mono border-r border-gray-50 dark:border-gray-800/50">{{ r.stockCode }}</td>
            <td class="px-4 py-3 text-gray-700 dark:text-gray-300 font-semibold border-r border-gray-50 dark:border-gray-800/50">{{ r.stockName }}</td>
            <td class="px-4 py-3 text-center border-r border-gray-50 dark:border-gray-800/50">
              <span class="px-1.5 py-0.5 rounded bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-400 font-bold uppercase text-[9px]">{{ r.unit }}</span>
            </td>
            <td class="px-4 py-3 text-right font-bold text-blue-600 bg-blue-50/20 dark:bg-blue-900/5">{{ formatQty(r.orderedQty) }}</td>
            <td class="px-4 py-3 text-right font-bold text-green-600 bg-green-50/20 dark:bg-green-900/5">{{ formatQty(r.receivedQty) }}</td>
            <td class="px-4 py-3 text-right font-bold text-orange-600 bg-orange-50/20 dark:bg-orange-900/5">{{ formatQty(r.remainingQty) }}</td>
          </tr>
        </tbody>
        <tfoot class="bg-gray-50 dark:bg-gray-900/80 font-black border-t-2 border-gray-200 dark:border-gray-700 text-[10px]">
          <tr>
            <td colspan="4" class="px-4 py-3 text-right uppercase tracking-widest text-gray-500">Genel Toplamlar</td>
            <td class="px-4 py-3 text-right text-blue-700">{{ formatQty(totals.ordered) }}</td>
            <td class="px-4 py-3 text-right text-green-700">{{ formatQty(totals.received) }}</td>
            <td class="px-4 py-3 text-right text-orange-700">{{ formatQty(totals.remaining) }}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import * as XLSX from 'xlsx';
import reportService, { type MaterialPurchaseReportRow } from '../../services/reportService';
import { supplierService, type Supplier } from '../../services/supplierService';
import { ApiErrorUtils } from '../../utils/apiError';
import { useNotificationStore } from '../../stores/notification';

const notification = useNotificationStore();
const filters = ref({ supplierId: '', materialName: '' });
const rows = ref<MaterialPurchaseReportRow[]>([]);
const suppliers = ref<Supplier[]>([]);
const loading = ref(false);
const error = ref('');

const totals = computed(() => {
  return rows.value.reduce((acc, row) => {
    acc.ordered += row.orderedQty;
    acc.received += row.receivedQty;
    acc.remaining += row.remainingQty;
    return acc;
  }, { ordered: 0, received: 0, remaining: 0 });
});

const loadSuppliers = async () => {
  try {
    suppliers.value = await supplierService.getAll();
  } catch (e) {
    console.error('Tedarikçiler yüklenemedi');
  }
};

const loadData = async () => {
  loading.value = true;
  error.value = '';
  try {
    rows.value = await reportService.getMaterialPurchases({
      supplierId: filters.value.supplierId || null,
      materialName: filters.value.materialName || null
    });
  } catch (e) {
    error.value = ApiErrorUtils.getErrorMessage(e) || 'Rapor getirilirken bir hata oluştu.';
    notification.add(error.value, 'error');
  } finally {
    loading.value = false;
  }
};

const formatQty = (val: number) => {
  return new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 0, maximumFractionDigits: 2 }).format(val);
};

const exportToExcel = () => {
  if (rows.value.length === 0) return;
  
  const exportData = rows.value.map(r => ({
    'Tedarikçi Firma': r.supplierName,
    'Malzeme Kodu': r.stockCode,
    'Malzeme Adı': r.stockName,
    'Birim': r.unit,
    'Sipariş Miktarı': r.orderedQty,
    'Teslim Alınan': r.receivedQty,
    'Kalan Bakiye': r.remainingQty,
  }));

  const worksheet = XLSX.utils.json_to_sheet(exportData);
  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, "Satinalma Raporu");
  
  // Apply a basic filename with current date
  const date = new Date().toISOString().split('T')[0];
  XLSX.writeFile(workbook, `Tedarikci_Malzeme_Raporu_${date}.xlsx`);
  
  notification.add('Excel dosyası başarıyla oluşturuldu.', 'success');
};

onMounted(async () => {
  await loadSuppliers();
  await loadData();
});
</script>

