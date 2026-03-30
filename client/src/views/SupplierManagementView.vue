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
      <div class="overflow-x-auto">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tedarikçi Adı</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Kod</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden sm:table-cell">E-posta</th>
            <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">Oluşturulma</th>
            <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
           <tr v-if="loading">
             <td colspan="5" class="px-6 py-4 text-center text-gray-500 dark:text-gray-400">Yükleniyor...</td>
           </tr>
           <tr v-else-if="!suppliers || suppliers.length === 0">
             <td colspan="5" class="px-6 py-4 text-center text-gray-500 dark:text-gray-400">Kayıt bulunamadı.</td>
           </tr>
           <tr v-for="supplier in suppliers" :key="supplier.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
             <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">{{ supplier.name }}</td>
             <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ supplier.supplierCode || '-' }}</td>
             <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 hidden sm:table-cell">
               <a v-if="supplier.email" :href="'mailto:' + supplier.email" class="text-blue-600 hover:underline">{{ supplier.email }}</a>
               <span v-else class="text-gray-300 dark:text-gray-600">—</span>
             </td>
             <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 hidden lg:table-cell">{{ formatDate(supplier.createdAt) }}</td>
             <td class="px-6 py-4 whitespace-nowrap text-right">
               <button @click="openEdit(supplier)" class="text-indigo-600 hover:text-indigo-900 text-sm font-medium">Düzenle</button>
             </td>
           </tr>
        </tbody>
      </table>
      </div>
    </div>

    <CreateSupplierModal
        :isOpen="showCreateModal"
        @close="showCreateModal = false"
        @saved="onSupplierSaved"
    />

    <!-- Edit Supplier Modal -->
    <Teleport to="body">
      <div v-if="showEditModal" class="fixed inset-0 z-50 overflow-y-auto" role="dialog" aria-modal="true">
        <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
          <div class="fixed inset-0 bg-gray-500 bg-opacity-75" @click="closeEdit"></div>
          <span class="hidden sm:inline-block sm:align-middle sm:h-screen">&#8203;</span>
          <div class="inline-block align-bottom bg-white dark:bg-gray-900 rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-lg sm:w-full sm:p-6">
            <h3 class="text-lg font-medium text-gray-900 dark:text-gray-100 mb-4">Tedarikçi Düzenle</h3>
            <div class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Tedarikçi Adı <span class="text-red-500">*</span></label>
                <input v-model="editForm.name" type="text" class="mt-1 bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm">
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Tedarikçi Kodu (Opsiyonel)</label>
                <input v-model="editForm.supplierCode" type="text" class="mt-1 bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm">
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">E-posta (Opsiyonel)</label>
                <input v-model="editForm.email" type="email" class="mt-1 bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm" placeholder="ornek@tedarikci.com">
              </div>
            </div>
            <div class="mt-5 sm:grid sm:grid-cols-2 sm:gap-3 sm:grid-flow-row-dense">
              <button @click="saveEdit" type="button" class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-indigo-600 text-base font-medium text-white hover:bg-indigo-700 sm:col-start-2 sm:text-sm">
                Kaydet
              </button>
              <button @click="closeEdit" type="button" class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 dark:border-gray-700 shadow-sm px-4 py-2 bg-white dark:bg-gray-800 text-base font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 sm:mt-0 sm:col-start-1 sm:text-sm">
                İptal
              </button>
            </div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>
<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue';
import { supplierService } from '../services/supplierService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import CreateSupplierModal from '../components/CreateSupplierModal.vue';

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
