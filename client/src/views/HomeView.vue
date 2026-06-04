<template>
  <div class="p-6 space-y-6">
    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-semibold text-gray-900 dark:text-gray-100">{{ pageTitle }}</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
          Hoşgeldiniz, {{ authStore.userName || authStore.userEmail }}
        </p>
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

    <!-- Role-specific dashboards -->
    <template v-else-if="stats">
      <!-- Aktif seferler özeti (Manager/Admin/Accounting/Dispatcher) -->
      <ActiveTripsSummary v-if="showActiveTrips" />

      <ManagerDashboard
        v-if="dashboardKind === 'manager'"
        :stats="stats"
        :simplified="authStore.userRole === 'Manager'"
        :critical-stocks="criticalStocks"
        :critical-stocks-loading="criticalStocksLoading"
      />
      <AccountingDashboard
        v-else-if="dashboardKind === 'accounting'"
        :stats="stats"
      />
      <WarehouseDashboard
        v-else-if="dashboardKind === 'warehouse'"
        :stats="stats"
        :critical-stocks="criticalStocks"
        :critical-stocks-loading="criticalStocksLoading"
      />
      <!-- Fallback (Driver should be redirected; show manager view for any other role) -->
      <ManagerDashboard
        v-else
        :stats="stats"
        :critical-stocks="criticalStocks"
        :critical-stocks-loading="criticalStocksLoading"
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
import { useAuthStore } from '../stores/auth';
import { dashboardService, type DashboardStats, type CriticalStockItem } from '../services/dashboardService';
import { ArrowPathIcon, ExclamationCircleIcon } from '@heroicons/vue/24/outline';
import ManagerDashboard from '../components/dashboard/ManagerDashboard.vue';
import AccountingDashboard from '../components/dashboard/AccountingDashboard.vue';
import WarehouseDashboard from '../components/dashboard/WarehouseDashboard.vue';
import ActiveTripsSummary from '../components/dashboard/ActiveTripsSummary.vue';

const authStore = useAuthStore();
const stats = ref<DashboardStats | null>(null);
const loading = ref(false);
const error = ref(false);

const criticalStocks = ref<CriticalStockItem[]>([]);
const criticalStocksLoading = ref(false);

type DashboardKind = 'manager' | 'accounting' | 'warehouse' | 'fallback';

const dashboardKind = computed<DashboardKind>(() => {
  switch (authStore.userRole) {
    case 'Manager':
    case 'Admin':
    case 'Dispatcher':
      return 'manager';
    case 'Accounting':
      return 'accounting';
    case 'Warehouse':
      return 'warehouse';
    default:
      return 'fallback';
  }
});

const pageTitle = computed(() => {
  switch (dashboardKind.value) {
    case 'manager':    return 'Yönetim Paneli';
    case 'accounting': return 'Muhasebe Paneli';
    case 'warehouse':  return 'Depo Paneli';
    default:           return 'Dashboard';
  }
});

const needsCriticalStocks = computed(() =>
  dashboardKind.value === 'manager' || dashboardKind.value === 'warehouse'
);

// Aktif seferler: yönetim + muhasebe görür (depo hariç)
const showActiveTrips = computed(() =>
  dashboardKind.value === 'manager' || dashboardKind.value === 'accounting'
);

async function loadCriticalStocks() {
  criticalStocksLoading.value = true;
  try {
    criticalStocks.value = await dashboardService.getCriticalStocks();
  } catch {
    criticalStocks.value = [];
  } finally {
    criticalStocksLoading.value = false;
  }
}

async function load() {
  loading.value = true;
  error.value = false;
  try {
    stats.value = await dashboardService.getStats();
  } catch {
    error.value = true;
  } finally {
    loading.value = false;
  }
  if (needsCriticalStocks.value) {
    loadCriticalStocks();
  }
}

onMounted(load);
</script>
