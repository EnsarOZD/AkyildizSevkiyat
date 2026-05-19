<template>
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
    <PageHeader title="Araç İade Takibi" subtitle="Seferden dönen araçlarda kalan ürünlerin kaydı ve çözümü" color="blue">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0zM3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z" />
        </svg>
      </template>
      <template #actions>
        <button
          v-role="['Admin', 'Manager', 'Accounting', 'Warehouse']"
          @click="openCreateModal"
          class="px-4 py-2 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700 transition text-sm"
        >
          + Yeni İade Kaydı
        </button>
      </template>
    </PageHeader>

    <!-- Filters -->
    <div class="flex gap-3 mb-4 flex-wrap">
      <select v-model="filterLineStatus" class="border dark:border-gray-700 rounded px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100">
        <option value="">Tüm Durumlar</option>
        <option value="0">Beklemede</option>
        <option value="1">Stoğa Eklendi</option>
        <option value="2">Sevkiyata Eşleşti</option>
      </select>
      <input v-model="filterFromDate" type="date" class="border dark:border-gray-700 rounded px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100" />
      <input v-model="filterToDate" type="date" class="border dark:border-gray-700 rounded px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100" />
      <button @click="filterAndLoad" class="px-4 py-2 bg-gray-600 text-white rounded text-sm hover:bg-gray-700">Filtrele</button>
    </div>

    <!-- Summary Cards -->
    <div class="grid grid-cols-3 gap-4 mb-6">
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 border-l-4 border-orange-400">
        <div class="text-2xl font-bold text-orange-600">{{ totalPendingLines }}</div>
        <div class="text-xs text-gray-500 dark:text-gray-400 mt-1">Bekleyen Satır</div>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 border-l-4 border-green-400">
        <div class="text-2xl font-bold text-green-600">{{ totalAddedToStock }}</div>
        <div class="text-xs text-gray-500 dark:text-gray-400 mt-1">Stoğa Eklendi</div>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 border-l-4 border-blue-400">
        <div class="text-2xl font-bold text-blue-600">{{ totalMatchedToShipment }}</div>
        <div class="text-xs text-gray-500 dark:text-gray-400 mt-1">Sevkiyata Eşleşti</div>
      </div>
    </div>

    <!-- List -->
    <div v-if="loading" class="text-center py-12 text-gray-500 dark:text-gray-400">Yükleniyor...</div>
    <div v-else-if="returns.length === 0" class="text-center py-12 text-gray-400">Kayıt bulunamadı.</div>
    <div v-else class="space-y-4">
      <div
        v-for="r in returns"
        :key="r.id"
        class="bg-white dark:bg-gray-900 rounded-lg shadow border border-gray-200 dark:border-gray-700 overflow-hidden"
      >
        <!-- Return Header -->
        <div
          class="flex items-center justify-between px-4 py-3 bg-gray-50 dark:bg-gray-800 cursor-pointer select-none"
          @click="toggleExpand(r.id)"
        >
          <div class="flex items-center gap-4 flex-wrap">
            <span class="font-semibold text-gray-800 dark:text-gray-100">{{ r.driverName }}</span>
            <span class="text-sm text-gray-500 dark:text-gray-400 font-mono">{{ r.plateNumber }}</span>
            <span class="text-sm text-gray-500 dark:text-gray-400">{{ formatDate(r.returnDate) }}</span>
            <span v-if="r.note" class="text-xs text-gray-400 italic truncate max-w-xs">{{ r.note }}</span>
          </div>
          <div class="flex items-center gap-3 shrink-0">
            <span v-if="r.pendingLines > 0" class="text-xs font-semibold px-2 py-0.5 rounded-full bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-300">
              {{ r.pendingLines }} bekliyor
            </span>
            <span class="text-xs text-gray-400">{{ r.totalLines }} kalem</span>
            <svg
              :class="['h-4 w-4 text-gray-400 transition-transform', expandedIds.has(r.id) ? 'rotate-180' : '']"
              fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
            </svg>
          </div>
        </div>

        <!-- Lines Table (expanded) -->
        <div v-if="expandedIds.has(r.id)" class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-2 text-left font-medium text-gray-500 dark:text-gray-400">Stok Kodu</th>
                <th class="px-4 py-2 text-left font-medium text-gray-500 dark:text-gray-400">Stok Adı</th>
                <th class="px-4 py-2 text-right font-medium text-gray-500 dark:text-gray-400">Miktar</th>
                <th class="px-4 py-2 text-left font-medium text-gray-500 dark:text-gray-400">Not</th>
                <th class="px-4 py-2 text-left font-medium text-gray-500 dark:text-gray-400">Durum</th>
                <th class="px-4 py-2 text-right font-medium text-gray-500 dark:text-gray-400">İşlem</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr v-for="line in r.lines" :key="line.id" :class="line.status === 'Pending' ? 'bg-orange-50 dark:bg-orange-950/20' : ''">
                <td class="px-4 py-2 font-mono text-gray-700 dark:text-gray-300">
                  {{ line.stockCode }}
                  <span v-if="!line.isLinkedToStock" class="ml-1 text-xs text-yellow-600 bg-yellow-100 dark:bg-yellow-900 dark:text-yellow-300 px-1 rounded">serbest</span>
                </td>
                <td class="px-4 py-2 text-gray-700 dark:text-gray-300">{{ line.stockName }}</td>
                <td class="px-4 py-2 text-right font-semibold">{{ line.qty }}</td>
                <td class="px-4 py-2 text-gray-500 dark:text-gray-400 text-xs">{{ line.note || '—' }}</td>
                <td class="px-4 py-2">
                  <span :class="lineStatusClass(line.status)" class="text-xs px-2 py-0.5 rounded-full font-medium">
                    {{ lineStatusLabel(line.status) }}
                  </span>
                  <span v-if="line.linkedShipmentId" class="ml-2 text-xs text-gray-500 dark:text-gray-400">
                    {{ line.linkedShipmentIrsaliyeNo ? `İrsaliye: ${line.linkedShipmentIrsaliyeNo}` : `#${line.linkedShipmentId}` }}
                  </span>
                </td>
                <td class="px-4 py-2 text-right">
                  <button
                    v-if="line.status === 'Pending'"
                    v-role="['Admin', 'Manager', 'Accounting']"
                    @click="openResolveModal(r, line)"
                    class="text-xs px-3 py-1 bg-blue-600 text-white rounded hover:bg-blue-700"
                  >
                    Çözüme Kavuştur
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="flex items-center justify-between px-1 py-2 mt-4">
      <span class="text-sm text-gray-500 dark:text-gray-400">Toplam {{ totalCount }} kayıt</span>
      <div class="flex items-center gap-1">
        <button
          @click="goToPage(currentPage - 1)"
          :disabled="currentPage <= 1"
          class="px-3 py-1.5 rounded text-sm border dark:border-gray-700 disabled:opacity-40 hover:bg-gray-100 dark:hover:bg-gray-800"
        >‹</button>
        <span class="px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300">{{ currentPage }} / {{ totalPages }}</span>
        <button
          @click="goToPage(currentPage + 1)"
          :disabled="currentPage >= totalPages"
          class="px-3 py-1.5 rounded text-sm border dark:border-gray-700 disabled:opacity-40 hover:bg-gray-100 dark:hover:bg-gray-800"
        >›</button>
      </div>
    </div>

    <!-- Create Modal -->
    <BaseModal :show="showCreateModal" title="Yeni Araç İade Kaydı" maxWidth="lg" @close="showCreateModal = false">
      <div class="space-y-4">
        <!-- Session Selector -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Sefer <span class="text-red-500">*</span></label>
          <div v-if="loadingSessions" class="text-sm text-gray-400">Seferler yükleniyor...</div>
          <select
            v-else
            v-model="createForm.driverSessionId"
            class="w-full border rounded px-3 py-2 focus:ring-2 focus:ring-blue-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 text-sm"
          >
            <option value="">Sefer seçin...</option>
            <option v-for="s in sessionOptions" :key="s.id" :value="s.id">
              {{ s.driverName }} — {{ s.plateNumber }} — {{ formatDate(s.startTime) }}
              <template v-if="s.status === 'Open'"> (Açık)</template>
            </option>
          </select>
        </div>

        <!-- Return Date -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">İade Tarihi</label>
          <input v-model="createForm.returnDate" type="date" class="w-full border rounded px-3 py-2 focus:ring-2 focus:ring-blue-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 text-sm" />
        </div>

        <!-- Note -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Not</label>
          <input v-model="createForm.note" type="text" class="w-full border rounded px-3 py-2 focus:ring-2 focus:ring-blue-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 text-sm" placeholder="Opsiyonel..." />
        </div>

        <!-- Lines -->
        <div>
          <div class="flex items-center justify-between mb-2">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Ürün Satırları <span class="text-red-500">*</span></label>
            <button type="button" @click="addLine" class="text-xs text-blue-500 hover:underline">+ Satır Ekle</button>
          </div>
          <div v-for="(line, idx) in createForm.lines" :key="idx" class="flex gap-2 items-start mb-2 p-2 bg-gray-50 dark:bg-gray-800 rounded border dark:border-gray-700">
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2 mb-1">
                <span class="text-xs text-gray-500 dark:text-gray-400">Stok</span>
                <button type="button" @click="line.useManual = !line.useManual" class="text-xs text-blue-500 hover:underline">
                  {{ line.useManual ? 'Katalogdan Seç' : 'Manuel Giriş' }}
                </button>
              </div>
              <template v-if="!line.useManual">
                <StockCombobox :operationType="0" :placeholder="'Stok kodu veya adı ara...'" @select="(s: any) => onLineStockSelect(idx, s)" />
                <div v-if="line.stockNameFree" class="mt-0.5 text-xs text-gray-500 dark:text-gray-400">
                  Seçili: <span class="font-medium">{{ line.stockCodeFree }} — {{ line.stockNameFree }}</span>
                </div>
              </template>
              <template v-else>
                <input v-model="line.stockCodeFree" type="text" placeholder="Stok kodu" class="w-full border rounded px-2 py-1.5 text-xs mb-1 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-100" />
                <input v-model="line.stockNameFree" type="text" placeholder="Stok adı" class="w-full border rounded px-2 py-1.5 text-xs dark:bg-gray-700 dark:border-gray-600 dark:text-gray-100" />
              </template>
            </div>
            <div class="w-20 shrink-0">
              <span class="text-xs text-gray-500 dark:text-gray-400 mb-1 block">Miktar</span>
              <input v-model.number="line.qty" type="number" min="0.01" step="0.01" class="w-full border rounded px-2 py-1.5 text-xs dark:bg-gray-700 dark:border-gray-600 dark:text-gray-100" />
            </div>
            <div class="flex-1 min-w-0">
              <span class="text-xs text-gray-500 dark:text-gray-400 mb-1 block">Not</span>
              <input v-model="line.note" type="text" placeholder="Opsiyonel" class="w-full border rounded px-2 py-1.5 text-xs dark:bg-gray-700 dark:border-gray-600 dark:text-gray-100" />
            </div>
            <button type="button" @click="removeLine(idx)" class="mt-5 text-red-400 hover:text-red-600 shrink-0">
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          <div v-if="createForm.lines.length === 0" class="text-xs text-gray-400 text-center py-2">
            Henüz satır eklenmedi. "+ Satır Ekle" ile ekleyin.
          </div>
        </div>
      </div>

      <template #footer>
        <button @click="showCreateModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button
          @click="submitCreate"
          :disabled="!createForm.driverSessionId || createForm.lines.length === 0"
          class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 font-bold disabled:bg-blue-300"
        >
          Kaydet
        </button>
      </template>
    </BaseModal>

    <!-- Resolve Modal -->
    <BaseModal :show="showResolveModal" title="Satırı Çözüme Kavuştur" maxWidth="sm" @close="showResolveModal = false">
      <div v-if="resolveTarget" class="space-y-4">
        <div class="text-sm bg-gray-50 dark:bg-gray-800 rounded p-3 border dark:border-gray-700">
          <span class="font-semibold">{{ resolveTarget.line.stockCode }}</span> — {{ resolveTarget.line.stockName }}
          <span class="ml-2 text-gray-500 dark:text-gray-400">({{ resolveTarget.line.qty }} adet)</span>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Çözüm Yöntemi</label>
          <div class="space-y-3">
            <!-- Add to Stock -->
            <label class="flex items-center gap-2 cursor-pointer">
              <input type="radio" v-model.number="resolveForm.action" :value="1" :disabled="!resolveTarget.line.isLinkedToStock" />
              <span class="text-sm" :class="!resolveTarget.line.isLinkedToStock ? 'text-gray-400' : ''">
                Stoğa Ekle
                <span v-if="!resolveTarget.line.isLinkedToStock" class="text-xs text-red-400 ml-1">(stok kartı eşleşmesi gerekli)</span>
              </span>
            </label>

            <!-- Match to Shipment -->
            <label class="flex items-center gap-2 cursor-pointer">
              <input type="radio" v-model.number="resolveForm.action" :value="2" />
              <span class="text-sm">Sevkiyata Eşleştir</span>
            </label>
            <div v-if="resolveForm.action === 2" class="ml-6">
              <input
                v-model="shipmentSearch"
                @input="onShipmentSearchInput"
                type="text"
                placeholder="İrsaliye no, sipariş no veya proje kodu..."
                class="border rounded px-3 py-2 text-sm w-full focus:ring-2 focus:ring-blue-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
              />
              <div v-if="shipmentResults.length > 0" class="relative">
                <div class="absolute z-20 w-full mt-1 bg-white dark:bg-gray-800 border dark:border-gray-700 rounded shadow-lg max-h-48 overflow-y-auto text-sm">
                  <div
                    v-for="s in shipmentResults"
                    :key="s.id"
                    @mousedown.prevent="selectShipment(s)"
                    class="px-3 py-2 cursor-pointer hover:bg-blue-50 dark:hover:bg-gray-700 border-b last:border-b-0"
                  >
                    <span class="font-semibold text-gray-800 dark:text-gray-100">{{ s.projectCode }}</span>
                    <span class="ml-2 text-gray-500 dark:text-gray-400">{{ s.projectName }}</span>
                    <span v-if="s.irsaliyeNo" class="ml-2 text-xs text-gray-400">İrs: {{ s.irsaliyeNo }}</span>
                    <span v-if="s.orderNumber" class="ml-2 text-xs text-gray-400">Sip: {{ s.orderNumber }}</span>
                    <span v-if="s.talepNo" class="ml-2 text-xs text-gray-400">Talep: {{ s.talepNo }}</span>
                    <span class="ml-2 text-xs text-blue-500">{{ s.status }}</span>
                  </div>
                </div>
              </div>
              <div v-if="resolveForm.linkedShipmentId" class="mt-2 text-xs text-green-600 dark:text-green-400 font-medium">
                ✓ Sevkiyat seçildi: #{{ resolveForm.linkedShipmentId }}
              </div>
            </div>
          </div>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Not</label>
          <textarea v-model="resolveForm.note" rows="2" class="w-full border rounded px-3 py-2 text-sm focus:ring-2 focus:ring-blue-400 resize-none dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
        </div>
      </div>

      <template #footer>
        <button @click="showResolveModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button
          @click="submitResolve"
          :disabled="resolveForm.action === 2 && !resolveForm.linkedShipmentId"
          class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 font-bold disabled:bg-blue-300"
        >
          Onayla
        </button>
      </template>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import BaseModal from '../components/BaseModal.vue';
import StockCombobox from '../components/StockCombobox.vue';
import vehicleReturnService, {
  type VehicleReturnDto,
  type VehicleReturnLineDto,
  type VehicleShipmentDto,
  VehicleReturnResolveAction,
} from '../services/vehicleReturnService';
import apiClient from '../services/apiClient';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';
import { formatDate } from '../utils/dateFormat';

const notificationStore = useNotificationStore();

const loading = ref(false);
const returns = ref<VehicleReturnDto[]>([]);
const filterLineStatus = ref('');
const filterFromDate = ref('');
const filterToDate = ref('');
const currentPage = ref(1);
const totalPages = ref(1);
const totalCount = ref(0);
const pageSize = 20;
const expandedIds = ref(new Set<number>());

const totalPendingLines = computed(() =>
  returns.value.reduce((sum, r) => sum + r.pendingLines, 0)
);
const totalAddedToStock = computed(() =>
  returns.value.reduce((sum, r) => sum + r.lines.filter(l => l.status === 'AddedToStock').length, 0)
);
const totalMatchedToShipment = computed(() =>
  returns.value.reduce((sum, r) => sum + r.lines.filter(l => l.status === 'MatchedToShipment').length, 0)
);

const toggleExpand = (id: number) => {
  if (expandedIds.value.has(id)) {
    expandedIds.value.delete(id);
  } else {
    expandedIds.value.add(id);
  }
  expandedIds.value = new Set(expandedIds.value);
};

const filterAndLoad = () => { currentPage.value = 1; loadData(1); };
const goToPage = (page: number) => loadData(page);

const loadData = async (page = currentPage.value) => {
  loading.value = true;
  try {
    const result = await vehicleReturnService.getAll({
      lineStatus: filterLineStatus.value !== '' ? Number(filterLineStatus.value) : undefined,
      fromDate: filterFromDate.value || undefined,
      toDate: filterToDate.value || undefined,
      pageNumber: page,
      pageSize,
    });
    returns.value = result.items;
    currentPage.value = result.pageIndex;
    totalPages.value = result.totalPages;
    totalCount.value = result.totalCount;

    // Auto-expand if there are pending lines
    expandedIds.value = new Set(
      result.items.filter(r => r.pendingLines > 0).map(r => r.id)
    );
  } catch {
    notificationStore.add('Veriler yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
};

const lineStatusLabel = (status: string) => {
  const map: Record<string, string> = {
    Pending: 'Beklemede',
    AddedToStock: 'Stoğa Eklendi',
    MatchedToShipment: 'Sevkiyata Eşleşti',
  };
  return map[status] || status;
};

const lineStatusClass = (status: string) => {
  switch (status) {
    case 'Pending': return 'bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-300';
    case 'AddedToStock': return 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300';
    case 'MatchedToShipment': return 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300';
    default: return 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300';
  }
};

// --- Create Modal ---
interface LineForm {
  stockMasterId?: number;
  stockCodeFree: string;
  stockNameFree: string;
  qty: number;
  note: string;
  useManual: boolean;
}

const showCreateModal = ref(false);
const loadingSessions = ref(false);
const sessionOptions = ref<{ id: string; driverName: string; plateNumber: string; startTime: string; status: string }[]>([]);
const createForm = ref({
  driverSessionId: '',
  returnDate: new Date().toISOString().slice(0, 10),
  note: '',
  lines: [] as LineForm[],
});

const addLine = () => {
  createForm.value.lines.push({
    stockMasterId: undefined,
    stockCodeFree: '',
    stockNameFree: '',
    qty: 1,
    note: '',
    useManual: false,
  });
};

const removeLine = (idx: number) => {
  createForm.value.lines.splice(idx, 1);
};

const onLineStockSelect = (idx: number, item: any) => {
  const line = createForm.value.lines[idx];
  if (!line) return;
  line.stockMasterId = item.id || item.Id;
  line.stockCodeFree = item.stockCode || item.StockCode || '';
  line.stockNameFree = item.stockName || item.StockName || '';
};

const loadSessions = async () => {
  loadingSessions.value = true;
  try {
    const toDate = new Date();
    const fromDate = new Date();
    fromDate.setDate(fromDate.getDate() - 30);
    const res = await apiClient.get('/admin/driver-sessions', {
      params: {
        fromDate: fromDate.toISOString(),
        toDate: toDate.toISOString(),
        pageSize: 100,
      }
    });
    const items = res.data?.items ?? res.data?.Items ?? [];
    sessionOptions.value = items.map((s: any) => ({
      id: s.id || s.sessionId,
      driverName: s.driverFullName || s.driverName || '—',
      plateNumber: s.plateNumber || '—',
      startTime: s.startTime,
      status: s.status,
    }));
  } catch {
    sessionOptions.value = [];
  } finally {
    loadingSessions.value = false;
  }
};

const openCreateModal = () => {
  createForm.value = {
    driverSessionId: '',
    returnDate: new Date().toISOString().slice(0, 10),
    note: '',
    lines: [],
  };
  showCreateModal.value = true;
  loadSessions();
};

const submitCreate = async () => {
  if (!createForm.value.driverSessionId || createForm.value.lines.length === 0) return;
  try {
    await vehicleReturnService.create({
      driverSessionId: createForm.value.driverSessionId,
      returnDate: createForm.value.returnDate || undefined,
      note: createForm.value.note || undefined,
      lines: createForm.value.lines.map(l => ({
        stockMasterId: l.stockMasterId || undefined,
        stockCodeFree: l.stockCodeFree || undefined,
        stockNameFree: l.stockNameFree || undefined,
        qty: l.qty,
        note: l.note || undefined,
      })),
    });
    showCreateModal.value = false;
    await loadData();
    notificationStore.add('İade kaydı oluşturuldu.', 'success');
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'İşlem başarısız.', 'error');
  }
};

// --- Resolve Modal ---
interface ResolveTarget { return: VehicleReturnDto; line: VehicleReturnLineDto }
const showResolveModal = ref(false);
const resolveTarget = ref<ResolveTarget | null>(null);
const resolveForm = ref<{ action: number; linkedShipmentId?: number; note: string }>({
  action: 1,
  linkedShipmentId: undefined,
  note: '',
});
const shipmentSearch = ref('');
const shipmentResults = ref<VehicleShipmentDto[]>([]);
let shipmentDebounce: ReturnType<typeof setTimeout> | null = null;

const openResolveModal = (r: VehicleReturnDto, line: VehicleReturnLineDto) => {
  resolveTarget.value = { return: r, line };
  resolveForm.value = {
    action: line.isLinkedToStock ? 1 : 2,
    linkedShipmentId: undefined,
    note: '',
  };
  shipmentSearch.value = '';
  shipmentResults.value = [];
  showResolveModal.value = true;
};

const onShipmentSearchInput = () => {
  if (shipmentDebounce) clearTimeout(shipmentDebounce);
  resolveForm.value.linkedShipmentId = undefined;
  shipmentResults.value = [];
  if (!shipmentSearch.value || shipmentSearch.value.length < 2) return;
  shipmentDebounce = setTimeout(async () => {
    if (!resolveTarget.value) return;
    try {
      const results = await vehicleReturnService.searchShipments(
        resolveTarget.value.return.driverSessionId,
        shipmentSearch.value
      );
      shipmentResults.value = results;
    } catch {
      shipmentResults.value = [];
    }
  }, 300);
};

const selectShipment = (s: VehicleShipmentDto) => {
  resolveForm.value.linkedShipmentId = s.id;
  shipmentSearch.value = `${s.projectCode} — ${s.irsaliyeNo || s.orderNumber || `#${s.id}`}`;
  shipmentResults.value = [];
};

const submitResolve = async () => {
  if (!resolveTarget.value) return;
  try {
    await vehicleReturnService.resolveLine(resolveTarget.value.line.id, {
      action: resolveForm.value.action as VehicleReturnResolveAction,
      linkedShipmentId: resolveForm.value.linkedShipmentId,
      note: resolveForm.value.note || undefined,
    });
    showResolveModal.value = false;
    await loadData();
    notificationStore.add('Satır çözüme kavuşturuldu.', 'success');
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'İşlem başarısız.', 'error');
  }
};

onMounted(loadData);
</script>
