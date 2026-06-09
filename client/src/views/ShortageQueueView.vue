<template>
  <div class="p-4 max-w-4xl mx-auto space-y-4">
    <PageHeader title="Eksik Ürün Kuyruğu">
      <template #description>Eksik kalan ürünler; "Gönder" ile proje/sipariş bazında tamamlama sevkiyatı oluşturulur.</template>
    </PageHeader>

    <!-- Filtreler -->
    <div class="flex flex-wrap items-center gap-2">
      <select v-model.number="statusFilter" @change="load" class="border rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:border-gray-700">
        <option :value="0">Beklemede</option>
        <option :value="1">Gönderim İstendi</option>
        <option :value="3">Gönderildi</option>
        <option :value="9">İptal</option>
      </select>
      <input v-model="search" placeholder="Proje / stok ara..." class="flex-1 min-w-[160px] border rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:border-gray-700" />
      <button @click="load" class="px-3 py-2 text-sm font-medium bg-gray-100 dark:bg-gray-700 rounded-lg">Yenile</button>
    </div>

    <!-- Gönder çubuğu -->
    <div v-if="selectedPending.length" class="bg-purple-50 dark:bg-purple-900/20 rounded-xl p-3 flex items-center justify-between">
      <span class="text-sm text-purple-800 dark:text-purple-200">{{ selectedPending.length }} eksik seçili — proje/sipariş bazında gruplanarak gönderilecek.</span>
      <button @click="dispatch" :disabled="busy" class="px-4 py-2 bg-purple-600 disabled:opacity-50 text-white font-bold rounded-lg text-sm">Gönder</button>
    </div>

    <div v-if="loading" class="text-center py-12 text-gray-400 text-sm">Yükleniyor...</div>
    <div v-else-if="filtered.length === 0" class="text-center py-12 text-gray-400 text-sm">Kayıt yok.</div>

    <div v-else class="space-y-2">
      <div v-for="r in filtered" :key="r.id" class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-3 flex items-center gap-3">
        <input v-if="r.status === 0" type="checkbox" :checked="selected.has(r.id)" @change="toggle(r.id)" class="h-5 w-5 rounded text-purple-600" />
        <div class="flex-1 min-w-0">
          <div class="flex flex-wrap items-center gap-1.5">
            <span class="font-semibold text-gray-900 dark:text-gray-100">{{ r.stockName }}</span>
            <span class="text-[11px] text-gray-400">({{ r.stockCode }})</span>
            <span class="px-1.5 py-0.5 rounded text-[10px] font-bold" :class="statusClass(r.status)">{{ statusLabel(r.status) }}</span>
          </div>
          <div class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
            <span class="font-bold text-red-600">{{ r.qty }}</span> eksik · {{ r.projectName }}
            <span v-if="r.externalOrderNumber"> · {{ r.externalOrderNumber }}</span>
            <span v-if="r.followupShipmentId"> · →
              <router-link :to="`/shipments/${r.followupShipmentId}`" class="text-purple-600 underline">tamamlama #{{ r.followupShipmentId }}</router-link>
            </span>
          </div>
          <div v-if="r.note" class="text-[11px] text-gray-400 mt-0.5">{{ r.note }}</div>
        </div>
        <button v-if="r.status === 0 || r.status === 1" @click="askCancel(r)" class="px-2 py-1.5 text-xs font-bold text-red-600 bg-red-50 dark:bg-red-900/20 rounded">İptal</button>
      </div>
    </div>

    <!-- İptal sebep modalı -->
    <div v-if="cancelTarget" class="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
      <div class="bg-white dark:bg-gray-900 rounded-xl p-4 w-full max-w-sm space-y-3">
        <h3 class="font-bold text-gray-900 dark:text-gray-100">Eksik Kaydı İptal</h3>
        <p class="text-xs text-gray-500">{{ cancelTarget.stockName }} — sebep zorunlu.</p>
        <textarea v-model="cancelReason" rows="3" class="w-full border rounded px-2 py-1.5 text-sm dark:bg-gray-800 dark:border-gray-700" placeholder="Sebep..."></textarea>
        <div class="flex gap-2">
          <button @click="cancelTarget = null" class="flex-1 py-2 bg-gray-100 dark:bg-gray-700 rounded-lg text-sm font-bold">Vazgeç</button>
          <button @click="confirmCancel" :disabled="!cancelReason.trim()" class="flex-1 py-2 bg-red-600 disabled:opacity-50 text-white rounded-lg text-sm font-bold">İptal Et</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import shortageService, { type Shortage, ShortageStatusLabels } from '../services/shortageService';
import { turkishLower } from '../utils/turkishSearch';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const notify = useNotificationStore();
const loading = ref(false);
const busy = ref(false);
const rows = ref<Shortage[]>([]);
const statusFilter = ref(0);
const search = ref('');
const selected = ref<Set<number>>(new Set());
const cancelTarget = ref<Shortage | null>(null);
const cancelReason = ref('');

const statusLabel = (s: number) => ShortageStatusLabels[s] ?? String(s);
function statusClass(s: number) {
  return s === 0 ? 'bg-yellow-100 text-yellow-800'
    : s === 1 ? 'bg-sky-100 text-sky-700'
    : s === 3 ? 'bg-green-100 text-green-700'
    : s === 9 ? 'bg-gray-200 text-gray-600'
    : 'bg-gray-100 text-gray-700';
}

const filtered = computed(() => {
  const q = turkishLower(search.value.trim());
  if (!q) return rows.value;
  return rows.value.filter(r => turkishLower(r.projectName).includes(q) || turkishLower(r.stockName).includes(q) || turkishLower(r.stockCode).includes(q));
});
const selectedPending = computed(() => rows.value.filter(r => r.status === 0 && selected.value.has(r.id)));

function toggle(id: number) {
  if (selected.value.has(id)) selected.value.delete(id); else selected.value.add(id);
  selected.value = new Set(selected.value);
}

async function load() {
  loading.value = true;
  try { rows.value = await shortageService.list(statusFilter.value, null); selected.value = new Set(); }
  catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Yüklenemedi.', 'error'); }
  finally { loading.value = false; }
}

async function dispatch() {
  const ids = selectedPending.value.map(r => r.id);
  if (ids.length === 0) return;
  busy.value = true;
  try {
    const res = await shortageService.dispatch(ids);
    notify.add(`${res.shipmentIds.length} tamamlama sevkiyatı oluşturuldu.`, 'success');
    await load();
  } catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'Gönderilemedi.', 'error'); }
  finally { busy.value = false; }
}

function askCancel(r: Shortage) { cancelTarget.value = r; cancelReason.value = ''; }
async function confirmCancel() {
  if (!cancelTarget.value || !cancelReason.value.trim()) return;
  try {
    await shortageService.cancel(cancelTarget.value.id, cancelReason.value.trim());
    cancelTarget.value = null;
    await load();
  } catch (e) { notify.add(ApiErrorUtils.getErrorMessage(e) || 'İptal edilemedi.', 'error'); }
}

onMounted(load);
</script>
