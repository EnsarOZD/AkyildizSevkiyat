<template>
  <div v-if="isOpen" class="fixed inset-0 z-50 overflow-y-auto" aria-labelledby="modal-title" role="dialog" aria-modal="true">
    <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
      <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" aria-hidden="true" @click="close"></div>
      <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>

      <!-- Wide Modal -->
      <div class="inline-block align-bottom bg-white dark:bg-gray-900 rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform transition-all w-full mx-4 sm:my-8 sm:align-middle sm:max-w-4xl sm:w-full sm:p-6">
        <div>
          <h3 class="text-lg leading-6 font-medium text-gray-900 dark:text-gray-100 border-b dark:border-gray-700 pb-2">Yeni Satınalma Siparişi</h3>

          <!-- Header Form -->
          <div class="mt-4 grid grid-cols-1 md:grid-cols-3 gap-4">
            <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Tedarikçi <span class="text-red-500">*</span></label>
                <div class="mt-1">
                    <SupplierSelect v-model="form.supplierId" />
                </div>
            </div>
            <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Sipariş Tarihi <span class="text-red-500">*</span></label>
                <input v-model="form.orderDate" type="date" class="mt-1 bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm">
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Termin Tarihi</label>
              <input v-model="form.expectedDeliveryDate" type="date" class="mt-1 bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm">
            </div>
          </div>
          <div class="mt-4">
             <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Not / Açıklama</label>
             <input v-model="form.note" type="text" class="mt-1 bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm" placeholder="Sipariş notları...">
          </div>

          <!-- Lines Grid -->
          <div class="mt-6">
            <div class="flex justify-between items-center mb-2">
                <h4 class="text-md font-medium text-gray-800 dark:text-gray-200">Sipariş Kalemleri</h4>
                <button @click="addLine" type="button" class="inline-flex items-center px-3 py-1 border border-transparent text-sm font-medium rounded-md shadow-sm text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none">
                    + Kalem Ekle
                </button>
            </div>

            <!-- Changed overflow-hidden to overflow-visible to prevent clipping dropdowns -->
            <div class="border dark:border-gray-700 rounded-md overflow-visible bg-gray-50 dark:bg-gray-800" style="min-height: 400px;">
                <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                    <thead class="bg-gray-100 dark:bg-gray-800">
                        <tr>
                            <th scope="col" class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider min-w-[120px]">Stok Kodu / Arama <span class="text-red-500">*</span></th>
                            <th scope="col" class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">Stok Adı</th>
                            <th scope="col" class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Miktar <span class="text-red-500">*</span></th>
                            <th scope="col" class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden sm:table-cell">Birim</th>
                            <th scope="col" class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">Not</th>
                            <th scope="col" class="relative px-4 py-3">
                                <span class="sr-only">Sil</span>
                            </th>
                        </tr>
                    </thead>
                    <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                        <tr v-for="(line, index) in form.lines" :key="line.tempId || index">
                            <td class="px-4 py-2">
                                <StockCombobox v-model="line.stockMasterId" @select="onStockSelect(line, $event)" />
                            </td>
                            <td class="px-4 py-2 hidden lg:table-cell">
                                <span class="text-sm text-gray-900 dark:text-gray-100">{{ line.stockName || '-' }}</span>
                            </td>
                            <td class="px-4 py-2">
                                <input v-model.number="line.orderedQty" type="number" step="0.01" class="shadow-sm focus:ring-indigo-500 focus:border-indigo-500 block w-full sm:text-sm border-gray-300 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded-md" placeholder="0.00">
                            </td>
                            <td class="px-4 py-2 hidden sm:table-cell">
                                <input v-model="line.unit" disabled type="text" class="bg-gray-100 dark:bg-gray-700 dark:border-gray-700 dark:text-gray-100 shadow-sm focus:ring-indigo-500 focus:border-indigo-500 block w-full sm:text-sm border-gray-300 rounded-md">
                            </td>
                            <td class="px-4 py-2 hidden lg:table-cell">
                                <input v-model="line.note" type="text" class="shadow-sm focus:ring-indigo-500 focus:border-indigo-500 block w-full sm:text-sm border-gray-300 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-100 rounded-md">
                            </td>
                            <td class="px-4 py-2 text-right">
                                <button @click="removeLine(index)" class="text-red-600 hover:text-red-900">
                                    <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
                                        <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 000-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
                                    </svg>
                                </button>
                            </td>
                        </tr>
                        <tr v-if="form.lines.length === 0">
                            <td colspan="5" class="px-4 py-6 text-center text-sm text-gray-500 dark:text-gray-400">
                                Henüz kalem eklenmedi. Lütfen en az bir kalem ekleyin.
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>

          </div>
        </div>

        <div class="mt-5 sm:mt-6 sm:grid sm:grid-cols-2 sm:gap-3 sm:grid-flow-row-dense">
          <button @click="save" type="button" class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-indigo-600 text-base font-medium text-white hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:col-start-2 sm:text-sm">
            Kaydet
          </button>
          <button @click="close" type="button" class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 dark:border-gray-700 shadow-sm px-4 py-2 bg-white dark:bg-gray-800 text-base font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500 sm:mt-0 sm:col-start-1 sm:text-sm">
            İptal
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import purchaseOrderService from '../services/purchaseOrderService';
import SupplierSelect from './SupplierSelect.vue';
import StockCombobox from './StockCombobox.vue';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const props = defineProps<{
  isOpen: boolean
}>();
const emit = defineEmits<{
  (e: 'close'): void
  (e: 'saved'): void
}>();
const notificationStore = useNotificationStore();

const today = new Date().toISOString().split('T')[0];

const generateId = () => Math.random().toString(36).substring(2, 15) + Math.random().toString(36).substring(2, 15);

interface LineItem {
    tempId: string;
    stockMasterId: number;
    stockName: string;
    orderedQty: number;
    unit: string;
    note: string;
}

const form = reactive({
    supplierId: '',
    orderDate: today,
    expectedDeliveryDate: '' as string,
    note: '',
    lines: [
        { tempId: generateId(), stockMasterId: 0, stockName: '', orderedQty: 0, unit: '', note: '' } as LineItem
    ]
});

// Setup default form state
const resetForm = () => {
    form.supplierId = '';
    form.orderDate = new Date().toISOString().split('T')[0];
    form.expectedDeliveryDate = '';
    form.note = '';
    form.lines = [{ tempId: generateId(), stockMasterId: 0, stockName: '', orderedQty: 0, unit: '', note: '' }];
};

const close = () => {
    resetForm();
    emit('close');
};

const addLine = () => {
    form.lines.push({ tempId: generateId(), stockMasterId: 0, stockName: '', orderedQty: 0, unit: '', note: '' });
};

const removeLine = (index: number) => {
    form.lines.splice(index, 1);
};

const onStockSelect = (line: LineItem, stock: any) => {
    if (stock) {
        line.stockMasterId = stock.id || stock.Id;
        line.stockName = stock.stockName || stock.StockName;
        line.unit = stock.unit || stock.Unit || 'Adet';
    } else {
        line.stockMasterId = 0;
        line.unit = '';
    }
};

const save = async () => {
    // Validation
    if (!form.supplierId) {
        notificationStore.add('Lütfen bir tedarikçi seçin.', 'warning');
        return;
    }
    if (!form.orderDate) {
        notificationStore.add('Sipariş tarihi zorunludur.', 'warning');
        return;
    }
    if (form.lines.length === 0) {
        notificationStore.add('En az bir kalem eklemelisiniz.', 'warning');
        return;
    }

    // Filter out invalid lines
    const validLines = form.lines.filter(l => l.stockMasterId > 0 && l.orderedQty > 0);

    if (validLines.length === 0) {
        notificationStore.add('Lütfen kalemlere stok ve miktar giriniz.', 'warning');
        return;
    }

    try {
        const payload = {
            supplierId: form.supplierId,
            orderDate: form.orderDate,
            expectedDeliveryDate: form.expectedDeliveryDate || undefined,
            note: form.note,
            lines: validLines.map(l => ({
                stockMasterId: l.stockMasterId,
                orderedQty: l.orderedQty,
                note: l.note
            }))
        };

        await purchaseOrderService.create(payload);

        notificationStore.add('Sipariş başarıyla oluşturuldu.', 'success');
        emit('saved');
        close();

    } catch (error) {
        console.error(error);
        notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'Sipariş oluşturulurken hata oluştu.', 'error');
    }
};
</script>
