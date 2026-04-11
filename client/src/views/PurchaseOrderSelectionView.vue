<template>
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
    <div class="mb-6 flex justify-between items-center">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100 font-display">Mal Kabul için Sipariş Seç</h1>
        <p class="text-gray-500 dark:text-gray-400 text-sm mt-1">İrsaliye oluşturmak istediğiniz siparişi ürün veya tedarikçi bazlı arayın.</p>
      </div>
      <div class="flex items-center gap-3">
        <button
          @click="openNoPOModal"
          class="flex items-center gap-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-700 dark:text-gray-300 px-3 sm:px-4 py-2 rounded-lg text-sm font-bold shadow-sm hover:bg-gray-50 dark:hover:bg-gray-750 transition-all"
        >
          <span>➕ <span class="hidden xs:inline">Siparişsiz Mal Kabul</span><span class="xs:hidden">Mal Kabul</span></span>
        </button>
        <router-link to="/goods-receipts" class="text-sm text-blue-600 hover:text-blue-800 font-medium">
          &larr; İrsaliye Listesine Dön
        </router-link>
      </div>
    </div>

    <!-- Multi-selection summary (Floating) -->
    <div v-if="selectedPoIds.length > 0" class="fixed bottom-6 left-1/2 -translate-x-1/2 z-50 animate-bounce-subtle">
       <button
         @click="startMultiPO"
         class="bg-blue-600 text-white px-8 py-3 rounded-full font-bold shadow-2xl hover:bg-blue-700 transition-all flex items-center gap-3 border-4 border-white dark:border-gray-900"
       >
         <span>📦 Seçili {{ selectedPoIds.length }} Siparişle Başlat</span>
         <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
           <path fill-rule="evenodd" d="M10.293 3.293a1 1 0 011.414 0l6 6a1 1 0 010 1.414l-6 6a1 1 0 01-1.414-1.414L14.586 11H3a1 1 0 110-2h11.586l-4.293-4.293a1 1 0 010-1.414z" clip-rule="evenodd" />
         </svg>
       </button>
    </div>

    <!-- Filters Section -->
    <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 p-5 mb-6 transition-all hover:shadow-md">
      <div class="grid grid-cols-1 md:grid-cols-4 gap-4 items-end">
        <div class="md:col-span-1">
          <label class="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">Tedarikçi <span class="text-red-500">*</span></label>
          <select
            v-model="filters.supplierId"
            class="w-full border-gray-200 dark:border-gray-700 rounded-lg shadow-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all text-sm p-2.5 bg-gray-50 dark:bg-gray-800 dark:text-gray-100"
          >
            <option value="">Tedarikçi Seçiniz...</option>
            <option v-for="s in suppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
          </select>
        </div>

        <div class="md:col-span-2">
          <label class="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">Ürün Adı veya Stok Kodu Ara</label>
          <div class="relative">
            <span class="absolute inset-y-0 left-0 pl-3 flex items-center text-gray-400 dark:text-gray-600">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
              </svg>
            </span>
            <input
              type="text"
              v-model="filters.searchTerm"
              placeholder="Filtrelemek için yazın..."
              class="w-full pl-10 border-gray-200 dark:border-gray-700 rounded-lg shadow-sm focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all text-sm p-2.5 dark:bg-gray-800 dark:text-gray-100"
            />
          </div>
        </div>

        <div class="flex gap-2">
          <button
            @click="fetchPOs"
            :disabled="!filters.supplierId || loading"
            class="flex-1 bg-blue-600 text-white rounded-lg px-4 py-2.5 font-bold text-sm shadow-sm hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-all"
          >
            <span v-if="loading">Yükleniyor...</span>
            <span v-else>PO Listele</span>
          </button>
          <button @click="resetFilters" class="p-2.5 border border-gray-200 dark:border-gray-700 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-all" title="Sıfırla">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 text-gray-400 dark:text-gray-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
          </button>
        </div>
      </div>

      <!-- Date Range (Optional toggle) -->
      <div class="mt-4 flex flex-wrap gap-4 items-center">
         <div class="flex items-center gap-2">
            <input type="date" v-model="filters.fromDate" class="text-xs border-gray-200 dark:border-gray-700 rounded p-1 dark:bg-gray-800 dark:text-gray-100" />
            <span class="text-gray-400 dark:text-gray-600">-</span>
            <input type="date" v-model="filters.toDate" class="text-xs border-gray-200 dark:border-gray-700 rounded p-1 dark:bg-gray-800 dark:text-gray-100" />
         </div>
         <div class="text-[10px] text-gray-400 dark:text-gray-600 font-bold uppercase tracking-widest italic">
            * Stats only include POS with "Posted" receipts
         </div>
      </div>
    </div>

    <!-- PO List -->
    <div v-if="!filters.supplierId" class="text-center py-20 bg-gray-50 dark:bg-gray-800 rounded-2xl border-2 border-dashed border-gray-200 dark:border-gray-700">
       <div class="mx-auto h-12 w-12 text-gray-300 dark:text-gray-600">
         <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor">
           <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
         </svg>
       </div>
       <h3 class="mt-2 text-sm font-medium text-gray-900 dark:text-gray-100 uppercase">Liste boş</h3>
       <p class="mt-1 text-sm text-gray-500 dark:text-gray-400">Lütfen bir tedarikçi seçerek işe başlayın.</p>
    </div>

    <div v-else-if="loading" class="flex justify-center py-20">
       <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
    </div>

    <div v-else-if="poList.length === 0" class="text-center py-20 bg-gray-50 dark:bg-gray-800 rounded-xl border border-gray-100 dark:border-gray-700">
       <p class="text-gray-500 dark:text-gray-400">Criteria uygun sipariş bulunamadı.</p>
    </div>

    <div v-else class="space-y-4">
       <div v-for="po in poList" :key="po.purchaseOrderId"
            class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 overflow-hidden hover:border-blue-200 transition-all group">
         <div class="p-4 sm:p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            <div class="flex-1">
               <div class="flex items-center gap-3">
                  <input
                    type="checkbox"
                    :id="'po-' + po.purchaseOrderId"
                    v-model="selectedPoIds"
                    :value="po.purchaseOrderId"
                    class="h-5 w-5 rounded border-gray-300 text-blue-600 focus:ring-blue-500 cursor-pointer"
                  />
                  <label :for="'po-' + po.purchaseOrderId" class="text-blue-600 font-extrabold text-lg cursor-pointer">{{ po.orderNumber }}</label>
                  <span v-if="po.receivedPercent >= 100" class="bg-green-100 text-green-700 text-[10px] font-bold px-2 py-0.5 rounded-full uppercase">Tamamlandı</span>
                  <span v-else-if="po.receivedPercent > 0" class="bg-yellow-100 text-yellow-700 text-[10px] font-bold px-2 py-0.5 rounded-full uppercase">Kısmi</span>
                  <span v-else class="bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 text-[10px] font-bold px-2 py-0.5 rounded-full uppercase">Bekliyor</span>
               </div>
               <div class="flex items-center gap-4 mt-1 text-sm text-gray-500 dark:text-gray-400 font-medium">
                  <div class="flex items-center gap-1">
                     <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                     </svg>
                     {{ formatDate(po.orderDate) }}
                  </div>
                  <div class="flex items-center gap-1">
                     <svg xmlns="http://www.w3.org/2000/svg" class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
                     </svg>
                     {{ po.supplierNameSnapshot }}
                  </div>
               </div>

               <!-- Progress Bar -->
               <div class="mt-4 max-w-sm">
                  <div class="flex justify-between text-[10px] font-bold text-gray-400 dark:text-gray-600 uppercase tracking-widest mb-1.5">
                     <span>Tamamlanma Oranı</span>
                     <span class="text-gray-600 dark:text-gray-400">%{{ po.receivedPercent.toFixed(1) }}</span>
                  </div>
                  <div class="h-2 w-full bg-gray-100 dark:bg-gray-700 rounded-full overflow-hidden">
                     <div
                        class="h-full transition-all duration-500 ease-out"
                        :class="po.receivedPercent >= 100 ? 'bg-green-500' : (po.receivedPercent > 50 ? 'bg-blue-500' : 'bg-yellow-500')"
                        :style="{ width: po.receivedPercent + '%' }"
                     ></div>
                  </div>
               </div>
            </div>

            <div class="flex flex-col sm:items-end gap-2 shrink-0">
               <div class="flex gap-4 mb-2">
                  <div class="text-right">
                     <div class="text-[10px] font-bold text-gray-400 dark:text-gray-600 uppercase">Kalan Satır</div>
                     <div class="text-sm font-bold text-gray-700 dark:text-gray-300">{{ po.remainingLineCount }} / {{ po.matchingLinesCount }}</div>
                  </div>
                  <div class="text-right">
                     <div class="text-[10px] font-bold text-gray-400 dark:text-gray-600 uppercase">Kalan Miktar</div>
                     <div class="text-sm font-bold text-indigo-700">{{ po.remainingTotalQty }}</div>
                  </div>
               </div>
               <button
                  @click="selectPO(po.purchaseOrderId)"
                  class="bg-indigo-50 text-indigo-700 hover:bg-indigo-600 hover:text-white px-6 py-2 rounded-lg font-bold text-sm transition-all shadow-sm border border-indigo-100"
               >
                  Bu PO ile Mal Kabul Aç &rarr;
               </button>
            </div>
         </div>

         <!-- Expandable Preview -->
         <div v-if="po.matchingLinesPreview && po.matchingLinesPreview.length > 0" class="bg-gray-50 dark:bg-gray-800 border-t border-gray-100 dark:border-gray-700 p-4">
            <h4 class="text-[10px] font-bold text-gray-400 dark:text-gray-600 uppercase tracking-widest mb-3">İçerik Önizleme (Eşleşenler)</h4>
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
               <div v-for="line in po.matchingLinesPreview" :key="line.purchaseOrderLineId" class="bg-white dark:bg-gray-900 p-3 rounded-lg border border-gray-100 dark:border-gray-700 shadow-xs">
                  <div class="flex justify-between items-start gap-2 mb-2">
                     <div class="text-xs font-bold text-gray-900 dark:text-gray-100 leading-tight break-all">{{ line.productName }}</div>
                     <div class="text-[10px] font-mono bg-gray-100 dark:bg-gray-700 px-1 rounded text-gray-500 dark:text-gray-400 flex-shrink-0">{{ line.stockCode }}</div>
                  </div>
                  <div class="flex justify-between items-end">
                     <div>
                        <div class="text-[9px] text-gray-400 dark:text-gray-600 font-bold uppercase">Sipariş / Kabul</div>
                        <div class="text-xs font-semibold text-gray-600 dark:text-gray-400">
                           {{ line.orderedQty }} / <span class="text-green-600">{{ line.receivedQty }}</span>
                        </div>
                     </div>
                     <div class="text-right">
                        <div class="text-[9px] text-gray-400 dark:text-gray-600 font-bold uppercase">Kalan</div>
                        <div class="text-xs font-black text-red-600">{{ line.remainingQty }}</div>
                     </div>
                  </div>
               </div>
            </div>
            <div v-if="po.matchingLinesCount > po.matchingLinesPreview.length" class="mt-3 text-[10px] text-center text-gray-400 dark:text-gray-600 font-medium">
               +{{ po.matchingLinesCount - po.matchingLinesPreview.length }} ürün daha eşleşti...
            </div>
         </div>
       </div>
    </div>
  </div>

  <CreateGoodsReceiptModal
    v-if="showCreateGRModal"
    :isOpen="showCreateGRModal"
    :purchaseOrder="selectedPoDetail"
    :purchaseOrderIds="multiPoIds"
    :initialSupplierId="filters.supplierId"
    @close="showCreateGRModal = false"
    @saved="onGRSaved"
  />
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { useRouter } from 'vue-router';
import purchaseOrderService from '../services/purchaseOrderService';
import { supplierService } from '../services/supplierService';
import CreateGoodsReceiptModal from '../components/CreateGoodsReceiptModal.vue';

const router = useRouter();
const loading = ref(false);
const suppliers = ref<any[]>([]);
const poList = ref<any[]>([]);

const showCreateGRModal = ref(false);
const selectedPoId = ref<string>('');
const selectedPoDetail = ref<any>(null);
const selectedPoIds = ref<string[]>([]);
const multiPoIds = ref<string[]>([]);

const filters = ref({
    supplierId: '',
    searchTerm: '',
    fromDate: new Date(new Date().setDate(new Date().getDate() - 30)).toISOString().split('T')[0],
    toDate: new Date().toISOString().split('T')[0]
});

// Debounce state
let searchTimeout: any = null;

const fetchSuppliers = async () => {
    try {
        suppliers.value = await supplierService.getAll();
    } catch (e) { console.error('Tedarikçiler yüklenemedi'); }
};

const fetchPOs = async () => {
    if (!filters.value.supplierId) return;

    loading.value = true;
    try {
        poList.value = await purchaseOrderService.getReceivable(filters.value);
    } catch (e) {
        console.error('PO listesi yüklenemedi');
    } finally {
        loading.value = false;
    }
};

const resetFilters = () => {
    filters.value = {
        supplierId: '',
        searchTerm: '',
        fromDate: new Date(new Date().setDate(new Date().getDate() - 30)).toISOString().split('T')[0],
        toDate: new Date().toISOString().split('T')[0]
    };
    poList.value = [];
};

const formatDate = (date: string) => {
    return new Date(date).toLocaleDateString('tr-TR');
};

const selectPO = async (id: string) => {
    selectedPoId.value = id;
    multiPoIds.value = [id];
    try {
        selectedPoDetail.value = await purchaseOrderService.getById(id);
    } catch {
        selectedPoDetail.value = null;
    }
    showCreateGRModal.value = true;
};

const startMultiPO = () => {
    if (selectedPoIds.value.length === 0) return;
    multiPoIds.value = [...selectedPoIds.value];
    selectedPoDetail.value = null; // We use IDs instead
    showCreateGRModal.value = true;
};

const openNoPOModal = () => {
    selectedPoId.value = '';
    multiPoIds.value = [];
    selectedPoDetail.value = null;
    showCreateGRModal.value = true;
};

const onGRSaved = (grId: string) => {
    showCreateGRModal.value = false;
    router.push({ name: 'GoodsReceiptDetail', params: { id: grId } });
};

watch(() => filters.value.searchTerm, () => {
    if (searchTimeout) clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => {
        fetchPOs();
    }, 400);
});

watch(() => filters.value.supplierId, () => {
    selectedPoIds.value = []; // Clear selection if supplier changes
    fetchPOs();
});

onMounted(() => {
    fetchSuppliers();
});
</script>

<style scoped>
.font-display {
  font-family: 'Outfit', 'Inter', sans-serif;
}

@keyframes bounce-subtle {
  0%, 100% { transform: translate(-50%, 0); }
  50% { transform: translate(-50%, -5px); }
}
.animate-bounce-subtle {
  animation: bounce-subtle 2s infinite ease-in-out;
}
</style>
