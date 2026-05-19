<template>
  <Teleport to="body">
    <Transition name="mobile-menu">
      <div
        v-if="show"
        class="fixed inset-0 z-50 flex flex-col bg-white dark:bg-gray-900 md:hidden overflow-hidden"
      >
        <!-- Header -->
        <div class="flex items-center justify-between px-4 py-4 border-b border-gray-200 dark:border-gray-800 flex-shrink-0">
          <div class="flex items-center gap-3">
            <img src="/logo-icon.svg" alt="Akyıldız" class="w-8 h-8 brightness-0 dark:invert" />
            <div>
              <p class="text-sm font-semibold text-gray-900 dark:text-white leading-tight">
                {{ authStore.userName || 'Kullanıcı' }}
              </p>
              <p class="text-xs text-gray-500 dark:text-gray-400 leading-tight">{{ authStore.userRole }}</p>
            </div>
          </div>
          <button
            @click="$emit('close')"
            class="p-2 rounded-xl text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
            aria-label="Menüyü kapat"
          >
            <XMarkIcon class="w-5 h-5" />
          </button>
        </div>

        <!-- Nav Groups -->
        <nav class="flex-1 overflow-y-auto px-3 py-4" aria-label="Tam menü">
          <div v-for="(group, gIdx) in filteredNav" :key="gIdx" class="mb-5 last:mb-0">
            <p
              v-if="group.title"
              class="px-2 mb-2 text-[10px] font-bold uppercase tracking-widest text-gray-400 dark:text-gray-500"
            >
              {{ group.title }}
            </p>
            <div class="space-y-0.5">
              <button
                v-for="(item, iIdx) in group.items"
                :key="iIdx"
                @click="navigate(item)"
                class="w-full flex items-center gap-3 px-3 py-3 rounded-xl transition-colors text-left"
                :class="isActive(item)
                  ? 'bg-blue-50 dark:bg-blue-900/20 text-blue-600 dark:text-blue-400'
                  : 'text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 active:bg-gray-200 dark:active:bg-gray-700'"
              >
                <component v-if="item.icon" :is="item.icon" class="w-5 h-5 flex-shrink-0" />
                <span class="text-sm font-medium">{{ item.label }}</span>
              </button>
            </div>
          </div>
        </nav>

        <!-- Footer: logout -->
        <div
          class="flex-shrink-0 px-4 py-4 border-t border-gray-200 dark:border-gray-800"
          :style="{ paddingBottom: 'max(1rem, env(safe-area-inset-bottom, 1rem))' }"
        >
          <button
            @click="handleLogout"
            class="w-full flex items-center gap-3 px-3 py-3 rounded-xl text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 active:bg-red-100 dark:active:bg-red-900/30 transition-colors"
          >
            <ArrowRightOnRectangleIcon class="w-5 h-5 flex-shrink-0" />
            <span class="text-sm font-medium">Çıkış Yap</span>
          </button>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRouter, useRoute } from 'vue-router';
import { XMarkIcon, ArrowRightOnRectangleIcon } from '@heroicons/vue/24/outline';
import { useAuthStore } from '../stores/auth';
import { NAV_ITEMS, type NavItem } from '../navigation';

defineProps<{ show: boolean }>();
const emit = defineEmits<{ close: [] }>();

const authStore = useAuthStore();
const router = useRouter();
const route = useRoute();

const SHIPMENT_FILTER_KEY = 'shipment_list_last_query';

const filteredNav = computed(() => {
  const userRole = authStore.userRole;
  return NAV_ITEMS
    .map(group => ({
      ...group,
      items: group.items.filter(item =>
        !item.roles || item.roles.length === 0 || item.roles.includes(userRole as any)
      ),
    }))
    .filter(group => group.items.length > 0);
});

function isActive(item: NavItem): boolean {
  const path = typeof item.to === 'string' ? item.to : router.resolve(item.to).path;
  if (path === '/') return route.path === '/';
  return route.path.startsWith(path);
}

function navigate(item: NavItem) {
  if (typeof item.to === 'string' && item.to === '/shipments') {
    try {
      const saved = sessionStorage.getItem(SHIPMENT_FILTER_KEY);
      if (saved) {
        const q = JSON.parse(saved) as Record<string, string>;
        if (Object.keys(q).length) {
          router.push({ path: '/shipments', query: q });
          emit('close');
          return;
        }
      }
    } catch { /* ignore */ }
  }
  router.push(item.to as string);
  emit('close');
}

function handleLogout() {
  emit('close');
  authStore.logout();
}
</script>

<style scoped>
.mobile-menu-enter-active,
.mobile-menu-leave-active {
  transition: transform 0.28s cubic-bezier(0.32, 0.72, 0, 1), opacity 0.28s ease;
}
.mobile-menu-enter-from,
.mobile-menu-leave-to {
  transform: translateY(100%);
  opacity: 0;
}
</style>
