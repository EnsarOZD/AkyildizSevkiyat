<template>
  <div class="max-w-4xl mx-auto space-y-4">
    <div class="flex items-center justify-between gap-3 flex-wrap">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Nakliye Teslim Linkleri</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
          Nakliyecilere gönderilen teslim fotoğrafı linkleri. Gerektiğinde tekrar gönderebilirsiniz.
        </p>
      </div>
      <label class="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400 cursor-pointer">
        <input type="checkbox" v-model="includeInactive" @change="load" class="rounded" />
        Tamamlanan / süresi dolanları da göster
      </label>
    </div>

    <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div v-if="loading" class="py-12 text-center text-sm text-gray-400">Yükleniyor...</div>
      <div v-else-if="items.length === 0" class="py-16 text-center text-sm text-gray-400 dark:text-gray-600">
        Aktif teslim linki yok.
      </div>

      <div v-else class="divide-y divide-gray-100 dark:divide-gray-800">
        <div v-for="d in items" :key="d.token" class="p-4 flex flex-col sm:flex-row sm:items-center gap-3">
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2 flex-wrap">
              <span class="font-semibold text-gray-900 dark:text-gray-100 truncate">{{ d.projectName }}</span>
              <span v-if="d.isCompleted" class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400">Tamamlandı</span>
              <span v-else-if="d.isExpired" class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-300">Süresi doldu</span>
              <span v-else class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400">Aktif</span>
            </div>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
              {{ d.carrierName }}<span v-if="d.carrierPhone"> · {{ d.carrierPhone }}</span> · {{ d.shipmentCount }} sevkiyat
            </p>
            <p class="text-[11px] text-gray-400 dark:text-gray-600 mt-0.5">
              Oluşturma: {{ fmt(d.createdAt) }} · Geçerlilik: {{ fmt(d.expiresAt) }}
              <span v-if="d.isCompleted && d.recipientName"> · Teslim alan: {{ d.recipientName }}</span>
            </p>
          </div>
          <div class="flex gap-2 flex-shrink-0">
            <a
              v-if="!d.isCompleted && !d.isExpired"
              :href="waHref(d.carrierPhone, d.projectName, d.token)"
              target="_blank" rel="noopener"
              class="px-3 py-2 rounded-lg text-white text-sm font-medium bg-green-600 hover:bg-green-700 whitespace-nowrap"
            >WhatsApp</a>
            <button
              @click="copy(d.token)"
              class="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 whitespace-nowrap"
            >Linki Kopyala</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import freightDeliveryService, { type FreightDeliveryListItem } from '../services/freightDeliveryService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import { uploadUrl, waHref } from '../utils/freightLink';

const notify = useNotificationStore();
const items = ref<FreightDeliveryListItem[]>([]);
const loading = ref(false);
const includeInactive = ref(false);

async function load() {
  loading.value = true;
  try {
    items.value = await freightDeliveryService.list(includeInactive.value);
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'Linkler yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
}

async function copy(token: string) {
  try {
    await navigator.clipboard.writeText(uploadUrl(token));
    notify.add('Link kopyalandı.', 'success');
  } catch {
    notify.add('Kopyalanamadı, linki elle seçin.', 'warning');
  }
}

function fmt(iso: string): string {
  return new Date(iso).toLocaleString('tr-TR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });
}

onMounted(load);
</script>
