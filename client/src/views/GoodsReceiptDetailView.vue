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
      <nav class="flex items-center gap-2 text-xs font-bold text-gray-400 dark:text-gray-500 uppercase tracking-widest mb-2">
        <router-link to="/goods-receipts" class="hover:text-indigo-600 transition-colors">İrsaliyeler</router-link>
        <span class="text-gray-300">/</span>
        <span class="text-indigo-600 dark:text-indigo-400">{{ receipt.waybillNo }}</span>
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
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01" />
                  </svg>
                </div>
                <div>
                  <h1 class="text-2xl font-black text-gray-900 dark:text-gray-100 leading-none tracking-tight">{{ receipt.supplierNameSnapshot }}</h1>
                  <div class="flex items-center gap-2 mt-2">
                     <StatusBadge :status="receipt.status" type="goodsReceipt" />
                     <span class="h-1 w-1 rounded-full bg-gray-300 dark:bg-gray-700"></span>
                     <span class="text-xs font-bold text-gray-400 dark:text-gray-500 uppercase tracking-widest">{{ formatDate(receipt.waybillDate) }}</span>
                  </div>
                </div>
              </div>

              <!-- Metadata Grid -->
              <div class="grid grid-cols-2 sm:flex sm:items-center gap-4 sm:gap-8 pt-2">
                <div class="space-y-0.5">
                  <p class="text-[10px] font-black text-gray-400 uppercase tracking-widest">İrsaliye No</p>
                  <p class="text-sm font-bold text-indigo-600 dark:text-indigo-400">{{ receipt.waybillNo }}</p>
                </div>
                <div v-if="receipt.purchaseOrderNumber" class="space-y-0.5">
                   <p class="text-[10px] font-black text-gray-400 uppercase tracking-widest">Sipariş No</p>
                   <router-link
                      v-if="receipt.purchaseOrderId"
                      :to="{ name: 'PurchaseOrderDetail', params: { id: receipt.purchaseOrderId } }"
                      class="text-sm font-bold text-gray-900 dark:text-gray-100 hover:text-indigo-600 transition-colors"
                    >#{{ receipt.purchaseOrderNumber }}</router-link>
                    <p v-else class="text-sm font-bold text-gray-900 dark:text-gray-100 italic">Siparişsiz</p>
                </div>
                <div class="space-y-0.5">
                   <p class="text-[10px] font-black text-gray-400 uppercase tracking-widest">Teslim Tarihi</p>
                   <p class="text-sm font-bold text-gray-900 dark:text-gray-100">{{ formatDate(receipt.receiptDate) }}</p>
                </div>
              </div>
            </div>

            <!-- Right: Stats & Main Actions -->
            <div class="flex flex-col sm:flex-row items-stretch sm:items-center gap-4">
               <!-- Mini Stats -->
               <div class="flex items-center gap-2 bg-gray-50 dark:bg-gray-800/50 p-2 rounded-2xl border border-gray-100 dark:border-gray-800">
                  <div class="px-4 py-2 text-center">
                    <p class="text-[10px] font-black text-gray-400 uppercase leading-none">Toplam Gelen</p>
                    <p class="text-lg font-black text-gray-900 dark:text-gray-100">{{ totalReceived }}</p>
                  </div>
                  <div class="w-px h-8 bg-gray-200 dark:bg-gray-700"></div>
                  <div class="px-4 py-2 text-center">
                    <p class="text-[10px] font-black text-green-500 uppercase leading-none">Kabul</p>
                    <p class="text-lg font-black text-green-600">{{ totalAccepted }}</p>
                  </div>
               </div>

               <!-- Vertical divider on large screens -->
               <div class="hidden lg:block w-px h-12 bg-gray-100 dark:bg-gray-800 mx-2"></div>

               <!-- Primary Actions -->
               <div class="flex flex-wrap gap-2">
                  <template v-if="receipt.status === 'Draft'">
                    <button
                      v-if="canPost"
                      @click="handlePost"
                      :disabled="actionLoading"
                      class="flex-1 sm:flex-none px-6 py-3 bg-indigo-600 hover:bg-indigo-700 text-white font-bold rounded-xl shadow-lg shadow-indigo-100 dark:shadow-none transition-all hover:scale-[1.02] active:scale-95 flex items-center justify-center gap-2"
                    >
                      <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                      Postla
                    </button>
                    <button
                      v-if="canPost"
                      @click="handleCancel"
                      :disabled="actionLoading"
                      class="p-3 text-red-500 bg-red-50 dark:bg-red-900/10 hover:bg-red-100 dark:hover:bg-red-900/20 rounded-xl transition-all"
                      title="İptal Et"
                    >
                      <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                    </button>
                  </template>

                  <template v-if="receipt.status === 'Posted'">
                    <button
                      v-if="canCreateCorrection"
                      @click="handleCreateCorrection"
                      :disabled="actionLoading"
                      class="flex-1 sm:flex-none px-6 py-3 bg-orange-500 hover:bg-orange-600 text-white font-bold rounded-xl shadow-lg shadow-orange-100 dark:shadow-none transition-all hover:scale-[1.02] active:scale-95 flex items-center justify-center gap-2"
                    >
                      Düzeltme Oluştur
                    </button>
                  </template>

                  <router-link
                    :to="{ name: 'GoodsReceiptPrint', params: { id: receipt.id } }"
                    target="_blank"
                    class="p-3 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-xl border border-gray-200 dark:border-gray-700 transition-all"
                    title="Yazdır"
                  >
                    <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" /></svg>
                  </router-link>
               </div>
            </div>
          </div>

          <!-- Note bottom -->
          <div v-if="receipt.note" class="mt-6 flex items-start gap-2 p-3 bg-amber-50 dark:bg-amber-900/10 border border-amber-100 dark:border-amber-900/30 rounded-2xl text-amber-800 dark:text-amber-400 text-sm italic">
            <svg class="h-4 w-4 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 8h10M7 12h4m1 8l-4-4H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-3l-4 4z" /></svg>
            {{ receipt.note }}
          </div>
        </div>
      </div>

      <!-- Item Control Section -->
      <div class="space-y-4">
        <div class="flex items-center justify-between px-2">
           <h2 class="text-sm font-black text-gray-400 uppercase tracking-widest flex items-center gap-2">
              İrsaliye Kalemleri
              <span class="bg-indigo-100 dark:bg-indigo-900/40 text-indigo-600 dark:text-indigo-400 px-2 py-0.5 rounded-full text-[10px] font-black">{{ receipt.lines?.length || 0 }}</span>
           </h2>
           <button
             v-if="receipt.status === 'Draft'"
             @click="showAddLineModal = true"
             class="px-4 py-2 bg-white dark:bg-gray-800 text-indigo-600 dark:text-indigo-400 border border-indigo-100 dark:border-indigo-800 rounded-xl text-xs font-bold shadow-sm hover:bg-indigo-50 transition-all flex items-center gap-2 group"
           >
             <svg class="h-4 w-4 group-hover:rotate-90 transition-transform" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" /></svg>
             Ürün Ekle
           </button>
        </div>

        <!-- Lines Table -->
        <div class="bg-white dark:bg-gray-900 rounded-3xl shadow-sm border border-gray-100 dark:border-gray-800 overflow-hidden">
   </div>

        <!-- Desktop table -->
        <div class="hidden sm:block overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50/50 dark:bg-gray-800/50">
              <tr>
                <th class="px-6 py-4 text-left text-[10px] font-black text-gray-400 uppercase tracking-widest">Ürün</th>
                <th class="px-4 py-4 text-center text-[10px] font-black text-gray-400 uppercase tracking-widest">Hedef (Sipariş)</th>
                <th class="px-4 py-4 text-center text-[10px] font-black text-gray-400 uppercase tracking-widest">Mevcut (Gelen)</th>
                <th class="px-4 py-4 text-center text-[10px] font-black text-gray-400 uppercase tracking-widest">Durum</th>
                <th v-if="receipt.status === 'Draft'" class="px-6 py-4 text-right"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr v-if="!receipt.lines || receipt.lines.length === 0">
                <td colspan="5" class="px-6 py-16 text-center">
                   <div class="flex flex-col items-center gap-2">
                      <div class="h-16 w-16 bg-gray-50 dark:bg-gray-800 rounded-full flex items-center justify-center text-gray-300 dark:text-gray-700">
                         <svg class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" /></svg>
                      </div>
                      <p class="text-sm font-bold text-gray-400">Bu irsaliyede henüz hiç kalem bulunmuyor.</p>
                      <button v-if="receipt.status === 'Draft'" @click="showAddLineModal = true" class="text-xs font-black text-indigo-500 uppercase tracking-widest hover:text-indigo-700 transition-colors">Şimdi Ekle &rarr;</button>
                   </div>
                </td>
              </tr>
              <tr v-for="line in receipt.lines" :key="line.id" class="hover:bg-indigo-50/30 dark:hover:bg-indigo-900/10 transition-colors group">
                <td class="px-6 py-4">
                  <div class="flex items-center gap-3">
                     <div class="h-10 w-10 shrink-0 bg-gray-100 dark:bg-gray-800 rounded-xl flex items-center justify-center text-gray-400 font-black text-xs">
                        {{ line.stockNameSnapshot?.charAt(0) }}
                     </div>
                     <div class="min-w-0">
                        <p class="text-sm font-black text-gray-900 dark:text-gray-100 leading-none truncate">{{ line.stockNameSnapshot }}</p>
                        <p class="text-[10px] text-gray-500 dark:text-gray-400 font-bold uppercase tracking-widest mt-1.5">{{ line.unitSnapshot }}</p>
                     </div>
                  </div>
                </td>
                <td class="px-4 py-4 text-center">
                   <div v-if="line.orderedQty" class="inline-flex flex-col items-center">
                      <span class="text-xs font-bold text-gray-900 dark:text-gray-100">{{ line.orderedQty }}</span>
                      <span class="text-[9px] font-black text-gray-400 uppercase tracking-tighter">Sipariş Edilen</span>
                   </div>
                   <span v-else class="text-xs font-black text-gray-300 uppercase italic">Sipariş Dışı</span>
                </td>
                <td class="px-4 py-4 text-center">
                   <div class="inline-flex flex-col items-center px-4 py-2 bg-gray-50 dark:bg-gray-800/50 rounded-xl border border-gray-100 dark:border-gray-800 min-w-[80px]">
                      <span class="text-sm font-black text-gray-900 dark:text-gray-100">{{ line.receivedQty ?? 0 }}</span>
                      <span class="text-[9px] font-black text-gray-400 uppercase tracking-tighter">Miktar</span>
                   </div>
                </td>
                <td class="px-4 py-4">
                   <div class="flex flex-col items-center gap-1">
                      <div class="flex items-center gap-1.5">
                         <span class="h-2 w-2 rounded-full" :class="(line.rejectedQty ?? 0) > 0 ? 'bg-orange-500' : 'bg-green-500'"></span>
                         <span class="text-xs font-black text-gray-900 dark:text-gray-100">Kabul: {{ calcAccepted(line) }}</span>
                      </div>
                      <span v-if="(line.rejectedQty ?? 0) > 0" class="text-[10px] font-bold text-red-500">
                         {{ line.rejectedQty }} Red ({{ line.rejectReason || 'Neden Belirtilmedi' }})
                      </span>
                   </div>
                </td>
                <td v-if="receipt.status === 'Draft'" class="px-6 py-4 text-right">
                  <div class="flex items-center justify-end gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                    <button
                      @click="openEditLine(line)"
                      class="px-3 py-1.5 text-[10px] font-black text-indigo-600 dark:text-indigo-400 bg-white dark:bg-gray-800 border border-indigo-100 dark:border-indigo-800 rounded-lg uppercase tracking-widest hover:bg-indigo-50 transition-colors"
                    >
                      Miktar Gir
                    </button>
                    <button
                      @click="confirmRemoveLine(line)"
                      class="p-2 text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-xl transition-all"
                    >
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

    <!-- AddGoodsReceiptLineModal -->
    <AddGoodsReceiptLineModal
      v-if="receipt"
      :isOpen="showAddLineModal"
      :goodsReceiptId="receipt.id"
      @close="showAddLineModal = false"
      @added="onLineSaved"
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
import AddGoodsReceiptLineModal from '../components/AddGoodsReceiptLineModal.vue';

const route = useRoute();
const router = useRouter();
const notificationStore = useNotificationStore();
const authStore = useAuthStore();

const receipt = ref<any>(null);
const loading = ref(false);
const error = ref('');
const actionLoading = ref(false);

const showEditLineModal = ref(false);
const showAddLineModal = ref(false);
const editingLine = ref<any>(null);

const totalReceived = computed(() => receipt.value?.lines?.reduce((sum: number, l: any) => sum + (l.receivedQty || 0), 0) || 0);
const totalAccepted = computed(() => receipt.value?.lines?.reduce((sum: number, l: any) => sum + ((l.receivedQty || 0) - (l.rejectedQty || 0)), 0) || 0);

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
