<template>
  <div class="p-4 sm:p-6">
    <div class="flex flex-col sm:flex-row sm:justify-between sm:items-start mb-6 gap-3">
      <div>
        <h1 class="text-2xl font-semibold text-gray-900 dark:text-gray-100">Netsis Cari Eşleşmeleri</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-1 max-w-2xl">
          ISS-IP siparişlerinden gelen <strong>Kurum Kodu</strong> ile Netsis ana fatura cari kodu eşleşmelerini burada tanımlayın.
          Yeni proje ISS import'unda otomatik açıldığında, kurum koduna karşılık gelen Netsis Cari Kodu otomatik atanır.
        </p>
      </div>
      <div class="flex gap-2">
        <button
          @click="openApplyPreview"
          class="inline-flex items-center px-4 py-2 bg-amber-500 text-white text-sm rounded-md hover:bg-amber-600"
        >
          Mevcut Projeleri Senkronize Et
        </button>
        <button
          @click="openCreate"
          class="inline-flex items-center px-4 py-2 bg-indigo-600 text-white text-sm rounded-md hover:bg-indigo-700"
        >
          + Yeni Eşleşme
        </button>
      </div>
    </div>

    <!-- Mappings table -->
    <div class="bg-white dark:bg-gray-900 rounded-md shadow-sm border border-gray-200 dark:border-gray-800 overflow-hidden">
      <div v-if="loading" class="p-6 text-center text-gray-500">Yükleniyor...</div>
      <div v-else-if="mappings.length === 0" class="p-6 text-center text-gray-500">
        Henüz eşleşme tanımlanmamış.
      </div>
      <table v-else class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Kurum Kodu</th>
            <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Netsis Cari Kodu</th>
            <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Açıklama</th>
            <th class="px-4 py-2 text-left text-xs font-medium text-gray-500 uppercase">Aktif</th>
            <th class="px-4 py-2 text-right text-xs font-medium text-gray-500 uppercase">İşlem</th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-for="m in mappings" :key="m.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
            <td class="px-4 py-2 text-sm font-mono">{{ m.institutionCode }}</td>
            <td class="px-4 py-2 text-sm font-mono">{{ m.netsisCariKodu }}</td>
            <td class="px-4 py-2 text-sm">{{ m.description || '—' }}</td>
            <td class="px-4 py-2 text-sm">
              <span :class="m.isActive
                ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200'
                : 'bg-gray-200 text-gray-700 dark:bg-gray-700 dark:text-gray-200'"
                class="px-2 py-0.5 rounded-full text-xs">
                {{ m.isActive ? 'Aktif' : 'Pasif' }}
              </span>
            </td>
            <td class="px-4 py-2 text-right text-sm">
              <button @click="openEdit(m)" class="text-indigo-600 hover:text-indigo-800 mr-3">Düzenle</button>
              <button @click="remove(m)" class="text-red-600 hover:text-red-800">Sil</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Show inactive toggle -->
    <div class="mt-3 flex items-center gap-2 text-sm">
      <input id="showInactive" v-model="showInactive" type="checkbox" class="rounded" />
      <label for="showInactive">Pasifleri göster</label>
    </div>

    <!-- Create/Edit Modal -->
    <div v-if="modalOpen" class="fixed inset-0 z-[60] overflow-y-auto" role="dialog" aria-modal="true">
      <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div class="fixed inset-0 bg-gray-500 bg-opacity-75" @click="modalOpen = false"></div>
        <span class="hidden sm:inline-block sm:align-middle sm:h-screen">&#8203;</span>
        <div class="inline-block align-bottom bg-white dark:bg-gray-900 rounded-lg px-4 pt-5 pb-4 text-left shadow-xl transform sm:my-8 sm:align-middle sm:max-w-md sm:w-full sm:p-6">
          <h3 class="text-lg font-medium">{{ modalMode === 'edit' ? 'Eşleşmeyi Düzenle' : 'Yeni Eşleşme' }}</h3>
          <div class="mt-4 space-y-3">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Kurum Kodu <span class="text-red-500">*</span></label>
              <input v-model="form.institutionCode" type="text" class="mt-1 input" placeholder="ISS'ten gelen KurumKodu" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Netsis Cari Kodu <span class="text-red-500">*</span></label>
              <input v-model="form.netsisCariKodu" type="text" class="mt-1 input" placeholder="örn: 120.01.001" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Açıklama</label>
              <input v-model="form.description" type="text" class="mt-1 input" placeholder="ISS Catering, Proser..." />
            </div>
            <div v-if="modalMode === 'edit'" class="flex items-center gap-2">
              <input id="isActiveEdit" v-model="form.isActive" type="checkbox" class="rounded" />
              <label for="isActiveEdit" class="text-sm">Aktif</label>
            </div>
          </div>
          <div class="mt-5 sm:grid sm:grid-cols-2 sm:gap-3">
            <button @click="save" :disabled="saving" class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-indigo-600 text-base font-medium text-white hover:bg-indigo-700 disabled:opacity-50 sm:col-start-2 sm:text-sm">
              {{ saving ? 'Kaydediliyor...' : 'Kaydet' }}
            </button>
            <button @click="modalOpen = false" :disabled="saving" class="mt-3 sm:mt-0 sm:col-start-1 w-full inline-flex justify-center rounded-md border border-gray-300 dark:border-gray-700 shadow-sm px-4 py-2 bg-white dark:bg-gray-800 text-base font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50 sm:text-sm">
              İptal
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Apply preview modal -->
    <div v-if="applyModalOpen" class="fixed inset-0 z-[60] overflow-y-auto" role="dialog" aria-modal="true">
      <div class="flex items-end justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
        <div class="fixed inset-0 bg-gray-500 bg-opacity-75" @click="applyModalOpen = false"></div>
        <span class="hidden sm:inline-block sm:align-middle sm:h-screen">&#8203;</span>
        <div class="inline-block align-bottom bg-white dark:bg-gray-900 rounded-lg px-4 pt-5 pb-4 text-left shadow-xl transform sm:my-8 sm:align-middle sm:max-w-4xl sm:w-full sm:p-6">
          <h3 class="text-lg font-medium">Mevcut Projeleri Senkronize Et</h3>

          <div v-if="applyLoading" class="py-6 text-center text-gray-500">Analiz yapılıyor...</div>
          <div v-else-if="!applyResult" class="py-6 text-center text-gray-500">Henüz analiz yok.</div>
          <div v-else class="mt-4 space-y-4">
            <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
              <div class="bg-gray-50 dark:bg-gray-800 p-3 rounded">
                <div class="text-xs text-gray-500">Toplam Taranan</div>
                <div class="text-xl font-semibold">{{ applyResult.totalProjectsScanned }}</div>
              </div>
              <div class="bg-amber-50 dark:bg-amber-900 p-3 rounded">
                <div class="text-xs text-amber-700 dark:text-amber-300">Etkilenecek</div>
                <div class="text-xl font-semibold text-amber-800 dark:text-amber-200">{{ applyResult.affectedCount }}</div>
              </div>
              <div class="bg-green-50 dark:bg-green-900 p-3 rounded">
                <div class="text-xs text-green-700 dark:text-green-300">Değişiklik Yok</div>
                <div class="text-xl font-semibold text-green-800 dark:text-green-200">{{ applyResult.unchangedCount }}</div>
              </div>
              <div class="bg-red-50 dark:bg-red-900 p-3 rounded">
                <div class="text-xs text-red-700 dark:text-red-300">Eşleşme Yok</div>
                <div class="text-xl font-semibold text-red-800 dark:text-red-200">{{ applyResult.noMappingCount }}</div>
              </div>
            </div>

            <div v-if="applyResult.changes.length > 0">
              <h4 class="text-sm font-semibold mb-2">Etkilenecek Projeler ({{ applyResult.changes.length }})</h4>
              <div class="max-h-64 overflow-y-auto border border-gray-200 dark:border-gray-700 rounded">
                <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
                  <thead class="bg-gray-50 dark:bg-gray-800 sticky top-0">
                    <tr>
                      <th class="px-3 py-1.5 text-left text-xs font-medium text-gray-500">Proje</th>
                      <th class="px-3 py-1.5 text-left text-xs font-medium text-gray-500">Kurum</th>
                      <th class="px-3 py-1.5 text-left text-xs font-medium text-gray-500">Mevcut</th>
                      <th class="px-3 py-1.5 text-left text-xs font-medium text-gray-500">Yeni</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
                    <tr v-for="c in applyResult.changes" :key="c.projectId">
                      <td class="px-3 py-1.5">
                        <div class="font-mono text-xs text-gray-500">{{ c.projectCode }}</div>
                        <div>{{ c.projectName }}</div>
                      </td>
                      <td class="px-3 py-1.5 font-mono text-xs">{{ c.institutionCode || '—' }}</td>
                      <td class="px-3 py-1.5 font-mono text-xs text-red-600 dark:text-red-400">{{ c.currentNetsisCariKodu || '(boş)' }}</td>
                      <td class="px-3 py-1.5 font-mono text-xs text-green-600 dark:text-green-400">{{ c.newNetsisCariKodu }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <div v-if="applyResult.withoutMapping.length > 0">
              <details class="border border-amber-200 dark:border-amber-800 rounded">
                <summary class="px-3 py-2 cursor-pointer text-sm font-medium text-amber-800 dark:text-amber-200 bg-amber-50 dark:bg-amber-900/30">
                  Eşleşmesi olmayan projeler ({{ applyResult.withoutMapping.length }})
                </summary>
                <div class="max-h-48 overflow-y-auto p-2 text-xs space-y-1">
                  <div v-for="p in applyResult.withoutMapping" :key="p.projectId" class="flex justify-between border-b border-gray-100 dark:border-gray-800 pb-1">
                    <span>{{ p.projectCode }} — {{ p.projectName }}</span>
                    <span class="font-mono text-gray-500">{{ p.institutionCode || '(kurum kodu yok)' }}</span>
                  </div>
                </div>
              </details>
            </div>
          </div>

          <div class="mt-5 sm:grid sm:grid-cols-2 sm:gap-3">
            <button
              @click="confirmApply"
              :disabled="applyLoading || !applyResult || applyResult.affectedCount === 0 || applying"
              class="w-full inline-flex justify-center rounded-md border border-transparent shadow-sm px-4 py-2 bg-amber-600 text-base font-medium text-white hover:bg-amber-700 disabled:opacity-50 sm:col-start-2 sm:text-sm"
            >
              {{ applying ? 'Uygulanıyor...' : `Onaylıyorum (${applyResult?.affectedCount ?? 0} proje güncellenir)` }}
            </button>
            <button @click="applyModalOpen = false" :disabled="applying" class="mt-3 sm:mt-0 sm:col-start-1 w-full inline-flex justify-center rounded-md border border-gray-300 dark:border-gray-700 shadow-sm px-4 py-2 bg-white dark:bg-gray-800 text-base font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-50 sm:text-sm">
              Kapat
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue';
import institutionCariMappingService, {
  type InstitutionCariMapping,
  type ApplyMappingsResult,
  type UpdateMappingInput,
} from '../services/institutionCariMappingService';
import { useNotification } from '../composables/useNotification';
import { ApiErrorUtils } from '../utils/apiError';

const { notify, confirm } = useNotification();

const mappings = ref<InstitutionCariMapping[]>([]);
const loading = ref(false);
const showInactive = ref(false);

const modalOpen = ref(false);
const modalMode = ref<'create' | 'edit'>('create');
const editingId = ref<number | null>(null);
const saving = ref(false);

const form = reactive<Omit<UpdateMappingInput, 'id'>>({
  institutionCode: '',
  netsisCariKodu: '',
  description: '',
  isActive: true,
});

const applyModalOpen = ref(false);
const applyLoading = ref(false);
const applying = ref(false);
const applyResult = ref<ApplyMappingsResult | null>(null);

async function fetchData() {
  loading.value = true;
  try {
    mappings.value = await institutionCariMappingService.getAll(showInactive.value);
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Eşleşmeler yüklenemedi.'));
  } finally {
    loading.value = false;
  }
}

watch(showInactive, fetchData);

function resetForm() {
  form.institutionCode = '';
  form.netsisCariKodu = '';
  form.description = '';
  form.isActive = true;
}

function openCreate() {
  resetForm();
  modalMode.value = 'create';
  editingId.value = null;
  modalOpen.value = true;
}

function openEdit(m: InstitutionCariMapping) {
  form.institutionCode = m.institutionCode;
  form.netsisCariKodu = m.netsisCariKodu;
  form.description = m.description ?? '';
  form.isActive = m.isActive;
  modalMode.value = 'edit';
  editingId.value = m.id;
  modalOpen.value = true;
}

async function save() {
  if (!form.institutionCode?.trim() || !form.netsisCariKodu?.trim()) {
    notify.error('Kurum Kodu ve Netsis Cari Kodu zorunludur.');
    return;
  }
  saving.value = true;
  try {
    if (modalMode.value === 'create') {
      await institutionCariMappingService.create({
        institutionCode: form.institutionCode.trim(),
        netsisCariKodu: form.netsisCariKodu.trim(),
        description: form.description?.trim() || null,
      });
      notify.success('Eşleşme oluşturuldu.');
    } else if (editingId.value != null) {
      await institutionCariMappingService.update(editingId.value, {
        id: editingId.value,
        institutionCode: form.institutionCode.trim(),
        netsisCariKodu: form.netsisCariKodu.trim(),
        description: form.description?.trim() || null,
        isActive: form.isActive,
      });
      notify.success('Eşleşme güncellendi.');
    }
    modalOpen.value = false;
    fetchData();
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Kaydedilemedi.'));
  } finally {
    saving.value = false;
  }
}

async function remove(m: InstitutionCariMapping) {
  const ok = await confirm.show({
    title: 'Eşleşmeyi Sil',
    message: `'${m.institutionCode}' kurum kodu için eşleşme silinsin mi? Bu projelerin mevcut NetsisCariKodu'su değişmez ama yeni proje açılırken bu kurum koduna otomatik atama yapılmaz.`,
    confirmText: 'Sil',
    type: 'danger',
  });
  if (!ok) return;
  try {
    await institutionCariMappingService.remove(m.id);
    notify.success('Silindi.');
    fetchData();
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Silinemedi.'));
  }
}

async function openApplyPreview() {
  applyModalOpen.value = true;
  applyResult.value = null;
  applyLoading.value = true;
  try {
    applyResult.value = await institutionCariMappingService.applyToProjects(true);
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Analiz yapılamadı.'));
    applyModalOpen.value = false;
  } finally {
    applyLoading.value = false;
  }
}

async function confirmApply() {
  if (!applyResult.value || applyResult.value.affectedCount === 0) return;
  const ok = await confirm.show({
    title: 'Toplu Güncelleme',
    message: `${applyResult.value.affectedCount} projenin NetsisCariKodu'su güncellenecek. Bu işlem geri alınamaz. Devam edilsin mi?`,
    confirmText: 'Uygula',
    type: 'warning',
  });
  if (!ok) return;

  applying.value = true;
  try {
    const result = await institutionCariMappingService.applyToProjects(false);
    notify.success(`${result.affectedCount} proje güncellendi.`);
    applyModalOpen.value = false;
    fetchData();
  } catch (e) {
    notify.error(ApiErrorUtils.getErrorMessage(e, 'Güncelleme başarısız.'));
  } finally {
    applying.value = false;
  }
}

fetchData();
</script>

<style scoped>
.input {
  @apply bg-white dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 block w-full border border-gray-300 rounded-md shadow-sm py-2 px-3 focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm;
}
</style>
