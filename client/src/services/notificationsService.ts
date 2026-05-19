import apiClient from './apiClient';

export interface NotificationDto {
    id: number;
    title: string;
    body: string;
    url: string | null;
    eventType: string;
    isRead: boolean;
    createdAt: string;
}

export const notificationsService = {
    async getAll(page = 1): Promise<NotificationDto[]> {
        const res = await apiClient.get<NotificationDto[]>('/notifications', { params: { page } });
        return res.data;
    },

    async markAllRead(): Promise<void> {
        await apiClient.patch('/notifications/read-all');
    },

    async markRead(id: number): Promise<void> {
        await apiClient.patch(`/notifications/${id}/read`);
    },

    async subscribe(endpoint: string, p256dh: string, auth: string): Promise<void> {
        await apiClient.post('/notifications/push-subscribe', { endpoint, p256dh, auth });
    },

    async unsubscribe(endpoint: string): Promise<void> {
        await apiClient.delete('/notifications/push-subscribe', { data: { endpoint } });
    },
};
