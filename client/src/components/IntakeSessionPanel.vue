<template>
  <div class="bg-indigo-50 dark:bg-indigo-900/10 rounded-2xl border-2 border-indigo-100 dark:border-indigo-800 shadow-sm overflow-hidden flex flex-col h-full sticky top-6">
    <!-- Header -->
    <div class="px-5 py-4 border-b border-indigo-100 dark:border-indigo-800 bg-white dark:bg-gray-900 flex items-center justify-between">
      <div class="flex items-center gap-2.5">
        <div class="p-2 bg-indigo-100 dark:bg-indigo-900/40 rounded-lg text-indigo-600 dark:text-indigo-400">
          <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
          </svg>
        </div>
        <div>
          <h2 class="text-sm font-bold text-gray-900 dark:text-gray-100 uppercase tracking-tight">Yeni İrsaliye Kaydı</h2>
          <p class="text-[10px] text-gray-500 dark:text-gray-400 font-medium uppercase tracking-widest leading-none mt-0.5">Oturum Hazırlanıyor</p>
        </div>
      </div>
      <button @click="$emit('reset')" class="p-1 px-2 text-[10px] font-bold text-gray-400 hover:text-red-500 uppercase transition-colors">
        Sıfırla
      </button>
    </div>

    <!-- Empty State -->
    <div v-if="!hasInitialData" class="flex-1 flex flex-col items-center justify-center p-8 text-center bg-gray-50/50 dark:bg-transparent">
       <div class="h-20 w-20 bg-indigo-100 dark:bg-indigo-900/20 rounded-full flex items-center justify-center mb-4 animate-pulse">
          <svg class="h-10 w-10 text-indigo-300 dark:text-indigo-700" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 9v3m0 0v3m0-3h3m-3 0H9m12 0a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
       </div>
       <p class="text-sm font-semibold text-gray-900 dark:text-gray-100">Sipariş Seçilmedi</p>
       <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Önce listeden bir sipariş seçin veya tarayın.</p>
    </div>

    <!-- Active Form -->
    <div v-else class="flex-1 overflow-y-auto p-5 scrollbar-thin scrollbar-thumb-indigo-200">
      <!-- Selected POs display -->
      <div v-if="purchaseOrderIds && purchaseOrderIds.length > 0" class="mb-6 space-y-2">
         <label class="block text-[10px] font-extrabold text-indigo-500 uppercase tracking-widest mb-1">Seçili Siparişler ({{ purchaseOrderIds.length }})</label>
         <div class="flex flex-wrap gap-2">
            <div v-for="id in purchaseOrderIds" :key="id" class="inline-flex items-center gap-1.5 bg-white dark:bg-gray-800 border-2 border-indigo-100 dark:border-indigo-800 rounded-lg px-2.5 py-1 text-xs font-bold text-indigo-700 dark:text-indigo-300">
               <span># {{ id.slice(0, 5) }}</span>
            </div>
         </div>
      </div>

      <!-- Main Form -->
      <div class="space-y-4">
        <!-- Supplier (Visible if no items selected yet) -->
        <div v-if="!purchaseOrderIds || purchaseOrderIds.length === 0">
          <label class="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase mb-1.5">Tedarikçi <span class="text-red-500">*</span></label>
          <select
            v-model="internalForm.supplierId"
            class="w-full border-gray-200 dark:border-gray-700 rounded-xl px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500"
          >
            <option value="">Tedarikçi Seçiniz...</option>
            <option v-for="s in suppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
          </select>
        </div>

        <div>
          <label class="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase mb-1.5">İrsaliye No <span class="text-red-500">*</span></label>
          <div class="relative">
             <input
               v-model="internalForm.waybillNo"
               type="text"
               placeholder="Örn: ABC202600001"
               class="w-full border-gray-200 dark:border-gray-700 rounded-xl px-3 py-2.5 text-sm font-bold uppercase placeholder:normal-case dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 pr-10"
               ref="waybillInput"
             />
             <button @click="$emit('scan')" type="button" class="absolute inset-y-0 right-0 px-3 text-indigo-600 hover:text-indigo-800 transition-colors" title="OCR Tara">
                <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                   <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" />
                </svg>
             </button>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase mb-1.5">İrsaliye Tarihi</label>
            <input
              v-model="internalForm.waybillDate"
              type="date"
              class="w-full border-gray-200 dark:border-gray-700 rounded-xl px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100"
            />
          </div>
          <div>
             <label class="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase mb-1.5">Teslim Tarihi</label>
             <input
               v-model="internalForm.receiptDate"
               type="date"
               class="w-full border-gray-200 dark:border-gray-700 rounded-xl px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100"
             />
          </div>
        </div>

        <div>
           <label class="block text-xs font-bold text-gray-500 dark:text-gray-400 uppercase mb-1.5">
              Not / Açıklama
              <span v-if="isNoPo" class="text-red-500 underline text-[10px] ml-1">Siparişsiz kabullerde zorunludur!</span>
           </label>
           <textarea
             v-model="internalForm.note"
             rows="3"
             placeholder="İlgili bir notunuz var mı?"
             class="w-full border-gray-200 dark:border-gray-700 rounded-xl px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 resize-none"
           ></textarea>
        </div>
      </div>
    </div>

    <!-- Footer Action -->
    <div v-if="hasInitialData" class="p-4 bg-white dark:bg-gray-800 border-t border-indigo-100 dark:border-indigo-800">
       <button
         @click="handleSave"
         :disabled="saving"
         class="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-bold py-3.5 rounded-xl shadow-lg shadow-indigo-200 dark:shadow-none transition-all flex items-center justify-center gap-2 group active:scale-[0.98]"
       >
         <span v-if="saving" class="h-5 w-5 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
         <span v-else class="flex items-center gap-2">
            Mal Kabulü Başlat
            <svg class="h-5 w-5 group-hover:translate-x-1 transition-transform" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7l5 5m0 0l-5 5m5-5H6" />
            </svg>
         </span>
       </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';

const props = defineProps<{
  purchaseOrderIds?: string[];
  initialSupplierId?: string;
  suppliers: any[];
  saving: boolean;
  forcedShow?: boolean;
}>();

const emit = defineEmits<{
  (e: 'submit', form: any): void;
  (e: 'reset'): void;
  (e: 'scan'): void;
}>();

const today = new Date().toISOString().split('T')[0];

const internalForm = ref({
  supplierId: '',
  waybillNo: '',
  waybillDate: today,
  receiptDate: today,
  note: ''
});

const isNoPo = computed(() => !props.purchaseOrderIds || props.purchaseOrderIds.length === 0);
const hasInitialData = computed(() => props.forcedShow || (props.purchaseOrderIds && props.purchaseOrderIds.length > 0) || props.initialSupplierId);

watch(() => props.initialSupplierId, (val) => {
  if (val) internalForm.value.supplierId = val;
}, { immediate: true });

const handleSave = () => {
  emit('submit', internalForm.value);
};

const waybillInput = ref<HTMLInputElement | null>(null);
const focusWaybill = () => waybillInput.value?.focus();

defineExpose({ focusWaybill, internalForm });
</script>
