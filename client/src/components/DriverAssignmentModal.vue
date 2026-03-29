<template>
  <div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
    <div class="bg-white dark:bg-gray-900 rounded-lg p-6 w-full max-w-lg shadow-xl border-t-4 max-h-[90vh] overflow-y-auto"
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
        <div v-if="summary.canProceed && summary.blockers.length === 0"
             class="rounded-md bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 p-3 flex items-center gap-2">
          <svg class="h-4 w-4 text-green-600 dark:text-green-400 flex-shrink-0" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd"/>
          </svg>
          <span class="text-sm font-medium text-green-700 dark:text-green-400">
            Tüm kontroller geçti — {{ summary.totalShipments }} sevkiyat araç atamasına hazır.
          </span>
        </div>

        <!-- Optimization info -->
        <div class="rounded-md bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 p-3 flex items-start gap-2">
          <svg class="h-4 w-4 text-blue-500 dark:text-blue-400 flex-shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd"/>
          </svg>
          <span class="text-sm text-blue-700 dark:text-blue-400">
            Araç ataması sonrası rota otomatik optimize edilecek. Mevcut proje sıralaması güncellenecektir.
          </span>
        </div>
      </div>

      <!-- Form -->
      <div class="space-y-4">

        <!-- Çoklu şoför seçimi -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Şoförler
            <span class="text-gray-400 font-normal ml-1">(ilk seçilen ana şoför)</span>
          </label>

          <div v-if="selectedDriverIds.length > 10" class="mb-2 p-2 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 rounded text-xs text-amber-700 dark:text-amber-400">
            10'dan fazla şoför seçildi. Lütfen sayıyı azaltın.
          </div>

          <div class="border border-gray-300 dark:border-gray-700 rounded-md max-h-48 overflow-y-auto">
            <label
              v-for="d in drivers" :key="d.id"
              class="flex items-center gap-3 px-3 py-2.5 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors border-b border-gray-100 dark:border-gray-800 last:border-0"
            >
              <input
                type="checkbox"
                :value="d.id"
                v-model="selectedDriverIds"
                @change="onDriverCheckChange(d.id)"
                class="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
              />
              <span class="flex-1 text-sm text-gray-900 dark:text-gray-100">{{ d.fullName }}</span>
              <span class="text-xs text-gray-400">{{ d.phone }}</span>
              <span v-if="selectedDriverIds[0] === d.id && selectedDriverIds.length > 0"
                    class="text-[11px] bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 px-1.5 py-0.5 rounded font-medium">
                Ana Şoför
              </span>
            </label>
            <div v-if="drivers.length === 0" class="px-3 py-3 text-sm text-gray-500 dark:text-gray-400 italic">
              Aktif şoför bulunamadı.
            </div>
          </div>

          <p v-if="selectedDriverIds.length > 0" class="mt-1 text-xs text-gray-500 dark:text-gray-400">
            {{ selectedDriverIds.length }} şoför seçildi. Sıra: {{ selectedDriverNames.join(' → ') }}
          </p>
        </div>

        <!-- Araç seçimi -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Araç Seçin</label>
          <select v-model="selectedVehicleId"
                  class="mt-1 block w-full pl-3 pr-10 py-2 text-base border-gray-300 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm rounded-md border dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
            <option :value="null">Seçiniz...</option>
            <option v-for="v in vehicles" :key="v.id" :value="v.id">
              {{ v.plateNumber }} — {{ v.vehicleTypeName || ['Kamyon','Kamyonet','Minibüs'][v.vehicleType] ?? '' }}{{ v.capacity ? ` (${v.capacity})` : '' }}
            </option>
          </select>
        </div>

        <!-- Hareket saati -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Hareket Saati</label>
          <input
            type="time"
            v-model="departureTime"
            class="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
          />
          <p class="mt-1 text-xs text-gray-500 dark:text-gray-400">Rota optimizasyonunda başlangıç saati olarak kullanılır.</p>
        </div>
      </div>

      <div class="mt-6 flex justify-end gap-3">
        <button @click="$emit('close')"
                class="px-4 py-3 min-h-[44px] text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded border border-gray-300 dark:border-gray-700">
          İptal
        </button>
        <button
          @click="save"
          :disabled="!canSave"
          class="px-6 py-3 min-h-[44px] bg-blue-600 text-white rounded font-bold hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2"
        >
          <span v-if="isSaving" class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></span>
          <span>{{ isSaving ? 'Rota optimize ediliyor ve kaydediliyor...' : 'KAYDET VE TRANSFERE HAZIRLA' }}</span>
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
const selectedDriverIds = ref<number[]>([]);
const selectedVehicleId = ref<number | null>(null);
const departureTime = ref('08:00');
const isSaving = ref(false);

const summary = ref<PreDispatchSummaryDto | null>(null);
const summaryLoading = ref(false);
const summaryLoaded = ref(false);

document.body.style.overflow = 'hidden';
onUnmounted(() => { document.body.style.overflow = ''; });

const selectedDriverNames = computed(() =>
  selectedDriverIds.value.map(id => drivers.value.find(d => d.id === id)?.fullName ?? `#${id}`)
);

const canSave = computed(() =>
  selectedDriverIds.value.length > 0 &&
  selectedDriverIds.value.length <= 10 &&
  selectedVehicleId.value !== null &&
  !isSaving.value &&
  !summaryLoading.value &&
  (summary.value?.canProceed ?? false)
);

// Checkbox sırası: ilk seçilen ana şoför olarak kalır
const driverSelectionOrder = ref<number[]>([]);
const onDriverCheckChange = (driverId: number) => {
  const checked = selectedDriverIds.value.includes(driverId);
  if (checked) {
    if (!driverSelectionOrder.value.includes(driverId))
      driverSelectionOrder.value.push(driverId);
  } else {
    driverSelectionOrder.value = driverSelectionOrder.value.filter(id => id !== driverId);
  }
  // Sırayı seçim sırasına göre yeniden düzenle
  selectedDriverIds.value = driverSelectionOrder.value.filter(id =>
    selectedDriverIds.value.includes(id)
  );
};

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
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Veriler yüklenemedi.', 'error');
  } finally {
    summaryLoading.value = false;
    summaryLoaded.value = true;
  }
});

const save = async () => {
  if (!canSave.value) return;

  isSaving.value = true;
  try {
    const result = await warehouseService.setDriverInfo({
      ZonePreparationId: props.zonePreparationId,
      DriverIds:         selectedDriverIds.value,
      VehicleId:         selectedVehicleId.value!,
      DepartureTime:     departureTime.value || null,
    });

    if (result?.optimizationWarning) {
      notificationStore.add(result.optimizationWarning, 'warning');
    } else if (result?.optimizationApplied) {
      notificationStore.add('Sürücü ve araç atandı. Rota optimize edildi.', 'success');
    } else {
      notificationStore.add('Sürücü ve araç atandı.', 'success');
    }

    emit('completed');
    emit('close');
  } catch (e: any) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kaydetme başarısız.', 'error');
  } finally {
    isSaving.value = false;
  }
};
</script>
