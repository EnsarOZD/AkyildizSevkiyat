<template>
  <div class="h-full flex flex-col p-4">
    <PageHeader title="Proje - Bölge Yönetimi" subtitle="Proje ve bölge eşleşmelerini yönetin" color="purple" class="mb-6">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 4a2 2 0 114 0v1a1 1 0 001 1h3a1 1 0 011 1v3a1 1 0 01-1 1h-1a2 2 0 100 4h1a1 1 0 011 1v3a1 1 0 01-1 1h-3a1 1 0 01-1-1v-1a2 2 0 10-4 0v1a1 1 0 01-1 1H7a1 1 0 01-1-1v-3a1 1 0 00-1-1H4a2 2 0 110-4h1a1 1 0 001-1V7a1 1 0 011-1h3a1 1 0 001-1V4z" />
        </svg>
      </template>
      <template #actions>
        <div class="flex flex-wrap gap-2">
          <BaseButton @click="exportToExcel" variant="success">Excel'e Aktar</BaseButton>
          <button @click="downloadTemplate" class="px-3 py-2 bg-gray-600 hover:bg-gray-700 text-white rounded-lg text-sm font-medium">Şablon İndir</button>
          <label class="px-3 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-medium cursor-pointer">
            Excel'den Yükle
            <input type="file" class="hidden" @change="importFromExcel" accept=".xlsx, .xls" />
          </label>
        </div>
      </template>
    </PageHeader>

    <!-- Filters -->
    <div class="flex flex-wrap gap-3 mb-4">
      <input
        v-model="searchTerm"
        type="text"
        placeholder="Proje Ara..."
        class="flex-1 min-w-[200px] border dark:border-gray-700 rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500 outline-none dark:bg-gray-800 dark:text-gray-100"
        @input="onSearch"
      />
      <label class="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 cursor-pointer select-none">
        <input type="checkbox" v-model="showInactive" @change="onSearch" class="rounded" />
        Pasif projeleri göster
      </label>
    </div>

    <!-- Error -->
    <div v-if="error" class="mb-4 p-3 bg-red-900/30 border border-red-700 rounded-lg flex items-center justify-between">
      <span class="text-red-400 text-sm">{{ error }}</span>
      <button @click="fetchProjects(); error = null" class="text-red-400 hover:text-red-300 text-sm underline ml-4">Tekrar dene</button>
    </div>

    <!-- Projects List -->
    <div class="bg-white dark:bg-gray-900 rounded-lg shadow overflow-hidden flex-1 overflow-y-auto">
      <div v-if="loading" class="flex items-center justify-center py-16 text-gray-400 text-sm gap-2">
        <span class="animate-spin h-4 w-4 border-2 border-blue-500 border-t-transparent rounded-full"></span>
        Yükleniyor...
      </div>
      <table v-else class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-800 sticky top-0 z-10">
          <tr>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Proje Kodu</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Proje Adı</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Bölge</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Netsis Fatura Cari</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Netsis Teslim Cari</th>
            <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider" title="Teslimat sırası">Sıra</th>
            <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Pencere Başlangıç</th>
            <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Pencere Bitiş</th>
            <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
            <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlem</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
          <tr
            v-for="project in projects" :key="project.id"
            class="hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
            :class="!project.isActive ? 'opacity-60' : ''"
          >
            <td class="px-4 py-3 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">{{ project.code }}</td>
            <td class="px-4 py-3 text-sm text-gray-500 dark:text-gray-400 max-w-[200px] truncate" :title="project.name">{{ project.name }}</td>
            <td class="px-4 py-3 whitespace-nowrap text-sm">
              <select
                :value="project.zoneId || ''"
                @change="updateZone(project, $event)"
                :disabled="!project.isActive"
                class="border-gray-300 dark:border-gray-700 rounded-md shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm p-1 dark:bg-gray-800 dark:text-gray-100 disabled:opacity-50"
                :class="{'border-red-300 bg-red-50': !project.zoneId && project.isActive}"
              >
                <option value="">-- Bölge Seç --</option>
                <option v-for="zone in zones" :key="zone.id" :value="zone.id">{{ zone.name }}</option>
              </select>
            </td>
            <td class="px-4 py-3 whitespace-nowrap text-sm">
              <input
                :value="project.netsisCariKodu || ''"
                type="text"
                placeholder="120.01.001"
                :disabled="!project.isActive"
                class="border border-gray-300 dark:border-gray-700 rounded-md shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm p-1 w-32 dark:bg-gray-800 dark:text-gray-100 disabled:opacity-50"
                @blur="updateNetsisCariKodu(project, $event)"
                @keydown.enter="($event.target as HTMLInputElement).blur()"
              />
            </td>
            <td class="px-4 py-3 whitespace-nowrap text-sm">
              <input
                :value="project.netsisTeslimCariKodu || ''"
                type="text"
                placeholder="10029"
                :disabled="!project.isActive"
                class="border border-gray-300 dark:border-gray-700 rounded-md shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm p-1 w-28 dark:bg-gray-800 dark:text-gray-100 disabled:opacity-50"
                @blur="updateNetsisTeslimCariKodu(project, $event)"
                @keydown.enter="($event.target as HTMLInputElement).blur()"
              />
            </td>
            <td class="px-4 py-3 whitespace-nowrap text-center text-sm">
              <input
                :value="project.deliveryOrder ?? ''"
                type="number"
                min="1"
                placeholder="—"
                :disabled="!project.isActive"
                class="border border-gray-300 dark:border-gray-700 rounded-md shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm p-1 w-16 text-center dark:bg-gray-800 dark:text-gray-100 disabled:opacity-50"
                @blur="updateDeliveryOrder(project, $event)"
                @keydown.enter="($event.target as HTMLInputElement).blur()"
              />
            </td>
            <td class="px-4 py-3 whitespace-nowrap text-center text-sm">
              <input
                :value="project.deliveryWindowStart ?? ''"
                type="time"
                :disabled="!project.isActive"
                class="border border-gray-300 dark:border-gray-700 rounded-md shadow-sm sm:text-sm p-1 dark:bg-gray-800 dark:text-gray-100 disabled:opacity-50"
                @blur="updateDeliveryWindow(project, 'start', $event)"
              />
            </td>
            <td class="px-4 py-3 whitespace-nowrap text-center text-sm">
              <input
                :value="project.deliveryWindowEnd ?? ''"
                type="time"
                :disabled="!project.isActive"
                class="border border-gray-300 dark:border-gray-700 rounded-md shadow-sm sm:text-sm p-1 dark:bg-gray-800 dark:text-gray-100 disabled:opacity-50"
                @blur="updateDeliveryWindow(project, 'end', $event)"
              />
            </td>
            <td class="px-4 py-3 whitespace-nowrap text-center text-sm">
              <span v-if="!project.isActive" class="px-2 py-1 text-xs rounded-full bg-gray-100 text-gray-500 dark:bg-gray-700 dark:text-gray-400">Pasif</span>
              <span v-else-if="project.zoneId" class="px-2 py-1 text-xs rounded-full bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300">Eşleşti</span>
              <span v-else class="px-2 py-1 text-xs rounded-full bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300">Tanımsız</span>
            </td>
            <td class="px-4 py-3 whitespace-nowrap text-right text-sm">
              <div class="flex items-center justify-end gap-1">
                <button
                  v-if="project.isActive"
                  @click="confirmToggle(project, false)"
                  class="px-2 py-1 text-xs rounded border border-amber-300 text-amber-700 hover:bg-amber-50 dark:border-amber-600 dark:text-amber-400 dark:hover:bg-amber-900/20 transition"
                  title="Pasife Al"
                >Pasife Al</button>
                <button
                  v-else
                  @click="confirmToggle(project, true)"
                  class="px-2 py-1 text-xs rounded border border-green-400 text-green-700 hover:bg-green-50 dark:border-green-600 dark:text-green-400 dark:hover:bg-green-900/20 transition"
                  title="Aktife Al"
                >Aktife Al</button>
              </div>
            </td>
          </tr>
          <tr v-if="!loading && projects.length === 0">
            <td colspan="10" class="px-6 py-8 text-center text-sm text-gray-500 dark:text-gray-400">Proje bulunamadı.</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="flex items-center justify-between mt-4 text-sm text-gray-600 dark:text-gray-400">
      <span>Toplam {{ totalCount }} proje</span>
      <div class="flex items-center gap-1">
        <button
          @click="goToPage(currentPage - 1)"
          :disabled="currentPage === 1"
          class="px-3 py-1.5 rounded border border-gray-300 dark:border-gray-600 disabled:opacity-40 hover:bg-gray-100 dark:hover:bg-gray-700 transition"
        >‹ Önceki</button>
        <template v-for="p in pageButtons" :key="p">
          <span v-if="p === '...'" class="px-2">…</span>
          <button
            v-else
            @click="goToPage(p as number)"
            class="px-3 py-1.5 rounded border transition"
            :class="p === currentPage
              ? 'bg-blue-600 text-white border-blue-600'
              : 'border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700'"
          >{{ p }}</button>
        </template>
        <button
          @click="goToPage(currentPage + 1)"
          :disabled="currentPage === totalPages"
          class="px-3 py-1.5 rounded border border-gray-300 dark:border-gray-600 disabled:opacity-40 hover:bg-gray-100 dark:hover:bg-gray-700 transition"
        >Sonraki ›</button>
      </div>
    </div>
    <div v-else-if="totalCount > 0" class="mt-3 text-xs text-gray-400 text-right">Toplam {{ totalCount }} proje</div>

    <!-- Confirm Modal -->
    <BaseModal :show="!!confirmTarget" title="Onay" maxWidth="sm" @close="confirmTarget = null">
      <p class="text-sm text-gray-700 dark:text-gray-300">
        <strong>{{ confirmTarget?.project?.name }}</strong> projesini
        <strong>{{ confirmTarget?.isActive ? 'aktife' : 'pasife' }}</strong> almak istediğinize emin misiniz?
      </p>
      <template #footer>
        <button @click="confirmTarget = null" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button
          @click="doToggle"
          class="px-4 py-2 text-white rounded font-bold transition"
          :class="confirmTarget?.isActive ? 'bg-green-600 hover:bg-green-700' : 'bg-amber-500 hover:bg-amber-600'"
        >{{ confirmTarget?.isActive ? 'Aktife Al' : 'Pasife Al' }}</button>
      </template>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import BaseButton from '../components/BaseButton.vue';
import BaseModal from '../components/BaseModal.vue';
import projectService from '../services/projectService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import * as XLSX from 'xlsx';

const notificationStore = useNotificationStore();

interface Zone { id: number; name: string; }
interface Project {
  id: number; code: string; name: string; region?: string;
  zoneId: number | null; zoneName: string | null; isActive: boolean;
  netsisCariKodu?: string | null; netsisTeslimCariKodu?: string | null;
  deliveryOrder?: number | null;
  deliveryWindowStart?: string | null; deliveryWindowEnd?: string | null;
}

const zones = ref<Zone[]>([]);
const projects = ref<Project[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);
const searchTerm = ref('');
const showInactive = ref(false);

const currentPage = ref(1);
const totalPages = ref(1);
const totalCount = ref(0);
const pageSize = 50;

let searchDebounce: ReturnType<typeof setTimeout> | null = null;

const confirmTarget = ref<{ project: Project; isActive: boolean } | null>(null);

const pageButtons = computed(() => {
  const pages: (number | string)[] = [];
  const total = totalPages.value;
  const cur = currentPage.value;
  if (total <= 7) {
    for (let i = 1; i <= total; i++) pages.push(i);
  } else {
    pages.push(1);
    if (cur > 3) pages.push('...');
    for (let i = Math.max(2, cur - 1); i <= Math.min(total - 1, cur + 1); i++) pages.push(i);
    if (cur < total - 2) pages.push('...');
    pages.push(total);
  }
  return pages;
});

const fetchZones = async () => {
  try { zones.value = await projectService.getZones(); } catch (e) { console.error(e); }
};

const fetchProjects = async (page = currentPage.value) => {
  loading.value = true;
  try {
    const result = await projectService.getProjects({
      pageNumber: page,
      pageSize,
      search: searchTerm.value || undefined,
      showInactive: showInactive.value,
    });
    projects.value = result.items;
    currentPage.value = result.pageIndex;
    totalPages.value = result.totalPages;
    totalCount.value = result.totalCount;
  } catch (e) {
    error.value = ApiErrorUtils.getErrorMessage(e) || 'Projeler yüklenemedi.';
    notificationStore.add(error.value, 'error');
  } finally {
    loading.value = false;
  }
};

const onSearch = () => {
  if (searchDebounce) clearTimeout(searchDebounce);
  searchDebounce = setTimeout(() => fetchProjects(1), 300);
};

const goToPage = (page: number) => {
  if (page < 1 || page > totalPages.value) return;
  fetchProjects(page);
};

const confirmToggle = (project: Project, isActive: boolean) => {
  confirmTarget.value = { project, isActive };
};

const doToggle = async () => {
  if (!confirmTarget.value) return;
  const { project, isActive } = confirmTarget.value;
  confirmTarget.value = null;
  try {
    await projectService.toggleProjectActive(project.id, isActive);
    project.isActive = isActive;
    notificationStore.add(`Proje ${isActive ? 'aktife' : 'pasife'} alındı.`, 'success');
    if (!showInactive.value && !isActive) {
      await fetchProjects();
    }
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'İşlem başarısız.', 'error');
  }
};

const updateNetsisCariKodu = async (project: Project, event: Event) => {
  const input = event.target as HTMLInputElement;
  const newValue = input.value.trim() || null;
  if (newValue === (project.netsisCariKodu ?? null)) return;
  const old = project.netsisCariKodu;
  project.netsisCariKodu = newValue;
  try {
    await projectService.updateNetsisCariKodu(project.id, newValue);
  } catch (err) {
    project.netsisCariKodu = old;
    input.value = old ?? '';
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Güncelleme başarısız.', 'error');
  }
};

const updateNetsisTeslimCariKodu = async (project: Project, event: Event) => {
  const input = event.target as HTMLInputElement;
  const newValue = input.value.trim() || null;
  if (newValue === (project.netsisTeslimCariKodu ?? null)) return;
  const old = project.netsisTeslimCariKodu;
  project.netsisTeslimCariKodu = newValue;
  try {
    await projectService.updateNetsisCariKodu(project.id, project.netsisCariKodu ?? null, newValue);
  } catch (err) {
    project.netsisTeslimCariKodu = old;
    input.value = old ?? '';
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Güncelleme başarısız.', 'error');
  }
};

const updateDeliveryOrder = async (project: Project, event: Event) => {
  const input = event.target as HTMLInputElement;
  const raw = input.value.trim();
  const newValue = raw === '' ? null : parseInt(raw, 10);
  if (isNaN(newValue as number) && newValue !== null) return;
  if (newValue === (project.deliveryOrder ?? null)) return;
  const old = project.deliveryOrder;
  project.deliveryOrder = newValue;
  try {
    await projectService.updateDeliveryOrder(project.id, newValue);
  } catch (err) {
    project.deliveryOrder = old;
    input.value = old != null ? String(old) : '';
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Güncelleme başarısız.', 'error');
  }
};

const updateDeliveryWindow = async (project: Project, field: 'start' | 'end', event: Event) => {
  const input = event.target as HTMLInputElement;
  const newValue = input.value || null;
  const oldStart = project.deliveryWindowStart ?? null;
  const oldEnd = project.deliveryWindowEnd ?? null;
  const newStart = field === 'start' ? newValue : oldStart;
  const newEnd = field === 'end' ? newValue : oldEnd;
  if (newStart && newEnd && newStart >= newEnd) {
    notificationStore.add('Başlangıç saati bitiş saatinden önce olmalıdır.', 'error');
    input.value = (field === 'start' ? oldStart : oldEnd) ?? '';
    return;
  }
  if (field === 'start') project.deliveryWindowStart = newValue;
  else project.deliveryWindowEnd = newValue;
  try {
    await projectService.updateDeliveryWindow(project.id, newStart, newEnd);
  } catch (err) {
    project.deliveryWindowStart = oldStart;
    project.deliveryWindowEnd = oldEnd;
    input.value = (field === 'start' ? oldStart : oldEnd) ?? '';
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Güncelleme başarısız.', 'error');
  }
};

const updateZone = async (project: Project, event: Event) => {
  const target = event.target as HTMLSelectElement;
  const newZoneId = target.value ? Number(target.value) : null;
  const oldZoneId = project.zoneId;
  project.zoneId = newZoneId;
  try {
    await projectService.assignZone(project.id, newZoneId as any);
  } catch (err) {
    project.zoneId = oldZoneId;
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Güncelleme başarısız.', 'error');
  }
};

const exportToExcel = async () => {
  try {
    await projectService.exportMappings();
    notificationStore.add('Excel dosyası indiriliyor...', 'success');
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Excel\'e aktarma başarısız.', 'error');
  }
};

const downloadTemplate = () => {
  const ws = XLSX.utils.aoa_to_sheet([
    ['Proje Kodu', 'Proje Adı', 'Bölge', 'Netsis Cari Kodu', 'Netsis Teslim Cari Kodu', 'Teslimat Sırası'],
    ['PRJ-001', 'Örnek Proje Adı', 'Marmara', '120.01.001', '10029', '1']
  ]);
  ws['!cols'] = [{ wch: 20 }, { wch: 40 }, { wch: 15 }, { wch: 20 }, { wch: 20 }, { wch: 15 }];
  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, 'Şablon');
  XLSX.writeFile(wb, 'Proje_Yükleme_Şablonu.xlsx');
};

const importFromExcel = async (event: Event) => {
  const input = event.target as HTMLInputElement;
  if (!input.files?.length) return;
  const file = input.files[0];
  if (!file) return;
  try {
    const result = await projectService.importMappings(file);
    notificationStore.add(`${result.updatedCount} proje güncellendi.`, 'success');
    await fetchProjects(1);
    input.value = '';
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Yükleme başarısız.', 'error');
    input.value = '';
  }
};

onMounted(() => {
  fetchZones();
  fetchProjects(1);
});
</script>
