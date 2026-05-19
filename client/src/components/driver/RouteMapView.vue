<template>
  <div class="relative w-full h-full">
    <div ref="mapEl" class="w-full h-full" />

    <!-- Legend -->
    <div
      v-if="stops.length > 0"
      class="absolute top-3 right-3 bg-white dark:bg-gray-800 rounded-xl shadow-md px-3 py-2 text-xs space-y-1 z-[400]"
    >
      <div class="flex items-center gap-2">
        <span class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-blue-600 text-white font-bold text-[10px]">N</span>
        <span class="text-gray-600 dark:text-gray-300">Durak</span>
      </div>
      <div class="flex items-center gap-2">
        <span class="inline-flex items-center justify-center w-5 h-5 rounded-full bg-green-500 text-white font-bold text-[10px]">✓</span>
        <span class="text-gray-600 dark:text-gray-300">Teslim edildi</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';
import type { DeliveryStopDto } from '../../services/driverService';

// Leaflet is loaded dynamically to avoid SSR issues
import type { Map as LMap, Marker, DivIcon } from 'leaflet';

const props = defineProps<{
  stops: DeliveryStopDto[];
  activeProjectId?: number;
}>();

const mapEl = ref<HTMLElement | null>(null);
let map: LMap | null = null;
let markers: Marker[] = [];

function makeIcon(L: typeof import('leaflet'), label: string, delivered: boolean, active: boolean): DivIcon {
  const bg = delivered ? '#22c55e' : active ? '#2563eb' : '#3b82f6';
  const border = active ? '#1d4ed8' : 'transparent';
  return L.divIcon({
    className: '',
    html: `<div style="
      background:${bg};
      border:2px solid ${border};
      box-shadow:${active ? '0 0 0 3px rgba(37,99,235,0.4)' : '0 2px 4px rgba(0,0,0,0.3)'};
      width:28px;height:28px;border-radius:50%;
      display:flex;align-items:center;justify-content:center;
      color:white;font-size:11px;font-weight:bold;
    ">${label}</div>`,
    iconSize: [28, 28],
    iconAnchor: [14, 14],
    popupAnchor: [0, -16],
  });
}

async function initMap() {
  if (!mapEl.value) return;

  const L = await import('leaflet');
  // @ts-ignore — Leaflet CSS
  await import('leaflet/dist/leaflet.css');

  // Fix default icon paths broken by bundlers
  delete (L.Icon.Default.prototype as any)._getIconUrl;
  L.Icon.Default.mergeOptions({
    iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
    iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
    shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
  });

  const stopsWithCoords = props.stops.filter(
    s => s.projectLatitude != null && s.projectLongitude != null
  );

  if (stopsWithCoords.length === 0) return;

  const center: [number, number] = [
    stopsWithCoords.reduce((s, p) => s + p.projectLatitude!, 0) / stopsWithCoords.length,
    stopsWithCoords.reduce((s, p) => s + p.projectLongitude!, 0) / stopsWithCoords.length,
  ];

  map = L.map(mapEl.value, { zoomControl: true }).setView(center, 8);

  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© OpenStreetMap contributors',
    maxZoom: 19,
  }).addTo(map);

  renderMarkers(L, stopsWithCoords);

  // Always fit bounds to show all markers
  const bounds = L.latLngBounds(
    stopsWithCoords.map(s => [s.projectLatitude!, s.projectLongitude!] as [number, number])
  );
  map.fitBounds(bounds, { padding: [40, 40], maxZoom: 13 });

  // Ensure tiles load correctly after the container becomes fully visible
  setTimeout(() => map?.invalidateSize(), 100);

  // Draw route line
  const latlngs = stopsWithCoords.map(
    s => [s.projectLatitude!, s.projectLongitude!] as [number, number]
  );
  L.polyline(latlngs, { color: '#3b82f6', weight: 3, opacity: 0.6, dashArray: '8 6' }).addTo(map);
}

function renderMarkers(L: typeof import('leaflet'), stopsWithCoords: DeliveryStopDto[]) {
  if (!map) return;
  markers.forEach(m => m.remove());
  markers = [];

  stopsWithCoords.forEach((stop, idx) => {
    const isActive = stop.projectId === props.activeProjectId;
    const icon = makeIcon(L, String(idx + 1), stop.isFullyDelivered, isActive);
    const marker = L.marker(
      [stop.projectLatitude!, stop.projectLongitude!],
      { icon, zIndexOffset: isActive ? 1000 : 0 }
    )
      .addTo(map!)
      .bindPopup(
        `<div style="font-size:13px;line-height:1.5;">
          <strong>${idx + 1}. ${stop.projectName}</strong>
          ${stop.projectAddress ? `<br><span style="color:#6b7280;">${stop.projectAddress}</span>` : ''}
          ${stop.isFullyDelivered ? '<br><span style="color:#22c55e;">✓ Teslim edildi</span>' : ''}
        </div>`
      );
    if (isActive) marker.openPopup();
    markers.push(marker);
  });
}

onMounted(initMap);

onUnmounted(() => {
  map?.remove();
  map = null;
});

watch(() => [props.stops, props.activeProjectId] as const, async () => {
  if (!map) return;
  const L = await import('leaflet');
  const stopsWithCoords = props.stops.filter(
    s => s.projectLatitude != null && s.projectLongitude != null
  );
  renderMarkers(L, stopsWithCoords);
});
</script>
