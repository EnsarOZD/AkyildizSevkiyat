<template>
  <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4 mb-4 w-full">
    <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-x-6 gap-y-3 text-sm">
      <div class="min-w-0">
        <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5 truncate">Proje</div>
        <div class="font-medium text-gray-800 dark:text-gray-200 break-all">{{ shipment.projectName }}</div>
      </div>
      <div class="min-w-0">
        <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Teslim Tarihi</div>
        <div class="font-medium text-gray-800 dark:text-gray-200">{{ formatDate(shipment.deliveryDate) }}</div>
      </div>
      <div v-if="shipment.zoneName" class="min-w-0">
        <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Bölge</div>
        <div class="font-medium text-gray-800 dark:text-gray-200 break-all">{{ shipment.zoneName }}</div>
      </div>
      <div v-if="shipment.externalOrderNumber" class="min-w-0">
        <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Sipariş No</div>
        <div class="font-mono text-blue-600 break-all">{{ shipment.externalOrderNumber }}</div>
      </div>
      <div v-if="shipment.talepNo" class="min-w-0">
        <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Talep No</div>
        <div class="font-medium text-gray-700 dark:text-gray-300 break-all">{{ shipment.talepNo }}</div>
      </div>
      <div v-if="shipment.teslimAlacakKisiler" class="min-w-0">
        <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Teslim Alacak</div>
        <div class="text-gray-700 dark:text-gray-300 break-all">{{ shipment.teslimAlacakKisiler }}</div>
        <div v-if="shipment.teslimAlacakTelefon" class="text-xs text-gray-500 dark:text-gray-400 break-all">
          {{ shipment.teslimAlacakTelefon }}
        </div>
      </div>
      <div v-if="shipment.yoneticiMail" class="col-span-1 sm:col-span-2 md:col-span-3 min-w-0">
        <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Yönetici Mail</div>
        <div class="text-xs text-gray-600 dark:text-gray-400 break-all">{{ shipment.yoneticiMail }}</div>
      </div>
    </div>
    <div
      v-if="shipment.aciklama"
      class="mt-3 pt-3 border-t text-sm text-gray-600 dark:text-gray-400 bg-gray-50 dark:bg-gray-800 rounded p-2 break-all overflow-hidden"
    >
      <span class="font-bold text-gray-700 dark:text-gray-300">Not: </span>{{ shipment.aciklama }}
    </div>
  </div>
</template>

<script setup lang="ts">
interface ShipmentInfo {
  projectName: string
  deliveryDate: string
  zoneName?: string
  externalOrderNumber?: string
  talepNo?: string
  teslimAlacakKisiler?: string
  teslimAlacakTelefon?: string
  yoneticiMail?: string
  aciklama?: string
}

defineProps<{ shipment: ShipmentInfo }>()

function formatDate(val: string): string {
  return new Date(val).toLocaleDateString('tr-TR')
}
</script>
