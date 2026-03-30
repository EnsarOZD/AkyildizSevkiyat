<template>
  <div class="max-w-8xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
    <!-- Main Grid -->
    <div class="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
      
      <!-- Left: Discovery & Selection (Col 8) -->
      <div class="lg:col-span-8 space-y-6">
        
        <!-- Header & Stats Card -->
        <div class="bg-white dark:bg-gray-900 rounded-3xl p-6 shadow-sm border border-gray-100 dark:border-gray-800 flex flex-col md:flex-row md:items-center justify-between gap-6 transition-all hover:shadow-md">
           <div class="space-y-1">
              <h1 class="text-3xl font-extrabold text-gray-900 dark:text-gray-100 font-display tracking-tight">Mal Kabul Merkezi</h1>
              <p class="text-gray-500 dark:text-gray-400 text-sm font-medium">Siparişlerinizi bulun, tarayın ve hızlıca sisteme dahil edin.</p>
           </div>
           <div class="flex items-center gap-4">
              <div class="px-4 py-2 bg-indigo-50 dark:bg-indigo-900/20 rounded-2xl border border-indigo-100 dark:border-indigo-800 text-center min-w-[100px]">
                 <p class="text-[10px] font-bold text-indigo-500 uppercase tracking-widest leading-none mb-1">Bekleyen</p>
                 <p class="text-xl font-black text-indigo-700 dark:text-indigo-300 leading-none">{{ receivableCount }}</p>
              </div>
              <div class="px-4 py-2 bg-emerald-50 dark:bg-emerald-900/20 rounded-2xl border border-emerald-100 dark:border-emerald-800 text-center min-w-[100px]">
                 <p class="text-[10px] font-bold text-emerald-500 uppercase tracking-widest leading-none mb-1">Tamamlanan</p>
                 <p class="text-xl font-black text-emerald-700 dark:text-emerald-300 leading-none">{{ completedTodayCount }}</p>
              </div>
           </div>
        </div>

        <!-- Search & Filter Bar -->
        <div class="bg-white dark:bg-gray-900 p-3 rounded-2xl shadow-sm border border-gray-100 dark:border-gray-800 flex flex-col sm:flex-row gap-3">
           <div class="relative flex-1 group">
              <span class="absolute inset-y-0 left-0 pl-4 flex items-center text-gray-400 group-focus-within:text-indigo-500 transition-colors">
                 <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                 </svg>
              </span>
              <input 
                v-model="filters.searchTerm" 
                @input="handleSearch"
                type="text" 
                placeholder="Tedarikçi, ürün veya sipariş no ara..." 
                class="w-full pl-12 pr-4 py-3 bg-gray-50/50 dark:bg-gray-950/30 border-none rounded-xl text-sm focus:ring-2 focus:ring-indigo-500 transition-all font-medium"
              />
           </div>
           <div class="flex items-center gap-2">
              <button @click="openNoPOFlow" class="px-5 py-3 bg-white dark:bg-gray-800 text-indigo-600 dark:text-indigo-400 border-2 border-indigo-100 dark:border-indigo-800 rounded-xl font-bold text-sm flex items-center gap-2 transition-all hover:bg-indigo-50 active:scale-95">
                <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                   <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
                </svg>
                <span>Siparişsiz Kabul</span>
              </button>
              <button @click="showScanModal = true" class="px-5 py-3 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl font-bold text-sm shadow-lg shadow-indigo-100 dark:shadow-none flex items-center gap-2 transition-all active:scale-95">
                <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                   <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" />
                </svg>
                <span>İrsaliye Tara</span>
              </button>
           </div>
        </div>

        <!-- PO List (Tabs/Groups) -->
        <div class="space-y-4">
           <div class="flex items-center justify-between px-2">
              <h3 class="text-xs font-black text-gray-400 dark:text-gray-500 uppercase tracking-widest">Bekleyen Siparişler</h3>
           </div>

           <div v-if="loading" class="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div v-for="i in 4" :key="i" class="h-32 bg-gray-100 dark:bg-gray-800 rounded-2xl animate-pulse border border-gray-100 dark:border-gray-800"></div>
           </div>

           <div v-else-if="poList.length === 0" class="bg-gray-50/50 dark:bg-gray-950/20 border-2 border-dashed border-gray-200 dark:border-gray-800 rounded-3xl py-20 text-center">
              <div class="mx-auto h-16 w-16 text-gray-300 dark:text-gray-700 flex items-center justify-center">
                 <svg class="h-full w-full" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
                 </svg>
              </div>
              <h4 class="mt-4 text-sm font-bold text-gray-900 dark:text-gray-100">Bekleyen sipariş bulunamadı</h4>
              <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Arama terimini değiştirebilir veya siparişsiz kabul yapabilirsiniz.</p>
           </div>

           <div v-else class="grid grid-cols-1 md:grid-cols-2 gap-4 pb-20 lg:pb-0">
              <div 
                v-for="po in poList" 
                :key="po.purchaseOrderId" 
                class="bg-white dark:bg-gray-900 rounded-2xl border-2 transition-all cursor-pointer group relative overflow-hidden flex flex-col h-full"
                :class="selectedPoIds.includes(po.purchaseOrderId) ? 'border-indigo-500 ring-2 ring-indigo-500/20' : 'border-gray-100 dark:border-gray-800 hover:border-indigo-200 dark:hover:border-indigo-900'"
                @click="togglePoSelection(po)"
              >
                 <!-- Selected Indicator overlay -->
                 <div v-if="selectedPoIds.includes(po.purchaseOrderId)" class="absolute top-0 right-0 p-2 bg-indigo-500 text-white rounded-bl-xl shadow-lg animate-in fade-in zoom-in duration-200">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                       <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
                    </svg>
                 </div>

                 <div class="p-4 flex-1">
                    <div class="flex items-start justify-between mb-2">
                       <span class="text-xs font-bold text-gray-400 dark:text-gray-600 uppercase tracking-widest">{{ po.orderNumber }}</span>
                       <span class="bg-gray-100 dark:bg-gray-800 px-2 py-0.5 rounded text-[10px] font-black text-gray-600 dark:text-gray-400 uppercase tracking-tight">PO</span>
                    </div>
                    <h5 class="text-sm font-black text-gray-900 dark:text-gray-100 mb-1 leading-snug group-hover:text-indigo-600 transition-colors">{{ po.supplierNameSnapshot }}</h5>
                    <div class="flex items-center gap-2 text-[10px] text-gray-500 dark:text-gray-400 font-bold mb-4">
                       <span class="whitespace-nowrap">{{ formatDate(po.orderDate) }}</span>
                       <span class="text-gray-200 dark:text-gray-800">•</span>
                       <span class="text-blue-600 dark:text-blue-400">{{ po.matchingLinesCount }} Kalem</span>
                    </div>

                    <!-- Line Preview -->
                    <div class="space-y-1.5 opacity-80 decoration-0">
                       <div v-for="line in po.matchingLinesPreview.slice(0, 2)" :key="line.purchaseOrderLineId" class="flex justify-between text-[11px] leading-tight">
                          <span class="text-gray-600 dark:text-gray-400 truncate max-w-[140px]">{{ line.productName }}</span>
                          <span class="font-black text-gray-900 dark:text-gray-100">{{ line.remainingQty }}</span>
                       </div>
                       <div v-if="po.matchingLinesCount > 2" class="text-[9px] font-bold text-gray-400 uppercase tracking-wider mt-1 text-right">
                          + {{ po.matchingLinesCount - 2 }} daha
                       </div>
                    </div>
                 </div>

                 <!-- Footer Stats -->
                 <div class="bg-gray-50 dark:bg-gray-950/30 px-4 py-2 border-t border-gray-100 dark:border-gray-800 mt-auto">
                    <div class="flex items-center justify-between">
                       <div class="h-1.5 w-24 bg-gray-200 dark:bg-gray-800 rounded-full overflow-hidden">
                          <div class="h-full bg-indigo-500 rounded-full" :style="{ width: po.receivedPercent + '%' }"></div>
                       </div>
                       <span class="text-[9px] font-black text-gray-400 uppercase tracking-tighter">%{{ po.receivedPercent.toFixed(0) }}</span>
                    </div>
                 </div>
              </div>
           </div>
        </div>

      </div>

      <!-- Right: Creation Workspace & Session (Col 4) -->
      <div class="lg:col-span-4 space-y-6">
        
        <!-- Intake Workspace Panel -->
        <IntakeSessionPanel 
           ref="sessionPanel"
           :purchaseOrderIds="selectedPoIds"
           :suppliers="suppliers"
           :initialSupplierId="filters.supplierId"
           :saving="saving"
           :forcedShow="isNoPoMode"
           @submit="createGoodsReceipt"
           @reset="resetSelection"
           @scan="showScanModal = true"
        />

        <!-- Recent Activity / Drafts -->
        <div class="bg-white dark:bg-gray-900 rounded-2xl p-5 shadow-sm border border-gray-100 dark:border-gray-800">
           <h3 class="text-xs font-black text-gray-400 dark:text-gray-500 uppercase tracking-widest mb-4">Son İrsaliyeler (Taslak)</h3>
           <div v-if="recentDrafts.length === 0" class="text-center py-6 text-xs text-gray-400 font-medium">
              Taslak kaydınız bulunmuyor.
           </div>
           <div v-else class="space-y-3">
              <div v-for="draft in recentDrafts" :key="draft.id" @click="goToDetail(draft.id)" class="group cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800/50 p-2.5 rounded-xl transition-all border border-transparent hover:border-gray-200 dark:hover:border-gray-800">
                 <div class="flex items-start justify-between mb-1">
                    <span class="text-xs font-black text-indigo-600 dark:text-indigo-400 leading-none">{{ draft.waybillNo }}</span>
                    <span class="text-[9px] font-bold text-gray-400 uppercase">{{ formatDate(draft.waybillDate) }}</span>
                 </div>
                 <h6 class="text-[11px] font-bold text-gray-800 dark:text-gray-200 truncate">{{ draft.supplierNameSnapshot }}</h6>
              </div>
           </div>
           <router-link to="/goods-receipts/list" class="block w-full text-center mt-4 text-[10px] font-black text-indigo-500 uppercase tracking-widest hover:text-indigo-700 transition-colors">Tümünü Gör &rarr;</router-link>
        </div>

      </div>
    </div>

    <!-- Modals -->
    <InvoiceScanModal 
       :isOpen="showScanModal"
       @close="showScanModal = false"
       @apply="handleScanResult"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import purchaseOrderService from '../services/purchaseOrderService';
import goodsReceiptService from '../services/goodsReceiptService';
import { supplierService } from '../services/supplierService';
import { useNotificationStore } from '../stores/notification';
import InvoiceScanModal from '../components/InvoiceScanModal.vue';
import IntakeSessionPanel from '../components/IntakeSessionPanel.vue';

const router = useRouter();
const route = useRoute();
const notificationStore = useNotificationStore();

const loading = ref(false);
const saving = ref(false);
const showScanModal = ref(false);

const suppliers = ref<any[]>([]);
const poList = ref<any[]>([]);
const recentDrafts = ref<any[]>([]);
const receivableCount = ref(0);
const completedTodayCount = ref(0);
const isNoPoMode = ref(false);

const selectedPoIds = ref<string[]>([]);
const filters = ref({
    supplierId: '',
    searchTerm: '',
    pageNumber: 1,
    pageSize: 10
});

const sessionPanel = ref<any>(null);



const fetchInitialData = async () => {
    loading.value = true;
    try {
        const [sups, pOs, drafts] = await Promise.all([
            supplierService.getAll(),
            purchaseOrderService.getReceivable({ pageSize: 50 }) as Promise<any>,
            goodsReceiptService.getAll({ status: '0' }) as Promise<any> // 0 = Draft
        ]);
        suppliers.value = sups;
        poList.value = Array.isArray(pOs) ? pOs : (pOs?.items || []);
        recentDrafts.value = Array.isArray(drafts) ? drafts.slice(0, 5) : (drafts?.items || []);
        receivableCount.value = poList.value.length;
        
        // Mock completed count for UI aesthetics
        completedTodayCount.value = 0; 
    } catch (e) {
        notificationStore.add('Veriler yüklenemedi.', 'error');
    } finally {
        loading.value = false;
    }
};

const handleSearch = async () => {
    // Basic local filtering for snappiness, or API call
    if (!filters.value.searchTerm) {
        fetchInitialData();
        return;
    }
    loading.value = true;
    try {
        const res = await (purchaseOrderService.getReceivable({ 
            searchTerm: filters.value.searchTerm,
            pageSize: 50
        }) as Promise<any>);
        poList.value = Array.isArray(res) ? res : (res?.items || []);
    } catch (e) {
        notificationStore.add('Arama başarısız.', 'error');
    } finally {
        loading.value = false;
    }
};

const togglePoSelection = (po: any) => {
    isNoPoMode.value = false;
    const idx = selectedPoIds.value.indexOf(po.purchaseOrderId);
    if (idx > -1) {
        selectedPoIds.value.splice(idx, 1);
    } else {
        // Multi-PO selection logic: same supplier check
        if (selectedPoIds.value.length > 0) {
            // We assume first PO sets the supplier constraint
            // In a real robust app, we'd fetch the PO detail to check supplierId
            // but for current snappiness, let's allow it and let backend validate
        }
        selectedPoIds.value.push(po.purchaseOrderId);
    }
    if (selectedPoIds.value.length > 0) {
       sessionPanel.value?.focusWaybill();
    }
};

const resetSelection = () => {
    selectedPoIds.value = [];
    isNoPoMode.value = false;
};

const createGoodsReceipt = async (form: any) => {
    saving.value = true;
    try {
        const payload = {
            ...form,
            purchaseOrderIds: selectedPoIds.value.length > 0 ? selectedPoIds.value : undefined
        };
        const res = await goodsReceiptService.create(payload);
        if (res.hasDuplicateWarning) {
           // We might want to show a warning modal here, 
           // but for MVP let's proceed to detail if OK
        }
        notificationStore.add('İrsaliye oluşturuldu. Kalem girişine yönlendiriliyorsunuz.', 'success');
        goToDetail(res.id);
    } catch (e: any) {
        // Notification store handles formatting
    } finally {
        saving.value = false;
    }
};

const handleScanResult = (data: any) => {
    if (sessionPanel.value) {
        isNoPoMode.value = true;
        if (data.waybillNo) sessionPanel.value.internalForm.waybillNo = data.waybillNo;
        if (data.waybillDate) sessionPanel.value.internalForm.waybillDate = data.waybillDate;
        sessionPanel.value.focusWaybill();
    }
};

const openNoPOFlow = () => {
    resetSelection();
    isNoPoMode.value = true;
    sessionPanel.value?.focusWaybill();
};

const goToDetail = (id: string) => {
    router.push({ name: 'GoodsReceiptDetail', params: { id } });
};

const formatDate = (date: string) => {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('tr-TR');
};

onMounted(async () => {
    await fetchInitialData();
    // Pre-select PO if navigated from PO detail/list
    const poId = route.query.poId as string | undefined;
    if (poId) {
        const match = poList.value.find((po: any) => po.purchaseOrderId === poId);
        if (match) {
            togglePoSelection(match);
        } else {
            // PO may not be in the default list — add directly and let backend validate
            selectedPoIds.value = [poId];
            sessionPanel.value?.focusWaybill();
        }
    }
});
</script>

<style scoped>
.font-display {
  font-family: 'Outfit', 'Inter', sans-serif;
}
</style>
