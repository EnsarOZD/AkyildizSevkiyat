<template>
  <div class="p-4 max-w-5xl mx-auto space-y-4">
    <PageHeader title="Kıyafet Depo Hazırlık">
      <template #description>Kıyafet sevkiyatlarının toplanması, koli sayısı ve Netsis aktarımı.</template>
    </PageHeader>

    <div v-if="loading" class="text-center py-12 text-gray-400 text-sm">Yükleniyor...</div>
    <div v-else-if="rows.length === 0" class="text-center py-12 text-gray-400 text-sm">Hazırlanacak kıyafet sevkiyatı yok.</div>

    <div v-else class="space-y-2">
      <div v-for="r in rows" :key="r.shipmentId"
           class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-3 flex flex-wrap items-center gap-3">
        <div class="flex-1 min-w-[200px]">
          <div class="flex items-center gap-2">
            <span class="font-semibold text-gray-900 dark:text-gray-100">{{ r.externalOrderNumber || ('#' + r.shipmentId) }}</span>
            <span v-if="r.talepNo" class="text-[11px] font-bold bg-indigo-50 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300 px-1.5 py-0.5 rounded">T:{{ r.talepNo }}</span>
            <span class="px-2 py-0.5 rounded-full text-[10px] font-bold" :class="statusClass(r.status)">{{ statusLabel(r.status) }}</span>
          </div>
          <div class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ r.projectCode }} — {{ r.projectName }} · {{ r.lineCount }} kalem</div>
          <div v-if="r.preparedByUserName" class="text-[11px] text-emerald-600 dark:text-emerald-400 mt-0.5">
            Hazırlayan: {{ r.preparedByUserName }}<span v-if="r.koliCount"> · {{ r.koliCount }}</span>
          </div>
        </div>

        <div class="flex items-center gap-2">
          <button v-if="r.status === 'Created' || r.status === 'Picking'"
                  @click="prepare(r)" :disabled="busyId === r.shipmentId"
                  class="px-4 py-2 bg-purple-600 hover:bg-purple-700 disabled:opacity-50 text-white font-semibold rounded-lg text-sm">
            Hazırla
          </button>
          <button v-else-if="r.status === 'ReadyForDispatch' && !r.netsisTransferred"
                  @click="exportNetsis(r)" :disabled="busyId === r.shipmentId"
                  class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 text-white font-semibold rounded-lg text-sm">
            Netsis'e Gönder
          </button>
          <span v-else-if="r.netsisTransferred" class="text-xs font-bold text-green-600">✓ Netsis</span>
        </div>
      </div>
    </div>

    <ClothingPickingModal v-if="modalShipmentId" :shipment-id="modalShipmentId"
                          @close="modalShipmentId = null" @completed="onCompleted" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import ClothingPickingModal from '../components/ClothingPickingModal.vue';
import clothingPrepService, { type ClothingPrepShipment } from '../services/clothingPrepService';
import shipmentService from '../services/shipmentService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const notify = useNotificationStore();
const rows = ref<ClothingPrepShipment[]>([]);
const loading = ref(false);
const busyId = ref<number | null>(null);
const modalShipmentId = ref<number | null>(null);

function statusLabel(s: string) {
  return s === 'Created' ? 'Hazırlanacak' : s === 'Picking' ? 'Hazırlanıyor' : s === 'ReadyForDispatch' ? 'Sevke Hazır' : s;
}
function statusClass(s: string) {
  return s === 'Created' ? 'bg-gray-100 text-gray-700'
    : s === 'Picking' ? 'bg-yellow-100 text-yellow-800'
    : 'bg-indigo-100 text-indigo-700';
}

async function load() {
  loading.value = true;
  try {
    rows.value = await clothingPrepService.dashboard();
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
}

async function prepare(r: ClothingPrepShipment) {
  busyId.value = r.shipmentId;
  try {
    // Created ise önce hazırlığa al (Picking), sonra toplama modalını aç.
    if (r.status === 'Created') {
      await clothingPrepService.start([r.shipmentId]);
    }
    modalShipmentId.value = r.shipmentId;
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
  } finally {
    busyId.value = null;
  }
}

async function exportNetsis(r: ClothingPrepShipment) {
  if (!await notify.promptConfirm({ title: 'Netsis Aktarımı', message: `${r.projectName} sevkiyatı Netsis'e aktarılsın mı?`, confirmText: 'Gönder' })) return;
  busyId.value = r.shipmentId;
  try {
    const res = await shipmentService.exportClothingToNetsis(r.shipmentId);
    notify.add(res.message || 'Netsis aktarımı tamamlandı.', 'success');
    await load();
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Netsis aktarımı başarısız.', 'error');
  } finally {
    busyId.value = null;
  }
}

async function onCompleted() {
  modalShipmentId.value = null;
  await load();
}

onMounted(load);
</script>
