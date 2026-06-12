<template>
  <div class="space-y-6" style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">

    <!-- ISS-IP içe aktarım şeridi (backend gönderirse) -->
    <router-link
      v-if="stats.lastIssImportBatch"
      to="/order-import"
      class="flex items-center gap-3.5 px-4 py-3.5 rounded-2xl bg-white dark:bg-[#0f2238] border border-gray-200 dark:border-white/10 hover:border-blue-300 dark:hover:border-blue-500/40 transition-colors"
    >
      <span class="w-9 h-9 rounded-xl bg-emerald-50 dark:bg-emerald-500/15 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shrink-0">
        <ArrowPathIcon class="w-5 h-5" />
      </span>
      <div class="min-w-0 flex-1">
        <p class="text-[13px] font-bold text-gray-900 dark:text-white">ISS-IP Sipariş Aktarımı · {{ stats.lastIssImportBatch.status }}</p>
        <p class="text-xs text-gray-500 dark:text-white/55 mt-0.5">
          {{ stats.lastIssImportBatch.newCount }} yeni sipariş · {{ stats.lastIssImportBatch.needsMappingCount }} eşleşme bekliyor
        </p>
      </div>
      <span class="text-xs font-bold text-blue-600 dark:text-blue-400 shrink-0">Detay</span>
    </router-link>

    <!-- KPI ızgarası — sade lacivert/mavi; yalnızca uyarılar renkli -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
      <component
        :is="card.to ? 'router-link' : 'div'"
        v-for="card in kpiCards"
        :key="card.label"
        :to="card.to"
        class="rounded-2xl border p-4 transition-all"
        :class="cardClass(card)"
      >
        <div class="flex items-center justify-between mb-3.5">
          <p class="text-[11px] font-bold uppercase tracking-wide leading-tight max-w-[8rem]"
             :class="labelClass(card)">{{ card.label }}</p>
          <span class="w-8 h-8 rounded-[10px] flex items-center justify-center shrink-0" :class="iconWrapClass(card)">
            <component :is="card.icon" class="w-4 h-4" />
          </span>
        </div>
        <div class="flex items-baseline gap-2">
          <span class="text-[28px] font-extrabold tracking-tight leading-none" :class="valueClass(card)">{{ card.value }}</span>
          <span v-if="card.sub" class="text-xs font-semibold text-gray-400 dark:text-white/40">{{ card.sub }}</span>
        </div>
      </component>
    </div>

    <!-- Sevkiyat Pipeline'ı -->
    <div class="bg-white dark:bg-[#0f2238] rounded-2xl border border-gray-200 dark:border-white/10 p-5">
      <div class="flex items-center gap-2.5 mb-4">
        <CubeIcon class="w-[18px] h-[18px] text-blue-600 dark:text-blue-400" />
        <div>
          <h2 class="text-[15px] font-extrabold text-gray-900 dark:text-white tracking-tight leading-none">Sevkiyat Pipeline'ı</h2>
          <p class="text-xs text-gray-500 dark:text-white/55 mt-1">Hazırlanan siparişlerin durum akışı</p>
        </div>
      </div>
      <div class="flex items-center gap-1.5">
        <template v-for="(step, idx) in pipeline" :key="step.label">
          <router-link :to="step.to" class="flex-1 min-w-0 rounded-xl py-4 px-2 text-center transition-transform hover:-translate-y-0.5" :class="step.cls">
            <div class="text-2xl font-extrabold tracking-tight leading-none">{{ step.count }}</div>
            <div class="text-[11px] font-bold uppercase tracking-wide mt-1.5">{{ step.label }}</div>
          </router-link>
          <ChevronRightIcon v-if="idx < pipeline.length - 1" class="w-4 h-4 text-gray-300 dark:text-white/25 shrink-0" />
        </template>
      </div>
    </div>

    <!-- İki sütun: Bugün araç bekleyenler + Yaklaşan sevkiyatlar -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Araç bekleyenler -->
      <div class="bg-white dark:bg-[#0f2238] rounded-2xl border border-gray-200 dark:border-white/10 p-5">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-[15px] font-extrabold text-gray-900 dark:text-white tracking-tight">Bugün Araç Bekleyenler</h2>
            <p class="text-xs text-gray-500 dark:text-white/55 mt-0.5">Hazır, şoför/araç atanmamış</p>
          </div>
          <span class="px-2.5 py-0.5 rounded-full text-xs font-bold bg-amber-100 dark:bg-amber-500/15 text-amber-700 dark:text-amber-300">
            {{ stats.waitingForVehicleTodayCount }}
          </span>
        </div>
        <div v-if="stats.waitingForVehicleToday.length === 0" class="flex flex-col items-center justify-center py-8 text-gray-400 dark:text-white/40">
          <CheckCircleIcon class="w-10 h-10 mb-2 opacity-50" />
          <p class="text-sm">Tüm hazır sevkiyatlar araç ataması yapılmış</p>
        </div>
        <div v-else>
          <router-link
            v-for="(s, i) in stats.waitingForVehicleToday" :key="s.id" :to="`/shipments/${s.id}`"
            class="flex items-center gap-3 py-2.5 -mx-2 px-2 rounded-lg hover:bg-gray-50 dark:hover:bg-white/5 transition-colors"
            :class="{ 'border-t border-gray-100 dark:border-white/5': i > 0 }"
          >
            <div class="flex-1 min-w-0">
              <p class="text-[13.5px] font-semibold text-gray-800 dark:text-white/90 truncate">{{ s.projectName }}</p>
              <p class="text-[11.5px] text-gray-400 dark:text-white/40 truncate">{{ s.irsaliyeNo ? `İrs: ${s.irsaliyeNo}` : s.talepNo }}</p>
            </div>
            <span class="px-2 py-0.5 rounded-full text-[11px] font-bold" :class="statusBadge(s.status)">{{ statusLabel(s.status) }}</span>
          </router-link>
        </div>
      </div>

      <!-- Yaklaşan -->
      <div class="bg-white dark:bg-[#0f2238] rounded-2xl border border-gray-200 dark:border-white/10 p-5">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-[15px] font-extrabold text-gray-900 dark:text-white tracking-tight">Yaklaşan Sevkiyatlar</h2>
          <router-link to="/shipments" class="text-xs font-bold text-blue-600 dark:text-blue-400 hover:underline">Tümünü gör</router-link>
        </div>
        <div v-if="stats.recentShipments.length === 0" class="flex flex-col items-center justify-center py-8 text-gray-400 dark:text-white/40">
          <ClipboardDocumentListIcon class="w-10 h-10 mb-2 opacity-50" />
          <p class="text-sm">Aktif sevkiyat bulunamadı</p>
        </div>
        <div v-else>
          <router-link
            v-for="(s, i) in stats.recentShipments" :key="s.id" :to="`/shipments/${s.id}`"
            class="flex items-center gap-3 py-2.5 -mx-2 px-2 rounded-lg hover:bg-gray-50 dark:hover:bg-white/5 transition-colors"
            :class="{ 'border-t border-gray-100 dark:border-white/5': i > 0 }"
          >
            <div class="flex-1 min-w-0">
              <p class="text-[13.5px] font-semibold text-gray-800 dark:text-white/90 truncate">{{ s.projectName }}</p>
              <p class="text-[11.5px] text-gray-400 dark:text-white/40 truncate">{{ s.talepNo }}</p>
            </div>
            <div class="flex flex-col items-end gap-1 shrink-0">
              <span class="px-2 py-0.5 rounded-full text-[11px] font-bold" :class="statusBadge(s.status)">{{ statusLabel(s.status) }}</span>
              <p class="text-[10px] text-gray-400 dark:text-white/40">{{ formatShortDate(s.deliveryDate) }}</p>
            </div>
          </router-link>
        </div>
      </div>
    </div>

    <!-- Kritik stok -->
    <CriticalStockWidget :items="criticalStocks" :loading="criticalStocksLoading" />
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted } from 'vue';
import {
  ClipboardDocumentListIcon, TruckIcon, ExclamationTriangleIcon, ClockIcon,
  ShoppingCartIcon, ArchiveBoxIcon, ArrowUturnLeftIcon, PhoneIcon,
  CheckCircleIcon, ChevronRightIcon, CubeIcon, ArrowPathIcon,
} from '@heroicons/vue/24/outline';
import type { DashboardStats, CriticalStockItem } from '../../services/dashboardService';
import { statusLabel, statusBadge, formatShortDate } from '../../utils/shipmentStatusUi';
import CriticalStockWidget from '../CriticalStockWidget.vue';
import freightDeliveryService from '../../services/freightDeliveryService';

const props = defineProps<{
  stats: DashboardStats;
  criticalStocks: CriticalStockItem[];
  criticalStocksLoading: boolean;
  /** Korundu (geriye uyumluluk); yeni düzen tek ve birleşiktir. */
  simplified?: boolean;
}>();

// Nakliyeden teslim bekleyen sayısı
const pendingFreightCount = ref(0);
onMounted(async () => {
  try {
    const list = await freightDeliveryService.list(false);
    pendingFreightCount.value = list.filter(f => !f.isCompleted && !f.isExpired).length;
  } catch { pendingFreightCount.value = 0; }
});

function dateStr(offsetDays = 0) {
  const d = new Date(); d.setDate(d.getDate() + offsetDays);
  return d.toISOString().slice(0, 10);
}
const today = dateStr(0);
const yesterday = dateStr(-1);
const ninetyDaysAgo = dateStr(-90);

type Kind = 'neutral' | 'danger' | 'warn';
interface KpiCard { label: string; value: number; icon: unknown; to?: string; kind: Kind; sub?: string; }

const kpiCards = computed<KpiCard[]>(() => [
  { label: 'Aktif Sevkiyat',     value: props.stats.totalActiveShipments,     icon: ClipboardDocumentListIcon, to: '/shipments', kind: 'neutral' },
  { label: 'Bugün Teslim',       value: props.stats.shipmentsToday,           icon: TruckIcon, to: `/shipments?startDate=${today}&endDate=${today}&statuses=5,6`, kind: 'neutral' },
  { label: 'Gecikmiş',           value: props.stats.shipmentsOverdue,         icon: ExclamationTriangleIcon, to: `/shipments?startDate=${ninetyDaysAgo}&endDate=${yesterday}`, kind: 'danger' },
  { label: 'Bugün Hazır Değil',  value: props.stats.todayShipmentsNotReadyCount, icon: ClockIcon, to: `/shipments?startDate=${today}&endDate=${today}&statuses=0,1,2`, kind: 'danger' },
  { label: 'Onay Bekleyen PO',   value: props.stats.pendingPOApprovalCount,   icon: ShoppingCartIcon, to: '/purchase-orders', kind: 'warn' },
  { label: 'Kritik Stok',        value: props.stats.criticalStockCount,       icon: ArchiveBoxIcon, to: '/stock-management', kind: 'danger' },
  { label: 'Belirsiz İade',      value: props.stats.pendingFloatingReturns,   icon: ArrowUturnLeftIcon, to: '/floating-returns', kind: 'warn' },
  { label: 'Nakliye Teslim Bekleniyor', value: pendingFreightCount.value, icon: PhoneIcon, to: '/freight-deliveries', kind: 'neutral' },
]);

const isAlert = (c: KpiCard) => (c.kind === 'danger' || c.kind === 'warn') && c.value > 0;

function cardClass(c: KpiCard) {
  if (isAlert(c) && c.kind === 'danger') return 'bg-red-50 dark:bg-red-500/10 border-red-200 dark:border-red-500/30';
  if (isAlert(c) && c.kind === 'warn')   return 'bg-amber-50 dark:bg-amber-500/10 border-amber-200 dark:border-amber-500/30';
  return 'bg-white dark:bg-[#0f2238] border-gray-200 dark:border-white/10 hover:border-blue-300 dark:hover:border-blue-500/40 hover:shadow-sm';
}
function labelClass(c: KpiCard) {
  if (isAlert(c) && c.kind === 'danger') return 'text-red-600 dark:text-red-300';
  if (isAlert(c) && c.kind === 'warn')   return 'text-amber-700 dark:text-amber-300';
  return 'text-gray-500 dark:text-white/55';
}
function valueClass(c: KpiCard) {
  if (isAlert(c) && c.kind === 'danger') return 'text-red-600 dark:text-red-300';
  if (isAlert(c) && c.kind === 'warn')   return 'text-amber-700 dark:text-amber-300';
  return 'text-gray-900 dark:text-white';
}
function iconWrapClass(c: KpiCard) {
  if (isAlert(c) && c.kind === 'danger') return 'bg-white/70 dark:bg-white/10 text-red-500 dark:text-red-300';
  if (isAlert(c) && c.kind === 'warn')   return 'bg-white/70 dark:bg-white/10 text-amber-600 dark:text-amber-300';
  return 'bg-gray-50 dark:bg-white/5 text-blue-600 dark:text-blue-400';
}

const pipeline = computed(() => [
  { label: 'Taslak',  count: props.stats.statusDraft,     to: '/shipments?statuses=0', cls: 'bg-gray-50 dark:bg-white/5 text-gray-700 dark:text-white/80' },
  { label: 'Depoda',  count: props.stats.statusWarehouse, to: '/shipments?statuses=1', cls: 'bg-blue-50 dark:bg-blue-500/15 text-blue-700 dark:text-blue-300' },
  { label: 'Toplama', count: props.stats.statusPicking,   to: '/shipments?statuses=2', cls: 'bg-amber-50 dark:bg-amber-500/15 text-amber-700 dark:text-amber-300' },
  { label: 'Hazır',   count: props.stats.statusReady,     to: '/shipments?statuses=3', cls: 'bg-emerald-50 dark:bg-emerald-500/15 text-emerald-700 dark:text-emerald-300' },
  { label: 'Araçta',  count: props.stats.statusOnRoute,   to: '/shipments?statuses=4', cls: 'bg-indigo-50 dark:bg-indigo-500/15 text-indigo-700 dark:text-indigo-300' },
]);
</script>
