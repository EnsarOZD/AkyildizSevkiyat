<template>
  <div
    class="fixed inset-y-0 left-0 z-50 w-64 bg-gray-900 text-white flex flex-col transition-transform duration-300 ease-in-out md:translate-x-0 md:static md:inset-auto"
    :class="isOpen ? 'translate-x-0' : '-translate-x-full'"
  >
    <!-- Logo / Brand -->
    <div class="px-5 py-5 flex justify-between items-center border-b border-gray-800">
      <div class="flex items-center gap-3">
        <img src="/logo-icon.svg" alt="Akyıldız" class="w-9 h-9 flex-shrink-0 brightness-0 invert" />
        <div>
          <p class="text-white font-semibold leading-tight">Akyıldız</p>
          <p class="text-gray-400 text-xs leading-tight">Sevkiyat Yönetimi</p>
        </div>
      </div>
      <button @click="$emit('close')" aria-label="Menüyü kapat" class="md:hidden text-gray-400 hover:text-white p-1 rounded">
        <XMarkIcon class="w-5 h-5" aria-hidden="true" />
      </button>
    </div>

    <!-- Navigation -->
    <nav class="flex-1 px-3 py-4 overflow-y-auto custom-scrollbar" aria-label="Ana menü">
      <div v-for="(group, gIdx) in filteredNav" :key="gIdx" class="mb-5 last:mb-0">

        <!-- Group Title -->
        <div v-if="group.title" class="px-3 mb-1.5 text-[10px] font-bold uppercase tracking-widest"
          :class="group.title === 'Planlanan' ? 'text-gray-600' : 'text-gray-500'">
          {{ group.title }}
        </div>

        <!-- Group Items -->
        <div class="space-y-0.5">
          <component
            v-for="(item, iIdx) in group.items"
            :key="iIdx"
            :is="item.to === '/shipments' ? 'button' : 'router-link'"
            v-bind="item.to === '/shipments' ? {} : { to: item.to }"
            @click="item.to === '/shipments' ? navTo(item) : undefined"
            class="w-full group flex items-center gap-3 px-3 py-2 rounded-lg transition-colors duration-150 hover:bg-gray-800"
            :class="[
              group.title === 'Planlanan' ? 'text-gray-600 hover:text-gray-400 opacity-60' : 'text-gray-400 hover:text-white',
              item.to === '/shipments' && currentRoute.path === '/shipments' ? '!bg-blue-600/20 !text-blue-400 border-l-2 border-blue-400 !opacity-100' : ''
            ]"
            :active-class="item.to !== '/shipments' ? '!bg-blue-600/20 !text-blue-400 border-l-2 border-blue-400 !opacity-100' : undefined"
          >
            <component
              v-if="item.icon"
              :is="item.icon"
              class="w-4 h-4 flex-shrink-0 transition-colors duration-150"
            />
            <span class="text-sm font-medium truncate">{{ item.label }}</span>
            <span v-if="group.title === 'Planlanan'" class="ml-auto text-[9px] text-gray-600 italic shrink-0">yakında</span>
            <span
              v-else-if="item.to === '/reconciliation' && reconciliationStore.openCount > 0"
              class="ml-auto flex-shrink-0 min-w-[18px] h-[18px] rounded-full bg-red-500 text-white text-[10px] font-bold flex items-center justify-center px-1"
            >{{ reconciliationStore.openCount > 99 ? '99+' : reconciliationStore.openCount }}</span>
          </component>
        </div>
      </div>
    </nav>

    <!-- User Info Footer -->
    <div class="px-4 py-3 border-t border-gray-800 flex items-center gap-3">
      <div class="w-7 h-7 rounded-full bg-gray-700 flex items-center justify-center flex-shrink-0">
        <span class="text-xs font-medium text-gray-300">
          {{ userInitial }}
        </span>
      </div>
      <div class="flex-1 min-w-0">
        <p class="text-xs font-medium text-gray-300 truncate">{{ authStore.userEmail || 'Kullanıcı' }}</p>
        <p class="text-[10px] text-gray-500">{{ authStore.userRole }}</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { XMarkIcon } from '@heroicons/vue/24/outline';
import { useAuthStore } from '../stores/auth';
import { useReconciliationStore } from '../stores/reconciliation';
import { NAV_ITEMS } from '../navigation';

const router = useRouter();
const currentRoute = useRoute();

const SHIPMENT_FILTER_KEY = 'shipment_list_last_query';

function navTo(item: { to: string | { name: string } }) {
  if (item.to === '/shipments') {
    try {
      const saved = sessionStorage.getItem(SHIPMENT_FILTER_KEY);
      if (saved) {
        const q = JSON.parse(saved) as Record<string, string>;
        if (Object.keys(q).length) {
          router.push({ path: '/shipments', query: q });
          return;
        }
      }
    } catch { /* ignore */ }
  }
  router.push(item.to as string);
}

const authStore = useAuthStore();
const reconciliationStore = useReconciliationStore();

onMounted(() => {
  const role = authStore.userRole;
  if (role === 'Admin' || role === 'Manager') {
    reconciliationStore.fetchOpenCount();
    // Refresh every 5 minutes
    setInterval(() => reconciliationStore.fetchOpenCount(), 5 * 60 * 1000);
  }
});

const filteredNav = computed(() => {
  const userRole = authStore.userRole;
  return NAV_ITEMS.map(group => ({
    ...group,
    items: group.items.filter(item => {
      if (!item.roles || item.roles.length === 0) return true;
      return item.roles.includes(userRole as any);
    }),
  })).filter(group => group.items.length > 0);
});

const userInitial = computed(() => authStore.userInitial);

defineProps<{ isOpen: boolean }>();
defineEmits(['close']);
</script>

<style scoped>
.custom-scrollbar::-webkit-scrollbar { width: 3px; }
.custom-scrollbar::-webkit-scrollbar-track { background: transparent; }
.custom-scrollbar::-webkit-scrollbar-thumb { background: #374151; border-radius: 10px; }
.custom-scrollbar::-webkit-scrollbar-thumb:hover { background: #4b5563; }
</style>
