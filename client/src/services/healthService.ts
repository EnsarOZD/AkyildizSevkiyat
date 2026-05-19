import apiClient from './apiClient';

export interface BackgroundServiceStatus {
    name: string;
    runAt: string;
    result: 'Success' | 'Failure';
    errorMessage: string | null;
}

export const healthService = {
    async getBackgroundServices(): Promise<BackgroundServiceStatus[]> {
        const res = await apiClient.get<BackgroundServiceStatus[]>('/health/background-services');
        return res.data;
    }
};
