<template>
  <div class="p-4 sm:p-6 space-y-6 max-w-5xl mx-auto">

    <!-- Header -->
    <div>
      <h1 class="text-2xl font-bold text-gray-800 dark:text-gray-100">Rota Optimizasyonu</h1>
      <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">Proje adreslerini Google Maps ile optimize edin.</p>
    </div>

    <!-- Step indicator -->
    <div class="flex items-center gap-2 text-sm">
      <div v-for="(s, i) in stepDefs" :key="s.label" class="contents">
        <div class="flex items-center gap-1.5 flex-shrink-0">
          <div
            class="w-6 h-6 rounded-full text-xs font-bold flex items-center justify-center"
            :class="step > s.n
              ? 'bg-indigo-600 text-white'
              : step === s.n
                ? 'bg-indigo-600 text-white'
                : 'bg-gray-200 dark:bg-gray-700 text-gray-500 dark:text-gray-400'"
          >
            <svg v-if="step > s.n" class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="3">
              <path stroke-linecap="round" stroke-linejoin="round" d="M5 13l4 4L19 7" />
            </svg>
            <span v-else>{{ s.n }}</span>
          </div>
          <span
            class="text-xs font-medium hidden sm:inline"
            :class="step === s.n ? 'text-indigo-600 dark:text-indigo-400' : 'text-gray-400 dark:text-gray-500'"
          >{{ s.label }}</span>
        </div>
        <div v-if="i < stepDefs.length - 1" class="flex-1 h-px bg-gray-200 dark:bg-gray-700" />
      </div>
    </div>

    <!-- ────────────────────────────── STEP 1: Project Selection ────────────────────────────── -->
    <div v-if="step === 1" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-4 space-y-4">
        <!-- Vehicle type -->
        <div class="sm:w-56 flex-shrink-0 space-y-2">
          <label class="block text-sm font-medium text-gray-700 dark:text-gray-300">Araç Tipi</label>
          <div class="flex rounded-lg border border-gray-300 dark:border-gray-700 overflow-hidden text-sm">
            <button
              v-for="v in vehicleTypes"
              :key="v.value"
              @click="vehicleType = v.value"
              class="flex-1 py-2 text-center transition-colors"
              :class="vehicleType === v.value
                ? 'bg-indigo-600 text-white font-medium'
                : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-gray-50 dark:hover:bg-gray-700'"
            >{{ v.label }}</button>
          </div>
          <!-- Bridge crossing checkbox -->
          <label
            v-if="vehicleTypes.find(v => v.value === vehicleType)?.bridge"
            class="flex items-center gap-2 cursor-pointer select-none"
          >
            <input
              type="checkbox"
              v-model="forceBridgeCrossing"
              class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
            />
            <span class="text-xs text-gray-600 dark:text-gray-400">
              <span class="font-medium">{{ vehicleTypes.find(v => v.value === vehicleType)?.bridge }}</span>
              Köprüsü'nden geç
            </span>
          </label>
        </div>

        <!-- Başlangıç Noktası -->
        <div class="mt-5 border-t dark:border-gray-700 pt-5">
          <h3 class="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">Başlangıç Noktası</h3>

          <!-- Radio group -->
          <div class="space-y-2">
            <label class="flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-colors"
              :class="startLocationType === 1 ? 'border-indigo-500 bg-indigo-50 dark:bg-indigo-900/20' : 'border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800'">
              <input type="radio" :value="1" v-model="startLocationType" class="mt-0.5 accent-indigo-600" />
              <div class="flex-1">
                <p class="text-sm font-medium text-gray-800 dark:text-gray-200">Depo</p>
                <p v-if="depotLoaded && depotSettings?.depotName" class="text-xs text-gray-500 dark:text-gray-400">{{ depotSettings.depotName }}</p>
                <p v-else-if="depotLoaded && !depotSettings?.depotLatitude" class="text-xs text-amber-600">
                  Depo tanımlanmamış.
                  <router-link to="/settings/depot" class="underline">Tanımlar sayfasına git</router-link>
                </p>
              </div>
            </label>

            <label class="flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-colors"
              :class="startLocationType === 0 ? 'border-indigo-500 bg-indigo-50 dark:bg-indigo-900/20' : 'border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800'">
              <input type="radio" :value="0" v-model="startLocationType" class="mt-0.5 accent-indigo-600" />
              <div class="flex-1">
                <p class="text-sm font-medium text-gray-800 dark:text-gray-200">Mevcut Konum</p>
                <p v-if="startLocationType === 0 && startLatitude && startLongitude" class="text-xs text-green-600">
                  {{ startLatitude.toFixed(5) }}, {{ startLongitude.toFixed(5) }}
                </p>
                <button v-if="startLocationType === 0" @click.prevent="getCurrentLocation()"
                  :disabled="gettingLocation"
                  class="mt-1 text-xs text-indigo-600 hover:underline disabled:opacity-50">
                  {{ gettingLocation ? 'Konum alınıyor...' : 'Konumu Yenile' }}
                </button>
              </div>
            </label>

            <label class="flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-colors"
              :class="startLocationType === 2 ? 'border-indigo-500 bg-indigo-50 dark:bg-indigo-900/20' : 'border-gray-200 dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-800'">
              <input type="radio" :value="2" v-model="startLocationType" class="mt-0.5 accent-indigo-600" />
              <div class="flex-1">
                <p class="text-sm font-medium text-gray-800 dark:text-gray-200">Manuel Adres</p>
                <input v-if="startLocationType === 2"
                  v-model="startManualAddress"
                  @click.stop
                  type="text"
                  placeholder="Başlangıç adresi girin"
                  class="mt-2 w-full border dark:border-gray-600 rounded px-2 py-1.5 text-sm dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-1 focus:ring-indigo-500"
                />
              </div>
            </label>
          </div>

          <p v-if="startLocationError" class="mt-2 text-sm text-red-500">{{ startLocationError }}</p>
        </div>

        <!-- Seçenekler -->
        <div class="mt-4 flex flex-wrap gap-4 items-center border-t dark:border-gray-700 pt-4">
          <label class="flex items-center gap-2 cursor-pointer">
            <input type="checkbox" v-model="returnToStart" class="accent-indigo-600" />
            <span class="text-sm text-gray-700 dark:text-gray-300">Dönüş: Başlangıca Geri Dön</span>
          </label>
          <div class="flex items-center gap-2">
            <label class="text-sm text-gray-700 dark:text-gray-300">Kalkış Saati:</label>
            <input type="time" v-model="departureTime" class="border dark:border-gray-700 rounded px-2 py-1 text-sm dark:bg-gray-800 dark:text-gray-100" />
          </div>
        </div>

        <!-- Project search + select all -->
        <div class="flex gap-3 items-center">
          <input
            v-model="projectSearch"
            type="text"
            placeholder="Proje ara..."
            class="flex-1 border border-gray-300 dark:border-gray-700 rounded-lg px-3 py-2 text-sm dark:bg-gray-800 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
          <button
            @click="toggleSelectAll"
            class="text-sm text-indigo-600 dark:text-indigo-400 hover:underline whitespace-nowrap"
          >
            {{ allFilteredSelected ? 'Tümünü kaldır' : 'Tümünü seç' }}
          </button>
        </div>

        <!-- Loading projects -->
        <div v-if="loadingProjects" class="text-sm text-gray-400 py-4 text-center">Projeler yükleniyor...</div>

        <!-- Project list -->
        <div v-else class="border dark:border-gray-700 rounded-lg overflow-hidden max-h-96 overflow-y-auto">
          <div v-if="filteredProjects.length === 0" class="text-sm text-gray-400 py-6 text-center">Proje bulunamadı.</div>
          <label
            v-for="project in filteredProjects"
            :key="project.code"
            class="flex items-center gap-3 px-4 py-2.5 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 border-b last:border-b-0 dark:border-gray-700"
            :class="{ 'bg-indigo-50 dark:bg-indigo-900/20': selectedCodes.has(project.code) }"
          >
            <input
              type="checkbox"
              :value="project.code"
              :checked="selectedCodes.has(project.code)"
              @change="toggleProject(project.code)"
              class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
            />
            <div class="flex-1 min-w-0">
              <div class="flex items-center gap-2">
                <span class="text-sm font-medium text-gray-800 dark:text-gray-100">{{ project.code }}</span>
                <span class="text-sm text-gray-500 dark:text-gray-400 truncate">{{ project.name }}</span>
              </div>
              <div v-if="project.address" class="text-xs text-gray-400 truncate mt-0.5">{{ project.address }}</div>
              <div v-else class="text-xs text-amber-500 mt-0.5">Adres tanımlı değil</div>
            </div>
          </label>
        </div>

        <div class="flex justify-between items-center pt-1">
          <span class="text-sm text-gray-500 dark:text-gray-400">
            {{ selectedCodes.size }} proje seçili
          </span>
          <button
            @click="goToStep2"
            :disabled="selectedCodes.size === 0 || comparingIss"
            class="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-medium rounded-lg transition-colors flex items-center gap-2"
          >
            <span v-if="comparingIss">
              <svg class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
              </svg>
            </span>
            <span>{{ comparingIss ? 'Karşılaştırılıyor...' : 'ISS ile Karşılaştır' }}</span>
            <ChevronRightIcon v-if="!comparingIss" class="w-4 h-4" />
          </button>
        </div>
      </div>
    </div>

    <!-- ────────────────────────────── STEP 2: ISS Comparison ────────────────────────────── -->
    <div v-if="step === 2" class="space-y-4">
      <div class="bg-white dark:bg-gray-900 rounded-lg shadow overflow-hidden">
        <div class="px-4 py-3 border-b dark:border-gray-700 flex items-center justify-between">
          <div>
            <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">ISS Karşılaştırma Sonuçları</h2>
            <p class="text-xs text-gray-400 mt-0.5">
              {{ comparisonResults.filter(r => r.hasDifference).length }} projede farklılık bulundu.
              İstediğiniz güncellemeleri onaylayın.
            </p>
          </div>
          <div class="flex gap-2">
            <button @click="step = 1" class="text-sm text-gray-500 hover:text-gray-700 dark:hover:text-gray-300">
              ← Geri
            </button>
          </div>
        </div>

        <!-- No differences banner -->
        <div v-if="comparisonResults.filter(r => r.hasDifference).length === 0" class="px-4 py-6 text-center">
          <CheckCircleIcon class="w-10 h-10 text-green-400 mx-auto mb-2" />
          <p class="text-sm font-medium text-gray-700 dark:text-gray-300">Tüm projeler ISS ile güncel.</p>
          <p class="text-xs text-gray-400 mt-1">Hiçbir güncelleme gerekmez.</p>
        </div>

        <!-- Differences table -->
        <div v-else class="overflow-x-auto">
          <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700 text-sm">
            <thead class="bg-gray-50 dark:bg-gray-800">
              <tr>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Proje</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Alan</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Mevcut</th>
                <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">ISS</th>
                <th class="px-4 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">Güncelle</th>
              </tr>
            </thead>
            <tbody class="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-700">
              <template v-for="result in comparisonResults.filter(r => r.hasDifference)" :key="result.projectCode">
                <tr v-if="result.nameChanged">
                  <td class="px-4 py-3 font-medium text-gray-800 dark:text-gray-100" :rowspan="result.nameChanged && result.addressChanged ? 2 : 1">
                    <div>{{ result.projectCode }}</div>
                    <div class="text-xs text-gray-400 font-normal">{{ result.projectName }}</div>
                  </td>
                  <td class="px-4 py-3 text-gray-600 dark:text-gray-400">Proje Adı</td>
                  <td class="px-4 py-3 text-gray-500 dark:text-gray-400 max-w-xs truncate">{{ result.currentName || '—' }}</td>
                  <td class="px-4 py-3 text-gray-800 dark:text-gray-100 max-w-xs truncate font-medium">{{ result.issName || '—' }}</td>
                  <td class="px-4 py-3 text-center">
                    <input
                      type="checkbox"
                      :checked="getSyncApproval(result.projectCode).approveNameUpdate"
                      @change="setSyncApproval(result.projectCode, 'name', ($event.target as HTMLInputElement).checked)"
                      class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                    />
                  </td>
                </tr>
                <tr v-if="result.addressChanged">
                  <td v-if="!result.nameChanged" class="px-4 py-3 font-medium text-gray-800 dark:text-gray-100">
                    <div>{{ result.projectCode }}</div>
                    <div class="text-xs text-gray-400 font-normal">{{ result.projectName }}</div>
                  </td>
                  <td class="px-4 py-3 text-gray-600 dark:text-gray-400">Adres</td>
                  <td class="px-4 py-3 text-gray-500 dark:text-gray-400 max-w-xs truncate">{{ result.currentAddress || '—' }}</td>
                  <td class="px-4 py-3 text-gray-800 dark:text-gray-100 max-w-xs truncate font-medium">{{ result.issAddress || '—' }}</td>
                  <td class="px-4 py-3 text-center">
                    <input
                      type="checkbox"
                      :checked="getSyncApproval(result.projectCode).approveAddressUpdate"
                      @change="setSyncApproval(result.projectCode, 'address', ($event.target as HTMLInputElement).checked)"
                      class="rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
                    />
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
        </div>

        <div class="px-4 py-3 border-t dark:border-gray-700 flex justify-between items-center">
          <span class="text-xs text-gray-400">
            {{ pendingSyncCount }} güncelleme onaylandı
          </span>
          <div class="flex gap-3">
            <button
              v-if="pendingSyncCount > 0"
              @click="applySync"
              :disabled="syncing"
              class="px-4 py-2 bg-amber-600 hover:bg-amber-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition-colors flex items-center gap-2"
            >
              <svg v-if="syncing" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
              </svg>
              {{ syncing ? 'Güncelleniyor...' : 'Seçilileri Güncelle' }}
            </button>
            <button
              @click="runOptimization"
              :disabled="optimizing"
              class="px-5 py-2 bg-indigo-600 hover:bg-indigo-700 disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-medium rounded-lg transition-colors flex items-center gap-2"
            >
              <svg v-if="optimizing" class="animate-spin w-4 h-4" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"/>
              </svg>
              <span>{{ optimizing ? 'Optimize Ediliyor...' : 'Rotayı Optimize Et' }}</span>
              <ChevronRightIcon v-if="!optimizing" class="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ────────────────────────────── STEP 3: Route Result ────────────────────────────── -->
    <div v-if="step === 3 && optimizationResult" class="space-y-4">

      <!-- Summary cards -->
      <div class="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-3 text-center">
          <div class="text-2xl font-bold text-indigo-600">{{ manualStops.length }}</div>
          <div class="text-xs text-gray-500 mt-1">Durak</div>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-3 text-center">
          <div class="text-2xl font-bold text-indigo-600">{{ formatDistance(optimizationResult.totalDistance) }}</div>
          <div class="text-xs text-gray-500 mt-1">Toplam Mesafe</div>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-3 text-center">
          <div class="text-2xl font-bold text-indigo-600">{{ formatDuration(optimizationResult.totalDuration) }}</div>
          <div class="text-xs text-gray-500 mt-1">Tahmini Süre</div>
        </div>
        <div class="bg-white dark:bg-gray-900 rounded-lg shadow p-3 text-center">
          <div class="text-2xl font-bold" :class="optimizationResult.excludedProjects.length > 0 ? 'text-amber-500' : 'text-green-500'">
            {{ optimizationResult.excludedProjects.length }}
          </div>
          <div class="text-xs text-gray-500 mt-1">Hariç Tutulan</div>
        </div>
      </div>

      <!-- Bridge + time window notices -->
      <div v-if="optimizationResult.bridgeNotice"
        class="bg-amber-50 dark:bg-amber-900/20 border border-amber-300 dark:border-amber-600 rounded-lg p-3 flex items-center gap-3">
        <svg class="w-5 h-5 text-amber-600 dark:text-amber-400 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M3 12h18M3 12c0-4.418 4.03-8 9-8s9 3.582 9 8M3 12c0 4.418 4.03 8 9 8s9-3.582 9-8M9 12v4m6-4v4" />
        </svg>
        <div class="text-sm text-amber-800 dark:text-amber-200">
          <span class="font-semibold">Zorunlu Köprü Geçişi:</span>
          {{ optimizationResult.bridgeNotice }}'nden geçiniz.
        </div>
      </div>

      <div v-if="optimizationResult?.timeWindowWarnings?.length"
        class="p-3 bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 rounded-lg">
        <p class="text-sm font-semibold text-amber-800 dark:text-amber-400 mb-2">
          ⚠️ {{ optimizationResult.timeWindowWarnings.length }} proje için teslimat penceresi aşılabilir
        </p>
        <div class="space-y-1">
          <div v-for="w in optimizationResult.timeWindowWarnings" :key="w.projectCode" class="text-xs text-amber-700 dark:text-amber-300">
            <strong>{{ w.projectName }}</strong>: Pencere {{ w.windowStart }}–{{ w.windowEnd }}, Tahmini Varış {{ w.estimatedArrival }}
          </div>
        </div>
      </div>

      <div v-if="optimizationResult.excludedProjects.length > 0"
        class="bg-amber-50 dark:bg-amber-900/20 border border-amber-200 dark:border-amber-700 rounded-lg p-3 flex items-start gap-2">
        <ExclamationTriangleIcon class="w-5 h-5 text-amber-500 flex-shrink-0 mt-0.5" />
        <div class="text-sm text-amber-700 dark:text-amber-300">
          <span class="font-medium">Adres tanımsız:</span>
          {{ optimizationResult.excludedProjects.join(', ') }}
        </div>
      </div>

      <!-- Mobile tab toggle -->
      <div class="flex lg:hidden rounded-lg overflow-hidden border border-gray-200 dark:border-gray-700 text-sm">
        <button
          @click="mapView = 'list'"
          class="flex-1 py-2 font-medium transition-colors"
          :class="mapView === 'list' ? 'bg-indigo-600 text-white' : 'bg-white dark:bg-gray-900 text-gray-600 dark:text-gray-400'"
        >Liste</button>
        <button
          @click="mapView = 'map'"
          class="flex-1 py-2 font-medium transition-colors"
          :class="mapView === 'map' ? 'bg-indigo-600 text-white' : 'bg-white dark:bg-gray-900 text-gray-600 dark:text-gray-400'"
        >Harita</button>
      </div>

      <!-- Split panel -->
      <div class="flex flex-col lg:flex-row gap-4" style="min-height:520px">

        <!-- ── List panel ── -->
        <div
          class="bg-white dark:bg-gray-900 rounded-xl shadow border border-gray-200 dark:border-gray-700 flex flex-col lg:w-96 flex-shrink-0"
          :class="mapView === 'map' ? 'hidden lg:flex' : 'flex'"
        >
          <div class="px-4 py-3 border-b dark:border-gray-700 flex items-center justify-between">
            <div class="flex items-center gap-3">
              <h2 class="text-sm font-semibold text-gray-700 dark:text-gray-300">Optimize Edilmiş Rota</h2>
              <span class="text-xs px-2 py-0.5 rounded-full bg-indigo-100 dark:bg-indigo-900/40 text-indigo-700 dark:text-indigo-300">
                {{ vehicleTypes.find(v => v.value === vehicleType)?.label ?? vehicleType }}
              </span>
            </div>
            <button @click="step = 2" class="text-sm text-gray-500 hover:text-gray-700 dark:hover:text-gray-300">
              ← Geri
            </button>
          </div>

          <p class="px-4 pt-2 text-xs text-gray-400 dark:text-gray-500">
            Sırayı değiştirmek için satırı sürükleyin.
          </p>

          <!-- Draggable stop list -->
          <ol class="flex-1 overflow-y-auto divide-y divide-gray-100 dark:divide-gray-800">
            <li
              v-for="(stop, idx) in manualStops"
              :key="stop.projectCode"
              draggable="true"
              @dragstart="onDragStart(idx)"
              @dragover.prevent="onDragOver(idx)"
              @drop.prevent="onDrop(idx)"
              @dragend="onDragEnd"
              class="flex items-start gap-3 px-4 py-3 cursor-grab active:cursor-grabbing select-none transition-colors"
              :class="[
                dragOverIndex === idx ? 'bg-indigo-50 dark:bg-indigo-900/20' : 'hover:bg-gray-50 dark:hover:bg-gray-800',
                dragIndexRef === idx ? 'opacity-40' : '',
              ]"
            >
              <!-- Drag handle -->
              <div class="flex-shrink-0 mt-1 text-gray-300 dark:text-gray-600">
                <svg class="w-4 h-4" fill="none" viewBox="0 0 16 16">
                  <circle cx="5" cy="4" r="1.2" fill="currentColor"/>
                  <circle cx="11" cy="4" r="1.2" fill="currentColor"/>
                  <circle cx="5" cy="8" r="1.2" fill="currentColor"/>
                  <circle cx="11" cy="8" r="1.2" fill="currentColor"/>
                  <circle cx="5" cy="12" r="1.2" fill="currentColor"/>
                  <circle cx="11" cy="12" r="1.2" fill="currentColor"/>
                </svg>
              </div>
              <!-- Number badge -->
              <div class="flex-shrink-0 w-7 h-7 rounded-full bg-indigo-100 dark:bg-indigo-900/40 text-indigo-700 dark:text-indigo-300 flex items-center justify-center text-xs font-bold">
                {{ idx + 1 }}
              </div>
              <!-- Content -->
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 flex-wrap">
                  <span class="text-xs font-mono bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 px-1.5 py-0.5 rounded">{{ stop.projectCode }}</span>
                  <span v-if="stop.projectName !== stop.projectCode" class="text-sm font-medium text-gray-800 dark:text-gray-100 truncate">{{ stop.projectName }}</span>
                </div>
                <div class="text-xs text-gray-400 mt-0.5 truncate">{{ stop.address || 'Adres tanımlı değil' }}</div>
              </div>
              <div v-if="stop.estimatedDistanceFromPrevious != null" class="flex-shrink-0 text-right">
                <div class="text-xs font-medium text-gray-600 dark:text-gray-400">{{ formatDistance(stop.estimatedDistanceFromPrevious) }}</div>
                <div class="text-xs text-gray-400">{{ formatDuration(stop.estimatedDurationFromPrevious ?? 0) }}</div>
              </div>
            </li>
          </ol>

          <!-- Action buttons -->
          <div class="px-4 py-3 border-t dark:border-gray-700 flex flex-wrap gap-2">
            <button
              @click="openInGoogleMaps"
              class="px-3 py-2 text-sm font-medium text-white bg-green-600 hover:bg-green-700 rounded-lg transition-colors flex items-center gap-1.5"
            >
              <svg class="w-4 h-4" viewBox="0 0 24 24" fill="currentColor">
                <path d="M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z"/>
              </svg>
              Google Maps'te Aç
            </button>
            <button
              @click="copyToClipboard"
              class="px-3 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors flex items-center gap-1.5"
            >
              <ClipboardDocumentIcon class="w-4 h-4" />
              Kopyala
            </button>
            <button
              @click="resetWizard"
              class="px-3 py-2 text-sm font-medium bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg transition-colors"
            >
              Yeni Rota
            </button>
          </div>
        </div>

        <!-- ── Map panel ── -->
        <div
          class="flex-1 rounded-xl overflow-hidden shadow border border-gray-200 dark:border-gray-700"
          :class="mapView === 'list' ? 'hidden lg:block' : 'block'"
          style="min-height:480px"
        >
          <RouteOptimizationMapPanel
            :stops="manualStops"
            :start-latitude="optimizationResult.startLatitude"
            :start-longitude="optimizationResult.startLongitude"
          />
        </div>

      </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue';
import {
  ChevronRightIcon,
  CheckCircleIcon,
  ExclamationTriangleIcon,
  ClipboardDocumentIcon,
} from '@heroicons/vue/24/outline';
import routeOptimizationService, {
  type ProjectSyncComparisonDto,
  type RouteOptimizationResultDto,
  type RouteStopDto,
  type SyncApprovalRequestDto,
} from '../services/routeOptimizationService';
import projectService from '../services/projectService';
import { useNotificationStore } from '../stores/notification';
import { ApiErrorUtils } from '../utils/apiError';
import { turkishIncludes } from '../utils/turkishSearch';
import systemSettingsService from '../services/systemSettingsService';
import RouteOptimizationMapPanel from '../components/RouteOptimizationMapPanel.vue';

// ── State ────────────────────────────────────────────────────────────────────
const stepDefs = [
  { n: 1, label: 'Proje Seç' },
  { n: 2, label: 'ISS Karşılaştır' },
  { n: 3, label: 'Rota Sonucu' },
];

const vehicleTypes = [
  { value: 'Kamyon',   label: 'Kamyon',   bridge: 'Yavuz Sultan Selim' },
  { value: 'Kamyonet', label: 'Kamyonet', bridge: 'Fatih Sultan Mehmet' },
  { value: 'Minibus',  label: 'Minibüs',  bridge: null },
];
const vehicleType = ref('Kamyon');
const forceBridgeCrossing = ref(true);

// Auto-toggle bridge checkbox based on vehicle type
watch(vehicleType, v => {
  const vt = vehicleTypes.find(x => x.value === v);
  forceBridgeCrossing.value = vt?.bridge != null;
});

// Step 1 — Start point
const startLocationType = ref<0 | 1 | 2>(1); // 0=CurrentLocation, 1=Depot, 2=ManualAddress
const startLatitude = ref<number | null>(null);
const startLongitude = ref<number | null>(null);
const startManualAddress = ref('');
const startLocationError = ref('');
const gettingLocation = ref(false);
const departureTime = ref('08:00');
const returnToStart = ref(false);

// Depot settings
const depotSettings = ref<{ depotName: string | null; depotAddress: string | null; depotLatitude: number | null; depotLongitude: number | null } | null>(null);
const depotLoaded = ref(false);
const notificationStore = useNotificationStore();

interface ProjectItem {
  code: string;
  name: string;
  address: string | null;
}

const step = ref<1 | 2 | 3>(1);
const projectSearch = ref('');
const projects = ref<ProjectItem[]>([]);
const loadingProjects = ref(false);
const selectedCodes = ref<Set<string>>(new Set());

const comparisonResults = ref<ProjectSyncComparisonDto[]>([]);
const syncApprovals = ref<Map<string, { approveNameUpdate: boolean; approveAddressUpdate: boolean }>>(new Map());
const comparingIss = ref(false);
const syncing = ref(false);

const optimizationResult = ref<RouteOptimizationResultDto | null>(null);
const optimizing = ref(false);

// Step 3 — map + drag state
const mapView = ref<'list' | 'map'>('list');
const manualStops = ref<RouteStopDto[]>([]);
let dragIndex = -1;
const dragIndexRef = ref(-1);
const dragOverIndex = ref(-1);

// ── Computed ─────────────────────────────────────────────────────────────────
const filteredProjects = computed(() => {
  if (!projectSearch.value) return projects.value;
  return projects.value.filter(
    p => turkishIncludes(p.code, projectSearch.value) || turkishIncludes(p.name, projectSearch.value)
  );
});

const allFilteredSelected = computed(
  () => filteredProjects.value.length > 0 && filteredProjects.value.every(p => selectedCodes.value.has(p.code))
);

const pendingSyncCount = computed(() => {
  let count = 0;
  for (const v of syncApprovals.value.values()) {
    if (v.approveNameUpdate) count++;
    if (v.approveAddressUpdate) count++;
  }
  return count;
});

// ── Project helpers ───────────────────────────────────────────────────────────
function toggleProject(code: string) {
  const next = new Set(selectedCodes.value);
  if (next.has(code)) next.delete(code);
  else next.add(code);
  selectedCodes.value = next;
}

function toggleSelectAll() {
  if (allFilteredSelected.value) {
    const next = new Set(selectedCodes.value);
    filteredProjects.value.forEach(p => next.delete(p.code));
    selectedCodes.value = next;
  } else {
    const next = new Set(selectedCodes.value);
    filteredProjects.value.forEach(p => next.add(p.code));
    selectedCodes.value = next;
  }
}

// ── Sync approval helpers ─────────────────────────────────────────────────────
function getSyncApproval(code: string) {
  return syncApprovals.value.get(code) ?? { approveNameUpdate: false, approveAddressUpdate: false };
}

function setSyncApproval(code: string, field: 'name' | 'address', value: boolean) {
  const cur = getSyncApproval(code);
  const next = new Map(syncApprovals.value);
  next.set(code, {
    approveNameUpdate: field === 'name' ? value : cur.approveNameUpdate,
    approveAddressUpdate: field === 'address' ? value : cur.approveAddressUpdate,
  });
  syncApprovals.value = next;
}

// ── Depot settings & location ─────────────────────────────────────────────────
const loadDepotSettings = async () => {
  try {
    depotSettings.value = await systemSettingsService.getDepotSettings();
    depotLoaded.value = true;
    if (depotSettings.value?.depotLatitude && depotSettings.value?.depotLongitude) {
      startLatitude.value = depotSettings.value.depotLatitude;
      startLongitude.value = depotSettings.value.depotLongitude;
    }
  } catch (e) {
    depotLoaded.value = true;
  }
};

watch(startLocationType, async (type) => {
  startLocationError.value = '';
  startLatitude.value = null;
  startLongitude.value = null;
  if (type === 1 && depotSettings.value?.depotLatitude) {
    startLatitude.value = depotSettings.value.depotLatitude;
    startLongitude.value = depotSettings.value.depotLongitude;
  } else if (type === 0) {
    await getCurrentLocation();
  }
});

const getCurrentLocation = () => {
  return new Promise<void>((resolve) => {
    gettingLocation.value = true;
    startLocationError.value = '';
    if (!navigator.geolocation) {
      startLocationError.value = 'Tarayıcınız konum özelliğini desteklemiyor. Manuel adres girin.';
      startLocationType.value = 2;
      gettingLocation.value = false;
      resolve();
      return;
    }
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        startLatitude.value = pos.coords.latitude;
        startLongitude.value = pos.coords.longitude;
        gettingLocation.value = false;
        resolve();
      },
      () => {
        startLocationError.value = 'Konum alınamadı. Lütfen konum iznini kontrol edin veya manuel adres girin.';
        startLocationType.value = 2;
        gettingLocation.value = false;
        resolve();
      },
      { timeout: 10000 }
    );
  });
};

// ── Step navigation ───────────────────────────────────────────────────────────
async function goToStep2() {
  if (selectedCodes.value.size === 0) return;
  comparingIss.value = true;
  try {
    const codes = Array.from(selectedCodes.value);
    comparisonResults.value = await routeOptimizationService.compareWithIss(codes);
    // Pre-check address updates for projects with differences
    const initApprovals = new Map<string, { approveNameUpdate: boolean; approveAddressUpdate: boolean }>();
    comparisonResults.value.forEach(r => {
      if (r.hasDifference) {
        initApprovals.set(r.projectCode, {
          approveNameUpdate: false,
          approveAddressUpdate: r.addressChanged, // default: approve address updates
        });
      }
    });
    syncApprovals.value = initApprovals;
    step.value = 2;
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'ISS karşılaştırması başarısız.', 'error');
  } finally {
    comparingIss.value = false;
  }
}

async function applySync() {
  const approvals: SyncApprovalRequestDto[] = [];
  for (const [code, approval] of syncApprovals.value.entries()) {
    if (approval.approveNameUpdate || approval.approveAddressUpdate) {
      approvals.push({ projectCode: code, ...approval });
    }
  }
  if (approvals.length === 0) return;

  syncing.value = true;
  try {
    const result = await routeOptimizationService.syncApprovals(approvals);
    notificationStore.add(`${result.updated} proje güncellendi.`, 'success');
    // Clear approved items from the map
    const next = new Map(syncApprovals.value);
    approvals.forEach(a => next.delete(a.projectCode));
    syncApprovals.value = next;
    // Refresh comparison results to reflect updates
    const codes = Array.from(selectedCodes.value);
    comparisonResults.value = await routeOptimizationService.compareWithIss(codes);
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Güncelleme başarısız.', 'error');
  } finally {
    syncing.value = false;
  }
}

async function runOptimization() {
  // Validate start location
  if (startLocationType.value === 0 && (!startLatitude.value || !startLongitude.value)) {
    notificationStore.add('Konum alınamadı. Lütfen konumu yenileyin veya başka bir başlangıç noktası seçin.', 'error');
    return;
  }
  if (startLocationType.value === 1 && (!depotSettings.value?.depotLatitude || !depotSettings.value?.depotLongitude)) {
    notificationStore.add('Depo konumu tanımlanmamış. Tanımlar sayfasından depo bilgilerini girin.', 'error');
    return;
  }
  if (startLocationType.value === 2 && !startManualAddress.value.trim()) {
    notificationStore.add('Lütfen başlangıç adresi girin.', 'error');
    return;
  }

  optimizing.value = true;
  try {
    optimizationResult.value = await routeOptimizationService.optimize({
      projectCodes: Array.from(selectedCodes.value),
      startAddress: startLocationType.value === 2 ? startManualAddress.value : null,
      vehicleType: vehicleType.value,
      forceBridgeCrossing: forceBridgeCrossing.value,
      startLocationType: startLocationType.value,
      startLatitude: startLatitude.value,
      startLongitude: startLongitude.value,
      returnToStart: returnToStart.value,
      departureTime: departureTime.value ? `${departureTime.value}:00` : null,
    });
    manualStops.value = [...(optimizationResult.value?.optimizedStops ?? [])];
    mapView.value = 'list';
    step.value = 3;
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Rota optimizasyonu başarısız.', 'error');
  } finally {
    optimizing.value = false;
  }
}

// ── Drag-to-reorder ───────────────────────────────────────────────────────────
function onDragStart(idx: number) {
  dragIndex = idx;
  dragIndexRef.value = idx;
}

function onDragOver(idx: number) {
  dragOverIndex.value = idx;
}

function onDrop(targetIdx: number) {
  if (dragIndex === -1 || dragIndex === targetIdx) return;
  const arr = [...manualStops.value];
  const moved = arr.splice(dragIndex, 1)[0];
  if (!moved) return;
  arr.splice(targetIdx, 0, moved);
  // Renumber orders to match new positions
  manualStops.value = arr.map((s, i) => ({ ...s, order: i + 1 }));
}

function onDragEnd() {
  dragIndex = -1;
  dragIndexRef.value = -1;
  dragOverIndex.value = -1;
}

// ── Google Maps URL ───────────────────────────────────────────────────────────
function openInGoogleMaps() {
  const stops = manualStops.value;
  if (stops.length === 0) return;

  const result = optimizationResult.value;
  const hasStartCoords = result?.startLatitude != null && result?.startLongitude != null;

  const pointStr = (lat: number | null | undefined, lng: number | null | undefined, addr: string | null | undefined): string => {
    if (lat != null && lng != null) return `${lat},${lng}`;
    return encodeURIComponent(addr ?? '');
  };

  const firstStop = stops[0]!;
  const lastStop = stops[stops.length - 1]!;

  const origin = hasStartCoords
    ? `${result!.startLatitude},${result!.startLongitude}`
    : pointStr(firstStop.latitude, firstStop.longitude, firstStop.address);

  const destination = pointStr(lastStop.latitude, lastStop.longitude, lastStop.address);

  const waypoints = stops.slice(0, -1).map(s =>
    pointStr(s.latitude, s.longitude, s.address)
  ).join('|');

  let url = `https://www.google.com/maps/dir/?api=1&origin=${origin}&destination=${destination}&travelmode=driving`;
  if (waypoints) url += `&waypoints=${waypoints}`;

  window.open(url, '_blank', 'noopener');
}

function resetWizard() {
  step.value = 1;
  selectedCodes.value = new Set();
  comparisonResults.value = [];
  syncApprovals.value = new Map();
  optimizationResult.value = null;
  manualStops.value = [];
  vehicleType.value = 'Kamyon';
  forceBridgeCrossing.value = true;
}

// ── Formatters ────────────────────────────────────────────────────────────────
// Backend sends distance in km, duration in minutes
function formatDistance(km: number): string {
  if (km >= 1) return `${km.toFixed(1)} km`;
  return `${Math.round(km * 1000)} m`;
}

function formatDuration(minutes: number): string {
  const m = Math.round(minutes);
  if (m < 60) return `${m} dk`;
  const h = Math.floor(m / 60);
  const rem = m % 60;
  return rem > 0 ? `${h} sa ${rem} dk` : `${h} sa`;
}

async function copyToClipboard() {
  if (!optimizationResult.value) return;
  const lines = optimizationResult.value.optimizedStops.map(
    s => `${s.order}. ${s.projectCode} — ${s.projectName}${s.address ? `\n   ${s.address}` : ''}`
  );
  const text = lines.join('\n');
  try {
    await navigator.clipboard.writeText(text);
    notificationStore.add('Rota panoya kopyalandı.', 'success');
  } catch {
    notificationStore.add('Kopyalama başarısız.', 'error');
  }
}

// ── Load projects ─────────────────────────────────────────────────────────────
onMounted(async () => {
  loadingProjects.value = true;
  try {
    const data = await projectService.getProjects({ pageSize: 9999 });
    projects.value = data.items.map((p: any) => ({
      code: p.code,
      name: p.name,
      address: p.address ?? null,
    }));
  } catch (err) {
    notificationStore.add(ApiErrorUtils.getErrorMessage(err) || 'Projeler yüklenemedi.', 'error');
  } finally {
    loadingProjects.value = false;
  }
  await loadDepotSettings();
});
</script>
