<template>
  <BaseModal :show="isOpen" title="Satır Miktarı Gir" maxWidth="md" @close="handleClose">
    <div class="space-y-4">
      <!-- Product info (readonly) -->
      <div class="bg-gray-50 dark:bg-gray-800 rounded-xl p-3 border border-gray-200 dark:border-gray-700">
        <p class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">Ürün</p>
        <p class="text-sm font-semibold text-gray-900 dark:text-gray-100">{{ line?.stockNameSnapshot || line?.stockName }}</p>
        <p class="text-xs text-gray-500 dark:text-gray-400 font-mono">{{ line?.unitSnapshot || line?.unit }}</p>
      </div>

      <!-- Readonly quantities -->
      <div class="grid grid-cols-2 gap-3">
        <div class="bg-gray-50 dark:bg-gray-800 rounded-lg p-3 border border-gray-200 dark:border-gray-700">
          <p class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase mb-1">Sipariş Edilen</p>
          <p class="text-lg font-bold text-gray-900 dark:text-gray-100">{{ line?.orderedQty ?? '-' }}</p>
        </div>
        <div v-if="previouslyReceived != null && previouslyReceived > 0" class="bg-blue-50 dark:bg-blue-900/20 rounded-lg p-3 border border-blue-200 dark:border-blue-800">
          <p class="text-xs font-bold text-blue-600 dark:text-blue-400 uppercase mb-1">Daha Önce Alınan</p>
          <p class="text-lg font-bold text-blue-700 dark:text-blue-300">{{ previouslyReceived }}</p>
        </div>
      </div>

      <!-- Received qty -->
      <div>
        <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">
          Teslim Alınan Miktar <span class="text-red-500">*</span>
        </label>
        <input
          v-model.number="form.receivedQty"
          type="number"
          step="0.01"
          min="0"
          class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
          :class="errors.receivedQty ? 'border-red-400' : ''"
          placeholder="0.00"
        />
        <p v-if="errors.receivedQty" class="mt-1 text-xs text-red-500">{{ errors.receivedQty }}</p>
      </div>

      <!-- Rejected qty -->
      <div>
        <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">Reddedilen Miktar</label>
        <input
          v-model.number="form.rejectedQty"
          type="number"
          step="0.01"
          min="0"
          class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
          placeholder="0.00"
        />
      </div>

      <!-- Reject reason (required if rejectedQty > 0) -->
      <div v-if="form.rejectedQty > 0">
        <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">
          Red Nedeni <span class="text-red-500">*</span>
        </label>
        <select
          v-model="form.rejectReason"
          class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
          :class="errors.rejectReason ? 'border-red-400' : ''"
        >
          <option value="">Neden seçiniz...</option>
          <option v-for="reason in rejectReasons" :key="reason" :value="reason">{{ reason }}</option>
        </select>
        <p v-if="errors.rejectReason" class="mt-1 text-xs text-red-500">{{ errors.rejectReason }}</p>
      </div>

      <!-- AcceptedQty display (calculated, readonly) -->
      <div class="bg-green-50 dark:bg-green-900/20 rounded-xl p-3 border border-green-200 dark:border-green-800">
        <p class="text-xs font-bold text-green-600 dark:text-green-400 uppercase tracking-wider mb-1">Kabul Edilen (Hesaplanan)</p>
        <p class="text-xl font-bold" :class="acceptedQty < 0 ? 'text-red-600' : 'text-green-700 dark:text-green-300'">
          {{ acceptedQty }}
        </p>
        <p v-if="acceptedQty < 0" class="text-xs text-red-500 mt-1">Red miktarı teslim alınan miktarı aşamaz!</p>
      </div>

      <!-- Note -->
      <div>
        <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">Not</label>
        <input
          v-model="form.note"
          type="text"
          class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
          placeholder="İsteğe bağlı..."
        />
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
        @click="handleSave"
        :disabled="saving || acceptedQty < 0"
        class="px-4 py-2 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
      >
        <span v-if="saving">Kaydediliyor...</span>
        <span v-else>Kaydet</span>
      </button>
    </template>
  </BaseModal>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import { useDefinedReasons, ReasonCategory } from '../composables/useDefinedReasons';
import BaseModal from './BaseModal.vue';
import goodsReceiptService from '../services/goodsReceiptService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const props = defineProps<{
  isOpen: boolean;
  goodsReceiptId: string;
  line: any;
  previouslyReceived?: number;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'saved'): void;
}>();

const notificationStore = useNotificationStore();
const saving = ref(false);

// Red sebepleri DB'den (Sebep Tanımları). "Diğer" her zaman en sonda sabit kalır.
const { reasons: managedRejectReasons, load: loadRejectReasons } = useDefinedReasons(ReasonCategory.GoodsReceiptReject);
const rejectReasons = computed(() => [...managedRejectReasons.value, 'Diğer']);
onMounted(loadRejectReasons);

const form = ref({
  receivedQty: 0,
  rejectedQty: 0,
  rejectReason: '',
  note: ''
});

const errors = ref<{ receivedQty?: string; rejectReason?: string }>({});

const acceptedQty = computed(() => (form.value.receivedQty || 0) - (form.value.rejectedQty || 0));

watch(() => props.isOpen, (val) => {
  if (val && props.line) {
    form.value = {
      receivedQty: props.line.receivedQty ?? 0,
      rejectedQty: props.line.rejectedQty ?? 0,
      rejectReason: props.line.rejectReason ?? '',
      note: props.line.note ?? ''
    };
    errors.value = {};
  }
});

const validate = () => {
  errors.value = {};
  if (form.value.receivedQty === null || form.value.receivedQty === undefined || form.value.receivedQty < 0) {
    errors.value.receivedQty = 'Teslim alınan miktar 0 veya daha büyük olmalıdır.';
  }
  if (form.value.rejectedQty > 0 && !form.value.rejectReason) {
    errors.value.rejectReason = 'Red nedeni belirtilmelidir.';
  }
  return Object.keys(errors.value).length === 0;
};

const handleClose = () => {
  if (saving.value) return;
  emit('close');
};

const handleSave = async () => {
  if (!validate()) return;
  if (acceptedQty.value < 0) return;
  if (saving.value) return;

  saving.value = true;
  try {
    await goodsReceiptService.updateLine(props.goodsReceiptId, props.line.id, {
      receivedQty: form.value.receivedQty,
      rejectedQty: form.value.rejectedQty,
      rejectReason: form.value.rejectReason || undefined,
      note: form.value.note || undefined
    });
    notificationStore.add('Satır güncellendi.', 'success');
    emit('saved');
    emit('close');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Güncelleme başarısız.', 'error');
  } finally {
    saving.value = false;
  }
};
</script>
