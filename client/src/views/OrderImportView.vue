<template>
  <div class="p-6 space-y-6">
    <div class="flex justify-between items-center">
      <div>
        <h1 class="text-2xl font-bold mb-2 dark:text-gray-100">Sipariş Aktarımı (ISS-IP)</h1>
        <p class="text-gray-600 dark:text-gray-400">Siparişleri içeri aktarın ve stok eşleşmelerini yönetin.</p>
      </div>
    </div>

    <!-- Giysi/Tekstil Operasyonları Uyarısı -->
    <div class="rounded-lg bg-amber-50 border border-amber-200 p-4 mb-4 flex items-start gap-3">
      <svg class="w-5 h-5 text-amber-600 mt-0.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.538-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/>
      </svg>
      <div>
        <p class="text-amber-800 font-medium text-sm">Giysi/Tekstil Operasyonları</p>
        <p class="text-amber-700 text-sm mt-0.5">
          Giysi kategorisindeki siparişler şu an depo hazırlık sürecinde desteklenmemektedir.
          Bu siparişleri sisteme aktarmadan önce sistem yöneticinize danışınız.
        </p>
      </div>
    </div>

    <!-- Import Controls -->
    <div class="bg-white dark:bg-gray-900 p-4 rounded shadow flex flex-wrap gap-4 items-end">
      <div>
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Başlangıç</label>
        <input type="date" v-model="startDate" class="border dark:border-gray-700 rounded px-3 py-2 dark:bg-gray-800 dark:text-gray-100" />
      </div>
      <div>
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Bitiş</label>
        <input type="date" v-model="endDate" class="border dark:border-gray-700 rounded px-3 py-2 dark:bg-gray-800 dark:text-gray-100" />
      </div>

      <button
        @click="importOrders"
        class="bg-indigo-600 text-white px-4 py-2 rounded hover:bg-indigo-700 flex items-center gap-2"
        :disabled="importing"
      >
        <svg v-if="importing" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
        </svg>
        <span v-if="importing">Aktarılıyor<span v-if="importBatchId"> (Batch #{{ importBatchId }})</span>...</span>
        <span v-else>Aktarımı Başlat</span>
      </button>

      <button
        @click="syncProjects"
        class="bg-gray-600 text-white px-4 py-2 rounded hover:bg-gray-700 flex items-center gap-2"
        :disabled="syncing"
      >
        <span v-if="syncing">Projeler Güncelleniyor...</span>
        <span v-else>Projeleri Senkronize Et</span>
      </button>

      <button
        @click="checkMappings"
        class="bg-teal-600 text-white px-4 py-2 rounded hover:bg-teal-700 flex items-center gap-2"
        :disabled="checking"
      >
        <span v-if="checking">Kontrol Ediliyor...</span>
        <span v-else>Eşleşmeleri Kontrol Et</span>
      </button>

      <div v-if="importResult" class="mt-3 w-full">
        <div class="rounded-lg border p-3 text-sm" :class="importResult.failedCount > 0 ? 'border-yellow-300 bg-yellow-50 dark:bg-yellow-900/20' : 'border-green-300 bg-green-50 dark:bg-green-900/20'">
          <div class="flex flex-wrap gap-4 font-medium">
            <span class="text-gray-600 dark:text-gray-400">ISS'ten gelen: <strong>{{ importResult.totalFromIss }}</strong></span>
            <span class="text-green-600">Yeni eklendi: <strong>{{ importResult.newCount }}</strong></span>
            <span class="text-gray-500">Atlandı: <strong>{{ importResult.skippedCount }}</strong></span>
            <span v-if="importResult.needsMappingCount > 0" class="text-yellow-600">Eşleştirme bekliyor: <strong>{{ importResult.needsMappingCount }}</strong></span>
            <span v-if="importResult.failedCount > 0" class="text-red-600">Hatalı: <strong>{{ importResult.failedCount }}</strong></span>
          </div>
          <div v-if="importResult.errors.length > 0" class="mt-2 space-y-1">
            <div v-for="err in importResult.errors.slice(0, 3)" :key="err" class="text-red-600 text-xs">{{ err }}</div>
            <div v-if="importResult.errors.length > 3" class="text-gray-500 text-xs">... ve {{ importResult.errors.length - 3 }} hata daha (Geçmiş sekmesinde görüntüleyin)</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Tabs & Content -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Mapping Manager (Always visible if needed, or conditional) -->
        <div class="lg:col-span-3" v-if="showMapping && activeTab === 'NeedsMapping'">
            <StockMappingManager @mapped="loadOrders" />
        </div>

        <div class="lg:col-span-3 bg-white dark:bg-gray-900 rounded shadow">
                     <div class="border-b dark:border-gray-700 px-4 py-3 flex flex-wrap gap-4 items-center justify-between">
                         <div class="flex gap-4">
                            <button
                                @click="activeTab = 'Ready'; page = 1; clearSelection()"
                                class="py-3 px-2 border-b-2 font-medium transition-colors"
                                :class="activeTab === 'Ready' ? 'border-green-500 text-green-600' : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'"
                            >
                                Aktif / Hazır ({{ readyCount }})
                            </button>
                            <button
                                @click="activeTab = 'NeedsMapping'; page = 1; clearSelection()"
                                class="py-3 px-2 border-b-2 font-medium transition-colors"
                                :class="activeTab === 'NeedsMapping' ? 'border-red-500 text-red-600' : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'"
                            >
                                Eşleştirme Bekliyor ({{ needsMappingCount }})
                            </button>
                            <button
                                @click="activeTab = 'Passive'; page = 1; clearSelection()"
                                class="py-3 px-2 border-b-2 font-medium transition-colors"
                                :class="activeTab === 'Passive' ? 'border-gray-500 text-gray-600' : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'"
                            >
                                Pasif Siparişler ({{ passiveCount }})
                            </button>
                            <button
                                @click="activeTab = 'History'; loadHistory()"
                                class="py-3 px-2 border-b-2 font-medium transition-colors"
                                :class="activeTab === 'History' ? 'border-indigo-500 text-indigo-600' : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'"
                            >
                                Import Geçmişi
                            </button>
                         </div>

                         <div class="flex gap-2 items-center">
                              <!-- Bulk Action Button -->
                             <button
                                v-if="activeTab === 'Ready' && selectedIds.size > 0"
                                @click="createBulkShipments"
                                class="bg-blue-600 text-white px-3 py-1 rounded shadow hover:bg-blue-700 font-bold text-sm flex items-center gap-2"
                             >
                                <span class="bg-white text-blue-600 rounded-full px-2 text-xs py-0.5">{{ selectedIds.size }}</span>
                                <span>SEÇİLENLERİ OLUŞTUR</span>
                             </button>

                             <input
                                type="text"
                                v-model="zoneSearch"
                                @input="handleSearch"
                                placeholder="Bölge Filtrele..."
                                class="border dark:border-gray-700 rounded px-3 py-1 text-sm w-32 dark:bg-gray-800 dark:text-gray-100"
                             />
                             <select v-model="talepNoFilter" @change="loadOrders" class="border dark:border-gray-700 rounded px-3 py-1 text-sm bg-white dark:bg-gray-800 dark:text-gray-100">
                                <option value="">Tümü</option>
                                <option value="Zero">Catering</option>
                                <option value="NonZero">Diğer</option>
                             </select>
                             <input
                                type="text"
                                v-model="searchQuery"
                                @input="handleSearch"
                                placeholder="Sipariş / Proje / Talep Ara..."
                                class="border dark:border-gray-700 rounded px-3 py-1 text-sm w-48 dark:bg-gray-800 dark:text-gray-100"
                             />
                         </div>
                     </div>

                     <!-- History Tab -->
                     <div v-if="activeTab === 'History'" class="p-4">
                         <div v-if="historyLoading" class="text-center py-8 dark:text-gray-400">Yükleniyor...</div>
                         <div v-else-if="batches.length === 0" class="text-center py-8 text-gray-500 dark:text-gray-400">Henüz import geçmişi yok.</div>
                         <div v-else class="overflow-x-auto">
                             <table class="w-full text-sm text-left border-collapse">
                                 <thead>
                                     <tr class="bg-gray-50 dark:bg-gray-800 text-xs font-semibold uppercase text-gray-500">
                                         <th class="p-3">Tarih</th>
                                         <th class="p-3">Aralık</th>
                                         <th class="p-3 text-center">Gelen</th>
                                         <th class="p-3 text-center">Yeni</th>
                                         <th class="p-3 text-center">Atlandı</th>
                                         <th class="p-3 text-center">Eşl. Bekl.</th>
                                         <th class="p-3 text-center">Hatalı</th>
                                         <th class="p-3 text-center">Süre</th>
                                         <th class="p-3">Durum</th>
                                     </tr>
                                 </thead>
                                 <tbody class="divide-y divide-gray-100 dark:divide-gray-700">
                                     <tr v-for="b in batches" :key="b.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
                                         <td class="p-3 dark:text-gray-300 text-xs">{{ formatDate(b.startedAt) }} {{ new Date(b.startedAt).toLocaleTimeString('tr-TR', {hour:'2-digit',minute:'2-digit'}) }}</td>
                                         <td class="p-3 text-xs text-gray-500">{{ formatDate(b.requestedStartDate) }} – {{ formatDate(b.requestedEndDate) }}</td>
                                         <td class="p-3 text-center dark:text-gray-300">{{ b.totalFromSource }}</td>
                                         <td class="p-3 text-center text-green-600 font-medium">{{ b.newCount }}</td>
                                         <td class="p-3 text-center text-gray-400">{{ b.skippedCount }}</td>
                                         <td class="p-3 text-center" :class="b.needsMappingCount > 0 ? 'text-yellow-600 font-medium' : 'text-gray-400'">{{ b.needsMappingCount }}</td>
                                         <td class="p-3 text-center" :class="b.failedCount > 0 ? 'text-red-600 font-medium' : 'text-gray-400'">{{ b.failedCount }}</td>
                                         <td class="p-3 text-center text-xs text-gray-500">{{ (b.durationMs / 1000).toFixed(1) }}s</td>
                                         <td class="p-3">
                                             <span class="px-2 py-0.5 rounded text-xs font-medium"
                                                 :class="{
                                                     'bg-green-100 text-green-700': b.status === 'Completed',
                                                     'bg-yellow-100 text-yellow-700': b.status === 'PartialSuccess',
                                                     'bg-red-100 text-red-700': b.status === 'Failed',
                                                     'bg-blue-100 text-blue-700': b.status === 'Running'
                                                 }">
                                                 {{ b.status === 'Completed' ? 'Tamamlandı' : b.status === 'PartialSuccess' ? 'Kısmi' : b.status === 'Failed' ? 'Hatalı' : 'Çalışıyor' }}
                                             </span>
                                             <div v-if="b.errorSummary" class="text-xs text-red-500 mt-1 max-w-xs truncate" :title="b.errorSummary">{{ b.errorSummary }}</div>
                                         </td>
                                     </tr>
                                 </tbody>
                             </table>
                         </div>
                     </div>

                     <div v-else class="p-4">
                         <div v-if="loading" class="text-center py-8 dark:text-gray-400">Yükleniyor...</div>
                         <div v-else-if="orders.length === 0" class="text-center py-8 text-gray-500 dark:text-gray-400">
                             Listenizde sipariş bulunamadı.
                         </div>

                         <div v-else>
                             <div class="overflow-x-auto">
                                <table class="w-full text-left border-collapse">
                                    <thead>
                                        <tr class="bg-gray-50 dark:bg-gray-800 text-xs font-semibold uppercase text-gray-600 dark:text-gray-400">
                                            <th class="p-3 w-10 text-center">
                                                <input
                                                    type="checkbox"
                                                    :checked="isAllSelected"
                                                    @change="toggleSelectAll"
                                                    class="h-4 w-4 rounded border-gray-300 dark:border-gray-700 text-blue-600 focus:ring-blue-500 cursor-pointer"
                                                />
                                            </th>
                                            <th class="p-3">Sipariş No</th>
                                            <th class="p-3">Talep No</th>
                                            <th class="p-3">Kurum/Proje Kodu</th>
                                            <th class="p-3">Proje Adı</th>
                                            <th class="p-3">Bölge</th>
                                            <th class="p-3">Tarih</th>
                                            <th class="p-3">Kalem</th>
                                            <th class="p-3">İşlem</th>
                                        </tr>
                                    </thead>
                                    <tbody class="divide-y divide-gray-100 dark:divide-gray-700">
                                        <tr v-for="order in orders" :key="order.id || order.Id" class="hover:bg-gray-50 dark:hover:bg-gray-800" :class="{'bg-blue-50': selectedIds.has(order.id || order.Id)}">
                                            <td class="p-3 text-center">
                                                <input
                                                    type="checkbox"
                                                    :checked="selectedIds.has(order.id || order.Id)"
                                                    @change="toggleSelection(order.id || order.Id)"
                                                    class="h-4 w-4 rounded border-gray-300 dark:border-gray-700 text-blue-600 focus:ring-blue-500 cursor-pointer"
                                                />
                                            </td>
                                            <td class="p-3 font-medium dark:text-gray-100">{{ order.externalOrderNumber || order.ExternalOrderNumber }}</td>
                                            <td class="p-3 text-sm dark:text-gray-300">{{ (order.talepNo || order.TalepNo) || '-' }}</td>
                                            <td class="p-3 text-sm text-gray-900 dark:text-gray-100 font-medium">
                                                <div class="text-xs text-gray-500 dark:text-gray-400">{{ order.institutionCode || order.InstitutionCode }}</div>
                                                <div>{{ order.projectCode || order.ProjectCode }}</div>
                                            </td>
                                            <td class="p-3 text-sm text-gray-500 dark:text-gray-400">{{ order.projectName || order.ProjectName }}</td>
                                            <td class="p-3 text-sm text-gray-500 dark:text-gray-400">{{ order.region || order.Region }}</td>
                                            <td class="p-3 text-sm dark:text-gray-300">{{ formatDate(order.orderDate || order.OrderDate) }}</td>
                                            <td class="p-3 dark:text-gray-300">{{ order.lineCount || order.LineCount }}</td>
                                            <td class="p-3 flex gap-2">
                                                <button
                                                    @click="openDetail(order)"
                                                    class="text-xs bg-gray-50 dark:bg-gray-800 text-gray-600 dark:text-gray-400 px-2 py-1 rounded hover:bg-gray-100 dark:hover:bg-gray-700 border border-gray-200 dark:border-gray-700"
                                                >
                                                    Detay
                                                </button>
                                                <template v-if="activeTab === 'Ready'">
                                                    <button
                                                        @click="createShipment(order.id || order.Id)"
                                                        class="text-xs bg-blue-50 text-blue-600 px-2 py-1 rounded hover:bg-blue-100 border border-blue-200"
                                                    >
                                                        Sevkiyat
                                                    </button>
                                                    <button
                                                        @click="toggleActive(order.id || order.Id, false)"
                                                        class="text-xs bg-amber-50 text-amber-600 px-2 py-1 rounded hover:bg-amber-100 border border-amber-200"
                                                    >
                                                        Pasife Al
                                                    </button>
                                                </template>
                                                <template v-else-if="activeTab === 'Passive'">
                                                     <button
                                                        @click="toggleActive(order.id || order.Id, true)"
                                                        class="text-xs bg-green-50 text-green-600 px-2 py-1 rounded hover:bg-green-100 border border-green-200"
                                                    >
                                                        Aktife Al
                                                    </button>
                                                </template>
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                             </div>

                     <!-- Pagination -->
                     <div class="mt-4 flex justify-between items-center border-t dark:border-gray-700 pt-4">
                        <div class="text-sm text-gray-500 dark:text-gray-400">
                            Toplam {{ totalCount }} kayıt, Sayfa {{ page }} / {{ totalPages }}
                        </div>
                        <div class="flex gap-2">
                            <button
                                @click="page--"
                                :disabled="page <= 1"
                                class="px-3 py-1 border dark:border-gray-700 rounded hover:bg-gray-50 dark:hover:bg-gray-800 disabled:opacity-50"
                            >
                                Önceki
                            </button>
                            <button
                                @click="page++"
                                :disabled="page >= totalPages"
                                class="px-3 py-1 border dark:border-gray-700 rounded hover:bg-gray-50 dark:hover:bg-gray-800 disabled:opacity-50"
                            >
                                Sonraki
                            </button>
                        </div>
                     </div>
                 </div>
             </div>
        </div>
        </div>

    <!-- Detail Modal -->
    <div v-if="selectedOrder" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50 overflow-y-auto">
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-full max-w-2xl max-h-[90vh] overflow-y-auto">
            <div class="px-6 py-4 border-b dark:border-gray-700 flex justify-between items-center sticky top-0 bg-white dark:bg-gray-900">
                <h3 class="text-lg font-bold dark:text-gray-100">Sipariş Detayı (#{{ selectedOrder.externalOrderNumber }})</h3>
                <button @click="selectedOrder = null" class="text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 font-bold text-xl">&times;</button>
            </div>
            <div class="p-6 space-y-4">
                 <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
                     <div>
                         <span class="block text-gray-500 dark:text-gray-400 text-xs">Talep No</span>
                         <span class="font-medium dark:text-gray-100">{{ selectedOrder.talepNo || '-' }}</span>
                     </div>
                      <div>
                         <span class="block text-gray-500 dark:text-gray-400 text-xs">Talep Türü</span>
                         <span class="font-medium dark:text-gray-100">{{ selectedOrder.talepTuru || '-' }}</span>
                     </div>
                     <div class="col-span-2">
                         <span class="block text-gray-500 dark:text-gray-400 text-xs">Proje</span>
                         <span class="font-medium dark:text-gray-100">{{ selectedOrder.projectCode }} - {{ selectedOrder.projectName }}</span>
                     </div>
                      <div class="col-span-2">
                         <span class="block text-gray-500 dark:text-gray-400 text-xs">Açıklama</span>
                         <div class="bg-gray-50 dark:bg-gray-800 p-2 rounded max-h-32 overflow-y-auto dark:text-gray-300">{{ selectedOrder.aciklama || '-' }}</div>
                     </div>

                     <div class="col-span-2 border-t dark:border-gray-700 pt-2 mt-2">
                         <h4 class="font-semibold mb-2 dark:text-gray-100">İletişim Bilgileri</h4>
                     </div>
                      <div>
                         <span class="block text-gray-500 dark:text-gray-400 text-xs">Teslim Alacak</span>
                         <span class="font-medium dark:text-gray-100">{{ selectedOrder.teslimAlacakKisiler || '-' }}</span>
                     </div>
                      <div>
                         <span class="block text-gray-500 dark:text-gray-400 text-xs">Telefonlar</span>
                         <span class="font-medium dark:text-gray-100">{{ selectedOrder.teslimAlacakTelefonNumaralari || '-' }}</span>
                     </div>
                       <div class="col-span-2">
                         <span class="block text-gray-500 dark:text-gray-400 text-xs">Yönetici Mailleri</span>
                         <span class="font-medium dark:text-gray-100 break-words">{{ selectedOrder.yoneticiMailAdresleri || '-' }}</span>
                     </div>
                 </div>
            </div>
            <div class="px-6 py-4 bg-gray-50 dark:bg-gray-800 border-t dark:border-gray-700 text-right">
                <button @click="selectedOrder = null" class="px-4 py-2 bg-gray-600 text-white rounded hover:bg-gray-700">Kapat</button>
            </div>
        </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, computed, onUnmounted } from 'vue';
import shipmentService from '../services/shipmentService';
import projectService from '../services/projectService';
import StockMappingManager from '../components/StockMappingManager.vue';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const notificationStore = useNotificationStore();
const confirm = {
    show: async (msg: string) => window.confirm(msg)
};

// Simple debounce
function simpleDebounce(fn: Function, delay: number) {
  let timeoutId: any;
  return (...args: any[]) => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => fn(...args), delay);
  };
}

const startDate = ref(new Date().toISOString().split('T')[0]);
const endDate = ref(new Date().toISOString().split('T')[0]);

const activeTab = ref('Ready');
const orders = ref<any[]>([]);
const loading = ref(false);
const importing = ref(false);
const importBatchId = ref<number | null>(null);
const importPollInterval = ref<ReturnType<typeof setInterval> | null>(null);
const importResult = ref<null | { totalFromIss: number; newCount: number; skippedCount: number; needsMappingCount: number; failedCount: number; errors: string[] }>(null);
const showMapping = ref(true);

const batches = ref<any[]>([]);
const historyLoading = ref(false);

// Pagination & Search
const page = ref(1);
const pageSize = ref(20);
const totalCount = ref(0);
const totalPages = ref(1);
const searchQuery = ref('');
const zoneSearch = ref(''); // New Zone Filter
const talepNoFilter = ref(''); // "Zero", "NonZero", ""
const selectedIds = ref(new Set<number>()); // New Selection Set

const readyCount = ref(0);
const needsMappingCount = ref(0);
const passiveCount = ref(0);

const selectedOrder = ref<any>(null);

const isAllSelected = computed(() => {
    return orders.value.length > 0 && orders.value.every(order => selectedIds.value.has(order.id));
});

const toggleSelection = (id: number) => {
    if (selectedIds.value.has(id)) {
        selectedIds.value.delete(id);
    } else {
        selectedIds.value.add(id);
    }
};

const toggleSelectAll = () => {
    if (isAllSelected.value) {
        // Deselect all visible
        orders.value.forEach(order => selectedIds.value.delete(order.id));
    } else {
        // Select all visible
        orders.value.forEach(order => selectedIds.value.add(order.id));
    }
};

const clearSelection = () => {
    selectedIds.value.clear();
};

const createBulkShipments = async () => {
    if (selectedIds.value.size === 0) return;
    if (!await confirm.show(`${selectedIds.value.size} adet sipariş için sevkiyat oluşturulacak. Onaylıyor musunuz?`)) return;

    try {
        const res = await shipmentService.bulkCreateFromIss({
            issOrderIds: Array.from(selectedIds.value)
        });
        notificationStore.add(`${res.createdCount} adet sevkiyat başarıyla oluşturuldu.`, 'success');
        // Reload and clear selection
        await loadOrders();
        clearSelection();
    } catch (e) {
        console.error(e);
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Toplu oluşturma sırasında hata oluştu.', 'error');
    }
};

const fetchCounts = async () => {
    try {
        const data = await shipmentService.getIssOrderCounts();
        readyCount.value = data.readyCount;
        needsMappingCount.value = data.needsMappingCount;
        passiveCount.value = data.passiveCount;
    } catch (e) {
        console.error(e);
    }
};

const loadOrders = async () => {
    loading.value = true;
    try {
        const data = await shipmentService.getIssOrders({
            tab: activeTab.value,
            page: page.value,
            pageSize: pageSize.value,
            search: searchQuery.value,
            zone: zoneSearch.value,
            talepNoStatus: talepNoFilter.value
        });

        orders.value = data.items;
        totalCount.value = data.totalCount;
        totalPages.value = data.totalPages;

        // Preserve selection across pages?
        // Usually tricky with simple Set, but we can keep them.
        // User asked to select all filtered, which currently only effectively works for visible page.
        // If they navigate, they can keep selecting.

        await fetchCounts();
    } catch (e) {
        console.error(e);
    } finally {
        loading.value = false;
    }
};

const handleSearch = simpleDebounce(() => {
    page.value = 1;
    loadOrders();
}, 500);

const loadHistory = async () => {
    historyLoading.value = true;
    try {
        const data = await shipmentService.getImportBatches();
        batches.value = data.items;
    } catch (e) {
        console.error(e);
    } finally {
        historyLoading.value = false;
    }
};

const stopImportPolling = () => {
    if (importPollInterval.value) {
        clearInterval(importPollInterval.value);
        importPollInterval.value = null;
    }
};

const importOrders = async () => {
    importing.value = true;
    importResult.value = null;
    importBatchId.value = null;
    stopImportPolling();
    try {
        const { batchId } = await shipmentService.startImportAsync({
            startDate: startDate.value || '',
            endDate: endDate.value || ''
        });
        importBatchId.value = batchId;

        // Poll every 3 seconds until batch completes
        importPollInterval.value = setInterval(async () => {
            try {
                const batch = await shipmentService.getImportBatchStatus(batchId);
                const done = batch.status !== 'Running';
                if (done) {
                    stopImportPolling();
                    importing.value = false;
                    importResult.value = {
                        totalFromIss: batch.totalFromSource,
                        newCount: batch.newCount,
                        skippedCount: batch.skippedCount,
                        needsMappingCount: batch.needsMappingCount,
                        failedCount: batch.failedCount,
                        errors: batch.errorSummary ? [batch.errorSummary] : []
                    };
                    page.value = 1;
                    await loadOrders();
                    await loadHistory();
                }
            } catch (e) {
                console.error('Batch poll error:', e);
                // Keep polling; transient errors should not stop monitoring
            }
        }, 3000);
    } catch (e) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Aktarım başlatılamadı', 'error');
        console.error(e);
        importing.value = false;
    }
};

onUnmounted(stopImportPolling);

const syncing = ref(false);
const syncProjects = async () => {
    syncing.value = true;
    try {
        const res = await projectService.syncProjects();
        await loadOrders();
        notificationStore.add(`${res.count} proje senkronize edildi.`, 'success');
    } catch (e) {
        console.error(e);
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Hata oluştu.', 'error');
    } finally {
        syncing.value = false;
    }
};

const checking = ref(false);
const checkMappings = async () => {
    checking.value = true;
    try {
        const res = await shipmentService.checkMappings();
        notificationStore.add(`${res.count} siparişin durumu güncellendi.`, 'success');
        await loadOrders();
    } catch (e) {
        console.error(e);
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Hata oluştu.', 'error');
    } finally {
        checking.value = false;
    }
};

const createShipment = async (orderId: number) => {
    if (!await confirm.show('Bu sipariş için sevkiyat oluşturmak istediğinize emin misiniz?')) return;
    try {
        await shipmentService.createShipmentFromIss(orderId);
        notificationStore.add('Sevkiyat başarıyla oluşturuldu.', 'success');
    } catch (e) {
        console.error(e);
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Sevkiyat oluşturulurken hata oluştu.', 'error');
    }
};

const toggleActive = async (id: number, isActive: boolean) => {
    if (!await confirm.show(isActive ? 'Siparişi aktife almak istiyor musunuz?' : 'Siparişi pasife almak istiyor musunuz?')) return;
    try {
        await shipmentService.toggleIssActive(id, isActive);
        await loadOrders();
    } catch (e) {
        console.error(e);
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
    }
};

const openDetail = (order: any) => {
    selectedOrder.value = order;
};

const formatDate = (d: string) => {
    if(!d) return '';
    return new Date(d).toLocaleDateString('tr-TR');
};

watch([activeTab, page], loadOrders);

onMounted(loadOrders);
</script>
