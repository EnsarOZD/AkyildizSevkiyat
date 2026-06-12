<template>
  <div>
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
      <div class="rounded-3xl shadow-lg shadow-blue-600/20 border border-blue-400/40 p-4 space-y-3" style="background: linear-gradient(160deg,#16335f,#0f2240);">
        <div class="flex items-start gap-3">
          <div
            class="flex-shrink-0 w-12 h-12 rounded-2xl flex items-center justify-center text-base font-extrabold shadow-lg"
            :class="stop.isFullyDelivered
              ? 'bg-emerald-500/16 text-emerald-300'
              : 'bg-gradient-to-br from-blue-500 to-blue-600 text-white shadow-blue-600/50'"
          >
            <CheckCircleIcon v-if="stop.isFullyDelivered" class="w-5 h-5" aria-hidden="true" />
            <span v-else>{{ stop.stopNumber }}</span>
          </div>
          <div>
            <h2 class="text-lg font-extrabold text-white leading-tight">{{ stop.projectName }}</h2>
            <p v-if="stop.zoneName" class="text-xs text-white/45 mt-0.5">{{ stop.zoneName }}</p>
          </div>
        </div>

        <!-- Summary pills -->
        <div class="flex gap-3 text-sm">
          <div class="flex items-center gap-1 text-white/60">
            <DocumentTextIcon class="w-4 h-4" aria-hidden="true" />
            <span>{{ stop.shipments.length }} irsaliye</span>
          </div>
          <div class="flex items-center gap-1 text-white/60">
            <ArchiveBoxIcon class="w-4 h-4" aria-hidden="true" />
            <span>{{ stop.totalLineCount }} kalem</span>
          </div>
          <div v-if="stop.isFullyDelivered" class="flex items-center gap-1 text-emerald-300 font-medium">
            <CheckCircleIcon class="w-4 h-4" aria-hidden="true" />
            Tamamlandı
          </div>
        </div>

        <!-- Address + navigation -->
        <div class="flex items-start gap-2">
          <MapPinIcon class="w-4 h-4 text-white/50 flex-shrink-0 mt-0.5" aria-hidden="true" />
          <div class="flex-1 min-w-0">
            <button
              v-if="stop.projectAddress || stop.projectLatitude"
              @click="openMaps(mapsUrl)"
              class="text-sm text-blue-300 hover:underline leading-snug block text-left"
            >
              <span v-if="stop.projectLatitude">
                📍 {{ stop.projectLatitude?.toFixed(5) }}, {{ stop.projectLongitude?.toFixed(5) }}
                <span v-if="stop.projectAddress" class="text-white/40 text-xs ml-1">({{ stop.projectAddress }})</span>
              </span>
              <span v-else>{{ stop.projectAddress }}</span>
            </button>
            <span v-else class="text-sm text-white/40 italic">Adres tanımsız</span>
          </div>
          <button
            @click="saveCurrentLocation"
            :disabled="savingLocation"
            class="flex-shrink-0 flex items-center gap-1 text-xs px-2.5 py-1.5 rounded-lg border transition-colors"
            :class="stop.projectLatitude
              ? 'border-green-400/40 text-green-300 bg-green-500/10 hover:bg-green-500/20'
              : 'border-white/20 text-white/70 bg-white/5 hover:bg-white/10'"
            :title="stop.projectLatitude ? 'Konumu Güncelle' : 'Mevcut Konumu Kaydet'"
          >
            <span v-if="savingLocation" class="w-3 h-3 border-2 border-current border-t-transparent rounded-full animate-spin"></span>
            <MapPinIcon v-else class="w-3 h-3" aria-hidden="true" />
            <span>{{ stop.projectLatitude ? 'Güncelle' : 'Kaydet' }}</span>
          </button>
        </div>

        <!-- Teslim alacak kişi + Ara -->
        <div
          v-if="stop.contactName || stop.contactPhone"
          class="flex items-center justify-between gap-3 rounded-lg bg-white/5 px-3 py-2"
        >
          <div class="min-w-0">
            <p v-if="stop.contactName" class="text-sm font-medium text-white/90 flex items-center gap-1.5">
              <UserIcon class="w-4 h-4 text-white/50 flex-shrink-0" aria-hidden="true" />
              <span class="break-words">{{ stop.contactName }}</span>
            </p>
            <p v-if="stop.contactPhone" class="text-sm text-white/60 flex items-center gap-1.5 mt-0.5">
              <PhoneIcon class="w-4 h-4 text-white/50 flex-shrink-0" aria-hidden="true" />
              {{ stop.contactPhone }}
            </p>
          </div>
          <a
            v-if="stop.contactPhone"
            :href="`tel:${stop.contactPhone}`"
            class="flex-shrink-0 flex items-center gap-1.5 px-4 py-2 bg-gradient-to-br from-emerald-500 to-green-600 hover:from-emerald-400 hover:to-green-500 text-white text-sm font-semibold rounded-xl transition-colors"
          >
            <PhoneIcon class="w-4 h-4" aria-hidden="true" />
            Ara
          </a>
        </div>
      </div>

      <!-- ══ Teslimat Formu (bekleyen sevkiyatlar varsa) ══ -->
      <template v-if="pendingShipments.length">

        <!-- Birleştirilmiş ürün listesi -->
        <div class="bg-white dark:bg-[#0f2744] rounded-2xl shadow-sm border border-gray-200 dark:border-white/10 overflow-hidden">
          <div class="px-4 py-3 border-b border-gray-100 dark:border-white/10 flex items-center justify-between">
            <h3 class="font-bold text-gray-900 dark:text-white">Araçtaki Ürünler</h3>
            <span class="text-xs text-gray-400">İndirdiğiniz ürünleri işaretleyin</span>
          </div>

          <template v-for="group in aggGroups" :key="group.label">
            <div class="px-4 py-1.5 bg-gray-100 dark:bg-white/[0.07] text-[11px] font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
              {{ group.label }}
            </div>
            <div
              v-for="p in group.items"
              :key="p.key"
              class="px-4 py-3 border-t border-gray-100 dark:border-white/10"
              :class="p.delivered ? 'bg-emerald-50/50 dark:bg-emerald-900/10' : ''"
            >
              <div class="flex items-start gap-3">
                <input
                  type="checkbox"
                  :checked="p.delivered"
                  @change="onToggleDelivered(p)"
                  class="mt-1 h-5 w-5 rounded border-gray-300 text-emerald-600 focus:ring-emerald-500 flex-shrink-0"
                />
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-gray-900 dark:text-white leading-tight">{{ p.stockName }}</p>
                  <p class="text-xs text-gray-400 mt-0.5">Yüklenen: <span class="font-semibold">{{ p.loadedQty }}</span> {{ p.unit }}</p>
                </div>
                <!-- Teslim miktarı -->
                <div class="w-20 flex-shrink-0">
                  <label class="block text-[10px] text-gray-400 mb-0.5 text-right">Teslim</label>
                  <input
                    v-model.number="p.deliveredQty"
                    @input="clampQty(p)"
                    type="number" min="0" :max="p.loadedQty" step="1"
                    class="w-full px-2 py-1.5 text-sm text-center rounded-lg border bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-emerald-400"
                    :class="p.deliveredQty < p.loadedQty ? 'border-orange-400' : 'border-gray-300 dark:border-white/20'"
                  />
                </div>
              </div>

              <!-- Eksik/0 → sebep zorunlu -->
              <div v-if="p.deliveredQty < p.loadedQty" class="mt-2 pl-8 space-y-2">
                <div class="flex items-center gap-2">
                  <span class="text-[11px] font-bold text-orange-600 dark:text-orange-400">
                    Eksik: {{ p.loadedQty - p.deliveredQty }} {{ p.unit }}
                  </span>
                  <select
                    v-model="p.returnReason"
                    class="flex-1 px-2 py-1.5 text-sm rounded-lg border border-orange-300 dark:border-orange-700 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-orange-400"
                  >
                    <option :value="null" disabled>Sebep seçin…</option>
                    <option v-for="r in REASONS" :key="r.value" :value="r.value">{{ r.label }}</option>
                  </select>
                </div>
                <input
                  v-if="p.returnReason === 99"
                  v-model="p.returnReasonText"
                  type="text" placeholder="Sebebi yazın…"
                  class="w-full px-3 py-1.5 text-sm rounded-lg border border-orange-300 dark:border-orange-700 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-orange-400"
                />
              </div>
            </div>
          </template>
        </div>

        <!-- Harici iade -->
        <div class="bg-white dark:bg-[#0f2744] rounded-2xl shadow-sm border border-gray-200 dark:border-white/10 overflow-hidden">
          <div class="px-4 py-3 border-b border-gray-100 dark:border-white/10 flex items-center justify-between">
            <div>
              <h3 class="font-bold text-gray-900 dark:text-white flex items-center gap-1.5">
                <ArrowUturnLeftIcon class="w-4 h-4 text-orange-500" aria-hidden="true" /> Harici İade
              </h3>
              <p class="text-xs text-gray-400 mt-0.5">Önceki sevkiyatlardan iade alınan ürünler</p>
            </div>
            <button
              @click="addExternalReturn"
              class="flex items-center gap-1 px-3 py-1.5 text-sm font-semibold text-orange-600 dark:text-orange-400 bg-orange-50 dark:bg-orange-900/20 rounded-lg hover:bg-orange-100 dark:hover:bg-orange-900/30 transition-colors"
            >
              <PlusIcon class="w-4 h-4" aria-hidden="true" /> İade Ekle
            </button>
          </div>
          <div v-if="externalReturns.length" class="divide-y divide-gray-100 dark:divide-white/10">
            <div v-for="(r, idx) in externalReturns" :key="idx" class="px-4 py-3 space-y-2">
              <div class="flex items-center gap-2">
                <input
                  v-model="r.stockNameFree"
                  type="text" placeholder="Ürün adı"
                  class="flex-1 px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-orange-400"
                />
                <input
                  v-model.number="r.qty"
                  type="number" min="0" step="1" placeholder="Adet"
                  class="w-20 px-2 py-2 text-sm text-center rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-orange-400"
                />
                <button @click="externalReturns.splice(idx, 1)" class="p-2 text-gray-400 hover:text-red-500">
                  <XMarkIcon class="w-5 h-5" aria-hidden="true" />
                </button>
              </div>
              <select
                v-model="r.returnReason"
                class="w-full px-2 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-orange-400"
              >
                <option v-for="rr in REASONS" :key="rr.value" :value="rr.value">{{ rr.label }}</option>
              </select>
            </div>
          </div>
          <div v-else class="px-4 py-3 text-sm text-gray-400 italic">İade yok.</div>
        </div>

        <!-- Teslim onayı -->
        <div class="bg-white dark:bg-[#0f2744] rounded-2xl shadow-sm border border-gray-200 dark:border-white/10 p-4 space-y-4">
          <h3 class="font-bold text-gray-900 dark:text-white">Teslim Onayı</h3>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Teslim Alan Kişi <span class="text-red-500">*</span></label>
            <input
              v-model="form.deliveryRecipient"
              type="text" placeholder="Adı Soyadı"
              class="w-full px-3 py-2.5 rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white placeholder-gray-400 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Not <span class="text-gray-400 font-normal">(isteğe bağlı)</span></label>
            <textarea
              v-model="form.deliveryNote"
              rows="2" placeholder="Teslimat notu..."
              class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white placeholder-gray-400 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
            ></textarea>
          </div>

          <!-- Fotoğraflar -->
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Teslim Fotoğrafları <span class="text-red-500">*</span>
              <span class="text-gray-400 font-normal ml-1">({{ form.photos.length }}/5)</span>
            </label>
            <div v-if="form.photos.length" class="grid grid-cols-3 gap-2 mb-2">
              <div v-for="(photo, idx) in form.photos" :key="idx" class="relative aspect-square">
                <img :src="photo.preview" class="w-full h-full object-cover rounded-lg cursor-pointer" @click="lightboxSrc = photo.preview" />
                <button @click="form.photos.splice(idx, 1)" class="absolute top-1 right-1 bg-black/50 text-white rounded-full p-0.5">
                  <XMarkIcon class="w-3.5 h-3.5" />
                </button>
              </div>
            </div>
            <label
              v-if="form.photos.length < 5"
              class="flex items-center justify-center gap-2 w-full py-3 border-2 border-dashed border-gray-300 dark:border-white/20 rounded-lg cursor-pointer hover:border-blue-400 transition-colors"
            >
              <input type="file" accept="image/*" capture="environment" class="hidden" @change="onPhotoSelected" />
              <span v-if="form.photoCompressing" class="text-sm text-gray-500 flex items-center gap-2">
                <span class="w-4 h-4 border-2 border-gray-400 border-t-transparent rounded-full animate-spin"></span>
                Sıkıştırılıyor...
              </span>
              <template v-else>
                <CameraIcon class="w-5 h-5 text-gray-400" aria-hidden="true" />
                <span class="text-sm text-gray-500 dark:text-gray-400">{{ form.photos.length === 0 ? 'Fotoğraf Çek / Seç' : 'Fotoğraf Ekle' }}</span>
              </template>
            </label>
          </div>
        </div>

        <!-- Submit -->
        <div class="sticky bottom-4">
          <button
            @click="submitStop"
            :disabled="submitting"
            class="w-full py-3.5 bg-gradient-to-br from-emerald-500 to-green-600 hover:from-emerald-400 hover:to-green-500 disabled:opacity-50 text-white font-semibold rounded-xl shadow-lg transition-colors flex items-center justify-center gap-2"
          >
            <span v-if="submitting" class="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            <CheckCircleIcon v-else class="w-5 h-5" aria-hidden="true" />
            {{ submitting ? 'Kaydediliyor...' : 'Teslimi Tamamla' }}
          </button>
        </div>
      </template>

      <!-- ══ Tamamlanan sevkiyatlar (geçmiş, salt-okunur) ══ -->
      <div
        v-for="shipment in completedShipments"
        :key="shipment.id"
        class="bg-white dark:bg-[#0f2744] rounded-xl shadow-sm border border-gray-200 dark:border-white/10 overflow-hidden"
      >
        <div class="px-4 py-3 space-y-3"
             :class="shipment.status === 'Delivered' ? 'bg-green-50 dark:bg-green-900/20' : 'bg-violet-50 dark:bg-violet-900/20'">
          <div class="flex items-center gap-2">
            <CheckCircleIcon v-if="shipment.status === 'Delivered'" class="w-5 h-5 text-green-500 flex-shrink-0" aria-hidden="true" />
            <ArrowUturnLeftIcon v-else class="w-5 h-5 text-amber-500 flex-shrink-0" aria-hidden="true" />
            <div class="flex-1">
              <p class="font-medium text-sm text-gray-900 dark:text-white">
                {{ shipment.externalOrderNumber || ('#' + shipment.id) }}
                <span v-if="shipment.irsaliyeNo" class="text-xs text-gray-400 font-normal">· {{ shipment.irsaliyeNo }}</span>
              </p>
              <p class="text-xs" :class="shipment.status === 'Delivered' ? 'text-green-700 dark:text-green-400' : 'text-amber-600 dark:text-amber-400'">
                {{ shipment.status === 'Delivered' ? 'Teslim Edildi' : 'Depoya İade' }}
                <span v-if="shipment.deliveredAt" class="font-normal">· {{ formatDateTime(shipment.deliveredAt) }}</span>
              </p>
            </div>
          </div>
          <p v-if="shipment.deliveryRecipient" class="text-sm text-gray-700 dark:text-gray-300">
            <span class="text-gray-400">Teslim Alan: </span>{{ shipment.deliveryRecipient }}
          </p>
          <p v-if="shipment.deliveryNote" class="text-sm text-gray-700 dark:text-gray-300">
            <span class="text-gray-400">Not: </span>{{ shipment.deliveryNote }}
          </p>
          <div v-if="resolvedDeliveryPhotos(shipment).length" class="grid grid-cols-3 gap-2">
            <img v-for="photo in resolvedDeliveryPhotos(shipment)" :key="photo.id" :src="photo.src"
                 class="w-full aspect-square object-cover rounded-lg cursor-pointer" @click="lightboxSrc = photo.src" />
          </div>
          <div v-else-if="getPhotoUrl(shipment.deliveryPhotoPath, shipment.deliveryPhotoBase64)">
            <img :src="getPhotoUrl(shipment.deliveryPhotoPath, shipment.deliveryPhotoBase64)!"
                 class="w-full max-h-40 object-cover rounded-lg cursor-pointer"
                 @click="lightboxSrc = getPhotoUrl(shipment.deliveryPhotoPath, shipment.deliveryPhotoBase64)" />
          </div>
          <button
            @click="openNoteModal(shipment.id)"
            class="w-full flex items-center justify-center gap-1.5 py-2 border border-gray-200 dark:border-white/10 text-gray-600 dark:text-gray-300 text-sm font-medium rounded-xl hover:bg-gray-50 dark:hover:bg-white/5 transition-colors"
          >
            <PencilSquareIcon class="w-4 h-4" aria-hidden="true" /> Not Ekle
          </button>
        </div>
      </div>

    </template>

    <!-- Lightbox -->
    <div v-if="lightboxSrc" class="fixed inset-0 z-[60] bg-black/90 flex items-center justify-center p-4" @click="lightboxSrc = null">
      <img :src="lightboxSrc" class="max-w-full max-h-full object-contain rounded-lg" />
    </div>
  </div>

  <!-- Not ekleme modalı -->
  <Teleport to="body">
    <div v-if="showNoteModal" class="fixed inset-0 z-50 flex items-end justify-center">
      <div class="absolute inset-0 bg-black/60" @click="closeNoteModal"></div>
      <div class="relative w-full max-w-lg bg-white dark:bg-[#0f2744] rounded-t-2xl p-5 space-y-4">
        <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Not Ekle</h3>
        <p class="text-sm text-gray-500 dark:text-gray-400">Bu not sevkiyat geçmişine eklenir; mevcut teslim notunu değiştirmez.</p>
        <textarea
          v-model="noteText" rows="3" placeholder="Notunuzu yazın..."
          class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white placeholder-gray-400 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
        ></textarea>
        <div class="flex gap-3">
          <button @click="closeNoteModal" class="flex-1 py-3 border border-gray-300 dark:border-white/20 text-gray-700 dark:text-gray-300 font-medium rounded-xl hover:bg-gray-50 dark:hover:bg-white/5 transition-colors text-sm">İptal</button>
          <button
            @click="submitNote"
            :disabled="noteSubmitting || !noteText.trim()"
            class="flex-1 py-3 bg-gradient-to-br from-blue-500 to-blue-600 hover:from-blue-400 hover:to-blue-500 disabled:bg-blue-400 text-white font-semibold rounded-xl transition-colors flex items-center justify-center gap-2 text-sm"
          >
            <span v-if="noteSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            {{ noteSubmitting ? 'Kaydediliyor...' : 'Kaydet' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
  </div>
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
  CameraIcon,
  XMarkIcon,
  ArrowUturnLeftIcon,
  PencilSquareIcon,
  PlusIcon,
} from '@heroicons/vue/24/outline';
import driverService, { type DeliveryStopDto, type StopShipmentDto, type DriverDeliveryPhotoDto } from '../services/driverService';
import shipmentService, { type DeliverStopLineInput } from '../services/shipmentService';
import { useNotificationStore } from '../stores/notification';
import { useOpenMaps } from '../composables/useOpenMaps';
import { getPhotoUrl } from '../utils/photoUrl';

const route  = useRoute();
const router = useRouter();
const notify = useNotificationStore();
const { openMaps } = useOpenMaps();

const projectId = Number(route.params.projectId);
const stop      = ref<DeliveryStopDto | null>(null);
const loading   = ref(false);
const error     = ref('');
const lightboxSrc = ref<string | null>(null);
const savingLocation = ref(false);
const submitting = ref(false);

// İade / eksik sebepleri (ReturnReason enum)
const REASONS = [
  { value: 0,  label: 'Müşteri reddetti' },
  { value: 1,  label: 'Hasarlı / Kırık' },
  { value: 3,  label: 'Yanlış ürün' },
  { value: 4,  label: 'Proje kapalı / bulunamadı' },
  { value: 99, label: 'Diğer' },
];

const CATEGORY_LABELS: Record<number, string> = {
  1: 'Gıda', 2: 'Sarf', 3: 'Kıyafet', 4: 'Temizlik', 5: 'Kırtasiye', 99: 'Diğer',
};

// ── Birleştirilmiş ürün modeli ──────────────────────────────────────────────
interface AggRef { shipmentId: number; lineId: number; loadedQty: number; }
interface AggProduct {
  key: string;
  stockName: string;
  unit: string;
  category: number;
  loadedQty: number;
  refs: AggRef[];
  delivered: boolean;
  deliveredQty: number;
  returnReason: number | null;
  returnReasonText: string;
}

const aggProducts = ref<AggProduct[]>([]);

interface ExternalReturnForm { stockNameFree: string; qty: number; returnReason: number; }
const externalReturns = ref<ExternalReturnForm[]>([]);

const form = reactive<{ deliveryRecipient: string; deliveryNote: string; photos: { base64: string; preview: string }[]; photoCompressing: boolean; }>({
  deliveryRecipient: '',
  deliveryNote: '',
  photos: [],
  photoCompressing: false,
});

const pendingShipments = computed(() =>
  stop.value?.shipments.filter(s => s.status !== 'Delivered' && s.status !== 'ReturnedToWarehouse') ?? []
);
const completedShipments = computed(() =>
  stop.value?.shipments.filter(s => s.status === 'Delivered' || s.status === 'ReturnedToWarehouse') ?? []
);

// Kategoriye göre gruplanmış birleşik ürünler (Sarf→Gıda→Kıyafet→diğer)
const aggGroups = computed(() => {
  const order: Record<number, number> = { 2: 1, 1: 2, 3: 3 };
  const getOrder = (cat: number) => order[cat] ?? 4;
  const sorted = [...aggProducts.value].sort((a, b) => {
    const od = getOrder(a.category) - getOrder(b.category);
    return od !== 0 ? od : a.stockName.localeCompare(b.stockName, 'tr');
  });
  const groups: { label: string; items: AggProduct[] }[] = [];
  for (const p of sorted) {
    const label = CATEGORY_LABELS[p.category] ?? 'Diğer';
    const last = groups[groups.length - 1];
    if (last?.label === label) last.items.push(p);
    else groups.push({ label, items: [p] });
  }
  return groups;
});

function buildAggregates() {
  const map = new Map<string, AggProduct>();
  for (const s of pendingShipments.value) {
    for (const line of (s.lines ?? [])) {
      const loaded = line.loadedQty ?? line.orderedQty;
      if (loaded <= 0) continue;
      const key = line.stockCode || line.stockName;
      let p = map.get(key);
      if (!p) {
        p = {
          key, stockName: line.stockName, unit: line.unit, category: line.category,
          loadedQty: 0, refs: [], delivered: false, deliveredQty: 0,
          returnReason: null, returnReasonText: '',
        };
        map.set(key, p);
      }
      p.loadedQty += loaded;
      p.refs.push({ shipmentId: s.id, lineId: line.id, loadedQty: loaded });
    }
  }
  aggProducts.value = Array.from(map.values());
}

const mapsUrl = computed(() => {
  const s = stop.value;
  if (!s) return '#';
  const destination = (s.projectLatitude != null && s.projectLongitude != null)
    ? `${s.projectLatitude},${s.projectLongitude}`
    : encodeURIComponent(s.projectAddress ?? '');
  return `https://www.google.com/maps/dir/?api=1&destination=${destination}&travelmode=driving`;
});

async function load() {
  loading.value = true;
  error.value = '';
  try {
    const routeData = await driverService.getRoute();
    const found = routeData.stops.find(s => s.projectId === projectId);
    if (!found) { error.value = 'Teslimat noktası bulunamadı.'; return; }
    stop.value = found;
    buildAggregates();
  } catch {
    error.value = 'Yüklenemedi. Lütfen tekrar deneyin.';
  } finally {
    loading.value = false;
  }
}

function onToggleDelivered(p: AggProduct) {
  p.delivered = !p.delivered;
  if (p.delivered) {
    p.deliveredQty = p.loadedQty;
    p.returnReason = null;
    p.returnReasonText = '';
  } else {
    p.deliveredQty = 0;
  }
}

function clampQty(p: AggProduct) {
  if (p.deliveredQty == null || isNaN(p.deliveredQty) || p.deliveredQty < 0) p.deliveredQty = 0;
  if (p.deliveredQty > p.loadedQty) p.deliveredQty = p.loadedQty;
  p.delivered = p.deliveredQty > 0;
}

async function saveCurrentLocation() {
  if (savingLocation.value || !stop.value) return;
  if (!navigator.geolocation) { notify.add('Bu cihaz konum desteklemiyor.', 'error'); return; }
  savingLocation.value = true;
  try {
    const pos = await new Promise<GeolocationPosition>((resolve, reject) =>
      navigator.geolocation.getCurrentPosition(resolve, reject, { enableHighAccuracy: true, timeout: 10000 })
    );
    const { latitude, longitude } = pos.coords;
    await driverService.saveProjectLocation(stop.value.projectId, latitude, longitude);
    (stop.value as any).projectLatitude = latitude;
    (stop.value as any).projectLongitude = longitude;
    notify.add('Konum kaydedildi.', 'success');
  } catch (e: any) {
    notify.add(e?.code === 1 ? 'Konum izni reddedildi.' : 'Konum alınamadı.', 'error');
  } finally {
    savingLocation.value = false;
  }
}

async function compressFile(file: File): Promise<{ base64: string; preview: string }> {
  const bitmap = await createImageBitmap(file, { imageOrientation: 'from-image' });
  const MAX = 1000;
  let w = bitmap.width, h = bitmap.height;
  if (w > MAX || h > MAX) {
    if (w > h) { h = Math.round((h / w) * MAX); w = MAX; }
    else       { w = Math.round((w / h) * MAX); h = MAX; }
  }
  const canvas = document.createElement('canvas');
  canvas.width = w; canvas.height = h;
  const ctx = canvas.getContext('2d');
  if (!ctx) throw new Error('Canvas context unavailable');
  ctx.drawImage(bitmap, 0, 0, w, h);
  bitmap.close?.();
  const compressed = canvas.toDataURL('image/jpeg', 0.75);
  return { base64: compressed.split(',')[1] ?? '', preview: compressed };
}

async function onPhotoSelected(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  input.value = '';
  if (!file) return;
  if (form.photos.length >= 5) { notify.add('En fazla 5 fotoğraf ekleyebilirsiniz.', 'warning'); return; }
  form.photoCompressing = true;
  try {
    const { base64, preview } = await compressFile(file);
    form.photos.push({ base64, preview });
  } catch {
    notify.add('Fotoğraf işlenemedi. Lütfen tekrar deneyin.', 'error');
  } finally {
    form.photoCompressing = false;
  }
}

function addExternalReturn() {
  externalReturns.value.push({ stockNameFree: '', qty: 1, returnReason: 0 });
}

async function tryGetPosition(): Promise<{ lat: number; lng: number } | null> {
  if (!navigator.geolocation) return null;
  try {
    const pos = await new Promise<GeolocationPosition>((resolve, reject) =>
      navigator.geolocation.getCurrentPosition(resolve, reject, { enableHighAccuracy: true, timeout: 6000 })
    );
    return { lat: pos.coords.latitude, lng: pos.coords.longitude };
  } catch {
    return null;
  }
}

async function submitStop() {
  if (!form.deliveryRecipient.trim()) { notify.add('Teslim alan kişi zorunludur.', 'warning'); return; }
  if (!form.photos.length) { notify.add('En az bir teslim fotoğrafı zorunludur.', 'warning'); return; }

  // Eksik/0 satırlarda sebep zorunlu
  for (const p of aggProducts.value) {
    if (p.deliveredQty < p.loadedQty) {
      if (p.returnReason == null) { notify.add(`"${p.stockName}" için eksik/iade sebebi seçin.`, 'warning'); return; }
      if (p.returnReason === 99 && !p.returnReasonText.trim()) { notify.add(`"${p.stockName}" için 'Diğer' açıklaması girin.`, 'warning'); return; }
    }
  }
  // Harici iade doğrulama
  for (const r of externalReturns.value) {
    if (r.qty > 0 && !r.stockNameFree.trim()) { notify.add('Harici iade için ürün adı girin.', 'warning'); return; }
  }

  // Birleşik ürünün teslim miktarını alt satırlara fill-first dağıt
  const lines: DeliverStopLineInput[] = [];
  for (const p of aggProducts.value) {
    let remaining = p.deliveredQty;
    for (const ref of p.refs) {
      const give = Math.max(0, Math.min(remaining, ref.loadedQty));
      remaining -= give;
      const short = give < ref.loadedQty;
      lines.push({
        shipmentLineId: ref.lineId,
        deliveredQty: give,
        returnReason: short ? p.returnReason : null,
        returnReasonText: short && p.returnReason === 99 ? p.returnReasonText.trim() : null,
      });
    }
  }

  const extReturns = externalReturns.value
    .filter(r => r.qty > 0 && r.stockNameFree.trim())
    .map(r => ({ stockNameFree: r.stockNameFree.trim(), qty: r.qty, returnReason: r.returnReason }));

  submitting.value = true;
  try {
    const pos = await tryGetPosition();
    const res = await shipmentService.deliverStop(projectId, {
      deliveryRecipient: form.deliveryRecipient.trim(),
      deliveryNote: form.deliveryNote.trim() || undefined,
      photosBase64: form.photos.map(p => p.base64),
      latitude: pos?.lat ?? null,
      longitude: pos?.lng ?? null,
      lines,
      externalReturns: extReturns.length ? extReturns : undefined,
    });
    let msg = `${res.deliveredShipments} sevkiyat teslim edildi.`;
    if (res.returnedShipments > 0) msg += ` ${res.returnedShipments} iade.`;
    if (res.floatingReturns > 0) msg += ` ${res.floatingReturns} harici iade kaydedildi.`;
    notify.add(msg, 'success');
    router.push({ name: 'DriverShipments' });
  } catch (e: any) {
    notify.add(e?.message || 'Teslim kaydedilemedi. Lütfen tekrar deneyin.', 'error');
  } finally {
    submitting.value = false;
  }
}

// ── Teslim fotoğrafları (tamamlananlar) ─────────────────────────────────────
interface ResolvedDeliveryPhoto { id: number; photoIndex: number; src: string; }
function resolvedDeliveryPhotos(shipment: StopShipmentDto): ResolvedDeliveryPhoto[] {
  const raw: DriverDeliveryPhotoDto[] = shipment.deliveryPhotos ?? [];
  return raw
    .map(p => {
      const src = getPhotoUrl(p.photoUrl?.replace(/^\/photos\//, ''), null);
      return src ? { id: p.id, photoIndex: p.photoIndex, src } : null;
    })
    .filter((p): p is ResolvedDeliveryPhoto => p !== null);
}

function formatDateTime(iso: string) {
  return new Date(iso).toLocaleString('tr-TR', {
    day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit',
  });
}

// ── Not ekleme ──────────────────────────────────────────────────────────────
const showNoteModal  = ref(false);
const noteSubmitting = ref(false);
const noteText       = ref('');
let   noteShipmentId = 0;

function openNoteModal(shipmentId: number) {
  noteShipmentId = shipmentId;
  noteText.value = '';
  showNoteModal.value = true;
}
function closeNoteModal() {
  if (noteSubmitting.value) return;
  showNoteModal.value = false;
}
async function submitNote() {
  const text = noteText.value.trim();
  if (!text) return;
  noteSubmitting.value = true;
  try {
    await shipmentService.addNote(noteShipmentId, text);
    notify.add('Not eklendi.', 'success');
    showNoteModal.value = false;
  } catch {
    notify.add('Not eklenemedi. Lütfen tekrar deneyin.', 'error');
  } finally {
    noteSubmitting.value = false;
  }
}

onMounted(load);
</script>
