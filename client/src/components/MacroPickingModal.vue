<template>
  <div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-2 sm:p-4 z-50">
    <div class="bg-gray-100 dark:bg-gray-800 rounded-lg w-full h-full flex flex-col md:max-w-2xl md:h-auto md:max-h-[90vh] shadow-xl overflow-hidden">

      <!-- Header -->
      <div class="bg-white dark:bg-gray-900 px-4 py-3 flex justify-between items-center border-b dark:border-gray-700 shadow-sm z-10">
        <div>
          <h3 class="text-base md:text-lg font-bold text-orange-700 dark:text-orange-400">MACRO TOPLAMA</h3>
          <div class="flex items-center gap-2 mt-0.5">
            <span class="text-xs font-bold px-2 py-0.5 bg-orange-100 dark:bg-orange-900/40 text-orange-700 dark:text-orange-400 rounded">Bölge Toplamı</span>
            <span class="text-xs text-gray-500 dark:text-gray-400">{{ items.length }} Kalem</span>
            <span v-if="savedCount > 0" class="text-xs font-bold text-green-600 dark:text-green-400">
              · {{ savedCount }} kaydedildi
            </span>
          </div>
        </div>
        <button @click="$emit('close')" class="text-gray-400 hover:text-gray-600 p-2 -mr-2">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <!-- Content -->
      <div class="flex-1 overflow-y-auto relative" ref="scrollContainer">
        <div v-if="loading" class="flex flex-col items-center justify-center h-40 gap-3 text-gray-500 dark:text-gray-400">
          <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-orange-600"></div>
          <span class="text-sm">Yükleniyor...</span>
        </div>

        <div v-else-if="items.length === 0" class="flex flex-col items-center justify-center h-40 m-4 bg-white dark:bg-gray-900 rounded border border-dashed border-gray-300 dark:border-gray-700 text-gray-400">
          <span class="text-4xl mb-2">📦</span>
          <p class="text-sm">Toplanacak Macro ürün bulunamadı.</p>
        </div>

        <div v-else class="pb-24">
          <div v-for="group in categorizedItems" :key="group.category" class="mb-2">
            <!-- Group Header -->
            <h4 class="px-3 py-1.5 bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-300 font-bold uppercase tracking-wider text-[10px] border-t dark:border-gray-600 border-b sticky top-0 z-10 flex items-center justify-between">
              <span>{{ group.category }}</span>
              <span class="font-normal text-gray-400 dark:text-gray-500 normal-case">{{ group.completedItems.length }}/{{ group.pendingItems.length + group.completedItems.length }}</span>
            </h4>

            <div class="divide-y divide-gray-100 dark:divide-gray-700">
              <!-- Pending items -->
              <div
                v-for="item in group.pendingItems"
                :key="item.stockCode"
                class="transition-all duration-200"
                :class="[
                  itemState(item).isFull    ? 'bg-green-50 dark:bg-green-900/20' :
                  itemState(item).isPartial ? 'bg-blue-50 dark:bg-blue-900/10'   :
                                              'bg-white dark:bg-gray-900',
                  expandedStock === item.stockCode ? 'ring-2 ring-inset ring-orange-300 dark:ring-orange-700' : ''
                ]"
              >
                <!-- Row Header -->
                <div @click="toggleExpand(item)" class="p-3 flex justify-between items-start cursor-pointer active:bg-gray-50 dark:active:bg-gray-800 select-none">
                  <div class="flex-1 pr-2 min-w-0">
                    <div class="flex items-center gap-1.5 mb-0.5">
                      <!-- Status icon -->
                      <span v-if="itemState(item).isFull" class="w-4 h-4 rounded-full bg-green-500 flex items-center justify-center flex-shrink-0">
                        <svg class="w-2.5 h-2.5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="3">
                          <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7"/>
                        </svg>
                      </span>
                      <span v-else-if="itemState(item).isPartial" class="w-4 h-4 rounded-full bg-blue-400 flex items-center justify-center flex-shrink-0">
                        <span class="text-white text-[8px] font-bold">~</span>
                      </span>
                      <span v-else class="w-4 h-4 rounded-full border-2 border-gray-300 dark:border-gray-600 flex-shrink-0"></span>
                      <span class="text-[10px] font-bold text-gray-400 dark:text-gray-600 font-mono truncate">{{ item.stockCode }}</span>
                    </div>
                    <div class="text-sm font-semibold text-gray-800 dark:text-gray-200 leading-tight mb-1">{{ item.stockName }}</div>
                    <div class="text-xs text-gray-500 dark:text-gray-400">
                      {{ item.unit }} ·
                      <span class="text-orange-600 dark:text-orange-400 font-bold">{{ item.projectCount }} proje</span>
                      <span v-if="itemState(item).isDirty && !itemState(item).isSaved" class="ml-1 text-blue-500 font-bold">· değişti</span>
                      <span v-if="itemState(item).isSaved" class="ml-1 text-green-600 font-bold">· kaydedildi</span>
                      <span v-if="item.localPickedQty > item.totalOrderedQty" class="ml-1 text-orange-600 font-bold">⚠ Sipariş üstü</span>
                    </div>
                  </div>

                  <!-- Qty display + stepper -->
                  <div class="flex flex-col items-end gap-1 flex-shrink-0">
                    <div class="flex items-baseline gap-1"
                      :class="
                        item.localPickedQty > item.totalOrderedQty ? 'text-orange-500 dark:text-orange-400' :
                        itemState(item).isFull    ? 'text-green-600 dark:text-green-400' :
                        itemState(item).isPartial ? 'text-blue-600 dark:text-blue-400'   :
                                                    'text-gray-300 dark:text-gray-600'
                      ">
                      <span class="text-2xl font-bold tracking-tight">{{ item.localPickedQty }}</span>
                      <span class="text-xs font-medium text-gray-400 dark:text-gray-600">/ {{ item.totalOrderedQty }}</span>
                    </div>
                    <div class="flex items-center gap-1 mt-1" @click.stop>
                      <button
                        @click="setFull(item)"
                        class="h-12 px-3 flex items-center justify-center rounded-xl bg-green-50 dark:bg-green-900/20 text-green-600 dark:text-green-400 active:bg-green-100 font-bold text-sm touch-manipulation border border-green-200 dark:border-green-800 shadow-sm"
                      >TAM</button>
                      <button
                        @click="changeQty(item, -1)"
                        class="w-12 h-12 flex items-center justify-center rounded-xl bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 active:bg-gray-200 dark:active:bg-gray-700 font-bold text-2xl touch-manipulation"
                      >−</button>
                      <button
                        @click="changeQty(item, 1)"
                        class="w-12 h-12 flex items-center justify-center rounded-xl bg-orange-50 dark:bg-orange-900/20 text-orange-600 active:bg-orange-100 font-bold text-2xl touch-manipulation border border-orange-200 dark:border-orange-800 shadow-sm"
                      >+</button>
                    </div>
                  </div>
                </div>

                <!-- Expanded Edit Panel -->
                <div v-if="expandedStock === item.stockCode" class="bg-orange-50 dark:bg-gray-800 px-3 pb-3 pt-0 border-t border-dashed border-orange-200 dark:border-orange-900 animate-fade-in-down">
                  <div class="pt-3 grid grid-cols-2 gap-3 items-end">
                    <div>
                      <label class="block text-[10px] font-bold text-gray-400 uppercase tracking-wider mb-1">Manuel Miktar</label>
                      <input
                        type="number"
                        v-model.number="item.localPickedQty"
                        @input="clampQty(item)"
                        class="w-full h-10 border border-orange-300 dark:border-gray-600 dark:bg-gray-900 dark:text-gray-100 rounded px-3 font-bold text-lg text-center focus:ring-2 focus:ring-orange-500 outline-none"
                        @click.stop
                      />
                    </div>
                    <div class="flex gap-2">
                      <button @click.stop="item.localPickedQty = 0; sound.error()" class="flex-1 h-12 bg-white dark:bg-gray-900 border border-red-200 text-red-500 rounded-lg font-bold text-sm active:bg-red-50 shadow-sm touch-manipulation">
                        SIFIRLA
                      </button>
                      <button @click.stop="item.localPickedQty = item.totalOrderedQty; sound.success()" class="flex-1 h-12 bg-white dark:bg-gray-900 border border-green-200 text-green-600 rounded-lg font-bold text-sm active:bg-green-50 shadow-sm touch-manipulation">
                        HEPSİ
                      </button>
                    </div>
                  </div>

                  <!-- Difference Reason (when qty != ordered) -->
                  <div v-if="item.localPickedQty !== item.totalOrderedQty" class="mt-3">
                    <label class="block text-[10px] font-bold text-orange-500 uppercase tracking-wider mb-1">
                      Fark Nedeni
                      <span v-if="item.localPickedQty < item.totalOrderedQty" class="text-red-500">*</span>
                      <span class="text-gray-400 font-normal normal-case">
                        {{ item.localPickedQty > item.totalOrderedQty ? '(Fazla toplama — opsiyonel)' : '(Miktar sipariş miktarından farklı)' }}
                      </span>
                    </label>
                    <DifferenceReasonInput v-model="item.differenceReason" :default-reason="item.localPickedQty > item.totalOrderedQty ? 'Fazla geldi' : 'Stokta yok'" />
                  </div>

                  <div class="mt-3">
                    <label class="block text-[10px] font-bold text-gray-400 uppercase tracking-wider mb-1">Stok Değişimi (Opsiyonel)</label>
                    <StockCombobox v-model="item.localStockId" :placeholder="'Değişmeyecek (' + item.stockName + ')'" class="text-sm" />
                  </div>

                  <button @click.stop="expandedStock = null" class="mt-3 w-full py-1.5 bg-white dark:bg-gray-900 text-gray-500 font-bold rounded border border-gray-200 dark:border-gray-700 text-sm">
                    Kapat
                  </button>
                </div>

              </div>

              <!-- Completed items (sunk to bottom, muted) -->
              <div
                v-for="item in group.completedItems"
                :key="item.stockCode + '-done'"
                class="opacity-60 transition-all duration-200 bg-green-50 dark:bg-green-900/20"
                :class="expandedStock === item.stockCode ? 'ring-2 ring-inset ring-orange-300 dark:ring-orange-700 !opacity-100' : ''"
              >
                <div @click="toggleExpand(item)" class="p-3 flex justify-between items-start cursor-pointer active:bg-gray-50 dark:active:bg-gray-800 select-none">
                  <div class="flex-1 pr-2 min-w-0">
                    <div class="flex items-center gap-1.5 mb-0.5">
                      <span class="w-4 h-4 rounded-full bg-green-500 flex items-center justify-center flex-shrink-0">
                        <svg class="w-2.5 h-2.5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="3">
                          <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7"/>
                        </svg>
                      </span>
                      <span class="text-[10px] font-bold text-gray-400 dark:text-gray-600 font-mono truncate">{{ item.stockCode }}</span>
                    </div>
                    <div class="text-sm font-semibold text-green-800 dark:text-green-300 leading-tight mb-1">{{ item.stockName }}</div>
                    <div class="text-xs text-gray-500 dark:text-gray-400">
                      {{ item.unit }} · <span class="text-orange-600 dark:text-orange-400 font-bold">{{ item.projectCount }} proje</span>
                      <span v-if="item.localPickedQty > item.totalOrderedQty" class="ml-1 text-orange-600 font-bold">⚠ Sipariş üstü</span>
                    </div>
                  </div>
                  <div class="flex flex-col items-end gap-1 flex-shrink-0">
                    <div class="flex items-baseline gap-1 text-green-600 dark:text-green-400">
                      <span class="text-2xl font-bold tracking-tight">{{ item.localPickedQty }}</span>
                      <span class="text-xs font-medium text-gray-400 dark:text-gray-600">/ {{ item.totalOrderedQty }}</span>
                    </div>
                    <div class="flex items-center gap-1 mt-1" @click.stop>
                      <button @click="changeQty(item, -1)" class="w-8 h-8 flex items-center justify-center rounded-full bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 active:bg-gray-200 dark:active:bg-gray-700 font-bold text-lg touch-manipulation">−</button>
                      <button @click="changeQty(item, 1)" class="w-8 h-8 flex items-center justify-center rounded-full bg-orange-50 dark:bg-orange-900/20 text-orange-600 active:bg-orange-100 font-bold text-lg touch-manipulation border border-orange-200 dark:border-orange-800 shadow-sm">+</button>
                    </div>
                  </div>
                </div>
                <div v-if="expandedStock === item.stockCode" class="bg-orange-50 dark:bg-gray-800 px-3 pb-3 pt-0 border-t border-dashed border-orange-200 dark:border-orange-900 animate-fade-in-down">
                  <div class="pt-3 grid grid-cols-2 gap-3 items-end">
                    <div>
                      <label class="block text-[10px] font-bold text-gray-400 uppercase tracking-wider mb-1">Manuel Miktar</label>
                      <input type="number" v-model.number="item.localPickedQty" @input="clampQty(item)" class="w-full h-10 border border-orange-300 dark:border-gray-600 dark:bg-gray-900 dark:text-gray-100 rounded px-3 font-bold text-lg text-center focus:ring-2 focus:ring-orange-500 outline-none" @click.stop />
                    </div>
                    <div class="flex gap-2">
                      <button @click.stop="item.localPickedQty = 0" class="flex-1 h-10 bg-white dark:bg-gray-900 border border-red-200 text-red-500 rounded font-bold text-xs hover:bg-red-50 shadow-sm">SIFIRLA</button>
                      <button @click.stop="item.localPickedQty = item.totalOrderedQty" class="flex-1 h-10 bg-white dark:bg-gray-900 border border-green-200 text-green-600 rounded font-bold text-xs hover:bg-green-50 shadow-sm">HEPSİ</button>
                    </div>
                  </div>
                  <div v-if="item.localPickedQty !== item.totalOrderedQty" class="mt-3">
                    <label class="block text-[10px] font-bold text-orange-500 uppercase tracking-wider mb-1">Fark Nedeni
                      <span v-if="item.localPickedQty < item.totalOrderedQty" class="text-red-500">*</span>
                    </label>
                    <DifferenceReasonInput v-model="item.differenceReason" :default-reason="item.localPickedQty > item.totalOrderedQty ? 'Fazla geldi' : 'Stokta yok'" />
                  </div>
                  <div class="mt-3">
                    <label class="block text-[10px] font-bold text-gray-400 uppercase tracking-wider mb-1">Stok Değişimi (Opsiyonel)</label>
                    <StockCombobox v-model="item.localStockId" :placeholder="'Değişmeyecek (' + item.stockName + ')'" class="text-sm" />
                  </div>
                  <button @click.stop="expandedStock = null" class="mt-3 w-full py-1.5 bg-white dark:bg-gray-900 text-gray-500 font-bold rounded border border-gray-200 dark:border-gray-700 text-sm">Kapat</button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Sticky Footer -->
      <div class="bg-white dark:bg-gray-900 border-t dark:border-gray-700 p-3 shadow-[0_-4px_6px_-1px_rgba(0,0,0,0.05)] z-20 flex gap-2">
        <button @click="$emit('close')" class="flex-1 py-3 text-gray-500 dark:text-gray-400 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg font-bold text-sm transition-colors">
          KAPAT
        </button>

        <!-- KAYDET: dirty unsaved items exist -->
        <button
          v-if="hasDirtyItems"
          @click="saveAll"
          :disabled="isSaving"
          class="flex-[2] py-3 bg-orange-600 hover:bg-orange-700 text-white rounded-lg font-bold text-sm shadow transition-all flex items-center justify-center gap-2 disabled:opacity-50"
        >
          <span v-if="isSaving" class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></span>
          <span>{{ isSaving ? 'KAYDEDİLİYOR...' : `KAYDET (${dirtyCount} kalem)` }}</span>
        </button>

        <!-- TAMAMLA: nothing dirty (items may be empty — no macro lines is valid) -->
        <button
          v-else-if="!hasDirtyItems"
          @click="markCompleted"
          :disabled="isCompleting"
          class="flex-[2] py-3 bg-green-600 hover:bg-green-700 text-white rounded-lg font-bold text-sm shadow transition-all flex items-center justify-center gap-2 disabled:opacity-50"
        >
          <span v-if="isCompleting" class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></span>
          <span>{{ isCompleting ? 'BİTİRİLİYOR...' : '✔ TOPLAMAYI BİTİR' }}</span>
        </button>
      </div>

    </div>
  </div>

  <!-- Shortage Allocation Modal — queued, one at a time -->
  <MacroAllocationModal
    v-if="shortageQueue.length > 0 && currentShortageItem"
    :zonePreparationId="zonePreparationId"
    :shipmentLineIds="currentShortageItem.lines.map((l: any) => l.id)"
    :lines="currentShortageItem.lines"
    :targetPickedTotal="currentShortageItem.localPickedQty"
    :orderedTotal="currentShortageItem.totalOrderedQty"
    :stockName="currentShortageItem.stockName"
    :queueIndex="shortageQueueIndex"
    :queueTotal="shortageQueue.length"
    @close="cancelShortageQueue"
    @saved="onShortageSaved"
  />

  <!-- ForceComplete Dialog -->
  <div v-if="showForceDialog" class="fixed inset-0 bg-black bg-opacity-60 flex items-end sm:items-center justify-center z-[70]" @click.self="showForceDialog = false">
    <div class="bg-white dark:bg-gray-900 w-full sm:max-w-md rounded-t-2xl sm:rounded-2xl p-5 shadow-2xl animate-slide-up">
      <h4 class="text-base font-bold text-gray-800 dark:text-gray-100 mb-1">Eksik Kalemlerle Tamamla</h4>
      <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">
        <span class="font-semibold text-orange-600">{{ zeroSavedCount }} kalem</span> toplanmamış (miktar = 0).
        Devam etmek için zorunlu neden girin.
      </p>
      <div class="mb-4">
        <label class="block text-xs font-bold text-gray-500 uppercase tracking-wider mb-1">Neden <span class="text-red-500">*</span></label>
        <textarea
          v-model="forceReason"
          rows="3"
          placeholder="Eksik kalemlerin neden toplanamadığını açıklayın..."
          class="w-full border border-gray-300 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-orange-400 focus:border-orange-400 outline-none resize-none"
        ></textarea>
      </div>
      <div class="flex gap-3">
        <button @click="showForceDialog = false" class="flex-1 py-3 bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 rounded-lg font-bold text-sm">
          İPTAL
        </button>
        <button @click="confirmForceComplete"
          :disabled="!forceReason.trim() || isCompleting"
          class="flex-[1.5] py-3 bg-orange-500 text-white rounded-lg font-bold text-sm shadow hover:bg-orange-600 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2">
          <span>YINE DE TAMAMLA</span>
          <span v-if="isCompleting" class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></span>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import warehouseService, { cleanDifferenceReason } from '../services/warehouseService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import { useSoundFeedback } from '../composables/useSoundFeedback';
import StockCombobox from './StockCombobox.vue';
import DifferenceReasonInput from './DifferenceReasonInput.vue';
import MacroAllocationModal from './MacroAllocationModal.vue';

interface MacroItem {
  stockCode:        string;
  stockName:        string;
  unit:             string;
  category:         string;
  projectCount:     number;
  totalOrderedQty:  number;
  lines:            any[];

  // local state (not saved to server yet)
  localPickedQty:   number;
  localStockId:     number | null;
  differenceReason: string;

  // server state (last saved value)
  savedPickedQty:   number;
  isSaved:          boolean;   // true once saved this session
}

const props = defineProps<{ zonePreparationId: number }>();
const emit  = defineEmits(['close', 'completed']);
const sound = useSoundFeedback();

const notify = useNotificationStore();

const items     = ref<MacroItem[]>([]);
const loading   = ref(false);
const isSaving  = ref(false);
const isCompleting = ref(false);

const expandedStock = ref<string | null>(null);

// ForceComplete dialog state
const showForceDialog = ref(false);
const forceReason     = ref('');
const zeroSavedCount  = computed(() => items.value.filter(i => i.savedPickedQty === 0).length);

// Shortage queue
const shortageQueue      = ref<MacroItem[]>([]);
const shortageQueueIndex = ref(0);
const currentShortageItem = computed(() =>
  shortageQueue.value[shortageQueueIndex.value] ?? null
);

// ── Grouped items (pending first, completed at bottom within each category) ─
interface MacroCategoryGroup {
  category: string;
  pendingItems: MacroItem[];
  completedItems: MacroItem[];
}

const categorizedItems = computed((): MacroCategoryGroup[] => {
  const map = new Map<string, MacroItem[]>();
  for (const item of items.value) {
    const cat = (item.category?.trim() || 'DİĞER').toUpperCase();
    if (!map.has(cat)) map.set(cat, []);
    map.get(cat)!.push(item);
  }
  return Array.from(map.entries()).map(([category, catItems]) => {
    const completed = catItems.filter(i => i.localPickedQty >= i.totalOrderedQty && i.localPickedQty > 0);
    const pending = catItems.filter(i => !(i.localPickedQty >= i.totalOrderedQty && i.localPickedQty > 0));
    return { category, pendingItems: pending, completedItems: completed };
  });
});

// ── Per-item derived state ─────────────────────────────────────────────────
function itemState(item: MacroItem) {
  const isFull    = item.localPickedQty >= item.totalOrderedQty;
  const isPartial = item.localPickedQty > 0 && item.localPickedQty < item.totalOrderedQty;
  const isDirty   = item.localPickedQty !== item.savedPickedQty ||
                    (item.localStockId != null && item.localStockId > 0);
  return { isFull, isPartial, isDirty, isSaved: item.isSaved };
}

const hasDirtyItems = computed(() => items.value.some(i => itemState(i).isDirty));
const dirtyCount    = computed(() => items.value.filter(i => itemState(i).isDirty).length);
const savedCount    = computed(() => items.value.filter(i => i.isSaved).length);

// ── Load ───────────────────────────────────────────────────────────────────
async function fetchItems() {
  loading.value = true;
  try {
    const data: any[] = await warehouseService.getMacroPickList({ zonePreparationId: props.zonePreparationId });
    items.value = data.map(d => ({
      stockCode:       d.stockCode,
      stockName:       d.stockName,
      unit:            d.unit,
      category:        d.category || '',
      projectCount:    d.projectCount,
      totalOrderedQty: d.totalOrderedQty,
      lines:           d.lines ?? [],

      localPickedQty:  d.totalPickedQty ?? 0,
      localStockId:    null,
      differenceReason: cleanDifferenceReason(d.differenceReason),

      savedPickedQty:  d.totalPickedQty ?? 0,
      isSaved:         (d.totalPickedQty ?? 0) > 0,
    }));
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Liste yüklenirken hata oluştu.', 'error');
  } finally {
    loading.value = false;
  }
}

// ── Interactions ───────────────────────────────────────────────────────────
function toggleExpand(item: MacroItem) {
  expandedStock.value = expandedStock.value === item.stockCode ? null : item.stockCode;
}

function changeQty(item: MacroItem, delta: number) {
  const next = item.localPickedQty + delta;
  item.localPickedQty = Math.max(0, next);
  if (delta > 0) sound.success();
  else navigator.vibrate?.(15);
}

// Kalemi tek dokunuşla sipariş miktarına eşitle (satırı genişletmeden "tam topla").
function setFull(item: MacroItem) {
  item.localPickedQty = item.totalOrderedQty;
  item.differenceReason = '';
  sound.success();
}

function clampQty(item: MacroItem) {
  if (item.localPickedQty < 0) item.localPickedQty = 0;
  navigator.vibrate?.(15);
}

// ── Save all dirty items ───────────────────────────────────────────────────
async function saveAll() {
  if (isSaving.value) return;

  const dirty = items.value.filter(i => itemState(i).isDirty);

  // Items going through updateAggregatedLines need a reason when qty != ordered
  const directSaveItems = dirty.filter(i =>
    i.localPickedQty >= i.totalOrderedQty ||
    i.localPickedQty === 0 ||
    i.localStockId != null
  );
  const missingReason = directSaveItems.filter(i =>
    i.localPickedQty < i.totalOrderedQty && !i.differenceReason.trim()
  );

  if (missingReason.length > 0) {
    // Expand the first offending item
    expandedStock.value = missingReason[0]?.stockCode ?? null;
    sound.error();
    notify.add(`${missingReason.length} kalem için fark nedeni girilmeli.`, 'error');
    return;
  }

  isSaving.value = true;
  const shortage: MacroItem[] = [];

  try {
    // Process full/over-quantity items first
    const fullItems = dirty.filter(i => i.localPickedQty >= i.totalOrderedQty || i.localStockId != null);
    await Promise.all(fullItems.map(item =>
      warehouseService.updateAggregatedLines({
        ZonePreparationId: props.zonePreparationId,
        ShipmentLineIds:   item.lines.map((l: any) => l.id),
        NewTotalPickedQty: item.localPickedQty,
        NewLocalStockId:   item.localStockId ?? null,
        DifferenceReason:  item.localPickedQty !== item.totalOrderedQty ? item.differenceReason.trim() : undefined,
      }).then(() => {
        item.savedPickedQty = item.localPickedQty;
        item.localStockId   = null;
        item.isSaved        = true;
      })
    ));

    // Collect shortage items (partial qty, no stock change) → goes to MacroAllocationModal
    const shortageItems = dirty.filter(i =>
      i.localPickedQty > 0 &&
      i.localPickedQty < i.totalOrderedQty &&
      !i.localStockId
    );
    shortage.push(...shortageItems);

    // Items with qty=0 that are dirty
    const zeroItems = dirty.filter(i => i.localPickedQty === 0 && !i.localStockId);
    await Promise.all(zeroItems.map(item =>
      warehouseService.updateAggregatedLines({
        ZonePreparationId: props.zonePreparationId,
        ShipmentLineIds:   item.lines.map((l: any) => l.id),
        NewTotalPickedQty: 0,
        NewLocalStockId:   null,
        DifferenceReason:  item.differenceReason.trim() || undefined,
      }).then(() => {
        item.savedPickedQty = 0;
        item.isSaved        = true;
      })
    ));

    if (shortage.length > 0) {
      notify.add(`${fullItems.length + zeroItems.length} kalem kaydedildi. ${shortage.length} eksik kalem için dağıtım yapmanız gerekiyor.`, 'warning');
      shortageQueue.value      = shortage;
      shortageQueueIndex.value = 0;
    } else {
      const total = fullItems.length + zeroItems.length;
      if (total > 0) { sound.success(); notify.add(`${total} kalem kaydedildi.`, 'success'); }
    }
  } catch (e: any) {
    sound.error();
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Kaydetme sırasında hata oluştu.', 'error');
  } finally {
    isSaving.value = false;
  }
}

// ── Shortage queue ─────────────────────────────────────────────────────────
function onShortageSaved() {
  const item = shortageQueue.value[shortageQueueIndex.value];
  if (item) {
    item.savedPickedQty = item.localPickedQty;
    item.isSaved        = true;
  }

  shortageQueueIndex.value++;

  if (shortageQueueIndex.value >= shortageQueue.value.length) {
    // Queue done
    shortageQueue.value      = [];
    shortageQueueIndex.value = 0;
    sound.success();
    notify.add('Tüm eksik dağıtımlar kaydedildi.', 'success');
  }
}

function cancelShortageQueue() {
  shortageQueue.value      = [];
  shortageQueueIndex.value = 0;
}

// ── Complete ───────────────────────────────────────────────────────────────
async function markCompleted() {
  if (isCompleting.value) return;

  if (zeroSavedCount.value > 0) {
    forceReason.value = '';
    showForceDialog.value = true;
    return;
  }

  try {
    await doComplete(false, undefined);
  } catch (e: any) {
    sound.error();
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
    isCompleting.value = false;
  }
}

async function confirmForceComplete() {
  if (!forceReason.value.trim()) return;
  showForceDialog.value = false;
  try {
    await doComplete(true, forceReason.value.trim());
  } catch (e: any) {
    sound.error();
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
    isCompleting.value = false;
  }
}

async function doComplete(forceComplete: boolean, forceReasonText: string | undefined) {
  isCompleting.value = true;
  try {
    const result = await warehouseService.markMacroReady({
      ZonePreparationId: props.zonePreparationId,
      ForceComplete: forceComplete || undefined,
      ForceReason: forceReasonText
    });

    let msg = 'Macro toplama tamamlandı.';
    if (result.unfilledMacroLineCount > 0) {
      msg += ` ${result.unfilledMacroLineCount} kalem toplanmadan tamamlandı.`;
    }
    notify.add(msg, result.unfilledMacroLineCount > 0 ? 'warning' : 'success');
    sound.complete();
    emit('completed');
    emit('close');
  } catch (e: any) {
    sound.error();
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
  } finally {
    isCompleting.value = false;
  }
}

onMounted(() => {
  sound.newAssignment();
  fetchItems();
});
</script>

<style scoped>
.animate-fade-in-down { animation: fadeInDown 0.15s ease-out; }
@keyframes fadeInDown { from { opacity: 0; transform: translateY(-4px); } to { opacity: 1; transform: translateY(0); } }
.animate-slide-up { animation: slideUp 0.25s ease-out; }
@keyframes slideUp { from { opacity: 0; transform: translateY(20px); } to { opacity: 1; transform: translateY(0); } }
</style>
