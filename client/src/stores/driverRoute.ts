import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useDriverRouteStore = defineStore('driverRoute', () => {
    const mapsRouteUrl = ref<string | null>(null);
    /** Şoförün açık (devam eden) bir seferi var mı? QR butonu durumu için. */
    const hasActiveSession = ref(false);
    /** Tüm teslimatlar tamamlandı mı? (sefer kapatılabilir) QR butonu durumu için. */
    const canEndSession = ref(false);
    return { mapsRouteUrl, hasActiveSession, canEndSession };
});
