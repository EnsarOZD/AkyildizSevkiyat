<template>
  <div class="flex h-screen bg-gray-100 dark:bg-[#0c1a2e] relative">
    <!-- Backdrop -->
    <div
        v-if="isSidebarOpen"
        @click="isSidebarOpen = false"
        class="fixed inset-0 bg-black bg-opacity-50 z-40 md:hidden"
    ></div>

    <AppSidebar
        :isOpen="isSidebarOpen"
        @close="isSidebarOpen = false"
    />

    <div class="flex-1 flex flex-col overflow-hidden w-full">
      <AppHeader @toggleSidebar="isSidebarOpen = !isSidebarOpen" @openSearch="searchOpen = true" />

      <main id="main-content" class="flex-1 overflow-x-hidden overflow-y-auto bg-gray-100 dark:bg-[#0c1a2e] p-4 md:p-6 relative">
        <router-view v-slot="{ Component }">
          <Transition name="page" mode="out-in">
            <component :is="Component" :key="$route.path" />
          </Transition>
        </router-view>
      </main>
    </div>

    <!-- Global Search Modal -->
    <GlobalSearchModal :show="searchOpen" @close="searchOpen = false" />
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import AppSidebar from '../components/AppSidebar.vue';
import AppHeader from '../components/AppHeader.vue';
import GlobalSearchModal from '../components/GlobalSearchModal.vue';
import { useKeyboardShortcut } from '../composables/useKeyboardShortcut';

const isSidebarOpen = ref(false);
const searchOpen = ref(false);
const route = useRoute();

// Ctrl+K → arama modalını aç
useKeyboardShortcut('k', () => { searchOpen.value = true; }, { ctrl: true, ignoreInInputs: false });

// Close sidebar on route change (mobile)
watch(() => route.path, () => {
    isSidebarOpen.value = false;
});
</script>
