import { ref, computed } from 'vue';
import { notificationsService } from '../services/notificationsService';

// VAPID public key — must match backend Vapid:PublicKey config
const VAPID_PUBLIC_KEY = import.meta.env.VITE_VAPID_PUBLIC_KEY as string | undefined;

function urlBase64ToUint8Array(base64String: string): Uint8Array<ArrayBuffer> {
    const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const rawData = atob(base64);
    const buf = new ArrayBuffer(rawData.length);
    const view = new Uint8Array(buf);
    for (let i = 0; i < rawData.length; i++) view[i] = rawData.charCodeAt(i);
    return view;
}

export function useWebPush() {
    const isSupported = computed(() =>
        typeof window !== 'undefined'
        && 'PushManager' in window
        && 'serviceWorker' in navigator
        && !!VAPID_PUBLIC_KEY);

    const permission = ref<NotificationPermission>(
        typeof Notification !== 'undefined' ? Notification.permission : 'denied');

    const isSubscribed = ref(false);
    const loading = ref(false);

    async function checkSubscription() {
        if (!isSupported.value) return;
        const reg = await navigator.serviceWorker.ready;
        const sub = await reg.pushManager.getSubscription();
        isSubscribed.value = !!sub;
    }

    async function subscribe(): Promise<boolean> {
        if (!isSupported.value || !VAPID_PUBLIC_KEY) return false;
        loading.value = true;
        try {
            const granted = await Notification.requestPermission();
            permission.value = granted;
            if (granted !== 'granted') return false;

            const reg = await navigator.serviceWorker.ready;
            const sub = await reg.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(VAPID_PUBLIC_KEY),
            });

            const key = sub.getKey('p256dh');
            const auth = sub.getKey('auth');
            await notificationsService.subscribe(
                sub.endpoint,
                key ? btoa(String.fromCharCode(...new Uint8Array(key))) : '',
                auth ? btoa(String.fromCharCode(...new Uint8Array(auth))) : '',
            );
            isSubscribed.value = true;
            return true;
        } catch {
            return false;
        } finally {
            loading.value = false;
        }
    }

    async function unsubscribe(): Promise<void> {
        if (!isSupported.value) return;
        loading.value = true;
        try {
            const reg = await navigator.serviceWorker.ready;
            const sub = await reg.pushManager.getSubscription();
            if (sub) {
                await notificationsService.unsubscribe(sub.endpoint);
                await sub.unsubscribe();
                isSubscribed.value = false;
            }
        } finally {
            loading.value = false;
        }
    }

    return { isSupported, permission, isSubscribed, loading, subscribe, unsubscribe, checkSubscription };
}
