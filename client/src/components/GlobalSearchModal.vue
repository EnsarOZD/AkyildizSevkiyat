<template>
  <Teleport to="body">
    <Transition name="search-overlay">
      <div
        v-if="show"
        class="fixed inset-0 z-[200] flex items-start justify-center pt-[10vh] px-4"
        @click.self="close"
        role="dialog"
        aria-modal="true"
        aria-label="Global arama"
      >
        <!-- Backdrop -->
        <div class="absolute inset-0 bg-black/50 backdrop-blur-sm" @click="close" />

        <!-- Dialog -->
        <div
          class="relative w-full max-w-2xl bg-white dark:bg-gray-900 rounded-2xl shadow-2xl overflow-hidden border border-gray-200 dark:border-gray-700"
          @keydown.esc="close"
          @keydown.up.prevent="moveSelection(-1)"
          @keydown.down.prevent="moveSelection(1)"
          @keydown.enter.prevent="confirmSelection"
        >
          <!-- Input -->
          <div class="flex items-center gap-3 px-4 py-3 border-b border-gray-200 dark:border-gray-700">
            <MagnifyingGlassIcon class="w-5 h-5 text-gray-400 flex-shrink-0" aria-hidden="true" />
            <input
              ref="inputRef"
              v-model="query"
              type="text"
              placeholder="Sevkiyat, stok, proje ara..."
              class="flex-1 bg-transparent text-gray-900 dark:text-gray-100 placeholder-gray-400 outline-none text-base"
              aria-label="Arama sorgusu"
              autocomplete="off"
              spellcheck="false"
            />
            <button
              v-if="query"
              @click="query = ''"
              aria-label="Aramayı temizle"
              class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
            >
              <XMarkIcon class="w-4 h-4" aria-hidden="true" />
            </button>
            <kbd class="hidden sm:inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-medium text-gray-400 border border-gray-200 dark:border-gray-700">Esc</kbd>
          </div>

          <!-- Results -->
          <div class="max-h-[60vh] overflow-y-auto" role="listbox">

            <!-- Loading -->
            <div v-if="loading" class="flex justify-center py-8">
              <div class="w-5 h-5 border-2 border-indigo-500 border-t-transparent rounded-full animate-spin"></div>
            </div>

            <!-- Empty query hint -->
            <div v-else-if="!query || query.length < 2" class="py-10 text-center text-sm text-gray-400">
              <MagnifyingGlassIcon class="w-8 h-8 mx-auto mb-2 opacity-30" aria-hidden="true" />
              En az 2 karakter girin
            </div>

            <!-- No results -->
            <div v-else-if="isEmpty" class="py-10 text-center text-sm text-gray-400">
              <span class="font-medium text-gray-600 dark:text-gray-300">"{{ query }}"</span> için sonuç bulunamadı.
            </div>

            <!-- Results grouped -->
            <template v-else>
              <!-- Shipments -->
              <div v-if="results.shipments.length > 0">
                <div class="px-4 py-2 text-[10px] font-bold text-gray-400 uppercase tracking-widest bg-gray-50 dark:bg-gray-800/50">
                  Sevkiyatlar
                </div>
                <button
                  v-for="(s, i) in results.shipments"
                  :key="'s-' + s.id"
                  role="option"
                  :aria-selected="flatIndex('s', i) === selectedIdx"
                  class="w-full flex items-center gap-3 px-4 py-3 hover:bg-indigo-50 dark:hover:bg-indigo-900/20 text-left transition-colors"
                  :class="flatIndex('s', i) === selectedIdx ? 'bg-indigo-50 dark:bg-indigo-900/20' : ''"
                  @click="goShipment(s.id)"
                  @mouseenter="selectedIdx = flatIndex('s', i)"
                >
                  <div class="w-8 h-8 rounded-lg bg-indigo-100 dark:bg-indigo-900/40 flex items-center justify-center flex-shrink-0">
                    <ClipboardDocumentListIcon class="w-4 h-4 text-indigo-600 dark:text-indigo-400" aria-hidden="true" />
                  </div>
                  <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
                      #{{ s.id }} — {{ s.projectName }}
                    </p>
                    <p class="text-xs text-gray-400 truncate">
                      {{ s.talepNo || s.projectCode }} · {{ fmtDate(s.deliveryDate) }}
                    </p>
                  </div>
                  <span class="text-[10px] px-2 py-0.5 rounded-full font-medium flex-shrink-0"
                    :class="statusClass(s.status)">
                    {{ statusLabel(s.status) }}
                  </span>
                </button>
              </div>

              <!-- Stocks -->
              <div v-if="results.stocks.length > 0">
                <div class="px-4 py-2 text-[10px] font-bold text-gray-400 uppercase tracking-widest bg-gray-50 dark:bg-gray-800/50">
                  Stoklar
                </div>
                <button
                  v-for="(s, i) in results.stocks"
                  :key="'st-' + s.id"
                  role="option"
                  :aria-selected="flatIndex('st', i) === selectedIdx"
                  class="w-full flex items-center gap-3 px-4 py-3 hover:bg-indigo-50 dark:hover:bg-indigo-900/20 text-left transition-colors"
                  :class="flatIndex('st', i) === selectedIdx ? 'bg-indigo-50 dark:bg-indigo-900/20' : ''"
                  @click="goStock()"
                  @mouseenter="selectedIdx = flatIndex('st', i)"
                >
                  <div class="w-8 h-8 rounded-lg bg-emerald-100 dark:bg-emerald-900/40 flex items-center justify-center flex-shrink-0">
                    <ArchiveBoxIcon class="w-4 h-4 text-emerald-600 dark:text-emerald-400" aria-hidden="true" />
                  </div>
                  <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">{{ s.stockName }}</p>
                    <p class="text-xs text-gray-400 truncate">{{ s.stockCode }}</p>
                  </div>
                  <span class="text-xs font-semibold text-gray-600 dark:text-gray-400 flex-shrink-0">
                    {{ s.availableQty }}
                  </span>
                </button>
              </div>

              <!-- Projects -->
              <div v-if="results.projects.length > 0">
                <div class="px-4 py-2 text-[10px] font-bold text-gray-400 uppercase tracking-widest bg-gray-50 dark:bg-gray-800/50">
                  Projeler
                </div>
                <button
                  v-for="(p, i) in results.projects"
                  :key="'p-' + p.id"
                  role="option"
                  :aria-selected="flatIndex('p', i) === selectedIdx"
                  class="w-full flex items-center gap-3 px-4 py-3 hover:bg-indigo-50 dark:hover:bg-indigo-900/20 text-left transition-colors"
                  :class="flatIndex('p', i) === selectedIdx ? 'bg-indigo-50 dark:bg-indigo-900/20' : ''"
                  @click="goProjects()"
                  @mouseenter="selectedIdx = flatIndex('p', i)"
                >
                  <div class="w-8 h-8 rounded-lg bg-blue-100 dark:bg-blue-900/40 flex items-center justify-center flex-shrink-0">
                    <BuildingOffice2Icon class="w-4 h-4 text-blue-600 dark:text-blue-400" aria-hidden="true" />
                  </div>
                  <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">{{ p.name }}</p>
                    <p class="text-xs text-gray-400 truncate">{{ p.code }}{{ p.region ? ` · ${p.region}` : '' }}</p>
                  </div>
                </button>
              </div>
            </template>
          </div>

          <!-- Footer hint -->
          <div class="px-4 py-2 border-t border-gray-100 dark:border-gray-800 flex items-center gap-4 text-[11px] text-gray-400">
            <span><kbd class="px-1 border border-gray-200 dark:border-gray-700 rounded text-[10px]">↑↓</kbd> gezin</span>
            <span><kbd class="px-1 border border-gray-200 dark:border-gray-700 rounded text-[10px]">Enter</kbd> aç</span>
            <span><kbd class="px-1 border border-gray-200 dark:border-gray-700 rounded text-[10px]">Esc</kbd> kapat</span>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, watch, computed, nextTick } from 'vue';
import { useRouter } from 'vue-router';
import {
  MagnifyingGlassIcon,
  XMarkIcon,
  ClipboardDocumentListIcon,
  ArchiveBoxIcon,
  BuildingOffice2Icon,
} from '@heroicons/vue/24/outline';
import searchService from '../services/searchService';
import type { GlobalSearchResult } from '../services/searchService';

const props = defineProps<{ show: boolean }>();
const emit = defineEmits<{ (e: 'close'): void }>();

const router = useRouter();
const inputRef = ref<HTMLInputElement | null>(null);
const query = ref('');
const loading = ref(false);
const results = ref<GlobalSearchResult>({ shipments: [], stocks: [], projects: [] });
const selectedIdx = ref(0);

let debounceTimer: ReturnType<typeof setTimeout> | null = null;

const isEmpty = computed(() =>
  results.value.shipments.length === 0 &&
  results.value.stocks.length === 0 &&
  results.value.projects.length === 0,
);

// Flat index helpers for keyboard navigation
const shipCount  = computed(() => results.value.shipments.length);
const stockCount = computed(() => results.value.stocks.length);
const totalCount = computed(() => shipCount.value + stockCount.value + results.value.projects.length);

function flatIndex(group: 's' | 'st' | 'p', i: number): number {
  if (group === 's')  return i;
  if (group === 'st') return shipCount.value + i;
  return shipCount.value + stockCount.value + i;
}

function moveSelection(dir: 1 | -1) {
  if (totalCount.value === 0) return;
  selectedIdx.value = (selectedIdx.value + dir + totalCount.value) % totalCount.value;
}

function confirmSelection() {
  const idx = selectedIdx.value;
  if (idx < shipCount.value) {
    const s = results.value.shipments[idx];
    if (s) goShipment(s.id);
  } else if (idx < shipCount.value + stockCount.value) {
    goStock();
  } else {
    goProjects();
  }
}

function goShipment(id: number) {
  close();
  router.push(`/shipments/${id}`);
}

function goStock() {
  close();
  router.push('/stocks');
}

function goProjects() {
  close();
  router.push('/projects/zone-mapping');
}

function close() {
  emit('close');
}

// Debounced search
watch(query, (val) => {
  selectedIdx.value = 0;
  if (debounceTimer) clearTimeout(debounceTimer);
  if (!val || val.length < 2) {
    results.value = { shipments: [], stocks: [], projects: [] };
    loading.value = false;
    return;
  }
  loading.value = true;
  debounceTimer = setTimeout(async () => {
    try {
      results.value = await searchService.search(val);
    } catch {
      // fail silently
    } finally {
      loading.value = false;
    }
  }, 300);
});

// Focus input and clear when modal opens
watch(() => props.show, (val) => {
  if (val) {
    query.value = '';
    results.value = { shipments: [], stocks: [], projects: [] };
    selectedIdx.value = 0;
    nextTick(() => inputRef.value?.focus());
  }
});

// ── Helpers ──────────────────────────────────────────────────────────────────
const fmtDate = (d: string) => new Date(d).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' });

const statusLabelMap: Record<string, string> = {
  Created: 'Taslak', AssignedToWarehouse: 'Depoda', Picking: 'Toplama',
  ReadyForDispatch: 'Hazır', AssignedToVehicle: 'Araçta', Dispatched: 'Yolda',
  Delivered: 'Teslim', Cancelled: 'İptal', Passive: 'Pasif',
};
const statusClassMap: Record<string, string> = {
  Created: 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400',
  AssignedToWarehouse: 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400',
  Picking: 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
  ReadyForDispatch: 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400',
  AssignedToVehicle: 'bg-indigo-100 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-400',
  Dispatched: 'bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-400',
  Delivered: 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400',
  Cancelled: 'bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400',
  Passive: 'bg-gray-100 dark:bg-gray-800 text-gray-400 dark:text-gray-500',
};
const statusLabel = (s: string) => statusLabelMap[s] ?? s;
const statusClass = (s: string) => statusClassMap[s] ?? 'bg-gray-100 text-gray-600';
</script>

<style scoped>
.search-overlay-enter-active,
.search-overlay-leave-active {
  transition: opacity 0.15s ease;
  pointer-events: none;
}
.search-overlay-enter-from,
.search-overlay-leave-to {
  opacity: 0;
}
.search-overlay-enter-active > div:last-child,
.search-overlay-leave-active > div:last-child {
  transition: opacity 0.15s ease, transform 0.15s ease;
}
.search-overlay-enter-from > div:last-child,
.search-overlay-leave-to > div:last-child {
  opacity: 0;
  transform: scale(0.97) translateY(-8px);
}
</style>
