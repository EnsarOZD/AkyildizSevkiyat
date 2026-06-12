<template>
  <div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-2 sm:p-4 z-50">
    <div class="bg-gray-100 dark:bg-gray-800 rounded-lg w-full h-full flex flex-col md:max-w-4xl md:h-auto md:max-h-[90vh] shadow-xl overflow-hidden">

      <!-- Header -->
      <div class="bg-white dark:bg-gray-900 px-4 py-3 flex justify-between items-center border-b dark:border-gray-700 shadow-sm z-10">
        <div class="overflow-hidden">
          <h3 class="text-base md:text-lg font-bold text-gray-800 dark:text-gray-200 truncate">{{ projectName }}</h3>
          <div class="flex items-center gap-2">
            <span class="text-xs font-bold px-2 py-0.5 bg-blue-100 text-blue-700 rounded">Şehir Dışı</span>
            <span class="text-xs text-gray-500 dark:text-gray-400 truncate">{{ zoneName }}</span>
            <span class="text-xs text-gray-400 dark:text-gray-600">· {{ items.length }} Kalem</span>
          </div>
        </div>
        <button @click="$emit('close')" class="text-gray-400 hover:text-gray-600 p-2 -mr-2">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <!-- Content -->
      <div class="flex-1 overflow-y-auto p-2 md:p-4 space-y-2 relative">
        <div v-if="loading" class="flex flex-col items-center justify-center h-40 gap-3 text-gray-500 dark:text-gray-400">
          <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
          <span class="text-sm">Yükleniyor...</span>
        </div>

        <div v-else-if="items.length === 0" class="flex flex-col items-center justify-center h-40 bg-white dark:bg-gray-900 rounded border border-dashed border-gray-300 dark:border-gray-700 text-gray-400">
          <span class="text-4xl mb-2">📦</span>
          <p>Bu hazırlık için ürün bulunamadı.</p>
        </div>

        <div v-else class="pb-24">
          <!-- Progress bar -->
          <div class="bg-white dark:bg-gray-900 rounded-lg p-3 border dark:border-gray-700 shadow-sm mb-2">
            <div class="flex justify-between items-center mb-1.5">
              <div class="flex items-center gap-3">
                <span class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wide">İlerleme</span>
                <button @click="setAllFull"
                        class="text-xs font-bold text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40 border border-blue-300 dark:border-blue-700 px-3 py-1.5 rounded-lg transition-colors">
                  Hepsi
                </button>
              </div>
              <span class="text-xs font-bold text-blue-600">{{ completedCount }}/{{ items.length }}</span>
            </div>
            <div class="h-2 bg-gray-100 dark:bg-gray-800 rounded-full overflow-hidden">
              <div class="h-full bg-blue-500 rounded-full transition-all duration-300"
                   :style="{ width: `${progress}%` }"></div>
            </div>
          </div>

          <!-- Items grouped by category → picking type -->
          <div v-for="catGroup in groupedItems" :key="catGroup.catName" class="mb-2">
            <h4 class="px-3 py-1.5 bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-300 font-bold uppercase tracking-wider text-[10px] border-t dark:border-gray-600 border-b sticky top-0 z-10">
              {{ catGroup.catName }}
            </h4>
            <template v-for="subGroup in catGroup.subGroups" :key="subGroup.label ?? '__other__'">
              <div v-if="subGroup.label" class="px-3 py-1 bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400 text-[10px] font-black uppercase tracking-widest border-b border-gray-200 dark:border-gray-700">
                {{ subGroup.label }}
              </div>
            <div class="divide-y divide-gray-100 dark:divide-gray-700">
          <div v-for="item in subGroup.items" :key="groupKey(item)"
               class="shadow-sm border-x dark:border-gray-700 transition-all duration-300"
               :class="getItemBorderClass(item)">

            <!-- Item header -->
            <div class="p-3 flex justify-between items-start gap-2">
              <div class="flex-1 min-w-0">
                <div class="text-[10px] md:text-xs font-bold text-gray-400 font-mono mb-0.5">{{ item.stockCode }}</div>
                <div class="text-sm font-semibold text-gray-800 dark:text-gray-200 leading-tight">{{ item.stockName }}</div>
                <!-- Substitute stock badge -->
                <div v-if="getSubstituteStock(item)" class="mt-1 flex items-center gap-1">
                  <span class="text-[10px] font-bold px-1.5 py-0.5 bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-400 rounded">
                    → {{ getSubstituteStock(item)!.stockCode }} · {{ getSubstituteStock(item)!.stockName }}
                  </span>
                </div>
                <div class="flex items-center gap-2 mt-1 flex-wrap">
                  <span class="text-[10px] px-1.5 py-0.5 bg-gray-100 dark:bg-gray-800 text-gray-500 rounded font-bold uppercase">{{ item.unit }}</span>
                  <!-- Sub-lines (projects) -->
                  <span v-for="sub in item.lines" :key="sub.shipmentLineId"
                        class="text-[10px] text-gray-400 dark:text-gray-600">
                    {{ sub.projectName }}: <span class="font-semibold text-gray-600 dark:text-gray-300">{{ sub.orderedQty }}</span>
                  </span>
                </div>
              </div>

              <!-- Quantity input area -->
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
                    class="w-16 text-center border rounded px-1 py-2.5 text-base font-bold dark:bg-gray-800 dark:border-gray-600 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
                    :class="getInputClass(item)"
                  />
                  <button @click="adjustQty(item, 1)"
                          class="w-11 h-11 rounded-lg bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 text-2xl font-bold flex items-center justify-center active:bg-blue-200 dark:active:bg-blue-900/50 touch-manipulation border border-blue-200 dark:border-blue-800">+</button>
                </div>
                <div class="flex gap-1.5 w-full">
                  <button @click="setFull(item)"
                          class="flex-1 py-2.5 rounded-lg text-xs font-bold bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-700 text-blue-600 dark:text-blue-400 active:bg-blue-100 touch-manipulation">
                    Tam: {{ item.totalOrderedQty }}
                  </button>
                  <button @click="toggleSubstitute(item)"
                          class="flex-1 py-2.5 rounded-lg text-xs font-bold active:bg-gray-200 dark:active:bg-gray-700 touch-manipulation border"
                          :class="getSubstituteStock(item)
                            ? 'bg-orange-50 dark:bg-orange-900/20 border-orange-200 dark:border-orange-700 text-orange-500 dark:text-orange-400'
                            : 'bg-gray-100 dark:bg-gray-800 border-gray-200 dark:border-gray-700 text-gray-500 dark:text-gray-400'">
                    {{ expandedSubstitute.has(groupKey(item)) ? 'Kapat' : 'Değiştir' }}
                  </button>
                </div>
              </div>
            </div>

            <!-- Substitute stock picker (expanded) -->
            <div v-if="expandedSubstitute.has(groupKey(item))" class="px-3 pb-3 border-t border-dashed border-gray-200 dark:border-gray-700 pt-2">
              <label class="block text-[10px] font-bold text-orange-500 dark:text-orange-400 uppercase tracking-wide mb-1">
                Yerine Verilecek Ürün
              </label>
              <StockCombobox
                :model-value="getSubstituteId(item)"
                @update:model-value="setSubstituteId(item, $event)"
                @select="setSubstituteDetail(item, $event ? { id: $event.id || $event.Id, stockCode: $event.stockCode || $event.StockCode, stockName: $event.stockName || $event.StockName } : null)"
                :placeholder="`Değişmeyecek (${item.stockName})`"
                class="text-sm"
              />
              <p v-if="getSubstituteStock(item)" class="text-[10px] text-orange-600 dark:text-orange-400 mt-1">
                ⚠ Gönderilecek ürün değişecek: <strong>{{ getSubstituteStock(item)!.stockName }}</strong>
              </p>
            </div>

            <!-- Difference reason (shown when qty differs from ordered) -->
            <div v-if="needsReason(item)" class="px-3 pb-3" :class="{ 'pt-2 border-t border-dashed border-gray-200 dark:border-gray-700': !expandedSubstitute.has(groupKey(item)) }">
              <label class="block text-[10px] font-bold text-orange-500 dark:text-orange-400 uppercase tracking-wide mb-1">Fark Nedeni</label>
              <DifferenceReasonInput
                :model-value="getReasonForItem(item)"
                @update:model-value="setReasonForItem(item, $event)"
                :default-reason="getPickedQty(item) > item.totalOrderedQty ? 'Fazla geldi' : 'Stokta yok'"
              />
            </div>
          </div>
            </div>
            </template>
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="bg-white dark:bg-gray-900 border-t dark:border-gray-700 p-3 flex gap-2">
        <button @click="$emit('close')"
                :disabled="isSavingProgress"
                class="flex-1 py-3 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg font-bold text-sm hover:bg-gray-50 dark:hover:bg-gray-800 active:scale-95 transition-all disabled:opacity-50">
          İptal
        </button>
        <button
          @click="saveProgress(false)"
          :disabled="isSavingProgress"
          class="flex-1 py-3 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 rounded-lg font-bold text-sm flex items-center justify-center gap-2 hover:bg-gray-50 dark:hover:bg-gray-800 active:scale-95 transition-all disabled:opacity-50"
        >
          <span v-if="isSavingProgress" class="animate-spin h-4 w-4 border-2 border-gray-400 border-t-transparent rounded-full"></span>
          <span>{{ isSavingProgress ? '...' : '💾 Kaydet' }}</span>
        </button>
        <button
          @click="saveProgress(true)"
          :disabled="!canComplete || isSavingProgress"
          class="flex-[2] py-3 rounded-lg font-bold text-sm flex items-center justify-center gap-2 active:scale-95 transition-all disabled:opacity-50"
          :class="allCompleted
            ? 'bg-blue-600 hover:bg-blue-700 text-white shadow-sm'
            : 'bg-amber-500 hover:bg-amber-600 text-white shadow-sm'"
        >
          <span v-if="isSavingProgress" class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></span>
          <span>{{ allCompleted ? '✓ Projeyi Tamamla' : `Eksikle Tamamla (${unfilledCount})` }}</span>
        </button>
      </div>
    </div>
  </div>

</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import warehouseService, { cleanDifferenceReason, type OutOfCityPickItemDto } from '../services/warehouseService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import { useSoundFeedback } from '../composables/useSoundFeedback';
import StockCombobox from './StockCombobox.vue';
import DifferenceReasonInput from './DifferenceReasonInput.vue';

const props = defineProps<{
  zonePreparationId: number;
  zoneName: string;
  projectId: number;
  projectName: string;
}>();

const emit = defineEmits(['close', 'completed']);
const notificationStore = useNotificationStore();
const sound = useSoundFeedback();

const items = ref<OutOfCityPickItemDto[]>([]);
const loading = ref(true);
const isSavingProgress = ref(false);

// State maps keyed by groupKey
const pickedQtyMap = ref<Record<string, number>>({});
const reasonMap = ref<Record<string, string>>({});
// substituteMap: groupKey → { id, stockCode, stockName }
const substituteMap = ref<Record<string, { id: number; stockCode: string; stockName: string } | null>>({});
const expandedSubstitute = ref<Set<string>>(new Set());

document.body.style.overflow = 'hidden';
onUnmounted(() => { document.body.style.overflow = ''; });

const groupKey = (item: OutOfCityPickItemDto) =>
  item.lines.map(l => l.shipmentLineId).sort().join('-');

const getPickedQty = (item: OutOfCityPickItemDto): number => {
  const key = groupKey(item);
  return pickedQtyMap.value[key] ?? item.totalPickedQty;
};

const setPickedQty = (item: OutOfCityPickItemDto, val: number) => {
  pickedQtyMap.value[groupKey(item)] = Math.max(0, val);
};

const adjustQty = (item: OutOfCityPickItemDto, delta: number) => {
  setPickedQty(item, getPickedQty(item) + delta);
  if (delta > 0) sound.success();
  else navigator.vibrate?.(15);
};

const setFull = (item: OutOfCityPickItemDto) => {
  setPickedQty(item, item.totalOrderedQty);
  sound.success();
};

const setAllFull = () => {
  for (const item of items.value) setPickedQty(item, item.totalOrderedQty);
  sound.success();
};

const onQtyChange = (item: OutOfCityPickItemDto, e: Event) => {
  const val = parseFloat((e.target as HTMLInputElement).value);
  setPickedQty(item, isNaN(val) ? 0 : val);
  navigator.vibrate?.(15);
};

// Substitute stock helpers
const getSubstituteId = (item: OutOfCityPickItemDto): number | null =>
  substituteMap.value[groupKey(item)]?.id ?? null;

const getSubstituteStock = (item: OutOfCityPickItemDto) =>
  substituteMap.value[groupKey(item)] ?? null;

const setSubstituteId = (item: OutOfCityPickItemDto, stockId: number | null) => {
  if (!stockId) substituteMap.value[groupKey(item)] = null;
  // Full details come from @select; don't overwrite if already set
};

const setSubstituteDetail = (item: OutOfCityPickItemDto, detail: { id: number; stockCode: string; stockName: string } | null) => {
  substituteMap.value[groupKey(item)] = detail;
};

const toggleSubstitute = (item: OutOfCityPickItemDto) => {
  const key = groupKey(item);
  if (expandedSubstitute.value.has(key)) {
    expandedSubstitute.value.delete(key);
  } else {
    expandedSubstitute.value.add(key);
  }
  // Force reactivity
  expandedSubstitute.value = new Set(expandedSubstitute.value);
};

const needsReason = (item: OutOfCityPickItemDto) => {
  const qty = getPickedQty(item);
  return qty !== item.totalOrderedQty && qty >= 0;
};

const getReasonForItem = (item: OutOfCityPickItemDto) => reasonMap.value[groupKey(item)] ?? '';
const setReasonForItem = (item: OutOfCityPickItemDto, val: string) => {
  reasonMap.value[groupKey(item)] = val;
};

const groupedItems = computed(() => {
  const catMap: Record<string, Record<string, OutOfCityPickItemDto[]>> = {};
  for (const item of items.value) {
    const cat = (item.category?.trim() || 'DİĞER').toUpperCase();
    const pt = item.pickingType === 'Macro' ? 'MAKRO'
             : item.pickingType === 'Micro' ? 'MİKRO'
             : '__OTHER__';
    if (!catMap[cat]) catMap[cat] = {};
    if (!catMap[cat][pt]) catMap[cat][pt] = [];
    catMap[cat][pt].push(item);
  }

  const ptOrder = ['MAKRO', 'MİKRO', '__OTHER__'];
  return Object.entries(catMap).map(([catName, subGroupMap]) => ({
    catName,
    subGroups: ptOrder
      .filter(pt => subGroupMap[pt]?.length)
      .map(pt => ({
        label: pt === '__OTHER__' ? null : pt,
        items: [...(subGroupMap[pt] ?? [])].sort((a, b) => {
          const aComp = getPickedQty(a) >= a.totalOrderedQty;
          const bComp = getPickedQty(b) >= b.totalOrderedQty;
          if (aComp !== bComp) return aComp ? 1 : -1;
          return a.stockName.localeCompare(b.stockName, 'tr');
        })
      }))
  }));
});

const completedCount = computed(() => items.value.filter(i => getPickedQty(i) >= i.totalOrderedQty).length);
const allCompleted = computed(() => completedCount.value === items.value.length);
const progress = computed(() => items.value.length ? Math.round(completedCount.value / items.value.length * 100) : 0);
const unfilledCount = computed(() => items.value.filter(i => getPickedQty(i) === 0).length);

const canComplete = computed(() => {
  if (isSavingProgress.value) return false;
  const missingReasons = items.value.filter(i => needsReason(i) && !getReasonForItem(i));
  return missingReasons.length === 0;
});

onMounted(async () => {
  sound.newAssignment();
  try {
    const data = await warehouseService.getOutOfCityPickList(props.zonePreparationId, props.projectId);
    items.value = data;
    for (const item of data) {
      if (item.totalPickedQty > 0) {
        pickedQtyMap.value[groupKey(item)] = item.totalPickedQty;
      }
      const savedReason = cleanDifferenceReason(item.differenceReason);
      if (savedReason) reasonMap.value[groupKey(item)] = savedReason;
    }
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Ürün listesi yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
});

const buildLineUpdates = () => {
  const result: { shipmentLineId: number; deliveredQty: number; differenceReason?: string | null; newLocalStockId?: number | null }[] = [];

  for (const item of items.value) {
    const totalPicked = getPickedQty(item);
    const totalOrdered = item.totalOrderedQty;
    // Hem "Kaydet" hem "Tamamla" akışında nedeni gönder ki ekran tekrar açıldığında kalsın.
    const reason = getReasonForItem(item) || null;
    const newLocalStockId = getSubstituteId(item) || null;

    if (item.lines.length === 1) {
      result.push({ shipmentLineId: item.lines[0]!.shipmentLineId, deliveredQty: totalPicked, differenceReason: reason, newLocalStockId });
    } else {
      let remaining = totalPicked;
      for (let i = 0; i < item.lines.length; i++) {
        const sub = item.lines[i]!;
        const isLast = i === item.lines.length - 1;
        const portion = isLast
          ? remaining
          : Math.round((sub.orderedQty / totalOrdered) * totalPicked * 100) / 100;
        result.push({ shipmentLineId: sub.shipmentLineId, deliveredQty: portion, differenceReason: reason, newLocalStockId });
        remaining -= portion;
      }
    }
  }
  return result;
};

const saveProgress = async (markComplete = false) => {
  if (isSavingProgress.value) return;
  isSavingProgress.value = true;
  try {
    const lines = buildLineUpdates();
    await warehouseService.saveOutOfCityProgress({ zonePreparationId: props.zonePreparationId, projectId: props.projectId, lines });
    notificationStore.add(markComplete ? `${props.projectName} tamamlandı.` : 'İlerleme kaydedildi.', 'success');
    if (markComplete) { sound.complete(); emit('completed'); } else { sound.success(); }
    emit('close');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kaydetme başarısız.', 'error');
    sound.error();
  } finally {
    isSavingProgress.value = false;
  }
};


const getItemBorderClass = (item: OutOfCityPickItemDto) => {
  const qty = getPickedQty(item);
  if (getSubstituteStock(item)) return 'border-l-4 border-orange-400 dark:border-orange-500 bg-orange-50 dark:bg-orange-900/10';
  if (qty === 0) return 'bg-white dark:bg-gray-900 border dark:border-gray-700';
  if (qty >= item.totalOrderedQty) return 'border-l-4 border-blue-400 dark:border-blue-500 bg-blue-50 dark:bg-blue-900/20';
  return 'border-l-4 border-amber-400 dark:border-amber-500 bg-amber-50 dark:bg-amber-900/10';
};

const getInputClass = (item: OutOfCityPickItemDto) => {
  const qty = getPickedQty(item);
  if (qty === 0) return 'border-gray-300 dark:border-gray-600';
  if (qty > item.totalOrderedQty) return 'border-blue-400 text-blue-700 dark:text-blue-400';
  if (qty >= item.totalOrderedQty) return 'border-blue-400 text-blue-700 dark:text-blue-400';
  return 'border-amber-400 text-amber-700 dark:text-amber-400';
};
</script>
