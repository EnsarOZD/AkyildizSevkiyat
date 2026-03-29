<template>
  <BaseModal :show="isOpen" title="📷 İrsaliye Tara" maxWidth="lg" :closeOnBackdrop="false" @close="handleClose">
    <!-- Step 1: Image Upload -->
    <div v-if="step === 'upload'" class="space-y-4">
      <p class="text-sm text-gray-600 dark:text-gray-400">
        İrsaliye fotoğrafını çekin veya dosya olarak yükleyin. OCR ile metin tanıma yapılacaktır.
      </p>

      <!-- Camera / File Input -->
      <div class="flex flex-col gap-3">
        <!-- Camera capture (mobile) -->
        <label
          class="flex items-center justify-center gap-2 px-4 py-8 border-2 border-dashed border-blue-300 dark:border-blue-700 rounded-xl cursor-pointer hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors"
        >
          <svg class="h-8 w-8 text-blue-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 9a2 2 0 012-2h.93a2 2 0 001.664-.89l.812-1.22A2 2 0 0110.07 4h3.86a2 2 0 011.664.89l.812 1.22A2 2 0 0018.07 7H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" />
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 13a3 3 0 11-6 0 3 3 0 016 0z" />
          </svg>
          <div class="text-center">
            <p class="text-sm font-semibold text-blue-700 dark:text-blue-300">Fotoğraf Çek veya Dosya Seç</p>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">JPG, PNG, WEBP desteklenir</p>
          </div>
          <input
            ref="fileInputRef"
            type="file"
            accept="image/*"
            capture="environment"
            class="hidden"
            @change="handleFileSelect"
          />
        </label>
      </div>

      <!-- Preview -->
      <div v-if="previewUrl" class="relative">
        <img :src="previewUrl" alt="İrsaliye önizleme" class="w-full max-h-64 object-contain rounded-lg border border-gray-200 dark:border-gray-700" />
        <button
          @click="clearImage"
          class="absolute top-2 right-2 bg-red-500 hover:bg-red-600 text-white rounded-full p-1.5 shadow-lg transition-colors"
        >
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>
    </div>

    <!-- Step 2: Processing -->
    <div v-if="step === 'processing'" class="space-y-4 py-8">
      <div class="flex flex-col items-center gap-4">
        <div class="animate-spin rounded-full h-12 w-12 border-4 border-blue-200 dark:border-blue-800 border-t-blue-600"></div>
        <div class="text-center">
          <p class="text-sm font-semibold text-gray-900 dark:text-gray-100">{{ progressStatus }}</p>
          <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">%{{ progressPercent }}</p>
        </div>
        <div class="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2.5">
          <div
            class="bg-blue-600 h-2.5 rounded-full transition-all duration-300"
            :style="{ width: progressPercent + '%' }"
          ></div>
        </div>
      </div>
    </div>

    <!-- Step 3: Results -->
    <div v-if="step === 'results'" class="space-y-4">
      <!-- Confidence -->
      <div
        class="flex items-center gap-2 px-3 py-2 rounded-lg text-sm"
        :class="(ocrResult?.confidence ?? 0) > 60
          ? 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-300'
          : 'bg-yellow-50 dark:bg-yellow-900/20 text-yellow-700 dark:text-yellow-300'"
      >
        <svg class="h-4 w-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
        </svg>
        <span>Tanıma güvenilirliği: <strong>%{{ Math.round(ocrResult?.confidence || 0) }}</strong></span>
      </div>

      <!-- Extracted Fields -->
      <div class="space-y-3">
        <div>
          <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">
            İrsaliye No
          </label>
          <input
            v-model="editableResult.waybillNo"
            type="text"
            class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
            :class="editableResult.waybillNo ? 'border-green-300 dark:border-green-700' : 'border-yellow-300 dark:border-yellow-700'"
            placeholder="Tanınamadı — manuel girin..."
          />
        </div>

        <div>
          <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">
            İrsaliye Tarihi
          </label>
          <input
            v-model="editableResult.waybillDate"
            type="date"
            class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
            :class="editableResult.waybillDate ? 'border-green-300 dark:border-green-700' : 'border-yellow-300 dark:border-yellow-700'"
          />
        </div>
      </div>

      <!-- Extracted Lines -->
      <div v-if="editableResult.lines.length > 0">
        <div class="flex items-center justify-between mb-2">
          <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">
            Tanınan Malzemeler ({{ editableResult.lines.length }})
          </label>
          <button
            @click="matchAllStocks"
            :disabled="matching"
            class="text-[10px] font-bold text-blue-600 hover:text-blue-800 uppercase flex items-center gap-1"
          >
            <svg v-if="matching" class="animate-spin h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
            </svg>
            Sistemde Eşleştir
          </button>
        </div>
        
        <div class="max-h-64 overflow-y-auto rounded-lg border border-gray-200 dark:border-gray-700">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 dark:bg-gray-800 sticky top-0">
              <tr>
                <th class="text-left px-3 py-2 text-xs font-semibold text-gray-500 dark:text-gray-400">Malzeme</th>
                <th class="text-right px-3 py-2 text-xs font-semibold text-gray-500 dark:text-gray-400 w-20">Miktar</th>
                <th class="text-center px-3 py-2 text-xs font-semibold text-gray-500 dark:text-gray-400 w-16">Birim</th>
                <th class="px-2 py-2 w-8"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-gray-700">
              <tr v-for="(line, i) in editableResult.lines" :key="i" class="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                <td class="px-3 py-2">
                  <div class="flex flex-col">
                    <input
                      v-model="line.stockName"
                      type="text"
                      class="w-full bg-transparent border-0 text-sm text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-1 focus:ring-blue-500 rounded px-1"
                    />
                    <div v-if="line.matchedStock" class="flex items-center gap-1 mt-0.5 px-1">
                      <span class="text-[9px] bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300 px-1 rounded font-bold uppercase">Eşleşti</span>
                      <span class="text-[10px] text-gray-500 truncate">{{ line.matchedStock.stockCode }}</span>
                    </div>
                    <div v-else-if="line.matchAttempted" class="px-1">
                      <span class="text-[9px] bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300 px-1 rounded font-bold uppercase">Bulunamadı</span>
                    </div>
                  </div>
                </td>
                <td class="px-3 py-2">
                  <input
                    v-model="line.quantity"
                    type="text"
                    class="w-full bg-transparent border-0 text-sm text-right text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-1 focus:ring-blue-500 rounded px-1"
                  />
                </td>
                <td class="px-3 py-2">
                  <input
                    v-model="line.unit"
                    type="text"
                    class="w-full bg-transparent border-0 text-sm text-center text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-1 focus:ring-blue-500 rounded px-1"
                  />
                </td>
                <td class="px-2 py-2">
                  <button @click="editableResult.lines.splice(i, 1)" class="text-red-400 hover:text-red-600 transition-colors">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Raw Text Toggle -->
      <details class="text-sm">
        <summary class="cursor-pointer text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 transition-colors">
          Ham OCR metni göster
        </summary>
        <pre class="mt-2 p-3 bg-gray-50 dark:bg-gray-800 rounded-lg text-xs text-gray-600 dark:text-gray-400 max-h-40 overflow-y-auto whitespace-pre-wrap">{{ ocrResult?.rawText }}</pre>
      </details>
    </div>

    <template #footer>
      <div class="flex items-center gap-2">
        <button
          @click="handleClose"
          class="px-4 py-2 text-sm font-semibold text-gray-700 dark:text-gray-300 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition-colors"
        >
          {{ step === 'results' ? 'Vazgeç' : 'İptal' }}
        </button>

        <button
          v-if="step === 'upload' && selectedFile"
          @click="startScan"
          class="px-4 py-2 text-sm font-semibold text-white bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors flex items-center gap-2"
        >
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
          Taramayı Başlat
        </button>

        <button
          v-if="step === 'results'"
          @click="applyResults"
          class="px-4 py-2 text-sm font-semibold text-white bg-green-600 hover:bg-green-700 rounded-lg transition-colors flex items-center gap-2"
        >
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
          Forma Uygula
        </button>
      </div>
    </template>
  </BaseModal>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import BaseModal from './BaseModal.vue';
import { scanInvoice, type OcrInvoiceResult, type OcrInvoiceLineResult } from '../services/ocrService';
import { stockService, type Stock } from '../services/stockService';
import { useNotificationStore } from '../stores/notification';
import { turkishLower } from '../utils/turkishSearch';

const props = defineProps<{
  isOpen: boolean;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'apply', data: { waybillNo: string; waybillDate: string; lines: OcrInvoiceLineResult[] }): void;
}>();

const notificationStore = useNotificationStore();

const step = ref<'upload' | 'processing' | 'results'>('upload');
const selectedFile = ref<File | null>(null);
const previewUrl = ref<string | null>(null);
const fileInputRef = ref<HTMLInputElement | null>(null);

const progressPercent = ref(0);
const progressStatus = ref('');

const ocrResult = ref<OcrInvoiceResult | null>(null);
const matching = ref(false);

interface EditableLine extends OcrInvoiceLineResult {
  matchedStock?: Stock | null;
  matchAttempted?: boolean;
}

const editableResult = ref<{ waybillNo: string; waybillDate: string; lines: EditableLine[] }>({
  waybillNo: '',
  waybillDate: '',
  lines: []
});

const reset = () => {
  step.value = 'upload';
  selectedFile.value = null;
  previewUrl.value = null;
  progressPercent.value = 0;
  progressStatus.value = '';
  ocrResult.value = null;
  matching.value = false;
  editableResult.value = { waybillNo: '', waybillDate: '', lines: [] };
};

watch(() => props.isOpen, (val) => {
  if (val) reset();
});

const handleFileSelect = (event: Event) => {
  const input = event.target as HTMLInputElement;
  if (input.files && input.files.length > 0) {
    const file = input.files[0]!;
    selectedFile.value = file;
    previewUrl.value = URL.createObjectURL(file);
  }
};

const clearImage = () => {
  selectedFile.value = null;
  if (previewUrl.value) {
    URL.revokeObjectURL(previewUrl.value);
    previewUrl.value = null;
  }
  if (fileInputRef.value) {
    fileInputRef.value.value = '';
  }
};

const startScan = async () => {
  if (!selectedFile.value) return;

  step.value = 'processing';

  try {
    const result = await scanInvoice(selectedFile.value, (progress, status) => {
      progressPercent.value = progress;
      progressStatus.value = status;
    });

    ocrResult.value = result;
    editableResult.value = {
      waybillNo: result.waybillNo,
      waybillDate: result.waybillDate,
      lines: result.lines.map(l => ({ ...l }))
    };
    step.value = 'results';
    // Auto-match after scan
    matchAllStocks();
  } catch (err) {
    console.error('OCR error:', err);
    notificationStore.add('OCR tarama sırasında hata oluştu. Lütfen tekrar deneyin.', 'error');
    step.value = 'upload';
  }
};

const matchAllStocks = async () => {
  if (editableResult.value.lines.length === 0 || matching.value) return;
  matching.value = true;
  
  try {
    const lines = editableResult.value.lines;
    for (const line of lines) {
      if (line.matchedStock) continue;
      
      // Try to find by name or code
      const searchTerm = line.stockName.trim();
      if (searchTerm.length < 3) continue;
      
      const res = await stockService.getAll({ search: searchTerm, size: 5 });
      line.matchAttempted = true;
      
      if (res.items && res.items.length > 0) {
        // Exact match or first match
        const exactMatch = res.items.find(s =>
          turkishLower(s.stockName) === turkishLower(searchTerm) ||
          turkishLower(s.stockCode) === turkishLower(searchTerm)
        );
        line.matchedStock = exactMatch || res.items[0];
      }
    }
  } catch (e) {
    console.error('Stock matching error:', e);
  } finally {
    matching.value = false;
  }
};

const applyResults = () => {
  emit('apply', {
    waybillNo: editableResult.value.waybillNo,
    waybillDate: editableResult.value.waybillDate,
    lines: editableResult.value.lines
  });
  emit('close');
};

const handleClose = () => {
  if (step.value === 'processing') return; // Don't close during processing
  reset();
  emit('close');
};
</script>
