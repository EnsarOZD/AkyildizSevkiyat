<template>
  <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-4">
    <div class="flex justify-between items-center mb-4">
      <h2 class="text-lg font-semibold">Eşleştirme Bekleyen Stoklar</h2>
    <div class="flex flex-wrap gap-2">
      <!-- Download Template Button -->
      <button @click="downloadTemplate" class="px-3 py-1 bg-blue-600 text-white rounded hover:bg-blue-700 text-sm flex items-center gap-1">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
        </svg>
        Excel İndir
      </button>

      <input type="file" ref="fileInput" class="hidden" accept=".xlsx" @change="uploadStocks">
      <button @click="fileInput?.click()" class="px-3 py-1 bg-green-600 text-white rounded hover:bg-green-700 text-sm flex items-center gap-1">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
        </svg>
        Stok (Yeni Kart) Yükle
      </button>

      <input type="file" ref="mappingFileInput" class="hidden" accept=".xlsx" @change="uploadStockMappings">
      <button @click="mappingFileInput?.click()" class="px-3 py-1 bg-purple-600 text-white rounded hover:bg-purple-700 text-sm flex items-center gap-1">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
        </svg>
        Excel Yükle (Eşleştirme)
      </button>

      <button @click="autoMatch" :disabled="autoMatching" class="px-3 py-1 bg-yellow-500 text-white rounded hover:bg-yellow-600 text-sm flex items-center gap-1 disabled:opacity-50">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
        </svg>
        {{ autoMatching ? 'Eşleştiriliyor...' : 'Otomatik Eşleştir' }}
      </button>

      <button @click="refreshbi" class="p-2 hover:bg-gray-100 dark:hover:bg-gray-700 rounded" title="Yenile">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 text-gray-500 dark:text-gray-400" viewBox="0 0 20 20" fill="currentColor">
          <path fill-rule="evenodd" d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z" clip-rule="evenodd" />
        </svg>
      </button>
    </div>
    </div>

    <div v-if="loading" class="text-center py-4">Checking...</div>

    <div v-else-if="unmappedStocks.length === 0" class="text-center py-8 text-gray-500 dark:text-gray-400">
      Wait! No unmapped stocks found. Everything looks good.
    </div>

    <div v-else class="space-y-4">
      <div v-for="stock in unmappedStocks" :key="stock.mappingId" class="border dark:border-gray-700 rounded p-3">
        <!-- Ana satır -->
        <div class="flex flex-col md:flex-row gap-4 items-center">
          <div class="flex-1">
            <div class="font-medium text-red-600">{{ stock.externalCode }}</div>
            <div class="text-sm text-gray-600 dark:text-gray-400">{{ stock.externalName }}</div>
          </div>

          <div class="flex flex-wrap gap-2 items-center">
              <!-- Local Stock Selector -->
              <div class="relative w-48">
                <StockCombobox
                  placeholder="Stok Ara (Kod/Ad)"
                  v-model="stock.selectedLocalId"
                  @search="(val) => stock.currentSearch = val"
                  @select="(item) => onStockSelected(stock, item)"
                />
              </div>

              <!-- Add Button — toggles inline create form -->
              <button
                v-if="!stock.selectedLocalId && stock.currentSearch && stock.currentSearch.length > 1"
                @click="stock.showCreateForm = !stock.showCreateForm"
                class="px-2 py-1 text-xs flex items-center gap-1 rounded"
                :class="stock.showCreateForm
                  ? 'bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300'
                  : 'bg-teal-500 hover:bg-teal-600 text-white'"
                title="Yeni Stok Kartı Aç"
              >
                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    :d="stock.showCreateForm ? 'M6 18L18 6M6 6l12 12' : 'M12 4v16m8-8H4'" />
                </svg>
                {{ stock.showCreateForm ? 'İptal' : 'Yeni Kart' }}
              </button>

              <!-- Netsis Stok Kodu — sadece yerel stok seçilince göster -->
              <div v-if="stock.selectedLocalId" class="flex items-center gap-1">
                <label class="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap">Netsis Kodu:</label>
                <input
                  v-model="stock.netsisStockCode"
                  type="text"
                  placeholder="Netsis stok kodu"
                  class="w-32 border rounded px-2 py-1 text-xs focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:border-gray-600 dark:text-gray-100"
                  :class="!stock.netsisStockCode ? 'border-orange-400 dark:border-orange-500' : 'border-gray-300'"
                  title="Bu stok kartının Netsis'teki stok kodu. Netsis aktarımı için zorunludur."
                />
                <span v-if="!stock.netsisStockCode" class="text-xs text-orange-500" title="Netsis aktarımı için zorunlu">⚠</span>
              </div>

              <button
                @click="mapStock(stock, false)"
                class="px-3 py-1 bg-blue-600 text-white rounded hover:bg-blue-700 text-sm disabled:opacity-50 disabled:cursor-not-allowed"
                :disabled="!stock.selectedLocalId"
              >
                Eşleştir
              </button>

              <button
                @click="mapStock(stock, true)"
                class="px-3 py-1 bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded hover:bg-gray-300 dark:hover:bg-gray-600 text-sm"
              >
                Yoksay
              </button>
          </div>
        </div>

        <!-- Inline yeni stok kartı formu -->
        <div v-if="stock.showCreateForm" class="mt-3 pt-3 border-t dark:border-gray-700 bg-teal-50 dark:bg-teal-900/10 rounded-b p-3">
          <p class="text-xs font-semibold text-teal-700 dark:text-teal-400 mb-2">
            Yeni stok kartı: <span class="font-bold">{{ stock.currentSearch }}</span>
          </p>
          <div class="flex flex-wrap gap-3 items-end">
            <!-- Kategori (zorunlu) -->
            <div>
              <label class="block text-xs text-gray-600 dark:text-gray-400 mb-0.5">
                Kategori <span class="text-red-500">*</span>
              </label>
              <select
                v-model="stock.newCategory"
                class="border rounded px-2 py-1 text-xs bg-white dark:bg-gray-800 dark:border-gray-600 dark:text-gray-100 focus:ring-2 focus:ring-teal-500"
                :class="!stock.newCategory ? 'border-red-400' : 'border-gray-300'"
              >
                <option :value="undefined" disabled>Seçin...</option>
                <option :value="1">Gıda</option>
                <option :value="2">Sarf</option>
                <option :value="3">Kıyafet</option>
                <option :value="4">Temizlik</option>
                <option :value="5">Kırtasiye</option>
                <option :value="99">Diğer</option>
              </select>
            </div>

            <!-- Birim -->
            <div>
              <label class="block text-xs text-gray-600 dark:text-gray-400 mb-0.5">Birim</label>
              <select
                v-model="stock.newUnit"
                class="border border-gray-300 dark:border-gray-600 rounded px-2 py-1 text-xs bg-white dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-teal-500"
              >
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

            <!-- Picking Tipi -->
            <div>
              <label class="block text-xs text-gray-600 dark:text-gray-400 mb-0.5">Picking</label>
              <select
                v-model="stock.newPickingType"
                class="border border-gray-300 dark:border-gray-600 rounded px-2 py-1 text-xs bg-white dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-teal-500"
              >
                <option :value="1">Micro</option>
                <option :value="2">Macro</option>
              </select>
            </div>

            <button
              @click="createAndSelectStock(stock)"
              :disabled="!stock.newCategory"
              class="px-3 py-1 bg-teal-600 hover:bg-teal-700 text-white rounded text-xs font-semibold disabled:opacity-40 disabled:cursor-not-allowed"
            >
              Oluştur
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import shipmentService from '../services/shipmentService';
import { stockService } from '../services/stockService';
import StockCombobox from './StockCombobox.vue';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const notificationStore = useNotificationStore();

interface UnmappedStock {
    mappingId: number;
    externalCode: string;
    externalName: string;
    selectedLocalId?: number;
    currentSearch?: string;
    netsisStockCode?: string;  // StockMaster.NetsisStockCode — Netsis aktarımı için zorunlu
    // Yeni kart formu
    showCreateForm?: boolean;
    newCategory?: number;       // StockCategory enum değeri (1-99, 0=Tanimsiz yasak)
    newUnit?: number;           // StockUnit enum değeri, varsayılan 0=Adet
    newPickingType?: number;    // PickingType enum değeri, varsayılan 1=Micro
}

const unmappedStocks = ref<UnmappedStock[]>([]);
const loading = ref(false);
const autoMatching = ref(false);
const emit = defineEmits(['mapped']);
const fileInput = ref<HTMLInputElement | null>(null);
const mappingFileInput = ref<HTMLInputElement | null>(null);

const autoMatch = async () => {
    autoMatching.value = true;
    try {
        const result = await shipmentService.autoMatchMappings();
        if (result.matchedCount > 0) {
            let msg = `${result.matchedCount} stok isim eşleşmesiyle otomatik eşleştirildi.`;
            if (result.ordersUnlocked > 0) msg += ` ${result.ordersUnlocked} sipariş hazır durumuna geçti.`;
            notificationStore.add(msg, 'success');
            emit('mapped');
            await refreshbi();
        } else {
            notificationStore.add('İsim eşleşmesi bulunamadı. Manuel eşleştirme gerekiyor.', 'warning');
        }
    } catch (e) {
        notificationStore.add('Otomatik eşleştirme başarısız: ' + (ApiErrorUtils.getErrorMessage(e) || 'Bilinmeyen hata'), 'error');
    } finally {
        autoMatching.value = false;
    }
};

const refreshbi = async () => {
    loading.value = true;
    try {
        const data = await shipmentService.getUnmappedStocks();
        unmappedStocks.value = data.map((s: any) => ({...s, selectedLocalId: null}));
    } catch (e) {
        console.error(e);
    } finally {
        loading.value = false;
    }
};

const uploadStocks = async (event: any) => {
    const file = event.target.files[0];
    if (!file) return;

    try {
        loading.value = true;
        const formData = new FormData();
        formData.append('file', file);
        const result = await shipmentService.importStocks(formData);
        const msg = `Eklendi: ${result.added}, Güncellendi: ${result.updated}, Atlandı: ${result.skipped}`;
        notificationStore.add(msg, result.errors.length > 0 ? 'warning' : 'success');
        if (result.errors.length > 0) {
            result.errors.slice(0, 3).forEach(e => notificationStore.add(e, 'error'));
        }
        window.location.reload();
    } catch (e) {
        console.error(e);
        notificationStore.add('Stok yükleme başarısız: ' + (ApiErrorUtils.getErrorMessage(e) || 'Bilinmeyen hata'), 'error');
    } finally {
        loading.value = false;
        if (fileInput.value) fileInput.value.value = '';
    }
};

const uploadStockMappings = async (event: any) => {
    const file = event.target.files[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('file', file);

    try {
        loading.value = true;
        const result = await shipmentService.importStockMappings(formData);

        if (result.mappedCount > 0) {
            notificationStore.add(`${result.mappedCount} eşleştirme başarıyla güncellendi.`, 'success');
        }

        if (result.notFoundStocks.length > 0) {
            notificationStore.add(
                `${result.notFoundStocks.length} yerel stok kodu bulunamadı: ${result.notFoundStocks.slice(0, 3).join(', ')}${result.notFoundStocks.length > 3 ? '...' : ''}`,
                'warning'
            );
        }

        if (result.notFoundMappings.length > 0) {
            notificationStore.add(
                `${result.notFoundMappings.length} ISS kodu eşleştirme tablosunda yok: ${result.notFoundMappings.slice(0, 3).join(', ')}${result.notFoundMappings.length > 3 ? '...' : ''}`,
                'warning'
            );
        }

        if (result.mappedCount === 0 && result.notFoundStocks.length === 0 && result.notFoundMappings.length === 0) {
            notificationStore.add('İşlenecek satır bulunamadı. C sütununu (Yerel Stok Kodu) doldurduğunuzdan emin olun.', 'warning');
        }

        if (result.mappedCount > 0) window.location.reload();
    } catch (e) {
        console.error(e);
        notificationStore.add('Eşleştirme yükleme başarısız: ' + (ApiErrorUtils.getErrorMessage(e) || 'Bilinmeyen hata'), 'error');
    } finally {
        loading.value = false;
        if (mappingFileInput.value) mappingFileInput.value.value = '';
    }
};

const onStockSelected = (stock: UnmappedStock, item: any) => {
    stock.netsisStockCode = item.netsisStockCode ?? item.NetsisStockCode ?? '';
};

const createAndSelectStock = async (stock: UnmappedStock) => {
    if (!stock.currentSearch || !stock.newCategory) return;

    try {
        const newStock = await stockService.create({
            stockCode: stock.currentSearch,
            stockName: stock.externalName,
            category: stock.newCategory,
            unit: stock.newUnit ?? 0,
            pickingType: stock.newPickingType ?? 1,
        });

        stock.selectedLocalId = newStock.id;
        stock.showCreateForm = false;
        notificationStore.add(`"${stock.currentSearch}" stok kartı oluşturuldu.`, 'success');
    } catch (e) {
        console.error(e);
        notificationStore.add("Stok oluşturulamadı: " + (ApiErrorUtils.getErrorMessage(e) || 'Bilinmeyen hata'), 'error');
    }
};

const mapStock = async (stock: UnmappedStock, ignore: boolean) => {
    try {
        // Netsis stok kodunu stok kartına kaydet (boş olmayan durumlarda)
        if (!ignore && stock.selectedLocalId && stock.netsisStockCode !== undefined) {
            await stockService.updateNetsisCode(stock.selectedLocalId, stock.netsisStockCode || null);
        }

        await shipmentService.createMapping({
            mappingId: stock.mappingId,
            localStockId: ignore ? null : stock.selectedLocalId,
            ignore: ignore
        });

        unmappedStocks.value = unmappedStocks.value.filter(s => s.mappingId !== stock.mappingId);
        emit('mapped');
    } catch (e) {
        notificationStore.add("Eşleştirme başarısız: " + (ApiErrorUtils.getErrorMessage(e) || 'Bilinmeyen hata'), 'error');
        console.error(e);
    }
};

const downloadTemplate = async () => {
    try {
        const data = await shipmentService.exportUnmappedStocks();
        const url = window.URL.createObjectURL(new Blob([data]));
        const link = document.createElement('a');
        link.href = url;
        link.setAttribute('download', 'EslestirilecekStoklar.xlsx');
        document.body.appendChild(link);
        link.click();
        link.remove();
    } catch (e) {
        console.error(e);
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || "Dosya indirilemedi.", 'error');
    }
};

onMounted(refreshbi);
</script>
