<template>
  <div class="space-y-6">
    <PageHeader title="Tanımlamalar" subtitle="Şoför, araç ve sistem tanımlamaları" color="gray">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z" />
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
        </svg>
      </template>
    </PageHeader>

    <!-- Tab Navigation -->
    <div class="border-b border-gray-200 dark:border-gray-700">
      <nav class="-mb-px flex overflow-x-auto" aria-label="Tabs">
        <button
          v-for="tab in tabs"
          :key="tab.key"
          @click="activeTab = tab.key"
          :class="[
            activeTab === tab.key
              ? 'border-indigo-500 text-indigo-600 dark:text-indigo-400'
              : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-600',
            'whitespace-nowrap py-4 px-4 border-b-2 font-medium text-sm flex-shrink-0'
          ]"
        >
          {{ tab.label }}
        </button>
      </nav>
    </div>

    <!-- ===== TAB: Şoför & Araç ===== -->
    <div v-show="activeTab === 'transport'" class="space-y-4">

      <!-- Transport Error -->
      <div v-if="transport_error" class="p-3 bg-red-900/30 border border-red-700 rounded-lg flex items-center justify-between">
        <span class="text-red-400 text-sm">{{ transport_error }}</span>
        <button @click="fetchDrivers(); fetchVehicles(); transport_error = null" class="text-red-400 hover:text-red-300 text-sm underline ml-4">Tekrar dene</button>
      </div>

      <!-- Transport Sub-tabs -->
      <div class="border-b border-gray-200 dark:border-gray-700">
        <nav class="-mb-px flex space-x-8" aria-label="Tabs">
          <button
            @click="transport_activeTab = 'drivers'"
            :class="[
              transport_activeTab === 'drivers'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-700',
              'whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm'
            ]"
          >Şoförler</button>
          <button
            @click="transport_activeTab = 'vehicles'"
            :class="[
              transport_activeTab === 'vehicles'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-700',
              'whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm'
            ]"
          >Araçlar</button>
        </nav>
      </div>

      <!-- Drivers Sub-tab -->
      <div v-show="transport_activeTab === 'drivers'" class="space-y-4">
        <div class="flex justify-end">
          <button @click="openDriverModal()" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">
            + Yeni Şoför Ekle
          </button>
        </div>
        <div class="bg-white dark:bg-gray-900 shadow overflow-hidden rounded-lg">
          <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead class="bg-gray-50 dark:bg-gray-800">
                <tr>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Ad Soyad</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden sm:table-cell">Telefon</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
                  <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
                </tr>
              </thead>
              <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                <tr v-for="driver in drivers" :key="driver.id">
                  <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">{{ driver.fullName }}</td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 hidden sm:table-cell">{{ driver.phone }}</td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span v-if="driver.isActive" class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800">Aktif</span>
                    <span v-else class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-red-100 text-red-800">Pasif</span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <button @click="openDriverModal(driver)" class="text-indigo-600 hover:text-indigo-900 mr-4">Düzenle</button>
                    <button @click="deleteDriver(driver.id)" class="text-red-600 hover:text-red-900">Sil</button>
                  </td>
                </tr>
                <tr v-if="drivers.length === 0">
                  <td colspan="4" class="px-6 py-4 text-center text-sm text-gray-500 dark:text-gray-400">Henüz şoför eklenmedi.</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Vehicles Sub-tab -->
      <div v-show="transport_activeTab === 'vehicles'" class="space-y-4">
        <div class="flex justify-end">
          <button @click="openVehicleModal()" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">
            + Yeni Araç Ekle
          </button>
        </div>
        <div class="bg-white dark:bg-gray-900 shadow overflow-hidden rounded-lg">
          <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead class="bg-gray-50 dark:bg-gray-800">
                <tr>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Plaka</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden sm:table-cell">Araç Tipi</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">Kapasite / Notlar</th>
                  <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
                  <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
                </tr>
              </thead>
              <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                <tr v-for="vehicle in vehicles" :key="vehicle.id">
                  <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">{{ vehicle.plateNumber }}</td>
                  <td class="px-6 py-4 whitespace-nowrap hidden sm:table-cell">
                    <span class="px-2 py-0.5 text-xs rounded-full font-medium"
                      :class="{
                        'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300': vehicle.vehicleType === 0,
                        'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-300': vehicle.vehicleType === 1,
                        'bg-teal-100 text-teal-800 dark:bg-teal-900/30 dark:text-teal-300': vehicle.vehicleType === 2,
                      }">{{ (vehicle.vehicleTypeName || ['Kamyon','Kamyonet','Minibüs'][vehicle.vehicleType]) ?? 'Kamyon' }}</span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400 hidden lg:table-cell">{{ vehicle.description || vehicle.capacity || '—' }}</td>
                  <td class="px-6 py-4 whitespace-nowrap">
                    <span v-if="vehicle.isActive" class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800">Aktif</span>
                    <span v-else class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-red-100 text-red-800">Pasif</span>
                  </td>
                  <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-3">
                    <button v-if="vehicle.qrCode" @click="openQrModal(vehicle)"
                      class="inline-flex items-center gap-1 text-emerald-600 hover:text-emerald-800 dark:text-emerald-400 dark:hover:text-emerald-300 font-medium">
                      <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h4.01M16 20h4M4 12h4m12 0h.01M5 8h2a1 1 0 001-1V5a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1zm12 0h2a1 1 0 001-1V5a1 1 0 00-1-1h-2a1 1 0 00-1 1v2a1 1 0 001 1zM5 20h2a1 1 0 001-1v-2a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1z"/></svg>
                      QR Yazdır
                    </button>
                    <button v-else @click="generateAndOpenQr(vehicle)"
                      class="inline-flex items-center gap-1 text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 font-medium">
                      <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
                      QR Oluştur
                    </button>
                    <button @click="openVehicleModal(vehicle)" class="text-indigo-600 hover:text-indigo-900">Düzenle</button>
                    <button @click="deleteVehicle(vehicle.id)" class="text-red-600 hover:text-red-900">Sil</button>
                  </td>
                </tr>
                <tr v-if="vehicles.length === 0">
                  <td colspan="5" class="px-6 py-4 text-center text-sm text-gray-500 dark:text-gray-400">Henüz araç eklenmedi.</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- Driver Modal -->
      <BaseModal
        :show="showDriverModal"
        :title="editingDriver ? 'Şoför Düzenle' : 'Yeni Şoför Ekle'"
        maxWidth="sm"
        @close="showDriverModal = false"
      >
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Ad Soyad</label>
            <input v-model="driverForm.fullName" type="text"
              class="block w-full border border-gray-300 dark:border-gray-700 rounded-input px-3 py-2 focus:ring-2 focus:ring-brand-500 focus:border-brand-500 focus:outline-none dark:bg-gray-800 dark:text-gray-100 text-sm" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Telefon</label>
            <input v-model="driverForm.phone" type="text"
              class="block w-full border border-gray-300 dark:border-gray-700 rounded-input px-3 py-2 focus:ring-2 focus:ring-brand-500 focus:border-brand-500 focus:outline-none dark:bg-gray-800 dark:text-gray-100 text-sm" />
          </div>
        </div>
        <template #footer>
          <button @click="showDriverModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
          <button @click="saveDriver" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 font-medium">Kaydet</button>
        </template>
      </BaseModal>

      <!-- Vehicle Modal -->
      <BaseModal
        :show="showVehicleModal"
        :title="editingVehicle ? 'Araç Düzenle' : 'Yeni Araç Ekle'"
        maxWidth="sm"
        @close="showVehicleModal = false"
      >
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Plaka</label>
            <input v-model="vehicleForm.plateNumber" type="text" style="text-transform:uppercase"
              class="block w-full border border-gray-300 dark:border-gray-700 rounded-input px-3 py-2 focus:ring-2 focus:ring-brand-500 focus:border-brand-500 focus:outline-none dark:bg-gray-800 dark:text-gray-100 text-sm" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Araç Tipi</label>
            <select v-model="vehicleForm.vehicleType"
              class="block w-full border border-gray-300 dark:border-gray-700 rounded-input px-3 py-2 focus:ring-2 focus:ring-brand-500 focus:border-brand-500 focus:outline-none dark:bg-gray-800 dark:text-gray-100 text-sm">
              <option :value="0">Kamyon</option>
              <option :value="1">Kamyonet</option>
              <option :value="2">Minibüs</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Kapasite / Notlar <span class="text-gray-400 font-normal">(opsiyonel)</span>
            </label>
            <input v-model="vehicleForm.capacity" type="text" placeholder="Örn: 5 ton"
              class="block w-full border border-gray-300 dark:border-gray-700 rounded-input px-3 py-2 focus:ring-2 focus:ring-brand-500 focus:border-brand-500 focus:outline-none dark:bg-gray-800 dark:text-gray-100 text-sm" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Açıklama <span class="text-gray-400 font-normal">(opsiyonel)</span>
            </label>
            <input v-model="vehicleForm.description" type="text" placeholder="Araç hakkında ek bilgi"
              class="block w-full border border-gray-300 dark:border-gray-700 rounded-input px-3 py-2 focus:ring-2 focus:ring-brand-500 focus:border-brand-500 focus:outline-none dark:bg-gray-800 dark:text-gray-100 text-sm" />
          </div>
        </div>
        <template #footer>
          <button @click="showVehicleModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
          <button @click="saveVehicle" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 font-medium">Kaydet</button>
        </template>
      </BaseModal>

      <!-- QR Modal -->
      <BaseModal
        :show="qr_showModal && !!qr_vehicle"
        maxWidth="sm"
        @close="qr_showModal = false"
      >
        <template #title>
          <div>
            <span class="text-base font-semibold">Araç QR Etiketi</span>
            <p v-if="qr_vehicle" class="text-xs text-gray-500 dark:text-gray-400 font-normal mt-0.5">
              {{ qr_vehicle.plateNumber }} — {{ qr_vehicleTypeName(qr_vehicle.vehicleType) }}
            </p>
          </div>
        </template>

        <div v-if="qr_vehicle">
          <p class="text-xs text-gray-400 uppercase tracking-wider mb-3 font-medium">Etiket Önizleme</p>
          <div class="flex justify-center">
            <div style="width:240px; font-family:Arial,sans-serif; border-radius:10px; overflow:hidden; box-shadow:0 2px 12px rgba(0,0,0,0.15);">
              <div style="background:#1e3a5f; padding:10px 14px; text-align:center;">
                <div style="color:#fff; font-size:13px; font-weight:700; letter-spacing:2px;">AKYİLDIZ</div>
                <div style="color:#93c5fd; font-size:9px; letter-spacing:1px; margin-top:2px;">SEVKİYAT SİSTEMİ</div>
              </div>
              <div style="background:#fff; padding:14px 14px 18px;">
                <div style="text-align:center; margin-bottom:12px;">
                  <div v-if="qr_loading" style="width:168px; height:168px; background:#f3f4f6; margin:0 auto; display:flex; align-items:center; justify-content:center; border-radius:4px;">
                    <svg style="width:24px;height:24px;color:#9ca3af;animation:spin 1s linear infinite" fill="none" viewBox="0 0 24 24"><circle style="opacity:.25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path style="opacity:.75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
                  </div>
                  <img v-else-if="qr_imageBase64" :src="qr_imageBase64" alt="QR" style="width:168px;height:168px;display:block;margin:0 auto;" />
                </div>
                <div style="border:2.5px solid #1e3a5f; border-radius:7px; padding:7px 10px; margin-bottom:10px; text-align:center;">
                  <span style="font-size:23px; font-weight:900; letter-spacing:3px; color:#1e3a5f; font-family:'Arial Black',Arial,sans-serif;">{{ qr_vehicle.plateNumber }}</span>
                </div>
                <div style="text-align:center;">
                  <span style="background:#e0e7ff; color:#3730a3; font-size:10px; font-weight:600; padding:3px 12px; border-radius:999px; letter-spacing:.5px;">
                    {{ qr_vehicleTypeName(qr_vehicle.vehicleType) }}
                  </span>
                </div>
              </div>
            </div>
          </div>
          <p class="text-xs text-center text-gray-400 mt-3">Yaklaşık 8×10 cm etiket boyutu</p>
        </div>

        <template #footer>
          <button @click="printQr" :disabled="qr_loading || !qr_imageBase64"
            class="flex-1 bg-blue-600 text-white py-2.5 rounded-lg hover:bg-blue-700 font-medium text-sm disabled:opacity-50 flex items-center justify-center gap-2 transition-colors">
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"/>
            </svg>
            Yazdır
          </button>
          <button @click="qr_showModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-sm">
            Kapat
          </button>
        </template>
      </BaseModal>

      <!-- Yazdırma Alanı (sadece print modunda görünür) -->
      <Teleport to="body">
        <div id="vehicle-qr-print-area" style="display:none; position:fixed; inset:0; background:white; align-items:center; justify-content:center;">
          <div style="font-family:Arial,sans-serif; width:226px;">
            <div style="background:#1e3a5f; padding:10px 14px; text-align:center;">
              <div style="color:#fff; font-size:14px; font-weight:700; letter-spacing:2px;">AKYİLDIZ</div>
              <div style="color:#93c5fd; font-size:9px; letter-spacing:1px; margin-top:2px;">SEVKİYAT SİSTEMİ</div>
            </div>
            <div style="background:#fff; padding:14px 14px 20px;">
              <div style="text-align:center; margin-bottom:12px;">
                <img v-if="qr_imageBase64" :src="qr_imageBase64" alt="QR" style="width:168px; height:168px; display:block; margin:0 auto;" />
              </div>
              <div style="border:2.5px solid #1e3a5f; border-radius:7px; padding:7px 10px; margin-bottom:10px; text-align:center;">
                <span style="font-size:24px; font-weight:900; letter-spacing:3px; color:#1e3a5f; font-family:'Arial Black',Arial,sans-serif;">{{ qr_vehicle?.plateNumber }}</span>
              </div>
              <div style="text-align:center;">
                <span style="background:#e0e7ff; color:#3730a3; font-size:10px; font-weight:600; padding:3px 12px; border-radius:999px;">{{ qr_vehicleTypeName(qr_vehicle?.vehicleType) }}</span>
              </div>
            </div>
          </div>
        </div>
      </Teleport>

    </div>

    <!-- ===== TAB: Bölge Yönetimi ===== -->
    <div v-show="activeTab === 'zones'" class="space-y-4">

      <!-- Zone Error -->
      <div v-if="zone_error" class="p-3 bg-red-900/30 border border-red-700 rounded-lg flex items-center justify-between">
        <span class="text-red-400 text-sm">{{ zone_error }}</span>
        <button @click="fetchZones(); zone_error = null" class="text-red-400 hover:text-red-300 text-sm underline ml-4">Tekrar dene</button>
      </div>

      <div class="flex justify-end">
        <button
          @click="zone_showModal = true; zone_isEdit = false; zone_form = { name: '', order: 0 }"
          class="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition"
        >
          + Yeni Bölge
        </button>
      </div>

      <div class="bg-white dark:bg-gray-900 rounded-lg shadow overflow-hidden">
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sıra</th>
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Bölge Adı</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
              </tr>
            </thead>
            <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
              <tr v-for="zone in zone_zones" :key="zone.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
                <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-900 dark:text-gray-100">{{ zone.order }}</td>
                <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">{{ zone.name }}</td>
                <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                  <button @click="zone_openEdit(zone)" class="text-indigo-600 hover:text-indigo-900 mr-4">Düzenle</button>
                  <button @click="zone_confirmDelete(zone)" class="text-red-600 hover:text-red-900">Sil</button>
                </td>
              </tr>
              <tr v-if="zone_zones.length === 0">
                <td colspan="3" class="px-6 py-4 text-center text-sm text-gray-500 dark:text-gray-400">Henüz bölge tanımlanmamış.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Zone Modal -->
      <BaseModal
        :show="zone_showModal"
        :title="zone_isEdit ? 'Bölgeyi Düzenle' : 'Yeni Bölge Ekle'"
        maxWidth="sm"
        @close="zone_showModal = false"
      >
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Bölge Adı</label>
            <input v-model="zone_form.name" type="text" placeholder="Örn: A Koridoru"
              class="block w-full border border-gray-300 dark:border-gray-700 rounded-input px-3 py-2 focus:ring-2 focus:ring-brand-500 focus:border-brand-500 focus:outline-none dark:bg-gray-800 dark:text-gray-100 text-sm" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Sıralama</label>
            <input v-model.number="zone_form.order" type="number"
              class="block w-full border border-gray-300 dark:border-gray-700 rounded-input px-3 py-2 focus:ring-2 focus:ring-brand-500 focus:border-brand-500 focus:outline-none dark:bg-gray-800 dark:text-gray-100 text-sm" />
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplama rotasında hangi sırada gidileceğini belirler (Küçükten büyüğe).</p>
          </div>
        </div>
        <template #footer>
          <button @click="zone_showModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
          <button @click="zone_saveZone" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 font-medium">Kaydet</button>
        </template>
      </BaseModal>

    </div>

    <!-- ===== TAB: Depo Tanımları ===== -->
    <div v-show="activeTab === 'depot'" class="max-w-2xl">
      <p class="text-gray-500 dark:text-gray-400 text-sm mb-5">Rota optimizasyonunda kullanılacak başlangıç noktası.</p>

      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-6 space-y-5">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Depo Adı</label>
          <input
            v-model="depot_form.depotName"
            type="text"
            placeholder="Örn: Akyıldız Ana Depo"
            class="w-full border dark:border-gray-700 rounded-lg px-3 py-2 dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 focus:outline-none"
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Adres</label>
          <div class="flex gap-2">
            <input
              v-model="depot_form.depotAddress"
              type="text"
              placeholder="Depo adresi"
              class="flex-1 border dark:border-gray-700 rounded-lg px-3 py-2 dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 focus:outline-none"
            />
            <button
              @click="depot_geocodeAddress"
              :disabled="!depot_form.depotAddress || depot_geocoding"
              class="px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed text-sm whitespace-nowrap flex items-center gap-2"
            >
              <svg v-if="depot_geocoding" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
              </svg>
              <span>{{ depot_geocoding ? 'Aranıyor...' : 'Koordinatı Bul' }}</span>
            </button>
          </div>
          <p v-if="depot_geocodeError" class="mt-1 text-sm text-red-500">{{ depot_geocodeError }}</p>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Enlem (Latitude)</label>
            <input
              v-model.number="depot_form.depotLatitude"
              type="number"
              step="0.000001"
              placeholder="41.0082"
              class="w-full border dark:border-gray-700 rounded-lg px-3 py-2 dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 focus:outline-none"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Boylam (Longitude)</label>
            <input
              v-model.number="depot_form.depotLongitude"
              type="number"
              step="0.000001"
              placeholder="28.9784"
              class="w-full border dark:border-gray-700 rounded-lg px-3 py-2 dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 focus:outline-none"
            />
          </div>
        </div>

        <div
          v-if="depot_form.depotLatitude && depot_form.depotLongitude"
          class="flex items-center gap-2 p-3 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-lg"
        >
          <svg class="w-4 h-4 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17.657 16.657L13.414 20.9a1.998 1.998 0 01-2.827 0l-4.244-4.243a8 8 0 1111.314 0z"/>
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 11a3 3 0 11-6 0 3 3 0 016 0z"/>
          </svg>
          <span class="text-sm text-green-700 dark:text-green-400">
            {{ depot_form.depotLatitude?.toFixed(6) }}, {{ depot_form.depotLongitude?.toFixed(6) }}
          </span>
        </div>

        <div class="flex gap-3 pt-2">
          <button
            @click="depot_save"
            :disabled="depot_saving"
            class="px-5 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 disabled:opacity-50 font-medium flex items-center gap-2"
          >
            <svg v-if="depot_saving" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
            </svg>
            <span>{{ depot_saving ? 'Kaydediliyor...' : 'Kaydet' }}</span>
          </button>
          <button
            @click="depot_load"
            :disabled="depot_loading"
            class="px-4 py-2 border dark:border-gray-700 rounded-lg text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800"
          >Sıfırla</button>
        </div>

        <p v-if="depot_savedMsg" class="text-sm text-green-600 dark:text-green-400 font-medium">✓ {{ depot_savedMsg }}</p>
      </div>
    </div>

    <!-- ===== TAB: Depo Adresleri ===== -->
    <div v-show="activeTab === 'locations'" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-6">
        <p class="text-gray-600 dark:text-gray-400 mb-4">Depo konumlarını ve adres tanımlamalarını yönetmek için ilgili sayfaya gidin.</p>
        <router-link
          to="/warehouse/locations"
          class="inline-flex items-center px-4 py-2 bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 font-medium"
        >
          Depo Adresleri Sayfasına Git
        </router-link>
      </div>
    </div>

    <!-- ===== TAB: Sipariş Sayaçları ===== -->
    <div v-show="activeTab === 'po_counter'" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-6">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-lg font-semibold text-gray-800 dark:text-gray-200">Satınalma Sipariş Sayaçları</h2>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">Sipariş numarasının sıra kısmını değiştirebilirsiniz. Bir sonraki sipariş "Sonraki Numara" ile başlar.</p>
          </div>
          <button @click="loadPoCounters" :disabled="poCounterLoading"
            class="text-sm text-indigo-600 hover:text-indigo-800 dark:text-indigo-400 underline">
            Yenile
          </button>
        </div>

        <div v-if="poCounterError" class="mb-4 p-3 bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-700 rounded text-red-700 dark:text-red-300 text-sm">
          {{ poCounterError }}
        </div>

        <div v-if="poCounterLoading" class="text-sm text-gray-500">Yükleniyor...</div>

        <div v-else-if="poCounters.length === 0" class="text-sm text-gray-500">
          Henüz sayaç kaydı yok (ilk sipariş oluşturulduğunda otomatik eklenir).
        </div>

        <div v-else class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-gray-200 dark:border-gray-700 text-gray-500 dark:text-gray-400 text-xs uppercase">
                <th class="py-2 px-3 text-left">Dönem</th>
                <th class="py-2 px-3 text-left">Son Sipariş No</th>
                <th class="py-2 px-3 text-left">Sonraki Numara</th>
                <th class="py-2 px-3 text-left">Sayaç Değeri</th>
                <th class="py-2 px-3 text-left"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="counter in poCounters" :key="counter.id"
                class="border-b border-gray-100 dark:border-gray-800">
                <td class="py-3 px-3 font-medium text-gray-800 dark:text-gray-200">
                  {{ counter.year }}/{{ String(counter.month).padStart(2, '0') }}
                </td>
                <td class="py-3 px-3 font-mono text-gray-700 dark:text-gray-300">{{ counter.formattedNumber }}</td>
                <td class="py-3 px-3 font-mono text-green-600 dark:text-green-400 font-semibold">{{ counter.nextNumber }}</td>
                <td class="py-3 px-3">
                  <input
                    v-if="editingCounterId === counter.id"
                    v-model.number="editingCounterValue"
                    type="number" min="0"
                    class="w-28 px-2 py-1 border border-indigo-400 rounded text-sm font-mono focus:outline-none focus:ring-2 focus:ring-indigo-500 dark:bg-gray-800 dark:text-gray-200"
                  />
                  <span v-else class="font-mono text-gray-600 dark:text-gray-400">{{ counter.lastValue }}</span>
                </td>
                <td class="py-3 px-3">
                  <div v-if="editingCounterId === counter.id" class="flex gap-2">
                    <button @click="savePoCounter(counter)" :disabled="poCounterSaving"
                      class="px-3 py-1 bg-indigo-600 text-white rounded text-xs hover:bg-indigo-700 disabled:opacity-50">
                      Kaydet
                    </button>
                    <button @click="editingCounterId = null"
                      class="px-3 py-1 border border-gray-300 dark:border-gray-600 rounded text-xs text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700">
                      İptal
                    </button>
                  </div>
                  <button v-else @click="startEditCounter(counter)"
                    class="px-3 py-1 border border-gray-300 dark:border-gray-600 rounded text-xs text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700">
                    Düzenle
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div class="mt-4 p-3 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-200 dark:border-yellow-700 rounded text-xs text-yellow-700 dark:text-yellow-300">
          ⚠️ Sayaç değerini düşürmek, daha önce kullanılmış numara üretebilir. Yalnızca yeni dönem başlangıcında veya hatalı artışları düzeltmek için kullanın.
        </div>
      </div>
    </div>

  </div>
</template>

<style>
@keyframes spin { to { transform: rotate(360deg); } }
@media print {
  body * { visibility: hidden !important; }
  #vehicle-qr-print-area,
  #vehicle-qr-print-area * { visibility: visible !important; }
  #vehicle-qr-print-area { display: flex !important; }
  @page { size: 80mm 100mm; margin: 0; }
}
</style>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import transportService from '../services/transportService';
import apiClient from '../services/apiClient';
import projectService from '../services/projectService';
import systemSettingsService, { type PoCounterDto } from '../services/systemSettingsService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import BaseModal from '../components/BaseModal.vue';

const notificationStore = useNotificationStore();

const tabs = [
  { key: 'transport', label: 'Şoför & Araç' },
  { key: 'zones',     label: 'Bölge Yönetimi' },
  { key: 'depot',     label: 'Depo Tanımları' },
  { key: 'locations', label: 'Depo Adresleri' },
  { key: 'po_counter', label: 'Sipariş Sayaçları' },
];
const activeTab = ref('transport');

// ─── Transport ──────────────────────────────────────────────────────────────

const transport_error = ref<string | null>(null);
const transport_activeTab = ref('drivers');
const drivers = ref<any[]>([]);
const vehicles = ref<any[]>([]);

const showDriverModal = ref(false);
const editingDriver = ref<any>(null);
const driverForm = ref({ fullName: '', phone: '' });

const showVehicleModal = ref(false);
const editingVehicle = ref<any>(null);
const vehicleForm = ref({ plateNumber: '', capacity: '', vehicleType: 0, description: '' });

const fetchDrivers = async () => {
  try {
    drivers.value = await transportService.getDrivers();
  } catch (e) {
    transport_error.value = ApiErrorUtils.getErrorMessage(e) || 'Şoförler yüklenemedi.';
    notificationStore.add(transport_error.value, 'error');
  }
};

const fetchVehicles = async () => {
  try {
    vehicles.value = await transportService.getVehicles();
  } catch (e) {
    transport_error.value = ApiErrorUtils.getErrorMessage(e) || 'Araçlar yüklenemedi.';
    notificationStore.add(transport_error.value, 'error');
  }
};

const openDriverModal = (driver: any = null) => {
  editingDriver.value = driver;
  driverForm.value = driver ? { fullName: driver.fullName, phone: driver.phone } : { fullName: '', phone: '' };
  showDriverModal.value = true;
};

const saveDriver = async () => {
  try {
    if (editingDriver.value) {
      await transportService.updateDriver({ id: editingDriver.value.id, ...driverForm.value, isActive: true });
    } else {
      await transportService.createDriver(driverForm.value);
    }
    await fetchDrivers();
    showDriverModal.value = false;
    notificationStore.add('Şoför kaydedildi.', 'success');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kaydetme başarısız.', 'error');
  }
};

const deleteDriver = async (id: number) => {
  const ok = await notificationStore.promptConfirm({ title: 'Şoförü Sil', message: 'Bu şoförü silmek istediğinize emin misiniz?', confirmText: 'Sil', type: 'danger' });
  if (!ok) return;
  try {
    await transportService.deleteDriver(id);
    fetchDrivers();
    notificationStore.add('Şoför silindi.', 'success');
  } catch (e) { notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Silme başarısız.', 'error'); }
};

const openVehicleModal = (vehicle: any = null) => {
  editingVehicle.value = vehicle;
  vehicleForm.value = vehicle
    ? { plateNumber: vehicle.plateNumber, capacity: vehicle.capacity ?? '', vehicleType: vehicle.vehicleType ?? 0, description: vehicle.description ?? '' }
    : { plateNumber: '', capacity: '', vehicleType: 0, description: '' };
  showVehicleModal.value = true;
};

const saveVehicle = async () => {
  try {
    if (editingVehicle.value) {
      await transportService.updateVehicle({ id: editingVehicle.value.id, ...vehicleForm.value, isActive: true });
    } else {
      await transportService.createVehicle(vehicleForm.value);
    }
    await fetchVehicles();
    showVehicleModal.value = false;
    notificationStore.add('Araç kaydedildi.', 'success');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kaydetme başarısız.', 'error');
  }
};

const deleteVehicle = async (id: number) => {
  const ok = await notificationStore.promptConfirm({ title: 'Aracı Sil', message: 'Bu aracı silmek istediğinize emin misiniz?', confirmText: 'Sil', type: 'danger' });
  if (!ok) return;
  try {
    await transportService.deleteVehicle(id);
    fetchVehicles();
    notificationStore.add('Araç silindi.', 'success');
  } catch (e) { notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Silme başarısız.', 'error'); }
};

// ─── QR ─────────────────────────────────────────────────────────────────────

const qr_showModal = ref(false);
const qr_vehicle = ref<any>(null);
const qr_imageBase64 = ref<string | null>(null);
const qr_loading = ref(false);
const qr_typeNames = ['Kamyon', 'Kamyonet', 'Minibüs'];
const qr_vehicleTypeName = (type: number | undefined) => qr_typeNames[type ?? 0] ?? 'Kamyon';

const generateAndOpenQr = async (vehicle: any) => {
  qr_vehicle.value = vehicle;
  qr_imageBase64.value = null;
  qr_loading.value = true;
  qr_showModal.value = true;
  try {
    const res = await apiClient.post<{ qrCode: string; qrImageBase64: string }>(
      `/vehicles/${vehicle.id}/generate-qr`
    );
    vehicle.qrCode = res.data.qrCode;
    qr_imageBase64.value = res.data.qrImageBase64;
    notificationStore.add('QR kod oluşturuldu.', 'success');
    await fetchVehicles();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'QR oluşturulamadı.', 'error');
    qr_showModal.value = false;
  } finally {
    qr_loading.value = false;
  }
};

const openQrModal = async (vehicle: any) => {
  qr_vehicle.value = vehicle;
  qr_imageBase64.value = null;
  qr_loading.value = true;
  qr_showModal.value = true;
  try {
    const res = await apiClient.post<{ qrCode: string; qrImageBase64: string }>(
      `/vehicles/${vehicle.id}/generate-qr`
    );
    qr_imageBase64.value = res.data.qrImageBase64;
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'QR yüklenemedi.', 'error');
  } finally {
    qr_loading.value = false;
  }
};

const printQr = () => {
  const el = document.getElementById('vehicle-qr-print-area');
  if (el) el.style.display = 'flex';
  window.print();
  if (el) el.style.display = 'none';
};

// ─── Zones ──────────────────────────────────────────────────────────────────

interface Zone { id: number; name: string; order: number; }

const zone_zones = ref<Zone[]>([]);
const zone_error = ref<string | null>(null);
const zone_showModal = ref(false);
const zone_isEdit = ref(false);
const zone_form = ref({ name: '', order: 0 });
const zone_currentId = ref<number | null>(null);

const fetchZones = async () => {
  try {
    zone_zones.value = await projectService.getZones();
  } catch (err) {
    zone_error.value = ApiErrorUtils.getErrorMessage(err) || 'Bölgeler yüklenirken hata oluştu.';
    notificationStore.add(zone_error.value, 'error');
  }
};

const zone_openEdit = (zone: Zone) => {
  zone_isEdit.value = true;
  zone_currentId.value = zone.id;
  zone_form.value = { name: zone.name, order: zone.order };
  zone_showModal.value = true;
};

const zone_saveZone = async () => {
  if (!zone_form.value.name) return notificationStore.add('Bölge adı zorunludur', 'warning');
  try {
    if (zone_isEdit.value && zone_currentId.value) {
      await projectService.updateZone(zone_currentId.value, { id: zone_currentId.value, name: zone_form.value.name, order: zone_form.value.order });
    } else {
      await projectService.createZone({ name: zone_form.value.name, order: zone_form.value.order });
    }
    zone_showModal.value = false;
    fetchZones();
    notificationStore.add('Bölge kaydedildi.', 'success');
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Kaydetme hatası.', 'error');
  }
};

const zone_confirmDelete = async (zone: Zone) => {
  const ok = await notificationStore.promptConfirm({ title: 'Bölgeyi Sil', message: `"${zone.name}" bölgesini silmek istediğinize emin misiniz?`, confirmText: 'Sil', type: 'danger' });
  if (!ok) return;
  try {
    await projectService.deleteZone(zone.id);
    fetchZones();
    notificationStore.add('Bölge silindi.', 'success');
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Silme hatası.', 'error');
  }
};

// ─── Depot Settings ─────────────────────────────────────────────────────────

const depot_form = ref({
  depotName: null as string | null,
  depotAddress: null as string | null,
  depotLatitude: null as number | null,
  depotLongitude: null as number | null,
});
const depot_loading = ref(false);
const depot_saving = ref(false);
const depot_geocoding = ref(false);
const depot_geocodeError = ref('');
const depot_savedMsg = ref('');

const depot_load = async () => {
  depot_loading.value = true;
  try {
    const data = await systemSettingsService.getDepotSettings();
    depot_form.value = { ...data };
  } catch (e) {
    console.error(e);
  } finally {
    depot_loading.value = false;
  }
};

const depot_geocodeAddress = async () => {
  if (!depot_form.value.depotAddress) return;
  depot_geocoding.value = true;
  depot_geocodeError.value = '';
  try {
    const result = await systemSettingsService.geocodeAddress(depot_form.value.depotAddress);
    if (result) {
      depot_form.value.depotLatitude = result.lat;
      depot_form.value.depotLongitude = result.lng;
    } else {
      depot_geocodeError.value = 'Adres bulunamadı. Daha açık bir adres deneyin.';
    }
  } catch (e) {
    depot_geocodeError.value = 'Koordinat bulunamadı.';
  } finally {
    depot_geocoding.value = false;
  }
};

const depot_save = async () => {
  depot_saving.value = true;
  depot_savedMsg.value = '';
  try {
    await systemSettingsService.saveDepotSettings(depot_form.value);
    depot_savedMsg.value = 'Depo ayarları kaydedildi.';
    setTimeout(() => { depot_savedMsg.value = ''; }, 3000);
  } catch (e) {
    notificationStore.add('Kayıt başarısız.', 'error');
  } finally {
    depot_saving.value = false;
  }
};

// ─── PO Counter ──────────────────────────────────────────────────────────────

const poCounters = ref<PoCounterDto[]>([]);
const poCounterLoading = ref(false);
const poCounterSaving = ref(false);
const poCounterError = ref<string | null>(null);
const editingCounterId = ref<number | null>(null);
const editingCounterValue = ref(0);

async function loadPoCounters() {
  poCounterLoading.value = true;
  poCounterError.value = null;
  try {
    poCounters.value = await systemSettingsService.getPoCounters();
  } catch (e) {
    poCounterError.value = ApiErrorUtils.getErrorMessage(e) || 'Sayaçlar yüklenemedi.';
  } finally {
    poCounterLoading.value = false;
  }
}

function startEditCounter(counter: PoCounterDto) {
  editingCounterId.value = counter.id;
  editingCounterValue.value = counter.lastValue;
}

async function savePoCounter(counter: PoCounterDto) {
  poCounterSaving.value = true;
  try {
    const updated = await systemSettingsService.updatePoCounter(counter.id, editingCounterValue.value);
    const idx = poCounters.value.findIndex(c => c.id === counter.id);
    if (idx !== -1) poCounters.value[idx] = updated;
    editingCounterId.value = null;
    notificationStore.add(`Sayaç güncellendi. Sonraki sipariş numarası: ${updated.nextNumber}`, 'success');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Güncelleme başarısız.', 'error');
  } finally {
    poCounterSaving.value = false;
  }
}

// ─── Init ────────────────────────────────────────────────────────────────────

onMounted(() => {
  fetchDrivers();
  fetchVehicles();
  fetchZones();
  depot_load();
  loadPoCounters();
});
</script>
