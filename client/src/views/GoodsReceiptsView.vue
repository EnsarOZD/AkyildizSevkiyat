<template>
  <div class="space-y-4">

    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
      <div>
        <h1 class="text-xl font-bold text-gray-900 dark:text-gray-100">Mal Kabul İrsaliyeleri</h1>
      </div>
      <button
        @click="$router.push('/goods-receipts/select-po')"
        class="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg text-sm font-semibold shadow-sm transition-colors w-full sm:w-auto justify-center"
      >
        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
        </svg>
        Yeni İrsaliye
      </button>
    </div>

    <!-- Filters -->
    <div class="bg-white dark:bg-gray-900 p-4 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700">
      <div class="flex flex-col sm:flex-row gap-3">
        <div class="w-full sm:w-44">
          <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">Durum</label>
          <select
            v-model="filters.status"
            @change="fetchReceipts"
            class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
          >
            <option value="">Tümü</option>
            <option value="0">Taslak</option>
            <option value="1">Postalandı</option>
            <option value="2">İptal</option>
          </select>
        </div>
        <div class="flex-1">
          <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-1">Tedarikçi Ara</label>
          <input
            type="text"
            v-model="filters.supplierName"
            @input="handleSearch"
            placeholder="Tedarikçi adı..."
            class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-600 focus:ring-2 focus:ring-blue-500 focus:border-transparent outline-none"
          />
        </div>
        <div class="flex items-end">
          <button
            @click="fetchReceipts"
            class="w-full sm:w-auto px-4 py-2 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 rounded-lg text-sm hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors border border-gray-200 dark:border-gray-700"
          >
            Yenile
          </button>
        </div>
      </div>
    </div>

    <!-- Error state -->
    <div v-if="error" class="rounded-lg bg-red-50 border border-red-200 p-6 text-center">
      <p class="text-red-700 font-medium">{{ error }}</p>
      <button @click="fetchReceipts" class="mt-3 text-sm text-red-600 underline hover:text-red-800">
        Tekrar dene
      </button>
    </div>

    <!-- Desktop Table -->
    <div class="hidden md:block bg-white dark:bg-gray-900 shadow-sm rounded-xl overflow-x-auto border border-gray-200 dark:border-gray-700">
      <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
        <thead class="bg-gray-50 dark:bg-gray-800">
          <tr>
            <th class="px-5 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">İrsaliye No</th>
            <th class="px-5 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Sipariş No</th>
            <th class="px-5 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tedarikçi</th>
            <th class="px-5 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Tarih</th>
            <th class="px-5 py-3 text-left text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Durum</th>
            <th class="px-5 py-3 text-center text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider hidden lg:table-cell">Kalem</th>
            <th class="px-5 py-3"></th>
          </tr>
        </thead>
        <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
          <tr v-if="loading">
            <td colspan="7"><SkeletonTable :rows="5" :columns="7" class="shadow-none rounded-none border-none" /></td>
          </tr>
          <tr v-else-if="!receipts || receipts.length === 0">
            <td colspan="7">
              <EmptyState :icon="InboxArrowDownIcon" title="Mal kabul irsaliyesi bulunamadı" />
            </td>
          </tr>
          <tr
            v-for="receipt in receipts"
            :key="receipt.id"
            class="hover:bg-gray-50 dark:hover:bg-gray-800 cursor-pointer transition-colors"
            @click="openDetail(receipt.id)"
          >
            <td class="px-5 py-3 text-sm font-semibold text-purple-600 dark:text-purple-400 whitespace-nowrap">{{ receipt.waybillNo }}</td>
            <td class="px-5 py-3 text-sm text-gray-500 dark:text-gray-400 whitespace-nowrap">
              <span v-if="receipt.purchaseOrderNumber" class="font-semibold">{{ receipt.purchaseOrderNumber }}</span>
              <span v-else class="text-gray-300 dark:text-gray-600">—</span>
            </td>
            <td class="px-5 py-3 text-sm font-medium text-gray-900 dark:text-gray-100">{{ receipt.supplierNameSnapshot }}</td>
            <td class="px-5 py-3 text-sm text-gray-500 dark:text-gray-400 whitespace-nowrap">{{ formatDate(receipt.waybillDate) }}</td>
            <td class="px-5 py-3 whitespace-nowrap"><StatusBadge :status="receipt.status" type="goodsReceipt" /></td>
            <td class="px-5 py-3 text-sm text-gray-500 dark:text-gray-400 text-center hidden lg:table-cell">{{ receipt.lineCount }}</td>
            <td class="px-5 py-3 text-right">
              <svg class="h-4 w-4 text-gray-400 inline" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
              </svg>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Mobile Cards (list) -->
    <div class="md:hidden space-y-2">
      <div v-if="loading"><SkeletonTable :rows="3" :columns="1" /></div>
      <EmptyState v-else-if="receipts.length === 0" :icon="InboxArrowDownIcon" title="Mal kabul irsaliyesi bulunamadı" />
      <div
        v-for="receipt in receipts"
        :key="receipt.id"
        class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 cursor-pointer active:bg-gray-50 dark:active:bg-gray-800 transition-colors"
        @click="openDetail(receipt.id)"
      >
        <div class="flex items-start justify-between gap-2 mb-2">
          <div class="min-w-0">
            <p class="font-bold text-purple-600 dark:text-purple-400 text-sm">{{ receipt.waybillNo }}</p>
            <p class="font-semibold text-gray-900 dark:text-gray-100 text-sm truncate">{{ receipt.supplierNameSnapshot }}</p>
          </div>
          <StatusBadge :status="receipt.status" type="goodsReceipt" />
        </div>
        <div class="flex items-center justify-between text-xs text-gray-500 dark:text-gray-400 mt-2 pt-2 border-t border-gray-100 dark:border-gray-800">
          <span>{{ formatDate(receipt.waybillDate) }}</span>
          <span v-if="receipt.purchaseOrderNumber" class="font-semibold text-indigo-600 dark:text-indigo-400">{{ receipt.purchaseOrderNumber }}</span>
          <span v-else class="text-gray-300 dark:text-gray-600">—</span>
          <span class="bg-gray-100 dark:bg-gray-800 px-2 py-0.5 rounded font-semibold">{{ receipt.lineCount }} kalem</span>
        </div>
      </div>
    </div>

    <!-- Create Modal (Shared Component) -->
    <CreateGoodsReceiptModal
      v-if="showCreateModal"
      :isOpen="showCreateModal"
      :purchaseOrder="selectedPoForCreate"
      @close="showCreateModal = false"
      @saved="onGRSaved"
    />

    <!-- Detail Modal -->
    <div v-if="showDetailModal && selectedReceipt" class="fixed inset-0 bg-black/50 flex items-start justify-center p-0 sm:p-4 z-50 overflow-y-auto">
      <div class="bg-white dark:bg-gray-900 w-full sm:rounded-xl sm:max-w-3xl relative min-h-screen sm:min-h-0 sm:my-8">

        <!-- Sticky header -->
        <div class="sticky top-0 z-10 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-700 px-4 sm:px-6 py-4">
          <div class="flex items-start gap-3 mb-1">
            <button @click="showDetailModal = false" class="mt-0.5 p-1.5 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-500 dark:text-gray-400 flex-shrink-0 transition-colors">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
            <div class="min-w-0 flex-1">
              <h2 class="text-base font-bold text-gray-900 dark:text-gray-100 truncate">{{ selectedReceipt.supplierNameSnapshot }}</h2>
              <div class="flex flex-wrap items-center gap-x-3 gap-y-1 mt-0.5">
                <span class="text-xs text-gray-500 dark:text-gray-400">{{ selectedReceipt.waybillNo }}</span>
                <span class="text-xs text-gray-500 dark:text-gray-400">{{ formatDate(selectedReceipt.waybillDate) }}</span>
                <StatusBadge :status="selectedReceipt.status" type="goodsReceipt" />
              </div>
            </div>
          </div>

          <!-- Unsaved warning -->
          <div v-if="hasUnsavedChanges && selectedReceipt.isEditable" class="mt-2 bg-amber-50 dark:bg-amber-900/20 text-amber-800 dark:text-amber-300 text-xs font-bold px-3 py-2 rounded-lg border border-amber-200 dark:border-amber-800 flex items-center gap-2">
            <span class="animate-bounce">⚠️</span> Kaydedilmemiş değişiklikler var!
          </div>

          <!-- Actions -->
          <div v-if="selectedReceipt.isEditable" class="flex flex-wrap gap-2 mt-3">
            <button
              @click="postReceipt"
              :disabled="posting || hasUnsavedChanges"
              :title="hasUnsavedChanges ? 'Önce değişiklikleri kaydedin' : ''"
              class="flex-1 sm:flex-none px-4 py-2 bg-green-600 hover:bg-green-700 text-white rounded-lg text-sm font-semibold transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <span v-if="posting">İşleniyor...</span>
              <span v-else>Postala (Kesinleştir)</span>
            </button>
            <button @click="cancelReceipt" :disabled="cancelling" class="flex-1 sm:flex-none px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-lg text-sm font-semibold transition-colors disabled:opacity-50">
              <span v-if="cancelling">İşleniyor...</span>
              <span v-else>İptal Et</span>
            </button>
          </div>
        </div>

        <!-- Lines -->
        <div class="px-4 sm:px-6 py-4">
          <h3 class="text-xs font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider mb-3">İrsaliye Kalemleri</h3>

          <!-- Desktop table -->
          <div class="hidden sm:block overflow-x-auto rounded-xl border border-gray-200 dark:border-gray-700">
            <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
              <thead class="bg-gray-50 dark:bg-gray-800">
                <tr>
                  <th class="px-4 py-2.5 text-left text-xs font-semibold text-gray-500 dark:text-gray-400">Stok</th>
                  <th class="px-4 py-2.5 text-right text-xs font-semibold text-gray-500 dark:text-gray-400">Sipariş</th>
                  <th class="px-4 py-2.5 text-right text-xs font-semibold text-gray-500 dark:text-gray-400">Gelen</th>
                  <th class="px-4 py-2.5 text-right text-xs font-semibold text-gray-500 dark:text-gray-400">Kabul</th>
                  <th class="px-4 py-2.5 text-right text-xs font-semibold text-gray-500 dark:text-gray-400">İşlem</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-200 dark:divide-gray-700">
                <tr v-for="line in selectedReceipt.lines" :key="line.id" class="hover:bg-gray-50 dark:hover:bg-gray-800">
                  <td class="px-4 py-3">
                    <p class="text-sm font-medium text-gray-900 dark:text-gray-100">{{ line.stockNameSnapshot }}</p>
                    <p class="text-[10px] text-gray-400 dark:text-gray-600 font-mono">{{ line.unitSnapshot }}</p>
                  </td>
                  <td class="px-4 py-3 text-right text-sm text-gray-500 dark:text-gray-400 font-medium">{{ line.orderedQty }}</td>
                  <td class="px-4 py-3">
                    <div class="flex flex-col items-end gap-1">
                      <input v-if="selectedReceipt.isEditable"
                             type="number" step="0.01"
                             v-model.number="line.receivedQty"
                             @input="markLineDirty(line)"
                             class="w-20 border rounded-lg px-2 py-1 text-sm text-right font-bold focus:ring-1 focus:ring-blue-500 dark:bg-gray-800 dark:text-gray-100"
                             :class="[line.isDirty ? 'border-amber-500 bg-amber-50 dark:bg-amber-900/20' : 'border-gray-300 dark:border-gray-700', calculateAccepted(line) > line.orderedQty ? 'text-red-600 border-red-300' : '']" />
                      <span v-else class="text-sm font-bold" :class="calculateAccepted(line) > line.orderedQty ? 'text-red-600' : ''">{{ line.receivedQty }}</span>
                      <div v-if="calculateAccepted(line) > line.orderedQty" class="text-[9px] text-red-500 font-bold uppercase">FAZLA GİRİŞ!</div>
                      <button v-if="selectedReceipt.isEditable" @click="toggleReject(line)"
                              class="text-[10px] px-1.5 py-0.5 rounded border font-bold uppercase transition-colors"
                              :class="line.rejectedQty > 0 || line.showReject ? 'text-red-600 bg-red-50 dark:bg-red-900/20 border-red-200 dark:border-red-800' : 'text-gray-400 dark:text-gray-600 bg-gray-50 dark:bg-gray-800 border-gray-200 dark:border-gray-700'">
                        {{ (line.rejectedQty > 0 || line.showReject) ? 'Sorun Var' : 'Sorun Yok' }}
                      </button>
                    </div>
                    <div v-if="selectedReceipt.isEditable && (line.showReject || line.rejectedQty > 0)" class="mt-2 p-2 bg-red-50 dark:bg-red-900/10 rounded-lg border border-red-100 dark:border-red-800 flex flex-col gap-2">
                      <div class="flex items-center justify-between gap-1">
                        <label class="text-[10px] font-bold text-red-700 dark:text-red-400 uppercase">Red Miktarı:</label>
                        <input type="number" step="0.01" v-model.number="line.rejectedQty" @input="markLineDirty(line)" class="w-16 border border-red-200 dark:border-red-800 rounded px-1 py-0.5 text-xs text-right text-red-700 dark:text-red-300 dark:bg-gray-800" />
                      </div>
                      <div>
                        <label class="text-[10px] font-bold text-red-700 dark:text-red-400 uppercase flex justify-between">
                          <span>Neden:</span>
                          <span v-if="line.rejectedQty > 0 && !line.rejectReason" class="text-red-600 animate-bounce">! SEÇİNİZ !</span>
                        </label>
                        <select v-model="line.rejectReason" @change="markLineDirty(line)" class="mt-1 text-xs border rounded-lg p-1 w-full dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100" :class="line.rejectedQty > 0 && !line.rejectReason ? 'bg-red-100 border-red-500' : 'border-red-200'">
                          <option value="">Seçiniz...</option>
                          <option v-for="reason in rejectReasons" :key="reason" :value="reason">{{ reason }}</option>
                        </select>
                      </div>
                    </div>
                  </td>
                  <td class="px-4 py-3 text-right">
                    <p class="text-sm font-bold" :class="line.rejectedQty > 0 ? 'text-orange-600' : 'text-green-700 dark:text-green-400'">{{ calculateAccepted(line) }}</p>
                    <p v-if="line.rejectReason && line.rejectedQty > 0" class="text-[10px] text-red-500 italic truncate max-w-[80px]">{{ line.rejectReason }}</p>
                  </td>
                  <td class="px-4 py-3 text-right">
                    <button v-if="selectedReceipt.isEditable"
                            @click="updateLine(line)"
                            :disabled="updatingLineId === line.id || (line.rejectedQty > 0 && !line.rejectReason)"
                            class="p-1.5 rounded-lg transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                            :class="line.isDirty ? 'bg-amber-100 dark:bg-amber-900/30 ring-2 ring-amber-400 text-amber-700' : 'text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20'"
                            title="Kaydet">
                      <svg v-if="updatingLineId !== line.id" class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                      </svg>
                      <div v-else class="h-5 w-5 border-2 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Mobile line cards -->
          <div class="sm:hidden space-y-3">
            <div
              v-for="line in selectedReceipt.lines"
              :key="line.id"
              class="bg-gray-50 dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden"
            >
              <!-- Line header -->
              <div class="px-3 py-2.5 border-b border-gray-200 dark:border-gray-700">
                <p class="font-semibold text-sm text-gray-900 dark:text-gray-100 leading-tight">{{ line.stockNameSnapshot }}</p>
                <p class="text-[11px] font-mono text-gray-400 dark:text-gray-600">{{ line.unitSnapshot }}</p>
              </div>

              <!-- Qty row -->
              <div class="px-3 py-3 space-y-3">
                <!-- Sipariş / Kabul summary -->
                <div class="grid grid-cols-3 gap-2 text-center">
                  <div class="bg-white dark:bg-gray-900 rounded-lg py-2 border border-gray-200 dark:border-gray-700">
                    <p class="text-[10px] text-gray-400 font-bold uppercase">Sipariş</p>
                    <p class="text-sm font-bold text-gray-900 dark:text-gray-100">{{ line.orderedQty }}</p>
                  </div>
                  <div class="bg-white dark:bg-gray-900 rounded-lg py-2 border border-gray-200 dark:border-gray-700">
                    <p class="text-[10px] text-gray-400 font-bold uppercase">Gelen</p>
                    <!-- Editable input on mobile -->
                    <div v-if="selectedReceipt.isEditable" class="px-2">
                      <input
                        type="number" step="0.01"
                        v-model.number="line.receivedQty"
                        @input="markLineDirty(line)"
                        class="w-full text-center text-sm font-bold border-b-2 bg-transparent focus:outline-none dark:text-gray-100"
                        :class="line.isDirty ? 'border-amber-500 text-amber-700 dark:text-amber-400' : 'border-gray-300 dark:border-gray-600'"
                      />
                    </div>
                    <p v-else class="text-sm font-bold text-gray-900 dark:text-gray-100">{{ line.receivedQty }}</p>
                  </div>
                  <div class="bg-white dark:bg-gray-900 rounded-lg py-2 border border-gray-200 dark:border-gray-700">
                    <p class="text-[10px] text-gray-400 font-bold uppercase">Kabul</p>
                    <p class="text-sm font-bold" :class="line.rejectedQty > 0 ? 'text-orange-600' : 'text-green-600 dark:text-green-400'">{{ calculateAccepted(line) }}</p>
                    <p v-if="calculateAccepted(line) > line.orderedQty" class="text-[9px] text-red-500 font-bold">FAZLA!</p>
                  </div>
                </div>

                <!-- Reject section -->
                <div v-if="selectedReceipt.isEditable" class="flex items-center gap-2">
                  <button
                    @click="toggleReject(line)"
                    class="flex-1 py-1.5 rounded-lg text-xs font-bold border transition-colors"
                    :class="line.rejectedQty > 0 || line.showReject
                      ? 'bg-red-50 dark:bg-red-900/20 text-red-600 dark:text-red-400 border-red-200 dark:border-red-800'
                      : 'bg-white dark:bg-gray-900 text-gray-500 dark:text-gray-400 border-gray-200 dark:border-gray-700'"
                  >
                    {{ (line.rejectedQty > 0 || line.showReject) ? '⚠️ Red: ' + line.rejectedQty : 'Sorun Yok' }}
                  </button>
                  <button
                    v-if="selectedReceipt.isEditable"
                    @click="updateLine(line)"
                    :disabled="updatingLineId === line.id || (line.rejectedQty > 0 && !line.rejectReason)"
                    class="px-4 py-1.5 rounded-lg text-xs font-bold transition-colors disabled:opacity-40 disabled:cursor-not-allowed flex items-center gap-1"
                    :class="line.isDirty ? 'bg-amber-500 hover:bg-amber-600 text-white' : 'bg-blue-600 hover:bg-blue-700 text-white'"
                  >
                    <div v-if="updatingLineId === line.id" class="h-3 w-3 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                    <svg v-else class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
                    </svg>
                    Kaydet
                  </button>
                </div>

                <!-- Red details (expanded) -->
                <div v-if="selectedReceipt.isEditable && (line.showReject || line.rejectedQty > 0)" class="bg-red-50 dark:bg-red-900/10 rounded-xl p-3 border border-red-200 dark:border-red-800 space-y-2">
                  <div class="flex items-center gap-2">
                    <label class="text-xs font-bold text-red-700 dark:text-red-400 whitespace-nowrap">Red Miktarı:</label>
                    <input type="number" step="0.01" v-model.number="line.rejectedQty" @input="markLineDirty(line)"
                           class="flex-1 border border-red-200 dark:border-red-800 rounded-lg px-2 py-1.5 text-sm text-right font-bold text-red-700 dark:text-red-300 dark:bg-gray-800" />
                  </div>
                  <div>
                    <div class="flex justify-between mb-1">
                      <label class="text-xs font-bold text-red-700 dark:text-red-400">Neden:</label>
                      <span v-if="line.rejectedQty > 0 && !line.rejectReason" class="text-xs text-red-600 font-bold animate-bounce">! Seçiniz !</span>
                    </div>
                    <select v-model="line.rejectReason" @change="markLineDirty(line)"
                            class="w-full text-sm border rounded-lg p-2 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                            :class="line.rejectedQty > 0 && !line.rejectReason ? 'bg-red-100 border-red-500' : 'border-red-200'">
                      <option value="">Seçiniz...</option>
                      <option v-for="reason in rejectReasons" :key="reason" :value="reason">{{ reason }}</option>
                    </select>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Add Line -->
          <div v-if="selectedReceipt.isEditable" class="mt-4 bg-indigo-50 dark:bg-indigo-900/10 rounded-xl p-4 border border-indigo-200 dark:border-indigo-800">
            <h4 class="text-xs font-bold text-indigo-700 dark:text-indigo-400 uppercase tracking-wider mb-3">Manuel Kalem Ekle</h4>
            <div class="space-y-3">
              <div>
                <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 mb-1">Stok Seçimi</label>
                <StockCombobox v-model="lineForm.stockMasterId" placeholder="Stok Ara..." />
              </div>
              <div class="flex gap-3 items-end">
                <div class="flex-1">
                  <label class="block text-xs font-semibold text-gray-500 dark:text-gray-400 mb-1">Gelen Miktar</label>
                  <input type="number" v-model="lineForm.receivedQty" class="w-full border border-gray-200 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100 focus:ring-2 focus:ring-indigo-500 focus:border-transparent outline-none" step="0.01" />
                </div>
                <button
                  @click="addLine"
                  :disabled="addingLine || !lineForm.stockMasterId"
                  class="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg text-sm font-semibold transition-colors disabled:opacity-50"
                >
                  <span v-if="addingLine">...</span>
                  <span v-else>+ Ekle</span>
                </button>
              </div>
              <p class="text-xs text-gray-500 dark:text-gray-400 italic">Sipariş dışı ürünler için kullanılır.</p>
            </div>
          </div>
        </div>

      </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { InboxArrowDownIcon } from '@heroicons/vue/24/outline';
import purchaseOrderService from '../services/purchaseOrderService';
import goodsReceiptService from '../services/goodsReceiptService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import { useKeyboardShortcut } from '../composables/useKeyboardShortcut';
import StockCombobox from '../components/StockCombobox.vue';
import StatusBadge from '../components/StatusBadge.vue';
import SkeletonTable from '../components/SkeletonTable.vue';
import EmptyState from '../components/EmptyState.vue';
import CreateGoodsReceiptModal from '../components/CreateGoodsReceiptModal.vue';

const route = useRoute();
const router = useRouter();
const notificationStore = useNotificationStore();
const receipts = ref<any[]>([]);
const loading = ref(false);
const error = ref('');
const filters = ref({ status: '', supplierName: '' });

const showCreateModal = ref(false);
const showDetailModal = ref(false);
const selectedReceipt = ref<any>(null);
const selectedPoForCreate = ref<any>(null);

const posting = ref(false);
const cancelling = ref(false);
const updatingLineId = ref<string | null>(null);
const addingLine = ref(false);
const hasUnsavedChanges = ref(false);

// N tuşu → yeni mal girişi oluştur
useKeyboardShortcut('n', () => { if (!showCreateModal.value && !showDetailModal.value) showCreateModal.value = true; });

const lineForm = ref({ stockMasterId: 0, receivedQty: 0 });
const rejectReasons = ['Hasarlı', 'Eksik / Kırık', 'Yanlış Ürün', 'Kalite Sorunu', 'Diğer'];

const toggleReject = async (line: any) => {
  line.showReject = !line.showReject;
  if (!line.showReject && line.rejectedQty > 0) {
    const ok = await notificationStore.promptConfirm({ title: 'Red Miktarını Sıfırla', message: 'Red miktarını sıfırlamak istiyor musunuz?', confirmText: 'Sıfırla', type: 'warning' });
    if (ok) {
      line.rejectedQty = 0;
      line.rejectReason = '';
      markLineDirty(line);
    } else {
      line.showReject = true;
    }
  }
};

const markLineDirty = (line: any) => {
  line.isDirty = true;
  hasUnsavedChanges.value = true;
};

const calculateAccepted = (line: any) => (line.receivedQty || 0) - (line.rejectedQty || 0);
const formatDate = (date: string) => date ? new Date(date).toLocaleDateString('tr-TR') : '-';

const fetchReceipts = async () => {
  loading.value = true;
  error.value = '';
  try {
    const params: any = { ...filters.value };
    // Clear empty strings to avoid 400 errors from backend enum parsing
    Object.keys(params).forEach(key => {
      if (params[key] === '') delete params[key];
    });
    
    receipts.value = await goodsReceiptService.getAll(params) || [];
  } catch (e) {
    error.value = 'Veriler yüklenirken bir hata oluştu.';
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Mal kabul listesi alınamadı.', 'error');
  } finally {
    loading.value = false;
  }
};

const handleSearch = () => { fetchReceipts(); };

// saveCreate removed as it's handled by component

const onGRSaved = (grId: string | number) => {
  showCreateModal.value = false;
  fetchReceipts();
  openDetail(grId);
};

const openDetail = async (id: any) => {
  try {
    selectedReceipt.value = await goodsReceiptService.getById(id);
    hasUnsavedChanges.value = false;
    showDetailModal.value = true;
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Detay alınamadı.', 'error');
  }
};

const addLine = async () => {
  if (!selectedReceipt.value || addingLine.value) return;
  const exists = selectedReceipt.value.lines.find((l: any) => l.stockMasterId === lineForm.value.stockMasterId);
  if (exists) {
    const ok = await notificationStore.promptConfirm({ title: 'Tekrar Ekle', message: 'Bu ürün zaten listede var. Yine de eklemek istiyor musunuz?', confirmText: 'Ekle', type: 'warning' });
    if (!ok) return;
  }
  addingLine.value = true;
  try {
    await goodsReceiptService.addLine({
      goodsReceiptId: selectedReceipt.value.id,
      stockMasterId: lineForm.value.stockMasterId,
      receivedQty: lineForm.value.receivedQty
    });
    // Refresh
    selectedReceipt.value = await goodsReceiptService.getById(selectedReceipt.value.id);
    lineForm.value.stockMasterId = 0;
    lineForm.value.receivedQty = 0;
    notificationStore.add('Kalem eklendi.', 'success');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Satır eklenemedi.', 'error');
  } finally {
    addingLine.value = false;
  }
};

const updateLine = async (line: any) => {
  if (!selectedReceipt.value || updatingLineId.value) return;
  if (line.rejectedQty > 0 && !line.rejectReason) {
    notificationStore.add('Red edilen miktar için neden belirtilmelidir.', 'warning');
    return;
  }
  updatingLineId.value = line.id;
  try {
    await goodsReceiptService.updateLine(selectedReceipt.value.id, line.id, {
      receivedQty: line.receivedQty,
      rejectedQty: line.rejectedQty,
      rejectReason: line.rejectReason,
      note: line.note
    });
    line.isDirty = false;
    notificationStore.add('Satır güncellendi.', 'success');
    hasUnsavedChanges.value = selectedReceipt.value.lines.some((l: any) => l.isDirty);
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Güncelleme hatası.', 'error');
  } finally {
    updatingLineId.value = null;
  }
};

const postReceipt = async () => {
  if (hasUnsavedChanges.value) {
    notificationStore.add('Lütfen önce kaydedilmemiş satırları güncelleyin.', 'error');
    return;
  }
  const ok = await notificationStore.promptConfirm({ title: 'Mal Kabul Kesinleştir', message: 'Bu işlem geri alınamaz. Mal kabul irsaliyesini kesinleştirmek istiyor musunuz?', confirmText: 'Kesinleştir', type: 'danger' });
  if (!ok) return;
  posting.value = true;
  try {
    await goodsReceiptService.post(selectedReceipt.value.id);
    fetchReceipts();
    showDetailModal.value = false;
    notificationStore.add('Mal kabul kesinleştirildi.', 'success');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Post işlemi başarısız.', 'error');
  } finally {
    posting.value = false;
  }
};

const cancelReceipt = async () => {
  const ok = await notificationStore.promptConfirm({ title: 'İrsaliye İptal', message: 'Mal kabul irsaliyesini iptal etmek istiyor musunuz?', confirmText: 'İptal Et', type: 'danger' });
  if (!ok) return;
  cancelling.value = true;
  try {
    await goodsReceiptService.cancel(selectedReceipt.value.id);
    fetchReceipts();
    showDetailModal.value = false;
    notificationStore.add('Mal kabul iptal edildi.', 'success');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İptal işlemi başarısız.', 'error');
  } finally {
    cancelling.value = false;
  }
};

const openCreateModalFromSelection = async (poId: string) => {
  try {
    const po = await purchaseOrderService.getById(String(poId));
    if (po) {
      selectedPoForCreate.value = po;
      showCreateModal.value = true;
      router.replace({ query: { ...route.query, createPoId: undefined } });
    }
  } catch {
    // ignore
  }
};

onMounted(() => {
  fetchReceipts();
  if (route.query.createPoId) {
    openCreateModalFromSelection(route.query.createPoId as string);
  }
});
</script>
