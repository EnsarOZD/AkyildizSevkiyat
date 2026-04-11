<template>
  <BaseModal :show="isOpen" title="Yeni Ürün Ekle" @close="$emit('close')">
    <div class="space-y-4">
      <!-- Search input -->
      <div>
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Ürün Ara</label>
        <div class="relative">
          <input
            v-model="searchTerm"
            type="text"
            placeholder="Ürün adı veya kodu yazın..."
            class="w-full border-gray-300 dark:border-gray-700 rounded-lg shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-800 dark:text-gray-100 pr-10"
            @input="debouncedSearch"
          />
          <div v-if="searching" class="absolute inset-y-0 right-0 pr-3 flex items-center">
            <div class="animate-spin h-4 w-4 border-2 border-blue-600 border-t-transparent rounded-full"></div>
          </div>
        </div>
      </div>

      <!-- Search results -->
      <div v-if="results.length > 0" class="max-h-60 overflow-y-auto border border-gray-200 dark:border-gray-700 rounded-lg divide-y divide-gray-200 dark:divide-gray-700">
        <button
          v-for="stock in results"
          :key="stock.id"
          class="w-full px-4 py-3 text-left hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors flex justify-between items-center group"
          :class="{ 'bg-blue-50 dark:bg-blue-900/20 ring-2 ring-inset ring-blue-500': selectedStock?.id === stock.id }"
          @click="selectStock(stock)"
        >
          <div class="min-w-0">
            <p class="text-sm font-bold text-gray-900 dark:text-gray-100">{{ stock.stockName }}</p>
            <p class="text-xs text-gray-500 dark:text-gray-400 font-mono">{{ stock.stockCode }}</p>
          </div>
          <div class="shrink-0 flex items-center gap-2">
            <span class="text-xs font-semibold text-gray-400 dark:text-gray-600 group-hover:text-blue-500">{{ stock.unit }}</span>
            <svg v-if="selectedStock?.id === stock.id" class="h-5 w-5 text-blue-500" viewBox="0 0 20 20" fill="currentColor">
              <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
            </svg>
          </div>
        </button>
      </div>
      <div v-else-if="searchTerm.length > 2 && !searching" class="text-center py-8 bg-gray-50 dark:bg-gray-800/50 rounded-lg">
        <p class="text-sm text-gray-500 dark:text-gray-400 italic">Ürün bulunamadı.</p>
      </div>

      <!-- Add form -->
      <div v-if="selectedStock" class="bg-blue-50 dark:bg-blue-900/10 p-4 rounded-xl border border-blue-100 dark:border-blue-800 space-y-4 animate-in fade-in slide-in-from-top-2">
        <div class="grid grid-cols-2 gap-4">
          <div>
            <label class="block text-xs font-bold text-blue-600 dark:text-blue-400 uppercase tracking-wider mb-1">Miktar ({{ selectedStock.unit }})</label>
            <input
              v-model.number="form.receivedQty"
              type="number"
              min="0.01"
              step="0.01"
              class="w-full border-gray-300 dark:border-gray-700 rounded-lg shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-800 dark:text-gray-100 font-bold"
              placeholder="0.00"
            />
          </div>
          <div class="flex items-end">
             <button
              @click="handleAdd"
              :disabled="!form.receivedQty || adding"
              class="w-full bg-blue-600 hover:bg-blue-700 text-white font-bold py-2 rounded-lg shadow-md shadow-blue-200 dark:shadow-none transition-all flex items-center justify-center gap-2 group active:scale-[0.98] disabled:opacity-50"
            >
              <span v-if="adding" class="h-4 w-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
              <span v-else>Ekle</span>
            </button>
          </div>
        </div>
        <div>
          <label class="block text-xs font-bold text-blue-600 dark:text-blue-400 uppercase tracking-wider mb-1">Not / Açıklama</label>
          <input
            v-model="form.note"
            type="text"
            placeholder="Opsiyonel not..."
            class="w-full border-gray-300 dark:border-gray-700 rounded-lg shadow-sm focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-800 dark:text-gray-100 text-sm"
          />
        </div>
      </div>
    </div>
  </BaseModal>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { stockService, type Stock } from '../services/stockService';
import goodsReceiptService from '../services/goodsReceiptService';
import { useNotificationStore } from '../stores/notification';
import BaseModal from './BaseModal.vue';

const props = defineProps<{
  isOpen: boolean;
  goodsReceiptId: string;
}>();

const emit = defineEmits(['close', 'added']);
const notificationStore = useNotificationStore();

const searchTerm = ref('');
const results = ref<Stock[]>([]);
const searching = ref(false);
const adding = ref(false);
const selectedStock = ref<Stock | null>(null);

const form = reactive({
  receivedQty: null as number | null,
  note: ''
});

let searchTimeout: any = null;
const debouncedSearch = () => {
  if (searchTimeout) clearTimeout(searchTimeout);
  if (searchTerm.value.length < 3) {
    results.value = [];
    return;
  }
  searchTimeout = setTimeout(search, 400);
};

const search = async () => {
  searching.value = true;
  try {
    const res = await stockService.getAll({ search: searchTerm.value, size: 20 });
    results.value = res.items;
  } catch (e) {
    console.error(e);
  } finally {
    searching.value = false;
  }
};

const selectStock = (stock: Stock) => {
  selectedStock.value = stock;
  form.receivedQty = null;
  form.note = '';
};

const handleAdd = async () => {
  if (!selectedStock.value || !form.receivedQty) return;
  
  adding.value = true;
  try {
    await goodsReceiptService.addLine({
      goodsReceiptId: props.goodsReceiptId,
      stockMasterId: selectedStock.value.id,
      receivedQty: form.receivedQty,
      note: form.note
    });
    notificationStore.add('Ürün eklendi.', 'success');
    emit('added');
    reset();
  } catch (e) {
    notificationStore.add('Ürün eklenirken bir hata oluştu.', 'error');
  } finally {
    adding.value = false;
  }
};

const reset = () => {
  searchTerm.value = '';
  results.value = [];
  selectedStock.value = null;
  form.receivedQty = null;
  form.note = '';
};
</script>
