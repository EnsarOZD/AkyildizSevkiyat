import { defineStore } from 'pinia';
import { ref } from 'vue';

export type NotificationType = 'success' | 'error' | 'info' | 'warning';

export interface NotificationItem {
    id: string;
    message: string;
    type: NotificationType;
    duration: number;
}

export interface ConfirmOptions {
    title?: string;
    message: string;
    confirmText?: string;
    cancelText?: string;
    type?: 'danger' | 'warning' | 'info';
}

export const useNotificationStore = defineStore('notification', () => {
    // --- Toasts ---
    const notifications = ref<NotificationItem[]>([]);

    const add = (message: string, type: NotificationType = 'info', duration: number = 3000) => {
        const id = Date.now().toString() + Math.random().toString().slice(2);
        notifications.value.push({ id, message, type, duration });

        if (duration > 0) {
            setTimeout(() => {
                remove(id);
            }, duration);
        }
    };

    const remove = (id: string) => {
        notifications.value = notifications.value.filter(n => n.id !== id);
    };

    // --- Confirmation Modal ---
    const confirmState = ref<{
        isOpen: boolean;
        options: ConfirmOptions;
        resolve: (value: boolean) => void;
    }>({
        isOpen: false,
        options: { message: '' },
        resolve: () => { },
    });

    const promptConfirm = (options: ConfirmOptions): Promise<boolean> => {
        return new Promise((resolve) => {
            confirmState.value = {
                isOpen: true,
                options,
                resolve,
            };
        });
    };

    const resolveConfirm = (result: boolean) => {
        confirmState.value.isOpen = false;
        confirmState.value.resolve(result);
    };

    return {
        notifications,
        add,
        remove,
        confirmState,
        promptConfirm,
        resolveConfirm
    };
});
