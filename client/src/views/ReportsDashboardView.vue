<template>
  <div class="p-4 sm:p-6 space-y-5" style="font-family: 'Plus Jakarta Sans', system-ui, sans-serif;">

    <!-- ══════════ HUB (rapor seçili değil) ══════════ -->
    <template v-if="!activeTab">
      <!-- başlık -->
      <div class="flex items-center gap-3">
        <span class="w-11 h-11 rounded-xl bg-[#0f2238] text-blue-400 flex items-center justify-center shrink-0">
          <ChartBarIcon class="w-6 h-6" />
        </span>
        <div>
          <h1 class="text-xl font-extrabold text-gray-900 dark:text-white tracking-tight">Raporlar Merkezi</h1>
          <p class="text-[13px] text-gray-500 dark:text-white/55 mt-0.5">Operasyonel raporlara kategori bazında hızlı erişim</p>
        </div>
      </div>

      <!-- arama + tarih -->
      <div class="flex flex-wrap items-center gap-3">
        <div class="flex items-center gap-2.5 h-11 px-3.5 rounded-xl bg-white dark:bg-[#0f2238] border border-gray-200 dark:border-white/10 flex-1 min-w-[200px]">
          <MagnifyingGlassIcon class="w-[18px] h-[18px] text-gray-400 dark:text-white/40" />
          <input v-model="search" type="text" placeholder="Rapor ara…"
                 class="flex-1 bg-transparent outline-none text-sm text-gray-800 dark:text-white placeholder-gray-400 dark:placeholder-white/40" />
        </div>
        <div class="flex items-center gap-2 h-11 px-3.5 rounded-xl bg-white dark:bg-[#0f2238] border border-gray-200 dark:border-white/10 text-sm font-semibold text-gray-700 dark:text-white/80">
          <CalendarDaysIcon class="w-4 h-4 text-blue-600 dark:text-blue-400" /> Bu ay
        </div>
      </div>

      <!-- gruplar -->
      <div v-for="g in filteredCatalog" :key="g.group" class="space-y-3">
        <div class="flex items-center gap-2.5">
          <component :is="g.groupIcon" class="w-[17px] h-[17px] text-blue-600 dark:text-blue-400" />
          <h2 class="text-[15px] font-extrabold text-gray-900 dark:text-white tracking-tight">{{ g.group }}</h2>
          <span class="text-xs text-gray-400 dark:text-white/40 font-semibold">· {{ g.items.length }} rapor</span>
          <div class="flex-1 h-px bg-gray-100 dark:bg-white/5"></div>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-3.5">
          <component
            :is="item.route ? 'router-link' : 'button'"
            v-for="item in g.items" :key="item.key"
            :to="item.route"
            type="button"
            @click="item.route ? null : openReport(item.key)"
            class="flex items-start gap-3.5 p-4 rounded-2xl text-left bg-white dark:bg-[#0f2238] border border-gray-200 dark:border-white/10 hover:border-blue-300 dark:hover:border-blue-500/40 hover:shadow-sm transition-all"
          >
            <span class="w-11 h-11 rounded-xl bg-gray-50 dark:bg-white/5 text-blue-600 dark:text-blue-400 flex items-center justify-center shrink-0">
              <component :is="item.icon" class="w-5 h-5" />
            </span>
            <div class="min-w-0 flex-1">
              <div class="flex items-center gap-2">
                <span class="text-[14.5px] font-bold text-gray-900 dark:text-white tracking-tight">{{ item.title }}</span>
                <span v-if="item.alert" class="w-1.5 h-1.5 rounded-full bg-red-500"></span>
              </div>
              <p class="text-[12.5px] text-gray-500 dark:text-white/55 leading-snug mt-1">{{ item.desc }}</p>
              <div class="flex items-center gap-2 mt-3">
                <span class="inline-flex items-center gap-1 text-[10.5px] font-bold uppercase tracking-wide text-gray-400 dark:text-white/40">
                  <ClockIcon class="w-3 h-3" />{{ item.tag }}
                </span>
                <span class="ml-auto inline-flex items-center gap-1 text-[12.5px] font-bold text-blue-600 dark:text-blue-400">
                  Aç <ArrowRightIcon class="w-3.5 h-3.5" />
                </span>
              </div>
            </div>
          </component>
        </div>
      </div>
    </template>

    <!-- ══════════ DETAY (rapor seçili) ══════════ -->
    <template v-else>
      <!-- breadcrumb + geri -->
      <div class="flex items-center justify-between gap-3">
        <div class="flex items-center gap-2 text-[13px] min-w-0">
          <button @click="openReport(null)" class="font-bold text-blue-600 dark:text-blue-400 hover:underline shrink-0">Raporlar</button>
          <ChevronRightIcon class="w-3.5 h-3.5 text-gray-400 dark:text-white/40 shrink-0" />
          <span class="font-bold text-gray-900 dark:text-white truncate">{{ activeReport?.title || 'Rapor' }}</span>
        </div>
        <button @click="openReport(null)"
          class="shrink-0 inline-flex items-center gap-1.5 h-9 px-3 rounded-lg border border-gray-200 dark:border-white/10 bg-white dark:bg-[#0f2238] text-[13px] font-semibold text-gray-600 dark:text-white/80 hover:bg-gray-50 dark:hover:bg-white/5 transition-colors">
          <ArrowLeftIcon class="w-4 h-4" /> Tüm Raporlar
        </button>
      </div>

      <!-- mevcut rapor bileşeni (içerik değişmedi) -->
      <component :is="currentComponent" />
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch, markRaw } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import {
  ChartBarIcon, MagnifyingGlassIcon, CalendarDaysIcon, ClockIcon, ArrowRightIcon,
  ChevronRightIcon, ArrowLeftIcon, CubeIcon, ArrowUturnLeftIcon, ArchiveBoxIcon,
  ArrowPathIcon, BuildingStorefrontIcon, Squares2X2Icon, ShoppingCartIcon,
  UserIcon, TruckIcon,
} from '@heroicons/vue/24/outline';

import ShipmentSummaryTab from '../components/reports/ShipmentSummaryTab.vue';
import PerformanceTab from '../components/reports/PerformanceTab.vue';
import ZoneMaterialTab from '../components/reports/ZoneMaterialTab.vue';
import StockStatusTab from '../components/reports/StockStatusTab.vue';
import StockMovementsTab from '../components/reports/StockMovementsTab.vue';
import WarehouseStockDistributionTab from '../components/reports/WarehouseStockDistributionTab.vue';
import ReturnsTab from '../components/reports/ReturnsTab.vue';
import PendingReceiptsTab from '../components/reports/PendingReceiptsTab.vue';
import MaterialPurchaseTab from '../components/reports/MaterialPurchaseTab.vue';
import DriverSessionsView from './DriverSessionsView.vue';

const route = useRoute();
const router = useRouter();

// key → inline rapor bileşeni
const COMPONENTS: Record<string, unknown> = {
  shipments: markRaw(ShipmentSummaryTab),
  performance: markRaw(PerformanceTab),
  'zone-material': markRaw(ZoneMaterialTab),
  stock: markRaw(StockStatusTab),
  'stock-movements': markRaw(StockMovementsTab),
  'warehouse-dist': markRaw(WarehouseStockDistributionTab),
  returns: markRaw(ReturnsTab),
  gr: markRaw(PendingReceiptsTab),
  'material-purchase': markRaw(MaterialPurchaseTab),
  'driver-sessions': markRaw(DriverSessionsView),
};

// Rapor kataloğu — gruplu, açıklamalı
interface ReportItem {
  key: string;
  icon: unknown;
  title: string;
  desc: string;
  tag: string;
  alert?: boolean;
  route?: string;
}
interface ReportGroup {
  group: string;
  groupIcon: unknown;
  items: ReportItem[];
}
const catalog: ReportGroup[] = [
  {
    group: 'Sevkiyat & Teslimat', groupIcon: markRaw(TruckIcon),
    items: [
      { key: 'shipments',   icon: markRaw(CubeIcon),            title: 'Sevkiyat Özeti',        desc: 'Dönemsel sevkiyat sayıları, durum dağılımı ve teslim oranları', tag: 'Günlük' },
      { key: 'performance', icon: markRaw(ChartBarIcon),        title: 'Teslimat Performansı',  desc: 'Zamanında teslim, gecikme ve şoför/bölge bazlı performans', tag: 'Günlük' },
      { key: 'returns',     icon: markRaw(ArrowUturnLeftIcon),  title: 'İade Analizi',          desc: 'İade nedenleri, oranları ve bölge/müşteri kırılımı', tag: 'Haftalık' },
    ],
  },
  {
    group: 'Stok & Depo', groupIcon: markRaw(BuildingStorefrontIcon),
    items: [
      { key: 'stock',           icon: markRaw(ArchiveBoxIcon),          title: 'Stok Durumu',         desc: 'Anlık stok seviyeleri, kritik ve fazla stok kalemleri', tag: 'Anlık', alert: true },
      { key: 'stock-movements', icon: markRaw(ArrowPathIcon),           title: 'Stok Hareketleri',    desc: 'Giriş / çıkış / sayım hareketleri ve dönemsel akış', tag: 'Günlük' },
      { key: 'warehouse-dist',  icon: markRaw(BuildingStorefrontIcon),  title: 'Depo Stok Dağılımı',  desc: 'Raf ve lokasyon bazlı stok dağılımı, doluluk oranı', tag: 'Anlık' },
      { key: 'zone-material',   icon: markRaw(Squares2X2Icon),          title: 'Bölge Malzeme Raporu', desc: 'Bölge / proje bazlı malzeme tüketimi ve dağılımı', tag: 'Günlük' },
    ],
  },
  {
    group: 'Satınalma & Tedarik', groupIcon: markRaw(ShoppingCartIcon),
    items: [
      { key: 'pos',               icon: markRaw(ShoppingCartIcon), title: 'Açık Satın Alma',         desc: 'Bekleyen ve kısmi teslim siparişler, tedarikçi durumu', tag: 'Anlık', route: '/purchase-orders' },
      { key: 'gr',                icon: markRaw(CubeIcon),         title: 'Bekleyen Mal Girişi',     desc: 'Onay / işlem bekleyen mal giriş taslakları', tag: 'Anlık' },
      { key: 'material-purchase', icon: markRaw(ChartBarIcon),     title: 'Tedarikçi / Malzeme Raporu', desc: 'Tedarikçi bazlı alım hacmi ve malzeme analizi', tag: 'Aylık' },
    ],
  },
  {
    group: 'Personel', groupIcon: markRaw(UserIcon),
    items: [
      { key: 'driver-sessions', icon: markRaw(UserIcon), title: 'Şoför Puantajı', desc: 'Şoför mesai / sefer süreleri ve oturum kayıtları', tag: 'Aylık' },
    ],
  },
];

const activeTab = ref<string | null>(null);
const search = ref('');

const allItems = computed(() => catalog.flatMap(g => g.items));
const activeReport = computed(() => allItems.value.find(i => i.key === activeTab.value));
const currentComponent = computed(() => (activeTab.value ? COMPONENTS[activeTab.value] : null));

const filteredCatalog = computed(() => {
  const q = search.value.trim().toLocaleLowerCase('tr');
  if (!q) return catalog;
  return catalog
    .map(g => ({ ...g, items: g.items.filter(i => `${i.title} ${i.desc}`.toLocaleLowerCase('tr').includes(q)) }))
    .filter(g => g.items.length > 0);
});

function openReport(key: string | null) {
  activeTab.value = key;
  router.replace({ query: key ? { tab: key } : {} });
}

function syncFromRoute() {
  const tabParam = route.query.tab as string | undefined;
  activeTab.value = tabParam && COMPONENTS[tabParam] ? tabParam : null;
}

onMounted(syncFromRoute);
watch(() => route.query.tab, syncFromRoute);
</script>
