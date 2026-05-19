import apiClient from './apiClient';

export interface LoginRequest {
    username: string;
    password: string;
    rememberMe?: boolean;
}

export interface User {
    id: number;
    email: string;
    username: string;
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
        // Tokens are set as HttpOnly cookies by the server — response still contains them for Postman compat
        const response = await apiClient.post<AuthResponse>('/auth/login', credentials);
        return response.data;
    },

    async refreshToken(): Promise<AuthResponse> {
        // #1: No body needed — refresh token sent automatically via HttpOnly cookie
        const response = await apiClient.post<AuthResponse>('/auth/refresh', {});
        return response.data;
    },

    async logout(): Promise<void> {
        try {
            // #1: No body needed — server reads refresh token from cookie and clears both cookies
            await apiClient.post('/auth/logout', {});
        } catch {
            // Logout hatası session temizlemeyi engellememeli
        }
    },

    clearStorage(): void {
        // #1: Tokens are no longer in localStorage — only clear user info
        localStorage.removeItem('user');
    }
};
