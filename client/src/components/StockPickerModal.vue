<template>
  <div v-if="isOpen" class="fixed inset-0 z-[70] overflow-y-auto" role="dialog" aria-modal="true">
    <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
      <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" aria-hidden="true" @click="close"></div>
      <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>
      <div class="inline-block align-bottom bg-white dark:bg-gray-900 rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-2xl sm:w-full sm:p-6">
        <h3 class="text-lg font-medium text-gray-900 dark:text-gray-100">Stok Seç</h3>

        <div class="mt-4">
          <input
            v-model="search"
            @input="onSearch"
            type="text"
            placeholder="Stok kodu, adı veya barkod..."
            class="input"
            autofocus
          />
        </div>

        <div class="mt-4 max-h-96 overflow-y-auto border border-gray-200 dark:border-gray-700 rounded-md">
          <div v-if="loading" class="p-4 text-center text-gray-500 text-sm">Yükleniyor...</div>
          <div v-else-if="results.length === 0" class="p-4 text-center text-gray-500 text-sm">Sonuç yok.</div>
          <button
            v-for="s in results"
            :key="s.id"
            @click="select(s)"
            type="button"
            class="w-full text-left px-4 py-3 hover:bg-gray-100 dark:hover:bg-gray-800 text-sm border-b border-gray-100 dark:border-gray-700 last:border-b-0"
          >
            <div class="flex justify-between items-start">
              <div>
                <div class="font-mono text-xs text-gray-500">{{ s.stockCode }}</div>
                <div class="font-medium text-gray-900 dark:text-gray-100">{{ s.stockName }}</div>
              </div>
              <div class="text-right text-xs text-gray-500">
                <div>{{ s.unit || '—' }}</div>
                <div>Stok: {{ s.onHandQty ?? 0 }}</div>
              </div>
            </div>
          </button>
        </div>

        <div class="mt-4 flex justify-end">
          <button @click="close" type="button" class="px-4 py-2 text-sm border border-gray-300 dark:border-gray-700 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800">
            Kapat
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { stockService, type Stock } from '../services/stockService';

const props = defineProps<{ isOpen: boolean }>();
const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'selected', stock: { id: number; stockCode: string; stockName: string; unit: string }): void;
}>();

const search = ref('');
const results = ref<Stock[]>([]);
const loading = ref(false);
let debounce: any = null;

async function fetchData() {
  loading.value = true;
  try {
    const res = await stockService.getAll({
      search: search.value.trim() || null,
      size: 50,
      isActive: true,
    });
    results.value = res.items;
  } catch {
    results.value = [];
  } finally {
    loading.value = false;
  }
}

function onSearch() {
  clearTimeout(debounce);
  debounce = setTimeout(fetchData, 250);
}

watch(() => props.isOpen, (open) => {
  if (open) {
    search.value = '';
    fetchData();
  }
});

function select(s: Stock) {
  emit('selected', {
    id: s.id,
    stockCode: s.stockCode,
    stockName: s.stockName,
    unit: s.unit || '',
  });
}

function close() {
  emit('close');
}
</script>

<style scoped>
.input {
  @apply bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm;
}
</style>
