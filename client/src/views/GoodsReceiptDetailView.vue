<template>
  <div class="space-y-4">
    <!-- Loading -->
    <div v-if="loading && !receipt" class="flex justify-center py-16">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="bg-red-50 dark:bg-red-900/20 rounded-xl border border-red-200 dark:border-red-800 p-6 text-center">
      <p class="text-red-700 dark:text-red-300 font-semibold">{{ error }}</p>
      <button @click="fetchReceipt" class="mt-3 text-sm text-red-600 underline">Tekrar dene</button>
    </div>

    <template v-else-if="receipt">
      <!-- Breadcrumb -->
      <div class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
        <router-link to="/goods-receipts" class="hover:text-blue-600 dark:hover:text-blue-400 transition-colors">Mal Kabul İrsaliyeleri</router-link>
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
        <span class="text-gray-900 dark:text-gray-100 font-semibold">{{ receipt.waybillNo }}</span>
      </div>

      <!-- Header card -->
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-4 sm:p-6">
        <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
          <div class="min-w-0">
            <div class="flex items-center gap-3 flex-wrap">
              <h1 class="text-lg font-bold text-gray-900 dark:text-gray-100">{{ receipt.supplierNameSnapshot }}</h1>
              <StatusBadge :status="receipt.status" type="goodsReceipt" />
            </div>
            <p class="text-sm font-mono text-gray-500 dark:text-gray-400 mt-1">İrsaliye No: {{ receipt.waybillNo }}</p>
            <div class="flex flex-wrap gap-x-4 gap-y-1 mt-2 text-sm text-gray-500 dark:text-gray-400">
              <span>İrsaliye Tarihi: <strong class="text-gray-900 dark:text-gray-100">{{ formatDate(receipt.waybillDate) }}</strong></span>
              <span v-if="receipt.receiptDate">Teslim: <strong class="text-gray-900 dark:text-gray-100">{{ formatDate(receipt.receiptDate) }}</strong></span>
              <span v-if="receipt.purchaseOrderNumber">
                PO:
                <router-link
                  v-if="receipt.purchaseOrderId"
                  :to="{ name: 'PurchaseOrderDetail', params: { id: receipt.purchaseOrderId } }"
                  class="font-semibold text-blue-600 dark:text-blue-400 hover:underline"
                >{{ receipt.purchaseOrderNumber }}</router-link>
                <strong v-else class="text-gray-900 dark:text-gray-100">{{ receipt.purchaseOrderNumber }}</strong>
              </span>
            </div>
            <div v-if="receipt.note" class="mt-2 text-sm text-gray-600 dark:text-gray-400 italic">{{ receipt.note }}</div>
          </div>

          <!-- Actions -->
          <div class="flex flex-wrap gap-2 shrink-0">
            <!-- Draft actions -->
            <template v-if="receipt.status === 'Draft'">
              <button
                v-if="canPost"
                @click="handlePost"
                :disabled="actionLoading"
                class="px-4 py-2 text-sm font-semibold text-white bg-green-600 hover:bg-green-700 rounded-lg transition-colors disabled:opacity-50"
              >
                Post Et
              </button>
              <button
                v-if="canPost"
                @click="handleCancel"
                :disabled="actionLoading"
                class="px-4 py-2 text-sm font-semibold text-white bg-red-600 hover:bg-red-700 rounded-lg transition-colors disabled:opacity-50"
              >
                İptal Et
              </button>
            </template>

            <!-- Posted actions -->
            <template v-if="receipt.status === 'Posted'">
              <button
                v-if="canCreateCorrection"
                @click="handleCreateCorrection"
                :disabled="actionLoading"
                class="px-4 py-2 text-sm font-semibold text-white bg-orange-600 hover:bg-orange-700 rounded-lg transition-colors disabled:opacity-50"
              >
                Düzeltme İrsaliyesi Oluştur
              </button>
            </template>

            <!-- Print button -->
            <router-link
              :to="{ name: 'GoodsReceiptPrint', params: { id: receipt.id } }"
              target="_blank"
              class="px-4 py-2 text-sm font-semibold text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg border border-gray-200 dark:border-gray-700 transition-colors inline-flex items-center gap-1.5"
            >
              <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
              </svg>
              Yazdır
            </router-link>
          </div>
        </div>
      </div>

      <!-- Lines table -->
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
        <div class="px-4 sm:px-6 py-3 border-b border-gray-200 dark:border-gray-700">
          <h2 class="text-sm font-bold text-gray-700 dark:text-gray-300 uppercase tracking-wider">İrsaliye Kalemleri</h2>
        </div>

        <!-- Desktop table -->
        <div class="hidden sm:block overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">Ürün</th>
                <th class="px-4 py-3 text-right text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">Sipariş</th>
                <th class="px-4 py-3 text-right text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">Teslim Alınan</th>
                <th class="px-4 py-3 text-right text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">Kabul</th>
                <th class="px-4 py-3 text-right text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">Red</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">Red Nedeni</th>
                <th v-if="receipt.status === 'Draft'" class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr v-if="!receipt.lines || receipt.lines.length === 0">
                <td colspan="7" class="px-4 py-8 text-center text-sm text-gray-500 dark:text-gray-400">Kalem bulunamadı.</td>
              </tr>
              <tr v-for="line in receipt.lines" :key="line.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
                <td class="px-4 py-3">
                  <p class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ line.stockNameSnapshot }}</p>
                  <p class="text-xs text-gray-400 dark:text-gray-600 font-mono">{{ line.unitSnapshot }}</p>
                </td>
                <td class="px-4 py-3 text-right text-sm text-gray-500 dark:text-gray-400">{{ line.orderedQty ?? '-' }}</td>
                <td class="px-4 py-3 text-right text-sm font-bold text-gray-900 dark:text-gray-100">{{ line.receivedQty ?? 0 }}</td>
                <td class="px-4 py-3 text-right text-sm font-bold" :class="(line.rejectedQty ?? 0) > 0 ? 'text-orange-600' : 'text-green-600'">
                  {{ calcAccepted(line) }}
                </td>
                <td class="px-4 py-3 text-right text-sm font-bold" :class="(line.rejectedQty ?? 0) > 0 ? 'text-red-600' : 'text-gray-400 dark:text-gray-600'">
                  {{ line.rejectedQty ?? 0 }}
                </td>
                <td class="px-4 py-3 text-sm text-gray-500 dark:text-gray-400">{{ line.rejectReason || '-' }}</td>
                <td v-if="receipt.status === 'Draft'" class="px-4 py-3 text-right">
                  <div class="flex items-center justify-end gap-1">
                    <button
                      @click="openEditLine(line)"
                      class="px-2.5 py-1.5 text-xs font-semibold text-blue-600 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40 rounded-lg border border-blue-200 dark:border-blue-800 transition-colors"
                    >
                      Miktar Gir
                    </button>
                    <button
                      @click="confirmRemoveLine(line)"
                      class="p-1.5 text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
                      title="Sil"
                    >
                      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                      </svg>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Mobile cards -->
        <div class="sm:hidden divide-y divide-gray-200 dark:divide-gray-700">
          <div v-if="!receipt.lines || receipt.lines.length === 0" class="px-4 py-8 text-center text-sm text-gray-500 dark:text-gray-400">
            Kalem bulunamadı.
          </div>
          <div v-for="line in receipt.lines" :key="line.id" class="px-4 py-3">
            <div class="flex justify-between items-start gap-2 mb-2">
              <div class="min-w-0">
                <p class="text-sm font-semibold text-gray-900 dark:text-gray-100 leading-tight">{{ line.stockNameSnapshot }}</p>
                <p class="text-xs text-gray-500 dark:text-gray-400 font-mono">{{ line.unitSnapshot }}</p>
              </div>
              <div v-if="receipt.status === 'Draft'" class="flex gap-1 shrink-0">
                <button @click="openEditLine(line)" class="px-2 py-1 text-xs font-semibold text-blue-600 bg-blue-50 dark:bg-blue-900/20 rounded-lg border border-blue-200 dark:border-blue-800">Gir</button>
                <button @click="confirmRemoveLine(line)" class="p-1.5 text-red-500 rounded-lg">
                  <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                </button>
              </div>
            </div>
            <div class="grid grid-cols-4 gap-1.5 text-center">
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg py-1.5 border border-gray-200 dark:border-gray-700">
                <p class="text-[10px] text-gray-400 uppercase font-bold">Sipariş</p>
                <p class="text-xs font-bold text-gray-900 dark:text-gray-100">{{ line.orderedQty ?? '-' }}</p>
              </div>
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg py-1.5 border border-gray-200 dark:border-gray-700">
                <p class="text-[10px] text-gray-400 uppercase font-bold">Gelen</p>
                <p class="text-xs font-bold text-gray-900 dark:text-gray-100">{{ line.receivedQty ?? 0 }}</p>
              </div>
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg py-1.5 border border-gray-200 dark:border-gray-700">
                <p class="text-[10px] text-gray-400 uppercase font-bold">Kabul</p>
                <p class="text-xs font-bold" :class="(line.rejectedQty ?? 0) > 0 ? 'text-orange-600' : 'text-green-600'">{{ calcAccepted(line) }}</p>
              </div>
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg py-1.5 border border-gray-200 dark:border-gray-700">
                <p class="text-[10px] text-gray-400 uppercase font-bold">Red</p>
                <p class="text-xs font-bold text-red-600">{{ line.rejectedQty ?? 0 }}</p>
              </div>
            </div>
            <div v-if="line.rejectReason" class="mt-1.5 text-xs text-red-500 italic">Red: {{ line.rejectReason }}</div>
          </div>
        </div>
      </div>
    </template>

    <!-- EditGoodsReceiptLineModal -->
    <EditGoodsReceiptLineModal
      v-if="editingLine"
      :isOpen="showEditLineModal"
      :goodsReceiptId="receipt?.id"
      :line="editingLine"
      @close="showEditLineModal = false; editingLine = null"
      @saved="onLineSaved"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import goodsReceiptService from '../services/goodsReceiptService';
import { useNotificationStore } from '../stores/notification';
import { useAuthStore } from '../stores/auth';
import { ApiErrorUtils } from '../utils/apiError';
import StatusBadge from '../components/StatusBadge.vue';
import EditGoodsReceiptLineModal from '../components/EditGoodsReceiptLineModal.vue';

const route = useRoute();
const router = useRouter();
const notificationStore = useNotificationStore();
const authStore = useAuthStore();

const receipt = ref<any>(null);
const loading = ref(false);
const error = ref('');
const actionLoading = ref(false);

const showEditLineModal = ref(false);
const editingLine = ref<any>(null);

const canPost = computed(() => ['Admin', 'Warehouse', 'Manager'].includes(authStore.userRole));
const canCreateCorrection = computed(() => ['Admin', 'Manager'].includes(authStore.userRole));

const formatDate = (date: string) => {
  if (!date) return '-';
  return new Date(date).toLocaleDateString('tr-TR');
};

const calcAccepted = (line: any) => (line.receivedQty ?? 0) - (line.rejectedQty ?? 0);

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

const handlePost = async () => {
  const ok = await notificationStore.promptConfirm({
    title: 'İrsaliyeyi Postala',
    message: 'Stok güncellenecek. Bu işlem geri alınamaz. Onaylıyor musunuz?',
    confirmText: 'Postala',
    type: 'danger'
  });
  if (!ok) return;
  actionLoading.value = true;
  try {
    await goodsReceiptService.post(String(route.params.id));
    notificationStore.add('İrsaliye postalandı.', 'success');
    await fetchReceipt();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Post işlemi başarısız.', 'error');
  } finally {
    actionLoading.value = false;
  }
};

const handleCancel = async () => {
  const ok = await notificationStore.promptConfirm({
    title: 'İrsaliye İptal',
    message: 'Mal kabul irsaliyesini iptal etmek istiyor musunuz?',
    confirmText: 'İptal Et',
    type: 'danger'
  });
  if (!ok) return;
  actionLoading.value = true;
  try {
    await goodsReceiptService.cancel(String(route.params.id));
    notificationStore.add('İrsaliye iptal edildi.', 'success');
    await fetchReceipt();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İptal başarısız.', 'error');
  } finally {
    actionLoading.value = false;
  }
};

const handleCreateCorrection = async () => {
  const ok = await notificationStore.promptConfirm({
    title: 'Düzeltme İrsaliyesi',
    message: 'Stok düzeltmesi yapılacak. Düzeltme irsaliyesi oluşturulsun mu?',
    confirmText: 'Oluştur',
    type: 'warning'
  });
  if (!ok) return;
  actionLoading.value = true;
  try {
    const res = await goodsReceiptService.createCorrection(String(route.params.id));
    notificationStore.add('Düzeltme irsaliyesi oluşturuldu.', 'success');
    router.push({ name: 'GoodsReceiptDetail', params: { id: String(res.id) } });
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Düzeltme oluşturulamadı.', 'error');
  } finally {
    actionLoading.value = false;
  }
};

const openEditLine = (line: any) => {
  editingLine.value = line;
  showEditLineModal.value = true;
};

const onLineSaved = async () => {
  showEditLineModal.value = false;
  editingLine.value = null;
  await fetchReceipt();
};

const confirmRemoveLine = async (line: any) => {
  const ok = await notificationStore.promptConfirm({
    title: 'Kalemi Sil',
    message: `"${line.stockNameSnapshot}" kalemi silinecek. Emin misiniz?`,
    confirmText: 'Sil',
    type: 'danger'
  });
  if (!ok) return;
  try {
    await goodsReceiptService.removeLine(String(route.params.id), line.id);
    notificationStore.add('Kalem silindi.', 'success');
    await fetchReceipt();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kalem silinemedi.', 'error');
  }
};

onMounted(fetchReceipt);
</script>
