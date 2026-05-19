<template>
  <div class="bg-white dark:bg-[#0f2744] rounded-2xl border border-gray-100 dark:border-white/10 p-4 space-y-3">
    <div class="flex items-start justify-between gap-3">
      <div class="flex-1 min-w-0">
        <p class="font-semibold text-sm text-gray-900 dark:text-white">{{ title }}</p>
        <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5 leading-snug">{{ description }}</p>
      </div>
      <!-- Status badge -->
      <span
        class="flex-shrink-0 px-2.5 py-1 rounded-full text-[11px] font-semibold"
        :class="badgeClass"
      >{{ badgeLabel }}</span>
    </div>

    <!-- Denied instructions -->
    <div v-if="status === 'denied'" class="rounded-xl bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 p-3">
      <p class="text-xs text-amber-800 dark:text-amber-300 leading-snug">{{ instructions }}</p>
    </div>

    <!-- Action button -->
    <button
      v-if="status === 'prompt' || status === 'unknown'"
      @click="$emit('request')"
      class="w-full py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-sm font-semibold rounded-xl transition-colors"
    >
      İzin Ver
    </button>
    <button
      v-else-if="status === 'denied'"
      disabled
      class="w-full py-2.5 bg-gray-100 dark:bg-white/5 text-gray-400 dark:text-gray-500 text-sm font-medium rounded-xl cursor-not-allowed"
    >
      Tarayıcı ayarlarından açın
    </button>
    <div
      v-else-if="status === 'granted'"
      class="flex items-center justify-center gap-1.5 text-green-600 dark:text-green-400 text-sm font-medium py-1"
    >
      <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="2.5" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 12.75l6 6 9-13.5" />
      </svg>
      İzin verildi
    </div>
    <div
      v-else-if="status === 'unsupported'"
      class="text-center text-xs text-gray-400 py-1"
    >
      Bu cihazda desteklenmiyor
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';

type PermStatus = 'unknown' | 'granted' | 'denied' | 'prompt' | 'unsupported';

const props = defineProps<{
  title: string;
  description: string;
  status: PermStatus;
  instructions: string;
}>();

defineEmits<{ request: [] }>();

const badgeLabel = computed(() => ({
  granted:     'Verildi',
  denied:      'Reddedildi',
  prompt:      'İstenmedi',
  unknown:     'Bilinmiyor',
  unsupported: 'Desteklenmiyor',
}[props.status]));

const badgeClass = computed(() => ({
  granted:     'bg-green-100 dark:bg-green-900/40 text-green-700 dark:text-green-400',
  denied:      'bg-red-100 dark:bg-red-900/40 text-red-700 dark:text-red-400',
  prompt:      'bg-amber-100 dark:bg-amber-900/40 text-amber-700 dark:text-amber-400',
  unknown:     'bg-gray-100 dark:bg-white/10 text-gray-500 dark:text-gray-400',
  unsupported: 'bg-gray-100 dark:bg-white/10 text-gray-400',
}[props.status]));
</script>
