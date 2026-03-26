import { defineStore } from 'pinia';
import { authService } from '../services/authService';
import type { User } from '../services/authService';
import { API_EVENTS } from '../services/apiClient';

interface AuthState {
    token: string | null;
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
        token: localStorage.getItem('token'),
        user: safeJsonParse<User | null>('user', null),
        _isInitialized: false,
    }),
    getters: {
        isAuthenticated: (state) => !!state.token,
        userRole: (state) => state.user?.role || '',
        userName: (state) => state.user ? `${state.user.firstName} ${state.user.lastName}`.trim() : '',
        userEmail: (state) => state.user?.email || '',
        userInitial: (state) => state.user?.firstName?.charAt(0).toUpperCase() || '?',
    },
    actions: {
        async login(email: string, password: string) {
            const { accessToken, user } = await authService.login({ email, password });

            this.token = accessToken;
            this.user = user;

            localStorage.setItem('token', accessToken);
            localStorage.setItem('user', JSON.stringify(user));
        },

        logout() {
            // Avoid redundant work if already cleared
            if (!this.token && !this.user) return;

            this.token = null;
            this.user = null;
            authService.logout();
            
            // Navigate to login (SPA safe if possible, but store lacks router instance usually)
            // Using window.location.href as a reliable fallback for global logouts
            if (window.location.pathname !== '/login') {
                window.location.href = '/login';
            }
        },

        async refreshToken(): Promise<boolean> {
            try {
                const response = await authService.refreshToken();
                this.token = response.accessToken;
                this.user = response.user;
                localStorage.setItem('token', response.accessToken);
                localStorage.setItem('user', JSON.stringify(response.user));
                return true;
            } catch {
                return false;
            }
        },

        /**
         * Initialize auth-related global event listeners with double-init guard
         */
        init() {
            if (this._isInitialized) return;

            // Debounce: aynı anda birden fazla 401 geldiğinde tek logout yapılsın
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
