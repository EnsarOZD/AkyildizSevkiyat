<template>
  <div
    class="fixed inset-y-0 left-0 z-50 w-[270px] text-white flex flex-col transition-transform duration-300 ease-in-out desktop:translate-x-0 desktop:static desktop:inset-auto"
    style="background: linear-gradient(180deg,#0d2240,#0a1626); font-family: 'Plus Jakarta Sans', system-ui, sans-serif;"
    :class="isOpen ? 'translate-x-0' : '-translate-x-full'"
  >
    <!-- Logo -->
    <div class="px-[18px] pt-5 pb-3.5 flex justify-between items-center">
      <div class="flex items-center gap-2.5">
        <svg width="30" height="30" viewBox="0 0 100 100" class="shrink-0">
          <path d="M28 81 L50 58 L72 81" fill="none" stroke="#60a5fa" stroke-width="8.6" stroke-linecap="round" stroke-linejoin="round" />
          <path d="M18 58 L50 24 L82 58" fill="none" stroke="#fff" stroke-width="11" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
        <div class="leading-none">
          <p class="text-[15px] font-extrabold tracking-[0.16em] text-white">AKYILDIZ</p>
          <p class="text-[8.5px] font-semibold tracking-[0.34em] text-white/40 mt-[3px]">SEVKİYAT</p>
        </div>
      </div>
      <button @click="$emit('close')" aria-label="Menüyü kapat" class="desktop:hidden text-white/50 hover:text-white p-1 rounded">
        <XMarkIcon class="w-5 h-5" />
      </button>
    </div>

    <!-- Arama -->
    <div class="px-3.5 pb-2.5">
      <div class="flex items-center gap-2.5 h-[38px] px-3 rounded-[10px] bg-white/[0.06] border border-white/10 focus-within:border-blue-400/50">
        <MagnifyingGlassIcon class="w-4 h-4 text-white/45 shrink-0" />
        <input v-model="query" type="text" placeholder="Menüde ara…"
          class="flex-1 min-w-0 bg-transparent outline-none text-[13px] text-white placeholder-white/40" />
        <button v-if="query" @click="query = ''" class="text-white/40 hover:text-white"><XMarkIcon class="w-3.5 h-3.5" /></button>
      </div>
    </div>

    <!-- Nav -->
    <nav class="flex-1 px-2.5 pb-2.5 overflow-y-auto custom-scrollbar" aria-label="Ana menü">

      <!-- Arama sonuçları (düz liste) -->
      <template v-if="query.trim()">
        <p v-if="searchResults.length === 0" class="px-3 py-3 text-[13px] text-white/40">Sonuç yok</p>
        <NavLink v-for="item in searchResults" :key="String(item.to)" :item="item" :current="currentRoute" :recon="reconciliationStore.openCount" @navigate="navTo" />
      </template>

      <!-- Normal akordeon -->
      <template v-else>
        <div v-for="(group, gIdx) in filteredNav" :key="gIdx">
          <!-- başlıksız (Dashboard) → düz öğe -->
          <template v-if="!group.title">
            <NavLink v-for="item in group.items" :key="String(item.to)" :item="item" :current="currentRoute" :recon="reconciliationStore.openCount" @navigate="navTo" />
          </template>

          <!-- akordeon grup -->
          <div v-else class="mt-1">
            <button
              @click="toggle(group.title)"
              class="w-full flex items-center gap-2.5 px-3 py-2.5 rounded-[9px] transition-colors text-[11.5px] font-bold uppercase tracking-[0.08em]"
              :class="openGroup === group.title ? 'text-white' : 'text-white/55 hover:text-white/80'"
            >
              <component :is="group.icon" class="w-4 h-4 shrink-0" />
              <span class="flex-1 text-left">{{ group.title }}</span>
              <span v-if="groupBadge(group) > 0" class="min-w-[18px] h-[18px] rounded-full bg-red-500/90 text-white text-[10px] font-bold flex items-center justify-center px-1">{{ groupBadge(group) > 99 ? '99+' : groupBadge(group) }}</span>
              <ChevronRightIcon class="w-3.5 h-3.5 opacity-60 transition-transform" :class="openGroup === group.title ? 'rotate-90' : ''" />
            </button>
            <div v-show="openGroup === group.title" class="pl-1 pb-1">
              <NavLink v-for="item in group.items" :key="String(item.to)" :item="item" :current="currentRoute" :recon="reconciliationStore.openCount" indent @navigate="navTo" />
            </div>
          </div>
        </div>
      </template>
    </nav>

    <!-- Kullanıcı -->
    <div class="m-3 p-3 rounded-xl bg-white/5 border border-white/10 flex items-center gap-2.5">
      <div class="w-9 h-9 rounded-[10px] bg-blue-500/20 flex items-center justify-center shrink-0">
        <span class="text-[13px] font-extrabold text-blue-300">{{ userInitial }}</span>
      </div>
      <div class="flex-1 min-w-0">
        <p class="text-[13px] font-bold text-white truncate">{{ authStore.userName || authStore.userEmail || 'Kullanıcı' }}</p>
        <p class="text-[11px] text-white/45">{{ authStore.userRole }}</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch, onMounted, h, type Component } from 'vue';
import { useRouter, useRoute, RouterLink } from 'vue-router';
import { XMarkIcon, MagnifyingGlassIcon, ChevronRightIcon } from '@heroicons/vue/24/outline';
import { useAuthStore } from '../stores/auth';
import { useReconciliationStore } from '../stores/reconciliation';
import { NAV_ITEMS, type NavItem, type NavGroup } from '../navigation';

const router = useRouter();
const currentRoute = useRoute();
const authStore = useAuthStore();
const reconciliationStore = useReconciliationStore();

defineProps<{ isOpen: boolean }>();
const emit = defineEmits(['close']);

const SHIPMENT_FILTER_KEY = 'shipment_list_last_query';

function navTo(item: NavItem) {
  // /shipments: kayıtlı filtreyle aç (tıklama RouterLink'te preventDefault edilir)
  if (item.to === '/shipments') {
    try {
      const saved = sessionStorage.getItem(SHIPMENT_FILTER_KEY);
      if (saved) {
        const q = JSON.parse(saved) as Record<string, string>;
        if (Object.keys(q).length) router.push({ path: '/shipments', query: q });
      }
    } catch { /* ignore */ }
  }
  // Diğer linklerde gezinmeyi RouterLink yapar; burada yalnızca menüyü kapat.
  emit('close');
}

// rol filtresi
const filteredNav = computed<NavGroup[]>(() => {
  const role = authStore.userRole;
  return NAV_ITEMS.map(group => ({
    ...group,
    items: group.items.filter(item => !item.roles || item.roles.length === 0 || item.roles.includes(role as never)),
  })).filter(group => group.items.length > 0);
});

// akordeon
const openGroup = ref<string | null>(null);
function toggle(title?: string) { if (title) openGroup.value = openGroup.value === title ? null : title; }

// aktif rotayı içeren grubu otomatik aç
function expandActive() {
  const path = currentRoute.path;
  const g = filteredNav.value.find(grp => grp.title && grp.items.some(it => typeof it.to === 'string' && (it.to === path || (it.to !== '/' && path.startsWith(it.to)))));
  if (g?.title) openGroup.value = g.title;
}
onMounted(() => {
  expandActive();
  const role = authStore.userRole;
  if (role === 'Admin' || role === 'Manager') {
    reconciliationStore.fetchOpenCount();
    setInterval(() => reconciliationStore.fetchOpenCount(), 5 * 60 * 1000);
  }
});
watch(() => currentRoute.path, expandActive);

// arama
const query = ref('');
const searchResults = computed<NavItem[]>(() => {
  const q = query.value.trim().toLocaleLowerCase('tr');
  if (!q) return [];
  return filteredNav.value.flatMap(g => g.items).filter(it => it.label.toLocaleLowerCase('tr').includes(q));
});

// grup başlığında toplam rozet (şimdilik yalnızca mutabakat)
function groupBadge(group: NavGroup): number {
  if (openGroup.value === group.title) return 0;
  return group.items.some(it => it.to === '/reconciliation') ? reconciliationStore.openCount : 0;
}

const userInitial = computed(() => authStore.userInitial);

// Satır bileşeni — her zaman RouterLink (tutarlı hizalama)
const NavLink = (props: { item: NavItem; current: ReturnType<typeof useRoute>; recon: number; indent?: boolean }, { emit: e }: { emit: (ev: 'navigate', it: NavItem) => void }) => {
  const it = props.item;
  const isShip = it.to === '/shipments';
  const path = props.current.path;
  const active = isShip ? path === '/shipments' : (it.to === path || (typeof it.to === 'string' && it.to !== '/' && path.startsWith(it.to + '/')) || (it.to === '/' && path === '/'));
  const base = 'w-full group flex items-center gap-3 rounded-[9px] transition-colors text-[13.5px] font-semibold relative text-left ' + (props.indent ? 'pl-4 pr-3 py-2' : 'px-3 py-2.5');
  const cls = active ? ' bg-blue-400/[0.16] text-white' : ' text-white/60 hover:bg-white/5 hover:text-white';
  const children = [
    active ? h('span', { class: 'absolute left-0 top-2 bottom-2 w-[3px] rounded-full bg-blue-400' }) : null,
    it.icon ? h(it.icon as Component, { class: 'w-[17px] h-[17px] shrink-0' }) : null,
    h('span', { class: 'flex-1 truncate' }, it.label),
    (it.to === '/reconciliation' && props.recon > 0)
      ? h('span', { class: 'min-w-[18px] h-[18px] rounded-full bg-red-500 text-white text-[10px] font-bold flex items-center justify-center px-1' }, props.recon > 99 ? '99+' : String(props.recon))
      : null,
  ];
  // Tüm öğeler RouterLink — tutarlı kutu modeli (button UA stili kaymaya yol açıyordu).
  const onClick = (ev: MouseEvent) => {
    if (isShip) {
      try {
        const saved = sessionStorage.getItem(SHIPMENT_FILTER_KEY);
        if (saved && Object.keys(JSON.parse(saved) || {}).length) ev.preventDefault();
      } catch { /* ignore */ }
    }
    e('navigate', it);
  };
  return h(RouterLink as Component, { to: it.to, class: base + cls, onClick }, () => children);
};
</script>

<style scoped>
.custom-scrollbar::-webkit-scrollbar { width: 4px; }
.custom-scrollbar::-webkit-scrollbar-track { background: transparent; }
.custom-scrollbar::-webkit-scrollbar-thumb { background: rgba(255,255,255,0.12); border-radius: 10px; }
.custom-scrollbar::-webkit-scrollbar-thumb:hover { background: rgba(255,255,255,0.2); }
</style>
