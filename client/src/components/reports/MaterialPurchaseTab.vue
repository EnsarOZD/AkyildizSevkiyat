<template>
  <div class="space-y-4" style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">
    <ReportToolbar :dates="false" :loading="loading" :can-export="rows.length > 0" apply-label="Raporu Listele" @apply="loadData" @export="exportExcel">
      <template #filters>
        <div>
          <label class="block text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-white/55 mb-1.5">Tedarikçi</label>
          <select v-model="filters.supplierId" :class="inputCls">
            <option value="">Tümü</option>
            <option v-for="s in suppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
          </select>
        </div>
        <div class="flex-1 min-w-40">
          <label class="block text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-white/55 mb-1.5">Malzeme Kodu / Adı</label>
          <input type="text" v-model="filters.materialName" @keyup.enter="loadData" placeholder="Ara…" :class="inputCls + ' w-full'" />
        </div>
      </template>
    </ReportToolbar>

    <div v-if="loading" class="flex justify-center py-12"><div class="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></div></div>
    <div v-else-if="error" class="text-center py-10 rounded-2xl bg-red-50 dark:bg-red-500/10 text-red-600 dark:text-red-300 border border-red-200 dark:border-red-500/30">{{ error }}</div>
    <div v-else-if="rows.length === 0" class="text-center py-16 rounded-2xl border border-dashed border-gray-200 dark:border-white/10 text-gray-400 dark:text-white/40 text-sm">Aradığınız kriterlere uygun kayıt bulunamadı.</div>

    <div v-else class="rounded-2xl border border-gray-200 dark:border-white/10 bg-white dark:bg-[#0f2238] overflow-hidden">
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="bg-gray-50 dark:bg-white/5 text-gray-500 dark:text-white/55">
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Tedarikçi</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Malz. Kodu</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide min-w-[200px]">Malzeme Adı</th>
              <th class="px-4 py-3 text-center text-[11px] font-bold uppercase tracking-wide">Birim</th>
              <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Sipariş</th>
              <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Gelen</th>
              <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Kalan</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-white/5">
            <tr v-for="(r, idx) in rows" :key="idx" class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
              <td class="px-4 py-3 font-semibold text-gray-900 dark:text-white">{{ r.supplierName }}</td>
              <td class="px-4 py-3 text-gray-400 dark:text-white/40 font-mono text-xs">{{ r.stockCode }}</td>
              <td class="px-4 py-3 text-gray-700 dark:text-white/85 font-medium">{{ r.stockName }}</td>
              <td class="px-4 py-3 text-center"><span class="px-1.5 py-0.5 rounded bg-gray-100 dark:bg-white/10 text-gray-600 dark:text-white/70 font-bold uppercase text-[9px]">{{ r.unit }}</span></td>
              <td class="px-4 py-3 text-right font-bold text-blue-600 dark:text-blue-300">{{ formatQty(r.orderedQty) }}</td>
              <td class="px-4 py-3 text-right font-bold text-emerald-600 dark:text-emerald-300">{{ formatQty(r.receivedQty) }}</td>
              <td class="px-4 py-3 text-right font-bold text-amber-600 dark:text-amber-300">{{ formatQty(r.remainingQty) }}</td>
            </tr>
          </tbody>
          <tfoot class="border-t-2 border-gray-200 dark:border-white/10">
            <tr class="bg-gray-50 dark:bg-white/5 font-extrabold text-[11px]">
              <td colspan="4" class="px-4 py-3 text-right uppercase tracking-widest text-gray-500 dark:text-white/55">Genel Toplamlar</td>
              <td class="px-4 py-3 text-right text-blue-600 dark:text-blue-300">{{ formatQty(totals.ordered) }}</td>
              <td class="px-4 py-3 text-right text-emerald-600 dark:text-emerald-300">{{ formatQty(totals.received) }}</td>
              <td class="px-4 py-3 text-right text-amber-600 dark:text-amber-300">{{ formatQty(totals.remaining) }}</td>
            </tr>
          </tfoot>
        </table>
      </div>
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
import ReportToolbar from './ReportToolbar.vue';

const notification = useNotificationStore();
const filters = ref({ supplierId: '', materialName: '' });
const rows = ref<MaterialPurchaseReportRow[]>([]);
const suppliers = ref<Supplier[]>([]);
const loading = ref(false);
const error = ref('');

const inputCls =
  'h-[42px] px-3 rounded-xl border border-gray-200 dark:border-white/15 bg-white dark:bg-[#13294a] ' +
  'text-sm text-gray-800 dark:text-white outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/12 transition-all';

const totals = computed(() => rows.value.reduce((acc, row) => {
  acc.ordered += row.orderedQty; acc.received += row.receivedQty; acc.remaining += row.remainingQty; return acc;
}, { ordered: 0, received: 0, remaining: 0 }));

const loadSuppliers = async () => {
  try { suppliers.value = await supplierService.getAll(); } catch { /* yoksay */ }
};

const loadData = async () => {
  loading.value = true;
  error.value = '';
  try {
    rows.value = await reportService.getMaterialPurchases({ supplierId: filters.value.supplierId || null, materialName: filters.value.materialName || null });
  } catch (e) {
    error.value = ApiErrorUtils.getErrorMessage(e) || 'Rapor getirilirken bir hata oluştu.';
    notification.add(error.value, 'error');
  } finally {
    loading.value = false;
  }
};

const formatQty = (val: number) => new Intl.NumberFormat('tr-TR', { minimumFractionDigits: 0, maximumFractionDigits: 2 }).format(val);

const exportExcel = () => {
  if (rows.value.length === 0) return;
  const exportData = rows.value.map(r => ({
    'Tedarikçi Firma': r.supplierName, 'Malzeme Kodu': r.stockCode, 'Malzeme Adı': r.stockName,
    'Birim': r.unit, 'Sipariş Miktarı': r.orderedQty, 'Teslim Alınan': r.receivedQty, 'Kalan Bakiye': r.remainingQty,
  }));
  const worksheet = XLSX.utils.json_to_sheet(exportData);
  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, worksheet, 'Satinalma Raporu');
  const date = new Date().toISOString().split('T')[0];
  XLSX.writeFile(workbook, `Tedarikci_Malzeme_Raporu_${date}.xlsx`);
  notification.add('Excel dosyası başarıyla oluşturuldu.', 'success');
};

onMounted(async () => { await loadSuppliers(); await loadData(); });
</script>
