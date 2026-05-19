<template>
  <div class="space-y-4">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Depo Haritası</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">Koridor ve modül bazlı 2D görünüm</p>
      </div>
      <div class="flex items-center gap-3">
        <button @click="load" :disabled="loading"
          class="flex items-center gap-2 px-3 py-1.5 text-sm border border-gray-300 dark:border-white/20 rounded-lg hover:bg-gray-50 dark:hover:bg-white/5 text-gray-700 dark:text-gray-300 disabled:opacity-50 transition-colors">
          <svg class="w-4 h-4" :class="{ 'animate-spin': loading }" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
          </svg>
          Yenile
        </button>
      </div>
    </div>

    <!-- Renk Lejantı -->
    <div class="flex flex-wrap gap-3 text-xs">
      <div v-for="t in typeColors" :key="t.id" class="flex items-center gap-1.5">
        <div class="w-4 h-4 rounded" :class="t.bg"></div>
        <span class="text-gray-600 dark:text-gray-400">{{ t.label }}</span>
      </div>
      <div class="flex items-center gap-1.5">
        <div class="w-4 h-4 rounded bg-gray-200 dark:bg-gray-700 opacity-50"></div>
        <span class="text-gray-600 dark:text-gray-400">Pasif</span>
      </div>
    </div>

    <!-- Error -->
    <div v-if="error" class="p-3 bg-red-900/30 border border-red-700 rounded-lg text-red-400 text-sm">{{ error }}</div>

    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-20">
      <div class="w-8 h-8 border-4 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- Map -->
    <template v-else>
      <div v-for="corridorGroup in corridors" :key="`${corridorGroup.koridorNo}`"
        class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 overflow-hidden">
        <div class="px-4 py-2 bg-gray-50 dark:bg-white/5 border-b border-gray-200 dark:border-white/10">
          <h2 class="font-semibold text-gray-700 dark:text-gray-300 text-sm">{{ corridorGroup.koridorNo }}. Koridor</h2>
        </div>

        <div class="overflow-x-auto">
          <div class="p-3 space-y-2 min-w-max">
            <div v-for="side in corridorGroup.sides" :key="side.taraf">
              <div class="flex items-center gap-2">
                <!-- Side label -->
                <div class="w-12 flex-shrink-0 text-xs font-semibold text-gray-500 dark:text-gray-400 text-right">
                  {{ side.taraf === 'K' ? 'Kuzey' : 'Güney' }}
                </div>
                <!-- Module cells -->
                <div class="flex gap-0.5">
                  <template v-for="modul in maxModulNo" :key="modul">
                    <div v-if="getCell(side.modules, modul)"
                      class="w-7 h-7 rounded-sm cursor-pointer flex items-center justify-center text-[9px] font-bold transition-all hover:scale-110 hover:z-10 relative"
                      :class="[
                        getCellColor(getCell(side.modules, modul)!),
                        !getCell(side.modules, modul)!.allActive ? 'opacity-40' : '',
                        !getCell(side.modules, modul)!.hasActive ? 'opacity-20' : '',
                      ]"
                      :title="`${corridorGroup.koridorNo}${side.taraf}-${String(modul).padStart(3,'0')} · ${typeLabelById(getCell(side.modules, modul)!.dominantTypeId)} · ${getCell(side.modules, modul)!.allActive ? 'Aktif' : getCell(side.modules, modul)!.hasActive ? 'Kısmen aktif' : 'Pasif'} · ${getCell(side.modules, modul)!.totalLocations} konum`"
                      @click="selectCell(corridorGroup.koridorNo, side.taraf, modul, getCell(side.modules, modul)!)"
                    >
                      {{ modul }}
                    </div>
                    <div v-else
                      class="w-7 h-7 rounded-sm bg-gray-100 dark:bg-white/5 opacity-30"
                      :title="`${corridorGroup.koridorNo}${side.taraf}-${String(modul).padStart(3,'0')} — yok`"
                    ></div>
                  </template>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-if="corridors.length === 0" class="text-center py-16 text-gray-400 dark:text-gray-500">
        Henüz hiç adres yok. Önce Depo Adres Yönetimi'nden adres oluşturun.
      </div>
    </template>

    <!-- Detail panel -->
    <transition name="slide-up">
      <div v-if="selectedCell"
        class="fixed bottom-0 left-0 right-0 bg-white dark:bg-[#0d1f3c] border-t border-gray-200 dark:border-white/10 shadow-2xl p-4 z-50">
        <div class="max-w-2xl mx-auto">
          <div class="flex items-start justify-between mb-3">
            <div>
              <p class="text-xs text-gray-400 uppercase tracking-wider">Seçili Modül</p>
              <h3 class="text-lg font-bold font-mono text-blue-700 dark:text-blue-300">
                {{ selectedCell.koridorNo }}{{ selectedCell.taraf }}-{{ String(selectedCell.modulNo).padStart(3,'0') }}
              </h3>
              <p class="text-xs text-gray-500 mt-0.5">
                {{ typeLabelById(selectedCell.cell.dominantTypeId) }} ·
                {{ selectedCell.cell.totalLocations }} konum ·
                {{ selectedCell.cell.allActive ? 'Tümü aktif' : selectedCell.cell.hasActive ? 'Kısmen aktif' : 'Tümü pasif' }}
              </p>
            </div>
            <button @click="selectedCell = null" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200">
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
              </svg>
            </button>
          </div>
          <div class="flex gap-2">
            <a :href="`/warehouse/locations?koridor=${selectedCell.koridorNo}&taraf=${selectedCell.taraf}&modul=${selectedCell.modulNo}`"
              class="flex-1 text-center py-2 text-sm font-medium rounded-lg bg-blue-600 text-white hover:bg-blue-700 transition-colors">
              Konumları Listele
            </a>
          </div>
        </div>
      </div>
    </transition>
  </div>
</template>

<style scoped>
.slide-up-enter-active, .slide-up-leave-active { transition: transform 0.25s ease; }
.slide-up-enter-from, .slide-up-leave-to { transform: translateY(100%); }
</style>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import warehouseLocationService, { type MapModuleDto } from '../services/warehouseLocationService';
import { ApiErrorUtils } from '../utils/apiError';

const loading = ref(false);
const error   = ref<string | null>(null);
const modules = ref<MapModuleDto[]>([]);

const typeColors = [
  { id: 0, label: 'Raf',              bg: 'bg-blue-500' },
  { id: 1, label: 'Zemin İstifleme', bg: 'bg-yellow-500' },
  { id: 2, label: 'Giriş',           bg: 'bg-emerald-500' },
  { id: 3, label: 'Çıkış',           bg: 'bg-red-500' },
  { id: 4, label: 'Karantina',       bg: 'bg-orange-500' },
  { id: 5, label: 'Hazırlama',       bg: 'bg-purple-500' },
  { id: 6, label: 'Toplama Gözü',   bg: 'bg-teal-500' },
];

function typeLabelById(id: number) {
  return typeColors.find(t => t.id === id)?.label ?? String(id);
}

function getCellColor(cell: MapModuleDto) {
  const t = typeColors.find(t => t.id === cell.dominantTypeId);
  return t?.bg ?? 'bg-gray-400';
}

const maxModulNo = computed(() => {
  if (modules.value.length === 0) return [];
  const max = Math.max(...modules.value.map(m => m.modulNo));
  return Array.from({ length: max }, (_, i) => i + 1);
});

interface CorridorSide {
  taraf: string;
  modules: MapModuleDto[];
}
interface CorridorGroup {
  koridorNo: number;
  sides: CorridorSide[];
}

const corridors = computed((): CorridorGroup[] => {
  const byKoridor = new Map<number, Map<string, MapModuleDto[]>>();
  for (const m of modules.value) {
    if (!byKoridor.has(m.koridorNo)) byKoridor.set(m.koridorNo, new Map());
    const tarafMap = byKoridor.get(m.koridorNo)!;
    if (!tarafMap.has(m.taraf)) tarafMap.set(m.taraf, []);
    tarafMap.get(m.taraf)!.push(m);
  }

  return [...byKoridor.entries()]
    .sort(([a], [b]) => a - b)
    .map(([koridorNo, tarafMap]) => ({
      koridorNo,
      sides: [...tarafMap.entries()]
        .sort(([a], [b]) => a.localeCompare(b))
        .map(([taraf, mods]) => ({ taraf, modules: mods })),
    }));
});

function getCell(mods: MapModuleDto[], modulNo: number): MapModuleDto | undefined {
  return mods.find(m => m.modulNo === modulNo);
}

// Selected cell panel
const selectedCell = ref<{ koridorNo: number; taraf: string; modulNo: number; cell: MapModuleDto } | null>(null);

function selectCell(koridorNo: number, taraf: string, modulNo: number, cell: MapModuleDto) {
  if (selectedCell.value?.koridorNo === koridorNo &&
      selectedCell.value?.taraf === taraf &&
      selectedCell.value?.modulNo === modulNo) {
    selectedCell.value = null;
  } else {
    selectedCell.value = { koridorNo, taraf, modulNo, cell };
  }
}

async function load() {
  loading.value = true;
  error.value   = null;
  try {
    modules.value = await warehouseLocationService.getMap();
  } catch (e) {
    error.value = ApiErrorUtils.getErrorMessage(e) || 'Harita yüklenemedi.';
  } finally {
    loading.value = false;
  }
}

onMounted(load);
</script>
