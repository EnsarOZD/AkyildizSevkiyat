<template>
  <div class="min-h-screen bg-gray-50 dark:bg-[#0a1626] flex flex-col">

    <!-- Header -->
    <div class="bg-white dark:bg-[#0f2038] border-b border-gray-200 dark:border-white/10 px-4 py-3 flex items-center gap-3">
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
      <div v-if="step === 'scan'" class="flex w-full rounded-lg border border-gray-300 dark:border-white/10 overflow-hidden text-sm">
        <button
          @click="mode = 'start'"
          class="flex-1 py-2.5 font-medium transition-colors"
          :class="mode === 'start' ? 'bg-blue-600 text-white' : 'bg-white dark:bg-white/5 text-gray-600 dark:text-gray-400'"
        >Sefer Başlat</button>
        <button
          @click="mode = 'end'"
          class="flex-1 py-2.5 font-medium transition-colors"
          :class="mode === 'end' ? 'bg-red-600 text-white' : 'bg-white dark:bg-white/5 text-gray-600 dark:text-gray-400'"
        >Sefer Bitir</button>
      </div>

      <!-- Location denied banner — sadece scan adımında göster -->
      <div v-if="step === 'scan' && permissionState === 'denied'" class="w-full rounded-lg bg-amber-50 dark:bg-amber-900/20 border border-amber-300 dark:border-amber-700 p-4">
        <p class="text-amber-800 dark:text-amber-300 font-medium text-sm mb-1">Konum izni gerekli</p>
        <p class="text-amber-700 dark:text-amber-400 text-xs mb-2">{{ settingsInstructions }}</p>
        <router-link
          to="/driver/settings"
          class="inline-block text-xs font-semibold text-amber-700 dark:text-amber-300 underline"
        >İzinler sayfasına git →</router-link>
      </div>

      <!-- GPS error banner -->
      <div v-else-if="gpsError" class="w-full rounded-lg bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-700 p-4 text-center">
        <p class="text-red-700 dark:text-red-300 font-medium text-sm">{{ gpsError }}</p>
        <button @click="requestGps" class="mt-2 text-sm text-red-600 underline">Tekrar dene</button>
      </div>

      <!-- ── STEP: scan (araç) + irsaliye (sefer başlat) ─────────────── -->
      <template v-if="step === 'scan' || step === 'irsaliye'">
        <!-- QR camera (her iki adımda da aynı kamera) -->
        <div v-if="!gpsError && permissionState !== 'denied'" class="w-full">
          <div class="relative bg-black rounded-xl overflow-hidden aspect-square w-full max-w-xs mx-auto">
            <video ref="videoEl" class="w-full h-full object-cover" autoplay playsinline muted></video>
            <canvas ref="canvasEl" class="hidden"></canvas>
            <div class="absolute inset-0 flex items-center justify-center pointer-events-none">
              <div class="w-48 h-48 border-2 rounded-lg opacity-70"
                :class="step === 'irsaliye' ? 'border-emerald-400' : 'border-white'"></div>
            </div>
            <p class="absolute bottom-3 left-0 right-0 text-center text-white text-xs opacity-75">
              {{ step === 'irsaliye' ? 'İrsaliye QR kodunu okutun' : 'Araç QR kodunu çerçeveye hizalayın' }}
            </p>
          </div>
          <div v-if="cameraLoading" class="mt-2 text-center text-sm text-gray-500">Kamera başlatılıyor...</div>
          <div v-if="cameraError" class="mt-2 text-center text-sm text-red-500">{{ cameraError }}</div>
        </div>

        <!-- Plate confirmation card (yalnızca araç adımı) -->
        <div v-if="step === 'scan' && detectedPlate" class="w-full rounded-xl border-2 bg-white dark:bg-[#0f2038] p-5 text-center"
          :class="mode === 'end' ? 'border-red-400 dark:border-red-600' : 'border-blue-400 dark:border-blue-600'">
          <p class="text-xs text-gray-500 dark:text-gray-400 mb-1 uppercase tracking-wider">Araç</p>
          <p class="text-3xl font-bold tracking-widest text-gray-900 dark:text-gray-100 mb-4">{{ detectedPlate }}</p>
          <p class="text-sm text-gray-600 dark:text-gray-400 mb-5">
            {{ mode === 'end' ? 'Bu araç ile sefer kapatılacak. Onaylıyor musunuz?' : 'Bu araç ile devam edilecek. İrsaliye okutmaya geçilsin mi?' }}
          </p>
          <div class="flex gap-3">
            <button @click="resetScan" class="flex-1 py-2.5 border dark:border-white/10 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 text-sm font-medium">
              İptal
            </button>
            <button @click="onPlateConfirmed"
              class="flex-1 py-2.5 rounded-lg text-white text-sm font-medium"
              :class="mode === 'end' ? 'bg-gradient-to-br from-red-500 to-red-600 hover:from-red-400 hover:to-red-500' : 'bg-gradient-to-br from-blue-500 to-blue-600 hover:from-blue-400 hover:to-blue-500'">
              {{ mode === 'end' ? 'Onayla' : 'Devam' }}
            </button>
          </div>
        </div>

        <!-- İrsaliye adımı bilgisi -->
        <div v-if="step === 'irsaliye'" class="w-full rounded-xl border border-emerald-300 dark:border-emerald-700 bg-emerald-50 dark:bg-emerald-900/20 p-4 text-center">
          <p class="text-sm text-emerald-800 dark:text-emerald-300 font-medium">
            <span class="font-bold">{{ detectedPlate }}</span> aracı için herhangi bir sevkiyatın irsaliye QR'ını okutun.
          </p>
          <p v-if="resolving" class="mt-2 text-xs text-emerald-700 dark:text-emerald-400">Sevkiyatlar çözümleniyor...</p>
          <p v-if="resolveError" class="mt-2 text-xs text-red-600">{{ resolveError }}</p>
          <button @click="resetScan" class="mt-3 text-xs text-gray-500 underline">İptal</button>
        </div>
      </template>

      <!-- ── STEP: shipments (sefer manifesti onayı) ──────────────────── -->
      <template v-else-if="step === 'shipments'">
        <div class="w-full rounded-xl bg-white dark:bg-[#0f2038] border border-gray-200 dark:border-white/10 p-4 space-y-3">
          <div class="text-center">
            <p class="text-base font-semibold text-gray-900 dark:text-gray-100">{{ detectedPlate }}</p>
            <p class="text-sm text-gray-500 dark:text-gray-400">
              Bu sefere <span class="font-bold text-blue-600 dark:text-blue-400">{{ resolvedShipments.length }} sevkiyat</span> dahil edilecek
            </p>
          </div>
          <div class="divide-y divide-gray-100 dark:divide-gray-800 max-h-72 overflow-y-auto">
            <div v-for="s in resolvedShipments" :key="s.id" class="py-2.5 flex justify-between items-start gap-2">
              <div class="min-w-0">
                <p class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">{{ s.projectName }}</p>
                <p class="text-xs text-gray-500 dark:text-gray-400 font-mono">
                  {{ s.irsaliyeNo || '—' }}<span v-if="s.talepNo"> · {{ s.talepNo }}</span>
                </p>
              </div>
              <span class="text-[11px] text-gray-400 whitespace-nowrap">{{ s.lineCount }} kalem</span>
            </div>
          </div>
          <div class="flex gap-3 pt-1">
            <button @click="resetScan" class="flex-1 py-2.5 border dark:border-white/10 rounded-lg text-gray-700 dark:text-gray-300 text-sm font-medium hover:bg-gray-50 dark:hover:bg-gray-800">
              İptal
            </button>
            <button @click="goToOdometerStep" class="flex-1 py-2.5 rounded-lg text-white text-sm font-medium bg-gradient-to-br from-blue-500 to-blue-600 hover:from-blue-400 hover:to-blue-500">
              Onayla, devam et
            </button>
          </div>
        </div>
      </template>

      <!-- ── STEP: odometer ─────────────────────────────── -->
      <template v-else-if="step === 'odometer'">
        <div class="w-full rounded-xl bg-white dark:bg-[#0f2038] border border-gray-200 dark:border-white/10 p-5 space-y-4">
          <div class="text-center">
            <p class="text-base font-semibold text-gray-900 dark:text-gray-100">
              {{ mode === 'end' ? 'Bitiş Bilgileri' : 'Başlangıç Bilgileri' }}
            </p>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
              Kilometre ve kadran fotoğrafı zorunludur
            </p>
          </div>

          <!-- KM Input -->
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Kilometre <span class="text-red-500">*</span>
            </label>
            <input
              v-model.number="odometerKm"
              type="number"
              min="1"
              inputmode="numeric"
              placeholder="Örn: 125430"
              class="w-full px-4 py-3 rounded-xl border text-base text-gray-900 dark:text-gray-100 bg-white dark:bg-white/5 placeholder-gray-400"
              :class="kmError ? 'border-red-400 focus:ring-red-400' : 'border-gray-300 dark:border-white/10 focus:ring-blue-400'"
            />
            <p v-if="kmError" class="mt-1 text-xs text-red-600">{{ kmError }}</p>
          </div>

          <!-- Photo label -->
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Kadran Fotoğrafı <span class="text-red-500">*</span>
            </label>

            <!-- Preview -->
            <div v-if="odometerPreview" class="relative">
              <img :src="odometerPreview" class="w-full max-h-48 object-cover rounded-lg" />
              <button @click="clearOdometer" class="absolute top-2 right-2 bg-black/50 text-white rounded-full p-1">
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
              </button>
            </div>

            <!-- Capture button -->
            <label v-if="!odometerPreview"
              class="flex items-center justify-center gap-2 w-full py-3 border-2 border-dashed rounded-lg cursor-pointer hover:border-blue-400 transition-colors"
              :class="photoError ? 'border-red-400 bg-red-50 dark:bg-red-900/10' : 'border-gray-300 dark:border-white/20'">
              <input ref="odometerInput" type="file" accept="image/*" capture="environment" class="hidden" @change="onOdometerSelected" />
              <svg class="w-5 h-5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 13a3 3 0 11-6 0 3 3 0 016 0z"/></svg>
              <span v-if="odometerCompressing" class="text-sm text-gray-500">Sıkıştırılıyor...</span>
              <span v-else class="text-sm text-gray-500 dark:text-gray-400">Fotoğraf Çek / Seç</span>
            </label>
            <p v-if="photoError" class="mt-1 text-xs text-red-600">{{ photoError }}</p>
          </div>

          <div class="flex gap-3 pt-1">
            <button @click="backFromOdometer" class="flex-1 py-2.5 border dark:border-white/10 rounded-lg text-gray-700 dark:text-gray-300 text-sm font-medium hover:bg-gray-50 dark:hover:bg-gray-800">
              Geri
            </button>
            <button @click="submitSession" :disabled="submitting"
              class="flex-1 py-2.5 rounded-lg text-white text-sm font-medium disabled:opacity-50"
              :class="mode === 'end' ? 'bg-gradient-to-br from-red-500 to-red-600 hover:from-red-400 hover:to-red-500' : 'bg-gradient-to-br from-blue-500 to-blue-600 hover:from-blue-400 hover:to-blue-500'">
              {{ submitting ? 'İşleniyor...' : (mode === 'end' ? 'Seferi Bitir' : 'Seferi Başlat') }}
            </button>
          </div>
        </div>
      </template>

      <!-- ── STEP: success ──────────────────────────────── -->
      <template v-else-if="step === 'success'">
        <div class="w-full rounded-xl bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-700 p-5 text-center">
          <div class="text-4xl mb-3">✅</div>
          <p class="text-green-800 dark:text-green-200 font-semibold">{{ successMessage }}</p>
          <router-link to="/driver" class="mt-4 inline-block text-sm text-green-700 dark:text-green-300 underline">
            Rotaya Dön
          </router-link>
        </div>
      </template>

      <!-- Error -->
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
import driverSessionService, { type TripShipmentDto } from '../services/driverSessionService';
import { ApiErrorUtils } from '../utils/apiError';
import { useLocationPermission } from '../composables/useLocationPermission';

const route = useRoute();
const mode = ref<'start' | 'end'>(route.query.mode === 'end' ? 'end' : 'start');
const step = ref<'scan' | 'irsaliye' | 'shipments' | 'odometer' | 'success'>('scan');

// İrsaliye adımı (yalnızca sefer başlat)
const detectedIrsaliyeNo = ref('');
const resolvedShipments = ref<TripShipmentDto[]>([]);
const resolving = ref(false);
const resolveError = ref('');

const { permissionState, settingsInstructions, checkPermission } = useLocationPermission();

const videoEl = ref<HTMLVideoElement | null>(null);
const canvasEl = ref<HTMLCanvasElement | null>(null);

const cameraLoading = ref(true);
const cameraError = ref('');
const gpsError = ref('');

let stream: MediaStream | null = null;
let scanInterval: ReturnType<typeof setInterval> | null = null;

const gpsPosition = ref<GeolocationPosition | null>(null);

const requestGps = () => {
  gpsError.value = '';
  navigator.geolocation.getCurrentPosition(
    pos => { gpsPosition.value = pos; },
    () => {
      gpsError.value = 'Konum izni gerekli. Lütfen tarayıcı ayarlarından izin verin.';
      checkPermission();
    },
    { enableHighAccuracy: true, timeout: 10000 }
  );
};

const detectedQr = ref('');
const detectedPlate = ref('');
const submitting = ref(false);
const successMessage = ref('');
const submitError = ref('');

// Odometer photo + KM
const odometerBase64 = ref('');
const odometerPreview = ref('');
const odometerCompressing = ref(false);
const odometerKm = ref<number | null>(null);
const kmError = ref('');
const photoError = ref('');

const startCamera = async () => {
  cameraLoading.value = true;
  cameraError.value = '';
  try {
    stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'environment' } });
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
    if (!videoEl.value || !canvasEl.value) return;
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
    if (!code) return;

    if (step.value === 'scan') {
      // Araç QR'ı
      if (detectedQr.value) return;
      if (code.data.startsWith('AKYILDIZ_VEHICLE_')) {
        detectedQr.value = code.data;
        const parts = code.data.split('_');
        detectedPlate.value = parts.slice(3).join(' ') || code.data;
      }
    } else if (step.value === 'irsaliye') {
      // İrsaliye QR'ı (GİB e-İrsaliye JSON: { ..., "no":"AKI...." })
      if (detectedIrsaliyeNo.value || resolving.value) return;
      const no = parseIrsaliyeNo(code.data);
      if (no) {
        detectedIrsaliyeNo.value = no;
        resolveShipments();
      }
    }
  }, 200);
};

function parseIrsaliyeNo(data: string): string | null {
  try {
    const obj = JSON.parse(data);
    if (obj && typeof obj.no === 'string' && obj.no.trim()) return obj.no.trim();
  } catch { /* JSON değilse regex dene */ }
  const m = data.match(/"no"\s*:\s*"([^"]+)"/);
  return m?.[1]?.trim() || null;
}

const onPlateConfirmed = () => {
  if (mode.value === 'end') {
    goToOdometerStep();
  } else {
    goToIrsaliyeStep();
  }
};

const goToIrsaliyeStep = () => {
  if (!gpsPosition.value) {
    gpsError.value = 'Konum bilgisi alınamadı. Lütfen GPS iznini kontrol edin.';
    return;
  }
  resolveError.value = '';
  detectedIrsaliyeNo.value = '';
  step.value = 'irsaliye'; // kamera açık kalır, döngü artık irsaliye okutur
};

const resolveShipments = async () => {
  resolving.value = true;
  resolveError.value = '';
  try {
    const res = await driverSessionService.resolveIrsaliye({
      qrCode: detectedQr.value,
      irsaliyeNo: detectedIrsaliyeNo.value,
    });
    resolvedShipments.value = res.shipments;
    step.value = 'shipments';
    stopCamera();
  } catch (e) {
    resolveError.value = ApiErrorUtils.getErrorMessage(e) || 'Sevkiyatlar çözümlenemedi.';
    detectedIrsaliyeNo.value = ''; // tekrar okutmaya izin ver
  } finally {
    resolving.value = false;
  }
};

const resetScan = () => {
  detectedQr.value = '';
  detectedPlate.value = '';
  detectedIrsaliyeNo.value = '';
  resolvedShipments.value = [];
  resolveError.value = '';
  resolving.value = false;
  submitting.value = false;
  submitError.value = '';
  successMessage.value = '';
  step.value = 'scan';
  clearOdometer();
  odometerKm.value = null;
  kmError.value = '';
  // Kamera önceki adımlarda durdurulmuş olabilir → yeniden başlat
  stopCamera();
  startCamera();
};

const goToOdometerStep = () => {
  if (!gpsPosition.value) {
    gpsError.value = 'Konum bilgisi alınamadı. Lütfen GPS iznini kontrol edin.';
    return;
  }
  step.value = 'odometer';
  stopCamera();
};

const backFromOdometer = () => {
  if (mode.value === 'start') {
    // Sefer başlat: sevkiyat onay adımına dön (kamera gerekmez)
    step.value = 'shipments';
  } else {
    // Sefer bitir: araç okutmaya dön
    step.value = 'scan';
    stopCamera();
    startCamera();
  }
};

function clearOdometer() {
  odometerBase64.value = '';
  odometerPreview.value = '';
  photoError.value = '';
}

function validateOdometerStep(): boolean {
  kmError.value = '';
  photoError.value = '';
  let valid = true;
  if (!odometerKm.value || odometerKm.value <= 0) {
    kmError.value = 'Kilometre bilgisi giriniz.';
    valid = false;
  }
  if (!odometerBase64.value) {
    photoError.value = 'Kadran fotoğrafı zorunludur.';
    valid = false;
  }
  return valid;
}

function onOdometerSelected(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;
  odometerCompressing.value = true;
  const reader = new FileReader();
  reader.onload = (e) => {
    const dataUrl = e.target?.result as string;
    const img = new Image();
    img.onload = () => {
      const MAX = 1200;
      let w = img.width, h = img.height;
      if (w > MAX || h > MAX) {
        if (w > h) { h = Math.round((h / w) * MAX); w = MAX; }
        else       { w = Math.round((w / h) * MAX); h = MAX; }
      }
      const canvas = document.createElement('canvas');
      canvas.width = w; canvas.height = h;
      canvas.getContext('2d')!.drawImage(img, 0, 0, w, h);
      const compressed = canvas.toDataURL('image/jpeg', 0.8);
      odometerBase64.value = compressed.split(',')[1] ?? '';
      odometerPreview.value = compressed;
      odometerCompressing.value = false;
    };
    img.src = dataUrl;
  };
  reader.readAsDataURL(file);
  input.value = '';
}

const submitSession = async () => {
  if (submitting.value) return;
  if (!validateOdometerStep()) return;
  submitting.value = true;
  submitError.value = '';
  try {
    const lat = gpsPosition.value!.coords.latitude;
    const lng = gpsPosition.value!.coords.longitude;

    if (mode.value === 'start') {
      const res = await driverSessionService.startSession({
        qrCode: detectedQr.value,
        latitude: lat,
        longitude: lng,
        startOdometerPhotoBase64: odometerBase64.value,
        startOdometerKm: odometerKm.value ?? undefined,
        irsaliyeNo: detectedIrsaliyeNo.value,
      });
      successMessage.value = `${res.vehiclePlateNumber} aracıyla sefer başlatıldı. ${res.shipmentCount} sevkiyat bağlandı.`;
    } else {
      const res = await driverSessionService.endSession({
        qrCode: detectedQr.value,
        latitude: lat,
        longitude: lng,
        endOdometerPhotoBase64: odometerBase64.value,
        endOdometerKm: odometerKm.value ?? undefined,
      });
      successMessage.value = `Sefer kapatıldı. Süre: ${res.totalDurationMinutes} dakika.`;
    }
    step.value = 'success';
  } catch (e) {
    submitError.value = ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.';
    // Baştan okutmaya dön
    detectedQr.value = '';
    detectedPlate.value = '';
    detectedIrsaliyeNo.value = '';
    resolvedShipments.value = [];
    step.value = 'scan';
    stopCamera();
    startCamera();
  } finally {
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
