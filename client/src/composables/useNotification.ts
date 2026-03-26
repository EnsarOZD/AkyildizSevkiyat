import { useNotificationStore, type ConfirmOptions } from '../stores/notification';

export const useNotification = () => {
    const store = useNotificationStore();

    const notify = {
        success: (message: string, duration?: number) => store.add(message, 'success', duration),
        error: (message: string, duration?: number) => store.add(message, 'error', duration),
        info: (message: string, duration?: number) => store.add(message, 'info', duration),
        warning: (message: string, duration?: number) => store.add(message, 'warning', duration),
    };

    const confirm = {
        show: (messageOrOptions: string | ConfirmOptions) => {
            let options: ConfirmOptions;
            if (typeof messageOrOptions === 'string') {
                options = { message: messageOrOptions };
            } else {
                options = messageOrOptions;
            }
            return store.promptConfirm(options);
        },
        // Helper for delete actions
        requireDelete: (itemName: string = 'öğeyi') => {
            return store.promptConfirm({
                title: 'Silme İşlemi',
                message: `Bu ${itemName} silinecek. Emin misiniz?`,
                confirmText: 'Evet, Sil',
                type: 'danger'
            });
        }
    };

    return {
        notify,
        confirm
    };
};
