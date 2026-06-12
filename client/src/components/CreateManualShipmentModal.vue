<template>
  <div v-if="isOpen" class="fixed inset-0 z-[60] overflow-y-auto" role="dialog" aria-modal="true">
    <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
      <div class="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" aria-hidden="true" @click="close"></div>
      <span class="hidden sm:inline-block sm:align-middle sm:h-screen" aria-hidden="true">&#8203;</span>
      <div class="inline-block align-bottom bg-white dark:bg-gray-900 rounded-lg px-4 pt-5 pb-4 text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-4xl sm:w-full sm:p-6">
        <h3 class="text-lg font-medium text-gray-900 dark:text-gray-100">Manuel Sevkiyat Oluştur</h3>

        <!-- Customer + delivery -->
        <div class="mt-4 grid grid-cols-1 sm:grid-cols-3 gap-4">
          <div class="sm:col-span-2">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Müşteri <span class="text-red-500">*</span></label>
            <div class="relative">
              <input
                v-model="customerSearch"
                @focus="customerDropdownOpen = true"
                @input="onCustomerSearch"
                type="text"
                placeholder="Müşteri kodu veya adı..."
                class="mt-1 input"
              />
              <div v-if="customerDropdownOpen && customerResults.length" class="absolute z-10 mt-1 w-full max-h-64 overflow-y-auto bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700 rounded-md shadow-lg">
                <button
                  v-for="c in customerResults"
                  :key="c.id"
                  @click="selectCustomer(c)"
                  type="button"
                  class="w-full text-left px-3 py-2 hover:bg-gray-100 dark:hover:bg-gray-700 text-sm border-b border-gray-100 dark:border-gray-700 last:border-b-0"
                >
                  <span class="font-mono text-xs text-gray-500">{{ c.code }}</span>
                  &mdash; {{ c.name }}
                </button>
              </div>
            </div>
            <p v-if="selectedCustomer" class="mt-1 text-xs text-emerald-600 dark:text-emerald-400">
              Seçildi: {{ selectedCustomer.code }} — {{ selectedCustomer.name }}
              <span v-if="!selectedCustomer.netsisCariKodu" class="text-red-500 ml-2">(Netsis cari kodu eksik!)</span>
            </p>
          </div>

          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Teslim Tarihi <span class="text-red-500">*</span></label>
            <input v-model="deliveryDate" type="date" class="mt-1 input" />
          </div>
        </div>

        <div class="mt-4 flex items-center gap-2">
          <input id="requiresWarehouse" v-model="requiresWarehousePreparation" type="checkbox" class="rounded" />
          <label for="requiresWarehouse" class="text-sm text-gray-700 dark:text-gray-300">
            Depo hazırlığı gerekli (işaretliyse sevkiyat zone'a alınır, değilse doğrudan sevke hazır olur)
          </label>
        </div>

        <!-- Lines -->
        <div class="mt-6">
          <div class="flex justify-between items-center mb-2">
            <h4 class="text-sm font-semibold text-gray-900 dark:text-gray-100">Kalemler</h4>
            <button @click="showStockPicker = true" type="button" class="px-3 py-1.5 bg-blue-600 text-white text-sm rounded-md hover:bg-blue-700">
              + Satır Ekle
            </button>
          </div>

          <div v-if="lines.length === 0" class="text-center py-6 text-sm text-gray-500 border border-dashed border-gray-300 dark:border-gray-700 rounded-md">
            Henüz kalem eklenmedi.
          </div>
          <table v-else class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 border border-gray-200 dark:border-gray-700 rounded-md">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Stok Kodu</th>
                <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Stok Adı</th>
                <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Birim</th>
                <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 uppercase">Miktar</th>
                <th class="px-3 py-2"></th>
              </tr>
            </thead>
            <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
              <tr v-for="(line, idx) in lines" :key="idx">
                <td class="px-3 py-2 text-sm font-mono">{{ line.stockCode }}</td>
                <td class="px-3 py-2 text-sm">{{ line.stockName }}</td>
                <td class="px-3 py-2 text-sm text-gray-500">{{ line.unit }}</td>
                <td class="px-3 py-2 text-sm">
                  <input v-model.number="line.qty" type="number" min="0.001" step="any" class="w-24 input py-1" />
                </td>
                <td class="px-3 py-2 text-right">
                  <button @click="removeLine(idx)" type="button" class="text-red-600 hover:text-red-800 text-sm">Sil</button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="mt-4">
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Not (Opsiyonel)</label>
          <textarea v-model="notes" rows="2" class="mt-1 input"></textarea>
        </div>

        <div class="mt-6 sm:grid sm:grid-cols-2 sm:gap-3">
          <button @click="save" :disabled="saving" type="button" class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-emerald-600 text-base font-medium text-white hover:bg-emerald-700 disabled:opacity-50 sm:col-start-2 sm:text-sm">
            {{ saving ? 'Oluşturuluyor...' : 'Sevkiyatı Oluştur' }}
          </button>
          <button @click="close" :disabled="saving" type="button" class="mt-3 w-full inline-flex justify-center rounded-md border border-gray-300 dark:border-gray-700 shadow-sm px-4 py-2 bg-white dark:bg-gray-800 text-base font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50 sm:mt-0 sm:col-start-1 sm:text-sm">
            İptal
          </button>
        </div>

        <!-- Stock picker -->
        <StockPickerModal
          :is-open="showStockPicker"
          @close="showStockPicker = false"
          @selected="onStockSelected"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useNotification } from '../composables/useNotification';
import customerService, { type Customer } from '../services/customerService';
import shipmentService from '../services/shipmentService';
import StockPickerModal from './StockPickerModal.vue';
import { ApiErrorUtils } from '../utils/apiError';

const props = defineProps<{ isOpen: boolean }>();
const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'created'): void;
}>();

const { notify } = useNotification();

const customerSearch = ref('');
const customerDropdownOpen = ref(false);
const customerResults = ref<Customer[]>([]);
const selectedCustomer = ref<Customer | null>(null);

const deliveryDate = ref(new Date().toISOString().slice(0, 10));
const requiresWarehousePreparation = ref(false);
const notes = ref('');

interface DraftLine {
  stockMasterId: number;
  stockCode: string;
  stockName: string;
  unit: string;
  qty: number;
}

const lines = ref<DraftLine[]>([]);
const showStockPicker = ref(false);
const saving = ref(false);

let searchDebounce: any = null;

function reset() {
  customerSearch.value = '';
  customerResults.value = [];
  selectedCustomer.value = null;
  deliveryDate.value = new Date().toISOString().slice(0, 10);
  requiresWarehousePreparation.value = false;
  notes.value = '';
  lines.value = [];
  customerDropdownOpen.value = false;
}

watch(() => props.isOpen, (open) => {
  if (open) reset();
});

function onCustomerSearch() {
  clearTimeout(searchDebounce);
  searchDebounce = setTimeout(async () => {
    if (!customerSearch.value.trim()) {
      customerResults.value = [];
      return;
    }
    try {
      const res = await customerService.getAll({
        search: customerSearch.value.trim(),
        pageSize: 10,
        showInactive: false,
      });
      customerResults.value = res.items;
      customerDropdownOpen.value = true;
    } catch {
      customerResults.value = [];
    }
  }, 250);
}

function selectCustomer(c: Customer) {
  selectedCustomer.value = c;
  customerSearch.value = `${c.code} — ${c.name}`;
  customerDropdownOpen.value = false;
}

function onStockSelected(stock: { id: number; stockCode: string; stockName: string; unit: string }) {
  const existing = lines.value.find(l => l.stockMasterId === stock.id);
  if (existing) {
    existing.qty += 1;
  } else {
    lines.value.push({
      stockMasterId: stock.id,
      stockCode: stock.stockCode,
      stockName: stock.stockName,
      unit: stock.unit,
      qty: 1,
    });
  }
  showStockPicker.value = false;
}

function removeLine(idx: number) {
  lines.value.splice(idx, 1);
}

function close() {
  emit('close');
}

async function save() {
  if (!selectedCustomer.value) {
    notify.error('Lütfen bir müşteri seçin.');
    return;
  }
  if (!selectedCustomer.value.netsisCariKodu) {
    notify.error('Seçilen müşterinin Netsis Cari Kodu eksik. Önce müşteri kartını düzenleyin.');
    return;
  }
  if (!deliveryDate.value) {
    notify.error('Teslim tarihi zorunludur.');
    return;
  }
  if (lines.value.length === 0) {
    notify.error('En az bir kalem eklemelisiniz.');
    return;
  }
  if (lines.value.some(l => !l.qty || l.qty <= 0)) {
    notify.error('Tüm kalemlerin miktarı sıfırdan büyük olmalı.');
    return;
  }

  saving.value = true;
  try {
    await shipmentService.createManual({
      customerId: selectedCustomer.value.id,
      deliveryDate: deliveryDate.value,
      requiresWarehousePreparation: requiresWarehousePreparation.value,
      lines: lines.value.map(l => ({ stockMasterId: l.stockMasterId, qty: l.qty })),
      notes: notes.value.trim() || null,
    });
    notify.success('Manuel sevkiyat başarıyla oluşturuldu.');
    emit('created');
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Sevkiyat oluşturulamadı.'));
  } finally {
    saving.value = false;
  }
}
</script>

<style scoped>
.input {
  @apply bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-blue-500 focus:border-blue-500 sm:text-sm;
}
</style>
