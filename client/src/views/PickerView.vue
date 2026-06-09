<template>
  <div class="p-3 max-w-2xl mx-auto space-y-3 pb-24">
    <PageHeader title="Kıyafet Toplama" />

    <!-- ════ LİSTE MODU ════ -->
    <template v-if="!active">
      <!-- Grup seçimi -->
      <div class="flex gap-2 overflow-x-auto pb-1">
        <button v-for="g in groupChips" :key="g.key" @click="selectGroup(g.id)"
                class="px-3 py-1.5 rounded-full text-sm font-semibold whitespace-nowrap border"
                :class="selectedGroupId === g.id ? 'bg-purple-600 text-white border-purple-600' : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-200 border-gray-300 dark:border-gray-700'">
          {{ g.name }}
        </button>
      </div>

      <!-- Devam eden işlerim -->
      <div v-if="queue.mine.length" class="bg-white dark:bg-gray-900 rounded-xl border border-emerald-200 dark:border-emerald-800 overflow-hidden">
        <div class="px-3 py-1.5 bg-emerald-50 dark:bg-emerald-900/30 text-[11px] font-bold uppercase text-emerald-700 dark:text-emerald-300">Devam Eden İşlerim</div>
        <button v-for="it in queue.mine" :key="it.shipmentId" @click="openJob(it)"
                class="w-full text-left p-3 border-t border-gray-100 dark:border-gray-800 flex items-center justify-between">
          <div>
            <div class="font-semibold text-gray-900 dark:text-gray-100">{{ it.externalOrderNumber || ('#' + it.shipmentId) }}
              <span v-if="it.pickingMode != null" class="text-[10px] font-bold bg-purple-100 text-purple-700 px-1.5 py-0.5 rounded ml-1">{{ modeLabel(it.pickingMode) }}</span>
            </div>
            <div class="text-xs text-gray-500">{{ it.projectName }} · {{ it.lineCount }} kalem</div>
          </div>
          <span class="text-purple-600 font-bold">Devam →</span>
        </button>
      </div>

      <!-- Sıradaki işi al -->
      <button v-if="queue.claimable.length" @click="claimNext" :disabled="busy"
              class="w-full py-3.5 bg-purple-600 hover:bg-purple-700 disabled:opacity-50 text-white font-bold rounded-xl shadow text-sm">
        Sıradaki İşi Al ({{ queue.claimable.length }} bekliyor)
      </button>

      <!-- Alınabilir liste -->
      <div class="space-y-2">
        <div v-for="it in queue.claimable" :key="it.shipmentId"
             class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-3 flex items-center justify-between gap-2">
          <div class="min-w-0">
            <div class="font-semibold text-gray-900 dark:text-gray-100 truncate">
              {{ it.externalOrderNumber || ('#' + it.shipmentId) }}
              <span v-if="it.reservedForMe" class="text-[10px] font-bold bg-indigo-100 text-indigo-700 px-1.5 py-0.5 rounded ml-1">size ayrıldı</span>
            </div>
            <div class="text-xs text-gray-500 truncate">{{ it.projectName }} · {{ it.lineCount }} kalem · sıra {{ it.queueOrder }}</div>
          </div>
          <button @click="claim(it)" :disabled="busy" class="px-3 py-2 bg-purple-600 disabled:opacity-50 text-white font-semibold rounded-lg text-xs whitespace-nowrap">Al</button>
        </div>
        <p v-if="!loading && queue.claimable.length === 0 && queue.mine.length === 0" class="text-center text-gray-400 text-sm py-8">Bu grupta bekleyen iş yok.</p>
      </div>
    </template>

    <!-- ════ AKTİF İŞ ════ -->
    <template v-else>
      <button @click="closeJob" class="text-sm text-purple-600 font-semibold">← Listeye dön</button>
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-3">
        <div class="font-bold text-gray-900 dark:text-gray-100">{{ active.externalOrderNumber || ('#' + active.shipmentId) }}</div>
        <div class="text-xs text-gray-500">{{ active.projectName }}</div>
      </div>

      <!-- Mod seçimi -->
      <div v-if="active.pickingMode == null" class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 space-y-3">
        <p class="text-sm font-semibold text-gray-700 dark:text-gray-200">Toplama modu seçin</p>
        <div class="grid grid-cols-3 gap-2">
          <button v-for="m in [0,1,2]" :key="m" @click="chooseMode(m)" :disabled="busy"
                  class="py-4 rounded-xl border-2 border-purple-200 dark:border-purple-800 font-bold text-purple-700 dark:text-purple-300 disabled:opacity-50">
            {{ modeLabel(m) }}
          </button>
        </div>
      </div>

      <template v-else>
        <!-- Cart: araba bağlama -->
        <div v-if="active.pickingMode === 0" class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-3 space-y-2">
          <p class="text-sm font-semibold text-gray-700 dark:text-gray-200">Araba Bağla</p>
          <QrScanInput placeholder="Araba kodu / QR" @scan="bindContainer" />
          <div v-if="containers.length" class="flex flex-wrap gap-1.5">
            <span v-for="c in containers" :key="c.containerAssignmentId" class="px-2 py-1 rounded-lg text-xs font-bold bg-sky-100 text-sky-700">🛒 {{ c.code }}</span>
          </div>
          <p v-else class="text-xs text-orange-500">Toplamaya başlamadan en az bir araba bağlayın.</p>
        </div>

        <!-- Toplama satırları -->
        <div v-if="canPick" class="space-y-3">
          <div v-for="grp in lineGroups" :key="grp.label" class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
            <div class="px-3 py-1.5 bg-gray-100 dark:bg-white/[0.06] text-[11px] font-bold uppercase text-gray-500">{{ grp.label }} ({{ grp.items.length }})</div>
            <div class="divide-y divide-gray-100 dark:divide-gray-800">
              <div v-for="it in grp.items" :key="it.lineId" class="p-3">
                <div class="flex items-start justify-between gap-2">
                  <div class="flex-1 min-w-0">
                    <p class="text-sm font-semibold text-gray-900 dark:text-gray-100">{{ it.stockName }}</p>
                    <div v-if="matched(it.stockName).length" class="flex flex-wrap gap-1 mt-1">
                      <span v-for="kw in matched(it.stockName)" :key="kw.id" class="px-1.5 py-0.5 rounded text-[10px] font-extrabold text-white" :style="{ backgroundColor: kw.color }">{{ kw.keyword }}</span>
                    </div>
                    <p class="text-[11px] text-gray-400 mt-0.5">Sipariş: {{ it.orderedQty }} {{ it.unit }}</p>
                  </div>
                  <input v-model.number="it.qty" type="number" min="0" class="w-20 px-2 py-1.5 text-center text-sm rounded-lg border bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100"
                         :class="it.qty < it.orderedQty ? 'border-orange-400' : 'border-gray-300 dark:border-gray-700'" />
                </div>
                <div v-if="it.qty !== it.orderedQty" class="mt-2">
                  <label class="block text-[10px] font-bold text-orange-500 uppercase mb-1">Fark Nedeni <span v-if="it.qty < it.orderedQty" class="text-red-500">*</span></label>
                  <DifferenceReasonInput v-model="it.reason" />
                </div>
              </div>
            </div>
          </div>
        </div>
      </template>
    </template>

    <!-- Aktif iş alt aksiyon çubuğu -->
    <div v-if="active && active.pickingMode != null" class="fixed bottom-0 inset-x-0 bg-white dark:bg-gray-800 border-t dark:border-gray-700 p-3 flex gap-2 max-w-2xl mx-auto">
      <button @click="togglePause" class="px-3 py-3 bg-amber-50 text-amber-700 font-bold rounded-xl text-sm">{{ active.paused ? 'Devam' : 'Duraklat' }}</button>
      <button v-if="canPick" @click="saveProgress" :disabled="busy" class="px-3 py-3 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-200 font-bold rounded-xl text-sm">Kaydet</button>
      <button v-if="canPick" @click="finishPicking" :disabled="busy" class="flex-1 py-3 bg-purple-600 disabled:opacity-50 text-white font-bold rounded-xl text-sm">Toplama Bitti</button>
    </div>

    <!-- Toplama bitti modalı -->
    <div v-if="finishOpen" class="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
      <div class="bg-white dark:bg-gray-900 rounded-xl p-4 w-full max-w-sm space-y-3">
        <h3 class="font-bold text-gray-900 dark:text-gray-100">Toplama Bitti</h3>
        <template v-if="active && active.pickingMode === 0">
          <p class="text-xs text-gray-500">Bağlı arabaları onaylayın:</p>
          <div class="flex flex-wrap gap-1.5">
            <span v-for="c in containers" :key="c.containerAssignmentId" class="px-2 py-1 rounded-lg text-xs font-bold bg-sky-100 text-sky-700">🛒 {{ c.code }}</span>
            <span v-if="!containers.length" class="text-xs text-red-500">Bağlı araba yok!</span>
          </div>
        </template>
        <template v-else-if="active && active.pickingMode === 1">
          <label class="block text-xs font-bold text-gray-500 uppercase">Palet Sayısı *</label>
          <input v-model.number="palletCount" type="number" min="1" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-700 dark:bg-gray-800 text-sm" />
        </template>
        <div class="flex gap-2 pt-1">
          <button @click="finishOpen = false" class="flex-1 py-2 bg-gray-100 dark:bg-gray-700 rounded-lg text-sm font-bold">Vazgeç</button>
          <button @click="confirmFinish" :disabled="busy" class="flex-1 py-2 bg-purple-600 disabled:opacity-50 text-white rounded-lg text-sm font-bold">Onayla</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import QrScanInput from '../components/QrScanInput.vue';
import DifferenceReasonInput from '../components/DifferenceReasonInput.vue';
import clothingPickingService, { type PickingQueue, type PickingQueueItem, type ShipmentContainer, PickingModeLabels } from '../services/clothingPickingService';
import clothingPrepService from '../services/clothingPrepService';
import pickingGroupService, { type PickingGroup } from '../services/pickingGroupService';
import clothingKeywordService, { type ClothingKeyword } from '../services/clothingKeywordService';
import { turkishLower } from '../utils/turkishSearch';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const notify = useNotificationStore();
const loading = ref(false);
const busy = ref(false);
const groups = ref<PickingGroup[]>([]);
const selectedGroupId = ref<number | null>(null);
const queue = ref<PickingQueue>({ claimable: [], mine: [] });
const keywords = ref<ClothingKeyword[]>([]);

const active = ref<PickingQueueItem | null>(null);
const containers = ref<ShipmentContainer[]>([]);
const finishOpen = ref(false);
const palletCount = ref<number | null>(null);

interface LineState { lineId: number; stockName: string; orderedQty: number; unit: string; clothingType: number | null | undefined; qty: number; reason: string; }
const lines = ref<LineState[]>([]);

const modeLabel = (m: number) => PickingModeLabels[m] ?? '?';
const groupChips = computed(() => [
  ...groups.value.filter(g => g.isActive).map(g => ({ key: 'g' + g.id, id: g.id as number | null, name: g.name })),
  { key: 'none', id: null as number | null, name: 'Gruplandırılmamış' },
]);
const canPick = computed(() => !!active.value && active.value.pickingMode != null && (active.value.pickingMode !== 0 || containers.value.length > 0));
const lineGroups = computed(() => {
  const shoes = lines.value.filter(l => l.clothingType === 1);
  const other = lines.value.filter(l => l.clothingType !== 1);
  const out: { label: string; items: LineState[] }[] = [];
  if (shoes.length) out.push({ label: 'Ayakkabı', items: shoes });
  if (other.length) out.push({ label: 'Diğer', items: other });
  return out;
});

const norm = (s: string) => turkishLower(s || '').replace(/\s+/g, '');
const matched = (name: string) => { const n = norm(name); return keywords.value.filter(k => k.isActive && n.includes(norm(k.keyword))); };

async function loadGroups() {
  try { groups.value = await pickingGroupService.getAll(true); } catch { /* sessiz */ }
}
async function selectGroup(id: number | null) {
  selectedGroupId.value = id;
  await loadQueue();
}
async function loadQueue() {
  loading.value = true;
  try { queue.value = await clothingPickingService.queue(selectedGroupId.value); }
  catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Yüklenemedi.', 'error'); }
  finally { loading.value = false; }
}

async function claimNext() {
  const first = queue.value.claimable[0];
  if (first) await claim(first);
}
async function claim(it: PickingQueueItem) {
  busy.value = true;
  try {
    await clothingPickingService.claim(it.shipmentId);
    await loadQueue();
    const mineItem = queue.value.mine.find(m => m.shipmentId === it.shipmentId);
    if (mineItem) await openJob(mineItem);
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'İş alınamadı.', 'warning');
    await loadQueue();
  } finally { busy.value = false; }
}

async function openJob(it: PickingQueueItem) {
  active.value = { ...it };
  await loadSession();
}
function closeJob() { active.value = null; lines.value = []; containers.value = []; }

async function loadSession() {
  if (!active.value) return;
  try {
    const [pl, cs, kws] = await Promise.all([
      clothingPrepService.pickList(active.value.shipmentId),
      clothingPickingService.containers(active.value.shipmentId),
      keywords.value.length ? Promise.resolve(keywords.value) : clothingKeywordService.getAll(true),
    ]);
    keywords.value = kws;
    containers.value = cs;
    lines.value = pl.lines.map(l => ({
      lineId: l.lineId, stockName: l.stockName, orderedQty: l.orderedQty, unit: l.unit,
      clothingType: l.clothingType, qty: l.deliveredQty, reason: l.differenceReason ?? '',
    }));
  } catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Yüklenemedi.', 'error'); }
}

async function chooseMode(m: number) {
  if (!active.value) return;
  busy.value = true;
  try {
    await clothingPickingService.setMode(active.value.shipmentId, m);
    active.value.pickingMode = m;
  } catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Hata.', 'error'); }
  finally { busy.value = false; }
}

async function bindContainer(code: string) {
  if (!active.value) return;
  try {
    await clothingPickingService.scanContainer(active.value.shipmentId, code);
    containers.value = await clothingPickingService.containers(active.value.shipmentId);
    notify.add(`Araba bağlandı: ${code}`, 'success');
  } catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Araba bağlanamadı.', 'error'); }
}

function payloadLines() {
  return lines.value.map(l => ({
    shipmentLineId: l.lineId, deliveredQty: l.qty,
    differenceReason: l.qty !== l.orderedQty ? l.reason.trim() : null,
  }));
}

async function saveProgress() {
  if (!active.value) return;
  busy.value = true;
  try { await clothingPickingService.saveProgress(active.value.shipmentId, payloadLines()); notify.add('Kaydedildi.', 'success'); }
  catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Kaydedilemedi.', 'error'); }
  finally { busy.value = false; }
}

async function togglePause() {
  if (!active.value) return;
  try {
    if (active.value.paused) { await clothingPickingService.resume(active.value.shipmentId); active.value.paused = false; }
    else { await clothingPickingService.pause(active.value.shipmentId); active.value.paused = true; }
  } catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Hata.', 'error'); }
}

function finishPicking() {
  const short = lines.value.find(l => l.qty < l.orderedQty && !l.reason.trim());
  if (short) { notify.add(`"${short.stockName}" için fark nedeni giriniz.`, 'warning'); return; }
  palletCount.value = null;
  finishOpen.value = true;
}
async function confirmFinish() {
  if (!active.value) return;
  if (active.value.pickingMode === 1 && (!palletCount.value || palletCount.value <= 0)) { notify.add('Palet sayısı giriniz.', 'warning'); return; }
  if (active.value.pickingMode === 0 && containers.value.length === 0) { notify.add('Bağlı araba yok.', 'warning'); return; }
  busy.value = true;
  try {
    await clothingPickingService.completePicking(active.value.shipmentId, payloadLines(), active.value.pickingMode === 0, palletCount.value);
    finishOpen.value = false;
    notify.add('Toplama tamamlandı.', 'success');
    closeJob();
    await loadQueue();
  } catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Tamamlanamadı.', 'error'); }
  finally { busy.value = false; }
}

onMounted(async () => { await loadGroups(); await loadQueue(); });
</script>
