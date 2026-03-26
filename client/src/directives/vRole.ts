import type { Directive } from 'vue';
import { useAuthStore } from '../stores/auth';

export const vRole: Directive = {
    mounted(el, binding) {
        const { value } = binding;
        const authStore = useAuthStore();
        const userRole = authStore.userRole;

        if (value && userRole) {
            const requiredRole = value;
            // If passing array: v-role="['Admin', 'Warehouse']"
            if (Array.isArray(requiredRole)) {
                if (!requiredRole.includes(userRole)) {
                    el.parentNode?.removeChild(el);
                }
            } else {
                // Single role
                if (userRole !== requiredRole) {
                    el.parentNode?.removeChild(el);
                }
            }
        }
    },
};
