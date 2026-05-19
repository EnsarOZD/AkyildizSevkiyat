<template>
  <div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
    <div class="bg-white dark:bg-gray-900 rounded-xl p-6 w-full max-w-md shadow-xl border-t-4 border-orange-500">

      <div class="flex items-center gap-3 mb-5">
        <div class="p-2 bg-orange-100 dark:bg-orange-900/30 rounded-lg">
          <svg class="h-6 w-6 text-orange-600 dark:text-orange-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
          </svg>
        </div>
        <div>
          <h3 class="text-lg font-bold text-gray-800 dark:text-gray-200">Kargo ile Gönder</h3>
          <p class="text-sm text-gray-500 dark:text-gray-400">{{ zoneName }}</p>
        </div>
      </div>

      <!-- Zone summary -->
      <div class="mb-5 bg-orange-50 dark:bg-orange-900/10 border border-orange-200 dark:border-orange-800 rounded-lg p-3 flex items-center gap-3">
        <svg class="h-5 w-5 text-orange-600 dark:text-orange-400 shrink-0" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
        </svg>
        <span class="text-sm text-orange-800 dark:text-orange-300">
          <strong>{{ shipmentCount }}</strong> sevkiyat kargo ile gönderilecek ve <strong>Sevk Edildi</strong> statüsüne alınacak.
        </span>
      </div>

      <div class="space-y-4">
        <!-- Cargo provider -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Kargo Firması <span class="text-red-500">*</span>
          </label>
          <select
            v-model="selectedProvider"
            class="block w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm bg-white dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-orange-500 focus:border-transparent outline-none"
          >
            <option :value="null" disabled>Seçiniz...</option>
            <option v-for="p in CARGO_PROVIDERS" :key="p.value" :value="p.value">{{ p.label }}</option>
          </select>
        </div>

        <!-- Tracking number (non-YK only) -->
        <div v-if="selectedProvider !== YURTICI_VALUE">
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Takip Numarası
            <span class="text-gray-400 font-normal ml-1">(opsiyonel)</span>
          </label>
          <input
            v-model="trackingNumber"
            type="text"
            placeholder="Kargo takip numarası..."
            class="block w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-orange-500 focus:border-transparent outline-none"
          />
        </div>

        <!-- Desi lines — Yurtiçi only -->
        <div v-if="selectedProvider === YURTICI_VALUE">
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
            Desi / Parça Bilgisi <span class="text-red-500">*</span>
          </label>

          <div class="space-y-2">
            <div
              v-for="(line, idx) in desiLines"
              :key="idx"
              class="flex items-center gap-2"
            >
              <input
                v-model.number="line.count"
                type="number"
                min="1"
                placeholder="Adet"
                class="w-20 border border-gray-300 dark:border-gray-700 rounded-lg px-2 py-2 text-sm text-center bg-white dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-orange-500 outline-none"
              />
              <span class="text-gray-500 dark:text-gray-400 text-sm">×</span>
              <select
                v-model="line.desiOption"
                class="flex-1 border border-gray-300 dark:border-gray-700 rounded-lg px-2 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-orange-500 outline-none"
              >
                <option :value="null" disabled>Desi seçin</option>
                <option v-for="opt in DESI_OPTIONS" :key="opt.value ?? 'custom'" :value="opt.value">
                  {{ opt.label }}
                </option>
              </select>
              <input
                v-if="line.desiOption === 0"
                v-model.number="line.customDesi"
                type="number"
                min="33"
                placeholder="Desi"
                class="w-20 border border-gray-300 dark:border-gray-700 rounded-lg px-2 py-2 text-sm text-center bg-white dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-orange-500 outline-none"
              />
              <button
                v-if="desiLines.length > 1"
                @click="removeLine(idx)"
                class="text-red-400 hover:text-red-600 p-1"
                title="Kaldır"
              >
                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                </svg>
              </button>
            </div>
          </div>

          <button
            @click="addLine"
            class="mt-2 flex items-center gap-1 text-sm text-orange-600 dark:text-orange-400 hover:underline"
          >
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
            </svg>
            Kalem Ekle
          </button>

          <!-- Totals -->
          <div v-if="totalPieces > 0 && totalDesi > 0" class="mt-3 flex gap-4 text-sm bg-gray-50 dark:bg-gray-800 rounded-lg px-3 py-2">
            <span class="text-gray-600 dark:text-gray-400">Toplam: <strong class="text-gray-900 dark:text-white">{{ totalPieces }} parça</strong></span>
            <span class="text-gray-400">·</span>
            <span class="text-gray-600 dark:text-gray-400">Toplam desi: <strong class="text-gray-900 dark:text-white">{{ totalDesi }}</strong></span>
          </div>
        </div>

        <!-- API note for Yurtiçi -->
        <div v-if="selectedProvider === YURTICI_VALUE" class="flex items-start gap-2 bg-blue-50 dark:bg-blue-900/10 border border-blue-200 dark:border-blue-800 rounded-lg p-3">
          <svg class="h-4 w-4 text-blue-500 shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
          </svg>
          <p class="text-xs text-blue-700 dark:text-blue-400">
            <strong>Yurtiçi Kargo</strong> seçildiğinde irsaliye no, proje adı, teslimat adresi, alıcı telefonu ve desi bilgisi kargo API'sine iletilir.
          </p>
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
          :disabled="!canSave || isSaving"
          class="px-6 py-2.5 bg-orange-500 hover:bg-orange-600 disabled:opacity-50 disabled:cursor-not-allowed text-white rounded-lg font-bold text-sm flex items-center gap-2"
        >
          <span v-if="isSaving" class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></span>
          <span>{{ isSaving ? 'Kaydediliyor...' : 'Kargoya Ver' }}</span>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onUnmounted } from 'vue';
import warehouseService, { CARGO_PROVIDERS } from '../services/warehouseService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';

const props = defineProps<{
  zonePreparationId: number;
  zoneName: string;
  shipmentCount: number;
}>();

const emit = defineEmits(['close', 'completed']);
const notificationStore = useNotificationStore();

const YURTICI_VALUE = 1;

const DESI_OPTIONS = [
  { label: 'Poşet',   value: 1  },
  { label: '5 Desi',  value: 5  },
  { label: '16 Desi', value: 16 },
  { label: '32 Desi', value: 32 },
  { label: '32+ Desi (özel)', value: 0 },
];

interface DesiLine {
  count: number;
  desiOption: number | null;
  customDesi: number | null;
}

const selectedProvider = ref<number | null>(null);
const trackingNumber = ref('');
const isSaving = ref(false);
const desiLines = ref<DesiLine[]>([{ count: 1, desiOption: null, customDesi: null }]);

function addLine() {
  desiLines.value.push({ count: 1, desiOption: null, customDesi: null });
}

function removeLine(idx: number) {
  desiLines.value.splice(idx, 1);
}

function effectiveDesi(line: DesiLine): number {
  if (line.desiOption === 0) return line.customDesi ?? 0;
  return line.desiOption ?? 0;
}

const totalPieces = computed(() =>
  desiLines.value.reduce((sum, l) => sum + (l.count || 0), 0)
);

const totalDesi = computed(() =>
  desiLines.value.reduce((sum, l) => sum + (l.count || 0) * effectiveDesi(l), 0)
);

const desiLinesValid = computed(() =>
  desiLines.value.every(l =>
    l.count > 0 &&
    l.desiOption !== null &&
    (l.desiOption !== 0 || (l.customDesi != null && l.customDesi > 32))
  )
);

const canSave = computed(() => {
  if (selectedProvider.value === null) return false;
  if (selectedProvider.value === YURTICI_VALUE) return desiLinesValid.value;
  return true;
});

document.body.style.overflow = 'hidden';
onUnmounted(() => { document.body.style.overflow = ''; });

const save = async () => {
  if (!canSave.value || isSaving.value) return;

  isSaving.value = true;
  try {
    const ykDesiLines = selectedProvider.value === YURTICI_VALUE
      ? desiLines.value.map(l => ({ count: l.count, desi: effectiveDesi(l) }))
      : null;

    await warehouseService.dispatchAsCargo({
      zonePreparationId: props.zonePreparationId,
      cargoProvider: selectedProvider.value!,
      cargoTrackingNumber: trackingNumber.value || null,
      ykDesiLines,
    });

    const providerLabel = CARGO_PROVIDERS.find(p => p.value === selectedProvider.value)?.label ?? 'Kargo';
    notificationStore.add(`${props.shipmentCount} sevkiyat ${providerLabel} ile gönderildi.`, 'success');
    emit('completed');
    emit('close');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kargo ataması başarısız.', 'error');
  } finally {
    isSaving.value = false;
  }
};
</script>
