<template>
  <div class="p-3 max-w-2xl mx-auto space-y-3 pb-6">
    <PageHeader title="Kıyafet Kapama" />

    <!-- QR ile araba okut -->
    <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-3 space-y-2">
      <p class="text-sm font-semibold text-gray-700 dark:text-gray-200">Araba Okut</p>
      <QrScanInput placeholder="Araba kodu / QR" @scan="lookupContainer" />
    </div>

    <!-- by-container sonucu -->
    <div v-if="lookup" class="bg-white dark:bg-gray-900 rounded-xl border border-purple-200 dark:border-purple-800 p-3 space-y-2">
      <p class="text-xs text-gray-500">🛒 {{ lookup.containerCode }} — {{ lookup.shipments.length }} sevkiyat</p>
      <div v-for="s in lookup.shipments" :key="s.shipmentId" class="border border-gray-100 dark:border-gray-800 rounded-lg p-2">
        <div class="flex items-center justify-between gap-2">
          <div>
            <div class="font-semibold text-gray-900 dark:text-gray-100">{{ s.externalOrderNumber || ('#' + s.shipmentId) }}
              <span v-if="s.pickingMode != null" class="text-[10px] font-bold bg-purple-100 text-purple-700 px-1.5 py-0.5 rounded ml-1">{{ modeLabel(s.pickingMode) }}</span>
            </div>
            <div class="text-xs text-gray-500">{{ s.projectName }}</div>
            <div v-if="s.otherContainerCodes.length" class="text-[11px] text-sky-600 mt-0.5">Diğer arabalar: {{ s.otherContainerCodes.join(', ') }}</div>
          </div>
          <div class="text-right">
            <span v-if="s.closed" class="text-xs font-bold text-green-600">✓ kapatıldı</span>
            <button v-else-if="s.pickingCompleted" @click="openClose(s.shipmentId, s.externalOrderNumber, s.projectName)" class="px-3 py-2 bg-purple-600 text-white font-semibold rounded-lg text-xs">Kapat</button>
            <span v-else class="text-xs font-bold text-orange-500">⏳ toplama sürüyor</span>
          </div>
        </div>
        <p v-if="!s.pickingCompleted && !s.closed" class="text-[11px] text-orange-500 mt-1">Toplama tamamlanmadan kapatılamaz.</p>
      </div>
    </div>

    <!-- Kapamaya hazır kuyruk -->
    <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div class="px-3 py-1.5 bg-gray-100 dark:bg-white/[0.06] text-[11px] font-bold uppercase text-gray-500">Kapamaya Hazır ({{ queue.length }})</div>
      <div v-if="loading" class="p-4 text-center text-gray-400 text-sm">Yükleniyor...</div>
      <div v-else-if="queue.length === 0" class="p-4 text-center text-gray-400 text-sm">Kapamaya hazır iş yok.</div>
      <div v-else class="divide-y divide-gray-100 dark:divide-gray-800">
        <button v-for="it in queue" :key="it.shipmentId" @click="openClose(it.shipmentId, it.externalOrderNumber, it.projectName)"
                class="w-full text-left p-3 flex items-center justify-between">
          <div>
            <div class="font-semibold text-gray-900 dark:text-gray-100">{{ it.externalOrderNumber || ('#' + it.shipmentId) }}
              <span v-if="it.pickingMode != null" class="text-[10px] font-bold bg-purple-100 text-purple-700 px-1.5 py-0.5 rounded ml-1">{{ modeLabel(it.pickingMode) }}</span>
            </div>
            <div class="text-xs text-gray-500">{{ it.projectName }} · {{ it.lineCount }} kalem</div>
          </div>
          <span class="text-purple-600 font-bold text-sm">Kapat →</span>
        </button>
      </div>
    </div>

    <!-- Kapama modalı -->
    <div v-if="closeTarget" class="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
      <div class="bg-white dark:bg-gray-900 rounded-xl p-4 w-full max-w-sm space-y-3">
        <h3 class="font-bold text-gray-900 dark:text-gray-100">Kapama — {{ closeTarget.label }}</h3>
        <p class="text-xs text-gray-500">{{ closeTarget.projectName }}</p>

        <template v-if="!closed">
          <div>
            <label class="block text-xs font-bold text-gray-500 uppercase mb-1">Koli Sayısı *</label>
            <input v-model.number="boxCount" type="number" min="1" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-700 dark:bg-gray-800 text-sm" />
          </div>
          <div>
            <label class="block text-xs font-bold text-gray-500 uppercase mb-1">Paket Tipi</label>
            <div class="flex gap-2">
              <button v-for="(lbl, idx) in ['Koli','Poşet']" :key="idx" @click="packageType = idx"
                      class="flex-1 py-2 rounded-lg border-2 text-sm font-bold"
                      :class="packageType === idx ? 'border-purple-500 text-purple-700 bg-purple-50' : 'border-gray-200 text-gray-500'">{{ lbl }}</button>
            </div>
          </div>
          <div>
            <label class="block text-xs font-bold text-gray-500 uppercase mb-1">Not (opsiyonel)</label>
            <input v-model="note" type="text" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-700 dark:bg-gray-800 text-sm" />
          </div>
          <div class="flex gap-2 pt-1">
            <button @click="closeTarget = null" class="flex-1 py-2 bg-gray-100 dark:bg-gray-700 rounded-lg text-sm font-bold">Vazgeç</button>
            <button @click="doClose" :disabled="busy || !boxCount || boxCount < 1" class="flex-1 py-2 bg-purple-600 disabled:opacity-50 text-white rounded-lg text-sm font-bold">Kapat</button>
          </div>
        </template>

        <!-- Kapatıldı → etiket -->
        <template v-else>
          <p class="text-sm font-semibold text-green-600">✓ Kapatıldı ({{ boxCount }} koli)</p>
          <div class="flex gap-2">
            <button @click="printLabel" :disabled="busy" class="flex-1 py-2.5 bg-purple-600 disabled:opacity-50 text-white rounded-lg text-sm font-bold">Etiket Bas</button>
            <button @click="handwritten" :disabled="busy" class="flex-1 py-2.5 bg-gray-100 dark:bg-gray-700 rounded-lg text-sm font-bold">Elle Yazıldı</button>
          </div>
          <button @click="finishClose" class="w-full py-2 text-sm font-bold text-gray-500">Kapat</button>
        </template>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import QrScanInput from '../components/QrScanInput.vue';
import clothingPickingService, { type ClosingQueueItem, type ByContainer, PickingModeLabels } from '../services/clothingPickingService';
import printService from '../services/printService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const notify = useNotificationStore();
const loading = ref(false);
const busy = ref(false);
const queue = ref<ClosingQueueItem[]>([]);
const lookup = ref<ByContainer | null>(null);

const closeTarget = ref<{ shipmentId: number; label: string; projectName: string } | null>(null);
const boxCount = ref<number | null>(null);
const packageType = ref(0);
const note = ref('');
const closed = ref(false);

const modeLabel = (m: number) => PickingModeLabels[m] ?? '?';

async function loadQueue() {
  loading.value = true;
  try { queue.value = await clothingPickingService.closingQueue(); }
  catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Yüklenemedi.', 'error'); }
  finally { loading.value = false; }
}

async function lookupContainer(code: string) {
  try { lookup.value = await clothingPickingService.byContainer(code); }
  catch (e) { lookup.value = null; notify.add(ApiErrorUtils.getErrorMessage(e) || 'Araba bulunamadı.', 'error'); }
}

function openClose(shipmentId: number, ext: string | null | undefined, projectName: string) {
  closeTarget.value = { shipmentId, label: ext || ('#' + shipmentId), projectName };
  boxCount.value = null; packageType.value = 0; note.value = ''; closed.value = false;
}

async function doClose() {
  if (!closeTarget.value || !boxCount.value || boxCount.value < 1) return;
  busy.value = true;
  try {
    await clothingPickingService.completeClosing(closeTarget.value.shipmentId, boxCount.value, packageType.value, note.value.trim() || null);
    closed.value = true;
    notify.add('Kapama tamamlandı.', 'success');
  } catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Kapatılamadı.', 'error'); }
  finally { busy.value = false; }
}

async function printLabel() {
  if (!closeTarget.value) return;
  busy.value = true;
  try { const r = await printService.createBoxJob(closeTarget.value.shipmentId); notify.add(r.message || 'Etiket kuyruğa alındı.', 'success'); }
  catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Etiket basılamadı.', 'error'); }
  finally { busy.value = false; }
}

async function handwritten() {
  if (!closeTarget.value) return;
  busy.value = true;
  try { await clothingPickingService.labelHandwritten(closeTarget.value.shipmentId); notify.add('Elle yazıldı olarak işaretlendi.', 'success'); }
  catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Hata.', 'error'); }
  finally { busy.value = false; }
}

async function finishClose() {
  closeTarget.value = null;
  lookup.value = null;
  await loadQueue();
}

onMounted(loadQueue);
</script>
