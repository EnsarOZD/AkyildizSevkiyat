<template>
  <div class="flex flex-col items-center justify-center py-16 px-6 text-center">
    <component
      v-if="icon"
      :is="icon"
      class="w-12 h-12 text-gray-300 mb-4"
    />
    <div v-else class="w-12 h-12 rounded-full bg-gray-100 dark:bg-gray-800 flex items-center justify-center mb-4">
      <InboxIcon class="w-6 h-6 text-gray-400 dark:text-gray-600" />
    </div>

    <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-1">{{ title }}</h3>
    <p v-if="description" class="text-sm text-gray-400 dark:text-gray-600 max-w-xs mb-4">{{ description }}</p>

    <slot />
  </div>
</template>

<script setup lang="ts">
import type { Component } from 'vue';
import { InboxIcon } from '@heroicons/vue/24/outline';

defineProps<{
  title: string;
  description?: string;
  icon?: Component;
}>();
</script>
