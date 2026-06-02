import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { notificationsService, type NotificationDto } from '../services/notificationsService';
import { useSoundFeedback } from '../composables/useSoundFeedback';

export const useNotificationsStore = defineStore('notifications', () => {
    const items = ref<NotificationDto[]>([]);
    const loading = ref(false);
    const page = ref(1);
    const hasMore = ref(false);
    const PAGE_SIZE = 30;
    let eventSource: EventSource | null = null;
    const sound = useSoundFeedback();

    const unreadCount = computed(() => items.value.filter(n => !n.isRead).length);

    async function fetchAll() {
        loading.value = true;
        try {
            page.value = 1;
            items.value = await notificationsService.getAll(1);
            hasMore.value = items.value.length >= PAGE_SIZE;
        } finally {
            loading.value = false;
        }
    }

    async function loadMore() {
        if (loading.value || !hasMore.value) return;
        loading.value = true;
        try {
            const next = await notificationsService.getAll(page.value + 1);
            page.value += 1;
            items.value.push(...next);
            hasMore.value = next.length >= PAGE_SIZE;
        } finally {
            loading.value = false;
        }
    }

    async function markAllRead() {
        await notificationsService.markAllRead();
        items.value.forEach(n => (n.isRead = true));
    }

    async function markRead(id: number) {
        await notificationsService.markRead(id);
        const n = items.value.find(n => n.id === id);
        if (n) n.isRead = true;
    }

    function connectSSE() {
        if (eventSource) return;
        const apiBase = import.meta.env.VITE_API_BASE_URL || '/api';
        eventSource = new EventSource(`${apiBase}/notifications/stream`, { withCredentials: true });

        eventSource.onmessage = (e) => {
            try {
                const data = JSON.parse(e.data) as {
                    title: string; body: string; url: string | null;
                    eventType: string | null; createdAt: string;
                };
                const notification: NotificationDto = {
                    id: Date.now(), // placeholder until next fetchAll
                    title: data.title,
                    body: data.body,
                    url: data.url,
                    eventType: data.eventType ?? '',
                    isRead: false,
                    createdAt: data.createdAt,
                };
                items.value.unshift(notification);

                // Ses geri bildirimi
                if (data.eventType === 'shipment_warehouse_assigned') {
                    sound.newAssignment();
                }
            } catch { /* ignore malformed */ }
        };

        eventSource.onerror = () => {
            // Auto-reconnect: close and retry after 5 seconds
            eventSource?.close();
            eventSource = null;
            setTimeout(connectSSE, 5000);
        };
    }

    function disconnectSSE() {
        eventSource?.close();
        eventSource = null;
    }

    return {
        items,
        loading,
        hasMore,
        unreadCount,
        fetchAll,
        loadMore,
        markAllRead,
        markRead,
        connectSSE,
        disconnectSSE,
    };
});
