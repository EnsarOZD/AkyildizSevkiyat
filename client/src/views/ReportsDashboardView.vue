<template>
  <div class="p-4 sm:p-6 space-y-4">

    <PageHeader title="Raporlar" subtitle="Operasyonel özet raporlar" color="purple">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
        </svg>
      </template>
    </PageHeader>

    <!-- Tabs -->
    <div class="border-b border-gray-200 dark:border-gray-700 overflow-x-auto">
      <nav class="-mb-px flex gap-1 min-w-max">
        <button
          v-for="tab in tabs"
          :key="tab.key"
          @click="activeTab = tab.key"
          class="py-2 px-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap"
          :class="activeTab === tab.key
            ? 'border-purple-600 text-purple-600'
            : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'"
        >
          {{ tab.label }}
        </button>
      </nav>
    </div>

    <!-- Tab Content -->
    <div class="mt-4">
      <ShipmentSummaryTab v-if="activeTab === 'shipments'" />
      <PerformanceTab v-else-if="activeTab === 'performance'" />
      <ZoneMaterialTab v-else-if="activeTab === 'zone-material'" />
      <StockStatusTab v-else-if="activeTab === 'stock'" />
      <ReturnsTab v-else-if="activeTab === 'returns'" />
      <PendingReceiptsTab v-else-if="activeTab === 'gr'" />
      <MaterialPurchaseTab v-else-if="activeTab === 'material-purchase'" />
      <DriverSessionsView v-else-if="activeTab === 'driver-sessions'" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import PageHeader from '../components/PageHeader.vue';

import ShipmentSummaryTab from '../components/reports/ShipmentSummaryTab.vue';
import PerformanceTab from '../components/reports/PerformanceTab.vue';
import ZoneMaterialTab from '../components/reports/ZoneMaterialTab.vue';
import StockStatusTab from '../components/reports/StockStatusTab.vue';
import ReturnsTab from '../components/reports/ReturnsTab.vue';
import PendingReceiptsTab from '../components/reports/PendingReceiptsTab.vue';
import MaterialPurchaseTab from '../components/reports/MaterialPurchaseTab.vue';
import DriverSessionsView from './DriverSessionsView.vue';

const route = useRoute();

const tabs = [
  { key: 'shipments',        label: 'Sevkiyat Özeti' },
  { key: 'performance',      label: 'Teslimat Performansı' },
  { key: 'zone-material',    label: 'Bölge Malzeme Raporu' },
  { key: 'stock',            label: 'Stok Durumu' },
  { key: 'returns',          label: 'İade Analizi' },
  { key: 'pos',              label: 'Açık Satın Alma' },
  { key: 'gr',               label: 'Bekleyen Mal Girişi' },
  { key: 'material-purchase', label: 'Tedarikçi/Malzeme Raporu' },
  { key: 'driver-sessions',  label: 'Şoför Puantajı' },
];

const activeTab = ref('shipments');

onMounted(() => {
  const tabParam = route.query.tab as string;
  if (tabParam && tabs.some(t => t.key === tabParam)) {
    activeTab.value = tabParam;
  }
});
</script>
