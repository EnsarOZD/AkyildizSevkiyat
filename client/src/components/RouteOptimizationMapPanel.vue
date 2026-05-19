<template>
  <div class="relative w-full h-full min-h-[400px]">
    <div ref="mapEl" class="absolute inset-0" />

    <!-- No coords notice -->
    <div
      v-if="stopsWithCoords.length === 0 && !loading"
      class="absolute inset-0 flex items-center justify-center bg-gray-50 dark:bg-gray-800 rounded-xl"
    >
      <div class="text-center">
        <svg class="w-12 h-12 text-gray-300 dark:text-gray-600 mx-auto mb-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 20l-5.447-2.724A1 1 0 013 16.382V5.618a1 1 0 011.447-.894L9 7m0 13l6-3m-6 3V7m6 10l4.553 2.276A1 1 0 0021 18.382V7.618a1 1 0 00-.553-.894L15 4m0 13V4m0 0L9 7" />
        </svg>
        <p class="text-sm text-gray-400 dark:text-gray-500">Koordinat bilgisi olmayan duraklar haritada gösterilemiyor.</p>
        <p class="text-xs text-gray-400 dark:text-gray-500 mt-1">Proje sayfasından koordinat ekleyin veya geocode edin.</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from 'vue';
import type { Map as LMap, Marker, Polyline } from 'leaflet';
import type { RouteStopDto } from '../services/routeOptimizationService';

const props = defineProps<{
  stops: RouteStopDto[];
  startLatitude?: number | null;
  startLongitude?: number | null;
}>();

const mapEl = ref<HTMLElement | null>(null);
const loading = ref(true);

let map: LMap | null = null;
let markers: Marker[] = [];
let routeLine: Polyline | null = null;
let startMarker: Marker | null = null;

const stopsWithCoords = computed(() =>
  props.stops.filter(s => s.latitude != null && s.longitude != null)
);

function makeStopIcon(L: typeof import('leaflet'), label: string): import('leaflet').DivIcon {
  return L.divIcon({
    className: '',
    html: `<div style="
      background:#4f46e5;
      width:30px;height:30px;border-radius:50%;
      display:flex;align-items:center;justify-content:center;
      color:white;font-size:11px;font-weight:bold;
      box-shadow:0 2px 6px rgba(0,0,0,0.3);
      border:2px solid white;
    ">${label}</div>`,
    iconSize: [30, 30],
    iconAnchor: [15, 15],
    popupAnchor: [0, -18],
  });
}

function makeStartIcon(L: typeof import('leaflet')): import('leaflet').DivIcon {
  return L.divIcon({
    className: '',
    html: `<div style="
      background:#16a34a;
      width:30px;height:30px;border-radius:50%;
      display:flex;align-items:center;justify-content:center;
      color:white;font-size:11px;font-weight:bold;
      box-shadow:0 2px 6px rgba(0,0,0,0.3);
      border:2px solid white;
    ">S</div>`,
    iconSize: [30, 30],
    iconAnchor: [15, 15],
    popupAnchor: [0, -18],
  });
}

async function initMap() {
  if (!mapEl.value) return;

  const L = await import('leaflet');
  await import('leaflet/dist/leaflet.css');

  delete (L.Icon.Default.prototype as any)._getIconUrl;
  L.Icon.Default.mergeOptions({
    iconRetinaUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png',
    iconUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png',
    shadowUrl: 'https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png',
  });

  const allPoints: [number, number][] = [];

  if (props.startLatitude != null && props.startLongitude != null)
    allPoints.push([props.startLatitude, props.startLongitude]);

  stopsWithCoords.value.forEach(s =>
    allPoints.push([s.latitude!, s.longitude!])
  );

  const center: [number, number] = allPoints.length > 0
    ? [
        allPoints.reduce((acc, p) => acc + p[0], 0) / allPoints.length,
        allPoints.reduce((acc, p) => acc + p[1], 0) / allPoints.length,
      ]
    : [41.01, 28.97];

  map = L.map(mapEl.value, { zoomControl: true }).setView(center, 9);
  L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
    attribution: '© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>',
    maxZoom: 19,
  }).addTo(map);

  if (allPoints.length > 1) {
    map.fitBounds(L.latLngBounds(allPoints), { padding: [50, 50], maxZoom: 12 });
  }

  setTimeout(() => map?.invalidateSize(), 150);

  renderAll(L);
  loading.value = false;
}

function renderAll(L: typeof import('leaflet')) {
  if (!map) return;

  // Clear existing
  markers.forEach(m => m.remove());
  markers = [];
  routeLine?.remove();
  routeLine = null;
  startMarker?.remove();
  startMarker = null;

  const linePoints: [number, number][] = [];

  // Start marker
  if (props.startLatitude != null && props.startLongitude != null) {
    const icon = makeStartIcon(L);
    startMarker = L.marker([props.startLatitude, props.startLongitude], { icon, zIndexOffset: 2000 })
      .addTo(map!)
      .bindPopup('<div style="font-size:13px;font-weight:bold;">Başlangıç</div>');
    linePoints.push([props.startLatitude, props.startLongitude]);
  }

  // Stop markers
  stopsWithCoords.value.forEach(stop => {
    const icon = makeStopIcon(L, String(stop.order));
    const marker = L.marker([stop.latitude!, stop.longitude!], { icon })
      .addTo(map!)
      .bindPopup(
        `<div style="font-size:13px;line-height:1.5;min-width:160px;">
          <div style="font-weight:bold;margin-bottom:4px;">${stop.order}. ${stop.projectName}</div>
          <div style="color:#6b7280;font-size:11px;">${stop.address ?? ''}</div>
          ${stop.estimatedDistanceFromPrevious != null
            ? `<div style="color:#4f46e5;font-size:11px;margin-top:4px;">+${stop.estimatedDistanceFromPrevious.toFixed(1)} km</div>`
            : ''}
        </div>`
      );
    markers.push(marker);
    linePoints.push([stop.latitude!, stop.longitude!]);
  });

  // Route polyline
  if (linePoints.length >= 2) {
    routeLine = L.polyline(linePoints, {
      color: '#4f46e5',
      weight: 3,
      opacity: 0.7,
      dashArray: '10 6',
    }).addTo(map!);
  }
}

onMounted(initMap);

onUnmounted(() => {
  map?.remove();
  map = null;
});

watch(
  () => [props.stops, props.startLatitude, props.startLongitude] as const,
  async () => {
    if (!map) return;
    const L = await import('leaflet');
    renderAll(L);

    // Re-fit bounds
    const allPoints: [number, number][] = [];
    if (props.startLatitude != null && props.startLongitude != null)
      allPoints.push([props.startLatitude, props.startLongitude]);
    stopsWithCoords.value.forEach(s =>
      allPoints.push([s.latitude!, s.longitude!])
    );
    if (allPoints.length > 1)
      map.fitBounds(L.latLngBounds(allPoints), { padding: [50, 50], maxZoom: 12 });
  },
  { deep: true }
);
</script>
