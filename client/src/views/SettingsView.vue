<template>
  <div class="space-y-6">
    <div class="flex justify-between items-center">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Tanımlamalar</h1>
    </div>

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
                  <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                    <button @click="openVehicleModal(vehicle)" class="text-indigo-600 hover:text-indigo-900 mr-4">Düzenle</button>
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
      <Teleport to="body">
        <div v-if="showDriverModal" class="fixed inset-0 bg-gray-500 bg-opacity-75 flex items-center justify-center z-50 p-4">
          <div class="bg-white dark:bg-gray-900 rounded-lg p-6 max-w-sm w-full">
            <h3 class="text-lg font-medium mb-4 dark:text-gray-100">{{ editingDriver ? 'Şoför Düzenle' : 'Yeni Şoför Ekle' }}</h3>
            <div class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Ad Soyad</label>
                <input v-model="driverForm.fullName" type="text" class="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Telefon</label>
                <input v-model="driverForm.phone" type="text" class="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
              </div>
            </div>
            <div class="mt-5 flex justify-end gap-2">
              <button @click="showDriverModal = false" class="px-4 py-2 border dark:border-gray-700 rounded text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800">İptal</button>
              <button @click="saveDriver" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">Kaydet</button>
            </div>
          </div>
        </div>
      </Teleport>

      <!-- Vehicle Modal -->
      <Teleport to="body">
        <div v-if="showVehicleModal" class="fixed inset-0 bg-gray-500 bg-opacity-75 flex items-center justify-center z-50 p-4">
          <div class="bg-white dark:bg-gray-900 rounded-lg p-6 max-w-sm w-full">
            <h3 class="text-lg font-medium mb-4 dark:text-gray-100">{{ editingVehicle ? 'Araç Düzenle' : 'Yeni Araç Ekle' }}</h3>
            <div class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Plaka</label>
                <input v-model="vehicleForm.plateNumber" type="text" class="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 uppercase dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Araç Tipi</label>
                <select v-model="vehicleForm.vehicleType" class="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
                  <option :value="0">Kamyon</option>
                  <option :value="1">Kamyonet</option>
                  <option :value="2">Minibüs</option>
                </select>
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Kapasite / Notlar <span class="text-gray-400 font-normal">(opsiyonel)</span></label>
                <input v-model="vehicleForm.capacity" type="text" placeholder="Örn: 5 ton" class="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Açıklama <span class="text-gray-400 font-normal">(opsiyonel)</span></label>
                <input v-model="vehicleForm.description" type="text" placeholder="Araç hakkında ek bilgi" class="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
              </div>
            </div>
            <div class="mt-5 flex justify-end gap-2">
              <button @click="showVehicleModal = false" class="px-4 py-2 border dark:border-gray-700 rounded text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800">İptal</button>
              <button @click="saveVehicle" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">Kaydet</button>
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
      <Teleport to="body">
        <div v-if="zone_showModal" class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
          <div class="bg-white dark:bg-gray-900 rounded-lg p-6 w-full max-w-md">
            <h3 class="text-lg font-bold mb-4 dark:text-gray-100">{{ zone_isEdit ? 'Bölgeyi Düzenle' : 'Yeni Bölge Ekle' }}</h3>
            <div class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Bölge Adı</label>
                <input v-model="zone_form.name" type="text" class="w-full border dark:border-gray-700 p-2 rounded focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-800 dark:text-gray-100" placeholder="Örn: A Koridoru" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Sıralama</label>
                <input v-model.number="zone_form.order" type="number" class="w-full border dark:border-gray-700 p-2 rounded focus:ring-blue-500 focus:border-blue-500 dark:bg-gray-800 dark:text-gray-100" />
                <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Toplama rotasında hangi sırada gidileceğini belirler (Küçükten büyüğe).</p>
              </div>
            </div>
            <div class="mt-6 flex justify-end gap-3">
              <button @click="zone_showModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded">İptal</button>
              <button @click="zone_saveZone" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">Kaydet</button>
            </div>
          </div>
        </div>
      </Teleport>

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

  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import transportService from '../services/transportService';
import projectService from '../services/projectService';
import systemSettingsService from '../services/systemSettingsService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';

const notificationStore = useNotificationStore();

const tabs = [
  { key: 'transport', label: 'Şoför & Araç' },
  { key: 'zones',     label: 'Bölge Yönetimi' },
  { key: 'depot',     label: 'Depo Tanımları' },
  { key: 'locations', label: 'Depo Adresleri' },
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

// ─── Init ────────────────────────────────────────────────────────────────────

onMounted(() => {
  fetchDrivers();
  fetchVehicles();
  fetchZones();
  depot_load();
});
</script>
