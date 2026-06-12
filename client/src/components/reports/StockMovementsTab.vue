<template>
  <div class="space-y-4" style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">
    <ReportToolbar
      v-model:start-date="filter.startDate"
      v-model:end-date="filter.endDate"
      :loading="loading"
      :can-export="false"
      apply-label="Listele"
      @apply="load(true)"
    >
      <template #filters>
        <div>
          <label class="block text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-white/55 mb-1.5">Hareket Tipi</label>
          <select v-model="filter.type" :class="inputCls">
            <option :value="null">Tümü</option>
            <option v-for="(label, id) in STOCK_TRANSACTION_TYPE_LABELS" :key="id" :value="Number(id)">{{ label }}</option>
          </select>
        </div>
        <div class="flex-1 min-w-40">
          <label class="block text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-white/55 mb-1.5">Stok Ara</label>
          <input v-model="filter.stockSearch" type="text" placeholder="Stok kodu veya adı…" :class="inputCls + ' w-full'" @keyup.enter="load(true)" />
        </div>
      </template>
    </ReportToolbar>

    <div v-if="data" class="grid grid-cols-2 sm:grid-cols-4 gap-3">
      <ReportStat label="Toplam Kayıt" :value="data.totalCount" />
      <ReportStat label="Giriş Miktarı" :value="inQty" tone="green" />
      <ReportStat label="Çıkış Miktarı" :value="outQty" tone="red" />
      <ReportStat label="Net Değişim" :value="netQty" tone="blue" />
    </div>

    <div class="rounded-2xl border border-gray-200 dark:border-white/10 bg-white dark:bg-[#0f2238] overflow-hidden">
      <div v-if="loading" class="flex justify-center py-12"><div class="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></div></div>
      <template v-else-if="data">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-gray-50 dark:bg-white/5 text-gray-500 dark:text-white/55">
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Tarih</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Stok Kodu</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Stok Adı</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide hidden sm:table-cell">Hareket Tipi</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Miktar</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide hidden md:table-cell">Referans</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide hidden lg:table-cell">Not</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-white/5">
              <tr v-if="data.items.length === 0"><td colspan="7" class="px-4 py-10 text-center text-gray-400 dark:text-white/40">Kayıt bulunamadı.</td></tr>
              <tr v-for="row in data.items" :key="row.id" class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
                <td class="px-4 py-3 text-xs text-gray-500 dark:text-white/55 whitespace-nowrap">{{ formatDate(row.date) }}</td>
                <td class="px-4 py-3 font-mono text-xs font-semibold text-gray-900 dark:text-white">{{ row.stockCode }}</td>
                <td class="px-4 py-3 text-gray-800 dark:text-white/85">{{ row.stockName }}</td>
                <td class="px-4 py-3 hidden sm:table-cell"><span class="inline-flex px-2 py-0.5 rounded-full text-[11px] font-bold" :class="typeChipClass(row.transactionTypeId)">{{ row.transactionType }}</span></td>
                <td class="px-4 py-3 text-right font-bold" :class="row.qty >= 0 ? 'text-emerald-600 dark:text-emerald-300' : 'text-red-600 dark:text-red-300'">{{ row.qty >= 0 ? '+' : '' }}{{ row.qty }} {{ row.unit }}</td>
                <td class="px-4 py-3 hidden md:table-cell font-mono text-xs text-gray-400 dark:text-white/40">{{ row.reference ?? '—' }}</td>
                <td class="px-4 py-3 hidden lg:table-cell text-xs text-gray-400 dark:text-white/40">{{ row.note ?? '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <div v-if="data.totalCount > data.pageSize" class="flex items-center justify-between px-4 py-3 border-t border-gray-100 dark:border-white/10">
          <p class="text-xs text-gray-500 dark:text-white/55">{{ data.totalCount }} kayıt · Sayfa {{ data.page }} / {{ Math.ceil(data.totalCount / data.pageSize) }}</p>
          <div class="flex gap-2">
            <button :disabled="data.page === 1" @click="changePage(data.page - 1)" class="px-3 py-1 text-xs font-semibold rounded-lg border border-gray-200 dark:border-white/15 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-white/5">‹ Önceki</button>
            <button :disabled="data.page * data.pageSize >= data.totalCount" @click="changePage(data.page + 1)" class="px-3 py-1 text-xs font-semibold rounded-lg border border-gray-200 dark:border-white/15 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-white/5">Sonraki ›</button>
          </div>
        </div>
      </template>
      <div v-else class="text-center py-12 text-gray-400 dark:text-white/40 text-sm">Filtre seçip "Listele" butonuna basın.</div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue';
import reportService, { type StockMovementsResult, STOCK_TRANSACTION_TYPE_LABELS } from '../../services/reportService';
import { useNotificationStore } from '../../stores/notification';
import ReportToolbar from './ReportToolbar.vue';
import ReportStat from './ReportStat.vue';

const notify = useNotificationStore();
const loading = ref(false);
const data = ref<StockMovementsResult | null>(null);

const inputCls =
  'h-[42px] px-3 rounded-xl border border-gray-200 dark:border-white/15 bg-white dark:bg-[#13294a] ' +
  'text-sm text-gray-800 dark:text-white outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/12 transition-all';

function today() { return new Date().toISOString().slice(0, 10); }
function daysAgo(n: number) { const d = new Date(); d.setDate(d.getDate() - n); return d.toISOString().slice(0, 10); }

const filter = reactive({ startDate: daysAgo(30), endDate: today(), type: null as number | null, stockSearch: '' });

const inQty = computed(() => data.value?.items.filter(r => r.qty > 0).reduce((s, r) => s + r.qty, 0).toFixed(2) ?? '0');
const outQty = computed(() => Math.abs(data.value?.items.filter(r => r.qty < 0).reduce((s, r) => s + r.qty, 0) ?? 0).toFixed(2));
const netQty = computed(() => {
  if (!data.value) return '0';
  const net = data.value.items.reduce((s, r) => s + r.qty, 0);
  return (net >= 0 ? '+' : '') + net.toFixed(2);
});

async function load(resetPage = false) {
  loading.value = true;
  try {
    const page = resetPage ? 1 : (data.value?.page ?? 1);
    data.value = await reportService.getStockMovements({ startDate: filter.startDate, endDate: filter.endDate, stockSearch: filter.stockSearch || undefined, type: filter.type, page, pageSize: 50 });
  } catch { notify.add('Stok hareketleri yüklenemedi.', 'error'); }
  finally { loading.value = false; }
}

async function changePage(page: number) {
  loading.value = true;
  try {
    data.value = await reportService.getStockMovements({ startDate: filter.startDate, endDate: filter.endDate, stockSearch: filter.stockSearch || undefined, type: filter.type, page, pageSize: 50 });
  } catch { notify.add('Yüklenemedi.', 'error'); }
  finally { loading.value = false; }
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}
function typeChipClass(typeId: number) {
  if (typeId === 0 || typeId === 5) return 'bg-emerald-100 dark:bg-emerald-500/15 text-emerald-700 dark:text-emerald-300';
  if (typeId === 1 || typeId === 6) return 'bg-red-100 dark:bg-red-500/15 text-red-700 dark:text-red-300';
  if (typeId === 2) return 'bg-amber-100 dark:bg-amber-500/15 text-amber-700 dark:text-amber-300';
  return 'bg-gray-100 dark:bg-white/10 text-gray-600 dark:text-white/70';
}

onMounted(() => load(true));
</script>
