<template>
  <span :class="cls" class="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold whitespace-nowrap">
    {{ label }}
  </span>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{ status: string }>();

// Akyıldız Şoför — koyu (C+B) tema rozetleri (yarı saydam, canlı)
const map: Record<string, { label: string; cls: string }> = {
  AssignedToVehicle: { label: 'Araçta',       cls: 'bg-blue-500/16 text-blue-300 ring-1 ring-blue-400/30' },
  Dispatched:        { label: 'Yolda',         cls: 'bg-amber-500/16 text-amber-300 ring-1 ring-amber-400/30' },
  Delivered:         { label: 'Teslim Edildi', cls: 'bg-emerald-500/16 text-emerald-300 ring-1 ring-emerald-400/30' },
  ReturnedToWarehouse: { label: 'İade Edildi', cls: 'bg-orange-500/16 text-orange-300 ring-1 ring-orange-400/30' },
};

const entry = computed(() => map[props.status] ?? { label: props.status, cls: 'bg-white/10 text-gray-300 ring-1 ring-white/15' });
const label = computed(() => entry.value.label);
const cls   = computed(() => entry.value.cls);
</script>
