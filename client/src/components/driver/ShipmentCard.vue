<template>
  <div class="space-y-2">
    <!-- Header row -->
    <div class="flex items-start justify-between gap-2">
      <div class="flex-1 min-w-0">
        <p class="font-semibold text-gray-900 dark:text-white truncate">{{ shipment.projectName }}</p>
        <p v-if="shipment.talepNo" class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
          Talep: {{ shipment.talepNo }}
        </p>
      </div>
      <StatusBadge :status="shipment.status" />
    </div>

    <!-- Address with navigation link -->
    <div v-if="shipment.projectAddress" class="flex items-start gap-2">
      <MapPinIcon class="w-4 h-4 text-gray-400 flex-shrink-0 mt-0.5" />
      <a
        :href="mapsUrl"
        target="_blank"
        rel="noopener"
        @click.stop
        class="text-sm text-blue-600 dark:text-blue-400 hover:underline leading-snug"
      >
        {{ shipment.projectAddress }}
      </a>
    </div>

    <!-- Recipient info -->
    <div v-if="shipment.teslimAlacakKisiler" class="flex items-center gap-2">
      <UserIcon class="w-4 h-4 text-gray-400 flex-shrink-0" />
      <span class="text-sm text-gray-700 dark:text-gray-300">{{ shipment.teslimAlacakKisiler }}</span>
    </div>

    <!-- Phone -->
    <div v-if="shipment.teslimAlacakTelefon" class="flex items-center gap-2">
      <PhoneIcon class="w-4 h-4 text-gray-400 flex-shrink-0" />
      <a
        :href="`tel:${shipment.teslimAlacakTelefon}`"
        @click.stop
        class="text-sm text-blue-600 dark:text-blue-400 hover:underline"
      >
        {{ shipment.teslimAlacakTelefon }}
      </a>
    </div>

    <!-- Footer row -->
    <div class="flex items-center justify-between pt-1 border-t border-gray-100 dark:border-white/5 text-xs text-gray-500 dark:text-gray-400">
      <div class="flex items-center gap-1">
        <CalendarIcon class="w-3.5 h-3.5" />
        {{ formatDate(shipment.deliveryDate) }}
      </div>
      <div class="flex items-center gap-1">
        <ArchiveBoxIcon class="w-3.5 h-3.5" />
        {{ shipment.lineCount }} kalem
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import {
  MapPinIcon,
  UserIcon,
  PhoneIcon,
  CalendarIcon,
  ArchiveBoxIcon,
} from '@heroicons/vue/24/outline';
import StatusBadge from './StatusBadge.vue';
import type { DriverShipmentDto } from '../../services/driverService';

const props = defineProps<{ shipment: DriverShipmentDto }>();

const mapsUrl = computed(() => {
  const addr = encodeURIComponent(props.shipment.projectAddress ?? '');
  return `https://www.google.com/maps/search/?api=1&query=${addr}`;
});

function formatDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric' });
}
</script>
