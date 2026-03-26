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
        <div v-if="group.title" class="px-3 mb-1.5 text-[10px] font-bold text-gray-500 uppercase tracking-widest">
          {{ group.title }}
        </div>

        <!-- Group Items -->
        <div class="space-y-0.5">
          <router-link
            v-for="(item, iIdx) in group.items"
            :key="iIdx"
            :to="item.to"
            class="group flex items-center gap-3 px-3 py-2 rounded-lg transition-colors duration-150 text-gray-400 hover:text-white hover:bg-gray-800"
            active-class="!bg-blue-600/20 !text-blue-400 border-l-2 border-blue-400"
          >
            <component
              v-if="item.icon"
              :is="item.icon"
              class="w-4 h-4 flex-shrink-0 transition-colors duration-150"
            />
            <span class="text-sm font-medium truncate">{{ item.label }}</span>

            <span
              v-if="item.badge"
              class="ml-auto px-1.5 py-0.5 text-[9px] font-bold rounded uppercase tracking-wide flex-shrink-0"
              :class="item.badge === 'Beta'
                ? 'bg-yellow-500/15 text-yellow-500 border border-yellow-500/30'
                : 'bg-green-500/15 text-green-500 border border-green-500/30'"
            >
              {{ item.badge }}
            </span>
          </router-link>
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
import { computed } from 'vue';
import { XMarkIcon } from '@heroicons/vue/24/outline';
import { useAuthStore } from '../stores/auth';
import { NAV_ITEMS } from '../navigation';

const authStore = useAuthStore();

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
