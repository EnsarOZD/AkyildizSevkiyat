import { defineStore } from 'pinia';
import { authService } from '../services/authService';
import type { User } from '../services/authService';
import { API_EVENTS } from '../services/apiClient';

interface AuthState {
    user: User | null;
    _isInitialized: boolean;
}

function safeJsonParse<T>(key: string, fallback: T): T {
    try {
        const item = localStorage.getItem(key);
        return item ? JSON.parse(item) : fallback;
    } catch {
        return fallback;
    }
}

export const useAuthStore = defineStore('auth', {
    state: (): AuthState => ({
        // #1: Tokens are in HttpOnly cookies — only store non-sensitive user info
        user: safeJsonParse<User | null>('user', null),
        _isInitialized: false,
    }),
    getters: {
        // #1: isAuthenticated is based on user presence, not token in memory
        isAuthenticated: (state) => !!state.user,
        userRole: (state) => state.user?.role || '',
        userName: (state) => state.user ? `${state.user.firstName} ${state.user.lastName}`.trim() : '',
        userEmail: (state) => state.user?.email || '',
        userInitial: (state) => state.user?.firstName?.charAt(0).toUpperCase() || '?',
    },
    actions: {
        async login(username: string, password: string, rememberMe = false) {
            const { user } = await authService.login({ username, password, rememberMe });
            // #1: Tokens are set as HttpOnly cookies by the server — never touch them here
            this.user = user;
            localStorage.setItem('user', JSON.stringify(user));
        },

        async logout() {
            if (!this.user) return;

            this.user = null;
            authService.clearStorage(); // Removes user from localStorage

            await authService.logout(); // Server clears HttpOnly cookies

            if (window.location.pathname !== '/login') {
                window.location.href = '/login';
            }
        },

        async refreshToken(): Promise<boolean> {
            try {
                // #1: No refresh token in localStorage — cookie sent automatically by browser
                const response = await authService.refreshToken();
                this.user = response.user;
                localStorage.setItem('user', JSON.stringify(response.user));
                return true;
            } catch {
                return false;
            }
        },

        init() {
            if (this._isInitialized) return;

            let logoutScheduled = false;
            window.addEventListener(API_EVENTS.UNAUTHORIZED, () => {
                if (logoutScheduled) return;
                logoutScheduled = true;
                this.logout();
            });

            this._isInitialized = true;
        }
    },
});
