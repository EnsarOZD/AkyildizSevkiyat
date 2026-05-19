<template>
  <div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
    <div class="bg-white dark:bg-gray-900 rounded-xl p-6 w-full max-w-md shadow-xl border-t-4 border-teal-500">

      <div class="flex items-center gap-3 mb-5">
        <div class="p-2 bg-teal-100 dark:bg-teal-900/30 rounded-lg">
          <svg class="h-6 w-6 text-teal-600 dark:text-teal-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
          </svg>
        </div>
        <div>
          <h3 class="text-lg font-bold text-gray-800 dark:text-gray-200">Nakliye ile Gönder</h3>
          <p class="text-sm text-gray-500 dark:text-gray-400">{{ zoneName }}</p>
        </div>
      </div>

      <!-- Zone summary -->
      <div class="mb-5 bg-teal-50 dark:bg-teal-900/10 border border-teal-200 dark:border-teal-800 rounded-lg p-3 flex items-center gap-3">
        <svg class="h-5 w-5 text-teal-600 dark:text-teal-400 shrink-0" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
        </svg>
        <span class="text-sm text-teal-800 dark:text-teal-300">
          <strong>{{ shipmentCount }}</strong> sevkiyat nakliye ile gönderilecek ve <strong>Sevk Edildi</strong> statüsüne alınacak.
        </span>
      </div>

      <div class="space-y-4">
        <!-- Ad Soyad -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Nakliyeci Ad Soyad <span class="text-red-500">*</span>
          </label>
          <input
            v-model="carrierName"
            type="text"
            placeholder="Ad Soyad..."
            class="block w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
          />
        </div>

        <!-- Plaka -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Araç Plakası
            <span class="text-gray-400 font-normal ml-1">(opsiyonel)</span>
          </label>
          <input
            v-model="carrierPlate"
            type="text"
            placeholder="34 ABC 123"
            maxlength="20"
            class="block w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none uppercase"
            @input="carrierPlate = ($event.target as HTMLInputElement).value.toUpperCase()"
          />
        </div>

        <!-- Telefon -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Telefon
            <span class="text-gray-400 font-normal ml-1">(opsiyonel)</span>
          </label>
          <input
            v-model="carrierPhone"
            type="tel"
            placeholder="05XX XXX XX XX"
            maxlength="30"
            class="block w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
          />
        </div>
      </div>

      <div class="mt-6 flex justify-end gap-3">
        <button
          @click="$emit('close')"
          class="px-4 py-2.5 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg border border-gray-300 dark:border-gray-700 text-sm font-medium"
        >
          İptal
        </button>
        <button
          @click="save"
          :disabled="!carrierName.trim() || isSaving"
          class="px-6 py-2.5 bg-teal-600 hover:bg-teal-700 disabled:opacity-50 disabled:cursor-not-allowed text-white rounded-lg font-bold text-sm flex items-center gap-2"
        >
          <span v-if="isSaving" class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></span>
          <span>{{ isSaving ? 'Kaydediliyor...' : 'Nakliyeye Ver' }}</span>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onUnmounted } from 'vue';
import warehouseService from '../services/warehouseService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';

const props = defineProps<{
  zonePreparationIds: number[];
  zoneName: string;
  shipmentCount: number;
}>();

const emit = defineEmits(['close', 'completed']);
const notificationStore = useNotificationStore();

const carrierName = ref('');
const carrierPlate = ref('');
const carrierPhone = ref('');
const isSaving = ref(false);

document.body.style.overflow = 'hidden';
onUnmounted(() => { document.body.style.overflow = ''; });

const save = async () => {
  if (!carrierName.value.trim() || isSaving.value) return;

  isSaving.value = true;
  try {
    await Promise.all(props.zonePreparationIds.map(id =>
      warehouseService.dispatchAsFreight({
        zonePreparationId: id,
        carrierName: carrierName.value.trim(),
        carrierPlate: carrierPlate.value.trim() || null,
        carrierPhone: carrierPhone.value.trim() || null,
      })
    ));

    const detail = carrierPlate.value.trim() ? ` (${carrierPlate.value.trim()})` : '';
    notificationStore.add(`${props.shipmentCount} sevkiyat ${carrierName.value.trim()}${detail} nakliyecisine verildi.`, 'success');
    emit('completed');
    emit('close');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Nakliye ataması başarısız.', 'error');
  } finally {
    isSaving.value = false;
  }
};
</script>
