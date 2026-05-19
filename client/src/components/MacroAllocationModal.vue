<template>
  <div class="fixed inset-0 bg-black bg-opacity-70 flex items-center justify-center p-4 z-[60]">
    <div class="bg-white dark:bg-gray-900 rounded-lg p-2 md:p-6 w-full md:max-w-2xl h-full md:h-auto md:max-h-[90vh] flex flex-col shadow-2xl">

        <div class="flex justify-between items-center mb-4 border-b dark:border-gray-700 pb-2">
            <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 flex-wrap">
                    <h3 class="text-lg font-bold text-red-600">⚠️ Eksik Dağıtım</h3>
                    <span v-if="(queueTotal ?? 0) > 1" class="text-xs font-bold px-2 py-0.5 bg-orange-100 text-orange-700 rounded-full">
                        {{ (queueIndex ?? 0) + 1 }} / {{ queueTotal }}
                    </span>
                </div>
                <p v-if="stockName" class="text-sm font-semibold text-gray-800 dark:text-gray-200 truncate mt-0.5">{{ stockName }}</p>
                <div class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                    Toplanan: <span class="font-bold text-black dark:text-gray-100">{{ targetPickedTotal }}</span>
                    / Sipariş: {{ orderedTotal }}
                </div>
            </div>
            <button @click="$emit('close')" class="text-gray-400 hover:text-gray-600 text-3xl px-2 flex-shrink-0">&times;</button>
        </div>

        <div class="bg-yellow-50 p-2 mb-2 rounded border border-yellow-200 text-xs text-yellow-800">
             Toplanan miktar siparişten az. Hangi projelere eksik gönderileceğini seçmelisiniz.
             Dağıtılan Toplam: <b>{{ currentAllocated }} / {{ targetPickedTotal }}</b>
        </div>

        <div class="mb-3">
            <label class="block text-[10px] font-bold text-orange-500 uppercase tracking-wider mb-1">
                Fark Nedeni <span class="text-red-500">*</span>
            </label>
            <input type="text"
                   v-model="differenceReason"
                   placeholder="Neden eksik? (ör: Stokta yok, Tedarik bekleniyor...)"
                   class="w-full h-9 border rounded px-3 text-sm focus:ring-2 focus:ring-orange-400 focus:border-orange-400 outline-none"
                   :class="differenceReason ? 'border-gray-300 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100' : 'border-orange-300 bg-orange-50 dark:bg-orange-950 dark:border-orange-700'"
            >
        </div>

        <div class="flex-1 overflow-y-auto mb-4 bg-gray-50 dark:bg-gray-800 rounded p-2 space-y-2">
            <div v-if="localLines.length === 0" class="text-center py-10">Veri bulunamadı.</div>
                    <div v-for="line in localLines" :key="line.id" class="bg-white dark:bg-gray-900 p-3 rounded shadow-sm border dark:border-gray-700 flex flex-col gap-2">
                         <div class="flex justify-between items-start">
                             <div class="font-bold text-gray-800 dark:text-gray-200 text-sm flex-1">
                                 {{ line.projectName }}
                                 <div class="text-[10px] text-gray-400 dark:text-gray-600 font-mono">Proje ID: #{{ line.projectId }}</div>
                             </div>
                     <div class="text-xs text-gray-500 dark:text-gray-400">
                         Sipariş: <b>{{ line.orderedQty }}</b>
                     </div>
                 </div>

                 <div class="flex items-center gap-2">
                     <span class="text-xs font-bold text-gray-500 dark:text-gray-400 w-20">Verilen:</span>
                     <input type="number"
                            v-model.number="line.allocatedQty"
                            class="flex-1 border dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded p-2 text-center font-bold text-lg focus:ring-2 focus:ring-blue-500 outline-none"
                            :class="{'bg-red-50 text-red-600': (line.allocatedQty || 0) < line.orderedQty, 'bg-green-50 text-green-600': (line.allocatedQty || 0) == line.orderedQty}"
                     />
                 </div>
            </div>
        </div>

        <div class="border-t dark:border-gray-700 pt-4 flex gap-2">
             <button @click="$emit('close')" class="flex-1 py-3 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 font-bold rounded">İptal</button>
             <button
                @click="saveAllocation"
                :disabled="currentAllocated !== targetPickedTotal || !differenceReason.trim()"
                class="flex-[2] py-3 text-white font-bold rounded shadow transition-all flex flex-col items-center justify-center leading-tight disabled:opacity-50 disabled:cursor-not-allowed"
                :class="currentAllocated === targetPickedTotal && differenceReason.trim() ? 'bg-green-600 hover:bg-green-700' : 'bg-gray-400'"
             >
                <span>KAYDET</span>
                <span v-if="currentAllocated !== targetPickedTotal" class="text-[10px] opacity-80">
                    ({{ currentAllocated }} != {{ targetPickedTotal }})
                </span>
             </button>
        </div>

    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import warehouseService from '../services/warehouseService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';

const props = defineProps<{
    zonePreparationId?: number;
    lines: { id: number; projectId: number; projectName: string; orderedQty: number; allocatedQty?: number }[];
    targetPickedTotal: number;
    orderedTotal: number;
    stockName?: string;
    queueIndex?: number;
    queueTotal?: number;
    onSave?: (allocations: { shipmentLineId: number; deliveredQty: number }[], reason: string) => Promise<void>;
}>();

const emit = defineEmits(['close', 'saved']);
const notificationStore = useNotificationStore();

const differenceReason = ref('');

// Clone props to local ref to allow editing
const localLines = ref<any[]>([]);

onMounted(() => {
    // Distribute equally or sequentially by default?
    // Or just init with current values if they exist?
    // Actually we are here because user picked LESS than total.
    // We should probably init all to 0 or distribute partially?
    // Let's init with 0 for user to decide found.
    // OR try to auto-distribute sequentially like backend does?
    // Let's auto-distribute to be helpful.

    let remaining = props.targetPickedTotal;
    localLines.value = props.lines.map(l => {
        let alloc = 0;
        if (remaining >= l.orderedQty) {
            alloc = l.orderedQty;
            remaining -= l.orderedQty;
        } else {
            alloc = remaining;
            remaining = 0;
        }
        return {
            ...l,
            allocatedQty: alloc
        };
    });
});

const currentAllocated = computed(() => {
    return localLines.value.reduce((sum, l) => sum + (l.allocatedQty || 0), 0);
});

const saveAllocation = async () => {
    if (currentAllocated.value !== props.targetPickedTotal) return;

    const allocations = localLines.value.map(l => ({
        shipmentLineId: l.id,
        deliveredQty: l.allocatedQty
    }));
    const reason = differenceReason.value.trim();

    try {
        if (props.onSave) {
            await props.onSave(allocations, reason);
        } else {
            await warehouseService.allocateMacroShortage({
                zonePreparationId: props.zonePreparationId!,
                allocations,
                differenceReason: reason
            });
        }
        notificationStore.add('Dağıtım kaydedildi.', 'success');
        emit('saved');
        emit('close');
    } catch (e: any) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Dağıtım kaydedilemedi.', 'error');
    }
};
</script>
