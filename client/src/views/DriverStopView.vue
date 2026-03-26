<template>
  <div class="space-y-4 pb-8">

    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-16">
      <div class="w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- Error -->
    <div v-else-if="error"
         class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl p-4 text-red-700 dark:text-red-400 text-sm">
      {{ error }}
    </div>

    <template v-else-if="stop">

      <!-- Stop header card -->
      <div class="bg-white dark:bg-[#0f2744] rounded-xl shadow-sm border border-gray-200 dark:border-white/10 p-4 space-y-3">
        <div class="flex items-start gap-3">
          <!-- Stop number badge -->
          <div
            class="flex-shrink-0 w-10 h-10 rounded-full flex items-center justify-center text-sm font-bold"
            :class="stop.isFullyDelivered
              ? 'bg-green-100 dark:bg-green-900/40 text-green-600 dark:text-green-400'
              : 'bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300'"
          >
            <CheckCircleIcon v-if="stop.isFullyDelivered" class="w-5 h-5" aria-hidden="true" />
            <span v-else>{{ stop.stopNumber }}</span>
          </div>
          <div>
            <h2 class="text-lg font-semibold text-gray-900 dark:text-white leading-tight">{{ stop.projectName }}</h2>
            <p v-if="stop.zoneName" class="text-xs text-gray-400 mt-0.5">{{ stop.zoneName }}</p>
          </div>
        </div>

        <!-- Summary pills -->
        <div class="flex gap-3 text-sm">
          <div class="flex items-center gap-1 text-gray-500 dark:text-gray-400">
            <DocumentTextIcon class="w-4 h-4" aria-hidden="true" />
            <span>{{ stop.shipments.length }} irsaliye</span>
          </div>
          <div class="flex items-center gap-1 text-gray-500 dark:text-gray-400">
            <ArchiveBoxIcon class="w-4 h-4" aria-hidden="true" />
            <span>{{ stop.totalLineCount }} kalem</span>
          </div>
          <div v-if="stop.isFullyDelivered" class="flex items-center gap-1 text-green-600 dark:text-green-400 font-medium">
            <CheckCircleIcon class="w-4 h-4" aria-hidden="true" />
            Tamamlandı
          </div>
        </div>

        <!-- Address + navigation -->
        <div class="flex items-start gap-2">
          <MapPinIcon class="w-4 h-4 text-gray-400 flex-shrink-0 mt-0.5" aria-hidden="true" />
          <div class="flex-1 min-w-0">
            <a
              v-if="stop.projectAddress || stop.projectLatitude"
              :href="mapsUrl"
              target="_blank"
              rel="noopener"
              class="text-sm text-blue-600 dark:text-blue-400 hover:underline leading-snug block"
            >
              <span v-if="stop.projectLatitude">
                📍 {{ stop.projectLatitude?.toFixed(5) }}, {{ stop.projectLongitude?.toFixed(5) }}
                <span v-if="stop.projectAddress" class="text-gray-400 text-xs ml-1">({{ stop.projectAddress }})</span>
              </span>
              <span v-else>{{ stop.projectAddress }}</span>
            </a>
            <span v-else class="text-sm text-gray-400 italic">Adres tanımsız</span>
          </div>
          <button
            @click="saveCurrentLocation"
            :disabled="savingLocation"
            class="flex-shrink-0 flex items-center gap-1 text-xs px-2.5 py-1.5 rounded-lg border transition-colors"
            :class="stop.projectLatitude
              ? 'border-green-300 dark:border-green-700 text-green-700 dark:text-green-400 bg-green-50 dark:bg-green-900/20 hover:bg-green-100 dark:hover:bg-green-900/30'
              : 'border-gray-300 dark:border-white/20 text-gray-600 dark:text-gray-400 bg-white dark:bg-white/5 hover:bg-gray-50 dark:hover:bg-white/10'"
            :title="stop.projectLatitude ? 'Konumu Güncelle' : 'Mevcut Konumu Kaydet'"
          >
            <span v-if="savingLocation" class="w-3 h-3 border-2 border-current border-t-transparent rounded-full animate-spin"></span>
            <MapPinIcon v-else class="w-3 h-3" aria-hidden="true" />
            <span>{{ stop.projectLatitude ? 'Güncelle' : 'Kaydet' }}</span>
          </button>
        </div>

        <!-- Contact from first shipment -->
        <div v-if="firstShipment?.teslimAlacakKisiler" class="flex items-center gap-2">
          <UserIcon class="w-4 h-4 text-gray-400 flex-shrink-0" aria-hidden="true" />
          <span class="text-sm text-gray-700 dark:text-gray-300">{{ firstShipment.teslimAlacakKisiler }}</span>
        </div>
        <div v-if="firstShipment?.teslimAlacakTelefon" class="flex items-center gap-2">
          <PhoneIcon class="w-4 h-4 text-gray-400 flex-shrink-0" aria-hidden="true" />
          <a
            :href="`tel:${firstShipment.teslimAlacakTelefon}`"
            class="text-sm text-blue-600 dark:text-blue-400 hover:underline"
          >
            {{ firstShipment.teslimAlacakTelefon }}
          </a>
        </div>
      </div>

      <!-- Shipment cards -->
      <div
        v-for="shipment in stop.shipments"
        :key="shipment.id"
        class="bg-white dark:bg-[#0f2744] rounded-xl shadow-sm border border-gray-200 dark:border-white/10 overflow-hidden"
      >
        <!-- Shipment header -->
        <div
          class="flex items-center justify-between px-4 py-3 cursor-pointer select-none"
          :class="[
             shipment.status === 'Delivered' ? 'bg-green-50 dark:bg-green-900/20' : '',
             shipment.status === 'ReturnedToWarehouse' ? 'bg-purple-50 dark:bg-purple-900/20' : ''
          ]"
          @click="toggleExpand(shipment.id)"
        >
          <div class="flex items-center gap-2">
            <CheckCircleIcon
              v-if="shipment.status === 'Delivered'"
              class="w-5 h-5 text-green-500 flex-shrink-0"
              aria-hidden="true"
            />
            <ClipboardDocumentListIcon
              v-else
              class="w-5 h-5 text-blue-500 flex-shrink-0"
              aria-hidden="true"
            />
            <div>
              <p class="font-medium text-sm text-gray-900 dark:text-white">
                {{ shipment.talepNo || ('#' + shipment.id) }}
              </p>
              <p v-if="shipment.irsaliyeNo" class="text-xs text-gray-400">İrsaliye: {{ shipment.irsaliyeNo }}</p>
            </div>
          </div>
          <div class="flex items-center gap-2">
            <span class="text-xs text-gray-400">{{ shipment.lineCount }} kalem</span>
            <ChevronDownIcon
              class="w-4 h-4 text-gray-400 transition-transform"
              :class="{ 'rotate-180': expandedIds.has(shipment.id) }"
              aria-hidden="true"
            />
          </div>
        </div>

        <div v-if="expandedIds.has(shipment.id) && shipment.status === 'Delivered'" class="px-4 pb-4 space-y-3 border-t border-gray-100 dark:border-white/10 pt-3">
          <div class="flex items-center gap-2 text-green-700 dark:text-green-400 text-sm font-medium">
            <CheckCircleIcon class="w-4 h-4" aria-hidden="true" />
            Teslim Edildi
            <span v-if="shipment.deliveredAt" class="font-normal text-green-600 dark:text-green-500">
              · {{ formatDateTime(shipment.deliveredAt) }}
            </span>
          </div>
          <p v-if="shipment.deliveryRecipient" class="text-sm text-gray-700 dark:text-gray-300">
            <span class="text-gray-400">Teslim Alan: </span>{{ shipment.deliveryRecipient }}
          </p>
          <p v-if="shipment.deliveryNote" class="text-sm text-gray-700 dark:text-gray-300">
            <span class="text-gray-400">Not: </span>{{ shipment.deliveryNote }}
          </p>
          <div v-if="shipment.deliveryPhotoBase64">
            <img
              :src="`data:image/jpeg;base64,${shipment.deliveryPhotoBase64}`"
              class="w-full max-h-40 object-cover rounded-lg cursor-pointer"
              @click="lightboxSrc = `data:image/jpeg;base64,${shipment.deliveryPhotoBase64}`"
            />
          </div>
        </div>

        <!-- Expanded: Action Buttons (Pending/Dispatched) -->
        <div v-if="expandedIds.has(shipment.id) && shipment.status !== 'Delivered' && shipment.status !== 'ReturnedToWarehouse'" class="px-4 pb-4 space-y-3 border-t border-gray-100 dark:border-white/10 pt-3">
          <div class="flex gap-3">
            <button
              @click.stop="router.push({ name: 'DriverDelivery', params: { id: shipment.id } })"
              class="flex-1 py-3 bg-green-600 hover:bg-green-700 text-white text-sm font-semibold rounded-lg transition-colors flex items-center justify-center gap-2"
            >
              <CheckCircleIcon class="w-4 h-4" aria-hidden="true" />
              Teslim Et
            </button>
            <button
              @click.stop="openReturnModal(shipment.id)"
              class="flex-1 py-3 bg-orange-600 hover:bg-orange-700 text-white text-sm font-semibold rounded-lg transition-colors flex items-center justify-center gap-2"
            >
              <ArrowUturnLeftIcon class="w-4 h-4" aria-hidden="true" />
              İade Et
            </button>
          </div>
        </div>
      </div>

      <!-- Mark all delivered (bulk) -->
      <div v-if="pendingShipments.length > 1" class="sticky bottom-4">
        <button
          @click="showBulkModal = true"
          :disabled="bulkSubmitting"
          class="w-full py-3.5 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white font-semibold rounded-xl shadow-lg transition-colors flex items-center justify-center gap-2"
        >
          <CheckCircleIcon class="w-5 h-5" aria-hidden="true" />
          Tümünü Teslim Et ({{ pendingShipments.length }} irsaliye)
        </button>
      </div>

    </template>

    <!-- Bulk delivery modal -->
    <Teleport to="body">
      <div v-if="showBulkModal" class="fixed inset-0 z-50 flex items-end justify-center">
        <div class="absolute inset-0 bg-black/60" @click="showBulkModal = false"></div>
        <div class="relative w-full max-w-lg bg-white dark:bg-[#0f2744] rounded-t-2xl p-5 space-y-4 max-h-[80vh] overflow-y-auto">
          <h3 class="text-lg font-semibold text-gray-900 dark:text-white">
            Tüm İrsaliyeleri Teslim Et
          </h3>
          <p class="text-sm text-gray-500 dark:text-gray-400">
            {{ pendingShipments.length }} bekleyen irsaliye aynı anda teslim edildi olarak işaretlenecek.
          </p>

          <!-- Recipient -->
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Teslim Alan Kişi</label>
            <input
              v-model="bulkForm.deliveryRecipient"
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
              v-model="bulkForm.deliveryNote"
              rows="2"
              placeholder="Teslimat notu..."
              class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white placeholder-gray-400 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            ></textarea>
          </div>

          <!-- Photo -->
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Fotoğraf <span class="text-gray-400 font-normal">(isteğe bağlı)</span>
            </label>
            <div v-if="bulkForm.photoPreview" class="mb-2 relative">
              <img
                :src="bulkForm.photoPreview"
                class="w-full max-h-40 object-cover rounded-lg cursor-pointer"
                @click="lightboxSrc = bulkForm.photoPreview"
              />
              <button
                @click="bulkForm.photoPreview = ''; bulkForm.photoBase64 = ''"
                class="absolute top-2 right-2 bg-black/50 text-white rounded-full p-1"
              >
                <XMarkIcon class="w-4 h-4" />
              </button>
            </div>
            <label
              v-if="!bulkForm.photoPreview"
              class="flex items-center justify-center gap-2 w-full py-3 border-2 border-dashed border-gray-300 dark:border-white/20 rounded-lg cursor-pointer hover:border-blue-400 transition-colors"
            >
              <input
                type="file"
                accept="image/*"
                capture="environment"
                class="hidden"
                @change="onBulkPhotoSelected"
              />
              <span v-if="bulkForm.photoCompressing" class="text-sm text-gray-500 flex items-center gap-2">
                <span class="w-4 h-4 border-2 border-gray-400 border-t-transparent rounded-full animate-spin"></span>
                Sıkıştırılıyor...
              </span>
              <template v-else>
                <CameraIcon class="w-5 h-5 text-gray-400" aria-hidden="true" />
                <span class="text-sm text-gray-500 dark:text-gray-400">Fotoğraf Çek / Seç</span>
              </template>
            </label>
          </div>

          <div class="flex gap-3 pt-1">
            <button
              @click="showBulkModal = false"
              class="flex-1 py-3 border border-gray-300 dark:border-white/20 text-gray-700 dark:text-gray-300 font-medium rounded-xl hover:bg-gray-50 dark:hover:bg-white/5 transition-colors"
            >
              İptal
            </button>
            <button
              @click="markAllDelivered"
              :disabled="bulkSubmitting"
              class="flex-2 flex-1 py-3 bg-green-600 hover:bg-green-700 disabled:bg-green-400 text-white font-semibold rounded-xl transition-colors flex items-center justify-center gap-2"
            >
              <span v-if="bulkSubmitting" class="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              <CheckCircleIcon v-else class="w-5 h-5" aria-hidden="true" />
              {{ bulkSubmitting ? 'Kaydediliyor...' : 'Tümünü Teslim Et' }}
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- Lightbox -->
    <div
      v-if="lightboxSrc"
      class="fixed inset-0 z-[60] bg-black/90 flex items-center justify-center p-4"
      @click="lightboxSrc = null"
    >
      <img :src="lightboxSrc" class="max-w-full max-h-full object-contain rounded-lg" />
    </div>
  </div>

  <!-- Return modal -->
  <Teleport to="body">
    <div v-if="showReturnModal" class="fixed inset-0 z-50 flex items-end justify-center">
      <div class="absolute inset-0 bg-black/60" @click="closeReturnModal"></div>
      <div class="relative w-full max-w-lg bg-white dark:bg-[#0f2744] rounded-t-2xl max-h-[90vh] flex flex-col">

        <!-- Header -->
        <div class="flex items-center justify-between px-5 pt-5 pb-3 border-b border-gray-100 dark:border-white/10 flex-shrink-0">
          <div>
            <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Kısmi / Tam İade</h3>
            <p v-if="returnShipmentInfo" class="text-xs text-gray-400 mt-0.5">
              {{ returnShipmentInfo.talepNo || ('#' + returnShipmentInfo.id) }}
            </p>
          </div>
          <button @click="closeReturnModal" class="p-2 rounded-lg text-gray-400 hover:bg-gray-100 dark:hover:bg-white/10">
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M6 18 18 6M6 6l12 12"/></svg>
          </button>
        </div>

        <!-- Loading lines -->
        <div v-if="returnLoading" class="flex justify-center py-10">
          <div class="w-7 h-7 border-4 border-orange-500 border-t-transparent rounded-full animate-spin"></div>
        </div>

        <!-- Lines -->
        <div v-else class="overflow-y-auto flex-1 px-5 py-4 space-y-3">

          <!-- Bulk fill toolbar -->
          <div class="flex items-center gap-2 p-3 bg-orange-50 dark:bg-orange-900/20 rounded-xl border border-orange-200 dark:border-orange-800">
            <div class="flex-1">
              <label class="block text-xs text-orange-700 dark:text-orange-400 font-medium mb-1">Toplu Sebep</label>
              <select
                v-model="bulkReturnReason"
                class="w-full px-2 py-1.5 text-sm rounded-lg border border-orange-300 dark:border-orange-700 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-orange-400"
              >
                <option :value="5">Müşteri reddetti</option>
                <option :value="1">Hasarlı / Kırık</option>
                <option :value="2">Fazla Yükleme</option>
                <option :value="3">Yanlış Ürün</option>
                <option :value="4">Proje Kapalıydı</option>
                <option :value="99">Diğer</option>
              </select>
            </div>
            <button
              @click="fillAllLines"
              class="flex-shrink-0 mt-5 px-3 py-1.5 bg-orange-600 hover:bg-orange-700 text-white text-sm font-semibold rounded-lg transition-colors whitespace-nowrap"
            >
              Tamamını İade Et
            </button>
          </div>

          <p class="text-xs text-gray-400">Kısmi iade için aşağıdaki miktarları düzenleyebilirsiniz. Sıfır bırakılan kalemler iade edilmez.</p>

          <div
            v-for="line in returnLines"
            :key="line.shipmentLineId"
            class="bg-gray-50 dark:bg-white/5 rounded-xl p-3 space-y-2"
          >
            <!-- Stock info -->
            <div class="flex items-start justify-between gap-2">
              <div class="flex-1 min-w-0">
                <p class="text-sm font-medium text-gray-900 dark:text-white leading-tight">{{ line.stockName }}</p>
                <p class="text-xs text-gray-400">{{ line.stockCode }}</p>
              </div>
              <div class="text-right text-xs text-gray-500 dark:text-gray-400 flex-shrink-0">
                <div>Teslim: <span class="font-medium">{{ line.maxQty }}</span></div>
                <div v-if="line.alreadyReturned > 0" class="text-orange-500">İade edildi: {{ line.alreadyReturned }}</div>
              </div>
            </div>

            <!-- Qty + Reason row -->
            <div class="flex gap-2 items-center">
              <div class="w-24 flex-shrink-0">
                <label class="block text-xs text-gray-500 dark:text-gray-400 mb-1">İade Miktarı</label>
                <input
                  v-model.number="line.returnedQty"
                  type="number"
                  min="0"
                  :max="line.maxQty - line.alreadyReturned"
                  step="1"
                  class="w-full px-2 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-orange-400"
                  :class="line.returnedQty > 0 ? 'border-orange-400' : ''"
                />
              </div>
              <div class="flex-1">
                <label class="block text-xs text-gray-500 dark:text-gray-400 mb-1">Sebep</label>
                <select
                  v-model="line.returnReason"
                  :disabled="line.returnedQty === 0"
                  class="w-full px-2 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-orange-400 disabled:opacity-40"
                >
                  <option :value="5">Müşteri reddetti</option>
                  <option :value="1">Hasarlı / Kırık</option>
                  <option :value="2">Fazla Yükleme</option>
                  <option :value="3">Yanlış Ürün</option>
                  <option :value="4">Proje Kapalıydı</option>
                  <option :value="99">Diğer</option>
                </select>
              </div>
            </div>
          </div>

          <!-- Global note -->
          <div class="pt-1">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Genel Not <span class="text-gray-400 font-normal">(isteğe bağlı)</span>
            </label>
            <textarea
              v-model="returnNote"
              rows="2"
              placeholder="Açıklama ekleyin..."
              class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white placeholder-gray-400 text-sm focus:outline-none focus:ring-2 focus:ring-orange-400 resize-none"
            ></textarea>
          </div>
        </div>

        <!-- Footer -->
        <div v-if="!returnLoading" class="flex gap-3 px-5 py-4 border-t border-gray-100 dark:border-white/10 flex-shrink-0">
          <button
            @click="closeReturnModal"
            class="flex-1 py-3 border border-gray-300 dark:border-white/20 text-gray-700 dark:text-gray-300 font-medium rounded-xl hover:bg-gray-50 dark:hover:bg-white/5 transition-colors text-sm"
          >
            İptal
          </button>
          <button
            @click="submitReturn"
            :disabled="returnSubmitting || returnLines.every(l => l.returnedQty === 0)"
            class="flex-2 flex-1 py-3 bg-orange-600 hover:bg-orange-700 disabled:bg-orange-300 text-white font-semibold rounded-xl transition-colors flex items-center justify-center gap-2 text-sm"
          >
            <span v-if="returnSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            <ArrowUturnLeftIcon v-else class="w-4 h-4" aria-hidden="true" />
            {{ returnSubmitting ? 'Kaydediliyor...' : `İade Kaydet (${returnLines.filter(l => l.returnedQty > 0).length} kalem)` }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>

</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  MapPinIcon,
  UserIcon,
  PhoneIcon,
  CheckCircleIcon,
  ArchiveBoxIcon,
  DocumentTextIcon,
  ChevronDownIcon,
  CameraIcon,
  XMarkIcon,
  ArrowUturnLeftIcon,
} from '@heroicons/vue/24/outline';
import driverService, { type DeliveryStopDto, type StopShipmentDto } from '../services/driverService';
import shipmentService from '../services/shipmentService';
import { useNotificationStore } from '../stores/notification';

interface ShipmentForm {
  deliveryRecipient: string;
  deliveryNote: string;
  photoBase64: string;
  photoPreview: string;
  photoCompressing: boolean;
}

const route  = useRoute();
const router = useRouter();
const notify = useNotificationStore();

const projectId = Number(route.params.projectId);
const stop      = ref<DeliveryStopDto | null>(null);
const loading   = ref(false);
const error     = ref('');
const lightboxSrc = ref<string | null>(null);

// Per-shipment expand state
const expandedIds   = reactive(new Set<number>());

// Bulk deliver
const showBulkModal  = ref(false);
const bulkSubmitting = ref(false);
const bulkForm = reactive<ShipmentForm>({
  deliveryRecipient: '',
  deliveryNote: '',
  photoBase64: '',
  photoPreview: '',
  photoCompressing: false,
});

const firstShipment = computed(() => stop.value?.shipments[0]);
const pendingShipments = computed(() =>
  stop.value?.shipments.filter(s => s.status !== 'Delivered' && s.status !== 'ReturnedToWarehouse') ?? []
);

const savingLocation = ref(false);

const mapsUrl = computed(() => {
  const s = stop.value;
  if (!s) return '#';
  const destination = (s.projectLatitude != null && s.projectLongitude != null)
    ? `${s.projectLatitude},${s.projectLongitude}`
    : encodeURIComponent(s.projectAddress ?? '');
  return `https://www.google.com/maps/dir/?api=1&destination=${destination}&travelmode=driving`;
});

async function saveCurrentLocation() {
  if (savingLocation.value || !stop.value) return;
  if (!navigator.geolocation) {
    notify.add('Bu cihaz konum desteklemiyor.', 'error');
    return;
  }
  savingLocation.value = true;
  try {
    const pos = await new Promise<GeolocationPosition>((resolve, reject) =>
      navigator.geolocation.getCurrentPosition(resolve, reject, { enableHighAccuracy: true, timeout: 10000 })
    );
    const { latitude, longitude } = pos.coords;
    await driverService.saveProjectLocation(stop.value.projectId, latitude, longitude);
    // Update local state so map link works immediately
    (stop.value as any).projectLatitude = latitude;
    (stop.value as any).projectLongitude = longitude;
    notify.add('Konum kaydedildi.', 'success');
  } catch (e: any) {
    const msg = e?.code === 1 ? 'Konum izni reddedildi.' : 'Konum alınamadı.';
    notify.add(msg, 'error');
  } finally {
    savingLocation.value = false;
  }
}

function initForms(s: DeliveryStopDto) {
  for (const shipment of s.shipments) {
    // Auto-expand pending shipments if there's only one
    if (shipment.status !== 'Delivered' && s.shipments.length === 1) {
      expandedIds.add(shipment.id);
    }
  }
}

async function load() {
  loading.value = true;
  error.value = '';
  try {
    const routeData = await driverService.getRoute();
    const found = routeData.stops.find(s => s.projectId === projectId);
    if (!found) {
      error.value = 'Teslimat noktası bulunamadı.';
      return;
    }
    stop.value = found;
    initForms(found);
  } catch {
    error.value = 'Yüklenemedi. Lütfen tekrar deneyin.';
  } finally {
    loading.value = false;
  }
}

function toggleExpand(id: number) {
  if (expandedIds.has(id)) expandedIds.delete(id);
  else expandedIds.add(id);
}

function compressPhoto(dataUrl: string, onDone: (base64: string, preview: string) => void) {
  const img = new Image();
  img.onload = () => {
    const MAX = 1000;
    let w = img.width, h = img.height;
    if (w > MAX || h > MAX) {
      if (w > h) { h = Math.round((h / w) * MAX); w = MAX; }
      else       { w = Math.round((w / h) * MAX); h = MAX; }
    }
    const canvas = document.createElement('canvas');
    canvas.width = w; canvas.height = h;
    canvas.getContext('2d')!.drawImage(img, 0, 0, w, h);
    const compressed = canvas.toDataURL('image/jpeg', 0.75);
    onDone(compressed.split(',')[1] ?? '', compressed);
  };
  img.src = dataUrl;
}

function onBulkPhotoSelected(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;
  bulkForm.photoCompressing = true;
  const reader = new FileReader();
  reader.onload = (e) => {
    compressPhoto(e.target?.result as string, (base64, preview) => {
      bulkForm.photoBase64 = base64;
      bulkForm.photoPreview = preview;
      bulkForm.photoCompressing = false;
    });
  };
  reader.readAsDataURL(file);
  input.value = '';
}

// Single delivery method removed, since we route to DriverDeliveryView now
async function markAllDelivered() {
  if (!bulkForm.deliveryRecipient?.trim()) {
    notify.add('Lütfen teslim alan kişi bilgisini giriniz.', 'warning');
    return;
  }
  bulkSubmitting.value = true;
  try {
    for (const shipment of pendingShipments.value) {
      await shipmentService.markDelivered(
        shipment.id,
        bulkForm.deliveryNote || undefined,
        bulkForm.deliveryRecipient || undefined,
        bulkForm.photoBase64 || undefined,
      );
    }
    notify.add(`${pendingShipments.value.length} irsaliye teslim edildi.`, 'success');
    showBulkModal.value = false;
    await load();
    // If all delivered, go back after a short moment
    if (stop.value?.isFullyDelivered) {
      router.push({ name: 'DriverShipments' });
    }
  } catch {
    notify.add('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
  } finally {
    bulkSubmitting.value = false;
  }
}

function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString('tr-TR', {
    day: '2-digit', month: 'short', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });
}

// ── Return Modal ──────────────────────────────────────────────────────────────

interface ReturnLineForm {
  shipmentLineId: number;
  stockCode: string;
  stockName: string;
  maxQty: number;
  alreadyReturned: number;
  returnedQty: number;
  returnReason: number;
}

const showReturnModal    = ref(false);
const returnLoading      = ref(false);
const returnSubmitting   = ref(false);
const returnNote         = ref('');
const returnLines        = ref<ReturnLineForm[]>([]);
const returnShipmentInfo = ref<StopShipmentDto | null>(null);
const bulkReturnReason   = ref(5); // CustomerRejected varsayılan
let   returnShipmentId   = 0;

function fillAllLines() {
  returnLines.value.forEach(l => {
    l.returnedQty  = l.maxQty - l.alreadyReturned;
    l.returnReason = bulkReturnReason.value;
  });
}

async function openReturnModal(shipmentId: number) {
  returnShipmentId = shipmentId;
  returnShipmentInfo.value = stop.value?.shipments.find(s => s.id === shipmentId) ?? null;
  returnLines.value    = [];
  returnNote.value     = '';
  bulkReturnReason.value = 5;
  showReturnModal.value = true;
  returnLoading.value   = true;
  try {
    const detail = await shipmentService.getDetail(shipmentId);
    returnLines.value = detail.lines.map(l => {
      const delivered      = l.deliveredQty > 0 ? l.deliveredQty : l.orderedQty;
      const alreadyReturned = l.returnedQty ?? 0;
      return {
        shipmentLineId: l.id,
        stockCode:      l.stockCode,
        stockName:      l.stockName,
        maxQty:         delivered,
        alreadyReturned,
        returnedQty:    0,
        returnReason:   5, // CustomerRejected varsayılan
      };
    }).filter(l => l.maxQty - l.alreadyReturned > 0); // iade edilebilir kalan olan kalemler
  } catch {
    notify.add('Kalem bilgileri yüklenemedi.', 'error');
    showReturnModal.value = false;
  } finally {
    returnLoading.value = false;
  }
}

function closeReturnModal() {
  if (returnSubmitting.value) return;
  showReturnModal.value = false;
}

async function submitReturn() {
  const linesToReturn = returnLines.value.filter(l => l.returnedQty > 0);
  if (linesToReturn.length === 0) return;

  // Miktar aşım kontrolü
  const overLimit = linesToReturn.find(l => l.returnedQty > l.maxQty - l.alreadyReturned);
  if (overLimit) {
    notify.add(`"${overLimit.stockName}" için girilen miktar iade edilebilir miktarı aşıyor.`, 'warning');
    return;
  }

  returnSubmitting.value = true;
  try {
    await shipmentService.recordVehicleReturn(returnShipmentId, {
      lines: linesToReturn.map(l => ({
        shipmentLineId: l.shipmentLineId,
        returnedQty:    l.returnedQty,
        returnReason:   l.returnReason,
      })),
      returnNote: returnNote.value.trim() || undefined,
    });
    notify.add('İade başarıyla kaydedildi.', 'success');
    showReturnModal.value = false;
    await load(); // listeyi yenile
  } catch {
    notify.add('Bir hata oluştu. Lütfen tekrar deneyin.', 'error');
  } finally {
    returnSubmitting.value = false;
  }
}

onMounted(load);
</script>
