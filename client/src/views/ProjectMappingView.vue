<template>
  <div class="h-full flex flex-col p-4">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-200">Proje - Bölge Yönetimi</h1>
      <div class="flex gap-4 items-center">
           <input
              v-model="searchTerm"
              type="text"
              placeholder="Proje Ara..."
              class="border dark:border-gray-700 rounded-lg px-4 py-2 focus:ring-2 focus:ring-blue-500 outline-none w-64 dark:bg-gray-800 dark:text-gray-100"
            />
            <button 
              @click="exportToExcel" 
              class="flex items-center gap-2 px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg transition-colors shadow-sm text-sm font-medium"
              title="Excel'e Aktar"
            >
              <i class="fas fa-file-export"></i>
              Excel'e Aktar
            </button>
            <label class="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors cursor-pointer shadow-sm text-sm font-medium" title="Excel'den Yükle">
              <i class="fas fa-file-import"></i>
              Excel'den Yükle
              <input type="file" class="hidden" @change="importFromExcel" accept=".xlsx, .xls" />
            </label>
      </div>
    </div>

    <!-- Error -->
    <div v-if="error" class="mx-4 mt-4 p-3 bg-red-900/30 border border-red-700 rounded-lg flex items-center justify-between">
      <span class="text-red-400 text-sm">{{ error }}</span>
      <button @click="fetchZones(); fetchProjects(); error = null" class="text-red-400 hover:text-red-300 text-sm underline ml-4">Tekrar dene</button>
    </div>

    <!-- Projects List -->
    <div class="bg-white dark:bg-gray-900 rounded-lg shadow overflow-hidden flex-1 overflow-y-auto">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-800 sticky top-0 z-10">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Proje Kodu</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Proje Adı</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Bölge</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Netsis Cari Kodu</th>
            <th class="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider" title="Bölge içindeki teslimat sırası (küçük = önce)">Sıra</th>
            <th class="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider" title="Teslimat penceresi başlangıç saati">Pencere Başlangıç</th>
            <th class="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider" title="Teslimat penceresi bitiş saati">Pencere Bitiş</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="project in filteredProjects" :key="project.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">{{ project.code }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ project.name }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">
                <label :for="`zone-${project.id}`" class="sr-only">Bölge</label>
                <select
                    :id="`zone-${project.id}`"
                    :value="project.zoneId || ''"
                    @change="updateZone(project, $event)"
                    class="border-gray-300 dark:border-gray-700 rounded-md shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm p-1 dark:bg-gray-800 dark:text-gray-100"
                    :class="{'border-red-300 bg-red-50': !project.zoneId}"
                >
                    <option value="">-- Bölge Seç --</option>
                    <option v-for="zone in zones" :key="zone.id" :value="zone.id">
                        {{ zone.name }}
                    </option>
                </select>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-sm">
              <label :for="`cari-${project.id}`" class="sr-only">Netsis Cari Kodu</label>
              <input
                :id="`cari-${project.id}`"
                :value="project.netsisCariKodu || ''"
                type="text"
                placeholder="Cari Kodu..."
                class="border border-gray-300 dark:border-gray-700 rounded-md shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm p-1 w-36 dark:bg-gray-800 dark:text-gray-100"
                @blur="updateNetsisCariKodu(project, $event)"
                @keydown.enter="($event.target as HTMLInputElement).blur()"
              />
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-center text-sm">
              <label :for="`sira-${project.id}`" class="sr-only">Teslimat Sırası</label>
              <input
                :id="`sira-${project.id}`"
                :value="project.deliveryOrder ?? ''"
                type="number"
                min="1"
                placeholder="—"
                class="border border-gray-300 dark:border-gray-700 rounded-md shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm p-1 w-16 text-center dark:bg-gray-800 dark:text-gray-100"
                @blur="updateDeliveryOrder(project, $event)"
                @keydown.enter="($event.target as HTMLInputElement).blur()"
              />
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-center text-sm">
              <input
                :id="`win-start-${project.id}`"
                :value="project.deliveryWindowStart ?? ''"
                type="time"
                class="border border-gray-300 dark:border-gray-700 rounded-md shadow-sm sm:text-sm p-1 dark:bg-gray-800 dark:text-gray-100"
                @blur="updateDeliveryWindow(project, 'start', $event)"
              />
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-center text-sm">
              <input
                :id="`win-end-${project.id}`"
                :value="project.deliveryWindowEnd ?? ''"
                type="time"
                class="border border-gray-300 dark:border-gray-700 rounded-md shadow-sm sm:text-sm p-1 dark:bg-gray-800 dark:text-gray-100"
                @blur="updateDeliveryWindow(project, 'end', $event)"
              />
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm">
                <span v-if="project.zoneId" class="px-2 py-1 text-xs rounded-full bg-green-100 text-green-800">Eşleşti</span>
                <span v-else class="px-2 py-1 text-xs rounded-full bg-red-100 text-red-800">Tanımsız</span>
            </td>
          </tr>
          <tr v-if="filteredProjects.length === 0">
             <td colspan="5" class="px-6 py-4 text-center text-sm text-gray-500 dark:text-gray-400">Proje bulunamadı.</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import projectService from '../services/projectService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';

const notificationStore = useNotificationStore();

interface Zone {
  id: number;
  name: string;
}

interface Project {
  id: number;
  code: string;
  name: string;
  region?: string;
  zoneId: number | null;
  zoneName: string | null;
  netsisCariKodu?: string | null;
  deliveryOrder?: number | null;
  deliveryWindowStart?: string | null;
  deliveryWindowEnd?: string | null;
}

const zones = ref<Zone[]>([]);
const projects = ref<Project[]>([]);
const error = ref<string | null>(null);
const searchTerm = ref('');

const fetchZones = async () => {
    try {
        zones.value = await projectService.getZones();
    } catch (e) {
        error.value = ApiErrorUtils.getErrorMessage(e) || 'Bölgeler yüklenemedi.';
        notificationStore.add(error.value, 'error');
    }
};

const fetchProjects = async () => {
    try {
        projects.value = await projectService.getProjects();
    } catch (e) {
        error.value = ApiErrorUtils.getErrorMessage(e) || 'Projeler yüklenemedi.';
        notificationStore.add(error.value, 'error');
    }
};

const filteredProjects = computed(() => {
    if(!searchTerm.value) return projects.value;
    const term = searchTerm.value.toLowerCase();
    return projects.value.filter(p =>
        p.code.toLowerCase().includes(term) ||
        p.name.toLowerCase().includes(term)
    );
});

const updateNetsisCariKodu = async (project: Project, event: Event) => {
    const input = event.target as HTMLInputElement;
    const newValue = input.value.trim() || null;

    if (newValue === (project.netsisCariKodu ?? null)) return; // no change

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
    const oldEnd   = project.deliveryWindowEnd ?? null;

    const newStart = field === 'start' ? newValue : oldStart;
    const newEnd   = field === 'end'   ? newValue : oldEnd;

    // Validation: if both set, start must be < end
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
        project.deliveryWindowEnd   = oldEnd;
        input.value = (field === 'start' ? oldStart : oldEnd) ?? '';
        notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Güncelleme başarısız.', 'error');
    }
};

const updateZone = async (project: Project, event: Event) => {
    const target = event.target as HTMLSelectElement;
    const newZoneId = target.value ? Number(target.value) : null;

    // Optimistic UI update
    const oldZoneId = project.zoneId;
    project.zoneId = newZoneId;

    try {
        await projectService.assignZone(project.id, newZoneId as any); // Type cast as assigning null might be allowed or handled
    } catch (err) {
        project.zoneId = oldZoneId; // Revert
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

const importFromExcel = async (event: Event) => {
  const input = event.target as HTMLInputElement;
  if (!input.files?.length) return;

  const file = input.files[0];
  if (!file) return;
  
  try {
    const result = await projectService.importMappings(file);
    notificationStore.add(`${result.updatedCount} proje güncellendi.`, 'success');
    
    // Refresh lists
    await fetchProjects();
    
    // Clear input
    input.value = '';
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Yükleme başarısız.', 'error');
    input.value = '';
  }
};

onMounted(() => {
    fetchZones();
    fetchProjects();
});
</script>
