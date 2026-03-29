<template>
  <div class="p-6 space-y-6">
    <div class="flex justify-between items-center">
      <div>
        <h1 class="text-2xl font-bold mb-2">Stok Yönetimi</h1>
        <p class="text-gray-600 dark:text-gray-400">Sistemdeki stok tanımlarını yönetin.</p>
      </div>
      <div class="flex gap-2">
        <button
          @click="downloadTemplate"
          class="bg-gray-600 text-white px-4 py-2 rounded hover:bg-gray-700 flex items-center gap-2"
        >
          <span>Şablon İndir</span>
        </button>
        <button
          @click="exportStocks"
          class="bg-blue-600 text-white px-4 py-2 rounded hover:bg-blue-700 flex items-center gap-2"
        >
          <span>📄 Excel İndir</span>
        </button>
        <button
          @click="openImportModal"
          class="bg-green-600 text-white px-4 py-2 rounded hover:bg-green-700 flex items-center gap-2"
        >
          <span>Excel ile Yükle / Güncelle</span>
        </button>
        <button
          @click="openModal()"
          class="bg-indigo-600 text-white px-4 py-2 rounded hover:bg-indigo-700 flex items-center gap-2"
        >
          <span>+ Yeni Stok</span>
        </button>
      </div>
    </div>

    <!-- Search -->
    <div class="bg-white dark:bg-gray-900 p-4 rounded shadow">
        <input
            v-model="searchQuery"
            @input="debouncedSearch"
            type="text"
            placeholder="Stok Kodu veya Adı Ara..."
            class="w-full border rounded px-3 py-2 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
        />
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
                <tr>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Kod</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Ad</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Birim</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Birim Fiyat</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">KDV %</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Toplama Tipi</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Kategori</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Marka</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Lokasyon</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Netsis Kodu</th>
                    <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlem</th>
                </tr>
            </thead>
            <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                <tr v-if="loading">
                    <td colspan="11" class="px-6 py-4 text-center text-gray-500 dark:text-gray-400">Yükleniyor...</td>
                </tr>
                <tr v-else-if="stocks.length === 0">
                    <td colspan="11" class="px-6 py-4 text-center text-gray-500 dark:text-gray-400">Kayıt bulunamadı.</td>
                </tr>
                <tr v-for="stock in stocks" :key="stock.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
                    <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">{{ stock.stockCode }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ stock.stockName }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ stock.unit || '-' }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ stock.unitPrice ? stock.unitPrice.toFixed(2) : '-' }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
                        {{ stock.taxRate !== undefined ? `%${stock.taxRate}` : '-' }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm">
                        <span v-if="stock.pickingTypeId === 1" class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-blue-100 text-blue-800">
                            Micro
                        </span>
                        <span v-else-if="stock.pickingTypeId === 2" class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-orange-100 text-orange-800">
                            Macro
                        </span>
                        <span v-else class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-gray-100 dark:bg-gray-800 text-gray-800 dark:text-gray-200">
                            Tanımsız
                        </span>
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ stock.category || '-' }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ stock.brand || '-' }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 font-mono text-xs">{{ stock.warehouseLocation || '-' }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 font-mono text-xs">{{ stock.netsisStockCode || '-' }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium flex justify-end gap-2">
                        <button @click="openThresholdModal(stock)" class="text-emerald-600 hover:text-emerald-900">Eşik</button>
                        <button @click="openModal(stock)" class="text-indigo-600 hover:text-indigo-900">Düzenle</button>
                        <button @click="deleteStock(stock)" class="text-red-600 hover:text-red-900">Sil</button>
                    </td>
                </tr>
            </tbody>
        </table>
      </div>
    </div>

    <!-- Pagination -->
    <Pagination
        :current-page="currentPage"
        :total-pages="totalPages"
        :total-count="totalCount"
        :page-size="pageSize"
        @page-change="handlePageChange"
    />

    <!-- Create/Edit Modal -->
    <div v-if="showModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
        <div class="bg-white dark:bg-gray-900 rounded-lg p-6 w-full max-w-md">
            <h3 class="text-lg font-bold mb-4">{{ isEditing ? 'Stok Düzenle' : 'Yeni Stok Ekle' }}</h3>
            <div class="space-y-4">
                 <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Stok Kodu</label>
                    <input v-model="form.stockCode" :disabled="isEditing" type="text" class="w-full border p-2 rounded disabled:bg-gray-100 dark:disabled:bg-gray-700 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
                 </div>
                 <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Stok Adı</label>
                    <input v-model="form.stockName" type="text" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
                 </div>
                  <!-- Category Selection -->
                  <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Kategori</label>
                    <select v-model.number="form.category" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
                        <option :value="0">Tanımsız</option>
                        <option :value="1">Gıda</option>
                        <option :value="2">Sarf</option>
                        <option :value="3">Kıyafet</option>
                        <option :value="4">Temizlik</option>
                        <option :value="5">Kırtasiye</option>
                        <option :value="99">Diğer</option>
                    </select>
                  </div>

                  <!-- Picking Type Selection -->
                  <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Toplama Tipi <span class="text-red-500">*</span></label>
                    <select v-model.number="form.pickingType" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
                        <option :value="0" disabled>Seçiniz...</option>
                        <option :value="1">Micro (Adet / Paket)</option>
                        <option :value="2">Macro (Koli / Palet)</option>
                    </select>
                    <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Lütfen doğru depo toplama tipini seçiniz.</p>
                  </div>

                  <div class="grid grid-cols-3 gap-2">
                      <div>
                        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Birim</label>
                        <select v-model.number="form.unit" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
                            <option :value="0">Adet</option>
                            <option :value="1">Kg</option>
                            <option :value="2">Paket</option>
                            <option :value="3">Koli</option>
                            <option :value="4">Litre</option>
                            <option :value="5">Metre</option>
                            <option :value="6">Metrekare</option>
                            <option :value="7">Set</option>
                            <option :value="8">Teneke</option>
                            <option :value="99">Diğer</option>
                        </select>
                      </div>
                      <div>
                        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Birim Fiyat</label>
                        <input v-model.number="form.unitPrice" type="number" step="0.01" inputmode="decimal" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
                      </div>
                      <div>
                        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">KDV %</label>
                        <select v-model.number="form.taxRate" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
                            <option :value="0">%0</option>
                            <option :value="1">%1</option>
                            <option :value="10">%10</option>
                            <option :value="20">%20</option>
                        </select>
                      </div>
                  </div>

                  <!-- Brand, MinStockQty, WarehouseLocation -->
                  <div class="border-t dark:border-gray-700 pt-4 mt-2">
                    <p class="text-xs text-gray-400 dark:text-gray-600 uppercase font-semibold mb-3">Ek Bilgiler</p>
                    <div class="grid grid-cols-2 gap-3">
                      <div>
                        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Marka</label>
                        <input v-model="form.brand" type="text" placeholder="Ör: Ariel" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
                      </div>
                      <div>
                        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Min. Stok Miktarı</label>
                        <input v-model.number="form.minStockQty" type="number" min="0" step="1" inputmode="numeric" placeholder="Ör: 10" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
                      </div>
                      <div>
                        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Yeniden Sipariş Noktası</label>
                        <input v-model.number="form.reorderPoint" type="number" min="0" step="1" inputmode="numeric" placeholder="Ör: 20" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
                      </div>
                      <div class="col-span-2">
                        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Depo Lokasyonu</label>
                        <input v-model="form.warehouseLocation" type="text" placeholder="Ör: A-Raf-3" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
                      </div>
                    </div>
                  </div>

                  <!-- Netsis -->
                  <div class="border-t dark:border-gray-700 pt-4 mt-2">
                    <p class="text-xs text-gray-400 dark:text-gray-600 uppercase font-semibold mb-3">Netsis Entegrasyonu</p>
                    <div>
                      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Netsis Stok Kodu</label>
                      <input v-model="form.netsisStockCode" type="text" placeholder="Ör: MML.001" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
                      <p class="text-xs text-gray-400 mt-1">Netsis sistemindeki stok kodu. API bağlantısı kurulunca stok bakiyesi senkronizasyonu için kullanılır.</p>
                    </div>
                  </div>
            </div>
            <div class="mt-6 flex justify-end gap-3">
                <button @click="showModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
                <button @click="saveStock" class="px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700">Kaydet</button>
            </div>
        </div>
    </div>

    <!-- Threshold Modal -->
    <div v-if="showThresholdModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
        <div class="bg-white dark:bg-gray-900 rounded-lg p-6 w-full max-w-sm">
            <h3 class="text-lg font-bold mb-1">Eşik Değerleri</h3>
            <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">{{ thresholdForm.stockName }}</p>
            <div class="space-y-4">
                <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Min. Stok Miktarı</label>
                    <input v-model.number="thresholdForm.minStockQty" type="number" min="0" step="1" inputmode="numeric" placeholder="Ör: 10" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
                </div>
                <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Yeniden Sipariş Noktası</label>
                    <input v-model.number="thresholdForm.reorderPoint" type="number" min="0" step="1" inputmode="numeric" placeholder="Ör: 20" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
                </div>
            </div>
            <div class="mt-6 flex justify-end gap-3">
                <button @click="showThresholdModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
                <button @click="saveThresholds" class="px-4 py-2 bg-emerald-600 text-white rounded hover:bg-emerald-700" :disabled="savingThresholds">
                    {{ savingThresholds ? 'Kaydediliyor...' : 'Kaydet' }}
                </button>
            </div>
        </div>
    </div>

    <!-- Import Modal -->
    <div v-if="showImportModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
        <div class="bg-white dark:bg-gray-900 rounded-lg p-6 w-full max-w-md">
             <div class="text-sm text-gray-600 dark:text-gray-400 mb-4 bg-gray-50 dark:bg-gray-800 p-3 rounded border dark:border-gray-700">
                <p class="font-bold mb-1 border-b dark:border-gray-700 pb-1">Excel Kolon Yapısı:</p>
                <ul class="space-y-1 list-disc list-inside">
                    <li><b>1. Kolon (Kod):</b> Örn: "ELMA-01"</li>
                    <li><b>2. Kolon (Ad):</b> Örn: "Amasya Elması"</li>
                    <li><b>3. Kolon (Birim):</b> Adet, Kg, Paket, Koli, Litre, Metre, Teneke...</li>
                    <li><b>4. Kolon (Fiyat):</b> Örn: "15.50"</li>
                    <li><b>5. Kolon (KDV):</b> 1, 10 veya 20</li>
                    <li><b>6. Kolon (Tip):</b> Micro veya Macro</li>
                    <li><b>7. Kolon (Durum):</b> Aktif veya Pasif</li>
                    <li><b>8. Kolon (Kategori):</b> Gida, Sarf, Kiyafet, Kirtasiye...</li>
                </ul>
             </div>

             <input type="file" ref="fileInput" accept=".xlsx, .xls" class="block w-full text-sm text-gray-500 dark:text-gray-400 file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-violet-50 file:text-violet-700 hover:file:bg-violet-100"/>

             <!-- Import Result Summary -->
             <div v-if="importResult" class="mt-4 text-sm rounded border p-3 space-y-1"
               :class="importResult.errors.length > 0 ? 'bg-red-50 border-red-200' : 'bg-green-50 border-green-200'">
               <p class="font-semibold" :class="importResult.errors.length > 0 ? 'text-red-800' : 'text-green-800'">
                 Son İçe Aktarma Sonucu:
               </p>
               <p>Eklenen: <strong>{{ importResult.added }}</strong> | Güncellenen: <strong>{{ importResult.updated }}</strong> | Atlanan: <strong>{{ importResult.skipped }}</strong></p>
               <ul v-if="importResult.warnings.length > 0" class="text-yellow-700 list-disc list-inside space-y-0.5 max-h-24 overflow-y-auto">
                 <li v-for="w in importResult.warnings" :key="w" class="text-xs">{{ w }}</li>
               </ul>
               <ul v-if="importResult.errors.length > 0" class="text-red-700 list-disc list-inside space-y-0.5 max-h-24 overflow-y-auto">
                 <li v-for="e in importResult.errors" :key="e" class="text-xs">{{ e }}</li>
               </ul>
             </div>

             <div class="mt-6 flex justify-end gap-3">
                <button @click="showImportModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
                <button @click="importStocks" class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700" :disabled="importing">
                    {{ importing ? 'Yükleniyor...' : 'Yükle' }}
                </button>
            </div>
        </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { stockService } from '../services/stockService';
import type { Stock, ImportStocksResult } from '../services/stockService';
import Pagination from '../components/Pagination.vue';
import { useNotification } from '../composables/useNotification';
import { ApiErrorUtils } from '../utils/apiError';
import { useKeyboardShortcut } from '../composables/useKeyboardShortcut';

const { notify, confirm } = useNotification();

const stocks = ref<Stock[]>([]);
const loading = ref(false);
const searchQuery = ref('');

// Pagination State
const currentPage = ref(1);
const totalPages = ref(1);
const totalCount = ref(0);
const pageSize = ref(15);

const fetchStocks = async (page = 1) => {
    loading.value = true;
    try {
        const data = await stockService.getAll({
            search: searchQuery.value || null,
            page: page,
            size: pageSize.value
        });

        stocks.value = data.items;
        currentPage.value = data.pageIndex;
        totalPages.value = data.totalPages;
        totalCount.value = data.totalCount;

    } catch (e) {
        console.error(e);
    } finally {
        loading.value = false;
    }
};

const handlePageChange = (page: number) => {
    fetchStocks(page);
};

let timeout: any;
const debouncedSearch = () => {
    clearTimeout(timeout);
    timeout = setTimeout(() => fetchStocks(1), 500);
};

const downloadTemplate = async () => {
    try {
        const data = await stockService.downloadTemplate();
        const url = window.URL.createObjectURL(new Blob([data]));
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', 'StokYuklemeŞablonu.xlsx');
        document.body.appendChild(link);
        link.click();
        link.remove();
    } catch (e) {
        notify.error(ApiErrorUtils.getErrorMessage(e, "Şablon indirilemedi."));
    }
};

const exportStocks = async () => {
    try {
        const data = await stockService.export();
        const url = window.URL.createObjectURL(new Blob([data]));
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', 'StokListesi.xlsx');
        document.body.appendChild(link);
        link.click();
        link.remove();
    } catch (e) {
        notify.error(ApiErrorUtils.getErrorMessage(e, "Dosya indirilemedi."));
    }
};

// Modal Logic
const showModal = ref(false);
const isEditing = ref(false);

// N tuşu → yeni stok ekle
useKeyboardShortcut('n', () => { if (!showModal.value) { isEditing.value = false; showModal.value = true; } });
const form = ref<any>({ id: 0, stockCode: '', stockName: '', unit: 0, unitPrice: null, taxRate: 20, pickingType: 0, category: 0, brand: '', minStockQty: null, reorderPoint: null, warehouseLocation: '', netsisStockCode: '' });

const openModal = (stock?: Stock) => {
    if (stock) {
        isEditing.value = true;
        form.value = {
            ...stock,
            pickingType: stock.pickingTypeId || 0,
            category: stock.categoryId || 0,
            unit: stock.unitId || 0,
            reorderPoint: stock.reorderPoint ?? null,
        };
    } else {
        isEditing.value = false;
        form.value = { id: 0, stockCode: '', stockName: '', unit: 0, unitPrice: null, taxRate: 20, pickingType: 0, category: 0, brand: '', minStockQty: null, reorderPoint: null, warehouseLocation: '', netsisStockCode: '' };
    }
    showModal.value = true;
};

const saveStock = async () => {
    try {
        if (!form.value.pickingType || form.value.pickingType === 0) {
             notify.warning('Lütfen Toplama Tipi (Micro/Macro) seçiniz.');
             return;
        }

        if (isEditing.value) {
            await stockService.update(form.value.id, form.value);
        } else {
            await stockService.create(form.value);
        }
        showModal.value = false;
        fetchStocks();
    } catch (e: unknown) {
        notify.error(ApiErrorUtils.getErrorMessage(e, 'Hata oluştu.'));
    }
};


const deleteStock = async (stock: Stock) => {
    if (!await confirm.requireDelete(stock.stockCode)) return;

    try {
        await stockService.delete(stock.id);
        notify.success('Stok silindi.');
        fetchStocks();
    } catch (e) {
        notify.error(ApiErrorUtils.getErrorMessage(e, 'Silme işlemi başarısız.'));
    }
};

// Threshold Modal Logic
const showThresholdModal = ref(false);
const savingThresholds = ref(false);
const thresholdForm = ref<{ id: number; stockName: string; minStockQty: number | null; reorderPoint: number | null }>({
    id: 0,
    stockName: '',
    minStockQty: null,
    reorderPoint: null,
});

const openThresholdModal = (stock: Stock) => {
    thresholdForm.value = {
        id: stock.id,
        stockName: stock.stockName,
        minStockQty: stock.minStockQty ?? null,
        reorderPoint: stock.reorderPoint ?? null,
    };
    showThresholdModal.value = true;
};

const saveThresholds = async () => {
    savingThresholds.value = true;
    try {
        await stockService.updateThresholds(thresholdForm.value.id, {
            minStockQty: thresholdForm.value.minStockQty,
            reorderPoint: thresholdForm.value.reorderPoint,
        });
        notify.success('Eşik değerleri güncellendi.');
        showThresholdModal.value = false;
        fetchStocks(currentPage.value);
    } catch (e) {
        notify.error(ApiErrorUtils.getErrorMessage(e, 'Eşik değerleri güncellenemedi.'));
    } finally {
        savingThresholds.value = false;
    }
};

// Import Logic
const showImportModal = ref(false);
const importing = ref(false);
const fileInput = ref<HTMLInputElement | null>(null);

const openImportModal = () => {
    showImportModal.value = true;
};

const importResult = ref<ImportStocksResult | null>(null);

const importStocks = async () => {
    if (!fileInput.value?.files?.length) {
        notify.warning('Lütfen bir dosya seçin.');
        return;
    }

    const file = fileInput.value.files[0];
    if (!file) return;

    importing.value = true;
    importResult.value = null;
    try {
        const result = await stockService.import(file);
        importResult.value = result;
        const msg = `${result.added} eklendi, ${result.updated} güncellendi, ${result.skipped} atlandı.`;
        if (result.errors.length > 0) {
            notify.warning(`İçe aktarma tamamlandı (${result.errors.length} hata). ${msg}`);
        } else {
            notify.success(`İçe aktarma tamamlandı. ${msg}`);
        }
        fetchStocks();
    } catch (e) {
        notify.error(ApiErrorUtils.getErrorMessage(e, 'Yükleme hatası.'));
    } finally {
        importing.value = false;
    }
};

onMounted(fetchStocks);
</script>
