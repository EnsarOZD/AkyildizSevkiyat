<template>
  <div class="space-y-4">

    <!-- ── Header ── -->
    <PageHeader title="Depo Hazırlık" :subtitle="loading ? 'Yükleniyor...' : activePickingCount > 0 ? `${activePickingCount} bekleyen hazırlık${overdueCount > 0 ? ` • ${overdueCount} gecikmiş` : ''}` : 'Bekleyen hazırlık yok'" color="amber">
      <template #icon>
        <svg class="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
        </svg>
      </template>
      <template #actions>
        <button
          @click="fetchAll"
          :disabled="loading"
          class="p-2 rounded-lg border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-800 text-gray-500 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors disabled:opacity-50"
          title="Yenile"
        >
          <svg :class="loading ? 'animate-spin' : ''" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
          </svg>
        </button>
      </template>
    </PageHeader>

    <!-- ── Tab bar (scrollable on mobile) ── -->
    <div class="overflow-x-auto -mx-4 px-4 sm:mx-0 sm:px-0">
      <div class="flex rounded-lg border border-gray-300 dark:border-gray-700 overflow-hidden text-xs w-max min-w-full sm:w-auto">
        <button
          v-for="tab in tabOptions" :key="tab.key"
          @click="activeTab = tab.key"
          class="relative flex-1 sm:flex-none whitespace-nowrap px-3 py-2 transition-colors"
          :class="activeTab === tab.key
            ? 'bg-blue-600 text-white'
            : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700'"
        >
          {{ tab.label }}
          <span
            v-if="tab.key === 'food' && foodPendingCount > 0"
            class="ml-1 bg-green-500 text-white rounded-full px-1.5 py-0.5 text-[10px] font-bold"
          >{{ foodPendingCount }}</span>
          <span
            v-if="tab.key === 'irsaliye' && irsaliyePendingCount > 0"
            class="ml-1 bg-red-500 text-white rounded-full px-1.5 py-0.5 text-[10px] font-bold"
          >{{ irsaliyePendingCount }}</span>
          <span
            v-if="tab.key === 'vehicle' && vehiclePendingCount > 0"
            class="ml-1 bg-purple-500 text-white rounded-full px-1.5 py-0.5 text-[10px] font-bold"
          >{{ vehiclePendingCount }}</span>
          <span
            v-if="tab.key === 'loading' && loadingPendingCount > 0"
            class="ml-1 bg-emerald-500 text-white rounded-full px-1.5 py-0.5 text-[10px] font-bold"
          >{{ loadingPendingCount }}</span>
        </button>
      </div>
    </div>

    <!-- ── Kritik Stok Widget ── -->
    <CriticalStockWidget :items="criticalStocks" :loading="criticalStocksLoading" />

    <!-- ── Error state ── -->
    <div v-if="error" class="rounded-lg bg-red-50 border border-red-200 p-6 text-center">
      <p class="text-red-700 font-medium">{{ error }}</p>
      <button @click="fetchAll" class="mt-3 text-sm text-red-600 underline hover:text-red-800">
        Tekrar dene
      </button>
    </div>

    <!-- ── Skeleton ── -->
    <div v-if="loading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 animate-pulse">
        <div class="flex items-center justify-between mb-3">
          <div class="h-4 bg-gray-200 dark:bg-gray-700 rounded w-32"></div>
          <div class="h-6 bg-gray-200 dark:bg-gray-700 rounded-full w-20"></div>
        </div>
        <div class="h-3 bg-gray-200 dark:bg-gray-700 rounded w-48 mb-4"></div>
        <div class="h-10 bg-gray-200 dark:bg-gray-700 rounded w-full"></div>
      </div>
    </div>

    <!-- ── Empty state ── -->
    <div
      v-else-if="visibleZones.length === 0"
      class="text-center py-20 bg-white dark:bg-gray-900 rounded-xl border border-dashed border-gray-300 dark:border-gray-700"
    >
      <svg class="mx-auto h-12 w-12 text-gray-300 dark:text-gray-600 mb-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4" />
      </svg>
      <p class="text-gray-500 dark:text-gray-400 font-medium">Son 14 günde bekleyen hazırlık bulunamadı</p>
      <p class="text-gray-400 dark:text-gray-600 text-sm mt-1">Tamamlananları görmek için "Aktifler" butonunu kapatın</p>
    </div>

    <!-- ── Zone Preparation Cards ── -->
    <div v-else class="space-y-3">

      <!-- Date group headers + cards -->
      <template v-for="group in displayGroupedByDate" :key="group.date">

        <!-- Date header -->
        <div class="flex items-center gap-2 pt-1">
          <span class="text-xs font-bold uppercase tracking-wider"
                :class="group.isPast ? 'text-red-500 dark:text-red-400' : group.isToday ? 'text-orange-500 dark:text-orange-400' : 'text-gray-500 dark:text-gray-400'">
            {{ group.label }}
          </span>
          <div class="flex-1 h-px bg-gray-200 dark:bg-gray-800"></div>
          <span v-if="group.isPast" class="text-[10px] font-bold bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 px-1.5 py-0.5 rounded uppercase">GECİKMİŞ</span>
          <span v-else-if="group.isToday" class="text-[10px] font-bold bg-orange-100 dark:bg-orange-900/30 text-orange-600 dark:text-orange-400 px-1.5 py-0.5 rounded uppercase">BUGÜN</span>
        </div>

        <!-- Zone cards for this date -->
        <div v-for="zonePrep in group.zones" :key="zonePrep.id"
             class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden shadow-sm"
             :class="group.isPast && zonePrep.statusId < 5 ? 'border-l-4 border-l-red-400' : group.isToday ? 'border-l-4 border-l-orange-400' : ''">

          <!-- Card header -->
          <div class="px-4 py-3 flex items-start justify-between gap-2 cursor-pointer select-none"
               @click="toggleExpanded(zonePrep.id)">
            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-2 flex-wrap">
                <span class="font-bold text-gray-900 dark:text-gray-100 text-sm truncate">{{ zonePrep.zoneName }}</span>
                <template v-if="zonePrep.mergedIds.length <= 1">
                  <span class="text-[10px] font-bold px-1.5 py-0.5 rounded border flex-shrink-0"
                        :class="zonePrep.batchNo === 1
                          ? 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400 border-gray-300 dark:border-gray-700'
                          : 'bg-orange-50 dark:bg-orange-900/20 text-orange-700 dark:text-orange-400 border-orange-200 dark:border-orange-800'">
                    {{ zonePrep.batchLabel }}
                  </span>
                </template>
                <template v-else>
                  <span class="text-[10px] font-bold px-1.5 py-0.5 rounded border flex-shrink-0 bg-purple-50 dark:bg-purple-900/20 text-purple-700 dark:text-purple-400 border-purple-200 dark:border-purple-800">
                    {{ zonePrep.mergedIds.length }} parti birleştirildi
                  </span>
                </template>
                <span v-if="zonePrep.isFrozen" class="text-[10px] font-bold bg-blue-50 dark:bg-blue-900/20 text-blue-600 dark:text-blue-400 border border-blue-200 dark:border-blue-800 px-1.5 py-0.5 rounded">🔒</span>
                <span v-if="zonePrep.openErrorCount > 0" class="text-[10px] font-bold bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400 border border-red-200 dark:border-red-800 px-1.5 py-0.5 rounded flex items-center gap-0.5">
                  ⚠ {{ zonePrep.openErrorCount }} hata
                </span>
                <span v-else-if="zonePrep.openWarningCount > 0" class="text-[10px] font-bold bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-400 border border-amber-200 dark:border-amber-800 px-1.5 py-0.5 rounded flex items-center gap-0.5">
                  ⚠ {{ zonePrep.openWarningCount }} uyarı
                </span>
              </div>
              <div class="flex items-center gap-2 mt-1 flex-wrap">
                <span class="text-[11px] font-semibold px-2 py-0.5 rounded-full"
                      :class="statusChipClass(zonePrep.statusId)">
                  {{ getStatusLabel(zonePrep.statusId) }}
                </span>
                <span v-if="zonePrep.projects?.length" class="text-xs text-gray-400 dark:text-gray-500">
                  {{ readyCount(zonePrep) }}/{{ zonePrep.projects.length }} proje hazır
                </span>
                <span v-if="zonePrep.foodTotalWeightKg != null && (zonePrep.statusId === 4 || zonePrep.statusId === 5)"
                      class="text-[10px] font-bold bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 border border-green-200 dark:border-green-800 px-1.5 py-0.5 rounded flex-shrink-0">
                  🌿 {{ (zonePrep.foodPickedWeightKg ?? 0).toFixed(1) }} / {{ zonePrep.foodTotalWeightKg.toFixed(1) }} kg
                </span>
              </div>
            </div>
            <!-- Chevron -->
            <svg class="h-4 w-4 text-gray-400 dark:text-gray-600 flex-shrink-0 mt-1 transition-transform"
                 :class="expandedIds.has(zonePrep.id) ? 'rotate-180' : ''"
                 fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
            </svg>
          </div>

          <!-- Action buttons row (always visible) -->
          <div class="px-4 pb-3 flex flex-wrap gap-2" :class="{ 'justify-between': true }">

            <button
              v-if="zonePrep.statusId === 0 && !zonePrep.isFrozen"
              @click.stop="startPreparation(zonePrep)"
              class="flex-1 md:flex-none bg-blue-600 hover:bg-blue-700 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
            >
              🚀 <span>Toplamayı Başlat</span>
            </button>

            <button
              v-if="canStartMacro(zonePrep)"
              @click.stop="openMacroModal(zonePrep)"
              :disabled="lockingMacroZoneId === zonePrep.id"
              class="flex-1 md:flex-none bg-orange-500 hover:bg-orange-600 disabled:opacity-60 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
              :title="zonePrep.macroLockedByUserName ? `🔒 ${zonePrep.macroLockedByUserName} tarafından toplanıyor` : ''"
            >
              <span v-if="lockingMacroZoneId === zonePrep.id">⏳</span>
              <span v-else>📦</span>
              <span>{{ zonePrep.macroLockedByUserName ? `🔒 ${zonePrep.macroLockedByUserName}` : 'Macro Hazırlık' }}</span>
            </button>

            <!-- Gıda Hazırlık butonu (statusId === 4 = GidaHazirlik) -->
            <button
              v-if="zonePrep.statusId === 4 && zonePrep.isFrozen"
              @click.stop="openFoodModal(zonePrep)"
              class="flex-1 md:flex-none bg-green-600 hover:bg-green-700 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
            >
              🥕 <span>Gıda Topla</span>
            </button>

            <!-- Admin: Durumu Güncelle (GidaHazirlik → shipments back to Picking) -->
            <button
              v-if="zonePrep.statusId === 4 && isAdmin"
              @click.stop="resetGidaShipments(zonePrep)"
              :disabled="resettingGidaId === zonePrep.id"
              class="flex-1 md:flex-none bg-orange-600 hover:bg-orange-700 disabled:opacity-50 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
            >
              <svg v-if="resettingGidaId === zonePrep.id" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
              </svg>
              <span v-else>↩</span>
              <span>Durumu Güncelle</span>
            </button>

            <!-- Out-of-city zone finalize (shown when frozen, status 1-3) -->
            <button
              v-if="zonePrep.isOutOfCity && zonePrep.isFrozen && zonePrep.statusId >= 1 && zonePrep.statusId < 4"
              @click.stop="finalizeOutOfCityZone(zonePrep)"
              :disabled="finalizingOutOfCityId === zonePrep.id"
              class="flex-1 md:flex-none bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
            >
              <svg v-if="finalizingOutOfCityId === zonePrep.id" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
              </svg>
              <span v-else>✓</span>
              <span>Sevke Hazırla</span>
            </button>

            <!-- Fetch irsaliye from Netsis (shown at status 5, before irsaliye fetched) -->
            <button
              v-if="zonePrep.statusId === 5 && !zonePrep.irsaliyeFetched && zonePrep.isFrozen"
              @click.stop="fetchIrsaliye(zonePrep)"
              :disabled="fetchingIrsaliyeId === zonePrep.id"
              class="flex-1 md:flex-none bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
            >
              <svg v-if="fetchingIrsaliyeId === zonePrep.id" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
              </svg>
              <span v-else>📄</span>
              <span>Netsisten İrsaliye Çek</span>
            </button>

            <!-- Assign driver (shown at status 5, after irsaliye fetched) -->
            <template v-if="zonePrep.statusId === 5 && zonePrep.irsaliyeFetched && zonePrep.isFrozen">
              <button
                @click.stop="openDriverModal(zonePrep)"
                class="flex-1 md:flex-none bg-purple-600 hover:bg-purple-700 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
              >
                🚚 <span>Şoför Ata</span>
              </button>
              <button
                @click.stop="warehousePickupAll(zonePrep)"
                :disabled="pickupAllZoneId === zonePrep.id"
                class="flex-1 md:flex-none bg-amber-600 hover:bg-amber-700 disabled:opacity-50 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
              >
                <svg v-if="pickupAllZoneId === zonePrep.id" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                </svg>
                <span v-else>🏭</span>
                <span>Depo Teslim</span>
              </button>
              <button
                @click.stop="openCargoModal(zonePrep)"
                class="flex-1 md:flex-none bg-orange-500 hover:bg-orange-600 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
              >
                📦 <span>Kargo ile Gönder</span>
              </button>
              <button
                @click.stop="openFreightModal(zonePrep)"
                class="flex-1 md:flex-none bg-teal-600 hover:bg-teal-700 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
              >
                🚐 <span>Nakliye ile Gönder</span>
              </button>
            </template>

            <!-- Verification button (shown at statusId >= 0) -->
            <button
              v-if="zonePrep.statusId >= 0"
              @click.stop="openVerificationModal(zonePrep)"
              class="flex-none border border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700 px-3 py-2 rounded-lg text-xs font-medium flex items-center gap-1.5 transition-colors"
              title="Hazırlanan ürünleri kontrol et"
            >
              🔍 <span>Kontrol</span>
            </button>

            <!-- Confirm loading (shown at status 6 = ReadyForTransfer) -->
            <button
              v-if="zonePrep.statusId === 6"
              @click.stop="confirmLoading(zonePrep)"
              :disabled="confirmingLoadingId === zonePrep.id"
              class="flex-1 md:flex-none bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 text-white px-4 py-2.5 rounded-lg font-bold text-sm flex items-center justify-center gap-2 shadow-sm transition-colors"
            >
              <svg v-if="confirmingLoadingId === zonePrep.id" class="animate-spin h-4 w-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
              </svg>
              <span v-else>🚛</span>
              <span>Yüklemeyi Onayla</span>
            </button>

            <!-- Admin: force-close stuck zone (only for individual batches) -->
            <button
              v-role="['Admin']"
              v-if="zonePrep.mergedIds.length === 1"
              @click.stop="confirmForceClose(zonePrep)"
              :disabled="forcingCloseId === zonePrep.id"
              class="ml-auto flex-none bg-transparent border border-red-300 dark:border-red-800 text-red-500 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 px-3 py-2 rounded-lg text-xs font-medium flex items-center gap-1.5 transition-colors disabled:opacity-50"
              title="Sevkiyatları serbest bırak — Sevke Hazır durumuna geri al (Admin)"
            >
              <svg class="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
              <span>Sevki Geri Al</span>
            </button>
          </div>

          <!-- Expanded: project grid -->
          <Transition name="expand">
            <div v-if="expandedIds.has(zonePrep.id)" class="border-t border-gray-100 dark:border-gray-800 px-4 py-4 space-y-4 bg-gray-50 dark:bg-gray-800/50">

              <!-- Out-of-city zone: single panel -->
              <div v-if="zonePrep.isOutOfCity" class="bg-teal-50 dark:bg-teal-900/20 border border-teal-200 dark:border-teal-800 rounded-lg p-4">
                <div class="flex items-center gap-2 mb-3">
                  <span class="text-sm font-bold text-teal-800 dark:text-teal-300">🏙️ Şehir Dışı Projeler</span>
                  <span class="text-[10px] font-bold bg-teal-200 dark:bg-teal-800 text-teal-800 dark:text-teal-200 px-1.5 py-0.5 rounded uppercase">Proje Bazlı Toplama</span>
                </div>
                <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
                  <div v-for="proj in zonePrep.projects" :key="proj.id"
                       class="bg-white dark:bg-gray-900 rounded-lg border border-teal-200 dark:border-teal-800 p-3">
                    <div class="flex items-start justify-between gap-1 mb-1">
                      <span class="font-bold text-sm text-teal-900 dark:text-teal-300 truncate">{{ proj.projectCode }}</span>
                      <span class="text-[10px] font-bold px-1.5 py-0.5 rounded-full flex-shrink-0"
                            :class="proj.isMicroReady ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' : 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400'">
                        {{ proj.isMicroReady ? 'HAZIR' : 'BEKLER' }}
                      </span>
                    </div>
                    <p class="text-xs text-gray-500 dark:text-gray-400 truncate">{{ proj.projectName }}</p>
                    <p v-if="proj.isMicroReady && proj.preparedByUserName" class="text-[10px] text-green-600 dark:text-green-400 truncate mb-1">
                      ✓ {{ proj.preparedByUserName }}
                    </p>
                    <p v-else-if="proj.pickingLockedByUserName" class="text-[10px] text-amber-600 dark:text-amber-400 truncate mb-1">
                      🔒 {{ proj.pickingLockedByUserName }}
                    </p>
                    <div v-else class="mb-1"></div>
                    <button
                      v-if="zonePrep.isFrozen && zonePrep.statusId >= 1 && zonePrep.statusId < 4"
                      @click.stop="openOutOfCityModal(zonePrep, proj)"
                      class="w-full py-1.5 bg-teal-600 hover:bg-teal-700 text-white text-xs font-bold rounded flex items-center justify-center gap-1 transition-colors"
                    >
                      🗂️ Topla
                    </button>
                  </div>
                </div>
              </div>

              <!-- Original projects -->
              <div v-if="!zonePrep.isOutOfCity && zonePrep.projects?.some((p: any) => !p.isAddedLater)">
                <p class="text-[11px] font-bold uppercase tracking-wider text-gray-400 dark:text-gray-500 mb-2">Planlanan Projeler</p>
                <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
                  <div v-for="proj in zonePrep.projects.filter((p: any) => !p.isAddedLater)" :key="proj.id"
                       class="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-700 p-3 relative overflow-hidden">
                    <div v-if="!zonePrep.isFrozen && zonePrep.statusId === 0"
                         class="absolute inset-0 bg-white/60 dark:bg-gray-900/60 flex items-center justify-center z-10 pointer-events-none">
                      <span class="text-gray-400 dark:text-gray-600 text-xs font-bold border border-gray-300 dark:border-gray-700 px-2 py-1 rounded rotate-[-10deg]">BEKLİYOR</span>
                    </div>
                    <div class="flex items-start justify-between gap-1 mb-1">
                      <span class="font-bold text-sm text-blue-900 dark:text-blue-300 truncate">{{ proj.projectCode }}</span>
                      <span class="text-[10px] font-bold px-1.5 py-0.5 rounded-full flex-shrink-0"
                            :class="proj.isMicroReady ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' : 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400'">
                        {{ proj.isMicroReady ? 'HAZIR' : 'BEKLER' }}
                      </span>
                    </div>
                    <p class="text-xs text-gray-500 dark:text-gray-400 truncate">{{ proj.projectName }}</p>
                    <p v-if="proj.isMicroReady && proj.preparedByUserName" class="text-[10px] text-green-600 dark:text-green-400 truncate mb-1">
                      ✓ {{ proj.preparedByUserName }}
                    </p>
                    <p v-else-if="proj.pickingLockedByUserName" class="text-[10px] text-amber-600 dark:text-amber-400 truncate mb-1">
                      🔒 {{ proj.pickingLockedByUserName }}
                    </p>
                    <div v-else class="mb-1"></div>
                    <button
                      @click="openMicroModal(proj)"
                      :disabled="!zonePrep.isFrozen || lockingProjectId === proj.id"
                      class="w-full py-1.5 rounded-lg text-xs font-bold transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                      :class="proj.isMicroReady
                        ? 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-400 border border-green-200 dark:border-green-800'
                        : 'bg-blue-600 text-white hover:bg-blue-700'"
                    >
                      {{ lockingProjectId === proj.id ? '⏳' : (proj.isMicroReady ? 'İncele / Düzenle' : 'Micro Topla') }}
                    </button>
                    <!-- Depo Teslim — status 5, irsaliye alındı, teslim almaya gelen müşteri -->
                    <button
                      v-if="zonePrep.statusId === 5 && proj.shipmentId != null"
                      @click.stop="warehousePickup(proj)"
                      :disabled="pickupShipmentId === proj.shipmentId"
                      class="mt-1.5 w-full py-1.5 bg-amber-600 hover:bg-amber-700 disabled:opacity-50 text-white rounded-lg text-xs font-bold flex items-center justify-center gap-1 transition-colors"
                    >
                      <svg v-if="pickupShipmentId === proj.shipmentId" class="animate-spin h-3 w-3" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                      </svg>
                      <span v-else>🏭</span>
                      Depo Teslim
                    </button>
                    <!-- Per-project dispatch: Şoför / Kargo / Nakliye (statusId 5, irsaliye alındı) -->
                    <template v-if="zonePrep.statusId === 5 && zonePrep.irsaliyeFetched">
                      <div class="mt-1.5 flex gap-1">
                        <button
                          @click.stop="openDriverModalForProject(proj)"
                          class="flex-1 py-1 bg-purple-600 hover:bg-purple-700 text-white rounded text-[10px] font-bold flex items-center justify-center gap-0.5 transition-colors"
                          title="Bu proje için şoför ata"
                        >🚚 Şoför</button>
                        <button
                          @click.stop="openCargoModalForProject(proj)"
                          class="flex-1 py-1 bg-orange-500 hover:bg-orange-600 text-white rounded text-[10px] font-bold flex items-center justify-center gap-0.5 transition-colors"
                          title="Bu projeyi kargo ile gönder"
                        >📦 Kargo</button>
                        <button
                          @click.stop="openFreightModalForProject(proj)"
                          class="flex-1 py-1 bg-teal-600 hover:bg-teal-700 text-white rounded text-[10px] font-bold flex items-center justify-center gap-0.5 transition-colors"
                          title="Bu projeyi nakliye ile gönder"
                        >🚐 Nakliye</button>
                      </div>
                    </template>
                  </div>
                </div>
              </div>

              <!-- Late-added projects -->
              <div v-if="!zonePrep.isOutOfCity && zonePrep.projects?.some((p: any) => p.isAddedLater)"
                   class="bg-red-50 dark:bg-red-900/10 border border-red-200 dark:border-red-800 rounded-lg p-3">
                <p class="text-[11px] font-bold uppercase tracking-wider text-red-600 dark:text-red-400 mb-2 flex items-center gap-1">
                  ⚡ Sonradan Eklenenler
                  <span class="bg-red-200 dark:bg-red-800 text-red-800 dark:text-red-200 px-1.5 rounded-full text-[9px]">DİKKAT</span>
                </p>
                <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
                  <div v-for="proj in zonePrep.projects.filter((p: any) => p.isAddedLater)" :key="proj.id"
                       class="bg-white dark:bg-gray-900 rounded-lg border border-red-200 dark:border-red-800 p-3 relative overflow-hidden">
                    <div class="absolute top-0 right-0 bg-red-600 text-white text-[9px] font-bold px-1.5 py-0.5 rounded-bl">YENİ</div>
                    <div class="flex items-start justify-between gap-1 mb-1">
                      <span class="font-bold text-sm text-red-900 dark:text-red-300 truncate">{{ proj.projectCode }}</span>
                      <span class="text-[10px] font-bold px-1.5 py-0.5 rounded-full flex-shrink-0 mt-3"
                            :class="proj.isMicroReady ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400' : 'bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400'">
                        {{ proj.isMicroReady ? 'HAZIR' : 'BEKLER' }}
                      </span>
                    </div>
                    <p class="text-xs text-gray-500 dark:text-gray-400 truncate">{{ proj.projectName }}</p>
                    <p v-if="proj.isMicroReady && proj.preparedByUserName" class="text-[10px] text-green-600 dark:text-green-400 truncate mb-1">
                      ✓ {{ proj.preparedByUserName }}
                    </p>
                    <p v-else-if="proj.pickingLockedByUserName" class="text-[10px] text-amber-600 dark:text-amber-400 truncate mb-1">
                      🔒 {{ proj.pickingLockedByUserName }}
                    </p>
                    <div v-else class="mb-1"></div>
                    <button
                      @click="openMicroModal(proj)"
                      :disabled="!zonePrep.isFrozen || lockingProjectId === proj.id"
                      class="w-full py-1.5 rounded-lg text-xs font-bold transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                      :class="proj.isMicroReady
                        ? 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-400 border border-green-200 dark:border-green-800'
                        : 'bg-red-600 text-white hover:bg-red-700'"
                    >
                      {{ lockingProjectId === proj.id ? '⏳' : (proj.isMicroReady ? 'İncele / Düzenle' : 'Micro Topla') }}
                    </button>
                    <button
                      v-if="zonePrep.statusId === 5 && proj.shipmentId != null"
                      @click.stop="warehousePickup(proj)"
                      :disabled="pickupShipmentId === proj.shipmentId"
                      class="mt-1.5 w-full py-1.5 bg-amber-600 hover:bg-amber-700 disabled:opacity-50 text-white rounded-lg text-xs font-bold flex items-center justify-center gap-1 transition-colors"
                    >
                      <svg v-if="pickupShipmentId === proj.shipmentId" class="animate-spin h-3 w-3" fill="none" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                      </svg>
                      <span v-else>🏭</span>
                      Depo Teslim
                    </button>
                    <template v-if="zonePrep.statusId === 5 && zonePrep.irsaliyeFetched">
                      <div class="mt-1.5 flex gap-1">
                        <button @click.stop="openDriverModalForProject(proj)"
                          class="flex-1 py-1 bg-purple-600 hover:bg-purple-700 text-white rounded text-[10px] font-bold flex items-center justify-center gap-0.5 transition-colors">🚚 Şoför</button>
                        <button @click.stop="openCargoModalForProject(proj)"
                          class="flex-1 py-1 bg-orange-500 hover:bg-orange-600 text-white rounded text-[10px] font-bold flex items-center justify-center gap-0.5 transition-colors">📦 Kargo</button>
                        <button @click.stop="openFreightModalForProject(proj)"
                          class="flex-1 py-1 bg-teal-600 hover:bg-teal-700 text-white rounded text-[10px] font-bold flex items-center justify-center gap-0.5 transition-colors">🚐 Nakliye</button>
                      </div>
                    </template>
                  </div>
                </div>
              </div>

            </div>
          </Transition>
        </div>

      </template>
    </div>

    <!-- ── Modals ── -->
    <MicroPickingModal
      v-if="showMicroModal"
      :zp-project-id="selectedProjectId"
      :project-name="selectedProjectName"
      @close="closeMicroModal"
      @completed="fetchAll"
    />
    <MacroPickingModal
      v-if="showMacroModal && selectedZonePrep"
      :zone-preparation-id="selectedZonePrep.id"
      @close="closeMacroModal"
      @completed="fetchAll"
    />
    <FoodPreparationModal
      v-if="showFoodModal"
      :zone-id="selectedFoodZoneId"
      :delivery-date="selectedFoodDeliveryDate"
      :zone-name="selectedFoodZoneName"
      @close="showFoodModal = false"
      @completed="() => { showFoodModal = false; fetchAll(); }"
    />
    <DriverAssignmentModal
      v-if="showDriverModal && selectedZonePrepIds.length > 0"
      :zone-preparation-ids="selectedZonePrepIds"
      @close="closeDriverModal"
      @completed="fetchAll"
    />
    <CargoDispatchModal
      v-if="showCargoModal && selectedZonePrep"
      :zone-preparation-id="selectedZonePrep.id"
      :zone-name="selectedZonePrep.zoneName"
      :shipment-count="selectedZonePrep.projects?.length ?? 0"
      @close="showCargoModal = false; selectedZonePrep = null"
      @completed="fetchAll"
    />
    <FreightDispatchModal
      v-if="showFreightModal && selectedFreightZoneIds.length > 0 && selectedZonePrep"
      :zone-preparation-ids="selectedFreightZoneIds"
      :zone-name="selectedZonePrep.zoneName"
      :shipment-count="selectedZonePrep.projects?.length ?? 0"
      @close="showFreightModal = false; selectedZonePrep = null; selectedFreightZoneIds = []"
      @completed="fetchAll"
    />
    <OutOfCityPickingModal
      v-if="showOutOfCityModal && selectedZonePrep && selectedOutOfCityProject"
      :zone-preparation-id="selectedZonePrep.id"
      :zone-name="selectedZonePrep.zoneName"
      :project-id="selectedOutOfCityProject.projectId"
      :project-name="`${selectedOutOfCityProject.projectCode} — ${selectedOutOfCityProject.projectName}`"
      @close="closeOutOfCityModal"
      @completed="() => { closeOutOfCityModal(); fetchAll(); }"
    />

    <!-- Verification Modal -->
    <Teleport to="body">
      <div v-if="showVerificationModal" class="fixed inset-0 z-50 overflow-y-auto">
        <div class="flex items-start justify-center min-h-screen pt-4 px-4 pb-20">
          <div class="fixed inset-0 bg-black/50" @click="showVerificationModal = false"></div>
          <div class="relative bg-white dark:bg-gray-900 rounded-xl shadow-xl w-full max-w-3xl mt-8">
            <div class="flex items-center justify-between px-5 py-4 border-b border-gray-200 dark:border-gray-700">
              <h3 class="font-bold text-gray-900 dark:text-gray-100 text-base">
                🔍 Hazırlık Kontrolü — {{ verificationZoneName }}
              </h3>
              <button @click="showVerificationModal = false" class="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200">
                <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
            <div class="p-5 max-h-[70vh] overflow-y-auto space-y-4">
              <div v-if="verificationLoading" class="text-center py-8 text-gray-400">Yükleniyor...</div>
              <div v-else-if="verificationData.length === 0" class="text-center py-8 text-gray-400">Veri bulunamadı.</div>
              <div v-else v-for="shipment in verificationData" :key="shipment.shipmentId"
                   class="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
                <div class="px-3 py-2 bg-gray-50 dark:bg-gray-800 flex items-center justify-between gap-2 flex-wrap">
                  <div class="flex items-center gap-2 flex-wrap">
                    <span class="font-bold text-sm text-gray-800 dark:text-gray-200">{{ shipment.projectCode }} — {{ shipment.projectName }}</span>
                    <span v-if="shipment.talepNo" class="text-[11px] text-gray-500 dark:text-gray-400">Talep: {{ shipment.talepNo }}</span>
                    <span v-if="shipment.irsaliyeNo" class="text-[11px] font-semibold text-blue-600 dark:text-blue-400">İrsaliye: {{ shipment.irsaliyeNo }}</span>
                  </div>
                </div>
                <table class="w-full text-xs">
                  <thead>
                    <tr class="bg-gray-100 dark:bg-gray-800/60 text-gray-500 dark:text-gray-400 text-left">
                      <th class="px-3 py-1.5 font-semibold">Ürün</th>
                      <th class="px-3 py-1.5 font-semibold text-right">Sipariş</th>
                      <th class="px-3 py-1.5 font-semibold text-right">Verilen</th>
                      <th class="px-3 py-1.5 font-semibold text-right">Fark</th>
                    </tr>
                  </thead>
                  <tbody class="divide-y divide-gray-100 dark:divide-gray-800">
                    <tr v-for="line in shipment.lines" :key="line.shipmentLineId"
                        :class="line.difference < 0 ? 'bg-red-50 dark:bg-red-900/10' : line.difference > 0 ? 'bg-yellow-50 dark:bg-yellow-900/10' : ''">
                      <td class="px-3 py-1.5">
                        <span class="font-medium text-gray-800 dark:text-gray-200">{{ line.stockName }}</span>
                        <span class="text-gray-400 dark:text-gray-500 ml-1">{{ line.stockCode }}</span>
                        <span v-if="line.differenceReason" class="block text-[10px] text-amber-600 dark:text-amber-400 mt-0.5">{{ line.differenceReason }}</span>
                      </td>
                      <td class="px-3 py-1.5 text-right text-gray-600 dark:text-gray-400">{{ line.orderedQty }} {{ line.unit }}</td>
                      <td class="px-3 py-1.5 text-right font-semibold"
                          :class="line.deliveredQty === 0 ? 'text-red-600 dark:text-red-400' : 'text-gray-800 dark:text-gray-200'">
                        {{ line.deliveredQty }} {{ line.unit }}
                      </td>
                      <td class="px-3 py-1.5 text-right font-bold"
                          :class="line.difference < 0 ? 'text-red-600 dark:text-red-400' : line.difference > 0 ? 'text-yellow-600 dark:text-yellow-400' : 'text-green-600 dark:text-green-400'">
                        {{ line.difference > 0 ? '+' : '' }}{{ line.difference !== 0 ? line.difference : '✓' }}
                      </td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Teleport>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue';
import PageHeader from '../components/PageHeader.vue';
import warehouseService, { type DashboardZoneDto, type VerificationShipmentDto } from '../services/warehouseService';
import { dashboardService, type CriticalStockItem } from '../services/dashboardService';
import { ApiErrorUtils } from '../utils/apiError';
import { useNotificationStore } from '../stores/notification';
import { useAuthStore } from '../stores/auth';
import { useNotificationsStore } from '../stores/notifications';
import { useSoundFeedback } from '../composables/useSoundFeedback';
import MicroPickingModal from '../components/MicroPickingModal.vue';
import MacroPickingModal from '../components/MacroPickingModal.vue';
import DriverAssignmentModal from '../components/DriverAssignmentModal.vue';
import CargoDispatchModal from '../components/CargoDispatchModal.vue';
import FreightDispatchModal from '../components/FreightDispatchModal.vue';
import OutOfCityPickingModal from '../components/OutOfCityPickingModal.vue';
import CriticalStockWidget from '../components/CriticalStockWidget.vue';
import FoodPreparationModal from '../components/FoodPreparationModal.vue';

const notificationStore = useNotificationStore();
const notificationsStore = useNotificationsStore();
const authStore = useAuthStore();
const sound = useSoundFeedback();
const isAdmin = computed(() => authStore.userRole === 'Admin');
const loading = ref(false);
const error = ref('');
const isStarting = ref(false);
const fetchingIrsaliyeId = ref<number | null>(null);
const confirmingLoadingId = ref<number | null>(null);
type TabKey = 'active' | 'food' | 'irsaliye' | 'vehicle' | 'loading' | 'all';
const activeTab = ref<TabKey>('active');
const tabOptions: { key: TabKey; label: string }[] = [
  { key: 'active',    label: 'Aktif Hazırlık' },
  { key: 'food',      label: 'Gıda Hazırlık' },
  { key: 'irsaliye',  label: 'İrsaliye Bekleyen' },
  { key: 'vehicle',   label: 'Şoför/Kargo Bekleyen' },
  { key: 'loading',   label: 'Yükleme Bekleyen' },
  { key: 'all',       label: 'Tümü' },
];
const expandedIds = ref<Set<number>>(new Set());

interface ZonePrepWithDate extends DashboardZoneDto {
  deliveryDateStr: string;
}

interface DisplayZoneItem extends ZonePrepWithDate {
  mergedIds: number[];
  batchLabels: string[];
}

// All fetched zone preps across date range
const allZones = ref<ZonePrepWithDate[]>([]);

// Modal state
const showMicroModal = ref(false);
const selectedProjectId = ref(0);
const selectedProjectName = ref('');
const showMacroModal = ref(false);
const showDriverModal = ref(false);
const selectedZonePrep = ref<DashboardZoneDto | null>(null);
const selectedZonePrepIds = ref<number[]>([]);
const criticalStocks = ref<CriticalStockItem[]>([]);
const criticalStocksLoading = ref(false);

// ── Tüm aktif zone hazırlıklarını tek çağrıyla getir (tarih filtresi yok) ──
const fetchAll = async () => {
  loading.value = true;
  error.value = '';
  try {
    const zones = await warehouseService.getDashboardAll();
    const withDate = zones.map(z => ({
      ...z,
      deliveryDateStr: z.deliveryDate.slice(0, 10),
    }));
    // Tarihe göre sırala
    withDate.sort((a, b) => a.deliveryDateStr.localeCompare(b.deliveryDateStr));
    allZones.value = withDate;


    expandedIds.value = new Set();
  } catch (e) {
    error.value = 'Veriler yüklenirken bir hata oluştu.';
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Veriler alınamadı.', 'error');
  } finally {
    loading.value = false;
  }
};

// ── Filtered / grouped data ──
// Status değerleri: 0=Draft, 1=MicroPicking, 2=MicroReady, 3=MacroPicking,
//                  4=GidaHazirlik, 5=ReadyForDriverInfo, 6=ReadyForTransfer, 7=Dispatched
const visibleZones = computed(() => {
  if (activeTab.value === 'food')
    return allZones.value.filter(z => z.statusId === 4);
  if (activeTab.value === 'irsaliye')
    return allZones.value.filter(z => z.statusId === 5 && !z.irsaliyeFetched);
  if (activeTab.value === 'vehicle')
    return allZones.value.filter(z => z.statusId === 5 && z.irsaliyeFetched);
  if (activeTab.value === 'loading')
    return allZones.value.filter(z => z.statusId === 6);
  if (activeTab.value === 'active')
    return allZones.value.filter(z => z.statusId < 4);
  return allZones.value; // 'all'
});

const uniqueZoneCount = (zones: typeof allZones.value) =>
  new Set(zones.map(z => z.zoneName)).size;

const foodPendingCount = computed(() =>
  uniqueZoneCount(allZones.value.filter(z => z.statusId === 4))
);

const irsaliyePendingCount = computed(() =>
  uniqueZoneCount(allZones.value.filter(z => z.statusId === 5 && !z.irsaliyeFetched))
);

const vehiclePendingCount = computed(() =>
  uniqueZoneCount(allZones.value.filter(z => z.statusId === 5 && z.irsaliyeFetched))
);

const loadingPendingCount = computed(() =>
  uniqueZoneCount(allZones.value.filter(z => z.statusId === 6))
);

const todayStr = (): string => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
};

const activePickingCount = computed(() =>
  uniqueZoneCount(allZones.value.filter(z => z.statusId < 4))
);
const overdueCount = computed(() =>
  allZones.value.filter(z => z.statusId < 4 && z.deliveryDateStr < todayStr()).length
);

interface DateGroup {
  date: string;
  label: string;
  isToday: boolean;
  isPast: boolean;
  zones: ZonePrepWithDate[];
}

const groupedByDate = computed((): DateGroup[] => {
  const map = new Map<string, DateGroup>();
  const today = todayStr();

  for (const zone of visibleZones.value) {
    const d = zone.deliveryDateStr;
    if (!map.has(d)) {
      const isToday = d === today;
      const isPast = d < today;
      const dateObj = new Date(d + 'T12:00:00');
      const label = isToday
        ? `Bugün — ${dateObj.toLocaleDateString('tr-TR', { day: 'numeric', month: 'long' })}`
        : dateObj.toLocaleDateString('tr-TR', { weekday: 'long', day: 'numeric', month: 'long' });
      map.set(d, { date: d, label, isToday, isPast, zones: [] });
    }
    map.get(d)!.zones.push(zone);
  }

  return Array.from(map.values());
});

// Merge same-zone preps within a date group for vehicle/loading/food tabs
const displayGroupedByDate = computed((): Array<{ date: string; label: string; isToday: boolean; isPast: boolean; zones: DisplayZoneItem[] }> => {
  const shouldMerge = activeTab.value === 'vehicle' || activeTab.value === 'loading' || activeTab.value === 'food' || activeTab.value === 'irsaliye';

  return groupedByDate.value.map(group => {
    if (!shouldMerge) {
      return {
        ...group,
        zones: group.zones.map(z => ({ ...z, mergedIds: [z.id], batchLabels: [z.batchLabel] })),
      };
    }

    const byName = new Map<string, ZonePrepWithDate[]>();
    for (const z of group.zones) {
      if (!byName.has(z.zoneName)) byName.set(z.zoneName, []);
      byName.get(z.zoneName)!.push(z);
    }

    const merged: DisplayZoneItem[] = [];
    for (const zones of byName.values()) {
      if (zones.length === 1) {
        merged.push({ ...zones[0]!, mergedIds: [zones[0]!.id], batchLabels: [zones[0]!.batchLabel] } as DisplayZoneItem);
      } else {
        const primary = zones[0]!;
        merged.push({
          ...primary,
          mergedIds: zones.map(z => z.id),
          batchLabels: zones.map(z => z.batchLabel),
          openErrorCount: zones.reduce((a, z) => a + z.openErrorCount, 0),
          openWarningCount: zones.reduce((a, z) => a + z.openWarningCount, 0),
          projects: zones.flatMap(z => z.projects || []),
        } as DisplayZoneItem);
      }
    }
    return { ...group, zones: merged };
  });
});

// ── UI helpers ──
const toggleExpanded = (id: number) => {
  const s = new Set(expandedIds.value);
  if (s.has(id)) s.delete(id);
  else s.add(id);
  expandedIds.value = s;
};

const readyCount = (zone: DashboardZoneDto) =>
  zone.projects?.filter((p: any) => p.isMicroReady).length ?? 0;

const statusChipClass = (statusId: number) => {
  if (statusId === 7) return 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400';
  if (statusId === 6) return 'bg-emerald-100 dark:bg-emerald-900/30 text-emerald-700 dark:text-emerald-400';
  if (statusId === 5) return 'bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-400';
  if (statusId === 4) return 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400';
  if (statusId === 3) return 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400';
  if (statusId >= 1) return 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400';
  return 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400';
};

const getStatusLabel = (statusId: number) => {
  switch (statusId) {
    case 0: return 'Başlamadı';
    case 1: return 'Micro Toplama';
    case 2: return 'Macro Bekliyor';
    case 3: return 'Macro Toplama';
    case 4: return 'Gıda Hazırlık';
    case 5: return 'İrsaliye Bekliyor';
    case 6: return 'Araca Atandı';
    case 7: return 'Sevk Edildi';
    default: return 'Bilinmiyor';
  }
};

const canStartMacro = (zone: DashboardZoneDto) =>
  zone.isFrozen && zone.projects?.length > 0 && zone.statusId < 4 && !zone.isOutOfCity;

// ── Lock helpers ──
const lockingProjectId = ref<number | null>(null);

const openMicroModal = async (proj: any) => {
  if (proj.pickingLockedByUserName) {
    const ok = confirm(`Bu proje şu anda ${proj.pickingLockedByUserName} tarafından toplanıyor.\n\nYine de açmak istiyor musunuz?`);
    if (!ok) return;
  }
  lockingProjectId.value = proj.id;
  try {
    await warehouseService.lockProjectPicking(proj.id, false);
  } catch (e: any) {
    const msg: string = e?.response?.data?.message || e?.message || '';
    notificationStore.add(msg || 'Proje kilitlenemedi.', 'error');
    lockingProjectId.value = null;
    return;
  }
  lockingProjectId.value = null;
  selectedProjectId.value = proj.id;
  selectedProjectName.value = proj.projectName;
  showMicroModal.value = true;
};

const closeMicroModal = async () => {
  showMicroModal.value = false;
  if (selectedProjectId.value) {
    try { await warehouseService.lockProjectPicking(selectedProjectId.value, true); } catch { /* ignore */ }
  }
};

const lockingMacroZoneId = ref<number | null>(null);

const openMacroModal = async (zone: DashboardZoneDto) => {
  if (zone.macroLockedByUserName) {
    const ok = confirm(`Bu bölge şu anda ${zone.macroLockedByUserName} tarafından toplanıyor.\n\nYine de açmak istiyor musunuz?`);
    if (!ok) return;
  }
  lockingMacroZoneId.value = zone.id;
  try {
    await warehouseService.lockMacroPicking(zone.id, false);
  } catch (e: any) {
    const msg: string = e?.response?.data?.message || e?.message || '';
    notificationStore.add(msg || 'Bölge kilitlenemedi.', 'error');
    lockingMacroZoneId.value = null;
    return;
  }
  lockingMacroZoneId.value = null;
  selectedZonePrep.value = zone;
  showMacroModal.value = true;
};

const closeMacroModal = async () => {
  showMacroModal.value = false;
  if (selectedZonePrep.value) {
    try { await warehouseService.lockMacroPicking(selectedZonePrep.value.id, true); } catch { /* ignore */ }
  }
};

const showFoodModal = ref(false);
const selectedFoodZoneId = ref(0);
const selectedFoodDeliveryDate = ref('');
const selectedFoodZoneName = ref('');
const openFoodModal = (zone: ZonePrepWithDate) => {
  selectedFoodZoneId.value = zone.zoneId;
  selectedFoodDeliveryDate.value = zone.deliveryDateStr;
  selectedFoodZoneName.value = zone.zoneName;
  showFoodModal.value = true;
};

const resettingGidaId = ref<number | null>(null);
const resetGidaShipments = async (zone: DisplayZoneItem) => {
  const ids = zone.mergedIds ?? [zone.id];
  const batchInfo = ids.length > 1 ? ` (${ids.length} parti)` : '';
  if (!confirm(`"${zone.zoneName}"${batchInfo} hazırlığındaki sevkiyatlar "Toplanıyor" durumuna çekilecek. Devam edilsin mi?`)) return;
  resettingGidaId.value = zone.id;
  try {
    let totalReset = 0;
    for (const id of ids) {
      const result = await warehouseService.resetGidaHazirlikShipments(id);
      totalReset += result.resetCount;
    }
    notificationStore.add(`${totalReset} sevkiyat "Toplanıyor" durumuna alındı.`, 'success');
    await fetchAll();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
  } finally {
    resettingGidaId.value = null;
  }
};

const pickupShipmentId = ref<number | null>(null);
const warehousePickup = async (proj: any) => {
  if (!proj.shipmentId) return;
  const recipient = prompt(`"${proj.projectCode} — ${proj.projectName}" için depo teslim.\n\nTeslim alanın adı:`)?.trim();
  if (!recipient) return;
  pickupShipmentId.value = proj.shipmentId;
  try {
    await warehouseService.markWarehousePickup(proj.shipmentId, recipient);
    notificationStore.add(`${proj.projectCode} depodan teslim edildi.`, 'success');
    sound.success();
    await fetchAll();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Depo teslim başarısız.', 'error');
  } finally {
    pickupShipmentId.value = null;
  }
};

// Zone-level "Depo Teslim (Tümü)" — marks all projects with a shipment in this zone as warehouse pickup
const pickupAllZoneId = ref<number | null>(null);
const warehousePickupAll = async (zone: DisplayZoneItem) => {
  const projectsWithShipment = zone.projects?.filter((p: any) => p.shipmentId != null) ?? [];
  if (projectsWithShipment.length === 0) {
    notificationStore.add('Bu bölgede teslim edilecek sevkiyat bulunamadı.', 'warning');
    return;
  }
  const recipient = prompt(`"${zone.zoneName}" tüm projeler için depo teslim.\n\nTeslim alanın adı:`)?.trim();
  if (!recipient) return;
  pickupAllZoneId.value = zone.id;
  try {
    let successCount = 0;
    for (const proj of projectsWithShipment) {
      try {
        await warehouseService.markWarehousePickup(proj.shipmentId!, recipient);
        successCount++;
      } catch { /* ignore individual errors, continue */ }
    }
    notificationStore.add(`${successCount}/${projectsWithShipment.length} proje depodan teslim edildi.`, 'success');
    sound.success();
    await fetchAll();
  } catch (e) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Depo teslim başarısız.', 'error');
  } finally {
    pickupAllZoneId.value = null;
  }
};

// Per-project dispatch: open zone-level modals pre-filtered for the specific project's batch
const openDriverModalForProject = (proj: any) => {
  selectedZonePrepIds.value = [proj.zonePreparationId];
  selectedZonePrep.value = allZones.value.find(z => z.id === proj.zonePreparationId) ?? null;
  showDriverModal.value = true;
};

const openCargoModalForProject = (proj: any) => {
  const zone = allZones.value.find(z => z.id === proj.zonePreparationId);
  if (!zone) return;
  selectedZonePrep.value = zone;
  showCargoModal.value = true;
};

const openFreightModalForProject = (proj: any) => {
  const zone = allZones.value.find(z => z.id === proj.zonePreparationId);
  if (!zone) return;
  selectedZonePrep.value = zone;
  selectedFreightZoneIds.value = [proj.zonePreparationId];
  showFreightModal.value = true;
};

const openDriverModal = (zone: DisplayZoneItem) => {
  selectedZonePrep.value = zone;
  selectedZonePrepIds.value = zone.mergedIds;
  showDriverModal.value = true;
};
const closeDriverModal = () => { showDriverModal.value = false; selectedZonePrepIds.value = []; };

const showCargoModal = ref(false);
const openCargoModal = (zone: DashboardZoneDto) => {
  selectedZonePrep.value = zone;
  showCargoModal.value = true;
};

const showFreightModal = ref(false);
const selectedFreightZoneIds = ref<number[]>([]);
const openFreightModal = (zone: DisplayZoneItem) => {
  selectedZonePrep.value = zone;
  selectedFreightZoneIds.value = zone.mergedIds;
  showFreightModal.value = true;
};

const showOutOfCityModal = ref(false);
const selectedOutOfCityProject = ref<any>(null);
const openOutOfCityModal = async (zone: DashboardZoneDto, proj: any) => {
  if (proj.pickingLockedByUserName) {
    const ok = confirm(`Bu proje şu anda ${proj.pickingLockedByUserName} tarafından toplanıyor.\n\nYine de açmak istiyor musunuz?`);
    if (!ok) return;
  }
  try {
    await warehouseService.lockProjectPicking(proj.id, false);
  } catch (e: any) {
    const msg: string = e?.response?.data?.message || e?.message || '';
    notificationStore.add(msg || 'Proje kilitlenemedi.', 'error');
    return;
  }
  selectedZonePrep.value = zone;
  selectedOutOfCityProject.value = proj;
  showOutOfCityModal.value = true;
};

const closeOutOfCityModal = async () => {
  showOutOfCityModal.value = false;
  if (selectedOutOfCityProject.value) {
    try { await warehouseService.lockProjectPicking(selectedOutOfCityProject.value.id, true); } catch { /* ignore */ }
  }
  selectedZonePrep.value = null;
  selectedOutOfCityProject.value = null;
};

const finalizingOutOfCityId = ref<number | null>(null);
const finalizeOutOfCityZone = async (zone: DashboardZoneDto) => {
  if (finalizingOutOfCityId.value) return;
  finalizingOutOfCityId.value = zone.id;
  try {
    const result = await warehouseService.markOutOfCityReady({
      zonePreparationId: zone.id,
      lines: [],
      forceComplete: false,
    });
    if (result.warnings?.length) {
      for (const w of result.warnings)
        notificationStore.add(`⚠ Fazla verildi: ${w}`, 'warning');
    }
    notificationStore.add(`${zone.zoneName} sevke hazır — irsaliye bekliyor.`, 'success');
    sound.success();
    await fetchAll();
  } catch (e: any) {
    const msg: string = e?.response?.data?.message || e?.message || '';
    if (msg.includes('toplanmadı')) {
      const ok = confirm(`${msg}\n\nYine de sevke hazır işaretlensin mi?`);
      if (ok) {
        try {
          await warehouseService.markOutOfCityReady({
            zonePreparationId: zone.id,
            lines: [],
            forceComplete: true,
            forceReason: 'Depo tarafından onaylandı.',
          });
          notificationStore.add(`${zone.zoneName} sevke hazır (eksik kalemlerle).`, 'warning');
          await fetchAll();
        } catch (e2) {
          notificationStore.add(ApiErrorUtils.getErrorMessage(e2) || 'İşlem başarısız.', 'error');
        }
      }
    } else {
      notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İşlem başarısız.', 'error');
    }
  } finally {
    finalizingOutOfCityId.value = null;
  }
};

const forcingCloseId = ref<number | null>(null);
const confirmForceClose = async (zone: DashboardZoneDto) => {
  const ok = confirm(`"${zone.zoneName} - ${zone.batchLabel}" hazırlığındaki tüm sevkiyatlar Sevke Hazır durumuna geri alınacak ve araç ataması temizlenecek.\n\nDevam etmek istiyor musunuz?`);
  if (!ok) return;
  forcingCloseId.value = zone.id;
  try {
    await warehouseService.adminForceCloseZone(zone.id);
    notificationStore.add('Sevkiyatlar Sevke Hazır durumuna geri alındı.', 'success');
    await fetchAll();
  } catch (e: any) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Kapatma başarısız.', 'error');
  } finally {
    forcingCloseId.value = null;
  }
};

const fetchIrsaliye = async (zonePrep: DisplayZoneItem) => {
  if (fetchingIrsaliyeId.value) return;
  fetchingIrsaliyeId.value = zonePrep.id;
  const ids = zonePrep.mergedIds ?? [zonePrep.id];
  try {
    let totalFetched = 0;
    const allWarnings: string[] = [];
    const allErrors: string[] = [];
    for (const id of ids) {
      const result = await warehouseService.fetchIrsaliye(id);
      totalFetched += result.fetched;
      allWarnings.push(...result.warnings);
      allErrors.push(...result.errors);
    }
    if (totalFetched > 0) {
      notificationStore.add(`${totalFetched} sevkiyatın irsaliye numarası çekildi.`, 'success');
    }
    allWarnings.forEach(w => notificationStore.add(w, 'warning'));
    allErrors.forEach(e => notificationStore.add(e, 'error'));
    if (totalFetched === 0 && allWarnings.length === 0 && allErrors.length === 0) {
      notificationStore.add('Tüm irsaliyeler zaten çekilmiş.', 'info');
    }
    await fetchAll();
  } catch (e: any) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'İrsaliye çekilemedi.', 'error');
  } finally {
    fetchingIrsaliyeId.value = null;
  }
};

const startPreparation = async (zonePrep: DashboardZoneDto) => {
  if (isStarting.value) return;
  const ok = await notificationStore.promptConfirm({
    title: 'Toplamayı Başlat',
    message: 'Toplamayı başlatmak istediğinize emin misiniz? Bu işlemden sonra yeni siparişler ayrı bir batch olarak takip edilecektir.',
    confirmText: 'Başlat',
    type: 'warning'
  });
  if (!ok) return;

  isStarting.value = true;
  try {
    await warehouseService.startZonePreparation({ zonePreparationId: zonePrep.id });
    await fetchAll();
    notificationStore.add('Toplama başarıyla başlatıldı.', 'success');
    sound.success();
  } catch (e: any) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Başlatılamadı.', 'error');
  } finally {
    isStarting.value = false;
  }
};

const confirmLoading = async (zonePrep: DisplayZoneItem) => {
  if (confirmingLoadingId.value) return;

  const partiLabel = zonePrep.mergedIds.length > 1 ? ` (${zonePrep.mergedIds.length} parti)` : '';
  const ok = await notificationStore.promptConfirm({
    title: 'Yüklemeyi Onayla',
    message: `${zonePrep.zoneName}${partiLabel} için araç yükleme tamamlandı mı? Bu işlem sevkiyatları "Sevk Edildi" statüsüne geçirecektir.`,
    confirmText: 'Evet, Yükleme Tamamlandı',
    type: 'warning'
  });
  if (!ok) return;

  confirmingLoadingId.value = zonePrep.id;
  try {
    await Promise.all(zonePrep.mergedIds.map(id => warehouseService.confirmLoading(id)));
    await fetchAll();
    notificationStore.add('Yükleme onaylandı. Sevkiyatlar "Sevk Edildi" statüsüne geçirildi.', 'success');
    sound.complete();
  } catch (e: any) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Yükleme onaylanamadı.', 'error');
  } finally {
    confirmingLoadingId.value = null;
  }
};

// ── Verification ──
const showVerificationModal = ref(false);
const verificationZoneId = ref<number | null>(null);
const verificationZoneName = ref('');
const verificationData = ref<VerificationShipmentDto[]>([]);
const verificationLoading = ref(false);

const openVerificationModal = async (zone: DashboardZoneDto) => {
  verificationZoneId.value = zone.id;
  verificationZoneName.value = zone.zoneName;
  showVerificationModal.value = true;
  verificationLoading.value = true;
  try {
    verificationData.value = await warehouseService.getZoneVerification(zone.id);
  } catch (e: any) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(e) || 'Verifikasyon yüklenemedi.', 'error');
    showVerificationModal.value = false;
  } finally {
    verificationLoading.value = false;
  }
};

// Herhangi bir modal açık mı? (Toplama sırasında otomatik yenilemeyi engeller)
const hasOpenModal = computed(() =>
  showMicroModal.value || showMacroModal.value || showFoodModal.value ||
  showDriverModal.value || showCargoModal.value || showFreightModal.value ||
  showOutOfCityModal.value || showVerificationModal.value
);

// Yeni sevkiyat depoya atandığında SSE üzerinden otomatik yenile
// Modal açıksa yenileme — aktif toplama sırasında liste değişirse toplamalar bölünür
watch(
  () => notificationsStore.items[0],
  (newItem) => {
    if (newItem?.eventType === 'shipment_warehouse_assigned' && !hasOpenModal.value) {
      fetchAll();
    }
  }
);

onMounted(async () => {
  fetchAll();
  criticalStocksLoading.value = true;
  try {
    criticalStocks.value = await dashboardService.getCriticalStocks();
  } catch {
    // sessiz hata — widget empty state gösterir
  } finally {
    criticalStocksLoading.value = false;
  }
});
</script>

<style scoped>
.expand-enter-active,
.expand-leave-active {
  transition: all 0.2s ease;
  overflow: hidden;
}
.expand-enter-from,
.expand-leave-to {
  opacity: 0;
  max-height: 0;
}
.expand-enter-to,
.expand-leave-from {
  opacity: 1;
  max-height: 2000px;
}
</style>
