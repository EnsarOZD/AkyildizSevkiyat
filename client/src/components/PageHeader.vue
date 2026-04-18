<template>
  <div
    class="relative overflow-hidden bg-white dark:bg-gray-900 rounded-3xl p-6 sm:p-8 shadow-xl dark:shadow-none border group mb-6"
    :class="colors.border"
    :style="`box-shadow: 0 20px 25px -5px ${colors.shadowColor}, 0 8px 10px -6px ${colors.shadowColor}`"
  >
    <div class="absolute -top-24 -right-24 h-64 w-64 rounded-full blur-3xl transition-all duration-700" :class="[colors.glow, colors.hoverGlow]"></div>
    <div class="relative flex flex-col sm:flex-row sm:items-center justify-between gap-4">
      <div class="flex items-center gap-4">
        <div class="p-3.5 rounded-2xl text-white shadow-lg shrink-0" :class="colors.iconBg">
          <slot name="icon">
            <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </slot>
        </div>
        <div>
          <h1 class="text-2xl font-black text-gray-900 dark:text-gray-100 tracking-tight leading-none">{{ title }}</h1>
          <p v-if="subtitle" class="text-sm font-bold text-gray-400 mt-1.5 uppercase tracking-widest">{{ subtitle }}</p>
        </div>
      </div>
      <div v-if="$slots.actions" class="flex items-center gap-2 shrink-0">
        <slot name="actions" />
      </div>
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

type ColorDef = { border: string; iconBg: string; glow: string; hoverGlow: string; shadowColor: string };
const colorDefs: Record<string, ColorDef> = {
  indigo: { border: 'border-indigo-50 dark:border-indigo-900',  iconBg: 'bg-indigo-600',  glow: 'bg-indigo-500/5',  hoverGlow: 'group-hover:bg-indigo-500/10',  shadowColor: 'rgba(99,102,241,0.08)' },
  blue:   { border: 'border-blue-50 dark:border-blue-900',      iconBg: 'bg-blue-600',    glow: 'bg-blue-500/5',    hoverGlow: 'group-hover:bg-blue-500/10',    shadowColor: 'rgba(59,130,246,0.08)' },
  green:  { border: 'border-green-50 dark:border-green-900',    iconBg: 'bg-green-600',   glow: 'bg-green-500/5',   hoverGlow: 'group-hover:bg-green-500/10',   shadowColor: 'rgba(22,163,74,0.08)' },
  amber:  { border: 'border-amber-50 dark:border-amber-900',    iconBg: 'bg-amber-500',   glow: 'bg-amber-500/5',   hoverGlow: 'group-hover:bg-amber-500/10',   shadowColor: 'rgba(245,158,11,0.08)' },
  orange: { border: 'border-orange-50 dark:border-orange-900',  iconBg: 'bg-orange-500',  glow: 'bg-orange-500/5',  hoverGlow: 'group-hover:bg-orange-500/10',  shadowColor: 'rgba(249,115,22,0.08)' },
  teal:   { border: 'border-teal-50 dark:border-teal-900',      iconBg: 'bg-teal-600',    glow: 'bg-teal-500/5',    hoverGlow: 'group-hover:bg-teal-500/10',    shadowColor: 'rgba(13,148,136,0.08)' },
  purple: { border: 'border-purple-50 dark:border-purple-900',  iconBg: 'bg-purple-600',  glow: 'bg-purple-500/5',  hoverGlow: 'group-hover:bg-purple-500/10',  shadowColor: 'rgba(147,51,234,0.08)' },
  red:    { border: 'border-red-50 dark:border-red-900',        iconBg: 'bg-red-600',     glow: 'bg-red-500/5',     hoverGlow: 'group-hover:bg-red-500/10',     shadowColor: 'rgba(220,38,38,0.08)' },
  gray:   { border: 'border-gray-100 dark:border-gray-800',     iconBg: 'bg-gray-600',    glow: 'bg-gray-500/5',    hoverGlow: 'group-hover:bg-gray-500/10',    shadowColor: 'rgba(107,114,128,0.08)' },
  slate:  { border: 'border-slate-100 dark:border-slate-800',   iconBg: 'bg-slate-600',   glow: 'bg-slate-500/5',   hoverGlow: 'group-hover:bg-slate-500/10',   shadowColor: 'rgba(71,85,105,0.08)' },
};

const colors = computed((): ColorDef => (colorDefs[props.color ?? 'indigo'] ?? colorDefs['indigo'])!);
</script>
