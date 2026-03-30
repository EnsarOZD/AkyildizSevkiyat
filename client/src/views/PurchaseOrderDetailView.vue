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
      <nav class="flex items-center gap-2 text-xs font-bold text-gray-400 dark:text-gray-500 uppercase tracking-widest mb-2">
        <router-link to="/purchase-orders" class="hover:text-indigo-600 transition-colors">Siparişler</router-link>
        <span class="text-gray-300">/</span>
        <span class="text-indigo-600 dark:text-indigo-400">{{ order.orderNumber }}</span>
      </nav>

      <!-- Main Header Card -->
      <div class="relative overflow-hidden bg-white dark:bg-gray-900 rounded-3xl shadow-xl shadow-indigo-100/50 dark:shadow-none border border-indigo-50 dark:border-indigo-900 group">
        <!-- Decorative gradient -->
        <div class="absolute -top-24 -right-24 h-64 w-64 bg-indigo-500/5 rounded-full blur-3xl group-hover:bg-indigo-500/10 transition-all duration-700"></div>
        
        <div class="relative p-6 sm:p-8">
          <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-6">
            <!-- Left: Info -->
            <div class="space-y-4">
              <div class="flex items-center gap-3">
                <div class="p-3 bg-indigo-600 rounded-2xl text-white shadow-lg shadow-indigo-200 dark:shadow-none">
                  <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" />
                  </svg>
                </div>
                <div>
                  <h1 class="text-2xl font-black text-gray-900 dark:text-gray-100 leading-none tracking-tight">{{ order.supplierNameSnapshot }}</h1>
                  <div class="flex items-center gap-2 mt-2">
                     <StatusBadge :status="order.status" type="purchaseOrder" />
                     <span class="h-1 w-1 rounded-full bg-gray-300 dark:bg-gray-700"></span>
                     <span class="text-xs font-bold text-gray-400 dark:text-gray-500 uppercase tracking-widest">{{ formatDate(order.orderDate) }}</span>
                  </div>
                </div>
              </div>

              <!-- Metadata Grid -->
              <div class="grid grid-cols-2 sm:flex sm:items-center gap-4 sm:gap-8 pt-2">
                <div class="space-y-0.5">
                  <p class="text-[10px] font-black text-gray-400 uppercase tracking-widest">Sipariş No</p>
                  <p class="text-sm font-bold text-indigo-600 dark:text-indigo-400">{{ order.orderNumber }}</p>
                </div>
                <div v-if="order.expectedDeliveryDate" class="space-y-0.5">
                   <p class="text-[10px] font-black text-gray-400 uppercase tracking-widest">Termin Tarihi</p>
                   <p class="text-sm font-bold text-gray-900 dark:text-gray-100">{{ formatDate(order.expectedDeliveryDate) }}</p>
                </div>
                <div class="space-y-0.5">
                   <p class="text-[10px] font-black text-gray-400 uppercase tracking-widest">Kalem Sayısı</p>
                   <p class="text-sm font-bold text-gray-900 dark:text-gray-100">{{ order.lines?.length || 0 }}</p>
                </div>
              </div>
            </div>

            <!-- Right: Stats & Main Actions -->
            <div class="flex flex-col sm:flex-row items-stretch sm:items-center gap-4">
               <!-- Mini Stats -->
               <div class="flex items-center gap-2 bg-gray-50 dark:bg-gray-800/50 p-2 rounded-2xl border border-gray-100 dark:border-gray-800">
                  <div class="px-4 py-2 text-center text-xs">
                    <p class="text-[10px] font-black text-gray-400 uppercase leading-none">Toplam Sipariş</p>
                    <p class="text-lg font-black text-gray-900 dark:text-gray-100 mt-1">{{ totalOrdered }}</p>
                  </div>
                  <div class="w-px h-8 bg-gray-200 dark:bg-gray-700"></div>
                  <div class="px-4 py-2 text-center text-xs">
                    <p class="text-[10px] font-black text-green-500 uppercase leading-none">Gelen</p>
                    <p class="text-lg font-black text-green-600 mt-1">{{ totalReceived }}</p>
                  </div>
               </div>

               <!-- Primary Actions -->
               <div class="flex flex-wrap gap-2">
                  <template v-if="order.status === 'Draft'">
                    <button v-if="canEdit" @click="handleApprove" :disabled="actionLoading" class="px-6 py-3 bg-indigo-600 hover:bg-indigo-700 text-white font-bold rounded-xl shadow-lg shadow-indigo-100 dark:shadow-none transition-all flex items-center justify-center gap-2">
                      <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                      Onayla
                    </button>
                    <button v-if="canEdit" @click="showEditForm = !showEditForm" class="p-3 text-indigo-600 bg-indigo-50 dark:bg-indigo-900/10 hover:bg-indigo-100 dark:hover:bg-indigo-900/20 rounded-xl transition-all">
                      <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                    </button>
                    <button v-if="canEdit" @click="handleCancel" class="p-3 text-red-500 bg-red-50 dark:bg-red-900/10 hover:bg-red-100 rounded-xl transition-all">
                      <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                    </button>
                  </template>
                  <template v-else-if="order.status === 'Approved' || order.status === 'PartiallyReceived'">
                     <button @click="goToMalKabul" class="px-6 py-3 bg-emerald-600 hover:bg-emerald-700 text-white font-bold rounded-xl shadow-lg shadow-emerald-100 dark:shadow-none transition-all flex items-center gap-2">
                       <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" /></svg>
                       Mal Kabul
                     </button>
                     <button v-if="canEdit" @click="handleClose" class="px-6 py-3 bg-gray-800 text-white font-bold rounded-xl transition-all">Kapat</button>
                  </template>
               </div>
            </div>
          </div>

          <!-- Edit form (inline) -->
          <div v-if="showEditForm && order.status === 'Draft'" class="mt-6 pt-6 border-t border-gray-100 dark:border-gray-800 animate-in slide-in-from-top-4 duration-300">
             <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 pb-4">
                <div class="space-y-1">
                  <label class="text-[10px] font-black text-gray-400 uppercase tracking-widest ml-1">Sipariş Tarihi</label>
                  <input v-model="editForm.orderDate" type="date" class="w-full border-gray-200 dark:border-gray-800 rounded-xl px-4 py-2 text-sm dark:bg-gray-800" />
                </div>
                <div class="space-y-1">
                  <label class="text-[10px] font-black text-gray-400 uppercase tracking-widest ml-1">Termin Tarihi</label>
                  <input v-model="editForm.expectedDeliveryDate" type="date" class="w-full border-gray-200 dark:border-gray-800 rounded-xl px-4 py-2 text-sm dark:bg-gray-800" />
                </div>
                <div class="space-y-1">
                  <label class="text-[10px] font-black text-gray-400 uppercase tracking-widest ml-1">Not / Açıklama</label>
                  <input v-model="editForm.note" type="text" class="w-full border-gray-200 dark:border-gray-800 rounded-xl px-4 py-2 text-sm dark:bg-gray-800" placeholder="Opsiyonel..." />
                </div>
             </div>
             <div class="flex justify-end gap-2">
                <button @click="showEditForm = false" class="px-4 py-2 text-xs font-bold text-gray-500 uppercase">Vazgeç</button>
                <button @click="handleUpdate" :disabled="updating" class="px-6 py-2 bg-indigo-600 text-white text-xs font-black uppercase rounded-lg shadow-lg shadow-indigo-100">Güncelle</button>
             </div>
          </div>

          <div v-else-if="order.note" class="mt-6 flex items-start gap-2 p-3 bg-amber-50 dark:bg-amber-900/10 border border-amber-100 dark:border-amber-900/30 rounded-2xl text-amber-800 dark:text-amber-400 text-sm italic">
            <svg class="h-4 w-4 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 8h10M7 12h4m1 8l-4-4H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-3l-4 4z" /></svg>
            {{ order.note }}
          </div>
        </div>
      </div>


      <!-- Lines -->
      <div class="bg-white dark:bg-gray-900 rounded-3xl shadow-sm border border-gray-100 dark:border-gray-800 overflow-hidden">
        <div class="px-4 sm:px-6 py-4 border-b border-gray-100 dark:border-gray-800 flex items-center justify-between">
          <h2 class="text-sm font-black text-gray-400 uppercase tracking-widest flex items-center gap-2">
             Sipariş Kalemleri
             <span class="bg-indigo-100 dark:bg-indigo-900/40 text-indigo-600 dark:text-indigo-400 px-2 py-0.5 rounded-full text-[10px] font-black">{{ order.lines?.length || 0 }}</span>
          </h2>
        </div>

        <!-- Desktop -->
        <div class="hidden sm:block overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-100 dark:divide-gray-800">
            <thead class="bg-gray-50/50 dark:bg-gray-800/50">
              <tr>
                <th class="px-6 py-4 text-left text-[10px] font-black text-gray-400 uppercase tracking-widest">Ürün Bilgisi</th>
                <th class="px-4 py-4 text-center text-[10px] font-black text-gray-400 uppercase tracking-widest">Sipariş Edilen</th>
                <th class="px-4 py-4 text-center text-[10px] font-black text-gray-400 uppercase tracking-widest">Teslim Alınan</th>
                <th class="px-4 py-4 text-center text-[10px] font-black text-gray-400 uppercase tracking-widest">Kalan</th>
                <th v-if="order.status === 'Draft'" class="px-6 py-4 text-right"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
              <tr v-if="!order.lines || order.lines.length === 0">
                <td colspan="5" class="px-6 py-16 text-center text-sm text-gray-400 font-bold uppercase tracking-widest">Henüz kalem eklenmemiş.</td>
              </tr>
              <tr v-for="line in order.lines" :key="line.id" class="hover:bg-indigo-50/30 dark:hover:bg-indigo-900/10 transition-colors group">
                <td class="px-6 py-4">
                  <div class="flex items-center gap-3">
                      <div class="h-10 w-10 shrink-0 bg-gray-100 dark:bg-gray-800 rounded-xl flex items-center justify-center text-gray-400 font-black text-xs">
                        {{ line.stockName?.charAt(0) }}
                      </div>
                      <div class="min-w-0">
                        <p class="text-sm font-black text-gray-900 dark:text-gray-100 leading-none truncate">{{ line.stockName }}</p>
                        <p class="text-[10px] text-gray-500 dark:text-gray-400 font-bold uppercase tracking-widest mt-1.5">{{ line.unit }}</p>
                      </div>
                    </div>
                  </td>
                <td class="px-4 py-4 text-center">
                   <div v-if="order.status === 'Draft' && editingLineId === line.id" class="flex items-center justify-center gap-1">
                      <input v-model.number="editLineForm.orderedQty" type="number" step="0.01" class="w-20 border-indigo-200 rounded-lg px-2 py-1 text-sm text-center dark:bg-gray-800 font-black" />
                      <button @click="saveLineEdit(line)" class="p-1 text-green-600"><svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" /></svg></button>
                   </div>
                   <span v-else class="text-sm font-black text-gray-900 dark:text-gray-100">{{ line.orderedQty }}</span>
                </td>
                <td class="px-4 py-4 text-center">
                   <span class="text-sm font-black text-green-600">{{ line.receivedQty ?? 0 }}</span>
                </td>
                <td class="px-4 py-4 text-center">
                   <span class="text-sm font-black" :class="(line.remainingQty || line.orderedQty) > 0 ? 'text-red-500' : 'text-gray-300'">
                      {{ line.remainingQty ?? line.orderedQty }}
                   </span>
                </td>
                <td v-if="order.status === 'Draft'" class="px-6 py-4 text-right">
                  <div class="flex items-center justify-end gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                    <button @click="startLineEdit(line)" class="p-2 text-indigo-600 hover:bg-indigo-50 dark:hover:bg-indigo-900/20 rounded-xl transition-all">
                      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                    </button>
                    <button @click="confirmRemoveLine(line)" class="p-2 text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-xl transition-all">
                      <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
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
                <p class="text-sm font-semibold text-gray-900 dark:text-gray-100 leading-tight">{{ line.stockName }}</p>
                <p class="text-xs text-gray-500 dark:text-gray-400">{{ line.unit }}</p>
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
import { useRoute, useRouter } from 'vue-router';
import purchaseOrderService from '../services/purchaseOrderService';
import { useNotificationStore } from '../stores/notification';
import { useAuthStore } from '../stores/auth';
import { ApiErrorUtils } from '../utils/apiError';
import StatusBadge from '../components/StatusBadge.vue';

const route = useRoute();
const router = useRouter();
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

const totalOrdered = computed(() => order.value?.lines?.reduce((sum: number, l: any) => sum + (l.orderedQty || 0), 0) || 0);
const totalReceived = computed(() => order.value?.lines?.reduce((sum: number, l: any) => sum + (l.receivedQty || 0), 0) || 0);

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
    message: `"${line.stockName}" kalemi silinecek. Emin misiniz?`,
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

const goToMalKabul = () => {
  router.push({ name: 'MalKabulDashboard', query: { poId: order.value.id } });
};

onMounted(() => {
  fetchOrder();
});
</script>
