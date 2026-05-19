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
        <div class="font-mono text-blue-600 dark:text-blue-400 break-all">{{ shipment.externalOrderNumber }}</div>
      </div>
      <div v-if="shipment.talepNo" class="min-w-0">
        <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Talep No</div>
        <div class="font-medium text-gray-700 dark:text-gray-300 break-all">{{ shipment.talepNo }}</div>
      </div>
      <div v-if="shipment.talepTuru" class="min-w-0">
        <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Talep Türü</div>
        <div class="font-medium text-gray-700 dark:text-gray-300 break-all">{{ shipment.talepTuru }}</div>
      </div>
      <div v-if="shipment.institutionCode" class="min-w-0">
        <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Kurum Bilgisi</div>
        <div class="font-medium text-gray-700 dark:text-gray-300 break-all">{{ shipment.institutionCode }}</div>
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
      <div v-if="shipment.ykCargoKey" class="min-w-0">
        <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Yurtiçi Kargo</div>
        <div class="font-mono text-orange-600 dark:text-orange-400 break-all">{{ shipment.ykCargoKey }}</div>
        <div v-if="shipment.ykJobId" class="text-xs text-gray-500 dark:text-gray-400">İş No: {{ shipment.ykJobId }}</div>
        <div v-if="shipment.ykOperationStatus" class="text-xs mt-0.5"
          :class="isYkSuccess ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'">
          {{ ykStatusLabel }}
          <span v-if="shipment.ykErrorCode"> — {{ shipment.ykErrorCode }}</span>
        </div>
        <div v-if="shipment.ykErrorMessage" class="text-xs text-red-500 dark:text-red-400 break-all">
          {{ shipment.ykErrorMessage }}
        </div>
        <div v-if="shipment.ykLastQueryAt" class="text-xs text-gray-400 dark:text-gray-500">
          Son sorgu: {{ formatDate(shipment.ykLastQueryAt) }}
        </div>
        <button
          @click="$emit('queryYkStatus')"
          :disabled="isQueryingYk"
          class="mt-1.5 flex items-center gap-1 text-xs text-orange-600 dark:text-orange-400 hover:underline disabled:opacity-50"
        >
          <svg v-if="isQueryingYk" class="animate-spin h-3 w-3" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
          </svg>
          <svg v-else class="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
          </svg>
          {{ isQueryingYk ? 'Sorgulanıyor...' : 'Durumu Sorgula' }}
        </button>
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
import { computed } from 'vue'

interface ShipmentInfo {
  projectName: string
  deliveryDate: string
  zoneName?: string
  externalOrderNumber?: string
  talepNo?: string
  talepTuru?: string
  institutionCode?: string
  teslimAlacakKisiler?: string
  teslimAlacakTelefon?: string
  yoneticiMail?: string
  aciklama?: string
  ykCargoKey?: string | null
  ykJobId?: number | null
  ykOperationStatus?: string | null
  ykErrorCode?: string | null
  ykErrorMessage?: string | null
  ykLastQueryAt?: string | null
}

const props = defineProps<{ shipment: ShipmentInfo; isQueryingYk?: boolean }>()
defineEmits<{ queryYkStatus: [] }>()

const YK_STATUS_LABELS: Record<string, string> = {
  NOP: 'İşlem Görmemiş',
  IND: 'Kargo Teslimatta',
  ISR: 'Fatura Düzenlenmedi',
  CNL: 'İptal Edildi',
  ISC: 'Daha Önce İptal',
  DLV: 'Teslim Edildi',
  BI:  'Şube İptal',
}

const isYkSuccess = computed(() =>
  props.shipment.ykOperationStatus != null &&
  ['NOP', 'IND', 'ISR', 'DLV'].includes(props.shipment.ykOperationStatus.toUpperCase())
)

const ykStatusLabel = computed(() =>
  props.shipment.ykOperationStatus
    ? (YK_STATUS_LABELS[props.shipment.ykOperationStatus] ?? props.shipment.ykOperationStatus)
    : ''
)

function formatDate(val: string): string {
  return new Date(val).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })
}
</script>
