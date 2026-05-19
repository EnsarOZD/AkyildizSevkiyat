<template>
  <div class="space-y-4">

    <!-- Hero Header -->
    <div class="relative overflow-hidden bg-white dark:bg-gray-900 rounded-3xl p-6 sm:p-8 shadow-xl shadow-indigo-100/50 dark:shadow-none border border-indigo-50 dark:border-indigo-900 group mb-6">
      <div class="absolute -top-24 -right-24 h-64 w-64 bg-indigo-500/5 rounded-full blur-3xl group-hover:bg-indigo-500/10 transition-all duration-700"></div>
      
      <div class="relative flex flex-col sm:flex-row sm:items-center justify-between gap-6">
        <div class="flex items-center gap-4">
          <div class="p-4 bg-indigo-600 rounded-2xl text-white shadow-lg shadow-indigo-200 dark:shadow-none">
            <svg class="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" />
            </svg>
          </div>
          <div>
            <h1 class="text-2xl font-black text-gray-900 dark:text-gray-100 tracking-tight leading-none">Satınalma Siparişleri</h1>
            <p class="text-sm font-bold text-gray-400 mt-2 uppercase tracking-widest">Tedarikçi Sipariş Yönetimi</p>
          </div>
        </div>

        <button
          @click="showCreateModal = true"
          class="flex items-center justify-center gap-2 px-6 py-3 bg-indigo-600 hover:bg-indigo-700 text-white font-black rounded-xl shadow-lg shadow-indigo-100 dark:shadow-none transition-all hover:scale-[1.02] active:scale-95"
        >
          <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Yeni Sipariş
        </button>
      </div>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-900 p-4 rounded-3xl shadow-sm border border-gray-100 dark:border-gray-800 mb-6">
      <div class="flex flex-col md:flex-row gap-4">
        <div class="w-full md:w-48">
          <label class="block text-[10px] font-black text-gray-400 uppercase tracking-widest mb-1.5 ml-1">Durum Filtresi</label>
          <select
            v-model="filters.status"
            @change="handleFilterChange"
            class="w-full border-gray-100 dark:border-gray-800 rounded-xl px-4 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 outline-none transition-all"
          >
            <option value="">Tümü</option>
            <option value="0">Taslak</option>
            <option value="1">Onaylandı</option>
            <option value="2">Kısmi Kabul</option>
            <option value="3">Kapandı</option>
            <option value="4">İptal</option>
          </select>
        </div>
        <div class="flex-1">
          <label class="block text-[10px] font-black text-gray-400 uppercase tracking-widest mb-1.5 ml-1">Tedarikçi Arama</label>
          <div class="relative">
            <input
              type="text"
              v-model="filters.supplierName"
              @input="handleSearch"
              placeholder="İsim ile hızlı ara..."
              class="w-full border-gray-100 dark:border-gray-800 rounded-xl px-4 py-2.5 pl-10 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 outline-none transition-all"
            />
            <svg class="absolute left-3 top-2.5 h-5 w-5 text-gray-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
          </div>
        </div>
        <div class="w-full md:w-44">
          <label class="block text-[10px] font-black text-gray-400 uppercase tracking-widest mb-1.5 ml-1">Mail Durumu</label>
          <select
            v-model="filters.emailSent"
            @change="handleFilterChange"
            class="w-full border-gray-100 dark:border-gray-800 rounded-xl px-4 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 outline-none transition-all"
          >
            <option value="">Tümü</option>
            <option value="false">Gönderilmedi</option>
            <option value="true">Gönderildi</option>
          </select>
        </div>
        <div class="flex-1 min-w-[160px]">
          <label class="block text-[10px] font-black text-gray-400 uppercase tracking-widest mb-1.5 ml-1">Ürün Adı</label>
          <div class="relative">
            <input
              type="text"
              v-model="filters.stockName"
              @input="handleSearch"
              placeholder="Ürün adı ile ara..."
              class="w-full border-gray-100 dark:border-gray-800 rounded-xl px-4 py-2.5 pl-10 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 outline-none transition-all"
            />
            <svg class="absolute left-3 top-2.5 h-5 w-5 text-gray-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
            </svg>
          </div>
        </div>
        <div class="flex items-end gap-2">
          <button
            @click="showPassive = !showPassive; fetchOrders(1)"
            :class="showPassive
              ? 'bg-amber-50 dark:bg-amber-900/20 border-amber-300 dark:border-amber-700 text-amber-600 dark:text-amber-400'
              : 'bg-gray-50 dark:bg-gray-800 border-gray-100 dark:border-gray-700 text-gray-400 hover:text-amber-600'"
            class="px-3 py-2.5 rounded-xl border transition-all text-sm font-bold whitespace-nowrap"
            :title="showPassive ? 'Pasif siparişler gösteriliyor' : 'Pasif siparişleri göster'"
          >
            {{ showPassive ? '● Pasif Dahil' : '○ Pasif Gizli' }}
          </button>
          <button
            @click="() => fetchOrders()"
            class="p-2.5 bg-gray-50 dark:bg-gray-800 text-gray-400 hover:text-indigo-600 rounded-xl border border-gray-100 dark:border-gray-700 transition-all"
            title="Yenile"
          >
            <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
               <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
          </button>
        </div>
      </div>
    </div>

    <!-- Error state -->
    <div v-if="error" class="rounded-lg bg-red-50 border border-red-200 p-6 text-center">
      <p class="text-red-700 font-medium">{{ error }}</p>
      <button @click="() => fetchOrders()" class="mt-3 text-sm text-red-600 underline hover:text-red-800">
        Tekrar dene
      </button>
    </div>

    <!-- Desktop Table -->
    <div class="hidden md:block bg-white dark:bg-gray-900 shadow-xl shadow-indigo-100/20 dark:shadow-none rounded-3xl overflow-hidden border border-gray-100 dark:border-gray-800">
      <table class="min-w-full divide-y divide-gray-100 dark:divide-gray-800">
        <thead class="bg-gray-50/50 dark:bg-gray-800/50">
          <tr>
            <th class="px-6 py-4 text-left text-[10px] font-black text-gray-400 uppercase tracking-widest">Sipariş No</th>
            <th class="px-6 py-4 text-left text-[10px] font-black text-gray-400 uppercase tracking-widest">Tedarikçi</th>
            <th class="px-6 py-4 text-left text-[10px] font-black text-gray-400 uppercase tracking-widest">Tarih Bilgisi</th>
            <th class="px-6 py-4 text-left text-[10px] font-black text-gray-400 uppercase tracking-widest">Durum</th>
            <th class="px-6 py-4 text-center text-[10px] font-black text-gray-400 uppercase tracking-widest">Kalem</th>
            <th class="px-6 py-4"></th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
          <tr v-if="loading">
            <td colspan="6" class="px-4 py-0">
              <SkeletonTable :rows="5" :columns="6" class="shadow-none rounded-none border-none" />
            </td>
          </tr>
          <tr v-else-if="orders.length === 0">
            <td colspan="6">
              <EmptyState :icon="ShoppingCartIcon" title="Satınalma siparişi bulunamadı" description="Yeni sipariş oluşturmak için butonu kullanın." />
            </td>
          </tr>
          <tr
            v-for="order in orders"
            :key="order.id || order.Id"
            class="hover:bg-indigo-50/30 dark:hover:bg-indigo-900/10 cursor-pointer transition-all group"
            @click="openDetail(order.id || order.Id)"
          >
            <td class="px-6 py-4">
              <span class="text-sm font-black text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-900/40 px-3 py-1.5 rounded-xl shadow-sm">
                #{{ order.orderNumber || order.OrderNumber }}
              </span>
            </td>
            <td class="px-6 py-4">
               <div class="flex items-center gap-3">
                  <div class="h-9 w-9 rounded-xl bg-gray-100 dark:bg-gray-800 flex items-center justify-center text-gray-400 text-xs font-black">
                     {{ (order.supplierNameSnapshot || order.SupplierNameSnapshot)?.charAt(0) }}
                  </div>
                  <span class="text-sm font-bold text-gray-900 dark:text-gray-100">
                    {{ order.supplierNameSnapshot || order.SupplierNameSnapshot }}
                  </span>
               </div>
            </td>
            <td class="px-6 py-4">
              <div class="flex flex-col">
                <span class="text-xs font-bold text-gray-900 dark:text-gray-100">{{ formatDate(order.orderDate || order.OrderDate) }}</span>
                <span class="text-[10px] font-black text-gray-400 uppercase tracking-tighter mt-1" v-if="order.expectedDeliveryDate">Termin: {{ formatDate(order.expectedDeliveryDate || order.ExpectedDeliveryDate) }}</span>
              </div>
            </td>
            <td class="px-6 py-4">
              <div class="flex flex-col gap-1">
                <StatusBadge :status="order.status || order.Status" type="purchaseOrder" />
                <span v-if="order.emailSentAt"
                      :title="'Mail gönderildi: ' + formatDateTime(order.emailSentAt) + (order.emailSentTo ? ' → ' + order.emailSentTo : '')"
                      class="inline-flex items-center justify-center w-6 h-6 rounded-full bg-green-100 dark:bg-green-900/30 text-green-600 dark:text-green-400 flex-shrink-0">
                  <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
                  </svg>
                </span>
                <span v-else-if="order.status !== 'Cancelled' && order.status !== 'cancelled'"
                      title="Mail henüz gönderilmedi"
                      class="inline-flex items-center justify-center w-6 h-6 rounded-full bg-gray-100 dark:bg-gray-800 text-gray-400 dark:text-gray-500 flex-shrink-0">
                  <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                  </svg>
                </span>
              </div>
            </td>
            <td class="px-6 py-4 text-center">
              <span class="text-sm font-black text-gray-900 dark:text-gray-100 bg-gray-50 dark:bg-gray-800 px-3 py-1 rounded-lg">
                {{ order.lineCount || order.LineCount }}
              </span>
            </td>
            <td class="px-6 py-4 text-right" @click.stop>
               <div class="flex items-center justify-end gap-2">
                  <button
                    v-if="!order.emailSentAt && (order.status || order.Status) !== 'Cancelled'"
                    @click.stop="sendOrderEmail(order)"
                    :disabled="sendingEmailId === (order.id || order.Id)"
                    class="px-3 py-1.5 text-xs font-bold text-sky-700 bg-sky-50 dark:bg-sky-900/20 hover:bg-sky-100 dark:hover:bg-sky-900/40 border border-sky-200 dark:border-sky-800 rounded-lg transition-all whitespace-nowrap disabled:opacity-50"
                    title="Tedarikçiye otomatik mail gönder"
                  >
                    {{ sendingEmailId === (order.id || order.Id) ? 'Gönderiliyor...' : 'Mail Gönder' }}
                  </button>
                  <button
                    v-if="(order.status || order.Status) === 'Approved' || (order.status || order.Status) === 'PartiallyReceived'"
                    @click.stop="goToMalKabul(order.id || order.Id)"
                    class="px-3 py-1.5 text-xs font-bold text-emerald-700 bg-emerald-50 dark:bg-emerald-900/20 hover:bg-emerald-100 dark:hover:bg-emerald-900/40 border border-emerald-200 dark:border-emerald-800 rounded-lg transition-all whitespace-nowrap"
                    title="Mal Kabul"
                  >
                    Mal Kabul
                  </button>
                  <div class="h-8 w-8 rounded-full flex items-center justify-center text-gray-300 group-hover:text-indigo-600 transition-colors">
                    <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                    </svg>
                  </div>
               </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Mobile Cards -->
    <div class="md:hidden space-y-3">
      <div v-if="loading"><SkeletonTable :rows="4" :columns="1" /></div>
      <EmptyState v-else-if="orders.length === 0" :icon="ShoppingCartIcon" title="Satınalma siparişi bulunamadı" />
      <div
        v-for="order in orders"
        :key="order.id || order.Id"
        class="bg-white dark:bg-gray-900 rounded-3xl border border-gray-100 dark:border-gray-800 p-5 shadow-sm active:scale-[0.98] transition-all"
        @click="openDetail(order.id || order.Id)"
      >
        <div class="flex items-start justify-between gap-2 mb-4">
          <div class="min-w-0">
            <p class="text-[10px] font-black text-indigo-600 dark:text-indigo-400 uppercase tracking-widest mb-1">#{{ order.orderNumber || order.OrderNumber }}</p>
            <p class="font-black text-gray-900 dark:text-gray-100 text-base leading-tight">{{ order.supplierNameSnapshot || order.SupplierNameSnapshot }}</p>
          </div>
          <div class="flex flex-col items-end gap-1">
            <StatusBadge :status="order.status || order.Status" type="purchaseOrder" />
            <span v-if="order.emailSentAt"
                  :title="'Mail gönderildi: ' + formatDateTime(order.emailSentAt) + (order.emailSentTo ? ' → ' + order.emailSentTo : '')"
                  class="inline-flex items-center justify-center w-6 h-6 rounded-full bg-green-100 dark:bg-green-900/30 text-green-600 dark:text-green-400 flex-shrink-0">
              <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
              </svg>
            </span>
            <span v-else-if="order.status !== 'Cancelled' && order.status !== 'cancelled'"
                  title="Mail henüz gönderilmedi"
                  class="inline-flex items-center justify-center w-6 h-6 rounded-full bg-gray-100 dark:bg-gray-800 text-gray-400 dark:text-gray-500 flex-shrink-0">
              <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
              </svg>
            </span>
          </div>
        </div>
        <div class="flex items-center justify-between text-[10px] font-black text-gray-400 uppercase tracking-widest pt-4 border-t border-gray-50 dark:border-gray-800">
          <div class="flex flex-col">
             <span>Sipariş: {{ formatDate(order.orderDate || order.OrderDate) }}</span>
             <span v-if="order.expectedDeliveryDate" class="text-indigo-500">Termin: {{ formatDate(order.expectedDeliveryDate || order.ExpectedDeliveryDate) }}</span>
          </div>
          <div class="flex items-center gap-2">
            <button
              v-if="!order.emailSentAt && (order.status || order.Status) !== 'Cancelled'"
              @click.stop="sendOrderEmail(order)"
              :disabled="sendingEmailId === (order.id || order.Id)"
              class="px-2 py-1 text-[10px] font-black text-sky-700 bg-sky-50 border border-sky-200 rounded-lg disabled:opacity-50"
            >{{ sendingEmailId === (order.id || order.Id) ? '...' : 'Mail' }}</button>
            <button
              v-if="(order.status || order.Status) === 'Approved' || (order.status || order.Status) === 'PartiallyReceived'"
              @click.stop="goToMalKabul(order.id || order.Id)"
              class="px-2 py-1 text-[10px] font-black text-emerald-700 bg-emerald-50 border border-emerald-200 rounded-lg"
            >Mal Kabul</button>
            <span class="bg-indigo-50 dark:bg-indigo-900/40 text-indigo-600 dark:text-indigo-400 px-2 py-1 rounded-lg">{{ order.lineCount || order.LineCount }} Kalem</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Pagination -->
    <div v-if="totalPages > 1" class="flex items-center justify-between px-1 py-2">
      <span class="text-sm text-gray-500 dark:text-gray-400">
        Toplam {{ totalCount }} kayıt
      </span>
      <div class="flex items-center gap-1">
        <button
          @click="goToPage(currentPage - 1)"
          :disabled="currentPage <= 1"
          class="px-3 py-1.5 rounded-lg text-sm border border-gray-200 dark:border-gray-700 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
        >
          ‹
        </button>
        <span class="px-3 py-1.5 text-sm text-gray-700 dark:text-gray-300">
          {{ currentPage }} / {{ totalPages }}
        </span>
        <button
          @click="goToPage(currentPage + 1)"
          :disabled="currentPage >= totalPages"
          class="px-3 py-1.5 rounded-lg text-sm border border-gray-200 dark:border-gray-700 disabled:opacity-40 disabled:cursor-not-allowed hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
        >
          ›
        </button>
      </div>
    </div>

    <!-- Create Modal -->
    <CreatePurchaseOrderModal
      :isOpen="showCreateModal"
      @close="showCreateModal = false"
      @saved="onOrderCreated"
    />

  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { ShoppingCartIcon } from '@heroicons/vue/24/outline';
import purchaseOrderService from '../services/purchaseOrderService';
import CreatePurchaseOrderModal from '../components/CreatePurchaseOrderModal.vue';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';
import StatusBadge from '../components/StatusBadge.vue';
import SkeletonTable from '../components/SkeletonTable.vue';
import { useKeyboardShortcut } from '../composables/useKeyboardShortcut';
import EmptyState from '../components/EmptyState.vue';

const router = useRouter();
const route = useRoute();
const notificationStore = useNotificationStore();
const orders = ref<any[]>([]);
const loading = ref(false);
const error = ref('');
const filters = ref({
  status: (route.query.status as string) || '',
  supplierName: (route.query.supplierName as string) || '',
  emailSent: (route.query.emailSent as string) || '',
  stockName: (route.query.stockName as string) || '',
});
const showPassive = ref(route.query.showPassive === '1');
const currentPage = ref(Number(route.query.page) || 1);
const totalPages = ref(1);
const totalCount = ref(0);
const pageSize = 20;

const showCreateModal = ref(false);

// N tuşu → yeni sipariş oluştur
useKeyboardShortcut('n', () => { if (!showCreateModal.value) showCreateModal.value = true; });

const formatDate = (date: string) => {
  if (!date) return '-';
  return new Date(date).toLocaleDateString('tr-TR');
};

const formatDateTime = (dt: string) => {
  if (!dt) return '-';
  return new Date(dt).toLocaleString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' });
};

const syncUrl = (page: number) => {
  const query: Record<string, string> = {};
  if (filters.value.status) query.status = filters.value.status;
  if (filters.value.supplierName) query.supplierName = filters.value.supplierName;
  if (filters.value.emailSent) query.emailSent = filters.value.emailSent;
  if (filters.value.stockName) query.stockName = filters.value.stockName;
  if (showPassive.value) query.showPassive = '1';
  if (page > 1) query.page = String(page);
  router.replace({ query });
};

const fetchOrders = async (page = currentPage.value) => {
  loading.value = true;
  error.value = '';
  currentPage.value = page;
  syncUrl(page);
  try {
    const result = await purchaseOrderService.getAll({
      ...filters.value,
      emailSent: filters.value.emailSent === 'true' ? true : filters.value.emailSent === 'false' ? false : undefined,
      hidePassive: !showPassive.value,
      pageNumber: page,
      pageSize
    });
    orders.value = result.items;
    currentPage.value = result.pageIndex;
    totalPages.value = result.totalPages;
    totalCount.value = result.totalCount;
  } catch (e) {
    error.value = 'Veriler yüklenirken bir hata oluştu.';
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Hata oluştu.', 'error');
  } finally {
    loading.value = false;
  }
};

const handleSearch = () => { fetchOrders(1); };
const handleFilterChange = () => { fetchOrders(1); };
const goToPage = (page: number) => fetchOrders(page);
const onOrderCreated = () => { fetchOrders(); };

const openDetail = (id: string) => {
  router.push({ name: 'PurchaseOrderDetail', params: { id } });
};

const goToMalKabul = (id: string) => {
  router.push({ name: 'MalKabulDashboard', query: { poId: id } });
};

const sendingEmailId = ref<string | null>(null);

const sendOrderEmail = async (order: any) => {
  const id = order.id || order.Id;
  if (sendingEmailId.value === id) return;
  if (!window.confirm(`Sipariş tedarikçiye sistem üzerinden mail ile gönderilecek. Onaylıyor musunuz?`)) return;
  sendingEmailId.value = id;
  try {
    const result = await purchaseOrderService.sendEmail(id);
    order.emailSentAt = new Date().toISOString();
    order.emailSentTo = result.sentTo;
    notificationStore.add(`Mail başarıyla gönderildi → ${result.sentTo}`, 'success');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Mail gönderilemedi.', 'error');
  } finally {
    sendingEmailId.value = null;
  }
};

onMounted(fetchOrders);
</script>
