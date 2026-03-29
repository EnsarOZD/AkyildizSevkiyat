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
    refreshToken: string;
    user: User;
}

export const authService = {
    async login(credentials: LoginRequest): Promise<AuthResponse> {
        const response = await apiClient.post<AuthResponse>('/auth/login', credentials);
        return response.data;
    },

    async refreshToken(refreshToken: string): Promise<AuthResponse> {
        const response = await apiClient.post<AuthResponse>('/auth/refresh', { refreshToken });
        return response.data;
    },

    async logout(refreshToken: string): Promise<void> {
        try {
            await apiClient.post('/auth/logout', { refreshToken });
        } catch {
            // Logout hatası session temizlemeyi engellememeli
        }
    },

    clearStorage(): void {
        localStorage.removeItem('token');
        localStorage.removeItem('refreshToken');
        localStorage.removeItem('user');
    }
};
