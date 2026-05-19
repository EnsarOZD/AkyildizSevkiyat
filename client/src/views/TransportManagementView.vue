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
            <BaseButton @click="openDriverModal()" variant="primary">+ Yeni Şoför Ekle</BaseButton>
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
            <BaseButton @click="openVehicleModal()" variant="primary">+ Yeni Araç Ekle</BaseButton>
        </div>

        <div class="bg-white dark:bg-gray-900 shadow overflow-hidden rounded-lg">
          <div class="overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                <thead class="bg-gray-50 dark:bg-gray-800">
                    <tr>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Plaka</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden sm:table-cell">Araç Tipi</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">Kapasite / Notlar</th>
                        <th class="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">QR</th>
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
                        <td class="px-6 py-4 whitespace-nowrap text-sm">
                          <span v-if="vehicle.qrCode" class="text-green-600 dark:text-green-400 font-medium">✅</span>
                          <span v-else class="text-gray-400">—</span>
                        </td>
                         <td class="px-6 py-4 whitespace-nowrap">
                            <span v-if="vehicle.isActive" class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-green-100 text-green-800">Aktif</span>
                            <span v-else class="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-red-100 text-red-800">Pasif</span>
                        </td>
                        <td class="px-6 py-4 whitespace-nowrap text-right text-sm font-medium space-x-3">
                            <button v-if="vehicle.qrCode" @click="openQrModal(vehicle)" class="text-green-600 hover:text-green-800 dark:text-green-400 dark:hover:text-green-300">QR Görüntüle</button>
                            <button v-else @click="generateQr(vehicle)" class="text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300">QR Oluştur</button>
                            <button @click="openVehicleModal(vehicle)" class="text-indigo-600 hover:text-indigo-900">Düzenle</button>
                            <button @click="deleteVehicle(vehicle.id)" class="text-red-600 hover:text-red-900">Sil</button>
                        </td>
                    </tr>
                </tbody>
            </table>
          </div>
        </div>
    </div>

    <!-- Driver Modal -->
    <BaseModal :show="showDriverModal" :title="editingDriver ? 'Şoför Düzenle' : 'Yeni Şoför Ekle'" maxWidth="sm" @close="showDriverModal = false">
      <div class="space-y-4">
        <BaseInput v-model="driverForm.fullName" label="Ad Soyad" />
        <BaseInput v-model="driverForm.phone" label="Telefon" />
      </div>
      <template #footer>
        <BaseButton @click="showDriverModal = false" variant="secondary">İptal</BaseButton>
        <BaseButton @click="saveDriver" variant="primary">Kaydet</BaseButton>
      </template>
    </BaseModal>

    <!-- Vehicle Modal -->
    <BaseModal :show="showVehicleModal" :title="editingVehicle ? 'Araç Düzenle' : 'Yeni Araç Ekle'" maxWidth="sm" @close="showVehicleModal = false">
      <div class="space-y-4">
        <BaseInput
          :model-value="vehicleForm.plateNumber"
          @update:model-value="vehicleForm.plateNumber = ($event ?? '').toUpperCase()"
          label="Plaka"
        />
        <BaseSelect v-model.number="vehicleForm.vehicleType" label="Araç Tipi">
          <option :value="0">Kamyon</option>
          <option :value="1">Kamyonet</option>
          <option :value="2">Minibüs</option>
        </BaseSelect>
        <BaseInput v-model="vehicleForm.capacity" label="Kapasite / Notlar" placeholder="Örn: 5 ton" hint="Opsiyonel" />
        <BaseInput v-model="vehicleForm.description" label="Açıklama" placeholder="Araç hakkında ek bilgi" hint="Opsiyonel" />
      </div>
      <template #footer>
        <BaseButton @click="showVehicleModal = false" variant="secondary">İptal</BaseButton>
        <BaseButton @click="saveVehicle" variant="primary">Kaydet</BaseButton>
      </template>
    </BaseModal>

    <!-- QR Modal -->
    <div v-if="showQrModal && qrVehicle" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50 print:hidden" @click.self="showQrModal = false">
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-2xl w-full max-w-sm mx-4">

        <!-- Modal Header -->
        <div class="flex items-center justify-between px-5 py-4 border-b border-gray-200 dark:border-gray-700">
          <div>
            <h3 class="text-base font-semibold text-gray-900 dark:text-gray-100">Araç QR Etiketi</h3>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ qrVehicle.plateNumber }} — {{ vehicleTypeName(qrVehicle.vehicleType) }}</p>
          </div>
          <button @click="showQrModal = false" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300">
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
          </button>
        </div>

        <!-- Label Preview -->
        <div class="px-5 py-4">
          <p class="text-xs text-gray-400 dark:text-gray-500 uppercase tracking-wider mb-3 font-medium">Etiket Önizleme</p>
          <div class="flex justify-center">
            <!-- This is exactly what will print -->
            <div class="qr-label-preview border-2 border-dashed border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
              <div class="qr-label bg-white" style="width:240px; padding: 16px 16px 20px; font-family: 'Arial', sans-serif;">
                <!-- Header bar -->
                <div style="background:#1e3a5f; padding:8px 12px; margin:-16px -16px 14px; text-align:center;">
                  <div style="color:#ffffff; font-size:13px; font-weight:700; letter-spacing:2px;">AKYİLDIZ</div>
                  <div style="color:#93c5fd; font-size:9px; letter-spacing:1px; margin-top:2px;">SEVKİYAT SİSTEMİ</div>
                </div>
                <!-- QR Code -->
                <div style="text-align:center; margin-bottom:12px;">
                  <div v-if="qrImageLoading" style="width:160px; height:160px; background:#f3f4f6; margin:0 auto; display:flex; align-items:center; justify-content:center; font-size:11px; color:#9ca3af;">Yükleniyor...</div>
                  <img v-else-if="qrImageBase64" :src="qrImageBase64" alt="QR" style="width:160px; height:160px; display:block; margin:0 auto;" />
                </div>
                <!-- Plate Number -->
                <div style="text-align:center; border:2px solid #1e3a5f; border-radius:6px; padding:6px 10px; margin-bottom:10px;">
                  <div style="font-size:22px; font-weight:900; letter-spacing:3px; color:#1e3a5f; font-family:'Arial Black', sans-serif;">{{ qrVehicle.plateNumber }}</div>
                </div>
                <!-- Vehicle Type -->
                <div style="text-align:center;">
                  <span style="background:#e0e7ff; color:#3730a3; font-size:10px; font-weight:600; padding:3px 10px; border-radius:999px; letter-spacing:0.5px;">{{ vehicleTypeName(qrVehicle.vehicleType) }}</span>
                </div>
              </div>
            </div>
          </div>
          <p class="text-xs text-center text-gray-400 dark:text-gray-500 mt-3">Yaklaşık 6×9 cm etiket boyutu</p>
        </div>

        <!-- Actions -->
        <div class="px-5 py-4 border-t border-gray-200 dark:border-gray-700 flex gap-2">
          <BaseButton @click="printQr" :disabled="qrImageLoading || !qrImageBase64" variant="primary" class="flex-1">
            <svg class="w-4 h-4 mr-1.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"/></svg>
            Yazdır
          </BaseButton>
          <BaseButton @click="showQrModal = false" variant="secondary">Kapat</BaseButton>
        </div>
      </div>
    </div>

    <!-- ── Yazdırma alanı (sadece print modunda görünür) ─────────────────── -->
    <div id="qr-print-area" style="display:none;">
      <div class="qr-label" style="width:226px; font-family:'Arial',sans-serif; margin:0 auto;">
        <div style="background:#1e3a5f; padding:10px 14px; text-align:center;">
          <div style="color:#ffffff; font-size:14px; font-weight:700; letter-spacing:2px;">AKYİLDIZ</div>
          <div style="color:#93c5fd; font-size:9px; letter-spacing:1px; margin-top:2px;">SEVKİYAT SİSTEMİ</div>
        </div>
        <div style="padding:14px 14px 18px; background:#ffffff;">
          <div style="text-align:center; margin-bottom:12px;">
            <img v-if="qrImageBase64" :src="qrImageBase64" alt="QR" style="width:168px; height:168px; display:block; margin:0 auto;" />
          </div>
          <div style="text-align:center; border:2.5px solid #1e3a5f; border-radius:6px; padding:6px 10px; margin-bottom:10px;">
            <div style="font-size:24px; font-weight:900; letter-spacing:3px; color:#1e3a5f; font-family:'Arial Black',sans-serif;">{{ qrVehicle?.plateNumber }}</div>
          </div>
          <div style="text-align:center;">
            <span style="background:#e0e7ff; color:#3730a3; font-size:10px; font-weight:600; padding:3px 10px; border-radius:999px; letter-spacing:0.5px;">{{ vehicleTypeName(qrVehicle?.vehicleType) }}</span>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>

<style>
/* ── Print Styles ─────────────────────────────────────────── */
@media print {
  /* Sayfadaki her şeyi gizle */
  body * { visibility: hidden !important; }

  /* Sadece print alanını göster */
  #qr-print-area,
  #qr-print-area * { visibility: visible !important; }

  #qr-print-area {
    display: block !important;
    position: fixed !important;
    top: 0 !important;
    left: 0 !important;
    width: 100% !important;
    height: 100% !important;
    display: flex !important;
    align-items: center !important;
    justify-content: center !important;
    background: white !important;
  }

  @page {
    size: 80mm 100mm;
    margin: 0;
  }
}
</style>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import transportService from '../services/transportService';
import apiClient from '../services/apiClient';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import BaseModal from '../components/BaseModal.vue';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/base/BaseInput.vue';
import BaseSelect from '../components/base/BaseSelect.vue';

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

// QR State
const showQrModal = ref(false);
const qrVehicle = ref<any>(null);
const qrImageBase64 = ref<string | null>(null);
const qrImageLoading = ref(false);

const vehicleTypeNames = ['Kamyon', 'Kamyonet', 'Minibüs'];
const vehicleTypeName = (type: number | undefined) => vehicleTypeNames[type ?? 0] ?? 'Kamyon';

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

// QR Actions
const generateQr = async (vehicle: any) => {
    qrVehicle.value = vehicle;
    qrImageBase64.value = null;
    qrImageLoading.value = true;
    showQrModal.value = true;
    try {
        const res = await apiClient.post<{ qrCode: string; qrImageBase64: string }>(
            `/vehicles/${vehicle.id}/generate-qr`
        );
        vehicle.qrCode = res.data.qrCode;
        qrImageBase64.value = res.data.qrImageBase64;
        notificationStore.add('QR kod oluşturuldu.', 'success');
        await fetchVehicles();
    } catch(e) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'QR oluşturulamadı.', 'error');
        showQrModal.value = false;
    } finally {
        qrImageLoading.value = false;
    }
};

const openQrModal = async (vehicle: any) => {
    qrVehicle.value = vehicle;
    qrImageBase64.value = null;
    qrImageLoading.value = true;
    showQrModal.value = true;
    try {
        const res = await apiClient.get<{ qrCode: string; qrImageBase64: string }>(
            `/vehicles/${vehicle.id}/qr-image`
        );
        qrImageBase64.value = res.data.qrImageBase64;
    } catch(e) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'QR yüklenemedi.', 'error');
        showQrModal.value = false;
    } finally {
        qrImageLoading.value = false;
    }
};

const printQr = () => {
    // print alanını görünür yap, yazdır, gizle
    const el = document.getElementById('qr-print-area');
    if (el) el.style.display = 'flex';
    window.print();
    if (el) el.style.display = 'none';
};

</script>
