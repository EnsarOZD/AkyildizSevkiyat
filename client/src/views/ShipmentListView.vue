<template>
  <div>
  <div class="p-3 sm:p-6">
    <PageHeader title="Sevkiyatlar" subtitle="Aktif ve pasif sevkiyat listesi" color="blue">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
        </svg>
      </template>
    </PageHeader>

    <!-- Tabs -->
    <div class="mb-5 border-b border-gray-200 dark:border-gray-700">
      <nav class="-mb-px flex space-x-6">
        <button
          @click="activeTab = 'catering'; filters.status = ''; page = 1"
          :class="activeTab === 'catering'
            ? 'border-blue-500 text-blue-600'
            : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300'"
          class="whitespace-nowrap py-3 px-1 border-b-2 font-medium text-sm transition-colors"
        >
          Catering Siparişler
        </button>
        <button
          @click="activeTab = 'clothing'; filters.status = ''; page = 1"
          :class="activeTab === 'clothing'
            ? 'border-purple-500 text-purple-600'
            : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300'"
          class="whitespace-nowrap py-3 px-1 border-b-2 font-medium text-sm transition-colors"
        >
          Kıyafet Siparişler
        </button>
        <button
          @click="activeTab = 'passive'; filters.status = ''; page = 1"
          :class="activeTab === 'passive'
            ? 'border-gray-500 text-gray-600'
            : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300'"
          class="whitespace-nowrap py-3 px-1 border-b-2 font-medium text-sm transition-colors"
        >
          Pasif / Taslaklar
        </button>
        <button
          @click="activeTab = 'other'; filters.status = ''; page = 1"
          :class="activeTab === 'other'
            ? 'border-emerald-500 text-emerald-600'
            : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 hover:border-gray-300'"
          class="whitespace-nowrap py-3 px-1 border-b-2 font-medium text-sm transition-colors"
        >
          Diğer
        </button>
      </nav>
    </div>

    <!-- Manuel sevkiyat oluştur butonu — sadece Diğer sekmesinde -->
    <div v-if="activeTab === 'other'" v-role="['Admin', 'Manager', 'Accounting']" class="mb-4 flex justify-end">
      <button
        @click="manualShipmentModalOpen = true"
        class="inline-flex items-center px-4 py-2 bg-emerald-600 text-white text-sm rounded-md hover:bg-emerald-700"
      >
        + Manuel Sevkiyat Oluştur
      </button>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4 mb-5">
      <div class="grid grid-cols-2 sm:grid-cols-3 xl:grid-cols-6 gap-3">
        <div>
          <BaseInput type="date" label="Başlangıç" v-model="filters.startDate" size="sm" />
        </div>
        <div>
          <BaseInput type="date" label="Bitiş" v-model="filters.endDate" size="sm" />
        </div>
        <div v-if="activeTab !== 'passive'">
          <BaseSelect label="Durum" v-model="filters.status" size="sm">
            <option value="">Tümü</option>
            <option value="0">Taslak</option>
            <option value="1">Depoda</option>
            <option value="2">Toplanıyor</option>
            <option value="3">Sevke Hazır</option>
            <option value="4">Araçta</option>
            <option value="5">Yolda</option>
            <option value="6">Teslim Edildi</option>
          </BaseSelect>
        </div>
        <div>
          <BaseSelect label="Bölge" v-model="filters.zoneId" size="sm">
            <option value="">Tüm Bölgeler</option>
            <option v-for="z in zones" :key="z.id" :value="z.id">{{ z.name }}</option>
          </BaseSelect>
        </div>
        <div>
          <BaseSelect label="Gönderim Tipi" v-model="filters.dispatchType" size="sm">
            <option value="">Tümü</option>
            <option value="Cargo">Kargo</option>
            <option value="Freight">Nakliye</option>
            <option value="Vehicle">Araç</option>
            <option value="None">Atanmamış</option>
          </BaseSelect>
        </div>
        <div class="col-span-2 sm:col-span-3 xl:col-span-1">
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1.5">Arama</label>
          <div class="flex gap-2">
            <BaseInput
              type="text"
              v-model="searchQuery"
              @input="handleSearch"
              placeholder="Sevkiyat No, Talep No, Proje..."
              size="sm"
              class="flex-1 min-w-0"
            />
            <BaseButton @click="fetchShipments" variant="secondary" size="sm" class="shrink-0">
              Filtrele
            </BaseButton>
          </div>
        </div>
      </div>
    </div>

    <!-- Netsis Durum Kontrol butonu (Admin/Manager/Accounting) -->
    <div class="flex justify-end gap-2 mb-3">
      <!-- Kurtarma butonu — sadece Admin, tek seferlik bug recovery -->
      <div v-role="['Admin']">
        <button
          @click="recoverNetsisTransfers"
          :disabled="recoveringNetsis"
          class="flex items-center gap-1.5 text-xs font-medium px-3 py-1.5 rounded-lg border border-red-300 dark:border-red-700 text-red-700 dark:text-red-400 bg-red-50 dark:bg-red-900/20 hover:bg-red-100 dark:hover:bg-red-900/40 disabled:opacity-50 transition-colors"
          title="Araçta/yolda/iade durumundaki ve irsaliyesi silinmiş sevkiyatları Netsis'te arar. Bulunanları Sevke Hazır durumuna geri alır."
        >
          <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"/>
          </svg>
          <span v-if="recoveringNetsis">Kurtarılıyor...</span>
          <span v-else>Netsis Veri Kurtar</span>
        </button>
      </div>

      <div v-role="['Admin', 'Manager', 'Accounting']">
        <button
          @click="verifyNetsisTransfers"
          :disabled="verifyingNetsis"
          class="flex items-center gap-1.5 text-xs font-medium px-3 py-1.5 rounded-lg border border-amber-300 dark:border-amber-700 text-amber-700 dark:text-amber-400 bg-amber-50 dark:bg-amber-900/20 hover:bg-amber-100 dark:hover:bg-amber-900/40 disabled:opacity-50 transition-colors"
          title="Netsis'e aktarılmış görünen siparişlerin Netsis'te hâlâ mevcut olup olmadığını kontrol eder. Silinmişlerin aktarım durumu sıfırlanır. Aktarılmamış ama Netsis'te mevcut olanlar otomatik işaretlenir."
        >
          <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z"/>
          </svg>
          <span v-if="verifyingNetsis">Kontrol başlatıldı...</span>
          <span v-else>Netsis Durum Kontrol</span>
        </button>
      </div>
    </div>

    <!-- Skeleton while loading -->
    <SkeletonTable v-if="loading" :rows="8" :columns="10" />

    <template v-else>
      <!-- Empty state -->
      <div v-if="shipments.length === 0" class="bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl">
        <EmptyState
          :icon="ClipboardDocumentListIcon"
          title="Sevkiyat bulunamadı"
          :description="activeTab === 'passive'
            ? 'Pasife alınmış sevkiyat bulunmuyor.'
            : activeTab === 'clothing'
              ? 'Kıyafet sevkiyatı bulunamadı.'
              : 'Arama kriterlerinize uygun catering sevkiyatı yok.'"
        />
      </div>

      <template v-else>
        <!-- Desktop table -->
        <div class="hidden xl:block bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-3 w-10">
                  <input
                    type="checkbox"
                    :checked="isAllReadySelected"
                    :indeterminate="selectedIds.size > 0 && !isAllReadySelected"
                    @change="toggleSelectAll"
                    :disabled="pageReadyIds.length === 0"
                    class="w-4 h-4 rounded border-gray-300 text-blue-600 cursor-pointer disabled:opacity-30"
                    title="Sayfadaki tüm seçilebilir sevkiyatları seç (Sevke Hazır / Araçta / Yolda + irsaliyeli)"
                  />
                </th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer select-none hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors" @click="toggleSort('id')">
                  No <span class="ml-1 opacity-50">{{ sortKey === 'id' ? (sortDir === 'asc' ? '↑' : '↓') : '↕' }}</span>
                </th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Talep No</th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sipariş No</th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İrsaliye No</th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Proje</th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Bölge</th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer select-none hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors" @click="toggleSort('deliveryDate')">
                  Tarih <span class="ml-1 opacity-50">{{ sortKey === 'deliveryDate' ? (sortDir === 'asc' ? '↑' : '↓') : '↕' }}</span>
                </th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider cursor-pointer select-none hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors" @click="toggleSort('status')">
                  Durum <span class="ml-1 opacity-50">{{ sortKey === 'status' ? (sortDir === 'asc' ? '↑' : '↓') : '↕' }}</span>
                </th>
                <th class="px-5 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">Gönderim</th>
                <th class="px-5 py-3 text-right text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">İşlem</th>
              </tr>
            </thead>
            <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
              <tr
                v-for="shipment in sortedShipments"
                :key="shipment.id"
                class="hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors"
                :class="[rowClass(shipment), selectedIds.has(shipment.id) ? 'bg-blue-50/60 dark:bg-blue-900/20' : '']"
              >
                <td class="px-4 py-3 w-10">
                  <input
                    v-if="isSelectable(shipment)"
                    type="checkbox"
                    :checked="selectedIds.has(shipment.id)"
                    @change="toggleSelect(shipment.id)"
                    class="w-4 h-4 rounded border-gray-300 text-blue-600 cursor-pointer"
                  />
                </td>
                <td class="px-5 py-3 whitespace-nowrap text-sm font-bold text-gray-900 dark:text-gray-100">
                  #{{ shipment.id }}
                  <span v-if="isOverdue(shipment)" class="ml-1 px-1.5 py-0.5 text-[10px] font-bold bg-red-100 text-red-600 rounded uppercase">Gecikmiş</span>
                  <span v-else-if="isToday(shipment)" class="ml-1 px-1.5 py-0.5 text-[10px] font-bold bg-blue-100 text-blue-600 rounded uppercase">Bugün</span>
                </td>
                <td class="px-5 py-3 whitespace-nowrap text-sm text-gray-700 dark:text-gray-300">{{ shipment.talepNo || '-' }}</td>
                <td class="px-5 py-3 whitespace-nowrap text-sm font-mono text-gray-600 dark:text-gray-400">{{ shipment.externalOrderNumber || '-' }}</td>
                <td class="px-5 py-3 whitespace-nowrap text-sm font-mono text-purple-600">{{ shipment.waybillNumber || '-' }}</td>
                <td class="px-5 py-3 whitespace-nowrap text-sm">
                  <div class="flex items-center gap-1.5">
                    <span class="font-medium text-gray-900 dark:text-gray-100">{{ shipment.projectCode }}</span>
                    <span
                      v-if="shipment.operationTypeValue === 1"
                      class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-bold bg-purple-100 text-purple-700 dark:bg-purple-900/40 dark:text-purple-300 border border-purple-200 dark:border-purple-700"
                    >Kıyafet</span>
                  </div>
                  <div class="text-xs text-gray-400">{{ shipment.projectName }}</div>
                </td>
                <td class="px-5 py-3 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">{{ shipment.region }}</td>
                <td class="px-5 py-3 whitespace-nowrap text-sm" :class="isOverdue(shipment) ? 'text-red-600 font-medium' : 'text-gray-500 dark:text-gray-400'">
                  {{ formatDate(shipment.deliveryDate) }}
                </td>
                <td class="px-5 py-3 whitespace-nowrap">
                  <StatusBadge :status="shipment.status" type="shipment" />
                </td>
                <td class="px-5 py-3 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
                  <template v-for="info in [getDispatchInfo(shipment)]" :key="shipment.id + '-dispatch'">
                    <template v-if="info">
                      <div class="flex items-center gap-1.5">
                        <span
                          :class="{
                            'bg-sky-100 text-sky-700 dark:bg-sky-900/40 dark:text-sky-300': info.type === 'Kargo',
                            'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300': info.type === 'Nakliye',
                            'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300': info.type === 'Araç',
                          }"
                          class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-bold"
                        >{{ info.type }}</span>
                        <span class="text-gray-700 dark:text-gray-300">{{ info.label }}</span>
                      </div>
                      <div v-if="info.plate" class="text-xs text-gray-400 mt-0.5">{{ info.plate }}</div>
                    </template>
                    <span v-else class="text-gray-300">—</span>
                  </template>
                </td>
                <td class="px-4 py-3 whitespace-nowrap text-right">
                  <div class="flex justify-end items-center gap-2">
                    <span
                      v-if="shipment.status === 'ReadyForDispatch' && shipment.netsisTransferredAt"
                      class="px-2 py-0.5 text-[10px] font-medium text-green-700 dark:text-green-400 bg-green-50 dark:bg-green-900/20 rounded"
                      title="Netsis'e aktarıldı"
                    >✓ Netsis</span>
                    <ShipmentActionsDropdown
                      :shipment="shipment"
                      :exporting="exportingIds.has(shipment.id)"
                      @assign-to-warehouse="quickAssignToWarehouse(shipment.id)"
                      @start-picking="quickStartPicking(shipment.id)"
                      @mark-ready="quickMarkReady(shipment.id)"
                      @export-to-netsis="singleExportToNetsis(shipment.id)"
                      @mark-delivered="openMarkDeliveredModal(shipment.id)"
                      @delete-draft="deleteDraft(shipment.id)"
                      @passive-on="confirmToggleStatus(shipment.id, true)"
                      @passive-off="confirmToggleStatus(shipment.id, false)"
                      @cancel="openCancelModal(shipment.id)"
                    />
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Mobile / tablet cards -->
        <div class="xl:hidden space-y-3">
          <div
            v-for="shipment in sortedShipments"
            :key="'m-' + shipment.id"
            class="bg-white dark:bg-gray-900 rounded-xl border p-4 transition-colors"
            :class="isOverdue(shipment) ? 'border-red-200 bg-red-50/30' : 'border-gray-200 dark:border-gray-700'"
          >
            <div class="flex justify-between items-start mb-2">
              <div>
                <span class="text-base font-bold text-gray-900 dark:text-gray-100">#{{ shipment.id }}</span>
                <span v-if="isOverdue(shipment)" class="ml-2 px-1.5 py-0.5 text-[10px] font-bold bg-red-100 text-red-600 rounded uppercase">Gecikmiş</span>
                <span v-else-if="isToday(shipment)" class="ml-2 px-1.5 py-0.5 text-[10px] font-bold bg-blue-100 text-blue-600 rounded uppercase">Bugün</span>
                <span class="ml-2 text-xs" :class="isOverdue(shipment) ? 'text-red-500 font-medium' : 'text-gray-400'">
                  {{ formatDate(shipment.deliveryDate) }}
                </span>
              </div>
              <StatusBadge :status="shipment.status" type="shipment" />
            </div>
            <div class="mb-3">
              <div class="font-medium text-gray-800 dark:text-gray-200">{{ shipment.projectName }}</div>
              <div class="text-xs text-gray-400">{{ shipment.projectCode }} · {{ shipment.region }}</div>
            </div>
            <div class="text-sm text-gray-600 dark:text-gray-400 grid grid-cols-2 gap-2 mb-3">
              <div>
                <span class="block text-xs text-gray-400">Talep No</span>
                {{ shipment.talepNo || '-' }}
              </div>
              <div>
                <span class="block text-xs text-gray-400">Sipariş No</span>
                {{ shipment.externalOrderNumber || '-' }}
              </div>
              <div v-for="info in [getDispatchInfo(shipment)]" :key="'m-dispatch-' + shipment.id" class="col-span-2">
                <template v-if="info">
                  <span class="block text-xs text-gray-400">Gönderim</span>
                  <span
                    :class="{
                      'bg-sky-100 text-sky-700': info.type === 'Kargo',
                      'bg-amber-100 text-amber-700': info.type === 'Nakliye',
                      'bg-green-100 text-green-700': info.type === 'Araç',
                    }"
                    class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-bold mr-1.5"
                  >{{ info.type }}</span>
                  {{ info.label }}<span v-if="info.plate"> · {{ info.plate }}</span>
                </template>
              </div>
            </div>
            <div class="flex justify-between items-center border-t border-gray-100 dark:border-gray-700 pt-3">
              <router-link
                :to="`/shipments/${shipment.id}`"
                class="text-white bg-blue-600 hover:bg-blue-700 px-4 py-1.5 rounded-lg text-sm font-medium shadow-sm transition-colors"
              >Detay →</router-link>
              <div class="flex items-center gap-2">
                <span
                  v-if="shipment.status === 'ReadyForDispatch' && shipment.netsisTransferredAt"
                  class="px-2 py-0.5 text-[10px] font-medium text-green-700 bg-green-50 dark:bg-green-900/20 dark:text-green-400 rounded"
                >✓ Netsis</span>
                <ShipmentActionsDropdown
                  :shipment="shipment"
                  :exporting="exportingIds.has(shipment.id)"
                  @assign-to-warehouse="quickAssignToWarehouse(shipment.id)"
                  @start-picking="quickStartPicking(shipment.id)"
                  @mark-ready="quickMarkReady(shipment.id)"
                  @export-to-netsis="singleExportToNetsis(shipment.id)"
                  @mark-delivered="openMarkDeliveredModal(shipment.id)"
                  @delete-draft="deleteDraft(shipment.id)"
                  @passive-on="confirmToggleStatus(shipment.id, true)"
                  @passive-off="confirmToggleStatus(shipment.id, false)"
                  @cancel="openCancelModal(shipment.id)"
                />
              </div>
            </div>
          </div>
        </div>
      </template>
    </template>

    <!-- Pagination -->
    <div v-if="!loading && shipments.length > 0" class="mt-4 flex flex-col sm:flex-row justify-between items-center gap-3 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
      <p class="text-sm text-gray-500 dark:text-gray-400 order-2 sm:order-1">
        Toplam <span class="font-medium text-gray-700 dark:text-gray-300">{{ totalCount }}</span> kayıt · Sayfa {{ page }} / {{ totalPages }}
      </p>
      <div class="flex gap-2 order-1 sm:order-2 w-full sm:w-auto">
        <BaseButton @click="page--; fetchShipments()" :disabled="page <= 1" variant="secondary" size="sm" class="flex-1 sm:flex-none">← Önceki</BaseButton>
        <BaseButton @click="page++; fetchShipments()" :disabled="page >= totalPages" variant="secondary" size="sm" class="flex-1 sm:flex-none">Sonraki →</BaseButton>
      </div>
    </div>

  </div>

  <!-- Floating bulk action bar -->
  <Teleport to="body">
    <Transition
      enter-active-class="transition ease-out duration-200"
      enter-from-class="translate-y-4 opacity-0"
      enter-to-class="translate-y-0 opacity-100"
      leave-active-class="transition ease-in duration-150"
      leave-from-class="translate-y-0 opacity-100"
      leave-to-class="translate-y-4 opacity-0"
    >
      <div
        v-if="selectedIds.size > 0"
        class="fixed bottom-6 left-1/2 -translate-x-1/2 z-40 flex items-center gap-3 bg-gray-900 dark:bg-gray-100 text-white dark:text-gray-900 rounded-2xl shadow-2xl px-5 py-3"
      >
        <span class="text-sm font-medium">{{ selectedIds.size }} sevkiyat seçili</span>
        <div class="w-px h-5 bg-white/20 dark:bg-gray-900/20"></div>
        <button
          v-role="['Admin', 'Manager', 'Accounting']"
          @click="bulkExportToNetsis"
          :disabled="isBulkExporting"
          class="flex items-center gap-1.5 bg-orange-500 hover:bg-orange-600 disabled:bg-orange-400 text-white text-sm font-semibold px-3 py-1.5 rounded-lg transition-colors"
        >
          <span v-if="isBulkExporting" class="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          <span v-else>📤</span>
          {{ isBulkExporting ? 'Aktarılıyor...' : 'Netsis\'e Aktar' }}
        </button>
        <button
          v-role="['Admin', 'Driver', 'Manager']"
          @click="openBulkModal"
          class="flex items-center gap-1.5 bg-blue-500 hover:bg-blue-600 text-white text-sm font-semibold px-3 py-1.5 rounded-lg transition-colors"
        >
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M8.25 18.75a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m3 0h6m-9 0H3.375a1.125 1.125 0 0 1-1.125-1.125V14.25m17.25 4.5a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m3 0h1.125c.621 0 1.129-.504 1.09-1.124a17.902 17.902 0 0 0-3.213-9.193 2.056 2.056 0 0 0-1.58-.86H14.25M16.5 18.75h-2.25m0-11.177v-.958c0-.568-.422-1.048-.987-1.106a48.554 48.554 0 0 0-10.026 0 1.106 1.106 0 0 0-.987 1.106v7.635m12-6.677v6.677m0 4.5v-4.5m0 0h-12" /></svg>
          Araca Ata
        </button>
        <button
          v-role="['Admin', 'Manager', 'Accounting']"
          @click="openBulkDeliverModal"
          class="flex items-center gap-1.5 bg-green-600 hover:bg-green-700 text-white text-sm font-semibold px-3 py-1.5 rounded-lg transition-colors"
        >
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M9 12.75 11.25 15 15 9.75M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z" /></svg>
          Toplu Teslim Et
        </button>
        <button
          @click="selectedIds.clear()"
          class="text-sm text-white/70 dark:text-gray-900/60 hover:text-white dark:hover:text-gray-900 px-2 py-1 rounded-lg transition-colors"
        >
          İptal
        </button>
      </div>
    </Transition>
  </Teleport>

  <!-- Bulk dispatch modal -->
  <Teleport to="body">
    <div
      v-if="showDispatchModal"
      class="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50"
      @click.self="showDispatchModal = false"
    >
      <div class="bg-white dark:bg-gray-900 rounded-lg p-6 w-full max-w-lg shadow-xl border-t-4 border-blue-500 max-h-[90vh] overflow-y-auto">
        <h3 class="text-xl font-bold text-gray-800 dark:text-gray-200 mb-1">Toplu Sevkiyat Gönder</h3>
        <p class="text-sm text-gray-500 dark:text-gray-400 mb-4">
          <span class="font-medium text-gray-700 dark:text-gray-200">{{ selectedIds.size }} sevkiyat</span> gönderilecek. Gönderim tipini seçin.
        </p>

        <!-- Dispatch type selector -->
        <div class="flex gap-2 mb-5">
          <button
            v-for="opt in dispatchTypeOptions" :key="opt.value"
            @click="bulkDispatchType = opt.value"
            :class="bulkDispatchType === opt.value
              ? 'bg-blue-600 text-white border-blue-600'
              : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 border-gray-300 dark:border-gray-700 hover:border-blue-400'"
            class="flex-1 border rounded-lg px-3 py-2.5 text-sm font-medium transition-colors flex flex-col items-center gap-1"
          >
            <span class="text-lg">{{ opt.icon }}</span>
            <span>{{ opt.label }}</span>
          </button>
        </div>

        <!-- Loading -->
        <div v-if="bulkListsLoading" class="flex items-center gap-2 text-sm text-gray-500 dark:text-gray-400 mb-4 py-2">
          <span class="animate-spin h-4 w-4 border-2 border-blue-500 border-t-transparent rounded-full"></span>
          Yükleniyor...
        </div>

        <!-- Kargo form -->
        <div v-else-if="bulkDispatchType === 'cargo'" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Kargo Firması <span class="text-red-500">*</span></label>
            <select v-model="bulkCargoProvider"
              class="w-full border border-gray-300 dark:border-gray-700 rounded-md px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option :value="null">Seçiniz...</option>
              <option v-for="cp in cargoProviderOptions" :key="cp.value" :value="cp.value">{{ cp.label }}</option>
            </select>
          </div>
        </div>

        <!-- Nakliye form -->
        <div v-else-if="bulkDispatchType === 'freight'" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Taşıyıcı Adı <span class="text-red-500">*</span></label>
            <input
              v-model="bulkFreightCarrierName"
              type="text"
              placeholder="Örn: Ekol Lojistik"
              class="w-full border border-gray-300 dark:border-gray-700 rounded-md px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Plaka <span class="text-gray-400 font-normal">(opsiyonel)</span></label>
            <input
              v-model="bulkFreightCarrierPlate"
              type="text"
              placeholder="34 ABC 123"
              class="w-full border border-gray-300 dark:border-gray-700 rounded-md px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Telefon <span class="text-gray-400 font-normal">(opsiyonel)</span></label>
            <input
              v-model="bulkFreightCarrierPhone"
              type="tel"
              placeholder="05XX XXX XX XX"
              maxlength="30"
              class="w-full border border-gray-300 dark:border-gray-700 rounded-md px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>

        <!-- Araç form -->
        <div v-else-if="bulkDispatchType === 'vehicle'" class="space-y-4">
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
              Şoförler
              <span class="text-gray-400 font-normal ml-1">(ilk seçilen ana şoför)</span>
            </label>
            <div class="border border-gray-300 dark:border-gray-700 rounded-md max-h-48 overflow-y-auto">
              <label
                v-for="d in bulkActiveDrivers" :key="d.id"
                class="flex items-center gap-3 px-3 py-2.5 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors border-b border-gray-100 dark:border-gray-800 last:border-0"
              >
                <input
                  type="checkbox"
                  :value="d.id"
                  v-model="bulkSelectedDriverIds"
                  class="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span class="flex-1 text-sm text-gray-900 dark:text-gray-100">{{ d.fullName }}</span>
                <span class="text-xs text-gray-400">{{ d.phone }}</span>
                <span
                  v-if="bulkSelectedDriverIds[0] === d.id && bulkSelectedDriverIds.length > 0"
                  class="text-[11px] bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 px-1.5 py-0.5 rounded font-medium"
                >Ana Şoför</span>
              </label>
              <div v-if="bulkActiveDrivers.length === 0" class="px-3 py-3 text-sm text-gray-500 dark:text-gray-400 italic">
                Aktif şoför bulunamadı.
              </div>
            </div>
            <p v-if="bulkSelectedDriverIds.length > 0" class="mt-1 text-xs text-gray-500 dark:text-gray-400">
              {{ bulkSelectedDriverIds.length }} şoför seçildi.
            </p>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Araç <span class="text-red-500">*</span></label>
            <select v-model="bulkVehicleId"
              class="w-full border border-gray-300 dark:border-gray-700 rounded-md px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option :value="null">Seçiniz...</option>
              <option v-for="v in bulkActiveVehicles" :key="v.id" :value="v.id">
                {{ v.plateNumber }} — {{ v.vehicleTypeName ?? '' }}{{ v.capacity ? ` (${v.capacity})` : '' }}
              </option>
            </select>
          </div>
          <div>
            <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Hareket Saati</label>
            <input
              type="time"
              v-model="bulkDepartureTime"
              class="w-full border border-gray-300 dark:border-gray-700 rounded-md px-3 py-2 text-sm bg-white dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>

        <div class="mt-6 flex justify-end gap-3">
          <button @click="showDispatchModal = false"
            class="px-4 py-2.5 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 rounded border border-gray-300 dark:border-gray-700 text-sm">
            İptal
          </button>
          <button
            @click="submitBulkDispatch"
            :disabled="!canSubmitBulkDispatch || bulkSubmitting"
            class="px-6 py-2.5 bg-blue-600 text-white rounded font-bold hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed flex items-center gap-2 text-sm"
          >
            <span v-if="bulkSubmitting" class="animate-spin h-4 w-4 border-2 border-white border-t-transparent rounded-full"></span>
            <span>{{ bulkSubmitting ? 'Gönderiliyor...' : 'GÖNDER' }}</span>
          </button>
        </div>
      </div>
    </div>
  </Teleport>

  <!-- Mark Delivered Modal -->
  <Teleport to="body">
    <Transition
      enter-active-class="transition ease-out duration-150"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition ease-in duration-100"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="deliverModal.open"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
        @click.self="deliverModal.open = false"
      >
        <div class="w-full max-w-md bg-white dark:bg-gray-800 rounded-2xl shadow-2xl overflow-hidden">
          <div class="px-5 py-4 border-b border-gray-100 dark:border-gray-700">
            <h3 class="font-bold text-gray-900 dark:text-white">Teslim Edildi İşaretle</h3>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">Sevkiyat #{{ deliverModal.shipmentId }}</p>
          </div>
          <div class="px-5 py-4 space-y-3">
            <div>
              <label class="block text-xs font-semibold text-gray-700 dark:text-gray-300 mb-1">
                Teslim Alan <span class="text-red-500">*</span>
              </label>
              <input
                v-model="deliverModal.recipient"
                type="text"
                placeholder="Teslim alan kişi / güvenlik adı"
                class="w-full px-3 py-2 text-sm rounded-xl border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500"
              />
            </div>
            <div>
              <label class="block text-xs font-semibold text-gray-700 dark:text-gray-300 mb-1">
                Açıklama / Not <span class="text-red-500">*</span>
              </label>
              <textarea
                v-model="deliverModal.note"
                rows="2"
                placeholder="Teslim notu veya müdahale açıklaması"
                class="w-full px-3 py-2 text-sm rounded-xl border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
              ></textarea>
            </div>
            <p v-if="deliverModal.error" class="text-xs text-red-500">{{ deliverModal.error }}</p>
          </div>
          <div class="px-5 pb-5 flex justify-end gap-2">
            <button
              @click="deliverModal.open = false"
              class="px-4 py-2 text-sm text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-xl transition-colors"
            >İptal</button>
            <button
              @click="confirmMarkDelivered"
              :disabled="deliverModal.submitting"
              class="px-4 py-2 text-sm font-semibold text-white bg-green-600 hover:bg-green-700 disabled:opacity-50 rounded-xl transition-colors flex items-center gap-2"
            >
              <span v-if="deliverModal.submitting" class="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              {{ deliverModal.submitting ? 'İşleniyor...' : 'Teslim Et' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>

  <!-- Bulk Mark Delivered Modal -->
  <Teleport to="body">
    <Transition
      enter-active-class="transition ease-out duration-150"
      enter-from-class="opacity-0"
      enter-to-class="opacity-100"
      leave-active-class="transition ease-in duration-100"
      leave-from-class="opacity-100"
      leave-to-class="opacity-0"
    >
      <div
        v-if="bulkDeliverModal.open"
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
        @click.self="bulkDeliverModal.open = false"
      >
        <div class="w-full max-w-md bg-white dark:bg-gray-800 rounded-2xl shadow-2xl overflow-hidden">
          <div class="px-5 py-4 border-b border-gray-100 dark:border-gray-700">
            <h3 class="font-bold text-gray-900 dark:text-white">Toplu Teslim Et</h3>
            <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
              <span class="font-semibold text-gray-700 dark:text-gray-200">{{ selectedIds.size }} sevkiyat</span> teslim edildi olarak işaretlenecek.
            </p>
          </div>
          <div class="px-5 py-4 space-y-3">
            <div>
              <label class="block text-xs font-semibold text-gray-700 dark:text-gray-300 mb-1">
                Teslim Alan <span class="text-red-500">*</span>
              </label>
              <input
                v-model="bulkDeliverModal.recipient"
                type="text"
                placeholder="Teslim alan kişi / güvenlik adı"
                class="w-full px-3 py-2 text-sm rounded-xl border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500"
              />
            </div>
            <div>
              <label class="block text-xs font-semibold text-gray-700 dark:text-gray-300 mb-1">
                Açıklama / Not <span class="text-red-500">*</span>
              </label>
              <textarea
                v-model="bulkDeliverModal.note"
                rows="2"
                placeholder="Toplu teslim işlemi için açıklama"
                class="w-full px-3 py-2 text-sm rounded-xl border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-green-500 resize-none"
              ></textarea>
            </div>
            <p v-if="bulkDeliverModal.error" class="text-xs text-red-500">{{ bulkDeliverModal.error }}</p>
          </div>
          <div class="px-5 pb-5 flex justify-end gap-2">
            <button
              @click="bulkDeliverModal.open = false"
              class="px-4 py-2 text-sm text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded-xl transition-colors"
            >İptal</button>
            <button
              @click="confirmBulkMarkDelivered"
              :disabled="bulkDeliverModal.submitting"
              class="px-4 py-2 text-sm font-semibold text-white bg-green-600 hover:bg-green-700 disabled:opacity-50 rounded-xl transition-colors flex items-center gap-2"
            >
              <span v-if="bulkDeliverModal.submitting" class="w-3.5 h-3.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
              {{ bulkDeliverModal.submitting ? 'İşleniyor...' : 'Toplu Teslim Et' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>

  <!-- İptal (sebepli) modalı -->
  <Teleport to="body">
    <div v-if="cancelModal.open" class="fixed inset-0 z-50 flex items-center justify-center p-4" @click.self="cancelModal.open = false">
      <div class="absolute inset-0 bg-black/50"></div>
      <div class="relative w-full max-w-md bg-white dark:bg-gray-900 rounded-2xl shadow-2xl p-5 space-y-4">
        <h3 class="text-lg font-bold text-gray-900 dark:text-gray-100">Sevkiyatı İptal Et</h3>
        <p class="text-sm text-gray-500 dark:text-gray-400">
          Sevkiyat pasife alınacak ve rezerve edilen stok serbest bırakılacak. Lütfen iptal sebebini seçin.
        </p>

        <div class="space-y-2">
          <label
            v-for="opt in cancelReasonOptions"
            :key="opt"
            class="flex items-center gap-3 px-3 py-2.5 rounded-lg border cursor-pointer transition-colors"
            :class="cancelModal.reason === opt
              ? 'border-red-400 bg-red-50 dark:bg-red-900/20 dark:border-red-700'
              : 'border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800'"
          >
            <input type="radio" :value="opt" v-model="cancelModal.reason" class="text-red-600 focus:ring-red-500" />
            <span class="text-sm font-medium text-gray-800 dark:text-gray-200">{{ opt }}</span>
            <span v-if="opt === 'Stokta yok'" class="ml-auto text-[10px] font-bold text-red-600 bg-red-100 dark:bg-red-900/30 px-2 py-0.5 rounded">MÜŞTERİYE MAİL</span>
          </label>
        </div>

        <div v-if="cancelModal.reason === 'Diğer'">
          <input
            v-model="cancelModal.customReason"
            type="text"
            placeholder="İptal sebebini yazın..."
            class="w-full px-3 py-2.5 rounded-lg border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 text-sm focus:outline-none focus:ring-2 focus:ring-red-500"
          />
        </div>

        <p v-if="cancelModal.reason === 'Stokta yok'" class="text-xs text-amber-600 dark:text-amber-400 bg-amber-50 dark:bg-amber-900/20 rounded-lg px-3 py-2">
          Bu seçimde projeye "stokta olmadığı için gönderilememiştir" bildirimi e-postası gönderilecektir.
        </p>

        <div class="flex gap-3 pt-1">
          <button @click="cancelModal.open = false" class="flex-1 py-2.5 border border-gray-300 dark:border-gray-700 text-gray-700 dark:text-gray-300 font-medium rounded-xl hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
            Vazgeç
          </button>
          <button
            @click="confirmCancel"
            :disabled="cancelModal.submitting || !effectiveCancelReason"
            class="flex-1 py-2.5 bg-red-600 hover:bg-red-700 disabled:opacity-50 text-white font-semibold rounded-xl transition-colors flex items-center justify-center gap-2"
          >
            <span v-if="cancelModal.submitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
            {{ cancelModal.submitting ? 'İptal ediliyor...' : 'İptal Et' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>

  <CreateManualShipmentModal
    :is-open="manualShipmentModalOpen"
    @close="manualShipmentModalOpen = false"
    @created="onManualShipmentCreated"
  />

  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted, watch } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import { useRoute, useRouter, onBeforeRouteLeave } from 'vue-router';
import { ClipboardDocumentListIcon } from '@heroicons/vue/24/outline';
import shipmentService, { type ZoneItem } from '../services/shipmentService';
import transportService, { type Driver, type Vehicle } from '../services/transportService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';
import StatusBadge from '../components/StatusBadge.vue';
import SkeletonTable from '../components/SkeletonTable.vue';
import EmptyState from '../components/EmptyState.vue';
import { useKeyboardShortcut } from '../composables/useKeyboardShortcut';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/base/BaseInput.vue';
import BaseSelect from '../components/base/BaseSelect.vue';
import ShipmentActionsDropdown from '../components/ShipmentActionsDropdown.vue';
import CreateManualShipmentModal from '../components/CreateManualShipmentModal.vue';

const notificationStore = useNotificationStore();
const route = useRoute();
const router = useRouter();

function simpleDebounce(fn: Function, delay: number) {
  let timeoutId: any;
  return (...args: any[]) => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => fn(...args), delay);
  };
}

const FILTER_STORAGE_KEY = 'shipment_list_last_query';

// URL'de filtre param yok ama sessionStorage'da kaydedilmiş varsa yükle
function resolveInitialQuery() {
  const q = route.query;
  const hasFilters = q.startDate || q.endDate || q.status || q.statuses || q.zone || q.search || q.dispatchType || q.tab;
  if (hasFilters) return q;
  try {
    const saved = sessionStorage.getItem(FILTER_STORAGE_KEY);
    if (saved) return JSON.parse(saved) as Record<string, string>;
  } catch { /* ignore */ }
  return q;
}

const q = resolveInitialQuery();
const activeTab = ref<'catering' | 'clothing' | 'passive' | 'other'>(
  q.tab === 'passive' ? 'passive'
    : q.tab === 'clothing' ? 'clothing'
    : q.tab === 'other' ? 'other'
    : 'catering'
);

const manualShipmentModalOpen = ref(false);

interface Shipment {
  id: number;
  projectCode: string;
  projectName: string;
  region: string;
  status: string;
  deliveryDate: string;
  driverName?: string;
  plateNumber?: string;
  talepNo?: string;
  externalOrderNumber?: string;
  waybillNumber?: string;
  netsisTransferredAt?: string | null;
  operationType?: string;
  operationTypeValue?: number;
  cargoProviderValue?: number | null;
  freightCarrierName?: string | null;
  freightCarrierPlate?: string | null;
}

const CARGO_PROVIDER_LABELS: Record<number, string> = {
  0: 'MNG Kargo',
  1: 'Yurtiçi Kargo',
  2: 'Aras Kargo',
  3: 'PTT Kargo',
  99: 'Diğer Kargo',
};

function getDispatchInfo(s: Shipment) {
  if (s.cargoProviderValue != null) {
    return { type: 'Kargo', label: CARGO_PROVIDER_LABELS[s.cargoProviderValue] ?? 'Kargo', plate: null };
  }
  if (s.freightCarrierName) {
    return { type: 'Nakliye', label: s.freightCarrierName, plate: s.freightCarrierPlate ?? null };
  }
  if (s.driverName) {
    return { type: 'Araç', label: s.driverName, plate: s.plateNumber ?? null };
  }
  return null;
}

const shipments = ref<Shipment[]>([]);
const loading = ref(false);
const zones = ref<ZoneItem[]>([]);

function defaultStartDate() {
  const d = new Date();
  d.setDate(d.getDate() - 7);
  return d.toISOString().slice(0, 10);
}
function defaultEndDate() {
  const d = new Date();
  d.setDate(d.getDate() + 7);
  return d.toISOString().slice(0, 10);
}

const filters = ref({
  startDate: typeof q.startDate === 'string' ? q.startDate : defaultStartDate(),
  endDate:   typeof q.endDate   === 'string' ? q.endDate   : defaultEndDate(),
  status: typeof q.status === 'string' ? q.status : '',
  statuses: typeof q.statuses === 'string' ? q.statuses : '', // multi-status (dashboard kartlarından)
  zoneId: typeof q.zone === 'string' ? q.zone : '',
  dispatchType: typeof q.dispatchType === 'string' ? q.dispatchType : '',
});

// ── Selection state ───────────────────────────────────────────────────────────
const selectedIds = reactive(new Set<number>());

function isSelectable(s: Shipment) {
  return s.status === 'ReadyForDispatch' ||
    (s.status === 'AssignedToVehicle' && !!s.waybillNumber) ||
    (s.status === 'Dispatched' && !!s.waybillNumber);
}

const pageSelectableIds = computed(() =>
  shipments.value.filter(isSelectable).map(s => s.id)
);

// Keep backward-compatible alias used by header checkbox
const pageReadyIds = pageSelectableIds;

const isAllReadySelected = computed(() =>
  pageSelectableIds.value.length > 0 && pageSelectableIds.value.every(id => selectedIds.has(id))
);

function toggleSelect(id: number) {
  if (selectedIds.has(id)) selectedIds.delete(id);
  else selectedIds.add(id);
}

function toggleSelectAll() {
  if (isAllReadySelected.value) {
    pageSelectableIds.value.forEach(id => selectedIds.delete(id));
  } else {
    pageSelectableIds.value.forEach(id => selectedIds.add(id));
  }
}

// ── Bulk dispatch modal ───────────────────────────────────────────────────────
type BulkDispatchType = 'cargo' | 'freight' | 'vehicle';

const showDispatchModal = ref(false);
const bulkSubmitting = ref(false);
const bulkListsLoading = ref(false);
const bulkDispatchType = ref<BulkDispatchType>('cargo');

// Cargo
const bulkCargoProvider = ref<number | null>(null);

// Freight
const bulkFreightCarrierName = ref('');
const bulkFreightCarrierPlate = ref('');
const bulkFreightCarrierPhone = ref('');

// Vehicle
const bulkSelectedDriverIds = ref<number[]>([]);
const bulkVehicleId = ref<number | null>(null);
const bulkDepartureTime = ref('08:00');

const bulkActiveDrivers = ref<Driver[]>([]);
const bulkActiveVehicles = ref<Vehicle[]>([]);

const dispatchTypeOptions = [
  { value: 'cargo' as BulkDispatchType,   label: 'Kargo',   icon: '📦' },
  { value: 'freight' as BulkDispatchType, label: 'Nakliye', icon: '🚛' },
  { value: 'vehicle' as BulkDispatchType, label: 'Araç',    icon: '🚗' },
];

const cargoProviderOptions = [
  { value: 0,  label: 'MNG Kargo' },
  { value: 1,  label: 'Yurtiçi Kargo' },
  { value: 2,  label: 'Aras Kargo' },
  { value: 3,  label: 'PTT Kargo' },
  { value: 99, label: 'Diğer Kargo' },
];

const canSubmitBulkDispatch = computed(() => {
  if (bulkDispatchType.value === 'cargo')   return bulkCargoProvider.value !== null;
  if (bulkDispatchType.value === 'freight') return bulkFreightCarrierName.value.trim().length > 0;
  if (bulkDispatchType.value === 'vehicle') return bulkSelectedDriverIds.value.length > 0 && bulkVehicleId.value !== null;
  return false;
});

async function openBulkModal() {
  bulkDispatchType.value = 'cargo';
  bulkCargoProvider.value = null;
  bulkFreightCarrierName.value = '';
  bulkFreightCarrierPlate.value = '';
  bulkFreightCarrierPhone.value = '';
  bulkSelectedDriverIds.value = [];
  bulkVehicleId.value = null;
  bulkDepartureTime.value = '08:00';
  showDispatchModal.value = true;

  if (bulkActiveDrivers.value.length === 0 || bulkActiveVehicles.value.length === 0) {
    bulkListsLoading.value = true;
    try {
      const [dList, vList] = await Promise.all([
        transportService.getActiveDrivers(),
        transportService.getActiveVehicles(),
      ]);
      bulkActiveDrivers.value = dList;
      bulkActiveVehicles.value = vList;
    } catch {
      notificationStore.add('Şoför/araç listesi yüklenemedi.', 'error');
    } finally {
      bulkListsLoading.value = false;
    }
  }
}

async function submitBulkDispatch() {
  if (!canSubmitBulkDispatch.value) return;
  bulkSubmitting.value = true;
  try {
    const ids = [...selectedIds];

    if (bulkDispatchType.value === 'cargo') {
      const result = await shipmentService.bulkDispatchAsCargo(ids, bulkCargoProvider.value!);
      if (result.successCount > 0)
        notificationStore.add(`${result.successCount} sevkiyat kargo olarak gönderildi.`, 'success');
      result.errors.forEach(e => notificationStore.add(e, 'warning'));

    } else if (bulkDispatchType.value === 'freight') {
      const result = await shipmentService.bulkDispatchAsFreight(
        ids,
        bulkFreightCarrierName.value.trim(),
        bulkFreightCarrierPlate.value.trim() || undefined,
        bulkFreightCarrierPhone.value.trim() || undefined,
      );
      if (result.successCount > 0)
        notificationStore.add(`${result.successCount} sevkiyat nakliye olarak gönderildi.`, 'success');
      result.errors.forEach(e => notificationStore.add(e, 'warning'));

    } else if (bulkDispatchType.value === 'vehicle') {
      const result = await shipmentService.bulkAssignVehicle({
        shipmentIds: ids,
        driverId: bulkSelectedDriverIds.value[0]!,
        vehicleId: bulkVehicleId.value!,
      });
      if (result.successCount > 0)
        notificationStore.add(`${result.successCount} sevkiyat araca atandı.`, 'success');
      result.errors.forEach(e => notificationStore.add(e, 'warning'));
    }

    showDispatchModal.value = false;
    selectedIds.clear();
    fetchShipments();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  } finally {
    bulkSubmitting.value = false;
  }
}
// ── Netsis export ─────────────────────────────────────────────────────────────
const isBulkExporting = ref(false);
const exportingIds = ref<Set<number>>(new Set());

// ── Netsis durum kontrol ───────────────────────────────────────────────────────
const recoveringNetsis = ref(false);
async function recoverNetsisTransfers() {
  if (!confirm('Araçta/yolda/iade durumundaki ve irsaliyesi silinmiş sevkiyatlar Netsis\'te aranacak. Bulunanlar Sevke Hazır durumuna geri alınacak.\n\nDevam edilsin mi?')) return;
  recoveringNetsis.value = true;
  try {
    const result = await shipmentService.recoverNetsisTransfers();
    if (result.recovered > 0) {
      notificationStore.add(
        `Netsis kurtarma tamamlandı: ${result.recovered} sevkiyat Sevke Hazır durumuna alındı. Araç atamalarını yenileyebilirsiniz.` +
        (result.notFound > 0 ? ` (${result.notFound} adedi Netsis'te bulunamadı)` : ''),
        'success'
      );
      await fetchShipments();
    } else {
      notificationStore.add(
        result.checked === 0
          ? 'Kurtarılacak sevkiyat bulunamadı.'
          : `Kontrol edildi (${result.checked} sevkiyat), hiçbiri Netsis'te bulunamadı.`,
        'info'
      );
    }
    if (result.error) {
      notificationStore.add(`Netsis hatası: ${result.error}`, 'warning');
    }
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Netsis kurtarma başlatılamadı.', 'error');
  } finally {
    recoveringNetsis.value = false;
  }
}

const verifyingNetsis = ref(false);
async function verifyNetsisTransfers() {
  verifyingNetsis.value = true;
  try {
    await shipmentService.verifyNetsisTransfers({
      startDate: filters.value.startDate,
      endDate: filters.value.endDate,
      zoneId: filters.value.zoneId ? Number(filters.value.zoneId) : undefined,
      status: filters.value.status ? Number(filters.value.status) : undefined,
      statuses: filters.value.statuses || undefined,
      search: searchQuery.value || undefined,
    });
    notificationStore.add(
      'Netsis durum kontrolü arka planda başlatıldı. Teslim edilmiş irsaliyeler dahil silinmiş aktarımlar tespit edilip sıfırlanacak; tamamlandığında listeyi yenileyebilirsiniz.',
      'info'
    );
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Netsis durum kontrolü başlatılamadı.', 'error');
  } finally {
    verifyingNetsis.value = false;
  }
}

const deliverModal = reactive({
  open: false,
  shipmentId: 0,
  recipient: '',
  note: '',
  error: '',
  submitting: false,
});

function openMarkDeliveredModal(id: number) {
  deliverModal.shipmentId = id;
  deliverModal.recipient = '';
  deliverModal.note = '';
  deliverModal.error = '';
  deliverModal.submitting = false;
  deliverModal.open = true;
}

async function confirmMarkDelivered() {
  if (!deliverModal.recipient.trim()) {
    deliverModal.error = 'Teslim alan bilgisi zorunludur.';
    return;
  }
  if (!deliverModal.note.trim()) {
    deliverModal.error = 'Açıklama / not zorunludur.';
    return;
  }
  deliverModal.error = '';
  deliverModal.submitting = true;
  try {
    await shipmentService.markDelivered(
      deliverModal.shipmentId,
      deliverModal.note,
      deliverModal.recipient,
      undefined,
      deliverModal.note,
    );
    notificationStore.add('Sevkiyat teslim edildi olarak işaretlendi.', 'success');
    deliverModal.open = false;
    await fetchShipments();
  } catch (error) {
    deliverModal.error = ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.';
  } finally {
    deliverModal.submitting = false;
  }
}

// ── Bulk Mark Delivered ────────────────────────────────────────────────────────
const bulkDeliverModal = reactive({
  open: false,
  recipient: '',
  note: '',
  error: '',
  submitting: false,
});

function openBulkDeliverModal() {
  bulkDeliverModal.recipient = '';
  bulkDeliverModal.note = '';
  bulkDeliverModal.error = '';
  bulkDeliverModal.submitting = false;
  bulkDeliverModal.open = true;
}

async function confirmBulkMarkDelivered() {
  if (!bulkDeliverModal.recipient.trim()) {
    bulkDeliverModal.error = 'Teslim alan bilgisi zorunludur.';
    return;
  }
  if (!bulkDeliverModal.note.trim()) {
    bulkDeliverModal.error = 'Açıklama / not zorunludur.';
    return;
  }
  bulkDeliverModal.error = '';
  bulkDeliverModal.submitting = true;
  try {
    const result = await shipmentService.bulkMarkDelivered(
      [...selectedIds],
      bulkDeliverModal.recipient.trim(),
      bulkDeliverModal.note.trim(),
    );
    if (result.successCount > 0)
      notificationStore.add(`${result.successCount} sevkiyat teslim edildi olarak işaretlendi.`, 'success');
    result.errors.forEach(e => notificationStore.add(e, 'warning'));
    bulkDeliverModal.open = false;
    selectedIds.clear();
    await fetchShipments();
  } catch (error) {
    bulkDeliverModal.error = ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.';
  } finally {
    bulkDeliverModal.submitting = false;
  }
}

async function singleExportToNetsis(id: number) {
  if (exportingIds.value.has(id)) return;
  const shipment = shipments.value.find(s => s.id === id);
  if (shipment?.netsisTransferredAt) {
    notificationStore.add(`Sevkiyat #${id} zaten Netsis'e aktarılmış (${new Date(shipment.netsisTransferredAt).toLocaleString('tr-TR')}).`, 'warning');
    return;
  }
  exportingIds.value = new Set(exportingIds.value).add(id);
  try {
    const result = await shipmentService.exportToNetsis(id);
    notificationStore.add(result.message || `Sevkiyat #${id} Netsis'e aktarıldı.`, 'success');
    await fetchShipments();
  } catch (error) {
    const msg = ApiErrorUtils.getErrorMessage(error) || 'Netsis aktarımı başarısız.';
    if (msg.includes('Netsis stok kodu') || msg.includes('Netsis Stok Kodu')) {
      notificationStore.add(`Sevkiyat #${id}: ${msg}`, 'error');
    } else {
      notificationStore.add(msg, 'error');
    }
  } finally {
    const next = new Set(exportingIds.value);
    next.delete(id);
    exportingIds.value = next;
  }
}

async function bulkExportToNetsis() {
  if (selectedIds.size === 0) return;

  // Zaten aktarılmış olanları önceden uyar
  const alreadyDone = [...selectedIds]
    .map(id => shipments.value.find(s => s.id === id))
    .filter(s => s?.netsisTransferredAt);
  if (alreadyDone.length === selectedIds.size) {
    notificationStore.add('Seçili sevkiyatların tamamı zaten Netsis\'e aktarılmış.', 'warning');
    return;
  }
  if (alreadyDone.length > 0) {
    notificationStore.add(`${alreadyDone.length} sevkiyat zaten aktarılmış — bunlar atlanacak.`, 'warning');
  }

  isBulkExporting.value = true;
  try {
    const result = await shipmentService.bulkExportToNetsis([...selectedIds]);
    if (result.exported > 0)
      notificationStore.add(`${result.exported} sevkiyat Netsis'e aktarıldı.`, 'success');
    if (result.skipped > 0 && result.exported === 0)
      notificationStore.add(`${result.skipped} sevkiyat atlandı (zaten aktarılmış veya eksik bilgi).`, 'warning');
    result.errors.forEach(e => notificationStore.add(e, 'error'));
    selectedIds.clear();
    await fetchShipments();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'Toplu Netsis aktarımı başarısız.', 'error');
  } finally {
    isBulkExporting.value = false;
  }
}

const searchQuery = ref(typeof q.search === 'string' ? q.search : '');

type SortKey = 'deliveryDate' | 'status' | 'id';
type SortDir = 'asc' | 'desc';
const sortKey = ref<SortKey>(
  (q.sortKey === 'status' || q.sortKey === 'id') ? q.sortKey as SortKey : 'deliveryDate'
);
const sortDir = ref<SortDir>(q.sortDir === 'asc' ? 'asc' : 'desc');

const sortedShipments = computed(() => {
  const list = [...shipments.value];
  list.sort((a, b) => {
    let va: any = a[sortKey.value];
    let vb: any = b[sortKey.value];
    if (sortKey.value === 'deliveryDate') {
      va = new Date(va).getTime();
      vb = new Date(vb).getTime();
    }
    if (va < vb) return sortDir.value === 'asc' ? -1 : 1;
    if (va > vb) return sortDir.value === 'asc' ? 1 : -1;
    return 0;
  });
  return list;
});

const toggleSort = (key: SortKey) => {
  if (sortKey.value === key) {
    sortDir.value = sortDir.value === 'asc' ? 'desc' : 'asc';
  } else {
    sortKey.value = key;
    sortDir.value = 'asc';
  }
};

const page = ref(typeof q.page === 'string' && Number(q.page) > 0 ? Number(q.page) : 1);
const pageSize = ref(20);
const totalCount = ref(0);
const totalPages = ref(1);

// Keyboard shortcuts: ← önceki sayfa, → sonraki sayfa
useKeyboardShortcut('ArrowLeft', () => {
  if (page.value > 1) { page.value--; fetchShipments(); }
});
useKeyboardShortcut('ArrowRight', () => {
  if (page.value < totalPages.value) { page.value++; fetchShipments(); }
});

const today = new Date();
today.setHours(0, 0, 0, 0);

const ACTIVE_STATUSES = new Set(['Created', 'AssignedToWarehouse', 'Picking', 'ReadyForDispatch', 'AssignedToVehicle']);

const isOverdue = (s: Shipment) => {
  const d = new Date(s.deliveryDate);
  d.setHours(0, 0, 0, 0);
  return d < today && ACTIVE_STATUSES.has(s.status);
};

const isToday = (s: Shipment) => {
  const d = new Date(s.deliveryDate);
  d.setHours(0, 0, 0, 0);
  return d.getTime() === today.getTime() && ACTIVE_STATUSES.has(s.status);
};

const rowClass = (s: Shipment) => {
  if (isOverdue(s)) return 'bg-red-50/40';
  return '';
};

const formatDate = (d: string) =>
  new Date(d).toLocaleDateString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric' });

const fetchShipments = async () => {
  loading.value = true;
  const params: any = {
    PageNumber: page.value,
    PageSize: pageSize.value,
    Search: searchQuery.value,
  };

  if (filters.value.startDate) params.startDate = filters.value.startDate;
  if (filters.value.endDate)   params.endDate   = filters.value.endDate + 'T23:59:59';

  if (activeTab.value === 'passive') {
    params.status = 10;
  } else {
    if (activeTab.value === 'catering') {
      params.OperationType = 0;
      params.Source = 0; // ISS
    } else if (activeTab.value === 'clothing') {
      params.OperationType = 1;
      params.Source = 0; // ISS
    } else if (activeTab.value === 'other') {
      params.Source = 1; // Manuel
    }

    if (filters.value.status) {
      params.status = filters.value.status;
      filters.value.statuses = '';
    } else if (filters.value.statuses) {
      params.statuses = filters.value.statuses;
    }
  }

  if (filters.value.zoneId) params.ZoneId = filters.value.zoneId;
  if (filters.value.dispatchType) params.DispatchType = filters.value.dispatchType;

  try {
    const data = await shipmentService.getAll(params);
    shipments.value = data.items as unknown as Shipment[];
    totalCount.value = data.totalCount;
    totalPages.value = data.totalPages;
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'Sevkiyatlar yüklenemedi.', 'error');
  } finally {
    loading.value = false;
  }
};

const handleSearch = simpleDebounce(() => {
  page.value = 1;
  syncUrl();
  fetchShipments();
}, 500);

const onManualShipmentCreated = () => {
  manualShipmentModalOpen.value = false;
  page.value = 1;
  fetchShipments();
};

const quickAssignToWarehouse = async (id: number) => {
  try {
    await shipmentService.assignToWarehouse(id);
    fetchShipments();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

const quickStartPicking = async (id: number) => {
  try {
    await shipmentService.startPicking(id);
    fetchShipments();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

const quickMarkReady = async (id: number) => {
  try {
    await shipmentService.markReady(id);
    fetchShipments();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

const deleteDraft = async (id: number) => {
  const confirmed = await notificationStore.promptConfirm({
    title: 'Taslak Sevkiyatı Sil',
    message: 'Bu taslak sevkiyat silinecek ve ilgili ISS siparişi tekrar aktarım listesine dönecek. Emin misiniz?',
    confirmText: 'Sil',
    type: 'warning',
  });
  if (!confirmed) return;
  try {
    await shipmentService.deleteDraft(id);
    fetchShipments();
    notificationStore.add('Taslak sevkiyat silindi. ISS siparişi aktarım listesine geri döndü.', 'success');
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'Silme işlemi başarısız.', 'error');
  }
};

// İptal (sebepli) modalı
const cancelReasonOptions = ['Stokta yok', 'Müşteri iptal etti', 'Diğer'];
const cancelModal = reactive({
  open: false,
  shipmentId: 0,
  reason: 'Stokta yok',
  customReason: '',
  submitting: false,
});

const effectiveCancelReason = computed(() =>
  cancelModal.reason === 'Diğer' ? cancelModal.customReason.trim() : cancelModal.reason
);

const openCancelModal = (id: number) => {
  cancelModal.shipmentId = id;
  cancelModal.reason = 'Stokta yok';
  cancelModal.customReason = '';
  cancelModal.submitting = false;
  cancelModal.open = true;
};

const confirmCancel = async () => {
  const reason = effectiveCancelReason.value;
  if (!reason) return;
  const notifyOutOfStock = cancelModal.reason === 'Stokta yok';
  cancelModal.submitting = true;
  try {
    const res = await shipmentService.cancelShipment(cancelModal.shipmentId, reason, notifyOutOfStock);
    cancelModal.open = false;
    if (notifyOutOfStock && res.emailSent) {
      notificationStore.add('Sevkiyat iptal edildi ve projeye bilgilendirme e-postası gönderildi.', 'success');
    } else if (notifyOutOfStock && !res.emailSent) {
      notificationStore.add(`Sevkiyat iptal edildi ancak e-posta gönderilemedi: ${res.emailError || 'bilinmeyen hata'}`, 'warning');
    } else {
      notificationStore.add('Sevkiyat iptal edildi.', 'success');
    }
    fetchShipments();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İptal işlemi başarısız.', 'error');
  } finally {
    cancelModal.submitting = false;
  }
};

const confirmToggleStatus = async (id: number, setPassive: boolean) => {
  let reason: string | undefined = undefined;

  if (setPassive) {
    const confirmed = await notificationStore.promptConfirm({
      title: 'Pasife Al',
      message: 'Bu sipariş pasife alınacak ve operasyon ekranlarından gizlenecek. Emin misiniz?',
      confirmText: 'Pasife Al',
      type: 'warning',
    });
    if (!confirmed) return;
  } else {
    // Aktife alma — sebep zorunlu
    const input = window.prompt('Bu sevkiyatı neden aktife almak istiyorsunuz? (Sebep zorunludur)');
    if (input === null) return; // İptal basıldı
    if (!input.trim()) {
      notificationStore.add('Aktife alma işlemi için sebep girmeniz zorunludur.', 'warning');
      return;
    }
    reason = input.trim();
  }

  try {
    await shipmentService.toggleStatus(id, setPassive, reason);
    fetchShipments();
  } catch (error) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(error) || 'İşlem başarısız.', 'error');
  }
};

const syncUrl = () => {
  const query: Record<string, string> = {};
  if (activeTab.value !== 'catering') query.tab = activeTab.value;
  if (filters.value.startDate && filters.value.startDate !== defaultStartDate()) query.startDate = filters.value.startDate;
  if (filters.value.endDate && filters.value.endDate !== defaultEndDate()) query.endDate = filters.value.endDate;
  if (filters.value.status && filters.value.status !== '0') query.status = filters.value.status;
  if (filters.value.statuses) query.statuses = filters.value.statuses;
  if (filters.value.zoneId) query.zone = filters.value.zoneId;
  if (filters.value.dispatchType) query.dispatchType = filters.value.dispatchType;
  if (searchQuery.value) query.search = searchQuery.value;
  if (page.value !== 1) query.page = String(page.value);
  if (sortKey.value !== 'deliveryDate') query.sortKey = sortKey.value;
  if (sortDir.value !== 'asc') query.sortDir = sortDir.value;
  router.replace({ query });
};

watch(activeTab, () => {
  page.value = 1;
  syncUrl();
  fetchShipments();
});

watch([
  () => filters.value.startDate,
  () => filters.value.endDate,
  () => filters.value.status,
  () => filters.value.statuses,
  () => filters.value.zoneId,
  () => filters.value.dispatchType,
], () => {
  page.value = 1;
  selectedIds.clear();
  syncUrl();
  fetchShipments();
});

watch(page, () => {
  syncUrl();
});

watch([sortKey, sortDir], () => {
  syncUrl();
});

async function fetchZones() {
  try {
    zones.value = await shipmentService.getZones();
  } catch {
    // Bölge listesi yüklenemezse filtre boş kalır, kritik değil
  }
}

onMounted(() => {
  // Eğer sessionStorage'dan filtreler yüklendiyse URL'yi güncelle
  const hasStoredFilters =
    filters.value.startDate !== defaultStartDate() ||
    filters.value.endDate !== defaultEndDate() ||
    filters.value.status ||
    filters.value.statuses ||
    filters.value.zoneId ||
    filters.value.dispatchType ||
    searchQuery.value;
  if (hasStoredFilters && !Object.keys(route.query).length) {
    syncUrl();
  }
  fetchZones();
  fetchShipments();
});

onBeforeRouteLeave(() => {
  try {
    const snap: Record<string, string> = {};
    if (activeTab.value !== 'catering') snap.tab = activeTab.value;
    if (filters.value.startDate) snap.startDate = filters.value.startDate;
    if (filters.value.endDate) snap.endDate = filters.value.endDate;
    if (filters.value.status) snap.status = filters.value.status;
    if (filters.value.statuses) snap.statuses = filters.value.statuses;
    if (filters.value.zoneId) snap.zone = filters.value.zoneId;
    if (filters.value.dispatchType) snap.dispatchType = filters.value.dispatchType;
    if (searchQuery.value) snap.search = searchQuery.value;
    sessionStorage.setItem(FILTER_STORAGE_KEY, JSON.stringify(snap));
  } catch { /* ignore */ }
});
</script>
