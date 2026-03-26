<template>
  <div class="h-full flex flex-col p-4">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-200">Bölge Yönetimi</h1>
      <button
        @click="showModal = true; isEdit = false; form = { name: '', order: 0 }"
        class="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition flex items-center gap-2"
      >
        <span>+ Yeni Bölge</span>
      </button>
    </div>

    <!-- Error -->
    <div v-if="error" class="mx-4 mt-4 p-3 bg-red-900/30 border border-red-700 rounded-lg flex items-center justify-between">
      <span class="text-red-400 text-sm">{{ error }}</span>
      <button @click="fetchZones(); error = null" class="text-red-400 hover:text-red-300 text-sm underline ml-4">Tekrar dene</button>
    </div>

    <!-- Zones List -->
    <div class="bg-white dark:bg-gray-900 rounded-lg shadow overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sıra</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Bölge Adı</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="zone in zones" :key="zone.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
            <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">{{ zone.order }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">{{ zone.name }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button @click="openEdit(zone)" class="text-indigo-600 hover:text-indigo-900 mr-4">Düzenle</button>
              <button @click="confirmDelete(zone)" class="text-red-600 hover:text-red-900">Sil</button>
            </td>
          </tr>
          <tr v-if="zones.length === 0">
             <td colspan="3" class="px-6 py-4 text-center text-sm text-gray-500 dark:text-gray-400">Henüz bölge tanımlanmamış.</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal -->
    <div v-if="showModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div class="bg-white dark:bg-gray-900 rounded-lg p-6 w-full max-w-md">
        <h3 class="text-lg font-bold mb-4 dark:text-gray-100">{{ isEdit ? 'Bölgeyi Düzenle' : 'Yeni Bölge Ekle' }}</h3>

        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Bölge Adı</label>
            <input v-model="form.name" type="text" class="w-full border dark:border-gray-700 p-2 rounded focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-800 dark:text-gray-100" placeholder="Örn: A Koridoru" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Sıralama</label>
            <input v-model.number="form.order" type="number" class="w-full border dark:border-gray-700 p-2 rounded focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-800 dark:text-gray-100" />
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplama rotasında hangi sırada gidileceğini belirler (Küçükten büyüğe).</p>
          </div>
        </div>

        <div class="mt-6 flex justify-end gap-3">
          <button @click="showModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
          <button @click="saveZone" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">Kaydet</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import projectService from '../services/projectService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';

const notificationStore = useNotificationStore();

interface Zone {
  id: number;
  name: string;
  order: number;
}

const zones = ref<Zone[]>([]);
const error = ref<string | null>(null);
const showModal = ref(false);
const isEdit = ref(false);
const form = ref({ name: '', order: 0 });
const currentId = ref<number | null>(null);

const fetchZones = async () => {
  try {
    zones.value = await projectService.getZones();
  } catch (err) {
    error.value = ApiErrorUtils.getErrorMessage(err) || 'Bölgeler yüklenirken hata oluştu.';
    notificationStore.add(error.value, 'error');
  }
};

const openEdit = (zone: Zone) => {
    isEdit.value = true;
    currentId.value = zone.id;
    form.value = { name: zone.name, order: zone.order };
    showModal.value = true;
};

const saveZone = async () => {
    if (!form.value.name) return notificationStore.add('Bölge adı zorunludur', 'warning');

    try {
        if (isEdit.value && currentId.value) {
            await projectService.updateZone(currentId.value, {
                id: currentId.value,
                name: form.value.name,
                order: form.value.order
            });
        } else {
            await projectService.createZone({
                name: form.value.name,
                order: form.value.order
            });
        }
        showModal.value = false;
        fetchZones();
        notificationStore.add('Bölge kaydedildi.', 'success');
    } catch (err) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Kaydetme hatası.', 'error');
    }
};

const confirmDelete = async (zone: Zone) => {
    const ok = await notificationStore.promptConfirm({ title: 'Bölgeyi Sil', message: `"${zone.name}" bölgesini silmek istediğinize emin misiniz?`, confirmText: 'Sil', type: 'danger' });
    if (!ok) return;
    try {
        await projectService.deleteZone(zone.id);
        fetchZones();
        notificationStore.add('Bölge silindi.', 'success');
    } catch (err) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Silme hatası.', 'error');
    }
};

onMounted(fetchZones);
</script>
