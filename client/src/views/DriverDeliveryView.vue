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
        <h1 class="text-xl font-bold text-gray-900 dark:text-white">Teslimat Detayı</h1>
      </div>
      <!-- Project info card -->
      <div class="bg-white dark:bg-[#0f2744] rounded-xl shadow-sm border border-gray-200 dark:border-white/10 p-4 space-y-3">
        <div>
          <h2 class="text-lg font-semibold text-gray-900 dark:text-white">{{ shipment.projectName }}</h2>
          <p v-if="shipment.externalOrderNumber" class="text-sm text-gray-500 dark:text-gray-400">Sipariş: {{ shipment.externalOrderNumber }}</p>
        </div>

        <div v-if="shipment.projectAddress" class="flex items-start gap-2">
          <MapPinIcon class="w-4 h-4 text-gray-400 flex-shrink-0 mt-0.5" />
          <a :href="mapsUrl" target="_blank" rel="noopener"
             class="text-sm text-blue-600 dark:text-blue-400 hover:underline">
            {{ shipment.projectAddress }}
          </a>
        </div>

        <div v-if="stop?.contactName" class="flex items-center gap-2">
          <UserIcon class="w-4 h-4 text-gray-400 flex-shrink-0" />
          <span class="text-sm text-gray-700 dark:text-gray-300">{{ stop.contactName }}</span>
        </div>

        <div v-if="stop?.contactPhone" class="flex items-center gap-2">
          <PhoneIcon class="w-4 h-4 text-gray-400 flex-shrink-0" />
          <a :href="`tel:${stop.contactPhone}`"
             class="text-sm text-blue-600 dark:text-blue-400 hover:underline">
            {{ stop.contactPhone }}
          </a>
        </div>

        <div v-if="shipment.deliveryDate" class="flex items-center gap-2">
          <CalendarIcon class="w-4 h-4 text-gray-400 flex-shrink-0" />
          <span class="text-sm text-gray-700 dark:text-gray-300">{{ formatDate(shipment.deliveryDate) }}</span>
        </div>

        <div class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
          <ArchiveBoxIcon class="w-4 h-4 flex-shrink-0" />
          {{ shipment.lineCount }} kalem
        </div>
      </div>

      <!-- Not yet dispatched — loading not confirmed by operator -->
      <div v-if="shipment.status !== 'Delivered' && shipment.status !== 'Dispatched'"
           class="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-xl p-4">
        <div class="flex items-center gap-2 text-amber-700 dark:text-amber-400 font-medium mb-1">
          <svg class="w-5 h-5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126ZM12 15.75h.007v.008H12v-.008Z" /></svg>
          Araç yüklemesi henüz onaylanmamış
        </div>
        <p class="text-sm text-amber-600 dark:text-amber-500">
          Bu irsaliye teslim edilebilmek için operasyon yetkilisinin yüklemeyi onaylaması gerekir. Mevcut durum: <strong>{{ shipment.status }}</strong>
        </p>
      </div>

      <!-- Already delivered -->
      <div v-else-if="shipment.status === 'Delivered'"
           class="bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-xl p-4">
        <div class="flex items-center gap-2 text-green-700 dark:text-green-400 font-medium mb-1">
          <CheckCircleIcon class="w-5 h-5" />
          Teslim Edildi
        </div>
        <p v-if="shipment.deliveredAt" class="text-sm text-green-600 dark:text-green-500">
          {{ formatDateTime(shipment.deliveredAt) }}
        </p>
        <!-- Delivery photos (new multi-photo) -->
        <div v-if="deliveryPhotos.length" class="mt-3 grid grid-cols-3 gap-2">
          <img
            v-for="photo in deliveryPhotos"
            :key="photo.photoIndex"
            :src="photo.photoUrl"
            class="w-full aspect-square object-cover rounded-lg cursor-pointer"
            @click="lightboxSrc = photo.photoUrl"
          />
        </div>
        <!-- Legacy single photo -->
        <div v-else-if="legacyPhotoSrc" class="mt-3">
          <img
            :src="legacyPhotoSrc"
            class="w-full max-h-48 object-cover rounded-lg cursor-pointer"
            @click="lightboxSrc = legacyPhotoSrc"
          />
        </div>
      </div>

      <!-- Delivery form (only when Dispatched) -->
      <template v-else>
        <!-- Teslim edilecek kalemler -->
        <div v-if="shipment.lines?.length" class="rounded-lg overflow-hidden border border-gray-200 dark:border-gray-700">
          <div class="px-4 py-2 bg-gray-50 dark:bg-gray-800 text-sm font-medium text-gray-600 dark:text-gray-400">
            Teslim Edilecek Kalemler
          </div>
          <div
            v-for="line in shipment.lines"
            :key="line.stockCode"
            class="flex justify-between px-4 py-2.5 border-t border-gray-100 dark:border-gray-700"
          >
            <span class="text-gray-900 dark:text-gray-100 text-sm">{{ line.stockName }}</span>
            <span class="text-gray-500 dark:text-gray-400 text-sm font-medium whitespace-nowrap ml-2">{{ line.orderedQty }} {{ line.unit }}</span>
          </div>
        </div>

        <div class="bg-white dark:bg-[#0f2744] rounded-xl shadow-sm border border-gray-200 dark:border-white/10 p-4 space-y-4">
          <h3 class="font-medium text-gray-900 dark:text-white">Teslim Bilgisi</h3>

          <!-- Recipient -->
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Teslim Alan Kişi
            </label>
            <input
              v-model="form.deliveryRecipient"
              type="text"
              placeholder="Adı Soyadı"
              class="w-full px-3 py-2.5 rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white placeholder-gray-400 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <!-- Note -->
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Not <span class="text-gray-400 font-normal">(isteğe bağlı)</span>
            </label>
            <textarea
              v-model="form.deliveryNote"
              rows="2"
              placeholder="Teslimat notu..."
              class="w-full px-3 py-2.5 rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white placeholder-gray-400 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            ></textarea>
          </div>

          <!-- Photos (up to 5) -->
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
              Teslim Fotoğrafları <span class="text-red-500">*</span>
              <span class="text-gray-400 font-normal ml-1">({{ form.photos.length }}/5)</span>
            </label>

            <!-- Thumbnail grid -->
            <div v-if="form.photos.length" class="grid grid-cols-3 gap-2 mb-2">
              <div
                v-for="(photo, idx) in form.photos"
                :key="idx"
                class="relative aspect-square"
              >
                <img
                  :src="photo.preview"
                  class="w-full h-full object-cover rounded-lg cursor-pointer"
                  @click="lightboxSrc = photo.preview"
                />
                <button
                  @click="removePhoto(idx)"
                  class="absolute top-1 right-1 bg-black/50 text-white rounded-full p-0.5"
                >
                  <XMarkIcon class="w-3.5 h-3.5" />
                </button>
              </div>
            </div>

            <!-- Add photo button (hidden when 5 photos taken) -->
            <label
              v-if="form.photos.length < 5"
              class="flex items-center justify-center gap-2 w-full py-3 border-2 border-dashed border-gray-300 dark:border-white/20 rounded-lg cursor-pointer hover:border-blue-400 dark:hover:border-blue-500 transition-colors"
            >
              <input
                ref="photoInput"
                type="file"
                accept="image/*"
                capture="environment"
                class="hidden"
                @change="onPhotoSelected"
              />
              <span v-if="form.photoCompressing" class="text-sm text-gray-500 dark:text-gray-400 flex items-center gap-2">
                <span class="w-4 h-4 border-2 border-gray-400 border-t-transparent rounded-full animate-spin"></span>
                Sıkıştırılıyor...
              </span>
              <template v-else>
                <CameraIcon class="w-5 h-5 text-gray-400" />
                <span class="text-sm text-gray-500 dark:text-gray-400">
                  {{ form.photos.length === 0 ? 'Fotoğraf Çek / Seç' : 'Fotoğraf Ekle' }}
                </span>
              </template>
            </label>
          </div>
        </div>

        <!-- Submit button -->
        <button
          @click="markDelivered"
          :disabled="submitting"
          class="w-full py-3.5 bg-green-600 hover:bg-green-700 disabled:bg-green-400 text-white font-semibold rounded-xl transition-colors flex items-center justify-center gap-2"
        >
          <span v-if="submitting" class="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          <CheckCircleIcon v-else class="w-5 h-5" />
          {{ submitting ? 'Kaydediliyor...' : 'Teslim Edildi Olarak İşaretle' }}
        </button>
      </template>
    </template>

    <!-- Lightbox -->
    <div
      v-if="lightboxSrc"
      class="fixed inset-0 z-50 bg-black/90 flex items-center justify-center p-4"
      @click="lightboxSrc = null"
    >
      <img :src="lightboxSrc" class="max-w-full max-h-full object-contain rounded-lg" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  MapPinIcon,
  UserIcon,
  PhoneIcon,
  CalendarIcon,
  ArchiveBoxIcon,
  CheckCircleIcon,
  CameraIcon,
  XMarkIcon,
  ArrowLeftIcon,
} from '@heroicons/vue/24/outline';
import driverService, { type StopShipmentDto, type DeliveryStopDto, type ShipmentLineDto } from '../services/driverService';
import shipmentService, { type DeliveryPhoto } from '../services/shipmentService';
import { useNotificationStore } from '../stores/notification';
import { useDeliveryQueue } from '../composables/useDeliveryQueue';
import { getPhotoUrl } from '../utils/photoUrl';

const deliveryPosition = ref<{ latitude: number; longitude: number } | null>(null);
const { isOnline, enqueue } = useDeliveryQueue();

function captureLocation() {
  if (!navigator.geolocation) return;
  navigator.geolocation.getCurrentPosition(
    (pos) => {
      deliveryPosition.value = {
        latitude: pos.coords.latitude,
        longitude: pos.coords.longitude,
      };
    },
    () => { /* GPS optional — proceed without */ },
    { enableHighAccuracy: true, timeout: 10000 }
  );
}

const route = useRoute();
const router = useRouter();
const notify = useNotificationStore();

interface ShipmentView extends StopShipmentDto {
  projectName: string;
  projectAddress?: string;
  lines: ShipmentLineDto[];
}

const shipmentId = Number(route.params.id);
const shipment = ref<ShipmentView | null>(null);
const stop = ref<DeliveryStopDto | null>(null);
const loading = ref(false);
const error = ref('');
const submitting = ref(false);
const lightboxSrc = ref<string | null>(null);

interface PhotoEntry {
  base64: string;
  preview: string;
}

const form = ref({
  deliveryRecipient: '',
  deliveryNote: '',
  photos: [] as PhotoEntry[],
  photoCompressing: false,
});

const mapsUrl = computed(() => {
  const s = stop.value;
  if (s?.projectLatitude != null && s?.projectLongitude != null)
    return `https://www.google.com/maps/dir/?api=1&destination=${s.projectLatitude},${s.projectLongitude}&travelmode=driving`;
  const addr = encodeURIComponent(shipment.value?.projectAddress ?? '');
  return `https://www.google.com/maps/search/?api=1&query=${addr}`;
});

const deliveryPhotos = computed<DeliveryPhoto[]>(() =>
  (shipment.value as any)?.deliveryPhotos ?? []
);
const legacyPhotoSrc = computed<string | null>(() =>
  shipment.value
    ? getPhotoUrl((shipment.value as any).deliveryPhotoPath, shipment.value.deliveryPhotoBase64)
    : null
);

async function load() {
  loading.value = true;
  error.value = '';
  try {
    const routeData = await driverService.getRoute();
    for (const s of routeData.stops) {
      const sh = s.shipments.find(sh => sh.id === shipmentId);
      if (sh) {
        stop.value = s;
        shipment.value = { ...sh, projectName: s.projectName, projectAddress: s.projectAddress };
        // Pre-fill teslim alan kişi (stop-level contact info)
        if (s.contactName) {
          form.value.deliveryRecipient = s.contactName.split(',')[0]?.trim() || '';
        }
        break;
      }
    }
    if (!shipment.value) error.value = 'Sevkiyat bulunamadı.';
  } catch {
    error.value = 'Yüklenemedi. Lütfen tekrar deneyin.';
  } finally {
    loading.value = false;
  }
}

function removePhoto(idx: number) {
  form.value.photos.splice(idx, 1);
}

function onPhotoSelected(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file || form.value.photos.length >= 5) return;

  form.value.photoCompressing = true;

  const reader = new FileReader();
  reader.onload = (e) => {
    const dataUrl = e.target?.result as string;
    const img = new Image();
    img.onload = () => {
      const MAX = 1000;
      let w = img.width;
      let h = img.height;
      if (w > MAX || h > MAX) {
        if (w > h) { h = Math.round((h / w) * MAX); w = MAX; }
        else       { w = Math.round((w / h) * MAX); h = MAX; }
      }
      const canvas = document.createElement('canvas');
      canvas.width  = w;
      canvas.height = h;
      canvas.getContext('2d')!.drawImage(img, 0, 0, w, h);
      const compressed = canvas.toDataURL('image/jpeg', 0.75);
      form.value.photos.push({
        base64: compressed.split(',')[1] ?? '',
        preview: compressed,
      });
      form.value.photoCompressing = false;
    };
    img.src = dataUrl;
  };
  reader.readAsDataURL(file);

  // reset so same file can be re-selected
  input.value = '';
}

async function markDelivered() {
  if (!form.value.deliveryRecipient?.trim()) {
    notify.add('Lütfen teslim alan kişi bilgisini giriniz.', 'warning');
    return;
  }
  if (form.value.photos.length === 0) {
    notify.add('En az bir teslim fotoğrafı zorunludur.', 'warning');
    return;
  }
  if (submitting.value) return;
  submitting.value = true;

  const photosBase64 = form.value.photos.map(p => p.base64);

  if (!isOnline.value) {
    // Offline — queue for later sync
    await enqueue({
      shipmentId,
      deliveryRecipient: form.value.deliveryRecipient || undefined,
      deliveryNote: form.value.deliveryNote || undefined,
      photosBase64,
      latitude: deliveryPosition.value?.latitude,
      longitude: deliveryPosition.value?.longitude,
    });
    notify.add('Çevrimdışısınız. Teslimat kaydedildi — internet bağlantısı gelince otomatik gönderilecek.', 'warning');
    submitting.value = false;
    router.back();
    return;
  }

  try {
    await shipmentService.markDelivered(
      shipmentId,
      form.value.deliveryNote || undefined,
      form.value.deliveryRecipient || undefined,
      photosBase64,
      undefined,
      deliveryPosition.value?.latitude,
      deliveryPosition.value?.longitude,
    );
    notify.add('Sevkiyat teslim edildi olarak işaretlendi.', 'success');
    router.back();
  } catch (err: unknown) {
    const { ApiErrorUtils } = await import('../utils/apiError');
    const msg = ApiErrorUtils.getErrorMessage(err);
    notify.add(msg || 'Bir hata oluştu, lütfen tekrar deneyin.', 'error');
  } finally {
    submitting.value = false;
  }
}

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric' });
}
function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

onMounted(() => {
  load();
  captureLocation();
});
</script>
