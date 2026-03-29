<template>
  <div class="p-6 space-y-6">
    <div>
      <h1 class="text-2xl font-bold">Raporlar</h1>
      <p class="text-gray-600 dark:text-gray-400">Operasyonel özet raporlar.</p>
    </div>

    <!-- Tabs -->
    <div class="border-b border-gray-200 dark:border-gray-700 overflow-x-auto">
      <nav class="-mb-px flex gap-1 min-w-max">
        <button
          v-for="tab in tabs"
          :key="tab.key"
          @click="activeTab = tab.key"
          class="py-2 px-3 text-sm font-medium border-b-2 transition-colors whitespace-nowrap"
          :class="activeTab === tab.key
            ? 'border-indigo-600 text-indigo-600'
            : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'"
        >
          {{ tab.label }}
        </button>
      </nav>
    </div>

    <!-- ── Tab: Sevkiyat Özeti ── -->
    <div v-if="activeTab === 'shipments'" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 p-4 rounded shadow flex flex-wrap gap-4 items-end">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Başlangıç</label>
          <input v-model="shipFilter.startDate" type="date" class="border dark:border-gray-700 p-2 rounded dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Bitiş</label>
          <input v-model="shipFilter.endDate" type="date" class="border dark:border-gray-700 p-2 rounded dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <button @click="loadShipmentSummary" class="bg-indigo-600 text-white px-4 py-2 rounded hover:bg-indigo-700">Filtrele</button>
        <button v-if="shipSummary" @click="exportShipments" class="ml-auto flex items-center gap-1.5 px-4 py-2 text-sm border border-green-600 text-green-700 dark:text-green-400 dark:border-green-600 rounded hover:bg-green-50 dark:hover:bg-green-900/20 transition-colors">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" /></svg>
          Excel İndir
        </button>
      </div>

      <div v-if="shipSummary" class="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-8 gap-3">
        <div class="bg-white dark:bg-gray-900 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-gray-900 dark:text-gray-100">{{ shipSummary.total }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam</p></div>
        <div class="bg-gray-50 dark:bg-gray-800 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-gray-600 dark:text-gray-400">{{ shipSummary.created }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Taslak</p></div>
        <div class="bg-yellow-50 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-yellow-700">{{ shipSummary.assignedToWarehouse }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Depoda</p></div>
        <div class="bg-blue-50 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-blue-700">{{ shipSummary.picking }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplanıyor</p></div>
        <div class="bg-purple-50 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-purple-700">{{ shipSummary.readyForDispatch }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Hazır</p></div>
        <div class="bg-indigo-50 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-indigo-700">{{ shipSummary.assignedToVehicle }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Araçta</p></div>
        <div class="bg-green-50 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-green-700">{{ shipSummary.delivered }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Teslim Edildi</p></div>
        <div class="bg-red-50 rounded shadow p-3 text-center"><p class="text-2xl font-bold text-red-700">{{ shipSummary.cancelled }}</p><p class="text-xs text-gray-500 dark:text-gray-400 mt-1">İptal</p></div>
      </div>

      <div v-if="shipSummary" class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
        <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">#</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Proje</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden sm:table-cell">Bölge</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Durum</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden sm:table-cell">Teslim Tarihi</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden lg:table-cell">Talep No</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden lg:table-cell">Kalem</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
            <tr v-if="shipSummary.rows.length === 0"><td colspan="7" class="px-4 py-6 text-center text-gray-400">Bu tarih aralığında sevkiyat bulunamadı.</td></tr>
            <tr v-for="row in shipSummary.rows" :key="row.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
              <td class="px-4 py-3 font-mono text-xs text-gray-500 dark:text-gray-400">{{ row.id }}</td>
              <td class="px-4 py-3 font-medium text-gray-900 dark:text-gray-100">{{ row.projectName }}</td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400 hidden sm:table-cell">{{ row.zoneName || '-' }}</td>
              <td class="px-4 py-3"><span class="px-2 py-0.5 text-xs rounded-full font-semibold" :class="statusClass(row.status)">{{ row.status }}</span></td>
              <td class="px-4 py-3 text-gray-600 dark:text-gray-400 hidden sm:table-cell">{{ fmtDate(row.deliveryDate) }}</td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400 text-xs hidden lg:table-cell">{{ row.talepNo || '-' }}</td>
              <td class="px-4 py-3 text-right text-gray-600 dark:text-gray-400 hidden lg:table-cell">{{ row.lineCount }}</td>
            </tr>
          </tbody>
        </table>
        </div>
      </div>
      <div v-if="!shipSummary && !loadingShip" class="text-center py-10 text-gray-400">Tarih aralığı seçip "Filtrele" butonuna tıklayın.</div>
      <div v-if="loadingShip" class="text-center py-10 text-gray-400">Yükleniyor...</div>
    </div>

    <!-- ── Tab: Performans ── -->
    <div v-if="activeTab === 'performance'" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 p-4 rounded shadow flex flex-wrap gap-4 items-end">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Başlangıç</label>
          <input v-model="perfFilter.startDate" type="date" class="border dark:border-gray-700 p-2 rounded dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Bitiş</label>
          <input v-model="perfFilter.endDate" type="date" class="border dark:border-gray-700 p-2 rounded dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <button @click="loadPerformance" class="bg-indigo-600 text-white px-4 py-2 rounded hover:bg-indigo-700">Filtrele</button>
        <button v-if="perfData" @click="exportPerformance" class="ml-auto flex items-center gap-1.5 px-4 py-2 text-sm border border-green-600 text-green-700 dark:text-green-400 dark:border-green-600 rounded hover:bg-green-50 dark:hover:bg-green-900/20 transition-colors">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" /></svg>
          Excel İndir
        </button>
      </div>

      <template v-if="perfData">
        <!-- KPI cards -->
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div class="bg-white dark:bg-gray-900 rounded shadow p-4 text-center">
            <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ perfData.totalDelivered }}</p>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam Teslim</p>
          </div>
          <div class="bg-green-50 dark:bg-green-900/20 rounded shadow p-4 text-center">
            <p class="text-3xl font-bold text-green-700 dark:text-green-400">{{ perfData.onTime }}</p>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Zamanında</p>
          </div>
          <div class="bg-red-50 dark:bg-red-900/20 rounded shadow p-4 text-center">
            <p class="text-3xl font-bold text-red-700 dark:text-red-400">{{ perfData.late }}</p>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Gecikmiş</p>
          </div>
          <div class="rounded shadow p-4 text-center" :class="perfData.onTimeRate >= 80 ? 'bg-green-50 dark:bg-green-900/20' : 'bg-red-50 dark:bg-red-900/20'">
            <p class="text-3xl font-bold" :class="perfData.onTimeRate >= 80 ? 'text-green-700 dark:text-green-400' : 'text-red-700 dark:text-red-400'">
              %{{ perfData.onTimeRate }}
            </p>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Zamanında Teslimat Oranı</p>
          </div>
        </div>

        <!-- By zone -->
        <div v-if="perfData.byZone.length > 0" class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
          <div class="px-4 py-3 border-b dark:border-gray-700">
            <h3 class="font-medium text-gray-900 dark:text-gray-100">Bölge Bazında Performans</h3>
          </div>
          <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Bölge</th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Toplam</th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Zamanında</th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Gecikmiş</th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Oran</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr v-for="z in perfData.byZone" :key="z.zoneName" class="hover:bg-gray-50 dark:hover:bg-gray-800">
                <td class="px-4 py-3 font-medium text-gray-900 dark:text-gray-100">{{ z.zoneName }}</td>
                <td class="px-4 py-3 text-right text-gray-600 dark:text-gray-400">{{ z.total }}</td>
                <td class="px-4 py-3 text-right text-green-700 dark:text-green-400 font-medium">{{ z.onTime }}</td>
                <td class="px-4 py-3 text-right text-red-700 dark:text-red-400 font-medium">{{ z.late }}</td>
                <td class="px-4 py-3 text-right font-semibold" :class="z.onTimeRate >= 80 ? 'text-green-700 dark:text-green-400' : 'text-red-700 dark:text-red-400'">
                  %{{ z.onTimeRate }}
                </td>
              </tr>
            </tbody>
          </table>
          </div>
        </div>

        <!-- Detail rows -->
        <div class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
          <div class="px-4 py-3 border-b dark:border-gray-700 flex items-center justify-between">
            <h3 class="font-medium text-gray-900 dark:text-gray-100">Teslim Detayları ({{ perfData.rows.length }})</h3>
            <label class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400 cursor-pointer">
              <input v-model="perfLateOnly" type="checkbox" class="rounded" />
              Sadece gecikmişler
            </label>
          </div>
          <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Proje</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Bölge</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Planlanan</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Teslim Edildi</th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Gecikme</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr v-if="filteredPerfRows.length === 0"><td colspan="5" class="px-4 py-6 text-center text-gray-400">Kayıt bulunamadı.</td></tr>
              <tr v-for="r in filteredPerfRows" :key="r.id" class="hover:bg-gray-50 dark:hover:bg-gray-800" :class="r.isLate ? 'bg-red-50/30 dark:bg-red-900/10' : ''">
                <td class="px-4 py-3 font-medium text-gray-900 dark:text-gray-100">{{ r.projectName }}</td>
                <td class="px-4 py-3 text-gray-500 dark:text-gray-400">{{ r.zoneName || '-' }}</td>
                <td class="px-4 py-3 text-gray-600 dark:text-gray-400">{{ fmtDate(r.deliveryDate) }}</td>
                <td class="px-4 py-3 text-gray-600 dark:text-gray-400">{{ fmtDate(r.deliveredAt) }}</td>
                <td class="px-4 py-3 text-right font-semibold" :class="r.isLate ? 'text-red-600 dark:text-red-400' : 'text-green-600 dark:text-green-400'">
                  {{ r.isLate ? `+${r.delayDays} gün` : 'Zamanında' }}
                </td>
              </tr>
            </tbody>
          </table>
          </div>
        </div>
      </template>
      <div v-if="!perfData && !loadingPerf" class="text-center py-10 text-gray-400">Tarih aralığı seçip "Filtrele" butonuna tıklayın.</div>
      <div v-if="loadingPerf" class="text-center py-10 text-gray-400">Yükleniyor...</div>
    </div>

    <!-- ── Tab: Stok Durumu ── -->
    <div v-if="activeTab === 'stock'" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 p-4 rounded shadow flex items-center gap-4">
        <label class="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 cursor-pointer">
          <input v-model="stockCriticalOnly" type="checkbox" class="rounded" @change="loadStockStatus" />
          Sadece kritik stoklar
        </label>
        <button @click="loadStockStatus" class="bg-indigo-600 text-white px-4 py-2 rounded hover:bg-indigo-700 text-sm">Yenile</button>
        <button v-if="stockData" @click="exportStock" class="ml-auto flex items-center gap-1.5 px-4 py-2 text-sm border border-green-600 text-green-700 dark:text-green-400 dark:border-green-600 rounded hover:bg-green-50 dark:hover:bg-green-900/20 transition-colors">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" /></svg>
          Excel İndir
        </button>
      </div>

      <template v-if="stockData">
        <div class="grid grid-cols-3 gap-4">
          <div class="bg-white dark:bg-gray-900 rounded shadow p-4 text-center">
            <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stockData.totalStocks }}</p>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam Stok</p>
          </div>
          <div class="bg-orange-50 dark:bg-orange-900/20 rounded shadow p-4 text-center">
            <p class="text-3xl font-bold text-orange-700 dark:text-orange-400">{{ stockData.criticalCount }}</p>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Kritik Seviye</p>
          </div>
          <div class="bg-red-50 dark:bg-red-900/20 rounded shadow p-4 text-center">
            <p class="text-3xl font-bold text-red-700 dark:text-red-400">{{ stockData.outOfStockCount }}</p>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Stok Tükendi</p>
          </div>
        </div>

        <div class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
          <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Kod</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Ad</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Lokasyon</th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Elde</th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Rezerve</th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Kullanılabilir</th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Min</th>
                <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Durum</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr v-if="stockData.rows.length === 0"><td colspan="8" class="px-4 py-6 text-center text-gray-400">Kayıt bulunamadı.</td></tr>
              <tr v-for="s in stockData.rows" :key="s.id" class="hover:bg-gray-50 dark:hover:bg-gray-800"
                  :class="s.isOutOfStock ? 'bg-red-50/40 dark:bg-red-900/10' : s.isCritical ? 'bg-orange-50/40 dark:bg-orange-900/10' : ''">
                <td class="px-4 py-3 font-mono text-xs font-medium text-gray-900 dark:text-gray-100">{{ s.stockCode }}</td>
                <td class="px-4 py-3 text-gray-900 dark:text-gray-100">{{ s.stockName }}</td>
                <td class="px-4 py-3 text-gray-500 dark:text-gray-400 text-xs font-mono">{{ s.warehouseLocation || '-' }}</td>
                <td class="px-4 py-3 text-right text-gray-700 dark:text-gray-300">{{ s.onHandQty }}</td>
                <td class="px-4 py-3 text-right text-yellow-700 dark:text-yellow-400">{{ s.reservedQty }}</td>
                <td class="px-4 py-3 text-right font-semibold" :class="s.isOutOfStock ? 'text-red-600 dark:text-red-400' : s.isCritical ? 'text-orange-600 dark:text-orange-400' : 'text-green-700 dark:text-green-400'">
                  {{ s.availableQty }}
                </td>
                <td class="px-4 py-3 text-right text-gray-500 dark:text-gray-400">{{ s.minStockQty ?? '-' }}</td>
                <td class="px-4 py-3 text-center">
                  <span v-if="s.isOutOfStock" class="px-2 py-0.5 text-xs rounded-full bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300 font-medium">Tükendi</span>
                  <span v-else-if="s.isCritical" class="px-2 py-0.5 text-xs rounded-full bg-orange-100 text-orange-800 dark:bg-orange-900/40 dark:text-orange-300 font-medium">Kritik</span>
                  <span v-else class="px-2 py-0.5 text-xs rounded-full bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300">Normal</span>
                </td>
              </tr>
            </tbody>
          </table>
          </div>
        </div>
      </template>
      <div v-if="!stockData && !loadingStock" class="text-center py-10 text-gray-400">Yükleniyor...</div>
      <div v-if="loadingStock" class="text-center py-10 text-gray-400">Yükleniyor...</div>
    </div>

    <!-- ── Tab: İadeler ── -->
    <div v-if="activeTab === 'returns'" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 p-4 rounded shadow flex flex-wrap gap-4 items-end">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Başlangıç</label>
          <input v-model="retFilter.startDate" type="date" class="border dark:border-gray-700 p-2 rounded dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Bitiş</label>
          <input v-model="retFilter.endDate" type="date" class="border dark:border-gray-700 p-2 rounded dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <button @click="loadReturns" class="bg-indigo-600 text-white px-4 py-2 rounded hover:bg-indigo-700">Filtrele</button>
        <button v-if="retData" @click="exportReturns" class="ml-auto flex items-center gap-1.5 px-4 py-2 text-sm border border-green-600 text-green-700 dark:text-green-400 dark:border-green-600 rounded hover:bg-green-50 dark:hover:bg-green-900/20 transition-colors">
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" /></svg>
          Excel İndir
        </button>
      </div>

      <template v-if="retData">
        <div class="grid grid-cols-2 gap-4">
          <div class="bg-white dark:bg-gray-900 rounded shadow p-4 text-center">
            <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ retData.totalReturnedLines }}</p>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam İade Kalemi</p>
          </div>
          <div class="bg-orange-50 dark:bg-orange-900/20 rounded shadow p-4 text-center">
            <p class="text-3xl font-bold text-orange-700 dark:text-orange-400">{{ retData.totalReturnedQty }}</p>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplam İade Miktarı</p>
          </div>
        </div>

        <!-- By reason -->
        <div v-if="retData.byReason.length > 0" class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
          <div class="px-4 py-3 border-b dark:border-gray-700">
            <h3 class="font-medium text-gray-900 dark:text-gray-100">Neden Bazında Özet</h3>
          </div>
          <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Neden</th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Adet</th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Toplam Miktar</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr v-for="r in retData.byReason" :key="r.reason" class="hover:bg-gray-50 dark:hover:bg-gray-800">
                <td class="px-4 py-3 font-medium text-gray-900 dark:text-gray-100">{{ returnReasonLabel(r.reason) }}</td>
                <td class="px-4 py-3 text-right text-gray-700 dark:text-gray-300">{{ r.count }}</td>
                <td class="px-4 py-3 text-right text-gray-700 dark:text-gray-300">{{ r.totalQty }}</td>
              </tr>
            </tbody>
          </table>
          </div>
        </div>

        <!-- Detail -->
        <div class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
          <div class="px-4 py-3 border-b dark:border-gray-700">
            <h3 class="font-medium text-gray-900 dark:text-gray-100">İade Detayları ({{ retData.rows.length }})</h3>
          </div>
          <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Proje</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Bölge</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Stok Kodu</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Stok Adı</th>
                <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Miktar</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Neden</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Tarih</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr v-if="retData.rows.length === 0"><td colspan="7" class="px-4 py-6 text-center text-gray-400">Bu tarih aralığında iade bulunamadı.</td></tr>
              <tr v-for="(r, i) in retData.rows" :key="i" class="hover:bg-gray-50 dark:hover:bg-gray-800">
                <td class="px-4 py-3 font-medium text-gray-900 dark:text-gray-100">{{ r.projectName }}</td>
                <td class="px-4 py-3 text-gray-500 dark:text-gray-400">{{ r.zoneName || '-' }}</td>
                <td class="px-4 py-3 font-mono text-xs text-gray-700 dark:text-gray-300">{{ r.stockCode }}</td>
                <td class="px-4 py-3 text-gray-700 dark:text-gray-300">{{ r.stockName }}</td>
                <td class="px-4 py-3 text-right font-semibold text-orange-700 dark:text-orange-400">{{ r.returnedQty }}</td>
                <td class="px-4 py-3 text-gray-600 dark:text-gray-400 text-xs">{{ returnReasonLabel(r.returnReason) }}</td>
                <td class="px-4 py-3 text-gray-500 dark:text-gray-400 text-xs">{{ r.returnedAt ? fmtDate(r.returnedAt) : '-' }}</td>
              </tr>
            </tbody>
          </table>
          </div>
        </div>
      </template>
      <div v-if="!retData && !loadingRet" class="text-center py-10 text-gray-400">Tarih aralığı seçip "Filtrele" butonuna tıklayın.</div>
      <div v-if="loadingRet" class="text-center py-10 text-gray-400">Yükleniyor...</div>
    </div>

    <!-- ── Tab: Açık Satın Alma Siparişleri ── -->
    <div v-if="activeTab === 'pos'" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
        <div class="px-4 py-3 border-b dark:border-gray-700 flex justify-between items-center">
          <h3 class="font-medium text-gray-900 dark:text-gray-100">Açık Satın Alma Siparişleri ({{ openPOs.length }})</h3>
          <div class="flex items-center gap-3">
            <button @click="loadOpenPOs" class="text-sm text-indigo-600 hover:underline">Yenile</button>
            <button v-if="openPOs.length > 0" @click="exportPOs" class="flex items-center gap-1.5 px-3 py-1.5 text-sm border border-green-600 text-green-700 dark:text-green-400 dark:border-green-600 rounded hover:bg-green-50 dark:hover:bg-green-900/20 transition-colors">
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" /></svg>
              Excel
            </button>
          </div>
        </div>
        <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Sipariş No</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Tedarikçi</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Sipariş Tarihi</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Beklenen Tarih</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Durum</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Kalem</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
            <tr v-if="loadingPO"><td colspan="6" class="px-4 py-6 text-center text-gray-400">Yükleniyor...</td></tr>
            <tr v-else-if="openPOs.length === 0"><td colspan="6" class="px-4 py-6 text-center text-gray-400">Açık sipariş bulunamadı.</td></tr>
            <tr v-for="po in openPOs" :key="po.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
              <td class="px-4 py-3 font-mono font-medium text-blue-700">{{ po.orderNumber }}</td>
              <td class="px-4 py-3 text-gray-900 dark:text-gray-100">{{ po.supplierName }}</td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400">{{ po.orderDate }}</td>
              <td class="px-4 py-3" :class="isOverdue(po.expectedDeliveryDate) ? 'text-red-700 font-semibold' : 'text-gray-500 dark:text-gray-400'">
                {{ po.expectedDeliveryDate || '-' }}
                <span v-if="isOverdue(po.expectedDeliveryDate)" class="text-xs ml-1">(Gecikmiş)</span>
              </td>
              <td class="px-4 py-3">
                <span class="px-2 py-0.5 text-xs rounded-full font-semibold" :class="po.status === 'PartiallyReceived' ? 'bg-yellow-100 text-yellow-800' : 'bg-blue-100 text-blue-800'">
                  {{ po.status === 'PartiallyReceived' ? 'Kısmi Alındı' : 'Onaylı' }}
                </span>
              </td>
              <td class="px-4 py-3 text-right text-gray-600 dark:text-gray-400">{{ po.lineCount }}</td>
            </tr>
          </tbody>
        </table>
        </div>
      </div>
    </div>

    <!-- ── Tab: Bekleyen Mal Girişleri ── -->
    <div v-if="activeTab === 'gr'" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 shadow rounded overflow-hidden">
        <div class="px-4 py-3 border-b dark:border-gray-700 flex justify-between items-center">
          <h3 class="font-medium text-gray-900 dark:text-gray-100">Bekleyen Mal Girişleri — Taslak ({{ pendingGRs.length }})</h3>
          <div class="flex items-center gap-3">
            <button @click="loadPendingGRs" class="text-sm text-indigo-600 hover:underline">Yenile</button>
            <button v-if="pendingGRs.length > 0" @click="exportGRs" class="flex items-center gap-1.5 px-3 py-1.5 text-sm border border-green-600 text-green-700 dark:text-green-400 dark:border-green-600 rounded hover:bg-green-50 dark:hover:bg-green-900/20 transition-colors">
              <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5M16.5 12L12 16.5m0 0L7.5 12m4.5 4.5V3" /></svg>
              Excel
            </button>
          </div>
        </div>
        <div class="overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">İrsaliye No</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Tedarikçi</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Tarih</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Bağlı Sipariş</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Kalem</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
            <tr v-if="loadingGR"><td colspan="5" class="px-4 py-6 text-center text-gray-400">Yükleniyor...</td></tr>
            <tr v-else-if="pendingGRs.length === 0"><td colspan="5" class="px-4 py-6 text-center text-gray-400">Bekleyen mal girişi yok.</td></tr>
            <tr v-for="gr in pendingGRs" :key="gr.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
              <td class="px-4 py-3 font-mono font-medium text-gray-900 dark:text-gray-100">{{ gr.waybillNo }}</td>
              <td class="px-4 py-3 text-gray-900 dark:text-gray-100">{{ gr.supplierName }}</td>
              <td class="px-4 py-3 text-gray-500 dark:text-gray-400">{{ gr.receiptDate }}</td>
              <td class="px-4 py-3 text-blue-700 font-mono text-xs">{{ gr.linkedOrderNumber || '-' }}</td>
              <td class="px-4 py-3 text-right text-gray-600 dark:text-gray-400">{{ gr.lineCount }}</td>
            </tr>
          </tbody>
        </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import reportService from '../services/reportService';
import type {
  ShipmentSummaryDto,
  OpenPurchaseOrderRow,
  PendingGoodsReceiptRow,
  ShipmentPerformanceDto,
  StockStatusReportDto,
  ReturnsReportDto,
} from '../services/reportService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotification } from '../composables/useNotification';
import { exportToExcel } from '../utils/exportExcel';

const { notify } = useNotification();

const tabs = [
  { key: 'shipments',   label: 'Sevkiyat Özeti' },
  { key: 'performance', label: 'Teslimat Performansı' },
  { key: 'stock',       label: 'Stok Durumu' },
  { key: 'returns',     label: 'İade Analizi' },
  { key: 'pos',         label: 'Açık Satın Alma' },
  { key: 'gr',          label: 'Bekleyen Mal Girişi' },
];
const activeTab = ref('shipments');

const today    = new Date().toISOString().slice(0, 10);
const monthAgo = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10);

// ── Helpers ──────────────────────────────────────────────────────────────────
const fmtDate = (d: string) => new Date(d).toLocaleDateString('tr-TR');

const statusClass = (status: string) => {
  const map: Record<string, string> = {
    Created:             'bg-gray-100 text-gray-800',
    AssignedToWarehouse: 'bg-yellow-100 text-yellow-800',
    Picking:             'bg-blue-100 text-blue-800',
    ReadyForDispatch:    'bg-purple-100 text-purple-800',
    AssignedToVehicle:   'bg-indigo-100 text-indigo-800',
    Delivered:           'bg-green-100 text-green-800',
    Cancelled:           'bg-red-100 text-red-800',
    Passive:             'bg-gray-100 text-gray-500',
  };
  return map[status] || 'bg-gray-100 text-gray-800';
};

const returnReasonLabelMap: Record<string, string> = {
  CustomerRejected:  'Müşteri Reddi',
  Damaged:           'Hasarlı',
  ExcessLoading:     'Fazla Yükleme',
  WrongItem:         'Yanlış Ürün',
  ProjectNotFound:   'Proje Bulunamadı',
  Other:             'Diğer',
};
const returnReasonLabel = (r?: string) =>
  r ? (returnReasonLabelMap[r] ?? r) : 'Belirtilmemiş';

const isOverdue = (dateStr?: string) => {
  if (!dateStr) return false;
  return new Date(dateStr) < new Date();
};

// ── Sevkiyat Özeti ────────────────────────────────────────────────────────────
const shipFilter  = ref({ startDate: monthAgo, endDate: today });
const shipSummary = ref<ShipmentSummaryDto | null>(null);
const loadingShip = ref(false);

const loadShipmentSummary = async () => {
  loadingShip.value = true;
  try {
    shipSummary.value = await reportService.getShipmentSummary(shipFilter.value);
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Rapor yüklenemedi.'));
  } finally {
    loadingShip.value = false;
  }
};

// ── Performans ────────────────────────────────────────────────────────────────
const perfFilter  = ref({ startDate: monthAgo, endDate: today });
const perfData    = ref<ShipmentPerformanceDto | null>(null);
const loadingPerf = ref(false);
const perfLateOnly = ref(false);

const filteredPerfRows = computed(() =>
  perfLateOnly.value ? (perfData.value?.rows.filter(r => r.isLate) ?? []) : (perfData.value?.rows ?? [])
);

const loadPerformance = async () => {
  loadingPerf.value = true;
  try {
    perfData.value = await reportService.getShipmentPerformance(perfFilter.value);
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Rapor yüklenemedi.'));
  } finally {
    loadingPerf.value = false;
  }
};

// ── Stok Durumu ───────────────────────────────────────────────────────────────
const stockData       = ref<StockStatusReportDto | null>(null);
const loadingStock    = ref(false);
const stockCriticalOnly = ref(false);

const loadStockStatus = async () => {
  loadingStock.value = true;
  try {
    stockData.value = await reportService.getStockStatus({ criticalOnly: stockCriticalOnly.value });
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Yüklenemedi.'));
  } finally {
    loadingStock.value = false;
  }
};

// ── İadeler ───────────────────────────────────────────────────────────────────
const retFilter  = ref({ startDate: monthAgo, endDate: today });
const retData    = ref<ReturnsReportDto | null>(null);
const loadingRet = ref(false);

const loadReturns = async () => {
  loadingRet.value = true;
  try {
    retData.value = await reportService.getReturns(retFilter.value);
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Rapor yüklenemedi.'));
  } finally {
    loadingRet.value = false;
  }
};

// ── Satın Alma & Mal Girişi ───────────────────────────────────────────────────
const openPOs    = ref<OpenPurchaseOrderRow[]>([]);
const loadingPO  = ref(false);

const loadOpenPOs = async () => {
  loadingPO.value = true;
  try {
    openPOs.value = await reportService.getOpenPurchaseOrders();
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Yüklenemedi.'));
  } finally {
    loadingPO.value = false;
  }
};

const pendingGRs = ref<PendingGoodsReceiptRow[]>([]);
const loadingGR  = ref(false);

const loadPendingGRs = async () => {
  loadingGR.value = true;
  try {
    pendingGRs.value = await reportService.getPendingGoodsReceipts();
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Yüklenemedi.'));
  } finally {
    loadingGR.value = false;
  }
};

// ── Excel Export ──────────────────────────────────────────────────────────────
const exportShipments = () => {
  if (!shipSummary.value) return;
  const rows = shipSummary.value.rows.map(r => ({
    'ID':            r.id,
    'Proje':         r.projectName,
    'Bölge':         r.zoneName || '',
    'Durum':         r.status,
    'Teslim Tarihi': fmtDate(r.deliveryDate),
    'Talep No':      r.talepNo || '',
    'Kalem Sayısı':  r.lineCount,
  }));
  exportToExcel(rows, 'Sevkiyat Özeti', `sevkiyat-ozeti-${shipFilter.value.startDate}_${shipFilter.value.endDate}`);
};

const exportPerformance = () => {
  if (!perfData.value) return;
  const rows = perfData.value.rows.map(r => ({
    'Proje':             r.projectName,
    'Bölge':             r.zoneName || '',
    'Talep No':          r.talepNo || '',
    'Planlanan Tarih':   fmtDate(r.deliveryDate),
    'Teslim Tarihi':     fmtDate(r.deliveredAt),
    'Gecikme (Gün)':     r.delayDays,
    'Durum':             r.isLate ? 'Gecikmiş' : 'Zamanında',
    'Sürücü':            r.driverName || '',
  }));
  exportToExcel(rows, 'Performans', `teslimat-performansi-${perfFilter.value.startDate}_${perfFilter.value.endDate}`);
};

const exportStock = () => {
  if (!stockData.value) return;
  const rows = stockData.value.rows.map(s => ({
    'Stok Kodu':       s.stockCode,
    'Stok Adı':        s.stockName,
    'Kategori':        s.category || '',
    'Lokasyon':        s.warehouseLocation || '',
    'Elde (Adet)':     s.onHandQty,
    'Rezerve':         s.reservedQty,
    'Kullanılabilir':  s.availableQty,
    'Min Stok':        s.minStockQty ?? '',
    'Durum':           s.isOutOfStock ? 'Tükendi' : s.isCritical ? 'Kritik' : 'Normal',
  }));
  exportToExcel(rows, 'Stok Durumu', 'stok-durumu');
};

const exportReturns = () => {
  if (!retData.value) return;
  const rows = retData.value.rows.map(r => ({
    'Sevkiyat ID':   r.shipmentId,
    'Talep No':      r.talepNo || '',
    'Proje':         r.projectName,
    'Bölge':         r.zoneName || '',
    'İade Tarihi':   r.returnedAt ? fmtDate(r.returnedAt) : '',
    'Stok Kodu':     r.stockCode,
    'Stok Adı':      r.stockName,
    'İade Miktarı':  r.returnedQty,
    'Neden':         returnReasonLabel(r.returnReason),
    'Not':           r.returnNote || '',
  }));
  exportToExcel(rows, 'İade Analizi', `iade-analizi-${retFilter.value.startDate}_${retFilter.value.endDate}`);
};

const exportPOs = () => {
  if (!openPOs.value.length) return;
  const rows = openPOs.value.map(p => ({
    'Sipariş No':         p.orderNumber,
    'Tedarikçi':          p.supplierName,
    'Sipariş Tarihi':     p.orderDate,
    'Beklenen Teslimat':  p.expectedDeliveryDate || '',
    'Durum':              p.status,
    'Kalem Sayısı':       p.lineCount,
  }));
  exportToExcel(rows, 'Açık Siparişler', 'acik-satin-alma');
};

const exportGRs = () => {
  if (!pendingGRs.value.length) return;
  const rows = pendingGRs.value.map(g => ({
    'İrsaliye No':    g.waybillNo,
    'Tarih':          g.receiptDate,
    'Tedarikçi':      g.supplierName,
    'Bağlı Sipariş':  g.linkedOrderNumber || '',
    'Kalem Sayısı':   g.lineCount,
    'Durum':          g.status,
  }));
  exportToExcel(rows, 'Bekleyen Mal Girişi', 'bekleyen-mal-girisi');
};

// ── Init ──────────────────────────────────────────────────────────────────────
onMounted(() => {
  loadShipmentSummary();
  loadStockStatus();
  loadOpenPOs();
  loadPendingGRs();
});
</script>
