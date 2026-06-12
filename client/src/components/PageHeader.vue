<template>
  <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
    <div class="flex items-center gap-3.5">
      <div class="h-11 w-11 rounded-xl flex items-center justify-center shrink-0 text-white" :class="colors.iconBg">
        <slot name="icon">
          <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
          </svg>
        </slot>
      </div>
      <div>
        <h1 class="text-2xl font-extrabold text-gray-900 dark:text-gray-100 tracking-tight leading-none">{{ title }}</h1>
        <p v-if="subtitle" class="text-xs font-semibold text-gray-400 dark:text-gray-500 mt-1.5 uppercase tracking-wider">{{ subtitle }}</p>
      </div>
    </div>
    <div v-if="$slots.actions" class="flex items-center gap-2 shrink-0">
      <slot name="actions" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  title: string;
  subtitle?: string;
  color?: 'indigo' | 'blue' | 'green' | 'amber' | 'orange' | 'teal' | 'purple' | 'red' | 'gray' | 'slate';
}>();
// Marka varsayılanı: mavi (eski indigo yerine)

// Flat tasarımda yalnızca ikon kutusu rengi kullanılır.
type ColorDef = { iconBg: string };
const colorDefs: Record<string, ColorDef> = {
  indigo: { iconBg: 'bg-blue-600' },
  blue:   { iconBg: 'bg-blue-600' },
  green:  { iconBg: 'bg-green-600' },
  amber:  { iconBg: 'bg-amber-500' },
  orange: { iconBg: 'bg-orange-500' },
  teal:   { iconBg: 'bg-blue-600' },
  purple: { iconBg: 'bg-violet-600' },
  red:    { iconBg: 'bg-red-600' },
  gray:   { iconBg: 'bg-gray-600' },
  slate:  { iconBg: 'bg-slate-600' },
};

const colors = computed((): ColorDef => (colorDefs[props.color ?? 'blue'] ?? colorDefs['blue'])!);
</script>
