<template>
  <div class="purchase-order-detail-wrapper">
    <div class="space-y-4">
      <!-- Loading state -->
      <div v-if="loading && !order" class="flex justify-center py-16">
        <div class="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>

      <!-- Error state -->
      <div v-else-if="error" class="bg-red-50 dark:bg-red-900/20 rounded-xl border border-red-200 dark:border-red-800 p-6 text-center">
        <p class="text-red-700 dark:text-red-300 font-semibold">{{ error }}</p>
        <button @click="fetchOrder" class="mt-3 text-sm text-red-600 underline">Tekrar dene</button>
      </div>

      <template v-else-if="order">
        <!-- Breadcrumb + Back -->
        <div class="flex items-center gap-3 mb-2">
          <button
            @click="router.back()"
            class="flex items-center gap-1.5 px-3 py-1.5 text-xs font-bold text-gray-500 dark:text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl hover:border-blue-300 transition-all"
          >
            <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M15 19l-7-7 7-7" />
            </svg>
            Geri
          </button>
          <nav class="flex items-center gap-2 text-xs font-bold text-gray-400 dark:text-gray-500 uppercase tracking-widest">
            <button @click="router.back()" class="hover:text-blue-600 transition-colors">Siparişler</button>
            <span class="text-gray-300">/</span>
            <span class="text-blue-600 dark:text-blue-400">{{ order.orderNumber }}</span>
          </nav>
        </div>

        <!-- Main Header Card -->
        <div class="relative overflow-hidden bg-white dark:bg-gray-900 rounded-3xl shadow-xl shadow-blue-100/50 dark:shadow-none border border-blue-50 dark:border-blue-900 group">
          <!-- Decorative gradient -->
          <div class="absolute -top-24 -right-24 h-64 w-64 bg-blue-500/5 rounded-full blur-3xl group-hover:bg-blue-500/10 transition-all duration-700"></div>
          
          <div class="relative p-6 sm:p-8">
            <div class="flex flex-col lg:flex-row lg:items-center justify-between gap-6">
              <!-- Left: Info -->
              <div class="space-y-4">
                <div class="flex items-center gap-3">
                  <div class="p-3 bg-blue-600 rounded-2xl text-white shadow-lg shadow-blue-200 dark:shadow-none">
                    <svg class="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 11V7a4 4 0 00-8 0v4M5 9h14l1 12H4L5 9z" />
                    </svg>
                  </div>
                  <div>
                    <h1 class="text-2xl font-black text-gray-900 dark:text-gray-100 leading-none tracking-tight">{{ order.supplierNameSnapshot }}</h1>
                    <div class="flex items-center gap-2 mt-2">
                       <StatusBadge :status="order.status" type="purchaseOrder" />
                       <span class="h-1 w-1 rounded-full bg-gray-300 dark:bg-gray-700"></span>
                       <span class="text-xs font-bold text-gray-400 dark:text-gray-500 uppercase tracking-widest">{{ formatDate(order.orderDate) }}</span>
                    </div>
                  </div>
                </div>

                <!-- Metadata Grid -->
                <div class="grid grid-cols-2 sm:flex sm:items-center gap-4 sm:gap-8 pt-2">
                  <div class="space-y-0.5">
                    <p class="text-[10px] font-black text-gray-400 uppercase tracking-widest">Sipariş No</p>
                    <p class="text-sm font-bold text-blue-600 dark:text-blue-400">{{ order.orderNumber }}</p>
                  </div>
                  <div v-if="order.expectedDeliveryDate" class="space-y-0.5">
                     <p class="text-[10px] font-black text-gray-400 uppercase tracking-widest">Termin Tarihi</p>
                     <p class="text-sm font-bold text-gray-900 dark:text-gray-100">{{ formatDate(order.expectedDeliveryDate) }}</p>
                  </div>
                  <div class="space-y-0.5">
                     <p class="text-[10px] font-black text-gray-400 uppercase tracking-widest">Kalem Sayısı</p>
                     <p class="text-sm font-bold text-gray-900 dark:text-gray-100">{{ order.lines?.length || 0 }}</p>
                  </div>
                </div>
              </div>

              <!-- Right: Stats & Main Actions -->
              <div class="flex flex-col sm:flex-row items-stretch sm:items-center gap-4">
                 <!-- Mini Stats -->
                 <div class="flex items-center gap-2 bg-gray-50 dark:bg-gray-800/50 p-2 rounded-2xl border border-gray-100 dark:border-gray-800">
                    <div class="px-4 py-2 text-center text-xs">
                      <p class="text-[10px] font-black text-gray-400 uppercase leading-none">Toplam Sipariş</p>
                      <p class="text-lg font-black text-gray-900 dark:text-gray-100 mt-1">{{ totalOrdered }}</p>
                    </div>
                    <div class="w-px h-8 bg-gray-200 dark:bg-gray-700"></div>
                    <div class="px-4 py-2 text-center text-xs">
                      <p class="text-[10px] font-black text-green-500 uppercase leading-none">Gelen</p>
                      <p class="text-lg font-black text-green-600 mt-1">{{ totalReceived }}</p>
                    </div>
                 </div>

                 <!-- PDF + Mail Actions (Approved+) -->
                 <div v-if="order.status !== 'Draft'" class="flex flex-wrap gap-2">
                    <button
                      @click="downloadPdf"
                      class="px-4 py-2.5 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-700 dark:text-gray-300 font-bold rounded-xl hover:bg-gray-50 dark:hover:bg-gray-700 transition-all flex items-center gap-2 text-sm"
                      title="PDF İndir"
                    >
                      <svg class="h-4 w-4 text-red-500" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" /></svg>
                      PDF
                    </button>
                    <button
                      @click="openMail"
                      class="px-4 py-2.5 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-700 dark:text-gray-300 font-bold rounded-xl hover:bg-gray-50 dark:hover:bg-gray-700 transition-all flex items-center gap-2 text-sm"
                      title="Mail Hazırla"
                    >
                      <svg class="h-4 w-4 text-blue-500" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" /></svg>
                      Mail
                    </button>
                 </div>

                 <!-- Primary Actions -->
                 <div class="flex flex-wrap gap-2">
                    <template v-if="order.status === 'Draft'">
                      <button v-if="canEdit" @click="handleApprove" :disabled="actionLoading" class="px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-xl shadow-lg shadow-blue-100 dark:shadow-none transition-all flex items-center justify-center gap-2">
                        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                        Onayla
                      </button>
                      <button v-if="canEdit" @click="showEditForm = !showEditForm" class="p-3 text-blue-600 bg-blue-50 dark:bg-blue-900/10 hover:bg-blue-100 dark:hover:bg-blue-900/20 rounded-xl transition-all">
                        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                      </button>
                      <button v-if="canEdit" @click="handleCancel" class="p-3 text-red-500 bg-red-50 dark:bg-red-900/10 hover:bg-red-100 rounded-xl transition-all">
                        <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                      </button>
                    </template>
                    <template v-else-if="order.status === 'Approved' || order.status === 'PartiallyReceived'">
                       <button @click="goToMalKabul" class="px-6 py-3 bg-emerald-600 hover:bg-emerald-700 text-white font-bold rounded-xl shadow-lg shadow-emerald-100 dark:shadow-none transition-all flex items-center gap-2">
                         <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" /></svg>
                         Mal Kabul
                       </button>
                       <!-- Netsis aktarım butonu — sadece henüz aktarılmamışsa görünür -->
                       <button
                         v-if="!order.netsisTransferredAt"
                         v-role="['Admin', 'Manager', 'Accounting']"
                         @click="handleExportToNetsis"
                         :disabled="netsisLoading"
                         class="px-6 py-3 bg-orange-600 hover:bg-orange-700 text-white font-bold rounded-xl shadow-lg shadow-orange-100 dark:shadow-none transition-all flex items-center gap-2 disabled:opacity-60"
                       >
                         <svg v-if="netsisLoading" class="h-5 w-5 animate-spin" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
                         <svg v-else class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" /></svg>
                         {{ netsisLoading ? 'Gönderiliyor...' : 'Netsis\'e Gönder' }}
                       </button>
                       <!-- Netsis aktarım durumu badge — aktarıldıysa göster -->
                       <div v-if="order.netsisTransferredAt" class="flex items-center gap-2 px-4 py-2.5 bg-green-50 dark:bg-green-900/20 border border-green-200 dark:border-green-800 rounded-xl">
                         <svg class="h-4 w-4 text-green-500 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" /></svg>
                         <div>
                           <p class="text-xs font-black text-green-700 dark:text-green-400 uppercase tracking-wider">Netsis'e Aktarıldı</p>
                           <p v-if="order.externalRef" class="text-xs text-green-600 dark:text-green-500 font-mono">{{ order.externalRef }}</p>
                         </div>
                       </div>
                       <button v-if="canEdit" @click="handleClose" class="px-6 py-3 bg-gray-800 text-white font-bold rounded-xl transition-all">Kapat</button>
                    </template>
                 </div>
              </div>
            </div>

            <!-- Edit form (inline) -->
            <div v-if="showEditForm && order.status === 'Draft'" class="mt-6 pt-6 border-t border-gray-100 dark:border-gray-800 animate-in slide-in-from-top-4 duration-300">
               <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 pb-4">
                  <div class="space-y-1">
                    <label class="text-[10px] font-black text-gray-400 uppercase tracking-widest ml-1">Sipariş Tarihi</label>
                    <input v-model="editForm.orderDate" type="date" class="w-full border-gray-200 dark:border-gray-800 rounded-xl px-4 py-2 text-sm dark:bg-gray-800" />
                  </div>
                  <div class="space-y-1">
                    <label class="text-[10px] font-black text-gray-400 uppercase tracking-widest ml-1">Termin Tarihi</label>
                    <input v-model="editForm.expectedDeliveryDate" type="date" class="w-full border-gray-200 dark:border-gray-800 rounded-xl px-4 py-2 text-sm dark:bg-gray-800" />
                  </div>
                  <div class="space-y-1">
                    <label class="text-[10px] font-black text-gray-400 uppercase tracking-widest ml-1">Not / Açıklama</label>
                    <input v-model="editForm.note" type="text" class="w-full border-gray-200 dark:border-gray-800 rounded-xl px-4 py-2 text-sm dark:bg-gray-800" placeholder="Opsiyonel..." />
                  </div>
               </div>
               <div class="flex justify-end gap-2">
                  <button @click="showEditForm = false" class="px-4 py-2 text-xs font-bold text-gray-500 uppercase">Vazgeç</button>
                  <button @click="handleUpdate" :disabled="updating" class="px-6 py-2 bg-blue-600 text-white text-xs font-black uppercase rounded-lg shadow-lg shadow-blue-100">Güncelle</button>
               </div>
            </div>

            <div v-else-if="order.note" class="mt-6 flex items-start gap-2 p-3 bg-amber-50 dark:bg-amber-900/10 border border-amber-100 dark:border-amber-900/30 rounded-2xl text-amber-800 dark:text-amber-400 text-sm italic">
              <svg class="h-4 w-4 shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 8h10M7 12h4m1 8l-4-4H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-3l-4 4z" /></svg>
              {{ order.note }}
            </div>
          </div>
        </div>


        <!-- Lines -->
        <div class="bg-white dark:bg-gray-900 rounded-3xl shadow-sm border border-gray-100 dark:border-gray-800 overflow-hidden">
          <div class="px-4 sm:px-6 py-4 border-b border-gray-100 dark:border-gray-800 flex items-center justify-between">
            <h2 class="text-sm font-black text-gray-400 uppercase tracking-widest flex items-center gap-2">
               Sipariş Kalemleri
               <span class="bg-blue-100 dark:bg-blue-900/40 text-blue-600 dark:text-blue-400 px-2 py-0.5 rounded-full text-[10px] font-black">{{ order.lines?.length || 0 }}</span>
            </h2>
          </div>

          <!-- Desktop -->
          <div class="hidden sm:block overflow-x-auto">
            <table class="min-w-full divide-y divide-gray-100 dark:divide-gray-800">
              <thead class="bg-gray-50/50 dark:bg-gray-800/50">
                <tr>
                  <th class="px-6 py-4 text-left text-[10px] font-black text-gray-400 uppercase tracking-widest">Ürün Bilgisi</th>
                  <th class="px-4 py-4 text-center text-[10px] font-black text-gray-400 uppercase tracking-widest">Sipariş Edilen</th>
                  <th class="px-4 py-4 text-center text-[10px] font-black text-gray-400 uppercase tracking-widest">Teslim Alınan</th>
                  <th class="px-4 py-4 text-center text-[10px] font-black text-gray-400 uppercase tracking-widest">Kalan</th>
                  <th v-if="order.status === 'Draft'" class="px-6 py-4 text-right"></th>
                </tr>
              </thead>
              <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
                <tr v-if="!order.lines || order.lines.length === 0">
                  <td colspan="5" class="px-6 py-16 text-center text-sm text-gray-400 font-bold uppercase tracking-widest">Henüz kalem eklenmemiş.</td>
                </tr>
                <tr v-for="line in order.lines" :key="line.id" class="hover:bg-blue-50/30 dark:hover:bg-blue-900/10 transition-colors group">
                  <td class="px-6 py-4">
                    <div class="flex items-center gap-3">
                        <div class="h-10 w-10 shrink-0 bg-gray-100 dark:bg-gray-800 rounded-xl flex items-center justify-center text-gray-400 font-black text-xs">
                          {{ line.stockName?.charAt(0) }}
                        </div>
                        <div class="min-w-0">
                          <p class="text-sm font-black text-gray-900 dark:text-gray-100 leading-none truncate">{{ line.stockName }}</p>
                          <p class="text-[10px] text-gray-500 dark:text-gray-400 font-bold uppercase tracking-widest mt-1.5">{{ line.unit }}</p>
                        </div>
                      </div>
                    </td>
                  <td class="px-4 py-4 text-center">
                     <div v-if="order.status === 'Draft' && editingLineId === line.id" class="flex items-center justify-center gap-1">
                        <input v-model.number="editLineForm.orderedQty" type="number" step="0.01" class="w-20 border-blue-200 rounded-lg px-2 py-1 text-sm text-center dark:bg-gray-800 font-black" />
                        <button @click="saveLineEdit(line)" class="p-1 text-green-600"><svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" /></svg></button>
                     </div>
                     <span v-else class="text-sm font-black text-gray-900 dark:text-gray-100">{{ line.orderedQty }}</span>
                  </td>
                  <td class="px-4 py-4 text-center">
                     <span class="text-sm font-black text-green-600">{{ line.receivedQty ?? 0 }}</span>
                  </td>
                  <td class="px-4 py-4 text-center">
                     <span class="text-sm font-black" :class="(line.remainingQty || line.orderedQty) > 0 ? 'text-red-500' : 'text-gray-300'">
                        {{ line.remainingQty ?? line.orderedQty }}
                     </span>
                  </td>
                  <td v-if="order.status === 'Draft'" class="px-6 py-4 text-right">
                    <div class="flex items-center justify-end gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                      <button @click="startLineEdit(line)" class="p-2 text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded-xl transition-all">
                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                      </button>
                      <button @click="confirmRemoveLine(line)" class="p-2 text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-xl transition-all">
                        <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                      </button>
                    </div>
                  </td>
                </tr>
                <tr v-if="order.status === 'Draft' && showAddLineForm" class="bg-blue-50/50 dark:bg-blue-900/20">
                  <td class="px-6 py-4">
                     <StockCombobox v-model="newLineForm.stockMasterId" />
                  </td>
                  <td class="px-4 py-4 text-center">
                     <input v-model.number="newLineForm.orderedQty" type="number" step="0.01" class="w-20 border-blue-200 rounded-lg px-2 py-1 text-sm text-center dark:bg-gray-800 font-black" placeholder="Miktar" />
                  </td>
                  <td class="px-4 py-4 text-center">
                     <!-- New line, no received qty yet -->
                     <span class="text-sm text-gray-400">-</span>
                  </td>
                  <td class="px-4 py-4 text-center">
                     <span class="text-sm text-gray-400">-</span>
                  </td>
                  <td class="px-6 py-4 text-right">
                     <div class="flex items-center justify-end gap-2">
                        <button @click="saveAddLine" :disabled="addingLine" class="p-2 text-green-600 hover:bg-green-100 rounded-xl transition-all">
                           <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" /></svg>
                        </button>
                        <button @click="showAddLineForm = false" class="p-2 text-gray-500 hover:bg-gray-200 rounded-xl transition-all">
                           <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" /></svg>
                        </button>
                     </div>
                  </td>
                </tr>
                <tr v-if="order.status === 'Draft' && !showAddLineForm" class="bg-gray-50/10 hover:bg-gray-50 dark:bg-gray-900 dark:hover:bg-gray-800/50 cursor-pointer" @click="showAddLineForm = true">
                  <td colspan="5" class="px-6 py-4 text-center text-sm font-bold text-blue-600 dark:text-blue-400 uppercase tracking-wider flex items-center justify-center gap-2">
                     <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                     Yeni Kalem Ekle
                  </td>
                </tr>
              </tbody>
            </table>
          </div>

          <!-- Mobile cards -->
          <div class="sm:hidden divide-y divide-gray-200 dark:divide-gray-700">
            <div v-if="!order.lines || order.lines.length === 0" class="px-4 py-8 text-center text-sm text-gray-500 dark:text-gray-400">
              Kalem bulunamadı.
            </div>
            <div v-for="line in order.lines" :key="line.id" class="px-4 py-3">
              <div class="flex justify-between items-start gap-2">
                <div class="min-w-0 flex-1">
                  <p class="text-sm font-semibold text-gray-900 dark:text-gray-100 leading-tight">{{ line.stockName }}</p>
                  <p class="text-xs text-gray-500 dark:text-gray-400">{{ line.unit }}</p>
                </div>
                <div v-if="order.status === 'Draft'" class="flex gap-1 shrink-0">
                  <button @click="startLineEdit(line)" class="p-1.5 text-blue-500 rounded-lg">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" /></svg>
                  </button>
                  <button @click="confirmRemoveLine(line)" class="p-1.5 text-red-500 rounded-lg">
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
                  </button>
                </div>
              </div>
              <div class="grid grid-cols-3 gap-2 mt-2 text-center">
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg py-1.5 border border-gray-200 dark:border-gray-700">
                  <p class="text-[10px] text-gray-400 uppercase font-bold">Sipariş</p>
                  <p v-if="editingLineId === line.id" class="px-1">
                    <input v-model.number="editLineForm.orderedQty" type="number" step="0.01" class="w-full text-center text-xs border border-blue-400 rounded dark:bg-gray-800 dark:text-gray-100" />
                  </p>
                  <p v-else class="text-sm font-bold text-gray-900 dark:text-gray-100">{{ line.orderedQty }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg py-1.5 border border-gray-200 dark:border-gray-700">
                  <p class="text-[10px] text-gray-400 uppercase font-bold">Gelen</p>
                  <p class="text-sm font-bold text-green-600">{{ line.receivedQty ?? 0 }}</p>
                </div>
                <div class="bg-gray-50 dark:bg-gray-800 rounded-lg py-1.5 border border-gray-200 dark:border-gray-700">
                  <p class="text-[10px] text-gray-400 uppercase font-bold">Kalan</p>
                  <p class="text-sm font-bold text-red-600">{{ line.remainingQty ?? line.orderedQty }}</p>
                </div>
              </div>
              <div v-if="editingLineId === line.id" class="flex gap-2 mt-2">
                <button @click="saveLineEdit(line)" :disabled="updatingLine" class="flex-1 py-1.5 text-xs font-bold text-white bg-green-600 rounded-lg">Kaydet</button>
                <button @click="editingLineId = null" class="flex-1 py-1.5 text-xs font-bold text-gray-600 bg-gray-100 dark:bg-gray-800 rounded-lg">İptal</button>
              </div>
            </div>
            <!-- Mobile Add Line Button -->
            <div v-if="order.status === 'Draft' && !showAddLineForm" class="px-4 py-4 w-full">
               <button @click="showAddLineForm = true" class="w-full py-3 bg-blue-50 dark:bg-blue-900/20 text-blue-600 dark:text-blue-400 rounded-xl font-bold text-sm tracking-wide uppercase flex items-center justify-center gap-2">
                  <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" /></svg>
                  Yeni Kalem Ekle
               </button>
            </div>
            <!-- Mobile Add Line Form -->
            <div v-if="order.status === 'Draft' && showAddLineForm" class="px-4 py-4 bg-blue-50/50 dark:bg-blue-900/10">
               <div class="space-y-3">
                  <StockCombobox v-model="newLineForm.stockMasterId" />
                  <input v-model.number="newLineForm.orderedQty" type="number" step="0.01" class="w-full border-gray-200 rounded-xl px-4 py-2 text-sm dark:bg-gray-800 font-black" placeholder="Miktar girin..." />
                  <div class="flex gap-2">
                     <button @click="saveAddLine" :disabled="addingLine" class="flex-1 py-2 text-xs font-bold text-white bg-green-600 rounded-xl">Ekle</button>
                     <button @click="showAddLineForm = false" class="flex-1 py-2 text-xs font-bold text-gray-600 bg-gray-200 dark:bg-gray-700 rounded-xl">Vazgeç</button>
                  </div>
               </div>
            </div>
          </div>
        </div>

        <!-- Linked GR section -->
        <div v-if="order.goodsReceiptCount > 0" class="bg-white dark:bg-gray-900 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-4">
          <h3 class="text-sm font-bold text-gray-700 dark:text-gray-300 uppercase tracking-wider mb-2">Bağlı Mal Kabul İrsaliyeleri</h3>
          <p class="text-sm text-gray-500 dark:text-gray-400">
            Bu siparişe bağlı
            <strong class="text-gray-900 dark:text-gray-100">{{ order.goodsReceiptCount }}</strong>
            adet irsaliye mevcut.
            <router-link to="/goods-receipts" class="ml-1 text-blue-600 hover:underline text-sm">İrsaliye listesine git &rarr;</router-link>
          </p>
        </div>
      </template>
    </div>

    <!-- Hidden PDF layout — captured by html2canvas -->
    <div
      v-if="order"
      ref="pdfContent"
      style="position:fixed;left:-9999px;top:0;width:794px;background:#fff;font-family:'Segoe UI',Arial,sans-serif;color:#1e293b;"
    >
      <!-- Header -->
      <div style="background:#1e3a5f;padding:20px 28px;display:flex;justify-content:space-between;align-items:center;">
        <img src="/logo.png" style="height:60px;width:auto;object-fit:contain;" crossorigin="anonymous" />
        <div style="text-align:right;">
          <div style="color:#fff;font-size:20px;font-weight:700;letter-spacing:1.5px;">SATIN ALMA SİPARİŞİ</div>
        </div>
      </div>

      <!-- Company + PO Info -->
      <div style="display:flex;justify-content:space-between;padding:18px 28px 14px;border-bottom:1px solid #cbd5e1;">
        <div>
          <div style="font-weight:700;font-size:14px;">AKYILDIZ İNŞAAT LOJİSTİK TEDARİK VE TESİS YÖNETİM HİZMETLERİ LTD. ŞTİ.</div>
          <div style="font-size:11px;color:#64748b;margin-top:4px;">Sultan Orhan Mah. 1175 sk. No:12/2</div>
          <div style="font-size:11px;color:#64748b;">Gebze/Kocaeli</div>
        </div>
        <div style="text-align:right;">
          <div style="font-size:11px;color:#64748b;">Sipariş No: <strong style="color:#1e293b;">{{ order.orderNumber }}</strong></div>
          <div style="font-size:11px;color:#64748b;margin-top:3px;">Tarih: <strong style="color:#1e293b;">{{ formatDate(order.orderDate) }}</strong></div>
          <div v-if="order.expectedDeliveryDate" style="font-size:11px;color:#64748b;margin-top:3px;">Termin: <strong style="color:#1e293b;">{{ formatDate(order.expectedDeliveryDate) }}</strong></div>
        </div>
      </div>

      <!-- Supplier Box -->
      <div style="margin:16px 28px;background:#f0f4f8;border-radius:4px;padding:12px 16px;">
        <div style="font-size:9px;color:#64748b;font-weight:700;text-transform:uppercase;letter-spacing:1px;">TEDARİKÇİ</div>
        <div style="font-size:14px;font-weight:700;color:#1e293b;margin-top:4px;">{{ order.supplierNameSnapshot }}</div>
        <div v-if="order.supplierEmail" style="font-size:11px;color:#475569;margin-top:2px;">{{ order.supplierEmail }}</div>
      </div>

      <!-- Items Table -->
      <div style="margin:0 28px;">
        <table style="width:100%;border-collapse:collapse;font-size:11px;">
          <thead>
            <tr style="background:#1e3a5f;">
              <th style="padding:8px 10px;text-align:left;color:#fff;font-weight:700;width:32px;">#</th>
              <th style="padding:8px 10px;text-align:left;color:#fff;font-weight:700;width:110px;">Stok Kodu</th>
              <th style="padding:8px 10px;text-align:left;color:#fff;font-weight:700;">Ürün Adı</th>
              <th style="padding:8px 10px;text-align:right;color:#fff;font-weight:700;width:70px;">Miktar</th>
              <th style="padding:8px 10px;text-align:left;color:#fff;font-weight:700;width:60px;">Birim</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(line, idx) in order.lines" :key="line.id" :style="{ background: (idx as any) % 2 === 0 ? '#f0f4f8' : '#ffffff' }">
              <td style="padding:7px 10px;">{{ (idx as any) + 1 }}</td>
              <td style="padding:7px 10px;">{{ line.stockCode || '-' }}</td>
              <td style="padding:7px 10px;">{{ line.stockName }}</td>
              <td style="padding:7px 10px;text-align:right;font-weight:600;">{{ line.orderedQty }}</td>
              <td style="padding:7px 10px;">{{ line.unit }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Notes -->
      <div style="margin:20px 28px 0;padding-top:14px;border-top:1px solid #cbd5e1;">
        <div style="font-size:10px;color:#475569;margin-bottom:5px;">• Sipariş numarasının irsaliye veya faturada belirtilmesi gerekmektedir.</div>
        <div style="font-size:10px;color:#475569;">• Siparişten fazla teslimatta fazla mal kabul edilmeyecektir.</div>
      </div>

      <!-- Signature line -->
      <div style="margin:28px 28px 36px;display:flex;justify-content:space-between;">
        <div style="font-size:11px;">Tarih: _______________</div>
        <div style="font-size:11px;">Yetkili İmza: ___________________________</div>
      </div>
    </div>

    <!-- CC Seçim Modalı -->
    <CcContactsPickerModal
      :show="showCcModal"
      title="Sipariş Maili — CC Seç"
      submit-label="Gönder"
      :submitting="actionLoading"
      :header-html="ccModalHeaderHtml"
      @close="closeCcModal"
      @submit="onCcSubmit"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { jsPDF } from 'jspdf';
import html2canvas from 'html2canvas';
import purchaseOrderService from '../services/purchaseOrderService';
import { supplierService } from '../services/supplierService';
import CcContactsPickerModal from '../components/CcContactsPickerModal.vue';
import { useNotificationStore } from '../stores/notification';
import { useAuthStore } from '../stores/auth';
import { ApiErrorUtils } from '../utils/apiError';
import { formatDate } from '../utils/dateFormat';
import StatusBadge from '../components/StatusBadge.vue';
import StockCombobox from '../components/StockCombobox.vue';

const route = useRoute();
const router = useRouter();
const notificationStore = useNotificationStore();
const authStore = useAuthStore();

const order = ref<any>(null);
const loading = ref(false);
const error = ref('');
const actionLoading = ref(false);
const netsisLoading = ref(false);
const updating = ref(false);
const updatingLine = ref(false);
const addingLine = ref(false);
const showEditForm = ref(false);
const showAddLineForm = ref(false);
const editingLineId = ref<string | null>(null);

const editForm = ref({ orderDate: '', expectedDeliveryDate: '', note: '' });
const editLineForm = ref({ orderedQty: 0, note: '' });
const newLineForm = ref({ stockMasterId: 0, orderedQty: 0 });
const pdfContent = ref<HTMLElement | null>(null);

const totalOrdered = computed(() => order.value?.lines?.reduce((sum: number, l: any) => sum + (l.orderedQty || 0), 0) || 0);
const totalReceived = computed(() => order.value?.lines?.reduce((sum: number, l: any) => sum + (l.receivedQty || 0), 0) || 0);

const canEdit = computed(() => {
  const role = authStore.userRole;
  return ['Admin', 'Accounting', 'Manager'].includes(role);
});

const fetchOrder = async () => {
  loading.value = true;
  error.value = '';
  try {
    order.value = await purchaseOrderService.getById(route.params.id as string);
  } catch (e) {
    error.value = ApiErrorUtils.getErrorMessage(e) || 'Sipariş yüklenemedi.';
  } finally {
    loading.value = false;
  }
};

const handleApprove = async () => {
  const ok = await notificationStore.promptConfirm({
    title: 'Sipariş Onayla',
    message: 'Satınalma siparişini onaylamak istiyor musunuz?',
    confirmText: 'Onayla',
    type: 'info'
  });
  if (!ok) return;
  actionLoading.value = true;
  try {
    await purchaseOrderService.approve(order.value.id);
    notificationStore.add('Sipariş onaylandı.', 'success');
    await fetchOrder();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem hatası.', 'error');
  } finally {
    actionLoading.value = false;
  }
};

const handleClose = async () => {
  const ok = await notificationStore.promptConfirm({
    title: 'Sipariş Kapat',
    message: 'Kalan miktarları almayacaksınız. Siparişi kapatmak istiyor musunuz?',
    confirmText: 'Kapat',
    type: 'warning'
  });
  if (!ok) return;
  actionLoading.value = true;
  try {
    await purchaseOrderService.close(order.value.id);
    notificationStore.add('Sipariş kapatıldı.', 'success');
    await fetchOrder();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem hatası.', 'error');
  } finally {
    actionLoading.value = false;
  }
};

const handleCancel = async () => {
  const ok = await notificationStore.promptConfirm({
    title: 'Sipariş İptal',
    message: 'Sipariş iptal edilecek. Bu işlem geri alınamaz. Emin misiniz?',
    confirmText: 'İptal Et',
    type: 'danger'
  });
  if (!ok) return;
  actionLoading.value = true;
  try {
    await purchaseOrderService.cancel(order.value.id);
    notificationStore.add('Sipariş iptal edildi.', 'success');
    await fetchOrder();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İptal başarısız.', 'error');
  } finally {
    actionLoading.value = false;
  }
};

const handleUpdate = async () => {
  updating.value = true;
  try {
    await purchaseOrderService.update(order.value.id, {
      orderDate: editForm.value.orderDate || undefined,
      expectedDeliveryDate: editForm.value.expectedDeliveryDate || undefined,
      note: editForm.value.note || undefined
    });
    notificationStore.add('Sipariş güncellendi.', 'success');
    showEditForm.value = false;
    await fetchOrder();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Güncelleme başarısız.', 'error');
  } finally {
    updating.value = false;
  }
};

const startLineEdit = (line: any) => {
  editingLineId.value = line.id;
  editLineForm.value = { orderedQty: line.orderedQty, note: line.note ?? '' };
};

const saveLineEdit = async (line: any) => {
  updatingLine.value = true;
  try {
    await purchaseOrderService.updateLine(order.value.id, line.id, {
      orderedQty: editLineForm.value.orderedQty,
      note: editLineForm.value.note || undefined
    });
    notificationStore.add('Kalem güncellendi.', 'success');
    editingLineId.value = null;
    await fetchOrder();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kalem güncellenemedi.', 'error');
  } finally {
    updatingLine.value = false;
  }
};

const saveAddLine = async () => {
  if (!newLineForm.value.stockMasterId || newLineForm.value.orderedQty <= 0) {
     notificationStore.add('Lütfen geçerli bir stok ve miktar seçin.', 'warning');
     return;
  }
  addingLine.value = true;
  try {
     await purchaseOrderService.addLine(order.value.id, {
        stockMasterId: newLineForm.value.stockMasterId,
        orderedQty: newLineForm.value.orderedQty
     });
     notificationStore.add('Yeni kalem eklendi.', 'success');
     newLineForm.value = { stockMasterId: 0, orderedQty: 0 };
     showAddLineForm.value = false;
     await fetchOrder();
  } catch (e) {
     notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kalem eklenemedi.', 'error');
  } finally {
     addingLine.value = false;
  }
};

const confirmRemoveLine = async (line: any) => {
  const ok = await notificationStore.promptConfirm({
    title: 'Kalemi Sil',
    message: `"${line.stockName}" kalemi silinecek. Emin misiniz?`,
    confirmText: 'Sil',
    type: 'danger'
  });
  if (!ok) return;
  try {
    await purchaseOrderService.removeLine(order.value.id, line.id);
    notificationStore.add('Kalem silindi.', 'success');
    await fetchOrder();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kalem silinemedi.', 'error');
  }
};

const goToMalKabul = () => {
  router.push({ name: 'MalKabulDashboard', query: { poId: order.value.id } });
};

const handleExportToNetsis = async () => {
  const ok = await notificationStore.promptConfirm({
    title: 'Netsis\'e Gönder',
    message: 'Satınalma siparişi Netsis\'e aktarılacak. Bu işlem geri alınamaz. Devam edilsin mi?',
    confirmText: 'Gönder',
    type: 'info'
  });
  if (!ok) return;
  netsisLoading.value = true;
  try {
    const res = await purchaseOrderService.exportToNetsis(order.value.id);
    notificationStore.add(res.message || `Netsis'e aktarıldı. Belge No: ${res.netsisPONo}`, 'success');
    await fetchOrder();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Netsis aktarımı başarısız.', 'error');
  } finally {
    netsisLoading.value = false;
  }
};

const downloadPdf = async () => {
  if (!pdfContent.value || !order.value) return;
  try {
    const canvas = await html2canvas(pdfContent.value, {
      scale: 2,
      useCORS: true,
      backgroundColor: '#ffffff',
      logging: false
    });
    const imgData = canvas.toDataURL('image/png');
    const pdf = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    const pageW = 210;
    const imgH = (canvas.height / canvas.width) * pageW;
    pdf.addImage(imgData, 'PNG', 0, 0, pageW, Math.min(imgH, 297));
    const fileName = `PO-${order.value.orderNumber}-${new Date().toISOString().split('T')[0]}.pdf`;
    pdf.save(fileName);
  } catch {
    notificationStore.add('PDF oluşturulamadı.', 'error');
  }
};

const showCcModal = ref(false);

const ccModalHeaderHtml = computed(() => {
  const o = order.value;
  if (!o) return '';
  const supplier = escapeHtml(o.supplierNameSnapshot || '—');
  const orderNo = escapeHtml(o.orderNumber || '');
  const email = escapeHtml(o.supplierEmail || '');
  return `<div class="font-semibold text-gray-800 dark:text-gray-100">${supplier}</div>
          <div class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">Sipariş: ${orderNo}</div>
          ${email ? `<div class="text-xs text-gray-500 dark:text-gray-400">Alıcı: ${email}</div>` : ''}`;
});

function escapeHtml(s: string): string {
  return s.replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]!));
}

const openMail = async () => {
  const o = order.value;

  if (!o.supplierEmail) {
    const emailStr = window.prompt('Tedarikçi mail adresi tanımlı değil. Lütfen bir e-posta girin (birden fazla için virgülle ayırın):');
    if (!emailStr) return;
    if (!emailStr.includes('@')) {
      notificationStore.add('Geçerli bir e-posta adresi girmediniz.', 'warning');
      return;
    }
    try {
      actionLoading.value = true;
      const sups = await supplierService.getAll();
      const sup = sups.find((s: any) => s.id === o.supplierId);
      if (sup) {
        await supplierService.update(sup.id, { name: sup.name, supplierCode: sup.supplierCode, email: emailStr });
        o.supplierEmail = emailStr;
        notificationStore.add('Tedarikçi e-posta adresi kaydedildi.', 'success');
      } else {
        notificationStore.add('Tedarikçi bulunamadı.', 'error');
        return;
      }
    } catch {
      notificationStore.add('Tedarikçi güncellenemedi.', 'error');
      return;
    } finally {
      actionLoading.value = false;
    }
  }

  showCcModal.value = true;
};

const closeCcModal = () => {
  if (actionLoading.value) return;
  showCcModal.value = false;
};

const onCcSubmit = async (ccEmails: string[]) => {
  const o = order.value;
  if (!o) return;
  showCcModal.value = false;
  try {
    actionLoading.value = true;
    const result = await purchaseOrderService.sendEmail(o.id, undefined, ccEmails);
    o.emailSentAt = new Date().toISOString();
    o.emailSentTo = result.sentTo;
    notificationStore.add(`Mail başarıyla gönderildi → ${result.sentTo}`, 'success');
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Mail gönderilemedi.', 'error');
  } finally {
    actionLoading.value = false;
  }
};

onMounted(() => {
  fetchOrder();
});
</script>
