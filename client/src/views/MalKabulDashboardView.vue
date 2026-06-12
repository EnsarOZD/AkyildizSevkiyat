<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-950 pb-28">

    <!-- Sticky Top Bar -->
    <div class="sticky top-0 z-30 bg-white dark:bg-gray-900 border-b border-gray-100 dark:border-gray-800 shadow-sm">
      <div class="px-4 pt-4 pb-3">
        <div class="flex items-center gap-3 mb-3">
          <button @click="router.back()" class="p-2 rounded-xl text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-800 -ml-1">
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M15 19l-7-7 7-7" />
            </svg>
          </button>
          <div class="flex-1 min-w-0">
            <h1 class="text-base font-black text-gray-900 dark:text-gray-100 leading-tight">Mal Kabul</h1>
            <p class="text-[11px] text-gray-500 dark:text-gray-400 font-medium">
              {{ loading ? 'Yükleniyor...' : `${materials.length} malzeme bekliyor` }}
            </p>
          </div>
          <router-link
            to="/goods-receipts"
            class="text-[11px] font-black text-blue-500 uppercase tracking-widest whitespace-nowrap"
          >
            İrsaliyeler
          </router-link>
        </div>

        <!-- Search -->
        <div class="relative">
          <span class="absolute inset-y-0 left-0 pl-3.5 flex items-center text-gray-400 pointer-events-none">
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
          </span>
          <input
            v-model="searchTerm"
            @input="handleSearch"
            type="search"
            placeholder="Malzeme adı veya stok kodu..."
            class="w-full pl-10 pr-4 py-2.5 bg-gray-100 dark:bg-gray-800 text-gray-900 dark:text-gray-100 border-none rounded-xl text-sm font-medium focus:ring-2 focus:ring-blue-500 focus:bg-white dark:focus:bg-gray-700 transition-all"
          />
        </div>
      </div>
    </div>

    <!-- Content -->
    <div class="px-4 pt-4 space-y-3">

      <!-- Loading Skeletons -->
      <template v-if="loading">
        <div v-for="i in 6" :key="i" class="h-20 bg-white dark:bg-gray-900 rounded-2xl animate-pulse border border-gray-100 dark:border-gray-800" />
      </template>

      <!-- Empty State -->
      <div v-else-if="materials.length === 0" class="flex flex-col items-center justify-center py-20 text-center">
        <div class="w-16 h-16 bg-gray-100 dark:bg-gray-800 rounded-full flex items-center justify-center mb-4">
          <svg class="w-8 h-8 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5m16 0h-2.586a1 1 0 00-.707.293l-2.414 2.414a1 1 0 01-.707.293h-3.172a1 1 0 01-.707-.293l-2.414-2.414A1 1 0 006.586 13H4" />
          </svg>
        </div>
        <h3 class="text-sm font-black text-gray-900 dark:text-gray-100">Bekleyen malzeme yok</h3>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-1 max-w-[220px]">
          {{ searchTerm ? 'Aramanızla eşleşen malzeme bulunamadı.' : 'Tüm siparişler teslim alındı.' }}
        </p>
        <button v-if="searchTerm" @click="searchTerm = ''; loadMaterials()" class="mt-3 text-xs font-bold text-blue-500 underline underline-offset-2">
          Aramayı Temizle
        </button>
      </div>

      <!-- Material Cards -->
      <template v-else>
        <div
          v-for="mat in materials"
          :key="mat.stockMasterId"
          class="bg-white dark:bg-gray-900 rounded-2xl border-2 transition-all overflow-hidden"
          :class="store.hasEntry(mat.stockMasterId)
            ? 'border-emerald-400 dark:border-emerald-600'
            : 'border-gray-100 dark:border-gray-800'"
        >
          <div class="px-4 py-3.5 flex items-center gap-3">

            <!-- Status dot / check -->
            <div class="flex-shrink-0">
              <div
                v-if="store.hasEntry(mat.stockMasterId)"
                class="w-8 h-8 bg-emerald-500 rounded-full flex items-center justify-center shadow-md shadow-emerald-100 dark:shadow-none"
              >
                <svg class="w-4 h-4 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="3" d="M5 13l4 4L19 7" />
                </svg>
              </div>
              <div v-else class="w-8 h-8 bg-gray-100 dark:bg-gray-800 rounded-full flex items-center justify-center">
                <svg class="w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 13V6a2 2 0 00-2-2H6a2 2 0 00-2 2v7m16 0v5a2 2 0 01-2 2H6a2 2 0 01-2-2v-5" />
                </svg>
              </div>
            </div>

            <!-- Material Info -->
            <div class="flex-1 min-w-0">
              <h3 class="text-sm font-black text-gray-900 dark:text-gray-100 leading-tight truncate">{{ mat.stockName }}</h3>
              <div class="flex items-center gap-2 mt-0.5">
                <span class="text-[10px] font-bold text-gray-400 uppercase tracking-wider">{{ mat.stockCode }}</span>
                <span class="text-gray-200 dark:text-gray-700 text-[10px]">•</span>
                <span class="text-[10px] font-bold text-blue-500">{{ mat.allocationCount }} sipariş</span>
                <span class="text-gray-200 dark:text-gray-700 text-[10px]">•</span>
                <span class="text-[10px] font-bold text-gray-600 dark:text-gray-400">{{ mat.totalRemainingQty }} {{ mat.unit }} bekleniyor</span>
              </div>
              <!-- Session summary if added -->
              <div v-if="store.hasEntry(mat.stockMasterId)" class="mt-1 flex items-center gap-1">
                <span class="text-[10px] font-black text-emerald-600 dark:text-emerald-400">
                  {{ getSessionSummary(mat.stockMasterId) }} teslim alındı
                </span>
              </div>
            </div>

            <!-- Action Button -->
            <button
              @click="openSheet(mat)"
              class="flex-shrink-0 px-4 py-2 rounded-xl text-xs font-black transition-all active:scale-95"
              :class="store.hasEntry(mat.stockMasterId)
                ? 'bg-emerald-50 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400 border border-emerald-200 dark:border-emerald-800'
                : 'bg-blue-600 text-white shadow-md shadow-blue-100 dark:shadow-none hover:bg-blue-700'"
            >
              {{ store.hasEntry(mat.stockMasterId) ? 'Düzenle' : 'Ekle' }}
            </button>
          </div>

          <!-- Allocation preview -->
          <div v-if="!store.hasEntry(mat.stockMasterId) && mat.allocationCount > 1" class="border-t border-gray-50 dark:border-gray-800/50 px-4 py-2 flex gap-3 overflow-x-auto">
            <div
              v-for="alloc in mat.allocations.slice(0, 4)"
              :key="alloc.purchaseOrderLineId"
              class="flex-shrink-0 text-[10px] bg-gray-50 dark:bg-gray-800 rounded-lg px-2 py-1 font-bold text-gray-500 dark:text-gray-400 whitespace-nowrap"
            >
              {{ alloc.purchaseOrderNumber }} · {{ alloc.remainingQty }} {{ mat.unit }}
            </div>
            <div v-if="mat.allocationCount > 4" class="flex-shrink-0 text-[10px] text-gray-400 font-bold self-center">
              +{{ mat.allocationCount - 4 }}
            </div>
          </div>
        </div>
      </template>
    </div>
  </div>

  <!-- Floating Session Bar (fixed bottom) -->
  <Transition name="slide-up">
    <div
      v-if="store.totalEntryCount > 0"
      class="fixed bottom-0 left-0 right-0 z-40 px-4 pb-safe-bottom"
      style="padding-bottom: max(1rem, env(safe-area-inset-bottom));"
    >
      <button
        @click="showPostModal = true"
        class="w-full bg-blue-600 hover:bg-blue-700 active:scale-[0.98] text-white rounded-2xl shadow-2xl shadow-blue-200 dark:shadow-none transition-all flex items-center px-5 py-4"
      >
        <div class="flex items-center gap-3 flex-1 min-w-0">
          <div class="w-7 h-7 bg-white/20 rounded-full flex items-center justify-center flex-shrink-0">
            <span class="text-xs font-black">{{ store.totalEntryCount }}</span>
          </div>
          <div class="text-left min-w-0">
            <p class="text-sm font-black leading-tight">{{ store.totalEntryCount }} malzeme eklendi</p>
            <p class="text-xs text-blue-200 font-medium">İrsaliye gir ve postla</p>
          </div>
        </div>
        <svg class="w-5 h-5 text-blue-200 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M9 5l7 7-7 7" />
        </svg>
      </button>
    </div>
  </Transition>

  <!-- Malzeme Kabul Bottom Sheet -->
  <MalzemeKabulSheet
    :isOpen="sheetOpen"
    :material="selectedMaterial"
    @close="sheetOpen = false"
    @saved="onEntrySaved"
  />

  <!-- İrsaliye + Postla Modal -->
  <IrsaliyePostlaModal
    :isOpen="showPostModal"
    @close="showPostModal = false"
    @posted="onPosted"
  />
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import purchaseOrderService from '../services/purchaseOrderService';
import { useNotificationStore } from '../stores/notification';
import { useMalKabulStore } from '../stores/malKabul';
import MalzemeKabulSheet from '../components/MalzemeKabulSheet.vue';
import IrsaliyePostlaModal from '../components/IrsaliyePostlaModal.vue';

const router = useRouter();
const notificationStore = useNotificationStore();
const store = useMalKabulStore();

const materials = ref<any[]>([]);
const loading = ref(false);
const searchTerm = ref('');
const sheetOpen = ref(false);
const showPostModal = ref(false);
const selectedMaterial = ref<any>(null);

let searchTimeout: ReturnType<typeof setTimeout> | null = null;

const loadMaterials = async (search?: string) => {
    loading.value = true;
    try {
        materials.value = await purchaseOrderService.getReceivableMaterials(search);
    } catch {
        notificationStore.add('Malzemeler yüklenemedi.', 'error');
    } finally {
        loading.value = false;
    }
};

const handleSearch = () => {
    if (searchTimeout) clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => loadMaterials(searchTerm.value || undefined), 350);
};

const openSheet = (mat: any) => {
    selectedMaterial.value = mat;
    sheetOpen.value = true;
};

const onEntrySaved = () => {
    // Material card updates reactively via store
};

const onPosted = (_id: string) => {
    showPostModal.value = false;
};

const getSessionSummary = (stockMasterId: number): string => {
    const entry = store.getEntry(stockMasterId);
    if (!entry) return '';
    const total = entry.allocations.reduce((s, a) => s + (a.receivedQty || 0), 0);
    return `${total} ${entry.unit}`;
};

onMounted(() => loadMaterials());
</script>

<style scoped>
.slide-up-enter-active,
.slide-up-leave-active {
    transition: transform 0.25s cubic-bezier(0.32, 0.72, 0, 1), opacity 0.2s ease;
}
.slide-up-enter-from,
.slide-up-leave-to {
    transform: translateY(100%);
    opacity: 0;
}
</style>
