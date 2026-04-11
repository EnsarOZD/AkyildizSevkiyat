<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-950 flex flex-col">

    <!-- Header -->
    <div class="bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-700 px-4 py-3 flex items-center gap-3">
      <router-link to="/driver" class="text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200">
        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
      </router-link>
      <h1 class="text-lg font-semibold text-gray-900 dark:text-gray-100">
        {{ mode === 'end' ? 'Sefer Bitir' : 'Sefer Başlat' }}
      </h1>
    </div>

    <div class="flex-1 flex flex-col items-center justify-start p-4 gap-4 max-w-md mx-auto w-full">

      <!-- Mode Tabs -->
      <div class="flex w-full rounded-lg border border-gray-300 dark:border-gray-700 overflow-hidden text-sm">
        <button
          @click="mode = 'start'"
          class="flex-1 py-2.5 font-medium transition-colors"
          :class="mode === 'start' ? 'bg-blue-600 text-white' : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400'"
        >Sefer Başlat</button>
        <button
          @click="mode = 'end'"
          class="flex-1 py-2.5 font-medium transition-colors"
          :class="mode === 'end' ? 'bg-red-600 text-white' : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400'"
        >Sefer Bitir</button>
      </div>

      <!-- GPS Uyarısı -->
      <div v-if="gpsError" class="w-full rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-700 p-4 text-center">
        <p class="text-red-700 dark:text-red-300 font-medium text-sm">{{ gpsError }}</p>
        <button @click="requestGps" class="mt-2 text-sm text-red-600 underline">Tekrar dene</button>
      </div>

      <!-- Kamera QR Tarayıcı -->
      <div v-if="!gpsError" class="w-full">
        <div class="relative bg-black rounded-xl overflow-hidden aspect-square w-full max-w-xs mx-auto">
          <video ref="videoEl" class="w-full h-full object-cover" autoplay playsinline muted></video>
          <canvas ref="canvasEl" class="hidden"></canvas>
          <!-- Tarama çerçevesi -->
          <div class="absolute inset-0 flex items-center justify-center pointer-events-none">
            <div class="w-48 h-48 border-2 border-white rounded-lg opacity-70"></div>
          </div>
          <p class="absolute bottom-3 left-0 right-0 text-center text-white text-xs opacity-75">
            QR kodu çerçeve içine hizalayın
          </p>
        </div>

        <!-- Kamera yüklenirken -->
        <div v-if="cameraLoading" class="mt-2 text-center text-sm text-gray-500">Kamera başlatılıyor...</div>
        <div v-if="cameraError" class="mt-2 text-center text-sm text-red-500">{{ cameraError }}</div>
      </div>

      <!-- Tespit edilen QR — Onay aşaması -->
      <div v-if="detectedPlate && !confirmed" class="w-full rounded-xl border-2 bg-white dark:bg-gray-900 p-5 text-center"
        :class="mode === 'end' ? 'border-red-400 dark:border-red-600' : 'border-blue-400 dark:border-blue-600'">
        <p class="text-xs text-gray-500 dark:text-gray-400 mb-1 uppercase tracking-wider">Araç</p>
        <p class="text-3xl font-bold tracking-widest text-gray-900 dark:text-gray-100 mb-4">{{ detectedPlate }}</p>
        <p class="text-sm text-gray-600 dark:text-gray-400 mb-5">
          {{ mode === 'end' ? 'Bu araç ile sefer kapatılacak. Onaylıyor musunuz?' : 'Bu araç ile sefer başlatılacak. Onaylıyor musunuz?' }}
        </p>
        <div class="flex gap-3">
          <button @click="resetScan" class="flex-1 py-2.5 border dark:border-gray-700 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 text-sm font-medium">
            İptal
          </button>
          <button @click="confirm" :disabled="submitting"
            class="flex-1 py-2.5 rounded-lg text-white text-sm font-medium disabled:opacity-50"
            :class="mode === 'end' ? 'bg-red-600 hover:bg-red-700' : 'bg-blue-600 hover:bg-blue-700'">
            {{ submitting ? 'İşleniyor...' : 'Onayla' }}
          </button>
        </div>
      </div>

      <!-- Başarı mesajı -->
      <div v-if="confirmed && successMessage" class="w-full rounded-xl bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-700 p-5 text-center">
        <div class="text-4xl mb-3">✅</div>
        <p class="text-green-800 dark:text-green-200 font-semibold">{{ successMessage }}</p>
        <router-link to="/driver" class="mt-4 inline-block text-sm text-green-700 dark:text-green-300 underline">
          Rotaya Dön
        </router-link>
      </div>

      <!-- Hata -->
      <div v-if="submitError" class="w-full rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-700 p-4 text-center">
        <p class="text-red-700 dark:text-red-300 text-sm">{{ submitError }}</p>
        <button @click="resetScan" class="mt-2 text-sm text-red-600 underline">Tekrar dene</button>
      </div>

    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';
import { useRoute } from 'vue-router';
import jsQR from 'jsqr';
import driverSessionService from '../services/driverSessionService';
import { ApiErrorUtils } from '../utils/apiError';

const route = useRoute();
const mode = ref<'start' | 'end'>(route.query.mode === 'end' ? 'end' : 'start');

const videoEl = ref<HTMLVideoElement | null>(null);
const canvasEl = ref<HTMLCanvasElement | null>(null);
const cameraLoading = ref(true);
const cameraError = ref('');
const gpsError = ref('');

let stream: MediaStream | null = null;
let scanInterval: ReturnType<typeof setInterval> | null = null;

// GPS
const gpsPosition = ref<GeolocationPosition | null>(null);

const requestGps = () => {
  gpsError.value = '';
  navigator.geolocation.getCurrentPosition(
    pos => { gpsPosition.value = pos; },
    () => { gpsError.value = 'Konum izni gerekli. Lütfen tarayıcı ayarlarından izin verin.'; },
    { enableHighAccuracy: true, timeout: 10000 }
  );
};

// Scan state
const detectedQr = ref('');
const detectedPlate = ref('');
const confirmed = ref(false);
const submitting = ref(false);
const successMessage = ref('');
const submitError = ref('');

const startCamera = async () => {
  cameraLoading.value = true;
  cameraError.value = '';
  try {
    stream = await navigator.mediaDevices.getUserMedia({
      video: { facingMode: 'environment' }
    });
    if (videoEl.value) {
      videoEl.value.srcObject = stream;
      await videoEl.value.play();
    }
    cameraLoading.value = false;
    startScanning();
  } catch {
    cameraLoading.value = false;
    cameraError.value = 'Kamera erişilemedi. Lütfen kamera iznini kontrol edin.';
  }
};

const startScanning = () => {
  scanInterval = setInterval(() => {
    if (detectedQr.value || !videoEl.value || !canvasEl.value) return;
    const video = videoEl.value;
    const canvas = canvasEl.value;
    if (video.readyState !== video.HAVE_ENOUGH_DATA) return;

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;
    ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
    const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
    const code = jsQR(imageData.data, imageData.width, imageData.height);

    if (code && code.data.startsWith('AKYILDIZ_VEHICLE_')) {
      detectedQr.value = code.data;
      // QR formatı: AKYILDIZ_VEHICLE_{Id}_{PlateNumber}
      const parts = code.data.split('_');
      detectedPlate.value = parts.slice(3).join(' ') || code.data;
    }
  }, 200);
};

const resetScan = () => {
  detectedQr.value = '';
  detectedPlate.value = '';
  confirmed.value = false;
  submitting.value = false;
  submitError.value = '';
  successMessage.value = '';
};

const confirm = async () => {
  if (!gpsPosition.value) {
    gpsError.value = 'Konum bilgisi alınamadı. Lütfen GPS iznini kontrol edin.';
    return;
  }
  submitting.value = true;
  submitError.value = '';
  try {
    const lat = gpsPosition.value.coords.latitude;
    const lng = gpsPosition.value.coords.longitude;

    if (mode.value === 'start') {
      const res = await driverSessionService.startSession({
        qrCode: detectedQr.value,
        latitude: lat,
        longitude: lng,
      });
      successMessage.value = `${res.vehiclePlateNumber} aracıyla sefer başlatıldı.`;
    } else {
      const res = await driverSessionService.endSession(detectedQr.value, lat, lng);
      successMessage.value = `Sefer kapatıldı. Süre: ${res.totalDurationMinutes} dakika.`;
    }
    confirmed.value = true;
    stopCamera();
  } catch (e) {
    submitError.value = ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.';
    submitting.value = false;
  }
};

const stopCamera = () => {
  if (scanInterval) { clearInterval(scanInterval); scanInterval = null; }
  if (stream) { stream.getTracks().forEach(t => t.stop()); stream = null; }
};

onMounted(() => {
  requestGps();
  startCamera();
});

onUnmounted(() => {
  stopCamera();
});

watch(mode, () => {
  resetScan();
});
</script>
