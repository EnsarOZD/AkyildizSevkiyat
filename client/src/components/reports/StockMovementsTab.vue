<template>
  <div class="space-y-4">

    <!-- Filtreler -->
    <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-white/10 p-4 flex flex-wrap gap-3 items-end">
      <div>
        <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Başlangıç</label>
        <input type="date" v-model="filter.startDate"
          class="px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white" />
      </div>
      <div>
        <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Bitiş</label>
        <input type="date" v-model="filter.endDate"
          class="px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white" />
      </div>
      <div>
        <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Hareket Tipi</label>
        <select v-model="filter.type"
          class="px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white">
          <option :value="null">Tümü</option>
          <option v-for="(label, id) in STOCK_TRANSACTION_TYPE_LABELS" :key="id" :value="Number(id)">{{ label }}</option>
        </select>
      </div>
      <div class="flex-1 min-w-40">
        <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Stok Ara</label>
        <input v-model="filter.stockSearch" type="text" placeholder="Stok kodu veya adı..."
          class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
          @keyup.enter="load(true)" />
      </div>
      <button @click="load(true)"
        class="px-4 py-2 text-sm font-medium bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors">
        Listele
      </button>
    </div>

    <!-- Özet -->
    <div v-if="data" class="grid grid-cols-2 sm:grid-cols-4 gap-4">
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-white/10 p-4 text-center">
        <p class="text-2xl font-bold text-gray-900 dark:text-white">{{ data.totalCount }}</p>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam Kayıt</p>
      </div>
      <div class="bg-green-50 dark:bg-green-900/20 rounded-xl border border-green-200 dark:border-green-900/40 p-4 text-center">
        <p class="text-2xl font-bold text-green-700 dark:text-green-400">{{ inQty }}</p>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Giriş Miktarı</p>
      </div>
      <div class="bg-red-50 dark:bg-red-900/20 rounded-xl border border-red-200 dark:border-red-900/40 p-4 text-center">
        <p class="text-2xl font-bold text-red-700 dark:text-red-400">{{ outQty }}</p>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Çıkış Miktarı</p>
      </div>
      <div class="bg-blue-50 dark:bg-blue-900/20 rounded-xl border border-blue-200 dark:border-blue-900/40 p-4 text-center">
        <p class="text-2xl font-bold text-blue-700 dark:text-blue-400">{{ netQty }}</p>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Net Değişim</p>
      </div>
    </div>

    <!-- Tablo -->
    <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-white/10 overflow-hidden">
      <div v-if="loading" class="flex justify-center py-12">
        <div class="w-7 h-7 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin"></div>
      </div>
      <template v-else-if="data">
        <div class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 dark:bg-white/5 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
              <tr>
                <th class="px-4 py-3 text-left">Tarih</th>
                <th class="px-4 py-3 text-left">Stok Kodu</th>
                <th class="px-4 py-3 text-left">Stok Adı</th>
                <th class="px-4 py-3 text-left hidden sm:table-cell">Hareket Tipi</th>
                <th class="px-4 py-3 text-right">Miktar</th>
                <th class="px-4 py-3 text-left hidden md:table-cell">Referans</th>
                <th class="px-4 py-3 text-left hidden lg:table-cell">Not</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-white/5">
              <tr v-if="data.items.length === 0">
                <td colspan="7" class="px-4 py-10 text-center text-gray-400">Kayıt bulunamadı.</td>
              </tr>
              <tr v-for="row in data.items" :key="row.id"
                class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
                <td class="px-4 py-3 text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap">
                  {{ formatDate(row.date) }}
                </td>
                <td class="px-4 py-3 font-mono text-xs font-semibold text-gray-900 dark:text-white">{{ row.stockCode }}</td>
                <td class="px-4 py-3 text-gray-800 dark:text-gray-200">{{ row.stockName }}</td>
                <td class="px-4 py-3 hidden sm:table-cell">
                  <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
                    :class="typeChipClass(row.transactionTypeId)">
                    {{ row.transactionType }}
                  </span>
                </td>
                <td class="px-4 py-3 text-right font-semibold"
                  :class="row.qty >= 0 ? 'text-green-700 dark:text-green-400' : 'text-red-600 dark:text-red-400'">
                  {{ row.qty >= 0 ? '+' : '' }}{{ row.qty }} {{ row.unit }}
                </td>
                <td class="px-4 py-3 hidden md:table-cell font-mono text-xs text-gray-500 dark:text-gray-400">
                  {{ row.reference ?? '—' }}
                </td>
                <td class="px-4 py-3 hidden lg:table-cell text-xs text-gray-500 dark:text-gray-400">
                  {{ row.note ?? '—' }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Sayfalama -->
        <div v-if="data.totalCount > data.pageSize" class="flex items-center justify-between px-4 py-3 border-t border-gray-100 dark:border-white/10">
          <p class="text-xs text-gray-500 dark:text-gray-400">
            {{ data.totalCount }} kayıt · Sayfa {{ data.page }} / {{ Math.ceil(data.totalCount / data.pageSize) }}
          </p>
          <div class="flex gap-2">
            <button :disabled="data.page === 1" @click="changePage(data.page - 1)"
              class="px-3 py-1 text-xs rounded border border-gray-300 dark:border-white/20 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-white/5">
              ‹ Önceki
            </button>
            <button :disabled="data.page * data.pageSize >= data.totalCount" @click="changePage(data.page + 1)"
              class="px-3 py-1 text-xs rounded border border-gray-300 dark:border-white/20 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-white/5">
              Sonraki ›
            </button>
          </div>
        </div>
      </template>
      <div v-else class="text-center py-12 text-gray-400 dark:text-gray-500 text-sm">
        Filtre seçip "Listele" butonuna basın.
      </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue';
import reportService, { type StockMovementsResult, STOCK_TRANSACTION_TYPE_LABELS } from '../../services/reportService';
import { useNotificationStore } from '../../stores/notification';

const notify  = useNotificationStore();
const loading = ref(false);
const data    = ref<StockMovementsResult | null>(null);

function today()    { return new Date().toISOString().slice(0, 10); }
function daysAgo(n: number) {
  const d = new Date();
  d.setDate(d.getDate() - n);
  return d.toISOString().slice(0, 10);
}

const filter = reactive({
  startDate:   daysAgo(30),
  endDate:     today(),
  type:        null as number | null,
  stockSearch: '',
});

const inQty  = computed(() => data.value?.items.filter(r => r.qty > 0).reduce((s, r) => s + r.qty, 0).toFixed(2) ?? '0');
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
    data.value = await reportService.getStockMovements({
      startDate:   filter.startDate,
      endDate:     filter.endDate,
      stockSearch: filter.stockSearch || undefined,
      type:        filter.type,
      page,
      pageSize:    50,
    });
  } catch {
    notify.add('Stok hareketleri yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
}

async function changePage(page: number) {
  loading.value = true;
  try {
    data.value = await reportService.getStockMovements({
      startDate:   filter.startDate,
      endDate:     filter.endDate,
      stockSearch: filter.stockSearch || undefined,
      type:        filter.type,
      page,
      pageSize:    50,
    });
  } catch {
    notify.add('Yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleString('tr-TR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

// GoodsIn, VehicleReturn → yeşil  |  ShipmentOut, GoodsInCorrection → kırmızı  |  diğerleri → gri
function typeChipClass(typeId: number) {
  if (typeId === 0 || typeId === 5)
    return 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300';
  if (typeId === 1 || typeId === 6)
    return 'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300';
  if (typeId === 2)
    return 'bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-300';
  return 'bg-gray-100 dark:bg-white/10 text-gray-600 dark:text-gray-300';
}

onMounted(() => load(true));
</script>
