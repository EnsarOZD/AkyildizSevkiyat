<template>
  <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl divide-y divide-gray-100 dark:divide-gray-700">

    <!-- Araç & Sürücü / Taşıyıcı -->
    <div class="p-5">
      <h3 class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3 flex items-center gap-2">
        Araç &amp; {{ isFreight ? 'Taşıyıcı' : 'Sürücü' }}
        <span v-if="isFreight" class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 normal-case">Nakliye</span>
      </h3>
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
        <div>
          <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">{{ isFreight ? 'Taşıyıcı' : 'Sürücü' }}</div>
          <div class="font-medium text-gray-800 dark:text-gray-200">{{ displayDriver || '—' }}</div>
        </div>
        <div>
          <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">Plaka</div>
          <div class="font-medium text-gray-800 dark:text-gray-200">{{ displayPlate || '—' }}</div>
        </div>
        <div v-if="phone">
          <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">Telefon</div>
          <a :href="`tel:${phone}`" class="font-medium text-blue-600 dark:text-blue-400 hover:underline">{{ phone }}</a>
        </div>
      </div>
    </div>

    <!-- İrsaliye -->
    <div class="p-5">
      <h3 class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">İrsaliye</h3>
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
        <div>
          <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">İrsaliye No</div>
          <div class="flex items-center gap-2">
            <span class="font-mono text-blue-700 dark:text-blue-400 font-medium">{{ shipment.irsaliyeNo || '—' }}</span>
            <button
              v-role="['Admin', 'Manager']"
              @click="$emit('openIrsaliye')"
              class="text-xs text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 border border-blue-200 dark:border-blue-800 rounded px-1.5 py-0.5 transition-colors"
            >
              {{ shipment.irsaliyeNo ? 'Güncelle' : 'Gir' }}
            </button>
          </div>
        </div>
        <div v-if="shipment.irsaliyeDate">
          <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">İrsaliye Tarihi</div>
          <div class="text-gray-700 dark:text-gray-300">{{ shipment.irsaliyeDate }}</div>
        </div>
        <div v-if="shipment.netsisTransferredAt" class="col-span-full">
          <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">Netsis Aktarım</div>
          <div class="text-xs text-green-700 dark:text-green-400 font-medium">
            {{ new Date(shipment.netsisTransferredAt).toLocaleString('tr-TR') }}
          </div>
        </div>
      </div>
    </div>

    <!-- Teslim Bilgisi -->
    <div v-if="shipment.deliveredAt" class="p-5 bg-green-50 dark:bg-green-900/10 border-t border-green-100 dark:border-green-900/30">
      <h3 class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3 flex items-center gap-2">
        Teslim Bilgisi
        <span v-if="isFreight" class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 normal-case">
          Nakliye ile teslim{{ displayDriver ? ' — ' + displayDriver : '' }}
        </span>
      </h3>
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
        <div>
          <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">Teslim Zamanı</div>
          <div class="font-medium text-green-700 dark:text-green-400">{{ new Date(shipment.deliveredAt).toLocaleString('tr-TR') }}</div>
        </div>
        <div v-if="shipment.deliveryRecipient">
          <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">Teslim Alan</div>
          <div class="font-medium text-gray-800 dark:text-gray-200">{{ shipment.deliveryRecipient }}</div>
        </div>
        <div
          v-if="shipment.deliveryNote"
          class="col-span-2 text-xs text-gray-600 dark:text-gray-400 italic bg-white dark:bg-gray-800/50 border border-green-100 dark:border-green-900/30 rounded p-2"
        >
          {{ shipment.deliveryNote }}
        </div>
        <div v-if="photos.length" class="col-span-2">
          <div class="text-xs text-gray-500 dark:text-gray-400 mb-1">
            Teslimat Fotoğrafı<span v-if="photos.length > 1"> ({{ photos.length }})</span>
          </div>
          <div class="flex flex-wrap gap-2">
            <img
              v-for="(src, i) in photos"
              :key="i"
              :src="src"
              alt="Teslimat fotoğrafı"
              class="rounded-lg border border-green-200 dark:border-green-800 h-32 w-32 object-cover cursor-pointer hover:opacity-90 transition-opacity"
              @click="$emit('photoClick', src)"
            />
          </div>
        </div>
      </div>
    </div>

    <!-- Empty state -->
    <div
      v-if="!shipment.driverName && !shipment.irsaliyeNo && !shipment.deliveredAt"
      class="p-8 text-center text-sm text-gray-400"
    >
      Henüz araç/sürücü atanmamış.
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { getPhotoUrl, absolutePhotoUrl } from '../../utils/photoUrl';

interface DeliveryPhoto {
  id: number
  photoUrl: string
  photoIndex: number
  takenAt: string
}

interface ShipmentDeliveryInfo {
  driverName?: string
  plateNumber?: string
  freightCarrierName?: string
  freightCarrierPlate?: string
  freightCarrierPhone?: string
  irsaliyeNo?: string
  irsaliyeDate?: string
  netsisTransferredAt?: string
  deliveredAt?: string
  deliveryNote?: string
  deliveryRecipient?: string
  deliveryPhotoBase64?: string
  deliveryPhotoPath?: string
  deliveryPhotos?: DeliveryPhoto[]
}

const props = defineProps<{ shipment: ShipmentDeliveryInfo }>()
defineEmits<{
  openIrsaliye: []
  photoClick: [src: string]
}>()

// Nakliye (freight) ile gönderildiğinde sürücü yerine taşıyıcı bilgisi dolu olur
const isFreight = computed(() => !!props.shipment.freightCarrierName)
const displayDriver = computed(() => props.shipment.driverName || props.shipment.freightCarrierName || '')
const displayPlate = computed(() => props.shipment.plateNumber || props.shipment.freightCarrierPlate || '')
const phone = computed(() => props.shipment.freightCarrierPhone || '')

// Çoklu teslim fotoğrafları (şoför/nakliye) + geriye dönük tekil foto
const photos = computed<string[]>(() => {
  const multi = (props.shipment.deliveryPhotos ?? [])
    .slice()
    .sort((a, b) => a.photoIndex - b.photoIndex)
    .map(p => absolutePhotoUrl(p.photoUrl))
    .filter((s): s is string => !!s)
  if (multi.length) return multi
  const legacy = getPhotoUrl(props.shipment.deliveryPhotoPath, props.shipment.deliveryPhotoBase64)
  return legacy ? [legacy] : []
})
</script>
