<template>
  <div class="min-h-screen bg-white p-6 print:p-0">

    <!-- Ekran kontrolleri (yazdırmada gizlenir) -->
    <div class="print:hidden mb-6 flex items-center gap-3 flex-wrap">
      <button
        @click="doPrint"
        class="px-4 py-2 bg-gray-900 hover:bg-gray-700 text-white rounded-lg text-sm font-semibold transition-colors"
      >
        Yazdır / PDF Kaydet
      </button>
      <router-link
        :to="{ name: 'PickingManager' }"
        class="px-4 py-2 text-sm font-semibold text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-lg transition-colors"
      >
        &larr; Panoya Dön
      </router-link>
      <span class="text-sm text-gray-500">{{ labels.length }} etiket</span>
    </div>

    <!-- Yükleniyor -->
    <div v-if="loading" class="flex items-center justify-center py-16">
      <div class="w-8 h-8 border-4 border-gray-300 border-t-gray-900 rounded-full animate-spin"></div>
    </div>

    <!-- Hata / boş -->
    <div v-else-if="error" class="text-center py-16 text-red-600">
      <p class="font-semibold">{{ error }}</p>
    </div>
    <div v-else-if="labels.length === 0" class="text-center py-16 text-gray-500">
      <p class="font-semibold">Yazdırılacak araba/palet bulunamadı.</p>
    </div>

    <!-- Etiket ızgarası -->
    <div v-else class="label-grid">
      <div v-for="l in labels" :key="l.id" class="label-cell">
        <img v-if="qrMap[l.id]" :src="qrMap[l.id]" alt="QR" class="label-qr" />
        <div class="label-code">{{ l.code }}</div>
        <div class="label-type">{{ typeLabel(l.type) }}</div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import QRCode from 'qrcode';
import containerService, { type Container, ContainerTypeLabels } from '../services/containerService';

const route = useRoute();
const loading = ref(true);
const error = ref<string | null>(null);
const labels = ref<Container[]>([]);
const qrMap = ref<Record<number, string>>({});

const typeLabel = (t: number) => ContainerTypeLabels[t] ?? '?';

function doPrint() {
  window.print();
}

onMounted(async () => {
  try {
    // ?ids=1,2,3 → seçili; yoksa tüm aktif Container'lar
    const idsParam = (route.query.ids as string | undefined)?.trim();
    const ids = idsParam
      ? idsParam.split(',').map(s => Number(s)).filter(n => Number.isFinite(n))
      : null;

    const all = await containerService.getAll(true);
    labels.value = ids ? all.filter(c => ids.includes(c.id)) : all;

    // Her araba için QR (içerik = Code) üret
    for (const c of labels.value) {
      qrMap.value[c.id] = await QRCode.toDataURL(c.code, {
        margin: 1,
        width: 320,
        errorCorrectionLevel: 'M',
      });
    }
  } catch {
    error.value = 'Etiketler yüklenemedi.';
  } finally {
    loading.value = false;
  }
});
</script>

<style scoped>
.label-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 6mm;
}

.label-cell {
  border: 2px solid #111;
  border-radius: 4px;
  padding: 6mm 4mm;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  page-break-inside: avoid;
  break-inside: avoid;
}

.label-qr {
  width: 45mm;
  height: 45mm;
}

.label-code {
  margin-top: 3mm;
  font-family: 'Courier New', monospace;
  font-size: 18pt;
  font-weight: 700;
  letter-spacing: 1px;
  color: #111;
  word-break: break-all;
}

.label-type {
  margin-top: 1mm;
  font-size: 9pt;
  color: #666;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

@media print {
  @page {
    size: A4;
    margin: 12mm;
  }
  body { margin: 0; padding: 0; }
}
</style>
