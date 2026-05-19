import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useDriverRouteStore = defineStore('driverRoute', () => {
    const mapsRouteUrl = ref<string | null>(null);
    return { mapsRouteUrl };
});
