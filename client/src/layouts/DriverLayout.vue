<template>
  <div class="min-h-screen bg-gray-100 dark:bg-[#0c1a2e] flex flex-col">
    <!-- Mobile header -->
    <header class="bg-white dark:bg-[#0f2744] shadow-sm sticky top-0 z-10">
      <div class="flex items-center justify-between px-4 py-3">
        <div class="flex items-center gap-3">
          <button
            v-if="showBack"
            @click="router.back()"
            aria-label="Geri dön"
            class="p-1.5 rounded-lg text-gray-500 hover:bg-gray-100 dark:hover:bg-white/10"
          >
            <ChevronLeftIcon class="w-5 h-5" aria-hidden="true" />
          </button>
          <div>
            <h1 class="text-base font-semibold text-gray-900 dark:text-white leading-tight">
              {{ pageTitle }}
            </h1>
            <p v-if="userName" class="text-xs text-gray-500 dark:text-gray-400">{{ userName }}</p>
          </div>
        </div>

        <button
          @click="handleLogout"
          aria-label="Çıkış yap"
          class="p-1.5 rounded-lg text-gray-500 hover:bg-gray-100 dark:hover:bg-white/10"
        >
          <ArrowRightOnRectangleIcon class="w-5 h-5" aria-hidden="true" />
        </button>
      </div>
    </header>

    <main class="flex-1 p-4 max-w-2xl mx-auto w-full">
      <router-view />
    </main>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { ChevronLeftIcon, ArrowRightOnRectangleIcon } from '@heroicons/vue/24/outline';
import { useAuthStore } from '../stores/auth';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const pageTitle = computed(() => (route.meta.title as string) || 'Şoför Paneli');
const showBack = computed(() => route.name !== 'DriverShipments');
const userName = computed(() => authStore.userName);

function handleLogout() {
  authStore.logout();
}
</script>
