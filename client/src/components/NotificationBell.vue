<template>
  <div class="relative" ref="containerRef">
    <!-- Bell button -->
    <button
      @click="toggle"
      :aria-label="`Bildirimler${unreadCount > 0 ? ` (${unreadCount} okunmamış)` : ''}`"
      class="relative p-1.5 rounded-lg text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
    >
      <BellIcon class="h-4 w-4" />
      <span
        v-if="unreadCount > 0"
        class="absolute -top-0.5 -right-0.5 min-w-[16px] h-4 px-0.5 rounded-full bg-red-500 text-white text-[9px] font-bold flex items-center justify-center"
      >{{ unreadCount > 99 ? '99+' : unreadCount }}</span>
    </button>

    <!-- Panel -->
    <Transition
      enter-active-class="transition ease-out duration-150"
      enter-from-class="opacity-0 scale-95 translate-y-1"
      enter-to-class="opacity-100 scale-100 translate-y-0"
      leave-active-class="transition ease-in duration-100"
      leave-from-class="opacity-100 scale-100 translate-y-0"
      leave-to-class="opacity-0 scale-95 translate-y-1"
    >
      <div
        v-if="open"
        class="absolute right-0 mt-2 w-80 sm:w-96 bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 rounded-xl shadow-xl z-50 overflow-hidden origin-top-right"
      >
        <!-- Header -->
        <div class="flex items-center justify-between px-4 py-3 border-b border-gray-100 dark:border-gray-800">
          <h3 class="text-sm font-semibold text-gray-800 dark:text-gray-200">Bildirimler</h3>
          <div class="flex items-center gap-2">
            <button
              v-if="unreadCount > 0"
              @click="markAllRead"
              class="text-xs text-indigo-600 dark:text-indigo-400 hover:underline"
            >Tümünü okundu işaretle</button>
            <button
              v-if="webPush.isSupported.value"
              @click="togglePush"
              :title="webPush.isSubscribed.value ? 'Push bildirimleri kapat' : 'Push bildirimleri aç'"
              class="p-1 rounded text-gray-400 hover:text-gray-600 dark:hover:text-gray-300 transition-colors"
            >
              <BellSlashIcon v-if="webPush.isSubscribed.value" class="w-3.5 h-3.5" />
              <BellAlertIcon v-else class="w-3.5 h-3.5" />
            </button>
          </div>
        </div>

        <!-- List -->
        <div class="max-h-80 overflow-y-auto divide-y divide-gray-100 dark:divide-gray-800">
          <div v-if="store.loading" class="py-8 text-center text-sm text-gray-400">Yükleniyor...</div>

          <div v-else-if="store.items.length === 0" class="py-10 text-center text-sm text-gray-400 dark:text-gray-600">
            Bildirim yok
          </div>

          <div
            v-for="n in store.items.slice(0, 30)"
            :key="n.id"
            @click="handleClick(n)"
            class="flex items-start gap-3 px-4 py-3 cursor-pointer transition-colors"
            :class="n.isRead
              ? 'hover:bg-gray-50 dark:hover:bg-gray-800/50'
              : 'bg-indigo-50/50 dark:bg-indigo-900/10 hover:bg-indigo-50 dark:hover:bg-indigo-900/20'"
          >
            <span
              class="mt-1 w-2 h-2 rounded-full flex-shrink-0"
              :class="n.isRead ? 'bg-transparent' : 'bg-indigo-500'"
            ></span>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-medium text-gray-800 dark:text-gray-200 leading-snug">{{ n.title }}</p>
              <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5 leading-snug">{{ n.body }}</p>
              <p class="text-[10px] text-gray-400 dark:text-gray-600 mt-1">{{ formatTime(n.createdAt) }}</p>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useRouter } from 'vue-router';
import { BellIcon, BellSlashIcon, BellAlertIcon } from '@heroicons/vue/24/outline';
import { useNotificationsStore } from '../stores/notifications';
import { useWebPush } from '../composables/useWebPush';

const store = useNotificationsStore();
const webPush = useWebPush();
const router = useRouter();
const open = ref(false);
const containerRef = ref<HTMLElement | null>(null);
const unreadCount = store.unreadCount; // computed ref, auto-unwrapped in template

function toggle() {
  open.value = !open.value;
}

async function markAllRead() {
  await store.markAllRead();
}

async function handleClick(n: { id: number; url: string | null; isRead: boolean }) {
  if (!n.isRead) await store.markRead(n.id);
  if (n.url) {
    open.value = false;
    router.push(n.url);
  }
}

async function togglePush() {
  if (webPush.isSubscribed.value) {
    await webPush.unsubscribe();
  } else {
    await webPush.subscribe();
  }
}

function formatTime(iso: string): string {
  const d = new Date(iso);
  const diffMs = Date.now() - d.getTime();
  const diffMin = Math.floor(diffMs / 60000);
  if (diffMin < 1) return 'Az önce';
  if (diffMin < 60) return `${diffMin}dk önce`;
  const diffH = Math.floor(diffMin / 60);
  if (diffH < 24) return `${diffH}s önce`;
  return d.toLocaleDateString('tr-TR', { day: '2-digit', month: 'short' });
}

function onClickOutside(e: MouseEvent) {
  if (containerRef.value && !containerRef.value.contains(e.target as Node)) {
    open.value = false;
  }
}

onMounted(async () => {
  document.addEventListener('mousedown', onClickOutside);
  await store.fetchAll();
  await webPush.checkSubscription();
});

onUnmounted(() => {
  document.removeEventListener('mousedown', onClickOutside);
});
</script>
