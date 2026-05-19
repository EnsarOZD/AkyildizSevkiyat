<template>
  <div class="min-h-screen bg-white p-6 print:p-4">
    <!-- Print button (hidden in print) -->
    <div class="print:hidden mb-6 flex items-center gap-3">
      <button
        @click="window.print()"
        class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-semibold transition-colors"
      >
        Yazdır
      </button>
      <router-link
        :to="{ name: 'GoodsReceiptDetail', params: { id: route.params.id } }"
        class="px-4 py-2 text-sm font-semibold text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-lg transition-colors"
      >
        &larr; Geri Dön
      </router-link>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-16 print:hidden">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="text-center py-8 print:hidden">
      <p class="text-red-600 font-semibold">{{ error }}</p>
    </div>

    <!-- Print content -->
    <div v-else-if="receipt" class="max-w-4xl mx-auto">
      <!-- Header -->
      <div class="text-center mb-6 border-b-2 border-gray-900 pb-4">
        <p class="text-xl font-bold text-gray-900 uppercase tracking-wide">AKYILDIZ TESİS HİZMETLERİ</p>
        <p class="text-lg font-bold text-gray-900 mt-1 uppercase">MAL KABUL KONTROL FORMU</p>
      </div>

      <!-- Two-column info -->
      <div class="grid grid-cols-2 gap-6 mb-6">
        <div class="border border-gray-300 rounded p-3">
          <p class="text-xs font-bold text-gray-500 uppercase tracking-wider mb-2">Satınalma Siparişi</p>
          <div class="space-y-1 text-sm">
            <div class="flex gap-2">
              <span class="text-gray-500 w-24">Sipariş No:</span>
              <span class="font-semibold">{{ receipt.purchaseOrderNumber || '-' }}</span>
            </div>
            <div class="flex gap-2">
              <span class="text-gray-500 w-24">Tedarikçi:</span>
              <span class="font-semibold">{{ receipt.supplierNameSnapshot }}</span>
            </div>
            <div class="flex gap-2">
              <span class="text-gray-500 w-24">Sipariş Tarihi:</span>
              <span class="font-semibold">{{ receipt.purchaseOrderDate ? formatDate(receipt.purchaseOrderDate) : '-' }}</span>
            </div>
          </div>
        </div>
        <div class="border border-gray-300 rounded p-3">
          <p class="text-xs font-bold text-gray-500 uppercase tracking-wider mb-2">İrsaliye Bilgileri</p>
          <div class="space-y-1 text-sm">
            <div class="flex gap-2">
              <span class="text-gray-500 w-24">İrsaliye No:</span>
              <span class="font-semibold">{{ receipt.waybillNo }}</span>
            </div>
            <div class="flex gap-2">
              <span class="text-gray-500 w-24">İrsaliye Tarihi:</span>
              <span class="font-semibold">{{ formatDate(receipt.waybillDate) }}</span>
            </div>
            <div class="flex gap-2">
              <span class="text-gray-500 w-24">Teslim Tarihi:</span>
              <span class="font-semibold">{{ receipt.receiptDate ? formatDate(receipt.receiptDate) : '-' }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Lines table -->
      <table class="w-full border-collapse text-sm mb-8">
        <thead>
          <tr class="bg-gray-100">
            <th class="border border-gray-300 px-3 py-2 text-left text-xs font-bold uppercase">#</th>
            <th class="border border-gray-300 px-3 py-2 text-left text-xs font-bold uppercase">Ürün Kodu</th>
            <th class="border border-gray-300 px-3 py-2 text-left text-xs font-bold uppercase">Ürün Adı</th>
            <th class="border border-gray-300 px-3 py-2 text-center text-xs font-bold uppercase">Sipariş Mkt.</th>
            <th class="border border-gray-300 px-3 py-2 text-center text-xs font-bold uppercase">Teslim Alınan</th>
            <th class="border border-gray-300 px-3 py-2 text-center text-xs font-bold uppercase">Kabul</th>
            <th class="border border-gray-300 px-3 py-2 text-center text-xs font-bold uppercase">Red</th>
            <th class="border border-gray-300 px-3 py-2 text-left text-xs font-bold uppercase">Red Nedeni</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="(line, index) in (receipt.lines as any[])"
            :key="line.id"
            :class="(index as number) % 2 === 0 ? 'bg-white' : 'bg-gray-50'"
          >
            <td class="border border-gray-300 px-3 py-2 text-center text-xs text-gray-500">{{ (index as number) + 1 }}</td>
            <td class="border border-gray-300 px-3 py-2 text-xs font-mono text-gray-600">{{ line.stockCode || '' }}</td>
            <td class="border border-gray-300 px-3 py-2 font-medium text-gray-900">{{ line.stockNameSnapshot }}</td>
            <td class="border border-gray-300 px-3 py-2 text-center">{{ line.orderedQty ?? '-' }}</td>
            <td class="border border-gray-300 px-3 py-2 text-center print:bg-yellow-50">
              <!-- Empty for manual fill in draft, show value if posted -->
              <span v-if="receipt.status === 'Posted'">{{ line.receivedQty ?? 0 }}</span>
              <span v-else class="text-gray-300">___</span>
            </td>
            <td class="border border-gray-300 px-3 py-2 text-center text-green-700 font-semibold">
              <span v-if="receipt.status === 'Posted'">{{ calcAccepted(line) }}</span>
              <span v-else class="text-gray-300">___</span>
            </td>
            <td class="border border-gray-300 px-3 py-2 text-center text-red-600">{{ line.rejectedQty ?? 0 }}</td>
            <td class="border border-gray-300 px-3 py-2 text-xs text-gray-500">{{ line.rejectReason || '' }}</td>
          </tr>
          <!-- Empty rows to fill page -->
          <tr v-for="i in emptyRowCount" :key="'empty-' + i" class="h-8">
            <td class="border border-gray-300 px-3 py-2 text-center text-xs text-gray-300">{{ linesLength + i }}</td>
            <td class="border border-gray-300 px-3 py-2"></td>
            <td class="border border-gray-300 px-3 py-2"></td>
            <td class="border border-gray-300 px-3 py-2"></td>
            <td class="border border-gray-300 px-3 py-2"></td>
            <td class="border border-gray-300 px-3 py-2"></td>
            <td class="border border-gray-300 px-3 py-2"></td>
            <td class="border border-gray-300 px-3 py-2"></td>
          </tr>
        </tbody>
      </table>

      <!-- Footer signature section -->
      <div class="grid grid-cols-3 gap-6 mt-8 pt-4 border-t border-gray-300">
        <div class="border border-gray-300 rounded p-3 min-h-[80px]">
          <p class="text-xs font-bold text-gray-500 uppercase mb-2">İrsaliye No</p>
          <p class="text-sm font-semibold">{{ receipt.waybillNo }}</p>
        </div>
        <div class="border border-gray-300 rounded p-3 min-h-[80px]">
          <p class="text-xs font-bold text-gray-500 uppercase mb-2">Tarih</p>
          <p class="text-sm font-semibold">{{ formatDate(receipt.receiptDate || receipt.waybillDate) }}</p>
        </div>
        <div class="border border-gray-300 rounded p-3 min-h-[80px]">
          <p class="text-xs font-bold text-gray-500 uppercase mb-2">Depo Personeli İmza</p>
          <div class="mt-6 border-b border-gray-400 mx-2"></div>
        </div>
      </div>

      <!-- Print note -->
      <div class="mt-4 text-center text-xs text-gray-400 print:hidden">
        Bu form yazdırılarak depo personeli tarafından imzalanmalıdır.
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import goodsReceiptService from '../services/goodsReceiptService';
import { ApiErrorUtils } from '../utils/apiError';
import { formatDate } from '../utils/dateFormat';

const route = useRoute();
const receipt = ref<any>(null);
const loading = ref(false);
const error = ref('');

const window = globalThis.window;

const calcAccepted = (line: any) => (line.receivedQty ?? 0) - (line.rejectedQty ?? 0);

const linesLength = computed(() => (receipt.value?.lines as any[])?.length ?? 0);
const emptyRowCount = computed(() => Math.max(0, 5 - linesLength.value));

const fetchReceipt = async () => {
  loading.value = true;
  error.value = '';
  try {
    receipt.value = await goodsReceiptService.getById(String(route.params.id));
  } catch (e) {
    error.value = ApiErrorUtils.getErrorMessage(e) || 'İrsaliye yüklenemedi.';
  } finally {
    loading.value = false;
  }
};

onMounted(fetchReceipt);
</script>

<style>
@media print {
  body {
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
  }
}
</style>
