<template>
  <div>
  <div class="p-6">
    <div class="mb-6 flex justify-between items-center">
      <h1 class="text-xl font-semibold text-gray-900 dark:text-gray-100">Sevkiyatlar</h1>
    </div>

    <!-- Tabs -->
    <div class="mb-5 border-b border-gray-200 dark:border-gray-700">
      <nav class="-mb-px flex space-x-6">
        <button
          @click="activeTab = 'active'; page = 1"
          :class="activeTab === 'active'
            ? 'border-blue-500 text-blue-600'
            : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300'"
          class="whitespace-nowrap py-3 px-1 border-b-2 font-medium text-sm transition-colors"
        >
          Aktif Siparişler
        </button>
        <button
          @click="activeTab = 'passive'; filters.status = ''; page = 1"
          :class="activeTab === 'passive'
            ? 'border-blue-500 text-blue-600'
            : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300'"
          class="whitespace-nowrap py-3 px-1 border-b-2 font-medium text-sm transition-colors"
        >
          Pasif / Taslaklar
        </button>
      </nav>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4 mb-5 flex gap-3 flex-wrap items-end">
      <div class="w-44">
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Tarih</label>
        <input type="date" v-model="filters.date" class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100" />
      </div>
      <div class="w-44" v-if="activeTab === 'active'">
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Durum</label>
        <select v-model="filters.status" class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100">
          <option value="">Tümü</option>
          <option value="0">Taslak</option>
          <option value="1">Depoda</option>
          <option value="2">Toplanıyor</option>
          <option value="3">Sevke Hazır</option>
          <option value="4">Yolda</option>
          <option value="5">Teslim Edildi</option>
        </select>
      </div>
      <div class="w-44">
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Bölge</label>
        <select v-model="filters.zoneId" class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100">
          <option value="">Tüm Bölgeler</option>
          <option v-for="z in zones" :key="z.id" :value="z.id">{{ z.name }}</option>
        </select>
      </div>
      <div class="flex-1 min-w-[180px]">
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Arama</label>
        <input
          type="text"
          v-model="searchQuery"
          @input="handleSearch"
          placeholder="Sevkiyat No, Talep No, Proje..."
          class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100"
        />
      </div>
      <button @click="fetchShipments" class="px-4 py-2 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 rounded-lg text-sm hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors">
        Filtrele
      </button>
    </div>

    <!-- Skeleton while loading -->
    <SkeletonTable v-if="loading" :rows="8" :columns="10" />

    <template v-else>
      <!-- Empty state -->
      <div v-if="shipments.length === 0" class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl">
        <EmptyState
          :icon="ClipboardDocumentListIcon"
          title="Sevkiyat bulunamadı"
          :description="activeTab === 'passive'
            ? 'Pasife alınmış sevkiyat bulunmuyor.'
            : 'Arama kriterlerinize uygun aktif sevkiyat yok.'"
        />
      </div>

      <template v-else>
        <!-- Desktop table -->
        <div class="hidden md:block bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-3 w-10">
                  <input
                    type="checkbox"
                    :checked="isAllReadySelected"
                    :indeterminate="selectedIds.size > 0 && !isAllReadySelected"
                    @change="toggleSelectAll"
                    :disabled="pageReadyIds.length === 0"
                    class="w-4 h-4 rounded border-gray-300 text-blue-600 cursor-pointer disabled:opacity-30"
                    title="Sayfadaki tüm 'Sevke Hazır' sevkiyatları seç"
                  />
                </th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer select-none hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors" @click="toggleSort('id')">
                  No <span class="ml-1 opacity-50">{{ sortKey === 'id' ? (sortDir === 'asc' ? '↑' : '↓') : '↕' }}</span>
                </th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Talep No</th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sipariş No</th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İrsaliye No</th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Proje</th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Bölge</th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer select-none hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors" @click="toggleSort('deliveryDate')">
                  Tarih <span class="ml-1 opacity-50">{{ sortKey === 'deliveryDate' ? (sortDir === 'asc' ? '↑' : '↓') : '↕' }}</span>
                </th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer select-none hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors" @click="toggleSort('status')">
                  Durum <span class="ml-1 opacity-50">{{ sortKey === 'status' ? (sortDir === 'asc' ? '↑' : '↓') : '↕' }}</span>
                </th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sürücü / Plaka</th>
                <th class="px-5 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlem</th>
              </tr>
            </thead>
            <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
              <tr
                v-for="shipment in sortedShipments"
                :key="shipment.id"
                class="hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
                :class="[rowClass(shipment), selectedIds.has(shipment.id) ? 'bg-blue-50/60 dark:bg-blue-900/20' : '']"
              >
                <td class="px-4 py-3 w-10">
                  <input
                    v-if="shipment.status === 'ReadyForDispatch'"
                    type="checkbox"
                    :checked="selectedIds.has(shipment.id)"
                    @change="toggleSelect(shipment.id)"
                    class="w-4 h-4 rounded border-gray-300 text-blue-600 cursor-pointer"
                  />
                </td>
                <td class="px-5 py-3 whitespace-nowrap text-sm font-bold text-gray-900 dark:text-gray-100">
                  #{{ shipment.id }}
                  <span v-if="isOverdue(shipment)" class="ml-1 px-1.5 py-0.5 text-[10px] font-bold bg-red-100 text-red-600 rounded uppercase">Gecikmiş</span>
                  <span v-else-if="isToday(shipment)" class="ml-1 px-1.5 py-0.5 text-[10px] font-bold bg-blue-100 text-blue-600 rounded uppercase">Bugün</span>
                </td>
                <td class="px-5 py-3 whitespace-nowrap text-sm text-gray-700 dark:text-gray-300">{{ shipment.talepNo || '-' }}</td>
                <td class="px-5 py-3 whitespace-nowrap text-sm font-mono text-gray-600 dark:text-gray-400">{{ shipment.externalOrderNumber || '-' }}</td>
                <td class="px-5 py-3 whitespace-nowrap text-sm font-mono text-purple-600">{{ shipment.waybillNumber || '-' }}</td>
                <td class="px-5 py-3 whitespace-nowrap text-sm">
                  <div class="font-medium text-gray-900 dark:text-gray-100">{{ shipment.projectCode }}</div>
                  <div class="text-xs text-gray-400">{{ shipment.projectName }}</div>
                </td>
                <td class="px-5 py-3 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ shipment.region }}</td>
                <td class="px-5 py-3 whitespace-nowrap text-sm" :class="isOverdue(shipment) ? 'text-red-600 font-medium' : 'text-gray-500 dark:text-gray-400'">
                  {{ formatDate(shipment.deliveryDate) }}
                </td>
                <td class="px-5 py-3 whitespace-nowrap">
                  <StatusBadge :status="shipment.status" type="shipment" />
                </td>
                <td class="px-5 py-3 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
                  <div v-if="shipment.driverName">{{ shipment.driverName }}</div>
                  <div v-if="shipment.plateNumber" class="text-xs text-gray-400">{{ shipment.plateNumber }}</div>
                  <span v-if="!shipment.driverName" class="text-gray-300">—</span>
                </td>
                <td class="px-5 py-3 whitespace-nowrap text-right text-sm">
                  <div class="flex justify-end items-center gap-1.5">
                    <!-- Workflow quick actions -->
                    <button
                      v-if="shipment.status === 'Created'"
                      v-role="['Admin', 'Accounting']"
                      @click="quickAssignToWarehouse(shipment.id)"
                      class="text-yellow-700 hover:text-yellow-900 bg-yellow-50 hover:bg-yellow-100 px-2 py-1 rounded text-xs font-medium transition-colors"
                    >Depoya Ata</button>
                    <button
                      v-if="shipment.status === 'AssignedToWarehouse'"
                      v-role="['Admin', 'Warehouse']"
                      @click="quickStartPicking(shipment.id)"
                      class="text-blue-700 hover:text-blue-900 bg-blue-50 hover:bg-blue-100 px-2 py-1 rounded text-xs font-medium transition-colors"
                    >Toplamaya Başla</button>
                    <button
                      v-if="shipment.status === 'Picking'"
                      v-role="['Admin', 'Warehouse']"
                      @click="quickMarkReady(shipment.id)"
                      class="text-purple-700 hover:text-purple-900 bg-purple-50 hover:bg-purple-100 px-2 py-1 rounded text-xs font-medium transition-colors"
                    >Hazır İşaretle</button>
                    <!-- Toggle active/passive -->
                    <button
                      v-if="shipment.status === 'Created'"
                      v-role="['Admin', 'Accounting']"
                      @click="confirmToggleStatus(shipment.id, true)"
                      class="text-amber-600 hover:text-amber-800 bg-amber-50 hover:bg-amber-100 px-2 py-1 rounded text-xs font-medium transition-colors"
                    >Pasife Al</button>
                    <button
                      v-if="shipment.status === 'Passive'"
                      v-role="['Admin', 'Accounting']"
                      @click="confirmToggleStatus(shipment.id, false)"
                      class="text-green-600 hover:text-green-800 bg-green-50 hover:bg-green-100 px-2 py-1 rounded text-xs font-medium transition-colors"
                    >Aktife Al</button>
                    <router-link
                      :to="`/shipments/${shipment.id}`"
                      class="text-blue-600 hover:text-blue-800 px-2 py-1 text-xs font-medium"
                    >Detay →</router-link>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Mobile cards -->
        <div class="md:hidden space-y-3">
          <div
            v-for="shipment in sortedShipments"
            :key="'m-' + shipment.id"
            class="bg-white dark:bg-gray-900 rounded-xl border p-4 transition-colors"
            :class="isOverdue(shipment) ? 'border-red-200 bg-red-50/30' : 'border-gray-200 dark:border-gray-700'"
          >
            <div class="flex justify-between items-start mb-2">
              <div>
                <span class="text-base font-bold text-gray-900 dark:text-gray-100">#{{ shipment.id }}</span>
                <span v-if="isOverdue(shipment)" class="ml-2 px-1.5 py-0.5 text-[10px] font-bold bg-red-100 text-red-600 rounded uppercase">Gecikmiş</span>
                <span v-else-if="isToday(shipment)" class="ml-2 px-1.5 py-0.5 text-[10px] font-bold bg-blue-100 text-blue-600 rounded uppercase">Bugün</span>
                <span class="ml-2 text-xs" :class="isOverdue(shipment) ? 'text-red-500 font-medium' : 'text-gray-400'">
                  {{ formatDate(shipment.deliveryDate) }}
                </span>
              </div>
              <StatusBadge :status="shipment.status" type="shipment" />
            </div>
            <div class="mb-3">
              <div class="font-medium text-gray-800 dark:text-gray-200">{{ shipment.projectName }}</div>
              <div class="text-xs text-gray-400">{{ shipment.projectCode }} · {{ shipment.region }}</div>
            </div>
            <div class="text-sm text-gray-600 dark:text-gray-400 grid grid-cols-2 gap-2 mb-3">
              <div>
                <span class="block text-xs text-gray-400">Talep No</span>
                {{ shipment.talepNo || '-' }}
              </div>
              <div>
                <span class="block text-xs text-gray-400">Sipariş No</span>
                {{ shipment.externalOrderNumber || '-' }}
              </div>
              <div v-if="shipment.driverName" class="col-span-2">
                <span class="block text-xs text-gray-400">Sürücü</span>
                {{ shipment.driverName }} ({{ shipment.plateNumber }})
              </div>
            </div>
            <div class="flex flex-wrap justify-end gap-2 border-t border-gray-100 dark:border-gray-700 pt-3">
              <button
                v-if="shipment.status === 'Created'"
                v-role="['Admin', 'Accounting']"
                @click="quickAssignToWarehouse(shipment.id)"
                class="text-yellow-700 text-xs font-medium bg-yellow-50 px-2 py-1 rounded"
              >Depoya Ata</button>
              <button
                v-if="shipment.status === 'AssignedToWarehouse'"
                v-role="['Admin', 'Warehouse']"
                @click="quickStartPicking(shipment.id)"
                class="text-blue-700 text-xs font-medium bg-blue-50 px-2 py-1 rounded"
              >Toplamaya Başla</button>
              <button
                v-if="shipment.status === 'Picking'"
                v-role="['Admin', 'Warehouse']"
                @click="quickMarkReady(shipment.id)"
                class="text-purple-700 text-xs font-medium bg-purple-50 px-2 py-1 rounded"
              >Hazır İşaretle</button>
              <button
                v-if="shipment.status === 'Created'"
                v-role="['Admin', 'Accounting']"
                @click="confirmToggleStatus(shipment.id, true)"
                class="text-amber-600 text-xs font-bold bg-amber-50 px-2 py-1 rounded"
              >Pasife Al</button>
              <button
                v-if="shipment.status === 'Passive'"
                v-role="['Admin', 'Accounting']"
                @click="confirmToggleStatus(shipment.id, false)"
                class="text-green-600 text-xs font-bold bg-green-50 px-2 py-1 rounded"
              >Aktife Al</button>
              <router-link
                :to="`/shipments/${shipment.id}`"
                class="text-white bg-blue-600 hover:bg-blue-700 px-4 py-1 rounded text-sm font-medium shadow-sm transition-colors"
              >Detay →</router-link>
            </div>
          </div>
        </div>
      </template>
    </template>

    <!-- Pagination -->
    <div v-if="!loading && shipments.length > 0" class="mt-4 flex justify-between items-center bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
      <p class="text-sm text-gray-500 dark:text-gray-400">
        Toplam <span class="font-medium text-gray-700 dark:text-gray-300">{{ totalCount }}</span> kayıt · Sayfa {{ page }} / {{ totalPages }}
      </p>
      <div class="flex gap-2">
        <button
          @click="page--; fetchShipments()"
          :disabled="page <= 1"
          class="px-3 py-1.5 border border-gray-300 dark:border-gray-700 rounded-lg text-sm hover:bg-gray-50 dark:hover:bg-gray-800 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >← Önceki</button>
        <button
          @click="page++; fetchShipments()"
          :disabled="page >= totalPages"
          class="px-3 py-1.5 border border-gray-300 dark:border-gray-700 rounded-lg text-sm hover:bg-gray-50 dark:hover:bg-gray-800 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
        >Sonraki →</button>
      </div>
    </div>

  </div>

  <!-- Floating bulk action bar -->
  <Teleport to="body">
    <Transition
      enter-active-class="transition ease-out duration-200"
      enter-from-class="translate-y-4 opacity-0"
      enter-to-class="translate-y-0 opacity-100"
      leave-active-class="transition ease-in duration-150"
      leave-from-class="translate-y-0 opacity-100"
      leave-to-class="translate-y-4 opacity-0"
    >
      <div
        v-if="selectedIds.size > 0"
        class="fixed bottom-6 left-1/2 -translate-x-1/2 z-40 flex items-center gap-3 bg-gray-900 dark:bg-gray-100 text-white dark:text-gray-900 rounded-2xl shadow-2xl px-5 py-3"
      >
        <span class="text-sm font-medium">{{ selectedIds.size }} sevkiyat seçili</span>
        <div class="w-px h-5 bg-white/20 dark:bg-gray-900/20"></div>
        <button
          v-role="['Admin', 'Dispatcher', 'Manager']"
          @click="showBulkModal = true"
          class="flex items-center gap-1.5 bg-blue-500 hover:bg-blue-600 text-white text-sm font-semibold px-3 py-1.5 rounded-lg transition-colors"
        >
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M8.25 18.75a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m3 0h6m-9 0H3.375a1.125 1.125 0 0 1-1.125-1.125V14.25m17.25 4.5a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m3 0h1.125c.621 0 1.129-.504 1.09-1.124a17.902 17.902 0 0 0-3.213-9.193 2.056 2.056 0 0 0-1.58-.86H14.25M16.5 18.75h-2.25m0-11.177v-.958c0-.568-.422-1.048-.987-1.106a48.554 48.554 0 0 0-10.026 0 1.106 1.106 0 0 0-.987 1.106v7.635m12-6.677v6.677m0 4.5v-4.5m0 0h-12" /></svg>
          Araca Ata
        </button>
        <button
          @click="selectedIds.clear()"
          class="text-sm text-white/70 dark:text-gray-900/60 hover:text-white dark:hover:text-gray-900 px-2 py-1 rounded-lg transition-colors"
        >
          İptal
        </button>
      </div>
    </Transition>
  </Teleport>

  <!-- Bulk assign vehicle modal -->
  <Teleport to="body">
    <div v-if="showBulkModal" class="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div class="absolute inset-0 bg-black/50" @click="showBulkModal = false"></div>
      <div class="relative w-full max-w-md bg-white dark:bg-gray-900 rounded-2xl shadow-xl p-6 space-y-4">
        <h3 class="text-lg font-semibold text-gray-900 dark:text-white">Toplu Araç Atama</h3>
        <p class="text-sm text-gray-500 dark:text-gray-400">
          <span class="font-medium text-gray-700 dark:text-gray-200">{{ selectedIds.size }} sevkiyat</span>
          aynı araç ve sürücüye atanacak.
        </p>

        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Sürücü Adı <span class="text-red-500">*</span></label>
          <input
            v-model="bulkForm.driverName"
            type="text"
            placeholder="Ad Soyad"
            class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm bg-white dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Plaka <span class="text-red-500">*</span></label>
          <input
            v-model="bulkForm.plateNumber"
            type="text"
            placeholder="34 ABC 123"
            class="w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm bg-white dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div class="flex gap-3 pt-1">
          <button
            @click="showBulkModal = false"
            class="flex-1 py-2.5 border border-gray-300 dark:border-gray-700 text-gray-700 dark:text-gray-300 rounded-xl text-sm font-medium hover:bg-gray-50 dark:hover:bg-white/5 transition-colors"
          >
            İptal
          </button>
          <button
            @click="submitBulkAssign"
            :disabled="bulkSubmitting"
            class="flex-1 py-2.5 bg-blue-600 hover:bg-blue-700 disabled:bg-blue-400 text-white rounded-xl text-sm font-semibold transition-colors flex items-center justify-center gap-2"
          >
            <span v-if="bulkSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            {{ bulkSubmitting ? 'Atanıyor...' : 'Araca Ata' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ClipboardDocumentListIcon } from '@heroicons/vue/24/outline';
import shipmentService, { type ZoneItem } from '../services/shipmentService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';
import StatusBadge from '../components/StatusBadge.vue';
import SkeletonTable from '../components/SkeletonTable.vue';
import EmptyState from '../components/EmptyState.vue';
import { useKeyboardShortcut } from '../composables/useKeyboardShortcut';

const notificationStore = useNotificationStore();
const route = useRoute();
const router = useRouter();

function simpleDebounce(fn: Function, delay: number) {
  let timeoutId: any;
  return (...args: any[]) => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => fn(...args), delay);
  };
}

const q = route.query;
const activeTab = ref<'active' | 'passive'>(q.tab === 'passive' ? 'passive' : 'active');

interface Shipment {
  id: number;
  projectCode: string;
  projectName: string;
  region: string;
  status: string;
  deliveryDate: string;
  driverName?: string;
  plateNumber?: string;
  talepNo?: string;
  externalOrderNumber?: string;
  waybillNumber?: string;
}

const shipments = ref<Shipment[]>([]);
const loading = ref(false);
const zones = ref<ZoneItem[]>([]);

const filters = ref({
  date: typeof q.date === 'string' ? q.date : '',
  status: typeof q.status === 'string' ? q.status : '',
  zoneId: typeof q.zone === 'string' ? q.zone : '',
});

// ── Selection state ───────────────────────────────────────────────────────────
const selectedIds = reactive(new Set<number>());

const pageReadyIds = computed(() =>
  shipments.value.filter(s => s.status === 'ReadyForDispatch').map(s => s.id)
);

const isAllReadySelected = computed(() =>
  pageReadyIds.value.length > 0 && pageReadyIds.value.every(id => selectedIds.has(id))
);

function toggleSelect(id: number) {
  if (selectedIds.has(id)) selectedIds.delete(id);
  else selectedIds.add(id);
}

function toggleSelectAll() {
  if (isAllReadySelected.value) {
    pageReadyIds.value.forEach(id => selectedIds.delete(id));
  } else {
    pageReadyIds.value.forEach(id => selectedIds.add(id));
  }
}

// ── Bulk assign modal ─────────────────────────────────────────────────────────
const showBulkModal = ref(false);
const bulkSubmitting = ref(false);
const bulkForm = ref({ driverName: '', plateNumber: '' });

async function submitBulkAssign() {
  if (!bulkForm.value.driverName.trim() || !bulkForm.value.plateNumber.trim()) {
    notificationStore.add('Sürücü adı ve plaka zorunludur.', 'warning');
    return;
  }
  bulkSubmitting.value = true;
  try {
    const result = await shipmentService.bulkAssignVehicle({
      shipmentIds: [...selectedIds],
      driverName: bulkForm.value.driverName.trim(),
      plateNumber: bulkForm.value.plateNumber.trim(),
    });
    if (result.successCount > 0) {
      notificationStore.add(`${result.successCount} sevkiyat araca atandı.`, 'success');
    }
    if (result.errors.length > 0) {
      notificationStore.add(`${result.errors.length} sevkiyat atanamadı: ${result.errors[0]}`, 'warning');
    }
    if (result.warning) {
      notificationStore.add(result.warning.message, 'warning');
    }
    showBulkModal.value = false;
    bulkForm.value = { driverName: '', plateNumber: '' };
    selectedIds.clear();
    fetchShipments();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  } finally {
    bulkSubmitting.value = false;
  }
}
const searchQuery = ref(typeof q.search === 'string' ? q.search : '');

type SortKey = 'deliveryDate' | 'status' | 'id';
type SortDir = 'asc' | 'desc';
const sortKey = ref<SortKey>(
  (q.sortKey === 'status' || q.sortKey === 'id') ? q.sortKey as SortKey : 'deliveryDate'
);
const sortDir = ref<SortDir>(q.sortDir === 'desc' ? 'desc' : 'asc');

const sortedShipments = computed(() => {
  const list = [...shipments.value];
  list.sort((a, b) => {
    let va: any = a[sortKey.value];
    let vb: any = b[sortKey.value];
    if (sortKey.value === 'deliveryDate') {
      va = new Date(va).getTime();
      vb = new Date(vb).getTime();
    }
    if (va < vb) return sortDir.value === 'asc' ? -1 : 1;
    if (va > vb) return sortDir.value === 'asc' ? 1 : -1;
    return 0;
  });
  return list;
});

const toggleSort = (key: SortKey) => {
  if (sortKey.value === key) {
    sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc';
  } else {
    sortKey.value = key;
    sortDir.value = 'asc';
  }
};

const page = ref(typeof q.page === 'string' && Number(q.page) > 0 ? Number(q.page) : 1);
const pageSize = ref(20);
const totalCount = ref(0);
const totalPages = ref(1);

// Keyboard shortcuts: ← önceki sayfa, → sonraki sayfa
useKeyboardShortcut('ArrowLeft', () => {
  if (page.value > 1) { page.value--; fetchShipments(); }
});
useKeyboardShortcut('ArrowRight', () => {
  if (page.value < totalPages.value) { page.value++; fetchShipments(); }
});

const today = new Date();
today.setHours(0, 0, 0, 0);

const ACTIVE_STATUSES = new Set(['Created', 'AssignedToWarehouse', 'Picking', 'ReadyForDispatch', 'AssignedToVehicle']);

const isOverdue = (s: Shipment) => {
  const d = new Date(s.deliveryDate);
  d.setHours(0, 0, 0, 0);
  return d < today && ACTIVE_STATUSES.has(s.status);
};

const isToday = (s: Shipment) => {
  const d = new Date(s.deliveryDate);
  d.setHours(0, 0, 0, 0);
  return d.getTime() === today.getTime() && ACTIVE_STATUSES.has(s.status);
};

const rowClass = (s: Shipment) => {
  if (isOverdue(s)) return 'bg-red-50/40';
  return '';
};

const formatDate = (d: string) =>
  new Date(d).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric' });

const fetchShipments = async () => {
  loading.value = true;
  const params: any = {
    PageNumber: page.value,
    PageSize: pageSize.value,
    Search: searchQuery.value,
  };

  if (filters.value.date) {
    params.startDate = filters.value.date;
    params.endDate = filters.value.date + 'T23:59:59';
  }

  if (activeTab.value === 'passive') {
    params.status = 10;
  } else {
    if (filters.value.status) params.status = filters.value.status;
  }

  if (filters.value.zoneId) params.ZoneId = filters.value.zoneId;

  try {
    const data = await shipmentService.getAll(params);
    shipments.value = data.items as unknown as Shipment[];
    totalCount.value = data.totalCount;
    totalPages.value = data.totalPages;
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'Sevkiyatlar yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
};

const handleSearch = simpleDebounce(() => {
  page.value = 1;
  syncUrl();
  fetchShipments();
}, 500);

const quickAssignToWarehouse = async (id: number) => {
  try {
    await shipmentService.assignToWarehouse(id);
    fetchShipments();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

const quickStartPicking = async (id: number) => {
  try {
    await shipmentService.startPicking(id);
    fetchShipments();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

const quickMarkReady = async (id: number) => {
  try {
    await shipmentService.markReady(id);
    fetchShipments();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

const confirmToggleStatus = async (id: number, setPassive: boolean) => {
  let reason: string | undefined = undefined;

  if (setPassive) {
    const confirmed = await notificationStore.promptConfirm({
      title: 'Pasife Al',
      message: 'Bu sipariş pasife alınacak ve operasyon ekranlarından gizlenecek. Emin misiniz?',
      confirmText: 'Pasife Al',
      type: 'warning',
    });
    if (!confirmed) return;
  } else {
    // Aktife alma — sebep zorunlu
    const input = window.prompt('Bu sevkiyatı neden aktife almak istiyorsunuz? (Sebep zorunludur)');
    if (input === null) return; // İptal basıldı
    if (!input.trim()) {
      notificationStore.add('Aktife alma işlemi için sebep girmeniz zorunludur.', 'warning');
      return;
    }
    reason = input.trim();
  }

  try {
    await shipmentService.toggleStatus(id, setPassive, reason);
    fetchShipments();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

const syncUrl = () => {
  const query: Record<string, string> = {};
  if (activeTab.value !== 'active') query.tab = activeTab.value;
  if (filters.value.date) query.date = filters.value.date;
  if (filters.value.status && filters.value.status !== '0') query.status = filters.value.status;
  if (filters.value.zoneId) query.zone = filters.value.zoneId;
  if (searchQuery.value) query.search = searchQuery.value;
  if (page.value !== 1) query.page = String(page.value);
  if (sortKey.value !== 'deliveryDate') query.sortKey = sortKey.value;
  if (sortDir.value !== 'asc') query.sortDir = sortDir.value;
  router.replace({ query });
};

watch(activeTab, () => {
  page.value = 1;
  syncUrl();
  fetchShipments();
});

watch([() => filters.value.date, () => filters.value.status, () => filters.value.zoneId], () => {
  page.value = 1;
  selectedIds.clear();
  syncUrl();
  fetchShipments();
});

watch(page, () => {
  syncUrl();
});

watch([sortKey, sortDir], () => {
  syncUrl();
});

async function fetchZones() {
  try {
    zones.value = await shipmentService.getZones();
  } catch {
    // Bölge listesi yüklenemezse filtre boş kalır, kritik değil
  }
}

onMounted(() => {
  fetchZones();
  fetchShipments();
});
</script>
