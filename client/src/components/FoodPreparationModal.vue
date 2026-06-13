<template>
  <div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-2 sm:p-4 z-50">
    <div class="bg-gray-100 dark:bg-gray-800 rounded-lg w-full h-full flex flex-col md:max-w-4xl md:h-auto md:max-h-[90vh] shadow-xl overflow-hidden">

      <!-- Header -->
      <div class="bg-white dark:bg-gray-900 px-4 py-3 flex justify-between items-center border-b dark:border-gray-700 shadow-sm z-10">
        <div class="overflow-hidden">
          <h3 class="text-base md:text-lg font-bold text-gray-800 dark:text-gray-200">
            🥕 Gıda Hazırlık — {{ zoneName }}
          </h3>
          <div class="flex items-center gap-2 flex-wrap">
            <span class="text-xs font-bold px-2 py-0.5 bg-green-100 text-green-700 rounded">Gıda Macro</span>
            <span class="text-xs text-gray-500 dark:text-gray-400">{{ deliveryDateFormatted }}</span>
            <span v-if="batchCount > 1" class="text-xs font-bold text-violet-600 dark:text-violet-400">{{ batchCount }} batch birleştirildi</span>
            <span class="text-xs text-gray-400 dark:text-gray-600">· {{ items.length }} Kalem</span>
            <span v-if="totalWeightKg != null" class="text-xs font-bold text-green-700 dark:text-green-400 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 px-1.5 py-0.5 rounded">
              🌿 {{ totalPickedWeightKg?.toFixed(1) }} / {{ totalWeightKg.toFixed(1) }} kg
            </span>
          </div>
        </div>
        <button @click="$emit('close')" class="text-gray-400 hover:text-gray-600 p-2 -mr-2 flex-shrink-0">
          <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <!-- Content -->
      <div class="flex-1 overflow-y-auto p-2 md:p-4 space-y-2 relative">
        <div v-if="loading" class="flex flex-col items-center justify-center h-40 gap-3 text-gray-500 dark:text-gray-400">
          <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-green-600"></div>
          <span class="text-sm">Yükleniyor...</span>
        </div>

        <div v-else-if="items.length === 0" class="flex flex-col items-center justify-center h-40 bg-white dark:bg-gray-900 rounded border border-dashed border-gray-300 dark:border-gray-700 text-gray-400">
          <span class="text-4xl mb-2">🥦</span>
          <p>Gıda ürünü bulunamadı.</p>
          <p class="text-sm mt-1 text-gray-400">Tüm toplamalar zaten tamamlanmış olabilir.</p>
        </div>

        <div v-else class="pb-24">
          <!-- Progress bar -->
          <div class="bg-white dark:bg-gray-900 rounded-lg p-3 border dark:border-gray-700 shadow-sm mb-2">
            <div class="flex justify-between items-center mb-1.5">
              <div class="flex items-center gap-3">
                <span class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wide">İlerleme</span>
                <button @click="setAllFull"
                        class="text-xs font-bold text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-900/20 hover:bg-green-100 dark:hover:bg-green-900/40 border border-green-300 dark:border-green-700 px-3 py-1.5 rounded-lg transition-colors">
                  Hepsini Ver
                </button>
              </div>
              <span class="text-xs font-bold text-green-600">{{ completedCount }}/{{ items.length }}</span>
            </div>
            <div class="h-2 bg-gray-100 dark:bg-gray-800 rounded-full overflow-hidden">
              <div class="h-full bg-green-500 rounded-full transition-all duration-300"
                   :style="{ width: `${progress}%` }"></div>
            </div>
          </div>

          <!-- Item list -->
          <div class="divide-y divide-gray-100 dark:divide-gray-700">
            <div v-for="item in sortedItems" :key="groupKey(item)"
                 class="shadow-sm border-x dark:border-gray-700 transition-all duration-300"
                 :class="getItemBorderClass(item)">

              <!-- Item header -->
              <div class="p-3 flex justify-between items-start gap-2">
                <div class="flex-1 min-w-0">
                  <div class="text-[10px] md:text-xs font-bold text-gray-400 font-mono mb-0.5">{{ item.stockCode }}</div>
                  <div class="text-sm font-semibold text-gray-800 dark:text-gray-200 leading-snug">{{ item.stockName }}</div>

                  <!-- Substitute badge -->
                  <div v-if="getSubstituteStock(item)" class="mt-1 flex items-center gap-1">
                    <span class="text-[10px] font-bold px-1.5 py-0.5 bg-violet-100 dark:bg-violet-900/30 text-violet-700 dark:text-violet-400 rounded">
                      → {{ getSubstituteStock(item)!.stockCode }} · {{ getSubstituteStock(item)!.stockName }}
                    </span>
                  </div>

                  <div class="flex items-center gap-2 mt-1 flex-wrap">
                    <span class="text-[10px] px-1.5 py-0.5 bg-gray-100 dark:bg-gray-800 text-gray-500 rounded font-bold uppercase">{{ item.unit }}</span>
                    <span v-if="item.totalWeightKg != null" class="text-[10px] text-green-600 dark:text-green-400 font-semibold">
                      🌿 {{ getPickedWeightKg(item)?.toFixed(1) }} / {{ item.totalWeightKg.toFixed(1) }} kg
                    </span>
                    <!-- Sub-lines: batch/project breakdown -->
                    <template v-if="item.lines.length > 1">
                      <span v-for="sub in item.lines" :key="sub.shipmentLineId"
                            class="text-[10px] text-gray-400 dark:text-gray-600">
                        {{ sub.projectName }}<span v-if="sub.batchNo > 1" class="text-violet-400"> (B{{ sub.batchNo }})</span>:
                        <span class="font-semibold text-gray-600 dark:text-gray-300">{{ sub.orderedQty }}</span>
                      </span>
                    </template>
                    <template v-else-if="item.lines.length === 1">
                      <span class="text-[10px] text-gray-400 dark:text-gray-600 break-words">
                        {{ item.lines[0]!.projectName }}
                      </span>
                    </template>
                  </div>
                </div>

                <!-- Quantity controls -->
                <div class="flex flex-col items-end gap-1 shrink-0">
                  <div class="text-right">
                    <span class="block text-[10px] text-gray-400 font-bold uppercase leading-none">Sipariş</span>
                    <span class="block text-xl font-black text-blue-600 dark:text-blue-400 leading-tight">{{ item.totalOrderedQty }}</span>
                  </div>
                  <div class="flex items-center gap-1.5">
                    <button @click="adjustQty(item, -1)"
                            class="w-11 h-11 rounded-lg bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 text-2xl font-bold flex items-center justify-center active:bg-gray-200 dark:active:bg-gray-700 touch-manipulation">−</button>
                    <input
                      type="number"
                      min="0"
                      :value="getPickedQty(item)"
                      @change="onQtyChange(item, $event)"
                      @focus="($event.target as HTMLInputElement).select()"
                      class="w-16 text-center border rounded px-1 py-2.5 text-base font-bold dark:bg-gray-800 dark:border-gray-600 dark:text-gray-100 focus:ring-2 focus:ring-green-500 focus:border-transparent outline-none"
                      :class="getInputClass(item)"
                    />
                    <button @click="adjustQty(item, 1)"
                            class="w-11 h-11 rounded-lg bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 text-2xl font-bold flex items-center justify-center active:bg-green-200 dark:active:bg-green-900/50 touch-manipulation border border-green-200 dark:border-green-800">+</button>
                  </div>
                  <div class="flex gap-1.5 w-full">
                    <button @click="setFull(item)"
                            class="flex-1 py-2.5 rounded-lg text-xs font-bold bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-700 text-green-600 dark:text-green-400 active:bg-green-100 touch-manipulation">
                      Tam: {{ item.totalOrderedQty }}
                    </button>
                    <button @click="toggleSubstitute(item)"
                            class="flex-1 py-2.5 rounded-lg text-xs font-bold active:bg-gray-200 dark:active:bg-gray-700 touch-manipulation border"
                            :class="getSubstituteStock(item)
                              ? 'bg-violet-50 dark:bg-violet-900/20 border-violet-200 dark:border-violet-700 text-violet-500 dark:text-violet-400'
                              : 'bg-gray-100 dark:bg-gray-800 border-gray-200 dark:border-gray-700 text-gray-500 dark:text-gray-400'">
                      {{ expandedSubstitute.has(groupKey(item)) ? 'Kapat' : 'Değiştir' }}
                    </button>
                  </div>
                </div>
              </div>

              <!-- Substitute stock picker -->
              <div v-if="expandedSubstitute.has(groupKey(item))" class="px-3 pb-3 border-t border-dashed border-gray-200 dark:border-gray-700 pt-2">
                <label class="block text-[10px] font-bold text-violet-500 dark:text-violet-400 uppercase tracking-wide mb-1">
                  Yerine Verilecek Ürün
                </label>
                <StockCombobox
                  :model-value="getSubstituteId(item)"
                  @update:model-value="setSubstituteId(item, $event)"
                  @select="setSubstituteDetail(item, $event ? { id: $event.id || $event.Id, stockCode: $event.stockCode || $event.StockCode, stockName: $event.stockName || $event.StockName } : null)"
                  :placeholder="`Değişmeyecek (${item.stockName})`"
                  class="text-sm"
                />
                <p v-if="getSubstituteStock(item)" class="text-[10px] text-violet-600 dark:text-violet-400 mt-1">
                  ⚠ Gönderilecek ürün değişecek: <strong>{{ getSubstituteStock(item)!.stockName }}</strong>
                </p>
              </div>

              <!-- Difference reason -->
              <div v-if="needsReason(item)" class="px-3 pb-3"
                   :class="{ 'pt-2 border-t border-dashed border-gray-200 dark:border-gray-700': !expandedSubstitute.has(groupKey(item)) }">
                <label class="block text-[10px] font-bold text-violet-500 dark:text-violet-400 uppercase tracking-wide mb-1">Fark Nedeni</label>
                <DifferenceReasonInput
                  :model-value="getReasonForItem(item)"
                  @update:model-value="setReasonForItem(item, $event)"
                  :default-reason="getPickedQty(item) > item.totalOrderedQty ? 'Fazla geldi' : 'Stokta yok'"
                />
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="bg-white dark:bg-gray-900 border-t dark:border-gray-700 p-3 flex gap-2">
        <button @click="$emit('close')"
                :disabled="isBusy"
                class="flex-1 py-3 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg font-bold text-sm hover:bg-gray-50 dark:hover:bg-gray-800 active:scale-95 transition-all disabled:opacity-50">
          İptal
        </button>
        <button
          @click="saveProgress"
          :disabled="isBusy"
          class="flex-1 py-3 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg font-bold text-sm flex items-center justify-center gap-2 hover:bg-gray-50 dark:hover:bg-gray-800 active:scale-95 transition-all disabled:opacity-50"
        >
          <span v-if="saving" class="animate-spin h-4 w-4 border-2 border-gray-400 border-t-transparent rounded-full"></span>
          <span>{{ saving ? '...' : '💾 Kaydet' }}</span>
        </button>
        <button
          @click="markReady"
          :disabled="!canComplete || isBusy"
          class="flex-[2] py-3 rounded-lg font-bold text-sm flex items-center justify-center gap-2 active:scale-95 transition-all disabled:opacity-50"
          :class="allCompleted
            ? 'bg-green-600 hover:bg-green-700 text-white shadow-sm'
            : 'bg-amber-500 hover:bg-amber-600 text-white shadow-sm'"
        >
          <span v-if="completing" class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></span>
          <span>{{ completing ? 'İşleniyor...' : allCompleted ? '✓ Gıda Hazırlığını Tamamla' : `Eksikle Tamamla (${unfilledCount} satır)` }}</span>
        </button>
      </div>
    </div>

    <!-- Gıda eksik dağıtım modalı -->
    <MacroAllocationModal
      v-if="currentShortageItem"
      :lines="currentShortageItem.lines.map(sub => ({ id: sub.shipmentLineId, projectId: sub.projectId, projectName: sub.projectName, orderedQty: sub.orderedQty }))"
      :targetPickedTotal="getPickedQty(currentShortageItem)"
      :orderedTotal="currentShortageItem.totalOrderedQty"
      :stockName="currentShortageItem.stockName"
      :queueIndex="shortageQueueIndex"
      :queueTotal="shortageQueue.length"
      :onSave="foodAllocationSave"
      @saved="onFoodShortageSaved"
      @close="() => { shortageQueue = []; shortageQueueIndex = 0; pendingMarkReady = false; loadItems(); }"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import warehouseService, { cleanDifferenceReason, type FoodPickItemDto } from '../services/warehouseService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';
import { useSoundFeedback } from '../composables/useSoundFeedback';
import StockCombobox from './StockCombobox.vue';
import DifferenceReasonInput from './DifferenceReasonInput.vue';
import MacroAllocationModal from './MacroAllocationModal.vue';

const props = defineProps<{
  zoneId: number;
  deliveryDate: string;
  zoneName: string;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'completed'): void;
}>();

const notificationStore = useNotificationStore();
const sound = useSoundFeedback();
const loading = ref(false);
const saving = ref(false);
const completing = ref(false);
const items = ref<FoodPickItemDto[]>([]);

// State maps keyed by groupKey
const pickedQtyMap = ref<Record<string, number>>({});
const reasonMap = ref<Record<string, string>>({});
const substituteMap = ref<Record<string, { id: number; stockCode: string; stockName: string } | null>>({});
const expandedSubstitute = ref<Set<string>>(new Set());

// Eksik dağıtım kuyruğu (birden fazla projeye sahip shortage items)
const shortageQueue = ref<FoodPickItemDto[]>([]);
const shortageQueueIndex = ref(0);
const currentShortageItem = computed(() => shortageQueue.value[shortageQueueIndex.value] ?? null);
const pendingMarkReady = ref(false);
// Bu session'da allocateFoodShortage çağrılmış itemların groupKey'leri — tekrar modal açmamak için
const allocatedKeys = ref<Record<string, true>>({});

const onFoodShortageSaved = async () => {
  const savedItem = shortageQueue.value[shortageQueueIndex.value];
  if (savedItem) allocatedKeys.value[groupKey(savedItem)] = true;
  shortageQueueIndex.value++;
  if (shortageQueueIndex.value >= shortageQueue.value.length) {
    shortageQueue.value = [];
    shortageQueueIndex.value = 0;
    sound.success();
    notificationStore.add('Tüm eksik dağıtımlar kaydedildi.', 'success');
    await loadItems();
    if (pendingMarkReady.value) {
      pendingMarkReady.value = false;
      await doMarkReady();
    }
  }
};

const foodAllocationSave = async (
  allocations: { shipmentLineId: number; deliveredQty: number }[],
  reason: string
) => {
  await warehouseService.allocateFoodShortage({ allocations, differenceReason: reason });
};

document.body.style.overflow = 'hidden';
onUnmounted(() => { document.body.style.overflow = ''; });

// Group key: sorted line IDs joined
const groupKey = (item: FoodPickItemDto) =>
  item.lines.map(l => l.shipmentLineId).sort((a, b) => a - b).join('-');

// ── Quantity helpers ──────────────────────────────────────────────────────────
const getPickedQty = (item: FoodPickItemDto): number =>
  pickedQtyMap.value[groupKey(item)] ?? item.totalPickedQty;

const setPickedQty = (item: FoodPickItemDto, val: number) => {
  pickedQtyMap.value[groupKey(item)] = Math.max(0, val);
};

const adjustQty = (item: FoodPickItemDto, delta: number) =>
  setPickedQty(item, getPickedQty(item) + delta);

const setFull = (item: FoodPickItemDto) =>
  setPickedQty(item, item.totalOrderedQty);

const setAllFull = () => {
  for (const item of items.value) setPickedQty(item, item.totalOrderedQty);
};

const onQtyChange = (item: FoodPickItemDto, e: Event) => {
  const val = parseFloat((e.target as HTMLInputElement).value);
  setPickedQty(item, isNaN(val) ? 0 : val);
};

// ── Substitute helpers ────────────────────────────────────────────────────────
const getSubstituteId = (item: FoodPickItemDto): number | null =>
  substituteMap.value[groupKey(item)]?.id ?? null;

const getSubstituteStock = (item: FoodPickItemDto) =>
  substituteMap.value[groupKey(item)] ?? null;

const setSubstituteId = (item: FoodPickItemDto, stockId: number | null) => {
  if (!stockId) substituteMap.value[groupKey(item)] = null;
};

const setSubstituteDetail = (item: FoodPickItemDto, detail: { id: number; stockCode: string; stockName: string } | null) => {
  substituteMap.value[groupKey(item)] = detail;
};

const toggleSubstitute = (item: FoodPickItemDto) => {
  const key = groupKey(item);
  const s = new Set(expandedSubstitute.value);
  if (s.has(key)) s.delete(key); else s.add(key);
  expandedSubstitute.value = s;
};

// ── Reason helpers ────────────────────────────────────────────────────────────
const needsReason = (item: FoodPickItemDto) => {
  const qty = getPickedQty(item);
  return qty !== item.totalOrderedQty && qty >= 0;
};

const getReasonForItem = (item: FoodPickItemDto) => reasonMap.value[groupKey(item)] ?? '';
const setReasonForItem = (item: FoodPickItemDto, val: string) => {
  reasonMap.value[groupKey(item)] = val;
};

// ── Computed ──────────────────────────────────────────────────────────────────
const deliveryDateFormatted = computed(() => {
  const d = new Date(props.deliveryDate + 'T12:00:00');
  return d.toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' });
});

const batchCount = computed(() => {
  const ids = new Set(items.value.flatMap(i => i.lines.map(l => l.zonePreparationId)));
  return ids.size;
});

const getPickedWeightKg = (item: FoodPickItemDto): number | null => {
  if (item.totalWeightKg == null || item.totalOrderedQty === 0) return null;
  const unitWeight = item.totalWeightKg / item.totalOrderedQty;
  return getPickedQty(item) * unitWeight;
};

const totalWeightKg = computed(() => {
  const hasAny = items.value.some(i => i.totalWeightKg != null);
  if (!hasAny) return null;
  return items.value.reduce((sum, i) => sum + (i.totalWeightKg ?? 0), 0);
});

const totalPickedWeightKg = computed(() => {
  const hasAny = items.value.some(i => i.totalWeightKg != null);
  if (!hasAny) return null;
  return items.value.reduce((sum, i) => sum + (getPickedWeightKg(i) ?? 0), 0);
});

const completedCount = computed(() =>
  items.value.filter(i => getPickedQty(i) >= i.totalOrderedQty && getPickedQty(i) > 0).length
);
const allCompleted = computed(() => completedCount.value === items.value.length && items.value.length > 0);
const progress = computed(() => items.value.length ? Math.round(completedCount.value / items.value.length * 100) : 0);
// Backend ile aynı seviyede: shipment satırı bazında DeliveredQty=0 olan satır sayısı.
const unfilledCount = computed(() =>
  items.value.reduce((sum, i) => sum + i.lines.filter(l => l.pickedQty === 0).length, 0)
);
const isBusy = computed(() => saving.value || completing.value);

const canComplete = computed(() => {
  if (isBusy.value) return false;
  return items.value.every(i => !needsReason(i) || !!getReasonForItem(i));
});

// Sort by pickingOrder then stockName; items stay in place as they're picked
const sortedItems = computed(() =>
  [...items.value].sort((a, b) => {
    if (a.pickingOrder !== b.pickingOrder) return a.pickingOrder - b.pickingOrder;
    return a.stockName.localeCompare(b.stockName, 'tr');
  })
);

// ── Styling helpers ───────────────────────────────────────────────────────────
const getItemBorderClass = (item: FoodPickItemDto) => {
  if (getSubstituteStock(item)) return 'border-l-4 border-violet-400 dark:border-violet-500 bg-violet-50 dark:bg-violet-900/10';
  const qty = getPickedQty(item);
  if (qty === 0) return 'bg-white dark:bg-gray-900 border dark:border-gray-700';
  if (qty >= item.totalOrderedQty) return 'border-l-4 border-green-400 dark:border-green-500 bg-green-50 dark:bg-green-900/20';
  return 'border-l-4 border-amber-400 dark:border-amber-500 bg-amber-50 dark:bg-amber-900/10';
};

const getInputClass = (item: FoodPickItemDto) => {
  const qty = getPickedQty(item);
  if (qty === 0) return 'border-gray-300 dark:border-gray-600';
  if (qty > item.totalOrderedQty) return 'border-blue-400 text-blue-700 dark:text-blue-400';
  if (qty >= item.totalOrderedQty) return 'border-green-400 text-green-700 dark:text-green-400';
  return 'border-amber-400 text-amber-700 dark:text-amber-400';
};

// ── Data loading ──────────────────────────────────────────────────────────────
const loadItems = async () => {
  loading.value = true;
  try {
    const data = await warehouseService.getFoodPickList({
      zoneId: props.zoneId,
      deliveryDate: props.deliveryDate,
    });
    items.value = data;
    for (const item of data) {
      if (item.totalPickedQty > 0) {
        pickedQtyMap.value[groupKey(item)] = item.totalPickedQty;
      }
      const savedReason = cleanDifferenceReason(item.differenceReason);
      if (savedReason) reasonMap.value[groupKey(item)] = savedReason;
    }
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Gıda listesi yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
};


// ── Actions ───────────────────────────────────────────────────────────────────
const isShortageItem = (item: FoodPickItemDto) => {
  const qty = getPickedQty(item);
  return qty > 0 && qty < item.totalOrderedQty && item.lines.length > 1 && !getSubstituteStock(item) && !allocatedKeys.value[groupKey(item)];
};

const saveProgress = async () => {
  const shortage = items.value.filter(isShortageItem);

  const updates = items.value
    .filter(item => {
      const key = groupKey(item);
      return pickedQtyMap.value[key] !== undefined && !isShortageItem(item);
    })
    .map(item => ({
      shipmentLineIds: item.lines.map(l => l.shipmentLineId),
      newTotalPickedQty: getPickedQty(item),
      differenceReason: needsReason(item) ? (getReasonForItem(item) || null) : null,
      newLocalStockId: getSubstituteId(item) || null,
    }));

  if (!updates.length && !shortage.length) {
    notificationStore.add('Değişiklik yok.', 'info');
    return;
  }

  saving.value = true;
  try {
    if (updates.length) {
      await warehouseService.updateFoodPickLines({ updates });
      if (!shortage.length) {
        await loadItems();
        notificationStore.add('Gıda miktarları kaydedildi.', 'success');
        sound.success();
      }
    }
    if (shortage.length) {
      shortageQueue.value = shortage;
      shortageQueueIndex.value = 0;
    }
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kaydetme başarısız.', 'error');
    sound.error();
  } finally {
    saving.value = false;
  }
};

const doMarkReady = async () => {
  // Backend tek tek shipment satırı seviyesinde DeliveredQty=0 kontrolü yapıyor;
  // grup toplamı dolu olsa bile multi-line bir grupta tek satır 0 kalmışsa
  // ForceComplete gönderilmezse "henüz toplanmadı" hatası atıyor.
  const unfilledLineCount = items.value.reduce(
    (sum, item) => sum + item.lines.filter(l => l.pickedQty === 0).length,
    0
  );
  const unfilledItemNames = items.value
    .filter(i => i.lines.some(l => l.pickedQty === 0))
    .map(i => i.stockName);

  if (unfilledLineCount > 0) {
    const ok = confirm(
      `${unfilledLineCount} gıda satırı henüz toplanmadı:\n${unfilledItemNames.slice(0, 5).map(n => '• ' + n).join('\n')}${unfilledItemNames.length > 5 ? '\n…' : ''}\n\nYine de tamamlamak istiyor musunuz?`
    );
    if (!ok) return;
  }

  completing.value = true;
  try {
    const result = await warehouseService.markFoodPreparationReady({
      zoneId: props.zoneId,
      deliveryDate: props.deliveryDate,
      forceComplete: unfilledLineCount > 0,
      forceReason: unfilledLineCount > 0 ? 'Depo tarafından onaylandı.' : undefined,
    });
    notificationStore.add(
      `Gıda hazırlığı tamamlandı. ${result.advancedBatchCount} batch irsaliye aşamasına geçirildi.`,
      'success'
    );
    sound.complete();
    emit('completed');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
    sound.error();
  } finally {
    completing.value = false;
  }
};

const markReady = async () => {
  const shortage = items.value.filter(isShortageItem);

  const updates = items.value
    .filter(item => {
      const key = groupKey(item);
      return pickedQtyMap.value[key] !== undefined && !isShortageItem(item);
    })
    .map(item => ({
      shipmentLineIds: item.lines.map(l => l.shipmentLineId),
      newTotalPickedQty: getPickedQty(item),
      differenceReason: needsReason(item) ? (getReasonForItem(item) || null) : null,
      newLocalStockId: getSubstituteId(item) || null,
    }));

  if (updates.length) {
    saving.value = true;
    try {
      await warehouseService.updateFoodPickLines({ updates });
    } catch (e) {
      notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kaydetme başarısız.', 'error');
      sound.error();
      saving.value = false;
      return;
    }
    saving.value = false;
  }

  if (shortage.length) {
    pendingMarkReady.value = true;
    shortageQueue.value = shortage;
    shortageQueueIndex.value = 0;
    return;
  }

  await loadItems();
  await doMarkReady();
};

onMounted(() => {
    sound.newAssignment();
    loadItems();
});
</script>
