<template>
  <div class="space-y-6">
    <PageHeader title="Harici Mail Adresleri" subtitle="Karşılaştırma maillerinde CC olarak eklenebilecek kişiler" color="gray">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
        </svg>
      </template>
      <template #actions>
        <button @click="openModal()" class="bg-blue-600 text-white px-4 py-2 rounded-xl hover:bg-blue-700 text-sm font-bold">+ Yeni Kişi</button>
      </template>
    </PageHeader>

    <div class="bg-white dark:bg-gray-900 rounded-2xl shadow border border-gray-100 dark:border-gray-800 overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Ad</th>
            <th class="px-6 py-3 text-left text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">E-posta</th>
            <th class="px-6 py-3 text-left text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden sm:table-cell">Not</th>
            <th class="px-6 py-3 text-left text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
            <th class="px-6 py-3 text-right text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlem</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-if="loading">
            <td colspan="5" class="px-6 py-8 text-center text-gray-400">Yükleniyor...</td>
          </tr>
          <tr v-else-if="contacts.length === 0">
            <td colspan="5" class="px-6 py-8 text-center text-gray-400">Henüz kişi eklenmedi.</td>
          </tr>
          <tr v-for="c in contacts" :key="c.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
            <td class="px-6 py-4 text-sm font-medium text-gray-900 dark:text-gray-100">{{ c.name }}</td>
            <td class="px-6 py-4 text-sm text-gray-600 dark:text-gray-300 font-mono">{{ c.email }}</td>
            <td class="px-6 py-4 text-sm text-gray-500 dark:text-gray-400 hidden sm:table-cell">{{ c.note || '—' }}</td>
            <td class="px-6 py-4">
              <span :class="c.isActive ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-400' : 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400'"
                    class="px-2 py-0.5 rounded-full text-xs font-bold">
                {{ c.isActive ? 'Aktif' : 'Pasif' }}
              </span>
            </td>
            <td class="px-6 py-4 text-right space-x-3">
              <button @click="openModal(c)" class="text-blue-600 hover:text-blue-900 text-sm font-medium">Düzenle</button>
              <button @click="deleteContact(c)" class="text-red-600 hover:text-red-900 text-sm font-medium">Sil</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal -->
    <div v-if="showModal" class="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
      <div class="bg-white dark:bg-gray-900 rounded-2xl p-6 w-full max-w-md">
        <h3 class="text-lg font-bold mb-4 text-gray-900 dark:text-gray-100">{{ isEditing ? 'Kişi Düzenle' : 'Yeni Kişi Ekle' }}</h3>
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Ad Soyad</label>
            <input v-model="form.name" type="text" class="w-full border rounded-lg p-2.5 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">E-posta</label>
            <input v-model="form.email" type="email" class="w-full border rounded-lg p-2.5 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Not</label>
            <input v-model="form.note" type="text" placeholder="Opsiyonel" class="w-full border rounded-lg p-2.5 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
          </div>
          <div v-if="isEditing" class="flex items-center gap-2">
            <input v-model="form.isActive" type="checkbox" id="isActive" class="rounded" />
            <label for="isActive" class="text-sm text-gray-700 dark:text-gray-300">Aktif</label>
          </div>
        </div>
        <div class="mt-6 flex justify-end gap-3">
          <button @click="showModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg">İptal</button>
          <button @click="save" :disabled="saving" class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 font-bold">
            {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import apiClient from '../services/apiClient';
import { useNotification } from '../composables/useNotification';
import { ApiErrorUtils } from '../utils/apiError';

const { notify, confirm } = useNotification();

interface Contact { id: number; name: string; email: string; note?: string; isActive: boolean; }

const contacts = ref<Contact[]>([]);
const loading = ref(false);
const showModal = ref(false);
const isEditing = ref(false);
const saving = ref(false);
const form = ref({ id: 0, name: '', email: '', note: '', isActive: true });

const fetchContacts = async () => {
  loading.value = true;
  try {
    const r = await apiClient.get('/external-email-contacts');
    contacts.value = r.data;
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Veriler yüklenemedi.'));
  } finally {
    loading.value = false;
  }
};

const openModal = (c?: Contact) => {
  if (c) {
    isEditing.value = true;
    form.value = { ...c, note: c.note || '' };
  } else {
    isEditing.value = false;
    form.value = { id: 0, name: '', email: '', note: '', isActive: true };
  }
  showModal.value = true;
};

const save = async () => {
  if (!form.value.name.trim() || !form.value.email.trim()) {
    notify.warning('Ad ve e-posta zorunludur.');
    return;
  }
  saving.value = true;
  try {
    if (isEditing.value) {
      await apiClient.put(`/external-email-contacts/${form.value.id}`, form.value);
      notify.success('Kişi güncellendi.');
    } else {
      await apiClient.post('/external-email-contacts', { name: form.value.name, email: form.value.email, note: form.value.note || null });
      notify.success('Kişi eklendi.');
    }
    showModal.value = false;
    await fetchContacts();
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Kaydedilemedi.'));
  } finally {
    saving.value = false;
  }
};

const deleteContact = async (c: Contact) => {
  if (!await confirm.requireDelete(c.name)) return;
  try {
    await apiClient.delete(`/external-email-contacts/${c.id}`);
    notify.success('Kişi silindi.');
    await fetchContacts();
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Silinemedi.'));
  }
};

onMounted(fetchContacts);
</script>
