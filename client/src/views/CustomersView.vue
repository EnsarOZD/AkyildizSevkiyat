<template>
  <div class="p-4 sm:p-6">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:justify-between sm:items-center mb-6 gap-3">
      <div>
        <h1 class="text-2xl font-semibold text-gray-900 dark:text-gray-100">Müşteriler</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
          ISS dışı manuel olarak eklenen müşteriler. Buradaki müşterilere doğrudan sevkiyat oluşturulabilir.
        </p>
      </div>
      <button
        @click="openCreate"
        class="inline-flex items-center px-4 py-2 bg-blue-600 text-white text-sm rounded-md hover:bg-blue-700"
      >
        + Yeni Müşteri
      </button>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-900 rounded-md shadow-sm border border-gray-200 dark:border-gray-800 p-4 mb-4 flex flex-col sm:flex-row gap-3 sm:items-end">
      <div class="flex-1">
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400">Ara</label>
        <input
          v-model="searchInput"
          @keyup.enter="applySearch"
          type="text"
          placeholder="Kod, ad veya Netsis cari kodu..."
          class="mt-1 block w-full input"
        />
      </div>
      <div class="flex items-center gap-2">
        <input id="showInactive" v-model="showInactive" type="checkbox" class="rounded" />
        <label for="showInactive" class="text-sm text-gray-700 dark:text-gray-300">Pasifleri göster</label>
      </div>
      <button @click="applySearch" class="px-4 py-2 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-200 rounded-md text-sm border border-gray-300 dark:border-gray-700 hover:bg-gray-200 dark:hover:bg-gray-700">
        Filtrele
      </button>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-gray-900 rounded-md shadow-sm border border-gray-200 dark:border-gray-800 overflow-hidden">
      <div v-if="loading" class="p-8 text-center text-gray-500">Yükleniyor...</div>
      <div v-else-if="customers.length === 0" class="p-8 text-center text-gray-500">
        Henüz müşteri eklenmemiş.
      </div>
      <table v-else class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Kod</th>
            <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Ad</th>
            <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Fatura Cari</th>
            <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Teslim Cari</th>
            <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Op. Tipi</th>
            <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">İl / İlçe</th>
            <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Durum</th>
            <th class="px-4 py-2 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">İşlem</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="c in customers" :key="c.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
            <td class="px-4 py-2 text-sm font-mono text-gray-900 dark:text-gray-100">{{ c.code }}</td>
            <td class="px-4 py-2 text-sm text-gray-900 dark:text-gray-100">{{ c.name }}</td>
            <td class="px-4 py-2 text-sm text-gray-700 dark:text-gray-300">{{ c.netsisCariKodu || '—' }}</td>
            <td class="px-4 py-2 text-sm text-gray-700 dark:text-gray-300">{{ c.netsisTeslimCariKodu || '—' }}</td>
            <td class="px-4 py-2 text-sm">
              <span :class="c.operationType === 1
                ? 'bg-violet-100 text-violet-800 dark:bg-violet-900 dark:text-violet-200'
                : 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200'"
                class="px-2 py-0.5 rounded-full text-xs">
                {{ c.operationType === 1 ? 'Kıyafet' : 'Catering' }}
              </span>
            </td>
            <td class="px-4 py-2 text-sm text-gray-700 dark:text-gray-300">
              {{ [c.cityName, c.districtName].filter(Boolean).join(' / ') || '—' }}
            </td>
            <td class="px-4 py-2 text-sm">
              <span :class="c.isActive
                ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200'
                : 'bg-gray-200 text-gray-700 dark:bg-gray-700 dark:text-gray-200'"
                class="px-2 py-0.5 rounded-full text-xs">
                {{ c.isActive ? 'Aktif' : 'Pasif' }}
              </span>
            </td>
            <td class="px-4 py-2 text-right text-sm">
              <button @click="openEdit(c)" class="text-blue-600 hover:text-blue-800 mr-3">Düzenle</button>
              <button @click="toggleActive(c)" class="text-gray-600 hover:text-gray-800">
                {{ c.isActive ? 'Pasifleştir' : 'Aktifleştir' }}
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="mt-4 flex justify-between items-center text-sm">
      <div class="text-gray-600 dark:text-gray-400">
        Toplam {{ totalCount }} kayıt — Sayfa {{ page }}/{{ totalPages }}
      </div>
      <div class="flex gap-2">
        <button @click="prevPage" :disabled="page <= 1" class="px-3 py-1 rounded border border-gray-300 dark:border-gray-700 disabled:opacity-50">Önceki</button>
        <button @click="nextPage" :disabled="page >= totalPages" class="px-3 py-1 rounded border border-gray-300 dark:border-gray-700 disabled:opacity-50">Sonraki</button>
      </div>
    </div>

    <CreateCustomerModal
      :is-open="modalOpen"
      :initial="modalInitial"
      @close="modalOpen = false"
      @saved="onSaved"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import customerService, { type Customer } from '../services/customerService';
import CreateCustomerModal from '../components/CreateCustomerModal.vue';
import { useNotification } from '../composables/useNotification';
import { ApiErrorUtils } from '../utils/apiError';

const { notify, confirm } = useNotification();

const customers = ref<Customer[]>([]);
const loading = ref(false);
const page = ref(1);
const pageSize = 25;
const totalPages = ref(1);
const totalCount = ref(0);

const searchInput = ref('');
const search = ref('');
const showInactive = ref(false);

const modalOpen = ref(false);
const modalInitial = ref<Customer | null>(null);

async function fetchData() {
  loading.value = true;
  try {
    const result = await customerService.getAll({
      pageNumber: page.value,
      pageSize,
      search: search.value || undefined,
      showInactive: showInactive.value,
    });
    customers.value = result.items;
    totalPages.value = result.totalPages;
    totalCount.value = result.totalCount;
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Müşteriler yüklenemedi.'));
  } finally {
    loading.value = false;
  }
}

function applySearch() {
  search.value = searchInput.value;
  page.value = 1;
  fetchData();
}

function prevPage() {
  if (page.value > 1) {
    page.value--;
    fetchData();
  }
}

function nextPage() {
  if (page.value < totalPages.value) {
    page.value++;
    fetchData();
  }
}

function openCreate() {
  modalInitial.value = null;
  modalOpen.value = true;
}

function openEdit(c: Customer) {
  modalInitial.value = c;
  modalOpen.value = true;
}

async function toggleActive(c: Customer) {
  const ok = await confirm.show({
    title: c.isActive ? 'Müşteriyi Pasifleştir' : 'Müşteriyi Aktifleştir',
    message: `'${c.name}' müşterisini ${c.isActive ? 'pasifleştirmek' : 'aktifleştirmek'} istediğinize emin misiniz?`,
    confirmText: c.isActive ? 'Pasifleştir' : 'Aktifleştir',
  });
  if (!ok) return;
  try {
    await customerService.toggleActive(c.id, !c.isActive);
    notify.success('Müşteri durumu güncellendi.');
    fetchData();
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Durum güncellenemedi.'));
  }
}

function onSaved() {
  fetchData();
}

watch(showInactive, () => {
  page.value = 1;
  fetchData();
});

fetchData();
</script>

<style scoped>
.input {
  @apply bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm;
}
</style>
