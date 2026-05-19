<template>
  <div>
    <!-- Row 1: Number + name + "ŞİMDİ" badge -->
    <div class="flex items-center gap-3 mb-2">
      <!-- Stop indicator -->
      <div
        class="flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center text-sm font-bold"
        :class="stop.isFullyDelivered
          ? 'bg-green-100 dark:bg-green-900/30 text-green-600 dark:text-green-400'
          : isActive
            ? 'bg-blue-600 text-white'
            : 'bg-gray-100 dark:bg-gray-700 text-gray-500 dark:text-gray-400'"
      >
        <CheckIcon v-if="stop.isFullyDelivered" class="w-4 h-4" />
        <span v-else>{{ stop.stopNumber }}</span>
      </div>

      <div class="flex-1 min-w-0">
        <p
          class="font-semibold leading-tight truncate"
          :class="stop.isFullyDelivered
            ? 'text-gray-400 dark:text-gray-500'
            : 'text-gray-900 dark:text-white'"
        >{{ stop.projectName }}</p>
        <p v-if="stop.projectAddress" class="text-xs text-gray-400 dark:text-gray-500 truncate mt-0.5 flex items-center gap-1">
          <MapPinIcon class="w-3 h-3 flex-shrink-0" />
          {{ stop.projectAddress }}
        </p>
      </div>

      <!-- Right side: "ŞİMDİ" badge or check -->
      <div class="flex-shrink-0">
        <span
          v-if="isActive"
          class="px-2 py-0.5 text-[11px] font-bold bg-amber-100 dark:bg-amber-900/40 text-amber-700 dark:text-amber-400 rounded-full border border-amber-200 dark:border-amber-700"
        >ŞİMDİ</span>
        <CheckIcon
          v-else-if="stop.isFullyDelivered"
          class="w-4 h-4 text-green-500 dark:text-green-400"
        />
      </div>
    </div>

    <!-- Row 2: Meta info (kalem count + TLP) -->
    <div class="flex items-center gap-3 ml-11 mb-2">
      <span class="flex items-center gap-1 text-xs text-gray-500 dark:text-gray-400">
        <ArchiveBoxIcon class="w-3.5 h-3.5 text-gray-400" />
        {{ stop.totalLineCount }} kalem
      </span>
      <span v-if="tlpCode" class="text-xs font-mono text-gray-400 dark:text-gray-500 truncate">
        {{ tlpCode }}
      </span>
      <span v-if="stop.shipments.length > 1" class="text-xs text-gray-400">
        {{ stop.shipments.length }} irsaliye
      </span>
    </div>

    <!-- Active stop action buttons -->
    <div v-if="isActive" class="ml-11 mt-3 space-y-2">
      <!-- Teslimat Gir — primary CTA -->
      <router-link
        :to="{ name: 'DriverStop', params: { projectId: stop.projectId } }"
        class="flex items-center justify-center gap-2 w-full py-2.5 bg-blue-600 hover:bg-blue-700 active:bg-blue-800 text-white text-sm font-semibold rounded-xl transition-colors"
      >
        <ClipboardDocumentListIcon class="w-4 h-4" />
        Teslimat Gir
      </router-link>

      <!-- Phone + Navigate -->
      <div class="flex gap-2">
        <a
          v-if="stop.contactPhone"
          :href="`tel:${stop.contactPhone}`"
          @click.stop
          class="flex-1 flex items-center justify-center gap-1.5 py-2 bg-gray-100 dark:bg-white/10 hover:bg-gray-200 dark:hover:bg-white/15 text-gray-700 dark:text-gray-300 text-xs font-medium rounded-xl transition-colors"
        >
          <PhoneIcon class="w-4 h-4" />
          Ara
        </a>
        <a
          v-if="mapsUrl"
          :href="mapsUrl"
          target="_blank"
          rel="noopener"
          @click.stop
          class="flex-1 flex items-center justify-center gap-1.5 py-2 bg-gray-100 dark:bg-white/10 hover:bg-gray-200 dark:hover:bg-white/15 text-gray-700 dark:text-gray-300 text-xs font-medium rounded-xl transition-colors"
        >
          <MapIcon class="w-4 h-4" />
          Yol Tarifi
        </a>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import {
  MapPinIcon,
  CheckIcon,
  ArchiveBoxIcon,
  PhoneIcon,
  MapIcon,
  ClipboardDocumentListIcon,
} from '@heroicons/vue/24/outline';
import type { DeliveryStopDto } from '../../services/driverService';

const props = defineProps<{
  stop: DeliveryStopDto;
  isActive?: boolean;
}>();

const tlpCode = computed(() => props.stop.shipments[0]?.externalOrderNumber ?? null);

const mapsUrl = computed(() => {
  const { projectLatitude: lat, projectLongitude: lng, projectAddress: addr } = props.stop;
  const destination = lat != null && lng != null
    ? `${lat},${lng}`
    : encodeURIComponent(addr ?? '');
  return destination ? `https://www.google.com/maps/dir/?api=1&destination=${destination}&travelmode=driving` : null;
});
</script>
