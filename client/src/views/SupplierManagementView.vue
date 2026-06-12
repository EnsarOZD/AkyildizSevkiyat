<template>
  <div>
    <PageHeader title="Tedarikçi Yönetimi" subtitle="Tedarikçi tanımlarını yönetin" color="blue" class="mb-6">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
        </svg>
      </template>
      <template #actions>
        <div class="flex flex-wrap gap-2">
          <button @click="downloadTemplate" class="px-3 py-2 bg-gray-600 text-white rounded-lg hover:bg-gray-700 text-sm font-medium">Şablon İndir</button>
          <button @click="triggerFileInput" class="px-3 py-2 bg-green-600 text-white rounded-lg hover:bg-green-700 text-sm font-medium">Excel Yükle</button>
          <input type="file" ref="fileInput" class="hidden" accept=".xlsx, .xls" @change="handleFileUpload" />
          <button @click="showCreateModal = true" class="px-3 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 text-sm font-medium">+ Yeni Tedarikçi</button>
        </div>
      </template>
    </PageHeader>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-900 p-4 rounded-lg shadow mb-6">
      <div class="flex gap-4">
        <div class="flex-1">
           <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Ara</label>
           <input v-model="searchQuery" @input="onSearch" type="text" placeholder="Tedarikçi adı veya kodu..." class="w-full border-gray-300 dark:border-gray-700 rounded-md shadow-sm border p-2 dark:bg-gray-800 dark:text-gray-100">
        </div>
      </div>
    </div>

    <!-- Tablo (masaüstü) + Kart (mobil) — ortak ResponsiveTable -->
    <p v-if="loading" class="text-center text-gray-500 dark:text-gray-400 py-8">Yükleniyor...</p>
    <ResponsiveTable
      v-else
      :columns="columns"
      :rows="suppliers"
      row-key="id"
      empty-text="Kayıt bulunamadı."
    >
      <!-- Tedarikçi adı (kartta başlık) -->
      <template #name="{ row }">{{ row.name }}</template>
      <!-- Kod -->
      <template #supplierCode="{ row }">{{ row.supplierCode || '—' }}</template>
      <!-- E-posta (çoklu adres) -->
      <template #email="{ row }">
        <div v-if="row.email" class="flex flex-wrap gap-1 md:justify-start justify-end">
          <a v-for="addr in splitEmails(row.email)" :key="addr" :href="'mailto:' + addr" class="text-blue-600 dark:text-blue-400 hover:underline">{{ addr }}</a>
        </div>
        <span v-else class="text-gray-300 dark:text-gray-600">—</span>
      </template>
      <!-- Oluşturulma / Güncelleme -->
      <template #createdAt="{ row }">
        <div>{{ formatDate(row.createdAt) }}</div>
        <div v-if="row.lastModified" class="text-xs text-gray-400 dark:text-gray-500">{{ formatDate(row.lastModified) }}</div>
      </template>
      <!-- İşlemler -->
      <template #actions="{ row }">
        <button @click="openEdit(row)" class="text-blue-600 hover:text-blue-900 dark:text-blue-400 dark:hover:text-blue-300 text-sm font-medium">Düzenle</button>
        <button @click="confirmDelete(row)" class="text-red-600 hover:text-red-900 dark:text-red-400 dark:hover:text-red-300 text-sm font-medium">Sil</button>
      </template>
    </ResponsiveTable>

    <CreateSupplierModal
        :isOpen="showCreateModal"
        @close="showCreateModal = false"
        @saved="onSupplierSaved"
    />

    <!-- Edit Supplier Modal -->
    <BaseModal :show="showEditModal" title="Tedarikçi Düzenle" maxWidth="md" @close="closeEdit">
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Tedarikçi Adı <span class="text-danger-500">*</span></label>
          <input v-model="editForm.name" type="text"
            class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-brand-500" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Netsis Cari Kodu <span class="text-danger-500">*</span></label>
          <input v-model="editForm.supplierCode" type="text" placeholder="örn: 120.001.001"
            class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-brand-500" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">E-posta (Opsiyonel)</label>
          <input v-model="editForm.email" type="text" placeholder="ornek@tedarikci.com, ikinci@tedarikci.com"
            class="w-full px-3 py-2 text-sm rounded-input border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-brand-500" />
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Birden fazla adres için virgül veya noktalı virgülle ayırın.</p>
        </div>
      </div>
      <template #footer>
        <button @click="closeEdit"
          class="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">İptal</button>
        <button @click="saveEdit"
          class="px-4 py-2 text-sm font-medium text-white bg-brand-600 hover:bg-brand-700 rounded-lg">Kaydet</button>
      </template>
    </BaseModal>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import ResponsiveTable from '../components/ResponsiveTable.vue';
import { supplierService } from '../services/supplierService';
import { ApiErrorUtils } from '../utils/apiError';
import { formatDate } from '../utils/dateFormat';
import { useNotificationStore } from '../stores/notification';
import CreateSupplierModal from '../components/CreateSupplierModal.vue';
import BaseModal from '../components/BaseModal.vue';

// Ortak tablo kolon tanımı (masaüstü tablo + mobil kart aynı kaynaktan)
const columns = [
  { key: 'name',         label: 'Tedarikçi Adı', priority: true },
  { key: 'supplierCode', label: 'Kod',           mono: true },
  { key: 'email',        label: 'E-posta' },
  { key: 'createdAt',    label: 'Oluşturulma / Güncelleme', hideOnMobile: true },
];

const suppliers = ref<any[]>([]);
const loading = ref(false);
const searchQuery = ref('');
const showCreateModal = ref(false);
const fileInput = ref<HTMLInputElement | null>(null);

const showEditModal = ref(false);
const editingId = ref<string>('');
const editForm = reactive({ name: '', supplierCode: '', email: '' });

const notificationStore = useNotificationStore();
let searchTimeout: any = null;

const splitEmails = (raw: string): string[] =>
    raw.split(/[,;]/).map(e => e.trim()).filter(e => e.includes('@'));

const fetchSuppliers = async () => {
    loading.value = true;
    try {
        suppliers.value = await supplierService.getAll(searchQuery.value);
    } catch (e) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Tedarikçiler yüklenirken hata oluştu.', 'error');
    } finally {
        loading.value = false;
    }
};

const onSearch = () => {
    if (searchTimeout) clearTimeout(searchTimeout);
    searchTimeout = setTimeout(() => {
        fetchSuppliers();
    }, 300);
};

const onSupplierSaved = () => {
    fetchSuppliers();
};

const openEdit = (supplier: any) => {
    editingId.value = supplier.id;
    editForm.name = supplier.name;
    editForm.supplierCode = supplier.supplierCode || '';
    editForm.email = supplier.email || '';
    showEditModal.value = true;
};

const closeEdit = () => {
    showEditModal.value = false;
};

const saveEdit = async () => {
    if (!editForm.name) {
        notificationStore.add('Tedarikçi adı zorunludur.', 'warning');
        return;
    }
    if (!editForm.supplierCode) {
        notificationStore.add('Netsis cari kodu zorunludur.', 'warning');
        return;
    }
    try {
        await supplierService.update(editingId.value, {
            name: editForm.name,
            supplierCode: editForm.supplierCode || undefined,
            email: editForm.email || undefined
        });
        notificationStore.add('Tedarikçi güncellendi.', 'success');
        showEditModal.value = false;
        fetchSuppliers();
    } catch (e) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Güncelleme başarısız.', 'error');
    }
};

const confirmDelete = async (supplier: any) => {
    if (!confirm(`"${supplier.name}" tedarikçisini silmek istediğinizden emin misiniz?`)) return;
    try {
        await supplierService.delete(supplier.id);
        notificationStore.add('Tedarikçi silindi.', 'success');
        fetchSuppliers();
    } catch (e) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Silme işlemi başarısız.', 'error');
    }
};

const triggerFileInput = () => {
    fileInput.value?.click();
};

const handleFileUpload = async (event: Event) => {
    const target = event.target as HTMLInputElement;
    if (target.files && target.files.length > 0) {
        const file = target.files[0];
        const formData = new FormData();
        formData.append('file', file as Blob);

        try {
            const res = await supplierService.import(formData);
            notificationStore.add(`${res.count} adet tedarikçi başarıyla işlendi.`, 'success');
            fetchSuppliers();
        } catch (e) {
            notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Dosya yüklenirken hata oluştu.', 'error');
        } finally {
            target.value = '';
        }
    }
};

const downloadTemplate = async () => {
    try {
        const data = await supplierService.downloadTemplate();
        const url = window.URL.createObjectURL(new Blob([data]));
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', 'TedarikçiYuklemeŞablonu.xlsx');
        document.body.appendChild(link);
        link.click();
        link.remove();
    } catch (e) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Şablon indirilemedi.', 'error');
    }
};

onMounted(() => {
    fetchSuppliers();
});
</script>
