<template>
  <div>
    <div class="mb-6 flex justify-between items-center">
      <h1 class="text-2xl font-semibold text-gray-900 dark:text-gray-100">Tedarikçi Yönetimi</h1>
      <div class="flex gap-2">
         <button @click="downloadTemplate" class="px-4 py-2 bg-gray-600 text-white rounded hover:bg-gray-700">
            Şablon İndir
         </button>
         <button @click="triggerFileInput" class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 flex items-center gap-2">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zM6.293 6.707a1 1 0 010-1.414l3-3a1 1 0 011.414 0l3 3a1 1 0 01-1.414 1.414L11 5.414V13a1 1 0 11-2 0V5.414L7.707 6.707a1 1 0 01-1.414 0z" clip-rule="evenodd" />
            </svg>
            Excel Yükle
         </button>
         <input type="file" ref="fileInput" class="hidden" accept=".xlsx, .xls" @change="handleFileUpload" />

         <button @click="showCreateModal = true" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">
           + Yeni Tedarikçi
         </button>
      </div>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-900 p-4 rounded-lg shadow mb-6">
      <div class="flex gap-4">
        <div class="flex-1">
           <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Ara</label>
           <input v-model="searchQuery" @input="onSearch" type="text" placeholder="Tedarikçi adı veya kodu..." class="w-full border-gray-300 dark:border-gray-700 rounded-md shadow-sm border p-2 dark:bg-gray-800 dark:text-gray-100">
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-gray-900 shadow rounded-lg overflow-hidden">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tedarikçi Adı</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Kod</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Oluşturulma Tarihi</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
           <tr v-if="loading">
             <td colspan="4" class="px-6 py-4 text-center text-gray-500 dark:text-gray-400">Yükleniyor...</td>
           </tr>
           <tr v-else-if="!suppliers || suppliers.length === 0">
             <td colspan="4" class="px-6 py-4 text-center text-gray-500 dark:text-gray-400">Kayıt bulunamadı.</td>
           </tr>
           <tr v-for="supplier in suppliers" :key="supplier.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
             <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">{{ supplier.name }}</td>
             <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ supplier.supplierCode || '-' }}</td>
             <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ formatDate(supplier.createdAt) }}</td>
             <td class="px-6 py-4 whitespace-nowrap">
               <span class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800">
                 Aktif
               </span>
             </td>
           </tr>
        </tbody>
      </table>
    </div>

    <CreateSupplierModal
        :isOpen="showCreateModal"
        @close="showCreateModal = false"
        @saved="onSupplierSaved"
    />
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { supplierService } from '../services/supplierService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import CreateSupplierModal from '../components/CreateSupplierModal.vue';

const suppliers = ref<any[]>([]);
const loading = ref(false);
const searchQuery = ref('');
const showCreateModal = ref(false);
const fileInput = ref<HTMLInputElement | null>(null);

const notificationStore = useNotificationStore();
let searchTimeout: any = null;

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

const formatDate = (date: string) => {
    if(!date) return '-';
    return new Date(date).toLocaleDateString('tr-TR');
};

onMounted(() => {
    fetchSuppliers();
});
</script>
