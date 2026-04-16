<template>
  <div>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Depo Adres Yönetimi</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
          Koridor · Taraf · Modül · Kat yapısında adres oluştur ve yönet
        </p>
      </div>
      <div class="flex gap-2">
        <button
          v-if="canManage"
          @click="showBulkModal = true"
          class="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-colors"
        >
          <PlusIcon class="w-4 h-4" />
          Toplu Oluştur
        </button>
        <button
          v-if="canManage"
          @click="openCreateModal"
          class="flex items-center gap-2 px-4 py-2 border border-gray-300 dark:border-white/20 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-white/5 text-sm font-medium rounded-lg transition-colors"
        >
          <PlusIcon class="w-4 h-4" />
          Tekli Ekle
        </button>
      </div>
    </div>

    <!-- Error -->
    <div v-if="error" class="mx-4 mt-4 p-3 bg-red-900/30 border border-red-700 rounded-lg flex items-center justify-between">
      <span class="text-red-400 text-sm">{{ error }}</span>
      <button @click="load(); error = null" class="text-red-400 hover:text-red-300 text-sm underline ml-4">Tekrar dene</button>
    </div>

    <!-- Stats -->
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
      <div class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 p-4">
        <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Toplam Adres</p>
        <p class="text-2xl font-bold text-gray-900 dark:text-white">{{ stats.total }}</p>
      </div>
      <div class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 p-4">
        <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Koridor Sayısı</p>
        <p class="text-2xl font-bold text-gray-900 dark:text-white">{{ stats.koridorCount }}</p>
      </div>
      <div class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 p-4">
        <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Aktif</p>
        <p class="text-2xl font-bold text-green-600 dark:text-green-400">{{ stats.active }}</p>
      </div>
      <div class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 p-4">
        <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Pasif</p>
        <p class="text-2xl font-bold text-gray-400">{{ stats.inactive }}</p>
      </div>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 p-4 flex flex-wrap gap-3">
      <select
        v-model="filterKoridor"
        class="px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white"
        @change="load(true)"
      >
        <option value="">Tüm Koridorlar</option>
        <option v-for="k in [1,2,3,4]" :key="k" :value="k">{{ k }}. Koridor</option>
      </select>

      <select
        v-model="filterTaraf"
        class="px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white"
        @change="load(true)"
      >
        <option value="">Tüm Taraflar</option>
        <option value="K">Kuzey (K)</option>
        <option value="G">Güney (G)</option>
      </select>

      <select
        v-model="filterType"
        class="px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white"
        @change="load(true)"
      >
        <option value="">Tüm Tipler</option>
        <option v-for="t in locationTypes" :key="t.id" :value="t.id">{{ t.label }}</option>
      </select>

      <label class="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 cursor-pointer">
        <input type="checkbox" v-model="filterInactive" @change="load(true)" class="rounded" />
        Pasif adresleri göster
      </label>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 overflow-hidden">
      <div v-if="loading" class="flex justify-center py-12">
        <div class="w-7 h-7 border-4 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
      </div>
      <template v-else>
        <div v-if="items.length > 0" class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead class="bg-gray-50 dark:bg-white/5 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
            <tr>
              <th class="px-4 py-3 text-left">Adres Kodu</th>
              <th class="px-4 py-3 text-left hidden sm:table-cell">Koridor</th>
              <th class="px-4 py-3 text-left hidden sm:table-cell">Taraf</th>
              <th class="px-4 py-3 text-left hidden lg:table-cell">Modül</th>
              <th class="px-4 py-3 text-left hidden lg:table-cell">Kat</th>
              <th class="px-4 py-3 text-left hidden sm:table-cell">Tip</th>
              <th class="px-4 py-3 text-left hidden lg:table-cell">Açıklama</th>
              <th class="px-4 py-3 text-left">Durum</th>
              <th v-if="canManage" class="px-4 py-3"></th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-white/5">
            <tr
              v-for="loc in items"
              :key="loc.id"
              class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors"
            >
              <td class="px-4 py-3 font-mono font-semibold text-blue-700 dark:text-blue-300">{{ loc.code }}</td>
              <td class="px-4 py-3 text-gray-900 dark:text-white hidden sm:table-cell">{{ loc.koridorNo }}</td>
              <td class="px-4 py-3 hidden sm:table-cell">
                <span
                  class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
                  :class="loc.taraf === 'K'
                    ? 'bg-sky-100 dark:bg-sky-900/30 text-sky-700 dark:text-sky-300'
                    : 'bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-300'"
                >
                  {{ loc.taraf === 'K' ? 'Kuzey' : 'Güney' }}
                </span>
              </td>
              <td class="px-4 py-3 text-gray-700 dark:text-gray-300 hidden lg:table-cell">{{ loc.modulNo.toString().padStart(3,'0') }}</td>
              <td class="px-4 py-3 text-gray-700 dark:text-gray-300 hidden lg:table-cell">{{ loc.kat.toString().padStart(2,'0') }}</td>
              <td class="px-4 py-3 hidden sm:table-cell">
                <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-300">
                  {{ locationTypeLabel(loc.locationTypeId) }}
                </span>
              </td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400 hidden lg:table-cell">{{ loc.description ?? '—' }}</td>
              <td class="px-4 py-3">
                <span
                  class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
                  :class="loc.isActive
                    ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400'
                    : 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400'"
                >
                  {{ loc.isActive ? 'Aktif' : 'Pasif' }}
                </span>
              </td>
              <td v-if="canManage" class="px-4 py-3 text-right">
                <button @click="openEditModal(loc)" class="text-xs text-blue-600 dark:text-blue-400 hover:underline">
                  Düzenle
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        </div>
        <div v-else class="text-center py-12 text-gray-400 dark:text-gray-500">Henüz adres kaydı yok.</div>
      </template>

      <!-- Pagination -->
      <div v-if="totalCount > pageSize" class="flex items-center justify-between px-4 py-3 border-t border-gray-100 dark:border-white/10">
        <p class="text-xs text-gray-500 dark:text-gray-400">
          {{ totalCount }} adres · Sayfa {{ page }} / {{ Math.ceil(totalCount / pageSize) }}
        </p>
        <div class="flex gap-2">
          <button :disabled="page === 1" @click="changePage(page-1)"
            class="px-3 py-1 text-xs rounded border border-gray-300 dark:border-white/20 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
            ‹ Önceki
          </button>
          <button :disabled="page * pageSize >= totalCount" @click="changePage(page+1)"
            class="px-3 py-1 text-xs rounded border border-gray-300 dark:border-white/20 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
            Sonraki ›
          </button>
        </div>
      </div>
    </div>
  </div>

  <!-- ── Toplu Oluşturma Modal ────────────────────────────────────────────── -->
  <BaseModal :show="showBulkModal" title="Toplu Adres Oluştur" maxWidth="md" @close="showBulkModal = false">
    <div class="space-y-4">
      <div class="p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg text-sm text-blue-800 dark:text-blue-300">
        <span class="font-semibold">Önizleme:</span>
        <span class="font-mono ml-1">{{ bulkPreviewCode }}</span>
        <span class="ml-2 text-blue-600 dark:text-blue-400 font-medium">({{ bulkPreviewCount }} adres)</span>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Koridor</label>
          <select v-model.number="bulk.koridorNo"
            class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500">
            <option v-for="k in [1,2,3,4]" :key="k" :value="k">{{ k }}. Koridor</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Taraf</label>
          <select v-model="bulk.taraf"
            class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500">
            <option value="K">Kuzey (K)</option>
            <option value="G">Güney (G)</option>
          </select>
        </div>
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Modül Aralığı</label>
        <div class="flex items-center gap-2">
          <input v-model.number="bulk.modulFrom" type="number" min="1" placeholder="Başlangıç"
            class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500" />
          <span class="text-gray-400 flex-shrink-0">–</span>
          <input v-model.number="bulk.modulTo" type="number" min="1" placeholder="Bitiş"
            class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500" />
        </div>
        <p class="text-xs text-gray-400 mt-1">Örn: 1 – 20 → 001, 002 … 020</p>
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Kat Aralığı</label>
        <div class="flex items-center gap-2">
          <input v-model.number="bulk.katFrom" type="number" min="1" placeholder="Alt kat"
            class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500" />
          <span class="text-gray-400 flex-shrink-0">–</span>
          <input v-model.number="bulk.katTo" type="number" min="1" placeholder="Üst kat"
            class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500" />
        </div>
        <p class="text-xs text-gray-400 mt-1">Örn: 1 – 5 → kat 01, 02, 03, 04, 05</p>
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Raf Tipi</label>
        <select v-model.number="bulk.locationType"
          class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500">
          <option v-for="t in locationTypes" :key="t.id" :value="t.id">{{ t.label }}</option>
        </select>
      </div>
    </div>
    <template #footer>
      <button @click="showBulkModal = false"
        class="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">İptal</button>
      <button @click="submitBulk" :disabled="bulkSubmitting || bulkPreviewCount === 0"
        class="flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-50 rounded-lg">
        <span v-if="bulkSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
        {{ bulkSubmitting ? 'Oluşturuluyor...' : `${bulkPreviewCount} Adres Oluştur` }}
      </button>
    </template>
  </BaseModal>

  <!-- ── Tekli Ekleme / Düzenleme Modal ──────────────────────────────────── -->
  <BaseModal :show="showFormModal" :title="editTarget ? 'Adres Düzenle' : 'Tekli Adres Ekle'" maxWidth="md" @close="showFormModal = false">
    <div class="space-y-4">
      <template v-if="!editTarget">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Koridor</label>
            <select v-model.number="form.koridorNo"
              class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500">
              <option v-for="k in [1,2,3,4]" :key="k" :value="k">{{ k }}</option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Taraf</label>
            <select v-model="form.taraf"
              class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500">
              <option value="K">Kuzey (K)</option>
              <option value="G">Güney (G)</option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Modül No</label>
            <input v-model.number="form.modulNo" type="number" min="1"
              class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500" />
          </div>
          <div>
            <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Kat</label>
            <input v-model.number="form.kat" type="number" min="1"
              class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500" />
          </div>
        </div>
        <div class="text-sm font-mono font-semibold text-blue-700 dark:text-blue-300 px-1">
          → {{ singlePreviewCode }}
        </div>
      </template>

      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Raf Tipi</label>
        <select v-model.number="form.locationType"
          class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500">
          <option v-for="t in locationTypes" :key="t.id" :value="t.id">{{ t.label }}</option>
        </select>
      </div>

      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Açıklama (isteğe bağlı)</label>
        <input v-model="form.description" type="text"
          class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-white/20 bg-white dark:bg-white/5 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-brand-500" />
      </div>

      <div v-if="editTarget" class="flex items-center gap-2">
        <input type="checkbox" v-model="form.isActive" id="isActiveChk" class="rounded" />
        <label for="isActiveChk" class="text-sm text-gray-700 dark:text-gray-300">Aktif</label>
      </div>
    </div>
    <template #footer>
      <button @click="showFormModal = false"
        class="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">İptal</button>
      <button @click="submitForm" :disabled="formSubmitting"
        class="flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-50 rounded-lg">
        <span v-if="formSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
        {{ editTarget ? 'Kaydet' : 'Ekle' }}
      </button>
    </template>
  </BaseModal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue';
import { PlusIcon } from '@heroicons/vue/24/outline';
import warehouseLocationService, { type WarehouseLocation } from '../services/warehouseLocationService';
import { useNotificationStore } from '../stores/notification';
import { useAuthStore } from '../stores/auth';
import { ApiErrorUtils } from '../utils/apiError';
import BaseModal from '../components/BaseModal.vue';

const notify    = useNotificationStore();
const authStore = useAuthStore();
const canManage = computed(() => ['Admin', 'Manager'].includes(authStore.userRole));

const locationTypes = [
  { id: 0, label: 'Raf' },
  { id: 1, label: 'Zemin İstifleme' },
  { id: 2, label: 'Giriş' },
  { id: 3, label: 'Çıkış' },
  { id: 4, label: 'Karantina' },
  { id: 5, label: 'Hazırlama Alanı' },
];
function locationTypeLabel(id: number) {
  return locationTypes.find(t => t.id === id)?.label ?? String(id);
}

// ── State ──────────────────────────────────────────────────────────────────
const items      = ref<WarehouseLocation[]>([]);
const loading    = ref(false);
const error      = ref<string | null>(null);
const totalCount = ref(0);
const page       = ref(1);
const pageSize   = 50;

const filterKoridor  = ref<number | ''>('');
const filterTaraf    = ref('');
const filterType     = ref<number | ''>('');
const filterInactive = ref(false);

const stats = computed(() => ({
  total:        totalCount.value,
  koridorCount: new Set(items.value.map(l => l.koridorNo)).size,
  active:       items.value.filter(l =>  l.isActive).length,
  inactive:     items.value.filter(l => !l.isActive).length,
}));

async function load(resetPage = false) {
  if (resetPage) page.value = 1;
  loading.value = true;
  try {
    const res = await warehouseLocationService.getAll({
      koridorNo:       filterKoridor.value !== '' ? (filterKoridor.value as number) : undefined,
      taraf:           filterTaraf.value   || undefined,
      type:            filterType.value    !== '' ? (filterType.value as number)    : undefined,
      includeInactive: filterInactive.value,
      page:            page.value,
      pageSize,
    });
    items.value      = res.items;
    totalCount.value = res.totalCount;
  } catch (err) {
    error.value = ApiErrorUtils.getErrorMessage(err) || 'Adres listesi yüklenemedi.';
    notify.add(error.value, 'error');
  } finally {
    loading.value = false;
  }
}

function changePage(p: number) {
  page.value = p;
  load();
}

// ── Bulk create ────────────────────────────────────────────────────────────
const showBulkModal  = ref(false);
const bulkSubmitting = ref(false);

const bulk = reactive({
  koridorNo:    1,
  taraf:        'K',
  modulFrom:    1,
  modulTo:      10,
  katFrom:      1,
  katTo:        5,
  locationType: 0,
});

const bulkPreviewCount = computed(() => {
  if (bulk.modulTo < bulk.modulFrom || bulk.katTo < bulk.katFrom) return 0;
  return (bulk.modulTo - bulk.modulFrom + 1) * (bulk.katTo - bulk.katFrom + 1);
});

const bulkPreviewCode = computed(() => {
  const first = `${bulk.koridorNo}${bulk.taraf}-${String(bulk.modulFrom).padStart(3,'0')}-${String(bulk.katFrom).padStart(2,'0')}`;
  const last  = `${bulk.koridorNo}${bulk.taraf}-${String(bulk.modulTo).padStart(3,'0')}-${String(bulk.katTo).padStart(2,'0')}`;
  return `${first} … ${last}`;
});

async function submitBulk() {
  bulkSubmitting.value = true;
  try {
    const res = await warehouseLocationService.bulkCreate({ ...bulk });
    notify.add(`${res.created} adres oluşturuldu${res.skipped > 0 ? `, ${res.skipped} zaten vardı` : ''}.`, 'success');
    showBulkModal.value = false;
    await load(true);
  } catch {
    notify.add('Toplu oluşturma başarısız.', 'error');
  } finally {
    bulkSubmitting.value = false;
  }
}

// ── Single create / edit ───────────────────────────────────────────────────
const showFormModal  = ref(false);
const formSubmitting = ref(false);
const editTarget     = ref<WarehouseLocation | null>(null);

const form = reactive({
  koridorNo:    1,
  taraf:        'K',
  modulNo:      1,
  kat:          1,
  locationType: 0,
  description:  '',
  isActive:     true,
});

const singlePreviewCode = computed(() =>
  `${form.koridorNo}${form.taraf}-${String(form.modulNo).padStart(3,'0')}-${String(form.kat).padStart(2,'0')}`
);

function openCreateModal() {
  editTarget.value = null;
  Object.assign(form, { koridorNo: 1, taraf: 'K', modulNo: 1, kat: 1, locationType: 0, description: '', isActive: true });
  showFormModal.value = true;
}

function openEditModal(loc: WarehouseLocation) {
  editTarget.value = loc;
  Object.assign(form, {
    koridorNo:    loc.koridorNo,
    taraf:        loc.taraf,
    modulNo:      loc.modulNo,
    kat:          loc.kat,
    locationType: loc.locationTypeId,
    description:  loc.description ?? '',
    isActive:     loc.isActive,
  });
  showFormModal.value = true;
}

async function submitForm() {
  formSubmitting.value = true;
  try {
    if (editTarget.value) {
      await warehouseLocationService.update(editTarget.value.id, {
        locationType: form.locationType,
        description:  form.description || undefined,
        isActive:     form.isActive,
      });
      notify.add('Adres güncellendi.', 'success');
    } else {
      await warehouseLocationService.create({
        koridorNo:    form.koridorNo,
        taraf:        form.taraf,
        modulNo:      form.modulNo,
        kat:          form.kat,
        locationType: form.locationType,
        description:  form.description || undefined,
      });
      notify.add('Adres eklendi.', 'success');
    }
    showFormModal.value = false;
    await load();
  } catch {
    notify.add('İşlem başarısız.', 'error');
  } finally {
    formSubmitting.value = false;
  }
}

onMounted(() => load());
</script>
