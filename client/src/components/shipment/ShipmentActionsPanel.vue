<template>
  <div class="w-full lg:w-72 shrink-0 lg:sticky lg:top-4 space-y-4 min-w-0 max-w-full">

    <!-- Status card -->
    <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4 w-full">
      <div class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">Durum</div>
      <StatusBadge :status="shipment.status" type="shipment" />
      <div v-if="!shipment.zoneId" class="mt-3 text-xs text-red-600 bg-red-50 border border-red-100 rounded px-2 py-1.5 flex items-center gap-1">
        ⚠️ Bölge henüz atanmamış
      </div>
    </div>

    <!-- Actions card -->
    <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4 w-full">
      <div class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">İşlemler</div>
      <div class="space-y-2">

        <!-- Kıyafet: Netsis'e Gönder -->
        <button
          v-if="shipment.operationTypeValue === 1 && shipment.status === 'Created' && !shipment.netsisTransferredAt"
          v-role="['Admin', 'Manager', 'Accounting', 'Driver']"
          @click="$emit('exportClothing')"
          :disabled="clothingExportLoading"
          class="w-full bg-purple-600 text-white py-2 px-4 rounded-lg hover:bg-purple-700 transition text-sm font-bold disabled:opacity-50"
        >
          <span v-if="clothingExportLoading">Netsis'e aktarılıyor...</span>
          <span v-else>Netsis'e Gönder (Kıyafet)</span>
        </button>

        <!-- Kıyafet aktarıldı badge -->
        <div
          v-if="shipment.operationTypeValue === 1 && shipment.netsisTransferredAt && shipment.irsaliyeNo"
          class="w-full bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-700 rounded-lg px-3 py-2 text-xs text-green-700 dark:text-green-300 font-medium"
        >
          ✓ Netsis'e aktarıldı<br/>
          <span class="font-mono">{{ shipment.irsaliyeNo }}</span>
        </div>

        <!-- İrsaliye Yenile: aktarılmış ama irsaliye yok -->
        <button
          v-if="shipment.netsisTransferredAt && !shipment.irsaliyeNo"
          v-role="['Admin', 'Manager', 'Accounting', 'Driver']"
          @click="$emit('fetchIrsaliye')"
          :disabled="irsaliyeFetchLoading"
          class="w-full border border-indigo-400 text-indigo-600 py-2 px-4 rounded-lg hover:bg-indigo-50 transition text-sm font-medium disabled:opacity-50"
        >
          <span v-if="irsaliyeFetchLoading">İrsaliye çekiliyor...</span>
          <span v-else>İrsaliye Yenile</span>
        </button>

        <!-- Admin: irsaliye varsa bile yeniden sorgula -->
        <button
          v-if="shipment.netsisTransferredAt && shipment.irsaliyeNo"
          v-role="['Admin']"
          @click="$emit('fetchIrsaliye')"
          :disabled="irsaliyeFetchLoading"
          class="w-full border border-gray-300 text-gray-500 py-1.5 px-4 rounded-lg hover:bg-gray-50 transition text-xs font-medium disabled:opacity-50"
          :title="`Mevcut: ${shipment.irsaliyeNo} — Netsis'ten yeniden sorgula`"
        >
          <span v-if="irsaliyeFetchLoading">Sorgulanıyor...</span>
          <span v-else>↺ İrsaliye No Yenile</span>
        </button>

        <button
          v-if="shipment.status === 'Created'"
          v-role="['Admin', 'Accounting']"
          @click="$emit('openEdit')"
          class="w-full bg-gray-600 text-white py-2 px-4 rounded-lg hover:bg-gray-700 transition text-sm font-medium"
        >Siparişi Düzenle</button>

        <button
          v-if="shipment.status === 'Created' && !shipment.zoneId"
          v-role="['Admin', 'Manager', 'Accounting']"
          @click="$emit('openZone')"
          class="w-full bg-orange-500 text-white py-2 px-4 rounded-lg hover:bg-orange-600 transition text-sm font-medium"
        >⚠️ Bölge Ata (Gerekli)</button>

        <!-- Depoya Ata — Kıyafet için gizle -->
        <button
          v-if="shipment.status === 'Created' && shipment.operationTypeValue !== 1"
          v-role="['Admin', 'Accounting', 'Manager']"
          @click="$emit('assignWarehouse')"
          class="w-full bg-blue-600 text-white py-2 px-4 rounded-lg hover:bg-blue-700 transition text-sm font-medium"
        >Depoya Ata</button>

        <!-- Sevke Hazır — Kıyafet için gizle -->
        <button
          v-if="['Picking', 'AssignedToWarehouse'].includes(shipment.status) && shipment.operationTypeValue !== 1"
          v-role="['Admin', 'Warehouse', 'Manager', 'Accounting']"
          @click="$emit('openMarkReady')"
          class="w-full bg-green-600 text-white py-2 px-4 rounded-lg hover:bg-green-700 transition text-sm font-bold"
        >Sevke Hazır İşaretle</button>

        <button
          v-if="['Picking', 'AssignedToWarehouse'].includes(shipment.status) && shipment.operationTypeValue !== 1"
          v-role="['Admin', 'Warehouse', 'Accounting']"
          @click="$emit('openQuantities')"
          class="w-full border border-blue-500 text-blue-600 py-2 px-4 rounded-lg hover:bg-blue-50 transition text-sm font-medium"
        >Miktarları Düzenle</button>

        <button
          v-if="shipment.status === 'ReadyForDispatch'"
          v-role="['Admin', 'Driver', 'Manager', 'Accounting']"
          @click="$emit('openAssignVehicle')"
          :disabled="!shipment.zoneId"
          class="w-full text-white py-2 px-4 rounded-lg transition text-sm font-medium"
          :class="!shipment.zoneId ? 'bg-gray-300 cursor-not-allowed' : 'bg-indigo-600 hover:bg-indigo-700'"
        >Araca Ata</button>

        <button
          v-if="shipment.status === 'AssignedToVehicle'"
          v-role="['Admin', 'Driver', 'Manager', 'Accounting']"
          @click="$emit('openDelivery')"
          class="w-full bg-green-600 text-white py-2 px-4 rounded-lg hover:bg-green-700 transition text-sm font-bold"
        >Teslim Edildi</button>

        <button
          v-if="['AssignedToVehicle', 'Delivered'].includes(shipment.status)"
          v-role="['Admin', 'Driver', 'Manager', 'Accounting', 'Warehouse']"
          @click="$emit('openVehicleReturn')"
          class="w-full bg-orange-500 text-white py-2 px-4 rounded-lg hover:bg-orange-600 transition text-sm font-medium"
        >Araç İadesi Kaydet</button>

        <!-- Taslağa Geri Al -->
        <button
          v-if="['AssignedToWarehouse', 'Picking', 'ReadyForDispatch'].includes(shipment.status)"
          v-role="['Admin', 'Manager', 'Accounting', 'Warehouse']"
          @click="$emit('openRevert')"
          class="w-full border border-red-400 text-red-600 dark:text-red-400 py-2 px-4 rounded-lg hover:bg-red-50 dark:hover:bg-red-900/20 transition text-sm font-medium"
        >Taslağa Geri Al / İptal</button>

        <!-- Admin Sıfırla -->
        <button
          v-if="['ReturnedToWarehouse', 'Delivered', 'Cancelled'].includes(shipment.status)"
          v-role="['Admin', 'Manager']"
          @click="$emit('openAdminReset')"
          class="w-full border border-red-500 text-red-600 dark:text-red-400 py-2 px-4 rounded-lg hover:bg-red-50 dark:hover:bg-red-900/20 transition text-sm font-medium"
        >⚙️ Sıfırla & Siparişi Serbest Bırak</button>

        <p
          v-if="['ReturnedToWarehouse', 'Cancelled'].includes(shipment.status)"
          class="text-xs text-gray-400 text-center py-2"
        >
          Bu sevkiyat için açık aksiyon yok.
        </p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import StatusBadge from '../StatusBadge.vue'

interface ShipmentActions {
  status: string
  zoneId?: number
  operationTypeValue?: number
  netsisTransferredAt?: string
  irsaliyeNo?: string
}

defineProps<{
  shipment: ShipmentActions
  clothingExportLoading?: boolean
  irsaliyeFetchLoading?: boolean
}>()

defineEmits<{
  exportClothing: []
  fetchIrsaliye: []
  openEdit: []
  openZone: []
  assignWarehouse: []
  openMarkReady: []
  openQuantities: []
  openAssignVehicle: []
  openDelivery: []
  openVehicleReturn: []
  openRevert: []
  openAdminReset: []
}>()
</script>
