<template>
  <div>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Depo Adres Yönetimi</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
          Raf adresleri, toplama gözleri ve özel alanlar
        </p>
      </div>
      <div class="flex gap-2 flex-wrap">
        <template v-if="selectedIds.size > 0">
          <button @click="printSelected" :disabled="bulkPrintLoading"
            class="flex items-center gap-2 px-4 py-2 bg-amber-600 hover:bg-amber-700 text-white text-sm font-medium rounded-lg transition-colors disabled:opacity-60">
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"/></svg>
            {{ bulkPrintLoading ? 'Yükleniyor...' : `${selectedIds.size} Etiket Bas` }}
          </button>
          <button v-if="canManage" @click="bulkDeleteSelected"
            class="flex items-center gap-2 px-4 py-2 bg-red-600 hover:bg-red-700 text-white text-sm font-medium rounded-lg transition-colors">
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>
            {{ selectedIds.size }} Adresi Sil
          </button>
        </template>
        <template v-if="canManage">
          <template v-if="viewMode === 'racks'">
            <button @click="showBulkModal = true"
              class="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-lg transition-colors">
              <PlusIcon class="w-4 h-4" /> Toplu Oluştur
            </button>
            <button @click="openCreateModal"
              class="flex items-center gap-2 px-4 py-2 border border-gray-300 dark:border-white/20 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-white/5 text-sm font-medium rounded-lg transition-colors">
              <PlusIcon class="w-4 h-4" /> Tekli Ekle
            </button>
          </template>
          <template v-else-if="viewMode === 'pickingface'">
            <button @click="showPfModal = true"
              class="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-sm font-medium rounded-lg transition-colors">
              <PlusIcon class="w-4 h-4" /> Toplama Gözü Ekle
            </button>
          </template>
          <template v-else-if="viewMode === 'areas'">
            <button @click="showAreaModal = true"
              class="flex items-center gap-2 px-4 py-2 bg-amber-600 hover:bg-amber-700 text-white text-sm font-medium rounded-lg transition-colors">
              <PlusIcon class="w-4 h-4" /> Özel Alan Ekle
            </button>
          </template>
        </template>
      </div>
    </div>

    <!-- Error -->
    <div v-if="error" class="p-3 bg-red-900/30 border border-red-700 rounded-lg flex items-center justify-between">
      <span class="text-red-400 text-sm">{{ error }}</span>
      <button @click="load(); error = null" class="text-red-400 hover:text-red-300 text-sm underline ml-4">Tekrar dene</button>
    </div>

    <!-- View Mode Tabs -->
    <div class="flex gap-1 p-1 bg-gray-100 dark:bg-white/5 rounded-xl w-fit">
      <button
        @click="switchMode('racks')"
        :class="[
          viewMode === 'racks'
            ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
            : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200',
          'px-4 py-2 rounded-lg text-sm font-medium transition-all'
        ]"
      >Raflar</button>
      <button
        @click="switchMode('pickingface')"
        :class="[
          viewMode === 'pickingface'
            ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
            : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200',
          'px-4 py-2 rounded-lg text-sm font-medium transition-all'
        ]"
      >Toplama Gözleri</button>
      <button
        @click="switchMode('areas')"
        :class="[
          viewMode === 'areas'
            ? 'bg-white dark:bg-gray-700 text-gray-900 dark:text-white shadow-sm'
            : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200',
          'px-4 py-2 rounded-lg text-sm font-medium transition-all'
        ]"
      >Özel Alanlar</button>
    </div>

    <!-- Stats -->
    <div class="grid grid-cols-2 sm:grid-cols-4 gap-4">
      <div class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 p-4">
        <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Toplam Adres</p>
        <p class="text-2xl font-bold text-gray-900 dark:text-white">{{ stats.total }}</p>
      </div>
      <div v-if="viewMode === 'racks'" class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 p-4">
        <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Koridor Sayısı</p>
        <p class="text-2xl font-bold text-gray-900 dark:text-white">{{ stats.koridorCount }}</p>
      </div>
      <div v-if="viewMode === 'pickingface' || viewMode === 'areas'" class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 p-4">
        <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">{{ viewMode === 'areas' ? 'Tip Sayısı' : 'Alan Sayısı' }}</p>
        <p class="text-2xl font-bold text-gray-900 dark:text-white">{{ stats.alanCount }}</p>
      </div>
      <div class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 p-4">
        <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Aktif</p>
        <p class="text-2xl font-bold text-green-600 dark:text-green-400">{{ stats.active }}</p>
      </div>
      <div class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 p-4">
        <p class="text-xs text-gray-500 dark:text-gray-400 mb-1">Pasif</p>
        <p class="text-2xl font-bold text-gray-400">{{ stats.inactive }}</p>
      </div>
    </div>

    <!-- Rack Filters -->
    <div v-if="viewMode === 'racks'" class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 p-4 flex flex-wrap gap-3">
      <select v-model="filterKoridor"
        class="px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
        @change="load(true)">
        <option value="">Tüm Koridorlar</option>
        <option v-for="k in [1,2,3,4]" :key="k" :value="k">{{ k }}. Koridor</option>
      </select>

      <select v-model="filterTaraf"
        class="px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
        @change="load(true)">
        <option value="">Tüm Taraflar</option>
        <option value="K">Kuzey (K)</option>
        <option value="G">Güney (G)</option>
      </select>

      <select v-model="filterType"
        class="px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
        @change="load(true)">
        <option value="">Tüm Tipler</option>
        <option v-for="t in rackTypes" :key="t.id" :value="t.id">{{ t.label }}</option>
      </select>

      <label class="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 cursor-pointer">
        <input type="checkbox" v-model="filterInactive" @change="load(true)" class="rounded" />
        Pasif adresleri göster
      </label>
    </div>

    <!-- Areas Filters -->
    <div v-if="viewMode === 'areas'" class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 p-4 flex flex-wrap gap-3">
      <select v-model="filterAreaType"
        class="px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
        @change="load(true)">
        <option value="">Tüm Tipler</option>
        <option v-for="(label, id) in AREA_TYPE_LABELS" :key="id" :value="Number(id)">{{ label }}</option>
      </select>
      <label class="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 cursor-pointer">
        <input type="checkbox" v-model="filterInactive" @change="load(true)" class="rounded" />
        Pasif adresleri göster
      </label>
    </div>

    <!-- PickingFace Filters -->
    <div v-if="viewMode === 'pickingface'" class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 p-4 flex flex-wrap gap-3">
      <select v-model="filterKoridor"
        class="px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
        @change="load(true)">
        <option value="">Tüm Koridorlar</option>
        <option v-for="k in [1,2,3,4]" :key="k" :value="k">{{ k }}. Koridor</option>
      </select>

      <select v-model="filterTaraf"
        class="px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
        @change="load(true)">
        <option value="">Tüm Taraflar</option>
        <option value="K">Kuzey (K)</option>
        <option value="G">Güney (G)</option>
      </select>

      <select v-model="filterAlan"
        class="px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white"
        @change="load(true)">
        <option value="">Tüm Alanlar</option>
        <option v-for="a in uniqueAlans" :key="a" :value="a">{{ a }}</option>
      </select>
      <label class="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 cursor-pointer">
        <input type="checkbox" v-model="filterInactive" @change="load(true)" class="rounded" />
        Pasif adresleri göster
      </label>
    </div>

    <!-- Table -->
    <div class="bg-white dark:bg-[#0f2744] rounded-xl border border-gray-200 dark:border-white/10 overflow-hidden">
      <div v-if="loading" class="flex justify-center py-12">
        <div class="w-7 h-7 border-4 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
      </div>
      <template v-else>
        <!-- Racks Table -->
        <div v-if="viewMode === 'racks' && items.length > 0" class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 dark:bg-white/5 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
              <tr>
                <th class="px-3 py-3 w-8">
                  <input type="checkbox" class="rounded" @change="toggleSelectAll" :checked="allPageSelected" />
                </th>
                <th class="px-4 py-3 text-left">Adres Kodu</th>
                <th class="px-4 py-3 text-left hidden sm:table-cell">Koridor</th>
                <th class="px-4 py-3 text-left hidden sm:table-cell">Taraf</th>
                <th class="px-4 py-3 text-left hidden lg:table-cell">Modül</th>
                <th class="px-4 py-3 text-left hidden lg:table-cell">Kat</th>
                <th class="px-4 py-3 text-left hidden sm:table-cell">Tip</th>
                <th class="px-4 py-3 text-left hidden lg:table-cell">QR Kodu</th>
                <th class="px-4 py-3 text-left hidden xl:table-cell">Kat Sayısı</th>
                <th class="px-4 py-3 text-left">Durum</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-white/5">
              <tr v-for="loc in items" :key="loc.id"
                class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors"
                :class="{ 'opacity-60': !loc.isActive }">
                <td class="px-3 py-3">
                  <input type="checkbox" class="rounded"
                    :checked="selectedIds.has(loc.id)"
                    @change="toggleSelect(loc.id)" />
                </td>
                <td class="px-4 py-3 font-mono font-semibold text-blue-700 dark:text-blue-300">{{ loc.code }}</td>
                <td class="px-4 py-3 text-gray-900 dark:text-white hidden sm:table-cell">{{ loc.koridorNo }}</td>
                <td class="px-4 py-3 hidden sm:table-cell">
                  <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
                    :class="loc.taraf === 'K'
                      ? 'bg-sky-100 dark:bg-sky-900/30 text-sky-700 dark:text-sky-300'
                      : 'bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-300'">
                    {{ loc.taraf === 'K' ? 'Kuzey' : 'Güney' }}
                  </span>
                </td>
                <td class="px-4 py-3 text-gray-700 dark:text-gray-300 hidden lg:table-cell">{{ loc.modulNo.toString().padStart(3,'0') }}</td>
                <td class="px-4 py-3 text-gray-700 dark:text-gray-300 hidden lg:table-cell">{{ loc.kat.toString().padStart(2,'0') }}</td>
                <td class="px-4 py-3 hidden sm:table-cell">
                  <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-violet-100 dark:bg-violet-900/30 text-violet-700 dark:text-violet-300">
                    {{ locationTypeLabel(loc.locationTypeId) }}
                  </span>
                </td>
                <td class="px-4 py-3 hidden lg:table-cell font-mono text-xs text-gray-500 dark:text-gray-400">{{ loc.qrCode ?? '—' }}</td>
                <td class="px-4 py-3 hidden xl:table-cell text-gray-500 dark:text-gray-400 text-xs">{{ loc.totalFloors != null ? `${loc.totalFloors} kat` : '—' }}</td>
                <td class="px-4 py-3">
                  <button v-if="canManage" @click="quickToggleActive(loc)"
                    class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium transition-colors"
                    :class="loc.isActive
                      ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 hover:bg-green-200'
                      : 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400 hover:bg-gray-200'"
                    :title="loc.isActive ? 'Pasife al' : 'Aktife al'">
                    {{ loc.isActive ? 'Aktif' : 'Pasif' }}
                  </button>
                  <span v-else class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
                    :class="loc.isActive
                      ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400'
                      : 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400'">
                    {{ loc.isActive ? 'Aktif' : 'Pasif' }}
                  </span>
                </td>
                <td class="px-4 py-3 text-right">
                  <div class="flex items-center justify-end gap-3">
                    <button @click="openQrModal(loc)"
                      class="inline-flex items-center gap-1 text-xs text-emerald-600 dark:text-emerald-400 hover:underline">
                      <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h4.01M16 20h4M4 12h4m12 0h.01M5 8h2a1 1 0 001-1V5a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1zm12 0h2a1 1 0 001-1V5a1 1 0 00-1-1h-2a1 1 0 00-1 1v2a1 1 0 001 1zM5 20h2a1 1 0 001-1v-2a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1z"/></svg>
                      QR
                    </button>
                    <button v-if="canManage" @click="openEditModal(loc)" class="text-xs text-blue-600 dark:text-blue-400 hover:underline">Düzenle</button>
                    <button v-if="canManage" @click="deleteLocation(loc)" class="text-xs text-red-500 dark:text-red-400 hover:underline">Sil</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- PickingFace Table -->
        <div v-if="viewMode === 'pickingface' && items.length > 0" class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 dark:bg-white/5 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
              <tr>
                <th class="px-3 py-3 w-8">
                  <input type="checkbox" class="rounded" @change="toggleSelectAll" :checked="allPageSelected" />
                </th>
                <th class="px-4 py-3 text-left">Göz Kodu</th>
                <th class="px-4 py-3 text-left">Alan</th>
                <th class="px-4 py-3 text-left hidden sm:table-cell">Açıklama</th>
                <th class="px-4 py-3 text-left">Durum</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-white/5">
              <tr v-for="loc in items" :key="loc.id"
                class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors"
                :class="{ 'opacity-60': !loc.isActive }">
                <td class="px-3 py-3">
                  <input type="checkbox" class="rounded"
                    :checked="selectedIds.has(loc.id)"
                    @change="toggleSelect(loc.id)" />
                </td>
                <td class="px-4 py-3 font-mono font-semibold text-emerald-700 dark:text-emerald-300">{{ loc.code }}</td>
                <td class="px-4 py-3 text-gray-900 dark:text-white text-sm">{{ loc.alan ?? '—' }}</td>
                <td class="px-4 py-3 text-gray-500 dark:text-gray-400 hidden sm:table-cell text-xs">{{ loc.description ?? '—' }}</td>
                <td class="px-4 py-3">
                  <button v-if="canManage" @click="quickToggleActive(loc)"
                    class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium transition-colors"
                    :class="loc.isActive
                      ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 hover:bg-green-200'
                      : 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400 hover:bg-gray-200'"
                    :title="loc.isActive ? 'Pasife al' : 'Aktife al'">
                    {{ loc.isActive ? 'Aktif' : 'Pasif' }}
                  </button>
                  <span v-else class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
                    :class="loc.isActive
                      ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400'
                      : 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400'">
                    {{ loc.isActive ? 'Aktif' : 'Pasif' }}
                  </span>
                </td>
                <td class="px-4 py-3 text-right">
                  <div class="flex items-center justify-end gap-3">
                    <button @click="openQrModal(loc)"
                      class="inline-flex items-center gap-1 text-xs text-emerald-600 dark:text-emerald-400 hover:underline">
                      <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h4.01M16 20h4M4 12h4m12 0h.01M5 8h2a1 1 0 001-1V5a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1zm12 0h2a1 1 0 001-1V5a1 1 0 00-1-1h-2a1 1 0 00-1 1v2a1 1 0 001 1zM5 20h2a1 1 0 001-1v-2a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1z"/></svg>
                      QR
                    </button>
                    <button v-if="canManage" @click="openEditModal(loc)" class="text-xs text-blue-600 dark:text-blue-400 hover:underline">Düzenle</button>
                    <button v-if="canManage" @click="deleteLocation(loc)" class="text-xs text-red-500 dark:text-red-400 hover:underline">Sil</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Areas Table -->
        <div v-if="viewMode === 'areas' && items.length > 0" class="overflow-x-auto">
          <table class="w-full text-sm">
            <thead class="bg-gray-50 dark:bg-white/5 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wide">
              <tr>
                <th class="px-3 py-3 w-8">
                  <input type="checkbox" class="rounded" @change="toggleSelectAll" :checked="allPageSelected" />
                </th>
                <th class="px-4 py-3 text-left">Adres Kodu</th>
                <th class="px-4 py-3 text-left">Alan Adı</th>
                <th class="px-4 py-3 text-left hidden sm:table-cell">Tip</th>
                <th class="px-4 py-3 text-left hidden sm:table-cell">Açıklama</th>
                <th class="px-4 py-3 text-left">Durum</th>
                <th class="px-4 py-3"></th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-white/5">
              <tr v-for="loc in items" :key="loc.id"
                class="hover:bg-gray-50 dark:hover:bg-white/5 transition-colors"
                :class="{ 'opacity-60': !loc.isActive }">
                <td class="px-3 py-3">
                  <input type="checkbox" class="rounded"
                    :checked="selectedIds.has(loc.id)"
                    @change="toggleSelect(loc.id)" />
                </td>
                <td class="px-4 py-3 font-mono font-semibold text-amber-700 dark:text-amber-300">{{ loc.code }}</td>
                <td class="px-4 py-3 text-gray-900 dark:text-white">{{ loc.alan ?? '—' }}</td>
                <td class="px-4 py-3 hidden sm:table-cell">
                  <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 dark:bg-amber-900/30 text-amber-700 dark:text-amber-300">
                    {{ AREA_TYPE_LABELS[loc.locationTypeId] ?? loc.locationType }}
                  </span>
                </td>
                <td class="px-4 py-3 text-gray-500 dark:text-gray-400 hidden sm:table-cell text-xs">{{ loc.description ?? '—' }}</td>
                <td class="px-4 py-3">
                  <button v-if="canManage" @click="quickToggleActive(loc)"
                    class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium transition-colors"
                    :class="loc.isActive
                      ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 hover:bg-green-200'
                      : 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400 hover:bg-gray-200'"
                    :title="loc.isActive ? 'Pasife al' : 'Aktife al'">
                    {{ loc.isActive ? 'Aktif' : 'Pasif' }}
                  </button>
                  <span v-else class="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium"
                    :class="loc.isActive
                      ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400'
                      : 'bg-gray-100 dark:bg-gray-800 text-gray-500 dark:text-gray-400'">
                    {{ loc.isActive ? 'Aktif' : 'Pasif' }}
                  </span>
                </td>
                <td class="px-4 py-3 text-right">
                  <div class="flex items-center justify-end gap-3">
                    <button @click="openQrModal(loc)"
                      class="inline-flex items-center gap-1 text-xs text-amber-600 dark:text-amber-400 hover:underline">
                      <svg class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v1m6 11h2m-6 0h-2v4m0-11v3m0 0h.01M12 12h4.01M16 20h4M4 12h4m12 0h.01M5 8h2a1 1 0 001-1V5a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1zm12 0h2a1 1 0 001-1V5a1 1 0 00-1-1h-2a1 1 0 00-1 1v2a1 1 0 001 1zM5 20h2a1 1 0 001-1v-2a1 1 0 00-1-1H5a1 1 0 00-1 1v2a1 1 0 001 1z"/></svg>
                      QR
                    </button>
                    <button v-if="canManage" @click="openEditModal(loc)" class="text-xs text-blue-600 dark:text-blue-400 hover:underline">Düzenle</button>
                    <button v-if="canManage" @click="deleteLocation(loc)" class="text-xs text-red-500 dark:text-red-400 hover:underline">Sil</button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <div v-if="items.length === 0" class="text-center py-12 text-gray-400 dark:text-gray-500">Henüz adres kaydı yok.</div>
      </template>

      <!-- Pagination -->
      <div v-if="totalCount > pageSize" class="flex items-center justify-between px-4 py-3 border-t border-gray-100 dark:border-white/10">
        <p class="text-xs text-gray-500 dark:text-gray-400">
          {{ totalCount }} adres · Sayfa {{ page }} / {{ Math.ceil(totalCount / pageSize) }}
        </p>
        <div class="flex gap-2">
          <button :disabled="page === 1" @click="changePage(page-1)"
            class="px-3 py-1 text-xs rounded border border-gray-300 dark:border-white/20 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
            ‹ Önceki
          </button>
          <button :disabled="page * pageSize >= totalCount" @click="changePage(page+1)"
            class="px-3 py-1 text-xs rounded border border-gray-300 dark:border-white/20 disabled:opacity-40 hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
            Sonraki ›
          </button>
        </div>
      </div>
    </div>
  </div>

  <!-- ── Toplu Raf Oluşturma Modal ─────────────────────────────────────────── -->
  <BaseModal :show="showBulkModal" title="Toplu Adres Oluştur" maxWidth="md" @close="showBulkModal = false">
    <div class="space-y-4">
      <div class="p-3 bg-blue-50 dark:bg-blue-900/20 rounded-lg text-sm text-blue-800 dark:text-blue-300">
        <span class="font-semibold">Önizleme:</span>
        <span class="font-mono ml-1">{{ bulkPreviewCode }}</span>
        <span class="ml-2 text-blue-600 dark:text-blue-400 font-medium">({{ bulkPreviewCount }} adres)</span>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Koridor</label>
          <select v-model.number="bulk.koridorNo"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500">
            <option v-for="k in [1,2,3,4]" :key="k" :value="k">{{ k }}. Koridor</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Taraf</label>
          <select v-model="bulk.taraf"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500">
            <option value="K">Kuzey (K)</option>
            <option value="G">Güney (G)</option>
          </select>
        </div>
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Modül Aralığı</label>
        <div class="flex items-center gap-2">
          <input v-model.number="bulk.modulFrom" type="number" min="1" placeholder="Başlangıç"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
          <span class="text-gray-400 flex-shrink-0">–</span>
          <input v-model.number="bulk.modulTo" type="number" min="1" placeholder="Bitiş"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
        </div>
        <p class="text-xs text-gray-400 mt-1">Örn: 1 – 20 → 001, 002 … 020</p>
      </div>

      <div>
        <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Kat Aralığı</label>
        <div class="flex items-center gap-2">
          <input v-model.number="bulk.katFrom" type="number" min="1" placeholder="Alt kat"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
          <span class="text-gray-400 flex-shrink-0">–</span>
          <input v-model.number="bulk.katTo" type="number" min="1" placeholder="Üst kat"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
        </div>
        <p class="text-xs text-gray-400 mt-1">Örn: 1 – 5 → kat 01, 02, 03, 04, 05</p>
      </div>

      <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Raf Tipi</label>
          <select v-model.number="bulk.locationType"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500">
            <option v-for="t in rackTypes" :key="t.id" :value="t.id">{{ t.label }}</option>
          </select>
        </div>
        <div>
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Konteyner Tipi</label>
          <select v-model.number="bulk.containerType"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500">
            <option :value="0">Palet</option>
            <option :value="1">Koli</option>
          </select>
        </div>
      </div>
    </div>
    <template #footer>
      <button @click="showBulkModal = false"
        class="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">İptal</button>
      <button @click="submitBulk" :disabled="bulkSubmitting || bulkPreviewCount === 0"
        class="flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-50 rounded-lg">
        <span v-if="bulkSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
        {{ bulkSubmitting ? 'Oluşturuluyor...' : `${bulkPreviewCount} Adres Oluştur` }}
      </button>
    </template>
  </BaseModal>

  <!-- ── Özel Alan Oluşturma Modal ───────────────────────────────────────── -->
  <BaseModal :show="showAreaModal" title="Özel Alan Ekle" maxWidth="md" @close="showAreaModal = false">
    <div class="space-y-4">
      <!-- Önizleme -->
      <div class="p-3 bg-amber-50 dark:bg-amber-900/20 rounded-lg text-sm text-amber-800 dark:text-amber-300">
        <span class="font-semibold">Önizleme:</span>
        <span class="font-mono ml-1">{{ areaPreviewCode }}</span>
        <span class="ml-2 font-medium">({{ area.count }} adres)</span>
      </div>

      <!-- Tip seçimi -->
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Alan Tipi</label>
        <div class="flex gap-2 flex-wrap">
          <button v-for="(label, typeId) in AREA_TYPE_LABELS" :key="typeId"
            @click="area.locationType = Number(typeId)"
            class="flex-1 py-2 px-3 text-sm font-medium rounded-lg border transition-colors"
            :class="area.locationType === Number(typeId)
              ? 'bg-amber-600 border-amber-600 text-white'
              : 'border-gray-300 dark:border-white/20 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-white/5'">
            {{ label }}
          </button>
        </div>
      </div>

      <!-- Alan adı -->
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Alan Adı</label>
        <input v-model="area.alan" type="text" placeholder="Örn: Mal Kabul Hattı A, İade Deposu, Soğuk Hava"
          class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500" />
        <p class="text-xs text-gray-400 mt-1">Etiket ve listede görünen açıklayıcı isim.</p>
      </div>

      <!-- Kod öneki + adet -->
      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Kod Öneki (Prefix)</label>
          <input v-model="area.prefix" type="text" maxlength="6" placeholder="MAL"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500 font-mono uppercase"
            @input="area.prefix = (area.prefix as string).toUpperCase()" />
          <p class="text-xs text-gray-400 mt-1">Örn: MAL → MAL-001</p>
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Adet</label>
          <input v-model.number="area.count" type="number" min="1" max="100" placeholder="1"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500" />
        </div>
      </div>

      <!-- Açıklama -->
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Açıklama (isteğe bağlı)</label>
        <input v-model="area.description" type="text"
          class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500" />
      </div>
    </div>
    <template #footer>
      <button @click="showAreaModal = false"
        class="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">İptal</button>
      <button @click="submitAreaLocation" :disabled="areaSubmitting || !area.alan.trim() || !area.prefix.trim() || area.count < 1"
        class="flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-amber-600 hover:bg-amber-700 disabled:opacity-50 rounded-lg">
        <span v-if="areaSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
        {{ areaSubmitting ? 'Oluşturuluyor...' : `${area.count} Alan Oluştur` }}
      </button>
    </template>
  </BaseModal>

  <!-- ── Toplama Gözü Oluşturma Modal ─────────────────────────────────────── -->
  <BaseModal :show="showPfModal" title="Toplama Gözü Ekle" maxWidth="md" @close="showPfModal = false">
    <div class="space-y-4">
      <!-- Önizleme -->
      <div class="p-3 bg-emerald-50 dark:bg-emerald-900/20 rounded-lg text-sm text-emerald-800 dark:text-emerald-300">
        <span class="font-semibold">Önizleme:</span>
        <span class="font-mono ml-1">{{ pfPreviewCode }}</span>
        <span class="ml-2 font-medium">({{ pfPreviewCount }} göz)</span>
      </div>

      <!-- Konteyner Tipi -->
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Konteyner Tipi</label>
        <div class="flex gap-2">
          <button v-for="ct in pfContainerTypes" :key="ct.value"
            @click="pf.containerType = ct.value"
            class="flex-1 py-2 px-3 text-sm font-medium rounded-lg border transition-colors"
            :class="pf.containerType === ct.value
              ? 'bg-emerald-600 border-emerald-600 text-white'
              : 'border-gray-300 dark:border-white/20 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-white/5'">
            {{ ct.label }}
          </button>
        </div>
        <p class="text-xs text-gray-400 mt-1.5">
          <template v-if="pf.containerType === 0">Palet alanı: modül bazında tek pozisyon, iç adres yok</template>
          <template v-else-if="pf.containerType === 1">Koli kayar raf: kat = harf (A=zemin, B=orta, C=üst), pozisyon = 01–06</template>
          <template v-else>Kutu gözü: kol harfi (A, B …) + göz numarası ile iç adresleme</template>
        </p>
      </div>

      <!-- Koridor + Taraf -->
      <div class="grid grid-cols-2 gap-3">
        <div>
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Koridor</label>
          <select v-model.number="pf.koridorNo"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-emerald-500">
            <option v-for="k in [1,2,3,4]" :key="k" :value="k">{{ k }}. Koridor</option>
          </select>
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Taraf</label>
          <select v-model="pf.taraf"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-emerald-500">
            <option value="K">Kuzey (K)</option>
            <option value="G">Güney (G)</option>
          </select>
        </div>
      </div>

      <!-- Modül Aralığı -->
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
          Modül Aralığı{{ pf.containerType === 2 ? ' (orta sıra — kutu gözleri)' : '' }}
        </label>
        <div class="flex items-center gap-2">
          <input v-model.number="pf.modulFrom" type="number" min="1" placeholder="1"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-emerald-500" />
          <span class="text-gray-400 flex-shrink-0">–</span>
          <input v-model.number="pf.modulTo" type="number" min="1" placeholder="10"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-emerald-500" />
        </div>
      </div>

      <!-- Kat — yalnızca Palet için -->
      <div v-if="pf.containerType === 0" class="p-2.5 bg-gray-50 dark:bg-white/5 rounded-lg text-xs text-gray-500 dark:text-gray-400">
        Palet toplama gözü zemin seviyesinde oluşturulur (Kat 00).
      </div>

      <!-- Koli / Kutu: harf bazlı iç adres -->
      <template v-if="pf.containerType === 1 || pf.containerType === 2">
        <div>
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
            {{ pf.containerType === 1 ? 'Kat Harfleri (virgülle ayır)' : 'İç Kollar (virgülle ayır)' }}
          </label>
          <input v-model="pf.innerLevelsRaw" type="text"
            :placeholder="pf.containerType === 1 ? 'A, B, C' : 'A, B'"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-emerald-500 font-mono uppercase" />
          <p class="text-xs text-gray-400 mt-1">
            <template v-if="pf.containerType === 1">Örn: "A, B, C" → A=zemin, B=orta, C=üst kat</template>
            <template v-else>Örn: "A, B" → her modülde A ve B kolu</template>
          </p>
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">
            {{ pf.containerType === 1 ? 'Göz Sayısı / Kat' : 'Göz Sayısı / Kol' }}
          </label>
          <input v-model.number="pf.positionsPerLevel" type="number" min="1" max="99"
            :placeholder="pf.containerType === 1 ? '6' : '6'"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-emerald-500" />
          <p class="text-xs text-gray-400 mt-1">
            <template v-if="pf.containerType === 1">Her katta kaç koli pozisyonu (örn: 6 → A01 … A06)</template>
            <template v-else>Her kolda kaç göz (A01, A02 … A06)</template>
          </p>
        </div>
      </template>

      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Açıklama (isteğe bağlı)</label>
        <input v-model="pf.description" type="text"
          class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-emerald-500" />
      </div>
    </div>
    <template #footer>
      <button @click="showPfModal = false"
        class="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">İptal</button>
      <button @click="submitPickingFace" :disabled="pfSubmitting || pfPreviewCount === 0"
        class="flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-emerald-600 hover:bg-emerald-700 disabled:opacity-50 rounded-lg">
        <span v-if="pfSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
        {{ pfSubmitting ? 'Oluşturuluyor...' : `${pfPreviewCount} Göz Oluştur` }}
      </button>
    </template>
  </BaseModal>

  <!-- ── Tekli Düzenleme / Oluşturma Modal ────────────────────────────────── -->
  <BaseModal :show="showFormModal" :title="editTarget ? 'Adres Düzenle' : 'Tekli Adres Ekle'" maxWidth="md" @close="showFormModal = false">
    <div class="space-y-4">
      <!-- Raf: koordinat alanları (yalnızca yeni oluştururken) -->
      <template v-if="!editTarget && viewMode === 'racks'">
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <div>
            <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Koridor</label>
            <select v-model.number="form.koridorNo"
              class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option v-for="k in [1,2,3,4]" :key="k" :value="k">{{ k }}</option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Taraf</label>
            <select v-model="form.taraf"
              class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500">
              <option value="K">Kuzey (K)</option>
              <option value="G">Güney (G)</option>
            </select>
          </div>
          <div>
            <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Modül No</label>
            <input v-model.number="form.modulNo" type="number" min="1"
              class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
          <div>
            <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Kat</label>
            <input v-model.number="form.kat" type="number" min="1"
              class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
          </div>
        </div>
        <div class="text-sm font-mono font-semibold text-blue-700 dark:text-blue-300 px-1">
          → {{ singlePreviewCode }}
        </div>
      </template>

      <!-- Raf tipi -->
      <div v-if="viewMode === 'racks'">
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Raf Tipi</label>
        <select v-model.number="form.locationType"
          class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500">
          <option v-for="t in rackTypes" :key="t.id" :value="t.id">{{ t.label }}</option>
        </select>
      </div>

      <!-- QR Kodu (Raf) -->
      <div v-if="viewMode === 'racks'">
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">QR Kodu (katsız)</label>
        <input v-model="form.qrCode" type="text" placeholder="Örn: 1K-001"
          class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono" />
        <p class="text-xs text-gray-400 mt-1">Boş bırakılırsa QR yazdır butonuyla otomatik oluşturulur.</p>
      </div>

      <!-- Toplam Kat Sayısı (Raf) -->
      <div v-if="viewMode === 'racks'">
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Toplam Kat Sayısı</label>
        <input v-model.number="form.totalFloors" type="number" min="1" placeholder="Örn: 5"
          class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
        <p class="text-xs text-gray-400 mt-1">Terminal QR okutulduktan sonra bu sayıda kat seçeneği gösterir.</p>
      </div>

      <!-- Alan (PickingFace veya Özel Alanlar) -->
      <div v-if="(viewMode === 'pickingface' || viewMode === 'areas') && editTarget">
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Alan Adı</label>
        <input v-model="form.alan" type="text" :placeholder="viewMode === 'areas' ? 'Örn: Mal Kabul Hattı A' : 'Asma Kat Altı'"
          class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-amber-500" />
      </div>

      <!-- Açıklama -->
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Açıklama (isteğe bağlı)</label>
        <input v-model="form.description" type="text"
          class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
      </div>

      <!-- Konteyner Tipi -->
      <div>
        <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Konteyner Tipi</label>
        <select v-model.number="form.containerType"
          class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500">
          <option :value="0">Palet</option>
          <option :value="1">Koli</option>
          <option :value="2">Kutu (İç Adres)</option>
        </select>
      </div>

      <!-- İç Adres (yalnızca Kutu) -->
      <div v-if="form.containerType === 2" class="grid grid-cols-2 gap-3">
        <div>
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">İç Kol (InnerLevel)</label>
          <input v-model="form.innerLevel" type="text" maxlength="2" placeholder="A"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500 font-mono uppercase" />
        </div>
        <div>
          <label class="block text-xs font-medium text-gray-600 dark:text-gray-400 mb-1">Göz No (InnerPosition)</label>
          <input v-model.number="form.innerPosition" type="number" min="1" placeholder="1"
            class="w-full px-3 py-2 text-sm rounded-lg border border-gray-300 dark:border-white/20 bg-white dark:bg-gray-800 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500" />
        </div>
        <div class="col-span-2 text-xs font-mono text-blue-600 dark:text-blue-400 px-1">
          → {{ singlePreviewCode }}{{ form.innerLevel && form.innerPosition ? `-${form.innerLevel.toUpperCase()}${String(form.innerPosition).padStart(2,'0')}` : '' }}
        </div>
      </div>

      <!-- Aktif toggle (sadece düzenlemede) -->
      <div v-if="editTarget" class="flex items-center gap-2">
        <input type="checkbox" v-model="form.isActive" id="isActiveChk" class="rounded" />
        <label for="isActiveChk" class="text-sm text-gray-700 dark:text-gray-300">Aktif</label>
      </div>
    </div>
    <template #footer>
      <button @click="showFormModal = false"
        class="px-4 py-2 text-sm text-gray-600 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg">İptal</button>
      <button @click="submitForm" :disabled="formSubmitting"
        class="flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-50 rounded-lg">
        <span v-if="formSubmitting" class="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
        {{ editTarget ? 'Kaydet' : 'Ekle' }}
      </button>
    </template>
  </BaseModal>

  <!-- ── QR Yazdır Modal ───────────────────────────────────────────────────── -->
  <BaseModal :show="qr_showModal" title="QR Etiketi Yazdır" maxWidth="sm" @close="qr_showModal = false">
    <div v-if="qr_loc">
      <p class="text-xs text-gray-400 uppercase tracking-wider mb-3 font-medium">Etiket Önizleme</p>
      <div class="flex justify-center">
        <div style="background:#fff; padding:14px 14px 18px; border-radius:8px; box-shadow:0 2px 8px rgba(0,0,0,.12);">
          <div style="text-align:center; margin-bottom:10px;">
            <div v-if="qr_loading" style="width:160px; height:160px; background:#f3f4f6; margin:0 auto; display:flex; align-items:center; justify-content:center; border-radius:4px;">
              <svg style="width:24px;height:24px;color:#9ca3af;animation:spin 1s linear infinite" fill="none" viewBox="0 0 24 24"><circle style="opacity:.25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path style="opacity:.75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/></svg>
            </div>
            <img v-else-if="qr_imageBase64" :src="qr_imageBase64" alt="QR" style="width:160px;height:160px;display:block;margin:0 auto;" />
          </div>
          <div style="border:2px solid #1e3a5f; border-radius:6px; padding:6px 12px; text-align:center;">
            <span style="font-size:20px; font-weight:900; letter-spacing:2px; color:#1e3a5f; font-family:'Arial Black',Arial,sans-serif;">{{ qr_loc.code }}</span>
          </div>
          <div v-if="qr_loc.alan" style="text-align:center; margin-top:6px;">
            <span style="font-size:10px; color:#6b7280;">{{ qr_loc.alan }}</span>
          </div>
          <div v-if="qr_qrValue && qr_loc.locationTypeId !== 6" style="text-align:center; margin-top:4px;">
            <span style="font-size:9px; color:#9ca3af; font-family:monospace;">{{ qr_qrValue }}</span>
          </div>
        </div>
      </div>
      <p v-if="qr_loc.locationTypeId !== 6 && !qr_loc.qrCode" class="text-xs text-amber-600 dark:text-amber-400 text-center mt-3">
        QR kodu otomatik oluşturulacak ve kaydedilecek.
      </p>
    </div>
    <template #footer>
      <button @click="printQr" :disabled="qr_loading || !qr_imageBase64"
        class="flex-1 bg-blue-600 text-white py-2.5 rounded-lg hover:bg-blue-700 font-medium text-sm disabled:opacity-50 flex items-center justify-center gap-2 transition-colors">
        <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4a2 2 0 00-2-2H9a2 2 0 00-2 2v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z"/></svg>
        Yazdır
      </button>
      <button @click="qr_showModal = false" class="px-4 py-2 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 rounded text-sm">
        Kapat
      </button>
    </template>
  </BaseModal>

  <!-- Single QR print area -->
  <div id="location-qr-print-area" style="display:none; justify-content:center; align-items:center; min-height:100vh;">
    <div v-if="qr_loc" style="background:#fff; padding:14px 14px 18px; border-radius:8px;">
      <div style="text-align:center; margin-bottom:10px;">
        <img v-if="qr_imageBase64" :src="qr_imageBase64" alt="QR" style="width:160px;height:160px;display:block;margin:0 auto;" />
      </div>
      <div style="border:2px solid #1e3a5f; border-radius:6px; padding:6px 12px; text-align:center;">
        <span style="font-size:20px; font-weight:900; letter-spacing:2px; color:#1e3a5f; font-family:'Arial Black',Arial,sans-serif;">{{ qr_loc?.code }}</span>
      </div>
      <div v-if="qr_loc?.alan" style="text-align:center; margin-top:6px;">
        <span style="font-size:10px; color:#6b7280;">{{ qr_loc.alan }}</span>
      </div>
    </div>
  </div>

  <!-- Bulk QR print area -->
  <div id="bulk-qr-print-area" style="display:none;">
    <div v-for="item in bulkPrintItems" :key="item.code"
      style="background:#fff; padding:14px 14px 18px; border-radius:8px; page-break-after:always; display:flex; flex-direction:column; align-items:center; justify-content:center; min-height:100vh;">
      <div style="text-align:center; margin-bottom:10px;">
        <img :src="item.qrImageBase64" alt="QR" style="width:160px;height:160px;display:block;margin:0 auto;" />
      </div>
      <div style="border:2px solid #1e3a5f; border-radius:6px; padding:6px 12px; text-align:center;">
        <span style="font-size:20px; font-weight:900; letter-spacing:2px; color:#1e3a5f; font-family:'Arial Black',Arial,sans-serif;">{{ item.code }}</span>
      </div>
      <div v-if="item.alan" style="text-align:center; margin-top:6px;">
        <span style="font-size:10px; color:#6b7280;">{{ item.alan }}</span>
      </div>
    </div>
  </div>

  </div>
</template>

<style>
@media print {
  body * { visibility: hidden !important; }
  #location-qr-print-area,
  #location-qr-print-area * { visibility: visible !important; }
  #location-qr-print-area { display: flex !important; }
  #bulk-qr-print-area,
  #bulk-qr-print-area * { visibility: visible !important; }
  #bulk-qr-print-area { display: block !important; }
  @page { size: 60mm 80mm; margin: 0; }
}
</style>

<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue';
import { PlusIcon } from '@heroicons/vue/24/outline';
import warehouseLocationService, { type WarehouseLocation, AREA_TYPE_LABELS } from '../services/warehouseLocationService';
import { useNotificationStore } from '../stores/notification';
import { useAuthStore } from '../stores/auth';
import { ApiErrorUtils } from '../utils/apiError';
import BaseModal from '../components/BaseModal.vue';

// ── Checkbox seçim ─────────────────────────────────────────────────────────
const selectedIds = ref<Set<number>>(new Set());

const allPageSelected = computed(() =>
  items.value.length > 0 && items.value.every(l => selectedIds.value.has(l.id))
);

function toggleSelect(id: number) {
  const s = new Set(selectedIds.value);
  if (s.has(id)) s.delete(id); else s.add(id);
  selectedIds.value = s;
}

function toggleSelectAll() {
  if (allPageSelected.value) {
    const s = new Set(selectedIds.value);
    items.value.forEach(l => s.delete(l.id));
    selectedIds.value = s;
  } else {
    const s = new Set(selectedIds.value);
    items.value.forEach(l => s.add(l.id));
    selectedIds.value = s;
  }
}

// ── Hızlı aktif/pasif toggle ───────────────────────────────────────────────
async function quickToggleActive(loc: WarehouseLocation) {
  const newActive = !loc.isActive;
  try {
    await warehouseLocationService.update(loc.id, {
      locationType:  loc.locationTypeId,
      description:   loc.description,
      maxWeightKg:   loc.maxWeightKg,
      maxPallets:    loc.maxPallets,
      isActive:      newActive,
      alan:          loc.alan,
      qrCode:        loc.qrCode,
      totalFloors:   loc.totalFloors,
      containerType: loc.containerTypeId,
      innerLevel:    loc.innerLevel,
      innerPosition: loc.innerPosition,
    });
    loc.isActive = newActive;
    notify.add(newActive ? `${loc.code} aktife alındı.` : `${loc.code} pasife alındı.`, 'success');
  } catch {
    notify.add('Durum güncellenemedi.', 'error');
  }
}

// ── Toplu silme ────────────────────────────────────────────────────────────
async function bulkDeleteSelected() {
  const count = selectedIds.value.size;
  const ok = await notify.promptConfirm({
    message:     `Seçili ${count} adresi kalıcı olarak silmek istediğinizden emin misiniz?`,
    confirmText: 'Sil',
    type:        'danger',
  });
  if (!ok) return;
  try {
    const ids = [...selectedIds.value];
    const res = await warehouseLocationService.bulkDelete(ids);
    items.value = items.value.filter(l => !ids.includes(l.id));
    totalCount.value -= res.deleted;
    selectedIds.value = new Set();
    notify.add(`${res.deleted} adres silindi.`, 'success');
  } catch {
    notify.add('Toplu silme başarısız.', 'error');
  }
}

// ── Toplu etiket basma ─────────────────────────────────────────────────────
const bulkPrintLoading = ref(false);
const bulkPrintItems   = ref<{ code: string; alan?: string; qrImageBase64: string }[]>([]);

async function printSelected() {
  if (selectedIds.value.size === 0) return;
  bulkPrintLoading.value = true;
  bulkPrintItems.value   = [];
  try {
    const ids = [...selectedIds.value];
    const results: { code: string; alan?: string; qrImageBase64: string }[] = [];
    for (const id of ids) {
      const loc = items.value.find(l => l.id === id);
      const qr  = await warehouseLocationService.getQr(id);
      results.push({ code: loc?.code ?? String(id), alan: loc?.alan, qrImageBase64: qr.qrImageBase64 });
    }
    bulkPrintItems.value = results;
    // Allow Vue to render the bulk print area, then print
    await new Promise(r => setTimeout(r, 150));
    const el = document.getElementById('bulk-qr-print-area');
    if (el) el.style.display = 'block';
    window.print();
    if (el) el.style.display = 'none';
  } catch {
    notify.add('Etiket yüklenemedi.', 'error');
  } finally {
    bulkPrintLoading.value = false;
  }
}

const notify    = useNotificationStore();
const authStore = useAuthStore();
const canManage = computed(() => ['Admin', 'Manager'].includes(authStore.userRole));

// ── Silme ──────────────────────────────────────────────────────────────────
async function deleteLocation(loc: WarehouseLocation) {
  const ok = await notify.promptConfirm({
    message:     `"${loc.code}" adresini kalıcı olarak silmek istediğinizden emin misiniz?`,
    confirmText: 'Sil',
    type:        'danger',
  });
  if (!ok) return;
  try {
    await warehouseLocationService.delete(loc.id);
    items.value = items.value.filter(l => l.id !== loc.id);
    selectedIds.value.delete(loc.id);
    totalCount.value -= 1;
    notify.add(`"${loc.code}" silindi.`, 'success');
  } catch {
    notify.add('Silme işlemi başarısız.', 'error');
  }
}

// PickingFace (6) excluded from rack types
const rackTypes = [
  { id: 0, label: 'Raf' },
  { id: 1, label: 'Zemin İstifleme' },
  { id: 2, label: 'Giriş' },
  { id: 3, label: 'Çıkış' },
  { id: 4, label: 'Karantina' },
  { id: 5, label: 'Hazırlama Alanı' },
];
const allTypeLabels: Record<number, string> = {
  0: 'Raf', 1: 'Zemin İstifleme', 2: 'Giriş', 3: 'Çıkış',
  4: 'Karantina', 5: 'Hazırlama Alanı', 6: 'Toplama Gözü', 7: 'İade',
};
function locationTypeLabel(id: number) { return allTypeLabels[id] ?? String(id); }

// ── View mode ──────────────────────────────────────────────────────────────
type ViewMode = 'racks' | 'pickingface' | 'areas';
const viewMode = ref<ViewMode>('racks');

function switchMode(mode: ViewMode) {
  viewMode.value = mode;
  filterKoridor.value  = '';
  filterTaraf.value    = '';
  filterType.value     = '';
  filterAlan.value     = '';
  filterAreaType.value = '';
  filterInactive.value = false;
  load(true);
}

// ── State ──────────────────────────────────────────────────────────────────
const items      = ref<WarehouseLocation[]>([]);
const loading    = ref(false);
const error      = ref<string | null>(null);
const totalCount = ref(0);
const page       = ref(1);
const pageSize   = 50;

const filterKoridor  = ref<number | ''>('');
const filterTaraf    = ref('');
const filterType     = ref<number | ''>('');
const filterAlan     = ref('');
const filterAreaType = ref<number | ''>('');
const filterInactive = ref(false);

const uniqueAlans = computed(() =>
  [...new Set(items.value.map(l => l.alan).filter(Boolean) as string[])].sort()
);

const stats = computed(() => ({
  total:        totalCount.value,
  koridorCount: new Set(items.value.map(l => l.koridorNo)).size,
  alanCount:    viewMode.value === 'areas'
    ? new Set(items.value.map(l => l.locationTypeId)).size
    : new Set(items.value.map(l => l.alan).filter(Boolean)).size,
  active:       items.value.filter(l =>  l.isActive).length,
  inactive:     items.value.filter(l => !l.isActive).length,
}));

async function load(resetPage = false) {
  if (resetPage) page.value = 1;
  loading.value = true;
  try {
    if (viewMode.value === 'areas') {
      const areaTypeIds = filterAreaType.value !== '' ? [filterAreaType.value as number] : [1, 2, 7];
      const requests = areaTypeIds.map(t =>
        warehouseLocationService.getAll({ type: t, includeInactive: filterInactive.value, page: 1, pageSize: 200 })
      );
      const results = await Promise.all(requests);
      let merged = results.flatMap(r => r.items);
      merged.sort((a, b) => a.code.localeCompare(b.code));
      items.value      = merged;
      totalCount.value = results.reduce((sum, r) => sum + r.totalCount, 0);
      return;
    }

    const typeFilter = viewMode.value === 'pickingface'
      ? 6
      : filterType.value !== '' ? (filterType.value as number) : undefined;

    const usesKoridorFilters = viewMode.value === 'racks' || viewMode.value === 'pickingface';
    const res = await warehouseLocationService.getAll({
      koridorNo:       usesKoridorFilters && filterKoridor.value !== '' ? (filterKoridor.value as number) : undefined,
      taraf:           usesKoridorFilters ? filterTaraf.value || undefined : undefined,
      type:            typeFilter,
      // Raflar listesinde toplama gözleri (PickingFace = 6) görünmesin
      excludeType:     viewMode.value === 'racks' ? 6 : undefined,
      includeInactive: filterInactive.value,
      page:            page.value,
      pageSize,
    });

    // Client-side alan filter for picking face
    let result = res.items;
    if (viewMode.value === 'pickingface' && filterAlan.value)
      result = result.filter(l => l.alan === filterAlan.value);

    items.value      = result;
    totalCount.value = res.totalCount;
  } catch (err) {
    error.value = ApiErrorUtils.getErrorMessage(err) || 'Adres listesi yüklenemedi.';
    notify.add(error.value, 'error');
  } finally {
    loading.value = false;
  }
}

function changePage(p: number) {
  page.value = p;
  load();
}

// ── Bulk Rack Create ───────────────────────────────────────────────────────
const showBulkModal  = ref(false);
const bulkSubmitting = ref(false);

const bulk = reactive({
  koridorNo:     1,
  taraf:         'K',
  modulFrom:     1,
  modulTo:       10,
  katFrom:       1,
  katTo:         5,
  locationType:  0,
  containerType: 0, // 0=Palet, 1=Koli
});

const bulkPreviewCount = computed(() => {
  if (bulk.modulTo < bulk.modulFrom || bulk.katTo < bulk.katFrom) return 0;
  return (bulk.modulTo - bulk.modulFrom + 1) * (bulk.katTo - bulk.katFrom + 1);
});

const bulkPreviewCode = computed(() => {
  const first = `${bulk.koridorNo}${bulk.taraf}-${String(bulk.modulFrom).padStart(3,'0')}-${String(bulk.katFrom).padStart(2,'0')}`;
  const last  = `${bulk.koridorNo}${bulk.taraf}-${String(bulk.modulTo).padStart(3,'0')}-${String(bulk.katTo).padStart(2,'0')}`;
  return `${first} … ${last}`;
});

async function submitBulk() {
  bulkSubmitting.value = true;
  try {
    const res = await warehouseLocationService.bulkCreate({
      koridorNo:     bulk.koridorNo,
      taraf:         bulk.taraf,
      modulFrom:     bulk.modulFrom,
      modulTo:       bulk.modulTo,
      katFrom:       bulk.katFrom,
      katTo:         bulk.katTo,
      locationType:  bulk.locationType,
      containerType: bulk.containerType,
    });
    notify.add(`${res.created} adres oluşturuldu${res.skipped > 0 ? `, ${res.skipped} zaten vardı` : ''}.`, 'success');
    showBulkModal.value = false;
    await load(true);
  } catch {
    notify.add('Toplu oluşturma başarısız.', 'error');
  } finally {
    bulkSubmitting.value = false;
  }
}

// ── Area Location Create ───────────────────────────────────────────────────
const showAreaModal  = ref(false);
const areaSubmitting = ref(false);

const area = reactive({
  locationType: 2,    // default: Receiving (mal kabul)
  alan:         '',
  prefix:       '',
  count:        1,
  description:  '',
});

const areaPreviewCode = computed(() => {
  const p = (area.prefix as string).trim().toUpperCase();
  if (!p) return '—';
  const last = String(area.count).padStart(3, '0');
  return area.count === 1 ? `${p}-001` : `${p}-001 … ${p}-${last}`;
});

async function submitAreaLocation() {
  areaSubmitting.value = true;
  try {
    const res = await warehouseLocationService.createAreaLocation({
      locationType: area.locationType,
      alan:         area.alan.trim(),
      prefix:       (area.prefix as string).trim().toUpperCase(),
      count:        area.count,
      description:  area.description || undefined,
    });
    notify.add(`${res.created} özel alan oluşturuldu${res.skipped > 0 ? `, ${res.skipped} atlandı` : ''}.`, 'success');
    showAreaModal.value = false;
    Object.assign(area, { alan: '', prefix: '', count: 1, description: '' });
    await load(true);
  } catch {
    notify.add('Özel alan oluşturulamadı.', 'error');
  } finally {
    areaSubmitting.value = false;
  }
}

// ── Picking Face Create ────────────────────────────────────────────────────
const showPfModal  = ref(false);
const pfSubmitting = ref(false);

const pfContainerTypes = [
  { value: 0, label: 'Palet' },
  { value: 1, label: 'Koli' },
  { value: 2, label: 'Kutu' },
];

const pf = reactive({
  koridorNo:         2,
  taraf:             'K',
  modulFrom:         1,
  modulTo:           5,
  containerType:     0,   // 0=Palet, 1=Koli, 2=Kutu
  katFrom:           0,   // Palet: fixed 0; Koli: user range
  katTo:             0,
  innerLevelsRaw:    'A, B',
  positionsPerLevel: 6,
  description:       '',
});

const pfInnerLevels = computed(() =>
  pf.innerLevelsRaw
    .split(',')
    .map(s => s.trim().toUpperCase())
    .filter(s => s.length > 0)
);

const pfKatRange = computed(() => Math.max(pf.katTo, pf.katFrom) - pf.katFrom + 1);

const pfPreviewCount = computed(() => {
  if (pf.modulTo < pf.modulFrom) return 0;
  const modulCount = pf.modulTo - pf.modulFrom + 1;
  if (pf.containerType === 1 || pf.containerType === 2) {
    // Koli / Kutu: modül × innerLevels × positions
    if (pfInnerLevels.value.length === 0 || pf.positionsPerLevel < 1) return 0;
    return modulCount * pfInnerLevels.value.length * pf.positionsPerLevel;
  }
  // Palet
  return modulCount * pfKatRange.value;
});

const pfPreviewCode = computed(() => {
  const t  = pf.taraf.toUpperCase();
  const mF = String(pf.modulFrom).padStart(3, '0');
  const mT = String(pf.modulTo).padStart(3, '0');
  if (pf.containerType === 1 || pf.containerType === 2) {
    if (pfInnerLevels.value.length === 0 || pf.positionsPerLevel < 1) return '—';
    const lvF = pfInnerLevels.value[0];
    const lvL = pfInnerLevels.value[pfInnerLevels.value.length - 1];
    const pL  = String(pf.positionsPerLevel).padStart(2, '0');
    return `${pf.koridorNo}${t}-${mF}-00-${lvF}01 … ${pf.koridorNo}${t}-${mT}-00-${lvL}${pL}`;
  }
  // Palet
  const katF  = String(pf.katFrom).padStart(2, '0');
  const katT  = String(Math.max(pf.katTo, pf.katFrom)).padStart(2, '0');
  const first = `${pf.koridorNo}${t}-${mF}-${katF}`;
  const last  = `${pf.koridorNo}${t}-${mT}-${katT}`;
  return first === last ? first : `${first} … ${last}`;
});

async function submitPickingFace() {
  pfSubmitting.value = true;
  try {
    const ct = pf.containerType;
    const res = await warehouseLocationService.createPickingFace({
      koridorNo:     pf.koridorNo,
      taraf:         pf.taraf,
      modulFrom:     pf.modulFrom,
      modulTo:       pf.modulTo,
      containerType: ct,
      ...(ct === 1 || ct === 2
        ? { innerLevels: pfInnerLevels.value, positionsPerLevel: pf.positionsPerLevel }
        : { katFrom: pf.katFrom, katTo: Math.max(pf.katTo, pf.katFrom) }
      ),
      description: pf.description || undefined,
    });
    notify.add(`${res.created} toplama gözü oluşturuldu${res.skipped > 0 ? `, ${res.skipped} zaten vardı` : ''}.`, 'success');
    showPfModal.value = false;
    await load(true);
  } catch {
    notify.add('Toplama gözü oluşturulamadı.', 'error');
  } finally {
    pfSubmitting.value = false;
  }
}

// ── Single Create / Edit ───────────────────────────────────────────────────
const showFormModal  = ref(false);
const formSubmitting = ref(false);
const editTarget     = ref<WarehouseLocation | null>(null);

const form = reactive({
  koridorNo:     1,
  taraf:         'K',
  modulNo:       1,
  kat:           1,
  locationType:  0,
  description:   '',
  isActive:      true,
  alan:          '',
  qrCode:        '',
  totalFloors:   null as number | null,
  containerType: 0,
  innerLevel:    '',
  innerPosition: null as number | null,
});

const singlePreviewCode = computed(() =>
  `${form.koridorNo}${form.taraf}-${String(form.modulNo).padStart(3,'0')}-${String(form.kat).padStart(2,'0')}`
);

function openCreateModal() {
  editTarget.value = null;
  Object.assign(form, {
    koridorNo: 1, taraf: 'K', modulNo: 1, kat: 1,
    locationType: 0, description: '', isActive: true,
    alan: '', qrCode: '', totalFloors: null,
    containerType: 0, innerLevel: '', innerPosition: null,
  });
  showFormModal.value = true;
}

function openEditModal(loc: WarehouseLocation) {
  editTarget.value = loc;
  Object.assign(form, {
    koridorNo:     loc.koridorNo,
    taraf:         loc.taraf,
    modulNo:       loc.modulNo,
    kat:           loc.kat,
    locationType:  loc.locationTypeId,
    description:   loc.description ?? '',
    isActive:      loc.isActive,
    alan:          loc.alan ?? '',
    qrCode:        loc.qrCode ?? '',
    totalFloors:   loc.totalFloors ?? null,
    containerType: loc.containerTypeId ?? 0,
    innerLevel:    loc.innerLevel ?? '',
    innerPosition: loc.innerPosition ?? null,
  });
  showFormModal.value = true;
}

async function submitForm() {
  formSubmitting.value = true;
  try {
    if (editTarget.value) {
      await warehouseLocationService.update(editTarget.value.id, {
        locationType:  form.locationType,
        description:   form.description || undefined,
        isActive:      form.isActive,
        alan:          form.alan || undefined,
        qrCode:        form.qrCode || undefined,
        totalFloors:   form.totalFloors ?? undefined,
        containerType: form.containerType,
        innerLevel:    form.containerType === 2 ? (form.innerLevel || undefined) : undefined,
        innerPosition: form.containerType === 2 ? (form.innerPosition ?? undefined) : undefined,
      });
      notify.add('Adres güncellendi.', 'success');
    } else {
      await warehouseLocationService.create({
        koridorNo:    form.koridorNo,
        taraf:        form.taraf,
        modulNo:      form.modulNo,
        kat:          form.kat,
        locationType: form.locationType,
        description:  form.description || undefined,
      });
      notify.add('Adres eklendi.', 'success');
    }
    showFormModal.value = false;
    await load();
  } catch {
    notify.add('İşlem başarısız.', 'error');
  } finally {
    formSubmitting.value = false;
  }
}

// ── QR ────────────────────────────────────────────────────────────────────
const qr_showModal   = ref(false);
const qr_loc         = ref<WarehouseLocation | null>(null);
const qr_imageBase64 = ref<string | null>(null);
const qr_qrValue     = ref<string | null>(null);
const qr_loading     = ref(false);

async function openQrModal(loc: WarehouseLocation) {
  qr_loc.value         = loc;
  qr_imageBase64.value = null;
  qr_qrValue.value     = null;
  qr_loading.value     = true;
  qr_showModal.value   = true;
  try {
    const res = await warehouseLocationService.getQr(loc.id);
    qr_imageBase64.value = res.qrImageBase64;
    qr_qrValue.value     = res.qrValue;
    // update local qrCode if it was auto-generated
    if (loc.locationTypeId !== 6 && !loc.qrCode) {
      loc.qrCode = res.qrValue;
    }
  } catch (e) {
    notify.add(ApiErrorUtils.getErrorMessage(e) || 'QR oluşturulamadı.', 'error');
    qr_showModal.value = false;
  } finally {
    qr_loading.value = false;
  }
}

function printQr() {
  const el = document.getElementById('location-qr-print-area');
  if (el) el.style.display = 'flex';
  window.print();
  if (el) el.style.display = 'none';
}

onMounted(() => load());
</script>
