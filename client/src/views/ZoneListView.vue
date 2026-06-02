<template>
  <div class="h-full flex flex-col p-4">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-200">Bölge Yönetimi</h1>
      <BaseButton @click="showModal = true; isEdit = false; form = { name: '', isOutOfCity: false }" variant="primary">
        + Yeni Bölge
      </BaseButton>
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
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Bölge Adı</th>
            <th class="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Şehir Dışı</th>
            <th class="px-6 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="zone in zones" :key="zone.id" class="hover:bg-gray-50 dark:hover:bg-gray-800" :class="{ 'opacity-50': !zone.isActive }">
            <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">{{ zone.name }}</td>
            <td class="px-6 py-4 whitespace-nowrap text-center">
              <span v-if="zone.isOutOfCity" class="text-xs font-bold bg-teal-100 dark:bg-teal-900/30 text-teal-700 dark:text-teal-400 px-2 py-0.5 rounded-full">Evet</span>
              <span v-else class="text-xs text-gray-400 dark:text-gray-600">—</span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-center">
              <span v-if="zone.isActive" class="text-xs font-bold bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 px-2 py-0.5 rounded-full">Aktif</span>
              <span v-else class="text-xs font-bold bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-300 px-2 py-0.5 rounded-full">Pasif</span>
            </td>
            <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
              <button @click="openEdit(zone)" class="text-indigo-600 hover:text-indigo-900 mr-4">Düzenle</button>
              <button v-if="zone.isActive" @click="toggleActive(zone, false)" class="text-amber-600 hover:text-amber-800 mr-4">Pasife Al</button>
              <button v-else @click="toggleActive(zone, true)" class="text-green-600 hover:text-green-800 mr-4">Aktif Et</button>
              <button @click="confirmDelete(zone)" class="text-red-600 hover:text-red-900">Sil</button>
            </td>
          </tr>
          <tr v-if="zones.length === 0">
             <td colspan="4" class="px-6 py-4 text-center text-sm text-gray-500 dark:text-gray-400">Henüz bölge tanımlanmamış.</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal -->
    <BaseModal :show="showModal" :title="isEdit ? 'Bölgeyi Düzenle' : 'Yeni Bölge Ekle'" maxWidth="sm" @close="showModal = false">
      <div class="space-y-4">
        <BaseInput v-model="form.name" label="Bölge Adı" placeholder="Örn: A Koridoru" />
        <label class="flex items-center gap-3 cursor-pointer select-none">
          <input type="checkbox" v-model="form.isOutOfCity" class="w-4 h-4 rounded border-gray-300 text-teal-600 focus:ring-teal-500" />
          <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Şehir Dışı Bölge</span>
          <span class="text-xs text-gray-400 dark:text-gray-500">(Micro/Macro yerine tek toplu hazırlık ekranı)</span>
        </label>
      </div>
      <template #footer>
        <BaseButton @click="showModal = false" variant="secondary">İptal</BaseButton>
        <BaseButton @click="saveZone" variant="primary">Kaydet</BaseButton>
      </template>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import projectService from '../services/projectService';
import { ApiErrorUtils, isApiError } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import BaseModal from '../components/BaseModal.vue';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/base/BaseInput.vue';

const notificationStore = useNotificationStore();

interface Zone {
  id: number;
  name: string;
  isOutOfCity: boolean;
  isActive: boolean;
}

const zones = ref<Zone[]>([]);
const error = ref<string | null>(null);
const showModal = ref(false);
const isEdit = ref(false);
const form = ref({ name: '', isOutOfCity: false });
const currentId = ref<number | null>(null);

const fetchZones = async () => {
  try {
    // Yönetim ekranı pasif bölgeleri de gösterir
    zones.value = await projectService.getZones(true);
  } catch (err) {
    error.value = ApiErrorUtils.getErrorMessage(err) || 'Bölgeler yüklenirken hata oluştu.';
    notificationStore.add(error.value, 'error');
  }
};

const openEdit = (zone: Zone) => {
    isEdit.value = true;
    currentId.value = zone.id;
    form.value = { name: zone.name, isOutOfCity: zone.isOutOfCity };
    showModal.value = true;
};

const saveZone = async () => {
    if (!form.value.name) return notificationStore.add('Bölge adı zorunludur', 'warning');
    try {
        if (isEdit.value && currentId.value) {
            await projectService.updateZone(currentId.value, {
                id: currentId.value,
                name: form.value.name,
                order: 0,
                isOutOfCity: form.value.isOutOfCity
            });
        } else {
            await projectService.createZone({
                name: form.value.name,
                order: 0,
                isOutOfCity: form.value.isOutOfCity
            });
        }
        showModal.value = false;
        fetchZones();
        notificationStore.add('Bölge kaydedildi.', 'success');
    } catch (err) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Kaydetme hatası.', 'error');
    }
};

const toggleActive = async (zone: Zone, isActive: boolean) => {
    if (isActive === false) {
        const ok = await notificationStore.promptConfirm({
            title: 'Bölgeyi Pasife Al',
            message: `"${zone.name}" bölgesi pasife alınacak. Pasif bölgeler seçim listelerinde görünmez ancak geçmiş kayıtlar korunur. İstediğinizde tekrar aktif edebilirsiniz.`,
            confirmText: 'Pasife Al', type: 'warning'
        });
        if (!ok) return;
    }
    try {
        await projectService.setZoneActive(zone.id, isActive);
        fetchZones();
        notificationStore.add(isActive ? 'Bölge yeniden aktif edildi.' : 'Bölge pasife alındı.', 'success');
    } catch (err) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'İşlem başarısız.', 'error');
    }
};

const confirmDelete = async (zone: Zone) => {
    const ok = await notificationStore.promptConfirm({ title: 'Bölgeyi Sil', message: `"${zone.name}" bölgesini kalıcı olarak silmek istediğinize emin misiniz?`, confirmText: 'Sil', type: 'danger' });
    if (!ok) return;
    try {
        await projectService.deleteZone(zone.id);
        fetchZones();
        notificationStore.add('Bölge silindi.', 'success');
    } catch (err) {
        // Hazırlık kayıtları olan bölge silinemez (409 conflict) → pasife almayı öner
        const isConflict = isApiError(err) && (err.type === 'conflict' || err.status === 409);
        if (isConflict && zone.isActive) {
            const offer = await notificationStore.promptConfirm({
                title: 'Bölge Silinemedi',
                message: `${ApiErrorUtils.getErrorMessage(err)}\n\nBunun yerine bölgeyi pasife almak ister misiniz?`,
                confirmText: 'Pasife Al', type: 'warning'
            });
            if (offer) {
                try {
                    await projectService.setZoneActive(zone.id, false);
                    fetchZones();
                    notificationStore.add('Bölge pasife alındı.', 'success');
                } catch (e) {
                    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
                }
            }
            return;
        }
        notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Silme hatası.', 'error');
    }
};

onMounted(fetchZones);
</script>
