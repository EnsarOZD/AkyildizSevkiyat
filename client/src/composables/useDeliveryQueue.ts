import { ref, onMounted, onUnmounted } from 'vue';
import shipmentService from '../services/shipmentService';

export interface PendingDelivery {
    id: string;
    shipmentId: number;
    deliveryRecipient?: string;
    deliveryNote?: string;
    photosBase64?: string[];
    latitude?: number;
    longitude?: number;
    queuedAt: string;
}

const DB_NAME = 'sevkiyat-offline';
const STORE = 'pending-deliveries';
const DB_VERSION = 1;

function openDb(): Promise<IDBDatabase> {
    return new Promise((resolve, reject) => {
        const req = indexedDB.open(DB_NAME, DB_VERSION);
        req.onupgradeneeded = () => {
            req.result.createObjectStore(STORE, { keyPath: 'id' });
        };
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}

async function idbGetAll(): Promise<PendingDelivery[]> {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, 'readonly');
        const req = tx.objectStore(STORE).getAll();
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}

async function idbAdd(item: PendingDelivery): Promise<void> {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, 'readwrite');
        tx.objectStore(STORE).put(item);
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

async function idbDelete(id: string): Promise<void> {
    const db = await openDb();
    return new Promise((resolve, reject) => {
        const tx = db.transaction(STORE, 'readwrite');
        tx.objectStore(STORE).delete(id);
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
    });
}

export function useDeliveryQueue() {
    const queue = ref<PendingDelivery[]>([]);
    const isOnline = ref(navigator.onLine);
    const flushing = ref(false);

    async function refreshQueue() {
        queue.value = await idbGetAll();
    }

    async function enqueue(item: Omit<PendingDelivery, 'id' | 'queuedAt'>) {
        const entry: PendingDelivery = {
            ...item,
            id: `${Date.now()}-${Math.random().toString(36).slice(2)}`,
            queuedAt: new Date().toISOString(),
        };
        await idbAdd(entry);
        await refreshQueue();
    }

    async function flushQueue() {
        if (flushing.value || !isOnline.value) return;
        flushing.value = true;
        await refreshQueue();
        const pending = [...queue.value];
        for (const item of pending) {
            try {
                await shipmentService.markDelivered(
                    item.shipmentId,
                    item.deliveryNote,
                    item.deliveryRecipient,
                    item.photosBase64,
                    undefined,
                    item.latitude,
                    item.longitude,
                );
                await idbDelete(item.id);
            } catch {
                // Leave failed items in queue — will retry on next flush
            }
        }
        await refreshQueue();
        flushing.value = false;
    }

    function onOnline() {
        isOnline.value = true;
        flushQueue();
    }
    function onOffline() {
        isOnline.value = false;
    }

    onMounted(async () => {
        window.addEventListener('online', onOnline);
        window.addEventListener('offline', onOffline);
        await refreshQueue();
        if (isOnline.value && queue.value.length > 0) {
            flushQueue();
        }
    });

    onUnmounted(() => {
        window.removeEventListener('online', onOnline);
        window.removeEventListener('offline', onOffline);
    });

    return { queue, isOnline, flushing, enqueue, flushQueue };
}
