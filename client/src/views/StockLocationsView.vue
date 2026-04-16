<template>
  <div>
  <div class="space-y-4">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-gray-900 dark:text-white">Stok Lokasyon Haritası</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">Hangi stok hangi lokasyonda, ne kadar</p>
      </div>
      <div class="flex gap-2" v-if="canManage">
        <button
          @click="openTransferModal()"
          class="flex items-center gap-2 px-3 py-2 text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 rounded-lg transition-colors"
        >
          <ArrowsRightLeftIcon class="w-4 h-4" aria-hidden="true" />
          Transfer
        </button>
        <button
          @click="openAssignModal()"
          class="flex items-center gap-2 px-3 py-2 text-sm font-medium text-white bg-emerald-600 hover:bg-emerald-700 rounded-lg transition-colors"
        >
          <PlusIcon class="w-4 h-4" aria-hidden="true" />
          Lokasyona Ata
        </button>
      </div>
    </div>

    <!-- Tabs -->
    <div class="flex gap-1 bg-gray-100 dark:bg-gray-800 p-1 rounded-xl w-fit">
      <button
        v-for="tab in tabs"
        :key="tab.id"
        @click="activeTab = tab.id"
        class="px-4 py-1.5 text-sm font-medium rounded-lg transition-colors"
        :class="activeTab === tab.id
          ? 'bg-white dark:bg-gray-900 text-gray-900 dark:text-white shadow-sm'
          : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200'"
      >
        {{ tab.label }}
      </button>
    </div>

    <!-- Error -->
    <div v-if="error" class="mx-4 mt-4 p-3 bg-red-900/30 border border-red-700 rounded-lg flex items-center justify-between">
      <span class="text-red-400 text-sm">{{ error }}</span>
      <button @click="loadMap(); error = null" class="text-red-400 hover:text-red-300 text-sm underline ml-4">Tekrar dene</button>
    </div>

    <!-- ── TAB: Stok-Lokasyon ─────────────────────────────────────────────── -->
    <template v-if="activeTab === 'map'">
      <!-- Filters -->
      <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
        <div class="flex flex-wrap gap-3">
          <input
            v-model="searchQuery"
            placeholder="Stok kodu veya adı ara..."
            class="flex-1 min-w-[200px] text-sm px-3 py-1.5 bg-gray-50 dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg text-gray-700 dark:text-gray-200 focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
          <select
            v-model="filterZone"
            class="text-sm px-3 py-1.5 bg-gray-50 dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg text-gray-700 dark:text-gray-200 focus:outline-none focus:ring-2 focus:ring-indigo-500"
          >
            <option value="">Tüm Bölgeler</option>
            <option v-for="z in uniqueZones" :key="z" :value="z">Bölge {{ z }}</option>
          </select>
          <div class="ml-auto text-sm text-gray-400 self-center">{{ filteredItems.length }} kayıt</div>
        </div>
      </div>

      <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
        <div v-if="loading" class="flex justify-center py-12">
          <div class="w-6 h-6 border-2 border-indigo-500 border-t-transparent rounded-full animate-spin"></div>
        </div>
        <div v-else-if="filteredItems.length === 0" class="py-16 text-center text-sm text-gray-400">
          <ArchiveBoxIcon class="w-10 h-10 mx-auto mb-3 opacity-30" aria-hidden="true" />
          Stok-lokasyon kaydı bulunamadı
        </div>
        <div v-else class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 dark:bg-gray-900/50 border-b border-gray-200 dark:border-gray-700">
              <tr>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Stok Kodu</th>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Stok Adı</th>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Lokasyon</th>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider hidden sm:table-cell">Bölge</th>
                <th class="text-right px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Mevcut</th>
                <th class="text-right px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider hidden md:table-cell">Rezerve</th>
                <th class="text-right px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Kullanılabilir</th>
                <th class="text-right px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider hidden lg:table-cell">Son Hareket</th>
                <th v-if="canManage" class="text-right px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">İşlem</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-gray-700">
              <tr
                v-for="item in filteredItems"
                :key="item.id"
                class="hover:bg-gray-50 dark:hover:bg-gray-700/30 transition-colors"
              >
                <td class="px-4 py-3 font-mono text-xs font-semibold text-gray-900 dark:text-gray-100">{{ item.stockCode }}</td>
                <td class="px-4 py-3 text-gray-700 dark:text-gray-300 truncate max-w-[180px]">{{ item.stockName }}</td>
                <td class="px-4 py-3">
                  <span class="font-mono text-xs bg-indigo-50 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300 px-2 py-0.5 rounded">
                    {{ item.locationCode }}
                  </span>
                </td>
                <td class="px-4 py-3 hidden sm:table-cell">
                  <span class="text-xs font-bold text-gray-500">{{ item.zone }}</span>
                </td>
                <td class="px-4 py-3 text-right font-medium text-gray-900 dark:text-gray-100">
                  {{ item.onHandQty.toLocaleString('tr-TR', { maximumFractionDigits: 2 }) }}
                  <span class="text-xs text-gray-400 ml-0.5">{{ item.unit }}</span>
                </td>
                <td class="px-4 py-3 text-right hidden md:table-cell">
                  <span :class="item.reservedQty > 0 ? 'text-yellow-600 dark:text-yellow-400' : 'text-gray-400'">
                    {{ item.reservedQty.toLocaleString('tr-TR', { maximumFractionDigits: 2 }) }}
                  </span>
                </td>
                <td class="px-4 py-3 text-right font-semibold"
                  :class="item.availableQty <= 0 ? 'text-red-500' : 'text-emerald-600 dark:text-emerald-400'">
                  {{ item.availableQty.toLocaleString('tr-TR', { maximumFractionDigits: 2 }) }}
                </td>
                <td class="px-4 py-3 text-right text-xs text-gray-400 hidden lg:table-cell">
                  {{ item.lastMovedAt ? fmtDate(item.lastMovedAt) : '—' }}
                </td>
                <td v-if="canManage" class="px-4 py-3 text-right">
                  <button
                    @click="openTransferModal(item)"
                    class="text-xs text-indigo-600 dark:text-indigo-400 hover:underline"
                  >
                    Transfer
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>

    <!-- ── TAB: Transfer Geçmişi ──────────────────────────────────────────── -->
    <template v-else-if="activeTab === 'history'">
      <div class="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
        <div v-if="historyLoading" class="flex justify-center py-12">
          <div class="w-6 h-6 border-2 border-indigo-500 border-t-transparent rounded-full animate-spin"></div>
        </div>
        <div v-else-if="history.length === 0" class="py-16 text-center text-sm text-gray-400">
          Henüz transfer kaydı yok
        </div>
        <div v-else class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 dark:bg-gray-900/50 border-b border-gray-200 dark:border-gray-700">
              <tr>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Tarih</th>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Stok</th>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Kaynak</th>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Hedef</th>
                <th class="text-right px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider">Miktar</th>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider hidden md:table-cell">Not</th>
                <th class="text-left px-4 py-3 text-xs font-semibold text-gray-500 uppercase tracking-wider hidden lg:table-cell">Yapan</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-gray-700">
              <tr v-for="t in history" :key="t.id" class="hover:bg-gray-50 dark:hover:bg-gray-700/30">
                <td class="px-4 py-3 text-xs text-gray-500">{{ fmtDate(t.transferredAt) }}</td>
                <td class="px-4 py-3">
                  <p class="font-mono text-xs font-semibold text-gray-900 dark:text-gray-100">{{ t.stockCode }}</p>
                  <p class="text-xs text-gray-400 truncate max-w-[140px]">{{ t.stockName }}</p>
                </td>
                <td class="px-4 py-3 font-mono text-xs text-gray-600 dark:text-gray-300">{{ t.fromLocationCode }}</td>
                <td class="px-4 py-3">
                  <span class="flex items-center gap-1 font-mono text-xs text-indigo-600 dark:text-indigo-400">
                    <ArrowRightIcon class="w-3 h-3" aria-hidden="true" />
                    {{ t.toLocationCode }}
                  </span>
                </td>
                <td class="px-4 py-3 text-right font-semibold text-gray-900 dark:text-white">
                  {{ t.qty.toLocaleString('tr-TR', { maximumFractionDigits: 2 }) }}
                </td>
                <td class="px-4 py-3 text-xs text-gray-500 hidden md:table-cell">{{ t.note || '—' }}</td>
                <td class="px-4 py-3 text-xs text-gray-500 hidden lg:table-cell">{{ t.transferredBy || '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <!-- History Pagination -->
        <div v-if="historyTotal > historyPageSize" class="px-4 py-3 border-t border-gray-100 dark:border-gray-700 flex items-center justify-between text-sm text-gray-500">
          <span>{{ (historyPage - 1) * historyPageSize + 1 }}–{{ Math.min(historyPage * historyPageSize, historyTotal) }} / {{ historyTotal }}</span>
          <div class="flex gap-1">
            <button :disabled="historyPage === 1" @click="historyPage--; loadHistory()"
              class="px-2 py-1 rounded border border-gray-200 dark:border-gray-700 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-gray-700">‹</button>
            <button :disabled="historyPage * historyPageSize >= historyTotal" @click="historyPage++; loadHistory()"
              class="px-2 py-1 rounded border border-gray-200 dark:border-gray-700 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-gray-700">›</button>
          </div>
        </div>
      </div>
    </template>
  </div>

  <!-- ─── Assign Modal ──────────────────────────────────────────────────────── -->
  <BaseModal :show="showAssignModal" title="Lokasyona Stok Ata" maxWidth="sm" @close="showAssignModal = false">
    <div class="space-y-4">
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Stok *</label>
        <select v-model.number="assignForm.stockMasterId"
          class="w-full px-3 py-2 text-sm rounded-input border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500">
          <option value="">Seçin...</option>
          <option v-for="s in stockOptions" :key="s.id" :value="s.id">{{ s.stockCode }} — {{ s.stockName }}</option>
        </select>
      </div>
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Lokasyon *</label>
        <select v-model.number="assignForm.warehouseLocationId"
          class="w-full px-3 py-2 text-sm rounded-input border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500">
          <option value="">Seçin...</option>
          <option v-for="l in locationOptions" :key="l.id" :value="l.id">{{ l.code }}</option>
        </select>
      </div>
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Miktar *</label>
        <input v-model.number="assignForm.qty" type="number" min="0" step="0.01" placeholder="0"
          class="w-full px-3 py-2 text-sm rounded-input border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500" />
        <p class="text-xs text-gray-400 mt-1">Mevcut değerin üzerine yazılır.</p>
      </div>
    </div>
    <template #footer>
      <button @click="showAssignModal = false"
        class="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">İptal</button>
      <button @click="saveAssign" :disabled="saving"
        class="px-4 py-2 text-sm font-medium text-white bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 rounded-lg">
        {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
      </button>
    </template>
  </BaseModal>

  <!-- ─── Transfer Modal ────────────────────────────────────────────────────── -->
  <BaseModal :show="showTransferModal" title="Lokasyonlar Arası Transfer" maxWidth="sm" @close="showTransferModal = false">
    <div class="space-y-4">
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Stok *</label>
        <select v-model.number="transferForm.stockMasterId" @change="onTransferStockChange"
          class="w-full px-3 py-2 text-sm rounded-input border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500">
          <option value="">Seçin...</option>
          <option v-for="s in stockOptions" :key="s.id" :value="s.id">{{ s.stockCode }} — {{ s.stockName }}</option>
        </select>
      </div>
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Kaynak Lokasyon *</label>
        <select v-model.number="transferForm.fromLocationId"
          class="w-full px-3 py-2 text-sm rounded-input border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500">
          <option value="">Seçin...</option>
          <option v-for="sl in stockLocationOptions" :key="sl.warehouseLocationId" :value="sl.warehouseLocationId">
            {{ sl.locationCode }} (Mevcut: {{ sl.availableQty }})
          </option>
        </select>
      </div>
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Hedef Lokasyon *</label>
        <select v-model.number="transferForm.toLocationId"
          class="w-full px-3 py-2 text-sm rounded-input border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500">
          <option value="">Seçin...</option>
          <option v-for="l in locationOptions" :key="l.id" :value="l.id"
            :disabled="l.id === transferForm.fromLocationId">
            {{ l.code }}
          </option>
        </select>
      </div>
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Miktar *</label>
        <input v-model.number="transferForm.qty" type="number" min="0.01" step="0.01"
          class="w-full px-3 py-2 text-sm rounded-input border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500" />
      </div>
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Not</label>
        <input v-model="transferForm.note" placeholder="Opsiyonel"
          class="w-full px-3 py-2 text-sm rounded-input border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500" />
      </div>
    </div>
    <template #footer>
      <button @click="showTransferModal = false"
        class="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">İptal</button>
      <button @click="saveTransfer" :disabled="saving"
        class="px-4 py-2 text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 rounded-lg">
        {{ saving ? 'Kaydediliyor...' : 'Transfer Et' }}
      </button>
    </template>
  </BaseModal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, reactive } from 'vue';
import { PlusIcon, ArrowsRightLeftIcon, ArrowRightIcon, ArchiveBoxIcon } from '@heroicons/vue/24/outline';
import { useAuthStore } from '../stores/auth';
import { useNotificationStore } from '../stores/notification';
import stockLocationService, { type StockLocationDto, type TransferHistoryDto } from '../services/stockLocationService';
import warehouseLocationService from '../services/warehouseLocationService';
import { stockService } from '../services/stockService';
import { ApiErrorUtils } from '../utils/apiError';
import { turkishIncludes } from '../utils/turkishSearch';
import BaseModal from '../components/BaseModal.vue';

const authStore  = useAuthStore();
const notify     = useNotificationStore();
const canManage  = computed(() => ['Admin', 'Manager', 'Warehouse'].includes(authStore.userRole));

// ── Tabs ─────────────────────────────────────────────────────────────────────
const tabs: { id: 'map' | 'history'; label: string }[] = [
  { id: 'map',     label: 'Stok-Lokasyon Haritası' },
  { id: 'history', label: 'Transfer Geçmişi' },
];
const activeTab = ref<'map' | 'history'>('map');

// ── Map Tab ───────────────────────────────────────────────────────────────────
const items    = ref<StockLocationDto[]>([]);
const loading  = ref(false);
const error    = ref<string | null>(null);
const searchQuery = ref('');
const filterZone  = ref('');

const uniqueZones = computed(() => [...new Set(items.value.map(i => i.zone))].sort());

const filteredItems = computed(() => {
  let list = items.value;
  if (filterZone.value)
    list = list.filter(i => i.zone === filterZone.value);
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.trim();
    list = list.filter(i =>
      turkishIncludes(i.stockCode, q) ||
      turkishIncludes(i.stockName, q)
    );
  }
  return list;
});

async function loadMap() {
  loading.value = true;
  try {
    items.value = await stockLocationService.getAll();
  } catch (e) {
    error.value = ApiErrorUtils.getErrorMessage(e) || 'Lokasyon haritası yüklenemedi.';
    notify.add(error.value, 'error');
  } finally {
    loading.value = false;
  }
}

// ── History Tab ───────────────────────────────────────────────────────────────
const history       = ref<TransferHistoryDto[]>([]);
const historyLoading = ref(false);
const historyPage    = ref(1);
const historyPageSize = ref(50);
const historyTotal   = ref(0);

async function loadHistory() {
  historyLoading.value = true;
  try {
    const res = await stockLocationService.getTransferHistory({
      page: historyPage.value,
      pageSize: historyPageSize.value,
    });
    history.value = res.items;
    historyTotal.value = res.totalCount;
  } catch {
    notify.add('Transfer geçmişi yüklenemedi.', 'error');
  } finally {
    historyLoading.value = false;
  }
}

// ── Reference data ────────────────────────────────────────────────────────────
const stockOptions    = ref<{ id: number; stockCode: string; stockName: string }[]>([]);
const locationOptions = ref<{ id: number; code: string }[]>([]);
const stockLocationOptions = ref<StockLocationDto[]>([]);

async function loadRefData() {
  const [stocksRes, locsRes] = await Promise.all([
    stockService.getAll({ page: 1, size: 500 }),
    warehouseLocationService.getAll({ pageSize: 1000 }),
  ]);
  stockOptions.value    = stocksRes.items.map((s: any) => ({ id: s.id, stockCode: s.stockCode, stockName: s.stockName }));
  locationOptions.value = locsRes.items.map((l: any) => ({ id: l.id, code: l.code }));
}

// ── Assign Modal ──────────────────────────────────────────────────────────────
const showAssignModal = ref(false);
const saving          = ref(false);
const assignForm = reactive({ stockMasterId: 0, warehouseLocationId: 0, qty: 0 });

function openAssignModal() {
  Object.assign(assignForm, { stockMasterId: 0, warehouseLocationId: 0, qty: 0 });
  showAssignModal.value = true;
}

async function saveAssign() {
  saving.value = true;
  try {
    await stockLocationService.assign(assignForm.stockMasterId, assignForm.warehouseLocationId, assignForm.qty);
    notify.add('Lokasyon ataması kaydedildi.', 'success');
    showAssignModal.value = false;
    await loadMap();
  } catch (e: any) {
    notify.add(e?.message ?? 'Hata oluştu.', 'error');
  } finally {
    saving.value = false;
  }
}

// ── Transfer Modal ────────────────────────────────────────────────────────────
const showTransferModal = ref(false);
const transferForm = reactive({
  stockMasterId: 0,
  fromLocationId: 0,
  toLocationId: 0,
  qty: 0,
  note: '',
});

function openTransferModal(item?: StockLocationDto) {
  Object.assign(transferForm, { stockMasterId: 0, fromLocationId: 0, toLocationId: 0, qty: 0, note: '' });
  if (item) {
    transferForm.stockMasterId  = item.stockMasterId;
    transferForm.fromLocationId = item.warehouseLocationId;
    stockLocationOptions.value  = items.value.filter(i => i.stockMasterId === item.stockMasterId);
  }
  showTransferModal.value = true;
}

async function onTransferStockChange() {
  transferForm.fromLocationId = 0;
  if (transferForm.stockMasterId) {
    stockLocationOptions.value = items.value.filter(i => i.stockMasterId === transferForm.stockMasterId);
  }
}

async function saveTransfer() {
  saving.value = true;
  try {
    await stockLocationService.transfer({
      stockMasterId: transferForm.stockMasterId,
      fromLocationId: transferForm.fromLocationId,
      toLocationId: transferForm.toLocationId,
      qty: transferForm.qty,
      note: transferForm.note || undefined,
    });
    notify.add('Transfer başarılı.', 'success');
    showTransferModal.value = false;
    await Promise.all([loadMap(), loadHistory()]);
  } catch (e: any) {
    notify.add(e?.message ?? 'Hata oluştu.', 'error');
  } finally {
    saving.value = false;
  }
}

// ── Helpers ───────────────────────────────────────────────────────────────────
const fmtDate = (d: string) =>
  new Date(d).toLocaleString('tr-TR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });

// ── Init ──────────────────────────────────────────────────────────────────────
onMounted(() => {
  loadMap();
  loadHistory();
  loadRefData();
});
</script>
