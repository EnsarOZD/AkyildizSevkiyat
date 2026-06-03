<template>
  <div>
    <!-- Row 1: Number + name + "ŞİMDİ" badge -->
    <div class="flex items-center gap-3 mb-2">
      <!-- Stop indicator -->
      <div
        class="flex-shrink-0 w-9 h-9 rounded-xl flex items-center justify-center text-sm font-extrabold"
        :class="stop.isFullyDelivered
          ? 'bg-emerald-500/16 text-emerald-300'
          : isActive
            ? 'text-white shadow-lg shadow-blue-600/40 bg-gradient-to-br from-blue-500 to-blue-600'
            : 'bg-blue-500/15 text-blue-300'"
      >
        <CheckIcon v-if="stop.isFullyDelivered" class="w-4 h-4" />
        <span v-else>{{ stop.stopNumber }}</span>
      </div>

      <div class="flex-1 min-w-0">
        <p
          class="font-bold leading-tight break-words"
          :class="stop.isFullyDelivered ? 'text-emerald-300/90' : 'text-white'"
        >{{ stop.projectName }}</p>
        <p v-if="stop.projectAddress" class="text-xs text-white/40 break-words mt-0.5 flex items-start gap-1">
          <MapPinIcon class="w-3 h-3 flex-shrink-0 mt-0.5" />
          {{ stop.projectAddress }}
        </p>
      </div>

      <!-- Right side: "ŞİMDİ" badge or check -->
      <div class="flex-shrink-0">
        <span
          v-if="isActive"
          class="inline-flex items-center gap-1 px-2 py-0.5 text-[11px] font-bold bg-amber-400/16 text-amber-300 rounded-full"
        >ŞİMDİ</span>
        <CheckCircleIcon
          v-else-if="stop.isFullyDelivered"
          class="w-5 h-5 text-emerald-400"
        />
      </div>
    </div>

    <!-- Row 2: Meta info (kalem count + TLP) -->
    <div class="flex items-center gap-3 ml-12 mb-2">
      <span class="flex items-center gap-1 text-xs text-white/50">
        <ArchiveBoxIcon class="w-3.5 h-3.5 text-white/40" />
        {{ stop.totalLineCount }} kalem
      </span>
      <span v-if="tlpCode" class="text-xs font-mono text-white/30 truncate">
        {{ tlpCode }}
      </span>
      <span v-if="stop.shipments.length > 1" class="text-xs text-white/40">
        {{ stop.shipments.length }} irsaliye
      </span>
    </div>

    <!-- Active stop action buttons -->
    <div v-if="isActive" class="ml-12 mt-3 space-y-2">
      <!-- Teslimat Gir — primary CTA -->
      <router-link
        :to="{ name: 'DriverStop', params: { projectId: stop.projectId } }"
        class="flex items-center justify-center gap-2 w-full h-12 bg-gradient-to-br from-blue-500 to-blue-600 active:from-blue-600 active:to-blue-700 text-white text-sm font-bold rounded-2xl transition-colors shadow-lg shadow-blue-600/30"
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
          class="flex-1 flex items-center justify-center gap-1.5 h-11 bg-emerald-500/16 ring-1 ring-emerald-400/30 text-emerald-300 text-xs font-bold rounded-2xl transition-colors"
        >
          <PhoneIcon class="w-4 h-4" />
          Ara
        </a>
        <button
          v-if="mapsUrl"
          @click.stop="openMaps(mapsUrl)"
          class="flex-1 flex items-center justify-center gap-1.5 h-11 bg-white/[0.08] hover:bg-white/[0.12] text-white text-xs font-bold rounded-2xl transition-colors"
        >
          <MapIcon class="w-4 h-4" />
          Yol Tarifi
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import {
  MapPinIcon,
  CheckIcon,
  CheckCircleIcon,
  ArchiveBoxIcon,
  PhoneIcon,
  MapIcon,
  ClipboardDocumentListIcon,
} from '@heroicons/vue/24/outline';
import type { DeliveryStopDto } from '../../services/driverService';
import { useOpenMaps } from '../../composables/useOpenMaps';

const props = defineProps<{
  stop: DeliveryStopDto;
  isActive?: boolean;
}>();

const { openMaps } = useOpenMaps();

const tlpCode = computed(() => props.stop.shipments[0]?.externalOrderNumber ?? null);

const mapsUrl = computed(() => {
  const { projectLatitude: lat, projectLongitude: lng, projectAddress: addr } = props.stop;
  const destination = lat != null && lng != null
    ? `${lat},${lng}`
    : encodeURIComponent(addr ?? '');
  return destination ? `https://www.google.com/maps/dir/?api=1&destination=${destination}&travelmode=driving` : null;
});
</script>
