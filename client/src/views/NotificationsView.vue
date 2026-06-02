<template>
  <div class="max-w-3xl mx-auto space-y-4">
    <div class="flex items-center justify-between">
      <div>
        <h1 class="text-2xl font-bold text-gray-900 dark:text-gray-100">Bildirimler</h1>
        <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">
          <span v-if="store.unreadCount > 0">{{ store.unreadCount }} okunmamış</span>
          <span v-else>Tüm bildirimler okundu</span>
        </p>
      </div>
      <button
        v-if="store.unreadCount > 0"
        @click="store.markAllRead()"
        class="px-3 py-2 text-sm font-medium rounded-lg bg-indigo-600 hover:bg-indigo-700 text-white transition-colors"
      >Tümünü okundu işaretle</button>
    </div>

    <div class="bg-white dark:bg-gray-900 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
      <div v-if="store.loading && store.items.length === 0" class="py-12 text-center text-sm text-gray-400">
        Yükleniyor...
      </div>
      <div v-else-if="store.items.length === 0" class="py-16 text-center text-sm text-gray-400 dark:text-gray-600">
        Henüz bildirim yok.
      </div>

      <div v-else class="divide-y divide-gray-100 dark:divide-gray-800">
        <div
          v-for="n in store.items"
          :key="n.id"
          @click="handleClick(n)"
          class="flex items-start gap-3 px-4 py-3.5 cursor-pointer transition-colors"
          :class="n.isRead
            ? 'hover:bg-gray-50 dark:hover:bg-gray-800/50'
            : 'bg-indigo-50/50 dark:bg-indigo-900/10 hover:bg-indigo-50 dark:hover:bg-indigo-900/20'"
        >
          <span class="mt-1.5 w-2 h-2 rounded-full flex-shrink-0"
            :class="n.isRead ? 'bg-transparent' : 'bg-indigo-500'"></span>
          <div class="flex-1 min-w-0">
            <p class="text-sm font-medium text-gray-800 dark:text-gray-200 leading-snug">{{ n.title }}</p>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5 leading-snug">{{ n.body }}</p>
            <p class="text-[11px] text-gray-400 dark:text-gray-600 mt-1">{{ formatTime(n.createdAt) }}</p>
          </div>
          <button
            v-if="!n.isRead"
            @click.stop="store.markRead(n.id)"
            class="text-[11px] text-indigo-600 dark:text-indigo-400 hover:underline flex-shrink-0 mt-0.5"
          >Okundu</button>
        </div>
      </div>

      <div v-if="store.hasMore" class="p-3 border-t border-gray-100 dark:border-gray-800 text-center">
        <button
          @click="store.loadMore()"
          :disabled="store.loading"
          class="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-700 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800 disabled:opacity-50"
        >{{ store.loading ? 'Yükleniyor...' : 'Daha fazla yükle' }}</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useNotificationsStore } from '../stores/notifications';
import type { NotificationDto } from '../services/notificationsService';

const store = useNotificationsStore();
const router = useRouter();

async function handleClick(n: NotificationDto) {
  if (!n.isRead) await store.markRead(n.id);
  if (n.url) router.push(n.url);
}

function formatTime(iso: string): string {
  const d = new Date(iso);
  return d.toLocaleString('tr-TR', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });
}

onMounted(() => {
  store.fetchAll();
});
</script>
