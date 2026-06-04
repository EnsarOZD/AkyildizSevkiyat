<template>
  <div v-if="loading" class="p-6 text-center py-10">
    <span class="text-gray-500 dark:text-gray-400">Yükleniyor...</span>
  </div>
  <div v-else-if="!shipment" class="p-6 text-center py-10">
    <span class="text-gray-500 dark:text-gray-400">Sevkiyat bulunamadı.</span>
  </div>
  <div v-else class="max-w-full overflow-x-hidden px-4 sm:px-6 py-4">

    <!-- Back button -->
    <button
      @click="router.back()"
      class="flex items-center gap-1.5 text-sm text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 transition-colors mb-4"
    >
      <ChevronLeftIcon class="w-4 h-4" />
      Sevkiyatlar
    </button>

    <!-- Page heading -->
    <div class="mb-5">
      <div class="flex items-start justify-between gap-4">
        <div class="min-w-0">
          <div class="flex items-center gap-2 flex-wrap">
            <h1 class="text-xl sm:text-2xl font-bold text-gray-900 dark:text-gray-100">Sevkiyat #{{ shipment.id }}</h1>
            <span
              v-if="shipment.operationTypeValue === 1"
              class="inline-flex items-center px-2 py-0.5 rounded text-[10px] font-bold bg-purple-100 text-purple-700 dark:bg-purple-900/40 dark:text-purple-300 border border-purple-200 dark:border-purple-700 uppercase"
            >Kıyafet</span>
          </div>
          <p class="text-xs sm:text-sm text-gray-500 dark:text-gray-400 mt-1 break-words font-medium">{{ shipment.projectName }}</p>
        </div>
        <div class="flex items-center gap-2 flex-shrink-0">
          <!-- Kargo Etiketi Bas — sadece YurtiKargo sevkiyatlarında göster -->
          <button
            v-if="shipment.ykCargoKey"
            @click="printCargoLabel"
            :disabled="cargoLabelPrinting"
            class="flex items-center gap-1.5 text-[11px] font-bold px-3 py-2 rounded-xl border border-blue-300 dark:border-blue-700 text-blue-700 dark:text-blue-300 hover:bg-blue-50 dark:hover:bg-blue-900/30 transition-all shadow-sm active:scale-95 disabled:opacity-50"
          >
            <svg v-if="!cargoLabelPrinting" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z"/>
            </svg>
            <svg v-else class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
            </svg>
            <span class="hidden sm:inline">{{ cargoLabelPrinting ? 'Gönderiliyor...' : 'Kargo Etiketi' }}</span>
            <span class="sm:hidden">Etiket</span>
          </button>

          <!-- A4 Yazıcı — kargo etiketi tarayıcıdan yazdırma -->
          <router-link
            v-if="shipment.ykCargoKey"
            :to="{ name: 'CargoLabel', params: { id: shipment.id } }"
            target="_blank"
            class="flex items-center gap-1.5 text-[11px] font-bold px-3 py-2 rounded-xl border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition-all shadow-sm active:scale-95"
          >
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"/>
            </svg>
            <span class="hidden sm:inline">A4 Yazıcı</span>
            <span class="sm:hidden">A4</span>
          </router-link>

          <router-link
            :to="{ name: 'ShipmentOrderPrint', params: { id: shipment.id } }"
            target="_blank"
            class="flex items-center gap-1.5 text-[11px] font-bold px-3 py-2 rounded-xl border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition-all shadow-sm active:scale-95"
          >
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"/>
            </svg>
            <span class="hidden sm:inline">Sipariş Formu</span>
            <span class="sm:hidden">Yazdır</span>
          </router-link>
        </div>
      </div>
    </div>

    <!-- Uyarı Banneri -->
    <div v-if="actionWarnings.length > 0" class="mb-4 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 rounded-xl p-4">
      <div class="flex items-start gap-3">
        <svg class="w-5 h-5 text-amber-500 mt-0.5 shrink-0" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
        </svg>
        <div class="flex-1 min-w-0">
          <p class="text-sm font-semibold text-amber-800 dark:text-amber-300 mb-1">İşlem tamamlandı — uyarılar mevcut</p>
          <ul class="text-xs text-amber-700 dark:text-amber-400 space-y-0.5">
            <li v-for="(w, i) in actionWarnings" :key="i" class="flex items-start gap-1">
              <span class="shrink-0">•</span><span>{{ w }}</span>
            </li>
          </ul>
        </div>
        <button @click="actionWarnings = []" class="text-amber-400 hover:text-amber-600 shrink-0 ml-2 text-lg font-bold leading-none">&times;</button>
      </div>
    </div>

    <!-- Two-column layout -->
    <div class="flex flex-col lg:flex-row gap-6 items-stretch lg:items-start w-full">

      <!-- LEFT: main content -->
      <div class="flex-1 min-w-0 w-full">

        <!-- Info card -->
        <ShipmentInfoCard :shipment="shipment" :is-querying-yk="isQueryingYk" @query-yk-status="queryYkStatus" />

        <!-- Tab nav -->
        <div class="border-b border-gray-200 dark:border-gray-700 mb-4 overflow-x-auto scrollbar-hide px-2">
          <nav class="-mb-px flex space-x-6 min-w-max pb-0.5">
            <button
              @click="activeDetailTab = 'lines'"
              :class="activeDetailTab === 'lines'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300'"
              class="whitespace-nowrap py-3 px-1 border-b-2 font-medium text-sm transition-colors"
            >
              Ürünler
              <span class="ml-1 text-xs text-gray-400">({{ shipment.lines.length }})</span>
            </button>
            <button
              @click="activeDetailTab = 'delivery'"
              :class="activeDetailTab === 'delivery'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300'"
              class="whitespace-nowrap py-3 px-1 border-b-2 font-medium text-sm transition-colors"
            >
              Sürücü & Teslimat
            </button>
            <button
              @click="activeDetailTab = 'history'"
              :class="activeDetailTab === 'history'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300'"
              class="whitespace-nowrap py-3 px-1 border-b-2 font-medium text-sm transition-colors"
            >
              Tarihçe
              <span class="ml-1 text-xs text-gray-400">({{ shipment.history.length }})</span>
            </button>
            <button
              @click="activeDetailTab = 'stock'"
              :class="activeDetailTab === 'stock'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300'"
              class="whitespace-nowrap py-3 px-1 border-b-2 font-medium text-sm transition-colors"
            >
              Stok Hareketleri
              <span class="ml-1 text-xs text-gray-400">({{ shipment.stockMovements?.length ?? 0 }})</span>
            </button>
          </nav>
        </div>

        <!-- Tab content -->
        <ShipmentLinesTab
          v-if="activeDetailTab === 'lines'"
          :groupedLines="groupedLines"
        />

        <ShipmentDeliveryTab
          v-if="activeDetailTab === 'delivery'"
          :shipment="shipment"
          @openIrsaliye="openIrsaliyeModal"
          @photoClick="src => photoLightboxSrc = src"
        />

        <ShipmentHistoryTab
          v-if="activeDetailTab === 'history'"
          :history="shipment.history"
          :printLogs="shipment.printLogs ?? []"
        />

        <!-- Stok Hareketleri sekmesi -->
        <div v-if="activeDetailTab === 'stock'">
          <div v-if="!shipment.stockMovements?.length" class="text-center py-10 text-gray-400 text-sm">
            Bu sevkiyata ait stok hareketi bulunmuyor.
          </div>
          <div v-else class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
              <thead class="bg-gray-50 dark:bg-gray-800">
                <tr>
                  <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Tarih</th>
                  <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">İşlem</th>
                  <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Stok</th>
                  <th class="px-4 py-2 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase">Miktar</th>
                  <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase hidden md:table-cell">Not</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100 dark:divide-gray-700">
                <tr v-for="(m, i) in shipment.stockMovements" :key="i">
                  <td class="px-4 py-2 text-gray-600 dark:text-gray-400 whitespace-nowrap">{{ formatStockDate(m.date) }}</td>
                  <td class="px-4 py-2 text-gray-700 dark:text-gray-300">{{ m.type }}</td>
                  <td class="px-4 py-2 text-gray-700 dark:text-gray-300">
                    <span class="font-mono text-xs">{{ m.stockCode }}</span>
                    <span class="text-gray-400"> · {{ m.stockName }}</span>
                  </td>
                  <td class="px-4 py-2 text-right font-semibold" :class="m.qty < 0 ? 'text-red-600 dark:text-red-400' : 'text-green-600 dark:text-green-400'">
                    {{ m.qty > 0 ? '+' : '' }}{{ m.qty }}
                  </td>
                  <td class="px-4 py-2 text-gray-500 dark:text-gray-400 hidden md:table-cell">{{ m.note || '—' }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- RIGHT: sticky sidebar -->
      <ShipmentActionsPanel
        :shipment="shipment"
        :clothingExportLoading="clothingExportLoading"
        :irsaliyeFetchLoading="irsaliyeFetchLoading"
        @exportClothing="exportClothingToNetsis"
        @fetchIrsaliye="fetchIrsaliye"
        @openEdit="openEditModal"
        @openZone="openZoneModal"
        @assignWarehouse="assignToWarehouse"
        @openMarkReady="openMarkReadyConfirm"
        @openQuantities="openQuantitiesModal"
        @openAssignVehicle="openAssignVehicleModal"
        @openDelivery="openDeliveryModal"
        @openVehicleReturn="openVehicleReturnModal"
        @openRevert="openRevertModal"
        @openRevertDelivered="openRevertDeliveredModal"
        @openAdminReset="openAdminResetModal"
      />
    </div>

    <!-- Fotoğraf Lightbox -->
    <div
      v-if="photoLightboxSrc"
      class="fixed inset-0 z-50 bg-black/80 flex items-center justify-center p-4"
      @click="photoLightboxSrc = null"
    >
      <img :src="photoLightboxSrc" alt="Teslimat fotoğrafı" class="max-w-full max-h-full rounded-lg object-contain" />
    </div>

    <!-- ── MODALS ── -->

    <!-- Zone Assign Modal -->
    <BaseModal :show="showZoneModal" title="Proje Bölgesi Ata" maxWidth="sm" @close="showZoneModal = false">
      <p class="text-sm text-gray-600 dark:text-gray-400 mb-4">
        Bu proje için henüz bir bölge tanımlanmamış. İşlemlere devam etmek için bir bölge seçiniz.
      </p>
      <div class="mb-3">
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Bölge</label>
        <select v-model="selectedZoneId" class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-gray-100">
          <option :value="null">Seçiniz...</option>
          <option v-for="zone in availableZones" :key="zone.id" :value="zone.id">{{ zone.name }}</option>
        </select>
      </div>

      <!-- Quick new zone form -->
      <div class="border-t border-gray-200 dark:border-gray-700 pt-3">
        <button
          v-if="!showNewZoneForm"
          @click="showNewZoneForm = true"
          class="text-xs text-blue-600 dark:text-blue-400 hover:underline flex items-center gap-1"
        >
          <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
          Yeni bölge ekle
        </button>
        <div v-else class="space-y-2">
          <p class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">Yeni Bölge</p>
          <input
            v-model="newZoneName"
            type="text"
            placeholder="Bölge adı"
            class="w-full border dark:border-gray-700 px-3 py-1.5 text-sm rounded focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-gray-100"
          />
          <label class="flex items-center gap-2 cursor-pointer select-none">
            <input type="checkbox" v-model="newZoneIsOutOfCity" class="w-4 h-4 rounded border-gray-300 text-teal-600 focus:ring-teal-500" />
            <span class="text-sm text-gray-700 dark:text-gray-300">Şehir Dışı Bölge</span>
          </label>
          <div class="flex gap-2">
            <button
              @click="createAndSelectZone"
              :disabled="!newZoneName.trim() || savingNewZone"
              class="flex-1 py-1.5 bg-blue-600 text-white text-sm rounded hover:bg-blue-700 disabled:opacity-50 font-medium"
            >{{ savingNewZone ? 'Kaydediliyor...' : 'Oluştur ve Seç' }}</button>
            <button @click="showNewZoneForm = false; newZoneName = ''" class="px-3 py-1.5 text-sm text-gray-500 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
          </div>
        </div>
      </div>

      <template #footer>
        <button @click="showZoneModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button
          @click="saveZoneAssignment"
          :disabled="!selectedZoneId"
          class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:bg-blue-300 font-bold"
        >Kaydet</button>
      </template>
    </BaseModal>

    <!-- Vehicle Modal -->
    <BaseModal :show="showVehicleModal" title="Araca Ata" maxWidth="md" @close="showVehicleModal = false">
      <div class="space-y-4">
        <div v-if="!shipment?.irsaliyeNo" class="flex items-start gap-2 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 rounded-lg p-3">
          <svg class="w-4 h-4 text-amber-500 mt-0.5 shrink-0" fill="currentColor" viewBox="0 0 20 20">
            <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd" />
          </svg>
          <p class="text-xs text-amber-700 dark:text-amber-300">
            Bu sevkiyat için henüz irsaliye numarası girilmemiş. Devam edebilirsiniz ancak irsaliye numarasını araç çıkışından önce kaydetmeniz önerilir.
          </p>
        </div>
        <div v-if="assignListsLoading" class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400">
          <span class="animate-spin h-4 w-4 border-2 border-blue-500 border-t-transparent rounded-full"></span>
          Yükleniyor...
        </div>
        <div v-else>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Şoför <span class="text-red-500">*</span></label>
            <select v-model="vehicleForm.driverId" class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-gray-100">
              <option :value="null">Seçiniz...</option>
              <option v-for="d in activeDrivers" :key="d.id" :value="d.id">{{ d.fullName }}</option>
            </select>
          </div>
          <div class="mt-3">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Araç <span class="text-red-500">*</span></label>
            <select v-model="vehicleForm.vehicleId" class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-gray-100">
              <option :value="null">Seçiniz...</option>
              <option v-for="v in activeVehicles" :key="v.id" :value="v.id">{{ v.plateNumber }}</option>
            </select>
            <div v-if="vehicleForm.vehicleId" class="mt-1.5">
              <span v-for="v in activeVehicles.filter(v => v.id === vehicleForm.vehicleId)" :key="v.id"
                    :class="['text-xs font-medium px-2 py-0.5 rounded',
                      v.vehicleType === 0 ? 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300' :
                      v.vehicleType === 1 ? 'bg-purple-100 text-purple-700 dark:bg-purple-900/40 dark:text-purple-300' :
                                            'bg-teal-100 text-teal-700 dark:bg-teal-900/40 dark:text-teal-300']">
                {{ v.vehicleTypeName }}
              </span>
            </div>
          </div>
        </div>
      </div>
      <template #footer>
        <button @click="showVehicleModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button @click="confirmAssignVehicle" :disabled="!vehicleForm.driverId || !vehicleForm.vehicleId"
                class="px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 font-bold disabled:opacity-50 disabled:cursor-not-allowed">Kaydet</button>
      </template>
    </BaseModal>

    <!-- Edit Details Modal -->
    <BaseModal :show="showEditModal" title="Siparişi Düzenle (Taslak)" maxWidth="5xl" @close="showEditModal = false">
      <div class="mb-6 bg-blue-50 dark:bg-blue-950/40 p-4 rounded border border-blue-100 dark:border-blue-900">
        <label class="block text-sm font-bold text-blue-800 dark:text-blue-300 mb-1">Teslim Tarihi</label>
        <input v-model="editForm.deliveryDate" type="date" class="border dark:border-gray-700 p-2 rounded w-full md:w-64 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100" />
      </div>
      <div class="bg-white dark:bg-gray-900 rounded border dark:border-gray-700 shadow-sm pb-48 overflow-x-auto">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-3 py-3 text-left text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider w-32">Stok Kodu</th>
              <th class="px-3 py-3 text-left text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Stok Adı</th>
              <th class="px-3 py-3 text-right text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider w-28">Miktar</th>
              <th class="px-3 py-3 text-center text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider w-12">Sil</th>
            </tr>
          </thead>
          <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
            <tr v-for="(line, idx) in editForm.lines" :key="line.id" class="hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
              <td class="px-3 py-2 text-sm">
                <span class="font-mono text-xs text-gray-500 dark:text-gray-400 bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded select-all whitespace-nowrap">
                  {{ line.stockCode || '—' }}
                </span>
              </td>
              <td class="px-3 py-2 text-sm">
                <template v-if="editingStockIdx === idx">
                  <StockCombobox
                    :ref="(el: any) => stockRefs[idx] = el"
                    :initialCode="line.stockCode"
                    :placeholder="line.stockName"
                    :operationType="shipment?.operationTypeValue ?? 0"
                    @select="(item: any) => { onStockSelect(item, line, idx); editingStockIdx = -1; }"
                    class="w-full"
                  />
                  <button @click="editingStockIdx = -1" class="mt-1 text-xs text-gray-400 hover:text-gray-600">İptal</button>
                </template>
                <template v-else>
                  <div class="flex items-start gap-2 min-w-0">
                    <span class="text-gray-800 dark:text-gray-100 text-sm break-words flex-1">{{ line.stockName || '—' }}</span>
                    <button
                      @click="editingStockIdx = idx"
                      class="shrink-0 text-xs text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 border border-blue-300 dark:border-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/30 px-2 py-0.5 rounded transition"
                    >Değiştir</button>
                  </div>
                </template>
              </td>
              <td class="px-3 py-3 text-sm">
                <input
                  :ref="(el: any) => qtyRefs[idx] = el"
                  v-model.number="line.orderedQty"
                  type="number"
                  min="1"
                  class="w-full border-gray-300 dark:border-gray-700 border rounded-md p-2 text-right font-bold text-gray-700 dark:text-gray-300 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-800 dark:text-gray-100"
                  @keydown.enter.prevent="onQtyEnter(idx)"
                  @keydown.delete.stop="line.orderedQty === null || line.orderedQty === 0 ? removeLineAndFocus(idx) : null"
                  @keydown.ctrl.backspace.prevent="removeLineAndFocus(idx)"
                  @focus="($event.target as HTMLInputElement).select()"
                />
              </td>
              <td class="px-3 py-3 text-sm text-center">
                <button @click="removeLine(idx)" class="text-red-400 hover:text-red-600 p-1.5 rounded hover:bg-red-50 dark:hover:bg-red-900/20 transition">
                  <span class="text-lg font-bold leading-none">&times;</span>
                </button>
              </td>
            </tr>
            <tr v-if="editForm.lines.length === 0">
              <td colspan="4" class="px-4 py-8 text-center text-gray-400 italic">Henüz ürün eklenmemiş.</td>
            </tr>
          </tbody>
        </table>
      </div>
      <button @click="addNewLine" class="mt-4 flex items-center gap-2 text-blue-600 hover:text-blue-800 font-semibold px-2 py-1 rounded hover:bg-blue-50 transition">
        <span class="text-xl font-bold">+</span> Yeni Satır Ekle
      </button>
      <template #footer>
        <button @click="showEditModal = false" class="px-6 py-2 text-gray-600 dark:text-gray-400 bg-white dark:bg-gray-900 border dark:border-gray-700 hover:bg-gray-200 dark:hover:bg-gray-700 rounded font-medium transition">İptal</button>
        <button @click="saveDetails" class="px-6 py-2 bg-blue-600 text-white rounded font-bold hover:bg-blue-700 shadow-lg shadow-blue-200 transition">Kaydet</button>
      </template>
    </BaseModal>

    <!-- Quantities Modal -->
    <BaseModal :show="showQuantitiesModal" title="Hazırlanan Miktarları Gir" maxWidth="4xl" @close="showQuantitiesModal = false">
      <div class="overflow-x-auto mb-4">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Stok</th>
              <th class="px-3 py-2 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sipariş</th>
              <th class="px-3 py-2 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Hazırlanan</th>
              <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Fark Nedeni</th>
              <th class="px-3 py-2 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Not</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-200 dark:divide-gray-700 bg-white dark:bg-gray-900">
            <tr v-for="line in quantitiesForm" :key="line.lineId" class="hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
              <td class="px-3 py-2 text-sm">
                <div class="font-medium text-gray-900 dark:text-gray-100 font-mono text-xs">{{ line.stockCode }}</div>
                <div class="text-gray-600 dark:text-gray-400 text-[11px] font-medium">{{ line.stockName }}</div>
              </td>
              <td class="px-3 py-2 text-sm text-right bg-gray-50 dark:bg-gray-800 font-bold text-gray-700 dark:text-gray-300">{{ line.orderedQty }}</td>
              <td class="px-3 py-2 text-sm text-right">
                <input
                  v-model.number="line.deliveredQty"
                  type="number"
                  min="0"
                  class="w-20 border rounded px-2 py-1 text-right font-black focus:ring-2 focus:ring-blue-500 focus:border-blue-500 transition-all dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                  :class="line.deliveredQty !== line.orderedQty ? 'bg-red-50 border-red-300 text-red-700 shadow-sm' : 'border-gray-200 text-gray-900 dark:text-gray-100'"
                />
              </td>
              <td class="px-3 py-2 text-sm">
                <select
                  v-if="line.deliveredQty !== line.orderedQty"
                  v-model="line.differenceReason"
                  class="w-full border border-red-200 rounded px-2 py-1 text-xs font-bold text-red-800 bg-red-50 focus:ring-2 focus:ring-red-500 transition-all animate-pulse"
                >
                  <option value="">Seçiniz...</option>
                  <option value="StockOut">Stok Yok</option>
                  <option value="Damaged">Hasarlı</option>
                  <option value="CustomerRequest">Müşteri İsteği</option>
                  <option value="Other">Diğer</option>
                </select>
                <span v-else class="text-gray-300 text-xs flex justify-center">-</span>
              </td>
              <td class="px-3 py-2 text-sm">
                <input v-model="line.note" type="text" class="w-full border border-gray-200 dark:border-gray-700 rounded px-2 py-1 text-xs focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-gray-100" placeholder="Not..." />
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <template #footer>
        <button @click="showQuantitiesModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700 rounded font-medium transition">İptal</button>
        <button @click="saveQuantities" class="px-6 py-2 bg-blue-600 text-white rounded font-bold hover:bg-blue-700 shadow-lg shadow-blue-200 transition">Değişiklikleri Kaydet</button>
      </template>
    </BaseModal>

    <!-- Delivery Proof Modal -->
    <BaseModal :show="showDeliveryModal" title="Teslim Bilgisi Gir" maxWidth="sm" @close="showDeliveryModal = false">
      <div class="space-y-4">
        <p class="text-sm text-gray-600 dark:text-gray-400 bg-green-50 border border-green-100 rounded p-3">
          Sevkiyat teslim edildi olarak işaretlenecek. İsteğe bağlı teslim bilgilerini girebilirsiniz.
        </p>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Teslim Alan Kişi</label>
          <input v-model="deliveryForm.deliveryRecipient" type="text" placeholder="Ör: Ahmet Yılmaz"
            class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-green-500 focus:border-green-500 dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Not</label>
          <textarea v-model="deliveryForm.deliveryNote" rows="2" placeholder="İsteğe bağlı sürücü notu..."
            class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-green-500 focus:border-green-500 resize-none dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Teslimat Fotoğrafı</label>
          <label class="flex flex-col items-center justify-center gap-2 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg p-4 cursor-pointer hover:border-green-400 transition-colors"
            :class="deliveryForm.photoPreview ? 'border-green-400' : ''">
            <input type="file" accept="image/*" capture="environment" class="hidden" @change="onDeliveryPhotoSelected" />
            <template v-if="deliveryForm.photoPreview">
              <img :src="deliveryForm.photoPreview" class="max-h-40 rounded object-contain" />
              <span class="text-xs text-green-600 font-medium">Fotoğraf seçildi — değiştirmek için tıklayın</span>
            </template>
            <template v-else>
              <svg class="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M15 13a3 3 0 11-6 0 3 3 0 016 0z" />
              </svg>
              <span class="text-xs text-gray-500">Fotoğraf çek veya seç <span class="text-gray-400">(isteğe bağlı)</span></span>
            </template>
          </label>
          <p v-if="deliveryForm.photoCompressing" class="text-xs text-gray-400 mt-1">Fotoğraf hazırlanıyor...</p>
        </div>
      </div>
      <template #footer>
        <button @click="showDeliveryModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button @click="confirmMarkDelivered" :disabled="deliveryForm.photoCompressing"
          class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 font-bold disabled:opacity-50">Teslim Edildi</button>
      </template>
    </BaseModal>

    <!-- İrsaliye Modal -->
    <BaseModal :show="showIrsaliyeModal" title="İrsaliye Bilgisi Gir / Güncelle" maxWidth="sm" @close="showIrsaliyeModal = false">
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">İrsaliye No <span class="text-red-500">*</span></label>
          <input v-model="irsaliyeForm.irsaliyeNo" type="text" maxlength="50" placeholder="Örn: IRŞ-2024-001"
            class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 font-mono dark:bg-gray-800 dark:text-gray-100" />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">İrsaliye Tarihi <span class="text-red-500">*</span></label>
          <input v-model="irsaliyeForm.irsaliyeDate" type="date"
            class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 dark:bg-gray-800 dark:text-gray-100" />
        </div>
      </div>
      <template #footer>
        <button @click="showIrsaliyeModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button @click="saveIrsaliye" :disabled="!irsaliyeForm.irsaliyeNo || !irsaliyeForm.irsaliyeDate"
          class="px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 font-bold disabled:bg-indigo-300">Kaydet</button>
      </template>
    </BaseModal>

    <!-- Vehicle Return Modal -->
    <BaseModal :show="showVehicleReturnModal" title="Araç İadesi Kaydet" maxWidth="lg" @close="showVehicleReturnModal = false">
      <div class="space-y-4">
        <p class="text-sm text-gray-600 dark:text-gray-400 bg-orange-50 border border-orange-100 rounded p-3">
          Araçtan geri dönen kalemlerin miktarını ve iade nedenini girin. Yalnızca iade olan kalemleri doldurun.
        </p>
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-3 py-2 text-left font-medium text-gray-500 dark:text-gray-400">Stok Kodu</th>
                <th class="px-3 py-2 text-left font-medium text-gray-500 dark:text-gray-400">Stok Adı</th>
                <th class="px-3 py-2 text-right font-medium text-gray-500 dark:text-gray-400">Teslim Mik.</th>
                <th class="px-3 py-2 text-right font-medium text-gray-500 dark:text-gray-400">İade Mik.</th>
                <th class="px-3 py-2 text-left font-medium text-gray-500 dark:text-gray-400">İade Nedeni</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <tr v-for="item in vehicleReturnForm.lines" :key="item.shipmentLineId">
                <td class="px-3 py-2 font-mono text-gray-800 dark:text-gray-200">{{ item.stockCode }}</td>
                <td class="px-3 py-2 text-gray-700 dark:text-gray-300">{{ item.stockName }}</td>
                <td class="px-3 py-2 text-right text-gray-600 dark:text-gray-400">{{ item.deliveredQty }}</td>
                <td class="px-3 py-2 text-right">
                  <input v-model.number="item.returnedQty" type="number" min="0" :max="item.deliveredQty" step="0.01"
                    class="w-24 border dark:border-gray-700 rounded px-2 py-1 text-right focus:ring-2 focus:ring-orange-400 dark:bg-gray-800 dark:text-gray-100" />
                </td>
                <td class="px-3 py-2">
                  <select v-model.number="item.returnReason" :disabled="!item.returnedQty"
                    class="border dark:border-gray-700 rounded px-2 py-1 text-sm focus:ring-2 focus:ring-orange-400 disabled:bg-gray-100 dark:bg-gray-800 dark:text-gray-100">
                    <option v-for="r in returnReasons" :key="r.value" :value="r.value">{{ r.label }}</option>
                  </select>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Genel Not</label>
          <textarea v-model="vehicleReturnForm.returnNote" rows="2" placeholder="İsteğe bağlı genel iade notu..."
            class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-orange-400 resize-none text-sm dark:bg-gray-800 dark:text-gray-100" />
        </div>
      </div>
      <template #footer>
        <button @click="showVehicleReturnModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button @click="confirmVehicleReturn" class="px-4 py-2 bg-orange-500 text-white rounded hover:bg-orange-600 font-bold">İadeyi Kaydet</button>
      </template>
    </BaseModal>

    <!-- MarkReady Confirm Modal -->
    <BaseModal :show="showMarkReadyConfirm" title="Sevke Hazır — Uyarılar" maxWidth="sm" @close="showMarkReadyConfirm = false">
      <div class="space-y-3">
        <p class="text-sm text-gray-600 dark:text-gray-400 bg-amber-50 border border-amber-100 rounded p-3">
          Miktar uyumsuzlukları tespit edildi. Yine de devam etmek istiyor musunuz?
        </p>
        <ul class="text-xs text-amber-700 dark:text-amber-400 space-y-1 max-h-48 overflow-y-auto">
          <li v-for="(w, i) in pendingMarkReadyWarnings" :key="i" class="flex items-start gap-1">
            <span class="shrink-0">•</span><span>{{ w }}</span>
          </li>
        </ul>
      </div>
      <template #footer>
        <button @click="showMarkReadyConfirm = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button @click="confirmMarkReady" class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 font-bold">Devam Et</button>
      </template>
    </BaseModal>

    <!-- Admin Reset Modal -->
    <BaseModal :show="showAdminResetModal" title="Sıfırla & Siparişi Serbest Bırak" maxWidth="sm" @close="showAdminResetModal = false">
      <div class="space-y-3">
        <p class="text-sm text-gray-600 dark:text-gray-400 bg-red-50 border border-red-100 rounded p-3">
          Bu sevkiyat <strong>Created</strong> durumuna sıfırlanacak. Netsis transfer verisi, araç/şoför bilgisi ve iade verileri temizlenir. ISS siparişi yeniden aktarılabilir hale gelir.
        </p>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Neden <span class="text-red-500">*</span></label>
          <textarea v-model="adminResetReason" rows="3" placeholder="Sıfırlama nedeni..."
            class="w-full border dark:border-gray-700 p-2 rounded resize-none text-sm focus:ring-2 focus:ring-red-400 dark:bg-gray-800 dark:text-gray-100" />
        </div>
      </div>
      <template #footer>
        <button @click="showAdminResetModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button @click="confirmAdminReset" :disabled="!adminResetReason.trim()"
          class="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 font-bold disabled:opacity-50">Sıfırla</button>
      </template>
    </BaseModal>

    <!-- Revert Delivered Modal -->
    <BaseModal :show="showRevertDeliveredModal" title="Teslimi Geri Al" maxWidth="sm" @close="showRevertDeliveredModal = false">
      <div class="space-y-3">
        <p class="text-sm text-gray-600 dark:text-gray-400 bg-orange-50 dark:bg-orange-900/20 border border-orange-200 dark:border-orange-700 rounded p-3">
          Bu sevkiyat <strong>Sevke Hazır</strong> durumuna geri alınacak. Teslimat kanıtı (fotoğraf, imza, konum) silinir. İşlem tarihçeye kaydedilir.
        </p>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Neden <span class="text-red-500">*</span></label>
          <textarea v-model="revertDeliveredReason" rows="3" placeholder="Geri alma nedeni..."
            class="w-full border dark:border-gray-700 p-2 rounded resize-none text-sm focus:ring-2 focus:ring-orange-400 dark:bg-gray-800 dark:text-gray-100" />
        </div>
      </div>
      <template #footer>
        <button @click="showRevertDeliveredModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button @click="confirmRevertDelivered" :disabled="!revertDeliveredReason.trim()"
          class="px-4 py-2 bg-orange-600 text-white rounded hover:bg-orange-700 font-bold disabled:opacity-50">Geri Al</button>
      </template>
    </BaseModal>

    <!-- Zone-based Driver Assignment Modal (when shipment belongs to a zone preparation) -->
    <DriverAssignmentModal
      v-if="showZoneDriverModal && shipment?.zonePreparationId"
      :zone-preparation-ids="[shipment.zonePreparationId]"
      @close="showZoneDriverModal = false"
      @completed="fetchShipmentDetail"
    />

    <!-- Revert to Draft Modal -->
    <BaseModal :show="showRevertModal" title="Taslağa Geri Çek" maxWidth="sm" @close="showRevertModal = false">
      <div class="space-y-3">
        <p class="text-sm text-gray-600 dark:text-gray-400 bg-red-50 border border-red-100 rounded p-3">
          Bu sevkiyat taslak durumuna geri alınacak. İşlem tarihçeye kaydedilir.
        </p>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Neden (İsteğe Bağlı)</label>
          <textarea v-model="revertReasonText" rows="3" placeholder="Geri çekme nedeni..."
            class="w-full border dark:border-gray-700 p-2 rounded resize-none text-sm focus:ring-2 focus:ring-red-400 dark:bg-gray-800 dark:text-gray-100" />
        </div>
      </div>
      <template #footer>
        <button @click="showRevertModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button @click="confirmRevert" class="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700 font-bold">Geri Çek</button>
      </template>
    </BaseModal>

  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import apiClient from '../services/apiClient';
import { ChevronLeftIcon } from '@heroicons/vue/24/outline';
import shipmentService from '../services/shipmentService';
import warehouseService from '../services/warehouseService';
import projectService from '../services/projectService';
import transportService, { type Driver, type Vehicle } from '../services/transportService';
import StockCombobox from '../components/StockCombobox.vue';
import BaseModal from '../components/BaseModal.vue';
import ShipmentInfoCard from '../components/shipment/ShipmentInfoCard.vue';
import ShipmentLinesTab from '../components/shipment/ShipmentLinesTab.vue';
import ShipmentDeliveryTab from '../components/shipment/ShipmentDeliveryTab.vue';
import ShipmentHistoryTab from '../components/shipment/ShipmentHistoryTab.vue';
import ShipmentActionsPanel from '../components/shipment/ShipmentActionsPanel.vue';
import DriverAssignmentModal from '../components/DriverAssignmentModal.vue';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

interface ShipmentDetail {
  id: number;
  projectId: number;
  projectCode?: string;
  projectName: string;
  zoneId?: number;
  zoneName?: string;
  zonePreparationId?: number;
  status: string;
  deliveryDate: string;
  driverName?: string;
  plateNumber?: string;
  irsaliyeNo?: string;
  irsaliyeDate?: string;
  netsisTransferredAt?: string;
  operationType?: string;
  operationTypeValue?: number;
  deliveredAt?: string;
  deliveryNote?: string;
  deliveryRecipient?: string;
  deliveryPhotoBase64?: string;
  deliveryPhotoPath?: string;
  externalOrderNumber?: string;
  talepNo?: string;
  talepTuru?: string;
  institutionCode?: string;
  teslimAlacakKisiler?: string;
  teslimAlacakTelefon?: string;
  yoneticiMail?: string;
  aciklama?: string;
  projectAddress?: string | null;
  ykCargoKey?: string | null;
  ykBarcode?: string | null;
  ykJobId?: number | null;
  ykOperationStatus?: string | null;
  lines: Array<{
    id: number;
    stockCode: string;
    stockName: string;
    localStockCode: string;
    unit?: string;
    orderedQty: number;
    deliveredQty: number;
    differenceReason?: string;
    note?: string;
    zoneName?: string;
    zoneOrder?: number;
  }>;
  history: Array<{
    oldStatus: string;
    newStatus: string;
    changedAt: string;
    changedBy: string;
    description?: string;
  }>;
  printLogs: Array<{
    id: number;
    printedAt: string;
    printedByName: string;
  }>;
  stockMovements?: Array<{
    date: string;
    type: string;
    qty: number;
    stockCode?: string | null;
    stockName?: string | null;
    note?: string | null;
  }>;
}

const route = useRoute();
const router = useRouter();
const notificationStore = useNotificationStore();
const cargoLabelPrinting = ref(false);

async function printCargoLabel() {
  if (!shipment.value?.ykBarcode && !shipment.value?.ykCargoKey) return;
  cargoLabelPrinting.value = true;
  try {
    const res = await apiClient.post('/print/jobs/cargo', {
      printerConfigId: null,
      payload: {
        barcode:      shipment.value.ykBarcode ?? shipment.value.ykCargoKey,
        receiverName: shipment.value.projectCode
          ? `${shipment.value.projectCode}-${shipment.value.projectName}`
          : shipment.value.projectName,
        address:      shipment.value.projectAddress ?? '',
        phone:        shipment.value.teslimAlacakTelefon ?? '',
        irsaliyeNo:   shipment.value.irsaliyeNo ?? null,
        shipmentId:   shipment.value.id,
        deliveryDate: new Date(shipment.value.deliveryDate).toLocaleDateString('tr-TR'),
      },
    });
    const jobId = res.data?.jobId;
    notificationStore.add('Kargo etiketi kuyruğa alındı, yazdırılıyor…', 'success');

    // Poll for completion — agent typically processes within 3s
    if (jobId) {
      for (let i = 0; i < 5; i++) {
        await new Promise(r => setTimeout(r, 2000));
        try {
          const jobRes = await apiClient.get(`/print/jobs/${jobId}`);
          const job = jobRes.data;
          if (job.status === 2) break; // Done
          if (job.status === 3) {
            // Failed
            notificationStore.add(
              `Yazıcı hatası: ${job.errorMessage ?? 'Bilinmeyen hata'}. Ayarlar → Yazıcı Yönetimi sayfasını kontrol edin.`,
              'error'
            );
            break;
          }
        } catch { break; }
      }
    }
  } catch {
    notificationStore.add('Etiket gönderilemedi. Yazıcı ayarlarını kontrol edin.', 'error');
  } finally {
    cargoLabelPrinting.value = false;
  }
}
const shipment = ref<ShipmentDetail | null>(null);
const loading = ref(false);
const isQueryingYk = ref(false);

async function queryYkStatus() {
  if (!shipment.value?.id || isQueryingYk.value) return;
  isQueryingYk.value = true;
  try {
    const status = await warehouseService.queryYkShipmentStatus(shipment.value.id);
    if (status) {
      shipment.value = await shipmentService.getDetail(shipment.value.id) as unknown as ShipmentDetail;
      notificationStore.add(`Kargo durumu: ${status.statusCode ?? '-'}`, 'success');
    } else {
      notificationStore.add('Durum bilgisi alınamadı.', 'warning');
    }
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Durum sorgulanamadı.', 'error');
  } finally {
    isQueryingYk.value = false;
  }
}

// Action warnings banner
const actionWarnings = ref<string[]>([]);

// Loading states for sidebar buttons
const clothingExportLoading = ref(false);
const irsaliyeFetchLoading = ref(false);

// MarkReady confirm
const showMarkReadyConfirm = ref(false);
const pendingMarkReadyWarnings = ref<string[]>([]);

// Tab state
const activeDetailTab = ref<'lines' | 'delivery' | 'history' | 'stock'>('lines');

function formatStockDate(iso: string) {
  return new Date(iso).toLocaleString('tr-TR', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit',
  });
}

// Photo lightbox
const photoLightboxSrc = ref<string | null>(null);

// Vehicle modal
const showVehicleModal = ref(false);
const showZoneDriverModal = ref(false);
const vehicleForm = ref<{ driverId: number | null; vehicleId: number | null }>({ driverId: null, vehicleId: null });
const activeDrivers = ref<Driver[]>([]);
const activeVehicles = ref<Vehicle[]>([]);
const assignListsLoading = ref(false);

// Zone modal
const showZoneModal = ref(false);
const selectedZoneId = ref<number | null>(null);
const availableZones = ref<any[]>([]);

// Revert modal
const showRevertModal = ref(false);
const revertReasonText = ref('');

// Admin reset modal
const showAdminResetModal = ref(false);
const adminResetReason = ref('');

// Revert delivered modal
const showRevertDeliveredModal = ref(false);
const revertDeliveredReason = ref('');

// Delivery modal
const showDeliveryModal = ref(false);
const deliveryForm = ref({ deliveryRecipient: '', deliveryNote: '', photoBase64: '', photoPreview: '', photoCompressing: false });

// İrsaliye modal
const showIrsaliyeModal = ref(false);
const irsaliyeForm = ref({ irsaliyeNo: '', irsaliyeDate: '' });

// Edit modal
const showEditModal = ref(false);
const editForm = ref<{ deliveryDate: string; lines: EditLine[] }>({ deliveryDate: '', lines: [] });
const qtyRefs = ref<any[]>([]);
const stockRefs = ref<any[]>([]);
const editingStockIdx = ref(-1);

// Quantities modal
const showQuantitiesModal = ref(false);
const quantitiesForm = ref<any[]>([]);

// Vehicle return modal
const showVehicleReturnModal = ref(false);
const vehicleReturnForm = ref<{ lines: VehicleReturnLine[]; returnNote: string }>({ lines: [], returnNote: '' });
const returnReasons = [
  { value: 0, label: 'Müşteri Reddi' },
  { value: 1, label: 'Hasarlı' },
  { value: 2, label: 'Fazla Yükleme' },
  { value: 3, label: 'Yanlış Ürün' },
  { value: 4, label: 'Proje Bulunamadı' },
  { value: 99, label: 'Diğer' },
];

interface EditLine {
  id: string;
  lineId?: number;
  stockCode: string;
  stockName: string;
  orderedQty: number;
  unit: number; // Unit enum value
}

interface VehicleReturnLine {
  shipmentLineId: number;
  stockCode: string;
  stockName: string;
  deliveredQty: number;
  returnedQty: number;
  returnReason: number;
}

// Grouped lines for ShipmentLinesTab
const groupedLines = computed(() => {
  if (!shipment.value?.lines) return [];
  const groups: { zoneName: string; lines: any[] }[] = [];
  shipment.value.lines.forEach(line => {
    const zoneName = line.zoneName || 'Tanımsız';
    const lastGroup = groups[groups.length - 1];
    if (!lastGroup || lastGroup.zoneName !== zoneName) {
      groups.push({ zoneName, lines: [line] });
    } else {
      lastGroup.lines.push(line);
    }
  });
  return groups;
});

const fetchShipmentDetail = async () => {
  loading.value = true;
  const id = Number(route.params.id);
  try {
    shipment.value = await shipmentService.getDetail(id) as unknown as ShipmentDetail;
  } catch (err) {
    console.error(err);
  } finally {
    loading.value = false;
  }
};

// Zone
const fetchZones = async () => {
  try { availableZones.value = await projectService.getZones(); } catch (e) { console.error(e); }
};
const openZoneModal = async () => { await fetchZones(); showZoneModal.value = true; };
const saveZoneAssignment = async () => {
  if (!selectedZoneId.value || !shipment.value) return;
  try {
    await projectService.assignZone(shipment.value.projectId, selectedZoneId.value);
    showZoneModal.value = false;
    await fetchShipmentDetail();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Bölge atama hatası.', 'error');
  }
};

const showNewZoneForm = ref(false);
const newZoneName = ref('');
const newZoneIsOutOfCity = ref(false);
const savingNewZone = ref(false);
const createAndSelectZone = async () => {
  if (!newZoneName.value.trim()) return;
  savingNewZone.value = true;
  try {
    const zone = await projectService.createZone({ name: newZoneName.value.trim(), order: 0, isOutOfCity: newZoneIsOutOfCity.value });
    await fetchZones();
    selectedZoneId.value = zone.id;
    showNewZoneForm.value = false;
    newZoneName.value = '';
    newZoneIsOutOfCity.value = false;
    notificationStore.add('Bölge oluşturuldu.', 'success');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Bölge oluşturulamadı.', 'error');
  } finally {
    savingNewZone.value = false;
  }
};

// Warehouse / ready / revert actions
const assignToWarehouse = async () => {
  if (!shipment.value) return;
  try {
    const result = await shipmentService.assignToWarehouse(shipment.value.id);
    if (result?.warnings?.length) {
      actionWarnings.value = result.warnings;
      await fetchShipmentDetail();
    } else {
      notificationStore.add('Sevkiyat depoya atandı.', 'success');
      router.push('/shipments');
    }
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

const openMarkReadyConfirm = () => { pendingMarkReadyWarnings.value = []; showMarkReadyConfirm.value = true; };
const confirmMarkReady = async () => { showMarkReadyConfirm.value = false; await markReady(); };
const markReady = async () => {
  if (!shipment.value) return;
  try {
    const result = await shipmentService.markReady(shipment.value.id);
    await fetchShipmentDetail();
    if (result?.warnings?.length) actionWarnings.value = result.warnings;
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

const openRevertModal = () => { revertReasonText.value = ''; showRevertModal.value = true; };

const openAdminResetModal = () => { adminResetReason.value = ''; showAdminResetModal.value = true; };
const openRevertDeliveredModal = () => { revertDeliveredReason.value = ''; showRevertDeliveredModal.value = true; };
const confirmRevertDelivered = async () => {
  if (!shipment.value || !revertDeliveredReason.value.trim()) return;
  showRevertDeliveredModal.value = false;
  try {
    await shipmentService.revertDelivered(shipment.value.id, revertDeliveredReason.value.trim());
    await fetchShipmentDetail();
    notificationStore.add('Teslimat geri alındı. Sevkiyat "Sevke Hazır" durumuna geçirildi.', 'success');
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};
const confirmAdminReset = async () => {
  if (!shipment.value || !adminResetReason.value.trim()) return;
  showAdminResetModal.value = false;
  try {
    await shipmentService.adminReset(shipment.value.id, adminResetReason.value.trim());
    await fetchShipmentDetail();
    notificationStore.add('Sevkiyat sıfırlandı ve sipariş serbest bırakıldı.', 'success');
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};;
const confirmRevert = async () => {
  if (!shipment.value) return;
  showRevertModal.value = false;
  try {
    await shipmentService.revertToDraft(shipment.value.id, { reason: revertReasonText.value });
    await fetchShipmentDetail();
    notificationStore.add('Sevkiyat taslağa geri alındı.', 'info');
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

// Netsis / irsaliye
const exportClothingToNetsis = async () => {
  if (!shipment.value) return;
  clothingExportLoading.value = true;
  try {
    const result = await shipmentService.exportClothingToNetsis(shipment.value.id);
    await fetchShipmentDetail();
    notificationStore.add(result.irsaliyeNo ? `Netsis'e aktarıldı. İrsaliye: ${result.irsaliyeNo}` : 'Netsis\'e aktarıldı. İrsaliye henüz kesilmemiş.', 'success');
    if (result.warnings?.length) actionWarnings.value = result.warnings;
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'Netsis aktarımı başarısız.', 'error');
  } finally {
    clothingExportLoading.value = false;
  }
};

const fetchIrsaliye = async () => {
  if (!shipment.value) return;
  irsaliyeFetchLoading.value = true;
  try {
    const result = await shipmentService.fetchShipmentIrsaliye(shipment.value.id);
    await fetchShipmentDetail();
    notificationStore.add(result.message || 'İrsaliye güncellendi.', result.irsaliyeNo ? 'success' : 'warning');
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İrsaliye çekimi başarısız.', 'error');
  } finally {
    irsaliyeFetchLoading.value = false;
  }
};

const openIrsaliyeModal = () => {
  irsaliyeForm.value = {
    irsaliyeNo: shipment.value?.irsaliyeNo || '',
    irsaliyeDate: shipment.value?.irsaliyeDate || new Date().toISOString().slice(0, 10),
  };
  showIrsaliyeModal.value = true;
};
const saveIrsaliye = async () => {
  if (!shipment.value || !irsaliyeForm.value.irsaliyeNo) return;
  try {
    await shipmentService.updateIrsaliye(shipment.value.id, irsaliyeForm.value.irsaliyeNo, irsaliyeForm.value.irsaliyeDate);
    showIrsaliyeModal.value = false;
    await fetchShipmentDetail();
    notificationStore.add('İrsaliye bilgisi güncellendi.', 'success');
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

// Vehicle assignment
const openAssignVehicleModal = async () => {
  if (shipment.value?.zonePreparationId) {
    showZoneDriverModal.value = true;
    return;
  }
  vehicleForm.value = { driverId: null, vehicleId: null };
  showVehicleModal.value = true;
  if (activeDrivers.value.length === 0 || activeVehicles.value.length === 0) {
    assignListsLoading.value = true;
    try {
      const [dList, vList] = await Promise.all([transportService.getActiveDrivers(), transportService.getActiveVehicles()]);
      activeDrivers.value = dList;
      activeVehicles.value = vList;
    } catch {
      notificationStore.add('Şoför/araç listesi yüklenemedi.', 'error');
    } finally {
      assignListsLoading.value = false;
    }
  }
};
const confirmAssignVehicle = async () => {
  if (!shipment.value || !vehicleForm.value.driverId || !vehicleForm.value.vehicleId) {
    notificationStore.add('Şoför ve araç seçimi zorunludur.', 'warning');
    return;
  }
  try {
    await shipmentService.assignVehicle(shipment.value.id, { driverId: vehicleForm.value.driverId, vehicleId: vehicleForm.value.vehicleId });
    showVehicleModal.value = false;
    await fetchShipmentDetail();
    notificationStore.add('Araç atandı.', 'success');
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

// Delivery proof
const openDeliveryModal = () => {
  deliveryForm.value = { deliveryRecipient: '', deliveryNote: '', photoBase64: '', photoPreview: '', photoCompressing: false };
  showDeliveryModal.value = true;
};
const onDeliveryPhotoSelected = (e: Event) => {
  const file = (e.target as HTMLInputElement).files?.[0];
  if (!file) return;
  deliveryForm.value.photoCompressing = true;
  const reader = new FileReader();
  reader.onload = (ev) => {
    const img = new Image();
    img.onload = () => {
      const MAX = 1000;
      let w = img.width, h = img.height;
      if (w > MAX || h > MAX) {
        if (w > h) { h = Math.round((h * MAX) / w); w = MAX; }
        else       { w = Math.round((w * MAX) / h); h = MAX; }
      }
      const canvas = document.createElement('canvas');
      canvas.width = w; canvas.height = h;
      canvas.getContext('2d')!.drawImage(img, 0, 0, w, h);
      const dataUrl = canvas.toDataURL('image/jpeg', 0.75);
      deliveryForm.value.photoBase64 = dataUrl.split(',')[1] ?? '';
      deliveryForm.value.photoPreview = dataUrl;
      deliveryForm.value.photoCompressing = false;
    };
    img.src = ev.target!.result as string;
  };
  reader.readAsDataURL(file);
};
const confirmMarkDelivered = async () => {
  if (!shipment.value) return;
  try {
    await shipmentService.markDelivered(shipment.value.id, deliveryForm.value.deliveryNote || undefined, deliveryForm.value.deliveryRecipient || undefined, deliveryForm.value.photoBase64 ? [deliveryForm.value.photoBase64] : undefined);
    showDeliveryModal.value = false;
    await fetchShipmentDetail();
    notificationStore.add('Sevkiyat teslim edildi olarak işaretlendi.', 'success');
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

// Edit details (draft)
const generateId = () => { try { return crypto.randomUUID(); } catch { return `${Date.now()}-${Math.random()}`; } };
const openEditModal = () => {
  if (!shipment.value) return;
  editForm.value.deliveryDate = shipment.value.deliveryDate.split('T')[0] || '';
  editForm.value.lines = shipment.value.lines.map(l => ({ 
    id: generateId(), 
    lineId: l.id, 
    stockCode: l.localStockCode || l.stockCode, 
    stockName: l.stockName, 
    orderedQty: l.orderedQty,
    unit: (l as any).unitValue ?? 0
  }));
  if (editForm.value.lines.length === 0) addNewLine();
  editingStockIdx.value = -1;
  showEditModal.value = true;
};
const addNewLine = () => {
  editForm.value.lines.push({ id: generateId(), lineId: undefined as any, stockCode: '', stockName: '', orderedQty: 1, unit: 0 });
  const newIdx = editForm.value.lines.length - 1;
  editingStockIdx.value = newIdx;
  requestAnimationFrame(() => {
    stockRefs.value[newIdx]?.focus();
    const el = stockRefs.value[newIdx]?.$el;
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
  });
};
const removeLine = (index: number) => {
  if (editForm.value.lines.length === 1) { const l = editForm.value.lines[0]; if (l) { l.stockCode = ''; l.stockName = ''; l.orderedQty = 1; } return; }
  editForm.value.lines.splice(index, 1);
};
const onStockSelect = (item: any, line: EditLine, idx: number) => {
  line.stockCode = item.stockCode || item.StockCode;
  line.stockName = item.stockName || item.StockName;
  line.unit = typeof item.unitId === 'number' ? item.unitId : (typeof item.UnitId === 'number' ? item.UnitId : 0);
  requestAnimationFrame(() => { qtyRefs.value[idx]?.focus(); });
};
const onQtyEnter = (idx: number) => {
  if (idx === editForm.value.lines.length - 1) { addNewLine(); return; }
  requestAnimationFrame(() => { stockRefs.value[idx + 1]?.focus(); const el = stockRefs.value[idx + 1]?.$el; if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' }); });
};
const removeLineAndFocus = (idx: number) => {
  const isLast = idx === editForm.value.lines.length - 1;
  removeLine(idx);
  const targetIdx = editForm.value.lines.length === 0 ? 0 : (isLast ? idx - 1 : idx);
  requestAnimationFrame(() => { stockRefs.value[targetIdx]?.focus(); });
};
const saveDetails = async () => {
  if (!shipment.value) return;
  const emptyIdx = editForm.value.lines.findIndex(l => !l.stockCode?.trim());
  if (emptyIdx !== -1) { notificationStore.add('Lütfen tüm satırlar için stok seçimi yapınız.', 'warning'); requestAnimationFrame(() => { stockRefs.value[emptyIdx]?.focus(); }); return; }
  try {
    await shipmentService.updateDetails(shipment.value.id, { 
      deliveryDate: editForm.value.deliveryDate, 
      lines: editForm.value.lines.map(l => ({ 
        lineId: l.lineId, 
        stockCode: l.stockCode, 
        stockName: l.stockName, 
        orderedQty: Number(l.orderedQty), 
        unit: Number(l.unit || 0) 
      })) 
    } as any);
    showEditModal.value = false;
    await fetchShipmentDetail();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'Güncelleme başarısız.', 'error');
  }
};

// Quantities
const openQuantitiesModal = () => {
  if (!shipment.value) return;
  quantitiesForm.value = shipment.value.lines.map(l => ({ lineId: l.id, stockCode: l.localStockCode || l.stockCode, stockName: l.stockName, orderedQty: l.orderedQty, deliveredQty: l.deliveredQty, differenceReason: l.differenceReason || '', note: l.note || '' }));
  showQuantitiesModal.value = true;
};
const saveQuantities = async () => {
  if (!shipment.value) return;
  const invalid = quantitiesForm.value.filter(l => l.deliveredQty !== l.orderedQty && !l.differenceReason?.trim());
  if (invalid.length > 0) { notificationStore.add('Dikkat: Sipariş miktarından farklı teslimat girilen satırlar için "Fark Nedeni" belirtmelisiniz.', 'warning'); return; }
  try {
    await shipmentService.updateQuantities(shipment.value.id, { lines: quantitiesForm.value.map(l => ({ lineId: l.lineId, deliveredQty: Number(l.deliveredQty), differenceReason: l.differenceReason, note: l.note })) });
    showQuantitiesModal.value = false;
    await fetchShipmentDetail();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'Güncelleme başarısız.', 'error');
  }
};

// Vehicle return
const openVehicleReturnModal = () => {
  if (!shipment.value) return;
  vehicleReturnForm.value = {
    lines: shipment.value.lines.map(l => ({ shipmentLineId: l.id, stockCode: (l as any).localStockCode || l.stockCode, stockName: l.stockName, deliveredQty: l.deliveredQty > 0 ? l.deliveredQty : l.orderedQty, returnedQty: 0, returnReason: 99 })),
    returnNote: '',
  };
  showVehicleReturnModal.value = true;
};
const confirmVehicleReturn = async () => {
  if (!shipment.value) return;
  const activeLines = vehicleReturnForm.value.lines.filter(l => l.returnedQty > 0);
  if (activeLines.length === 0) { notificationStore.add('En az bir kalem için iade miktarı girilmelidir.', 'warning'); return; }
  try {
    await shipmentService.recordVehicleReturn(shipment.value.id, { lines: activeLines.map(l => ({ shipmentLineId: l.shipmentLineId, returnedQty: l.returnedQty, returnReason: l.returnReason })), returnNote: vehicleReturnForm.value.returnNote || undefined });
    showVehicleReturnModal.value = false;
    await fetchShipmentDetail();
    notificationStore.add('Araç iadesi başarıyla kaydedildi.', 'success');
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

// Ensure route is defined (it should be at the top of script setup)

onMounted(async () => { 
  await fetchShipmentDetail(); 
  if (route.query.action === 'return' && shipment.value?.status === 'Dispatched') {
    openVehicleReturnModal();
  }
});
</script>
