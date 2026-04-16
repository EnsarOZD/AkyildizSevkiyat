<template>
  <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl divide-y divide-gray-100 dark:divide-gray-700">

    <!-- Araç & Sürücü -->
    <div class="p-5">
      <h3 class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Araç & Sürücü</h3>
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
        <div>
          <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">Sürücü</div>
          <div class="font-medium text-gray-800 dark:text-gray-200">{{ shipment.driverName || '—' }}</div>
        </div>
        <div>
          <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">Plaka</div>
          <div class="font-medium text-gray-800 dark:text-gray-200">{{ shipment.plateNumber || '—' }}</div>
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
            <span class="font-mono text-blue-700 font-medium">{{ shipment.irsaliyeNo || '—' }}</span>
            <button
              v-role="['Admin', 'Manager']"
              @click="$emit('openIrsaliye')"
              class="text-xs text-indigo-600 hover:text-indigo-800 border border-indigo-200 rounded px-1.5 py-0.5 transition-colors"
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
          <div class="text-xs text-green-700 font-medium">
            {{ new Date(shipment.netsisTransferredAt).toLocaleString('tr-TR') }}
          </div>
        </div>
      </div>
    </div>

    <!-- Teslim Bilgisi -->
    <div v-if="shipment.deliveredAt" class="p-5 bg-green-50 dark:bg-green-900/10">
      <h3 class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Teslim Bilgisi</h3>
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
        <div>
          <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">Teslim Zamanı</div>
          <div class="font-medium text-green-700">{{ new Date(shipment.deliveredAt).toLocaleString('tr-TR') }}</div>
        </div>
        <div v-if="shipment.deliveryRecipient">
          <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">Teslim Alan</div>
          <div class="font-medium text-gray-800 dark:text-gray-200">{{ shipment.deliveryRecipient }}</div>
        </div>
        <div
          v-if="shipment.deliveryNote"
          class="col-span-2 text-xs text-gray-600 dark:text-gray-400 italic bg-white dark:bg-gray-900 border border-green-100 rounded p-2"
        >
          {{ shipment.deliveryNote }}
        </div>
        <div v-if="shipment.deliveryPhotoBase64" class="col-span-2">
          <div class="text-xs text-gray-500 dark:text-gray-400 mb-1">Teslimat Fotoğrafı</div>
          <img
            :src="`data:image/jpeg;base64,${shipment.deliveryPhotoBase64}`"
            alt="Teslimat fotoğrafı"
            class="rounded-lg border border-green-200 max-h-64 object-contain cursor-pointer hover:opacity-90 transition-opacity"
            @click="$emit('photoClick', `data:image/jpeg;base64,${shipment.deliveryPhotoBase64}`)"
          />
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
interface ShipmentDeliveryInfo {
  driverName?: string
  plateNumber?: string
  irsaliyeNo?: string
  irsaliyeDate?: string
  netsisTransferredAt?: string
  deliveredAt?: string
  deliveryNote?: string
  deliveryRecipient?: string
  deliveryPhotoBase64?: string
}

defineProps<{ shipment: ShipmentDeliveryInfo }>()
defineEmits<{
  openIrsaliye: []
  photoClick: [src: string]
}>()
</script>
