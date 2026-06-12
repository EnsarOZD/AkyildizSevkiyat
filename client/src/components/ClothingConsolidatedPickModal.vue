<template>
  <div class="fixed inset-0 bg-black/50 flex items-end sm:items-center justify-center p-0 sm:p-4 z-50">
    <div class="bg-gray-100 dark:bg-gray-900 w-full h-full sm:h-auto sm:max-h-[92vh] sm:max-w-2xl sm:rounded-2xl flex flex-col overflow-hidden shadow-xl">

      <div class="bg-white dark:bg-gray-800 px-4 py-3 border-b dark:border-gray-700 flex justify-between items-center">
        <div>
          <h3 class="font-bold text-violet-700 dark:text-violet-300">KONSOLİDE TOPLAMA</h3>
          <p class="text-xs text-gray-500 dark:text-gray-400">{{ shipmentIds.length }} irsaliye · ürün bazında toplam</p>
        </div>
        <button @click="$emit('close')" class="p-2 text-gray-400 hover:text-gray-600">
          <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
        </button>
      </div>

      <div class="flex-1 overflow-y-auto p-3 space-y-3">
        <div v-if="loading" class="flex justify-center py-16">
          <div class="w-8 h-8 border-4 border-violet-600 border-t-transparent rounded-full animate-spin"></div>
        </div>

        <template v-else>
          <!-- Ürün toplamları -->
          <div v-for="group in groups" :key="group.label" class="bg-white dark:bg-gray-800 rounded-xl overflow-hidden border border-gray-200 dark:border-gray-700">
            <div class="px-3 py-1.5 bg-gray-100 dark:bg-white/[0.06] text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-gray-400">
              {{ group.label }} ({{ group.items.length }})
            </div>
            <div class="divide-y divide-gray-100 dark:divide-gray-700">
              <div v-for="p in group.items" :key="p.stockCode" class="p-3">
                <div class="flex items-start justify-between gap-3">
                  <div class="flex-1 min-w-0">
                    <p class="text-sm font-semibold text-gray-900 dark:text-gray-100 leading-snug">{{ p.stockName }}</p>
                    <div v-if="matchedKeywords(p.stockName).length" class="flex flex-wrap gap-1 mt-1">
                      <span v-for="kw in matchedKeywords(p.stockName)" :key="kw.id"
                            class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-extrabold text-white"
                            :style="{ backgroundColor: kw.color }">{{ kw.keyword }}</span>
                    </div>
                    <p class="text-[11px] text-gray-400 mt-0.5">Toplam Sipariş: {{ p.totalOrderedQty }} {{ p.unit }} ({{ p.refs.length }} irsaliye)</p>
                  </div>
                  <div class="w-24 flex-shrink-0">
                    <label class="block text-[10px] text-gray-400 text-right mb-0.5">Toplanan</label>
                    <input v-model.number="p.picked" @input="clamp(p)" type="number" min="0" step="1"
                           class="w-full px-2 py-1.5 text-center text-sm rounded-lg border bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-violet-400"
                           :class="p.picked < p.totalOrderedQty ? 'border-orange-400' : 'border-gray-300 dark:border-gray-700'" />
                  </div>
                </div>
                <div v-if="p.picked < p.totalOrderedQty" class="mt-2">
                  <label class="block text-[10px] font-bold text-orange-500 uppercase mb-1">Fark Nedeni <span class="text-red-500">*</span></label>
                  <DifferenceReasonInput v-model="p.reason" />
                </div>
              </div>
            </div>
          </div>

          <!-- İrsaliye bazında koli -->
          <div class="bg-white dark:bg-gray-800 rounded-xl overflow-hidden border border-gray-200 dark:border-gray-700">
            <div class="px-3 py-1.5 bg-gray-100 dark:bg-white/[0.06] text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-gray-400">
              İrsaliyeler — Koli Sayısı
            </div>
            <div class="divide-y divide-gray-100 dark:divide-gray-700">
              <div v-for="s in data?.shipments ?? []" :key="s.shipmentId" class="p-3 flex items-center gap-3">
                <div class="flex-1 min-w-0">
                  <p class="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
                    {{ s.externalOrderNumber || ('#' + s.shipmentId) }}<span v-if="s.talepNo" class="text-gray-400"> · T:{{ s.talepNo }}</span>
                  </p>
                  <p class="text-[11px] text-gray-400 truncate">{{ s.projectName }}</p>
                </div>
                <input v-model="koli[s.shipmentId]" type="text" placeholder="Koli"
                       class="w-32 px-2 py-1.5 text-sm rounded-lg border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-violet-400" />
              </div>
            </div>
          </div>
        </template>
      </div>

      <div class="bg-white dark:bg-gray-800 border-t dark:border-gray-700 p-3 flex gap-2">
        <button @click="$emit('close')" class="flex-1 py-3 bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 font-bold rounded-xl text-sm">KAPAT</button>
        <button @click="distributeAndComplete" :disabled="submitting || loading"
                class="flex-[2] py-3 bg-violet-600 hover:bg-violet-700 disabled:opacity-50 text-white font-bold rounded-xl text-sm flex items-center justify-center gap-2">
          <span v-if="submitting" class="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          {{ submitting ? 'İşleniyor...' : 'Dağıt ve Tamamla' }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue';
import clothingPrepService, { type ClothingAggregatePickList } from '../services/clothingPrepService';
import clothingKeywordService, { type ClothingKeyword } from '../services/clothingKeywordService';
import DifferenceReasonInput from './DifferenceReasonInput.vue';
import { turkishLower } from '../utils/turkishSearch';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const props = defineProps<{ shipmentIds: number[] }>();
const emit = defineEmits<{ (e: 'close'): void; (e: 'completed'): void }>();

const notify = useNotificationStore();
const loading = ref(false);
const submitting = ref(false);
const data = ref<ClothingAggregatePickList | null>(null);
const keywords = ref<ClothingKeyword[]>([]);
const koli = reactive<Record<number, string>>({});

interface ProductState {
  stockCode: string; stockName: string; unit: string; clothingType: number | null | undefined;
  totalOrderedQty: number; picked: number; reason: string; refs: { shipmentId: number; lineId: number; orderedQty: number }[];
}
const products = ref<ProductState[]>([]);

const groups = computed(() => {
  const shoes = products.value.filter(p => p.clothingType === 1);
  const other = products.value.filter(p => p.clothingType !== 1);
  const out: { label: string; items: ProductState[] }[] = [];
  if (shoes.length) out.push({ label: 'Ayakkabı', items: shoes });
  if (other.length) out.push({ label: 'Diğer', items: other });
  return out;
});

const norm = (s: string) => turkishLower(s || '').replace(/\s+/g, '');
function matchedKeywords(name: string): ClothingKeyword[] {
  const n = norm(name);
  return keywords.value.filter(k => k.isActive && n.includes(norm(k.keyword)));
}
function clamp(p: ProductState) {
  if (p.picked == null || isNaN(p.picked) || p.picked < 0) p.picked = 0;
}

async function load() {
  loading.value = true;
  try {
    const [agg, kws] = await Promise.all([
      clothingPrepService.aggregatePickList(props.shipmentIds),
      clothingKeywordService.getAll(true),
    ]);
    data.value = agg;
    keywords.value = kws;
    products.value = agg.products.map(p => ({
      stockCode: p.stockCode, stockName: p.stockName, unit: p.unit, clothingType: p.clothingType,
      totalOrderedQty: p.totalOrderedQty,
      picked: p.totalDeliveredQty > 0 ? p.totalDeliveredQty : p.totalOrderedQty,
      reason: '',
      refs: p.refs,
    }));
    for (const s of agg.shipments) koli[s.shipmentId] = s.koliCount ?? '';
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Yüklenemedi.', 'error');
    emit('close');
  } finally {
    loading.value = false;
  }
}

async function distributeAndComplete() {
  const missing = products.value.find(p => p.picked < p.totalOrderedQty && !p.reason.trim());
  if (missing) { notify.add(`"${missing.stockName}" için fark nedeni giriniz.`, 'warning'); return; }

  submitting.value = true;
  try {
    // 1) Toplamları satırlara FIFO dağıt
    await clothingPrepService.distributeAggregate(
      props.shipmentIds,
      products.value.map(p => ({
        stockCode: p.stockCode,
        pickedQty: p.picked,
        differenceReason: p.picked < p.totalOrderedQty ? p.reason.trim() : null,
      })),
    );
    // 2) Her irsaliyeyi koli sayısıyla tamamla (satırlar dağıtımda kaydedildi)
    for (const s of data.value?.shipments ?? []) {
      await clothingPrepService.complete(s.shipmentId, (koli[s.shipmentId] || '').trim() || null, []);
    }
    notify.add(`${(data.value?.shipments ?? []).length} irsaliye hazırlandı.`, 'success');
    emit('completed');
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
  } finally {
    submitting.value = false;
  }
}

onMounted(load);
</script>
