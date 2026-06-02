<template>
  <div v-if="isOpen" class="fixed inset-0 z-[60] overflow-y-auto" role="dialog" aria-modal="true">
    <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
      <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" aria-hidden="true" @click="close"></div>
      <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>
      <div class="inline-block align-bottom bg-white dark:bg-gray-900 rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-2xl sm:w-full sm:p-6">
        <h3 class="text-lg font-medium text-gray-900 dark:text-gray-100">
          {{ mode === 'edit' ? 'Müşteri Düzenle' : 'Yeni Müşteri Ekle' }}
        </h3>

        <div class="mt-4 grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div class="sm:col-span-2">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Müşteri Adı <span class="text-red-500">*</span></label>
            <input v-model="form.name" type="text" class="mt-1 input" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Netsis Cari Kodu (Fatura) <span class="text-red-500">*</span></label>
            <input v-model="form.netsisCariKodu" type="text" class="mt-1 input" placeholder="örn: 120.01.001" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Netsis Teslim Cari Kodu (Opsiyonel)</label>
            <input v-model="form.netsisTeslimCariKodu" type="text" class="mt-1 input" placeholder="boş bırakılırsa fatura cari kullanılır" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Operasyon Tipi</label>
            <select v-model.number="form.operationType" class="mt-1 input">
              <option :value="0">Catering</option>
              <option :value="1">Kıyafet</option>
            </select>
          </div>

          <div class="sm:col-span-2">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Adres</label>
            <textarea v-model="form.address" rows="2" class="mt-1 input"></textarea>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">İl</label>
            <input v-model="form.cityName" type="text" class="mt-1 input" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">İlçe</label>
            <input v-model="form.districtName" type="text" class="mt-1 input" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Enlem (Latitude)</label>
            <input v-model.number="form.latitude" type="number" step="any" class="mt-1 input" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Boylam (Longitude)</label>
            <input v-model.number="form.longitude" type="number" step="any" class="mt-1 input" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Teslim Alacak Kişi</label>
            <input v-model="form.defaultContactName" type="text" class="mt-1 input" />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Telefon</label>
            <input v-model="form.defaultContactPhone" type="text" class="mt-1 input" />
          </div>
        </div>

        <div class="mt-6 sm:grid sm:grid-cols-2 sm:gap-3">
          <button @click="save" :disabled="saving" type="button" class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-indigo-600 text-base font-medium text-white hover:bg-indigo-700 disabled:opacity-50 sm:col-start-2 sm:text-sm">
            {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
          </button>
          <button @click="close" :disabled="saving" type="button" class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 dark:border-gray-700 shadow-sm px-4 py-2 bg-white dark:bg-gray-800 text-base font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50 sm:mt-0 sm:col-start-1 sm:text-sm">
            İptal
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, watch, ref } from 'vue';
import { useNotification } from '../composables/useNotification';
import customerService, { type Customer, type CustomerInput } from '../services/customerService';
import { ApiErrorUtils } from '../utils/apiError';

const props = defineProps<{
  isOpen: boolean;
  initial?: Customer | null;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'saved'): void;
}>();

const { notify } = useNotification();
const saving = ref(false);

const emptyForm = (): CustomerInput => ({
  name: '',
  netsisCariKodu: '',
  netsisTeslimCariKodu: '',
  operationType: 0,
  address: '',
  cityName: '',
  districtName: '',
  latitude: null,
  longitude: null,
  defaultContactName: '',
  defaultContactPhone: '',
});

const form = reactive<CustomerInput>(emptyForm());
const mode = ref<'create' | 'edit'>('create');

watch(() => [props.isOpen, props.initial], () => {
  if (props.isOpen) {
    if (props.initial) {
      mode.value = 'edit';
      Object.assign(form, {
        name: props.initial.name,
        netsisCariKodu: props.initial.netsisCariKodu ?? '',
        netsisTeslimCariKodu: props.initial.netsisTeslimCariKodu ?? '',
        operationType: props.initial.operationType,
        address: props.initial.address ?? '',
        cityName: props.initial.cityName ?? '',
        districtName: props.initial.districtName ?? '',
        latitude: props.initial.latitude,
        longitude: props.initial.longitude,
        defaultContactName: props.initial.defaultContactName ?? '',
        defaultContactPhone: props.initial.defaultContactPhone ?? '',
      });
    } else {
      mode.value = 'create';
      Object.assign(form, emptyForm());
    }
  }
}, { immediate: true });

const close = () => emit('close');

const save = async () => {
  if (!form.name?.trim()) {
    notify.error('Müşteri adı zorunludur.');
    return;
  }
  if (!form.netsisCariKodu?.trim()) {
    notify.error('Netsis Cari Kodu zorunludur.');
    return;
  }

  saving.value = true;
  try {
    const payload: CustomerInput = {
      name: form.name.trim(),
      netsisCariKodu: form.netsisCariKodu.trim(),
      netsisTeslimCariKodu: form.netsisTeslimCariKodu?.trim() || null,
      operationType: form.operationType,
      address: form.address?.trim() || null,
      cityName: form.cityName?.trim() || null,
      districtName: form.districtName?.trim() || null,
      latitude: form.latitude ?? null,
      longitude: form.longitude ?? null,
      defaultContactName: form.defaultContactName?.trim() || null,
      defaultContactPhone: form.defaultContactPhone?.trim() || null,
    };

    if (mode.value === 'edit' && props.initial) {
      await customerService.update(props.initial.id, payload);
      notify.success('Müşteri güncellendi.');
    } else {
      await customerService.create(payload);
      notify.success('Müşteri başarıyla oluşturuldu.');
    }

    emit('saved');
    close();
  } catch (error) {
    notify.error(ApiErrorUtils.getErrorMessage(error, 'Müşteri kaydedilirken hata oluştu.'));
  } finally {
    saving.value = false;
  }
};
</script>

<style scoped>
.input {
  @apply bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm;
}
</style>
