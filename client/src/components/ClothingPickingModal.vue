<template>
  <div class="fixed inset-0 bg-black/50 flex items-end sm:items-center justify-center p-0 sm:p-4 z-50">
    <div class="bg-gray-100 dark:bg-gray-900 w-full h-full sm:h-auto sm:max-h-[92vh] sm:max-w-2xl sm:rounded-2xl flex flex-col overflow-hidden shadow-xl">

      <!-- Header -->
      <div class="bg-white dark:bg-gray-800 px-4 py-3 border-b dark:border-gray-700 flex justify-between items-center">
        <div>
          <h3 class="font-bold text-violet-700 dark:text-violet-300">KIYAFET HAZIRLIK</h3>
          <p v-if="pick" class="text-xs text-gray-500 dark:text-gray-400">
            {{ pick.externalOrderNumber || ('#' + pick.shipmentId) }}
            <span v-if="pick.talepNo"> · T:{{ pick.talepNo }}</span> · {{ pick.projectName }}
          </p>
        </div>
        <button @click="$emit('close')" class="p-2 text-gray-400 hover:text-gray-600">
          <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
        </button>
      </div>

      <!-- Content -->
      <div class="flex-1 overflow-y-auto p-3 space-y-3">
        <div v-if="loading" class="flex justify-center py-16">
          <div class="w-8 h-8 border-4 border-violet-600 border-t-transparent rounded-full animate-spin"></div>
        </div>

        <template v-else>
          <div v-for="group in groups" :key="group.label" class="bg-white dark:bg-gray-800 rounded-xl overflow-hidden border border-gray-200 dark:border-gray-700">
            <div class="px-3 py-1.5 bg-gray-100 dark:bg-white/[0.06] text-[11px] font-bold uppercase tracking-wide text-gray-500 dark:text-gray-400">
              {{ group.label }} ({{ group.items.length }})
            </div>
            <div class="divide-y divide-gray-100 dark:divide-gray-700">
              <div v-for="item in group.items" :key="item.lineId" class="p-3">
                <div class="flex items-start justify-between gap-3">
                  <div class="flex-1 min-w-0">
                    <p class="text-sm font-semibold text-gray-900 dark:text-gray-100 leading-snug">{{ item.stockName }}</p>
                    <div v-if="matchedKeywords(item.stockName).length" class="flex flex-wrap gap-1 mt-1">
                      <span v-for="kw in matchedKeywords(item.stockName)" :key="kw.id"
                            class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-extrabold text-white"
                            :style="{ backgroundColor: kw.color }">{{ kw.keyword }}</span>
                    </div>
                    <p class="text-[11px] text-gray-400 mt-0.5">Sipariş: {{ item.orderedQty }} {{ item.unit }}</p>
                  </div>
                  <div class="w-24 flex-shrink-0">
                    <label class="block text-[10px] text-gray-400 text-right mb-0.5">Hazırlanan</label>
                    <input v-model.number="item.qty" @input="clamp(item)" type="number" min="0" step="1"
                           class="w-full px-2 py-1.5 text-center text-sm rounded-lg border bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-violet-400"
                           :class="item.qty < item.orderedQty ? 'border-orange-400' : 'border-gray-300 dark:border-gray-700'" />
                  </div>
                </div>
                <!-- Eksik → sebep -->
                <div v-if="item.qty !== item.orderedQty" class="mt-2">
                  <label class="block text-[10px] font-bold text-orange-500 uppercase mb-1">
                    Fark Nedeni <span v-if="item.qty < item.orderedQty" class="text-red-500">*</span>
                  </label>
                  <DifferenceReasonInput v-model="item.reason" />
                </div>
              </div>
            </div>
          </div>
        </template>
      </div>

      <!-- Footer -->
      <div class="bg-white dark:bg-gray-800 border-t dark:border-gray-700 p-3 space-y-2">
        <div class="flex items-center gap-2">
          <label class="text-sm font-medium text-gray-700 dark:text-gray-300 whitespace-nowrap">Koli Sayısı</label>
          <input v-model="koliCount" type="text" placeholder="Örn: 3 koli + 1 poşet"
                 class="flex-1 px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-violet-400" />
        </div>
        <div class="flex gap-2">
          <button @click="$emit('close')" class="flex-1 py-3 bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 font-bold rounded-xl text-sm">KAPAT</button>
          <button @click="complete" :disabled="submitting"
                  class="flex-[2] py-3 bg-violet-600 hover:bg-violet-700 disabled:opacity-50 text-white font-bold rounded-xl text-sm flex items-center justify-center gap-2">
            <span v-if="submitting" class="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            {{ submitting ? 'Kaydediliyor...' : 'Hazırlığı Tamamla' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import clothingPrepService, { type ClothingPickLine } from '../services/clothingPrepService';
import clothingKeywordService, { type ClothingKeyword } from '../services/clothingKeywordService';
import DifferenceReasonInput from './DifferenceReasonInput.vue';
import { turkishLower } from '../utils/turkishSearch';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const props = defineProps<{ shipmentId: number }>();
const emit = defineEmits<{ (e: 'close'): void; (e: 'completed'): void }>();

const notify = useNotificationStore();
const loading = ref(false);
const submitting = ref(false);
const pick = ref<Awaited<ReturnType<typeof clothingPrepService.pickList>> | null>(null);
const keywords = ref<ClothingKeyword[]>([]);
const koliCount = ref('');

interface ItemState {
  lineId: number; stockCode: string; stockName: string; orderedQty: number;
  unit: string; clothingType: number | null | undefined; qty: number; reason: string;
}
const items = ref<ItemState[]>([]);

const groups = computed(() => {
  const shoes = items.value.filter(i => i.clothingType === 1);
  const other = items.value.filter(i => i.clothingType !== 1);
  const out: { label: string; items: ItemState[] }[] = [];
  if (shoes.length) out.push({ label: 'Ayakkabı', items: shoes });
  if (other.length) out.push({ label: 'Diğer', items: other });
  return out;
});

// Boşluk + Türkçe normalize (kısakol = kısa kol)
const norm = (s: string) => turkishLower(s || '').replace(/\s+/g, '');

function matchedKeywords(stockName: string): ClothingKeyword[] {
  const n = norm(stockName);
  return keywords.value.filter(k => k.isActive && n.includes(norm(k.keyword)));
}

function clamp(item: ItemState) {
  if (item.qty == null || isNaN(item.qty) || item.qty < 0) item.qty = 0;
}

async function load() {
  loading.value = true;
  try {
    const [pl, kws] = await Promise.all([
      clothingPrepService.pickList(props.shipmentId),
      clothingKeywordService.getAll(true),
    ]);
    pick.value = pl;
    keywords.value = kws;
    koliCount.value = pl.koliCount ?? '';
    items.value = pl.lines.map((l: ClothingPickLine) => ({
      lineId: l.lineId,
      stockCode: l.stockCode,
      stockName: l.stockName,
      orderedQty: l.orderedQty,
      unit: l.unit,
      clothingType: l.clothingType,
      // İlk açılışta toplanan miktar girilmemişse sipariş miktarı (tam) varsayılır.
      qty: l.deliveredQty > 0 ? l.deliveredQty : l.orderedQty,
      reason: l.differenceReason ?? '',
    }));
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Yüklenemedi.', 'error');
    emit('close');
  } finally {
    loading.value = false;
  }
}

async function complete() {
  // Eksik/fark satırlarda sebep zorunlu
  const missing = items.value.find(i => i.qty !== i.orderedQty && !i.reason.trim());
  if (missing) {
    notify.add(`"${missing.stockName}" için fark nedeni giriniz.`, 'warning');
    return;
  }
  submitting.value = true;
  try {
    await clothingPrepService.complete(
      props.shipmentId,
      koliCount.value.trim() || null,
      items.value.map(i => ({
        shipmentLineId: i.lineId,
        deliveredQty: i.qty,
        differenceReason: i.qty !== i.orderedQty ? i.reason.trim() : null,
      })),
    );
    notify.add('Hazırlık tamamlandı.', 'success');
    emit('completed');
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Kaydedilemedi.', 'error');
  } finally {
    submitting.value = false;
  }
}

onMounted(load);
</script>
