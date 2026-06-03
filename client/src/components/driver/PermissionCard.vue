<template>
  <div class="bg-[#0f2038] rounded-2xl border border-white/[0.07] p-4 space-y-3">
    <div class="flex items-start gap-3">
      <!-- Icon chip -->
      <div class="w-10 h-10 rounded-xl bg-blue-500/15 text-blue-300 flex items-center justify-center flex-shrink-0">
        <component :is="icon" class="w-5 h-5" />
      </div>
      <div class="flex-1 min-w-0">
        <p class="font-bold text-sm text-white">{{ title }}</p>
        <p class="text-xs text-white/45 mt-0.5 leading-snug">{{ description }}</p>
      </div>
      <!-- Status badge -->
      <span
        class="flex-shrink-0 px-2.5 py-1 rounded-full text-[11px] font-bold"
        :class="badgeClass"
      >{{ badgeLabel }}</span>
    </div>

    <!-- Denied instructions -->
    <div v-if="status === 'denied'" class="rounded-xl bg-amber-500/10 border border-amber-400/30 p-3">
      <p class="text-xs text-amber-200/90 leading-snug">{{ instructions }}</p>
    </div>

    <!-- Action button -->
    <button
      v-if="status === 'prompt' || status === 'unknown'"
      @click="$emit('request')"
      class="w-full h-11 bg-gradient-to-br from-blue-500 to-blue-600 hover:from-blue-400 hover:to-blue-500 text-white text-sm font-bold rounded-xl transition-colors shadow-lg shadow-blue-600/25"
    >
      İzin Ver
    </button>
    <button
      v-else-if="status === 'denied'"
      disabled
      class="w-full h-11 bg-white/5 text-white/40 text-sm font-medium rounded-xl cursor-not-allowed"
    >
      Tarayıcı ayarlarından açın
    </button>
    <div
      v-else-if="status === 'granted'"
      class="flex items-center justify-center gap-1.5 text-emerald-400 text-sm font-semibold py-1"
    >
      <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="2.5" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 12.75l6 6 9-13.5" />
      </svg>
      İzin verildi
    </div>
    <div
      v-else-if="status === 'unsupported'"
      class="text-center text-xs text-white/35 py-1"
    >
      Bu cihazda desteklenmiyor
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { MapPinIcon, CameraIcon, BellIcon, ShieldCheckIcon } from '@heroicons/vue/24/outline';

type PermStatus = 'unknown' | 'granted' | 'denied' | 'prompt' | 'unsupported';

const props = defineProps<{
  title: string;
  description: string;
  status: PermStatus;
  instructions: string;
}>();

defineEmits<{ request: [] }>();

// Başlığa göre ikon (parent API'si değişmeden drop-in kalsın diye)
const icon = computed(() => {
  if (props.title.includes('Konum')) return MapPinIcon;
  if (props.title.includes('Kamera')) return CameraIcon;
  if (props.title.includes('Bildirim')) return BellIcon;
  return ShieldCheckIcon;
});

const badgeLabel = computed(() => ({
  granted:     'Verildi',
  denied:      'Reddedildi',
  prompt:      'İstenmedi',
  unknown:     'Bilinmiyor',
  unsupported: 'Desteklenmiyor',
}[props.status]));

const badgeClass = computed(() => ({
  granted:     'bg-emerald-500/16 text-emerald-300',
  denied:      'bg-red-500/16 text-red-300',
  prompt:      'bg-amber-500/16 text-amber-300',
  unknown:     'bg-white/10 text-white/50',
  unsupported: 'bg-white/10 text-white/40',
}[props.status]));
</script>
