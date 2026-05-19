<template>
  <nav
    class="fixed bottom-0 inset-x-0 z-30 md:hidden bg-white dark:bg-gray-900 border-t border-gray-200 dark:border-gray-800"
    :style="{ paddingBottom: 'env(safe-area-inset-bottom, 0px)' }"
  >
    <div class="flex h-16">
      <button
        v-for="item in quickItems"
        :key="item.label"
        @click="navigate(item)"
        class="flex-1 flex flex-col items-center justify-center gap-1 transition-colors min-w-0 px-1"
        :class="isActive(item)
          ? 'text-blue-500 dark:text-blue-400'
          : 'text-gray-500 dark:text-gray-400 active:text-gray-700 dark:active:text-gray-200'"
      >
        <component :is="item.icon" class="w-5 h-5 flex-shrink-0" />
        <span class="text-[10px] font-medium leading-none truncate w-full text-center">{{ item.label }}</span>
      </button>

      <button
        @click="$emit('openMenu')"
        class="flex-1 flex flex-col items-center justify-center gap-1 text-gray-500 dark:text-gray-400 active:text-gray-700 dark:active:text-gray-200 transition-colors min-w-0 px-1"
      >
        <Bars3Icon class="w-5 h-5 flex-shrink-0" />
        <span class="text-[10px] font-medium leading-none">Menü</span>
      </button>
    </div>
  </nav>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { Bars3Icon } from '@heroicons/vue/24/outline';
import { useAuthStore } from '../stores/auth';
import { MOBILE_NAV_BY_ROLE, type MobileQuickItem } from '../navigation';

defineEmits<{ openMenu: [] }>();

const authStore = useAuthStore();
const router = useRouter();
const route = useRoute();

const SHIPMENT_FILTER_KEY = 'shipment_list_last_query';

const quickItems = computed((): MobileQuickItem[] => MOBILE_NAV_BY_ROLE[authStore.userRole] ?? []);

function isActive(item: MobileQuickItem): boolean {
  const path = typeof item.to === 'string' ? item.to : router.resolve(item.to).path;
  if (path === '/') return route.path === '/';
  return route.path.startsWith(path);
}

function navigate(item: MobileQuickItem) {
  if (typeof item.to === 'string' && item.to === '/shipments') {
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
</script>
