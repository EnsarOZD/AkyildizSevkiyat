<template>
  <div class="p-4 md:p-6">

    <!-- Header -->
    <div class="mb-5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
      <div>
        <h1 class="text-xl font-semibold text-gray-900 dark:text-gray-100">Mutabakat Kontrolleri</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">Operasyonel tutarsızlıkları görüntüle ve yönet</p>
      </div>
      <BaseButton @click="runChecks" :disabled="isRunning" :loading="isRunning" variant="primary">
        <svg v-if="!isRunning" xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 mr-1.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
        </svg>
        {{ isRunning ? 'Çalışıyor...' : 'Kontrolleri Çalıştır' }}
      </BaseButton>
    </div>

    <!-- Last run result banner -->
    <div v-if="lastRunResult" class="mb-5 bg-indigo-50 dark:bg-indigo-900/20 border border-indigo-200 dark:border-indigo-800 rounded-xl p-4 flex flex-wrap items-center gap-4">
      <span class="text-sm font-bold text-indigo-700 dark:text-indigo-400">Son çalıştırma sonucu:</span>
      <span class="text-sm text-indigo-600 dark:text-indigo-300">
        <b>{{ lastRunResult.newIssues }}</b> yeni sorun,
        <b>{{ lastRunResult.autoResolved }}</b> otomatik çözüldü,
        <b>{{ lastRunResult.totalChecked }}</b> kayıt kontrol edildi
        <span class="text-indigo-400">({{ lastRunResult.durationMs }}ms)</span>
      </span>
      <button @click="lastRunResult = null" class="ml-auto text-indigo-400 hover:text-indigo-600 text-lg leading-none">&times;</button>
    </div>

    <!-- Open Summary Cards -->
    <div v-if="openSummary && hasSummary" class="mb-5 grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-3">
      <div
        v-for="(label, key) in checkTypeLabels"
        :key="key"
        class="bg-white dark:bg-gray-900 border rounded-xl p-3 cursor-pointer transition-colors"
        :class="filters.checkType === key
          ? 'border-indigo-400 ring-2 ring-indigo-200 dark:ring-indigo-800'
          : 'border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600'"
        @click="toggleCheckTypeFilter(key as ReconciliationCheckType)"
      >
        <div class="text-2xl font-bold" :class="(openSummary[key as ReconciliationCheckType] ?? 0) > 0 ? 'text-red-600 dark:text-red-400' : 'text-gray-300 dark:text-gray-600'">
          {{ openSummary[key as ReconciliationCheckType] ?? 0 }}
        </div>
        <div class="text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wide mt-1 leading-tight">{{ label }}</div>
      </div>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4 mb-5">
      <div class="flex flex-wrap gap-3 items-end">
        <div class="w-44">
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Kontrol Türü</label>
          <select v-model="filters.checkType" class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100">
            <option value="">Tümü</option>
            <option v-for="(label, key) in checkTypeLabels" :key="key" :value="key">{{ label }}</option>
          </select>
        </div>
        <div class="w-36">
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Durum</label>
          <select v-model="filters.status" class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100">
            <option value="">Tümü</option>
            <option value="Open">Açık</option>
            <option value="Acknowledged">Onaylandı</option>
            <option value="AutoResolved">Otomatik Çözüldü</option>
          </select>
        </div>
        <div class="w-32">
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Önem</label>
          <select v-model="filters.severity" class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100">
            <option value="">Tümü</option>
            <option value="Error">Hata</option>
            <option value="Warning">Uyarı</option>
          </select>
        </div>
        <div class="w-40">
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Başlangıç</label>
          <input type="date" v-model="filters.fromDate" class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <div class="w-40">
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Bitiş</label>
          <input type="date" v-model="filters.toDate" class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <BaseButton @click="applyFilters" variant="secondary" size="sm">Filtrele</BaseButton>
        <BaseButton @click="resetFilters" variant="ghost" size="sm">Sıfırla</BaseButton>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">

      <!-- Loading skeleton -->
      <div v-if="loading" class="p-8 flex justify-center">
        <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-indigo-600"></div>
      </div>

      <!-- Empty state -->
      <div v-else-if="issues.length === 0" class="flex flex-col items-center justify-center py-16 text-gray-400">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-12 w-12 mb-3 text-gray-200 dark:text-gray-700" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <p class="font-medium text-sm">Sorun bulunamadı</p>
        <p class="text-xs mt-1">Filtreler değiştirilebilir veya kontroller yeniden çalıştırılabilir</p>
      </div>

      <!-- Table content -->
      <div v-else class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tür</th>
              <th class="px-4 py-3 text-left text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Önem</th>
              <th class="px-4 py-3 text-left text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
              <th class="px-4 py-3 text-left text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Açıklama</th>
              <th class="px-4 py-3 text-left text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">Beklenen / Gerçek</th>
              <th class="px-4 py-3 text-left text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden md:table-cell">Tarih</th>
              <th class="px-4 py-3 text-right text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlem</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
            <tr
              v-for="issue in issues"
              :key="issue.id"
              class="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
              :class="issue.severity === 'Error' && issue.status === 'Open' ? 'bg-red-50/30 dark:bg-red-900/10' : ''"
            >
              <!-- Check Type -->
              <td class="px-4 py-3">
                <span class="inline-block px-2 py-0.5 rounded text-[10px] font-bold uppercase"
                  :class="checkTypeClass(issue.checkType)">
                  {{ checkTypeLabels[issue.checkType] }}
                </span>
              </td>

              <!-- Severity -->
              <td class="px-4 py-3">
                <span class="inline-flex items-center gap-1 text-xs font-bold"
                  :class="issue.severity === 'Error' ? 'text-red-600 dark:text-red-400' : 'text-yellow-600 dark:text-yellow-400'">
                  <span class="w-1.5 h-1.5 rounded-full flex-shrink-0"
                    :class="issue.severity === 'Error' ? 'bg-red-500' : 'bg-yellow-500'"></span>
                  {{ issue.severity === 'Error' ? 'Hata' : 'Uyarı' }}
                </span>
              </td>

              <!-- Status -->
              <td class="px-4 py-3">
                <span class="inline-block px-2 py-0.5 rounded-full text-[10px] font-bold"
                  :class="{
                    'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-400': issue.status === 'Open',
                    'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-400': issue.status === 'Acknowledged',
                    'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-400': issue.status === 'AutoResolved',
                  }">
                  {{ statusLabel(issue.status) }}
                </span>
              </td>

              <!-- Description -->
              <td class="px-4 py-3 max-w-xs">
                <p class="text-sm text-gray-800 dark:text-gray-200 leading-snug">{{ issue.description }}</p>
                <p v-if="issue.shipmentId" class="text-[10px] text-gray-400 mt-0.5 font-mono">
                  Sevkiyat #{{ issue.shipmentId }}
                </p>
                <p v-if="issue.status === 'Acknowledged' && issue.acknowledgementNote" class="text-[10px] text-blue-600 dark:text-blue-400 mt-0.5 italic">
                  Not: {{ issue.acknowledgementNote }}
                </p>
              </td>

              <!-- Expected / Actual -->
              <td class="px-4 py-3 hidden lg:table-cell">
                <div v-if="issue.expectedValue || issue.actualValue" class="text-xs space-y-0.5">
                  <div v-if="issue.expectedValue" class="text-gray-500">
                    <span class="font-bold text-gray-400">Beklenen:</span> {{ issue.expectedValue }}
                  </div>
                  <div v-if="issue.actualValue" class="text-gray-500">
                    <span class="font-bold text-gray-400">Gerçek:</span> {{ issue.actualValue }}
                  </div>
                </div>
                <span v-else class="text-gray-300 text-xs">—</span>
              </td>

              <!-- Date -->
              <td class="px-4 py-3 hidden md:table-cell whitespace-nowrap">
                <span class="text-xs text-gray-500 dark:text-gray-400">{{ formatDate(issue.detectedAt) }}</span>
              </td>

              <!-- Actions -->
              <td class="px-4 py-3 text-right">
                <button
                  v-if="issue.status === 'Open'"
                  @click="openAcknowledge(issue)"
                  class="px-3 py-1.5 text-xs font-bold text-blue-600 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-lg transition-colors border border-blue-200 dark:border-blue-800"
                >
                  Onayla
                </button>
                <span v-else class="text-gray-300 dark:text-gray-600 text-xs">—</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <Pagination
        v-if="totalCount > pageSize"
        :currentPage="page"
        :totalPages="totalPages"
        :totalCount="totalCount"
        :pageSize="pageSize"
        @page-change="onPageChange"
      />
    </div>

  </div>

  <!-- Acknowledge Dialog -->
  <div v-if="acknowledgeTarget" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
    <div class="bg-white dark:bg-gray-900 rounded-xl w-full max-w-md shadow-2xl p-6">
      <h3 class="text-base font-bold text-gray-800 dark:text-gray-100 mb-1">Sorunu Onayla</h3>
      <p class="text-sm text-gray-500 dark:text-gray-400 mb-1">
        <span class="font-medium text-gray-700 dark:text-gray-300">{{ acknowledgeTarget.description }}</span>
      </p>
      <p class="text-xs text-gray-400 mb-4">Ne yapıldığını veya neden kabul edildiğini açıklayın.</p>
      <div class="mb-4">
        <label class="block text-xs font-bold text-gray-500 uppercase tracking-wider mb-1">Not <span class="text-red-500">*</span></label>
        <textarea
          v-model="acknowledgeNote"
          rows="3"
          placeholder="Açıklama yazın..."
          class="w-full border border-gray-300 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-blue-400 focus:border-blue-400 outline-none resize-none"
          ref="acknowledgeTextarea"
        ></textarea>
      </div>
      <div class="flex gap-3">
        <button @click="acknowledgeTarget = null; acknowledgeNote = ''" class="flex-1 py-2.5 bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-300 rounded-lg font-bold text-sm">
          İptal
        </button>
        <button
          @click="submitAcknowledge"
          :disabled="!acknowledgeNote.trim() || isAcknowledging"
          class="flex-[2] py-2.5 bg-blue-600 text-white rounded-lg font-bold text-sm shadow hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
        >
          <span v-if="isAcknowledging" class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></span>
          Onayla
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick } from 'vue';
import BaseButton from '../components/BaseButton.vue';
import reconciliationService, {
  type ReconciliationIssueDto,
  type ReconciliationCheckType,
} from '../services/reconciliationService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import Pagination from '../components/Pagination.vue';

const notify = useNotificationStore();

// ── State ──────────────────────────────────────────────────────────────────
const issues     = ref<ReconciliationIssueDto[]>([]);
const totalCount = ref(0);
const totalPages = ref(1);
const page       = ref(1);
const pageSize   = 25;
const loading    = ref(false);
const isRunning  = ref(false);
const lastRunResult = ref<{ newIssues: number; autoResolved: number; totalChecked: number; durationMs: number } | null>(null);
const openSummary = ref<Record<string, number>>({});
const hasSummary  = computed(() => Object.values(openSummary.value).some(v => v > 0) || true);

const filters = ref({
  checkType: '' as ReconciliationCheckType | '',
  status: 'Open' as string,  // default: show Open only
  severity: '' as string,
  fromDate: '',
  toDate: '',
});

// Acknowledge dialog
const acknowledgeTarget  = ref<ReconciliationIssueDto | null>(null);
const acknowledgeNote    = ref('');
const isAcknowledging    = ref(false);
const acknowledgeTextarea = ref<HTMLTextAreaElement | null>(null);

// ── Lookup tables ──────────────────────────────────────────────────────────
const checkTypeLabels: Record<ReconciliationCheckType, string> = {
  IssQtyMismatch:        'ISS Miktar',
  PickingIncomplete:     'Eksik Toplama',
  NetsisTransferMissing: 'Netsis Aktarım',
  IrsaliyeMissing:       'İrsaliye',
  IssCoverageGap:        'ISS Kapsam',
};

const checkTypeClass = (type: ReconciliationCheckType) => {
  const map: Record<ReconciliationCheckType, string> = {
    IssQtyMismatch:        'bg-orange-100 text-orange-700 dark:bg-orange-900/40 dark:text-orange-400',
    PickingIncomplete:     'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/40 dark:text-yellow-400',
    NetsisTransferMissing: 'bg-purple-100 text-purple-700 dark:bg-purple-900/40 dark:text-purple-400',
    IrsaliyeMissing:       'bg-pink-100 text-pink-700 dark:bg-pink-900/40 dark:text-pink-400',
    IssCoverageGap:        'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400',
  };
  return map[type] ?? 'bg-gray-100 text-gray-600';
};

const statusLabel = (s: string) => {
  if (s === 'Open') return 'Açık';
  if (s === 'Acknowledged') return 'Onaylandı';
  if (s === 'AutoResolved') return 'Otomatik Çözüldü';
  return s;
};

const formatDate = (iso: string) => {
  if (!iso) return '';
  const d = new Date(iso);
  return d.toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
};

// ── Data fetching ──────────────────────────────────────────────────────────
async function fetchIssues() {
  loading.value = true;
  try {
    const result = await reconciliationService.getIssues({
      checkType: (filters.value.checkType || undefined) as any,
      status:    (filters.value.status    || undefined) as any,
      severity:  (filters.value.severity  || undefined) as any,
      fromDate:  filters.value.fromDate  || undefined,
      toDate:    filters.value.toDate    || undefined,
      page:      page.value,
      pageSize,
    });
    issues.value     = result.items;
    totalCount.value = result.totalCount;
    totalPages.value = result.totalPages;
    openSummary.value = result.openSummary ?? {};
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Liste yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
}

async function runChecks() {
  isRunning.value = true;
  lastRunResult.value = null;
  try {
    const result = await reconciliationService.runChecks(
      filters.value.fromDate || undefined,
      filters.value.toDate   || undefined,
    );
    lastRunResult.value = result;
    notify.add(`Kontroller tamamlandı: ${result.newIssues} yeni sorun, ${result.autoResolved} çözüldü.`, 'success');
    await fetchIssues();
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Kontroller çalıştırılamadı.', 'error');
  } finally {
    isRunning.value = false;
  }
}

// ── Filters ────────────────────────────────────────────────────────────────
function applyFilters() {
  page.value = 1;
  fetchIssues();
}

function resetFilters() {
  filters.value = { checkType: '', status: 'Open', severity: '', fromDate: '', toDate: '' };
  page.value = 1;
  fetchIssues();
}

function toggleCheckTypeFilter(type: ReconciliationCheckType) {
  filters.value.checkType = filters.value.checkType === type ? '' : type;
  filters.value.status = '';
  page.value = 1;
  fetchIssues();
}

function onPageChange(p: number) {
  page.value = p;
  fetchIssues();
}

// ── Acknowledge ─────────────────────────────────────────────────────────────
function openAcknowledge(issue: ReconciliationIssueDto) {
  acknowledgeTarget.value = issue;
  acknowledgeNote.value   = '';
  nextTick(() => acknowledgeTextarea.value?.focus());
}

async function submitAcknowledge() {
  if (!acknowledgeTarget.value || !acknowledgeNote.value.trim()) return;
  isAcknowledging.value = true;
  try {
    await reconciliationService.acknowledge(acknowledgeTarget.value.id, acknowledgeNote.value.trim());
    notify.add(`Sorun #${acknowledgeTarget.value.id} onaylandı.`, 'success');
    acknowledgeTarget.value = null;
    acknowledgeNote.value   = '';
    await fetchIssues();
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
  } finally {
    isAcknowledging.value = false;
  }
}

onMounted(fetchIssues);
</script>
