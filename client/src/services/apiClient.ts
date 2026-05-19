import axios, { AxiosError } from 'axios';
import type { AxiosInstance, InternalAxiosRequestConfig, AxiosResponse } from 'axios';
import { ApiError, isApiErrorResponse } from '../utils/apiError';
import { useAuthStore } from '../stores/auth';

export const API_EVENTS = {
    UNAUTHORIZED: 'api:unauthorized',
    FORBIDDEN: 'api:forbidden',
    SERVER_ERROR: 'api:server-error'
};

const apiClient: AxiosInstance = axios.create({
    baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
    timeout: 30000,
    headers: { 'Content-Type': 'application/json' },
    withCredentials: true, // #1: Send HttpOnly cookies with every request
});

// #1: Request interceptor — no longer injects Bearer token (cookie handles auth).
// Kept for Postman/non-browser clients that may still pass a Bearer header externally.
apiClient.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => config,
    (error) => Promise.reject(error)
);

/**
 * Concurrent refresh queue — parallel 401s wait for a single refresh call.
 */
let isRefreshing = false;
let failedQueue: Array<{ resolve: () => void; reject: (err: unknown) => void }> = [];

function processQueue(error: unknown) {
    failedQueue.forEach(p => error ? p.reject(error) : p.resolve());
    failedQueue = [];
}

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

                // Refresh endpoint 401 → infinite loop guard
                if (originalRequest?.url?.includes('/auth/refresh')) {
                    processQueue(normalizedError);
                    window.dispatchEvent(new CustomEvent(API_EVENTS.UNAUTHORIZED));
                    return Promise.reject(normalizedError);
                }

                // Another refresh in-flight → queue this request
                if (isRefreshing) {
                    return new Promise((resolve, reject) => {
                        failedQueue.push({
                            // #1: Cookie is updated by server response — just retry
                            resolve: () => resolve(apiClient(originalRequest)),
                            reject,
                        });
                    });
                }

                if (!originalRequest._retry) {
                    originalRequest._retry = true;
                    isRefreshing = true;

                    const store = useAuthStore();
                    const success = await store.refreshToken();

                    isRefreshing = false;

                    if (success) {
                        processQueue(null);
                        // #1: New cookie set by server — retry without manual header injection
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
