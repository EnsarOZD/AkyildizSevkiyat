<template>
  <div class="space-y-4">

    <!-- Yenile butonu -->
    <div class="flex items-center justify-between">
      <p class="text-sm text-gray-500 dark:text-gray-400">
        Depo lokasyon tiplerine göre stok miktarı dağılımı. Yalnızca stok atanmış lokasyonlar gösterilir.
      </p>
      <button @click="load"
        class="px-4 py-2 text-sm font-medium bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors">
        Yenile
      </button>
    </div>

    <div v-if="loading" class="flex justify-center py-12">
      <div class="w-7 h-7 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <template v-else-if="data">

      <!-- Özet kartlar (LocationType bazında) -->
      <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
        <div v-for="s in data.summary" :key="s.locationTypeId"
          class="rounded-xl border p-4 cursor-pointer transition-all"
          :class="[
            summaryCardClass(s.locationTypeId),
            activeTypeFilter === s.locationTypeId
              ? 'ring-2 ring-indigo-500 dark:ring-indigo-400'
              : 'hover:shadow-md',
          ]"
          @click="toggleTypeFilter(s.locationTypeId)">
          <div class="flex items-center justify-between mb-2">
            <span class="text-xs font-semibold uppercase tracking-wide" :class="summaryLabelClass(s.locationTypeId)">
              {{ s.locationTypeLabel }}
            </span>
            <span class="text-xs text-gray-400">{{ s.locationCount }} lok · {{ s.stockCount }} ürün</span>
          </div>
          <p class="text-2xl font-bold text-gray-900 dark:text-white">{{ fmt(s.totalOnHand) }}</p>
          <div class="flex gap-3 mt-1.5 text-xs text-gray-500 dark:text-gray-400">
            <span>Rezerve: <span class="font-medium text-amber-600 dark:text-amber-400">{{ fmt(s.totalReserved) }}</span></span>
            <span>Kullanılabilir: <span class="font-medium text-green-600 dark:text-green-400">{{ fmt(s.totalAvailable) }}</span></span>
          </div>
        </div>
      </div>

      <!-- Toplam özet -->
      <div class="grid grid-cols-3 gap-4">
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-white/10 p-4 text-center">
          <p class="text-2xl font-bold text-gray-900 dark:text-white">{{ fmt(totalOnHand) }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam Elde (tüm lokasyonlar)</p>
        </div>
        <div class="bg-amber-50 dark:bg-amber-900/20 rounded-xl border border-amber-200 dark:border-amber-900/40 p-4 text-center">
          <p class="text-2xl font-bold text-amber-700 dark:text-amber-400">{{ fmt(totalReserved) }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam Rezerve</p>
        </div>
        <div class="bg-green-50 dark:bg-green-900/20 rounded-xl border border-green-200 dark:border-green-900/40 p-4 text-center">
          <p class="text-2xl font-bold text-green-700 dark:text-green-400">{{ fmt(totalAvailable) }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam Kullanılabilir</p>
        </div>
      </div>

      <!-- Detay filtreler -->
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-white/10 p-4 flex flex-wrap gap-3 items-end">
        <div class="flex-1 min-w-40">
          <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Stok Ara</label>
          <input v-model="search" type="text" placeholder="Stok kodu veya adı..."
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white" />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Lokasyon Tipi</label>
          <select v-model="activeTypeFilter"
            class="px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white">
            <option :value="null">Tümü</option>
            <option v-for="s in data.summary" :key="s.locationTypeId" :value="s.locationTypeId">
              {{ s.locationTypeLabel }}
            </option>
          </select>
        </div>
        <button v-if="activeTypeFilter !== null || search" @click="clearFilters"
          class="px-3 py-2 text-sm text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200 border border-gray-300 dark:border-white/20 rounded-lg">
          Temizle
        </button>
      </div>

      <!-- Detay tablo -->
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-white/10 overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 dark:bg-white/5 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
              <tr>
                <th class="px-4 py-3 text-left">Lokasyon</th>
                <th class="px-4 py-3 text-left hidden sm:table-cell">Tip</th>
                <th class="px-4 py-3 text-left hidden md:table-cell">Alan</th>
                <th class="px-4 py-3 text-left">Stok Kodu</th>
                <th class="px-4 py-3 text-left hidden sm:table-cell">Stok Adı</th>
                <th class="px-4 py-3 text-right">Elde</th>
                <th class="px-4 py-3 text-right hidden sm:table-cell">Rezerve</th>
                <th class="px-4 py-3 text-right">Kullanılabilir</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-white/5">
              <tr v-if="filteredDetails.length === 0">
                <td colspan="8" class="px-4 py-10 text-center text-gray-400">Kayıt bulunamadı.</td>
              </tr>
              <tr v-for="row in filteredDetails" :key="`${row.warehouseLocationId}-${row.stockMasterId}`"
                class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
                <td class="px-4 py-3 font-mono text-xs font-semibold text-blue-700 dark:text-blue-300">{{ row.locationCode }}</td>
                <td class="px-4 py-3 hidden sm:table-cell">
                  <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
                    :class="summaryCardClass(row.locationTypeId)">
                    {{ row.locationTypeLabel }}
                  </span>
                </td>
                <td class="px-4 py-3 hidden md:table-cell text-xs text-gray-500 dark:text-gray-400">{{ row.alan ?? '—' }}</td>
                <td class="px-4 py-3 font-mono text-xs font-semibold text-gray-900 dark:text-white">{{ row.stockCode }}</td>
                <td class="px-4 py-3 hidden sm:table-cell text-gray-800 dark:text-gray-200 truncate max-w-48">{{ row.stockName }}</td>
                <td class="px-4 py-3 text-right text-gray-700 dark:text-gray-300">{{ row.onHandQty }} <span class="text-xs text-gray-400">{{ row.unit }}</span></td>
                <td class="px-4 py-3 text-right hidden sm:table-cell text-amber-600 dark:text-amber-400">{{ row.reservedQty }}</td>
                <td class="px-4 py-3 text-right font-semibold"
                  :class="row.availableQty > 0 ? 'text-green-700 dark:text-green-400' : 'text-red-600 dark:text-red-400'">
                  {{ row.availableQty }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="px-4 py-2 border-t border-gray-100 dark:border-white/10 text-xs text-gray-400">
          {{ filteredDetails.length }} satır gösteriliyor
        </div>
      </div>

    </template>

    <div v-else-if="!loading" class="text-center py-12 text-gray-400 dark:text-gray-500 text-sm">
      Veri yüklenemedi.
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import reportService, { type WarehouseStockDistributionDto } from '../../services/reportService';
import { useNotificationStore } from '../../stores/notification';

const notify = useNotificationStore();
const loading = ref(false);
const data    = ref<WarehouseStockDistributionDto | null>(null);
const search           = ref('');
const activeTypeFilter = ref<number | null>(null);

const totalOnHand    = computed(() => data.value?.summary.reduce((s, r) => s + r.totalOnHand, 0) ?? 0);
const totalReserved  = computed(() => data.value?.summary.reduce((s, r) => s + r.totalReserved, 0) ?? 0);
const totalAvailable = computed(() => data.value?.summary.reduce((s, r) => s + r.totalAvailable, 0) ?? 0);

const filteredDetails = computed(() => {
  if (!data.value) return [];
  let rows = data.value.details;
  if (activeTypeFilter.value !== null)
    rows = rows.filter(r => r.locationTypeId === activeTypeFilter.value);
  if (search.value.trim()) {
    const q = search.value.trim().toLowerCase();
    rows = rows.filter(r =>
      r.stockCode.toLowerCase().includes(q) ||
      r.stockName.toLowerCase().includes(q)
    );
  }
  return rows;
});

function toggleTypeFilter(typeId: number) {
  activeTypeFilter.value = activeTypeFilter.value === typeId ? null : typeId;
}

function clearFilters() {
  activeTypeFilter.value = null;
  search.value = '';
}

function fmt(n: number) {
  return n.toLocaleString('tr-TR', { minimumFractionDigits: 0, maximumFractionDigits: 2 });
}

// LocationType'a göre renk: Rack=mavi, FloorStack=turuncu, Receiving=mor, PickingFace=yeşil, Returns=kırmızı, diğerleri=gri
function summaryCardClass(typeId: number) {
  const map: Record<number, string> = {
    0: 'bg-blue-50 dark:bg-blue-900/20 border-blue-200 dark:border-blue-900/40',
    1: 'bg-orange-50 dark:bg-orange-900/20 border-orange-200 dark:border-orange-900/40',
    2: 'bg-purple-50 dark:bg-purple-900/20 border-purple-200 dark:border-purple-900/40',
    3: 'bg-indigo-50 dark:bg-indigo-900/20 border-indigo-200 dark:border-indigo-900/40',
    4: 'bg-yellow-50 dark:bg-yellow-900/20 border-yellow-200 dark:border-yellow-900/40',
    5: 'bg-gray-50 dark:bg-white/5 border-gray-200 dark:border-white/10',
    6: 'bg-emerald-50 dark:bg-emerald-900/20 border-emerald-200 dark:border-emerald-900/40',
    7: 'bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-900/40',
  };
  return map[typeId] ?? 'bg-gray-50 dark:bg-white/5 border-gray-200 dark:border-white/10';
}

function summaryLabelClass(typeId: number) {
  const map: Record<number, string> = {
    0: 'text-blue-700 dark:text-blue-300',
    1: 'text-orange-700 dark:text-orange-300',
    2: 'text-purple-700 dark:text-purple-300',
    3: 'text-indigo-700 dark:text-indigo-300',
    4: 'text-yellow-700 dark:text-yellow-300',
    5: 'text-gray-600 dark:text-gray-300',
    6: 'text-emerald-700 dark:text-emerald-300',
    7: 'text-red-700 dark:text-red-300',
  };
  return map[typeId] ?? 'text-gray-600 dark:text-gray-300';
}

async function load() {
  loading.value = true;
  try {
    data.value = await reportService.getWarehouseStockDistribution();
  } catch {
    notify.add('Depo stok dağılımı yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>
