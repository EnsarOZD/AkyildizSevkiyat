<template>
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
    <PageHeader title="Belirsiz İadeler" subtitle="Kaynağı bilinmeyen / sevkiyatı belirsiz iade kalemleri" color="orange">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
        </svg>
      </template>
      <template #actions>
        <button
          v-role="['Admin', 'Manager', 'Warehouse', 'Driver']"
          @click="openCreateModal"
          class="px-4 py-2 bg-orange-500 text-white rounded-lg font-semibold hover:bg-orange-600 transition text-sm"
        >
          + Yeni İade Kaydı
        </button>
      </template>
    </PageHeader>

    <!-- Filters -->
    <div class="flex gap-3 mb-4 flex-wrap">
      <select v-model="filterStatus" class="border dark:border-gray-700 rounded px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100">
        <option value="">Tüm Durumlar</option>
        <option value="0">Beklemede</option>
        <option value="1">Sevkiyata Eşleşti</option>
        <option value="2">Stoğa Eklendi</option>
        <option value="3">Hariç Tutuldu</option>
      </select>
      <input v-model="filterFromDate" type="date" class="border dark:border-gray-700 rounded px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100" />
      <input v-model="filterToDate" type="date" class="border dark:border-gray-700 rounded px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100" />
      <button @click="filterAndLoad" class="px-4 py-2 bg-gray-600 text-white rounded text-sm hover:bg-gray-700">Filtrele</button>
    </div>

    <!-- Summary cards -->
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 border-l-4 border-orange-400">
        <div class="text-2xl font-bold text-orange-600">{{ pendingCount }}</div>
        <div class="text-xs text-gray-500 dark:text-gray-400 mt-1">Beklemede</div>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 border-l-4 border-blue-400">
        <div class="text-2xl font-bold text-blue-600">{{ matchedCount }}</div>
        <div class="text-xs text-gray-500 dark:text-gray-400 mt-1">Eşleştirildi</div>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 border-l-4 border-green-400">
        <div class="text-2xl font-bold text-green-600">{{ addedToStockCount }}</div>
        <div class="text-xs text-gray-500 dark:text-gray-400 mt-1">Stoğa Eklendi</div>
      </div>
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 border-l-4 border-gray-400">
        <div class="text-2xl font-bold text-gray-600 dark:text-gray-400">{{ writtenOffCount }}</div>
        <div class="text-xs text-gray-500 dark:text-gray-400 mt-1">Hariç Tutuldu</div>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-gray-900 shadow rounded-lg overflow-hidden">
      <div v-if="loading" class="p-8 text-center text-gray-500 dark:text-gray-400">Yükleniyor...</div>
      <div v-else-if="returns.length === 0" class="p-8 text-center text-gray-400">Kayıt bulunamadı.</div>
      <table v-else class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Tarih</th>
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Stok Kodu</th>
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Stok Adı</th>
            <th class="px-4 py-3 text-right font-medium text-gray-500 dark:text-gray-400">Miktar</th>
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">İade Nedeni</th>
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Durum</th>
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Bağlı Sevkiyat</th>
            <th class="px-4 py-3 text-right font-medium text-gray-500 dark:text-gray-400">İşlem</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="r in returns" :key="r.id" :class="{ 'bg-orange-50': r.status === 'Pending' }">
            <td class="px-4 py-3 text-gray-600 dark:text-gray-400">{{ formatDate(r.returnDate) }}</td>
            <td class="px-4 py-3 font-mono text-gray-800 dark:text-gray-200">
              {{ r.stockCode }}
              <span v-if="!r.isLinkedToStock" class="ml-1 text-xs text-yellow-600 bg-yellow-100 px-1 rounded">serbest</span>
            </td>
            <td class="px-4 py-3 text-gray-700 dark:text-gray-300">{{ r.stockName }}</td>
            <td class="px-4 py-3 text-right font-semibold">{{ r.qty }}</td>
            <td class="px-4 py-3 text-gray-600 dark:text-gray-400">{{ formatReturnReason(r.returnReason) }}</td>
            <td class="px-4 py-3">
              <StatusBadge :status="r.status" type="floatingReturn" />
            </td>
            <td class="px-4 py-3 text-gray-500 dark:text-gray-400">
              <span v-if="r.linkedShipmentId">#{{ r.linkedShipmentId }}</span>
              <span v-else class="text-gray-300">—</span>
            </td>
            <td class="px-4 py-3 text-right">
              <button
                v-if="r.status === 'Pending'"
                v-role="['Admin', 'Manager']"
                @click="openResolveModal(r)"
                class="text-xs px-3 py-1 bg-blue-600 text-white rounded hover:bg-blue-700"
              >
                Çözüme Kavuştur
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="flex items-center justify-between px-1 py-2">
      <span class="text-sm text-gray-500 dark:text-gray-400">
        Toplam {{ totalCount }} kayıt
      </span>
      <div class="flex items-center gap-1">
        <button
          @click="goToPage(currentPage - 1)"
          :disabled="currentPage <= 1"
          class="px-3 py-1.5 rounded text-sm border dark:border-gray-700 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
        >
          ‹
        </button>
        <span class="px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300">
          {{ currentPage }} / {{ totalPages }}
        </span>
        <button
          @click="goToPage(currentPage + 1)"
          :disabled="currentPage >= totalPages"
          class="px-3 py-1.5 rounded text-sm border dark:border-gray-700 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
        >
          ›
        </button>
      </div>
    </div>

    <!-- Create Modal -->
    <BaseModal :show="showCreateModal" title="Yeni Belirsiz İade Kaydı" maxWidth="sm" @close="showCreateModal = false">
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">İade Tarihi <span class="text-red-500">*</span></label>
          <input v-model="createForm.returnDate" type="date" class="w-full border rounded px-3 py-2 focus:ring-2 focus:ring-orange-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
        </div>
        <div>
          <div class="flex items-center justify-between mb-1">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Stok</label>
            <button type="button" @click="useManualStock = !useManualStock" class="text-xs text-blue-500 hover:underline">
              {{ useManualStock ? 'Stok Kataloğundan Seç' : 'Manuel Giriş' }}
            </button>
          </div>
          <template v-if="!useManualStock">
            <StockCombobox :operationType="0" placeholder="Stok kodu veya adı ara..." @select="onStockSelect" />
            <div v-if="createForm.stockCodeFree" class="mt-1 text-xs text-gray-500 dark:text-gray-400">
              Seçili: <span class="font-medium text-gray-700 dark:text-gray-200">{{ createForm.stockCodeFree }} — {{ createForm.stockNameFree }}</span>
            </div>
          </template>
          <template v-else>
            <input v-model="createForm.stockCodeFree" type="text" maxlength="50" placeholder="Stok kodu (serbest)" class="w-full border rounded px-3 py-2 mb-2 focus:ring-2 focus:ring-orange-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 text-sm" />
            <input v-model="createForm.stockNameFree" type="text" maxlength="200" placeholder="Stok adı (serbest)" class="w-full border rounded px-3 py-2 focus:ring-2 focus:ring-orange-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 text-sm" />
          </template>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Miktar <span class="text-red-500">*</span></label>
          <input v-model.number="createForm.qty" type="number" min="0.01" step="0.01" class="w-full border rounded px-3 py-2 focus:ring-2 focus:ring-orange-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">İade Nedeni</label>
          <select v-model.number="createForm.returnReason" class="w-full border rounded px-3 py-2 focus:ring-2 focus:ring-orange-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
            <option v-for="r in returnReasonOptions" :key="r.value" :value="r.value">{{ r.label }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Not</label>
          <textarea v-model="createForm.note" rows="2" class="w-full border rounded px-3 py-2 focus:ring-2 focus:ring-orange-400 resize-none text-sm dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
        </div>
      </div>
      <template #footer>
        <button @click="showCreateModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button
          @click="submitCreate"
          :disabled="!createForm.returnDate || !createForm.qty"
          class="px-4 py-2 bg-orange-500 text-white rounded hover:bg-orange-600 font-bold disabled:bg-orange-300"
        >
          Kaydet
        </button>
      </template>
    </BaseModal>

    <!-- Resolve Modal -->
    <BaseModal :show="showResolveModal" title="İadeyi Çözüme Kavuştur" maxWidth="sm" @close="showResolveModal = false">
      <div v-if="resolveTarget" class="space-y-4">
        <div class="text-sm bg-gray-50 dark:bg-gray-800 rounded p-3 border dark:border-gray-700">
          <span class="font-semibold">{{ resolveTarget.stockCode }}</span> — {{ resolveTarget.stockName }}
          <span class="ml-2 text-gray-500 dark:text-gray-400">({{ resolveTarget.qty }} adet)</span>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Çözüm Yöntemi</label>
          <div class="space-y-2">
            <label class="flex items-center gap-2 cursor-pointer">
              <input type="radio" v-model.number="resolveForm.action" :value="1" />
              <span class="text-sm">Sevkiyata Eşleştir</span>
            </label>
            <div v-if="resolveForm.action === 1" class="ml-6 relative">
              <input
                v-model="shipmentSearch"
                @input="onShipmentSearchInput"
                type="text"
                placeholder="Sevkiyat no veya proje adı ara..."
                class="border rounded px-3 py-2 text-sm w-full focus:ring-2 focus:ring-blue-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
              />
              <div v-if="shipmentResults.length > 0" class="absolute z-20 w-full mt-1 bg-white dark:bg-gray-800 border dark:border-gray-700 rounded shadow-lg max-h-48 overflow-y-auto text-sm">
                <div
                  v-for="s in shipmentResults"
                  :key="s.id"
                  @mousedown.prevent="selectShipment(s)"
                  class="px-3 py-2 cursor-pointer hover:bg-blue-50 dark:hover:bg-gray-700 border-b last:border-b-0"
                >
                  <span class="font-semibold text-gray-800 dark:text-gray-100">#{{ s.id }}</span>
                  <span class="ml-2 text-gray-600 dark:text-gray-400">{{ s.shipmentNumber }}</span>
                  <span v-if="s.projectNameSnapshot" class="ml-2 text-xs text-gray-500">{{ s.projectNameSnapshot }}</span>
                </div>
              </div>
              <div v-if="resolveForm.linkedShipmentId" class="mt-1 text-xs text-gray-500 dark:text-gray-400">
                Seçili Sevkiyat ID: <span class="font-medium text-gray-700 dark:text-gray-200">{{ resolveForm.linkedShipmentId }}</span>
              </div>
            </div>
            <label class="flex items-center gap-2 cursor-pointer">
              <input type="radio" v-model.number="resolveForm.action" :value="2" :disabled="!resolveTarget.isLinkedToStock" />
              <span class="text-sm" :class="{ 'text-gray-400': !resolveTarget.isLinkedToStock }">
                Stoğa Ekle
                <span v-if="!resolveTarget.isLinkedToStock" class="text-xs text-red-400">(stok kartı eşleşmesi gerekli)</span>
              </span>
            </label>
            <label class="flex items-center gap-2 cursor-pointer">
              <input type="radio" v-model.number="resolveForm.action" :value="3" />
              <span class="text-sm">Hariç Tut (Yazı Sil)</span>
            </label>
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
          class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 font-bold"
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
import floatingReturnService, { type FloatingReturnDto, ResolveAction } from '../services/floatingReturnService';
import shipmentService, { type Shipment } from '../services/shipmentService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';
import StatusBadge from '../components/StatusBadge.vue';

const notificationStore = useNotificationStore();

const loading = ref(false);
const returns = ref<FloatingReturnDto[]>([]);
const filterStatus = ref('');
const filterFromDate = ref('');
const filterToDate = ref('');
const currentPage = ref(1);
const totalPages = ref(1);
const totalCount = ref(0);
const pageSize = 20;

const pendingCount = computed(() => returns.value.filter(r => r.status === 'Pending').length);
const matchedCount = computed(() => returns.value.filter(r => r.status === 'MatchedToShipment').length);
const addedToStockCount = computed(() => returns.value.filter(r => r.status === 'AddedToStock').length);
const writtenOffCount = computed(() => returns.value.filter(r => r.status === 'WrittenOff').length);

const returnReasonOptions = [
  { value: 0, label: 'Müşteri Reddi' },
  { value: 1, label: 'Hasarlı' },
  { value: 2, label: 'Fazla Yükleme' },
  { value: 3, label: 'Yanlış Ürün' },
  { value: 4, label: 'Proje Bulunamadı' },
  { value: 99, label: 'Diğer' },
];

const filterAndLoad = () => { currentPage.value = 1; loadData(1); };
const goToPage = (page: number) => loadData(page);

const loadData = async (page = currentPage.value) => {
  loading.value = true;
  try {
    const result = await floatingReturnService.getAll({
      status: filterStatus.value !== '' ? Number(filterStatus.value) : undefined,
      fromDate: filterFromDate.value || undefined,
      toDate: filterToDate.value || undefined,
      pageNumber: page,
      pageSize,
    });
    returns.value = result.items;
    currentPage.value = result.pageIndex;
    totalPages.value = result.totalPages;
    totalCount.value = result.totalCount;
  } catch (err) {
    notificationStore.add('Veriler yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
};

const formatDate = (d: string) => new Date(d).toLocaleDateString('tr-TR');

const formatReturnReason = (r: string) => {
  const map: Record<string, string> = {
    CustomerRejected: 'Müşteri Reddi',
    Damaged: 'Hasarlı',
    ExcessLoading: 'Fazla Yükleme',
    WrongItem: 'Yanlış Ürün',
    ProjectNotFound: 'Proje Bulunamadı',
    Other: 'Diğer',
  };
  return map[r] || r;
};


// Create Modal
const showCreateModal = ref(false);
const useManualStock = ref(false);
const createForm = ref({
  returnDate: new Date().toISOString().slice(0, 10),
  stockCodeFree: '',
  stockNameFree: '',
  qty: 0,
  returnReason: 99,
  note: '',
});

const onStockSelect = (item: any) => {
  createForm.value.stockCodeFree = item.stockCode || item.StockCode || '';
  createForm.value.stockNameFree = item.stockName || item.StockName || '';
};

const openCreateModal = () => {
  createForm.value = {
    returnDate: new Date().toISOString().slice(0, 10),
    stockCodeFree: '',
    stockNameFree: '',
    qty: 0,
    returnReason: 99,
    note: '',
  };
  useManualStock.value = false;
  showCreateModal.value = true;
};

const submitCreate = async () => {
  try {
    await floatingReturnService.create({
      returnDate: createForm.value.returnDate,
      stockCodeFree: createForm.value.stockCodeFree || undefined,
      stockNameFree: createForm.value.stockNameFree || undefined,
      qty: createForm.value.qty,
      returnReason: createForm.value.returnReason,
      note: createForm.value.note || undefined,
    });
    showCreateModal.value = false;
    await loadData();
    notificationStore.add('İade kaydı oluşturuldu.', 'success');
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'İşlem başarısız.', 'error');
  }
};

// Shipment search for resolve modal
const shipmentSearch = ref('');
const shipmentResults = ref<Shipment[]>([]);
let shipmentDebounce: any = null;

const onShipmentSearchInput = () => {
  if (shipmentDebounce) clearTimeout(shipmentDebounce);
  resolveForm.value.linkedShipmentId = undefined;
  if (!shipmentSearch.value || shipmentSearch.value.length < 2) {
    shipmentResults.value = [];
    return;
  }
  shipmentDebounce = setTimeout(async () => {
    try {
      const result = await shipmentService.getAll({ search: shipmentSearch.value, pageSize: 10 });
      shipmentResults.value = result.items;
    } catch {
      shipmentResults.value = [];
    }
  }, 300);
};

const selectShipment = (s: Shipment) => {
  resolveForm.value.linkedShipmentId = s.id;
  shipmentSearch.value = `#${s.id} — ${s.shipmentNumber}`;
  shipmentResults.value = [];
};

// Resolve Modal
const showResolveModal = ref(false);
const resolveTarget = ref<FloatingReturnDto | null>(null);
const resolveForm = ref<{ action: number; linkedShipmentId?: number; note: string }>({
  action: 1,
  linkedShipmentId: undefined,
  note: '',
});

const openResolveModal = (r: FloatingReturnDto) => {
  resolveTarget.value = r;
  resolveForm.value = { action: 1, linkedShipmentId: undefined, note: '' };
  shipmentSearch.value = '';
  shipmentResults.value = [];
  showResolveModal.value = true;
};

const submitResolve = async () => {
  if (!resolveTarget.value) return;
  try {
    await floatingReturnService.resolve(resolveTarget.value.id, {
      action: resolveForm.value.action as ResolveAction,
      linkedShipmentId: resolveForm.value.linkedShipmentId,
      note: resolveForm.value.note || undefined,
    });
    showResolveModal.value = false;
    await loadData();
    notificationStore.add('İade çözüme kavuşturuldu.', 'success');
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'İşlem başarısız.', 'error');
  }
};

onMounted(loadData);
</script>
