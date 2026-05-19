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

      <!-- Daha önce yazdırılmış uyarısı -->
      <div
        v-if="previousPrintLogs.length > 0"
        class="flex items-start gap-2 bg-amber-50 border border-amber-300 rounded-lg px-4 py-2 text-sm text-amber-800"
      >
        <svg class="w-4 h-4 mt-0.5 shrink-0 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
          <path fill-rule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clip-rule="evenodd"/>
        </svg>
        <div>
          <p class="font-semibold">Bu form daha önce {{ previousPrintLogs.length }}x yazdırıldı</p>
          <ul class="mt-0.5 space-y-0.5 text-xs text-amber-700">
            <li v-for="log in previousPrintLogs" :key="log.id">
              {{ formatDateTime(log.printedAt) }} — {{ log.printedByName }}
            </li>
          </ul>
        </div>
      </div>
    </div>

    <!-- Yükleniyor -->
    <div v-if="loading" class="flex justify-center py-16 print:hidden">
      <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-600"></div>
    </div>

    <!-- Hata -->
    <div v-else-if="error" class="text-center py-8 print:hidden">
      <p class="text-red-600 font-semibold">{{ error }}</p>
    </div>

    <!-- Form içeriği -->
    <div v-else-if="shipment" class="max-w-4xl mx-auto print:max-w-none">

      <!-- ── Üst Başlık ─────────────────────────────────────────────── -->
      <div class="flex items-start justify-between border-b-2 border-gray-900 pb-4 mb-5">
        <div class="flex-shrink-0">
          <img :src="logoUrl" alt="Akyıldız" class="h-14 w-auto grayscale" />
        </div>
        <div class="text-center flex-1 px-4">
          <p class="text-xl font-bold text-gray-900 uppercase tracking-wide">AKYILDIZ TESİS HİZMETLERİ</p>
          <p class="text-base font-bold text-gray-900 uppercase mt-0.5">MÜŞTERİ SİPARİŞ FORMU</p>
        </div>
        <div class="flex-shrink-0 text-right text-xs text-gray-600 min-w-28">
          <div><span class="font-semibold">Form No:</span> #{{ shipment.id }}</div>
          <div><span class="font-semibold">Tarih:</span> {{ today }}</div>
        </div>
      </div>

      <!-- ── Proje ve Sipariş Bilgileri ────────────────────────────── -->
      <div class="grid grid-cols-2 gap-4 mb-5">

        <!-- Sol: Teslim Bilgileri -->
        <div class="border border-gray-400 rounded p-3">
          <p class="text-xs font-bold text-gray-500 uppercase tracking-wider mb-2 border-b border-gray-300 pb-1">
            Teslim Bilgileri
          </p>
          <div class="space-y-1 text-sm">
            <div v-if="shipment.projectCode" class="flex gap-2">
              <span class="text-gray-500 w-28 flex-shrink-0">Proje Kodu:</span>
              <span class="font-medium break-words">{{ shipment.projectCode }}</span>
            </div>
            <div class="flex gap-2">
              <span class="text-gray-500 w-28 flex-shrink-0">Proje Adı:</span>
              <span class="font-medium break-words">{{ shipment.projectName || '—' }}</span>
            </div>
            <div v-if="shipment.projectAddress" class="flex gap-2">
              <span class="text-gray-500 w-28 flex-shrink-0">Adres:</span>
              <span class="font-medium break-words">{{ shipment.projectAddress }}</span>
            </div>
            <div v-if="shipment.teslimAlacakKisiler" class="flex gap-2">
              <span class="text-gray-500 w-28 flex-shrink-0">Teslim Alacak:</span>
              <span class="font-medium break-words">{{ shipment.teslimAlacakKisiler }}</span>
            </div>
            <div v-if="shipment.teslimAlacakTelefon" class="flex gap-2">
              <span class="text-gray-500 w-28 flex-shrink-0">Telefon:</span>
              <span class="font-medium break-words">{{ shipment.teslimAlacakTelefon }}</span>
            </div>
          </div>
        </div>

        <!-- Sağ: Sipariş Bilgileri -->
        <div class="border border-gray-400 rounded p-3">
          <p class="text-xs font-bold text-gray-500 uppercase tracking-wider mb-2 border-b border-gray-300 pb-1">
            Sipariş Bilgileri
          </p>
          <div class="space-y-1 text-sm">
            <div class="flex gap-2">
              <span class="text-gray-500 w-28 flex-shrink-0">Teslim Tarihi:</span>
              <span class="font-medium">{{ formatDate(shipment.deliveryDate) }}</span>
            </div>
            <div v-if="shipment.externalOrderNumber" class="flex gap-2">
              <span class="text-gray-500 w-28 flex-shrink-0">Sipariş No:</span>
              <span class="font-medium break-words">{{ shipment.externalOrderNumber }}</span>
            </div>
            <div v-if="shipment.talepNo" class="flex gap-2">
              <span class="text-gray-500 w-28 flex-shrink-0">Talep No:</span>
              <span class="font-medium break-words">{{ shipment.talepNo }}</span>
            </div>
            <div v-if="shipment.talepTuru" class="flex gap-2">
              <span class="text-gray-500 w-28 flex-shrink-0">Talep Türü:</span>
              <span class="font-medium break-words">{{ shipment.talepTuru }}</span>
            </div>
            <div v-if="shipment.institutionCode" class="flex gap-2">
              <span class="text-gray-500 w-28 flex-shrink-0">Kurum Bilgisi:</span>
              <span class="font-medium break-words">{{ shipment.institutionCode }}</span>
            </div>
            <div v-if="shipment.aciklama" class="flex gap-2">
              <span class="text-gray-500 w-28 flex-shrink-0">Açıklama:</span>
              <span class="font-medium break-words">{{ shipment.aciklama }}</span>
            </div>
            <div class="flex gap-2">
              <span class="text-gray-500 w-28 flex-shrink-0">Durum:</span>
              <span class="font-medium">{{ statusLabel }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- ── Kalemler Tablosu ───────────────────────────────────────── -->
      <table class="w-full border-collapse text-sm mb-6">
        <thead>
          <tr class="bg-gray-100">
            <th class="border border-gray-400 px-2 py-2 text-center text-xs font-bold w-8">#</th>
            <th class="border border-gray-400 px-3 py-2 text-left text-xs font-bold w-32">Stok Kodu</th>
            <th class="border border-gray-400 px-3 py-2 text-left text-xs font-bold">Stok Adı</th>
            <th class="border border-gray-400 px-3 py-2 text-center text-xs font-bold w-20">Miktar</th>
            <th class="border border-gray-400 px-3 py-2 text-center text-xs font-bold w-16">Birim</th>
            <th class="border border-gray-400 px-3 py-2 text-center text-xs font-bold w-24">Teslim</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="(line, idx) in shipment.lines"
            :key="line.id"
            :class="idx % 2 === 0 ? 'bg-white' : 'bg-gray-50'"
          >
            <td class="border border-gray-300 px-2 py-1.5 text-center text-xs text-gray-500">{{ idx + 1 }}</td>
            <td class="border border-gray-300 px-3 py-1.5 text-xs font-mono">{{ line.localStockCode || line.stockCode }}</td>
            <td class="border border-gray-300 px-3 py-1.5 text-xs">{{ line.stockName }}</td>
            <td class="border border-gray-300 px-3 py-1.5 text-center text-sm font-semibold">{{ line.orderedQty }}</td>
            <td class="border border-gray-300 px-3 py-1.5 text-center text-xs text-gray-600">{{ line.unit || '—' }}</td>
            <td class="border border-gray-300 px-3 py-1.5 text-center text-xs text-gray-400">___________</td>
          </tr>
          <tr v-for="i in 3" :key="`empty-${i}`" class="bg-white">
            <td class="border border-gray-300 px-2 py-1.5 text-center text-xs text-gray-300">{{ shipment.lines.length + i }}</td>
            <td class="border border-gray-300 px-3 py-1.5 text-xs">&nbsp;</td>
            <td class="border border-gray-300 px-3 py-1.5 text-xs">&nbsp;</td>
            <td class="border border-gray-300 px-3 py-1.5">&nbsp;</td>
            <td class="border border-gray-300 px-3 py-1.5">&nbsp;</td>
            <td class="border border-gray-300 px-3 py-1.5">&nbsp;</td>
          </tr>
        </tbody>
      </table>

      <!-- ── Alt Bilgi ─────────────────────────────────────────────── -->
      <div class="text-center text-xs text-gray-400 border-t border-gray-300 pt-2 print:fixed print:bottom-2 print:left-0 print:right-0">
        Bu form Akyıldız Tesis Hizmetleri tarafından oluşturulmuştur. • {{ today }}
      </div>
    </div>

    <!-- Hiçbir durum yoksa (yükleme bitti, hata yok, veri yok) -->
    <div v-else class="text-center py-16 print:hidden text-gray-400 text-sm">
      Sevkiyat verisi yüklenemedi.
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import shipmentService, { type ShipmentDetail, type ShipmentPrintLog } from '../services/shipmentService';
import logoUrl from '../assets/logo.png';

const route    = useRoute();
const loading  = ref(true);
const error    = ref<string | null>(null);
const shipment = ref<ShipmentDetail | null>(null);
const previousPrintLogs = ref<ShipmentPrintLog[]>([]);

const today = new Date().toLocaleDateString('tr-TR', { year: 'numeric', month: '2-digit', day: '2-digit' });

const statusLabel = computed(() => {
  const map: Record<string, string> = {
    Draft: 'Taslak', Warehouse: 'Depoda', Picking: 'Toplamada',
    ReadyForDispatch: 'Hazır', Ready: 'Hazır', AssignedToVehicle: 'Araçta',
    Preparing: 'Hazırlanıyor', Delivered: 'Teslim Edildi',
  };
  const s = shipment.value?.status ?? '';
  return map[s] ?? s;
});

function formatDate(val: string | null | undefined): string {
  if (!val) return '—';
  const d = new Date(val);
  return isNaN(d.getTime()) ? val : d.toLocaleDateString('tr-TR');
}

function formatDateTime(val: string): string {
  const d = new Date(val);
  if (isNaN(d.getTime())) return val;
  return d.toLocaleString('tr-TR', { year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' });
}

async function doPrint() {
  try {
    await shipmentService.logPrint(Number(route.params.id));
  } catch {
    // Log failure is non-critical — still allow printing
  }
  window.print();
}

onMounted(async () => {
  try {
    const id = Number(route.params.id);
    shipment.value = await shipmentService.getDetail(id);
    previousPrintLogs.value = shipment.value?.printLogs ?? [];
  } catch (e: any) {
    error.value = e?.message || e?.response?.data?.message || 'Sevkiyat bilgisi alınamadı.';
  } finally {
    loading.value = false;
  }
});
</script>

<style>
@media print {
  @page {
    size: A4 portrait;
    margin: 12mm 12mm 12mm 12mm;
  }
  body {
    print-color-adjust: exact;
    -webkit-print-color-adjust: exact;
  }
  .bg-gray-50  { background-color: #f9fafb !important; }
  .bg-gray-100 { background-color: #f3f4f6 !important; }
}
</style>
