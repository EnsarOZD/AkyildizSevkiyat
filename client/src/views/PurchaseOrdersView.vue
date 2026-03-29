<template>
  <div class="space-y-4">

    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-gray-900 dark:text-gray-100">Satınalma Siparişleri</h1>
      </div>
      <button
        @click="showCreateModal = true"
        class="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-semibold shadow-sm transition-colors w-full sm:w-auto justify-center"
      >
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        Yeni Sipariş
      </button>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-900 p-4 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700">
      <div class="flex flex-col sm:flex-row gap-3">
        <div class="w-full sm:w-44">
          <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">Durum</label>
          <select
            v-model="filters.status"
            @change="handleFilterChange"
            class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
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
          <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">Tedarikçi Ara</label>
          <input
            type="text"
            v-model="filters.supplierName"
            @input="handleSearch"
            placeholder="Tedarikçi adı..."
            class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-600 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
          />
        </div>
        <div class="flex items-end">
          <button
            @click="() => fetchOrders()"
            class="w-full sm:w-auto px-4 py-2 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 rounded-lg text-sm hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors border border-gray-200 dark:border-gray-700"
          >
            Yenile
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
    <div class="hidden md:block bg-white dark:bg-gray-900 shadow-sm rounded-xl overflow-x-auto border border-gray-200 dark:border-gray-700">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-5 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sipariş No</th>
            <th class="px-5 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tedarikçi</th>
            <th class="px-5 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tarih</th>
            <th class="px-5 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">Teslim</th>
            <th class="px-5 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
            <th class="px-5 py-3 text-center text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">Kalem</th>
            <th class="px-5 py-3"></th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-if="loading">
            <td colspan="7" class="px-4 py-0">
              <SkeletonTable :rows="5" :columns="7" class="shadow-none rounded-none border-none" />
            </td>
          </tr>
          <tr v-else-if="orders.length === 0">
            <td colspan="7">
              <EmptyState :icon="ShoppingCartIcon" title="Satınalma siparişi bulunamadı" description="Yeni sipariş oluşturmak için butonu kullanın." />
            </td>
          </tr>
          <tr
            v-for="order in orders"
            :key="order.id || order.Id"
            class="hover:bg-gray-50 dark:hover:bg-gray-800 cursor-pointer transition-colors"
            @click="openDetail(order.id || order.Id)"
          >
            <td class="px-5 py-3 text-sm font-semibold text-blue-600 dark:text-blue-400 whitespace-nowrap">
              {{ order.orderNumber || order.OrderNumber }}
            </td>
            <td class="px-5 py-3 text-sm font-medium text-gray-900 dark:text-gray-100">
              {{ order.supplierNameSnapshot || order.SupplierNameSnapshot }}
            </td>
            <td class="px-5 py-3 text-sm text-gray-500 dark:text-gray-400 whitespace-nowrap">
              {{ formatDate(order.orderDate || order.OrderDate) }}
            </td>
            <td class="px-5 py-3 text-sm text-gray-500 dark:text-gray-400 whitespace-nowrap hidden lg:table-cell">
              {{ formatDate(order.expectedDeliveryDate || order.ExpectedDeliveryDate) }}
            </td>
            <td class="px-5 py-3 whitespace-nowrap">
              <StatusBadge :status="order.status || order.Status" type="purchaseOrder" />
            </td>
            <td class="px-5 py-3 text-sm text-gray-500 dark:text-gray-400 text-center hidden lg:table-cell">
              {{ order.lineCount || order.LineCount }}
            </td>
            <td class="px-5 py-3 text-right">
              <svg class="h-4 w-4 text-gray-400 inline" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
              </svg>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Mobile Cards -->
    <div class="md:hidden space-y-2">
      <div v-if="loading"><SkeletonTable :rows="4" :columns="1" /></div>
      <EmptyState v-else-if="orders.length === 0" :icon="ShoppingCartIcon" title="Satınalma siparişi bulunamadı" />
      <div
        v-for="order in orders"
        :key="order.id || order.Id"
        class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 cursor-pointer active:bg-gray-50 dark:active:bg-gray-800 transition-colors"
        @click="openDetail(order.id || order.Id)"
      >
        <div class="flex items-start justify-between gap-2 mb-2">
          <div class="min-w-0">
            <p class="font-bold text-blue-600 dark:text-blue-400 text-sm truncate">{{ order.orderNumber || order.OrderNumber }}</p>
            <p class="font-semibold text-gray-900 dark:text-gray-100 text-sm truncate">{{ order.supplierNameSnapshot || order.SupplierNameSnapshot }}</p>
          </div>
          <StatusBadge :status="order.status || order.Status" type="purchaseOrder" />
        </div>
        <div class="flex items-center justify-between text-xs text-gray-500 dark:text-gray-400 mt-2 pt-2 border-t border-gray-100 dark:border-gray-800">
          <span>{{ formatDate(order.orderDate || order.OrderDate) }}</span>
          <span>Teslim: {{ formatDate(order.expectedDeliveryDate || order.ExpectedDeliveryDate) }}</span>
          <span class="bg-gray-100 dark:bg-gray-800 px-2 py-0.5 rounded font-semibold">{{ order.lineCount || order.LineCount }} kalem</span>
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
import { useRouter } from 'vue-router';
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
const notificationStore = useNotificationStore();
const orders = ref<any[]>([]);
const loading = ref(false);
const error = ref('');
const filters = ref({ status: '', supplierName: '' });
const currentPage = ref(1);
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

const fetchOrders = async (page = currentPage.value) => {
  loading.value = true;
  error.value = '';
  try {
    const result = await purchaseOrderService.getAll({ ...filters.value, pageNumber: page, pageSize });
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

const handleSearch = () => { currentPage.value = 1; fetchOrders(1); };
const handleFilterChange = () => { currentPage.value = 1; fetchOrders(1); };
const goToPage = (page: number) => fetchOrders(page);
const onOrderCreated = () => { fetchOrders(); };

const openDetail = (id: string) => {
  router.push({ name: 'PurchaseOrderDetail', params: { id } });
};

onMounted(fetchOrders);
</script>
