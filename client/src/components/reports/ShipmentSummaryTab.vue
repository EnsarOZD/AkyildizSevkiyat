<template>
  <div class="space-y-4" style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">

    <!-- Filtre çubuğu -->
    <ReportToolbar
      v-model:start-date="filter.startDate"
      v-model:end-date="filter.endDate"
      :loading="loading"
      :can-export="!!summary"
      @apply="loadData"
      @export="exportExcel"
    />

    <!-- Özet KPI'lar -->
    <div v-if="summary" class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-8 gap-3">
      <ReportStat label="Toplam"        :value="summary.total" />
      <ReportStat label="Taslak"        :value="summary.created" tone="gray" />
      <ReportStat label="Depoda"        :value="summary.assignedToWarehouse" tone="blue" />
      <ReportStat label="Toplanıyor"    :value="summary.picking" tone="amber" />
      <ReportStat label="Hazır"         :value="summary.readyForDispatch" tone="green" />
      <ReportStat label="Araçta"        :value="summary.assignedToVehicle" tone="indigo" />
      <ReportStat label="Teslim Edildi" :value="summary.delivered" tone="green" />
      <ReportStat label="İptal"         :value="summary.cancelled" tone="red" />
    </div>

    <!-- Tablo -->
    <div v-if="summary" class="rounded-2xl border border-gray-200 dark:border-white/10 bg-white dark:bg-[#0f2238] overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full text-sm">
          <thead>
            <tr class="bg-gray-50 dark:bg-white/5 text-gray-500 dark:text-white/55">
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">#</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Proje</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide hidden sm:table-cell">Bölge</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide">Durum</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide hidden sm:table-cell">Teslim Tarihi</th>
              <th class="px-4 py-3 text-left text-[11px] font-bold uppercase tracking-wide hidden lg:table-cell">Talep No</th>
              <th class="px-4 py-3 text-right text-[11px] font-bold uppercase tracking-wide hidden lg:table-cell">Kalem</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-white/5">
            <tr v-if="summary.rows.length === 0">
              <td colspan="7" class="px-4 py-8 text-center text-gray-400 dark:text-white/40">Bu tarih aralığında sevkiyat bulunamadı.</td>
            </tr>
            <tr v-for="row in summary.rows" :key="row.id" class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
              <td class="px-4 py-3 font-mono text-xs text-gray-400 dark:text-white/40">{{ row.id }}</td>
              <td class="px-4 py-3 font-semibold text-gray-900 dark:text-white">{{ row.projectName }}</td>
              <td class="px-4 py-3 text-gray-500 dark:text-white/55 hidden sm:table-cell">{{ row.zoneName || '—' }}</td>
              <td class="px-4 py-3">
                <span class="px-2 py-0.5 text-[11px] rounded-full font-bold" :class="statusBadge(row.status)">{{ statusLabel(row.status) }}</span>
              </td>
              <td class="px-4 py-3 text-gray-600 dark:text-white/65 hidden sm:table-cell">{{ fmtDate(row.deliveryDate) }}</td>
              <td class="px-4 py-3 text-gray-400 dark:text-white/40 text-xs hidden lg:table-cell">{{ row.talepNo || '—' }}</td>
              <td class="px-4 py-3 text-right text-gray-600 dark:text-white/65 hidden lg:table-cell">{{ row.lineCount }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- boş / yükleniyor -->
    <div v-if="!summary && !loading" class="text-center py-12 text-gray-400 dark:text-white/40">Tarih aralığı seçip "Filtrele" butonuna tıklayın.</div>
    <div v-if="loading" class="flex justify-center py-12">
      <div class="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import reportService from '../../services/reportService';
import type { ShipmentSummaryDto } from '../../services/reportService';
import { ApiErrorUtils } from '../../utils/apiError';
import { formatDate as fmtDate } from '../../utils/dateFormat';
import { statusLabel, statusBadge } from '../../utils/shipmentStatusUi';
import { useNotification } from '../../composables/useNotification';
import { exportToExcel } from '../../utils/exportExcel';
import ReportToolbar from './ReportToolbar.vue';
import ReportStat from './ReportStat.vue';

const { notify } = useNotification();

const today = new Date().toISOString().slice(0, 10);
const monthAgo = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10);

const filter = ref({ startDate: monthAgo, endDate: today });
const summary = ref<ShipmentSummaryDto | null>(null);
const loading = ref(false);

const loadData = async () => {
  loading.value = true;
  try {
    summary.value = await reportService.getShipmentSummary(filter.value);
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Rapor yüklenemedi.'));
  } finally {
    loading.value = false;
  }
};

const exportExcel = () => {
  if (!summary.value) return;
  const rows = summary.value.rows.map(r => ({
    'ID': r.id,
    'Proje': r.projectName,
    'Bölge': r.zoneName || '',
    'Durum': statusLabel(r.status),
    'Teslim Tarihi': fmtDate(r.deliveryDate),
    'Talep No': r.talepNo || '',
    'Kalem Sayısı': r.lineCount,
  }));
  exportToExcel(rows, 'Sevkiyat Özeti', `sevkiyat-ozeti-${filter.value.startDate}_${filter.value.endDate}`);
};

onMounted(loadData);
</script>
