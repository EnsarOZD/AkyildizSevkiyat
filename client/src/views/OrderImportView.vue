<template>
  <div class="p-4 sm:p-6 space-y-4 sm:space-y-6">
    <PageHeader title="ISS Entegrasyon" subtitle="Sipariş Aktarımı ve Stok Eşleşmesi" color="blue">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8.111 16.404a5.5 5.5 0 017.778 0M12 20h.01m-7.08-7.071c3.904-3.905 10.236-3.905 14.141 0M1.394 9.393c5.857-5.857 15.355-5.857 21.213 0" />
        </svg>
      </template>
    </PageHeader>

    <!-- Giysi/Tekstil Operasyonları Uyarısı -->
    <div class="rounded-lg bg-amber-50 border border-amber-200 p-3 sm:p-4 mb-2 sm:mb-4 flex items-start gap-3">
      <svg class="w-5 h-5 text-amber-600 mt-0.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
              d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.538-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/>
      </svg>
      <div>
        <p class="text-amber-800 font-semibold text-xs sm:text-sm uppercase tracking-wide">Giysi / Tekstil Uyarısı</p>
        <p class="text-amber-700 text-xs sm:text-sm mt-0.5 leading-relaxed">
          Giysi kategorisindeki siparişler şu an depo hazırlık sürecinde desteklenmemektedir.
          Sistem yöneticinize danışınız.
        </p>
      </div>
    </div>

    <!-- Import Controls -->
    <div class="bg-white dark:bg-gray-900 p-4 rounded shadow">
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 items-end">
        <BaseInput type="date" label="Başlangıç" v-model="startDate" size="sm" />
        <BaseInput type="date" label="Bitiş" v-model="endDate" size="sm" />
        <BaseButton @click="importOrders" :loading="importing" variant="primary" class="w-full">
          Aktarımı Başlat
        </BaseButton>

        <div class="flex gap-2 w-full">
            <button
                @click="syncProjects"
                class="flex-1 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 px-3 py-2 rounded hover:bg-gray-200 dark:hover:bg-gray-700 flex items-center justify-center gap-2 text-sm font-medium transition-colors"
                title="Projeleri Senkronize Et"
                :disabled="syncing"
            >
                <span v-if="syncing">...</span>
                <span v-else>Projeler</span>
            </button>

            <button
                @click="checkMappings"
                class="flex-1 bg-teal-50 dark:bg-teal-900/20 text-teal-700 dark:text-teal-400 px-3 py-2 rounded hover:bg-teal-100 dark:hover:bg-teal-900/40 border border-teal-200 dark:border-teal-800 flex items-center justify-center gap-2 text-sm font-medium transition-colors"
                title="Eşleşmeleri Kontrol Et"
                :disabled="checking"
            >
                <span v-if="checking">...</span>
                <span v-else>Kontrol</span>
            </button>

            <button
                @click="checkNetsisTransfers({ fromDate: startDate, toDate: endDate, checkTransferred: true })"
                class="flex-1 bg-indigo-50 dark:bg-indigo-900/20 text-indigo-700 dark:text-indigo-400 px-3 py-2 rounded hover:bg-indigo-100 dark:hover:bg-indigo-900/40 border border-indigo-200 dark:border-indigo-800 flex items-center justify-center gap-2 text-sm font-medium transition-colors"
                :title="`Netsis'te aktarılmış siparişleri kontrol et — silinenleri geri al (${startDate} – ${endDate})`"
                :disabled="checkingNetsis"
            >
                <span v-if="checkingNetsis">...</span>
                <span v-else>Netsis</span>
            </button>
        </div>
      </div>

      <div v-if="importResult" class="mt-4">
        <div class="rounded-lg border p-3" :class="importResult.failedCount > 0 ? 'border-yellow-300 bg-yellow-50 dark:bg-yellow-900/20' : 'border-green-300 bg-green-50 dark:bg-green-900/20'">
          <div class="flex flex-wrap gap-x-4 gap-y-2 text-xs sm:text-sm font-medium">
            <span class="text-gray-600 dark:text-gray-400">Gelen: <strong>{{ importResult.totalFromIss }}</strong></span>
            <span class="text-green-600">Yeni: <strong>{{ importResult.newCount }}</strong></span>
            <span class="text-gray-500">Atlanan: <strong>{{ importResult.skippedCount }}</strong></span>
            <span v-if="importResult.needsMappingCount > 0" class="text-yellow-600">Eşleştirme: <strong>{{ importResult.needsMappingCount }}</strong></span>
            <span v-if="importResult.failedCount > 0" class="text-red-600">Hata: <strong>{{ importResult.failedCount }}</strong></span>
          </div>
          <div v-if="importResult.errors.length > 0" class="mt-2 space-y-1">
            <div v-for="err in importResult.errors.slice(0, 2)" :key="err" class="text-red-600 text-[10px] sm:text-xs italic">{{ err }}</div>
          </div>
        </div>
      </div>
    </div>

    <!-- Netsis Tekil Sipariş Sorgulama -->
    <div class="bg-white dark:bg-gray-900 p-4 rounded shadow">
      <p class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide mb-3">Netsis Tekil Kontrol</p>
      <div class="flex gap-2 items-end">
        <div class="flex-1">
          <label class="block text-xs text-gray-500 dark:text-gray-400 mb-1">Sipariş No (ISS veya Netsis formatında)</label>
          <input
            v-model="singleOrderNumber"
            type="text"
            placeholder="Örn: 202604290926"
            class="w-full border border-gray-300 dark:border-gray-700 rounded px-3 py-2 text-sm bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-400"
            @keydown.enter="checkSingleOrder"
          />
        </div>
        <button
          @click="checkSingleOrder"
          :disabled="!singleOrderNumber.trim() || checkingSingle"
          class="bg-indigo-600 text-white px-4 py-2 rounded text-sm font-semibold hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors shrink-0"
        >
          <span v-if="checkingSingle">Sorgulanıyor...</span>
          <span v-else>Netsis Sorgula</span>
        </button>
      </div>
      <div v-if="singleOrderResult" class="mt-3 rounded-lg border p-3 text-sm"
        :class="{
          'border-green-300 bg-green-50 dark:bg-green-900/20 text-green-800 dark:text-green-200': singleOrderResult.reverted,
          'border-blue-300 bg-blue-50 dark:bg-blue-900/20 text-blue-800 dark:text-blue-200': singleOrderResult.found && singleOrderResult.existsInNetsis,
          'border-amber-300 bg-amber-50 dark:bg-amber-900/20 text-amber-800 dark:text-amber-200': singleOrderResult.found && !singleOrderResult.wasTransferred,
          'border-red-300 bg-red-50 dark:bg-red-900/20 text-red-800 dark:text-red-200': !singleOrderResult.found,
        }">
        {{ singleOrderResult.message }}
        <span v-if="singleOrderResult.externalOrderNumber && singleOrderResult.externalOrderNumber !== singleOrderNumber" class="block text-xs opacity-70 mt-1">
          ISS Sipariş No: {{ singleOrderResult.externalOrderNumber }}
        </span>
      </div>
    </div>

    <!-- Tabs & Content -->
    <div class="grid grid-cols-1 lg:grid-cols-3 gap-4 sm:gap-6">
        <!-- Eşleştirme Bekleyen sekmesinde stok eşleştirme sayfasına yönlendirme -->
        <div class="lg:col-span-3" v-if="activeTab === 'NeedsMapping'">
            <div class="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-800 rounded-lg p-4 flex items-center justify-between gap-4">
                <div class="flex items-start gap-3">
                    <svg class="w-5 h-5 text-amber-600 dark:text-amber-400 mt-0.5 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                            d="M8 7h12m0 0l-4-4m4 4l-4 4m0 6H4m0 0l4 4m-4-4l4-4" />
                    </svg>
                    <div>
                        <p class="text-amber-800 dark:text-amber-300 font-semibold text-sm">Stok eşleştirme gerekiyor</p>
                        <p class="text-amber-700 dark:text-amber-400 text-xs mt-0.5">
                            Bu sipariş(ler)de henüz sistem stok kartına eşleştirilmemiş ISS kalemleri var.
                            Eşleştirme yaptıktan sonra siparişler "Hazır" sekmesine geçecek.
                        </p>
                    </div>
                </div>
                <router-link to="/stocks/mappings"
                    class="shrink-0 px-4 py-2 bg-teal-600 text-white rounded-lg hover:bg-teal-700 text-sm font-semibold transition-colors whitespace-nowrap">
                    Eşleştirme Sayfası →
                </router-link>
            </div>
        </div>

        <div class="lg:col-span-3 bg-white dark:bg-gray-900 rounded-lg shadow-sm border dark:border-gray-800">
            <!-- Tabs Bar -->
            <div class="border-b dark:border-gray-800 flex items-center justify-between">
                <div class="flex overflow-x-auto whitespace-nowrap scrollbar-hide">
                    <button
                        v-for="tab in [{id:'Ready', label:'Hazır', count:readyCount, color:'green'}, {id:'NeedsMapping', label:'Eşleştirme', count:needsMappingCount, color:'red'}, {id:'Passive', label:'Pasif', count:passiveCount, color:'gray'}, {id:'History', label:'Geçmiş', count:null, color:'indigo'}]"
                        :key="tab.id"
                        @click="tab.id === 'History' ? loadHistory() : (activeTab = tab.id as any, page = 1, clearSelection())"
                        class="py-4 px-4 sm:px-6 border-b-2 font-semibold text-xs sm:text-sm transition-all relative"
                        :class="activeTab === tab.id 
                            ? `border-${tab.color}-500 text-${tab.color}-600 bg-${tab.color}-50/30` 
                            : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200'"
                    >
                        {{ tab.label }}
                        <span v-if="tab.count !== null" class="ml-1 text-[10px] opacity-70">({{ tab.count }})</span>
                    </button>
                </div>
            </div>

            <!-- Filter & Action Bar -->
            <div class="p-3 sm:p-4 bg-gray-50/50 dark:bg-gray-800/30 border-b dark:border-gray-800 flex flex-col sm:flex-row gap-3 sm:items-center justify-between">
                <div class="flex items-center gap-2 flex-wrap">
                    <!-- Bulk Action Buttons -->
                    <template v-if="selectedIds.size > 0">
                        <button
                            v-if="activeTab === 'Ready'"
                            @click="createBulkShipments"
                            class="bg-blue-600 text-white px-3 py-2 rounded-lg shadow-sm hover:bg-blue-700 font-bold text-xs flex items-center gap-2 transition-transform active:scale-95"
                        >
                            <span class="bg-white text-blue-600 rounded-full w-5 h-5 flex items-center justify-center text-[10px]">{{ selectedIds.size }}</span>
                            <span>SEÇİLENLERİ OLUŞTUR</span>
                        </button>
                        <button
                            v-if="activeTab === 'Ready' || activeTab === 'NeedsMapping'"
                            @click="bulkDeactivateSelected"
                            :disabled="deactivating"
                            class="bg-red-500 text-white px-3 py-2 rounded-lg shadow-sm hover:bg-red-600 font-bold text-xs flex items-center gap-2 transition-transform active:scale-95 disabled:opacity-50"
                        >
                            <span class="bg-white text-red-500 rounded-full w-5 h-5 flex items-center justify-center text-[10px]">{{ selectedIds.size }}</span>
                            <span>{{ deactivating ? 'PASİFE ALINIYOR...' : 'PASİFE AL' }}</span>
                        </button>
                    </template>
                    <div v-else class="text-gray-400 text-xs italic sm:block hidden">Sipariş listesini aşağıdan yönetebilirsiniz.</div>
                </div>

                <div class="flex flex-wrap gap-2">
                    <BaseInput
                        v-model="zoneSearch"
                        @input="handleSearch"
                        placeholder="Bölge..."
                        size="sm"
                        class="flex-1 min-w-[120px]"
                    />
                    <BaseSelect v-model="operationTypeFilter" @change="loadOrders" size="sm">
                        <option value="">İş Türü (Tümü)</option>
                        <option value="Catering">Catering</option>
                        <option value="Clothing">Kıyafet</option>
                    </BaseSelect>
                    <BaseInput
                        v-model="searchQuery"
                        @input="handleSearch"
                        placeholder="Sipariş / Proje / Talep..."
                        size="sm"
                        class="flex-[2] min-w-[160px]"
                    />
                </div>
            </div>

                     <!-- History Tab -->
                     <div v-if="activeTab === 'History'" class="p-4">
                         <div v-if="historyLoading" class="text-center py-8 dark:text-gray-400 font-medium">Veriler Yükleniyor...</div>
                         <div v-else-if="batches.length === 0" class="text-center py-8 text-gray-500 dark:text-gray-400">Henüz import geçmişi bulunamadı.</div>
                         <div v-else>
                             <!-- Desktop Table -->
                             <div class="hidden md:block overflow-x-auto">
                                 <table class="w-full text-xs text-left border-collapse">
                                     <thead>
                                         <tr class="bg-gray-50 dark:bg-gray-800/50 text-[10px] font-bold uppercase text-gray-400 tracking-wider border-b dark:border-gray-700">
                                             <th class="p-3">Başlatılma Zamanı</th>
                                             <th class="p-3">Talep Aralığı</th>
                                             <th class="p-3 text-center">Toplam</th>
                                             <th class="p-3 text-center text-green-600">Yeni</th>
                                             <th class="p-3 text-center text-yellow-600">Eşl. Bekl.</th>
                                             <th class="p-3 text-center text-red-600">Hata</th>
                                             <th class="p-3 text-center">Süre</th>
                                             <th class="p-3 text-right">Durum</th>
                                         </tr>
                                     </thead>
                                     <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
                                         <tr v-for="b in batches" :key="b.id" class="hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
                                             <td class="p-3 dark:text-gray-300">{{ formatDate(b.startedAt) }} {{ new Date(b.startedAt).toLocaleTimeString('tr-TR', {hour:'2-digit',minute:'2-digit'}) }}</td>
                                             <td class="p-3 text-gray-500 dark:text-gray-400 italic">{{ formatDate(b.requestedStartDate) }} – {{ formatDate(b.requestedEndDate) }}</td>
                                             <td class="p-3 text-center font-bold dark:text-gray-300">{{ b.totalFromSource }}</td>
                                             <td class="p-3 text-center text-green-600 font-bold">{{ b.newCount }}</td>
                                             <td class="p-3 text-center font-bold" :class="b.needsMappingCount > 0 ? 'text-yellow-600' : 'text-gray-400'">{{ b.needsMappingCount }}</td>
                                             <td class="p-3 text-center font-bold" :class="b.failedCount > 0 ? 'text-red-600' : 'text-gray-400'">{{ b.failedCount }}</td>
                                             <td class="p-3 text-center text-[10px] text-gray-400">{{ (b.durationMs / 1000).toFixed(1) }}s</td>
                                             <td class="p-3 text-right">
                                                 <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-tight"
                                                     :class="{
                                                         'bg-green-100 text-green-700 border border-green-200': b.status === 'Completed',
                                                         'bg-yellow-100 text-yellow-700 border border-yellow-200': b.status === 'PartialSuccess',
                                                         'bg-red-100 text-red-700 border border-red-200': b.status === 'Failed',
                                                         'bg-blue-100 text-blue-700 border border-blue-200': b.status === 'Running'
                                                     }">
                                                     {{ b.status === 'Completed' ? 'TAMAM' : b.status === 'PartialSuccess' ? 'KISMİ' : b.status === 'Failed' ? 'HATA' : 'AKTARILIYOR' }}
                                                 </span>
                                             </td>
                                         </tr>
                                     </tbody>
                                 </table>
                             </div>

                             <!-- Mobile List -->
                             <div class="md:hidden space-y-3">
                                 <div v-for="b in batches" :key="b.id" class="bg-gray-50 dark:bg-gray-800 p-4 rounded-xl border dark:border-gray-700">
                                     <div class="flex justify-between items-start mb-3">
                                         <div>
                                             <div class="text-[11px] font-bold text-gray-900 dark:text-gray-100">{{ formatDate(b.startedAt) }} — {{ new Date(b.startedAt).toLocaleTimeString('tr-TR', {hour:'2-digit',minute:'2-digit'}) }}</div>
                                             <div class="text-[10px] text-gray-500 dark:text-gray-400">{{ formatDate(b.requestedStartDate) }} - {{ formatDate(b.requestedEndDate) }}</div>
                                         </div>
                                         <span class="px-2 py-0.5 rounded text-[9px] font-bold uppercase"
                                             :class="{
                                                 'bg-green-100 text-green-700': b.status === 'Completed',
                                                 'bg-yellow-100 text-yellow-700': b.status === 'PartialSuccess',
                                                 'bg-red-100 text-red-700': b.status === 'Failed',
                                                 'bg-blue-100 text-blue-700': b.status === 'Running'
                                             }">
                                             {{ b.status === 'Completed' ? 'Tamam' : b.status === 'PartialSuccess' ? 'Kısmi' : b.status === 'Failed' ? 'Hata' : '...' }}
                                         </span>
                                     </div>
                                     <div class="grid grid-cols-4 gap-2 text-center">
                                         <div class="bg-white dark:bg-gray-900 p-1.5 rounded border dark:border-gray-700">
                                             <div class="text-[9px] text-gray-400 uppercase font-bold">Gelen</div>
                                             <div class="text-xs font-bold dark:text-gray-200">{{ b.totalFromSource }}</div>
                                         </div>
                                         <div class="bg-white dark:bg-gray-900 p-1.5 rounded border dark:border-gray-700">
                                             <div class="text-[9px] text-green-400 uppercase font-bold">Yeni</div>
                                             <div class="text-xs font-bold text-green-600">{{ b.newCount }}</div>
                                         </div>
                                         <div class="bg-white dark:bg-gray-900 p-1.5 rounded border dark:border-gray-700">
                                             <div class="text-[9px] text-yellow-400 uppercase font-bold">Eşl.</div>
                                             <div class="text-xs font-bold text-yellow-600">{{ b.needsMappingCount }}</div>
                                         </div>
                                         <div class="bg-white dark:bg-gray-900 p-1.5 rounded border dark:border-gray-700">
                                             <div class="text-[9px] text-red-400 uppercase font-bold">Hata</div>
                                             <div class="text-xs font-bold text-red-600">{{ b.failedCount }}</div>
                                         </div>
                                     </div>
                                 </div>
                             </div>
                         </div>
                     </div>

                     <div v-else class="p-4">
                         <div v-if="loading" class="text-center py-8 dark:text-gray-400">Yükleniyor...</div>
                         <div v-else-if="orders.length === 0" class="text-center py-8 text-gray-500 dark:text-gray-400">
                             Listenizde sipariş bulunamadı.
                         </div>

                        <div v-else>
                            <!-- Desktop Table View -->
                            <div class="hidden sm:block overflow-x-auto">
                                <table class="w-full text-left border-collapse">
                                    <thead>
                                        <tr class="bg-gray-50 dark:bg-gray-800 text-[10px] font-bold uppercase text-gray-500 tracking-wider">
                                            <th class="p-4 w-10 text-center">
                                                <input
                                                    type="checkbox"
                                                    :checked="isAllSelected"
                                                    @change="toggleSelectAll"
                                                    class="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500 cursor-pointer"
                                                />
                                            </th>
                                            <th class="p-4">Sipariş No</th>
                                            <th class="p-4">Talep No</th>
                                            <th class="p-4">Proje Bilgisi</th>
                                            <th class="p-4">Bölge</th>
                                            <th class="p-4">Sipariş T.</th>
                                            <th class="p-4">Termin</th>
                                            <th class="p-4">Kalem</th>
                                            <th class="p-4 text-right">İşlem</th>
                                        </tr>
                                    </thead>
                                    <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
                                        <tr v-for="order in orders" :key="order.id || order.Id" 
                                            class="hover:bg-gray-50 dark:hover:bg-gray-800/50 transition-colors group" 
                                            :class="{'bg-blue-50/50 dark:bg-blue-900/10': selectedIds.has(order.id || order.Id)}"
                                        >
                                            <td class="p-4 text-center">
                                                <input
                                                    type="checkbox"
                                                    :checked="selectedIds.has(order.id || order.Id)"
                                                    @change="toggleSelection(order.id || order.Id)"
                                                    class="h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-500 cursor-pointer"
                                                />
                                            </td>
                                            <td class="p-4">
                                                <div class="font-bold text-gray-900 dark:text-gray-100">{{ order.externalOrderNumber || order.ExternalOrderNumber }}</div>
                                            </td>
                                            <td class="p-4">
                                                <span class="inline-block px-2 py-1 rounded-md bg-indigo-50 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300 font-extrabold text-sm tracking-tight">
                                                    {{ (order.talepNo || order.TalepNo) || '-' }}
                                                </span>
                                            </td>
                                            <td class="p-4">
                                                <div class="text-[10px] text-gray-400 uppercase font-bold">{{ order.institutionCode || order.InstitutionCode }}</div>
                                                <div class="text-xs font-semibold dark:text-gray-200 flex items-center gap-1.5">
                                                    {{ order.projectCode || order.ProjectCode }}
                                                    <span v-if="order.isClothing" class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-bold bg-purple-100 text-purple-700 dark:bg-purple-900/40 dark:text-purple-300 border border-purple-200 dark:border-purple-700">Kıyafet</span>
                                                </div>
                                                <div class="text-xs text-gray-500">{{ order.projectName || order.ProjectName }}</div>
                                                <div v-if="order.aciklama || order.Aciklama" class="text-[10px] text-indigo-600 dark:text-indigo-400 truncate max-w-[150px] mt-0.5" :title="order.aciklama || order.Aciklama">
                                                    {{ order.aciklama || order.Aciklama }}
                                                </div>
                                            </td>
                                            <td class="p-4 text-xs font-medium dark:text-gray-300">{{ order.region || order.Region }}</td>
                                            <td class="p-4 text-xs dark:text-gray-400">{{ formatDate(order.orderDate || order.OrderDate) }}</td>
                                            <td class="p-4 text-xs font-semibold text-emerald-700 dark:text-emerald-400">{{ formatDate(order.deliveryDate || order.DeliveryDate) }}</td>
                                            <td class="p-4">
                                                <span class="px-2 py-0.5 bg-gray-100 dark:bg-gray-800 rounded-full text-[10px] font-bold text-gray-600 dark:text-gray-400">
                                                    {{ order.lineCount || order.LineCount }} Kalem
                                                </span>
                                            </td>
                                            <td class="p-4 text-right">
                                                <div class="flex justify-end gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                                                    <button @click="openDetail(order)" class="p-1.5 text-gray-500 hover:text-indigo-600 hover:bg-indigo-50 dark:hover:bg-indigo-900/20 rounded" title="Detay"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"/><path d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"/></svg></button>
                                                    <template v-if="activeTab === 'Ready'">
                                                        <button @click="createShipment(order.id || order.Id)" class="p-1.5 text-blue-500 hover:text-blue-600 hover:bg-blue-50 dark:hover:bg-blue-900/20 rounded" title="Sevkiyat Oluştur"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="M13 10V3L4 14h7v7l9-11h-7z" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"/></svg></button>
                                                        <button @click="toggleActive(order.id || order.Id, false)" class="p-1.5 text-amber-500 hover:text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-900/20 rounded" title="Pasife Al"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"/></svg></button>
                                                    </template>
                                                    <template v-else-if="activeTab === 'Passive'">
                                                        <button @click="toggleActive(order.id || order.Id, true)" class="p-1.5 text-green-500 hover:text-green-600 hover:bg-green-50 dark:hover:bg-green-900/20 rounded" title="Aktife Al"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="M5 13l4 4L19 7" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"/></svg></button>
                                                    </template>
                                                </div>
                                                <!-- Mobile actions (shown when not hovered to ensure accessibility) -->
                                                <div class="sm:hidden flex justify-end gap-2">
                                                     <button @click="openDetail(order)" class="text-xs text-indigo-600 font-bold uppercase">Detay</button>
                                                </div>
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>

                            <!-- Mobile Card View -->
                            <div class="sm:hidden space-y-3">
                                <div v-for="order in orders" :key="order.id || order.Id" 
                                    class="bg-white dark:bg-gray-800 border dark:border-gray-700 rounded-xl p-4 shadow-sm active:bg-gray-50 transition-colors relative overflow-hidden"
                                    :class="{'ring-2 ring-blue-500 bg-blue-50/30': selectedIds.has(order.id || order.Id)}"
                                >
                                    <div class="flex justify-between items-start mb-3">
                                        <div class="flex items-start gap-3">
                                            <input
                                                type="checkbox"
                                                :checked="selectedIds.has(order.id || order.Id)"
                                                @change="toggleSelection(order.id || order.Id)"
                                                class="mt-1 h-5 w-5 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                                            />
                                            <div>
                                                <div class="font-bold text-gray-900 dark:text-gray-100 flex items-center gap-2 flex-wrap">
                                                    {{ order.externalOrderNumber || order.ExternalOrderNumber }}
                                                    <span class="text-sm font-extrabold bg-indigo-50 dark:bg-indigo-900/30 text-indigo-700 dark:text-indigo-300 px-2 py-0.5 rounded-md tracking-tight">T: {{ order.talepNo || order.TalepNo || '-' }}</span>
                                                </div>
                                                <div class="text-[11px] text-gray-500 dark:text-gray-400 mt-1 font-medium flex items-center gap-1.5 flex-wrap">
                                                    <span>{{ order.projectCode || order.ProjectCode }} — {{ order.projectName || order.ProjectName }}</span>
                                                    <span v-if="order.isClothing" class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-bold bg-purple-100 text-purple-700 dark:bg-purple-900/40 dark:text-purple-300 border border-purple-200 dark:border-purple-700">Kıyafet</span>
                                                </div>
                                                <div v-if="order.aciklama || order.Aciklama" class="text-[10px] text-indigo-500 dark:text-indigo-400 mt-0.5 line-clamp-1">
                                                    {{ order.aciklama || order.Aciklama }}
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                    
                                    <div class="grid grid-cols-3 gap-2 mb-4">
                                        <div class="bg-gray-50 dark:bg-gray-900/50 p-2 rounded-lg">
                                            <div class="text-[9px] text-gray-400 uppercase font-bold mb-0.5">Bölge</div>
                                            <div class="text-[11px] font-semibold dark:text-gray-300 truncate">{{ order.region || order.Region }}</div>
                                        </div>
                                        <div class="bg-gray-50 dark:bg-gray-900/50 p-2 rounded-lg">
                                            <div class="text-[9px] text-gray-400 uppercase font-bold mb-0.5">Sipariş T.</div>
                                            <div class="text-[11px] font-semibold dark:text-gray-300">{{ formatDate(order.orderDate || order.OrderDate) }}</div>
                                        </div>
                                        <div class="bg-gray-50 dark:bg-gray-900/50 p-2 rounded-lg">
                                            <div class="text-[9px] text-gray-400 uppercase font-bold mb-0.5">Termin</div>
                                            <div class="text-[11px] font-semibold text-emerald-700 dark:text-emerald-400">{{ formatDate(order.deliveryDate || order.DeliveryDate) }}</div>
                                        </div>
                                    </div>

                                    <div class="flex items-center justify-between pt-3 border-t dark:border-gray-700">
                                        <span class="text-[10px] font-bold text-gray-400 uppercase">{{ order.lineCount || order.LineCount }} Kalem Ürün</span>
                                        <div class="flex gap-2">
                                            <button @click="openDetail(order)" class="px-3 py-1.5 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-200 rounded-lg text-xs font-bold transition-colors active:bg-gray-200">DETAY</button>
                                            <template v-if="activeTab === 'Ready'">
                                                <button @click="createShipment(order.id || order.Id)" class="px-3 py-1.5 bg-blue-600 text-white rounded-lg text-xs font-bold shadow-sm shadow-blue-500/20 active:bg-blue-700">SEVKİYAT</button>
                                            </template>
                                            <template v-else-if="activeTab === 'Passive'">
                                                <button @click="toggleActive(order.id || order.Id, true)" class="px-3 py-1.5 bg-green-600 text-white rounded-lg text-xs font-bold shadow-sm shadow-green-500/20 active:bg-green-700">AKTİVE ET</button>
                                            </template>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>

                     <!-- Pagination -->
                     <div class="mt-6 flex flex-col sm:flex-row justify-between items-center gap-4 border-t dark:border-gray-800 pt-4">
                        <div class="text-xs sm:text-sm text-gray-500 dark:text-gray-400 font-medium order-2 sm:order-1">
                            Toplam <span class="text-gray-900 dark:text-gray-100 font-bold">{{ totalCount }}</span> kayıt, Sayfa {{ page }} / {{ totalPages }}
                        </div>
                        <div class="flex gap-2 order-1 sm:order-2 w-full sm:w-auto">
                            <BaseButton @click="page--" :disabled="page <= 1" variant="secondary" size="sm" class="flex-1 sm:flex-none">← Önceki</BaseButton>
                            <BaseButton @click="page++" :disabled="page >= totalPages" variant="secondary" size="sm" class="flex-1 sm:flex-none">Sonraki →</BaseButton>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <!-- Detail Modal -->
        <div v-if="selectedOrder" class="fixed inset-0 bg-gray-900/60 backdrop-blur-sm flex items-end sm:items-center justify-center p-0 sm:p-4 z-50">
            <div class="bg-white dark:bg-gray-900 rounded-t-2xl sm:rounded-2xl shadow-2xl w-full max-w-2xl max-h-[95vh] flex flex-col overflow-hidden animate-in fade-in slide-in-from-bottom duration-300">
                <div class="px-5 py-4 border-b dark:border-gray-800 flex justify-between items-center bg-white dark:bg-gray-900">
                    <div>
                        <h3 class="text-lg font-bold dark:text-gray-100 leading-none">Sipariş Detayı</h3>
                        <span class="text-[10px] text-gray-400 font-bold uppercase tracking-wider">#{{ selectedOrder.externalOrderNumber }}</span>
                    </div>
                    <button @click="selectedOrder = null" class="p-2 text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 transition-colors">
                        <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="M6 18L18 6M6 6l12 12" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"/></svg>
                    </button>
                </div>
                
                <div class="p-5 sm:p-6 space-y-5 overflow-y-auto custom-scrollbar">
                     <div class="grid grid-cols-1 sm:grid-cols-2 gap-5 text-sm">
                         <div class="bg-gray-50 dark:bg-gray-800/50 p-3 rounded-xl border dark:border-gray-700">
                             <span class="block text-gray-400 text-[10px] font-bold uppercase mb-1">Talep No / Türü</span>
                             <span class="font-bold dark:text-gray-100 block">{{ selectedOrder.talepNo || '-' }}</span>
                             <span class="text-xs text-gray-500">{{ selectedOrder.talepTuru || '-' }}</span>
                         </div>
                         <div class="bg-gray-50 dark:bg-gray-800/50 p-3 rounded-xl border dark:border-gray-700">
                             <span class="block text-gray-400 text-[10px] font-bold uppercase mb-1">Proje</span>
                             <span class="font-bold dark:text-gray-100 block">{{ selectedOrder.projectName }}</span>
                             <span class="text-xs text-gray-500">{{ selectedOrder.projectCode }}</span>
                         </div>
                         <div class="col-span-full">
                             <span class="block text-amber-600 dark:text-amber-400 text-xs font-extrabold uppercase tracking-wider mb-1">Açıklama / Not</span>
                             <div
                                 class="p-4 rounded-xl leading-relaxed border-2 min-h-[64px] whitespace-pre-wrap break-words"
                                 :class="selectedOrder.aciklama
                                     ? 'bg-amber-50 dark:bg-amber-900/20 border-amber-300 dark:border-amber-700 text-amber-900 dark:text-amber-200 text-base font-semibold'
                                     : 'bg-gray-50 dark:bg-gray-800/50 border-gray-200 dark:border-gray-700 text-gray-400 text-sm italic'"
                             >
                                 {{ selectedOrder.aciklama || 'Açıklama bulunmuyor.' }}
                             </div>
                         </div>

                         <!-- Malzemeler -->
                         <div class="col-span-full border-t dark:border-gray-800 pt-4 mt-2">
                             <h4 class="text-xs font-bold text-indigo-500 uppercase tracking-widest mb-3">Malzemeler</h4>

                             <!-- Loading -->
                             <div v-if="detailLoading" class="flex items-center gap-2 text-sm text-gray-400 py-4">
                                 <svg class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                                     <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                                     <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
                                 </svg>
                                 Yükleniyor...
                             </div>

                             <!-- Empty -->
                             <p v-else-if="!selectedOrder.lines?.length" class="text-sm text-gray-400 italic py-2">Malzeme kaydı bulunamadı.</p>

                             <!-- Table -->
                             <div v-else class="overflow-x-auto rounded-xl border border-gray-200 dark:border-gray-700">
                                 <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
                                     <thead class="bg-gray-50 dark:bg-gray-800">
                                         <tr>
                                             <th class="px-3 py-2 text-left text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider w-8">#</th>
                                             <th class="px-3 py-2 text-left text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Stok Kodu</th>
                                             <th class="px-3 py-2 text-left text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Stok Adı</th>
                                             <th class="px-3 py-2 text-right text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Miktar</th>
                                             <th class="px-3 py-2 text-right text-[10px] font-bold text-gray-500 dark:text-gray-400 uppercase tracking-wider">Birim</th>
                                         </tr>
                                     </thead>
                                     <tbody class="divide-y divide-gray-100 dark:divide-gray-800 bg-white dark:bg-gray-900">
                                         <tr v-for="line in selectedOrder.lines" :key="line.id" class="hover:bg-gray-50 dark:hover:bg-gray-800/50">
                                             <td class="px-3 py-2 text-gray-400 dark:text-gray-600 text-xs">{{ line.lineNumber }}</td>
                                             <td class="px-3 py-2 font-mono text-xs text-gray-600 dark:text-gray-400">{{ line.stockCode }}</td>
                                             <td class="px-3 py-2 text-gray-900 dark:text-gray-100 font-medium">{{ line.stockName }}</td>
                                             <td class="px-3 py-2 text-right font-bold text-gray-900 dark:text-gray-100">{{ line.orderedQty }}</td>
                                             <td class="px-3 py-2 text-right text-xs text-gray-500 dark:text-gray-400">{{ line.unit }}</td>
                                         </tr>
                                     </tbody>
                                 </table>
                             </div>
                         </div>

                         <div class="col-span-full border-t dark:border-gray-800 pt-4 mt-2">
                             <h4 class="text-xs font-bold text-indigo-500 uppercase tracking-widest mb-3">İletişim & Teslimat</h4>
                             <div class="space-y-4">
                                <div class="flex items-start gap-4">
                                    <div class="p-2 bg-blue-50 dark:bg-blue-900/20 text-blue-500 rounded-lg"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"/></svg></div>
                                    <div>
                                        <span class="block text-[10px] text-gray-400 font-bold">ALACAK KİŞİLER</span>
                                        <span class="text-sm dark:text-gray-200">{{ selectedOrder.teslimAlacakKisiler || '-' }}</span>
                                    </div>
                                </div>
                                <div class="flex items-start gap-4">
                                    <div class="p-2 bg-green-50 dark:bg-green-900/20 text-green-500 rounded-lg"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="M3 5a2 2 0 012-2h3.28a1 1 0 01.948.684l1.498 4.493a1 1 0 01-.502 1.21l-2.257 1.13a11.042 11.042 0 005.516 5.516l1.13-2.257a1 1 0 011.21-.502l4.493 1.498a1 1 0 01.684.949V19a2 2 0 01-2 2h-1C9.716 21 3 14.284 3 6V5z" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"/></svg></div>
                                    <div>
                                        <span class="block text-[10px] text-gray-400 font-bold">TELEFONLAR</span>
                                        <span class="text-sm dark:text-gray-200">{{ selectedOrder.teslimAlacakTelefonNumaralari || '-' }}</span>
                                    </div>
                                </div>
                                <div class="flex items-start gap-4">
                                    <div class="p-2 bg-amber-50 dark:bg-amber-900/20 text-amber-500 rounded-lg"><svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"/></svg></div>
                                    <div>
                                        <span class="block text-[10px] text-gray-400 font-bold">YÖNETİCİ MAİLLERİ</span>
                                        <span class="text-xs dark:text-gray-300 break-all leading-relaxed">{{ selectedOrder.yoneticiMailAdresleri || '-' }}</span>
                                    </div>
                                </div>
                             </div>
                         </div>
                     </div>
                </div>
                
                <div class="p-4 bg-gray-50 dark:bg-gray-800/80 border-t dark:border-gray-800 flex gap-3">
                    <BaseButton @click="selectedOrder = null" variant="secondary" class="flex-1">Kapat</BaseButton>
                    <template v-if="activeTab === 'Ready'">
                        <BaseButton @click="createShipment(selectedOrder.id || selectedOrder.Id)" variant="primary" class="flex-1">Sevkiyat Oluştur</BaseButton>
                    </template>
                </div>
            </div>
        </div>

        <!-- Adres değişikliği uyarı modalı (proje senkronizasyonu) -->
        <div v-if="showAddressChanges" class="fixed inset-0 bg-black/50 flex items-center justify-center p-4 z-50">
            <div class="bg-white dark:bg-gray-900 rounded-xl w-full max-w-2xl max-h-[85vh] flex flex-col shadow-xl">
                <div class="px-5 py-4 border-b dark:border-gray-700 flex items-center justify-between">
                    <div>
                        <h3 class="text-lg font-bold text-gray-900 dark:text-gray-100">Adres Değişiklikleri</h3>
                        <p class="text-sm text-gray-500 dark:text-gray-400">{{ addressChanges.length }} projenin adresi ISS'te değişmiş</p>
                    </div>
                    <button @click="showAddressChanges = false" class="p-1.5 rounded-lg text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800">✕</button>
                </div>
                <div class="overflow-y-auto p-5 space-y-3">
                    <div v-for="c in addressChanges" :key="c.projectId" class="border dark:border-gray-700 rounded-lg p-3">
                        <div class="flex items-center gap-2 mb-2">
                            <span class="font-mono text-xs font-bold text-gray-800 dark:text-gray-200">{{ c.projectCode }}</span>
                            <span class="text-sm font-medium text-gray-700 dark:text-gray-300 truncate">{{ c.projectName }}</span>
                        </div>
                        <div class="text-xs space-y-1">
                            <div class="flex gap-2">
                                <span class="shrink-0 text-gray-400 w-12">Eski:</span>
                                <span class="text-red-600 dark:text-red-400 line-through">{{ c.oldAddress || '(boş)' }}</span>
                            </div>
                            <div class="flex gap-2">
                                <span class="shrink-0 text-gray-400 w-12">Yeni:</span>
                                <span class="text-green-700 dark:text-green-400 font-medium">{{ c.newAddress || '(boş)' }}</span>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="px-5 py-3 border-t dark:border-gray-700 flex justify-between items-center">
                    <p class="text-xs text-gray-400">Adresi değişen projelerin koordinatlarını Koordinat Doğrulama'dan kontrol edin.</p>
                    <button @click="showAddressChanges = false" class="px-4 py-2 bg-indigo-600 text-white rounded-lg text-sm font-medium hover:bg-indigo-700">Tamam</button>
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, computed, onUnmounted } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import shipmentService from '../services/shipmentService';
import projectService, { type ProjectAddressChange } from '../services/projectService';
import apiClient from '../services/apiClient';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';
import { formatDate } from '../utils/dateFormat';
import BaseButton from '../components/BaseButton.vue';
import BaseInput from '../components/base/BaseInput.vue';
import BaseSelect from '../components/base/BaseSelect.vue';

const notificationStore = useNotificationStore();
const confirm = {
    show: async (msg: string) => window.confirm(msg)
};

// Simple debounce
function simpleDebounce(fn: Function, delay: number) {
  let timeoutId: any;
  return (...args: any[]) => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(() => fn(...args), delay);
  };
}

const startDate = ref(new Date().toISOString().split('T')[0]);
const endDate = ref(new Date().toISOString().split('T')[0]);

const activeTab = ref('Ready');
const orders = ref<any[]>([]);
const loading = ref(false);
const importing = ref(false);
const importBatchId = ref<number | null>(null);
const importPollInterval = ref<ReturnType<typeof setInterval> | null>(null);
const importResult = ref<null | { totalFromIss: number; newCount: number; skippedCount: number; needsMappingCount: number; failedCount: number; errors: string[] }>(null);
const batches = ref<any[]>([]);
const historyLoading = ref(false);

// Pagination & Search
const page = ref(1);
const pageSize = ref(20);
const totalCount = ref(0);
const totalPages = ref(1);
const searchQuery = ref('');
const zoneSearch = ref(''); // New Zone Filter
const operationTypeFilter = ref(''); // "Catering", "Clothing", ""
const selectedIds = ref(new Set<number>()); // New Selection Set

const readyCount = ref(0);
const needsMappingCount = ref(0);
const passiveCount = ref(0);

const selectedOrder = ref<any>(null);
const detailLoading = ref(false);

const isAllSelected = computed(() => {
    return orders.value.length > 0 && orders.value.every(order => selectedIds.value.has(order.id));
});

const toggleSelection = (id: number) => {
    if (selectedIds.value.has(id)) {
        selectedIds.value.delete(id);
    } else {
        selectedIds.value.add(id);
    }
};

const toggleSelectAll = () => {
    if (isAllSelected.value) {
        // Deselect all visible
        orders.value.forEach(order => selectedIds.value.delete(order.id));
    } else {
        // Select all visible
        orders.value.forEach(order => selectedIds.value.add(order.id));
    }
};

const clearSelection = () => {
    selectedIds.value.clear();
};

const createBulkShipments = async () => {
    if (selectedIds.value.size === 0) return;
    if (!await confirm.show(`${selectedIds.value.size} adet sipariş için sevkiyat oluşturulacak. Onaylıyor musunuz?`)) return;

    try {
        const res = await shipmentService.bulkCreateFromIss({
            issOrderIds: Array.from(selectedIds.value)
        });

        if (res.successCount > 0) {
            notificationStore.add(`${res.successCount} adet sevkiyat başarıyla oluşturuldu.`, 'success');
        }
        if (res.failureCount > 0) {
            const sampleReason = res.failures[0]?.reason || 'Bilinmeyen hata';
            notificationStore.add(
                `${res.failureCount} sipariş aktarılamadı. Örn: ${sampleReason}`,
                res.successCount > 0 ? 'warning' : 'error'
            );
        }
        if (res.successCount === 0 && res.failureCount === 0) {
            notificationStore.add('İşlenecek uygun sipariş bulunamadı.', 'info');
        }

        // Reload and clear selection
        await loadOrders();
        clearSelection();
    } catch (e) {
        console.error(e);
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Toplu oluşturma sırasında hata oluştu.', 'error');
    }
};

const deactivating = ref(false);
const bulkDeactivateSelected = async () => {
    if (selectedIds.value.size === 0) return;
    if (!await confirm.show(`${selectedIds.value.size} adet sipariş pasife alınacak. Bu siparişler artık aktif listede görünmeyecek. Onaylıyor musunuz?`)) return;

    deactivating.value = true;
    try {
        const res = await shipmentService.bulkDeactivateOrders(Array.from(selectedIds.value));
        notificationStore.add(`${res.count} adet sipariş pasife alındı.`, 'success');
        await loadOrders();
        clearSelection();
    } catch (e) {
        console.error(e);
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Pasife alma sırasında hata oluştu.', 'error');
    } finally {
        deactivating.value = false;
    }
};

const fetchCounts = async () => {
    try {
        const data = await shipmentService.getIssOrderCounts();
        readyCount.value = data.readyCount;
        needsMappingCount.value = data.needsMappingCount;
        passiveCount.value = data.passiveCount;
    } catch (e) {
        console.error(e);
    }
};

const loadOrders = async () => {
    loading.value = true;
    try {
        const data = await shipmentService.getIssOrders({
            tab: activeTab.value,
            page: page.value,
            pageSize: pageSize.value,
            search: searchQuery.value,
            zone: zoneSearch.value,
            operationType: operationTypeFilter.value
        });

        orders.value = data.items;
        totalCount.value = data.totalCount;
        totalPages.value = data.totalPages;

        // Preserve selection across pages?
        // Usually tricky with simple Set, but we can keep them.
        // User asked to select all filtered, which currently only effectively works for visible page.
        // If they navigate, they can keep selecting.

        await fetchCounts();
    } catch (e) {
        console.error(e);
    } finally {
        loading.value = false;
    }
};

const handleSearch = simpleDebounce(() => {
    page.value = 1;
    loadOrders();
}, 500);

const loadHistory = async () => {
    historyLoading.value = true;
    try {
        const data = await shipmentService.getImportBatches();
        batches.value = data.items;
    } catch (e) {
        console.error(e);
    } finally {
        historyLoading.value = false;
    }
};

const stopImportPolling = () => {
    if (importPollInterval.value) {
        clearInterval(importPollInterval.value);
        importPollInterval.value = null;
    }
};

const importOrders = async () => {
    importing.value = true;
    importResult.value = null;
    importBatchId.value = null;
    stopImportPolling();
    try {
        const { batchId } = await shipmentService.startImportAsync({
            startDate: startDate.value || '',
            endDate: endDate.value || ''
        });
        importBatchId.value = batchId;

        // Poll every 3 seconds until batch completes
        importPollInterval.value = setInterval(async () => {
            try {
                const batch = await shipmentService.getImportBatchStatus(batchId);
                const done = batch.status !== 'Running';
                if (done) {
                    stopImportPolling();
                    importing.value = false;
                    importResult.value = {
                        totalFromIss: batch.totalFromSource,
                        newCount: batch.newCount,
                        skippedCount: batch.skippedCount,
                        needsMappingCount: batch.needsMappingCount,
                        failedCount: batch.failedCount,
                        errors: batch.errorSummary ? [batch.errorSummary] : []
                    };
                    page.value = 1;
                    await loadOrders();
                    await loadHistory();
                    // Netsis'e aktarılmış siparişleri otomatik gizle (aktarılan tarih aralığıyla)
                    checkNetsisTransfers({ fromDate: startDate.value, toDate: endDate.value });
                }
            } catch (e) {
                console.error('Batch poll error:', e);
                // Keep polling; transient errors should not stop monitoring
            }
        }, 3000);
    } catch (e) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Aktarım başlatılamadı', 'error');
        console.error(e);
        importing.value = false;
    }
};

onUnmounted(stopImportPolling);

const syncing = ref(false);
const addressChanges = ref<ProjectAddressChange[]>([]);
const showAddressChanges = ref(false);
const syncProjects = () => {
    if (syncing.value) return;
    syncing.value = true;
    notificationStore.add('Proje senkronizasyonu arka planda devam ediyor...', 'info');
    projectService.syncProjects({ forceAll: false }, { timeout: 0 })
        .then(res => {
            notificationStore.add(`${res.count} proje senkronize edildi.`, 'success');
            addressChanges.value = res.addressChanges ?? [];
            if (addressChanges.value.length > 0) {
                showAddressChanges.value = true;
                notificationStore.add(`${addressChanges.value.length} projenin adresi değişti.`, 'warning');
            }
            loadOrders();
        })
        .catch(e => {
            notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Proje senkronizasyonu başarısız.', 'error');
        })
        .finally(() => { syncing.value = false; });
};

const singleOrderNumber = ref('');
const checkingSingle = ref(false);
const singleOrderResult = ref<{ found: boolean; wasTransferred: boolean; existsInNetsis: boolean; reverted: boolean; message: string; externalOrderNumber?: string } | null>(null);

const checkSingleOrder = async () => {
    const num = singleOrderNumber.value.trim();
    if (!num || checkingSingle.value) return;
    checkingSingle.value = true;
    singleOrderResult.value = null;
    try {
        const result = await shipmentService.checkSingleOrderNetsis(num);
        singleOrderResult.value = result;
        if (result.reverted) {
            notificationStore.add(`Sipariş Netsis'te bulunamadı ve aktarım bilgisi sıfırlandı.`, 'warning');
            loadOrders();
        }
    } catch (e) {
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Sorgu sırasında hata oluştu.', 'error');
    } finally {
        checkingSingle.value = false;
    }
};

const checkingNetsis = ref(false);
const checkNetsisTransfers = (opts?: { fromDate?: string; toDate?: string; checkTransferred?: boolean }) => {
    if (checkingNetsis.value) return;
    checkingNetsis.value = true;
    const rangeLabel = opts?.fromDate
        ? ` (${opts.fromDate}${opts.toDate && opts.toDate !== opts.fromDate ? ' – ' + opts.toDate : ''})`
        : ' (son 30 gün)';
    notificationStore.add(`Netsis transferleri kontrol ediliyor${rangeLabel}, lütfen bekleyiniz...`, 'info');
    shipmentService.checkNetsisTransfers(opts)
        .then((result) => {
            if (result.error) {
                notificationStore.add(`Netsis kontrolü tamamlandı ancak hata oluştu: ${result.error}`, 'warning');
            } else {
                const parts: string[] = [];
                if (result.markedAsTransferred) parts.push(`${result.markedAsTransferred} aktarıldı olarak işaretlendi`);
                if (result.resetToActive) parts.push(`${result.resetToActive} yerel çakışma düzeltildi`);
                if (result.netsisDeletedCount) parts.push(`⚠ ${result.netsisDeletedCount} sipariş Netsis'te bulunamadı ve listeye geri alındı`);
                const summary = parts.length ? parts.join(' • ') : 'Değişiklik yok';
                notificationStore.add(`Netsis kontrolü tamamlandı — ${summary}.`, result.netsisDeletedCount ? 'warning' : 'success');
            }
            loadOrders();
        })
        .catch(e => {
            notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Netsis kontrolü başlatılamadı.', 'error');
        })
        .finally(() => {
            checkingNetsis.value = false;
        });
};

const checking = ref(false);
const checkMappings = async () => {
    checking.value = true;
    try {
        const res = await shipmentService.checkMappings();
        notificationStore.add(`${res.count} siparişin durumu güncellendi.`, 'success');
        await loadOrders();
    } catch (e) {
        console.error(e);
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Hata oluştu.', 'error');
    } finally {
        checking.value = false;
    }
};

const createShipment = async (orderId: number) => {
    if (!await confirm.show('Bu sipariş için sevkiyat oluşturmak istediğinize emin misiniz?')) return;
    try {
        await shipmentService.createShipmentFromIss(orderId);
        notificationStore.add('Sevkiyat başarıyla oluşturuldu.', 'success');
        selectedOrder.value = null;
        await loadOrders();
    } catch (e) {
        console.error(e);
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Sevkiyat oluşturulurken hata oluştu.', 'error');
    }
};

const toggleActive = async (id: number, isActive: boolean) => {
    if (!await confirm.show(isActive ? 'Siparişi aktife almak istiyor musunuz?' : 'Siparişi pasife almak istiyor musunuz?')) return;
    try {
        await shipmentService.toggleIssActive(id, isActive);
        await loadOrders();
    } catch (e) {
        console.error(e);
        notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
    }
};

const openDetail = async (order: any) => {
    selectedOrder.value = { ...order, lines: null };
    detailLoading.value = true;
    try {
        const res = await apiClient.get<any>(`/orders/${order.id || order.Id}`);
        selectedOrder.value = { ...selectedOrder.value, lines: res.data.lines ?? [] };
    } catch {
        selectedOrder.value = { ...selectedOrder.value, lines: [] };
    } finally {
        detailLoading.value = false;
    }
};

watch([activeTab, page], loadOrders);

onMounted(async () => {
    await loadOrders();
    // Sayfa açılışında arka planda sessizce Netsis kontrolü yap (son 30 gün), bitince listeyi yenile
    shipmentService.checkNetsisTransfers()
        .then((result) => {
            if ((result.markedAsTransferred ?? 0) > 0 || (result.resetToActive ?? 0) > 0) {
                loadOrders();
            }
        })
        .catch(() => { /* sessiz hata — kullanıcıyı engelleme */ });
});
</script>
