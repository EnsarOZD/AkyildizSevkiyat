import { defineStore } from 'pinia';
import { authService } from '../services/authService';
import type { User } from '../services/authService';
import { API_EVENTS } from '../services/apiClient';

interface AuthState {
    token: string | null;
    storedRefreshToken: string | null;
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
        storedRefreshToken: localStorage.getItem('refreshToken'),
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
            const { accessToken, refreshToken, user } = await authService.login({ email, password });

            this.token = accessToken;
            this.storedRefreshToken = refreshToken;
            this.user = user;

            localStorage.setItem('token', accessToken);
            localStorage.setItem('refreshToken', refreshToken);
            localStorage.setItem('user', JSON.stringify(user));
        },

        async logout() {
            if (!this.token && !this.user) return;

            const rt = this.storedRefreshToken;

            this.token = null;
            this.storedRefreshToken = null;
            this.user = null;
            authService.clearStorage();

            if (rt) {
                await authService.logout(rt);
            }

            if (window.location.pathname !== '/login') {
                window.location.href = '/login';
            }
        },

        async refreshToken(): Promise<boolean> {
            const rt = this.storedRefreshToken ?? localStorage.getItem('refreshToken');
            if (!rt) return false;

            try {
                const response = await authService.refreshToken(rt);
                this.token = response.accessToken;
                this.storedRefreshToken = response.refreshToken;
                this.user = response.user;
                localStorage.setItem('token', response.accessToken);
                localStorage.setItem('refreshToken', response.refreshToken);
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
