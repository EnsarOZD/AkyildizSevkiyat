<template>
  <div class="p-6 space-y-6">
    <div>
      <h1 class="text-2xl font-bold">Raporlar</h1>
      <p class="text-gray-600 dark:text-gray-400">Operasyonel özet raporlar.</p>
    </div>

    <!-- Tabs -->
    <div class="border-b border-gray-200 dark:border-gray-700 overflow-x-auto">
      <nav class="-mb-px flex gap-1 min-w-max">
        <button
          v-for="tab in tabs"
          :key="tab.key"
          @click="activeTab = tab.key"
          class="py-2 px-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap"
          :class="activeTab === tab.key
            ? 'border-indigo-600 text-indigo-600'
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
      <OpenOrdersTab v-else-if="activeTab === 'pos'" />
      <PendingReceiptsTab v-else-if="activeTab === 'gr'" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';

// Tab Components
import ShipmentSummaryTab from '../components/reports/ShipmentSummaryTab.vue';
import PerformanceTab from '../components/reports/PerformanceTab.vue';
import ZoneMaterialTab from '../components/reports/ZoneMaterialTab.vue';
import StockStatusTab from '../components/reports/StockStatusTab.vue';
import ReturnsTab from '../components/reports/ReturnsTab.vue';
import OpenOrdersTab from '../components/reports/OpenOrdersTab.vue';
import PendingReceiptsTab from '../components/reports/PendingReceiptsTab.vue';

const route = useRoute();

const tabs = [
  { key: 'shipments',    label: 'Sevkiyat Özeti' },
  { key: 'performance',  label: 'Teslimat Performansı' },
  { key: 'zone-material', label: 'Bölge Malzeme Raporu' },
  { key: 'stock',        label: 'Stok Durumu' },
  { key: 'returns',      label: 'İade Analizi' },
  { key: 'pos',          label: 'Açık Satın Alma' },
  { key: 'gr',           label: 'Bekleyen Mal Girişi' },
];

const activeTab = ref('shipments');

onMounted(() => {
  // URL parametresi ile belirli sekmeyi açma desteği (örn: ?tab=zone-material)
  const tabParam = route.query.tab as string;
  if (tabParam && tabs.some(t => t.key === tabParam)) {
    activeTab.value = tabParam;
  }
});
</script>
