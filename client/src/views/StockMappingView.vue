<template>
  <div class="p-4 sm:p-6 space-y-4 sm:space-y-6">
    <PageHeader title="Stok Eşleştirme" subtitle="ISS stok kodlarını sistem stok kartlarıyla eşleştirin" color="teal">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
        </svg>
      </template>
    </PageHeader>

    <!-- Toolbar -->
    <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-3 sm:p-4 flex flex-wrap gap-2 items-center justify-between">
      <div class="flex flex-wrap gap-2">
        <button @click="downloadTemplate"
          class="px-3 py-1.5 bg-blue-600 text-white rounded hover:bg-blue-700 text-sm flex items-center gap-1.5 font-medium">
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
          </svg>
          Excel İndir
        </button>

        <input type="file" ref="mappingFileInput" class="hidden" accept=".xlsx" @change="uploadMappings" />
        <button @click="mappingFileInput?.click()"
          class="px-3 py-1.5 bg-purple-600 text-white rounded hover:bg-purple-700 text-sm flex items-center gap-1.5 font-medium">
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
          </svg>
          Excel Yükle
        </button>

        <input type="file" ref="stockFileInput" class="hidden" accept=".xlsx" @change="uploadStocks" />
        <button @click="stockFileInput?.click()"
          class="px-3 py-1.5 bg-green-600 text-white rounded hover:bg-green-700 text-sm flex items-center gap-1.5 font-medium">
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M12 4v16m8-8H4" />
          </svg>
          Yeni Stok Kartı Yükle
        </button>

        <button @click="autoMatch" :disabled="autoMatching"
          class="px-3 py-1.5 bg-yellow-500 text-white rounded hover:bg-yellow-600 text-sm flex items-center gap-1.5 font-medium disabled:opacity-50">
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M13 10V3L4 14h7v7l9-11h-7z" />
          </svg>
          {{ autoMatching ? 'Eşleştiriliyor...' : 'Otomatik Eşleştir' }}
        </button>
      </div>

      <!-- Search + Filter -->
      <div class="flex gap-2 flex-wrap">
        <input v-model="search" @input="onSearch" type="text" placeholder="Ara (ISS kodu, ad, sistem kodu)..."
          class="border border-gray-300 dark:border-gray-700 rounded px-3 py-1.5 text-sm bg-white dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-teal-400 w-64" />
        <button @click="refresh"
          class="p-2 hover:bg-gray-100 dark:hover:bg-gray-700 rounded" title="Yenile">
          <svg class="h-5 w-5 text-gray-500 dark:text-gray-400" viewBox="0 0 20 20" fill="currentColor">
            <path fill-rule="evenodd"
              d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z"
              clip-rule="evenodd" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Status Tabs -->
    <div class="bg-white dark:bg-gray-900 rounded-lg shadow-sm border dark:border-gray-800">
      <div class="border-b dark:border-gray-800 flex overflow-x-auto scrollbar-hide">
        <button v-for="tab in statusTabs" :key="tab.value"
          @click="switchTab(tab.value)"
          class="py-3 px-5 border-b-2 font-semibold text-sm transition-all whitespace-nowrap"
          :class="activeStatus === tab.value
            ? 'border-teal-500 text-teal-600 bg-teal-50/30'
            : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'">
          {{ tab.label }}
          <span class="ml-1 text-[11px] opacity-60">({{ tab.count ?? '?' }})</span>
        </button>
      </div>

      <!-- Table -->
      <div class="p-4">
        <div v-if="loading" class="text-center py-10 text-gray-400 dark:text-gray-500 font-medium">
          Yükleniyor...
        </div>

        <div v-else-if="mappings.length === 0" class="text-center py-12 text-gray-500 dark:text-gray-400">
          <svg class="mx-auto h-10 w-10 text-gray-300 dark:text-gray-600 mb-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
              d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
          </svg>
          {{ activeStatus === 'Unmapped' ? 'Eşleştirme bekleyen stok bulunamadı.' : 'Kayıt bulunamadı.' }}
        </div>

        <div v-else class="space-y-3">
          <div v-for="item in mappings" :key="item.id"
            class="border dark:border-gray-700 rounded-lg p-3 hover:bg-gray-50 dark:hover:bg-gray-800/40 transition-colors">

            <!-- Unmapped row — inline eşleştirme formu -->
            <template v-if="item.status === 'Unmapped'">
              <div class="flex flex-col md:flex-row gap-3 items-start md:items-center">
                <!-- ISS bilgisi -->
                <div class="flex-1 min-w-0">
                  <div class="font-mono text-xs text-red-500 dark:text-red-400 font-bold">{{ item.externalCode }}</div>
                  <div class="text-sm text-gray-700 dark:text-gray-300 truncate">{{ item.externalName }}</div>
                </div>

                <!-- Eşleştirme araçları -->
                <div class="flex flex-wrap gap-2 items-center shrink-0">
                  <div class="w-52">
                    <StockCombobox
                      placeholder="Stok Ara (Kod / Ad)"
                      v-model="rowState[item.id]!.selectedLocalId"
                      @search="(val) => (rowState[item.id]!.currentSearch = val)"
                      @select="(sel) => onStockSelected(item.id, sel)"
                    />
                  </div>

                  <!-- Yeni kart oluştur düğmesi -->
                  <button
                    v-if="!rowState[item.id]?.selectedLocalId && rowState[item.id]?.currentSearch && rowState[item.id]!.currentSearch!.length > 1"
                    @click="rowState[item.id]!.showCreateForm = !rowState[item.id]!.showCreateForm"
                    class="px-2 py-1 text-xs flex items-center gap-1 rounded"
                    :class="rowState[item.id]?.showCreateForm
                      ? 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300'
                      : 'bg-teal-500 hover:bg-teal-600 text-white'"
                    title="Yeni Stok Kartı Aç">
                    <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                        :d="rowState[item.id]?.showCreateForm ? 'M6 18L18 6M6 6l12 12' : 'M12 4v16m8-8H4'" />
                    </svg>
                    {{ rowState[item.id]?.showCreateForm ? 'İptal' : 'Yeni Kart' }}
                  </button>

                  <!-- Netsis kodu -->
                  <div v-if="rowState[item.id]?.selectedLocalId" class="flex items-center gap-1">
                    <label class="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap">Netsis:</label>
                    <input v-model="rowState[item.id]!.netsisCode" type="text" placeholder="Netsis kodu"
                      class="w-28 border rounded px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-teal-500 dark:bg-gray-800 dark:border-gray-600 dark:text-gray-100"
                      :class="!rowState[item.id]?.netsisCode ? 'border-orange-400 dark:border-orange-500' : 'border-gray-300'" />
                    <span v-if="!rowState[item.id]?.netsisCode" class="text-orange-500 text-xs" title="Netsis aktarımı için zorunlu">⚠</span>
                  </div>

                  <button @click="mapItem(item, false)"
                    :disabled="!rowState[item.id]?.selectedLocalId"
                    class="px-3 py-1 bg-teal-600 text-white rounded hover:bg-teal-700 text-sm disabled:opacity-40 disabled:cursor-not-allowed font-medium">
                    Eşleştir
                  </button>
                  <button @click="mapItem(item, true)"
                    class="px-3 py-1 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-300 dark:hover:bg-gray-600 text-sm">
                    Yoksay
                  </button>
                </div>
              </div>

              <!-- Inline yeni stok kartı formu -->
              <div v-if="rowState[item.id]?.showCreateForm"
                class="mt-3 pt-3 border-t dark:border-gray-700 bg-teal-50 dark:bg-teal-900/10 rounded-b p-3">
                <p class="text-xs font-semibold text-teal-700 dark:text-teal-400 mb-2">
                  Yeni stok kartı: <span class="font-bold">{{ rowState[item.id]?.currentSearch }}</span>
                </p>
                <div class="flex flex-wrap gap-3 items-end">
                  <div>
                    <label class="block text-xs text-gray-600 dark:text-gray-400 mb-0.5">Kategori <span class="text-red-500">*</span></label>
                    <select v-model="rowState[item.id]!.newCategory"
                      class="border rounded px-2 py-1 text-xs bg-white dark:bg-gray-800 dark:border-gray-600 dark:text-gray-100 focus:ring-2 focus:ring-teal-500"
                      :class="!rowState[item.id]?.newCategory ? 'border-red-400' : 'border-gray-300'">
                      <option :value="undefined" disabled>Seçin...</option>
                      <option :value="1">Gıda</option>
                      <option :value="2">Sarf</option>
                      <option :value="3">Kıyafet</option>
                      <option :value="4">Temizlik</option>
                      <option :value="5">Kırtasiye</option>
                      <option :value="99">Diğer</option>
                    </select>
                  </div>
                  <div>
                    <label class="block text-xs text-gray-600 dark:text-gray-400 mb-0.5">Birim</label>
                    <select v-model="rowState[item.id]!.newUnit"
                      class="border border-gray-300 dark:border-gray-600 rounded px-2 py-1 text-xs bg-white dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-teal-500">
                      <option :value="0">Adet</option>
                      <option :value="1">Kg</option>
                      <option :value="2">Paket</option>
                      <option :value="3">Koli</option>
                      <option :value="4">Litre</option>
                      <option :value="5">Metre</option>
                      <option :value="6">Metrekare</option>
                      <option :value="7">Set</option>
                      <option :value="8">Teneke</option>
                      <option :value="99">Diğer</option>
                    </select>
                  </div>
                  <div>
                    <label class="block text-xs text-gray-600 dark:text-gray-400 mb-0.5">Picking</label>
                    <select v-model="rowState[item.id]!.newPickingType"
                      class="border border-gray-300 dark:border-gray-600 rounded px-2 py-1 text-xs bg-white dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-teal-500">
                      <option :value="1">Micro</option>
                      <option :value="2">Macro</option>
                    </select>
                  </div>
                  <button @click="createAndSelectStock(item)"
                    :disabled="!rowState[item.id]?.newCategory"
                    class="px-3 py-1 bg-teal-600 hover:bg-teal-700 text-white rounded text-xs font-semibold disabled:opacity-40 disabled:cursor-not-allowed">
                    Oluştur
                  </button>
                </div>
              </div>
            </template>

            <!-- Mapped / Ignored row — düzenleme -->
            <template v-else>
              <div class="flex flex-col sm:flex-row gap-3 items-start sm:items-center">
                <!-- ISS bilgisi -->
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2 mb-0.5">
                    <span class="px-1.5 py-0.5 rounded text-[10px] font-bold uppercase"
                      :class="item.status === 'Mapped'
                        ? 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
                        : 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400'">
                      {{ item.status === 'Mapped' ? 'Eşleşti' : 'Yoksayıldı' }}
                    </span>
                    <span class="font-mono text-xs text-gray-500 dark:text-gray-400">{{ item.externalCode }}</span>
                  </div>
                  <div class="text-sm font-medium text-gray-800 dark:text-gray-200 truncate">{{ item.externalName }}</div>
                </div>

                <!-- Eşleştirme bilgisi -->
                <div v-if="item.status === 'Mapped'" class="flex-1 min-w-0">
                  <div class="text-[10px] text-gray-400 dark:text-gray-500 uppercase font-bold mb-0.5">Sistem Stok</div>
                  <div class="text-sm font-semibold text-teal-700 dark:text-teal-400">{{ item.localStockCode }}</div>
                  <div class="text-xs text-gray-600 dark:text-gray-400 truncate">{{ item.localStockName }}</div>
                  <div v-if="item.netsisStockCode" class="text-[10px] text-gray-400 mt-0.5">
                    Netsis: <span class="font-mono font-medium text-gray-600 dark:text-gray-300">{{ item.netsisStockCode }}</span>
                  </div>
                  <div v-else class="text-[10px] text-orange-500 mt-0.5">⚠ Netsis kodu eksik</div>
                </div>

                <!-- Düzenleme modu değilse -->
                <div v-if="!editingId || editingId !== item.id" class="flex gap-2 shrink-0">
                  <button @click="startEdit(item)"
                    class="px-3 py-1 text-sm bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-200 dark:hover:bg-gray-700 font-medium">
                    Düzenle
                  </button>
                  <button @click="clearMapping(item)"
                    class="px-3 py-1 text-sm bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 rounded hover:bg-red-100 dark:hover:bg-red-900/40 font-medium"
                    title="Eşleştirmeyi kaldır (Unmapped'e döndür)">
                    Sil
                  </button>
                </div>

                <!-- Düzenleme modu açıksa -->
                <div v-else class="flex flex-wrap gap-2 items-center shrink-0">
                  <div class="w-52">
                    <StockCombobox
                      placeholder="Yeni Stok Seç"
                      v-model="editState.selectedLocalId"
                      @select="(sel) => onEditStockSelected(sel)"
                    />
                  </div>
                  <div class="flex items-center gap-1">
                    <label class="text-xs text-gray-500 whitespace-nowrap">Netsis:</label>
                    <input v-model="editState.netsisCode" type="text" placeholder="Netsis kodu"
                      class="w-28 border rounded px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-teal-500 dark:bg-gray-800 dark:border-gray-600 dark:text-gray-100"
                      :class="!editState.netsisCode ? 'border-orange-400' : 'border-gray-300'" />
                  </div>
                  <button @click="saveEdit(item)"
                    :disabled="!editState.selectedLocalId"
                    class="px-3 py-1 bg-teal-600 text-white rounded hover:bg-teal-700 text-sm disabled:opacity-40 font-medium">
                    Kaydet
                  </button>
                  <button @click="cancelEdit"
                    class="px-3 py-1 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded text-sm">
                    İptal
                  </button>
                </div>
              </div>
            </template>
          </div>
        </div>

        <!-- Pagination -->
        <div v-if="totalPages > 1" class="mt-5 flex flex-col sm:flex-row justify-between items-center gap-3 border-t dark:border-gray-800 pt-4">
          <div class="text-xs text-gray-500 dark:text-gray-400">
            Toplam <span class="font-bold text-gray-800 dark:text-gray-100">{{ totalCount }}</span> kayıt —
            Sayfa {{ page }} / {{ totalPages }}
          </div>
          <div class="flex gap-2">
            <button @click="page--; load()" :disabled="page <= 1"
              class="px-3 py-1.5 text-sm border dark:border-gray-700 rounded disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-gray-800">
              ← Önceki
            </button>
            <button @click="page++; load()" :disabled="page >= totalPages"
              class="px-3 py-1.5 text-sm border dark:border-gray-700 rounded disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-gray-800">
              Sonraki →
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import StockCombobox from '../components/StockCombobox.vue';
import stockMappingService, { type StockMappingDto } from '../services/stockMappingService';
import { stockService } from '../services/stockService';
import shipmentService from '../services/shipmentService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const notificationStore = useNotificationStore();

// ---------- state ----------
const loading = ref(false);
const autoMatching = ref(false);
const mappings = ref<StockMappingDto[]>([]);
const totalCount = ref(0);
const totalPages = ref(1);
const page = ref(1);
const search = ref('');
const activeStatus = ref<string>('Unmapped');

const mappingFileInput = ref<HTMLInputElement | null>(null);
const stockFileInput = ref<HTMLInputElement | null>(null);

const unmappedCount = ref<number | null>(null);
const mappedCount = ref<number | null>(null);
const ignoredCount = ref<number | null>(null);

const statusTabs = ref([
  { value: 'Unmapped', label: 'Eşleştirme Bekleyen', count: unmappedCount },
  { value: 'Mapped',   label: 'Eşleştirilmiş',       count: mappedCount },
  { value: 'Ignored',  label: 'Yoksayılan',           count: ignoredCount },
]);

// Per-row state for Unmapped items
interface RowState {
  selectedLocalId?: number;
  currentSearch?: string;
  netsisCode?: string;
  showCreateForm?: boolean;
  newCategory?: number;
  newUnit?: number;
  newPickingType?: number;
}
const rowState = reactive<Record<number, RowState>>({});

// Edit state for Mapped/Ignored items
const editingId = ref<number | null>(null);
const editState = reactive<{ selectedLocalId?: number; netsisCode?: string }>({});

// ---------- load ----------
const load = async () => {
  loading.value = true;
  try {
    const result = await stockMappingService.getAll({
      statusFilter: activeStatus.value,
      search: search.value,
      pageNumber: page.value,
      pageSize: 30,
    });
    mappings.value = result.items;
    totalCount.value = result.totalCount;
    totalPages.value = result.totalPages;

    // Initialize row state for new items
    for (const item of result.items) {
      if (!(item.id in rowState)) {
        rowState[item.id] = { newUnit: 0, newPickingType: 1 };
      }
    }

    // Update tab count for current tab
    if (activeStatus.value === 'Unmapped') unmappedCount.value = result.totalCount;
    else if (activeStatus.value === 'Mapped') mappedCount.value = result.totalCount;
    else if (activeStatus.value === 'Ignored') ignoredCount.value = result.totalCount;
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Yükleme başarısız.', 'error');
  } finally {
    loading.value = false;
  }
};

const loadCounts = async () => {
  try {
    const [u, m, i] = await Promise.all([
      stockMappingService.getAll({ statusFilter: 'Unmapped', pageSize: 1 }),
      stockMappingService.getAll({ statusFilter: 'Mapped',   pageSize: 1 }),
      stockMappingService.getAll({ statusFilter: 'Ignored',  pageSize: 1 }),
    ]);
    unmappedCount.value = u.totalCount;
    mappedCount.value   = m.totalCount;
    ignoredCount.value  = i.totalCount;
  } catch { /* sessiz */ }
};

const refresh = async () => {
  page.value = 1;
  await load();
  await loadCounts();
};

const switchTab = (val: string) => {
  activeStatus.value = val;
  page.value = 1;
  editingId.value = null;
  load();
};

let searchTimer: ReturnType<typeof setTimeout> | null = null;
const onSearch = () => {
  if (searchTimer) clearTimeout(searchTimer);
  searchTimer = setTimeout(() => { page.value = 1; load(); }, 400);
};

// ---------- unmapped actions ----------
const onStockSelected = (id: number, item: any) => {
  if (!rowState[id]) rowState[id] = { newUnit: 0, newPickingType: 1 };
  rowState[id].selectedLocalId = item.id;
  rowState[id].netsisCode = item.netsisStockCode ?? item.NetsisStockCode ?? '';
};

const createAndSelectStock = async (mapping: StockMappingDto) => {
  const s = rowState[mapping.id];
  if (!s?.currentSearch || !s.newCategory) return;
  try {
    const newStock = await stockService.create({
      stockCode: s.currentSearch,
      stockName: mapping.externalName,
      category: s.newCategory,
      unit: s.newUnit ?? 0,
      pickingType: s.newPickingType ?? 1,
    });
    s.selectedLocalId = newStock.id;
    s.showCreateForm = false;
    notificationStore.add(`"${s.currentSearch}" stok kartı oluşturuldu.`, 'success');
  } catch (e) {
    notificationStore.add('Stok oluşturulamadı: ' + (ApiErrorUtils.getErrorMessage(e) || ''), 'error');
  }
};

const mapItem = async (mapping: StockMappingDto, ignore: boolean) => {
  const s = rowState[mapping.id];
  try {
    if (!ignore && s?.selectedLocalId && s.netsisCode !== undefined) {
      await stockService.updateNetsisCode(s.selectedLocalId, s.netsisCode || null);
    }
    await stockMappingService.map({
      mappingId: mapping.id,
      localStockId: ignore ? null : s?.selectedLocalId,
      ignore,
    });
    notificationStore.add(ignore ? 'Stok yoksayıldı.' : 'Eşleştirme kaydedildi.', 'success');
    await refresh();
  } catch (e) {
    notificationStore.add('Eşleştirme başarısız: ' + (ApiErrorUtils.getErrorMessage(e) || ''), 'error');
  }
};

// ---------- mapped / ignored edit ----------
const startEdit = (item: StockMappingDto) => {
  editingId.value = item.id;
  editState.selectedLocalId = item.localStockId ?? undefined;
  editState.netsisCode = item.netsisStockCode ?? '';
};

const onEditStockSelected = (sel: any) => {
  editState.selectedLocalId = sel.id;
  editState.netsisCode = sel.netsisStockCode ?? sel.NetsisStockCode ?? '';
};

const cancelEdit = () => { editingId.value = null; };

const saveEdit = async (item: StockMappingDto) => {
  if (!editState.selectedLocalId) return;
  try {
    if (editState.netsisCode !== undefined) {
      await stockService.updateNetsisCode(editState.selectedLocalId, editState.netsisCode || null);
    }
    await stockMappingService.map({
      mappingId: item.id,
      localStockId: editState.selectedLocalId,
      ignore: false,
    });
    notificationStore.add('Eşleştirme güncellendi.', 'success');
    editingId.value = null;
    await refresh();
  } catch (e) {
    notificationStore.add('Güncelleme başarısız: ' + (ApiErrorUtils.getErrorMessage(e) || ''), 'error');
  }
};

const clearMapping = async (item: StockMappingDto) => {
  if (!window.confirm(`"${item.externalName}" eşleştirmesi kaldırılacak. Onaylıyor musunuz?`)) return;
  try {
    await stockMappingService.map({ mappingId: item.id, localStockId: null, ignore: false });
    notificationStore.add('Eşleştirme kaldırıldı.', 'success');
    await refresh();
  } catch (e) {
    notificationStore.add('İşlem başarısız: ' + (ApiErrorUtils.getErrorMessage(e) || ''), 'error');
  }
};

// ---------- bulk actions ----------
const autoMatch = async () => {
  autoMatching.value = true;
  try {
    const result = await stockMappingService.autoMatch();
    if (result.matchedCount > 0) {
      let msg = `${result.matchedCount} stok otomatik eşleştirildi.`;
      if (result.ordersUnlocked > 0) msg += ` ${result.ordersUnlocked} sipariş hazır durumuna geçti.`;
      notificationStore.add(msg, 'success');
      await refresh();
    } else {
      notificationStore.add('İsim eşleşmesi bulunamadı. Manuel eşleştirme gerekiyor.', 'warning');
    }
  } catch (e) {
    notificationStore.add('Otomatik eşleştirme başarısız: ' + (ApiErrorUtils.getErrorMessage(e) || ''), 'error');
  } finally {
    autoMatching.value = false;
  }
};

const downloadTemplate = async () => {
  try {
    const blob = await stockMappingService.exportUnmapped();
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', 'EslestirilecekStoklar.xlsx');
    document.body.appendChild(link);
    link.click();
    link.remove();
    URL.revokeObjectURL(url);
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Dosya indirilemedi.', 'error');
  }
};

const uploadMappings = async (event: Event) => {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (!file) return;
  const formData = new FormData();
  formData.append('file', file);
  try {
    loading.value = true;
    const result = await stockMappingService.importMappings(formData);
    if (result.mappedCount > 0) notificationStore.add(`${result.mappedCount} eşleştirme güncellendi.`, 'success');
    if (result.notFoundStocks.length > 0) {
      notificationStore.add(`${result.notFoundStocks.length} yerel stok bulunamadı: ${result.notFoundStocks.slice(0, 3).join(', ')}${result.notFoundStocks.length > 3 ? '...' : ''}`, 'warning');
    }
    if (result.mappedCount > 0) await refresh();
  } catch (e) {
    notificationStore.add('Yükleme başarısız: ' + (ApiErrorUtils.getErrorMessage(e) || ''), 'error');
  } finally {
    loading.value = false;
    if (mappingFileInput.value) mappingFileInput.value.value = '';
  }
};

const uploadStocks = async (event: Event) => {
  const file = (event.target as HTMLInputElement).files?.[0];
  if (!file) return;
  const formData = new FormData();
  formData.append('file', file);
  try {
    loading.value = true;
    const result = await shipmentService.importStocks(formData);
    const msg = `Eklendi: ${result.added}, Güncellendi: ${result.updated}, Atlandı: ${result.skipped}`;
    notificationStore.add(msg, result.errors.length > 0 ? 'warning' : 'success');
    if (result.errors.length > 0) result.errors.slice(0, 3).forEach(e => notificationStore.add(e, 'error'));
    window.location.reload();
  } catch (e) {
    notificationStore.add('Stok yükleme başarısız: ' + (ApiErrorUtils.getErrorMessage(e) || ''), 'error');
  } finally {
    loading.value = false;
    if (stockFileInput.value) stockFileInput.value.value = '';
  }
};

onMounted(() => {
  load();
  loadCounts();
});
</script>
