<template>
  <div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-2 sm:p-4 z-50">
    <div class="bg-gray-100 dark:bg-gray-800 rounded-lg w-full h-full flex flex-col md:max-w-4xl md:h-auto md:max-h-[90vh] shadow-xl overflow-hidden">

        <!-- Header -->
        <div class="bg-white dark:bg-gray-900 px-4 py-3 flex justify-between items-center border-b dark:border-gray-700 shadow-sm z-10">
            <div class="overflow-hidden">
                <h3 class="text-base md:text-lg font-bold text-gray-800 dark:text-gray-200 truncate">{{ projectName }}</h3>
                <div class="flex items-center gap-2">
                    <span class="text-xs font-bold px-2 py-0.5 bg-blue-100 text-blue-700 rounded">Micro</span>
                    <span class="text-xs text-gray-500 dark:text-gray-400">{{ items.length }} Kalem</span>
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
                <p>Bu proje için Micro ürün bulunamadı.</p>
            </div>

            <div v-else class="space-y-3 pb-20">
                 <div v-for="item in items" :key="item.shipmentLineId"
                      class="bg-white dark:bg-gray-900 rounded-lg shadow-sm border transition-all duration-200"
                      :class="getItemBorderClass(item)"
                 >
                     <!-- Row Header (Always Visible) -->
                     <div @click="toggleExpand(item)" class="p-3 flex justify-between items-start cursor-pointer active:bg-gray-50 dark:active:bg-gray-800">
                         <div class="flex-1 pr-2">
                             <div class="text-[10px] md:text-xs font-bold text-gray-400 dark:text-gray-600 font-mono mb-0.5">
                                 {{ item.stockCode }}
                             </div>
                             <div class="text-sm font-semibold text-gray-800 dark:text-gray-200 leading-tight mb-1">
                                 {{ item.stockName }}
                             </div>
                             <div class="flex items-center gap-2">
                                 <div class="inline-block px-1.5 py-0.5 bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 text-[10px] font-bold rounded uppercase">Birim: {{ item.unit }}</div>
                                 <!-- Reason warning badge -->
                                 <div v-if="needsReason(item) && !item.differenceReason" class="inline-block px-1.5 py-0.5 bg-orange-100 text-orange-700 text-[10px] font-bold rounded">
                                     ⚠ Neden girilmeli
                                 </div>
                             </div>
                         </div>

                         <!-- Quick Actions Area -->
                         <div class="flex flex-col items-end gap-1">
                             <div class="flex items-baseline gap-1" :class="getQtyColorClass(item)">
                                <span class="text-2xl font-bold tracking-tight">{{ item.localPickedQty }}</span>
                                <span class="text-xs font-medium text-gray-400 dark:text-gray-600">/ {{ item.totalQty }}</span>
                             </div>

                             <div class="flex items-center gap-1 mt-1" @click.stop>
                                 <button @click="changeQty(item, -1)"
                                         class="w-8 h-8 flex items-center justify-center rounded-full bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 active:bg-gray-200 dark:active:bg-gray-700 font-bold text-lg touch-manipulation">
                                     -
                                 </button>
                                 <button @click="changeQty(item, 1)"
                                         class="w-8 h-8 flex items-center justify-center rounded-full bg-blue-50 text-blue-600 active:bg-blue-100 font-bold text-lg touch-manipulation border border-blue-100 shadow-sm">
                                     +
                                 </button>
                             </div>
                         </div>
                     </div>

                     <!-- Expanded Content -->
                     <div v-if="item.isExpanded" class="bg-gray-50 dark:bg-gray-800 p-3 pt-0 border-t border-dashed border-gray-200 dark:border-gray-700 animate-fade-in-down">
                        <div class="pt-3 grid grid-cols-2 gap-3 items-end">
                            <div>
                                <label class="block text-[10px] font-bold text-gray-400 uppercase tracking-wider mb-1">Manuel Miktar</label>
                                <input type="number"
                                       v-model.number="item.localPickedQty"
                                       @input="onQtyInput(item)"
                                       class="w-full h-10 border border-gray-300 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded px-3 font-bold text-lg text-center focus:ring-2 focus:ring-blue-500 focus:border-blue-500 outline-none"
                                >
                            </div>
                             <div class="flex gap-2">
                                <button @click="setQty(item, 0)" class="flex-1 h-10 bg-white border border-red-200 text-red-500 rounded font-bold text-xs hover:bg-red-50 shadow-sm">
                                    SIFIRLA
                                </button>
                                <button @click="setQty(item, item.totalQty)" class="flex-1 h-10 bg-white border border-green-200 text-green-600 rounded font-bold text-xs hover:bg-green-50 shadow-sm">
                                    HEPSİ
                                </button>
                            </div>
                        </div>

                        <!-- Difference Reason (shown when qty differs from ordered) -->
                        <div v-if="needsReason(item)" class="mt-3">
                            <label class="block text-[10px] font-bold text-orange-500 uppercase tracking-wider mb-1">
                                Fark Nedeni <span class="text-red-500">*</span>
                                <span class="text-gray-400 font-normal normal-case">(Miktar sipariş miktarından farklı)</span>
                            </label>
                            <input type="text"
                                   v-model="item.differenceReason"
                                   placeholder="Neden farklı? (ör: Stokta yok, Fazla geldi...)"
                                   class="w-full h-10 border rounded px-3 text-sm focus:ring-2 focus:ring-orange-400 focus:border-orange-400 outline-none"
                                   :class="item.differenceReason ? 'border-gray-300 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100' : 'border-orange-300 bg-orange-50 dark:bg-orange-950 dark:border-orange-700'"
                            >
                        </div>

                        <div class="mt-3">
                            <label class="block text-[10px] font-bold text-gray-400 uppercase tracking-wider mb-1">Farklı Ürün (Opsiyonel)</label>
                            <StockCombobox v-model="item.localStockId" :placeholder="'Değişmeyecek (' + item.stockName + ')'" class="text-sm" />
                             <p v-if="item.localStockId && item.localStockId > 0" class="text-xs text-orange-600 mt-1 flex items-center gap-1">
                                ⚠️ Ürün değişecek.
                             </p>
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

             <button v-if="hasDirtyItems" @click="saveChanges(false)"
                :disabled="isSaving || hasUnsatisfiedReasons"
                class="flex-1 py-3 bg-blue-600 text-white rounded-lg font-bold text-sm shadow hover:bg-blue-700 active:translate-y-0.5 transition-all flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed">
                <span>KAYDET</span>
                <span v-if="isSaving" class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></span>
            </button>

            <button v-else @click="saveAndComplete"
                :disabled="isCompleting"
                class="flex-[1.5] py-3 bg-green-600 text-white rounded-lg font-bold text-sm shadow hover:bg-green-700 active:translate-y-0.5 transition-all flex items-center justify-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed">
                <span>✔ TAMAMLA</span>
                <span v-if="isCompleting" class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></span>
            </button>
        </div>

    </div>

    <!-- ForceComplete Dialog (bottom sheet style) -->
    <div v-if="showForceDialog" class="fixed inset-0 bg-black bg-opacity-60 flex items-end sm:items-center justify-center z-[60]" @click.self="showForceDialog = false">
        <div class="bg-white dark:bg-gray-900 w-full sm:max-w-md rounded-t-2xl sm:rounded-2xl p-5 shadow-2xl animate-slide-up">
            <h4 class="text-base font-bold text-gray-800 dark:text-gray-100 mb-1">Eksik Kalemlerle Tamamla</h4>
            <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">
                <span class="font-semibold text-orange-600">{{ zeroQtyCount }} kalem</span> toplanmamış (miktar = 0).
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
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import warehouseService from '../services/warehouseService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import StockCombobox from './StockCombobox.vue';

const props = defineProps<{
    zpProjectId: number;
    projectName: string;
}>();

const emit = defineEmits(['close', 'completed']);
const notificationStore = useNotificationStore();

interface MicroItem {
    shipmentLineId: number;
    stockCode: string;
    stockName: string;
    unit: string;
    totalQty: number;
    originalPickedQty: number;

    // Local State
    localPickedQty: number;
    localStockId: number | null;
    differenceReason: string;
    isExpanded: boolean;
}

const items = ref<MicroItem[]>([]);
const loading = ref(false);
const isSaving = ref(false);
const isCompleting = ref(false);

// ForceComplete dialog state
const showForceDialog = ref(false);
const forceReason = ref('');
const zeroQtyCount = computed(() => items.value.filter(i => i.localPickedQty === 0).length);

const hasDirtyItems = computed(() => {
    return items.value.some(i => i.localPickedQty !== i.originalPickedQty || (i.localStockId && i.localStockId > 0));
});

const needsReason = (item: MicroItem) => {
    return item.localPickedQty !== item.totalQty;
};

// True when a dirty item has a qty mismatch but no reason filled in
const hasUnsatisfiedReasons = computed(() => {
    return items.value.some(i => {
        const isDirty = i.localPickedQty !== i.originalPickedQty || (i.localStockId && i.localStockId > 0);
        return isDirty && needsReason(i) && !i.differenceReason.trim();
    });
});

const getItemBorderClass = (item: MicroItem) => {
    if (item.isExpanded) return 'ring-2 ring-blue-400 ring-offset-1 border-transparent';
    if (needsReason(item) && !item.differenceReason && item.localPickedQty !== item.originalPickedQty) return 'border-orange-300';
    return 'border-gray-100 dark:border-gray-700';
};

const fetchItems = async () => {
    loading.value = true;
    try {
        const data = await warehouseService.getMicroPickList({ zpProjectId: props.zpProjectId });

        items.value = data.map((d: any) => ({
            shipmentLineId: d.shipmentLineId,
            stockCode: d.stockCode,
            stockName: d.stockName,
            unit: d.unit,
            totalQty: d.totalQty,
            originalPickedQty: d.pickedQty || 0,

            localPickedQty: d.pickedQty || 0,
            localStockId: null,
            differenceReason: '',
            isExpanded: false
        }));
    } catch (e) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Liste yüklenirken hata oluştu.', 'error');
    } finally {
        loading.value = false;
    }
};

const toggleExpand = (item: MicroItem) => {
    item.isExpanded = !item.isExpanded;
};

const changeQty = (item: MicroItem, delta: number) => {
    let newVal = item.localPickedQty + delta;
    if (newVal < 0) newVal = 0;
    item.localPickedQty = newVal;
};

const setQty = (item: MicroItem, qty: number) => {
    item.localPickedQty = qty;
};

const onQtyInput = (item: MicroItem) => {
    if (item.localPickedQty < 0) item.localPickedQty = 0;
};

const getQtyColorClass = (item: MicroItem) => {
    if (item.localPickedQty === 0) return 'text-gray-300';
    if (item.localPickedQty === item.totalQty) return 'text-green-600';
    return 'text-blue-600';
};

// Auto-expand items that need a reason but don't have one, then return false if blocked
const validateAndExpandMissingReasons = (): boolean => {
    const blocked = items.value.filter(i => {
        const isDirty = i.localPickedQty !== i.originalPickedQty || (i.localStockId && i.localStockId > 0);
        return isDirty && needsReason(i) && !i.differenceReason.trim();
    });
    if (blocked.length > 0) {
        blocked.forEach(i => { i.isExpanded = true; });
        notificationStore.add(`${blocked.length} kalem için fark nedeni girilmeli.`, 'error');
        return false;
    }
    return true;
};

const saveChanges = async (isCompleteFlow: boolean = false) => {
    if (!validateAndExpandMissingReasons()) return;

    const dirtyItems = items.value.filter(i =>
        i.localPickedQty !== i.originalPickedQty ||
        (i.localStockId && i.localStockId > 0)
    );

    if (dirtyItems.length === 0 && !isCompleteFlow) return;

    isSaving.value = true;
    try {
        if (dirtyItems.length > 0) {
            const payload = {
                zonePreparationProjectId: props.zpProjectId,
                lines: dirtyItems.map(i => ({
                    shipmentLineId: i.shipmentLineId,
                    deliveredQty: i.localPickedQty,
                    newLocalStockId: i.localStockId,
                    differenceReason: needsReason(i) ? i.differenceReason.trim() || undefined : undefined
                }))
            };

            await warehouseService.updateMicroLinesBulk(payload);

            dirtyItems.forEach(i => {
                i.originalPickedQty = i.localPickedQty;
                i.localStockId = null;
            });
            if (!isCompleteFlow) {
                notificationStore.add('Değişiklikler kaydedildi.', 'success');
            }
        }
    } catch (e: any) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kaydetme sırasında hata oluştu.', 'error');
        throw e;
    } finally {
        isSaving.value = false;
    }
};

const saveAndComplete = async () => {
    // If there are zero-qty items, show force dialog instead of browser confirm
    if (zeroQtyCount.value > 0) {
        forceReason.value = '';
        showForceDialog.value = true;
        return;
    }

    await doComplete(false, undefined);
};

const confirmForceComplete = async () => {
    if (!forceReason.value.trim()) return;
    showForceDialog.value = false;
    await doComplete(true, forceReason.value.trim());
};

const doComplete = async (forceComplete: boolean, forceReasonText: string | undefined) => {
    isCompleting.value = true;
    try {
        await saveChanges(true);

        const result = await warehouseService.markMicroReady({
            ZonePreparationProjectId: props.zpProjectId,
            ForceComplete: forceComplete || undefined,
            ForceReason: forceReasonText
        });

        let msg = 'Micro toplama tamamlandı.';
        if (result.unmappedLineCount > 0) {
            msg += ` ⚠ ${result.unmappedLineCount} kalem stok eşlemesi eksik.`;
        }
        if (result.unfilledLineCount > 0) {
            msg += ` ${result.unfilledLineCount} kalem toplanmadan tamamlandı.`;
        }

        notificationStore.add(msg, result.unmappedLineCount > 0 || result.unfilledLineCount > 0 ? 'warning' : 'success');
        emit('completed');
        emit('close');
    } catch (e: any) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Tamamlama sırasında hata oluştu.', 'error');
    } finally {
        isCompleting.value = false;
    }
};

onMounted(fetchItems);
</script>

<style scoped>
.animate-fade-in-down {
    animation: fadeInDown 0.2s ease-out;
}
@keyframes fadeInDown {
    from { opacity: 0; transform: translateY(-5px); }
    to { opacity: 1; transform: translateY(0); }
}
.animate-slide-up {
    animation: slideUp 0.25s ease-out;
}
@keyframes slideUp {
    from { opacity: 0; transform: translateY(20px); }
    to { opacity: 1; transform: translateY(0); }
}
</style>
