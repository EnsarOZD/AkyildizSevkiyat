<template>
  <div class="p-6 space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-semibold text-gray-900 dark:text-gray-100">Dashboard</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">Hoşgeldiniz, {{ authStore.userName || authStore.userEmail }}</p>
      </div>
      <button
        @click="load"
        class="flex items-center gap-1.5 px-3 py-1.5 text-sm text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg transition-colors"
      >
        <ArrowPathIcon class="w-4 h-4" :class="{ 'animate-spin': loading }" />
        Yenile
      </button>
    </div>

    <!-- Loading skeleton -->
    <template v-if="loading && !stats">
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div v-for="i in 4" :key="i" class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 animate-pulse">
          <div class="h-3 bg-gray-200 dark:bg-gray-700 rounded w-24 mb-3"></div>
          <div class="h-8 bg-gray-200 dark:bg-gray-700 rounded w-16"></div>
        </div>
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5 animate-pulse h-48"></div>
        <div class="lg:col-span-2 bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5 animate-pulse h-48"></div>
      </div>
    </template>

    <template v-else-if="stats">
      <!-- Warehouse KPI row -->
      <div v-if="isWarehouse" class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Depo Hazırlık (Toplama) -->
        <router-link to="/warehouse" class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 hover:border-blue-300 hover:shadow-sm transition-all group">
          <div class="flex items-center justify-between mb-3">
            <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Depo Hazırlık</p>
            <div class="w-8 h-8 rounded-lg bg-blue-50 flex items-center justify-center">
              <ClipboardDocumentListIcon class="w-4 h-4 text-blue-600" />
            </div>
          </div>
          <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stats.statusWarehouse }}</p>
        </router-link>

        <!-- Bekleyen Mal Girişi -->
        <router-link to="/goods-receipts" class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 hover:border-yellow-300 hover:shadow-sm transition-all group">
          <div class="flex items-center justify-between mb-3">
            <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Bekleyen İrsaliye</p>
            <div class="w-8 h-8 rounded-lg bg-yellow-50 flex items-center justify-center">
              <InboxArrowDownIcon class="w-4 h-4 text-yellow-600" />
            </div>
          </div>
          <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stats.pendingGoodsReceiptsCount }}</p>
        </router-link>

        <!-- Belirsiz İade -->
        <router-link to="/floating-returns" class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 hover:border-orange-300 hover:shadow-sm transition-all group">
          <div class="flex items-center justify-between mb-3">
            <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Belirsiz İadeler</p>
            <div class="w-8 h-8 rounded-lg bg-orange-50 flex items-center justify-center">
              <ExclamationCircleIcon class="w-4 h-4 text-orange-600" />
            </div>
          </div>
          <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stats.pendingFloatingReturns }}</p>
        </router-link>
        
        <!-- Kritik Stok -->
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 hover:border-red-300 hover:shadow-sm transition-all group">
          <div class="flex items-center justify-between mb-3">
            <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Kritik Stok</p>
            <div class="w-8 h-8 rounded-lg bg-red-50 flex items-center justify-center">
              <ArchiveBoxIcon class="w-4 h-4 text-red-600" />
            </div>
          </div>
          <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stats.criticalStockCount }}</p>
        </div>
      </div>

      <!-- Primary KPI row for Managers/Admin -->
      <div v-if="!isWarehouse" class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Aktif Sevkiyat -->
        <router-link to="/shipments" class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 hover:border-blue-300 hover:shadow-sm transition-all group">
          <div class="flex items-center justify-between mb-3">
            <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Aktif Sevkiyat</p>
            <div class="w-8 h-8 rounded-lg bg-blue-50 flex items-center justify-center group-hover:bg-blue-100 transition-colors">
              <ClipboardDocumentListIcon class="w-4 h-4 text-blue-600" />
            </div>
          </div>
          <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stats.totalActiveShipments }}</p>
        </router-link>

        <!-- Bugün Teslim -->
        <router-link to="/shipments" class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 hover:border-green-300 hover:shadow-sm transition-all group">
          <div class="flex items-center justify-between mb-3">
            <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Bugün Teslim</p>
            <div class="w-8 h-8 rounded-lg bg-green-50 flex items-center justify-center group-hover:bg-green-100 transition-colors">
              <TruckIcon class="w-4 h-4 text-green-600" />
            </div>
          </div>
          <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stats.shipmentsToday }}</p>
        </router-link>

        <!-- Gecikmiş -->
        <router-link to="/shipments" class="bg-white dark:bg-gray-900 rounded-xl border p-4 hover:shadow-sm transition-all group"
          :class="stats.shipmentsOverdue > 0 ? 'border-red-200 bg-red-50/30' : 'border-gray-200 dark:border-gray-700'"
        >
          <div class="flex items-center justify-between mb-3">
            <p class="text-xs font-medium uppercase tracking-wide" :class="stats.shipmentsOverdue > 0 ? 'text-red-500' : 'text-gray-500 dark:text-gray-400'">Gecikmiş</p>
            <div class="w-8 h-8 rounded-lg flex items-center justify-center transition-colors"
              :class="stats.shipmentsOverdue > 0 ? 'bg-red-100' : 'bg-gray-50 dark:bg-gray-800 group-hover:bg-gray-100 dark:group-hover:bg-gray-700'"
            >
              <ExclamationCircleIcon class="w-4 h-4" :class="stats.shipmentsOverdue > 0 ? 'text-red-500' : 'text-gray-400'" />
            </div>
          </div>
          <p class="text-3xl font-bold" :class="stats.shipmentsOverdue > 0 ? 'text-red-600' : 'text-gray-900 dark:text-gray-100'">
            {{ stats.shipmentsOverdue }}
          </p>
        </router-link>

        <!-- Bu Hafta Teslim -->
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
          <div class="flex items-center justify-between mb-3">
            <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Bu Hafta Teslim</p>
            <div class="w-8 h-8 rounded-lg bg-purple-50 flex items-center justify-center">
              <CheckCircleIcon class="w-4 h-4 text-purple-600" />
            </div>
          </div>
          <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stats.shipmentsDeliveredThisWeek }}</p>
        </div>
      </div>

      <!-- Role-based KPI row -->
      <div v-if="showPendingPO || showNotReadyShipments" class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <!-- Onay Bekleyen PO (Manager, Admin, Accounting) -->
        <router-link
          v-if="showPendingPO"
          to="/purchase-orders"
          class="rounded-xl border p-4 hover:shadow-sm transition-all group"
          :class="stats.pendingPOApprovalCount > 0
            ? 'bg-yellow-50 dark:bg-yellow-900/10 border-yellow-300 dark:border-yellow-700'
            : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700 hover:border-yellow-200'"
        >
          <div class="flex items-center justify-between mb-3">
            <p class="text-xs font-medium uppercase tracking-wide"
              :class="stats.pendingPOApprovalCount > 0 ? 'text-yellow-600 dark:text-yellow-400' : 'text-gray-500 dark:text-gray-400'">
              Onay Bekleyen PO
            </p>
            <div class="w-8 h-8 rounded-lg flex items-center justify-center transition-colors"
              :class="stats.pendingPOApprovalCount > 0 ? 'bg-yellow-100 dark:bg-yellow-900/30' : 'bg-gray-50 dark:bg-gray-800'">
              <ShoppingCartIcon class="w-4 h-4"
                :class="stats.pendingPOApprovalCount > 0 ? 'text-yellow-600 dark:text-yellow-400' : 'text-gray-400'" />
            </div>
          </div>
          <p class="text-3xl font-bold"
            :class="stats.pendingPOApprovalCount > 0 ? 'text-yellow-700 dark:text-yellow-300' : 'text-gray-900 dark:text-gray-100'">
            {{ stats.pendingPOApprovalCount }}
          </p>
        </router-link>

        <!-- Bugün Hazır Olmayan Sevkiyat (Manager, Admin) -->
        <router-link
          v-if="showNotReadyShipments"
          to="/shipments"
          class="rounded-xl border p-4 hover:shadow-sm transition-all group"
          :class="stats.todayShipmentsNotReadyCount > 0
            ? 'bg-red-50/40 dark:bg-red-900/10 border-red-200 dark:border-red-800'
            : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700 hover:border-green-200'"
        >
          <div class="flex items-center justify-between mb-3">
            <p class="text-xs font-medium uppercase tracking-wide"
              :class="stats.todayShipmentsNotReadyCount > 0 ? 'text-red-500' : 'text-gray-500 dark:text-gray-400'">
              Bugün Hazır Olmayan
            </p>
            <div class="w-8 h-8 rounded-lg flex items-center justify-center transition-colors"
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

      <!-- Two-column layout -->
      <div v-if="!isWarehouse" class="grid grid-cols-1 lg:grid-cols-3 gap-6">

        <!-- Status breakdown -->
        <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
          <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">Durum Dağılımı</h2>
          <div class="space-y-2.5">
            <div v-for="row in statusRows" :key="row.label" class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <div class="w-2 h-2 rounded-full flex-shrink-0" :class="row.dot"></div>
                <span class="text-sm text-gray-600 dark:text-gray-400">{{ row.label }}</span>
              </div>
              <span class="text-sm font-semibold text-gray-800 dark:text-gray-200">{{ row.count }}</span>
            </div>
          </div>

          <!-- Alerts -->
          <div class="mt-5 pt-4 border-t border-gray-100 dark:border-gray-700 space-y-2">
            <router-link
              v-if="stats.pendingFloatingReturns > 0"
              to="/floating-returns"
              class="flex items-center gap-2 px-3 py-2 bg-orange-50 dark:bg-orange-900/20 border border-orange-200 dark:border-orange-800 rounded-lg text-sm text-orange-700 dark:text-orange-400 hover:bg-orange-100 dark:hover:bg-orange-900/30 transition-colors"
            >
              <ExclamationCircleIcon class="w-4 h-4 flex-shrink-0" />
              <span>{{ stats.pendingFloatingReturns }} belirsiz iade bekliyor</span>
            </router-link>
            <router-link
              v-if="stats.criticalStockCount > 0"
              to="/reports"
              class="flex items-center gap-2 px-3 py-2 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-lg text-sm text-red-700 dark:text-red-400 hover:bg-red-100 dark:hover:bg-red-900/30 transition-colors"
            >
              <ArchiveBoxIcon class="w-4 h-4 flex-shrink-0" />
              <span>{{ stats.criticalStockCount }} stok kritik seviyede</span>
            </router-link>
            <router-link
              v-if="stats.pendingGoodsReceiptsCount > 0"
              to="/goods-receipts"
              class="flex items-center gap-2 px-3 py-2 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-800 rounded-lg text-sm text-yellow-700 dark:text-yellow-400 hover:bg-yellow-100 dark:hover:bg-yellow-900/30 transition-colors"
            >
              <InboxArrowDownIcon class="w-4 h-4 flex-shrink-0" />
              <span>{{ stats.pendingGoodsReceiptsCount }} bekleyen mal girişi</span>
            </router-link>
          </div>
        </div>

        <!-- Recent shipments -->
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
                <span
                  class="px-2 py-0.5 rounded-full text-[11px] font-medium"
                  :class="statusClass(s.status)"
                >{{ statusLabel(s.status) }}</span>
                <p class="text-[10px] text-gray-400">{{ formatDate(s.deliveryDate) }}</p>
              </div>
            </router-link>
          </div>
        </div>

      </div>

      <!-- Critical Stocks Widget (Manager, Admin, Warehouse) -->
      <CriticalStockWidget
        v-if="showCriticalStocks"
        :items="criticalStocks"
        :loading="criticalStocksLoading"
      />

    </template>

    <!-- Error state -->
    <div v-else-if="error" class="flex flex-col items-center justify-center py-16">
      <ExclamationCircleIcon class="w-10 h-10 mb-2 text-red-400" />
      <p class="text-sm text-red-500">Dashboard verileri yüklenemedi.</p>
      <button @click="load" class="mt-3 text-sm text-blue-600 hover:underline">Tekrar dene</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { dashboardService, type DashboardStats, type CriticalStockItem } from '../services/dashboardService';
import CriticalStockWidget from '../components/CriticalStockWidget.vue';
import {
  ClipboardDocumentListIcon,
  TruckIcon,
  ExclamationCircleIcon,
  CheckCircleIcon,
  ArrowPathIcon,
  ArchiveBoxIcon,
  InboxArrowDownIcon,
  ShoppingCartIcon,
  XCircleIcon,
} from '@heroicons/vue/24/outline';

const authStore = useAuthStore();
const router = useRouter();

const stats = ref<DashboardStats | null>(null);
const loading = ref(false);
const error = ref(false);

const criticalStocks = ref<CriticalStockItem[]>([]);
const criticalStocksLoading = ref(false);

const isManagerOrAdmin = computed(() =>
  authStore.userRole === 'Manager' || authStore.userRole === 'Admin'
);
const isWarehouse = computed(() => authStore.userRole === 'Warehouse');
const isAccounting = computed(() => authStore.userRole === 'Accounting');
const showCriticalStocks = computed(() => isManagerOrAdmin.value || isWarehouse.value);
const showPendingPO = computed(() => isManagerOrAdmin.value || isAccounting.value);
const showNotReadyShipments = computed(() => isManagerOrAdmin.value);

const statusRows = computed(() => {
  if (!stats.value) return [];
  return [
    { label: 'Taslak',         count: stats.value.statusDraft,      dot: 'bg-gray-400' },
    { label: 'Depoda Hazırlık',count: stats.value.statusWarehouse,  dot: 'bg-blue-400' },
    { label: 'Toplama',        count: stats.value.statusPicking,    dot: 'bg-yellow-400' },
    { label: 'Sevkiyata Hazır',count: stats.value.statusReady,      dot: 'bg-green-400' },
    { label: 'Yolda',          count: stats.value.statusOnRoute,    dot: 'bg-purple-400' },
  ];
});

const statusLabelMap: Record<string, string> = {
  Created:             'Taslak',
  AssignedToWarehouse: 'Depoda',
  Picking:             'Toplama',
  ReadyForDispatch:    'Hazır',
  AssignedToVehicle:   'Yolda',
  Delivered:           'Teslim',
  ReturnedToWarehouse: 'İade',
  Cancelled:           'İptal',
};

const statusClassMap: Record<string, string> = {
  Created:             'bg-gray-100 text-gray-600',
  AssignedToWarehouse: 'bg-blue-100 text-blue-700',
  Picking:             'bg-yellow-100 text-yellow-700',
  ReadyForDispatch:    'bg-green-100 text-green-700',
  AssignedToVehicle:   'bg-purple-100 text-purple-700',
  Delivered:           'bg-emerald-100 text-emerald-700',
  ReturnedToWarehouse: 'bg-orange-100 text-orange-700',
  Cancelled:           'bg-red-100 text-red-600',
};

const statusLabel = (s: string) => statusLabelMap[s] ?? s;
const statusClass = (s: string) => statusClassMap[s] ?? 'bg-gray-100 text-gray-600';

const formatDate = (d: string) =>
  new Date(d).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' });

const loadCriticalStocks = async () => {
  criticalStocksLoading.value = true;
  try {
    criticalStocks.value = await dashboardService.getCriticalStocks();
  } catch {
    criticalStocks.value = [];
  } finally {
    criticalStocksLoading.value = false;
  }
};

const load = async () => {
  loading.value = true;
  error.value = false;
  try {
    stats.value = await dashboardService.getStats();
  } catch {
    error.value = true;
  } finally {
    loading.value = false;
  }
  if (showCriticalStocks.value) {
    loadCriticalStocks();
  }
};

onMounted(() => {
  if (authStore.userRole === 'Dispatcher') {
    router.replace('/driver');
    return;
  }
  load();
});
</script>
