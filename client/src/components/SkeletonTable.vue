<template>
  <div v-bind="$attrs">
    <!-- Desktop table skeleton -->
    <div class="hidden md:block bg-white dark:bg-gray-900 shadow rounded-lg overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th v-for="i in columns" :key="i" class="px-6 py-3">
              <div class="h-3 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" :style="`width: ${50 + (i * 17) % 50}%`"></div>
            </th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="r in rows" :key="r" class="animate-pulse">
            <td v-for="c in columns" :key="c" class="px-6 py-4">
              <div class="h-4 bg-gray-200 dark:bg-gray-700 rounded" :style="`width: ${40 + (r + c * 3) % 55}%; opacity: ${1 - r * 0.08}`"></div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Mobile card skeleton -->
    <div class="md:hidden space-y-3">
      <div v-for="r in rows" :key="r" class="bg-white dark:bg-gray-900 p-4 rounded-lg shadow border border-gray-100 dark:border-gray-700 animate-pulse"
        :style="`opacity: ${1 - r * 0.1}`"
      >
        <div class="flex justify-between mb-3">
          <div class="h-5 bg-gray-200 dark:bg-gray-700 rounded w-24"></div>
          <div class="h-5 bg-gray-200 dark:bg-gray-700 rounded w-16"></div>
        </div>
        <div class="h-4 bg-gray-200 dark:bg-gray-700 rounded w-3/4 mb-2"></div>
        <div class="h-3 bg-gray-200 dark:bg-gray-700 rounded w-1/2 mb-4"></div>
        <div class="h-8 bg-gray-200 dark:bg-gray-700 rounded w-20 ml-auto"></div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
withDefaults(defineProps<{
  rows?: number;
  columns?: number;
}>(), { rows: 5, columns: 6 });
</script>
