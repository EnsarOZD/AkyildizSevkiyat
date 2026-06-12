<template>
  <div class="bg-white dark:bg-[#0f2238] rounded-2xl border border-gray-200 dark:border-white/10 p-5"
       style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">
    <div class="flex items-center justify-between mb-4">
      <div class="flex items-center gap-2.5">
        <ArchiveBoxIcon class="w-[18px] h-[18px] text-blue-600 dark:text-blue-400" />
        <div>
          <h2 class="text-[15px] font-extrabold text-gray-900 dark:text-white tracking-tight leading-none">Kritik Stok</h2>
          <p class="text-xs text-gray-500 dark:text-white/55 mt-1">Minimum seviyenin altındaki kalemler</p>
        </div>
      </div>
      <span v-if="items.length" class="px-2.5 py-0.5 rounded-full text-xs font-bold bg-red-100 dark:bg-red-500/15 text-red-700 dark:text-red-300">
        {{ items.length }} kalem
      </span>
    </div>

    <!-- loading -->
    <div v-if="loading" class="flex justify-center py-8">
      <div class="w-6 h-6 border-2 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- empty -->
    <div v-else-if="items.length === 0" class="flex flex-col items-center justify-center py-8 text-gray-400 dark:text-white/40">
      <CheckCircleIcon class="w-10 h-10 mb-2 opacity-50" />
      <p class="text-sm">Kritik seviyede stok yok</p>
    </div>

    <!-- list -->
    <div v-else>
      <div
        v-for="(s, i) in items" :key="s.stockMasterId"
        class="flex items-center gap-4 py-3"
        :class="{ 'border-t border-gray-100 dark:border-white/5': i > 0 }"
      >
        <div class="flex-1 min-w-0">
          <p class="text-[13.5px] font-semibold text-gray-800 dark:text-white/90 truncate">{{ s.stockName }}</p>
          <p class="text-[11.5px] text-gray-400 dark:text-white/40 font-mono mt-0.5">{{ s.stockCode }}</p>
        </div>
        <div class="w-32 shrink-0">
          <div class="flex justify-between text-[11px] mb-1.5">
            <span class="font-bold" :class="ratio(s) < 0.4 ? 'text-red-600 dark:text-red-300' : 'text-amber-600 dark:text-amber-300'">{{ s.onHand }}</span>
            <span class="text-gray-400 dark:text-white/40">/ {{ s.minStockQty }} {{ s.unit }}</span>
          </div>
          <div class="h-1.5 rounded-full bg-gray-100 dark:bg-white/10 overflow-hidden">
            <div class="h-full rounded-full" :class="ratio(s) < 0.4 ? 'bg-red-500' : 'bg-amber-500'"
                 :style="{ width: barWidth(s) + '%' }" />
          </div>
        </div>
        <router-link to="/purchase-orders"
          class="text-xs font-bold text-blue-600 dark:text-blue-400 hover:underline whitespace-nowrap shrink-0">
          Sipariş
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ArchiveBoxIcon, CheckCircleIcon } from '@heroicons/vue/24/outline';
import type { CriticalStockItem } from '../services/dashboardService';

const props = defineProps<{ items: CriticalStockItem[]; loading: boolean }>();

const ratio = (s: CriticalStockItem) => (s.minStockQty > 0 ? s.onHand / s.minStockQty : 0);
const barWidth = (s: CriticalStockItem) => Math.max(4, Math.min(100, Math.round(ratio(s) * 100)));
</script>
