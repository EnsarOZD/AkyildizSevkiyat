<template>
  <div class="p-6 space-y-6">
    <PageHeader title="Stok Yönetimi" subtitle="Sistemdeki stok tanımlarını yönetin" color="gray">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
        </svg>
      </template>
      <template #actions>
        <div class="flex flex-wrap gap-2">
          <button @click="downloadTemplate" class="bg-gray-600 text-white px-3 py-2 rounded-lg hover:bg-gray-700 text-sm font-medium">Şablon İndir</button>
          <button @click="exportStocks" class="bg-blue-600 text-white px-3 py-2 rounded-lg hover:bg-blue-700 text-sm font-medium">Excel İndir</button>
          <button @click="openImportModal" class="bg-green-600 text-white px-3 py-2 rounded-lg hover:bg-green-700 text-sm font-medium">Excel ile Yükle</button>
          <button @click="openModal()" class="bg-blue-600 text-white px-3 py-2 rounded-lg hover:bg-blue-700 text-sm font-medium">+ Yeni Stok</button>
        </div>
      </template>
    </PageHeader>

    <!-- Search + Filter Panel -->
    <div class="bg-white dark:bg-gray-900 p-4 rounded shadow space-y-3">
      <div class="flex flex-wrap items-center gap-2">
        <input
          v-model="searchQuery"
          @input="debouncedSearch"
          type="text"
          placeholder="Stok Kodu veya Adı Ara..."
          class="flex-1 min-w-[200px] border rounded px-3 py-2 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
        />
        <button
          @click="filterOpen = !filterOpen"
          class="flex items-center gap-1 px-3 py-2 border rounded text-sm font-medium transition"
          :class="activeFilterCount > 0 ? 'border-blue-400 text-blue-600 bg-blue-50 dark:bg-blue-900/30' : 'border-gray-300 text-gray-600 dark:text-gray-400 dark:border-gray-700'"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 4a1 1 0 011-1h16a1 1 0 011 1v2a1 1 0 01-.293.707L13 13.414V19a1 1 0 01-.553.894l-4 2A1 1 0 017 21v-7.586L3.293 6.707A1 1 0 013 6V4z"/></svg>
          Filtrele
          <span v-if="activeFilterCount > 0" class="ml-1 bg-blue-600 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">{{ activeFilterCount }}</span>
        </button>
        <button v-if="activeFilterCount > 0" @click="clearFilters" class="text-xs text-red-500 hover:text-red-700 px-2 py-1 border border-red-300 rounded">Temizle</button>
      </div>
      <div v-if="filterOpen" class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 pt-1 border-t dark:border-gray-700">
        <div>
          <label class="block text-xs text-gray-500 dark:text-gray-400 mb-1">Kategori</label>
          <select v-model="filterCategory" @change="debouncedSearch" class="w-full border rounded px-2 py-1.5 text-sm dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
            <option :value="null">Tümü</option>
            <option :value="0">Tanımsız</option>
            <option :value="1">Gıda</option>
            <option :value="2">Sarf</option>
            <option :value="3">Kıyafet</option>
            <option :value="4">Temizlik</option>
            <option :value="5">Kırtasiye</option>
            <option :value="99">Diğer</option>
          </select>
        </div>
        <div>
          <label class="block text-xs text-gray-500 dark:text-gray-400 mb-1">Toplama Tipi</label>
          <select v-model="filterPickingType" @change="debouncedSearch" class="w-full border rounded px-2 py-1.5 text-sm dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
            <option :value="null">Tümü</option>
            <option :value="1">Micro</option>
            <option :value="2">Macro</option>
          </select>
        </div>
        <div>
          <label class="block text-xs text-gray-500 dark:text-gray-400 mb-1">Birim</label>
          <select v-model="filterUnit" @change="debouncedSearch" class="w-full border rounded px-2 py-1.5 text-sm dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
            <option :value="null">Tümü</option>
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
          <label class="block text-xs text-gray-500 dark:text-gray-400 mb-1">Durum</label>
          <div class="flex rounded border dark:border-gray-700 overflow-hidden text-sm">
            <button
              @click="filterIsActive = null; debouncedSearch()"
              class="flex-1 px-2 py-1.5 transition"
              :class="filterIsActive === null ? 'bg-blue-600 text-white' : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'"
            >Tümü</button>
            <button
              @click="filterIsActive = true; debouncedSearch()"
              class="flex-1 px-2 py-1.5 border-l dark:border-gray-700 transition"
              :class="filterIsActive === true ? 'bg-green-600 text-white' : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'"
            >Aktif</button>
            <button
              @click="filterIsActive = false; debouncedSearch()"
              class="flex-1 px-2 py-1.5 border-l dark:border-gray-700 transition"
              :class="filterIsActive === false ? 'bg-red-500 text-white' : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'"
            >Pasif</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
      <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
                <tr>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer select-none hover:text-gray-700 dark:hover:text-gray-200" @click="toggleSort('stockCode')">
                      Kod <span class="ml-1">{{ sortIndicator('stockCode') }}</span>
                    </th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer select-none hover:text-gray-700 dark:hover:text-gray-200" @click="toggleSort('stockName')">
                      Ad <span class="ml-1">{{ sortIndicator('stockName') }}</span>
                    </th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell cursor-pointer select-none hover:text-gray-700 dark:hover:text-gray-200" @click="toggleSort('unitId')">
                      Birim <span class="ml-1">{{ sortIndicator('unitId') }}</span>
                    </th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell cursor-pointer select-none hover:text-gray-700 dark:hover:text-gray-200" @click="toggleSort('unitPrice')">
                      Birim Fiyat <span class="ml-1">{{ sortIndicator('unitPrice') }}</span>
                    </th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">KDV %</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">Toplama Tipi</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">Kategori</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">Marka</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">Lokasyon</th>
                    <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">Netsis Kodu</th>
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
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 hidden lg:table-cell">{{ stock.unit || '-' }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 hidden lg:table-cell">{{ stock.unitPrice ? stock.unitPrice.toFixed(2) : '-' }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 hidden lg:table-cell">
                        {{ stock.taxRate !== undefined ? `%${stock.taxRate}` : '-' }}
                    </td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm hidden lg:table-cell">
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
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 hidden lg:table-cell">{{ stock.category || '-' }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 hidden lg:table-cell">{{ stock.brand || '-' }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 font-mono text-xs hidden lg:table-cell">{{ stock.warehouseLocation || '-' }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 font-mono text-xs hidden lg:table-cell">{{ stock.netsisStockCode || '-' }}</td>
                    <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium flex justify-end gap-2">
                        <button v-role="['Admin','Manager','Warehouse']" @click="openAdjustModal(stock)" class="text-amber-600 hover:text-amber-900">Sayım</button>
                        <button @click="openThresholdModal(stock)" class="text-emerald-600 hover:text-emerald-900">Eşik</button>
                        <button @click="openModal(stock)" class="text-blue-600 hover:text-blue-900">Düzenle</button>
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
        <div class="bg-white dark:bg-gray-900 rounded-lg p-6 w-full max-w-md max-h-[90vh] overflow-y-auto">
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
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Kategori <span class="text-red-500">*</span></label>
                    <select v-model.number="form.category" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" :class="form.category === 0 ? 'border-red-400' : ''">
                        <option :value="0" disabled>Seçiniz...</option>
                        <option :value="1">Gıda</option>
                        <option :value="2">Sarf</option>
                        <option :value="3">Kıyafet</option>
                        <option :value="4">Temizlik</option>
                        <option :value="5">Kırtasiye</option>
                        <option :value="99">Diğer</option>
                    </select>
                    <p v-if="form.category === 0" class="text-xs text-red-500 mt-1">Kategori seçilmek zorundadır.</p>
                  </div>

                  <!-- Clothing Type (only for Kıyafet) -->
                  <div v-if="form.category === 3">
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Kıyafet Türü <span class="text-gray-400 font-normal">(toplama gruplaması)</span></label>
                    <select v-model.number="form.clothingType" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
                        <option :value="0">Diğer</option>
                        <option :value="1">Ayakkabı</option>
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

                  <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
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
                    <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
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
                      <div>
                        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Birim Ağırlık (kg)</label>
                        <input v-model.number="form.weightKg" type="number" min="0" step="0.001" inputmode="decimal" placeholder="Ör: 1.5" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
                        <p class="text-xs text-gray-400 mt-0.5">Tonaj hesabı için kullanılır. Boş bırakılabilir.</p>
                      </div>
                      <div>
                        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Gıda Toplama Sırası</label>
                        <input v-model.number="form.pickingOrder" type="number" min="0" step="1" inputmode="numeric" placeholder="Ör: 5" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
                        <p class="text-xs text-gray-400 mt-0.5">Gıda toplama modalında sıralama için kullanılır. Düşük değer önce gelir.</p>
                      </div>
                      <div class="col-span-2">
                        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Depo Lokasyonu</label>
                        <input v-model="form.warehouseLocation" type="text" placeholder="Ör: A-Raf-3" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
                      </div>
                    </div>
                  </div>

                  <!-- Barkod -->
                  <div class="border-t dark:border-gray-700 pt-4 mt-2">
                    <p class="text-xs text-gray-400 dark:text-gray-600 uppercase font-semibold mb-3">Barkod</p>
                    <div>
                      <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Birincil Barkod</label>
                      <input v-model="form.barcode" type="text" placeholder="EAN13 / Code128 / QR değeri"
                        class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 font-mono" />
                      <p class="text-xs text-gray-400 mt-1">Tedarikçi barkodunu buraya girin. Birden fazla barkod için stok kaydı oluşturulduktan sonra barkod yönetimini kullanın.</p>
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
                <button @click="saveStock" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">Kaydet</button>
            </div>
        </div>
    </div>

    <!-- Threshold Modal -->
    <div v-if="showThresholdModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
        <div class="bg-white dark:bg-gray-900 rounded-lg p-6 w-full max-w-sm max-h-[90vh] overflow-y-auto">
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

    <!-- Sayım / Giriş Modal -->
    <div v-if="showAdjustModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
        <div class="bg-white dark:bg-gray-900 rounded-lg p-6 w-full max-w-sm max-h-[90vh] overflow-y-auto">
            <h3 class="text-lg font-bold mb-1">Stok Sayım / Giriş</h3>
            <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">{{ adjustForm.stockName }}</p>
            <div class="space-y-4">
                <div class="text-sm bg-gray-50 dark:bg-gray-800 rounded p-3 border dark:border-gray-700 text-gray-700 dark:text-gray-300">
                    Mevcut stok: <span class="font-bold">{{ adjustForm.currentOnHand }}</span>
                </div>
                <div class="flex gap-2">
                    <button
                        @click="adjustForm.mode = 0"
                        class="flex-1 py-2 rounded text-sm font-medium border transition"
                        :class="adjustForm.mode === 0 ? 'bg-amber-600 text-white border-amber-600' : 'text-gray-600 dark:text-gray-300 border-gray-300 dark:border-gray-700'"
                    >Sayım (yeni mevcut)</button>
                    <button
                        @click="adjustForm.mode = 1"
                        class="flex-1 py-2 rounded text-sm font-medium border transition"
                        :class="adjustForm.mode === 1 ? 'bg-amber-600 text-white border-amber-600' : 'text-gray-600 dark:text-gray-300 border-gray-300 dark:border-gray-700'"
                    >Giriş (ekle)</button>
                </div>
                <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">
                        {{ adjustForm.mode === 0 ? 'Sayılan Miktar (yeni mevcut)' : 'Eklenecek Miktar' }}
                    </label>
                    <input v-model.number="adjustForm.quantity" type="number" min="0" step="any" inputmode="decimal" class="w-full border p-2 rounded dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
                    <p v-if="adjustForm.mode === 0 && adjustForm.quantity != null" class="text-xs mt-1" :class="adjustDiff === 0 ? 'text-gray-400' : adjustDiff > 0 ? 'text-green-600' : 'text-red-600'">
                        Fark: {{ adjustDiff > 0 ? '+' : '' }}{{ adjustDiff }} → yeni mevcut {{ adjustForm.quantity }}
                    </p>
                    <p v-else-if="adjustForm.mode === 1 && adjustForm.quantity != null" class="text-xs mt-1 text-green-600">
                        Yeni mevcut: {{ adjustForm.currentOnHand + (adjustForm.quantity || 0) }}
                    </p>
                </div>
                <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Not <span class="text-gray-400 font-normal">(isteğe bağlı)</span></label>
                    <textarea v-model="adjustForm.note" rows="2" maxlength="500" class="w-full border p-2 rounded resize-none dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" placeholder="Sayım/giriş açıklaması..."></textarea>
                </div>
            </div>
            <div class="mt-6 flex justify-end gap-3">
                <button @click="showAdjustModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
                <button @click="saveAdjust" class="px-4 py-2 bg-amber-600 text-white rounded hover:bg-amber-700" :disabled="savingAdjust || adjustForm.quantity == null">
                    {{ savingAdjust ? 'Kaydediliyor...' : 'Kaydet' }}
                </button>
            </div>
        </div>
    </div>

    <!-- Import Modal -->
    <div v-if="showImportModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
        <div class="bg-white dark:bg-gray-900 rounded-lg p-6 w-full max-w-md max-h-[90vh] overflow-y-auto">
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
import { ref, computed, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
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

// Filter state
const filterOpen = ref(false);
const filterCategory = ref<number | null>(null);
const filterPickingType = ref<number | null>(null);
const filterUnit = ref<number | null>(null);
const filterIsActive = ref<boolean | null>(null);

const activeFilterCount = computed(() => {
    let n = 0;
    if (filterCategory.value !== null) n++;
    if (filterPickingType.value !== null) n++;
    if (filterUnit.value !== null) n++;
    if (filterIsActive.value !== null) n++;
    return n;
});

const clearFilters = () => {
    filterCategory.value = null;
    filterPickingType.value = null;
    filterUnit.value = null;
    filterIsActive.value = null;
    fetchStocks(1);
};

// Sort state (client-side)
const sortBy = ref<string>('stockCode');
const sortDir = ref<'asc' | 'desc'>('asc');

const toggleSort = (col: string) => {
    if (sortBy.value === col) {
        sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc';
    } else {
        sortBy.value = col;
        sortDir.value = 'asc';
    }
    sortStocks();
};

const sortIndicator = (col: string) => {
    if (sortBy.value !== col) return '↕';
    return sortDir.value === 'asc' ? '↑' : '↓';
};

const sortStocks = () => {
    const dir = sortDir.value === 'asc' ? 1 : -1;
    stocks.value = [...stocks.value].sort((a: any, b: any) => {
        const av = a[sortBy.value] ?? '';
        const bv = b[sortBy.value] ?? '';
        if (typeof av === 'number' && typeof bv === 'number') return (av - bv) * dir;
        return String(av).localeCompare(String(bv), 'tr') * dir;
    });
};

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
            size: pageSize.value,
            categoryId: filterCategory.value,
            pickingTypeId: filterPickingType.value,
            unitId: filterUnit.value,
            isActive: filterIsActive.value,
        });

        stocks.value = data.items;
        currentPage.value = data.pageIndex;
        totalPages.value = data.totalPages;
        totalCount.value = data.totalCount;
        sortStocks();

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
const form = ref<any>({ id: 0, stockCode: '', stockName: '', unit: 0, unitPrice: null, taxRate: 20, pickingType: 0, category: null, brand: '', minStockQty: null, reorderPoint: null, warehouseLocation: '', netsisStockCode: '', weightKg: null, pickingOrder: 0, barcode: '', clothingType: 0 });

const openModal = (stock?: Stock) => {
    if (stock) {
        isEditing.value = true;
        form.value = {
            ...stock,
            pickingType: stock.pickingTypeId || 0,
            category: stock.categoryId || 0,
            unit: stock.unitId || 0,
            reorderPoint: stock.reorderPoint ?? null,
            pickingOrder: stock.pickingOrder ?? 0,
            clothingType: stock.clothingTypeId ?? 0,
        };
    } else {
        isEditing.value = false;
        form.value = { id: 0, stockCode: '', stockName: '', unit: 0, unitPrice: null, taxRate: 20, pickingType: 0, category: null, brand: '', minStockQty: null, reorderPoint: null, warehouseLocation: '', netsisStockCode: '', weightKg: null, pickingOrder: 0, barcode: '' };
    }
    showModal.value = true;
};

const saveStock = async () => {
    try {
        if (!form.value.category || form.value.category === 0) {
            notify.warning('Lütfen Kategori seçiniz.');
            return;
        }

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

// Sayım / Giriş Logic
const showAdjustModal = ref(false);
const savingAdjust = ref(false);
const adjustForm = ref<{ id: number; stockName: string; currentOnHand: number; mode: 0 | 1; quantity: number | null; note: string }>({
    id: 0,
    stockName: '',
    currentOnHand: 0,
    mode: 0,
    quantity: null,
    note: '',
});

const adjustDiff = computed(() =>
    adjustForm.value.mode === 0 && adjustForm.value.quantity != null
        ? adjustForm.value.quantity - adjustForm.value.currentOnHand
        : 0
);

const openAdjustModal = (stock: Stock) => {
    adjustForm.value = {
        id: stock.id,
        stockName: stock.stockName,
        currentOnHand: stock.onHandQty ?? 0,
        mode: 0,
        quantity: null,
        note: '',
    };
    showAdjustModal.value = true;
};

const saveAdjust = async () => {
    if (adjustForm.value.quantity == null) {
        notify.error('Lütfen miktar giriniz.');
        return;
    }
    savingAdjust.value = true;
    try {
        const newQty = await stockService.adjustOnHand(adjustForm.value.id, {
            quantity: adjustForm.value.quantity,
            mode: adjustForm.value.mode,
            note: adjustForm.value.note || null,
        });
        notify.success(`Stok güncellendi. Yeni mevcut: ${newQty}`);
        showAdjustModal.value = false;
        fetchStocks(currentPage.value);
    } catch (e) {
        notify.error(ApiErrorUtils.getErrorMessage(e, 'Stok güncellenemedi.'));
    } finally {
        savingAdjust.value = false;
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
