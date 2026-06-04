<template>
  <div class="space-y-6">
    <template v-if="!simplified">
    <!-- KPI row 1: Sevkiyat odaklı -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
      <router-link to="/shipments"
        class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 hover:border-blue-300 hover:shadow-sm transition-all">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Aktif Sevkiyat</p>
          <div class="w-8 h-8 rounded-lg bg-blue-50 dark:bg-blue-900/30 flex items-center justify-center">
            <ClipboardDocumentListIcon class="w-4 h-4 text-blue-600" />
          </div>
        </div>
        <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stats.totalActiveShipments }}</p>
      </router-link>

      <router-link :to="`/shipments?startDate=${today}&endDate=${today}&statuses=5,6`"
        class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4 hover:border-green-300 hover:shadow-sm transition-all">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Bugün Teslim</p>
          <div class="w-8 h-8 rounded-lg bg-green-50 dark:bg-green-900/30 flex items-center justify-center">
            <TruckIcon class="w-4 h-4 text-green-600" />
          </div>
        </div>
        <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stats.shipmentsToday }}</p>
      </router-link>

      <router-link :to="`/shipments?startDate=${ninetyDaysAgo}&endDate=${yesterday}`"
        class="rounded-xl border p-4 hover:shadow-sm transition-all"
        :class="stats.shipmentsOverdue > 0
          ? 'bg-red-50/30 dark:bg-red-900/10 border-red-200 dark:border-red-800'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="stats.shipmentsOverdue > 0 ? 'text-red-500' : 'text-gray-500 dark:text-gray-400'">
            Gecikmiş
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="stats.shipmentsOverdue > 0 ? 'bg-red-100 dark:bg-red-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <ExclamationCircleIcon class="w-4 h-4"
              :class="stats.shipmentsOverdue > 0 ? 'text-red-500' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="stats.shipmentsOverdue > 0 ? 'text-red-600' : 'text-gray-900 dark:text-gray-100'">
          {{ stats.shipmentsOverdue }}
        </p>
      </router-link>

      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-4">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wide">Bu Hafta Teslim</p>
          <div class="w-8 h-8 rounded-lg bg-purple-50 dark:bg-purple-900/30 flex items-center justify-center">
            <CheckCircleIcon class="w-4 h-4 text-purple-600" />
          </div>
        </div>
        <p class="text-3xl font-bold text-gray-900 dark:text-gray-100">{{ stats.shipmentsDeliveredThisWeek }}</p>
      </div>
    </div>

    <!-- KPI row 2: Operasyonel -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
      <router-link to="/purchase-orders"
        class="rounded-xl border p-4 hover:shadow-sm transition-all"
        :class="stats.pendingPOApprovalCount > 0
          ? 'bg-yellow-50 dark:bg-yellow-900/10 border-yellow-300 dark:border-yellow-700'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="stats.pendingPOApprovalCount > 0 ? 'text-yellow-700 dark:text-yellow-400' : 'text-gray-500 dark:text-gray-400'">
            Onay Bekleyen PO
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="stats.pendingPOApprovalCount > 0 ? 'bg-yellow-100 dark:bg-yellow-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <ShoppingCartIcon class="w-4 h-4"
              :class="stats.pendingPOApprovalCount > 0 ? 'text-yellow-600' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="stats.pendingPOApprovalCount > 0 ? 'text-yellow-700 dark:text-yellow-300' : 'text-gray-900 dark:text-gray-100'">
          {{ stats.pendingPOApprovalCount }}
        </p>
      </router-link>

      <router-link :to="`/shipments?startDate=${today}&endDate=${today}&statuses=0,1,2`"
        class="rounded-xl border p-4 hover:shadow-sm transition-all"
        :class="stats.todayShipmentsNotReadyCount > 0
          ? 'bg-red-50/40 dark:bg-red-900/10 border-red-200 dark:border-red-800'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="stats.todayShipmentsNotReadyCount > 0 ? 'text-red-500' : 'text-gray-500 dark:text-gray-400'">
            Bugün Hazır Değil
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="stats.todayShipmentsNotReadyCount > 0 ? 'bg-red-100 dark:bg-red-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <XCircleIcon class="w-4 h-4"
              :class="stats.todayShipmentsNotReadyCount > 0 ? 'text-red-500' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="stats.todayShipmentsNotReadyCount > 0 ? 'text-red-600' : 'text-gray-900 dark:text-gray-100'">
          {{ stats.todayShipmentsNotReadyCount }}
        </p>
      </router-link>

      <router-link to="/floating-returns"
        class="rounded-xl border p-4 hover:shadow-sm transition-all"
        :class="stats.pendingFloatingReturns > 0
          ? 'bg-orange-50 dark:bg-orange-900/10 border-orange-300 dark:border-orange-700'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="stats.pendingFloatingReturns > 0 ? 'text-orange-700 dark:text-orange-400' : 'text-gray-500 dark:text-gray-400'">
            Belirsiz İade
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="stats.pendingFloatingReturns > 0 ? 'bg-orange-100 dark:bg-orange-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <ExclamationCircleIcon class="w-4 h-4"
              :class="stats.pendingFloatingReturns > 0 ? 'text-orange-600' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="stats.pendingFloatingReturns > 0 ? 'text-orange-700 dark:text-orange-300' : 'text-gray-900 dark:text-gray-100'">
          {{ stats.pendingFloatingReturns }}
        </p>
      </router-link>

      <div class="rounded-xl border p-4"
        :class="stats.criticalStockCount > 0
          ? 'bg-red-50 dark:bg-red-900/10 border-red-300 dark:border-red-700'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="stats.criticalStockCount > 0 ? 'text-red-700 dark:text-red-400' : 'text-gray-500 dark:text-gray-400'">
            Kritik Stok
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="stats.criticalStockCount > 0 ? 'bg-red-100 dark:bg-red-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <ArchiveBoxIcon class="w-4 h-4"
              :class="stats.criticalStockCount > 0 ? 'text-red-600' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="stats.criticalStockCount > 0 ? 'text-red-700 dark:text-red-300' : 'text-gray-900 dark:text-gray-100'">
          {{ stats.criticalStockCount }}
        </p>
      </div>

      <router-link to="/freight-deliveries"
        class="rounded-xl border p-4 hover:shadow-sm transition-all"
        :class="pendingFreightCount > 0
          ? 'bg-teal-50 dark:bg-teal-900/10 border-teal-300 dark:border-teal-700'
          : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
        <div class="flex items-center justify-between mb-3">
          <p class="text-xs font-medium uppercase tracking-wide"
            :class="pendingFreightCount > 0 ? 'text-teal-700 dark:text-teal-400' : 'text-gray-500 dark:text-gray-400'">
            Nakliye Teslim Bekleniyor
          </p>
          <div class="w-8 h-8 rounded-lg flex items-center justify-center"
            :class="pendingFreightCount > 0 ? 'bg-teal-100 dark:bg-teal-900/30' : 'bg-gray-50 dark:bg-gray-800'">
            <PhoneIcon class="w-4 h-4"
              :class="pendingFreightCount > 0 ? 'text-teal-600' : 'text-gray-400'" />
          </div>
        </div>
        <p class="text-3xl font-bold"
          :class="pendingFreightCount > 0 ? 'text-teal-700 dark:text-teal-300' : 'text-gray-900 dark:text-gray-100'">
          {{ pendingFreightCount }}
        </p>
      </router-link>
    </div>
    </template>

    <!-- ════════ Manager sadeleştirilmiş düzen: Sevkiyatlar + Satınalma ════════ -->
    <template v-else>
      <!-- Sevkiyatlar -->
      <section>
        <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Sevkiyatlar</h2>
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <!-- Gecikmiş -->
          <router-link :to="`/shipments?startDate=${ninetyDaysAgo}&endDate=${yesterday}`"
            class="rounded-xl border p-4 hover:shadow-sm transition-all"
            :class="stats.shipmentsOverdue > 0
              ? 'bg-red-50/30 dark:bg-red-900/10 border-red-200 dark:border-red-800'
              : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
            <div class="flex items-center justify-between mb-3">
              <p class="text-xs font-medium uppercase tracking-wide"
                :class="stats.shipmentsOverdue > 0 ? 'text-red-500' : 'text-gray-500 dark:text-gray-400'">Gecikmiş</p>
              <div class="w-8 h-8 rounded-lg flex items-center justify-center"
                :class="stats.shipmentsOverdue > 0 ? 'bg-red-100 dark:bg-red-900/30' : 'bg-gray-50 dark:bg-gray-800'">
                <ExclamationCircleIcon class="w-4 h-4" :class="stats.shipmentsOverdue > 0 ? 'text-red-500' : 'text-gray-400'" />
              </div>
            </div>
            <p class="text-3xl font-bold" :class="stats.shipmentsOverdue > 0 ? 'text-red-600' : 'text-gray-900 dark:text-gray-100'">{{ stats.shipmentsOverdue }}</p>
          </router-link>

          <!-- Bugün Hazır Değil -->
          <router-link :to="`/shipments?startDate=${today}&endDate=${today}&statuses=0,1,2`"
            class="rounded-xl border p-4 hover:shadow-sm transition-all"
            :class="stats.todayShipmentsNotReadyCount > 0
              ? 'bg-red-50/40 dark:bg-red-900/10 border-red-200 dark:border-red-800'
              : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
            <div class="flex items-center justify-between mb-3">
              <p class="text-xs font-medium uppercase tracking-wide"
                :class="stats.todayShipmentsNotReadyCount > 0 ? 'text-red-500' : 'text-gray-500 dark:text-gray-400'">Bugün Hazır Değil</p>
              <div class="w-8 h-8 rounded-lg flex items-center justify-center"
                :class="stats.todayShipmentsNotReadyCount > 0 ? 'bg-red-100 dark:bg-red-900/30' : 'bg-gray-50 dark:bg-gray-800'">
                <XCircleIcon class="w-4 h-4" :class="stats.todayShipmentsNotReadyCount > 0 ? 'text-red-500' : 'text-gray-400'" />
              </div>
            </div>
            <p class="text-3xl font-bold" :class="stats.todayShipmentsNotReadyCount > 0 ? 'text-red-600' : 'text-gray-900 dark:text-gray-100'">{{ stats.todayShipmentsNotReadyCount }}</p>
          </router-link>

          <!-- Belirsiz İade -->
          <router-link to="/floating-returns"
            class="rounded-xl border p-4 hover:shadow-sm transition-all"
            :class="stats.pendingFloatingReturns > 0
              ? 'bg-orange-50 dark:bg-orange-900/10 border-orange-300 dark:border-orange-700'
              : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
            <div class="flex items-center justify-between mb-3">
              <p class="text-xs font-medium uppercase tracking-wide"
                :class="stats.pendingFloatingReturns > 0 ? 'text-orange-700 dark:text-orange-400' : 'text-gray-500 dark:text-gray-400'">Belirsiz İade</p>
              <div class="w-8 h-8 rounded-lg flex items-center justify-center"
                :class="stats.pendingFloatingReturns > 0 ? 'bg-orange-100 dark:bg-orange-900/30' : 'bg-gray-50 dark:bg-gray-800'">
                <ExclamationCircleIcon class="w-4 h-4" :class="stats.pendingFloatingReturns > 0 ? 'text-orange-600' : 'text-gray-400'" />
              </div>
            </div>
            <p class="text-3xl font-bold" :class="stats.pendingFloatingReturns > 0 ? 'text-orange-700 dark:text-orange-300' : 'text-gray-900 dark:text-gray-100'">{{ stats.pendingFloatingReturns }}</p>
          </router-link>

          <!-- Nakliye Teslim Bekleniyor -->
          <router-link to="/freight-deliveries"
            class="rounded-xl border p-4 hover:shadow-sm transition-all"
            :class="pendingFreightCount > 0
              ? 'bg-teal-50 dark:bg-teal-900/10 border-teal-300 dark:border-teal-700'
              : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
            <div class="flex items-center justify-between mb-3">
              <p class="text-xs font-medium uppercase tracking-wide"
                :class="pendingFreightCount > 0 ? 'text-teal-700 dark:text-teal-400' : 'text-gray-500 dark:text-gray-400'">Nakliye Teslim Bekleniyor</p>
              <div class="w-8 h-8 rounded-lg flex items-center justify-center"
                :class="pendingFreightCount > 0 ? 'bg-teal-100 dark:bg-teal-900/30' : 'bg-gray-50 dark:bg-gray-800'">
                <PhoneIcon class="w-4 h-4" :class="pendingFreightCount > 0 ? 'text-teal-600' : 'text-gray-400'" />
              </div>
            </div>
            <p class="text-3xl font-bold" :class="pendingFreightCount > 0 ? 'text-teal-700 dark:text-teal-300' : 'text-gray-900 dark:text-gray-100'">{{ pendingFreightCount }}</p>
          </router-link>
        </div>
      </section>

      <!-- Satınalma -->
      <section>
        <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Satınalma</h2>
        <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <!-- Onay Bekleyen PO -->
          <router-link to="/purchase-orders"
            class="rounded-xl border p-4 hover:shadow-sm transition-all"
            :class="stats.pendingPOApprovalCount > 0
              ? 'bg-yellow-50 dark:bg-yellow-900/10 border-yellow-300 dark:border-yellow-700'
              : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
            <div class="flex items-center justify-between mb-3">
              <p class="text-xs font-medium uppercase tracking-wide"
                :class="stats.pendingPOApprovalCount > 0 ? 'text-yellow-700 dark:text-yellow-400' : 'text-gray-500 dark:text-gray-400'">Onay Bekleyen PO</p>
              <div class="w-8 h-8 rounded-lg flex items-center justify-center"
                :class="stats.pendingPOApprovalCount > 0 ? 'bg-yellow-100 dark:bg-yellow-900/30' : 'bg-gray-50 dark:bg-gray-800'">
                <ShoppingCartIcon class="w-4 h-4" :class="stats.pendingPOApprovalCount > 0 ? 'text-yellow-600' : 'text-gray-400'" />
              </div>
            </div>
            <p class="text-3xl font-bold" :class="stats.pendingPOApprovalCount > 0 ? 'text-yellow-700 dark:text-yellow-300' : 'text-gray-900 dark:text-gray-100'">{{ stats.pendingPOApprovalCount }}</p>
          </router-link>

          <!-- Kritik Stok -->
          <div class="rounded-xl border p-4"
            :class="stats.criticalStockCount > 0
              ? 'bg-red-50 dark:bg-red-900/10 border-red-300 dark:border-red-700'
              : 'bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-700'">
            <div class="flex items-center justify-between mb-3">
              <p class="text-xs font-medium uppercase tracking-wide"
                :class="stats.criticalStockCount > 0 ? 'text-red-700 dark:text-red-400' : 'text-gray-500 dark:text-gray-400'">Kritik Stok</p>
              <div class="w-8 h-8 rounded-lg flex items-center justify-center"
                :class="stats.criticalStockCount > 0 ? 'bg-red-100 dark:bg-red-900/30' : 'bg-gray-50 dark:bg-gray-800'">
                <ArchiveBoxIcon class="w-4 h-4" :class="stats.criticalStockCount > 0 ? 'text-red-600' : 'text-gray-400'" />
              </div>
            </div>
            <p class="text-3xl font-bold" :class="stats.criticalStockCount > 0 ? 'text-red-700 dark:text-red-300' : 'text-gray-900 dark:text-gray-100'">{{ stats.criticalStockCount }}</p>
          </div>
        </div>
      </section>
    </template>

    <!-- Pipeline akışı -->
    <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
      <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-4">Sevkiyat Pipeline'ı</h2>
      <div class="flex items-stretch gap-2 overflow-x-auto">
        <div
          v-for="(step, idx) in pipeline"
          :key="step.label"
          class="flex items-center gap-2 flex-shrink-0"
        >
          <router-link
            :to="step.to"
            class="flex flex-col items-center px-3 py-2.5 rounded-lg min-w-[88px] transition-colors"
            :class="step.color"
          >
            <span class="text-2xl font-bold">{{ step.count }}</span>
            <span class="text-[11px] font-medium uppercase tracking-wide mt-0.5">{{ step.label }}</span>
          </router-link>
          <ChevronRightIcon v-if="idx < pipeline.length - 1" class="w-4 h-4 text-gray-300 dark:text-gray-600 flex-shrink-0" />
        </div>
      </div>
    </div>

    <!-- Two-col: waiting-for-vehicle + upcoming -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Bugün araç bekleyenler -->
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
        <div class="flex items-center justify-between mb-4">
          <div>
            <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">Bugün Araç Bekleyenler</h2>
            <p class="text-xs text-gray-400 mt-0.5">Hazır ama şoför/araç atanmamış</p>
          </div>
          <span class="px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-300">
            {{ stats.waitingForVehicleTodayCount }}
          </span>
        </div>
        <div v-if="stats.waitingForVehicleToday.length === 0" class="flex flex-col items-center justify-center py-8 text-gray-400">
          <CheckCircleIcon class="w-10 h-10 mb-2 opacity-40" />
          <p class="text-sm">Tüm hazır sevkiyatlar araç ataması yapılmış</p>
        </div>
        <div v-else class="divide-y divide-gray-100 dark:divide-gray-700">
          <router-link
            v-for="s in stats.waitingForVehicleToday"
            :key="s.id"
            :to="`/shipments/${s.id}`"
            class="flex items-center gap-3 py-2.5 hover:bg-gray-50 dark:hover:bg-gray-800 -mx-2 px-2 rounded-lg transition-colors"
          >
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200 truncate">{{ s.projectName }}</p>
              <p class="text-xs text-gray-400 truncate">{{ s.irsaliyeNo ? `İrs: ${s.irsaliyeNo}` : s.talepNo }}</p>
            </div>
            <span class="px-2 py-0.5 rounded-full text-[11px] font-medium" :class="statusBadge(s.status)">
              {{ statusLabel(s.status) }}
            </span>
          </router-link>
        </div>
      </div>

      <!-- Yaklaşan sevkiyatlar -->
      <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 p-5">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">Yaklaşan Sevkiyatlar</h2>
          <router-link to="/shipments" class="text-xs text-blue-600 hover:underline">Tümünü gör</router-link>
        </div>
        <div v-if="stats.recentShipments.length === 0" class="flex flex-col items-center justify-center py-8 text-gray-400">
          <ClipboardDocumentListIcon class="w-10 h-10 mb-2 opacity-40" />
          <p class="text-sm">Aktif sevkiyat bulunamadı</p>
        </div>
        <div v-else class="divide-y divide-gray-100 dark:divide-gray-700">
          <router-link
            v-for="s in stats.recentShipments"
            :key="s.id"
            :to="`/shipments/${s.id}`"
            class="flex items-center gap-3 py-2.5 hover:bg-gray-50 dark:hover:bg-gray-800 -mx-2 px-2 rounded-lg transition-colors"
          >
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200 truncate">{{ s.projectName }}</p>
              <p class="text-xs text-gray-400 truncate">{{ s.talepNo }}</p>
            </div>
            <div class="flex flex-col items-end gap-1 flex-shrink-0">
              <span class="px-2 py-0.5 rounded-full text-[11px] font-medium" :class="statusBadge(s.status)">
                {{ statusLabel(s.status) }}
              </span>
              <p class="text-[10px] text-gray-400">{{ formatShortDate(s.deliveryDate) }}</p>
            </div>
          </router-link>
        </div>
      </div>
    </div>

    <!-- Kritik stok widget -->
    <CriticalStockWidget :items="criticalStocks" :loading="criticalStocksLoading" />
  </div>
</template>

<script setup lang="ts">
import { computed, ref, onMounted } from 'vue';
import {
  ClipboardDocumentListIcon, TruckIcon, ExclamationCircleIcon, CheckCircleIcon,
  ArchiveBoxIcon, ShoppingCartIcon, XCircleIcon, ChevronRightIcon, PhoneIcon,
} from '@heroicons/vue/24/outline';
import type { DashboardStats, CriticalStockItem } from '../../services/dashboardService';
import { statusLabel, statusBadge, formatShortDate } from '../../utils/shipmentStatusUi';
import CriticalStockWidget from '../CriticalStockWidget.vue';
import freightDeliveryService from '../../services/freightDeliveryService';

const props = defineProps<{
  stats: DashboardStats;
  criticalStocks: CriticalStockItem[];
  criticalStocksLoading: boolean;
  /** Manager için sadeleştirilmiş düzen: Aktif/Bugün/Bu Hafta kartları gizli,
   *  kartlar "Sevkiyatlar" ve "Satınalma" başlıkları altında gruplanır. */
  simplified?: boolean;
}>();

// Nakliyeden teslim bekleyen (tamamlanmamış, süresi geçmemiş) link sayısı
const pendingFreightCount = ref(0);
onMounted(async () => {
  try {
    const list = await freightDeliveryService.list(false);
    pendingFreightCount.value = list.filter(f => !f.isCompleted && !f.isExpired).length;
  } catch {
    pendingFreightCount.value = 0;
  }
});

function dateStr(offsetDays = 0) {
  const d = new Date();
  d.setDate(d.getDate() + offsetDays);
  return d.toISOString().slice(0, 10);
}
const today = dateStr(0);
const yesterday = dateStr(-1);
const ninetyDaysAgo = dateStr(-90);

// Pipeline: Created → AssignedToWarehouse → Picking → ReadyForDispatch → AssignedToVehicle → Dispatched
const pipeline = computed(() => [
  { label: 'Taslak',   count: props.stats.statusDraft,     to: '/shipments?statuses=0', color: 'bg-gray-50 dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-100' },
  { label: 'Depoda',   count: props.stats.statusWarehouse, to: '/shipments?statuses=1', color: 'bg-blue-50 dark:bg-blue-900/20 text-blue-700 dark:text-blue-300 hover:bg-blue-100' },
  { label: 'Toplama',  count: props.stats.statusPicking,   to: '/shipments?statuses=2', color: 'bg-yellow-50 dark:bg-yellow-900/20 text-yellow-700 dark:text-yellow-300 hover:bg-yellow-100' },
  { label: 'Hazır',    count: props.stats.statusReady,     to: '/shipments?statuses=3', color: 'bg-green-50 dark:bg-green-900/20 text-green-700 dark:text-green-300 hover:bg-green-100' },
  { label: 'Araçta',   count: props.stats.statusOnRoute,   to: '/shipments?statuses=4', color: 'bg-indigo-50 dark:bg-indigo-900/20 text-indigo-700 dark:text-indigo-300 hover:bg-indigo-100' },
]);
</script>
