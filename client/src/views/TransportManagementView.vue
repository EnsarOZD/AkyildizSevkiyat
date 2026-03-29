<template>
  <div class="space-y-6">
    <div class="flex justify-between items-center">
      <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Şoför ve Araç Yönetimi</h1>
    </div>

    <!-- Error -->
    <div v-if="error" class="mx-4 mt-4 p-3 bg-red-900/30 border border-red-700 rounded-lg flex items-center justify-between">
      <span class="text-red-400 text-sm">{{ error }}</span>
      <button @click="fetchDrivers(); fetchVehicles(); error = null" class="text-red-400 hover:text-red-300 text-sm underline ml-4">Tekrar dene</button>
    </div>

    <!-- Tabs -->
    <div class="border-b border-gray-200 dark:border-gray-700">
      <nav class="-mb-px flex space-x-8" aria-label="Tabs">
        <button
          @click="activeTab = 'drivers'"
          :class="[
            activeTab === 'drivers'
              ? 'border-blue-500 text-blue-600'
              : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-700',
            'whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm'
          ]"
        >
          Şoförler
        </button>
        <button
          @click="activeTab = 'vehicles'"
          :class="[
            activeTab === 'vehicles'
              ? 'border-blue-500 text-blue-600'
              : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300 dark:hover:border-gray-700',
            'whitespace-nowrap py-4 px-1 border-b-2 font-medium text-sm'
          ]"
        >
          Araçlar
        </button>
      </nav>
    </div>

    <!-- Drivers Tab -->
    <div v-if="activeTab === 'drivers'" class="space-y-4">
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
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Telefon</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
                        <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
                    </tr>
                </thead>
                <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                    <tr v-for="driver in drivers" :key="driver.id">
                        <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">{{ driver.fullName }}</td>
                        <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ driver.phone }}</td>
                        <td class="px-6 py-4 whitespace-nowrap">
                            <span v-if="driver.isActive" class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800">Aktif</span>
                            <span v-else class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-red-100 text-red-800">Pasif</span>
                        </td>
                        <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                            <button @click="openDriverModal(driver)" class="text-indigo-600 hover:text-indigo-900 mr-4">Düzenle</button>
                            <button @click="deleteDriver(driver.id)" class="text-red-600 hover:text-red-900">Sil</button>
                        </td>
                    </tr>
                </tbody>
            </table>
          </div>
        </div>
    </div>

    <!-- Vehicles Tab -->
    <div v-if="activeTab === 'vehicles'" class="space-y-4">
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
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Araç Tipi</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Kapasite / Notlar</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
                        <th class="px-6 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlemler</th>
                    </tr>
                </thead>
                <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
                    <tr v-for="vehicle in vehicles" :key="vehicle.id">
                        <td class="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900 dark:text-gray-100">{{ vehicle.plateNumber }}</td>
                        <td class="px-6 py-4 whitespace-nowrap">
                          <span class="px-2 py-0.5 text-xs rounded-full font-medium"
                            :class="{
                              'bg-blue-100 text-blue-800 dark:bg-blue-900/30 dark:text-blue-300': vehicle.vehicleType === 0,
                              'bg-purple-100 text-purple-800 dark:bg-purple-900/30 dark:text-purple-300': vehicle.vehicleType === 1,
                              'bg-teal-100 text-teal-800 dark:bg-teal-900/30 dark:text-teal-300': vehicle.vehicleType === 2,
                            }">{{ (vehicle.vehicleTypeName || ['Kamyon','Kamyonet','Minibüs'][vehicle.vehicleType]) ?? 'Kamyon' }}</span>
                        </td>
                        <td class="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ vehicle.description || vehicle.capacity || '—' }}</td>
                         <td class="px-6 py-4 whitespace-nowrap">
                            <span v-if="vehicle.isActive" class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800">Aktif</span>
                            <span v-else class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-red-100 text-red-800">Pasif</span>
                        </td>
                        <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                            <button @click="openVehicleModal(vehicle)" class="text-indigo-600 hover:text-indigo-900 mr-4">Düzenle</button>
                             <button @click="deleteVehicle(vehicle.id)" class="text-red-600 hover:text-red-900">Sil</button>
                        </td>
                    </tr>
                </tbody>
            </table>
          </div>
        </div>
    </div>

    <!-- Driver Modal -->
    <div v-if="showDriverModal" class="fixed inset-0 bg-gray-500 bg-opacity-75 flex items-center justify-center z-50">
        <div class="bg-white dark:bg-gray-900 rounded-lg p-6 max-w-sm w-full">
            <h3 class="text-lg font-medium mb-4">{{ editingDriver ? 'Şoför Düzenle' : 'Yeni Şoför Ekle' }}</h3>
            <div class="space-y-4">
                <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Ad Soyad</label>
                    <input v-model="driverForm.fullName" type="text" class="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
                </div>
                 <div>
                    <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Telefon</label>
                    <input v-model="driverForm.phone" type="text" class="mt-1 block w-full border-gray-300 rounded-md shadow-sm focus:ring-blue-500 focus:border-blue-500 sm:text-sm border p-2 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100">
                </div>
                <!-- IsActive check for edit -->
            </div>
            <div class="mt-5 flex justify-end gap-2">
                <button @click="showDriverModal = false" class="px-4 py-2 border dark:border-gray-700 rounded text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800">İptal</button>
                <button @click="saveDriver" class="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700">Kaydet</button>
            </div>
        </div>
    </div>

    <!-- Vehicle Modal -->
    <div v-if="showVehicleModal" class="fixed inset-0 bg-gray-500 bg-opacity-75 flex items-center justify-center z-50">
        <div class="bg-white dark:bg-gray-900 rounded-lg p-6 max-w-sm w-full">
            <h3 class="text-lg font-medium mb-4">{{ editingVehicle ? 'Araç Düzenle' : 'Yeni Araç Ekle' }}</h3>
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

  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import transportService from '../services/transportService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';

const notificationStore = useNotificationStore();

const error = ref<string | null>(null);
const activeTab = ref('drivers');
const drivers = ref<any[]>([]);
const vehicles = ref<any[]>([]);

// Driver State
const showDriverModal = ref(false);
const editingDriver = ref<any>(null);
const driverForm = ref({ fullName: '', phone: '' });

// Vehicle State
const showVehicleModal = ref(false);
const editingVehicle = ref<any>(null);
const vehicleForm = ref({ plateNumber: '', capacity: '', vehicleType: 0, description: '' });

onMounted(() => {
    fetchDrivers();
    fetchVehicles();
});

const fetchDrivers = async () => {
    try {
        drivers.value = await transportService.getDrivers();
    } catch(e) {
        error.value = ApiErrorUtils.getErrorMessage(e) || 'Şoförler yüklenemedi.';
        notificationStore.add(error.value, 'error');
    }
};

const fetchVehicles = async () => {
    try {
        vehicles.value = await transportService.getVehicles();
    } catch(e) {
        error.value = ApiErrorUtils.getErrorMessage(e) || 'Araçlar yüklenemedi.';
        notificationStore.add(error.value, 'error');
    }
};

// Driver Actions
const openDriverModal = (driver: any = null) => {
    editingDriver.value = driver;
    if (driver) {
        driverForm.value = { fullName: driver.fullName, phone: driver.phone };
    } else {
        driverForm.value = { fullName: '', phone: '' };
    }
    showDriverModal.value = true;
};

const saveDriver = async () => {
    try {
        if (editingDriver.value) {
            await transportService.updateDriver({
                id: editingDriver.value.id,
                ...driverForm.value,
                isActive: true
            });
        } else {
            await transportService.createDriver(driverForm.value);
        }
        await fetchDrivers();
        showDriverModal.value = false;
        notificationStore.add('Şoför kaydedildi.', 'success');
    } catch(e) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || "Kaydetme başarısız.", "error");
    }
};

const deleteDriver = async (id: number) => {
    const ok = await notificationStore.promptConfirm({ title: 'Şoförü Sil', message: 'Bu şoförü silmek istediğinize emin misiniz?', confirmText: 'Sil', type: 'danger' });
    if (!ok) return;
    try {
        await transportService.deleteDriver(id);
        fetchDrivers();
        notificationStore.add('Şoför silindi.', 'success');
    } catch(e) { notificationStore.add(ApiErrorUtils.getErrorMessage(e) || "Silme başarısız.", "error"); }
};

// Vehicle Actions
const openVehicleModal = (vehicle: any = null) => {
    editingVehicle.value = vehicle;
    if (vehicle) {
        vehicleForm.value = {
            plateNumber: vehicle.plateNumber,
            capacity:    vehicle.capacity ?? '',
            vehicleType: vehicle.vehicleType ?? 0,
            description: vehicle.description ?? '',
        };
    } else {
        vehicleForm.value = { plateNumber: '', capacity: '', vehicleType: 0, description: '' };
    }
    showVehicleModal.value = true;
};

const saveVehicle = async () => {
     try {
        if (editingVehicle.value) {
            await transportService.updateVehicle({
                id: editingVehicle.value.id,
                ...vehicleForm.value,
                isActive: true
            });
        } else {
            await transportService.createVehicle(vehicleForm.value);
        }
        await fetchVehicles();
        showVehicleModal.value = false;
        notificationStore.add('Araç kaydedildi.', 'success');
    } catch(e) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || "Kaydetme başarısız.", "error");
    }
};

const deleteVehicle = async (id: number) => {
    const ok = await notificationStore.promptConfirm({ title: 'Aracı Sil', message: 'Bu aracı silmek istediğinize emin misiniz?', confirmText: 'Sil', type: 'danger' });
    if (!ok) return;
    try {
        await transportService.deleteVehicle(id);
        fetchVehicles();
        notificationStore.add('Araç silindi.', 'success');
    } catch(e) { notificationStore.add(ApiErrorUtils.getErrorMessage(e) || "Silme başarısız.", "error"); }
};

</script>
