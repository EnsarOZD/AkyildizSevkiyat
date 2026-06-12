<template>
  <div class="rounded-2xl border border-gray-200 dark:border-white/10 bg-white dark:bg-[#0f2238] p-4"
       style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">
    <div class="flex items-center gap-1.5 mb-2">
      <span v-if="tone !== 'neutral'" class="w-1.5 h-1.5 rounded-full" :class="dotCls"></span>
      <p class="text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-white/55 leading-tight">{{ label }}</p>
    </div>
    <p class="text-[26px] font-extrabold tracking-tight leading-none" :class="valueCls">{{ value }}</p>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(defineProps<{
  label: string;
  value: string | number;
  /** neutral | blue | amber | green | indigo | red | gray */
  tone?: string;
}>(), { tone: 'neutral' });

const VALUE: Record<string, string> = {
  neutral: 'text-gray-900 dark:text-white',
  blue:   'text-blue-600 dark:text-blue-300',
  amber:  'text-amber-600 dark:text-amber-300',
  green:  'text-emerald-600 dark:text-emerald-300',
  indigo: 'text-indigo-600 dark:text-indigo-300',
  red:    'text-red-600 dark:text-red-300',
  gray:   'text-gray-500 dark:text-white/60',
};
const DOT: Record<string, string> = {
  blue: 'bg-blue-500', amber: 'bg-amber-500', green: 'bg-emerald-500',
  indigo: 'bg-indigo-500', red: 'bg-red-500', gray: 'bg-gray-400',
};
const valueCls = computed(() => VALUE[props.tone] || VALUE.neutral);
const dotCls = computed(() => DOT[props.tone] || '');
</script>
