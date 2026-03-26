<template>
  <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
    <div class="flex items-center justify-between mb-4">
      <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">Kritik Stoklar</h2>
      <router-link to="/stocks" class="text-xs text-blue-600 hover:underline">Tümünü gör</router-link>
    </div>

    <!-- Loading skeleton -->
    <template v-if="loading">
      <div class="space-y-2">
        <div v-for="i in 3" :key="i" class="animate-pulse flex items-center gap-3 py-2">
          <div class="h-3 bg-gray-200 dark:bg-gray-700 rounded flex-1"></div>
          <div class="h-3 bg-gray-200 dark:bg-gray-700 rounded w-12"></div>
          <div class="h-3 bg-gray-200 dark:bg-gray-700 rounded w-12"></div>
          <div class="h-3 bg-gray-200 dark:bg-gray-700 rounded w-14"></div>
        </div>
      </div>
    </template>

    <!-- Empty state -->
    <template v-else-if="items.length === 0">
      <div class="flex flex-col items-center justify-center py-6 gap-2">
        <CheckCircleIcon class="w-8 h-8 text-green-400" />
        <p class="text-sm text-gray-400 dark:text-gray-500">Kritik stok yok</p>
      </div>
    </template>

    <!-- Table -->
    <template v-else>
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="border-b border-gray-100 dark:border-gray-700">
              <th class="text-left text-xs font-medium text-gray-400 dark:text-gray-500 pb-2 pr-3">Stok</th>
              <th class="text-right text-xs font-medium text-gray-400 dark:text-gray-500 pb-2 px-2">Mevcut</th>
              <th class="text-right text-xs font-medium text-gray-400 dark:text-gray-500 pb-2 px-2">Minimum</th>
              <th class="text-right text-xs font-medium text-gray-400 dark:text-gray-500 pb-2 pl-2">Fark</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="item in visibleItems"
              :key="item.stockMasterId"
              class="border-b border-gray-50 dark:border-gray-800 last:border-0"
              :class="rowClass(item)"
            >
              <td class="py-2 pr-3">
                <p class="font-medium text-gray-800 dark:text-gray-200 truncate max-w-[160px]" :title="item.stockName">{{ item.stockName }}</p>
                <p class="text-xs text-gray-400 dark:text-gray-500">{{ item.stockCode }}</p>
              </td>
              <td class="py-2 px-2 text-right font-semibold" :class="item.onHand < 0 ? 'text-red-600' : 'text-gray-700 dark:text-gray-300'">
                {{ item.onHand }} {{ item.unit }}
              </td>
              <td class="py-2 px-2 text-right text-gray-500 dark:text-gray-400">
                {{ item.minStockQty }} {{ item.unit }}
              </td>
              <td class="py-2 pl-2 text-right font-semibold" :class="diffClass(item)">
                {{ item.onHand - item.minStockQty }}
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- More link -->
      <div v-if="items.length > 5" class="mt-3 pt-3 border-t border-gray-100 dark:border-gray-700">
        <router-link
          to="/stocks"
          class="text-xs text-blue-600 hover:underline"
        >
          +{{ items.length - 5 }} daha
        </router-link>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { CheckCircleIcon } from '@heroicons/vue/24/outline';
import type { CriticalStockItem } from '../services/dashboardService';

const props = defineProps<{
  items: CriticalStockItem[];
  loading: boolean;
}>();

const visibleItems = computed(() => props.items.slice(0, 5));

const rowClass = (item: CriticalStockItem): string => {
  if (item.onHand < 0) return 'bg-red-50/60 dark:bg-red-900/10';
  if (item.onHand < item.minStockQty / 2) return 'bg-orange-50/60 dark:bg-orange-900/10';
  return '';
};

const diffClass = (item: CriticalStockItem): string => {
  const diff = item.onHand - item.minStockQty;
  if (diff < 0) return 'text-red-600';
  return 'text-orange-500';
};
</script>
