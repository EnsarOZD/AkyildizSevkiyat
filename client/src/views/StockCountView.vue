<template>
  <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">

    <!-- LIST MODE -->
    <template v-if="!activeCount">
      <div class="flex justify-between items-center mb-6">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Stok Sayımları</h1>
          <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">Tüm stok kalemleri sayılır, fark varsa düzeltme fişi oluşturulur.</p>
        </div>
        <button
          v-role="['Admin', 'Manager']"
          @click="openCreateModal"
          class="px-4 py-2 bg-indigo-600 text-white rounded font-semibold hover:bg-indigo-700 transition"
        >
          + Yeni Sayım Başlat
        </button>
      </div>

      <div v-if="error" class="mx-4 mt-4 p-3 bg-red-900/30 border border-red-700 rounded-lg flex items-center justify-between">
        <span class="text-red-400 text-sm">{{ error }}</span>
        <button @click="loadList(); error = null" class="text-red-400 hover:text-red-300 text-sm underline ml-4">Tekrar dene</button>
      </div>
      <div v-if="loadingList" class="py-12 text-center text-gray-400">Yükleniyor...</div>
      <div v-else-if="counts.length === 0" class="py-12 text-center text-gray-400">Henüz sayım kaydı yok.</div>
      <div v-else class="bg-white dark:bg-gray-900 shadow rounded-lg overflow-hidden">
        <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">#</th>
              <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Tarih</th>
              <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Durum</th>
              <th class="px-4 py-3 text-right font-medium text-gray-500 dark:text-gray-400">Toplam</th>
              <th class="px-4 py-3 text-right font-medium text-gray-500 dark:text-gray-400">Sayılan</th>
              <th class="px-4 py-3 text-right font-medium text-gray-500 dark:text-gray-400">Düzeltilen</th>
              <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Not</th>
              <th class="px-4 py-3 text-right font-medium text-gray-500 dark:text-gray-400">İşlem</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
            <tr v-for="c in counts" :key="c.id" :class="{ 'bg-yellow-50': c.status === 'Draft' }">
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400">{{ c.id }}</td>
              <td class="px-4 py-3 font-medium">{{ formatDate(c.countDate) }}</td>
              <td class="px-4 py-3">
                <span :class="statusClass(c.status)" class="px-2 py-0.5 rounded-full text-xs font-semibold">
                  {{ statusLabel(c.status) }}
                </span>
              </td>
              <td class="px-4 py-3 text-right">{{ c.totalLines }}</td>
              <td class="px-4 py-3 text-right">{{ c.countedLines }}</td>
              <td class="px-4 py-3 text-right" :class="{ 'text-red-600 font-semibold': c.adjustedLines > 0 }">
                {{ c.adjustedLines }}
              </td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400 truncate max-w-xs">{{ c.note || '—' }}</td>
              <td class="px-4 py-3 text-right">
                <button
                  @click="openCount(c.id)"
                  class="text-xs px-3 py-1 bg-indigo-600 text-white rounded hover:bg-indigo-700"
                >
                  {{ c.status === 'Draft' ? 'Sayıma Devam Et' : 'Görüntüle' }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        </div>
      </div>
    </template>

    <!-- DETAIL MODE -->
    <template v-else>
      <div class="flex items-center gap-4 mb-6">
        <button @click="activeCount = null" class="text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 transition">
          ← Geri
        </button>
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">
            Sayım #{{ activeCount.id }} — {{ formatDate(activeCount.countDate) }}
          </h1>
          <span :class="statusClass(activeCount.status)" class="mt-1 inline-block px-2 py-0.5 rounded-full text-xs font-semibold">
            {{ statusLabel(activeCount.status) }}
          </span>
        </div>
        <div class="ml-auto flex gap-2 flex-wrap" v-if="activeCount.status === 'Draft'">
          <button
            @click="downloadTemplate"
            :disabled="exporting"
            class="px-4 py-2 bg-gray-600 text-white rounded font-semibold hover:bg-gray-700 disabled:bg-gray-400 transition"
          >
            {{ exporting ? 'İndiriliyor...' : '📥 Şablon İndir' }}
          </button>
          <button
            @click="triggerFileInput"
            :disabled="importing"
            class="px-4 py-2 bg-orange-600 text-white rounded font-semibold hover:bg-orange-700 disabled:bg-orange-300 transition"
          >
            {{ importing ? 'Yükleniyor...' : '📤 Excel\'den Yükle' }}
          </button>
          <input
            ref="fileInputRef"
            type="file"
            accept=".xlsx,.xls"
            class="hidden"
            @change="handleFileUpload"
          />
          <button
            @click="saveLines"
            :disabled="saving"
            class="px-4 py-2 bg-blue-600 text-white rounded font-semibold hover:bg-blue-700 disabled:bg-blue-300 transition"
          >
            {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
          </button>
          <button
            v-role="['Admin', 'Manager']"
            @click="confirmComplete"
            class="px-4 py-2 bg-green-600 text-white rounded font-semibold hover:bg-green-700 transition"
          >
            Sayımı Tamamla & Düzeltme Fişi Oluştur
          </button>
          <button
            v-role="['Admin', 'Manager']"
            @click="showCancelModal = true"
            class="px-4 py-2 bg-red-600 text-white rounded font-semibold hover:bg-red-700 transition"
          >
            ❌ Sayımı İptal Et
          </button>
        </div>
      </div>

      <!-- Import Result Banner -->
      <div v-if="importResult" class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 mb-4 border-l-4" :class="importResult.updatedCount > 0 ? 'border-green-500' : 'border-yellow-500'">
        <div class="flex justify-between items-center">
          <div class="text-sm">
            <span class="font-semibold text-green-700">{{ importResult.updatedCount }}</span> satır güncellendi,
            <span class="font-semibold text-yellow-600">{{ importResult.skippedCount }}</span> satır atlandı.
          </div>
          <button @click="importResult = null" class="text-gray-400 hover:text-gray-600 text-xs">✕</button>
        </div>
      </div>

      <!-- Progress bar -->
      <div v-if="activeCount.status === 'Draft'" class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 mb-4 flex items-center gap-4">
        <div class="flex-1 bg-gray-200 dark:bg-gray-700 rounded-full h-3">
          <div
            class="bg-indigo-500 h-3 rounded-full transition-all"
            :style="{ width: progressPct + '%' }"
          />
        </div>
        <span class="text-sm font-medium text-gray-600 dark:text-gray-400">
          {{ countedCount }} / {{ activeCount.lines.length }} sayıldı ({{ progressPct }}%)
        </span>
      </div>

      <!-- Filters -->
      <div class="flex gap-2 mb-4">
        <input
          v-model="searchFilter"
          type="text"
          placeholder="Stok kodu veya adı ara..."
          class="border dark:border-gray-700 rounded px-3 py-2 text-sm flex-1 dark:bg-gray-800 dark:text-gray-100"
        />
        <select v-model="diffFilter" class="border dark:border-gray-700 rounded px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100">
          <option value="">Tüm Satırlar</option>
          <option value="uncounted">Sayılmamış</option>
          <option value="diff">Fark Var</option>
          <option value="ok">Fark Yok</option>
        </select>
      </div>

      <div class="bg-white dark:bg-gray-900 shadow rounded-lg overflow-hidden">
        <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-3 py-3 text-left font-medium text-gray-500 dark:text-gray-400 w-8">#</th>
              <th class="px-3 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Stok Kodu</th>
              <th class="px-3 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Stok Adı</th>
              <th class="px-3 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Lokasyon</th>
              <th class="px-3 py-3 text-right font-medium text-gray-500 dark:text-gray-400">Beklenen</th>
              <th class="px-3 py-3 text-right font-medium text-gray-500 dark:text-gray-400">Sayılan</th>
              <th class="px-3 py-3 text-right font-medium text-gray-500 dark:text-gray-400">Fark</th>
              <th class="px-3 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Not</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
            <tr
              v-for="(line, idx) in filteredLines"
              :key="line.id"
              :class="rowClass(line)"
            >
              <td class="px-3 py-2 text-gray-400 text-xs">{{ idx + 1 }}</td>
              <td class="px-3 py-2 font-mono text-gray-800 dark:text-gray-200">{{ line.stockCode }}</td>
              <td class="px-3 py-2 text-gray-700 dark:text-gray-300">{{ line.stockName }}</td>
              <td class="px-3 py-2 text-gray-500 dark:text-gray-400 font-mono text-xs">{{ line.warehouseLocation || '—' }}</td>
              <td class="px-3 py-2 text-right text-gray-600 dark:text-gray-400">{{ line.expectedQty }}</td>
              <td class="px-3 py-2 text-right">
                <input
                  v-if="activeCount.status === 'Draft' && lineEdits[line.id]"
                  v-model.number="lineEdits[line.id]!.actualQty"
                  type="number"
                  min="0"
                  step="0.01"
                  class="w-24 border rounded px-2 py-1 text-right text-sm focus:ring-2 focus:ring-indigo-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                  :class="{ 'border-yellow-400 bg-yellow-50': lineEdits[line.id]!.actualQty == null }"
                  placeholder="?"
                />
                <span v-else>{{ line.actualQty ?? '—' }}</span>
              </td>
              <td class="px-3 py-2 text-right font-semibold" :class="diffClass(line)">
                {{ formatDiff(line) }}
              </td>
              <td class="px-3 py-2">
                <input
                  v-if="activeCount.status === 'Draft' && lineEdits[line.id]"
                  v-model="lineEdits[line.id]!.note"
                  type="text"
                  placeholder="opsiyonel"
                  class="w-full border rounded px-2 py-1 text-xs focus:ring-2 focus:ring-indigo-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                />
                <span v-else class="text-xs text-gray-500 dark:text-gray-400">{{ line.note || '' }}</span>
              </td>
            </tr>
          </tbody>
        </table>
        </div>
      </div>
    </template>

    <!-- Create Modal -->
    <BaseModal :show="showCreateModal" title="Yeni Sayım Başlat" maxWidth="sm" @close="showCreateModal = false">
      <div class="space-y-4">
        <p class="text-sm text-gray-600 dark:text-gray-400 bg-indigo-50 border border-indigo-100 rounded p-3">
          Tüm aktif stok kalemleri mevcut sistematik miktarlarıyla sayım formuna eklenir.
        </p>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Sayım Tarihi <span class="text-red-500">*</span></label>
          <input v-model="createForm.countDate" type="date" class="w-full border rounded px-3 py-2 focus:ring-2 focus:ring-indigo-400 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Not</label>
          <textarea v-model="createForm.note" rows="2" class="w-full border rounded px-3 py-2 text-sm focus:ring-2 focus:ring-indigo-400 resize-none placeholder-gray-400 dark:placeholder-gray-600 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" placeholder="Opsiyonel..." />
        </div>
      </div>
      <template #footer>
        <button @click="showCreateModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button
          @click="submitCreate"
          :disabled="!createForm.countDate || creating"
          class="px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 font-bold disabled:bg-indigo-300"
        >
          {{ creating ? 'Oluşturuluyor...' : 'Sayımı Başlat' }}
        </button>
      </template>
    </BaseModal>

    <!-- Complete Confirm Modal -->
    <BaseModal :show="showCompleteModal" title="Sayımı Tamamla" maxWidth="sm" @close="showCompleteModal = false">
      <div class="space-y-3">
        <p class="text-sm text-gray-700 dark:text-gray-300">
          Sayım tamamlandığında:
        </p>
        <ul class="text-sm text-gray-600 dark:text-gray-400 list-disc list-inside space-y-1">
          <li>Girilen miktarlarla beklenen miktarlar karşılaştırılır</li>
          <li>Fark olan her kalem için <strong>ManualAdjust</strong> stok hareketi oluşturulur</li>
          <li>Stok miktarları otomatik güncellenir</li>
          <li>Bu işlem <strong>geri alınamaz</strong></li>
        </ul>
        <p class="text-sm font-semibold text-orange-600 bg-orange-50 border border-orange-100 rounded p-2">
          Sayılmamış satırlar (boş bırakılanlar) düzeltme fişine dahil edilmez.
        </p>
      </div>
      <template #footer>
        <button @click="showCompleteModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button
          @click="submitComplete"
          :disabled="completing"
          class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 font-bold disabled:bg-green-300"
        >
          {{ completing ? 'İşleniyor...' : 'Onayla ve Tamamla' }}
        </button>
      </template>
    </BaseModal>

    <!-- Cancel Confirm Modal -->
    <BaseModal :show="showCancelModal" title="Sayımı İptal Et" maxWidth="sm" @close="showCancelModal = false">
      <div class="space-y-3">
        <p class="text-sm text-gray-700 dark:text-gray-300">
          Bu sayımı iptal etmek istediğinize emin misiniz?
        </p>
        <p class="text-sm font-semibold text-red-600 bg-red-50 border border-red-100 rounded p-2">
          İptal edilen sayımlar geri alınamaz. Stok miktarlarında herhangi bir değişiklik yapılmaz.
        </p>
      </div>
      <template #footer>
        <button @click="showCancelModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">Vazgeç</button>
        <button
          @click="submitCancel"
          :disabled="cancelling"
          class="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 font-bold disabled:bg-red-300"
        >
          {{ cancelling ? 'İptal Ediliyor...' : 'Evet, İptal Et' }}
        </button>
      </template>
    </BaseModal>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import BaseModal from '../components/BaseModal.vue';
import stockCountService, { type StockCountDetail, type StockCountLineDetail, type StockCountSummary } from '../services/stockCountService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';
import { turkishIncludes } from '../utils/turkishSearch';

const notificationStore = useNotificationStore();

// List state
const counts = ref<StockCountSummary[]>([]);
const loadingList = ref(false);
const error = ref<string | null>(null);

// Detail state
const activeCount = ref<StockCountDetail | null>(null);
const lineEdits = ref<Record<number, { actualQty: number | null; note: string }>>({});
const saving = ref(false);
const exporting = ref(false);
const importing = ref(false);
const importResult = ref<{ updatedCount: number; skippedCount: number; errorCount: number } | null>(null);
const fileInputRef = ref<HTMLInputElement | null>(null);

// Filters
const searchFilter = ref('');
const diffFilter = ref('');

const filteredLines = computed(() => {
  if (!activeCount.value) return [];
  return activeCount.value.lines.filter(l => {
    const q = searchFilter.value;
    if (q && !turkishIncludes(l.stockCode, q) && !turkishIncludes(l.stockName, q)) return false;

    const edit = lineEdits.value[l.id];
    const actual = edit ? edit.actualQty : l.actualQty;
    if (diffFilter.value === 'uncounted' && actual != null) return false;
    if (diffFilter.value === 'diff') {
      if (actual == null) return false;
      if (actual === l.expectedQty) return false;
    }
    if (diffFilter.value === 'ok') {
      if (actual == null || actual !== l.expectedQty) return false;
    }
    return true;
  });
});

const countedCount = computed(() => {
  if (!activeCount.value) return 0;
  return activeCount.value.lines.filter(l => {
    const edit = lineEdits.value[l.id];
    return edit ? edit.actualQty != null : l.actualQty != null;
  }).length;
});

const progressPct = computed(() => {
  if (!activeCount.value || activeCount.value.lines.length === 0) return 0;
  return Math.round((countedCount.value / activeCount.value.lines.length) * 100);
});

const loadList = async () => {
  loadingList.value = true;
  try {
    counts.value = await stockCountService.getAll();
  } catch (err) {
    error.value = ApiErrorUtils.getErrorMessage(err) || 'Sayım listesi yüklenemedi.';
    notificationStore.add(error.value, 'error');
  } finally {
    loadingList.value = false;
  }
};

const openCount = async (id: number) => {
  const detail = await stockCountService.getDetail(id);
  activeCount.value = detail;
  // Init edits
  const edits: Record<number, { actualQty: number | null; note: string }> = {};
  for (const l of detail.lines) {
    edits[l.id] = { actualQty: l.actualQty ?? null, note: l.note ?? '' };
  }
  lineEdits.value = edits;
};

const saveLines = async () => {
  if (!activeCount.value) return;
  saving.value = true;
  try {
    const lines = Object.entries(lineEdits.value)
      .filter(([, v]) => v.actualQty != null)
      .map(([id, v]) => ({
        stockCountLineId: Number(id),
        actualQty: v.actualQty as number,
        note: v.note || undefined,
      }));
    if (lines.length === 0) {
      notificationStore.add('Kaydedilecek değişiklik yok.', 'warning');
      return;
    }
    await stockCountService.updateLines(activeCount.value.id, lines);
    notificationStore.add('Sayım satırları kaydedildi.', 'success');
    await openCount(activeCount.value.id);
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Kayıt başarısız.', 'error');
  } finally {
    saving.value = false;
  }
};

// Excel
const downloadTemplate = async () => {
  if (!activeCount.value) return;
  exporting.value = true;
  try {
    await stockCountService.exportTemplate(activeCount.value.id);
    notificationStore.add('Şablon indirildi.', 'success');
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Şablon indirilemedi.', 'error');
  } finally {
    exporting.value = false;
  }
};

const triggerFileInput = () => {
  fileInputRef.value?.click();
};

const handleFileUpload = async (event: Event) => {
  const target = event.target as HTMLInputElement;
  const file = target.files?.[0];
  if (!file || !activeCount.value) return;

  importing.value = true;
  importResult.value = null;
  try {
    const result = await stockCountService.importExcel(activeCount.value.id, file);
    importResult.value = result;
    notificationStore.add(
      `Excel yüklendi: ${result.updatedCount} güncellendi, ${result.skippedCount} atlandı.`,
      result.updatedCount > 0 ? 'success' : 'warning'
    );
    // Reload the count to see new values
    await openCount(activeCount.value.id);
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Excel yüklenemedi.', 'error');
  } finally {
    importing.value = false;
    // Reset file input so re-selecting the same file triggers change
    target.value = '';
  }
};

// Create
const showCreateModal = ref(false);
const creating = ref(false);
const createForm = ref({ countDate: new Date().toISOString().slice(0, 10), note: '' });

const openCreateModal = () => {
  createForm.value = { countDate: new Date().toISOString().slice(0, 10), note: '' };
  showCreateModal.value = true;
};

const submitCreate = async () => {
  creating.value = true;
  try {
    const res = await stockCountService.create(createForm.value.countDate, createForm.value.note || undefined);
    showCreateModal.value = false;
    notificationStore.add('Sayım oluşturuldu.', 'success');
    await openCount(res.id);
    await loadList();
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'İşlem başarısız.', 'error');
  } finally {
    creating.value = false;
  }
};

// Complete
const showCompleteModal = ref(false);
const completing = ref(false);

const confirmComplete = async () => {
  // Save first
  await saveLines();
  showCompleteModal.value = true;
};

const submitComplete = async () => {
  if (!activeCount.value) return;
  completing.value = true;
  try {
    const result = await stockCountService.complete(activeCount.value.id);
    showCompleteModal.value = false;
    notificationStore.add(
      `Sayım tamamlandı. ${result.adjustedLines} satırda düzeltme yapıldı.` +
      (result.totalPositiveDiff > 0 ? ` Fazla: +${result.totalPositiveDiff}` : '') +
      (result.totalNegativeDiff < 0 ? ` Eksik: ${result.totalNegativeDiff}` : ''),
      'success'
    );
    await openCount(activeCount.value.id);
    await loadList();
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Tamamlama başarısız.', 'error');
  } finally {
    completing.value = false;
  }
};

// Cancel
const showCancelModal = ref(false);
const cancelling = ref(false);

const submitCancel = async () => {
  if (!activeCount.value) return;
  cancelling.value = true;
  try {
    await stockCountService.cancel(activeCount.value.id);
    showCancelModal.value = false;
    notificationStore.add('Sayım iptal edildi.', 'success');
    activeCount.value = null;
    await loadList();
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'İptal başarısız.', 'error');
  } finally {
    cancelling.value = false;
  }
};

// Helpers
const formatDate = (d: string) => new Date(d).toLocaleDateString('tr-TR');

const statusClass = (s: string) => {
  if (s === 'Draft') return 'bg-yellow-100 text-yellow-800';
  if (s === 'Cancelled') return 'bg-red-100 text-red-800';
  return 'bg-green-100 text-green-800';
};

const statusLabel = (s: string) => {
  if (s === 'Draft') return 'Devam Ediyor';
  if (s === 'Cancelled') return 'İptal Edildi';
  return 'Tamamlandı';
};

const rowClass = (line: StockCountLineDetail) => {
  const edit = lineEdits.value[line.id];
  const actual = edit ? edit.actualQty : line.actualQty;
  if (actual == null) return '';
  const diff = actual - line.expectedQty;
  if (diff < 0) return 'bg-red-50';
  if (diff > 0) return 'bg-blue-50';
  return 'bg-green-50';
};

const diffClass = (line: StockCountLineDetail) => {
  const edit = lineEdits.value[line.id];
  const actual = edit ? edit.actualQty : line.actualQty;
  if (actual == null) return 'text-gray-300';
  const diff = actual - line.expectedQty;
  if (diff < 0) return 'text-red-600';
  if (diff > 0) return 'text-blue-600';
  return 'text-green-600';
};

const formatDiff = (line: StockCountLineDetail) => {
  const edit = lineEdits.value[line.id];
  const actual = edit ? edit.actualQty : line.actualQty;
  if (actual == null) return '—';
  const diff = actual - line.expectedQty;
  if (diff === 0) return '✓';
  return diff > 0 ? `+${diff}` : `${diff}`;
};

onMounted(loadList);
</script>
