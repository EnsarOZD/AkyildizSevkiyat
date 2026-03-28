<template>
  <div class="p-4 sm:p-6 space-y-6 max-w-5xl mx-auto">

    <!-- Header -->
    <div>
      <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100">Rota Optimizasyonu</h1>
      <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">Proje adreslerini Google Maps ile optimize edin.</p>
    </div>

    <!-- Step indicator -->
    <div class="flex items-center gap-2 text-sm">
      <StepBadge :step="1" :active="step === 1" :done="step > 1" label="Proje Seç" />
      <div class="flex-1 h-px bg-gray-200 dark:bg-gray-700" />
      <StepBadge :step="2" :active="step === 2" :done="step > 2" label="ISS Karşılaştır" />
      <div class="flex-1 h-px bg-gray-200 dark:bg-gray-700" />
      <StepBadge :step="3" :active="step === 3" :done="false" label="Rota Sonucu" />
    </div>

    <!-- ────────────────────────────── STEP 1: Project Selection ────────────────────────────── -->
    <div v-if="step === 1" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 space-y-4">
        <!-- Start address -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Başlangıç Adresi <span class="text-gray-400 font-normal">(opsiyonel)</span>
          </label>
          <input
            v-model="startAddress"
            type="text"
            placeholder="Depo adresi veya başlangıç noktası..."
            class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>

        <!-- Project search + select all -->
        <div class="flex gap-3 items-center">
          <input
            v-model="projectSearch"
            type="text"
            placeholder="Proje ara..."
            class="flex-1 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
          <button
            @click="toggleSelectAll"
            class="text-sm text-indigo-600 dark:text-indigo-400 hover:underline whitespace-nowrap"
          >
            {{ allFilteredSelected ? 'Tümünü kaldır' : 'Tümünü seç' }}
          </button>
        </div>

        <!-- Loading projects -->
        <div v-if="loadingProjects" class="text-sm text-gray-400 py-4 text-center">Projeler yükleniyor...</div>

        <!-- Project list -->
        <div v-else class="border dark:border-gray-700 rounded-lg overflow-hidden max-h-96 overflow-y-auto">
          <div v-if="filteredProjects.length === 0" class="text-sm text-gray-400 py-6 text-center">Proje bulunamadı.</div>
          <label
            v-for="project in filteredProjects"
            :key="project.code"
            class="flex items-center gap-3 px-4 py-2.5 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 border-b last:border-b-0 dark:border-gray-700"
            :class="{ 'bg-indigo-50 dark:bg-indigo-900/20': selectedCodes.has(project.code) }"
          >
            <input
              type="checkbox"
              :value="project.code"
              :checked="selectedCodes.has(project.code)"
              @change="toggleProject(project.code)"
              class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
            />
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2">
                <span class="text-sm font-medium text-gray-800 dark:text-gray-100">{{ project.code }}</span>
                <span class="text-sm text-gray-500 dark:text-gray-400 truncate">{{ project.name }}</span>
              </div>
              <div v-if="project.address" class="text-xs text-gray-400 truncate mt-0.5">{{ project.address }}</div>
              <div v-else class="text-xs text-amber-500 mt-0.5">Adres tanımlı değil</div>
            </div>
          </label>
        </div>

        <div class="flex justify-between items-center pt-1">
          <span class="text-sm text-gray-500 dark:text-gray-400">
            {{ selectedCodes.size }} proje seçili
          </span>
          <button
            @click="goToStep2"
            :disabled="selectedCodes.size === 0 || comparingIss"
            class="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-medium rounded-lg transition-colors flex items-center gap-2"
          >
            <span v-if="comparingIss">
              <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
              </svg>
            </span>
            <span>{{ comparingIss ? 'Karşılaştırılıyor...' : 'ISS ile Karşılaştır' }}</span>
            <ChevronRightIcon v-if="!comparingIss" class="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>

    <!-- ────────────────────────────── STEP 2: ISS Comparison ────────────────────────────── -->
    <div v-if="step === 2" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow overflow-hidden">
        <div class="px-4 py-3 border-b dark:border-gray-700 flex items-center justify-between">
          <div>
            <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">ISS Karşılaştırma Sonuçları</h2>
            <p class="text-xs text-gray-400 mt-0.5">
              {{ comparisonResults.filter(r => r.hasDifference).length }} projede farklılık bulundu.
              İstediğiniz güncellemeleri onaylayın.
            </p>
          </div>
          <div class="flex gap-2">
            <button @click="step = 1" class="text-sm text-gray-500 hover:text-gray-700 dark:hover:text-gray-300">
              ← Geri
            </button>
          </div>
        </div>

        <!-- No differences banner -->
        <div v-if="comparisonResults.filter(r => r.hasDifference).length === 0" class="px-4 py-6 text-center">
          <CheckCircleIcon class="w-10 h-10 text-green-400 mx-auto mb-2" />
          <p class="text-sm font-medium text-gray-700 dark:text-gray-300">Tüm projeler ISS ile güncel.</p>
          <p class="text-xs text-gray-400 mt-1">Hiçbir güncelleme gerekmez.</p>
        </div>

        <!-- Differences table -->
        <div v-else class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Proje</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Alan</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Mevcut</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">ISS</th>
                <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Güncelle</th>
              </tr>
            </thead>
            <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
              <template v-for="result in comparisonResults.filter(r => r.hasDifference)" :key="result.projectCode">
                <tr v-if="result.nameChanged">
                  <td class="px-4 py-3 font-medium text-gray-800 dark:text-gray-100" :rowspan="result.nameChanged && result.addressChanged ? 2 : 1">
                    <div>{{ result.projectCode }}</div>
                    <div class="text-xs text-gray-400 font-normal">{{ result.projectName }}</div>
                  </td>
                  <td class="px-4 py-3 text-gray-600 dark:text-gray-400">Proje Adı</td>
                  <td class="px-4 py-3 text-gray-500 dark:text-gray-400 max-w-xs truncate">{{ result.currentName || '—' }}</td>
                  <td class="px-4 py-3 text-gray-800 dark:text-gray-100 max-w-xs truncate font-medium">{{ result.issName || '—' }}</td>
                  <td class="px-4 py-3 text-center">
                    <input
                      type="checkbox"
                      :checked="getSyncApproval(result.projectCode).approveNameUpdate"
                      @change="setSyncApproval(result.projectCode, 'name', ($event.target as HTMLInputElement).checked)"
                      class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                    />
                  </td>
                </tr>
                <tr v-if="result.addressChanged">
                  <td v-if="!result.nameChanged" class="px-4 py-3 font-medium text-gray-800 dark:text-gray-100">
                    <div>{{ result.projectCode }}</div>
                    <div class="text-xs text-gray-400 font-normal">{{ result.projectName }}</div>
                  </td>
                  <td class="px-4 py-3 text-gray-600 dark:text-gray-400">Adres</td>
                  <td class="px-4 py-3 text-gray-500 dark:text-gray-400 max-w-xs truncate">{{ result.currentAddress || '—' }}</td>
                  <td class="px-4 py-3 text-gray-800 dark:text-gray-100 max-w-xs truncate font-medium">{{ result.issAddress || '—' }}</td>
                  <td class="px-4 py-3 text-center">
                    <input
                      type="checkbox"
                      :checked="getSyncApproval(result.projectCode).approveAddressUpdate"
                      @change="setSyncApproval(result.projectCode, 'address', ($event.target as HTMLInputElement).checked)"
                      class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                    />
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>

        <div class="px-4 py-3 border-t dark:border-gray-700 flex justify-between items-center">
          <span class="text-xs text-gray-400">
            {{ pendingSyncCount }} güncelleme onaylandı
          </span>
          <div class="flex gap-3">
            <button
              v-if="pendingSyncCount > 0"
              @click="applySync"
              :disabled="syncing"
              class="px-4 py-2 bg-amber-600 hover:bg-amber-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition-colors flex items-center gap-2"
            >
              <svg v-if="syncing" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
              </svg>
              {{ syncing ? 'Güncelleniyor...' : 'Seçilileri Güncelle' }}
            </button>
            <button
              @click="runOptimization"
              :disabled="optimizing"
              class="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-medium rounded-lg transition-colors flex items-center gap-2"
            >
              <svg v-if="optimizing" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
              </svg>
              <span>{{ optimizing ? 'Optimize Ediliyor...' : 'Rotayı Optimize Et' }}</span>
              <ChevronRightIcon v-if="!optimizing" class="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ────────────────────────────── STEP 3: Route Result ────────────────────────────── -->
    <div v-if="step === 3 && optimizationResult" class="space-y-4">
      <!-- Summary cards -->
      <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-3 text-center">
          <div class="text-2xl font-bold text-indigo-600">{{ optimizationResult.optimizedStops.length }}</div>
          <div class="text-xs text-gray-500 mt-1">Durak</div>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-3 text-center">
          <div class="text-2xl font-bold text-indigo-600">{{ formatDistance(optimizationResult.totalDistance) }}</div>
          <div class="text-xs text-gray-500 mt-1">Toplam Mesafe</div>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-3 text-center">
          <div class="text-2xl font-bold text-indigo-600">{{ formatDuration(optimizationResult.totalDuration) }}</div>
          <div class="text-xs text-gray-500 mt-1">Tahmini Süre</div>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-3 text-center">
          <div class="text-2xl font-bold" :class="optimizationResult.excludedProjects.length > 0 ? 'text-amber-500' : 'text-green-500'">
            {{ optimizationResult.excludedProjects.length }}
          </div>
          <div class="text-xs text-gray-500 mt-1">Hariç Tutulan</div>
        </div>
      </div>

      <!-- Excluded projects warning -->
      <div
        v-if="optimizationResult.excludedProjects.length > 0"
        class="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 rounded-lg p-3 flex items-start gap-2"
      >
        <ExclamationTriangleIcon class="w-5 h-5 text-amber-500 flex-shrink-0 mt-0.5" />
        <div class="text-sm text-amber-700 dark:text-amber-300">
          <span class="font-medium">Adres tanımsız:</span>
          {{ optimizationResult.excludedProjects.join(', ') }}
        </div>
      </div>

      <!-- Route stops -->
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow overflow-hidden">
        <div class="px-4 py-3 border-b dark:border-gray-700 flex items-center justify-between">
          <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">Optimize Edilmiş Rota</h2>
          <button @click="step = 2" class="text-sm text-gray-500 hover:text-gray-700 dark:hover:text-gray-300">
            ← Geri
          </button>
        </div>
        <ol class="divide-y divide-gray-100 dark:divide-gray-800">
          <li
            v-for="stop in optimizationResult.optimizedStops"
            :key="stop.order"
            class="flex items-start gap-4 px-4 py-3"
          >
            <div class="flex-shrink-0 w-7 h-7 rounded-full bg-indigo-100 dark:bg-indigo-900/40 text-indigo-700 dark:text-indigo-300 flex items-center justify-center text-xs font-bold">
              {{ stop.order }}
            </div>
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2">
                <span class="text-sm font-medium text-gray-800 dark:text-gray-100">{{ stop.projectCode }}</span>
                <span class="text-sm text-gray-500 dark:text-gray-400 truncate">{{ stop.projectName }}</span>
              </div>
              <div class="text-xs text-gray-400 mt-0.5 truncate">{{ stop.address || 'Adres yok' }}</div>
            </div>
            <div v-if="stop.estimatedDistanceFromPrevious != null" class="flex-shrink-0 text-right">
              <div class="text-xs font-medium text-gray-600 dark:text-gray-400">{{ formatDistance(stop.estimatedDistanceFromPrevious) }}</div>
              <div class="text-xs text-gray-400">{{ formatDuration(stop.estimatedDurationFromPrevious ?? 0) }}</div>
            </div>
          </li>
        </ol>

        <div class="px-4 py-3 border-t dark:border-gray-700 flex justify-end gap-3">
          <button
            @click="copyToClipboard"
            class="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors flex items-center gap-2"
          >
            <ClipboardDocumentIcon class="w-4 h-4" />
            Kopyala
          </button>
          <button
            @click="resetWizard"
            class="px-4 py-2 text-sm font-medium bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg transition-colors"
          >
            Yeni Rota
          </button>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import {
  ChevronRightIcon,
  CheckCircleIcon,
  ExclamationTriangleIcon,
  ClipboardDocumentIcon,
} from '@heroicons/vue/24/outline';
import routeOptimizationService, {
  type ProjectSyncComparisonDto,
  type RouteOptimizationResultDto,
  type SyncApprovalRequestDto,
} from '../services/routeOptimizationService';
import projectService from '../services/projectService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

// ── Step badge inline component ──────────────────────────────────────────────
const StepBadge = {
  props: { step: Number, active: Boolean, done: Boolean, label: String },
  template: `
    <div class="flex items-center gap-1.5 flex-shrink-0">
      <div
        class="w-6 h-6 rounded-full text-xs font-bold flex items-center justify-center"
        :class="done
          ? 'bg-indigo-600 text-white'
          : active
            ? 'bg-indigo-600 text-white'
            : 'bg-gray-200 dark:bg-gray-700 text-gray-500 dark:text-gray-400'"
      >
        <svg v-if="done" class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="3">
          <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
        </svg>
        <span v-else>{{ step }}</span>
      </div>
      <span
        class="text-xs font-medium hidden sm:inline"
        :class="active ? 'text-indigo-600 dark:text-indigo-400' : 'text-gray-400 dark:text-gray-500'"
      >{{ label }}</span>
    </div>
  `,
};

// ── State ────────────────────────────────────────────────────────────────────
const notificationStore = useNotificationStore();

interface ProjectItem {
  code: string;
  name: string;
  address: string | null;
}

const step = ref<1 | 2 | 3>(1);
const startAddress = ref('');
const projectSearch = ref('');
const projects = ref<ProjectItem[]>([]);
const loadingProjects = ref(false);
const selectedCodes = ref<Set<string>>(new Set());

const comparisonResults = ref<ProjectSyncComparisonDto[]>([]);
const syncApprovals = ref<Map<string, { approveNameUpdate: boolean; approveAddressUpdate: boolean }>>(new Map());
const comparingIss = ref(false);
const syncing = ref(false);

const optimizationResult = ref<RouteOptimizationResultDto | null>(null);
const optimizing = ref(false);

// ── Computed ─────────────────────────────────────────────────────────────────
const filteredProjects = computed(() => {
  if (!projectSearch.value) return projects.value;
  const term = projectSearch.value.toLowerCase();
  return projects.value.filter(
    p => p.code.toLowerCase().includes(term) || p.name.toLowerCase().includes(term)
  );
});

const allFilteredSelected = computed(
  () => filteredProjects.value.length > 0 && filteredProjects.value.every(p => selectedCodes.value.has(p.code))
);

const pendingSyncCount = computed(() => {
  let count = 0;
  for (const v of syncApprovals.value.values()) {
    if (v.approveNameUpdate) count++;
    if (v.approveAddressUpdate) count++;
  }
  return count;
});

// ── Project helpers ───────────────────────────────────────────────────────────
function toggleProject(code: string) {
  const next = new Set(selectedCodes.value);
  if (next.has(code)) next.delete(code);
  else next.add(code);
  selectedCodes.value = next;
}

function toggleSelectAll() {
  if (allFilteredSelected.value) {
    const next = new Set(selectedCodes.value);
    filteredProjects.value.forEach(p => next.delete(p.code));
    selectedCodes.value = next;
  } else {
    const next = new Set(selectedCodes.value);
    filteredProjects.value.forEach(p => next.add(p.code));
    selectedCodes.value = next;
  }
}

// ── Sync approval helpers ─────────────────────────────────────────────────────
function getSyncApproval(code: string) {
  return syncApprovals.value.get(code) ?? { approveNameUpdate: false, approveAddressUpdate: false };
}

function setSyncApproval(code: string, field: 'name' | 'address', value: boolean) {
  const cur = getSyncApproval(code);
  const next = new Map(syncApprovals.value);
  next.set(code, {
    approveNameUpdate: field === 'name' ? value : cur.approveNameUpdate,
    approveAddressUpdate: field === 'address' ? value : cur.approveAddressUpdate,
  });
  syncApprovals.value = next;
}

// ── Step navigation ───────────────────────────────────────────────────────────
async function goToStep2() {
  if (selectedCodes.value.size === 0) return;
  comparingIss.value = true;
  try {
    const codes = Array.from(selectedCodes.value);
    comparisonResults.value = await routeOptimizationService.compareWithIss(codes);
    // Pre-check address updates for projects with differences
    const initApprovals = new Map<string, { approveNameUpdate: boolean; approveAddressUpdate: boolean }>();
    comparisonResults.value.forEach(r => {
      if (r.hasDifference) {
        initApprovals.set(r.projectCode, {
          approveNameUpdate: false,
          approveAddressUpdate: r.addressChanged, // default: approve address updates
        });
      }
    });
    syncApprovals.value = initApprovals;
    step.value = 2;
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'ISS karşılaştırması başarısız.', 'error');
  } finally {
    comparingIss.value = false;
  }
}

async function applySync() {
  const approvals: SyncApprovalRequestDto[] = [];
  for (const [code, approval] of syncApprovals.value.entries()) {
    if (approval.approveNameUpdate || approval.approveAddressUpdate) {
      approvals.push({ projectCode: code, ...approval });
    }
  }
  if (approvals.length === 0) return;

  syncing.value = true;
  try {
    const result = await routeOptimizationService.syncApprovals(approvals);
    notificationStore.add(`${result.updated} proje güncellendi.`, 'success');
    // Clear approved items from the map
    const next = new Map(syncApprovals.value);
    approvals.forEach(a => next.delete(a.projectCode));
    syncApprovals.value = next;
    // Refresh comparison results to reflect updates
    const codes = Array.from(selectedCodes.value);
    comparisonResults.value = await routeOptimizationService.compareWithIss(codes);
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Güncelleme başarısız.', 'error');
  } finally {
    syncing.value = false;
  }
}

async function runOptimization() {
  optimizing.value = true;
  try {
    optimizationResult.value = await routeOptimizationService.optimize({
      projectCodes: Array.from(selectedCodes.value),
      startAddress: startAddress.value.trim() || null,
    });
    step.value = 3;
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Rota optimizasyonu başarısız.', 'error');
  } finally {
    optimizing.value = false;
  }
}

function resetWizard() {
  step.value = 1;
  selectedCodes.value = new Set();
  comparisonResults.value = [];
  syncApprovals.value = new Map();
  optimizationResult.value = null;
}

// ── Formatters ────────────────────────────────────────────────────────────────
function formatDistance(meters: number): string {
  if (meters >= 1000) return `${(meters / 1000).toFixed(1)} km`;
  return `${Math.round(meters)} m`;
}

function formatDuration(seconds: number): string {
  const m = Math.round(seconds / 60);
  if (m < 60) return `${m} dk`;
  const h = Math.floor(m / 60);
  const rem = m % 60;
  return rem > 0 ? `${h} sa ${rem} dk` : `${h} sa`;
}

async function copyToClipboard() {
  if (!optimizationResult.value) return;
  const lines = optimizationResult.value.optimizedStops.map(
    s => `${s.order}. ${s.projectCode} — ${s.projectName}${s.address ? `\n   ${s.address}` : ''}`
  );
  const text = lines.join('\n');
  try {
    await navigator.clipboard.writeText(text);
    notificationStore.add('Rota panoya kopyalandı.', 'success');
  } catch {
    notificationStore.add('Kopyalama başarısız.', 'error');
  }
}

// ── Load projects ─────────────────────────────────────────────────────────────
onMounted(async () => {
  loadingProjects.value = true;
  try {
    const data = await projectService.getProjects();
    projects.value = data.map((p: any) => ({
      code: p.code,
      name: p.name,
      address: p.address ?? null,
    }));
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Projeler yüklenemedi.', 'error');
  } finally {
    loadingProjects.value = false;
  }
});
</script>
