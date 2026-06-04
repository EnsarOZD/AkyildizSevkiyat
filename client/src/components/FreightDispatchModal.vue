<template>
  <div class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
    <div class="bg-white dark:bg-gray-900 rounded-xl p-6 w-full max-w-md shadow-xl border-t-4 border-teal-500">

      <div class="flex items-center gap-3 mb-5">
        <div class="p-2 bg-teal-100 dark:bg-teal-900/30 rounded-lg">
          <svg class="h-6 w-6 text-teal-600 dark:text-teal-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
          </svg>
        </div>
        <div>
          <h3 class="text-lg font-bold text-gray-800 dark:text-gray-200">Nakliye ile Gönder</h3>
          <p class="text-sm text-gray-500 dark:text-gray-400">{{ zoneName }}</p>
        </div>
      </div>

      <template v-if="phase === 'form'">
      <!-- Zone summary -->
      <div class="mb-5 bg-teal-50 dark:bg-teal-900/10 border border-teal-200 dark:border-teal-800 rounded-lg p-3 flex items-center gap-3">
        <svg class="h-5 w-5 text-teal-600 dark:text-teal-400 shrink-0" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
        </svg>
        <span class="text-sm text-teal-800 dark:text-teal-300">
          <strong>{{ shipmentCount }}</strong> sevkiyat nakliye ile gönderilecek ve <strong>Sevk Edildi</strong> statüsüne alınacak.
        </span>
      </div>

      <div class="space-y-4">
        <!-- Nakliyeci seçimi -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Nakliyeci <span class="text-red-500">*</span>
          </label>
          <select
            v-model="carrierChoice"
            @change="onCarrierChange"
            class="block w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
          >
            <option value="" disabled>Seçiniz...</option>
            <option v-for="c in carriers" :key="c.id" :value="String(c.id)">
              {{ c.name }}{{ c.city ? ' · ' + c.city : '' }}
            </option>
            <option value="other">Diğer (manuel giriş)</option>
          </select>
        </div>

        <!-- Tanımlı nakliyeci: plaka seçimi -->
        <template v-if="selectedCarrier">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Araç Plakası</label>
            <select
              v-if="selectedCarrier.vehicles.filter(v => v.isActive).length"
              v-model="carrierPlate"
              class="block w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
            >
              <option value="">Plaka seçiniz... (opsiyonel)</option>
              <option v-for="v in selectedCarrier.vehicles.filter(v => v.isActive)" :key="v.id" :value="v.plateNumber">
                {{ v.plateNumber }}
              </option>
            </select>
            <p v-else class="text-xs text-gray-400 italic py-1">Bu nakliyeciye tanımlı plaka yok.</p>
          </div>
          <div v-if="carrierPhone" class="text-sm text-gray-500 dark:text-gray-400">
            <span class="text-gray-400">Telefon: </span>{{ carrierPhone }}
          </div>
        </template>

        <!-- Diğer: manuel giriş -->
        <template v-else-if="carrierChoice === 'other'">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Nakliyeci Ad Soyad <span class="text-red-500">*</span>
            </label>
            <input
              v-model="carrierName"
              type="text"
              placeholder="Ad Soyad..."
              class="block w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Araç Plakası <span class="text-gray-400 font-normal ml-1">(opsiyonel)</span>
            </label>
            <input
              v-model="carrierPlate"
              type="text"
              placeholder="34 ABC 123"
              maxlength="20"
              class="block w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none uppercase"
              @input="carrierPlate = ($event.target as HTMLInputElement).value.toUpperCase()"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Telefon <span class="text-gray-400 font-normal ml-1">(opsiyonel)</span>
            </label>
            <input
              v-model="carrierPhone"
              type="tel"
              placeholder="05XX XXX XX XX"
              maxlength="30"
              class="block w-full border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-teal-500 focus:border-transparent outline-none"
            />
          </div>
        </template>
      </div>

      <div class="mt-6 flex justify-end gap-3">
        <button
          @click="$emit('close')"
          class="px-4 py-2.5 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-lg border border-gray-300 dark:border-gray-700 text-sm font-medium"
        >
          İptal
        </button>
        <button
          @click="save"
          :disabled="!carrierName.trim() || isSaving"
          class="px-6 py-2.5 bg-teal-600 hover:bg-teal-700 disabled:opacity-50 disabled:cursor-not-allowed text-white rounded-lg font-bold text-sm flex items-center gap-2"
        >
          <span v-if="isSaving" class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></span>
          <span>{{ isSaving ? 'Kaydediliyor...' : 'Nakliyeye Ver' }}</span>
        </button>
      </div>
      </template>

      <!-- Links phase: proje bazında WhatsApp teslim linkleri -->
      <template v-else>
        <div class="mb-4 bg-green-50 dark:bg-green-900/10 border border-green-200 dark:border-green-800 rounded-lg p-3 text-sm text-green-800 dark:text-green-300">
          Nakliyeye verildi. Aşağıdaki teslim linklerini nakliyeciye WhatsApp ile gönderin (proje bazında). Linkler 72 saat geçerlidir.
        </div>
        <div class="space-y-2 max-h-80 overflow-y-auto">
          <div v-for="link in links" :key="link.token" class="border border-gray-200 dark:border-gray-700 rounded-lg p-3">
            <div class="flex justify-between items-start gap-2 mb-2">
              <div class="min-w-0">
                <p class="text-sm font-semibold text-gray-800 dark:text-gray-200 truncate">{{ link.projectName }}</p>
                <p class="text-xs text-gray-500">{{ link.shipmentCount }} sevkiyat</p>
              </div>
            </div>
            <div class="flex gap-2">
              <a :href="waHref(link)" target="_blank" rel="noopener"
                class="flex-1 text-center py-2 rounded-lg text-white text-sm font-medium bg-green-600 hover:bg-green-700">
                WhatsApp ile Gönder
              </a>
              <button @click="copyLink(link)"
                class="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800">
                Kopyala
              </button>
            </div>
          </div>
        </div>
        <div class="mt-5 flex justify-end">
          <button @click="$emit('close')"
            class="px-6 py-2.5 bg-teal-600 hover:bg-teal-700 text-white rounded-lg font-bold text-sm">Kapat</button>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import warehouseService, { type FreightDeliveryLink } from '../services/warehouseService';
import carrierService, { type Carrier } from '../services/carrierService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import { uploadUrl, waHref as buildWaHref } from '../utils/freightLink';

const props = defineProps<{
  zonePreparationIds: number[];
  zoneName: string;
  shipmentCount: number;
}>();

const emit = defineEmits(['close', 'completed']);
const notificationStore = useNotificationStore();

const carrierName = ref('');
const carrierPlate = ref('');
const carrierPhone = ref('');
const isSaving = ref(false);

// Nakliyeci seçimi: '' (seçilmedi) | carrierId (string) | 'other' (manuel)
const carriers = ref<Carrier[]>([]);
const carrierChoice = ref<string>('');
const selectedCarrier = computed<Carrier | null>(() => {
  const id = Number(carrierChoice.value);
  return Number.isNaN(id) ? null : carriers.value.find(c => c.id === id) ?? null;
});

function onCarrierChange() {
  carrierPlate.value = '';
  const c = selectedCarrier.value;
  if (c) {
    carrierName.value = c.name;
    carrierPhone.value = c.phone ?? '';
  } else {
    // 'other' veya boş → manuel alanları temizle
    carrierName.value = '';
    carrierPhone.value = '';
  }
}

onMounted(async () => {
  try {
    carriers.value = await carrierService.list({ isActive: true });
  } catch {
    carriers.value = [];
  }
});

const phase = ref<'form' | 'links'>('form');
const links = ref<FreightDeliveryLink[]>([]);

const waHref = (link: FreightDeliveryLink) =>
  buildWaHref(link.carrierPhone || carrierPhone.value, link.projectName, link.token);

const copyLink = async (link: FreightDeliveryLink) => {
  try {
    await navigator.clipboard.writeText(uploadUrl(link.token));
    notificationStore.add('Link kopyalandı.', 'success');
  } catch {
    notificationStore.add('Kopyalanamadı, linki elle seçin.', 'warning');
  }
};

document.body.style.overflow = 'hidden';
onUnmounted(() => { document.body.style.overflow = ''; });

const save = async () => {
  if (!carrierName.value.trim() || isSaving.value) return;

  isSaving.value = true;
  try {
    const results = await Promise.all(props.zonePreparationIds.map(id =>
      warehouseService.dispatchAsFreight({
        zonePreparationId: id,
        carrierName: carrierName.value.trim(),
        carrierPlate: carrierPlate.value.trim() || null,
        carrierPhone: carrierPhone.value.trim() || null,
      })
    ));

    links.value = results.flatMap(r => r.links ?? []);
    const detail = carrierPlate.value.trim() ? ` (${carrierPlate.value.trim()})` : '';
    notificationStore.add(`${props.shipmentCount} sevkiyat ${carrierName.value.trim()}${detail} nakliyecisine verildi.`, 'success');
    emit('completed');
    // Modalı kapatma — proje bazında teslim linklerini göster
    phase.value = 'links';
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Nakliye ataması başarısız.', 'error');
  } finally {
    isSaving.value = false;
  }
};
</script>
