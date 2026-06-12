<!--
  ResponsiveTable.vue — Akyıldız ortak tablo bileşeni
  ===================================================
  Tek bir kolon + satır tanımından HEM masaüstü tablo HEM telefon kartı üretir.
  Yana kaydırma yerine telefonda her satır temiz bir karta dönüşür.

  Kullanım (örnek README'de):
    - :columns ve :rows ver, row-key belirt.
    - Hücre özelleştirme: column.key adında bir slot tanımla (ör. #durum).
    - Satır işlemleri: #actions slot'u (her iki düzende sağda görünür).

  columns: Array of {
    key: string;            // row[key] alanı + slot adı
    label: string;          // başlık
    align?: 'left'|'right'|'center';   // varsayılan left
    priority?: boolean;     // true → mobil kartta ÜST satırda (başlık) gösterilir
    hideOnMobile?: boolean; // mobil kartta hiç gösterme
    mono?: boolean;         // monospace
    class?: string;         // hücre ekstra sınıf
  }>
-->
<template>
  <div>
    <!-- ── Masaüstü tablo (md ve üstü) ── -->
    <div class="hidden md:block bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-x-auto">
      <table class="w-full">
        <thead>
          <tr class="bg-gray-50 dark:bg-gray-800/60 border-b border-gray-200 dark:border-gray-700">
            <th
              v-for="col in columns"
              :key="col.key"
              class="px-5 py-3 text-[11px] font-bold uppercase tracking-wider text-gray-500 dark:text-gray-400 whitespace-nowrap"
              :class="[alignClass(col.align), col.sortable ? 'cursor-pointer select-none hover:text-gray-700 dark:hover:text-gray-200' : '']"
              @click="col.sortable && onSort(col.key)"
            >{{ col.label }}<span v-if="col.sortable" class="ml-1">{{ sortIndicator(col.key) }}</span></th>
            <th v-if="$slots.actions" class="px-5 py-3 text-right text-[11px] font-bold uppercase tracking-wider text-gray-500 dark:text-gray-400">İşlem</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="row in displayRows"
            :key="row[rowKey]"
            class="border-b border-gray-100 dark:border-gray-800 last:border-0 hover:bg-gray-50/60 dark:hover:bg-white/[0.02] transition-colors"
          >
            <td
              v-for="col in columns"
              :key="col.key"
              class="px-5 py-3 text-sm text-gray-700 dark:text-gray-300 whitespace-nowrap"
              :class="[alignClass(col.align), col.mono ? 'font-mono text-xs' : '', col.class]"
            >
              <slot :name="col.key" :row="row" :value="row[col.key]">{{ formatValue(row[col.key]) }}</slot>
            </td>
            <td v-if="$slots.actions" class="px-5 py-3 text-right whitespace-nowrap">
              <div class="flex justify-end items-center gap-2"><slot name="actions" :row="row" /></div>
            </td>
          </tr>
          <tr v-if="rows.length === 0">
            <td :colspan="columns.length + ($slots.actions ? 1 : 0)" class="px-5 py-10 text-center text-sm text-gray-400">{{ emptyText }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- ── Telefon kartları (md altı) ── -->
    <div class="md:hidden space-y-3">
      <p v-if="rows.length === 0" class="text-center text-sm text-gray-400 py-8">{{ emptyText }}</p>
      <div
        v-for="row in displayRows"
        :key="'m-' + row[rowKey]"
        class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4"
      >
        <!-- başlık satırı: priority kolonlar -->
        <div class="flex items-start justify-between gap-3 mb-2">
          <div class="min-w-0">
            <template v-for="col in priorityCols" :key="col.key">
              <div class="font-bold text-gray-900 dark:text-gray-100 truncate" :class="col.mono ? 'font-mono text-sm' : 'text-[15px]'">
                <slot :name="col.key" :row="row" :value="row[col.key]">{{ formatValue(row[col.key]) }}</slot>
              </div>
            </template>
          </div>
          <div v-if="$slots.actions" class="shrink-0 flex items-center gap-2"><slot name="actions" :row="row" /></div>
        </div>
        <!-- diğer alanlar: etiket + değer ızgarası -->
        <dl class="grid grid-cols-2 gap-x-3 gap-y-1.5">
          <template v-for="col in detailCols" :key="col.key">
            <div class="contents">
              <dt class="text-[11px] font-semibold text-gray-400 dark:text-gray-500 uppercase tracking-wide self-center">{{ col.label }}</dt>
              <dd class="text-sm text-gray-700 dark:text-gray-300 text-right truncate" :class="col.mono ? 'font-mono text-xs' : ''">
                <slot :name="col.key" :row="row" :value="row[col.key]">{{ formatValue(row[col.key]) }}</slot>
              </dd>
            </div>
          </template>
        </dl>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';

export interface ResponsiveColumn {
  key: string;
  label: string;
  align?: 'left' | 'right' | 'center';
  priority?: boolean;
  hideOnMobile?: boolean;
  mono?: boolean;
  sortable?: boolean;   // true → başlık tıklanabilir, istemci tarafı sıralama
  class?: string;
}

const props = withDefaults(defineProps<{
  columns: ResponsiveColumn[];
  rows: Array<Record<string, any>>;
  rowKey?: string;
  emptyText?: string;
  defaultSort?: string;   // başlangıç sıralaması (sortable bir kolon key'i)
}>(), {
  rowKey: 'id',
  emptyText: 'Kayıt bulunamadı.',
});

const priorityCols = computed(() => {
  const p = props.columns.filter(c => c.priority && !c.hideOnMobile);
  return p.length ? p : props.columns.slice(0, 1); // priority yoksa ilk kolon başlık
});
const detailCols = computed(() => {
  const prio = new Set(priorityCols.value.map(c => c.key));
  return props.columns.filter(c => !prio.has(c.key) && !c.hideOnMobile);
});

// ── İstemci tarafı sıralama (yalnızca sortable kolonlar) ──
const sortBy = ref<string | null>(props.defaultSort ?? null);
const sortDir = ref<'asc' | 'desc'>('asc');

function onSort(key: string) {
  if (sortBy.value === key) {
    sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc';
  } else {
    sortBy.value = key;
    sortDir.value = 'asc';
  }
}
function sortIndicator(key: string) {
  if (sortBy.value !== key) return '↕';
  return sortDir.value === 'asc' ? '↑' : '↓';
}

const displayRows = computed(() => {
  if (!sortBy.value) return props.rows;
  const key = sortBy.value;
  const dir = sortDir.value === 'asc' ? 1 : -1;
  return [...props.rows].sort((a, b) => {
    const av = a[key] ?? '';
    const bv = b[key] ?? '';
    if (typeof av === 'number' && typeof bv === 'number') return (av - bv) * dir;
    return String(av).localeCompare(String(bv), 'tr') * dir;
  });
});

function alignClass(a?: string) {
  return a === 'right' ? 'text-right' : a === 'center' ? 'text-center' : 'text-left';
}
function formatValue(v: unknown) {
  return v === null || v === undefined || v === '' ? '—' : String(v);
}
</script>
