<template>
  <div class="grid grid-cols-2 lg:grid-cols-5 gap-4"
       style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">
    <div
      v-for="card in cards" :key="card.label"
      class="rounded-2xl border p-4 transition-all"
      :class="cardClass(card)"
    >
      <div class="flex items-center justify-between mb-3.5">
        <p class="text-[11px] font-bold uppercase tracking-wide leading-tight max-w-[8rem]" :class="labelClass(card)">{{ card.label }}</p>
        <span class="w-8 h-8 rounded-[10px] flex items-center justify-center shrink-0" :class="iconWrapClass(card)">
          <component :is="card.icon" class="w-4 h-4" />
        </span>
      </div>
      <p class="text-[28px] font-extrabold tracking-tight leading-none" :class="valueClass(card)">{{ card.value }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import {
  CubeIcon, ClockIcon, ExclamationTriangleIcon, ArchiveBoxIcon, ArchiveBoxXMarkIcon,
} from '@heroicons/vue/24/outline';
import type { DashboardZoneDto } from '../services/warehouseService';

/**
 * Depo paneli özet KPI şeridi — yeni kimlik (lacivert + tek mavi, monokrom).
 * WarehouseDashboard.vue içinde PageHeader'dan sonra, tab bar'dan önce kullanın:
 *   <WarehouseKpiStrip :zones="allZones" :pending-goods-receipts-count="..." :critical-stock-count="criticalStocks.length" />
 */
const props = defineProps<{
  zones: DashboardZoneDto[];
  pendingGoodsReceiptsCount?: number;
  criticalStockCount?: number;
}>();

function todayStr(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}
const dStr = (z: DashboardZoneDto) => z.deliveryDate.slice(0, 10);
const uniq = (zs: DashboardZoneDto[]) => new Set(zs.map(z => z.zoneName)).size;

// statusId: 0..3 = aktif hazırlık, <6 = henüz yüklenmedi
const pending  = computed(() => uniq(props.zones.filter(z => z.statusId < 4)));
const todayCnt = computed(() => uniq(props.zones.filter(z => z.statusId < 6 && dStr(z) === todayStr())));
const overdue  = computed(() => uniq(props.zones.filter(z => z.statusId < 4 && dStr(z) < todayStr())));

type Kind = 'neutral' | 'danger' | 'warn';
interface KCard { label: string; value: number; icon: unknown; kind: Kind; }

const cards = computed<KCard[]>(() => [
  { label: 'Bekleyen Hazırlık',   value: pending.value,                    icon: CubeIcon,                 kind: 'neutral' },
  { label: 'Bugün Hazırlanacak',  value: todayCnt.value,                   icon: ClockIcon,                kind: 'neutral' },
  { label: 'Gecikmiş Hazırlık',   value: overdue.value,                    icon: ExclamationTriangleIcon,  kind: 'danger' },
  { label: 'Bekleyen Mal Girişi', value: props.pendingGoodsReceiptsCount ?? 0, icon: ArchiveBoxIcon,       kind: 'warn' },
  { label: 'Kritik Stok',         value: props.criticalStockCount ?? 0,    icon: ArchiveBoxXMarkIcon,      kind: 'danger' },
]);

const isAlert = (c: KCard) => (c.kind === 'danger' || c.kind === 'warn') && c.value > 0;
function cardClass(c: KCard) {
  if (isAlert(c) && c.kind === 'danger') return 'bg-red-50 dark:bg-red-500/10 border-red-200 dark:border-red-500/30';
  if (isAlert(c) && c.kind === 'warn')   return 'bg-amber-50 dark:bg-amber-500/10 border-amber-200 dark:border-amber-500/30';
  return 'bg-white dark:bg-[#0f2238] border-gray-200 dark:border-white/10 hover:border-blue-300 dark:hover:border-blue-500/40 hover:shadow-sm';
}
function labelClass(c: KCard) {
  if (isAlert(c) && c.kind === 'danger') return 'text-red-600 dark:text-red-300';
  if (isAlert(c) && c.kind === 'warn')   return 'text-amber-700 dark:text-amber-300';
  return 'text-gray-500 dark:text-white/55';
}
function valueClass(c: KCard) {
  if (isAlert(c) && c.kind === 'danger') return 'text-red-600 dark:text-red-300';
  if (isAlert(c) && c.kind === 'warn')   return 'text-amber-700 dark:text-amber-300';
  return 'text-gray-900 dark:text-white';
}
function iconWrapClass(c: KCard) {
  if (isAlert(c) && c.kind === 'danger') return 'bg-white/70 dark:bg-white/10 text-red-500 dark:text-red-300';
  if (isAlert(c) && c.kind === 'warn')   return 'bg-white/70 dark:bg-white/10 text-amber-600 dark:text-amber-300';
  return 'bg-gray-50 dark:bg-white/5 text-blue-600 dark:text-blue-400';
}
</script>
