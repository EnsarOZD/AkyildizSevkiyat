<template>
  <div class="flex flex-wrap items-end gap-3 p-4 rounded-2xl bg-white dark:bg-[#0f2238] border border-gray-200 dark:border-white/10"
       style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">
    <!-- tarih aralığı -->
    <div v-if="dates">
      <label class="block text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-white/55 mb-1.5">Başlangıç</label>
      <input :value="startDate" @input="onStart" type="date" :class="inputCls" />
    </div>
    <div v-if="dates">
      <label class="block text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-white/55 mb-1.5">Bitiş</label>
      <input :value="endDate" @input="onEnd" type="date" :class="inputCls" />
    </div>

    <!-- ek filtreler (slot) -->
    <slot name="filters" />

    <!-- Filtrele -->
    <button @click="$emit('apply')" :disabled="loading"
      class="h-[42px] px-5 rounded-xl bg-blue-600 hover:bg-blue-700 disabled:opacity-60 text-white text-sm font-bold inline-flex items-center gap-2 transition-colors">
      <ArrowPathIcon v-if="loading" class="w-4 h-4 animate-spin" />
      <FunnelIcon v-else class="w-4 h-4" />
      {{ applyLabel }}
    </button>

    <!-- Excel -->
    <button v-if="canExport" @click="$emit('export')"
      class="ml-auto h-[42px] px-4 rounded-xl border border-gray-200 dark:border-white/15 bg-white dark:bg-white/5 text-gray-700 dark:text-white/80 hover:bg-gray-50 dark:hover:bg-white/10 text-sm font-bold inline-flex items-center gap-2 transition-colors">
      <ArrowDownTrayIcon class="w-4 h-4 text-emerald-600 dark:text-emerald-400" />
      Excel İndir
    </button>
  </div>
</template>

<script setup lang="ts">
import { ArrowPathIcon, FunnelIcon, ArrowDownTrayIcon } from '@heroicons/vue/24/outline';

withDefaults(defineProps<{
  startDate?: string;
  endDate?: string;
  loading?: boolean;
  canExport?: boolean;
  applyLabel?: string;
  /** tarih girişlerini göster (varsayılan true) */
  dates?: boolean;
}>(), { applyLabel: 'Filtrele', dates: true });

const emit = defineEmits<{
  (e: 'update:startDate', v: string): void;
  (e: 'update:endDate', v: string): void;
  (e: 'apply'): void;
  (e: 'export'): void;
}>();

const onStart = (e: Event) => emit('update:startDate', (e.target as HTMLInputElement).value);
const onEnd = (e: Event) => emit('update:endDate', (e.target as HTMLInputElement).value);

const inputCls =
  'h-[42px] px-3 rounded-xl border border-gray-200 dark:border-white/15 bg-white dark:bg-[#13294a] ' +
  'text-sm text-gray-800 dark:text-white outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-500/12 transition-all';
</script>
