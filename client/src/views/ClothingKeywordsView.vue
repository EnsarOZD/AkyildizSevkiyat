<template>
  <div class="p-4 max-w-3xl mx-auto space-y-4">
    <PageHeader title="Kıyafet Toplama — Vurgu Kelimeleri">
      <template #description>
        Stok adında bu kelimeler geçtiğinde toplama ekranında renkli rozetle gösterilir.
        Eşleşme boşluk/yazım farklarına dayanıklıdır (kısakol = kısa kol).
      </template>
    </PageHeader>

    <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 space-y-4">
      <!-- Add new -->
      <div class="flex flex-wrap items-end gap-3 border-b border-gray-100 dark:border-gray-800 pb-4">
        <div class="flex-1 min-w-[160px]">
          <label class="block text-xs font-bold text-gray-500 uppercase mb-1">Kelime</label>
          <input v-model="newForm.keyword" type="text" placeholder="Örn: Kısakol"
                 class="w-full border rounded px-3 py-2 text-sm dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
        </div>
        <div>
          <label class="block text-xs font-bold text-gray-500 uppercase mb-1">Renk</label>
          <input v-model="newForm.color" type="color" class="h-10 w-14 border rounded dark:border-gray-700 bg-white dark:bg-gray-800" />
        </div>
        <button @click="addKeyword" :disabled="!newForm.keyword.trim() || saving"
                class="px-4 py-2 bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white font-semibold rounded-lg text-sm">
          Ekle
        </button>
      </div>

      <div v-if="loading" class="text-center py-8 text-gray-400 text-sm">Yükleniyor...</div>
      <div v-else-if="keywords.length === 0" class="text-center py-8 text-gray-400 text-sm">Henüz kelime eklenmedi.</div>

      <div v-else class="space-y-2">
        <div v-for="k in keywords" :key="k.id"
             class="flex items-center gap-3 p-2 rounded-lg border border-gray-100 dark:border-gray-800">
          <span class="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-bold text-white" :style="{ backgroundColor: k.color }">
            {{ k.keyword }}
          </span>
          <input v-model="k.keyword" type="text" class="flex-1 border rounded px-2 py-1.5 text-sm dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" />
          <input v-model="k.color" type="color" class="h-8 w-10 border rounded dark:border-gray-700 bg-white dark:bg-gray-800" />
          <label class="flex items-center gap-1 text-xs text-gray-600 dark:text-gray-300">
            <input type="checkbox" v-model="k.isActive" /> Aktif
          </label>
          <button @click="saveKeyword(k)" class="px-3 py-1.5 text-xs font-bold text-blue-600 bg-blue-50 dark:bg-blue-900/30 rounded">Kaydet</button>
          <button @click="removeKeyword(k)" class="px-2 py-1.5 text-xs font-bold text-red-600 bg-red-50 dark:bg-red-900/20 rounded">Sil</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import clothingKeywordService, { type ClothingKeyword } from '../services/clothingKeywordService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';

const notify = useNotificationStore();
const keywords = ref<ClothingKeyword[]>([]);
const loading = ref(false);
const saving = ref(false);
const newForm = ref({ keyword: '', color: '#ef4444' });

async function load() {
  loading.value = true;
  try {
    keywords.value = await clothingKeywordService.getAll(false);
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
}

async function addKeyword() {
  if (!newForm.value.keyword.trim()) return;
  saving.value = true;
  try {
    await clothingKeywordService.save({
      keyword: newForm.value.keyword.trim(),
      color: newForm.value.color,
      isActive: true,
      sortOrder: keywords.value.length,
    });
    newForm.value.keyword = '';
    await load();
    notify.add('Kelime eklendi.', 'success');
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Eklenemedi.', 'error');
  } finally {
    saving.value = false;
  }
}

async function saveKeyword(k: ClothingKeyword) {
  try {
    await clothingKeywordService.save({ id: k.id, keyword: k.keyword.trim(), color: k.color, isActive: k.isActive, sortOrder: k.sortOrder });
    notify.add('Güncellendi.', 'success');
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Güncellenemedi.', 'error');
  }
}

async function removeKeyword(k: ClothingKeyword) {
  if (!await notify.promptConfirm({ title: 'Sil', message: `"${k.keyword}" silinsin mi?`, confirmText: 'Sil', type: 'danger' })) return;
  try {
    await clothingKeywordService.remove(k.id);
    await load();
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Silinemedi.', 'error');
  }
}

onMounted(load);
</script>
