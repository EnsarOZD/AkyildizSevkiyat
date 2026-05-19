<template>
  <BaseModal :show="isOpen" title="Yeni Mal Kabul İrsaliyesi" maxWidth="lg" :closeOnBackdrop="false" @close="handleClose">
    <!-- Duplicate warning -->
    <div v-if="duplicateWarning" class="mb-4 bg-red-50 dark:bg-red-900/20 p-4 rounded-xl border border-red-200 dark:border-red-800">
      <p class="text-red-800 dark:text-red-300 font-bold text-sm">Mükerrer İrsaliye Uyarısı!</p>
      <p class="text-sm text-red-600 dark:text-red-400 mt-1">Bu tedarikçi ve irsaliye numarası ile daha önce kayıt yapılmış. Yine de devam etmek istiyor musunuz?</p>
      <button
        @click="submitForm(true)"
        :disabled="saving"
        class="mt-3 text-xs bg-red-600 hover:bg-red-700 text-white px-3 py-1.5 rounded-lg font-bold transition-colors disabled:opacity-50"
      >
        <span v-if="saving">İşleniyor...</span>
        <span v-else>Yine de Devam Et</span>
      </button>
    </div>

    <!-- PO info -->
    <div v-if="purchaseOrder" class="mb-4 bg-blue-50 dark:bg-blue-900/20 rounded-xl p-3 border border-blue-200 dark:border-blue-800">
      <p class="text-xs font-bold text-blue-700 dark:text-blue-300 uppercase tracking-wider mb-1">Bağlı Sipariş</p>
      <p class="text-sm font-semibold text-blue-900 dark:text-blue-100">{{ purchaseOrder.orderNumber }} — {{ purchaseOrder.supplierNameSnapshot }}</p>
      <p class="text-xs text-blue-600 dark:text-blue-400 mt-0.5">{{ formatDate(purchaseOrder.orderDate) }}</p>
    </div>
    <div v-else-if="purchaseOrderIds && purchaseOrderIds.length > 0" class="mb-4 bg-indigo-50 dark:bg-indigo-900/20 rounded-xl p-3 border border-indigo-200 dark:border-indigo-800">
      <p class="text-xs font-bold text-indigo-700 dark:text-indigo-300 uppercase tracking-wider mb-1">Bağlı Siparişler</p>
      <p class="text-sm font-semibold text-indigo-900 dark:text-indigo-100">{{ purchaseOrderIds.length }} adet sipariş seçildi</p>
    </div>
    <div v-else class="mb-4 p-3 bg-amber-50 dark:bg-amber-900/10 border border-amber-200 dark:border-amber-800 rounded-xl">
       <p class="text-xs font-bold text-amber-700 dark:text-amber-400 uppercase tracking-wider mb-2">Siparişsiz Mal Kabul</p>
       <label class="block text-[10px] font-bold text-gray-400 dark:text-gray-500 uppercase mb-1">Tedarikçi Seçimi</label>
       <select
         v-model="form.supplierId"
         class="w-full border border-gray-200 dark:border-gray-700 rounded-lg p-2 text-sm dark:bg-gray-800 dark:text-gray-100 outline-none focus:ring-2 focus:ring-amber-500"
       >
         <option value="">Tedarikçi Seçiniz...</option>
         <option v-for="s in suppliers" :key="s.id" :value="s.id">{{ s.name }}</option>
       </select>
    </div>

    <!-- OCR Scan Button -->
    <div class="mb-4">
      <button
        @click="showScanModal = true"
        type="button"
        class="w-full flex items-center justify-center gap-2 px-4 py-3 border-2 border-dashed border-indigo-300 dark:border-indigo-700 rounded-xl text-sm font-semibold text-indigo-600 dark:text-indigo-400 hover:bg-indigo-50 dark:hover:bg-indigo-900/20 transition-colors"
      >
        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" />
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 13a3 3 0 11-6 0 3 3 0 016 0z" />
        </svg>
        📷 İrsaliye Tara (OCR)
      </button>
    </div>

    <div class="space-y-4">
      <div>
        <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">
          İrsaliye No <span class="text-red-500">*</span>
        </label>
        <input
          v-model="form.waybillNo"
          @blur="formatWaybillNo"
          type="text"
          class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
          placeholder="İrsaliye numarası giriniz..."
          :class="errors.waybillNo ? 'border-red-400' : ''"
        />
        <p v-if="errors.waybillNo" class="mt-1 text-xs text-red-500">{{ errors.waybillNo }}</p>
      </div>

      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">
            İrsaliye Tarihi <span class="text-red-500">*</span>
          </label>
          <input
            v-model="form.waybillDate"
            type="date"
            class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
            :class="errors.waybillDate ? 'border-red-400' : ''"
          />
          <p v-if="errors.waybillDate" class="mt-1 text-xs text-red-500">{{ errors.waybillDate }}</p>
        </div>
        <div>
          <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">
            Teslim Tarihi
          </label>
          <input
            v-model="form.receiptDate"
            type="date"
            class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
          />
        </div>
      </div>

      <div>
        <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">
            Not <span v-if="!purchaseOrder && (!purchaseOrderIds || purchaseOrderIds.length === 0)" class="text-red-500">*</span>
        </label>
        <textarea
          v-model="form.note"
          rows="2"
          class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none resize-none"
          :placeholder="(!purchaseOrder && (!purchaseOrderIds || purchaseOrderIds.length === 0)) ? 'Siparişsiz kabul için açıklama zorunludur...' : 'İsteğe bağlı...'"
          :class="errors.note ? 'border-red-400' : ''"
        ></textarea>
        <p v-if="errors.note" class="mt-1 text-xs text-red-500">{{ errors.note }}</p>
      </div>
    </div>

    <template #footer>
      <button
        @click="handleClose"
        :disabled="saving"
        class="px-4 py-2 text-sm font-semibold text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition-colors disabled:opacity-50"
      >
        İptal
      </button>
      <button
        @click="submitForm(false)"
        :disabled="saving"
        class="px-4 py-2 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
      >
        <span v-if="saving">Kaydediliyor...</span>
        <span v-else>Kaydet</span>
      </button>
    </template>
  </BaseModal>

  <!-- OCR Scan Modal -->
  <InvoiceScanModal
    :isOpen="showScanModal"
    @close="showScanModal = false"
    @apply="handleScanResult"
  />
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue';
import BaseModal from './BaseModal.vue';
import InvoiceScanModal from './InvoiceScanModal.vue';
import goodsReceiptService from '../services/goodsReceiptService';
import { supplierService } from '../services/supplierService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';
import { formatDate } from '../utils/dateFormat';
import type { OcrInvoiceLineResult } from '../services/ocrService';

const props = defineProps<{
  isOpen: boolean;
  purchaseOrder?: any;
  purchaseOrderIds?: string[];
  initialSupplierId?: string;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'saved', id: string): void;
}>();

const notificationStore = useNotificationStore();
const today = new Date().toISOString().split('T')[0];

const saving = ref(false);
const duplicateWarning = ref(false);
const showScanModal = ref(false);
const suppliers = ref<any[]>([]);

const form = ref({
  supplierId: '',
  waybillNo: '',
  waybillDate: today,
  receiptDate: today,
  note: ''
});

const errors = ref<{ waybillNo?: string; waybillDate?: string; note?: string; supplierId?: string }>({});

const formatWaybillNo = () => {
  let val = form.value.waybillNo.trim().toUpperCase();
  // Örn: AKI20261 -> AKI202600000001 (15 hane)
  if (val.length > 7 && /^[A-Z]{3}\d{4}/.test(val)) {
    const prefix = val.substring(0, 7);
    const suffix = val.substring(7);
    if (/^\d+$/.test(suffix) && val.length < 16) {
        val = prefix + suffix.padStart(9, '0');
    }
  }
  form.value.waybillNo = val;
};

const fetchSuppliers = async () => {
    try {
        suppliers.value = await supplierService.getAll();
    } catch (e) { console.error('Tedarikçiler yüklenemedi'); }
};

const resetForm = () => {
  form.value = { 
    supplierId: props.initialSupplierId || '',
    waybillNo: '', 
    waybillDate: today, 
    receiptDate: today, 
    note: '' 
  };
  errors.value = {};
  duplicateWarning.value = false;
  showScanModal.value = false;
};

const handleScanResult = (data: { waybillNo: string; waybillDate: string; lines: OcrInvoiceLineResult[] }) => {
  if (data.waybillNo) form.value.waybillNo = data.waybillNo;
  if (data.waybillDate) form.value.waybillDate = data.waybillDate;
  notificationStore.add(
    `OCR sonuçları uygulandı. ${data.waybillNo ? 'İrsaliye No: ' + data.waybillNo : 'İrsaliye No tanınamadı.'} ${data.lines.length > 0 ? data.lines.length + ' malzeme satırı bulundu.' : ''}`,
    'info'
  );
};

watch(() => props.isOpen, (newVal) => {
  if (newVal) {
    resetForm();
    if (suppliers.value.length === 0) fetchSuppliers();
  }
});

const validate = () => {
  errors.value = {};
  if (!form.value.waybillNo.trim()) errors.value.waybillNo = 'İrsaliye numarası zorunludur.';
  if (!form.value.waybillDate) errors.value.waybillDate = 'İrsaliye tarihi zorunludur.';
  
  const hasPO = props.purchaseOrder || (props.purchaseOrderIds && props.purchaseOrderIds.length > 0);
  if (!hasPO) {
      if (!form.value.supplierId) errors.value.supplierId = 'Tedarikçi seçimi zorunludur.';
      if (!form.value.note.trim()) errors.value.note = 'Siparişsiz mal kabul için açıklama zorunludur.';
  }
  
  return Object.keys(errors.value).length === 0;
};

const handleClose = () => {
  if (saving.value) return;
  resetForm();
  emit('close');
};

const submitForm = async (ignoreDuplicate: boolean) => {
  if (!ignoreDuplicate && !validate()) return;
  if (saving.value) return;

  saving.value = true;
  try {
    const payload: any = {
      waybillNo: form.value.waybillNo.trim(),
      waybillDate: form.value.waybillDate,
      receiptDate: form.value.receiptDate || form.value.waybillDate,
      note: form.value.note || undefined,
      ignoreDuplicateWarning: ignoreDuplicate
    };

    if (props.purchaseOrder?.id) {
      payload.purchaseOrderId = props.purchaseOrder.id;
      payload.supplierId = props.purchaseOrder.supplierId;
    } else if (props.purchaseOrderIds && props.purchaseOrderIds.length > 0) {
      payload.purchaseOrderIds = props.purchaseOrderIds;
      // SupplierId will be resolved at backend from the POs
    } else {
      payload.supplierId = form.value.supplierId;
    }

    const res = await goodsReceiptService.create(payload);

    if (res.hasDuplicateWarning && !ignoreDuplicate) {
      duplicateWarning.value = true;
      saving.value = false;
      return;
    }

    notificationStore.add('Mal kabul irsaliyesi oluşturuldu.', 'success');
    emit('saved', res.id);
    resetForm();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kayıt oluşturulamadı.', 'error');
  } finally {
    saving.value = false;
  }
};

onMounted(() => {
    if (props.isOpen) fetchSuppliers();
});
</script>
