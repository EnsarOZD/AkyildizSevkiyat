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
    timeout: 30000, // 30 second timeout
    headers: {
        'Content-Type': 'application/json',
    },
});

/**
 * Request Interceptor: Inject Auth Token
 * Token süresi dolmuşsa isteği göndermeden UNAUTHORIZED event'i fırlatır.
 */
apiClient.interceptors.request.use(
    (config: InternalAxiosRequestConfig) => {
        const token = localStorage.getItem('token');
        if (token) {
            // Süresi dolmuş token'ı header'a eklemeye devam et;
            // response interceptor'daki refresh mekanizması 401 durumunda devreye girer.
            if (config.headers) {
                config.headers.Authorization = `Bearer ${token}`;
            }
        }
        return config;
    },
    (error) => Promise.reject(error)
);

/**
 * Response Interceptor: Normalize Errors and Handle Global Catch
 */
apiClient.interceptors.response.use(
    (response: AxiosResponse) => response,
    async (error: AxiosError) => {
        let normalizedError: Error = error;
        const status = error.response?.status;
        const data = error.response?.data;

        // 1. Handle Response Errors (Status codes >= 400)
        if (error.response) {
            if (isApiErrorResponse(data)) {
                // Backend returned a structured error
                normalizedError = new ApiError(data, status);
            } else {
                // Backend returned an unstructured error, create a fallback ApiError
                normalizedError = new ApiError({
                    type: 'server_error',
                    message: `Beklenmedik bir hata oluştu (Status: ${status})`
                }, status);
            }

            // Global side-effects for specific status codes
            if (status === 401) {
                const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

                // Refresh endpoint'inin kendisi 401 aldıysa → sonsuz döngü önle, direkt logout
                if (originalRequest?.url?.includes('/auth/refresh')) {
                    window.dispatchEvent(new CustomEvent(API_EVENTS.UNAUTHORIZED));
                    return Promise.reject(normalizedError);
                }

                // İlk 401: token refresh dene
                if (!originalRequest._retry) {
                    originalRequest._retry = true;
                    const store = useAuthStore();
                    const success = await store.refreshToken();

                    if (success) {
                        // Yeni token'ı header'a ekle ve orijinal isteği tekrarla
                        if (originalRequest.headers) {
                            originalRequest.headers['Authorization'] = `Bearer ${store.token}`;
                        }
                        return apiClient(originalRequest);
                    }
                }

                // Refresh başarısız veya ikinci 401 → logout
                window.dispatchEvent(new CustomEvent(API_EVENTS.UNAUTHORIZED));
            } else if (status === 403) {
                window.dispatchEvent(new CustomEvent(API_EVENTS.FORBIDDEN));
            } else if (status && status >= 500) {
                // Broaden 5xx handling
                window.dispatchEvent(new CustomEvent(API_EVENTS.SERVER_ERROR));
            }
        }
        // 2. Handle Network / No Response Errors
        else if (error.request) {
            // The request was made but no response was received
            normalizedError = new ApiError({
                type: 'network_error',
                message: 'Sunucuya bağlanılamadı. Lütfen internet bağlantınızı kontrol ediniz.'
            });
        }
        // 3. Something happened while setting up the request
        else {
            normalizedError = new ApiError({
                type: 'server_error',
                message: error.message || 'İstek oluşturulurken bir hata oluştu.'
            });
        }

        return Promise.reject(normalizedError);
    }
);

export default apiClient;
