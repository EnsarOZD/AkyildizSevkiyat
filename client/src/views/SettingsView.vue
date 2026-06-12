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
              ? 'border-blue-500 text-blue-600 dark:text-blue-400'
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
                    <button @click="openDriverModal(driver)" class="text-blue-600 hover:text-blue-900 mr-4">Düzenle</button>
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
                        'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300': vehicle.vehicleType === 0 || vehicle.vehicleType === 2,
                        'bg-violet-100 text-violet-800 dark:bg-violet-900/30 dark:text-violet-300': vehicle.vehicleType === 1,
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
                    <button @click="openVehicleModal(vehicle)" class="text-blue-600 hover:text-blue-900">Düzenle</button>
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
          @click="zone_showModal = true; zone_isEdit = false; zone_form = { name: '', isOutOfCity: false }"
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
                <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Bölge Adı</th>
                <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
              </tr>
            </thead>
            <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
              <tr v-for="zone in zone_zones" :key="zone.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
                <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">{{ zone.name }}</td>
                <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                  <button @click="zone_openEdit(zone)" class="text-blue-600 hover:text-blue-900 mr-4">Düzenle</button>
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
          <label class="flex items-center gap-3 cursor-pointer select-none">
            <input type="checkbox" v-model="zone_form.isOutOfCity" class="w-4 h-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500" />
            <span class="text-sm font-medium text-gray-700 dark:text-gray-300">Şehir Dışı Bölge</span>
            <span class="text-xs text-gray-400 dark:text-gray-500">(Micro/Macro yerine tek toplu hazırlık)</span>
          </label>
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
            class="w-full border dark:border-gray-700 rounded-lg px-3 py-2 dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:outline-none"
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Adres</label>
          <div class="flex gap-2">
            <input
              v-model="depot_form.depotAddress"
              type="text"
              placeholder="Depo adresi"
              class="flex-1 border dark:border-gray-700 rounded-lg px-3 py-2 dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:outline-none"
            />
            <button
              @click="depot_geocodeAddress"
              :disabled="!depot_form.depotAddress || depot_geocoding"
              class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed text-sm whitespace-nowrap flex items-center gap-2"
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
              class="w-full border dark:border-gray-700 rounded-lg px-3 py-2 dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:outline-none"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Boylam (Longitude)</label>
            <input
              v-model.number="depot_form.depotLongitude"
              type="number"
              step="0.000001"
              placeholder="28.9784"
              class="w-full border dark:border-gray-700 rounded-lg px-3 py-2 dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:outline-none"
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
            class="px-5 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 font-medium flex items-center gap-2"
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
          class="inline-flex items-center px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 font-medium"
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
            class="text-sm text-blue-600 hover:text-blue-800 dark:text-blue-400 underline">
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
                    class="w-28 px-2 py-1 border border-blue-400 rounded text-sm font-mono focus:outline-none focus:ring-2 focus:ring-blue-500 dark:bg-gray-800 dark:text-gray-200"
                  />
                  <span v-else class="font-mono text-gray-600 dark:text-gray-400">{{ counter.lastValue }}</span>
                </td>
                <td class="py-3 px-3">
                  <div v-if="editingCounterId === counter.id" class="flex gap-2">
                    <button @click="savePoCounter(counter)" :disabled="poCounterSaving"
                      class="px-3 py-1 bg-blue-600 text-white rounded text-xs hover:bg-blue-700 disabled:opacity-50">
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

    <!-- ===== TAB: E-posta Ayarları ===== -->
    <div v-show="activeTab === 'email'" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-6 space-y-6 max-w-2xl">

        <div class="pb-4 border-b border-gray-200 dark:border-gray-700">
          <h2 class="text-base font-semibold text-gray-800 dark:text-gray-200">E-posta CC Adresleri</h2>
          <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
            Sistematik olarak gönderilen e-postalara CC olarak eklenecek adresleri tanımlayın. Birden fazla adres için virgülle ayırın.
          </p>
        </div>

        <!-- Satınalma CC -->
        <div class="space-y-2">
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">
            Satınalma Siparişi CC Adresleri
            <span class="ml-1 text-xs text-gray-400 font-normal">(procurement@)</span>
          </label>
          <textarea
            v-model="email_form.procurementEmailCc"
            rows="3"
            placeholder="ornek@sirket.com, baska@sirket.com"
            class="block w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
          ></textarea>
          <p class="text-xs text-gray-400 dark:text-gray-500">
            Tedarikçiye gönderilen satınalma sipariş e-postalarına bu adresler CC olarak eklenir.
          </p>
        </div>

        <!-- Sevkiyat / Eksik Bildirim CC -->
        <div class="space-y-2">
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">
            Sevkiyat Bildirim CC Adresleri
            <span class="ml-1 text-xs text-gray-400 font-normal">(dispatch@)</span>
          </label>
          <textarea
            v-model="email_form.dispatchEmailCc"
            rows="3"
            placeholder="ornek@sirket.com, baska@sirket.com"
            class="block w-full border border-gray-300 dark:border-gray-600 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
          ></textarea>
          <p class="text-xs text-gray-400 dark:text-gray-500">
            Eksik ürün ve kısmi gönderim bildirim e-postalarına bu adresler CC olarak eklenir.
          </p>
        </div>

        <!-- Şoför Atama E-posta Aktif/Pasif -->
        <div class="flex items-center justify-between py-3 border-t border-gray-100 dark:border-gray-700">
          <div>
            <p class="text-sm font-medium text-gray-700 dark:text-gray-300">Şoför Atama Bildirimi</p>
            <p class="text-xs text-gray-400 dark:text-gray-500 mt-0.5">
              Şoföre atama yapıldığında yönetici mail adreslerine otomatik e-posta gönderilsin.
            </p>
          </div>
          <button
            type="button"
            @click="email_form.dispatchEmailEnabled = !email_form.dispatchEmailEnabled"
            :class="email_form.dispatchEmailEnabled
              ? 'bg-blue-600'
              : 'bg-gray-200 dark:bg-gray-700'"
            class="relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
            :aria-checked="email_form.dispatchEmailEnabled"
            role="switch"
          >
            <span
              :class="email_form.dispatchEmailEnabled ? 'translate-x-5' : 'translate-x-0'"
              class="pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200"
            />
          </button>
        </div>

        <div v-if="email_error" class="p-3 bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-700 rounded text-red-700 dark:text-red-300 text-sm">
          {{ email_error }}
        </div>

        <div class="flex items-center gap-3">
          <button
            @click="email_save"
            :disabled="email_saving"
            class="px-5 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 font-medium flex items-center gap-2"
          >
            <svg v-if="email_saving" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
            </svg>
            <span>{{ email_saving ? 'Kaydediliyor...' : 'Kaydet' }}</span>
          </button>
          <span v-if="email_savedMsg" class="text-sm text-green-600 dark:text-green-400 font-medium">✓ {{ email_savedMsg }}</span>
        </div>
      </div>
    </div>

    <!-- ===== TAB: Depo Yönetimi (WMS) ===== -->
    <div v-show="activeTab === 'wms'" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-6 space-y-6 max-w-2xl">

        <div class="pb-4 border-b border-gray-200 dark:border-gray-700">
          <h2 class="text-base font-semibold text-gray-800 dark:text-gray-200">Depo Yönetimi (WMS) Ayarları</h2>
          <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
            Bu ayarlar kapalıyken mevcut depo akışı değişmez. Barkod ve lokasyon özelliklerini hazır olduğunuzda etkinleştirin.
          </p>
        </div>

        <!-- Putaway Toggle -->
        <div class="flex items-start justify-between gap-4">
          <div class="flex-1">
            <p class="text-sm font-medium text-gray-800 dark:text-gray-200">Mal Kabul Dağıtımı (Putaway)</p>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
              Mal kabul onaylandıktan sonra ürünleri raf ve toplama gözlerine dağıtma ekranını etkinleştirir.
            </p>
          </div>
          <button
            @click="wms_form.wmsPutawayEnabled = !wms_form.wmsPutawayEnabled"
            :class="[
              wms_form.wmsPutawayEnabled
                ? 'bg-blue-600'
                : 'bg-gray-200 dark:bg-gray-700',
              'relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2'
            ]"
            role="switch"
            :aria-checked="wms_form.wmsPutawayEnabled"
          >
            <span
              :class="[
                wms_form.wmsPutawayEnabled ? 'translate-x-5' : 'translate-x-0',
                'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200'
              ]"
            ></span>
          </button>
        </div>

        <!-- Location Picking Toggle -->
        <div class="flex items-start justify-between gap-4">
          <div class="flex-1">
            <p class="text-sm font-medium text-gray-800 dark:text-gray-200">Lokasyon Bazlı Toplama</p>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
              Picking sırasında sistem raf/toplama gözü önerisi yapar ve stok düşümü lokasyon bazında gerçekleşir.
              Kapalıyken yalnızca ürün bazında düşüm yapılır (mevcut davranış).
            </p>
          </div>
          <button
            @click="wms_form.wmsLocationPickingEnabled = !wms_form.wmsLocationPickingEnabled"
            :class="[
              wms_form.wmsLocationPickingEnabled
                ? 'bg-blue-600'
                : 'bg-gray-200 dark:bg-gray-700',
              'relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2'
            ]"
            role="switch"
            :aria-checked="wms_form.wmsLocationPickingEnabled"
          >
            <span
              :class="[
                wms_form.wmsLocationPickingEnabled ? 'translate-x-5' : 'translate-x-0',
                'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200'
              ]"
            ></span>
          </button>
        </div>

        <!-- Barcode Picking Toggle -->
        <div class="flex items-start justify-between gap-4">
          <div class="flex-1">
            <p class="text-sm font-medium text-gray-800 dark:text-gray-200">Barkodlu Toplama</p>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
              Ürün toplarken barkod tarama zorunlu olur. Kapalıyken manuel miktar girişiyle toplama yapılabilir (mevcut davranış).
            </p>
          </div>
          <button
            @click="wms_form.wmsBarcodePickingEnabled = !wms_form.wmsBarcodePickingEnabled"
            :class="[
              wms_form.wmsBarcodePickingEnabled
                ? 'bg-blue-600'
                : 'bg-gray-200 dark:bg-gray-700',
              'relative inline-flex h-6 w-11 flex-shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2'
            ]"
            role="switch"
            :aria-checked="wms_form.wmsBarcodePickingEnabled"
          >
            <span
              :class="[
                wms_form.wmsBarcodePickingEnabled ? 'translate-x-5' : 'translate-x-0',
                'pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow ring-0 transition duration-200'
              ]"
            ></span>
          </button>
        </div>

        <div v-if="wms_error" class="p-3 bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-700 rounded text-red-700 dark:text-red-300 text-sm">
          {{ wms_error }}
        </div>

        <div class="flex items-center gap-3">
          <button
            @click="wms_save"
            :disabled="wms_saving"
            class="px-5 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50 font-medium flex items-center gap-2"
          >
            <svg v-if="wms_saving" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
            </svg>
            <span>{{ wms_saving ? 'Kaydediliyor...' : 'Kaydet' }}</span>
          </button>
          <span v-if="wms_savedMsg" class="text-sm text-green-600 dark:text-green-400 font-medium">✓ {{ wms_savedMsg }}</span>
        </div>
      </div>
    </div>

    <!-- ===== TAB: Güvenlik ===== -->
    <div v-show="activeTab === 'security'" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-6">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-lg font-semibold text-gray-800 dark:text-gray-200">Giriş Kilitleri</h2>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">5 başarısız girişten sonra IP adresi 5 dakika kilitlenir. Sahadaki kullanıcılar için kilidi buradan kaldırabilirsiniz.</p>
          </div>
          <button @click="loadLoginBlocks" :disabled="securityLoading"
            class="text-sm text-blue-600 hover:text-blue-800 dark:text-blue-400 underline">
            Yenile
          </button>
        </div>

        <div v-if="securityLoading" class="text-sm text-gray-500">Yükleniyor...</div>

        <div v-else-if="blockedIps.length === 0"
          class="text-center py-10 text-gray-400 dark:text-gray-600 text-sm">
          Şu anda kilitli IP adresi yok.
        </div>

        <div v-else class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-gray-200 dark:border-gray-700 text-gray-500 dark:text-gray-400 text-xs uppercase">
                <th class="py-2 px-3 text-left">IP Adresi</th>
                <th class="py-2 px-3 text-left">Deneme</th>
                <th class="py-2 px-3 text-left">Kilit Bitiş</th>
                <th class="py-2 px-3 text-left"></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="block in blockedIps" :key="block.ip"
                class="border-b border-gray-100 dark:border-gray-800">
                <td class="py-3 px-3 font-mono text-gray-800 dark:text-gray-200">{{ block.ip }}</td>
                <td class="py-3 px-3 text-red-600 dark:text-red-400 font-semibold">{{ block.attempts }}</td>
                <td class="py-3 px-3 text-gray-600 dark:text-gray-400">{{ formatBlockedUntil(block.blockedUntil) }}</td>
                <td class="py-3 px-3">
                  <button @click="resetLoginBlock(block.ip)" :disabled="securityResetting === block.ip"
                    class="px-3 py-1 bg-green-600 hover:bg-green-700 text-white rounded text-xs disabled:opacity-50">
                    {{ securityResetting === block.ip ? '...' : 'Kilidi Kaldır' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- ===== TAB: Yazıcı Yönetimi ===== -->
    <div v-show="activeTab === 'printers'" class="space-y-6">

      <!-- Agent'lar -->
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-6">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-lg font-semibold text-gray-800 dark:text-gray-200">Bağlı Agent'lar</h2>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">Depo bilgisayarlarında çalışan yerel baskı servisleri.</p>
          </div>
          <button @click="loadPrinters" class="text-sm text-blue-600 dark:text-blue-400 underline">Yenile</button>
        </div>
        <div v-if="printer_loading" class="text-sm text-gray-400 py-4">Yükleniyor...</div>
        <div v-else-if="printer_agents.length === 0"
          class="text-center py-8 text-sm text-gray-400 dark:text-gray-600">
          Henüz bağlı agent yok. AkyildizKargoEtiket servisini depo bilgisayarında başlatın.
        </div>
        <div v-else class="space-y-3">
          <div v-for="agent in printer_agents" :key="agent.id"
            class="flex items-start justify-between p-4 rounded-lg border"
            :class="agent.isOnline
              ? 'border-green-200 dark:border-green-800 bg-green-50 dark:bg-green-900/20'
              : 'border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800/50'">
            <div class="flex items-center gap-3">
              <span class="w-2.5 h-2.5 rounded-full flex-shrink-0"
                :class="agent.isOnline ? 'bg-green-500' : 'bg-gray-400'"></span>
              <div>
                <p class="text-sm font-semibold text-gray-800 dark:text-gray-200">{{ agent.displayName }}</p>
                <p class="text-xs text-gray-500 dark:text-gray-400">{{ agent.machineName }}</p>
                <div v-if="agent.installedPrinters?.length" class="mt-1 flex flex-wrap gap-1">
                  <span v-for="p in agent.installedPrinters" :key="p"
                    class="text-xs bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 px-2 py-0.5 rounded">
                    {{ p }}
                  </span>
                </div>
              </div>
            </div>
            <div class="text-right flex-shrink-0 ml-4 text-xs text-gray-500 dark:text-gray-400">
              <span :class="agent.isOnline ? 'text-green-600 dark:text-green-400 font-semibold' : ''">
                {{ agent.isOnline ? 'Çevrimiçi' : 'Çevrimdışı' }}
              </span>
              <p class="mt-1">Son: {{ formatRunAt(agent.lastSeenAt) }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Yazıcı Konfigürasyonları -->
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-6">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-lg font-semibold text-gray-800 dark:text-gray-200">Yazıcı Konfigürasyonları</h2>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">Hangi etiket tipinin hangi yazıcıdan çıkacağını tanımlayın.</p>
          </div>
          <button @click="openPrinterModal()"
            class="px-3 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-semibold transition-colors">
            + Yazıcı Ekle
          </button>
        </div>
        <div v-if="printer_configs.length === 0"
          class="text-center py-8 text-sm text-gray-400 dark:text-gray-600">
          Henüz tanımlı yazıcı yok.
        </div>
        <div v-else class="space-y-3">
          <div v-for="cfg in printer_configs" :key="cfg.id"
            class="flex items-center justify-between p-4 rounded-lg border border-gray-200 dark:border-gray-700">
            <div class="flex items-center gap-3">
              <div class="p-2 rounded-lg"
                :class="cfg.labelType === 0 ? 'bg-blue-50 dark:bg-blue-900/30' : 'bg-amber-50 dark:bg-amber-900/30'">
                <svg class="w-5 h-5" :class="cfg.labelType === 0 ? 'text-blue-600' : 'text-amber-600'"
                  fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                    d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
                </svg>
              </div>
              <div>
                <div class="flex items-center gap-2">
                  <p class="text-sm font-semibold text-gray-800 dark:text-gray-200">{{ cfg.name }}</p>
                  <span v-if="cfg.isDefault"
                    class="text-xs bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 px-1.5 py-0.5 rounded">
                    Varsayılan
                  </span>
                </div>
                <p class="text-xs text-gray-500 dark:text-gray-400">
                  {{ cfg.labelTypeName }} · {{ cfg.windowsPrinterName }}
                </p>
                <p class="text-xs mt-0.5"
                  :class="cfg.agentOnline ? 'text-green-600 dark:text-green-400' : 'text-gray-400'">
                  {{ cfg.agentDisplayName ?? 'Agent atanmadı' }}
                  <span v-if="cfg.agentDisplayName">
                    ({{ cfg.agentOnline ? 'çevrimiçi' : 'çevrimdışı' }})
                  </span>
                </p>
              </div>
            </div>
            <div class="flex items-center gap-2">
              <button @click="testPrint(cfg.id)"
                class="px-2 py-1 text-xs font-semibold text-gray-600 dark:text-gray-400 border border-gray-300 dark:border-gray-600 rounded hover:bg-gray-100 dark:hover:bg-gray-800">
                Test
              </button>
              <button @click="openPrinterModal(cfg)"
                class="px-2 py-1 text-xs font-semibold text-blue-600 dark:text-blue-400 border border-blue-300 dark:border-blue-700 rounded hover:bg-blue-50 dark:hover:bg-blue-900/30">
                Düzenle
              </button>
              <button @click="deletePrinterConfig(cfg.id)"
                class="px-2 py-1 text-xs font-semibold text-red-600 dark:text-red-400 border border-red-300 dark:border-red-700 rounded hover:bg-red-50 dark:hover:bg-red-900/30">
                Sil
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Son Baskılar -->
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-6">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-lg font-semibold text-gray-800 dark:text-gray-200">Son Baskılar</h2>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">Son 20 baskı işi ve sonuçları.</p>
          </div>
          <button @click="loadPrintJobs" class="text-sm text-blue-600 dark:text-blue-400 underline">Yenile</button>
        </div>
        <div v-if="print_jobs_loading" class="text-sm text-gray-400 py-4">Yükleniyor...</div>
        <div v-else-if="print_jobs.length === 0" class="text-center py-6 text-sm text-gray-400 dark:text-gray-600">
          Henüz baskı kaydı yok.
        </div>
        <div v-else class="overflow-x-auto">
          <table class="w-full text-xs">
            <thead>
              <tr class="text-left text-gray-500 dark:text-gray-400 border-b border-gray-200 dark:border-gray-700">
                <th class="pb-2 pr-3">#</th>
                <th class="pb-2 pr-3">Yazıcı</th>
                <th class="pb-2 pr-3">Durum</th>
                <th class="pb-2 pr-3">Hata</th>
                <th class="pb-2">Zaman</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
              <tr v-for="job in print_jobs" :key="job.id" class="py-1">
                <td class="py-1.5 pr-3 text-gray-400">{{ job.id }}</td>
                <td class="py-1.5 pr-3 text-gray-700 dark:text-gray-300">{{ job.printerName }}</td>
                <td class="py-1.5 pr-3">
                  <span class="px-1.5 py-0.5 rounded text-xs font-semibold"
                    :class="{
                      'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400': job.status === 0,
                      'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400': job.status === 1,
                      'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400': job.status === 2,
                      'bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400': job.status === 3,
                    }">
                    {{ ['Bekliyor','Yazdırılıyor','Tamamlandı','Hata'][job.status] ?? job.statusName }}
                  </span>
                </td>
                <td class="py-1.5 pr-3 text-red-600 dark:text-red-400 max-w-xs truncate" :title="job.errorMessage ?? ''">
                  {{ job.errorMessage ?? '' }}
                </td>
                <td class="py-1.5 text-gray-400 whitespace-nowrap">{{ formatRunAt(job.createdAt) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Yazıcı Modal -->
      <BaseModal :show="showPrinterModal" :title="printerModalEdit ? 'Yazıcı Düzenle' : 'Yazıcı Ekle'"
        @close="showPrinterModal = false">
        <div class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Ad</label>
            <input v-model="printerForm.name" type="text" placeholder="ör. Kargo Etiketi Yazıcısı"
              class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm text-gray-900 dark:text-gray-100" />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Etiket Tipi</label>
            <select v-model="printerForm.labelType"
              class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm text-gray-900 dark:text-gray-100">
              <option :value="0">Kargo Etiketi</option>
              <option :value="1">Koli Etiketi</option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Agent</label>
            <select v-model="printerForm.agentId"
              class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm text-gray-900 dark:text-gray-100">
              <option :value="null">Agent seçin</option>
              <option v-for="a in printer_agents" :key="a.id" :value="a.id">
                {{ a.displayName }} ({{ a.machineName }})
              </option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Windows Yazıcı Adı</label>
            <div v-if="selectedAgentPrinters.length > 0">
              <select v-model="printerForm.windowsPrinterName"
                class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm text-gray-900 dark:text-gray-100">
                <option value="">Yazıcı seçin</option>
                <option v-for="p in selectedAgentPrinters" :key="p" :value="p">{{ p }}</option>
              </select>
            </div>
            <input v-else v-model="printerForm.windowsPrinterName" type="text"
              placeholder="ör. ZDesigner LP 2844"
              class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm text-gray-900 dark:text-gray-100" />
            <p class="text-xs text-gray-400 mt-1">Agent çevrimiçiyse kurulu yazıcıları otomatik listelenir.</p>
          </div>
          <div class="flex items-center gap-2">
            <input id="isDefault" v-model="printerForm.isDefault" type="checkbox"
              class="rounded border-gray-300 text-blue-600" />
            <label for="isDefault" class="text-sm text-gray-700 dark:text-gray-300">
              Bu etiket tipi için varsayılan yazıcı
            </label>
          </div>
        </div>
        <template #footer>
          <button @click="showPrinterModal = false"
            class="px-4 py-2 text-sm font-semibold text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-700 rounded-lg hover:bg-gray-200">
            İptal
          </button>
          <button @click="savePrinterConfig" :disabled="printer_saving"
            class="px-4 py-2 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg disabled:opacity-50">
            {{ printer_saving ? 'Kaydediliyor...' : 'Kaydet' }}
          </button>
        </template>
      </BaseModal>

    </div>

    <!-- ===== TAB: Sistem Sağlığı (Admin) ===== -->
    <div v-show="activeTab === 'system'" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-6">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-lg font-semibold text-gray-800 dark:text-gray-200">Arkaplan Servisleri</h2>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">Her servisin son çalışma zamanı ve sonucu. Uygulama başladıktan sonra ilk çalışmaya kadar kayıt görünmez.</p>
          </div>
          <button @click="loadSystemHealth" :disabled="system_loading"
            class="text-sm text-blue-600 hover:text-blue-800 dark:text-blue-400 underline disabled:opacity-50">
            Yenile
          </button>
        </div>

        <div v-if="system_loading" class="text-sm text-gray-500 dark:text-gray-400">Yükleniyor...</div>

        <div v-else-if="system_error" class="p-3 bg-red-900/30 border border-red-700 rounded-lg text-red-400 text-sm">
          {{ system_error }}
        </div>

        <div v-else-if="system_services.length === 0"
          class="text-center py-10 text-gray-400 dark:text-gray-600 text-sm">
          Henüz çalışma kaydı yok. Servisler periyodik aralıkta çalıştıktan sonra burada görünür.
        </div>

        <div v-else class="space-y-3">
          <div v-for="svc in system_services" :key="svc.name"
            class="flex items-start justify-between p-4 rounded-lg border"
            :class="svc.result === 'Success'
              ? 'border-green-200 dark:border-green-800 bg-green-50 dark:bg-green-900/20'
              : 'border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/20'">
            <div class="flex items-center gap-3">
              <span class="w-2.5 h-2.5 rounded-full flex-shrink-0 mt-0.5"
                :class="svc.result === 'Success' ? 'bg-green-500' : 'bg-red-500'"></span>
              <div>
                <p class="text-sm font-medium text-gray-800 dark:text-gray-200">
                  {{ serviceLabels[svc.name] ?? svc.name }}
                </p>
                <p v-if="svc.errorMessage" class="text-xs text-red-600 dark:text-red-400 mt-0.5 font-mono">
                  {{ svc.errorMessage }}
                </p>
              </div>
            </div>
            <div class="text-right flex-shrink-0 ml-4">
              <span class="text-xs font-semibold px-2 py-0.5 rounded-full"
                :class="svc.result === 'Success'
                  ? 'bg-green-100 dark:bg-green-800 text-green-700 dark:text-green-300'
                  : 'bg-red-100 dark:bg-red-800 text-red-700 dark:text-red-300'">
                {{ svc.result === 'Success' ? 'Başarılı' : 'Hatalı' }}
              </span>
              <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">{{ formatRunAt(svc.runAt) }}</p>
            </div>
          </div>
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
import { ref, computed, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import transportService from '../services/transportService';
import apiClient from '../services/apiClient';
import projectService from '../services/projectService';
import systemSettingsService, { type PoCounterDto, type EmailSettingsDto, type WmsSettingsDto } from '../services/systemSettingsService';
import { healthService, type BackgroundServiceStatus } from '../services/healthService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import { useAuthStore } from '../stores/auth';
import BaseModal from '../components/BaseModal.vue';

const notificationStore = useNotificationStore();
const authStore = useAuthStore();

const tabs = computed(() => {
  const base = [
    { key: 'transport',   label: 'Şoför & Araç' },
    { key: 'zones',       label: 'Bölge Yönetimi' },
    { key: 'depot',       label: 'Depo Tanımları' },
    { key: 'locations',   label: 'Depo Adresleri' },
    { key: 'po_counter',  label: 'Sipariş Sayaçları' },
    { key: 'email',       label: 'E-posta Ayarları' },
    { key: 'wms',         label: 'Depo Yönetimi' },
    { key: 'security',    label: 'Güvenlik' },
    { key: 'printers',    label: 'Yazıcı Yönetimi' },
  ];
  if (authStore.userRole === 'Admin') {
    base.push({ key: 'system', label: 'Sistem Sağlığı' });
  }
  return base;
});
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

interface Zone { id: number; name: string; isOutOfCity: boolean; }

const zone_zones = ref<Zone[]>([]);
const zone_error = ref<string | null>(null);
const zone_showModal = ref(false);
const zone_isEdit = ref(false);
const zone_form = ref({ name: '', isOutOfCity: false });
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
  zone_form.value = { name: zone.name, isOutOfCity: zone.isOutOfCity };
  zone_showModal.value = true;
};

const zone_saveZone = async () => {
  if (!zone_form.value.name) return notificationStore.add('Bölge adı zorunludur', 'warning');
  try {
    if (zone_isEdit.value && zone_currentId.value) {
      await projectService.updateZone(zone_currentId.value, { id: zone_currentId.value, name: zone_form.value.name, order: 0, isOutOfCity: zone_form.value.isOutOfCity });
    } else {
      await projectService.createZone({ name: zone_form.value.name, order: 0, isOutOfCity: zone_form.value.isOutOfCity });
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

// ─── Email Settings ──────────────────────────────────────────────────────────

const email_form = ref<EmailSettingsDto>({ procurementEmailCc: null, dispatchEmailCc: null, dispatchEmailEnabled: true });
const email_saving = ref(false);
const email_error = ref<string | null>(null);
const email_savedMsg = ref('');

async function email_load() {
  try {
    email_form.value = await systemSettingsService.getEmailSettings();
  } catch (e) {
    // sessizce geç — sekme açılmadığında hata göstermemek için
  }
}

async function email_save() {
  email_saving.value = true;
  email_error.value = null;
  email_savedMsg.value = '';
  try {
    email_form.value = await systemSettingsService.saveEmailSettings(email_form.value);
    email_savedMsg.value = 'E-posta ayarları kaydedildi.';
    setTimeout(() => { email_savedMsg.value = ''; }, 3000);
  } catch (e) {
    email_error.value = ApiErrorUtils.getErrorMessage(e) || 'Kayıt başarısız.';
  } finally {
    email_saving.value = false;
  }
}

// ─── WMS Ayarları ───────────────────────────────────────────────────────────

const wms_form = ref<WmsSettingsDto>({
  wmsPutawayEnabled: false,
  wmsLocationPickingEnabled: false,
  wmsBarcodePickingEnabled: false,
});
const wms_saving = ref(false);
const wms_error = ref<string | null>(null);
const wms_savedMsg = ref('');

async function wms_load() {
  try {
    wms_form.value = await systemSettingsService.getWmsSettings();
  } catch {
    // sessizce geç — sekme açılmadığında hata göstermemek için
  }
}

async function wms_save() {
  wms_saving.value = true;
  wms_error.value = null;
  wms_savedMsg.value = '';
  try {
    wms_form.value = await systemSettingsService.saveWmsSettings(wms_form.value);
    wms_savedMsg.value = 'WMS ayarları kaydedildi.';
    setTimeout(() => { wms_savedMsg.value = ''; }, 3000);
  } catch (e) {
    wms_error.value = ApiErrorUtils.getErrorMessage(e) || 'Kayıt başarısız.';
  } finally {
    wms_saving.value = false;
  }
}

// ─── Güvenlik / Login Kilitleri ─────────────────────────────────────────────

const securityLoading = ref(false);
const securityResetting = ref<string | null>(null);
const blockedIps = ref<{ ip: string; attempts: number; blockedUntil: string }[]>([]);

async function loadLoginBlocks() {
  securityLoading.value = true;
  try {
    const res = await apiClient.get('/auth/login-blocks');
    blockedIps.value = res.data;
  } catch {
    // sessizce geç — sekme açılmadığında hata göstermemek için
  } finally {
    securityLoading.value = false;
  }
}

async function resetLoginBlock(ip: string) {
  securityResetting.value = ip;
  try {
    await apiClient.post('/auth/reset-login-block', { ip });
    blockedIps.value = blockedIps.value.filter(b => b.ip !== ip);
    notificationStore.add(`${ip} adresinin kilidi kaldırıldı.`, 'success');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
  } finally {
    securityResetting.value = null;
  }
}

function formatBlockedUntil(iso: string) {
  const d = new Date(iso);
  return d.toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit', second: '2-digit' });
}

// ─── Sistem Sağlığı (Admin) ─────────────────────────────────────────────────

const system_loading = ref(false);
const system_services = ref<BackgroundServiceStatus[]>([]);
const system_error = ref<string | null>(null);

const serviceLabels: Record<string, string> = {
  'iss-import': 'ISS-IP Sipariş İmport',
  'yk-sync': 'Yurtiçi Kargo Durum Sync',
  'netsis-irsaliye': 'Netsis İrsaliye Sync',
};

async function loadSystemHealth() {
  system_loading.value = true;
  system_error.value = null;
  try {
    system_services.value = await healthService.getBackgroundServices();
  } catch (e) {
    system_error.value = ApiErrorUtils.getErrorMessage(e) || 'Servis durumu yüklenemedi.';
  } finally {
    system_loading.value = false;
  }
}

function formatRunAt(runAt: string): string {
  const d = new Date(runAt);
  const diffMs = Date.now() - d.getTime();
  const diffMin = Math.floor(diffMs / 60000);
  if (diffMin < 1) return 'Az önce';
  if (diffMin < 60) return `${diffMin} dakika önce`;
  const diffH = Math.floor(diffMin / 60);
  if (diffH < 24) return `${diffH} saat önce`;
  return `${Math.floor(diffH / 24)} gün önce`;
}

onMounted(() => {
  fetchDrivers();
  fetchVehicles();
  fetchZones();
  depot_load();
  loadPoCounters();
  email_load();
  wms_load();
  loadLoginBlocks();
  if (authStore.userRole === 'Admin') loadSystemHealth();
  loadPrinters();
  loadPrintJobs();
});

// ─── Yazıcı Yönetimi ────────────────────────────────────────────────────────

const printer_loading = ref(false);
const printer_saving  = ref(false);
const printer_agents  = ref<any[]>([]);
const printer_configs = ref<any[]>([]);
const showPrinterModal  = ref(false);
const printerModalEdit  = ref<any | null>(null);
const printerForm = ref({
  name: '',
  labelType: 0,
  windowsPrinterName: '',
  agentId: null as number | null,
  isDefault: false,
});

const selectedAgentPrinters = computed(() => {
  if (!printerForm.value.agentId) return [];
  const agent = printer_agents.value.find(a => a.id === printerForm.value.agentId);
  return agent?.installedPrinters ?? [];
});

async function loadPrinters() {
  printer_loading.value = true;
  try {
    const [agents, configs] = await Promise.all([
      apiClient.get('/print/agents').then(r => r.data),
      apiClient.get('/print/printer-configs').then(r => r.data),
    ]);
    printer_agents.value  = agents;
    printer_configs.value = configs;
  } catch {
    // sessizce geç
  } finally {
    printer_loading.value = false;
  }
}

const print_jobs         = ref<any[]>([]);
const print_jobs_loading = ref(false);

async function loadPrintJobs() {
  print_jobs_loading.value = true;
  try {
    const res = await apiClient.get('/print/jobs?page=1&pageSize=20');
    print_jobs.value = res.data.items ?? [];
  } catch {
    // sessizce geç
  } finally {
    print_jobs_loading.value = false;
  }
}

function openPrinterModal(cfg?: any) {
  printerModalEdit.value = cfg ?? null;
  printerForm.value = cfg
    ? { name: cfg.name, labelType: cfg.labelType, windowsPrinterName: cfg.windowsPrinterName, agentId: cfg.agentId ?? null, isDefault: cfg.isDefault }
    : { name: '', labelType: 0, windowsPrinterName: '', agentId: null, isDefault: false };
  showPrinterModal.value = true;
}

async function savePrinterConfig() {
  if (!printerForm.value.name || !printerForm.value.windowsPrinterName) {
    notificationStore.add('Ad ve yazıcı adı zorunludur.', 'error');
    return;
  }
  printer_saving.value = true;
  try {
    if (printerModalEdit.value) {
      await apiClient.put(`/print/printer-configs/${printerModalEdit.value.id}`, printerForm.value);
    } else {
      await apiClient.post('/print/printer-configs', printerForm.value);
    }
    showPrinterModal.value = false;
    notificationStore.add('Yazıcı kaydedildi.', 'success');
    await loadPrinters();
  } catch {
    notificationStore.add('Kayıt sırasında hata oluştu.', 'error');
  } finally {
    printer_saving.value = false;
  }
}

async function deletePrinterConfig(id: number) {
  if (!confirm('Bu yazıcıyı silmek istediğinize emin misiniz?')) return;
  try {
    await apiClient.delete(`/print/printer-configs/${id}`);
    notificationStore.add('Yazıcı silindi.', 'success');
    await loadPrinters();
  } catch {
    notificationStore.add('Silme sırasında hata oluştu.', 'error');
  }
}

async function testPrint(id: number) {
  try {
    await apiClient.post(`/print/printer-configs/${id}/test`);
    notificationStore.add('Test baskısı kuyruğa alındı.', 'success');
  } catch {
    notificationStore.add('Test baskısı gönderilemedi.', 'error');
  }
}
</script>
