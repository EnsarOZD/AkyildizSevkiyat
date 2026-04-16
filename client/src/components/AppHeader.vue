<template>
  <header class="h-14 bg-white dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 flex items-center justify-between px-4 md:px-6 flex-shrink-0">
    <!-- Left: hamburger + breadcrumb -->
    <div class="flex items-center gap-3 min-w-0">
      <button @click="$emit('toggleSidebar')" aria-label="Menüyü aç/kapat" class="md:hidden text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 flex-shrink-0 p-1">
        <Bars3Icon class="h-5 w-5" aria-hidden="true" />
      </button>

      <!-- Breadcrumb -->
      <nav class="flex items-center gap-1 min-w-0" aria-label="Konum yolu">
        <template v-for="(crumb, i) in breadcrumbs" :key="i">
          <ChevronRightIcon v-if="i > 0" class="w-3.5 h-3.5 text-gray-400 dark:text-gray-600 flex-shrink-0" />
          <router-link
            v-if="crumb.to"
            :to="crumb.to"
            class="text-sm text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-300 transition-colors truncate"
          >{{ crumb.label }}</router-link>
          <span
            v-else
            class="text-sm font-semibold text-gray-800 dark:text-gray-100 truncate"
          >{{ crumb.label }}</span>
        </template>
      </nav>
    </div>

    <!-- Right: search + theme toggle + user info + logout -->
    <div class="flex items-center gap-3 flex-shrink-0">
      <HeaderWidget />
      <!-- Global search trigger -->
      <button
        @click="$emit('openSearch')"
        aria-label="Ara (Ctrl+K)"
        class="hidden sm:flex items-center gap-2 px-3 py-1.5 text-sm text-gray-400 bg-gray-100 dark:bg-gray-800 hover:bg-gray-200 dark:hover:bg-gray-700 rounded-lg transition-colors"
      >
        <MagnifyingGlassIcon class="w-4 h-4" aria-hidden="true" />
        <span class="text-xs text-gray-400">Ara</span>
        <kbd class="ml-1 px-1.5 py-0.5 text-[10px] font-medium bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded">Ctrl K</kbd>
      </button>
      <button
        @click="$emit('openSearch')"
        aria-label="Ara"
        class="sm:hidden p-1.5 rounded-lg text-gray-500 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
      >
        <MagnifyingGlassIcon class="h-4 w-4" aria-hidden="true" />
      </button>
      <!-- Dark mode toggle -->
      <button
        @click="themeStore.toggle()"
        :aria-label="themeStore.isDark ? 'Açık moda geç' : 'Koyu moda geç'"
        class="p-1.5 rounded-lg text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
      >
        <SunIcon v-if="themeStore.isDark" class="h-4 w-4" aria-hidden="true" />
        <MoonIcon v-else class="h-4 w-4" aria-hidden="true" />
      </button>

      <div class="h-6 w-px bg-gray-200 dark:bg-gray-700"></div>

      <div class="text-right hidden sm:block">
        <p class="text-xs font-medium text-gray-800 dark:text-gray-200 leading-none mb-0.5">
          {{ authStore.user?.firstName }} {{ authStore.user?.lastName }}
        </p>
        <p class="text-[10px] text-gray-400 dark:text-gray-500">{{ authStore.userRole }}</p>
      </div>

      <div class="h-6 w-px bg-gray-200 dark:bg-gray-700"></div>

      <button
        @click="authStore.logout()"
        aria-label="Çıkış yap"
        class="flex items-center gap-1.5 px-2.5 py-1.5 text-sm text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors"
      >
        <ArrowRightOnRectangleIcon class="h-4 w-4" aria-hidden="true" />
        <span class="hidden md:inline text-sm font-medium">Çıkış</span>
      </button>
    </div>
  </header>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { Bars3Icon, ChevronRightIcon, ArrowRightOnRectangleIcon, SunIcon, MoonIcon, MagnifyingGlassIcon } from '@heroicons/vue/24/outline';
import { useAuthStore } from '../stores/auth';
import { useThemeStore } from '../stores/theme';
import HeaderWidget from './HeaderWidget.vue';

defineEmits(['toggleSidebar', 'openSearch']);

const authStore = useAuthStore();
const themeStore = useThemeStore();
const route = useRoute();

interface BreadcrumbItem { label: string; to?: string; }

const breadcrumbs = computed((): BreadcrumbItem[] => {
  const title = (route.meta.title as string) || 'Dashboard';
  const parents = (route.meta.breadcrumb as BreadcrumbItem[] | undefined) ?? [];

  if (parents.length === 0) {
    return [{ label: title }];
  }

  return [...parents, { label: title }];
});
</script>
