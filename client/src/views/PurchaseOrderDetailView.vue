<template>
  <div class="space-y-4">
    <!-- Loading state -->
    <div v-if="loading && !order" class="flex justify-center py-16">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
    </div>

    <!-- Error state -->
    <div v-else-if="error" class="bg-red-50 dark:bg-red-900/20 rounded-xl border border-red-200 dark:border-red-800 p-6 text-center">
      <p class="text-red-700 dark:text-red-300 font-semibold">{{ error }}</p>
      <button @click="fetchOrder" class="mt-3 text-sm text-red-600 underline">Tekrar dene</button>
    </div>

    <template v-else-if="order">
      <!-- Breadcrumb -->
      <div class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
        <router-link to="/purchase-orders" class="hover:text-blue-600 dark:hover:text-blue-400 transition-colors">Satınalma Siparişleri</router-link>
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
        </svg>
        <span class="text-gray-900 dark:text-gray-100 font-semibold">{{ order.orderNumber }}</span>
      </div>

      <!-- Header card -->
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-4 sm:p-6">
        <div class="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
          <div class="min-w-0">
            <div class="flex items-center gap-3 flex-wrap">
              <h1 class="text-lg font-bold text-gray-900 dark:text-gray-100">{{ order.supplierNameSnapshot }}</h1>
              <StatusBadge :status="order.status" type="purchaseOrder" />
            </div>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-1 font-mono">{{ order.orderNumber }}</p>
            <div class="flex flex-wrap gap-x-4 gap-y-1 mt-2 text-sm text-gray-500 dark:text-gray-400">
              <span>Sipariş: <strong class="text-gray-900 dark:text-gray-100">{{ formatDate(order.orderDate) }}</strong></span>
              <span v-if="order.expectedDeliveryDate">Termin: <strong class="text-gray-900 dark:text-gray-100">{{ formatDate(order.expectedDeliveryDate) }}</strong></span>
            </div>
            <div v-if="order.note" class="mt-2 text-sm text-gray-600 dark:text-gray-400 italic">{{ order.note }}</div>
          </div>

          <!-- Action buttons -->
          <div class="flex flex-wrap gap-2 shrink-0">
            <!-- Draft actions -->
            <template v-if="order.status === 'Draft'">
              <button
                v-if="canEdit"
                @click="showEditForm = true"
                class="px-4 py-2 text-sm font-semibold text-blue-700 dark:text-blue-400 bg-blue-50 dark:bg-blue-900/20 hover:bg-blue-100 dark:hover:bg-blue-900/40 rounded-lg border border-blue-200 dark:border-blue-800 transition-colors"
              >
                Düzenle
              </button>
              <button
                v-if="canEdit"
                @click="handleApprove"
                :disabled="actionLoading"
                class="px-4 py-2 text-sm font-semibold text-white bg-green-600 hover:bg-green-700 rounded-lg transition-colors disabled:opacity-50"
              >
                Onayla
              </button>
              <button
                v-if="canEdit"
                @click="handleCancel"
                :disabled="actionLoading"
                class="px-4 py-2 text-sm font-semibold text-white bg-red-600 hover:bg-red-700 rounded-lg transition-colors disabled:opacity-50"
              >
                İptal Et
              </button>
            </template>

            <!-- Approved / PartiallyReceived actions -->
            <template v-if="order.status === 'Approved' || order.status === 'PartiallyReceived'">
              <button
                v-if="canEdit"
                @click="handleClose"
                :disabled="actionLoading"
                class="px-4 py-2 text-sm font-semibold text-white bg-gray-600 hover:bg-gray-700 rounded-lg transition-colors disabled:opacity-50"
              >
                Kapat
              </button>
              <button
                v-if="canEdit"
                @click="handleCancel"
                :disabled="actionLoading"
                class="px-4 py-2 text-sm font-semibold text-white bg-red-600 hover:bg-red-700 rounded-lg transition-colors disabled:opacity-50"
              >
                İptal Et
              </button>
            </template>
          </div>
        </div>

        <!-- Edit form (Draft only) -->
        <div v-if="showEditForm && order.status === 'Draft'" class="mt-4 pt-4 border-t border-gray-200 dark:border-gray-700">
          <h3 class="text-sm font-bold text-gray-700 dark:text-gray-300 uppercase tracking-wider mb-3">Sipariş Bilgileri Düzenle</h3>
          <div class="grid grid-cols-1 sm:grid-cols-3 gap-3">
            <div>
              <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase mb-1">Sipariş Tarihi</label>
              <input v-model="editForm.orderDate" type="date" class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 outline-none" />
            </div>
            <div>
              <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase mb-1">Termin Tarihi</label>
              <input v-model="editForm.expectedDeliveryDate" type="date" class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 outline-none" />
            </div>
            <div>
              <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase mb-1">Not</label>
              <input v-model="editForm.note" type="text" class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 outline-none" placeholder="İsteğe bağlı..." />
            </div>
          </div>
          <div class="flex gap-2 mt-3">
            <button @click="handleUpdate" :disabled="updating" class="px-4 py-2 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg disabled:opacity-50 transition-colors">
              <span v-if="updating">Kaydediliyor...</span>
              <span v-else>Güncelle</span>
            </button>
            <button @click="showEditForm = false" class="px-4 py-2 text-sm font-semibold text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition-colors">İptal</button>
          </div>
        </div>
      </div>

      <!-- Lines -->
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
        <div class="px-4 sm:px-6 py-3 border-b border-gray-200 dark:border-gray-700 flex items-center justify-between">
          <h2 class="text-sm font-bold text-gray-700 dark:text-gray-300 uppercase tracking-wider">Sipariş Kalemleri</h2>
        </div>

        <!-- Desktop -->
        <div class="hidden sm:block overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">Ürün</th>
                <th class="px-4 py-3 text-right text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">Sipariş Miktarı</th>
                <th class="px-4 py-3 text-right text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">Gelen</th>
                <th class="px-4 py-3 text-right text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">Kalan</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">Birim</th>
                <th class="px-4 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase">Not</th>
                <th v-if="order.status === 'Draft'" class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr v-if="!order.lines || order.lines.length === 0">
                <td colspan="7" class="px-4 py-8 text-center text-sm text-gray-500 dark:text-gray-400">Kalem bulunamadı.</td>
              </tr>
              <tr v-for="line in order.lines" :key="line.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
                <td class="px-4 py-3">
                  <p class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ line.stockNameSnapshot }}</p>
                </td>
                <td class="px-4 py-3 text-right">
                  <!-- Edit mode for draft -->
                  <div v-if="order.status === 'Draft' && editingLineId === line.id" class="flex items-center justify-end gap-1">
                    <input
                      v-model.number="editLineForm.orderedQty"
                      type="number" step="0.01" min="0.01"
                      class="w-20 border border-blue-400 rounded px-2 py-1 text-sm text-right dark:bg-gray-800 dark:text-gray-100"
                    />
                    <button @click="saveLineEdit(line)" :disabled="updatingLine" class="p-1 text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20 rounded">
                      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" /></svg>
                    </button>
                    <button @click="editingLineId = null" class="p-1 text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-800 rounded">
                      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
                    </button>
                  </div>
                  <span v-else class="text-sm font-bold text-gray-900 dark:text-gray-100">{{ line.orderedQty }}</span>
                </td>
                <td class="px-4 py-3 text-right text-sm font-bold text-green-600">{{ line.receivedQty ?? 0 }}</td>
                <td class="px-4 py-3 text-right text-sm font-bold text-red-600">{{ line.remainingQty ?? line.orderedQty }}</td>
                <td class="px-4 py-3 text-sm text-gray-500 dark:text-gray-400">{{ line.unitSnapshot }}</td>
                <td class="px-4 py-3 text-sm text-gray-500 dark:text-gray-400">{{ line.note || '-' }}</td>
                <td v-if="order.status === 'Draft'" class="px-4 py-3 text-right">
                  <div class="flex items-center justify-end gap-1">
                    <button
                      v-if="editingLineId !== line.id"
                      @click="startLineEdit(line)"
                      class="p-1.5 text-blue-500 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-lg transition-colors"
                      title="Düzenle"
                    >
                      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                      </svg>
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
          <div v-if="!order.lines || order.lines.length === 0" class="px-4 py-8 text-center text-sm text-gray-500 dark:text-gray-400">
            Kalem bulunamadı.
          </div>
          <div v-for="line in order.lines" :key="line.id" class="px-4 py-3">
            <div class="flex justify-between items-start gap-2">
              <div class="min-w-0 flex-1">
                <p class="text-sm font-semibold text-gray-900 dark:text-gray-100 leading-tight">{{ line.stockNameSnapshot }}</p>
                <p class="text-xs text-gray-500 dark:text-gray-400">{{ line.unitSnapshot }}</p>
              </div>
              <div v-if="order.status === 'Draft'" class="flex gap-1 shrink-0">
                <button @click="startLineEdit(line)" class="p-1.5 text-blue-500 rounded-lg">
                  <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                </button>
                <button @click="confirmRemoveLine(line)" class="p-1.5 text-red-500 rounded-lg">
                  <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                </button>
              </div>
            </div>
            <div class="grid grid-cols-3 gap-2 mt-2 text-center">
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg py-1.5 border border-gray-200 dark:border-gray-700">
                <p class="text-[10px] text-gray-400 uppercase font-bold">Sipariş</p>
                <p v-if="editingLineId === line.id" class="px-1">
                  <input v-model.number="editLineForm.orderedQty" type="number" step="0.01" class="w-full text-center text-xs border border-blue-400 rounded dark:bg-gray-800 dark:text-gray-100" />
                </p>
                <p v-else class="text-sm font-bold text-gray-900 dark:text-gray-100">{{ line.orderedQty }}</p>
              </div>
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg py-1.5 border border-gray-200 dark:border-gray-700">
                <p class="text-[10px] text-gray-400 uppercase font-bold">Gelen</p>
                <p class="text-sm font-bold text-green-600">{{ line.receivedQty ?? 0 }}</p>
              </div>
              <div class="bg-gray-50 dark:bg-gray-800 rounded-lg py-1.5 border border-gray-200 dark:border-gray-700">
                <p class="text-[10px] text-gray-400 uppercase font-bold">Kalan</p>
                <p class="text-sm font-bold text-red-600">{{ line.remainingQty ?? line.orderedQty }}</p>
              </div>
            </div>
            <div v-if="editingLineId === line.id" class="flex gap-2 mt-2">
              <button @click="saveLineEdit(line)" :disabled="updatingLine" class="flex-1 py-1.5 text-xs font-bold text-white bg-green-600 rounded-lg">Kaydet</button>
              <button @click="editingLineId = null" class="flex-1 py-1.5 text-xs font-bold text-gray-600 bg-gray-100 dark:bg-gray-800 rounded-lg">İptal</button>
            </div>
          </div>
        </div>
      </div>

      <!-- Linked GR section -->
      <div v-if="order.goodsReceiptCount > 0" class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-4">
        <h3 class="text-sm font-bold text-gray-700 dark:text-gray-300 uppercase tracking-wider mb-2">Bağlı Mal Kabul İrsaliyeleri</h3>
        <p class="text-sm text-gray-500 dark:text-gray-400">
          Bu siparişe bağlı
          <strong class="text-gray-900 dark:text-gray-100">{{ order.goodsReceiptCount }}</strong>
          adet irsaliye mevcut.
          <router-link to="/goods-receipts" class="ml-1 text-blue-600 hover:underline text-sm">İrsaliye listesine git &rarr;</router-link>
        </p>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import purchaseOrderService from '../services/purchaseOrderService';
import { useNotificationStore } from '../stores/notification';
import { useAuthStore } from '../stores/auth';
import { ApiErrorUtils } from '../utils/apiError';
import StatusBadge from '../components/StatusBadge.vue';

const route = useRoute();
const notificationStore = useNotificationStore();
const authStore = useAuthStore();

const order = ref<any>(null);
const loading = ref(false);
const error = ref('');
const actionLoading = ref(false);
const updating = ref(false);
const updatingLine = ref(false);
const showEditForm = ref(false);
const editingLineId = ref<string | null>(null);

const editForm = ref({ orderDate: '', expectedDeliveryDate: '', note: '' });
const editLineForm = ref({ orderedQty: 0, note: '' });

const canEdit = computed(() => {
  const role = authStore.userRole;
  return ['Admin', 'Accounting', 'Manager'].includes(role);
});

const formatDate = (date: string) => {
  if (!date) return '-';
  return new Date(date).toLocaleDateString('tr-TR');
};

const fetchOrder = async () => {
  loading.value = true;
  error.value = '';
  try {
    order.value = await purchaseOrderService.getById(route.params.id as string);
  } catch (e) {
    error.value = ApiErrorUtils.getErrorMessage(e) || 'Sipariş yüklenemedi.';
  } finally {
    loading.value = false;
  }
};

const handleApprove = async () => {
  const ok = await notificationStore.promptConfirm({
    title: 'Sipariş Onayla',
    message: 'Satınalma siparişini onaylamak istiyor musunuz?',
    confirmText: 'Onayla',
    type: 'info'
  });
  if (!ok) return;
  actionLoading.value = true;
  try {
    await purchaseOrderService.approve(order.value.id);
    notificationStore.add('Sipariş onaylandı.', 'success');
    await fetchOrder();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem hatası.', 'error');
  } finally {
    actionLoading.value = false;
  }
};

const handleClose = async () => {
  const ok = await notificationStore.promptConfirm({
    title: 'Sipariş Kapat',
    message: 'Kalan miktarları almayacaksınız. Siparişi kapatmak istiyor musunuz?',
    confirmText: 'Kapat',
    type: 'warning'
  });
  if (!ok) return;
  actionLoading.value = true;
  try {
    await purchaseOrderService.close(order.value.id);
    notificationStore.add('Sipariş kapatıldı.', 'success');
    await fetchOrder();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem hatası.', 'error');
  } finally {
    actionLoading.value = false;
  }
};

const handleCancel = async () => {
  const ok = await notificationStore.promptConfirm({
    title: 'Sipariş İptal',
    message: 'Sipariş iptal edilecek. Bu işlem geri alınamaz. Emin misiniz?',
    confirmText: 'İptal Et',
    type: 'danger'
  });
  if (!ok) return;
  actionLoading.value = true;
  try {
    await purchaseOrderService.cancel(order.value.id);
    notificationStore.add('Sipariş iptal edildi.', 'success');
    await fetchOrder();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İptal başarısız.', 'error');
  } finally {
    actionLoading.value = false;
  }
};

const handleUpdate = async () => {
  updating.value = true;
  try {
    await purchaseOrderService.update(order.value.id, {
      orderDate: editForm.value.orderDate || undefined,
      expectedDeliveryDate: editForm.value.expectedDeliveryDate || undefined,
      note: editForm.value.note || undefined
    });
    notificationStore.add('Sipariş güncellendi.', 'success');
    showEditForm.value = false;
    await fetchOrder();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Güncelleme başarısız.', 'error');
  } finally {
    updating.value = false;
  }
};

const startLineEdit = (line: any) => {
  editingLineId.value = line.id;
  editLineForm.value = { orderedQty: line.orderedQty, note: line.note ?? '' };
};

const saveLineEdit = async (line: any) => {
  updatingLine.value = true;
  try {
    await purchaseOrderService.updateLine(order.value.id, line.id, {
      orderedQty: editLineForm.value.orderedQty,
      note: editLineForm.value.note || undefined
    });
    notificationStore.add('Kalem güncellendi.', 'success');
    editingLineId.value = null;
    await fetchOrder();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kalem güncellenemedi.', 'error');
  } finally {
    updatingLine.value = false;
  }
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
    await purchaseOrderService.removeLine(order.value.id, line.id);
    notificationStore.add('Kalem silindi.', 'success');
    await fetchOrder();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kalem silinemedi.', 'error');
  }
};

onMounted(() => {
  fetchOrder();
});
</script>
