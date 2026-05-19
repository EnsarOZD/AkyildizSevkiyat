<template>
  <div class="flex h-screen bg-gray-100 dark:bg-[#0c1a2e] relative">
    <!-- Backdrop (sidebar) -->
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

      <main id="main-content" class="flex-1 overflow-x-hidden overflow-y-auto bg-gray-100 dark:bg-[#0c1a2e] p-4 md:p-6 pb-20 md:pb-6 relative">
        <router-view v-slot="{ Component }">
          <Transition name="page" mode="out-in">
            <component :is="Component" :key="$route.path" />
          </Transition>
        </router-view>
      </main>
    </div>

    <!-- Global Search Modal -->
    <GlobalSearchModal :show="searchOpen" @close="searchOpen = false" />

    <!-- Mobile Bottom Nav -->
    <MobileBottomNav @openMenu="isMobileMenuOpen = true" />

    <!-- Mobile Full Menu Modal -->
    <MobileMenuModal :show="isMobileMenuOpen" @close="isMobileMenuOpen = false" />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted, onUnmounted } from 'vue';
import { useRoute } from 'vue-router';
import AppSidebar from '../components/AppSidebar.vue';
import AppHeader from '../components/AppHeader.vue';
import GlobalSearchModal from '../components/GlobalSearchModal.vue';
import MobileBottomNav from '../components/MobileBottomNav.vue';
import MobileMenuModal from '../components/MobileMenuModal.vue';
import { useKeyboardShortcut } from '../composables/useKeyboardShortcut';
import { useNotificationsStore } from '../stores/notifications';

const isSidebarOpen = ref(false);
const isMobileMenuOpen = ref(false);
const searchOpen = ref(false);
const route = useRoute();
const notificationsStore = useNotificationsStore();

onMounted(() => notificationsStore.connectSSE());
onUnmounted(() => notificationsStore.disconnectSSE());

// Ctrl+K → arama modalını aç
useKeyboardShortcut('k', () => { searchOpen.value = true; }, { ctrl: true, ignoreInInputs: false });

// Close overlays on route change (mobile)
watch(() => route.path, () => {
    isSidebarOpen.value = false;
    isMobileMenuOpen.value = false;
});
</script>
