import apiClient from './apiClient';

export interface LoginRequest {
    email: string;
    password: string;
}

export interface User {
    id: number;
    email: string;
    firstName: string;
    lastName: string;
    role: string;
}

export interface AuthResponse {
    accessToken: string;
    user: User;
}

export const authService = {
    /**
     * Authenticate user and return token + user info
     */
    async login(credentials: LoginRequest): Promise<AuthResponse> {
        const response = await apiClient.post<AuthResponse>('/auth/login', credentials);
        return response.data;
    },

    /**
     * POST /auth/refresh — mevcut token ile yeni token al
     * Mevcut token Authorization header'da zaten gidecek (apiClient interceptor'ı ekliyor)
     * Dönen response: { accessToken, user } (login ile aynı format)
     */
    async refreshToken(): Promise<AuthResponse> {
        const response = await apiClient.post<AuthResponse>('/auth/refresh');
        return response.data;
    },

    /**
     * Clear session tokens and local storage data
     */
    logout(): void {
        localStorage.removeItem('token');
        localStorage.removeItem('user');
    }
};
