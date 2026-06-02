<template>
  <div class="min-h-screen bg-gray-100 dark:bg-gray-950 flex flex-col">
    <!-- Header -->
    <div class="bg-emerald-600 text-white px-4 py-3 flex items-center gap-2">
      <span class="text-lg">📦</span>
      <h1 class="text-base font-semibold">Teslim Fotoğrafı Yükleme</h1>
    </div>

    <div class="flex-1 p-4 max-w-md mx-auto w-full">
      <!-- Loading -->
      <div v-if="loading" class="flex justify-center py-16">
        <div class="w-7 h-7 border-4 border-emerald-600 border-t-transparent rounded-full animate-spin"></div>
      </div>

      <!-- Load error / invalid token -->
      <div v-else-if="loadError" class="rounded-xl bg-red-50 border border-red-200 p-5 text-center">
        <div class="text-3xl mb-2">⚠️</div>
        <p class="text-red-700 font-medium">{{ loadError }}</p>
      </div>

      <!-- Already completed -->
      <div v-else-if="info && info.isCompleted" class="rounded-xl bg-green-50 border border-green-200 p-5 text-center">
        <div class="text-4xl mb-2">✅</div>
        <p class="text-green-800 font-semibold">Bu teslimat zaten tamamlanmış.</p>
        <p v-if="info.recipientName" class="text-sm text-green-700 mt-1">Teslim alan: {{ info.recipientName }}</p>
      </div>

      <!-- Expired -->
      <div v-else-if="info && info.isExpired" class="rounded-xl bg-amber-50 border border-amber-200 p-5 text-center">
        <div class="text-3xl mb-2">⏱️</div>
        <p class="text-amber-800 font-semibold">Bu teslim linkinin süresi dolmuş.</p>
        <p class="text-sm text-amber-700 mt-1">Lütfen yetkiliyle iletişime geçin.</p>
      </div>

      <!-- Success -->
      <div v-else-if="done" class="rounded-xl bg-green-50 border border-green-200 p-5 text-center">
        <div class="text-4xl mb-2">✅</div>
        <p class="text-green-800 font-semibold">Teslim fotoğrafları kaydedildi. Teşekkürler.</p>
      </div>

      <!-- Form -->
      <div v-else-if="info" class="space-y-4">
        <!-- Project + shipments -->
        <div class="rounded-xl bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 p-4">
          <p class="text-xs text-gray-500 uppercase tracking-wider">Teslim Noktası</p>
          <p class="text-lg font-bold text-gray-900 dark:text-gray-100">{{ info.projectName }}</p>
          <p class="text-sm text-gray-500 mt-0.5">Taşıyıcı: {{ info.carrierName }}</p>
          <div class="mt-3 border-t border-gray-100 dark:border-gray-800 pt-2 space-y-1">
            <p class="text-xs text-gray-500">{{ info.shipments.length }} sevkiyat</p>
            <div v-for="s in info.shipments" :key="s.shipmentId" class="flex justify-between text-xs text-gray-600 dark:text-gray-400">
              <span class="font-mono">{{ s.irsaliyeNo || '—' }}</span>
              <span>{{ s.lineCount }} kalem</span>
            </div>
          </div>
        </div>

        <!-- Recipient -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Teslim Alan <span class="text-red-500">*</span>
          </label>
          <input v-model="recipientName" type="text" placeholder="Teslim alan kişinin adı"
            class="w-full px-4 py-3 rounded-xl border text-base bg-white dark:bg-gray-800 dark:text-gray-100"
            :class="recipientError ? 'border-red-400' : 'border-gray-300 dark:border-gray-600'" />
          <p v-if="recipientError" class="mt-1 text-xs text-red-600">{{ recipientError }}</p>
        </div>

        <!-- Note -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Not (opsiyonel)</label>
          <input v-model="note" type="text" placeholder="Açıklama"
            class="w-full px-4 py-3 rounded-xl border border-gray-300 dark:border-gray-600 text-base bg-white dark:bg-gray-800 dark:text-gray-100" />
        </div>

        <!-- Photos -->
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
            Teslim Fotoğrafları <span class="text-red-500">*</span>
            <span class="text-gray-400 font-normal">(en fazla 5)</span>
          </label>
          <div class="grid grid-cols-3 gap-2">
            <div v-for="(p, i) in previews" :key="i" class="relative aspect-square">
              <img :src="p" class="w-full h-full object-cover rounded-lg" />
              <button @click="removePhoto(i)" class="absolute top-1 right-1 bg-black/50 text-white rounded-full p-0.5">
                <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/></svg>
              </button>
            </div>
            <label v-if="previews.length < 5"
              class="aspect-square flex items-center justify-center border-2 border-dashed border-gray-300 dark:border-white/20 rounded-lg cursor-pointer hover:border-emerald-400">
              <input type="file" accept="image/*" capture="environment" class="hidden" @change="onPhotoSelected" />
              <span v-if="compressing" class="text-xs text-gray-400">...</span>
              <svg v-else class="w-7 h-7 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/></svg>
            </label>
          </div>
          <p v-if="photoError" class="mt-1 text-xs text-red-600">{{ photoError }}</p>
        </div>

        <button @click="submit" :disabled="submitting"
          class="w-full py-3 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-semibold disabled:opacity-50">
          {{ submitting ? 'Gönderiliyor...' : 'Teslimi Tamamla' }}
        </button>
        <p v-if="submitError" class="text-sm text-red-600 text-center">{{ submitError }}</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import freightDeliveryService, { type FreightDeliveryInfo } from '../services/freightDeliveryService';
import { ApiErrorUtils } from '../utils/apiError';

const route = useRoute();
const token = String(route.params.token || '');

const loading = ref(true);
const loadError = ref('');
const info = ref<FreightDeliveryInfo | null>(null);

const recipientName = ref('');
const note = ref('');
const photosBase64 = ref<string[]>([]);
const previews = ref<string[]>([]);
const compressing = ref(false);

const recipientError = ref('');
const photoError = ref('');
const submitError = ref('');
const submitting = ref(false);
const done = ref(false);

onMounted(async () => {
  try {
    info.value = await freightDeliveryService.getInfo(token);
  } catch (e) {
    loadError.value = ApiErrorUtils.getErrorMessage(e) || 'Teslim linki bulunamadı veya geçersiz.';
  } finally {
    loading.value = false;
  }
});

function onPhotoSelected(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file || previews.value.length >= 5) { if (input) input.value = ''; return; }
  compressing.value = true;
  const reader = new FileReader();
  reader.onload = (e) => {
    const img = new Image();
    img.onload = () => {
      const MAX = 1280;
      let w = img.width, h = img.height;
      if (w > MAX || h > MAX) {
        if (w > h) { h = Math.round((h / w) * MAX); w = MAX; }
        else { w = Math.round((w / h) * MAX); h = MAX; }
      }
      const canvas = document.createElement('canvas');
      canvas.width = w; canvas.height = h;
      canvas.getContext('2d')!.drawImage(img, 0, 0, w, h);
      const compressed = canvas.toDataURL('image/jpeg', 0.8);
      previews.value.push(compressed);
      photosBase64.value.push(compressed.split(',')[1] ?? '');
      compressing.value = false;
    };
    img.src = e.target?.result as string;
  };
  reader.readAsDataURL(file);
  input.value = '';
}

function removePhoto(i: number) {
  previews.value.splice(i, 1);
  photosBase64.value.splice(i, 1);
}

async function submit() {
  recipientError.value = '';
  photoError.value = '';
  submitError.value = '';
  if (!recipientName.value.trim()) { recipientError.value = 'Teslim alan bilgisi zorunludur.'; return; }
  if (photosBase64.value.length === 0) { photoError.value = 'En az bir fotoğraf yükleyin.'; return; }

  submitting.value = true;
  try {
    await freightDeliveryService.submitProof(token, {
      recipientName: recipientName.value.trim(),
      note: note.value.trim() || null,
      photosBase64: photosBase64.value,
    });
    done.value = true;
  } catch (e) {
    submitError.value = ApiErrorUtils.getErrorMessage(e) || 'Gönderim başarısız. Lütfen tekrar deneyin.';
  } finally {
    submitting.value = false;
  }
}
</script>
