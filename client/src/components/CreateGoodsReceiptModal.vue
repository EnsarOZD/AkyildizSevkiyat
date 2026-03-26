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

    <div class="space-y-4">
      <div>
        <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">
          İrsaliye No <span class="text-red-500">*</span>
        </label>
        <input
          v-model="form.waybillNo"
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
        <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">Not</label>
        <textarea
          v-model="form.note"
          rows="2"
          class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none resize-none"
          placeholder="İsteğe bağlı..."
        ></textarea>
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
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import BaseModal from './BaseModal.vue';
import goodsReceiptService from '../services/goodsReceiptService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const props = defineProps<{
  isOpen: boolean;
  purchaseOrder?: any;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'saved', id: string): void;
}>();

const notificationStore = useNotificationStore();
const today = new Date().toISOString().split('T')[0];

const saving = ref(false);
const duplicateWarning = ref(false);

const form = ref({
  waybillNo: '',
  waybillDate: today,
  receiptDate: today,
  note: ''
});

const errors = ref<{ waybillNo?: string; waybillDate?: string }>({});

const formatDate = (date: string) => {
  if (!date) return '-';
  return new Date(date).toLocaleDateString('tr-TR');
};

const resetForm = () => {
  form.value = { waybillNo: '', waybillDate: today, receiptDate: today, note: '' };
  errors.value = {};
  duplicateWarning.value = false;
};

watch(() => props.isOpen, (val) => {
  if (val) resetForm();
});

const validate = () => {
  errors.value = {};
  if (!form.value.waybillNo.trim()) errors.value.waybillNo = 'İrsaliye numarası zorunludur.';
  if (!form.value.waybillDate) errors.value.waybillDate = 'İrsaliye tarihi zorunludur.';
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
</script>
