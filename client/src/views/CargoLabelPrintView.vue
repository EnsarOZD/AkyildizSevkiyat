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
        :to="{ name: 'ShipmentDetail', params: { id: route.params.id } }"
        class="px-4 py-2 text-sm font-semibold text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-lg transition-colors"
      >
        &larr; Sevkiyata Dön
      </router-link>
    </div>

    <!-- Yükleniyor -->
    <div v-if="loading" class="flex items-center justify-center py-16">
      <div class="w-8 h-8 border-4 border-gray-300 border-t-gray-900 rounded-full animate-spin"></div>
    </div>

    <!-- Hata -->
    <div v-else-if="error" class="text-center py-16 text-red-600">
      <p class="font-semibold">{{ error }}</p>
    </div>

    <!-- Kargo Etiketi -->
    <div v-else-if="shipment" class="label-container mx-auto">

      <!-- Üst başlık -->
      <div class="label-header">
        <img :src="logoUrl" alt="Akyıldız" class="label-logo" />
        <div class="label-header-text">
          <div class="label-company">AKYILDIZ LOJİSTİK</div>
          <div class="label-subtitle">Kargo Etiketi</div>
        </div>
        <div class="label-cargo-brand">YURTİÇİ KARGO</div>
      </div>

      <!-- Alıcı bilgileri -->
      <div class="label-section">
        <div class="label-section-title">ALICI</div>
        <div class="label-field-large">{{ shipment.projectName }}</div>
        <div class="label-field" v-if="shipment.projectAddress">{{ shipment.projectAddress }}</div>
        <div class="label-field" v-if="shipment.teslimAlacakTelefon">Tel: {{ shipment.teslimAlacakTelefon }}</div>
      </div>

      <div class="label-divider"></div>

      <!-- Barkod -->
      <div class="label-barcode-section">
        <div v-if="shipment.ykBarcode">
          <svg ref="barcodeEl" class="label-barcode-svg"></svg>
          <div class="label-barcode-text">{{ shipment.ykBarcode }}</div>
        </div>
        <div v-else class="label-no-barcode">
          <div class="label-cargo-key">{{ shipment.ykCargoKey }}</div>
          <div class="label-no-barcode-note">YK barkodu henüz oluşmadı — entegrasyon yapılandırmasını kontrol edin</div>
        </div>
      </div>

      <div class="label-divider"></div>

      <!-- Alt bilgiler -->
      <div class="label-footer-grid">
        <div class="label-footer-item">
          <span class="label-footer-label">Sevkiyat No</span>
          <span class="label-footer-value">#{{ shipment.id }}</span>
        </div>
        <div class="label-footer-item" v-if="shipment.irsaliyeNo">
          <span class="label-footer-label">İrsaliye No</span>
          <span class="label-footer-value">{{ shipment.irsaliyeNo }}</span>
        </div>
        <div class="label-footer-item">
          <span class="label-footer-label">Teslim Tarihi</span>
          <span class="label-footer-value">{{ formatDate(shipment.deliveryDate) }}</span>
        </div>
        <div class="label-footer-item" v-if="shipment.ykJobId">
          <span class="label-footer-label">YK İş Emri</span>
          <span class="label-footer-value">{{ shipment.ykJobId }}</span>
        </div>
      </div>

      <div class="label-divider"></div>

      <!-- Gönderici -->
      <div class="label-sender">
        <span class="label-footer-label">GÖNDERİCİ: </span>
        Akyıldız Lojistik A.Ş.
      </div>

    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, nextTick } from 'vue';
import { useRoute } from 'vue-router';
import JsBarcode from 'jsbarcode';
import shipmentService, { type ShipmentDetail } from '../services/shipmentService';
import logoUrl from '../assets/logo.png';
import { formatDate } from '../utils/dateFormat';

const route = useRoute();
const shipment = ref<ShipmentDetail | null>(null);
const loading = ref(true);
const error = ref<string | null>(null);
const barcodeEl = ref<SVGSVGElement | null>(null);

function renderBarcode() {
  if (!barcodeEl.value || !shipment.value?.ykBarcode) return;
  const barcodeValue = shipment.value.ykBarcode;
  try {
    JsBarcode(barcodeEl.value, barcodeValue!, {
      format: 'CODE128',
      width: 2.5,
      height: 80,
      displayValue: false,
      margin: 0,
    });
  } catch {
    // barkod formatı geçersizse sessizce geç
  }
}

function doPrint() {
  window.print();
}

onMounted(async () => {
  const id = Number(route.params.id);
  try {
    shipment.value = await shipmentService.getDetail(id);
    await nextTick();
    renderBarcode();
  } catch {
    error.value = 'Sevkiyat bilgileri yüklenemedi.';
  } finally {
    loading.value = false;
  }
});
</script>

<style scoped>
.label-container {
  width: 100mm;
  border: 2px solid #111;
  font-family: Arial, sans-serif;
  font-size: 10pt;
}

.label-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 10px;
  background: #111;
  color: #fff;
}

.label-logo {
  height: 28px;
  width: auto;
  filter: brightness(0) invert(1);
}

.label-header-text {
  flex: 1;
}

.label-company {
  font-size: 11pt;
  font-weight: 700;
  letter-spacing: 0.5px;
}

.label-subtitle {
  font-size: 7pt;
  opacity: 0.8;
}

.label-cargo-brand {
  font-size: 9pt;
  font-weight: 700;
  text-align: right;
  border: 1px solid rgba(255,255,255,0.5);
  padding: 2px 5px;
  border-radius: 3px;
}

.label-section {
  padding: 8px 10px;
}

.label-section-title {
  font-size: 7pt;
  font-weight: 700;
  color: #666;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 3px;
}

.label-field-large {
  font-size: 13pt;
  font-weight: 700;
  line-height: 1.2;
}

.label-field {
  font-size: 9pt;
  margin-top: 2px;
  color: #333;
}

.label-divider {
  border-top: 1.5px solid #111;
}

.label-barcode-section {
  padding: 10px;
  text-align: center;
}

.label-barcode-svg {
  width: 100%;
  max-width: 88mm;
  height: auto;
}

.label-barcode-text {
  font-size: 9pt;
  letter-spacing: 2px;
  margin-top: 2px;
  font-family: 'Courier New', monospace;
}

.label-no-barcode {
  padding: 8px 0;
}

.label-cargo-key {
  font-size: 14pt;
  font-weight: 700;
  letter-spacing: 1px;
}

.label-no-barcode-note {
  font-size: 7pt;
  color: #999;
  margin-top: 4px;
}


.label-footer-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1px;
  background: #111;
  border-top: none;
}

.label-footer-item {
  background: #fff;
  padding: 5px 8px;
  display: flex;
  flex-direction: column;
}

.label-footer-label {
  font-size: 7pt;
  color: #666;
  text-transform: uppercase;
  letter-spacing: 0.3px;
}

.label-footer-value {
  font-size: 9pt;
  font-weight: 600;
}

.label-sender {
  padding: 5px 10px;
  font-size: 8pt;
  color: #444;
}

/* Baskı stilleri */
@media print {
  @page {
    size: A4;
    margin: 15mm;
  }

  body {
    margin: 0;
    padding: 0;
  }

  .label-container {
    border: 1.5px solid #111;
    width: 100mm;
    margin: 0 auto;
    page-break-after: avoid;
  }
}
</style>
