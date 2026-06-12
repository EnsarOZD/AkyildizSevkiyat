<template>
  <div class="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
    <PageHeader title="Nakliyeciler" subtitle="Taşeron taşıyıcı tanımları, plakaları ve iletişim bilgileri" color="teal">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17a2 2 0 11-4 0 2 2 0 014 0zM19 17a2 2 0 11-4 0 2 2 0 014 0z" />
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16V6a1 1 0 00-1-1H4a1 1 0 00-1 1v10a1 1 0 001 1h1m8-1a1 1 0 001 1h2m-3-1V8h4l3 4v3a1 1 0 01-1 1h-1" />
        </svg>
      </template>
      <template #actions>
        <button
          v-role="['Admin', 'Manager', 'Accounting']"
          @click="openCreate"
          class="px-4 py-2 bg-blue-600 text-white rounded-lg font-semibold hover:bg-blue-700 transition text-sm"
        >
          + Yeni Nakliyeci
        </button>
      </template>
    </PageHeader>

    <!-- Filters -->
    <div class="flex gap-3 mb-4 flex-wrap">
      <input
        v-model="search"
        @input="debouncedLoad"
        type="text"
        placeholder="Nakliyeci, il veya plaka ara..."
        class="flex-1 min-w-[200px] border dark:border-gray-700 rounded px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100"
      />
      <select v-model="activeFilter" @change="load" class="border dark:border-gray-700 rounded px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100">
        <option value="">Tümü</option>
        <option value="true">Aktif</option>
        <option value="false">Pasif</option>
      </select>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-gray-900 shadow rounded-lg overflow-hidden">
      <div v-if="loading" class="p-8 text-center text-gray-500 dark:text-gray-400">Yükleniyor...</div>
      <div v-else-if="carriers.length === 0" class="p-8 text-center text-gray-400">Kayıt bulunamadı.</div>
      <table v-else class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Nakliyeci</th>
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">İl</th>
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Telefon</th>
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Plakalar</th>
            <th class="px-4 py-3 text-left font-medium text-gray-500 dark:text-gray-400">Durum</th>
            <th class="px-4 py-3 text-right font-medium text-gray-500 dark:text-gray-400">İşlem</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="c in carriers" :key="c.id">
            <td class="px-4 py-3 font-medium text-gray-900 dark:text-gray-100">{{ c.name }}</td>
            <td class="px-4 py-3 text-gray-600 dark:text-gray-400">{{ c.city || '—' }}</td>
            <td class="px-4 py-3 text-gray-600 dark:text-gray-400">{{ c.phone || '—' }}</td>
            <td class="px-4 py-3">
              <div v-if="c.vehicles.length" class="flex flex-wrap gap-1">
                <span v-for="v in c.vehicles" :key="v.id"
                  class="px-2 py-0.5 rounded text-xs font-mono bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300"
                  :class="{ 'opacity-50 line-through': !v.isActive }">
                  {{ v.plateNumber }}
                </span>
              </div>
              <span v-else class="text-gray-400 text-xs">—</span>
            </td>
            <td class="px-4 py-3">
              <span class="px-2 py-0.5 rounded-full text-xs font-semibold"
                :class="c.isActive ? 'bg-green-100 text-green-800 dark:bg-green-900/30 dark:text-green-300' : 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400'">
                {{ c.isActive ? 'Aktif' : 'Pasif' }}
              </span>
            </td>
            <td class="px-4 py-3 text-right whitespace-nowrap">
              <button v-role="['Admin', 'Manager', 'Accounting']" @click="openEdit(c)" class="text-blue-600 hover:text-blue-900 mr-3">Düzenle</button>
              <button v-role="['Admin', 'Manager']" @click="remove(c)" class="text-red-600 hover:text-red-900">Sil</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Create/Edit Modal -->
    <BaseModal :show="showModal" :title="isEditing ? 'Nakliyeci Düzenle' : 'Yeni Nakliyeci'" maxWidth="md" @close="showModal = false">
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Ad / Ünvan <span class="text-red-500">*</span></label>
          <input v-model="form.name" type="text" maxlength="200" class="w-full border rounded px-3 py-2 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
        </div>
        <div class="grid grid-cols-2 gap-3">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">İl / Bölge</label>
            <input v-model="form.city" type="text" maxlength="100" placeholder="Örn: İstanbul" class="w-full border rounded px-3 py-2 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Telefon</label>
            <input v-model="form.phone" type="tel" maxlength="30" placeholder="05XX XXX XX XX" class="w-full border rounded px-3 py-2 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
          </div>
        </div>

        <!-- Plakalar -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Araç Plakaları</label>
          <div class="space-y-2">
            <div v-for="(_plate, idx) in form.plates" :key="idx" class="flex gap-2">
              <input
                v-model="form.plates[idx]"
                type="text"
                maxlength="20"
                placeholder="34 ABC 123"
                class="flex-1 border rounded px-3 py-2 text-sm uppercase dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                @input="form.plates[idx] = ($event.target as HTMLInputElement).value.toUpperCase()"
              />
              <button @click="form.plates.splice(idx, 1)" class="px-3 rounded text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20">✕</button>
            </div>
          </div>
          <button @click="form.plates.push('')" class="mt-2 text-sm text-blue-600 hover:underline">+ Plaka Ekle</button>
        </div>

        <label v-if="isEditing" class="flex items-center gap-2 cursor-pointer">
          <input type="checkbox" v-model="form.isActive" />
          <span class="text-sm text-gray-700 dark:text-gray-300">Aktif</span>
        </label>
      </div>
      <template #footer>
        <button @click="showModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button @click="save" :disabled="!form.name.trim() || saving" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 font-bold disabled:bg-blue-300">
          {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
        </button>
      </template>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import BaseModal from '../components/BaseModal.vue';
import carrierService, { type Carrier } from '../services/carrierService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const notify = useNotificationStore();

const carriers = ref<Carrier[]>([]);
const loading = ref(false);
const search = ref('');
const activeFilter = ref('');

let debounceTimer: ReturnType<typeof setTimeout> | null = null;
const debouncedLoad = () => {
  if (debounceTimer) clearTimeout(debounceTimer);
  debounceTimer = setTimeout(load, 300);
};

const load = async () => {
  loading.value = true;
  try {
    carriers.value = await carrierService.list({
      search: search.value || undefined,
      isActive: activeFilter.value === '' ? undefined : activeFilter.value === 'true',
    });
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Nakliyeciler yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
};

// Modal
const showModal = ref(false);
const saving = ref(false);
const isEditing = ref(false);
const editingId = ref<number | null>(null);
const form = ref<{ name: string; city: string; phone: string; isActive: boolean; plates: string[] }>({
  name: '', city: '', phone: '', isActive: true, plates: [],
});

const openCreate = () => {
  isEditing.value = false;
  editingId.value = null;
  form.value = { name: '', city: '', phone: '', isActive: true, plates: [''] };
  showModal.value = true;
};

const openEdit = (c: Carrier) => {
  isEditing.value = true;
  editingId.value = c.id;
  form.value = {
    name: c.name,
    city: c.city ?? '',
    phone: c.phone ?? '',
    isActive: c.isActive,
    plates: c.vehicles.map(v => v.plateNumber),
  };
  showModal.value = true;
};

const save = async () => {
  if (!form.value.name.trim()) return;
  saving.value = true;
  try {
    const payload = {
      name: form.value.name.trim(),
      city: form.value.city.trim() || null,
      phone: form.value.phone.trim() || null,
      isActive: form.value.isActive,
      plates: form.value.plates.map(p => p.trim()).filter(p => p),
    };
    if (isEditing.value && editingId.value != null) {
      await carrierService.update(editingId.value, payload);
      notify.add('Nakliyeci güncellendi.', 'success');
    } else {
      await carrierService.create(payload);
      notify.add('Nakliyeci oluşturuldu.', 'success');
    }
    showModal.value = false;
    await load();
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
  } finally {
    saving.value = false;
  }
};

const remove = async (c: Carrier) => {
  const ok = await notify.promptConfirm({
    title: 'Nakliyeci Sil',
    message: `"${c.name}" nakliyecisi ve tüm plakaları silinecek. Emin misiniz?`,
    confirmText: 'Sil',
    cancelText: 'Vazgeç',
    type: 'danger',
  });
  if (!ok) return;
  try {
    await carrierService.remove(c.id);
    notify.add('Nakliyeci silindi.', 'success');
    await load();
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Silinemedi.', 'error');
  }
};

onMounted(load);
</script>
