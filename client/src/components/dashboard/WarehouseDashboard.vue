<template>
  <div class="space-y-6">
    <!-- KPI row 1: Hazırlık & alımlar -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
      <router-link to="/warehouse"
        class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 hover:border-blue-300 hover:shadow-sm transition-all">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Depo Hazırlık</p>
          <div class="w-8 h-8 rounded-lg bg-blue-50 dark:bg-blue-900/30 flex items-center justify-center">
            <BuildingStorefrontIcon class="w-4 h-4 text-blue-600" />
          </div>
        </div>
        <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stats.statusWarehouse }}</p>
      </router-link>

      <router-link to="/warehouse"
        class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 hover:border-yellow-300 hover:shadow-sm transition-all">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Toplama</p>
          <div class="w-8 h-8 rounded-lg bg-yellow-50 dark:bg-yellow-900/30 flex items-center justify-center">
            <ClipboardDocumentListIcon class="w-4 h-4 text-yellow-600" />
          </div>
        </div>
        <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stats.statusPicking }}</p>
      </router-link>

      <router-link to="/goods-receipts/intake"
        class="rounded-xl border p-4 hover:shadow-sm transition-all"
        :class="stats.pendingGoodsReceiptsCount > 0
          ? 'bg-amber-50 dark:bg-amber-900/10 border-amber-300 dark:border-amber-700'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="stats.pendingGoodsReceiptsCount > 0 ? 'text-amber-700 dark:text-amber-400' : 'text-gray-500 dark:text-gray-400'">
            Bekleyen Mal Kabul
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="stats.pendingGoodsReceiptsCount > 0 ? 'bg-amber-100 dark:bg-amber-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <InboxArrowDownIcon class="w-4 h-4"
              :class="stats.pendingGoodsReceiptsCount > 0 ? 'text-amber-600' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="stats.pendingGoodsReceiptsCount > 0 ? 'text-amber-700 dark:text-amber-300' : 'text-gray-900 dark:text-gray-100'">
          {{ stats.pendingGoodsReceiptsCount }}
        </p>
      </router-link>

      <div class="rounded-xl border p-4"
        :class="stats.criticalStockCount > 0
          ? 'bg-red-50 dark:bg-red-900/10 border-red-300 dark:border-red-700'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="stats.criticalStockCount > 0 ? 'text-red-700 dark:text-red-400' : 'text-gray-500 dark:text-gray-400'">
            Kritik Stok
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="stats.criticalStockCount > 0 ? 'bg-red-100 dark:bg-red-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <ArchiveBoxIcon class="w-4 h-4"
              :class="stats.criticalStockCount > 0 ? 'text-red-600' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="stats.criticalStockCount > 0 ? 'text-red-700 dark:text-red-300' : 'text-gray-900 dark:text-gray-100'">
          {{ stats.criticalStockCount }}
        </p>
      </div>
    </div>

    <!-- KPI row 2: Bugün hazırlık & PO & iade -->
    <div class="grid grid-cols-2 lg:grid-cols-3 gap-4">
      <router-link :to="`/shipments?startDate=${today}&endDate=${today}&statuses=0,1,2`"
        class="rounded-xl border p-4 hover:shadow-sm transition-all"
        :class="stats.todayShipmentsNotReadyCount > 0
          ? 'bg-red-50/40 dark:bg-red-900/10 border-red-200 dark:border-red-800'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="stats.todayShipmentsNotReadyCount > 0 ? 'text-red-500' : 'text-gray-500 dark:text-gray-400'">
            Bugün Hazırlanması Gereken
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="stats.todayShipmentsNotReadyCount > 0 ? 'bg-red-100 dark:bg-red-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <ClockIcon class="w-4 h-4"
              :class="stats.todayShipmentsNotReadyCount > 0 ? 'text-red-500' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="stats.todayShipmentsNotReadyCount > 0 ? 'text-red-600' : 'text-gray-900 dark:text-gray-100'">
          {{ stats.todayShipmentsNotReadyCount }}
        </p>
      </router-link>

      <router-link to="/purchase-orders"
        class="rounded-xl border p-4 hover:shadow-sm transition-all"
        :class="stats.pendingPOApprovalCount > 0
          ? 'bg-yellow-50 dark:bg-yellow-900/10 border-yellow-300 dark:border-yellow-700'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="stats.pendingPOApprovalCount > 0 ? 'text-yellow-700 dark:text-yellow-400' : 'text-gray-500 dark:text-gray-400'">
            Onay Bekleyen PO
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="stats.pendingPOApprovalCount > 0 ? 'bg-yellow-100 dark:bg-yellow-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <ShoppingCartIcon class="w-4 h-4"
              :class="stats.pendingPOApprovalCount > 0 ? 'text-yellow-600' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="stats.pendingPOApprovalCount > 0 ? 'text-yellow-700 dark:text-yellow-300' : 'text-gray-900 dark:text-gray-100'">
          {{ stats.pendingPOApprovalCount }}
        </p>
      </router-link>

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
    </div>

    <!-- Quick action -->
    <div class="flex flex-wrap gap-3">
      <router-link to="/purchase-orders"
        class="inline-flex items-center gap-2 px-4 py-2.5 rounded-lg bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium transition-colors">
        <PlusIcon class="w-4 h-4" />
        Yeni Satınalma Siparişi
      </router-link>
      <router-link to="/goods-receipts/intake"
        class="inline-flex items-center gap-2 px-4 py-2.5 rounded-lg bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700 text-gray-700 dark:text-gray-200 text-sm font-medium transition-colors">
        <InboxArrowDownIcon class="w-4 h-4" />
        Mal Kabul
      </router-link>
    </div>

    <!-- Aktif bölge hazırlıkları + Bugün hazırlık gereken -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">Aktif Bölge Hazırlıkları</h2>
            <p class="text-xs text-gray-400 mt-0.5">Açık ZP'ler, durum bazlı</p>
          </div>
          <router-link to="/warehouse" class="text-xs text-blue-600 hover:underline">Tümünü gör</router-link>
        </div>
        <div v-if="stats.activeZonePreparations.length === 0" class="flex flex-col items-center justify-center py-8 text-gray-400">
          <CheckCircleIcon class="w-10 h-10 mb-2 opacity-40" />
          <p class="text-sm">Aktif bölge hazırlığı yok</p>
        </div>
        <div v-else class="divide-y divide-gray-100 dark:divide-gray-700">
          <router-link
            v-for="zp in stats.activeZonePreparations"
            :key="zp.id"
            to="/warehouse"
            class="flex items-center gap-3 py-2.5 hover:bg-gray-50 dark:hover:bg-gray-800 -mx-2 px-2 rounded-lg transition-colors"
          >
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2">
                <p class="text-sm font-medium text-gray-800 dark:text-gray-200 truncate">{{ zp.zoneName }}</p>
                <span class="text-xs text-gray-400">{{ zp.shipmentCount }} sevkiyat</span>
              </div>
              <p class="text-xs text-gray-400 truncate mt-0.5">
                <span v-if="zp.driverName">{{ zp.driverName }}</span>
                <span v-if="zp.driverName && zp.plateNumber"> · </span>
                <span v-if="zp.plateNumber">{{ zp.plateNumber }}</span>
                <span v-if="!zp.driverName && !zp.plateNumber">{{ formatShortDate(zp.deliveryDate) }}</span>
              </p>
            </div>
            <span class="px-2 py-0.5 rounded-full text-[11px] font-medium" :class="zonePrepBadge(zp.status)">
              {{ zonePrepStatusLabel(zp.status) }}
            </span>
          </router-link>
        </div>
      </div>

      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">Bugün Hazırlanması Gereken</h2>
            <p class="text-xs text-gray-400 mt-0.5">Bugün teslim, hâlâ hazır değil</p>
          </div>
          <span class="px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300">
            {{ stats.todayShipmentsNotReadyCount }}
          </span>
        </div>
        <div v-if="stats.todayPreparationNeededShipments.length === 0" class="flex flex-col items-center justify-center py-8 text-gray-400">
          <CheckCircleIcon class="w-10 h-10 mb-2 opacity-40" />
          <p class="text-sm">Bugünün hazırlığı tamamlanmış</p>
        </div>
        <div v-else class="divide-y divide-gray-100 dark:divide-gray-700">
          <router-link
            v-for="s in stats.todayPreparationNeededShipments"
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
    </div>

    <!-- Kritik stok widget -->
    <CriticalStockWidget :items="criticalStocks" :loading="criticalStocksLoading" />
  </div>
</template>

<script setup lang="ts">
import {
  BuildingStorefrontIcon, ClipboardDocumentListIcon, InboxArrowDownIcon, ArchiveBoxIcon,
  ClockIcon, ShoppingCartIcon, ArrowPathIcon, CheckCircleIcon, PlusIcon,
} from '@heroicons/vue/24/outline';
import type { DashboardStats, CriticalStockItem } from '../../services/dashboardService';
import { statusLabel, statusBadge, zonePrepStatusLabel, formatShortDate } from '../../utils/shipmentStatusUi';
import CriticalStockWidget from '../CriticalStockWidget.vue';

defineProps<{
  stats: DashboardStats;
  criticalStocks: CriticalStockItem[];
  criticalStocksLoading: boolean;
}>();

function dateStr() {
  return new Date().toISOString().slice(0, 10);
}
const today = dateStr();

const ZP_BADGE: Record<string, string> = {
  Draft:              'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-300',
  MicroPicking:       'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300',
  MicroReady:         'bg-cyan-100 text-cyan-700 dark:bg-cyan-900/40 dark:text-cyan-300',
  MacroPicking:       'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/40 dark:text-yellow-300',
  GidaHazirlik:       'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
  ReadyForDriverInfo: 'bg-purple-100 text-purple-700 dark:bg-purple-900/40 dark:text-purple-300',
  ReadyForTransfer:   'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300',
  Dispatched:         'bg-orange-100 text-orange-700 dark:bg-orange-900/40 dark:text-orange-300',
};
const zonePrepBadge = (s: string) => ZP_BADGE[s] ?? 'bg-gray-100 text-gray-600';
</script>
