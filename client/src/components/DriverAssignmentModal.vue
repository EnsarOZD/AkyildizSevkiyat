<template>
  <div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
    <div class="bg-white dark:bg-gray-900 rounded-lg p-6 w-full max-w-lg shadow-xl border-t-4"
         :class="summaryLoaded && !summary?.canProceed ? 'border-red-500' : 'border-blue-500'">

      <h3 class="text-xl font-bold text-gray-800 dark:text-gray-200 mb-4">Şoför ve Araç Atama</h3>

      <!-- Pre-flight summary -->
      <div v-if="summaryLoading" class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400 mb-4">
        <span class="animate-spin h-4 w-4 border-2 border-blue-500 border-t-transparent rounded-full"></span>
        Ön kontrol yapılıyor...
      </div>

      <div v-if="summaryLoaded && summary" class="mb-4 space-y-2">
        <!-- Blockers -->
        <div v-if="summary.blockers.length > 0" class="rounded-md bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 p-3">
          <div class="flex items-center gap-2 mb-1">
            <svg class="h-4 w-4 text-red-600 dark:text-red-400 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd"/>
            </svg>
            <span class="text-sm font-semibold text-red-700 dark:text-red-400">Araç ataması engelleyen sorunlar:</span>
          </div>
          <ul class="list-disc list-inside space-y-1">
            <li v-for="b in summary.blockers" :key="b" class="text-sm text-red-700 dark:text-red-400">{{ b }}</li>
          </ul>
        </div>

        <!-- Warnings -->
        <div v-if="summary.warnings.length > 0" class="rounded-md bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 p-3">
          <div class="flex items-center gap-2 mb-1">
            <svg class="h-4 w-4 text-amber-600 dark:text-amber-400 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd"/>
            </svg>
            <span class="text-sm font-semibold text-amber-700 dark:text-amber-400">Uyarılar (engellemez):</span>
          </div>
          <ul class="list-disc list-inside space-y-1">
            <li v-for="w in summary.warnings" :key="w" class="text-sm text-amber-700 dark:text-amber-400">{{ w }}</li>
          </ul>
        </div>

        <!-- All OK -->
        <div v-if="summary.canProceed && summary.blockers.length === 0" class="rounded-md bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 p-3 flex items-center gap-2">
          <svg class="h-4 w-4 text-green-600 dark:text-green-400 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
          </svg>
          <span class="text-sm font-medium text-green-700 dark:text-green-400">
            Tüm kontroller geçti — {{ summary.totalShipments }} sevkiyat araç atamasına hazır.
          </span>
        </div>
      </div>

      <!-- Driver / Vehicle selects -->
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Şoför Seçin</label>
          <select v-model="selectedDriverId" class="mt-1 block w-full pl-3 pr-10 py-2 text-base border-gray-300 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm rounded-md border dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
            <option :value="null">Seçiniz...</option>
            <option v-for="d in drivers" :key="d.id" :value="d.id">{{ d.fullName }} ({{ d.phone }})</option>
          </select>
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Araç Seçin</label>
          <select v-model="selectedVehicleId" class="mt-1 block w-full pl-3 pr-10 py-2 text-base border-gray-300 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm rounded-md border dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
            <option :value="null">Seçiniz...</option>
            <option v-for="v in vehicles" :key="v.id" :value="v.id">{{ v.plateNumber }} ({{ v.capacity }})</option>
          </select>
        </div>
      </div>

      <div class="mt-6 flex justify-end gap-3">
        <button @click="$emit('close')" class="px-4 py-3 min-h-[44px] text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded border border-gray-300 dark:border-gray-700">İptal</button>
        <button
          @click="save"
          :disabled="!canSave"
          class="px-6 py-3 min-h-[44px] bg-blue-600 text-white rounded font-bold hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
        >
          <span v-if="isSaving" class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></span>
          <span>{{ isSaving ? 'KAYDEDİLİYOR...' : 'KAYDET VE TRANSFERE HAZIRLA' }}</span>
        </button>
      </div>

    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed } from 'vue';
import transportService from '../services/transportService';
import warehouseService, { type PreDispatchSummaryDto } from '../services/warehouseService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';

const props = defineProps<{
  zonePreparationId: number;
}>();

const emit = defineEmits(['close', 'completed']);
const notificationStore = useNotificationStore();

const drivers = ref<any[]>([]);
const vehicles = ref<any[]>([]);
const selectedDriverId = ref<number | null>(null);
const selectedVehicleId = ref<number | null>(null);
const isSaving = ref(false);

const summary = ref<PreDispatchSummaryDto | null>(null);
const summaryLoading = ref(false);
const summaryLoaded = ref(false);

document.body.style.overflow = 'hidden';
onUnmounted(() => { document.body.style.overflow = ''; });

const canSave = computed(() =>
  selectedDriverId.value !== null &&
  selectedVehicleId.value !== null &&
  !isSaving.value &&
  !summaryLoading.value &&
  (summary.value?.canProceed ?? false)
);

onMounted(async () => {
  summaryLoading.value = true;
  try {
    const [dList, vList, s] = await Promise.all([
      transportService.getDrivers(),
      transportService.getVehicles(),
      warehouseService.getPreDispatchSummary(props.zonePreparationId)
    ]);
    drivers.value = dList.filter((d: any) => d.isActive);
    vehicles.value = vList.filter((v: any) => v.isActive);
    summary.value = s;
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || "Veriler yüklenemedi.", 'error');
  } finally {
    summaryLoading.value = false;
    summaryLoaded.value = true;
  }
});

const save = async () => {
  if (!canSave.value) return;

  isSaving.value = true;
  try {
    await warehouseService.setDriverInfo({
      ZonePreparationId: props.zonePreparationId,
      DriverId: selectedDriverId.value!,
      VehicleId: selectedVehicleId.value!
    });
    notificationStore.add('Sürücü ve araç atandı.', 'success');
    emit('completed');
    emit('close');
  } catch (e: any) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || "Kaydetme başarısız.", 'error');
  } finally {
    isSaving.value = false;
  }
};
</script>
