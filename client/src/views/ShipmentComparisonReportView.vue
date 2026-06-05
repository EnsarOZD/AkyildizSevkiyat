<template>
  <div class="space-y-5">
    <PageHeader
      title="Sevkiyat Karşılaştırma Raporu"
      subtitle="ISS sipariş miktarı, sevkiyat miktarı ve irsaliye karşılaştırması"
      color="indigo"
    >
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
      </template>
      <template #actions>
        <button @click="exportExcel" :disabled="loading || !report"
          class="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 rounded-lg transition-colors">
          <ArrowDownTrayIcon class="w-4 h-4" />
          Excel İndir
        </button>
      </template>
    </PageHeader>

    <!-- Filtreler -->
    <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-4">
      <div class="flex flex-wrap gap-4 items-end">
        <div>
          <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Başlangıç Tarihi</label>
          <input type="date" v-model="filters.dateFrom"
            class="px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500" />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Bitiş Tarihi</label>
          <input type="date" v-model="filters.dateTo"
            class="px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500" />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Bölge</label>
          <select v-model.number="filters.zoneId"
            class="px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500">
            <option :value="undefined">Tüm Bölgeler</option>
            <option v-for="z in zones" :key="z.id" :value="z.id">{{ z.name }}</option>
          </select>
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Durum Filtresi</label>
          <select v-model="filters.statusFilter"
            class="px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500">
            <option value="all">Tümü</option>
            <option value="issues">Sorunlular</option>
            <option value="missing">Eksik / Gönderilmeyenler</option>
          </select>
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Mail Durumu</label>
          <select v-model="filters.mailSentFilter"
            class="px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500">
            <option :value="null">Tümü</option>
            <option :value="true">Mail Gönderildi</option>
            <option :value="false">Mail Gönderilmedi</option>
          </select>
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">Proje Ara</label>
          <input type="text" v-model="projectSearch" placeholder="Proje adı veya kodu..."
            class="px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500 w-52" />
        </div>
        <button @click="load(1)" :disabled="loading"
          class="px-4 py-2 text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 rounded-lg transition-colors">
          {{ loading ? 'Yükleniyor...' : 'Uygula' }}
        </button>
      </div>
    </div>

    <!-- Özet Kartlar -->
    <div v-if="report" class="grid grid-cols-2 sm:grid-cols-4 gap-4">
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 text-center">
        <p class="text-2xl font-bold text-gray-900 dark:text-white">{{ report.totalCount }}</p>
        <p class="text-xs text-gray-500 mt-1">Toplam Sevkiyat</p>
      </div>
      <div class="bg-green-50 dark:bg-green-900/20 rounded-xl border border-green-200 dark:border-green-700 p-4 text-center">
        <p class="text-2xl font-bold text-green-700 dark:text-green-300">{{ report.totalCount - report.totalIssues }}</p>
        <p class="text-xs text-green-600 dark:text-green-400 mt-1">Tam Eşleşme</p>
      </div>
      <div class="bg-amber-50 dark:bg-amber-900/20 rounded-xl border border-amber-200 dark:border-amber-700 p-4 text-center">
        <p class="text-2xl font-bold text-amber-700 dark:text-amber-300">{{ report.totalIssues - report.totalMissing }}</p>
        <p class="text-xs text-amber-600 dark:text-amber-400 mt-1">İkame / Kısmi</p>
      </div>
      <div class="bg-red-50 dark:bg-red-900/20 rounded-xl border border-red-200 dark:border-red-700 p-4 text-center">
        <p class="text-2xl font-bold text-red-700 dark:text-red-300">{{ report.totalMissing }}</p>
        <p class="text-xs text-red-600 dark:text-red-400 mt-1">Eksik / Stok Yetersiz</p>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-16">
      <div class="w-8 h-8 border-4 border-indigo-600 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- Hata -->
    <div v-else-if="error" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl p-4 text-red-700 dark:text-red-400 text-sm">
      {{ error }}
    </div>

    <!-- Tablo -->
    <template v-else-if="report && filteredItems.length > 0">
      <div class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 overflow-hidden">
        <div class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="w-8 px-3 py-3"></th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sipariş No</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Talep No</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İrsaliye No</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Proje Kodu</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Proje Adı</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Bölge</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tarih</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Eşleşme</th>
                <th class="px-4 py-3 w-10"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
              <template v-for="item in filteredItems" :key="item.shipmentId">
                <!-- Ana satır -->
                <tr
                  class="cursor-pointer transition-colors"
                  :class="[rowBgClass(item.overallStatus), expanded.has(item.shipmentId) ? 'border-b-0' : '']"
                  @click="toggle(item.shipmentId)"
                >
                  <td class="px-3 py-3 text-center text-gray-400">
                    <ChevronDownIcon v-if="expanded.has(item.shipmentId)" class="w-4 h-4 inline transition-transform rotate-180" />
                    <ChevronDownIcon v-else class="w-4 h-4 inline transition-transform" />
                  </td>
                  <td class="px-4 py-3 text-sm font-mono text-gray-900 dark:text-gray-100 whitespace-nowrap">
                    {{ item.orderNumber || '—' }}
                  </td>
                  <td class="px-4 py-3 text-sm font-medium text-gray-900 dark:text-gray-100 whitespace-nowrap">
                    {{ item.talepNo || '—' }}
                  </td>
                  <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400 whitespace-nowrap">
                    {{ item.irsaliyeNo || '—' }}
                  </td>
                  <td class="px-4 py-3 text-sm font-mono text-gray-600 dark:text-gray-400">{{ item.projectCode }}</td>
                  <td class="px-4 py-3 text-sm text-gray-900 dark:text-gray-100">{{ item.projectName }}</td>
                  <td class="px-4 py-3 text-sm text-gray-500 dark:text-gray-400 whitespace-nowrap">{{ item.zoneName || '—' }}</td>
                  <td class="px-4 py-3 text-sm text-gray-600 dark:text-gray-400 whitespace-nowrap">
                    {{ formatDate(item.deliveryDate) }}
                  </td>
                  <td class="px-4 py-3 text-sm">
                    <span class="px-2 py-0.5 rounded-full text-xs font-medium" :class="statusBadgeClass(item.shipmentStatus)">
                      {{ statusLabel(item.shipmentStatus) }}
                    </span>
                    <div v-if="item.cancelReason" class="mt-1 inline-flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-bold text-red-700 bg-red-100 dark:bg-red-900/30 dark:text-red-300" :title="`İptal sebebi: ${item.cancelReason}`">
                      İPTAL · {{ item.cancelReason }}
                    </div>
                  </td>
                  <td class="px-4 py-3">
                    <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-semibold" :class="overallBadgeClass(item.overallStatus)">
                      {{ overallLabel(item.overallStatus) }}
                    </span>
                  </td>
                  <td class="px-3 py-3 text-center" @click.stop>
                    <template v-if="needsMailButton(item)">
                      <!-- Gönderiliyor spinner -->
                      <div v-if="sendingMailId === item.shipmentId"
                        class="inline-flex items-center justify-center w-7 h-7">
                        <svg class="w-4 h-4 animate-spin text-blue-500" fill="none" viewBox="0 0 24 24">
                          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                        </svg>
                      </div>
                      <template v-else>
                        <!-- Daha önce gönderildi badge -->
                        <div v-if="item.missingItemsMailSentAt" class="flex flex-col items-center gap-0.5">
                          <button
                            @click="sendMail(item)"
                            :disabled="!item.yoneticiMail"
                            :title="item.yoneticiMail ? `Tekrar gönder: ${item.yoneticiMail}` : 'Mail adresi yok'"
                            class="inline-flex items-center justify-center w-6 h-6 rounded transition-colors text-green-600 dark:text-green-400 hover:bg-green-50 dark:hover:bg-green-900/30 disabled:opacity-40 disabled:cursor-not-allowed"
                          >
                            <svg xmlns="http://www.w3.org/2000/svg" class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                              <path stroke-linecap="round" stroke-linejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                            </svg>
                          </button>
                          <span class="text-[9px] text-green-600 dark:text-green-400 leading-none whitespace-nowrap">
                            {{ formatMailDate(item.missingItemsMailSentAt) }}
                          </span>
                        </div>
                        <!-- İlk gönderim butonu -->
                        <button
                          v-else
                          @click="sendMail(item)"
                          :disabled="!item.yoneticiMail"
                          :title="item.yoneticiMail ? `Mail gönder: ${item.yoneticiMail}` : 'Mail adresi yok'"
                          class="inline-flex items-center justify-center w-7 h-7 rounded-md transition-colors"
                          :class="item.yoneticiMail
                            ? 'text-blue-600 dark:text-blue-400 hover:bg-blue-50 dark:hover:bg-blue-900/30'
                            : 'text-gray-300 dark:text-gray-600 cursor-not-allowed'"
                        >
                          <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                          </svg>
                        </button>
                      </template>
                    </template>
                  </td>
                </tr>

                <!-- Detay satırları (expanded) -->
                <tr v-if="expanded.has(item.shipmentId)" :key="`detail-${item.shipmentId}`">
                  <td colspan="11" class="p-0">
                    <div class="bg-gray-50 dark:bg-gray-800/60 border-t border-gray-200 dark:border-gray-700 px-6 py-3">
                      <table class="w-full text-xs">
                        <thead>
                          <tr class="text-gray-500 dark:text-gray-400 border-b border-gray-200 dark:border-gray-600">
                            <th class="py-1.5 text-left font-medium pr-4">ISS Stok Kodu</th>
                            <th class="py-1.5 text-left font-medium pr-4">ISS Ürün Adı</th>
                            <th class="py-1.5 text-right font-medium pr-4">ISS Miktar</th>
                            <th class="py-1.5 text-left font-medium pr-4">Gönderilen Stok Kodu</th>
                            <th class="py-1.5 text-left font-medium pr-4">Gönderilen Ürün Adı</th>
                            <th class="py-1.5 text-right font-medium pr-4">Gönderilen Miktar</th>
                            <th class="py-1.5 text-left font-medium pr-4">Fark</th>
                            <th class="py-1.5 text-left font-medium">Durum / Açıklama</th>
                          </tr>
                        </thead>
                        <tbody class="divide-y divide-gray-100 dark:divide-gray-700">
                          <tr v-for="(line, li) in item.lines" :key="li" :class="lineBgClass(line.status)">
                            <td class="py-1.5 pr-4 font-mono text-gray-700 dark:text-gray-300">{{ line.issStockCode }}</td>
                            <td class="py-1.5 pr-4 text-gray-700 dark:text-gray-300">{{ line.issStockName }}</td>
                            <td class="py-1.5 pr-4 text-right font-medium text-gray-700 dark:text-gray-300">{{ line.issOrderedQty }}</td>
                            <td class="py-1.5 pr-4 font-mono"
                              :class="line.status === 'substitution' || line.status === 'partial_substitution' ? 'text-amber-600 dark:text-amber-400 font-semibold' : 'text-gray-700 dark:text-gray-300'">
                              {{ line.actualStockCode || '—' }}
                            </td>
                            <td class="py-1.5 pr-4"
                              :class="line.status === 'substitution' || line.status === 'partial_substitution' ? 'text-amber-600 dark:text-amber-400' : 'text-gray-700 dark:text-gray-300'">
                              {{ line.actualStockName || '—' }}
                            </td>
                            <td class="py-1.5 pr-4 text-right font-medium"
                              :class="line.actualQty < line.issOrderedQty ? 'text-red-600 dark:text-red-400' : 'text-green-600 dark:text-green-400'">
                              {{ line.actualQty }}
                            </td>
                            <td class="py-1.5 pr-4 font-medium"
                              :class="line.actualQty - line.issOrderedQty < 0 ? 'text-red-600 dark:text-red-400' : 'text-gray-500 dark:text-gray-400'">
                              {{ line.actualQty - line.issOrderedQty > 0 ? '+' : '' }}{{ line.status !== 'extra' ? (line.actualQty - line.issOrderedQty) : '—' }}
                            </td>
                            <td class="py-1.5">
                              <span class="px-1.5 py-0.5 rounded text-xs font-medium" :class="lineBadgeClass(line.status)">
                                {{ lineStatusLabel(line.status) }}
                              </span>
                              <span v-if="line.differenceReason" class="ml-2 text-gray-500 dark:text-gray-400 italic">
                                {{ line.differenceReason }}
                              </span>
                            </td>
                          </tr>
                        </tbody>
                      </table>
                    </div>
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Sayfalama -->
      <div v-if="report.totalPages > 1" class="flex items-center justify-between px-1">
        <p class="text-sm text-gray-500 dark:text-gray-400">
          Toplam {{ report.totalCount }} kayıt — Sayfa {{ report.pageIndex }} / {{ report.totalPages }}
        </p>
        <div class="flex gap-2">
          <button @click="load(report!.pageIndex - 1)" :disabled="report.pageIndex <= 1 || loading"
            class="px-3 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded-lg disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-gray-800">
            ‹ Önceki
          </button>
          <button @click="load(report!.pageIndex + 1)" :disabled="report.pageIndex >= report.totalPages || loading"
            class="px-3 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded-lg disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-gray-800">
            Sonraki ›
          </button>
        </div>
      </div>
    </template>

    <!-- Boş -->
    <div v-else-if="report && filteredItems.length === 0 && !loading"
      class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-12 text-center">
      <p class="text-gray-500 dark:text-gray-400">Seçilen tarih aralığında sevkiyat bulunamadı.</p>
    </div>

    <!-- CC Seçim Modalı -->
    <BaseModal :show="showCcModal" title="Mail Gönder — CC Seç" maxWidth="sm" @close="showCcModal = false">
      <div class="space-y-3">
        <div v-if="ccModalItem" class="text-sm bg-gray-50 dark:bg-gray-800 rounded p-3 border dark:border-gray-700">
          <div class="font-semibold text-gray-800 dark:text-gray-100">{{ ccModalItem.projectName }}</div>
          <div class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">Alıcı: {{ ccModalItem.yoneticiMail }}</div>
        </div>

        <div>
          <p class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">CC'ye eklenecek kişiler <span class="text-gray-400 font-normal">(opsiyonel)</span></p>
          <div v-if="loadingContacts" class="text-sm text-gray-400 py-2">Yükleniyor...</div>
          <div v-else-if="externalContacts.length === 0" class="text-sm text-gray-400 py-2 italic">Harici mail adresi tanımlı değil.</div>
          <div v-else class="space-y-1 max-h-56 overflow-y-auto border dark:border-gray-700 rounded p-2">
            <label
              v-for="c in externalContacts"
              :key="c.id"
              class="flex items-center gap-3 px-2 py-2 rounded cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800"
            >
              <input
                type="checkbox"
                :checked="selectedCcIds.has(c.id)"
                @change="toggleCc(c.id)"
                class="h-4 w-4 rounded border-gray-300 text-indigo-600"
              />
              <div class="flex-1 min-w-0">
                <div class="text-sm font-medium text-gray-800 dark:text-gray-100">{{ c.name }}</div>
                <div class="text-xs text-gray-500 dark:text-gray-400">{{ c.email }}</div>
              </div>
            </label>
          </div>
        </div>
      </div>

      <template #footer>
        <button @click="showCcModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-sm">İptal</button>
        <button
          @click="submitSendMail"
          class="px-4 py-2 bg-indigo-600 text-white rounded hover:bg-indigo-700 font-semibold text-sm"
        >
          Gönder
        </button>
      </template>
    </BaseModal>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ChevronDownIcon, ArrowDownTrayIcon } from '@heroicons/vue/24/outline';
import PageHeader from '../components/PageHeader.vue';
import BaseModal from '../components/BaseModal.vue';
import comparisonReportService, { type ShipmentComparisonReportDto, type ShipmentComparisonDto } from '../services/comparisonReportService';
import shipmentService, { type ZoneItem } from '../services/shipmentService';
import apiClient from '../services/apiClient';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';
import { formatDate } from '../utils/dateFormat';

const notify = useNotificationStore();
const route = useRoute();
const router = useRouter();

const today = new Date().toISOString().slice(0, 10);

const zones = ref<ZoneItem[]>([]);

const FILTER_STORAGE_KEY = 'shipment_comparison_last_query';

function resolveInitialFilters() {
  const q = route.query;
  if (q.dateFrom || q.dateTo || q.zone || q.status || q.mailSent) {
    return {
      dateFrom: (q.dateFrom as string) || today,
      dateTo: (q.dateTo as string) || today,
      zoneId: q.zone ? Number(q.zone) : undefined,
      statusFilter: (q.status as 'all' | 'issues' | 'missing') || 'all',
      mailSentFilter: q.mailSent === 'true' ? true : q.mailSent === 'false' ? false : null,
    };
  }
  try {
    const saved = sessionStorage.getItem(FILTER_STORAGE_KEY);
    if (saved) {
      const s = JSON.parse(saved);
      return {
        dateFrom: s.dateFrom || today,
        dateTo: s.dateTo || today,
        zoneId: s.zone ? Number(s.zone) : undefined,
        statusFilter: (s.status as 'all' | 'issues' | 'missing') || 'all',
        mailSentFilter: s.mailSent === 'true' ? true : s.mailSent === 'false' ? false : null,
      };
    }
  } catch { /* ignore */ }
  return { dateFrom: today, dateTo: today, zoneId: undefined, statusFilter: 'all' as const, mailSentFilter: null };
}

const initFilters = resolveInitialFilters();

const filters = ref({
  dateFrom: initFilters.dateFrom,
  dateTo: initFilters.dateTo,
  zoneId: initFilters.zoneId as number | undefined,
  statusFilter: initFilters.statusFilter as 'all' | 'issues' | 'missing',
  mailSentFilter: initFilters.mailSentFilter as boolean | null,
});

function syncUrl() {
  const query: Record<string, string> = {};
  if (filters.value.dateFrom && filters.value.dateFrom !== today) query.dateFrom = filters.value.dateFrom;
  if (filters.value.dateTo && filters.value.dateTo !== today) query.dateTo = filters.value.dateTo;
  if (filters.value.zoneId) query.zone = String(filters.value.zoneId);
  if (filters.value.statusFilter && filters.value.statusFilter !== 'all') query.status = filters.value.statusFilter;
  if (filters.value.mailSentFilter !== null && filters.value.mailSentFilter !== undefined)
    query.mailSent = String(filters.value.mailSentFilter);
  router.replace({ query });
  try {
    sessionStorage.setItem(FILTER_STORAGE_KEY, JSON.stringify(query));
  } catch { /* ignore */ }
}

const projectSearch = ref('');
const loading = ref(false);
const error = ref('');
const report = ref<ShipmentComparisonReportDto | null>(null);
const expanded = ref<Set<number>>(new Set());

const filteredItems = computed<ShipmentComparisonDto[]>(() => {
  if (!report.value) return [];
  const q = projectSearch.value.trim().toLowerCase();
  if (!q) return report.value.items;
  return report.value.items.filter(
    i => i.projectName.toLowerCase().includes(q) || i.projectCode.toLowerCase().includes(q)
  );
});

async function load(page = 1) {
  syncUrl();
  loading.value = true;
  error.value = '';
  expanded.value = new Set();
  try {
    report.value = await comparisonReportService.get({
      dateFrom: filters.value.dateFrom,
      dateTo: filters.value.dateTo,
      zoneId: filters.value.zoneId,
      statusFilter: filters.value.statusFilter,
      mailSentFilter: filters.value.mailSentFilter ?? undefined,
      pageNumber: page,
      pageSize: 25,
    });
  } catch (e) {
    error.value = ApiErrorUtils.getErrorMessage(e) || 'Rapor yüklenemedi.';
    notify.add(error.value, 'error');
  } finally {
    loading.value = false;
  }
}

function toggle(id: number) {
  if (expanded.value.has(id)) {
    expanded.value.delete(id);
  } else {
    expanded.value.add(id);
  }
  // trigger reactivity
  expanded.value = new Set(expanded.value);
}

// ── Formatters ───────────────────────────────────────────────────────────────

function statusLabel(s: string) {
  const m: Record<string, string> = {
    Created: 'Oluşturuldu', AssignedToWarehouse: 'Depoda', Picking: 'Toplama',
    ReadyForDispatch: 'Sevke Hazır', AssignedToVehicle: 'Araçta',
    Dispatched: 'Yolda', Delivered: 'Teslim', Cancelled: 'İptal', Passive: 'İptal/Pasif',
  };
  return m[s] || s;
}

function statusBadgeClass(s: string) {
  const m: Record<string, string> = {
    Created: 'bg-gray-100 text-gray-700',
    AssignedToWarehouse: 'bg-blue-100 text-blue-700',
    Picking: 'bg-yellow-100 text-yellow-800',
    ReadyForDispatch: 'bg-indigo-100 text-indigo-700',
    AssignedToVehicle: 'bg-purple-100 text-purple-700',
    Dispatched: 'bg-orange-100 text-orange-700',
    Delivered: 'bg-green-100 text-green-700',
    Passive: 'bg-red-100 text-red-700',
  };
  return m[s] || 'bg-gray-100 text-gray-600';
}

function overallLabel(s: string) {
  const m: Record<string, string> = {
    full_match: '✓ Tam Eşleşme',
    has_substitutions: '⇄ İkame Var',
    has_shortfalls: '↓ Kısmi Gönderim',
    has_missing: '✕ Eksik Ürün',
    critical: '⚠ Kritik',
  };
  return m[s] || s;
}

function overallBadgeClass(s: string) {
  const m: Record<string, string> = {
    full_match: 'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-300',
    has_substitutions: 'bg-amber-100 text-amber-800 dark:bg-amber-900/40 dark:text-amber-300',
    has_shortfalls: 'bg-orange-100 text-orange-800 dark:bg-orange-900/40 dark:text-orange-300',
    has_missing: 'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-300',
    critical: 'bg-red-200 text-red-900 dark:bg-red-800/60 dark:text-red-200',
  };
  return m[s] || 'bg-gray-100 text-gray-600';
}

function rowBgClass(s: string) {
  const m: Record<string, string> = {
    full_match: 'hover:bg-green-50 dark:hover:bg-green-900/10',
    has_substitutions: 'bg-amber-50/40 dark:bg-amber-900/5 hover:bg-amber-50 dark:hover:bg-amber-900/10',
    has_shortfalls: 'bg-orange-50/40 dark:bg-orange-900/5 hover:bg-orange-50 dark:hover:bg-orange-900/10',
    has_missing: 'bg-red-50/40 dark:bg-red-900/5 hover:bg-red-50 dark:hover:bg-red-900/10',
    critical: 'bg-red-50/60 dark:bg-red-900/10 hover:bg-red-100/60 dark:hover:bg-red-900/20',
  };
  return m[s] || 'hover:bg-gray-50 dark:hover:bg-gray-800/30';
}

function lineStatusLabel(s: string) {
  const m: Record<string, string> = {
    full_match: 'Eşleşti', partial: 'Kısmi', substitution: 'İkame',
    partial_substitution: 'Kısmi İkame', no_fulfillment: 'Gönderilmedi',
    missing: 'Kalemde Yok', extra: 'Ekstra',
  };
  return m[s] || s;
}

function lineBadgeClass(s: string) {
  const m: Record<string, string> = {
    full_match: 'bg-green-100 text-green-800',
    partial: 'bg-orange-100 text-orange-800',
    substitution: 'bg-amber-100 text-amber-800',
    partial_substitution: 'bg-orange-200 text-orange-900',
    no_fulfillment: 'bg-red-100 text-red-800',
    missing: 'bg-red-200 text-red-900',
    extra: 'bg-blue-100 text-blue-700',
  };
  return m[s] || 'bg-gray-100 text-gray-600';
}

function lineBgClass(s: string) {
  const m: Record<string, string> = {
    substitution: 'bg-amber-50/60 dark:bg-amber-900/10',
    partial_substitution: 'bg-orange-50/60 dark:bg-orange-900/10',
    no_fulfillment: 'bg-red-50/60 dark:bg-red-900/10',
    missing: 'bg-red-100/60 dark:bg-red-900/20',
    partial: 'bg-orange-50/40 dark:bg-orange-900/5',
  };
  return m[s] || '';
}

// ── Mail ─────────────────────────────────────────────────────────────────────

const sendingMailId = ref<number | null>(null);

function needsMailButton(item: ShipmentComparisonDto): boolean {
  return item.overallStatus === 'has_missing' ||
         item.overallStatus === 'critical' ||
         item.overallStatus === 'has_shortfalls';
}

// CC Modal state
interface ExternalContact { id: number; name: string; email: string; note?: string }
const showCcModal = ref(false);
const ccModalItem = ref<ShipmentComparisonDto | null>(null);
const externalContacts = ref<ExternalContact[]>([]);
const selectedCcIds = ref<Set<number>>(new Set());
const loadingContacts = ref(false);

async function openMailModal(item: ShipmentComparisonDto) {
  if (!item.yoneticiMail || sendingMailId.value !== null) return;
  ccModalItem.value = item;
  selectedCcIds.value = new Set();
  showCcModal.value = true;
  if (externalContacts.value.length === 0) {
    loadingContacts.value = true;
    try {
      const res = await apiClient.get('/external-email-contacts', { params: { activeOnly: true } });
      externalContacts.value = (res.data || []).filter((c: ExternalContact) => c.email);
    } catch {
      externalContacts.value = [];
    } finally {
      loadingContacts.value = false;
    }
  }
}

function toggleCc(id: number) {
  if (selectedCcIds.value.has(id)) selectedCcIds.value.delete(id);
  else selectedCcIds.value.add(id);
  selectedCcIds.value = new Set(selectedCcIds.value);
}

async function submitSendMail() {
  if (!ccModalItem.value || sendingMailId.value !== null) return;
  const item = ccModalItem.value;
  showCcModal.value = false;
  sendingMailId.value = item.shipmentId;
  const ccEmails = externalContacts.value
    .filter(c => selectedCcIds.value.has(c.id))
    .map(c => c.email);
  try {
    await shipmentService.sendComparisonEmail(item.shipmentId, ccEmails.length ? ccEmails : undefined);
    item.missingItemsMailSentAt = new Date().toISOString();
    notify.add('E-posta başarıyla gönderildi.', 'success');
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'E-posta gönderilemedi.', 'error');
  } finally {
    sendingMailId.value = null;
  }
}

// Keep sendMail as alias to openMailModal for backward compatibility with template
const sendMail = openMailModal;

function formatMailDate(iso: string) {
  return new Date(iso).toLocaleDateString('tr-TR', { day: '2-digit', month: '2-digit' });
}

// ── Excel Export ──────────────────────────────────────────────────────────────

async function exportExcel() {
  if (!report.value) return;
  const { utils, writeFile } = await import('xlsx');

  const rows: any[] = [];
  for (const item of filteredItems.value) {
    for (const line of item.lines) {
      rows.push({
        'Sipariş No': item.orderNumber ?? '',
        'Talep No': item.talepNo ?? '',
        'İrsaliye No': item.irsaliyeNo ?? '',
        'Proje Kodu': item.projectCode,
        'Proje Adı': item.projectName,
        'Tarih': formatDate(item.deliveryDate),
        'Sevkiyat Durumu': statusLabel(item.shipmentStatus),
        'Eşleşme Durumu': overallLabel(item.overallStatus),
        'ISS Stok Kodu': line.issStockCode,
        'ISS Ürün Adı': line.issStockName,
        'ISS Miktar': line.issOrderedQty,
        'Gönderilen Stok Kodu': line.actualStockCode ?? '',
        'Gönderilen Ürün Adı': line.actualStockName ?? '',
        'Gönderilen Miktar': line.actualQty,
        'Fark': line.issOrderedQty > 0 ? line.actualQty - line.issOrderedQty : '',
        'Kalem Durumu': lineStatusLabel(line.status),
        'Açıklama': line.differenceReason ?? '',
      });
    }
  }

  const ws = utils.json_to_sheet(rows);
  const wb = utils.book_new();
  utils.book_append_sheet(wb, ws, 'Karşılaştırma');
  writeFile(wb, `sevkiyat-karsilastirma-${filters.value.dateFrom}.xlsx`);
}

onMounted(async () => {
  zones.value = await shipmentService.getZones().catch(() => []);
  // If filters came from sessionStorage (no URL params), push them into the URL
  if (!Object.keys(route.query).length) syncUrl();
  load();
});
</script>
