<template>
  <div class="p-4 max-w-7xl mx-auto">
    <div class="flex items-center justify-between mb-4">
      <h1 class="text-xl font-bold text-gray-900 dark:text-white">Kargo Takip</h1>
      <span class="text-sm text-gray-500 dark:text-gray-400">{{ totalCount }} kayıt</span>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4 mb-4">
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
        <div class="lg:col-span-2">
          <input
            v-model="filters.search"
            type="text"
            placeholder="Proje, sipariş no, talep no, kargo takip no..."
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
            @keyup.enter="loadReport(1)"
          />
        </div>
        <div>
          <select
            v-model="filters.ykStatus"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="">Tüm YK Durumlar</option>
            <option v-for="(label, code) in YK_STATUS_LABELS" :key="code" :value="code">{{ label }}</option>
            <option value="__null">Sorgulanmamış</option>
          </select>
        </div>
        <div class="flex gap-2">
          <button
            @click="loadReport(1)"
            :disabled="loading"
            class="flex-1 rounded-lg bg-blue-600 hover:bg-blue-700 disabled:opacity-50 text-white px-4 py-2 text-sm font-medium transition-colors"
          >
            {{ loading ? 'Yükleniyor...' : 'Ara' }}
          </button>
          <button
            @click="resetFilters"
            class="rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 px-3 py-2 text-sm transition-colors"
          >
            Sıfırla
          </button>
        </div>
      </div>
      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-3">
        <div>
          <label class="block text-xs text-gray-500 dark:text-gray-400 mb-1">Teslimat Tarihi (Başlangıç)</label>
          <input
            v-model="filters.startDate"
            type="date"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
        <div>
          <label class="block text-xs text-gray-500 dark:text-gray-400 mb-1">Teslimat Tarihi (Bitiş)</label>
          <input
            v-model="filters.endDate"
            type="date"
            class="w-full rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-gray-900 dark:text-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>
      </div>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
      <div v-if="loading && items.length === 0" class="flex justify-center items-center py-16">
        <svg class="animate-spin h-6 w-6 text-blue-500" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
        </svg>
      </div>

      <div v-else-if="items.length === 0" class="text-center py-16 text-gray-500 dark:text-gray-400">
        Kayıt bulunamadı.
      </div>

      <div v-else class="overflow-x-auto">
        <table class="min-w-full text-sm divide-y divide-gray-200 dark:divide-gray-700">
          <thead class="bg-gray-50 dark:bg-gray-800 text-xs text-gray-500 dark:text-gray-400 uppercase tracking-wide">
            <tr>
              <th class="px-4 py-3 text-left">Sevkiyat</th>
              <th class="px-4 py-3 text-left">Proje</th>
              <th class="px-4 py-3 text-left">Sipariş / Talep</th>
              <th class="px-4 py-3 text-left">Kargo Takip No</th>
              <th class="px-4 py-3 text-left">Teslimat</th>
              <th class="px-4 py-3 text-left">YK Durum</th>
              <th class="px-4 py-3 text-left">Son Sorgu</th>
              <th class="px-4 py-3 text-center">İşlem</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
            <tr
              v-for="item in items"
              :key="item.id"
              class="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors"
            >
              <td class="px-4 py-3 whitespace-nowrap">
                <RouterLink
                  :to="`/shipments/${item.id}`"
                  class="text-blue-600 dark:text-blue-400 hover:underline font-mono text-xs"
                >#{{ item.id }}</RouterLink>
                <div class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">{{ formatDate(item.deliveryDate) }}</div>
              </td>
              <td class="px-4 py-3">
                <div class="font-medium text-gray-900 dark:text-white text-sm">{{ item.projectName }}</div>
                <div class="text-xs text-gray-400 font-mono">{{ item.projectCode }}</div>
              </td>
              <td class="px-4 py-3">
                <div v-if="item.externalOrderNumber" class="font-mono text-xs text-blue-600 dark:text-blue-400">{{ item.externalOrderNumber }}</div>
                <div v-if="item.talepNo" class="text-xs text-gray-500 dark:text-gray-400">{{ item.talepNo }}</div>
                <div v-if="!item.externalOrderNumber && !item.talepNo" class="text-xs text-gray-400">—</div>
              </td>
              <td class="px-4 py-3">
                <span class="font-mono text-sm text-orange-600 dark:text-orange-400">{{ item.ykCargoKey }}</span>
                <div v-if="item.ykJobId" class="text-xs text-gray-400">İş No: {{ item.ykJobId }}</div>
              </td>
              <td class="px-4 py-3 whitespace-nowrap text-xs text-gray-600 dark:text-gray-400">
                {{ item.dispatchedAt ? formatDate(item.dispatchedAt) : formatDate(item.deliveryDate) }}
              </td>
              <td class="px-4 py-3">
                <div v-if="item.ykOperationStatus">
                  <span
                    class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium"
                    :class="ykStatusClass(item.ykOperationStatus)"
                  >{{ ykStatusLabel(item.ykOperationStatus) }}</span>
                  <div v-if="item.ykOperationMessage && item.ykOperationMessage !== item.ykOperationStatus" class="text-xs text-gray-400 mt-0.5 max-w-[180px] truncate" :title="item.ykOperationMessage">
                    {{ item.ykOperationMessage }}
                  </div>
                  <div v-if="item.ykErrorCode" class="text-xs text-red-500">Hata: {{ item.ykErrorCode }}</div>
                </div>
                <span v-else class="text-xs text-gray-400">Sorgulanmamış</span>
              </td>
              <td class="px-4 py-3 whitespace-nowrap text-xs text-gray-500 dark:text-gray-400">
                {{ item.ykLastQueryAt ? formatDate(item.ykLastQueryAt) : '—' }}
              </td>
              <td class="px-4 py-3 text-center">
                <button
                  @click="queryStatus(item)"
                  :disabled="queryingIds.has(item.id)"
                  class="inline-flex items-center gap-1 text-xs text-orange-600 dark:text-orange-400 hover:underline disabled:opacity-50"
                >
                  <svg v-if="queryingIds.has(item.id)" class="animate-spin h-3 w-3" fill="none" viewBox="0 0 24 24">
                    <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                    <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8z"/>
                  </svg>
                  <svg v-else class="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
                  </svg>
                  {{ queryingIds.has(item.id) ? 'Sorgulanıyor' : 'Sorgula' }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination -->
      <div v-if="totalPages > 1" class="flex items-center justify-between px-4 py-3 border-t border-gray-200 dark:border-gray-700">
        <span class="text-xs text-gray-500 dark:text-gray-400">
          Sayfa {{ currentPage }} / {{ totalPages }} ({{ totalCount }} kayıt)
        </span>
        <div class="flex gap-1">
          <button
            v-for="page in visiblePages"
            :key="page"
            @click="loadReport(page)"
            :class="[
              'px-3 py-1 rounded text-xs',
              page === currentPage
                ? 'bg-blue-600 text-white'
                : 'border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800'
            ]"
          >{{ page }}</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import warehouseService, { type YkCargoReportItem } from '../services/warehouseService'
import { useNotification } from '../composables/useNotification'

const { notify } = useNotification()

const YK_STATUS_LABELS: Record<string, string> = {
  NOP: 'İşlem Görmemiş',
  IND: 'Kargo Teslimatta',
  ISR: 'Fatura Düzenlenmedi',
  CNL: 'İptal Edildi',
  ISC: 'Daha Önce İptal',
  DLV: 'Teslim Edildi',
  BI:  'Şube İptal',
}

const YK_SUCCESS = new Set(['NOP', 'IND', 'ISR', 'DLV'])

const filters = reactive({
  search: '',
  ykStatus: '',
  startDate: '',
  endDate: '',
})

const loading = ref(false)
const items = ref<YkCargoReportItem[]>([])
const currentPage = ref(1)
const totalPages = ref(1)
const totalCount = ref(0)
const queryingIds = ref(new Set<number>())

const visiblePages = computed(() => {
  const pages: number[] = []
  const start = Math.max(1, currentPage.value - 2)
  const end = Math.min(totalPages.value, currentPage.value + 2)
  for (let i = start; i <= end; i++) pages.push(i)
  return pages
})

function ykStatusLabel(code: string): string {
  return YK_STATUS_LABELS[code] ?? code
}

function ykStatusClass(code: string): string {
  if (code === 'DLV') return 'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400'
  if (YK_SUCCESS.has(code)) return 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400'
  return 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400'
}

function formatDate(val: string): string {
  return new Date(val).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

async function loadReport(page = 1) {
  loading.value = true
  try {
    const result = await warehouseService.getYkCargoReport({
      search: filters.search || undefined,
      ykStatus: filters.ykStatus && filters.ykStatus !== '__null' ? filters.ykStatus : undefined,
      startDate: filters.startDate || undefined,
      endDate: filters.endDate || undefined,
      pageNumber: page,
      pageSize: 20,
    })
    items.value = result.items
    currentPage.value = result.pageIndex
    totalPages.value = result.totalPages
    totalCount.value = result.totalCount
  } catch {
    notify.error('Kargo raporu yüklenemedi.')
  } finally {
    loading.value = false
  }
}

async function queryStatus(item: YkCargoReportItem) {
  queryingIds.value = new Set([...queryingIds.value, item.id])
  try {
    const status = await warehouseService.queryYkShipmentStatus(item.id)
    if (status) {
      item.ykOperationStatus = status.statusCode ?? null
      item.ykOperationMessage = status.statusDescription ?? null
      item.ykLastQueryAt = new Date().toISOString()
    }
  } catch {
    notify.error('Durum sorgulanamadı.')
  } finally {
    queryingIds.value = new Set([...queryingIds.value].filter(id => id !== item.id))
  }
}

function resetFilters() {
  filters.search = ''
  filters.ykStatus = ''
  filters.startDate = ''
  filters.endDate = ''
  loadReport(1)
}

onMounted(() => loadReport(1))
</script>
