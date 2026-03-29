import axios, { AxiosError } from 'axios';
import type { AxiosInstance, InternalAxiosRequestConfig, AxiosResponse } from 'axios';
import { ApiError, isApiErrorResponse } from '../utils/apiError';
import { useAuthStore } from '../stores/auth';

/**
 * Global event names for API events
 */
export const API_EVENTS = {
    UNAUTHORIZED: 'api:unauthorized',
    FORBIDDEN: 'api:forbidden',
    SERVER_ERROR: 'api:server-error'
};

const apiClient: AxiosInstance = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
    timeout: 30000,
    headers: {
        'Content-Type': 'application/json',
    },
});

/**
 * Request Interceptor: Inject Auth Token
 */
apiClient.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
        const token = localStorage.getItem('token');
        if (token && config.headers) {
            config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
    },
    (error) => Promise.reject(error)
);

/**
 * Concurrent refresh queue — paralel 401'lerde tek refresh yapılmasını garantiler.
 */
let isRefreshing = false;
let failedQueue: Array<{ resolve: () => void; reject: (err: unknown) => void }> = [];

function processQueue(error: unknown) {
    failedQueue.forEach(p => error ? p.reject(error) : p.resolve());
    failedQueue = [];
}

/**
 * Response Interceptor: Normalize Errors and Handle Global Catch
 */
apiClient.interceptors.response.use(
    (response: AxiosResponse) => response,
    async (error: AxiosError) => {
        let normalizedError: Error = error;
        const status = error.response?.status;
        const data = error.response?.data;

        if (error.response) {
            if (isApiErrorResponse(data)) {
                normalizedError = new ApiError(data, status);
            } else {
                normalizedError = new ApiError({
                    type: 'server_error',
                    message: `Beklenmedik bir hata oluştu (Status: ${status})`
                }, status);
            }

            if (status === 401) {
                const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

                // Refresh endpoint'inin kendisi 401 aldıysa → sonsuz döngü önle, direkt logout
                if (originalRequest?.url?.includes('/auth/refresh')) {
                    processQueue(normalizedError);
                    window.dispatchEvent(new CustomEvent(API_EVENTS.UNAUTHORIZED));
                    return Promise.reject(normalizedError);
                }

                // Başka bir refresh zaten devam ediyorsa → queue'ya ekle, bekle
                if (isRefreshing) {
                    return new Promise((resolve, reject) => {
                        failedQueue.push({
                            resolve: () => {
                                const token = localStorage.getItem('token');
                                if (originalRequest.headers && token) {
                                    originalRequest.headers['Authorization'] = `Bearer ${token}`;
                                }
                                resolve(apiClient(originalRequest));
                            },
                            reject
                        });
                    });
                }

                // İlk 401: refresh başlat
                if (!originalRequest._retry) {
                    originalRequest._retry = true;
                    isRefreshing = true;

                    const store = useAuthStore();
                    const success = await store.refreshToken();

                    isRefreshing = false;

                    if (success) {
                        processQueue(null);
                        if (originalRequest.headers) {
                            originalRequest.headers['Authorization'] = `Bearer ${store.token}`;
                        }
                        return apiClient(originalRequest);
                    } else {
                        processQueue(normalizedError);
                        window.dispatchEvent(new CustomEvent(API_EVENTS.UNAUTHORIZED));
                        return Promise.reject(normalizedError);
                    }
                }

                window.dispatchEvent(new CustomEvent(API_EVENTS.UNAUTHORIZED));
            } else if (status === 403) {
                window.dispatchEvent(new CustomEvent(API_EVENTS.FORBIDDEN));
            } else if (status && status >= 500) {
                window.dispatchEvent(new CustomEvent(API_EVENTS.SERVER_ERROR));
            }
        } else if (error.request) {
            normalizedError = new ApiError({
                type: 'network_error',
                message: 'Sunucuya bağlanılamadı. Lütfen internet bağlantınızı kontrol ediniz.'
            });
        } else {
            normalizedError = new ApiError({
                type: 'server_error',
                message: error.message || 'İstek oluşturulurken bir hata oluştu.'
            });
        }

        return Promise.reject(normalizedError);
    }
);

export default apiClient;
