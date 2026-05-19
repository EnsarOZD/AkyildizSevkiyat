<template>
  <!-- Backdrop -->
  <Teleport to="body">
    <Transition name="sheet">
      <div v-if="isOpen" class="fixed inset-0 z-50 flex flex-col justify-end">
        <div class="absolute inset-0 bg-black/50" @click="$emit('close')" />

        <!-- Sheet Panel -->
        <div class="relative bg-white dark:bg-gray-900 rounded-t-3xl max-h-[85vh] flex flex-col shadow-2xl">
          <!-- Drag Handle -->
          <div class="flex justify-center pt-3 pb-1 flex-shrink-0">
            <div class="w-10 h-1 rounded-full bg-gray-300 dark:bg-gray-600" />
          </div>

          <!-- Header -->
          <div class="px-5 pt-2 pb-4 border-b border-gray-100 dark:border-gray-800 flex-shrink-0">
            <div class="flex items-start justify-between">
              <div>
                <h2 class="text-base font-black text-gray-900 dark:text-gray-100 leading-tight">{{ material?.stockName }}</h2>
                <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5 font-medium">
                  {{ material?.stockCode }} · {{ material?.unit }} · Toplam {{ material?.totalRemainingQty }} kalan
                </p>
              </div>
              <button @click="$emit('close')" class="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 -mr-1 -mt-1 rounded-xl">
                <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>

          <!-- Scrollable PO Allocation List -->
          <div class="overflow-y-auto flex-1 px-5 py-4 space-y-4">
            <div
              v-for="alloc in localAllocations"
              :key="alloc.purchaseOrderLineId"
              class="bg-gray-50 dark:bg-gray-800/60 rounded-2xl p-4 space-y-3"
            >
              <!-- PO Header -->
              <div class="flex items-start justify-between">
                <div>
                  <span class="text-[10px] font-black text-indigo-500 uppercase tracking-widest">{{ alloc.purchaseOrderNumber }}</span>
                  <p class="text-sm font-bold text-gray-800 dark:text-gray-200 leading-tight mt-0.5">{{ alloc.supplierNameSnapshot }}</p>
                </div>
                <div class="text-right">
                  <p class="text-[10px] font-bold text-gray-400 uppercase">Kalan</p>
                  <p class="text-lg font-black text-gray-900 dark:text-gray-100 leading-none">{{ alloc.remainingQty }}</p>
                </div>
              </div>

              <!-- Qty Inputs -->
              <div class="grid grid-cols-2 gap-3">
                <div>
                  <label class="block text-[10px] font-black text-gray-500 uppercase tracking-widest mb-1.5">Teslim Alınan</label>
                  <input
                    v-model.number="alloc.receivedQty"
                    type="number"
                    min="0"
                    step="any"
                    inputmode="decimal"
                    class="w-full px-3 py-2.5 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl text-base font-black text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 focus:border-transparent text-center"
                    placeholder="0"
                    @input="onReceivedChange(alloc)"
                  />
                </div>
                <div>
                  <label class="block text-[10px] font-black text-gray-500 uppercase tracking-widest mb-1.5">Reddedilen</label>
                  <input
                    v-model.number="alloc.rejectedQty"
                    type="number"
                    min="0"
                    step="any"
                    inputmode="decimal"
                    class="w-full px-3 py-2.5 bg-white dark:bg-gray-900 border rounded-xl text-base font-black text-center focus:ring-2 focus:border-transparent"
                    :class="alloc.rejectedQty > 0
                      ? 'border-red-300 dark:border-red-700 text-red-600 dark:text-red-400 focus:ring-red-500'
                      : 'border-gray-200 dark:border-gray-700 text-gray-900 dark:text-gray-100 focus:ring-indigo-500'"
                    placeholder="0"
                    @input="onRejectedChange(alloc)"
                  />
                </div>
              </div>

              <!-- Kabul Edilen (computed display) -->
              <div v-if="(alloc.receivedQty || 0) > 0" class="flex items-center justify-between bg-emerald-50 dark:bg-emerald-900/20 rounded-xl px-3 py-2">
                <span class="text-xs font-bold text-emerald-700 dark:text-emerald-400">Kabul Edilecek</span>
                <span class="text-sm font-black text-emerald-700 dark:text-emerald-300">
                  {{ Math.max(0, (alloc.receivedQty || 0) - (alloc.rejectedQty || 0)) }} {{ material?.unit }}
                </span>
              </div>

              <!-- Reject Reason (only when rejected > 0) -->
              <div v-if="(alloc.rejectedQty || 0) > 0">
                <label class="block text-[10px] font-black text-red-500 uppercase tracking-widest mb-1.5">Red Nedeni *</label>
                <select
                  v-model="alloc.rejectReason"
                  class="w-full px-3 py-2.5 bg-white dark:bg-gray-900 border border-red-300 dark:border-red-700 rounded-xl text-sm font-bold text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-red-500 focus:border-transparent"
                >
                  <option value="">Neden seçin...</option>
                  <option value="Hasarlı">Hasarlı</option>
                  <option value="Eksik/Kırık">Eksik / Kırık</option>
                  <option value="Yanlış Ürün">Yanlış Ürün</option>
                  <option value="Kalite Sorunu">Kalite Sorunu</option>
                  <option value="Diğer">Diğer</option>
                </select>
                <p v-if="(alloc.rejectedQty || 0) > 0 && !alloc.rejectReason" class="text-xs text-red-500 font-bold mt-1">
                  Red nedeni zorunludur
                </p>
              </div>

              <!-- Over-order warning -->
              <p v-if="(alloc.receivedQty || 0) > alloc.remainingQty" class="text-xs font-bold text-amber-600 dark:text-amber-400 flex items-center gap-1">
                <svg class="w-4 h-4 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01M12 3C6.477 3 2 7.477 2 12s4.477 9 10 9 10-4.477 10-9S17.523 3 12 3z" />
                </svg>
                Siparişi aşıyor ({{ alloc.remainingQty }} bekleniyor)
              </p>
            </div>
          </div>

          <!-- Footer Actions -->
          <div class="px-5 py-4 border-t border-gray-100 dark:border-gray-800 flex gap-3 flex-shrink-0 bg-white dark:bg-gray-900">
            <button
              @click="$emit('close')"
              class="flex-1 py-3 rounded-2xl border-2 border-gray-200 dark:border-gray-700 text-sm font-bold text-gray-600 dark:text-gray-400"
            >
              Vazgeç
            </button>
            <button
              @click="handleSave"
              :disabled="!canSave"
              class="flex-[2] py-3 rounded-2xl text-sm font-black text-white transition-all"
              :class="canSave
                ? 'bg-indigo-600 hover:bg-indigo-700 active:scale-[0.98]'
                : 'bg-gray-300 dark:bg-gray-700 cursor-not-allowed'"
            >
              {{ existingEntry ? 'Güncelle' : 'Listeye Ekle' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useMalKabulStore, type POAllocation, type SessionEntry } from '../stores/malKabul';

interface Material {
    stockMasterId: number;
    stockName: string;
    stockCode: string;
    unit: string;
    totalRemainingQty: number;
    allocations: any[];
}

const props = defineProps<{
    isOpen: boolean;
    material: Material | null;
}>();

const emit = defineEmits<{
    close: [];
    saved: [];
}>();

const store = useMalKabulStore();

const localAllocations = ref<POAllocation[]>([]);

const existingEntry = computed(() =>
    props.material ? store.getEntry(props.material.stockMasterId) : undefined
);

// Sync local state when sheet opens
watch(() => props.isOpen, (open) => {
    if (!open || !props.material) return;

    const existing = store.getEntry(props.material.stockMasterId);

    localAllocations.value = props.material.allocations.map((a: any) => {
        const existingAlloc = existing?.allocations.find(
            ea => ea.purchaseOrderLineId === a.purchaseOrderLineId
        );
        return {
            purchaseOrderLineId: a.purchaseOrderLineId,
            purchaseOrderId: a.purchaseOrderId,
            purchaseOrderNumber: a.purchaseOrderNumber,
            supplierNameSnapshot: a.supplierNameSnapshot,
            supplierId: a.supplierId,
            orderedQty: a.orderedQty,
            remainingQty: a.remainingQty,
            receivedQty: existingAlloc?.receivedQty ?? 0,
            rejectedQty: existingAlloc?.rejectedQty ?? 0,
            rejectReason: existingAlloc?.rejectReason ?? '',
        } as POAllocation;
    });
});

const onReceivedChange = (alloc: POAllocation) => {
    if ((alloc.receivedQty || 0) === 0) {
        alloc.rejectedQty = 0;
        alloc.rejectReason = '';
    }
};

const onRejectedChange = (alloc: POAllocation) => {
    if ((alloc.rejectedQty || 0) === 0) {
        alloc.rejectReason = '';
    }
};

const hasAnyQty = computed(() =>
    localAllocations.value.some(a => (a.receivedQty || 0) > 0)
);

const allRejectReasonsProvided = computed(() =>
    localAllocations.value.every(a =>
        (a.rejectedQty || 0) === 0 || !!a.rejectReason
    )
);

const noNegativeValues = computed(() =>
    localAllocations.value.every(a =>
        (a.receivedQty || 0) >= 0 && (a.rejectedQty || 0) >= 0 &&
        (a.rejectedQty || 0) <= (a.receivedQty || 0)
    )
);

const canSave = computed(() =>
    hasAnyQty.value && allRejectReasonsProvided.value && noNegativeValues.value
);

const handleSave = () => {
    if (!canSave.value || !props.material) return;

    const entry: SessionEntry = {
        stockMasterId: props.material.stockMasterId,
        stockName: props.material.stockName,
        stockCode: props.material.stockCode,
        unit: props.material.unit,
        allocations: localAllocations.value.filter(a => (a.receivedQty || 0) > 0),
    };

    store.upsertEntry(entry);
    emit('saved');
    emit('close');
};
</script>

<style scoped>
.sheet-enter-active,
.sheet-leave-active {
    transition: opacity 0.2s ease;
}
.sheet-enter-active .relative,
.sheet-leave-active .relative {
    transition: transform 0.25s cubic-bezier(0.32, 0.72, 0, 1);
}
.sheet-enter-from,
.sheet-leave-to {
    opacity: 0;
}
.sheet-enter-from .relative {
    transform: translateY(100%);
}
.sheet-leave-to .relative {
    transform: translateY(100%);
}
</style>
