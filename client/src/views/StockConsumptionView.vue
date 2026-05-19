<template>
  <div class="space-y-5">

    <!-- Header -->
    <div class="flex flex-wrap items-center justify-between gap-3">
      <h1 class="text-xl font-bold text-gray-900 dark:text-gray-100">Stok Tüketim / Zai</h1>
      <div class="flex gap-2">
        <button @click="exportExcel"
          class="flex items-center gap-1.5 px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300 transition-colors">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
          Excel
        </button>
        <button v-role="['Admin','Manager','Warehouse']" @click="openForm()"
          class="flex items-center gap-1.5 px-3 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Kayıt Ekle
        </button>
      </div>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
      <div class="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <div>
          <label class="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">Başlangıç</label>
          <input type="date" v-model="filters.fromDate"
            class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-1.5 text-sm bg-white dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">Bitiş</label>
          <input type="date" v-model="filters.toDate"
            class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-1.5 text-sm bg-white dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">Tip</label>
          <select v-model="filters.type"
            class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-1.5 text-sm bg-white dark:bg-gray-800 dark:text-gray-100">
            <option :value="undefined">Tümü</option>
            <option :value="0">Zai</option>
            <option :value="1">Dahili Kullanım</option>
            <option :value="2">Depo Satışı</option>
          </select>
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">Arama</label>
          <input v-model="filters.search" type="text" placeholder="Stok adı / kodu / kişi…"
            @keydown.enter="load"
            class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-1.5 text-sm bg-white dark:bg-gray-800 dark:text-gray-100" />
        </div>
      </div>
      <div class="mt-3 flex gap-2 justify-end">
        <button @click="resetFilters"
          class="px-3 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 text-gray-600 dark:text-gray-400">
          Temizle
        </button>
        <button @click="load" :disabled="loading"
          class="px-4 py-1.5 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-50">
          Filtrele
        </button>
      </div>
    </div>

    <!-- Özet Kartlar -->
    <div class="grid grid-cols-3 gap-3">
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 text-center">
        <p class="text-2xl font-bold text-amber-600 dark:text-amber-400">{{ summaryZai }}</p>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Zai Kayıt</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 text-center">
        <p class="text-2xl font-bold text-blue-600 dark:text-blue-400">{{ summaryDahili }}</p>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Dahili Kullanım</p>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 text-center">
        <p class="text-2xl font-bold text-green-600 dark:text-green-400">{{ summaryFmt }}</p>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Depo Satış Tutarı</p>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div v-if="loading" class="flex justify-center py-10">
        <div class="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
      </div>

      <div v-else-if="items.length === 0" class="text-center py-10 text-gray-500 dark:text-gray-400 text-sm">
        Kayıt bulunamadı.
      </div>

      <div v-else class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Tarih</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Tip</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Stok</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Miktar</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Detay</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden md:table-cell">Tutar</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden lg:table-cell">Kaydeden</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">İşlem</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
            <tr v-for="item in items" :key="item.id"
              :class="{
                'bg-amber-50 dark:bg-amber-900/10': item.typeValue === 0,
                'bg-blue-50/30 dark:bg-blue-900/5': item.typeValue === 1,
                'bg-green-50/30 dark:bg-green-900/5': item.typeValue === 2,
              }">
              <td class="px-4 py-3 text-gray-900 dark:text-gray-100 whitespace-nowrap">
                {{ fmtDate(item.date) }}
              </td>
              <td class="px-4 py-3">
                <span class="px-2 py-0.5 rounded-full text-xs font-semibold"
                  :class="{
                    'bg-amber-100 text-amber-800 dark:bg-amber-900/30 dark:text-amber-300': item.typeValue === 0,
                    'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300': item.typeValue === 1,
                    'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300': item.typeValue === 2,
                  }">
                  {{ item.typeLabel }}
                </span>
              </td>
              <td class="px-4 py-3">
                <p class="font-medium text-gray-900 dark:text-gray-100 truncate max-w-[200px]">{{ item.stockName }}</p>
                <p class="text-xs text-gray-500 dark:text-gray-400">{{ item.stockCode }}</p>
              </td>
              <td class="px-4 py-3 text-right font-medium text-gray-900 dark:text-gray-100 whitespace-nowrap">
                {{ item.quantity }} {{ item.unit }}
              </td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 max-w-[180px]">
                <template v-if="item.typeValue === 0">
                  <span class="text-xs">{{ item.reason }}</span>
                </template>
                <template v-else-if="item.typeValue === 1">
                  <span class="text-xs font-medium">{{ item.recipientName }}</span>
                </template>
                <template v-else>
                  <span class="text-xs">{{ fmtCurrency(item.salePrice) }} / {{ item.unit }}</span>
                </template>
                <p v-if="item.note" class="text-[11px] text-gray-400 dark:text-gray-500 italic mt-0.5 truncate">{{ item.note }}</p>
              </td>
              <td class="px-4 py-3 text-right text-gray-700 dark:text-gray-300 hidden md:table-cell whitespace-nowrap">
                {{ item.totalSaleAmount ? fmtCurrency(item.totalSaleAmount) : '—' }}
              </td>
              <td class="px-4 py-3 text-xs text-gray-500 dark:text-gray-400 hidden lg:table-cell">{{ item.createdBy }}</td>
              <td class="px-4 py-3 text-right">
                <button v-role="['Admin','Manager']" @click="confirmDelete(item)"
                  class="text-xs text-red-500 hover:text-red-700 dark:text-red-400 dark:hover:text-red-300">
                  Sil
                </button>
              </td>
            </tr>
          </tbody>
        </table>

        <!-- Pagination -->
        <div class="px-4 py-3 flex items-center justify-between border-t border-gray-200 dark:border-gray-700">
          <p class="text-xs text-gray-500 dark:text-gray-400">Toplam {{ totalCount }} kayıt</p>
          <div class="flex gap-2">
            <button @click="prevPage" :disabled="page === 1 || loading"
              class="px-3 py-1 text-xs border dark:border-gray-700 rounded disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-gray-800">‹ Önceki</button>
            <span class="px-3 py-1 text-xs">{{ page }} / {{ totalPages }}</span>
            <button @click="nextPage" :disabled="page >= totalPages || loading"
              class="px-3 py-1 text-xs border dark:border-gray-700 rounded disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-gray-800">Sonraki ›</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Add Form Modal -->
    <Teleport to="body">
      <div v-if="showForm" class="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4" @click.self="showForm = false">
        <div class="bg-white dark:bg-gray-900 rounded-2xl shadow-2xl w-full max-w-lg">
          <div class="flex items-center justify-between px-6 py-4 border-b border-gray-200 dark:border-gray-700">
            <h2 class="text-base font-semibold text-gray-900 dark:text-gray-100">Tüketim Kaydı Ekle</h2>
            <button @click="showForm = false" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <form @submit.prevent="submit" class="px-6 py-5 space-y-4">
            <!-- Stok -->
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Stok <span class="text-red-500">*</span></label>
              <input v-model="stockSearch" type="text" placeholder="Stok adı veya kodu ile ara…"
                @input="onStockSearch"
                class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100" />
              <div v-if="stockResults.length > 0 && !form.stockMasterId"
                class="mt-1 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-800 max-h-40 overflow-y-auto shadow-lg">
                <button v-for="s in stockResults" :key="s.id" type="button" @click="selectStock(s)"
                  class="w-full text-left px-3 py-2 text-sm hover:bg-gray-50 dark:hover:bg-gray-700 border-b last:border-0 border-gray-100 dark:border-gray-700">
                  <span class="font-medium text-gray-900 dark:text-gray-100">{{ s.stockName }}</span>
                  <span class="text-xs text-gray-500 dark:text-gray-400 ml-2">{{ s.stockCode }}</span>
                </button>
              </div>
              <p v-if="form.stockMasterId" class="mt-1 text-xs text-green-600 dark:text-green-400">✓ {{ stockSearch }}</p>
            </div>

            <!-- Tip -->
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Tip <span class="text-red-500">*</span></label>
              <div class="grid grid-cols-3 gap-2">
                <button v-for="(label, val) in typeOptions" :key="val" type="button"
                  @click="form.type = Number(val) as 0|1|2"
                  class="py-2 rounded-lg text-sm font-medium border transition-colors"
                  :class="form.type === Number(val)
                    ? 'bg-blue-600 text-white border-blue-600'
                    : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 border-gray-300 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700'">
                  {{ label }}
                </button>
              </div>
            </div>

            <!-- Miktar + Tarih -->
            <div class="grid grid-cols-2 gap-3">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Miktar <span class="text-red-500">*</span></label>
                <input v-model.number="form.quantity" type="number" step="0.001" min="0.001"
                  class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Tarih <span class="text-red-500">*</span></label>
                <input v-model="form.date" type="date"
                  class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100" />
              </div>
            </div>

            <!-- Zai: Sebep -->
            <div v-if="form.type === 0">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Sebep <span class="text-red-500">*</span></label>
              <input v-model="form.reason" type="text" placeholder="Zai sebebini girin…"
                class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100" />
            </div>

            <!-- Dahili: Teslim Alan -->
            <div v-if="form.type === 1">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Teslim Alan <span class="text-red-500">*</span></label>
              <input v-model="form.recipientName" type="text" placeholder="Ad Soyad…"
                class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100" />
            </div>

            <!-- Depo Satış: Fiyat -->
            <div v-if="form.type === 2">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Satış Fiyatı (₺) <span class="text-red-500">*</span></label>
              <input v-model.number="form.salePrice" type="number" step="0.01" min="0.01" placeholder="0.00"
                class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100" />
            </div>

            <!-- Not -->
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Not</label>
              <input v-model="form.note" type="text" placeholder="İsteğe bağlı…"
                class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100" />
            </div>

            <p v-if="formError" class="text-sm text-red-600 dark:text-red-400">{{ formError }}</p>

            <div class="flex justify-end gap-3 pt-1">
              <button type="button" @click="showForm = false"
                class="px-4 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800">
                İptal
              </button>
              <button type="submit" :disabled="submitting"
                class="px-5 py-2 text-sm bg-blue-600 hover:bg-blue-700 text-white rounded-lg disabled:opacity-50">
                {{ submitting ? 'Kaydediliyor…' : 'Kaydet' }}
              </button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import stockConsumptionService, { type StockConsumptionDto } from '../services/stockConsumptionService';
import apiClient from '../services/apiClient';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';

const notificationStore = useNotificationStore();

// ── State ──────────────────────────────────────────────
const loading = ref(false);
const items = ref<StockConsumptionDto[]>([]);
const totalCount = ref(0);
const totalPages = ref(1);
const page = ref(1);

const filters = ref({
  fromDate: '',
  toDate: '',
  type: undefined as number | undefined,
  search: '',
});

// ── Load ───────────────────────────────────────────────
async function load() {
  loading.value = true;
  try {
    const res = await stockConsumptionService.getAll({
      fromDate: filters.value.fromDate || undefined,
      toDate: filters.value.toDate || undefined,
      type: filters.value.type,
      search: filters.value.search || undefined,
      page: page.value,
      size: 30,
    });
    items.value = res.items;
    totalCount.value = res.totalCount;
    totalPages.value = res.totalPages;
  } finally {
    loading.value = false;
  }
}

function resetFilters() {
  filters.value = { fromDate: '', toDate: '', type: undefined, search: '' };
  page.value = 1;
  load();
}

function prevPage() { if (page.value > 1) { page.value--; load(); } }
function nextPage() { if (page.value < totalPages.value) { page.value++; load(); } }

// ── Summary ────────────────────────────────────────────
const summaryZai = computed(() => items.value.filter(i => i.typeValue === 0).length);
const summaryDahili = computed(() => items.value.filter(i => i.typeValue === 1).length);
const summaryFmt = computed(() => {
  const total = items.value
    .filter(i => i.typeValue === 2 && i.totalSaleAmount)
    .reduce((s, i) => s + (i.totalSaleAmount ?? 0), 0);
  return fmtCurrency(total);
});

// ── Export ─────────────────────────────────────────────
function exportExcel() {
  const url = stockConsumptionService.getExportUrl({
    fromDate: filters.value.fromDate || undefined,
    toDate: filters.value.toDate || undefined,
    type: filters.value.type,
    search: filters.value.search || undefined,
  });
  window.open(url, '_blank');
}

// ── Delete ─────────────────────────────────────────────
async function confirmDelete(item: StockConsumptionDto) {
  const ok = confirm(`"${item.stockName}" kaydını silmek istiyor musunuz?`);
  if (!ok) return;
  try {
    await stockConsumptionService.delete(item.id);
    notificationStore.add('Kayıt silindi.', 'success');
    load();
  } catch (e: any) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Silme başarısız.', 'error');
  }
}

// ── Form ───────────────────────────────────────────────
const showForm = ref(false);
const submitting = ref(false);
const formError = ref('');

const typeOptions: Record<string, string> = { '0': 'Zai', '1': 'Dahili Kullanım', '2': 'Depo Satışı' };

const form = ref({
  stockMasterId: 0,
  type: 0 as 0 | 1 | 2,
  quantity: undefined as number | undefined,
  date: new Date().toISOString().slice(0, 10),
  reason: '',
  recipientName: '',
  salePrice: undefined as number | undefined,
  note: '',
});

function openForm() {
  form.value = {
    stockMasterId: 0, type: 0, quantity: undefined,
    date: new Date().toISOString().slice(0, 10),
    reason: '', recipientName: '', salePrice: undefined, note: '',
  };
  stockSearch.value = '';
  stockResults.value = [];
  formError.value = '';
  showForm.value = true;
}

async function submit() {
  formError.value = '';
  if (!form.value.stockMasterId) { formError.value = 'Lütfen bir stok seçin.'; return; }
  if (!form.value.quantity || form.value.quantity <= 0) { formError.value = 'Geçerli bir miktar girin.'; return; }
  if (form.value.type === 0 && !form.value.reason.trim()) { formError.value = 'Zai için sebep zorunludur.'; return; }
  if (form.value.type === 1 && !form.value.recipientName.trim()) { formError.value = 'Teslim alan kişi zorunludur.'; return; }
  if (form.value.type === 2 && (!form.value.salePrice || form.value.salePrice <= 0)) { formError.value = 'Satış fiyatı zorunludur.'; return; }

  submitting.value = true;
  try {
    await stockConsumptionService.create({
      stockMasterId: form.value.stockMasterId,
      type: form.value.type,
      quantity: form.value.quantity!,
      date: form.value.date,
      reason: form.value.type === 0 ? form.value.reason : undefined,
      recipientName: form.value.type === 1 ? form.value.recipientName : undefined,
      salePrice: form.value.type === 2 ? form.value.salePrice : undefined,
      note: form.value.note || undefined,
    });
    notificationStore.add('Kayıt eklendi.', 'success');
    showForm.value = false;
    load();
  } catch (e: any) {
    formError.value = ApiErrorUtils.getErrorMessage(e) || 'Kayıt başarısız.';
  } finally {
    submitting.value = false;
  }
}

// ── Stock Search ───────────────────────────────────────
const stockSearch = ref('');
const stockResults = ref<{ id: number; stockCode: string; stockName: string }[]>([]);
let searchTimer: ReturnType<typeof setTimeout> | null = null;

function onStockSearch() {
  form.value.stockMasterId = 0;
  if (searchTimer) clearTimeout(searchTimer);
  if (!stockSearch.value.trim()) { stockResults.value = []; return; }
  searchTimer = setTimeout(async () => {
    const res = await apiClient.get('/stocks', { params: { search: stockSearch.value, size: 8 } });
    stockResults.value = (res.data.items ?? res.data).map((s: any) => ({
      id: s.id,
      stockCode: s.stockCode,
      stockName: s.stockName,
    }));
  }, 300);
}

function selectStock(s: { id: number; stockCode: string; stockName: string }) {
  form.value.stockMasterId = s.id;
  stockSearch.value = `${s.stockName} (${s.stockCode})`;
  stockResults.value = [];
}

// ── Helpers ────────────────────────────────────────────
function fmtDate(d: string) {
  if (!d) return '';
  const [y, m, day] = d.split('-');
  return `${day}.${m}.${y}`;
}

function fmtCurrency(val?: number | null) {
  if (val === undefined || val === null) return '—';
  return new Intl.NumberFormat('tr-TR', { style: 'currency', currency: 'TRY' }).format(val);
}

onMounted(load);
</script>
