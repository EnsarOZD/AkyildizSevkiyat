<template>
  <div v-if="isOpen" class="fixed inset-0 z-[60] overflow-y-auto" aria-labelledby="modal-title" role="dialog" aria-modal="true">
    <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
      <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" aria-hidden="true" @click="close"></div>
      <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>
      <div class="inline-block align-bottom bg-white dark:bg-gray-900 rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full sm:p-6">
        <div>
          <h3 class="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100" id="modal-title">Yeni Tedarikçi Ekle</h3>
          <div class="mt-4 space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Tedarikçi Adı <span class="text-red-500">*</span></label>
              <input v-model="form.name" type="text" class="mt-1 bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm">
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Netsis Cari Kodu <span class="text-red-500">*</span></label>
              <input v-model="form.supplierCode" type="text" class="mt-1 bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm" placeholder="örn: 120.001.001">
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">E-posta (Opsiyonel)</label>
              <input v-model="form.email" type="email" class="mt-1 bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm" placeholder="ornek@tedarikci.com">
            </div>
          </div>
        </div>
        <div class="mt-5 sm:mt-6 sm:grid sm:grid-cols-2 sm:gap-3 sm:grid-flow-row-dense">
          <button @click="save" type="button" class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-indigo-600 text-base font-medium text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:col-start-2 sm:text-sm">
            Kaydet
          </button>
          <button @click="close" type="button" class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 dark:border-gray-700 shadow-sm px-4 py-2 bg-white dark:bg-gray-800 text-base font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:mt-0 sm:col-start-1 sm:text-sm">
            İptal
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import { useNotification } from '../composables/useNotification';
import { supplierService } from '../services/supplierService';
import type { Supplier } from '../services/supplierService';
import { ApiErrorUtils } from '../utils/apiError';

const props = defineProps<{
  isOpen: boolean
}>();
const emit = defineEmits<{
  (e: 'close'): void
  (e: 'saved', supplier: Supplier): void
}>();
const { notify } = useNotification();

const form = reactive({
  name: '',
  supplierCode: '',
  email: ''
});

const close = () => {
  form.name = '';
  form.supplierCode = '';
  form.email = '';
  emit('close');
};

const save = async () => {
  if (!form.name) {
    notify.error('Tedarikçi adı zorunludur.');
    return;
  }
  if (!form.supplierCode) {
    notify.error('Netsis cari kodu zorunludur.');
    return;
  }

  try {
    const newSupplier = await supplierService.create({
      name: form.name,
      supplierCode: form.supplierCode,
      email: form.email || undefined
    });

    notify.success('Tedarikçi başarıyla oluşturuldu.');

    emit('saved', newSupplier);
    close();
  } catch (error) {
    notify.error(ApiErrorUtils.getErrorMessage(error, 'Tedarikçi oluşturulurken hata oluştu.'));
  }
};
</script>
