<template>
  <div class="p-4 md:p-6">
    <PageHeader title="Netsis Uzlaştırma" subtitle="Netsis ile sevkiyat irsaliye mutabakatı" color="amber" class="mb-6">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
      </template>
    </PageHeader>

    <!-- Filtreler -->
    <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4 mb-5 flex gap-3 flex-wrap items-end">
      <div class="w-44">
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Başlangıç Tarihi</label>
        <input type="date" v-model="filters.fromDate" class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100" />
      </div>
      <div class="w-44">
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Bitiş Tarihi</label>
        <input type="date" v-model="filters.toDate" class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100" />
      </div>
      <div class="w-44">
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Operasyon Tipi</label>
        <select v-model="filters.operationType" class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100">
          <option value="">Tümü</option>
          <option value="0">Catering</option>
          <option value="1">Kıyafet</option>
        </select>
      </div>
      <div class="flex items-end gap-2">
        <label class="flex items-center gap-2 cursor-pointer">
          <input type="checkbox" v-model="filters.onlyDiff" class="w-4 h-4 rounded border-gray-300 text-blue-600" />
          <span class="text-sm text-gray-700 dark:text-gray-300">Sadece farklılık olanlar</span>
        </label>
      </div>
      <button
        @click="fetchData"
        :disabled="loading"
        class="ml-auto px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition text-sm font-medium disabled:opacity-50"
      >
        <span v-if="loading">Yükleniyor...</span>
        <span v-else>Filtrele</span>
      </button>
    </div>

    <!-- Özet -->
    <div v-if="data.length > 0" class="mb-4 flex gap-4 flex-wrap text-sm">
      <span class="text-gray-500 dark:text-gray-400">
        <strong class="text-gray-900 dark:text-gray-100">{{ data.length }}</strong> sevkiyat
      </span>
      <span class="text-red-600 dark:text-red-400">
        <strong>{{ data.filter(d => d.hasDifference).length }}</strong> farklılık var
      </span>
    </div>

    <!-- Liste -->
    <div v-if="loading" class="text-center py-12 text-gray-400">Yükleniyor...</div>

    <div v-else-if="data.length === 0" class="text-center py-12 text-gray-400">
      Veri bulunamadı. Tarih aralığı seçip "Filtrele" butonuna basın.
    </div>

    <div v-else class="space-y-3">
      <div
        v-for="shipment in data"
        :key="shipment.shipmentId"
        class="bg-white dark:bg-gray-900 border rounded-xl overflow-hidden"
        :class="shipment.hasDifference
          ? 'border-orange-200 dark:border-orange-800'
          : 'border-gray-200 dark:border-gray-700'"
      >
        <!-- Header -->
        <div
          class="px-4 py-3 flex items-center justify-between cursor-pointer select-none"
          :class="shipment.hasDifference
            ? 'bg-orange-50 dark:bg-orange-900/20'
            : 'bg-gray-50 dark:bg-gray-800'"
          @click="toggleExpand(shipment.shipmentId)"
        >
          <div class="flex items-center gap-3 min-w-0 flex-1">
            <span class="text-sm font-bold text-gray-800 dark:text-gray-100 shrink-0">
              #{{ shipment.shipmentId }}
            </span>
            <span class="text-sm text-gray-600 dark:text-gray-400 truncate">{{ shipment.projectName }}</span>
            <span
              class="shrink-0 inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-bold border"
              :class="shipment.operationType === 'Kıyafet'
                ? 'bg-purple-100 text-purple-700 border-purple-200 dark:bg-purple-900/40 dark:text-purple-300 dark:border-purple-700'
                : 'bg-blue-100 text-blue-700 border-blue-200 dark:bg-blue-900/40 dark:text-blue-300 dark:border-blue-700'"
            >{{ shipment.operationType }}</span>
          </div>
          <div class="flex items-center gap-3 ml-4 shrink-0">
            <span class="text-xs text-gray-500 dark:text-gray-400">
              {{ formatDate(shipment.deliveryDate) }}
            </span>
            <span v-if="shipment.irsaliyeNo" class="text-xs font-mono text-indigo-600 dark:text-indigo-400">
              {{ shipment.irsaliyeNo }}
            </span>
            <span
              class="text-xs font-bold px-2 py-0.5 rounded"
              :class="shipment.hasDifference
                ? 'bg-orange-100 text-orange-700 dark:bg-orange-900/40 dark:text-orange-300'
                : 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300'"
            >{{ shipment.hasDifference ? '⚠ Farklılık' : '✓ Uyumlu' }}</span>
            <svg
              class="w-4 h-4 text-gray-400 transition-transform"
              :class="expanded.has(shipment.shipmentId) ? 'rotate-180' : ''"
              fill="none" stroke="currentColor" viewBox="0 0 24 24"
            >
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
            </svg>
          </div>
        </div>

        <!-- Lines detail -->
        <div v-if="expanded.has(shipment.shipmentId)" class="overflow-x-auto border-t border-gray-100 dark:border-gray-700">
          <table class="min-w-full divide-y divide-gray-100 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Stok Kodu</th>
                <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase min-w-[180px]">Stok Adı</th>
                <th class="px-4 py-2 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">ISS Mik.</th>
                <th class="px-4 py-2 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Sipariş Mik.</th>
                <th class="px-4 py-2 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Toplanan</th>
                <th class="px-4 py-2 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Netsis Mik.</th>
                <th class="px-4 py-2 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Durum</th>
              </tr>
            </thead>
            <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-100 dark:divide-gray-700">
              <tr v-for="line in shipment.lines" :key="line.shipmentLineId">
                <td class="px-4 py-2 text-xs font-mono text-gray-600 dark:text-gray-400 whitespace-nowrap">{{ line.stockCode }}</td>
                <td class="px-4 py-2 text-sm text-gray-800 dark:text-gray-200">{{ line.stockName }}</td>
                <td class="px-4 py-2 text-sm text-right text-gray-500 dark:text-gray-400">{{ line.issQty }}</td>
                <td class="px-4 py-2 text-sm text-right font-medium text-gray-700 dark:text-gray-300">{{ line.orderedQty }}</td>
                <td class="px-4 py-2 text-sm text-right font-medium text-gray-700 dark:text-gray-300">{{ line.deliveredQty }}</td>
                <td class="px-4 py-2 text-sm text-right font-bold"
                    :class="line.netsisQty !== line.issQty ? 'text-orange-600 dark:text-orange-400' : 'text-gray-700 dark:text-gray-300'">
                  {{ line.netsisQty }}
                </td>
                <td class="px-4 py-2 text-center">
                  <span
                    class="inline-flex items-center px-2 py-0.5 rounded text-xs font-bold"
                    :class="statusClass(line.status)"
                  >{{ statusLabel(line.status) }}</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import apiClient from '../services/apiClient';

interface LineReconciliationDto {
  shipmentLineId: number;
  stockCode: string;
  stockName: string;
  unit: string;
  issQty: number;
  orderedQty: number;
  deliveredQty: number;
  netsisQty: number;
  status: string;
}

interface ShipmentReconciliationDto {
  shipmentId: number;
  projectName: string;
  deliveryDate: string;
  operationType: string;
  netsisOrderNo?: string;
  irsaliyeNo?: string;
  hasDifference: boolean;
  lines: LineReconciliationDto[];
}

const loading = ref(false);
const data = ref<ShipmentReconciliationDto[]>([]);
const expanded = ref<Set<number>>(new Set());

const today = new Date();
const monthAgo = new Date(today);
monthAgo.setMonth(monthAgo.getMonth() - 1);

const filters = ref({
  fromDate: monthAgo.toISOString().slice(0, 10),
  toDate: today.toISOString().slice(0, 10),
  operationType: '',
  onlyDiff: false,
});

const fetchData = async () => {
  loading.value = true;
  try {
    const params: Record<string, any> = {
      fromDate: filters.value.fromDate || undefined,
      toDate: filters.value.toDate || undefined,
      onlyDiff: filters.value.onlyDiff,
    };
    if (filters.value.operationType !== '') {
      params.operationType = Number(filters.value.operationType);
    }
    const response = await apiClient.get('/netsis/reconciliation', { params });
    data.value = response.data;
  } catch (e) {
    console.error(e);
  } finally {
    loading.value = false;
  }
};

const toggleExpand = (id: number) => {
  if (expanded.value.has(id)) {
    expanded.value.delete(id);
  } else {
    expanded.value.add(id);
  }
};

const formatDate = (d: string) =>
  new Date(d).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' });

const statusLabel = (status: string): string => {
  const map: Record<string, string> = {
    Equal: '✅ Uyumlu',
    ClothingDirect: '📦 Kıyafet',
    Revised: '⚠ Revize',
    UnderPicked: '⚠ Eksik',
    OverPicked: '⚠ Fazla',
    NotPicked: '❌ Toplanmadı',
  };
  return map[status] || status;
};

const statusClass = (status: string): string => {
  const map: Record<string, string> = {
    Equal:         'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300',
    ClothingDirect:'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300',
    Revised:       'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/40 dark:text-yellow-300',
    UnderPicked:   'bg-orange-100 text-orange-700 dark:bg-orange-900/40 dark:text-orange-300',
    OverPicked:    'bg-orange-100 text-orange-700 dark:bg-orange-900/40 dark:text-orange-300',
    NotPicked:     'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
  };
  return map[status] || 'bg-gray-100 text-gray-700';
};

onMounted(() => fetchData());
</script>
