<template>
  <div class="p-4 max-w-3xl mx-auto space-y-4">
    <PageHeader title="Sebep Tanımları" color="blue">
      <template #description>
        Sevkiyat hazırlık (fark) ve mal kabul (red) ekranlarında çıkan sebep seçeneklerini buradan yönetin.
        Yeni bir sebep eklediğinizde ilgili ekranlarda otomatik görünür.
      </template>
    </PageHeader>

    <!-- Kategori sekmeleri -->
    <div class="flex gap-2">
      <button
        v-for="c in categories" :key="c.value"
        @click="activeCategory = c.value"
        class="px-4 py-2 rounded-lg text-sm font-semibold border transition-colors"
        :class="activeCategory === c.value
          ? 'bg-blue-600 text-white border-blue-600'
          : 'bg-white dark:bg-gray-900 text-gray-600 dark:text-gray-300 border-gray-200 dark:border-gray-700 hover:border-blue-400'"
      >{{ c.label }}</button>
    </div>

    <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 space-y-4">
      <!-- Yeni ekle -->
      <div class="flex flex-wrap items-end gap-3 border-b border-gray-100 dark:border-gray-800 pb-4">
        <div class="flex-1 min-w-[200px]">
          <label class="block text-xs font-bold text-gray-500 uppercase mb-1">Sebep</label>
          <input v-model="newLabel" type="text" placeholder="Örn: Stokta yok"
                 @keyup.enter="addReason"
                 class="w-full border rounded px-3 py-2 text-sm dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
        </div>
        <button @click="addReason" :disabled="!newLabel.trim() || saving"
                class="px-4 py-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white font-semibold rounded-lg text-sm">
          Ekle
        </button>
      </div>

      <div v-if="loading" class="text-center py-8 text-gray-400 text-sm">Yükleniyor...</div>
      <div v-else-if="filtered.length === 0" class="text-center py-8 text-gray-400 text-sm">Bu kategoride henüz sebep yok.</div>

      <div v-else class="space-y-2">
        <div v-for="r in filtered" :key="r.id"
             class="flex items-center gap-3 p-2 rounded-lg border border-gray-100 dark:border-gray-800">
          <input v-model="r.label" type="text"
                 class="flex-1 border rounded px-2 py-1.5 text-sm dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
          <div class="flex items-center gap-1">
            <label class="text-[10px] font-bold text-gray-400 uppercase">Sıra</label>
            <input v-model.number="r.sortOrder" type="number"
                   class="w-16 border rounded px-2 py-1.5 text-sm dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
          </div>
          <label class="flex items-center gap-1 text-xs text-gray-600 dark:text-gray-300">
            <input type="checkbox" v-model="r.isActive" /> Aktif
          </label>
          <button @click="saveReason(r)" class="px-3 py-1.5 text-xs font-bold text-blue-600 bg-blue-50 dark:bg-blue-900/30 rounded">Kaydet</button>
          <button @click="removeReason(r)" class="px-2 py-1.5 text-xs font-bold text-red-600 bg-red-50 dark:bg-red-900/20 rounded">Sil</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import { definedReasonService, type DefinedReason } from '../services/definedReasonService';
import { ReasonCategory } from '../composables/useDefinedReasons';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const notify = useNotificationStore();

const categories = [
  { value: ReasonCategory.PickingDifference, label: 'Sevkiyat Hazırlık (Fark)' },
  { value: ReasonCategory.GoodsReceiptReject, label: 'Mal Kabul (Red)' },
];

const activeCategory = ref<number>(ReasonCategory.PickingDifference);
const reasons = ref<DefinedReason[]>([]);
const loading = ref(false);
const saving = ref(false);
const newLabel = ref('');

const filtered = computed(() => reasons.value.filter(r => r.category === activeCategory.value));

async function load() {
  loading.value = true;
  try {
    reasons.value = await definedReasonService.getAll();
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
}

async function addReason() {
  if (!newLabel.value.trim()) return;
  saving.value = true;
  try {
    await definedReasonService.create({
      category: activeCategory.value,
      label: newLabel.value.trim(),
      sortOrder: filtered.value.length,
    });
    newLabel.value = '';
    await load();
    notify.add('Sebep eklendi.', 'success');
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Eklenemedi.', 'error');
  } finally {
    saving.value = false;
  }
}

async function saveReason(r: DefinedReason) {
  if (!r.label.trim()) {
    notify.add('Sebep boş olamaz.', 'warning');
    return;
  }
  try {
    await definedReasonService.update(r.id, {
      id: r.id,
      category: r.category,
      label: r.label.trim(),
      sortOrder: r.sortOrder,
      isActive: r.isActive,
    });
    notify.add('Güncellendi.', 'success');
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Güncellenemedi.', 'error');
  }
}

async function removeReason(r: DefinedReason) {
  if (!await notify.promptConfirm({ title: 'Sil', message: `"${r.label}" silinsin mi?`, confirmText: 'Sil', type: 'danger' })) return;
  try {
    await definedReasonService.remove(r.id);
    await load();
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Silinemedi.', 'error');
  }
}

onMounted(load);
</script>
