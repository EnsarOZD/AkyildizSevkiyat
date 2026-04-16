<template>
  <div class="space-y-4 shadow rounded-xl bg-white dark:bg-gray-900 overflow-hidden border border-gray-200 dark:border-gray-700 p-5">
    <!-- Filters -->
    <div class="flex flex-col sm:flex-row gap-4 mb-6">
      <div class="w-full sm:w-1/3 space-y-1">
        <label class="text-xs font-bold text-gray-500 uppercase tracking-wider">Tedarikçi</label>
        <select v-model="filters.supplierId" class="w-full rounded-xl border border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-800 px-3 py-2 text-sm focus:ring-2 focus:ring-indigo-500 text-gray-900 dark:text-gray-100">
          <option value="">Tümü</option>
          <option v-for="s in suppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
        </select>
      </div>
      
      <div class="w-full sm:w-1/3 flex items-end">
        <button @click="loadData" :disabled="loading" class="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-bold rounded-xl flex items-center justify-center gap-2 shadow-sm transition-all w-full sm:w-auto">
          <svg v-if="loading" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
          Hücreleri Güncelle
        </button>
      </div>
    </div>

    <!-- Error/Empty State -->
    <div v-if="error" class="text-center py-10 bg-red-50 dark:bg-red-900/10 rounded-xl text-red-600">
      {{ error }}
    </div>
    <div v-else-if="!loading && rows.length === 0" class="text-center py-16 bg-gray-50 dark:bg-gray-800/30 rounded-xl border border-dashed border-gray-200 dark:border-gray-700">
      <div class="text-gray-400 dark:text-gray-500 mb-2">
        <svg class="h-10 w-10 mx-auto opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" /></svg>
      </div>
      <p class="text-sm font-medium text-gray-500 dark:text-gray-400">Aradığınız kriterlere uygun satınalma/malzeme kaydı bulunamadı.</p>
    </div>

    <!-- Table -->
    <div v-else class="overflow-x-auto border border-gray-200 dark:border-gray-700 rounded-xl">
      <table class="w-full text-sm text-left">
        <thead class="text-xs text-gray-500 uppercase bg-gray-50 dark:bg-gray-800/50">
          <tr>
            <th class="px-6 py-4 font-bold tracking-wider">Tedarikçi</th>
            <th class="px-6 py-4 font-bold tracking-wider">Malzeme Adı</th>
            <th class="px-6 py-4 font-bold text-center tracking-wider text-blue-600">Sipariş Edilen</th>
            <th class="px-6 py-4 font-bold text-center tracking-wider text-green-600">Gelen</th>
            <th class="px-6 py-4 font-bold text-center tracking-wider text-orange-500">Kalan</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200 dark:divide-gray-800">
          <tr v-for="(r, idx) in rows" :key="idx" class="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors">
            <td class="px-6 py-4 font-semibold text-gray-900 dark:text-gray-100">{{ r.supplierName }}</td>
            <td class="px-6 py-4 text-gray-700 dark:text-gray-300">{{ r.stockName }}</td>
            <td class="px-6 py-4 text-center font-bold text-blue-600">{{ r.orderedQty }}</td>
            <td class="px-6 py-4 text-center font-bold text-green-600">{{ r.receivedQty }}</td>
            <td class="px-6 py-4 text-center font-bold text-orange-500">{{ r.remainingQty }}</td>
          </tr>
        </tbody>
        <tfoot class="bg-gray-50 dark:bg-gray-800/80 font-bold border-t-2 border-gray-200 dark:border-gray-700">
          <tr>
            <td colspan="2" class="px-6 py-3 text-right">GENEL TOPLAM</td>
            <td class="px-6 py-3 text-center text-blue-600">{{ totals.ordered }}</td>
            <td class="px-6 py-3 text-center text-green-600">{{ totals.received }}</td>
            <td class="px-6 py-3 text-center text-orange-500">{{ totals.remaining }}</td>
          </tr>
        </tfoot>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import reportService, { type MaterialPurchaseReportRow } from '../../services/reportService';
import { supplierService, type Supplier } from '../../services/supplierService';
import { ApiErrorUtils } from '../../utils/apiError';
import { useNotificationStore } from '../../stores/notification';

const notification = useNotificationStore();
const filters = ref({ supplierId: '' });
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
    supplierService.getAll().catch(()=>{}).finally(()=>{});
  }
};

const loadData = async () => {
  loading.value = true;
  error.value = '';
  try {
    rows.value = await reportService.getMaterialPurchases({
      supplierId: filters.value.supplierId || null
    });
  } catch (e) {
    error.value = ApiErrorUtils.getErrorMessage(e) || 'Rapor getirilirken bir hata oluştu.';
    notification.add(error.value, 'error');
  } finally {
    loading.value = false;
  }
};

onMounted(async () => {
  await loadSuppliers();
  await loadData();
});
</script>
