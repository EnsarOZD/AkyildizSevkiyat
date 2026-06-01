<template>
  <div class="space-y-6">
    <!-- KPI row 1: Sipariş → Netsis akışı -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
      <router-link to="/orders/import"
        class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 hover:border-blue-300 hover:shadow-sm transition-all">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Bugünkü ISS Sipariş</p>
          <div class="w-8 h-8 rounded-lg bg-blue-50 dark:bg-blue-900/30 flex items-center justify-center">
            <InboxArrowDownIcon class="w-4 h-4 text-blue-600" />
          </div>
        </div>
        <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stats.newIssOrdersTodayCount }}</p>
      </router-link>

      <router-link to="/stocks/mappings"
        class="rounded-xl border p-4 hover:shadow-sm transition-all"
        :class="stats.pendingStockMappingCount > 0
          ? 'bg-yellow-50 dark:bg-yellow-900/10 border-yellow-300 dark:border-yellow-700'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="stats.pendingStockMappingCount > 0 ? 'text-yellow-700 dark:text-yellow-400' : 'text-gray-500 dark:text-gray-400'">
            Eşleşme Bekleyen
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="stats.pendingStockMappingCount > 0 ? 'bg-yellow-100 dark:bg-yellow-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <ArrowsRightLeftIcon class="w-4 h-4"
              :class="stats.pendingStockMappingCount > 0 ? 'text-yellow-600' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="stats.pendingStockMappingCount > 0 ? 'text-yellow-700 dark:text-yellow-300' : 'text-gray-900 dark:text-gray-100'">
          {{ stats.pendingStockMappingCount }}
        </p>
      </router-link>

      <router-link to="/netsis/reconciliation"
        class="rounded-xl border p-4 hover:shadow-sm transition-all"
        :class="stats.pendingNetsisTransferCount > 0
          ? 'bg-orange-50 dark:bg-orange-900/10 border-orange-300 dark:border-orange-700'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="stats.pendingNetsisTransferCount > 0 ? 'text-orange-700 dark:text-orange-400' : 'text-gray-500 dark:text-gray-400'">
            Netsis Aktarım Bekleyen
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="stats.pendingNetsisTransferCount > 0 ? 'bg-orange-100 dark:bg-orange-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <DocumentCheckIcon class="w-4 h-4"
              :class="stats.pendingNetsisTransferCount > 0 ? 'text-orange-600' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="stats.pendingNetsisTransferCount > 0 ? 'text-orange-700 dark:text-orange-300' : 'text-gray-900 dark:text-gray-100'">
          {{ stats.pendingNetsisTransferCount }}
        </p>
      </router-link>

      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Bugün Yola Çıkan</p>
          <div class="w-8 h-8 rounded-lg bg-green-50 dark:bg-green-900/30 flex items-center justify-center">
            <TruckIcon class="w-4 h-4 text-green-600" />
          </div>
        </div>
        <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stats.todayDispatchedCount }}</p>
      </div>
    </div>

    <!-- KPI row 2: İade & eksik -->
    <div class="grid grid-cols-2 lg:grid-cols-3 gap-4">
      <router-link to="/floating-returns"
        class="rounded-xl border p-4 hover:shadow-sm transition-all"
        :class="stats.pendingFloatingReturns > 0
          ? 'bg-orange-50 dark:bg-orange-900/10 border-orange-300 dark:border-orange-700'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="stats.pendingFloatingReturns > 0 ? 'text-orange-700 dark:text-orange-400' : 'text-gray-500 dark:text-gray-400'">
            Belirsiz İade
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="stats.pendingFloatingReturns > 0 ? 'bg-orange-100 dark:bg-orange-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <ArrowPathIcon class="w-4 h-4"
              :class="stats.pendingFloatingReturns > 0 ? 'text-orange-600' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="stats.pendingFloatingReturns > 0 ? 'text-orange-700 dark:text-orange-300' : 'text-gray-900 dark:text-gray-100'">
          {{ stats.pendingFloatingReturns }}
        </p>
      </router-link>

      <div class="rounded-xl border p-4"
        :class="stats.missingItemsMailPendingCount > 0
          ? 'bg-amber-50 dark:bg-amber-900/10 border-amber-300 dark:border-amber-700'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="stats.missingItemsMailPendingCount > 0 ? 'text-amber-700 dark:text-amber-400' : 'text-gray-500 dark:text-gray-400'">
            Eksik Malzeme Mail
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="stats.missingItemsMailPendingCount > 0 ? 'bg-amber-100 dark:bg-amber-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <EnvelopeIcon class="w-4 h-4"
              :class="stats.missingItemsMailPendingCount > 0 ? 'text-amber-600' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="stats.missingItemsMailPendingCount > 0 ? 'text-amber-700 dark:text-amber-300' : 'text-gray-900 dark:text-gray-100'">
          {{ stats.missingItemsMailPendingCount }}
        </p>
      </div>

      <router-link :to="`/shipments?startDate=${today}&endDate=${today}&statuses=0,1,2`"
        class="rounded-xl border p-4 hover:shadow-sm transition-all"
        :class="stats.todayShipmentsNotReadyCount > 0
          ? 'bg-red-50/40 dark:bg-red-900/10 border-red-200 dark:border-red-800'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="stats.todayShipmentsNotReadyCount > 0 ? 'text-red-500' : 'text-gray-500 dark:text-gray-400'">
            Bugün Hazır Değil
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="stats.todayShipmentsNotReadyCount > 0 ? 'bg-red-100 dark:bg-red-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <XCircleIcon class="w-4 h-4"
              :class="stats.todayShipmentsNotReadyCount > 0 ? 'text-red-500' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="stats.todayShipmentsNotReadyCount > 0 ? 'text-red-600' : 'text-gray-900 dark:text-gray-100'">
          {{ stats.todayShipmentsNotReadyCount }}
        </p>
      </router-link>
    </div>

    <!-- Netsis bekleyen + Eksik mail bekleyen -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">Netsis Aktarımı Bekleyen</h2>
            <p class="text-xs text-gray-400 mt-0.5">Depodan çıkmış, Netsis'e aktarılmamış</p>
          </div>
          <span class="px-2 py-0.5 rounded-full text-xs font-medium bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-300">
            {{ stats.pendingNetsisTransferCount }}
          </span>
        </div>
        <div v-if="stats.pendingNetsisShipments.length === 0" class="flex flex-col items-center justify-center py-8 text-gray-400">
          <CheckCircleIcon class="w-10 h-10 mb-2 opacity-40" />
          <p class="text-sm">Bekleyen Netsis aktarımı yok</p>
        </div>
        <div v-else class="divide-y divide-gray-100 dark:divide-gray-700">
          <router-link
            v-for="s in stats.pendingNetsisShipments"
            :key="s.id"
            :to="`/shipments/${s.id}`"
            class="flex items-center gap-3 py-2.5 hover:bg-gray-50 dark:hover:bg-gray-800 -mx-2 px-2 rounded-lg transition-colors"
          >
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200 truncate">{{ s.projectName }}</p>
              <p class="text-xs text-gray-400 truncate">{{ s.irsaliyeNo ? `İrs: ${s.irsaliyeNo}` : s.talepNo }}</p>
            </div>
            <span class="px-2 py-0.5 rounded-full text-[11px] font-medium" :class="statusBadge(s.status)">
              {{ statusLabel(s.status) }}
            </span>
          </router-link>
        </div>
      </div>

      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">Eksik Malzeme Bildirimi Bekleyen</h2>
            <p class="text-xs text-gray-400 mt-0.5">Teslim sonrası mail gönderilmemiş</p>
          </div>
          <span class="px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-300">
            {{ stats.missingItemsMailPendingCount }}
          </span>
        </div>
        <div v-if="stats.missingItemsPendingShipments.length === 0" class="flex flex-col items-center justify-center py-8 text-gray-400">
          <CheckCircleIcon class="w-10 h-10 mb-2 opacity-40" />
          <p class="text-sm">Bekleyen eksik malzeme bildirimi yok</p>
        </div>
        <div v-else class="divide-y divide-gray-100 dark:divide-gray-700">
          <router-link
            v-for="s in stats.missingItemsPendingShipments"
            :key="s.id"
            :to="`/shipments/${s.id}`"
            class="flex items-center gap-3 py-2.5 hover:bg-gray-50 dark:hover:bg-gray-800 -mx-2 px-2 rounded-lg transition-colors"
          >
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200 truncate">{{ s.projectName }}</p>
              <p class="text-xs text-gray-400 truncate">{{ s.irsaliyeNo ? `İrs: ${s.irsaliyeNo}` : s.talepNo }}</p>
            </div>
            <p class="text-[10px] text-gray-400 flex-shrink-0">{{ formatShortDate(s.deliveryDate) }}</p>
          </router-link>
        </div>
      </div>
    </div>

    <!-- Yaklaşan sevkiyatlar + son ISS import -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
      <div class="lg:col-span-2 bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">Yaklaşan Sevkiyatlar</h2>
          <router-link to="/shipments" class="text-xs text-blue-600 hover:underline">Tümünü gör</router-link>
        </div>
        <div v-if="stats.recentShipments.length === 0" class="flex flex-col items-center justify-center py-8 text-gray-400">
          <ClipboardDocumentListIcon class="w-10 h-10 mb-2 opacity-40" />
          <p class="text-sm">Aktif sevkiyat bulunamadı</p>
        </div>
        <div v-else class="divide-y divide-gray-100 dark:divide-gray-700">
          <router-link
            v-for="s in stats.recentShipments"
            :key="s.id"
            :to="`/shipments/${s.id}`"
            class="flex items-center gap-3 py-2.5 hover:bg-gray-50 dark:hover:bg-gray-800 -mx-2 px-2 rounded-lg transition-colors"
          >
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200 truncate">{{ s.projectName }}</p>
              <p class="text-xs text-gray-400 truncate">{{ s.talepNo }}</p>
            </div>
            <div class="flex flex-col items-end gap-1 flex-shrink-0">
              <span class="px-2 py-0.5 rounded-full text-[11px] font-medium" :class="statusBadge(s.status)">
                {{ statusLabel(s.status) }}
              </span>
              <p class="text-[10px] text-gray-400">{{ formatShortDate(s.deliveryDate) }}</p>
            </div>
          </router-link>
        </div>
      </div>

      <!-- Son ISS Import durumu -->
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">Son ISS Import</h2>
          <router-link to="/orders/import" class="text-xs text-blue-600 hover:underline">Detay</router-link>
        </div>
        <div v-if="!stats.lastIssImportBatch" class="flex flex-col items-center justify-center py-8 text-gray-400">
          <SignalIcon class="w-10 h-10 mb-2 opacity-40" />
          <p class="text-sm">Henüz import yapılmamış</p>
        </div>
        <div v-else class="space-y-3">
          <div class="flex items-center gap-2 text-sm">
            <span class="px-2 py-0.5 rounded-full text-[11px] font-medium"
              :class="batchStatusBadge(stats.lastIssImportBatch.status)">
              {{ batchStatusLabel(stats.lastIssImportBatch.status) }}
            </span>
            <span class="text-gray-500 dark:text-gray-400">#{{ stats.lastIssImportBatch.id }}</span>
          </div>
          <p class="text-xs text-gray-400">{{ formatDateTime(stats.lastIssImportBatch.startedAt) }}</p>
          <div class="grid grid-cols-3 gap-2 pt-2 border-t border-gray-100 dark:border-gray-700">
            <div>
              <p class="text-[10px] uppercase tracking-wide text-gray-400">Yeni</p>
              <p class="text-lg font-bold text-gray-800 dark:text-gray-200">{{ stats.lastIssImportBatch.newCount }}</p>
            </div>
            <div>
              <p class="text-[10px] uppercase tracking-wide text-gray-400">Eşlemesiz</p>
              <p class="text-lg font-bold"
                :class="stats.lastIssImportBatch.needsMappingCount > 0 ? 'text-yellow-600' : 'text-gray-800 dark:text-gray-200'">
                {{ stats.lastIssImportBatch.needsMappingCount }}
              </p>
            </div>
            <div>
              <p class="text-[10px] uppercase tracking-wide text-gray-400">Hata</p>
              <p class="text-lg font-bold"
                :class="stats.lastIssImportBatch.failedCount > 0 ? 'text-red-600' : 'text-gray-800 dark:text-gray-200'">
                {{ stats.lastIssImportBatch.failedCount }}
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import {
  InboxArrowDownIcon, ArrowsRightLeftIcon, DocumentCheckIcon, TruckIcon,
  ArrowPathIcon, EnvelopeIcon, XCircleIcon, CheckCircleIcon,
  ClipboardDocumentListIcon, SignalIcon,
} from '@heroicons/vue/24/outline';
import type { DashboardStats } from '../../services/dashboardService';
import { statusLabel, statusBadge, formatShortDate, formatDateTime } from '../../utils/shipmentStatusUi';

defineProps<{ stats: DashboardStats }>();

function dateStr() {
  return new Date().toISOString().slice(0, 10);
}
const today = dateStr();

const BATCH_STATUS_LABEL: Record<string, string> = {
  Running:        'Çalışıyor',
  Completed:      'Başarılı',
  Failed:         'Hatalı',
  PartialSuccess: 'Kısmi',
};
const BATCH_STATUS_BADGE: Record<string, string> = {
  Running:        'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300',
  Completed:      'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300',
  Failed:         'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
  PartialSuccess: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
};
const batchStatusLabel = (s: string) => BATCH_STATUS_LABEL[s] ?? s;
const batchStatusBadge = (s: string) => BATCH_STATUS_BADGE[s] ?? 'bg-gray-100 text-gray-700';
</script>
