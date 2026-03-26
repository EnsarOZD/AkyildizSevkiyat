<template>
  <span :class="cls" class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium whitespace-nowrap">
    {{ label }}
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{ status: string }>();

const map: Record<string, { label: string; cls: string }> = {
  AssignedToVehicle: { label: 'Araçta',       cls: 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300' },
  Delivered:         { label: 'Teslim Edildi', cls: 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300' },
};

const entry = computed(() => map[props.status] ?? { label: props.status, cls: 'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300' });
const label = computed(() => entry.value.label);
const cls   = computed(() => entry.value.cls);
</script>
