<template>
  <div class="space-y-4" style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">

    <ReportToolbar :dates="false" :loading="loading" :can-export="false" apply-label="Yenile" @apply="load">
      <template #filters>
        <p class="text-[13px] text-gray-500 dark:text-white/55 h-[42px] flex items-center max-w-md">
          Depo lokasyon tiplerine göre stok dağılımı. Yalnızca stok atanmış lokasyonlar.
        </p>
      </template>
    </ReportToolbar>

    <div v-if="loading" class="flex justify-center py-12"><div class="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></div></div>

    <template v-else-if="data">
      <!-- LocationType özet kartları (tıklanabilir filtre) -->
      <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
        <button v-for="s in data.summary" :key="s.locationTypeId"
          @click="toggleTypeFilter(s.locationTypeId)"
          class="text-left rounded-2xl border p-4 transition-all bg-white dark:bg-[#0f2238]"
          :class="activeTypeFilter === s.locationTypeId ? 'border-blue-500 ring-2 ring-blue-500/30' : 'border-gray-200 dark:border-white/10 hover:border-blue-300 dark:hover:border-blue-500/40'">
          <div class="flex items-center justify-between mb-2 gap-2">
            <span class="inline-flex items-center gap-1.5 text-[11px] font-bold uppercase tracking-wide" :class="typeText(s.locationTypeId)">
              <span class="w-1.5 h-1.5 rounded-full" :class="typeDot(s.locationTypeId)"></span>{{ s.locationTypeLabel }}
            </span>
            <span class="text-[10.5px] text-gray-400 dark:text-white/40 whitespace-nowrap">{{ s.locationCount }} lok</span>
          </div>
          <p class="text-2xl font-extrabold text-gray-900 dark:text-white tracking-tight">{{ fmt(s.totalOnHand) }}</p>
          <div class="flex gap-3 mt-1.5 text-[11px] text-gray-500 dark:text-white/55">
            <span>Rez: <span class="font-bold text-amber-600 dark:text-amber-300">{{ fmt(s.totalReserved) }}</span></span>
            <span>Kull: <span class="font-bold text-emerald-600 dark:text-emerald-300">{{ fmt(s.totalAvailable) }}</span></span>
          </div>
        </button>
      </div>

      <!-- Toplam -->
      <div class="grid grid-cols-3 gap-3">
        <ReportStat label="Toplam Elde" :value="fmt(totalOnHand)" />
        <ReportStat label="Toplam Rezerve" :value="fmt(totalReserved)" tone="amber" />
        <ReportStat label="Toplam Kullanılabilir" :value="fmt(totalAvailable)" tone="green" />
      </div>

      <!-- Detay filtreler -->
      <ReportToolbar :dates="false" :loading="false" :can-export="false" apply-label="" @apply="() => {}">
        <template #filters>
          <div class="flex-1 min-w-40">
            <label class="block text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-white/55 mb-1.5">Stok Ara</label>
            <input v-model="search" type="text" placeholder="Stok kodu veya adı…" :class="inputCls + ' w-full'" />
          </div>
          <div>
            <label class="block text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-white/55 mb-1.5">Lokasyon Tipi</label>
            <select v-model="activeTypeFilter" :class="inputCls">
              <option :value="null">Tümü</option>
              <option v-for="s in data.summary" :key="s.locationTypeId" :value="s.locationTypeId">{{ s.locationTypeLabel }}</option>
            </select>
          </div>
          <button v-if="activeTypeFilter !== null || search" @click="clearFilters"
            class="h-[42px] px-3 rounded-xl border border-gray-200 dark:border-white/15 text-[13px] font-semibold text-gray-500 dark:text-white/70 hover:bg-gray-50 dark:hover:bg-white/5">Temizle</button>
        </template>
      </ReportToolbar>

      <!-- Detay tablo -->
      <div class="rounded-2xl border border-gray-200 dark:border-white/10 bg-white dark:bg-[#0f2238] overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="bg-gray-50 dark:bg-white/5 text-gray-500 dark:text-white/55">
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Lokasyon</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide hidden sm:table-cell">Tip</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide hidden md:table-cell">Alan</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Stok Kodu</th>
                <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide hidden sm:table-cell">Stok Adı</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Elde</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide hidden sm:table-cell">Rezerve</th>
                <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide">Kullanılabilir</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-white/5">
              <tr v-if="filteredDetails.length === 0"><td colspan="8" class="px-4 py-10 text-center text-gray-400 dark:text-white/40">Kayıt bulunamadı.</td></tr>
              <tr v-for="row in filteredDetails" :key="`${row.warehouseLocationId}-${row.stockMasterId}`" class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
                <td class="px-4 py-3 font-mono text-xs font-semibold text-blue-600 dark:text-blue-300">{{ row.locationCode }}</td>
                <td class="px-4 py-3 hidden sm:table-cell"><span class="inline-flex items-center gap-1.5 text-[11px] font-bold" :class="typeText(row.locationTypeId)"><span class="w-1.5 h-1.5 rounded-full" :class="typeDot(row.locationTypeId)"></span>{{ row.locationTypeLabel }}</span></td>
                <td class="px-4 py-3 hidden md:table-cell text-xs text-gray-400 dark:text-white/40">{{ row.alan ?? '—' }}</td>
                <td class="px-4 py-3 font-mono text-xs font-semibold text-gray-900 dark:text-white">{{ row.stockCode }}</td>
                <td class="px-4 py-3 hidden sm:table-cell text-gray-800 dark:text-white/85 truncate max-w-48">{{ row.stockName }}</td>
                <td class="px-4 py-3 text-right text-gray-700 dark:text-white/75">{{ row.onHandQty }} <span class="text-xs text-gray-400 dark:text-white/40">{{ row.unit }}</span></td>
                <td class="px-4 py-3 text-right hidden sm:table-cell text-amber-600 dark:text-amber-300">{{ row.reservedQty }}</td>
                <td class="px-4 py-3 text-right font-bold" :class="row.availableQty > 0 ? 'text-emerald-600 dark:text-emerald-300' : 'text-red-600 dark:text-red-300'">{{ row.availableQty }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="px-4 py-2 border-t border-gray-100 dark:border-white/10 text-xs text-gray-400 dark:text-white/40">{{ filteredDetails.length }} satır</div>
      </div>
    </template>

    <div v-else-if="!loading" class="text-center py-12 text-gray-400 dark:text-white/40 text-sm">Veri yüklenemedi.</div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import reportService, { type WarehouseStockDistributionDto } from '../../services/reportService';
import { useNotificationStore } from '../../stores/notification';
import ReportToolbar from './ReportToolbar.vue';
import ReportStat from './ReportStat.vue';

const notify = useNotificationStore();
const loading = ref(false);
const data = ref<WarehouseStockDistributionDto | null>(null);
const search = ref('');
const activeTypeFilter = ref<number | null>(null);

const inputCls =
  'h-[42px] px-3 rounded-xl border border-gray-200 dark:border-white/15 bg-white dark:bg-[#13294a] ' +
  'text-sm text-gray-800 dark:text-white outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/12 transition-all';

const totalOnHand = computed(() => data.value?.summary.reduce((s, r) => s + r.totalOnHand, 0) ?? 0);
const totalReserved = computed(() => data.value?.summary.reduce((s, r) => s + r.totalReserved, 0) ?? 0);
const totalAvailable = computed(() => data.value?.summary.reduce((s, r) => s + r.totalAvailable, 0) ?? 0);

const filteredDetails = computed(() => {
  if (!data.value) return [];
  let rows = data.value.details;
  if (activeTypeFilter.value !== null) rows = rows.filter(r => r.locationTypeId === activeTypeFilter.value);
  if (search.value.trim()) {
    const q = search.value.trim().toLowerCase();
    rows = rows.filter(r => r.stockCode.toLowerCase().includes(q) || r.stockName.toLowerCase().includes(q));
  }
  return rows;
});

function toggleTypeFilter(typeId: number) { activeTypeFilter.value = activeTypeFilter.value === typeId ? null : typeId; }
function clearFilters() { activeTypeFilter.value = null; search.value = ''; }
function fmt(n: number) { return n.toLocaleString('tr-TR', { minimumFractionDigits: 0, maximumFractionDigits: 2 }); }

const TONE_TEXT: Record<number, string> = {
  0: 'text-blue-600 dark:text-blue-300', 1: 'text-amber-600 dark:text-amber-300', 2: 'text-blue-600 dark:text-blue-300',
  3: 'text-blue-600 dark:text-blue-300', 4: 'text-amber-600 dark:text-amber-300', 5: 'text-gray-500 dark:text-white/60',
  6: 'text-emerald-600 dark:text-emerald-300', 7: 'text-red-600 dark:text-red-300',
};
const TONE_DOT: Record<number, string> = {
  0: 'bg-blue-500', 1: 'bg-amber-500', 2: 'bg-blue-500', 3: 'bg-blue-500', 4: 'bg-amber-500', 5: 'bg-gray-400', 6: 'bg-emerald-500', 7: 'bg-red-500',
};
const typeText = (id: number) => TONE_TEXT[id] ?? TONE_TEXT[5];
const typeDot = (id: number) => TONE_DOT[id] ?? TONE_DOT[5];

async function load() {
  loading.value = true;
  try { data.value = await reportService.getWarehouseStockDistribution(); }
  catch { notify.add('Depo stok dağılımı yüklenemedi.', 'error'); }
  finally { loading.value = false; }
}

onMounted(load);
</script>
