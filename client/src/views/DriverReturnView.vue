<template>
  <div class="space-y-4 pb-8">
    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-16">
      <div class="w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl p-4 text-red-700 dark:text-red-400 text-sm flex items-center gap-2">
      <button @click="router.back()" class="p-1 hover:bg-red-100 rounded-lg shrink-0">
        <ArrowLeftIcon class="w-5 h-5 flex-shrink-0" />
      </button>
      <span>{{ error }}</span>
    </div>

    <template v-else-if="shipment">
      <!-- Back button header -->
      <div class="flex items-center gap-3 mb-2 px-1">
        <button @click="router.back()" class="p-2 -ml-2 rounded-xl text-gray-500 hover:bg-gray-100 dark:hover:bg-white/10 transition-colors">
          <ArrowLeftIcon class="w-6 h-6" />
        </button>
        <h1 class="text-xl font-bold text-gray-900 dark:text-white">Araç İadesi</h1>
      </div>

      <!-- Project info card -->
      <div class="bg-white dark:bg-[#0f2744] rounded-xl shadow-sm border border-gray-200 dark:border-white/10 p-4 space-y-3">
        <div>
          <h2 class="text-lg font-semibold text-gray-900 dark:text-white">{{ shipment.projectName }}</h2>
          <p v-if="shipment.talepNo" class="text-sm text-gray-500 dark:text-gray-400">Talep: {{ shipment.talepNo }}</p>
        </div>

        <div class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
          <ArchiveBoxIcon class="w-4 h-4 flex-shrink-0" />
          {{ shipmentDetails?.lines.length || shipment.lineCount }} kalem iade edilecek
        </div>
      </div>

      <!-- Return form -->
      <div class="bg-white dark:bg-[#0f2744] rounded-xl shadow-sm border border-gray-200 dark:border-white/10 p-4 space-y-4">
        <h3 class="font-medium text-gray-900 dark:text-white">İade Bilgisi</h3>

        <!-- Reason -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            İade Sebebi (Tüm Kalemler İçin)
          </label>
          <select
            v-model="form.returnReason"
            class="w-full px-3 py-2.5 rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <!-- 1=Hasarli, 2=YanlisUrun, 3=EksikUrun, 4=FazlaUrun, 5=MusteriIptali, 6=Diger -->
            <option :value="1">Hasarlı / Kırık</option>
            <option :value="2">Yanlış Ürün</option>
            <option :value="3">Eksik Ürün</option>
            <option :value="4">Fazla Ürün</option>
            <option :value="5">Müşteri İptali / Almadı</option>
            <option :value="6">Diğer</option>
          </select>
        </div>

        <!-- Note -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Açıklama <span class="text-gray-400 font-normal">(Tercihen)</span>
          </label>
          <textarea
            v-model="form.returnNote"
            rows="2"
            placeholder="Kısmi iadelerde veya 'Diğer' seçildiğinde açıklama giriniz..."
            class="w-full px-3 py-2.5 rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white placeholder-gray-400 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
          ></textarea>
        </div>

        <!-- Submit button -->
        <button
          @click="recordReturn"
          :disabled="submitting || !shipmentDetails"
          class="w-full mt-4 py-3.5 bg-orange-600 hover:bg-orange-700 disabled:bg-orange-400 text-white font-semibold rounded-xl transition-colors flex items-center justify-center gap-2"
        >
          <span v-if="submitting" class="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          <ArrowUturnLeftIcon v-else class="w-5 h-5" />
          {{ submitting ? 'Kaydediliyor...' : 'İade Kaydet' }}
        </button>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  ArchiveBoxIcon,
  ArrowUturnLeftIcon,
  ArrowLeftIcon,
} from '@heroicons/vue/24/outline';
import driverService, { type DriverShipmentDto } from '../services/driverService';
import shipmentService, { type ShipmentDetail } from '../services/shipmentService';
import { useNotificationStore } from '../stores/notification';

const route = useRoute();
const router = useRouter();
const notify = useNotificationStore();

const shipmentId = Number(route.params.id);
const shipment = ref<DriverShipmentDto | null>(null);
const shipmentDetails = ref<ShipmentDetail | null>(null);
const loading = ref(false);
const error = ref('');
const submitting = ref(false);

const form = ref({
  returnReason: 5, // Default to MusteriIptali
  returnNote: '',
});

async function load() {
  loading.value = true;
  error.value = '';
  try {
    const all = await driverService.getShipments();
    shipment.value = all.find(s => s.id === shipmentId) ?? null;
    if (!shipment.value) {
      error.value = 'Sevkiyat bulunamadı.';
      return;
    }
    // Also fetch full shipment details to get the lines so we can return them all
    shipmentDetails.value = await shipmentService.getDetail(shipmentId);
  } catch {
    error.value = 'Yüklenemedi. Lütfen tekrar deneyin.';
  } finally {
    loading.value = false;
  }
}

async function recordReturn() {
  if (submitting.value || !shipmentDetails.value) return; 
  
  submitting.value = true;
  try {
    // For MVP driver return, we automatically return the fully undelivered quantity for all lines over an entire shipment payload.
    const linesToReturn = shipmentDetails.value.lines.map(l => {
      // Return whatever is not already returned or delivered.
      // Easiest is to fallback to orderedQty if it was never delivered. 
      const delivered = l.deliveredQty > 0 ? l.deliveredQty : l.orderedQty;
      const returnedAlready = l.returnedQty ?? 0;
      return {
        shipmentLineId: l.id,
        returnedQty: Math.max(0, delivered - returnedAlready),
        returnReason: form.value.returnReason,
      };
    }).filter(l => l.returnedQty > 0);

    if (linesToReturn.length === 0) {
      notify.add('İade edilebilecek kalem bulunamadı.', 'warning');
      submitting.value = false;
      return;
    }

    await shipmentService.recordVehicleReturn(shipmentId, {
      lines: linesToReturn,
      returnNote: form.value.returnNote || undefined,
    });
    
    notify.add('Araç iadesi başarıyla kaydedildi.', 'success');
    // Return to the previous screen (DriverStopView)
    router.back();
  } catch {
    notify.add('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
  } finally {
    submitting.value = false;
  }
}

onMounted(load);
</script>
