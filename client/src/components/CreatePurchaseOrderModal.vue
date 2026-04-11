<template>
  <BaseModal :show="isOpen" title="Yeni Satınalma Siparişi" maxWidth="4xl" @close="close">
    <div class="space-y-6">
      <!-- Section: Header Info -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-6 p-6 bg-gray-50 dark:bg-gray-800/50 rounded-3xl border border-gray-100 dark:border-gray-800">
        <div class="space-y-4">
          <div>
            <label class="block text-[10px] font-black text-gray-400 uppercase tracking-widest mb-1.5 ml-1">Tedarikçi Seçimi</label>
            <SupplierSelect v-model="form.supplierId" />
          </div>
          <div>
            <label class="block text-[10px] font-black text-gray-400 uppercase tracking-widest mb-1.5 ml-1">Sipariş Notu</label>
            <input 
              v-model="form.note" 
              type="text" 
              placeholder="Örn: Acil teslimat ricası..."
              class="w-full border-gray-200 dark:border-gray-700 rounded-xl px-4 py-2.5 text-sm dark:bg-gray-800 focus:ring-2 focus:ring-indigo-500 outline-none" 
            />
          </div>
        </div>

        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-[10px] font-black text-gray-400 uppercase tracking-widest mb-1.5 ml-1">Sipariş Tarihi</label>
            <input 
              v-model="form.orderDate" 
              type="date" 
              class="w-full border-gray-200 dark:border-gray-700 rounded-xl px-4 py-2.5 text-sm dark:bg-gray-800 focus:ring-2 focus:ring-indigo-500 outline-none font-bold" 
            />
          </div>
          <div>
            <label class="block text-[10px] font-black text-gray-400 uppercase tracking-widest mb-1.5 ml-1">Termin Tarihi</label>
            <input 
              v-model="form.expectedDeliveryDate" 
              type="date" 
              class="w-full border-gray-200 dark:border-gray-700 rounded-xl px-4 py-2.5 text-sm dark:bg-gray-800 focus:ring-2 focus:ring-indigo-500 outline-none font-bold text-indigo-600 dark:text-indigo-400" 
            />
          </div>
          <div class="col-span-2 flex items-center justify-center p-4 border-2 border-dashed border-gray-200 dark:border-gray-700 rounded-2xl text-gray-400">
             <div class="text-center">
                <p class="text-[10px] font-black uppercase tracking-widest">Sipariş Durumu</p>
                <p class="text-sm font-bold text-gray-500">TASLAK</p>
             </div>
          </div>
        </div>
      </div>

      <!-- Section: Lines -->
      <div class="space-y-4">
        <div class="flex items-center justify-between px-2">
           <h4 class="text-sm font-black text-gray-400 uppercase tracking-widest flex items-center gap-2">
              Sipariş Kalemleri
              <span class="bg-indigo-100 dark:bg-indigo-900/40 text-indigo-600 dark:text-indigo-400 px-2 py-0.5 rounded-full text-[10px] font-black">{{ form.lines.length }}</span>
           </h4>
           <button 
             @click="addLine" 
             type="button" 
             class="px-4 py-2 bg-indigo-50 dark:bg-indigo-900/20 text-indigo-600 dark:text-indigo-400 rounded-xl text-xs font-black uppercase tracking-widest hover:bg-indigo-100 transition-all flex items-center gap-2"
           >
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
              Yeni Satır
           </button>
        </div>

        <div class="bg-white dark:bg-gray-900 rounded-3xl border border-gray-100 dark:border-gray-800 overflow-hidden shadow-sm">
          <table class="min-w-full divide-y divide-gray-100 dark:divide-gray-800">
             <thead class="bg-gray-50/50 dark:bg-gray-800/50">
                <tr>
                   <th class="px-6 py-4 text-left text-[10px] font-black text-gray-400 uppercase tracking-widest">Ürün Ara</th>
                   <th class="px-4 py-4 text-center text-[10px] font-black text-gray-400 uppercase tracking-widest">Miktar</th>
                   <th class="px-4 py-4 text-left text-[10px] font-black text-gray-400 uppercase tracking-widest">Birim</th>
                   <th class="px-6 py-4"></th>
                </tr>
             </thead>
             <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
                <tr v-for="(line, index) in form.lines" :key="line.tempId" class="group transition-colors hover:bg-gray-50/50 dark:hover:bg-gray-800/30">
                   <td class="px-6 py-4 min-w-[280px]">
                      <StockCombobox v-model="line.stockMasterId" @select="onStockSelect(line, $event)" />
                   </td>
                   <td class="px-4 py-4">
                      <input 
                        v-model.number="line.orderedQty" 
                        type="number" 
                        step="0.01" 
                        class="w-full text-center border-gray-100 dark:border-gray-800 rounded-xl px-2 py-2 text-sm dark:bg-gray-800 font-black focus:ring-2 focus:ring-indigo-500 outline-none" 
                        placeholder="0.00"
                      />
                   </td>
                   <td class="px-4 py-4">
                      <span class="text-xs font-black text-gray-400 uppercase tracking-widest bg-gray-50 dark:bg-gray-800 px-3 py-1.5 rounded-lg">{{ line.unit || '-' }}</span>
                   </td>
                   <td class="px-6 py-4 text-right">
                      <button 
                        @click="removeLine(index)" 
                        class="p-2 text-gray-300 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-xl transition-all"
                      >
                         <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                      </button>
                   </td>
                </tr>
                <tr v-if="form.lines.length === 0">
                   <td colspan="4" class="px-6 py-12 text-center text-sm font-bold text-gray-400 italic">Henüz kalem eklemediniz.</td>
                </tr>
             </tbody>
          </table>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex items-center justify-between w-full">
         <p class="text-[10px] font-black text-gray-400 uppercase tracking-widest hidden sm:block">
            * Yıldızlı alanlar zorunludur
         </p>
         <div class="flex gap-3 w-full sm:w-auto">
            <button 
              @click="close" 
              class="flex-1 sm:flex-none px-6 py-3 text-xs font-black text-gray-500 uppercase tracking-widest hover:bg-gray-100 dark:hover:bg-gray-800 rounded-xl transition-all"
            >
              Vazgeç
            </button>
            <button 
              @click="save" 
              class="flex-1 sm:flex-none px-10 py-3 bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-black uppercase tracking-widest rounded-xl shadow-lg shadow-indigo-100 dark:shadow-none transition-all hover:scale-[1.02] active:scale-95"
            >
              Siparişi Kaydet
            </button>
         </div>
      </div>
    </template>
  </BaseModal>
</template>

<script setup lang="ts">
import { reactive } from 'vue';
import purchaseOrderService from '../services/purchaseOrderService';
import SupplierSelect from './SupplierSelect.vue';
import StockCombobox from './StockCombobox.vue';
import BaseModal from './BaseModal.vue';
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

const generateId = () => Math.random().toString(36).substring(2, 15);

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
    if (!form.supplierId) {
        notificationStore.add('Lütfen bir tedarikçi seçin.', 'warning');
        return;
    }
    
    const validLines = form.lines.filter(l => l.stockMasterId > 0 && l.orderedQty > 0);

    if (validLines.length === 0) {
        notificationStore.add('Lütfen geçerli kalemler (stok ve miktar) giriniz.', 'warning');
        return;
    }

    try {
        const payload = {
            supplierId: form.supplierId,
            orderDate: form.orderDate as string,
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
        notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'Sipariş oluşturulurken hata oluştu.', 'error');
    }
};
</script>
