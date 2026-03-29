<template>
  <div v-if="loading" class="p-6 text-center py-10">
    <span class="text-gray-500 dark:text-gray-400">Yükleniyor...</span>
  </div>
  <div v-else-if="!shipment" class="p-6 text-center py-10">
    <span class="text-gray-500 dark:text-gray-400">Sevkiyat bulunamadı.</span>
  </div>
  <div v-else class="p-6">

    <!-- Back button -->
    <button
      @click="router.back()"
      class="flex items-center gap-1.5 text-sm text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 transition-colors mb-4"
    >
      <ChevronLeftIcon class="w-4 h-4" />
      Sevkiyatlar
    </button>

    <!-- Page Header -->
    <div class="mb-5">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Sevkiyat #{{ shipment.id }}</h1>
      <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">{{ shipment.projectName }}</p>
    </div>

    <!-- 2-col layout: left = content, right = sticky sidebar -->
    <div class="flex flex-col lg:flex-row gap-6 items-start">

      <!-- ── LEFT: main content ── -->
      <div class="flex-1 min-w-0">

        <!-- Info card -->
        <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4 mb-4">
          <div class="grid grid-cols-2 md:grid-cols-3 gap-x-6 gap-y-3 text-sm">
            <div>
              <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Proje</div>
              <div class="font-medium text-gray-800 dark:text-gray-200">{{ shipment.projectName }}</div>
            </div>
            <div>
              <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Teslim Tarihi</div>
              <div class="font-medium text-gray-800 dark:text-gray-200">{{ new Date(shipment.deliveryDate).toLocaleDateString('tr-TR') }}</div>
            </div>
            <div v-if="shipment.zoneName">
              <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Bölge</div>
              <div class="font-medium text-gray-800 dark:text-gray-200">{{ shipment.zoneName }}</div>
            </div>
            <div v-if="shipment.externalOrderNumber">
              <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Sipariş No</div>
              <div class="font-mono text-blue-600">{{ shipment.externalOrderNumber }}</div>
            </div>
            <div v-if="shipment.talepNo">
              <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Talep No</div>
              <div class="font-medium text-gray-700 dark:text-gray-300">{{ shipment.talepNo }}</div>
            </div>
            <div v-if="shipment.teslimAlacakKisiler">
              <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Teslim Alacak</div>
              <div class="text-gray-700 dark:text-gray-300">{{ shipment.teslimAlacakKisiler }}</div>
              <div v-if="shipment.teslimAlacakTelefon" class="text-xs text-gray-500 dark:text-gray-400">{{ shipment.teslimAlacakTelefon }}</div>
            </div>
            <div v-if="shipment.yoneticiMail" class="col-span-2 md:col-span-3">
              <div class="text-xs text-gray-500 dark:text-gray-400 font-medium mb-0.5">Yönetici Mail</div>
              <div class="text-xs text-gray-600 dark:text-gray-400 break-all">{{ shipment.yoneticiMail }}</div>
            </div>
          </div>
          <div v-if="shipment.aciklama" class="mt-3 pt-3 border-t text-sm text-gray-600 dark:text-gray-400 bg-gray-50 dark:bg-gray-800 rounded p-2">
            <span class="font-bold text-gray-700 dark:text-gray-300">Not: </span>{{ shipment.aciklama }}
          </div>
        </div>

        <!-- Tab nav -->
        <div class="border-b border-gray-200 dark:border-gray-700 mb-4">
          <nav class="-mb-px flex space-x-6">
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
          </nav>
        </div>

        <!-- Tab: Ürünler -->
        <div v-if="activeDetailTab === 'lines'" class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
          <div v-for="(group, groupIdx) in groupedLines" :key="groupIdx" class="border-b last:border-b-0">
            <div
              class="px-5 py-2 font-bold text-sm flex justify-between items-center"
              :class="group.zoneName === 'Tanımsız' || group.zoneName === 'No Zone'
                ? 'bg-red-100 text-red-800'
                : 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300'"
            >
              <span>{{ group.zoneName }}</span>
              <span class="text-xs font-normal bg-white dark:bg-gray-900 px-2 py-1 rounded border dark:border-gray-700">{{ group.lines.length }} Kalem</span>
            </div>
            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead v-if="groupIdx === 0" class="bg-gray-50 dark:bg-gray-800">
                <tr>
                  <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Stok Kodu</th>
                  <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Stok Adı</th>
                  <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Birim</th>
                  <th class="px-5 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sipariş</th>
                  <th class="px-5 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Teslim</th>
                </tr>
              </thead>
              <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                <tr v-for="line in group.lines" :key="line.id">
                  <td class="px-5 py-3 whitespace-nowrap text-sm font-mono text-gray-900 dark:text-gray-100">
                    {{ line.localStockCode || line.stockCode }}
                    <div v-if="line.localStockCode && line.localStockCode !== line.stockCode" class="text-xs text-gray-400">
                      ISS: {{ line.stockCode }}
                    </div>
                  </td>
                  <td class="px-5 py-3 text-sm text-gray-900 dark:text-gray-100">{{ line.stockName }}</td>
                  <td class="px-5 py-3 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ line.unit || '-' }}</td>
                  <td class="px-5 py-3 whitespace-nowrap text-sm font-bold text-gray-900 dark:text-gray-100 text-right">{{ line.orderedQty }}</td>
                  <td class="px-5 py-3 whitespace-nowrap text-sm text-right"
                      :class="line.deliveredQty > 0 && line.deliveredQty !== line.orderedQty ? 'text-red-600 font-bold' : 'text-gray-900 dark:text-gray-100'">
                    {{ line.deliveredQty > 0 ? line.deliveredQty : '-' }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-if="groupedLines.length === 0" class="px-5 py-10 text-center text-sm text-gray-400">
            Ürün kaydı bulunamadı.
          </div>
        </div>

        <!-- Tab: Sürücü & Teslimat -->
        <div v-if="activeDetailTab === 'delivery'" class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl divide-y divide-gray-100 dark:divide-gray-700">
          <!-- Araç & Sürücü -->
          <div class="p-5">
            <h3 class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Araç & Sürücü</h3>
            <div class="grid grid-cols-2 gap-4 text-sm">
              <div>
                <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">Sürücü</div>
                <div class="font-medium text-gray-800 dark:text-gray-200">{{ shipment.driverName || '—' }}</div>
              </div>
              <div>
                <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">Plaka</div>
                <div class="font-medium text-gray-800 dark:text-gray-200">{{ shipment.plateNumber || '—' }}</div>
              </div>
            </div>
          </div>
          <!-- İrsaliye -->
          <div class="p-5">
            <h3 class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">İrsaliye</h3>
            <div class="grid grid-cols-2 gap-4 text-sm">
              <div>
                <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">İrsaliye No</div>
                <div class="flex items-center gap-2">
                  <span class="font-mono text-blue-700 font-medium">{{ shipment.irsaliyeNo || '—' }}</span>
                  <button
                    v-role="['Admin', 'Manager']"
                    @click="openIrsaliyeModal"
                    class="text-xs text-indigo-600 hover:text-indigo-800 border border-indigo-200 rounded px-1.5 py-0.5 transition-colors"
                  >{{ shipment.irsaliyeNo ? 'Güncelle' : 'Gir' }}</button>
                </div>
              </div>
              <div v-if="shipment.irsaliyeDate">
                <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">İrsaliye Tarihi</div>
                <div class="text-gray-700 dark:text-gray-300">{{ shipment.irsaliyeDate }}</div>
              </div>
              <div v-if="shipment.netsisTransferredAt" class="col-span-2">
                <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">Netsis Aktarım</div>
                <div class="text-xs text-green-700 font-medium">{{ new Date(shipment.netsisTransferredAt).toLocaleString('tr-TR') }}</div>
              </div>
            </div>
          </div>
          <!-- Teslim Bilgisi -->
          <div v-if="shipment.deliveredAt" class="p-5 bg-green-50 dark:bg-green-900/10">
            <h3 class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">Teslim Bilgisi</h3>
            <div class="grid grid-cols-2 gap-4 text-sm">
              <div>
                <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">Teslim Zamanı</div>
                <div class="font-medium text-green-700">{{ new Date(shipment.deliveredAt).toLocaleString('tr-TR') }}</div>
              </div>
              <div v-if="shipment.deliveryRecipient">
                <div class="text-xs text-gray-500 dark:text-gray-400 mb-0.5">Teslim Alan</div>
                <div class="font-medium text-gray-800 dark:text-gray-200">{{ shipment.deliveryRecipient }}</div>
              </div>
              <div v-if="shipment.deliveryNote" class="col-span-2 text-xs text-gray-600 dark:text-gray-400 italic bg-white dark:bg-gray-900 border border-green-100 rounded p-2">
                {{ shipment.deliveryNote }}
              </div>
              <!-- Teslimat Fotoğrafı -->
              <div v-if="shipment.deliveryPhotoBase64" class="col-span-2">
                <div class="text-xs text-gray-500 dark:text-gray-400 mb-1">Teslimat Fotoğrafı</div>
                <img
                  :src="`data:image/jpeg;base64,${shipment.deliveryPhotoBase64}`"
                  alt="Teslimat fotoğrafı"
                  class="rounded-lg border border-green-200 max-h-64 object-contain cursor-pointer hover:opacity-90 transition-opacity"
                  @click="photoLightboxSrc = `data:image/jpeg;base64,${shipment.deliveryPhotoBase64}`"
                />
              </div>
            </div>
          </div>

          <!-- Fotoğraf Lightbox -->
          <div
            v-if="photoLightboxSrc"
            class="fixed inset-0 z-50 bg-black/80 flex items-center justify-center p-4"
            @click="photoLightboxSrc = null"
          >
            <img :src="photoLightboxSrc" alt="Teslimat fotoğrafı" class="max-w-full max-h-full rounded-lg object-contain" />
          </div>
          <!-- Empty state -->
          <div v-if="!shipment.driverName && !shipment.irsaliyeNo && !shipment.deliveredAt" class="p-8 text-center text-sm text-gray-400">
            Henüz araç/sürücü atanmamış.
          </div>
        </div>

        <!-- Tab: Tarihçe -->
        <div v-if="activeDetailTab === 'history'" class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
          <ul class="divide-y divide-gray-200 dark:divide-gray-700">
            <li v-for="(history, index) in shipment.history" :key="index" class="px-5 py-4">
              <div class="flex items-start justify-between gap-4">
                <div>
                  <p class="text-sm text-gray-800 dark:text-gray-200">
                    <strong>{{ history.oldStatus }}</strong>
                    <span class="mx-1 text-gray-400">→</span>
                    <strong>{{ history.newStatus }}</strong>
                  </p>
                  <p v-if="history.description" class="text-xs text-gray-500 dark:text-gray-400 mt-0.5 italic">{{ history.description }}</p>
                  <p class="text-xs text-gray-400 mt-1">{{ history.changedBy }}</p>
                </div>
                <span class="text-xs text-gray-500 dark:text-gray-400 whitespace-nowrap shrink-0">
                  {{ new Date(history.changedAt).toLocaleString('tr-TR') }}
                </span>
              </div>
            </li>
            <li v-if="shipment.history.length === 0" class="px-5 py-10 text-center text-sm text-gray-400">
              Henüz tarihçe kaydı yok.
            </li>
          </ul>
        </div>

      </div>

      <!-- ── RIGHT: Sticky sidebar ── -->
      <div class="w-full lg:w-72 shrink-0 lg:sticky lg:top-6 space-y-4">

        <!-- Status card -->
        <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
          <div class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-2">Durum</div>
          <StatusBadge :status="shipment.status" type="shipment" />
          <div v-if="!shipment.zoneId" class="mt-3 text-xs text-red-600 bg-red-50 border border-red-100 rounded px-2 py-1.5 flex items-center gap-1">
            ⚠️ Bölge henüz atanmamış
          </div>
        </div>

        <!-- Actions card -->
        <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
          <div class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">İşlemler</div>
          <div class="space-y-2">

            <button
              v-if="shipment.status === 'Created'"
              v-role="['Admin', 'Accounting']"
              @click="openEditModal"
              class="w-full bg-gray-600 text-white py-2 px-4 rounded-lg hover:bg-gray-700 transition text-sm font-medium"
            >Siparişi Düzenle</button>

            <button
              v-if="shipment.status === 'Created' && !shipment.zoneId"
              v-role="['Admin']"
              @click="openZoneModal"
              class="w-full bg-orange-500 text-white py-2 px-4 rounded-lg hover:bg-orange-600 transition text-sm font-medium"
            >⚠️ Bölge Ata (Gerekli)</button>

            <button
              v-if="shipment.status === 'Created'"
              v-role="['Admin', 'Accounting']"
              @click="assignToWarehouse"
              :disabled="!shipment.zoneId"
              class="w-full text-white py-2 px-4 rounded-lg transition text-sm font-medium"
              :class="!shipment.zoneId ? 'bg-gray-300 cursor-not-allowed' : 'bg-yellow-500 hover:bg-yellow-600'"
            >Depoya Ata</button>

            <button
              v-if="['AssignedToWarehouse', 'Picking'].includes(shipment.status)"
              v-role="['Admin', 'Accounting']"
              @click="openRevertModal"
              class="w-full border border-red-400 text-red-600 py-2 px-4 rounded-lg hover:bg-red-50 transition text-sm font-medium"
            >Taslağa Geri Çek</button>

            <button
              v-if="shipment.status === 'AssignedToWarehouse'"
              v-role="['Admin', 'Warehouse']"
              @click="startPicking"
              class="w-full bg-blue-600 text-white py-2 px-4 rounded-lg hover:bg-blue-700 transition text-sm font-medium"
            >Toplamaya Başla</button>

            <button
              v-if="shipment.status === 'Picking'"
              v-role="['Admin', 'Warehouse']"
              @click="markReady"
              class="w-full bg-purple-600 text-white py-2 px-4 rounded-lg hover:bg-purple-700 transition text-sm font-medium"
            >Hazır Olarak İşaretle</button>

            <button
              v-if="['Picking', 'AssignedToWarehouse'].includes(shipment.status)"
              v-role="['Admin', 'Warehouse']"
              @click="openQuantitiesModal"
              class="w-full border border-blue-500 text-blue-600 py-2 px-4 rounded-lg hover:bg-blue-50 transition text-sm font-medium"
            >Miktarları Düzenle</button>

            <button
              v-if="shipment.status === 'ReadyForDispatch'"
              v-role="['Admin', 'Dispatcher']"
              @click="openAssignVehicleModal"
              :disabled="!shipment.zoneId"
              class="w-full text-white py-2 px-4 rounded-lg transition text-sm font-medium"
              :class="!shipment.zoneId ? 'bg-gray-300 cursor-not-allowed' : 'bg-indigo-600 hover:bg-indigo-700'"
            >Araca Ata</button>

            <button
              v-if="shipment.status === 'AssignedToVehicle'"
              v-role="['Admin', 'Dispatcher', 'Manager']"
              @click="openDeliveryModal"
              class="w-full bg-green-600 text-white py-2 px-4 rounded-lg hover:bg-green-700 transition text-sm font-bold"
            >Teslim Edildi</button>

            <button
              v-if="['AssignedToVehicle', 'Delivered'].includes(shipment.status)"
              v-role="['Admin', 'Dispatcher', 'Manager', 'Warehouse']"
              @click="openVehicleReturnModal"
              class="w-full bg-orange-500 text-white py-2 px-4 rounded-lg hover:bg-orange-600 transition text-sm font-medium"
            >Araç İadesi Kaydet</button>

            <p v-if="['ReturnedToWarehouse', 'Cancelled'].includes(shipment.status)" class="text-xs text-gray-400 text-center py-2">
              Bu sevkiyat için açık aksiyon yok.
            </p>
          </div>
        </div>

      </div>
    </div>

    <!-- ── MODALS ── -->

    <!-- Zone Assign Modal -->
    <BaseModal :show="showZoneModal" title="Proje Bölgesi Ata" maxWidth="sm" @close="showZoneModal = false">
      <p class="text-sm text-gray-600 dark:text-gray-400 mb-4">
        Bu proje için henüz bir bölge tanımlanmamış. İşlemlere devam etmek için bir bölge seçiniz.
      </p>
      <div class="mb-4">
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">Bölge</label>
        <select v-model="selectedZoneId" class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-gray-100">
          <option :value="null">Seçiniz...</option>
          <option v-for="zone in availableZones" :key="zone.id" :value="zone.id">{{ zone.name }}</option>
        </select>
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
            <select v-model="vehicleForm.driverId"
                    class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-gray-100">
              <option :value="null">Seçiniz...</option>
              <option v-for="d in activeDrivers" :key="d.id" :value="d.id">{{ d.fullName }}</option>
            </select>
          </div>
          <div class="mt-3">
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Araç <span class="text-red-500">*</span></label>
            <select v-model="vehicleForm.vehicleId"
                    class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-gray-100">
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
      <div class="mb-6 bg-blue-50 p-4 rounded border border-blue-100">
        <label class="block text-sm font-bold text-blue-800 mb-1">Teslim Tarihi</label>
        <input v-model="editForm.deliveryDate" type="date" class="border dark:border-gray-700 p-2 rounded w-full md:w-64 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-800 dark:text-gray-100" />
      </div>
      <div class="bg-white dark:bg-gray-900 rounded border dark:border-gray-700 shadow-sm pb-48">
        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
          <thead class="bg-gray-50 dark:bg-gray-800">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Stok Seçimi</th>
              <th class="px-4 py-3 text-right text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider w-32">Miktar</th>
              <th class="px-4 py-3 text-center text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider w-16">Sil</th>
            </tr>
          </thead>
          <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
            <tr v-for="(line, idx) in editForm.lines" :key="line.id" class="hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
              <td class="px-4 py-3 text-sm">
                <StockCombobox
                  :ref="(el: any) => stockRefs[idx] = el"
                  :initialCode="line.stockCode"
                  :placeholder="line.stockName"
                  @select="(item: any) => onStockSelect(item, line, idx)"
                  class="w-full"
                />
                <div class="mt-1 flex flex-col gap-0.5" v-if="line.stockCode">
                  <div class="text-xs text-gray-400">Kod: <span class="font-mono">{{ line.stockCode }}</span></div>
                  <div class="text-xs text-gray-800 dark:text-gray-200 font-medium">{{ line.stockName }}</div>
                </div>
              </td>
              <td class="px-4 py-3 text-sm">
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
              <td class="px-4 py-3 text-sm text-center">
                <button @click="removeLine(idx)" class="text-red-500 hover:text-red-700 p-2 rounded hover:bg-red-50 transition">
                  <span class="text-lg font-bold">&times;</span>
                </button>
              </td>
            </tr>
            <tr v-if="editForm.lines.length === 0">
              <td colspan="3" class="px-4 py-8 text-center text-gray-400 italic">Henüz ürün eklenmemiş.</td>
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
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 mb-4">
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
          <input
            v-model="deliveryForm.deliveryRecipient"
            type="text"
            placeholder="Ör: Ahmet Yılmaz"
            class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-green-500 focus:border-green-500 dark:bg-gray-800 dark:text-gray-100"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Not</label>
          <textarea
            v-model="deliveryForm.deliveryNote"
            rows="2"
            placeholder="İsteğe bağlı sürücü notu..."
            class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-green-500 focus:border-green-500 resize-none dark:bg-gray-800 dark:text-gray-100"
          />
        </div>
        <!-- Fotoğraf -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Teslimat Fotoğrafı</label>
          <label class="flex flex-col items-center justify-center gap-2 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg p-4 cursor-pointer hover:border-green-400 transition-colors"
            :class="deliveryForm.photoPreview ? 'border-green-400' : ''">
            <input
              type="file"
              accept="image/*"
              capture="environment"
              class="hidden"
              @change="onDeliveryPhotoSelected"
            />
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
        <button
          @click="confirmMarkDelivered"
          :disabled="deliveryForm.photoCompressing"
          class="px-4 py-2 bg-green-600 text-white rounded hover:bg-green-700 font-bold disabled:opacity-50"
        >Teslim Edildi</button>
      </template>
    </BaseModal>

    <!-- İrsaliye Modal -->
    <BaseModal :show="showIrsaliyeModal" title="İrsaliye Bilgisi Gir / Güncelle" maxWidth="sm" @close="showIrsaliyeModal = false">
      <div class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">İrsaliye No <span class="text-red-500">*</span></label>
          <input
            v-model="irsaliyeForm.irsaliyeNo"
            type="text"
            maxlength="50"
            placeholder="Örn: IRŞ-2024-001"
            class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 font-mono dark:bg-gray-800 dark:text-gray-100"
          />
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">İrsaliye Tarihi <span class="text-red-500">*</span></label>
          <input
            v-model="irsaliyeForm.irsaliyeDate"
            type="date"
            class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-indigo-500 focus:border-indigo-500 dark:bg-gray-800 dark:text-gray-100"
          />
        </div>
      </div>
      <template #footer>
        <button @click="showIrsaliyeModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button
          @click="saveIrsaliye"
          :disabled="!irsaliyeForm.irsaliyeNo || !irsaliyeForm.irsaliyeDate"
          class="px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 font-bold disabled:bg-indigo-300"
        >Kaydet</button>
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
                  <input
                    v-model.number="item.returnedQty"
                    type="number"
                    min="0"
                    :max="item.deliveredQty"
                    step="0.01"
                    class="w-24 border dark:border-gray-700 rounded px-2 py-1 text-right focus:ring-2 focus:ring-orange-400 dark:bg-gray-800 dark:text-gray-100"
                  />
                </td>
                <td class="px-3 py-2">
                  <select
                    v-model.number="item.returnReason"
                    :disabled="!item.returnedQty"
                    class="border dark:border-gray-700 rounded px-2 py-1 text-sm focus:ring-2 focus:ring-orange-400 disabled:bg-gray-100 dark:bg-gray-800 dark:text-gray-100"
                  >
                    <option v-for="r in returnReasons" :key="r.value" :value="r.value">{{ r.label }}</option>
                  </select>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Genel Not</label>
          <textarea
            v-model="vehicleReturnForm.returnNote"
            rows="2"
            placeholder="İsteğe bağlı genel iade notu..."
            class="w-full border dark:border-gray-700 p-2 rounded focus:ring-2 focus:ring-orange-400 resize-none text-sm dark:bg-gray-800 dark:text-gray-100"
          />
        </div>
      </div>
      <template #footer>
        <button @click="showVehicleReturnModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
        <button @click="confirmVehicleReturn" class="px-4 py-2 bg-orange-500 text-white rounded hover:bg-orange-600 font-bold">İadeyi Kaydet</button>
      </template>
    </BaseModal>

    <!-- Revert to Draft Modal -->
    <BaseModal :show="showRevertModal" title="Taslağa Geri Çek" maxWidth="sm" @close="showRevertModal = false">
      <div class="space-y-3">
        <p class="text-sm text-gray-600 dark:text-gray-400 bg-red-50 border border-red-100 rounded p-3">
          Bu sevkiyat taslak durumuna geri alınacak. İşlem tarihçeye kaydedilir.
        </p>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Neden (İsteğe Bağlı)</label>
          <textarea
            v-model="revertReasonText"
            rows="3"
            placeholder="Geri çekme nedeni..."
            class="w-full border dark:border-gray-700 p-2 rounded resize-none text-sm focus:ring-2 focus:ring-red-400 dark:bg-gray-800 dark:text-gray-100"
          />
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
import { ChevronLeftIcon } from '@heroicons/vue/24/outline';
import shipmentService from '../services/shipmentService';
import projectService from '../services/projectService';
import transportService, { type Driver, type Vehicle } from '../services/transportService';
import StockCombobox from '../components/StockCombobox.vue';
import BaseModal from '../components/BaseModal.vue';
import StatusBadge from '../components/StatusBadge.vue';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

interface ShipmentDetail {
  id: number;
  projectId: number;
  projectName: string;
  zoneId?: number;
  zoneName?: string;
  status: string;
  deliveryDate: string;
  driverName?: string;
  plateNumber?: string;
  irsaliyeNo?: string;
  irsaliyeDate?: string;
  netsisTransferredAt?: string;
  deliveredAt?: string;
  deliveryNote?: string;
  deliveryRecipient?: string;
  deliveryPhotoBase64?: string;
  externalOrderNumber?: string;
  talepNo?: string;
  teslimAlacakKisiler?: string;
  teslimAlacakTelefon?: string;
  yoneticiMail?: string;
  aciklama?: string;
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
}

const route = useRoute();
const router = useRouter();
const notificationStore = useNotificationStore();
const shipment = ref<ShipmentDetail | null>(null);
const loading = ref(false);

// Tab state
const activeDetailTab = ref<'lines' | 'delivery' | 'history'>('lines');

// Modal State
const showVehicleModal = ref(false);
const vehicleForm = ref<{ driverId: number | null; vehicleId: number | null }>({ driverId: null, vehicleId: null });
const activeDrivers = ref<Driver[]>([]);
const activeVehicles = ref<Vehicle[]>([]);
const assignListsLoading = ref(false);

// Zone Logic
const showZoneModal = ref(false);
const selectedZoneId = ref<number | null>(null);
const availableZones = ref<any[]>([]);

const fetchZones = async () => {
  try {
    availableZones.value = await projectService.getZones();
  } catch (e) {
    console.error('Zones fetch error', e);
  }
};

const openZoneModal = async () => {
  await fetchZones();
  showZoneModal.value = true;
};

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

// Computed Grouped Lines
const groupedLines = computed(() => {
  if (!shipment.value || !shipment.value.lines) return [];
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

const assignToWarehouse = async () => {
  if (!shipment.value) return;
  try {
    await shipmentService.assignToWarehouse(shipment.value.id);
    await fetchShipmentDetail();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

const startPicking = async () => {
  if (!shipment.value) return;
  try {
    await shipmentService.startPicking(shipment.value.id);
    await fetchShipmentDetail();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

const markReady = async () => {
  if (!shipment.value) return;
  try {
    await shipmentService.markReady(shipment.value.id);
    await fetchShipmentDetail();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

// Revert to Draft Modal
const showRevertModal = ref(false);
const revertReasonText = ref('');

const openRevertModal = () => {
  revertReasonText.value = '';
  showRevertModal.value = true;
};

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

const openAssignVehicleModal = async () => {
  vehicleForm.value = { driverId: null, vehicleId: null };
  showVehicleModal.value = true;
  if (activeDrivers.value.length === 0 || activeVehicles.value.length === 0) {
    assignListsLoading.value = true;
    try {
      const [dList, vList] = await Promise.all([
        transportService.getActiveDrivers(),
        transportService.getActiveVehicles(),
      ]);
      activeDrivers.value = dList;
      activeVehicles.value = vList;
    } catch {
      notificationStore.add('Şoför/araç listesi yüklenemedi.', 'error');
    } finally {
      assignListsLoading.value = false;
    }
  }
};

// Delivery Proof Modal
const showDeliveryModal = ref(false);
const deliveryForm = ref({ deliveryRecipient: '', deliveryNote: '', photoBase64: '', photoPreview: '', photoCompressing: false });
const photoLightboxSrc = ref<string | null>(null);

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
      let w = img.width;
      let h = img.height;
      if (w > MAX || h > MAX) {
        if (w > h) { h = Math.round((h * MAX) / w); w = MAX; }
        else       { w = Math.round((w * MAX) / h); h = MAX; }
      }
      const canvas = document.createElement('canvas');
      canvas.width = w;
      canvas.height = h;
      canvas.getContext('2d')!.drawImage(img, 0, 0, w, h);
      const dataUrl = canvas.toDataURL('image/jpeg', 0.75);
      deliveryForm.value.photoBase64 = dataUrl.split(',')[1] ?? ''; // strip data:image/jpeg;base64, prefix
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
    await shipmentService.markDelivered(
      shipment.value.id,
      deliveryForm.value.deliveryNote || undefined,
      deliveryForm.value.deliveryRecipient || undefined,
      deliveryForm.value.photoBase64 || undefined
    );
    showDeliveryModal.value = false;
    await fetchShipmentDetail();
    notificationStore.add('Sevkiyat teslim edildi olarak işaretlendi.', 'success');
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

// İrsaliye Modal
const showIrsaliyeModal = ref(false);
const irsaliyeForm = ref({ irsaliyeNo: '', irsaliyeDate: '' });

const openIrsaliyeModal = () => {
  irsaliyeForm.value = {
    irsaliyeNo: shipment.value?.irsaliyeNo || '',
    irsaliyeDate: shipment.value?.irsaliyeDate || new Date().toISOString().slice(0, 10)
  };
  showIrsaliyeModal.value = true;
};

const saveIrsaliye = async () => {
  if (!shipment.value || !irsaliyeForm.value.irsaliyeNo) return;
  try {
    await shipmentService.updateIrsaliye(
      shipment.value.id,
      irsaliyeForm.value.irsaliyeNo,
      irsaliyeForm.value.irsaliyeDate
    );
    showIrsaliyeModal.value = false;
    await fetchShipmentDetail();
    notificationStore.add('İrsaliye bilgisi güncellendi.', 'success');
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

const confirmAssignVehicle = async () => {
  if (!shipment.value) return;
  if (!vehicleForm.value.driverId || !vehicleForm.value.vehicleId) {
    notificationStore.add('Şoför ve araç seçimi zorunludur.', 'warning');
    return;
  }
  try {
    await shipmentService.assignVehicle(shipment.value.id, {
      driverId: vehicleForm.value.driverId,
      vehicleId: vehicleForm.value.vehicleId,
    });
    showVehicleModal.value = false;
    await fetchShipmentDetail();
    notificationStore.add('Araç atandı.', 'success');
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

// Edit Details (Draft)
interface EditLine {
  id: string;
  lineId?: number;
  stockCode: string;
  stockName: string;
  orderedQty: number;
}

const showEditModal = ref(false);
const editForm = ref<{ deliveryDate: string; lines: EditLine[] }>({ deliveryDate: '', lines: [] });
const qtyRefs = ref<any[]>([]);
const stockRefs = ref<any[]>([]);

const generateId = () => {
  try {
    return crypto.randomUUID();
  } catch {
    return `${Date.now()}-${Math.random()}`;
  }
};

const openEditModal = () => {
  if (!shipment.value) return;
  editForm.value.deliveryDate = shipment.value.deliveryDate.split('T')[0] || '';
  editForm.value.lines = shipment.value.lines.map(l => ({
    id: generateId(),
    lineId: l.id,
    stockCode: l.localStockCode || l.stockCode,
    stockName: l.stockName,
    orderedQty: l.orderedQty
  }));
  if (editForm.value.lines.length === 0) addNewLine();
  showEditModal.value = true;
};

const addNewLine = () => {
  editForm.value.lines.push({ id: generateId(), lineId: 0, stockCode: '', stockName: '', orderedQty: 1 });
  requestAnimationFrame(() => {
    const lastIdx = editForm.value.lines.length - 1;
    stockRefs.value[lastIdx]?.focus();
    const el = stockRefs.value[lastIdx]?.$el;
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
  });
};

const removeLine = (index: number) => {
  if (editForm.value.lines.length === 1) {
    const line = editForm.value.lines[0];
    if (line) { line.stockCode = ''; line.stockName = ''; line.orderedQty = 1; }
    return;
  }
  editForm.value.lines.splice(index, 1);
};

const onStockSelect = (item: any, line: EditLine, idx: number) => {
  line.stockCode = item.stockCode || item.StockCode;
  line.stockName = item.stockName || item.StockName;
  requestAnimationFrame(() => { qtyRefs.value[idx]?.focus(); });
};

const onQtyEnter = (idx: number) => {
  if (idx === editForm.value.lines.length - 1) { addNewLine(); return; }
  requestAnimationFrame(() => {
    stockRefs.value[idx + 1]?.focus();
    const el = stockRefs.value[idx + 1]?.$el;
    if (el) el.scrollIntoView({ behavior: 'smooth', block: 'center' });
  });
};

const removeLineAndFocus = (idx: number) => {
  const isOnly = editForm.value.lines.length === 1;
  const isLast = idx === editForm.value.lines.length - 1;
  removeLine(idx);
  const targetIdx = isOnly ? 0 : (isLast ? idx - 1 : idx);
  requestAnimationFrame(() => { stockRefs.value[targetIdx]?.focus(); });
};

const saveDetails = async () => {
  if (!shipment.value) return;
  const emptyLineIdx = editForm.value.lines.findIndex(l => !l.stockCode?.trim());
  if (emptyLineIdx !== -1) {
    notificationStore.add('Lütfen tüm satırlar için stok seçimi yapınız.', 'warning');
    requestAnimationFrame(() => { stockRefs.value[emptyLineIdx]?.focus(); });
    return;
  }
  try {
    const payload = {
      deliveryDate: editForm.value.deliveryDate,
      lines: editForm.value.lines.map(l => ({
        lineId: l.lineId,
        stockCode: l.stockCode,
        stockName: l.stockName,
        orderedQty: Number(l.orderedQty)
      }))
    };
    await shipmentService.updateDetails(shipment.value.id, payload as any);
    showEditModal.value = false;
    await fetchShipmentDetail();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'Güncelleme başarısız.', 'error');
  }
};

// Quantities Modal
const showQuantitiesModal = ref(false);
const quantitiesForm = ref<any[]>([]);

const openQuantitiesModal = () => {
  if (!shipment.value) return;
  quantitiesForm.value = shipment.value.lines.map(l => ({
    lineId: l.id,
    stockCode: l.localStockCode || l.stockCode,
    stockName: l.stockName,
    orderedQty: l.orderedQty,
    deliveredQty: l.deliveredQty,
    differenceReason: l.differenceReason || '',
    note: l.note || ''
  }));
  showQuantitiesModal.value = true;
};

const saveQuantities = async () => {
  if (!shipment.value) return;
  const invalidLines = quantitiesForm.value.filter(l =>
    l.deliveredQty !== l.orderedQty && !l.differenceReason?.trim()
  );
  if (invalidLines.length > 0) {
    notificationStore.add('Dikkat: Sipariş miktarından farklı teslimat girilen satırlar için "Fark Nedeni" belirtmelisiniz.', 'warning');
    return;
  }
  try {
    const payload = {
      lines: quantitiesForm.value.map(l => ({
        lineId: l.lineId,
        deliveredQty: Number(l.deliveredQty),
        differenceReason: l.differenceReason,
        note: l.note
      }))
    };
    await shipmentService.updateQuantities(shipment.value.id, payload);
    showQuantitiesModal.value = false;
    await fetchShipmentDetail();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'Güncelleme başarısız.', 'error');
  }
};

// Vehicle Return Modal
const returnReasons = [
  { value: 0, label: 'Müşteri Reddi' },
  { value: 1, label: 'Hasarlı' },
  { value: 2, label: 'Fazla Yükleme' },
  { value: 3, label: 'Yanlış Ürün' },
  { value: 4, label: 'Proje Bulunamadı' },
  { value: 99, label: 'Diğer' },
];

interface VehicleReturnLine {
  shipmentLineId: number;
  stockCode: string;
  stockName: string;
  deliveredQty: number;
  returnedQty: number;
  returnReason: number;
}

const showVehicleReturnModal = ref(false);
const vehicleReturnForm = ref<{ lines: VehicleReturnLine[]; returnNote: string }>({ lines: [], returnNote: '' });

const openVehicleReturnModal = () => {
  if (!shipment.value) return;
  vehicleReturnForm.value = {
    lines: shipment.value.lines.map(l => ({
      shipmentLineId: l.id,
      stockCode: (l as any).localStockCode || l.stockCode,
      stockName: l.stockName,
      deliveredQty: l.deliveredQty > 0 ? l.deliveredQty : l.orderedQty,
      returnedQty: 0,
      returnReason: 99,
    })),
    returnNote: '',
  };
  showVehicleReturnModal.value = true;
};

const confirmVehicleReturn = async () => {
  if (!shipment.value) return;
  const activeLines = vehicleReturnForm.value.lines.filter(l => l.returnedQty > 0);
  if (activeLines.length === 0) {
    notificationStore.add('En az bir kalem için iade miktarı girilmelidir.', 'warning');
    return;
  }
  try {
    await shipmentService.recordVehicleReturn(shipment.value.id, {
      lines: activeLines.map(l => ({
        shipmentLineId: l.shipmentLineId,
        returnedQty: l.returnedQty,
        returnReason: l.returnReason,
      })),
      returnNote: vehicleReturnForm.value.returnNote || undefined,
    });
    showVehicleReturnModal.value = false;
    await fetchShipmentDetail();
    notificationStore.add('Araç iadesi başarıyla kaydedildi.', 'success');
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

onMounted(() => {
  fetchShipmentDetail();
});
</script>
