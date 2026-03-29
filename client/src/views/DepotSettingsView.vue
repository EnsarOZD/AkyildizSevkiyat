<template>
  <div class="p-6 max-w-2xl">
    <div class="mb-6">
      <h1 class="text-2xl font-bold dark:text-gray-100">Depo Tanımları</h1>
      <p class="text-gray-500 dark:text-gray-400 text-sm mt-1">Rota optimizasyonunda kullanılacak başlangıç noktası.</p>
    </div>

    <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-6 space-y-5">
      <div>
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Depo Adı</label>
        <input
          v-model="form.depotName"
          type="text"
          placeholder="Örn: Akyıldız Ana Depo"
          class="w-full border dark:border-gray-700 rounded-lg px-3 py-2 dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 focus:outline-none"
        />
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Adres</label>
        <div class="flex gap-2">
          <input
            v-model="form.depotAddress"
            type="text"
            placeholder="Depo adresi"
            class="flex-1 border dark:border-gray-700 rounded-lg px-3 py-2 dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 focus:outline-none"
          />
          <button
            @click="geocodeAddress"
            :disabled="!form.depotAddress || geocoding"
            class="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed text-sm whitespace-nowrap flex items-center gap-2"
          >
            <svg v-if="geocoding" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
            </svg>
            <span>{{ geocoding ? 'Aranıyor...' : 'Koordinatı Bul' }}</span>
          </button>
        </div>
        <p v-if="geocodeError" class="mt-1 text-sm text-red-500">{{ geocodeError }}</p>
      </div>

      <div class="grid grid-cols-2 gap-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Enlem (Latitude)</label>
          <input
            v-model.number="form.depotLatitude"
            type="number"
            step="0.000001"
            placeholder="41.0082"
            class="w-full border dark:border-gray-700 rounded-lg px-3 py-2 dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 focus:outline-none"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Boylam (Longitude)</label>
          <input
            v-model.number="form.depotLongitude"
            type="number"
            step="0.000001"
            placeholder="28.9784"
            class="w-full border dark:border-gray-700 rounded-lg px-3 py-2 dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 focus:outline-none"
          />
        </div>
      </div>

      <!-- Koordinat göstergesi -->
      <div
        v-if="form.depotLatitude && form.depotLongitude"
        class="flex items-center gap-2 p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg"
      >
        <svg class="w-4 h-4 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/>
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/>
        </svg>
        <span class="text-sm text-green-700 dark:text-green-400">
          {{ form.depotLatitude.toFixed(6) }}, {{ form.depotLongitude.toFixed(6) }}
        </span>
      </div>

      <div class="flex gap-3 pt-2">
        <button
          @click="save"
          :disabled="saving"
          class="px-5 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 font-medium flex items-center gap-2"
        >
          <svg v-if="saving" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
          </svg>
          <span>{{ saving ? 'Kaydediliyor...' : 'Kaydet' }}</span>
        </button>
        <button
          @click="load"
          :disabled="loading"
          class="px-4 py-2 border dark:border-gray-700 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800"
        >
          Sıfırla
        </button>
      </div>

      <p v-if="savedMsg" class="text-sm text-green-600 dark:text-green-400 font-medium">✓ {{ savedMsg }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import systemSettingsService from '../services/systemSettingsService';
import { useNotificationStore } from '../stores/notification';

const notificationStore = useNotificationStore();

const form = ref({
  depotName: null as string | null,
  depotAddress: null as string | null,
  depotLatitude: null as number | null,
  depotLongitude: null as number | null,
});

const loading = ref(false);
const saving = ref(false);
const geocoding = ref(false);
const geocodeError = ref('');
const savedMsg = ref('');

const load = async () => {
  loading.value = true;
  try {
    const data = await systemSettingsService.getDepotSettings();
    form.value = { ...data };
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
};

const geocodeAddress = async () => {
  if (!form.value.depotAddress) return;
  geocoding.value = true;
  geocodeError.value = '';
  try {
    const result = await systemSettingsService.geocodeAddress(form.value.depotAddress);
    if (result) {
      form.value.depotLatitude = result.lat;
      form.value.depotLongitude = result.lng;
    } else {
      geocodeError.value = 'Adres bulunamadı. Daha açık bir adres deneyin.';
    }
  } catch (e) {
    geocodeError.value = 'Koordinat bulunamadı.';
  } finally {
    geocoding.value = false;
  }
};

const save = async () => {
  saving.value = true;
  savedMsg.value = '';
  try {
    await systemSettingsService.saveDepotSettings(form.value);
    savedMsg.value = 'Depo ayarları kaydedildi.';
    setTimeout(() => { savedMsg.value = ''; }, 3000);
  } catch (e) {
    notificationStore.add('Kayıt başarısız.', 'error');
  } finally {
    saving.value = false;
  }
};

onMounted(load);
</script>
