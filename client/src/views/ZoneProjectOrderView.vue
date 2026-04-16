<template>
  <div class="space-y-4">

    <!-- Error -->
    <div v-if="error" class="mx-4 mt-4 p-3 bg-red-900/30 border border-red-700 rounded-lg flex items-center justify-between">
      <span class="text-red-400 text-sm">{{ error }}</span>
      <button @click="loadData(); error = null" class="text-red-400 hover:text-red-300 text-sm underline ml-4">Tekrar dene</button>
    </div>

    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-xl font-bold text-gray-900 dark:text-gray-100">Teslimat Sırası</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">Bölge seçerek projeleri sürükle-bırak ile sıralayın</p>
      </div>
    </div>

    <!-- Zone selector tabs -->
    <div class="flex flex-wrap gap-2">
      <button
        v-for="zone in zones"
        :key="zone.id"
        @click="selectZone(zone)"
        class="px-4 py-2 rounded-lg text-sm font-medium border transition-colors"
        :class="selectedZone?.id === zone.id
          ? 'bg-blue-600 text-white border-blue-600'
          : 'bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 border-gray-300 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800'"
      >
        {{ zone.name }}
        <span class="ml-1.5 text-xs opacity-70">
          {{ projectsByZone[zone.id]?.length ?? 0 }}
        </span>
      </button>
      <button
        @click="selectZone(null)"
        class="px-4 py-2 rounded-lg text-sm font-medium border transition-colors"
        :class="selectedZone === null && showUnassigned
          ? 'bg-orange-500 text-white border-orange-500'
          : 'bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 border-gray-300 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800'"
      >
        Bölgesiz
        <span class="ml-1.5 text-xs opacity-70">{{ unassignedProjects.length }}</span>
      </button>
    </div>

    <!-- No zone selected -->
    <div v-if="!selectedZone && !showUnassigned" class="text-center py-16 text-gray-400 dark:text-gray-600">
      <MapIcon class="w-12 h-12 mx-auto mb-3 opacity-40" aria-hidden="true" />
      <p class="text-base font-medium">Bölge seçin</p>
      <p class="text-sm mt-1">Sıralamak istediğiniz bölgeyi yukarıdan seçin</p>
    </div>

    <!-- Project list -->
    <div v-else-if="orderedProjects.length === 0" class="text-center py-16 text-gray-400 dark:text-gray-600">
      <p class="font-medium">Bu bölgede proje yok</p>
    </div>

    <template v-else>
      <!-- Instruction + dirty indicator -->
      <div class="flex items-center justify-between">
        <p class="text-xs text-gray-400 dark:text-gray-600 flex items-center gap-1.5">
          <Bars3Icon class="w-3.5 h-3.5" aria-hidden="true" />
          Sıralamak için satırları sürükleyin
        </p>
        <span v-if="isDirty" class="text-xs font-medium text-orange-600 dark:text-orange-400 flex items-center gap-1">
          <span class="w-1.5 h-1.5 rounded-full bg-orange-500 inline-block"></span>
          Kaydedilmemiş değişiklik
        </span>
      </div>

      <!-- Drag list -->
      <div class="space-y-1.5" @dragover.prevent @drop.prevent>
        <div
          v-for="(proj, idx) in orderedProjects"
          :key="proj.id"
          draggable="true"
          @dragstart="onDragStart(idx, $event)"
          @dragover.prevent="onDragOver(idx)"
          @dragleave="onDragLeave"
          @drop.prevent="onDrop(idx)"
          @dragend="onDragEnd"
          class="flex items-center gap-3 bg-white dark:bg-gray-900 rounded-xl border px-3 py-3 select-none transition-all duration-150 cursor-grab active:cursor-grabbing"
          :class="[
            dragOverIdx === idx && draggedIdx !== idx
              ? 'border-blue-400 dark:border-blue-500 bg-blue-50 dark:bg-blue-900/20 shadow-md scale-[1.01]'
              : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600',
            draggedIdx === idx ? 'opacity-40' : 'opacity-100'
          ]"
        >
          <!-- Drag handle -->
          <div class="text-gray-300 dark:text-gray-600 flex-shrink-0">
            <Bars3Icon class="w-5 h-5" aria-hidden="true" />
          </div>

          <!-- Order badge -->
          <div
            class="flex-shrink-0 w-7 h-7 rounded-full flex items-center justify-center text-xs font-bold"
            :class="proj.deliveryOrder != null
              ? 'bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300'
              : 'bg-gray-100 dark:bg-gray-800 text-gray-400'"
          >
            {{ idx + 1 }}
          </div>

          <!-- Project info -->
          <div class="flex-1 min-w-0">
            <p class="text-sm font-semibold text-gray-900 dark:text-gray-100 truncate">{{ proj.name }}</p>
            <p class="text-xs text-gray-400 dark:text-gray-500 truncate">{{ proj.code }}</p>
          </div>

          <!-- Location indicator -->
          <div v-if="proj.latitude != null" class="flex-shrink-0" title="GPS koordinatı kaydedilmiş">
            <MapPinIcon class="w-4 h-4 text-green-500" aria-hidden="true" />
          </div>
          <div v-else class="flex-shrink-0" title="Adres yok">
            <MapPinIcon class="w-4 h-4 text-gray-300 dark:text-gray-700" aria-hidden="true" />
          </div>
        </div>
      </div>

      <!-- Save bar -->
      <div class="sticky bottom-4 pt-2">
        <BaseButton
          @click="saveOrder"
          :disabled="!isDirty"
          :loading="saving"
          variant="primary"
          class="w-full py-3.5"
        >
          <template #icon><CheckIcon class="w-4 h-4 mr-1" aria-hidden="true" /></template>
          {{ isDirty ? 'Sırayı Kaydet' : 'Kaydedildi ✓' }}
        </BaseButton>
      </div>
    </template>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { MapIcon, MapPinIcon, Bars3Icon, CheckIcon } from '@heroicons/vue/24/outline';
import projectService, { type Zone } from '../services/projectService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import BaseButton from '../components/BaseButton.vue';

interface ProjectItem {
  id: number;
  code: string;
  name: string;
  zoneId: number | null;
  deliveryOrder: number | null;
  latitude?: number | null;
  longitude?: number | null;
}

const notify = useNotificationStore();

const error = ref<string | null>(null);
const zones = ref<Zone[]>([]);
const allProjects = ref<ProjectItem[]>([]);
const selectedZone = ref<Zone | null | undefined>(undefined); // undefined = nothing selected
const showUnassigned = ref(false);
const saving = ref(false);

// Current zone's projects in user-edited order
const orderedProjects = ref<ProjectItem[]>([]);
// Snapshot of original order to detect dirty
const savedOrder = ref<number[]>([]);

const isDirty = computed(() =>
  orderedProjects.value.map(p => p.id).join(',') !== savedOrder.value.join(',')
);

const projectsByZone = computed(() => {
  const map: Record<number, ProjectItem[]> = {};
  for (const p of allProjects.value) {
    if (p.zoneId != null) {
      const zid = p.zoneId;
      if (!map[zid]) map[zid] = [];
      map[zid].push(p);
    }
  }
  return map;
});

const unassignedProjects = computed(() =>
  allProjects.value.filter(p => p.zoneId == null)
);

function selectZone(zone: Zone | null) {
  if (isDirty.value) {
    // Warn before switching (simple confirm)
    if (!confirm('Kaydedilmemiş değişiklikler var. Yine de geçmek istiyor musunuz?')) return;
  }
  selectedZone.value = zone;
  showUnassigned.value = zone === null;
  loadZoneProjects();
}

function loadZoneProjects() {
  let list: ProjectItem[];
  if (showUnassigned.value) {
    list = [...unassignedProjects.value];
  } else if (selectedZone.value) {
    list = [...(projectsByZone.value[selectedZone.value.id] ?? [])];
  } else {
    orderedProjects.value = [];
    savedOrder.value = [];
    return;
  }
  // Sort by existing deliveryOrder (null = end), then alphabetically
  list.sort((a, b) => {
    if (a.deliveryOrder != null && b.deliveryOrder != null) return a.deliveryOrder - b.deliveryOrder;
    if (a.deliveryOrder != null) return -1;
    if (b.deliveryOrder != null) return 1;
    return a.name.localeCompare(b.name);
  });
  orderedProjects.value = list;
  savedOrder.value = list.map(p => p.id);
}

// ── Drag-and-drop state ──────────────────────────────────────────────────────
const draggedIdx  = ref<number | null>(null);
const dragOverIdx = ref<number | null>(null);

function onDragStart(idx: number, event: DragEvent) {
  draggedIdx.value = idx;
  if (event.dataTransfer) {
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', String(idx));
  }
}

function onDragOver(idx: number) {
  dragOverIdx.value = idx;
}

function onDragLeave() {
  // only clear if not immediately replaced
}

function onDrop(toIdx: number) {
  const fromIdx = draggedIdx.value;
  if (fromIdx === null || fromIdx === toIdx) return;

  const list = [...orderedProjects.value];
  const removed = list.splice(fromIdx, 1);
  if (!removed[0]) return;
  list.splice(toIdx, 0, removed[0]);
  orderedProjects.value = list;

  draggedIdx.value = null;
  dragOverIdx.value = null;
}

function onDragEnd() {
  draggedIdx.value = null;
  dragOverIdx.value = null;
}

// ── Save ─────────────────────────────────────────────────────────────────────
async function saveOrder() {
  if (!isDirty.value || saving.value) return;
  saving.value = true;
  try {
    const orders = orderedProjects.value.map((p, idx) => ({
      projectId: p.id,
      deliveryOrder: idx + 1,
    }));
    await projectService.bulkUpdateDeliveryOrders(orders);

    // Update local data so dirty check resets
    orderedProjects.value.forEach((p, idx) => { p.deliveryOrder = idx + 1; });
    savedOrder.value = orderedProjects.value.map(p => p.id);

    // Sync back to allProjects
    for (const p of orderedProjects.value) {
      const ap = allProjects.value.find(x => x.id === p.id);
      if (ap) ap.deliveryOrder = p.deliveryOrder;
    }

    notify.add('Sıralama kaydedildi.', 'success');
  } catch (e: any) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Kaydedilemedi.', 'error');
  } finally {
    saving.value = false;
  }
}

// ── Load ─────────────────────────────────────────────────────────────────────
async function loadData() {
  error.value = null;
  try {
    const [zoneList, projectList] = await Promise.all([
      projectService.getZones(),
      projectService.getProjects(),
    ]);
    zones.value = zoneList.sort((a, b) => (a.order ?? 0) - (b.order ?? 0));
    allProjects.value = projectList.map((p: any) => ({
      id: p.id,
      code: p.code,
      name: p.name,
      zoneId: p.zoneId ?? null,
      deliveryOrder: p.deliveryOrder ?? null,
      latitude: p.latitude ?? null,
      longitude: p.longitude ?? null,
    }));
  } catch (e: any) {
    error.value = ApiErrorUtils.getErrorMessage(e) || 'Veriler yüklenemedi.';
    notify.add(error.value, 'error');
  }
}

onMounted(loadData);
</script>
