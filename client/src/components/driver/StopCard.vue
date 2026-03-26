<template>
  <div class="space-y-2">
    <!-- Header row: stop number + project name + status -->
    <div class="flex items-start gap-3">
      <!-- Stop number badge -->
      <div
        class="flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold"
        :class="stop.isFullyDelivered
          ? 'bg-green-100 dark:bg-green-900/40 text-green-600 dark:text-green-400'
          : 'bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300'"
      >
        <CheckIcon v-if="stop.isFullyDelivered" class="w-4 h-4" aria-hidden="true" />
        <span v-else>{{ stop.stopNumber }}</span>
      </div>

      <div class="flex-1 min-w-0">
        <p class="font-semibold text-gray-900 dark:text-white truncate">{{ stop.projectName }}</p>
        <p v-if="stop.zoneName" class="text-xs text-gray-400 mt-0.5">{{ stop.zoneName }}</p>
      </div>

      <!-- Shipment / line summary -->
      <div class="flex-shrink-0 text-right">
        <p class="text-sm font-semibold text-gray-900 dark:text-white">
          {{ stop.shipments.length }}
          <span class="text-xs font-normal text-gray-500">irsaliye</span>
        </p>
        <p class="text-xs text-gray-400">{{ stop.totalLineCount }} kalem</p>
      </div>
    </div>

    <!-- Address -->
    <div v-if="stop.projectAddress" class="flex items-start gap-2 ml-11">
      <MapPinIcon class="w-4 h-4 text-gray-400 flex-shrink-0 mt-0.5" aria-hidden="true" />
      <a
        :href="mapsUrl"
        target="_blank"
        rel="noopener"
        @click.stop
        class="text-sm text-blue-600 dark:text-blue-400 hover:underline leading-snug"
      >
        {{ stop.projectAddress }}
      </a>
    </div>

    <!-- Contact info from first shipment -->
    <div v-if="firstShipment?.teslimAlacakKisiler" class="flex items-center gap-2 ml-11">
      <UserIcon class="w-4 h-4 text-gray-400 flex-shrink-0" aria-hidden="true" />
      <span class="text-sm text-gray-700 dark:text-gray-300">{{ firstShipment.teslimAlacakKisiler }}</span>
    </div>
    <div v-if="firstShipment?.teslimAlacakTelefon" class="flex items-center gap-2 ml-11">
      <PhoneIcon class="w-4 h-4 text-gray-400 flex-shrink-0" aria-hidden="true" />
      <a
        :href="`tel:${firstShipment.teslimAlacakTelefon}`"
        @click.stop
        class="text-sm text-blue-600 dark:text-blue-400 hover:underline"
      >
        {{ firstShipment.teslimAlacakTelefon }}
      </a>
    </div>

    <!-- Shipment pills -->
    <div class="flex flex-wrap gap-1.5 ml-11 mt-1">
      <span
        v-for="s in stop.shipments"
        :key="s.id"
        class="inline-flex items-center gap-1 text-[11px] px-2 py-0.5 rounded-full"
        :class="[
          s.status === 'Delivered' ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' : '',
          s.status === 'Dispatched' ? 'bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-400' : '',
          (s.status !== 'Delivered' && s.status !== 'Dispatched') ? 'bg-indigo-50 dark:bg-indigo-900/20 text-indigo-700 dark:text-indigo-300' : ''
        ]"
      >
        <CheckIcon v-if="s.status === 'Delivered'" class="w-3 h-3" aria-hidden="true" />
        <TruckIcon v-else-if="s.status === 'Dispatched'" class="w-3 h-3" aria-hidden="true" />
        {{ s.talepNo || ('#' + s.id) }} 
        <span v-if="s.status === 'Dispatched'" class="opacity-75">(Yolda)</span>
      </span>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { MapPinIcon, UserIcon, PhoneIcon, CheckIcon, TruckIcon } from '@heroicons/vue/24/outline';
import type { DeliveryStopDto } from '../../services/driverService';

const props = defineProps<{ stop: DeliveryStopDto }>();

const firstShipment = computed(() => props.stop.shipments[0]);

const mapsUrl = computed(() => {
  const { projectLatitude: lat, projectLongitude: lng, projectAddress: addr } = props.stop;
  const destination = (lat != null && lng != null)
    ? `${lat},${lng}`
    : encodeURIComponent(addr ?? '');
  return `https://www.google.com/maps/dir/?api=1&destination=${destination}&travelmode=driving`;
});
</script>
