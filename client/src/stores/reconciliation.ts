import { defineStore } from 'pinia';
import { ref } from 'vue';
import apiClient from '../services/apiClient';

export const useReconciliationStore = defineStore('reconciliation', () => {
    const openCount = ref(0);

    async function fetchOpenCount() {
        try {
            const res = await apiClient.get<{ count: number }>('/reconciliation/open-count');
            openCount.value = res.data.count;
        } catch {
            // Silently ignore — badge missing is not critical
        }
    }

    return { openCount, fetchOpenCount };
});
