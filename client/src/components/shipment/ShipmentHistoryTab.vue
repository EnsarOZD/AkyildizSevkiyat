<template>
  <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
    <ul class="divide-y divide-gray-200 dark:divide-gray-700">
      <li v-for="(entry, index) in history" :key="index" class="px-5 py-4">
        <div class="flex items-start justify-between gap-4">
          <div>
            <p class="text-sm text-gray-800 dark:text-gray-200">
              <strong>{{ formatStatus(entry.oldStatus) }}</strong>
              <span class="mx-1 text-gray-400">→</span>
              <strong>{{ formatStatus(entry.newStatus) }}</strong>
            </p>
            <p v-if="entry.description" class="text-xs text-gray-500 dark:text-gray-400 mt-0.5 italic">
              {{ entry.description }}
            </p>
            <p class="text-xs text-gray-400 mt-1">{{ entry.changedBy }}</p>
          </div>
          <span class="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap shrink-0">
            {{ new Date(entry.changedAt).toLocaleString('tr-TR') }}
          </span>
        </div>
      </li>
      <li v-if="history.length === 0" class="px-5 py-10 text-center text-sm text-gray-400">
        Henüz tarihçe kaydı yok.
      </li>
    </ul>

    <!-- Yazdırma Geçmişi -->
    <div v-if="printLogs.length > 0" class="border-t border-gray-200 dark:border-gray-700">
      <div class="px-5 py-3 bg-gray-50 dark:bg-gray-800 flex items-center gap-2">
        <svg class="w-4 h-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
        </svg>
        <span class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">
          Yazdırma Geçmişi ({{ printLogs.length }})
        </span>
      </div>
      <ul class="divide-y divide-gray-100 dark:divide-gray-700">
        <li
          v-for="log in printLogs"
          :key="log.id"
          class="px-5 py-3 flex items-center justify-between gap-4"
        >
          <span class="font-medium text-sm text-gray-700 dark:text-gray-300">{{ log.printedByName }}</span>
          <span class="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap shrink-0">
            {{ new Date(log.printedAt).toLocaleString('tr-TR') }}
          </span>
        </li>
      </ul>
    </div>
  </div>
</template>

<script setup lang="ts">
interface HistoryEntry {
  oldStatus: string
  newStatus: string
  changedAt: string
  changedBy: string
  description?: string
}

interface PrintLog {
  id: number
  printedAt: string
  printedByName: string
}

defineProps<{
  history: HistoryEntry[]
  printLogs: PrintLog[]
}>()

const STATUS_LABELS: Record<string, string> = {
  Draft: 'Taslak',
  Warehouse: 'Depoda',
  Picking: 'Toplamada',
  ReadyForDispatch: 'Hazır',
  Ready: 'Hazır',
  AssignedToVehicle: 'Araçta',
  Preparing: 'Hazırlanıyor',
  Delivered: 'Teslim Edildi',
  Returned: 'İade Edildi',
  Cancelled: 'İptal',
  Created: 'Oluşturuldu',
}

function formatStatus(s: string): string {
  return STATUS_LABELS[s] ?? s
}
</script>
