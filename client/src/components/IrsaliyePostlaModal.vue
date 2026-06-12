<template>
  <Teleport to="body">
    <Transition name="modal-fade">
      <div v-if="isOpen" class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:p-4">
        <div class="absolute inset-0 bg-black/60 backdrop-blur-sm" @click="!posting && $emit('close')" />

        <div class="relative bg-white dark:bg-gray-900 w-full sm:max-w-lg sm:rounded-3xl rounded-t-3xl max-h-[92vh] flex flex-col shadow-2xl">
          <!-- Handle -->
          <div class="flex justify-center pt-3 pb-1 flex-shrink-0 sm:hidden">
            <div class="w-10 h-1 rounded-full bg-gray-300 dark:bg-gray-600" />
          </div>

          <!-- Header -->
          <div class="px-5 pt-4 pb-4 border-b border-gray-100 dark:border-gray-800 flex-shrink-0">
            <div class="flex items-center justify-between">
              <div>
                <h2 class="text-base font-black text-gray-900 dark:text-gray-100">İrsaliye Bilgileri</h2>
                <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ store.totalEntryCount }} malzeme · {{ store.supplierInfo?.supplierName }}</p>
              </div>
              <button
                v-if="!posting"
                @click="$emit('close')"
                class="p-2 text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 rounded-xl"
              >
                <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>

          <!-- Scrollable body -->
          <div class="overflow-y-auto flex-1">
            <!-- İrsaliye Form -->
            <div class="px-5 pt-5 pb-4 space-y-4">
              <div>
                <label class="block text-[10px] font-black text-gray-500 uppercase tracking-widest mb-1.5">İrsaliye No *</label>
                <input
                  v-model="form.waybillNo"
                  type="text"
                  placeholder="Örn: 2024/001234"
                  class="w-full px-4 py-3 bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl text-sm font-bold text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent uppercase"
                  @input="form.waybillNo = ($event.target as HTMLInputElement).value.toUpperCase()"
                />
              </div>
              <div class="grid grid-cols-2 gap-3">
                <div>
                  <label class="block text-[10px] font-black text-gray-500 uppercase tracking-widest mb-1.5">İrsaliye Tarihi *</label>
                  <input
                    v-model="form.waybillDate"
                    type="date"
                    class="w-full px-3 py-3 bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl text-sm font-bold text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  />
                </div>
                <div>
                  <label class="block text-[10px] font-black text-gray-500 uppercase tracking-widest mb-1.5">Teslim Tarihi</label>
                  <input
                    v-model="form.receiptDate"
                    type="date"
                    class="w-full px-3 py-3 bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl text-sm font-bold text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  />
                </div>
              </div>
            </div>

            <!-- Duplicate Warning -->
            <div v-if="duplicateWarning" class="mx-5 mb-4 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-2xl p-4">
              <div class="flex items-start gap-3">
                <svg class="w-5 h-5 text-amber-500 flex-shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01M12 3C6.477 3 2 7.477 2 12s4.477 9 10 9 10-4.477 10-9S17.523 3 12 3z" />
                </svg>
                <div>
                  <p class="text-sm font-black text-amber-700 dark:text-amber-400">Aynı irsaliye daha önce girilmiş</p>
                  <p class="text-xs text-amber-600 dark:text-amber-500 mt-1">Bu irsaliye numarası ve tarihiyle kayıt mevcut. Yine de devam etmek istiyor musunuz?</p>
                  <button
                    @click="postWithDuplicateOverride"
                    class="mt-2 text-xs font-black text-amber-700 dark:text-amber-400 underline underline-offset-2"
                  >
                    Evet, yine de postla →
                  </button>
                </div>
              </div>
            </div>

            <!-- Malzeme Özeti -->
            <div class="px-5 pb-5">
              <p class="text-[10px] font-black text-gray-400 uppercase tracking-widest mb-3">Kabul Özeti</p>
              <div class="space-y-2">
                <div
                  v-for="entry in store.entries"
                  :key="entry.stockMasterId"
                  class="bg-gray-50 dark:bg-gray-800/50 rounded-2xl px-4 py-3"
                >
                  <div class="flex items-start justify-between">
                    <div class="flex-1 min-w-0">
                      <p class="text-sm font-black text-gray-900 dark:text-gray-100 leading-tight truncate">{{ entry.stockName }}</p>
                      <p class="text-[10px] text-gray-500 dark:text-gray-400 font-bold mt-0.5">{{ entry.stockCode }} · {{ entry.allocations.length }} sipariş</p>
                    </div>
                    <div class="text-right ml-3 flex-shrink-0">
                      <p class="text-base font-black text-gray-900 dark:text-gray-100">
                        {{ entry.allocations.reduce((s, a) => s + (a.receivedQty || 0), 0) }}
                        <span class="text-xs font-bold text-gray-400">{{ entry.unit }}</span>
                      </p>
                      <p v-if="entry.allocations.some(a => (a.rejectedQty || 0) > 0)" class="text-xs font-bold text-red-500">
                        {{ entry.allocations.reduce((s, a) => s + (a.rejectedQty || 0), 0) }} red
                      </p>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Footer -->
          <div class="px-5 py-4 border-t border-gray-100 dark:border-gray-800 flex-shrink-0 bg-white dark:bg-gray-900">
            <div v-if="posting" class="flex items-center justify-center gap-3 py-3">
              <svg class="animate-spin h-5 w-5 text-blue-500" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4" />
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
              </svg>
              <span class="text-sm font-bold text-gray-600 dark:text-gray-400">{{ postingStep }}</span>
            </div>
            <div v-else class="flex gap-3">
              <button
                @click="$emit('close')"
                class="flex-1 py-3.5 rounded-2xl border-2 border-gray-200 dark:border-gray-700 text-sm font-bold text-gray-600 dark:text-gray-400"
              >
                Geri Dön
              </button>
              <button
                @click="handlePost(false)"
                :disabled="!canPost"
                class="flex-[2] py-3.5 rounded-2xl text-sm font-black text-white transition-all"
                :class="canPost
                  ? 'bg-emerald-600 hover:bg-emerald-700 active:scale-[0.98] shadow-lg shadow-emerald-100 dark:shadow-none'
                  : 'bg-gray-300 dark:bg-gray-700 cursor-not-allowed'"
              >
                Postla ve Kaydet
              </button>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import { useRouter } from 'vue-router';
import { useMalKabulStore } from '../stores/malKabul';
import goodsReceiptService from '../services/goodsReceiptService';
import { useNotificationStore } from '../stores/notification';

const props = defineProps<{ isOpen: boolean }>();
const emit = defineEmits<{ close: []; posted: [id: string] }>();

const store = useMalKabulStore();
const notificationStore = useNotificationStore();
const router = useRouter();

const today = new Date().toISOString().split('T')[0] ?? '';
const form = ref<{ waybillNo: string; waybillDate: string; receiptDate: string }>({ waybillNo: '', waybillDate: today, receiptDate: today });
const posting = ref(false);
const postingStep = ref('');
const duplicateWarning = ref(false);

watch(() => props.isOpen, (open) => {
    if (open) {
        duplicateWarning.value = false;
        postingStep.value = '';
    }
});

const canPost = computed(() =>
    !!form.value.waybillNo.trim() && !!form.value.waybillDate && store.totalEntryCount > 0
);

async function handlePost(ignoreDuplicate: boolean) {
    if (!canPost.value) return;
    duplicateWarning.value = false;
    posting.value = true;

    try {
        const poIds = store.involvedPoIds;

        // Step 1: Create receipt
        postingStep.value = 'İrsaliye oluşturuluyor...';
        const createRes = await goodsReceiptService.create({
            purchaseOrderIds: poIds,
            waybillNo: form.value.waybillNo.trim(),
            waybillDate: form.value.waybillDate,
            receiptDate: form.value.receiptDate || form.value.waybillDate,
            ignoreDuplicateWarning: ignoreDuplicate,
        });

        if (createRes.hasDuplicateWarning || !createRes.id) {
            duplicateWarning.value = true;
            posting.value = false;
            return;
        }

        const receiptId = createRes.id as string;

        // Step 2: Get receipt detail to map line IDs
        postingStep.value = 'Kalemler eşleştiriliyor...';
        const detail = await goodsReceiptService.getById(receiptId);

        // Step 3: Build batch update payload
        const lineUpdates: { lineId: string; receivedQty: number; rejectedQty: number; rejectReason?: string }[] = [];
        for (const entry of store.entries) {
            for (const alloc of entry.allocations) {
                if ((alloc.receivedQty || 0) <= 0) continue;
                const matchedLine = detail.lines.find(l => l.purchaseOrderLineId === alloc.purchaseOrderLineId);
                if (matchedLine) {
                    lineUpdates.push({
                        lineId: matchedLine.id,
                        receivedQty: alloc.receivedQty,
                        rejectedQty: alloc.rejectedQty || 0,
                        rejectReason: alloc.rejectReason || undefined,
                    });
                }
            }
        }

        // Step 4: Batch update lines
        if (lineUpdates.length > 0) {
            postingStep.value = 'Miktarlar kaydediliyor...';
            await goodsReceiptService.batchUpdateLines(receiptId, lineUpdates);
        }

        // Step 5: Post
        postingStep.value = 'Stok güncelleniyor...';
        await goodsReceiptService.post(receiptId);

        store.clearSession();
        notificationStore.add('Mal kabul başarıyla postlandı. Stok güncellendi.', 'success');
        emit('posted', receiptId);
        router.push({ name: 'GoodsReceiptDetail', params: { id: receiptId } });
    } catch (e: any) {
        notificationStore.add(e?.message || 'Postlama sırasında hata oluştu.', 'error');
        posting.value = false;
    }
}

const postWithDuplicateOverride = () => handlePost(true);
</script>

<style scoped>
.modal-fade-enter-active,
.modal-fade-leave-active {
    transition: opacity 0.2s ease;
}
.modal-fade-enter-from,
.modal-fade-leave-to {
    opacity: 0;
}
</style>
