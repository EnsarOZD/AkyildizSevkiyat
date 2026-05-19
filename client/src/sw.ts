import { cleanupOutdatedCaches, precacheAndRoute } from 'workbox-precaching';
import { registerRoute } from 'workbox-routing';
import { NetworkFirst } from 'workbox-strategies';
import { CacheableResponsePlugin } from 'workbox-cacheable-response';

declare let self: ServiceWorkerGlobalScope;

cleanupOutdatedCaches();
precacheAndRoute(self.__WB_MANIFEST);

// Network-first for all API calls — never serve stale data.
// SSE (EventSource) requests are excluded: they have Accept: text/event-stream
// and must not be intercepted by a caching strategy.
registerRoute(
    ({ url, request }) =>
        url.pathname.startsWith('/api/') &&
        request.headers.get('accept') !== 'text/event-stream',
    new NetworkFirst({
        cacheName: 'api-cache',
        networkTimeoutSeconds: 10,
        plugins: [new CacheableResponsePlugin({ statuses: [0, 200] })],
    })
);

self.addEventListener('push', (event) => {
    if (!event.data) return;
    const data = event.data.json() as { title: string; body: string; url?: string | null };
    event.waitUntil(
        self.registration.showNotification(data.title, {
            body: data.body,
            icon: '/logo-icon.png',
            badge: '/logo-icon.png',
            data: { url: data.url ?? '/' },
            vibrate: [100, 50, 100],
        })
    );
});

self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    const url: string = event.notification.data?.url ?? '/';
    event.waitUntil(
        self.clients
            .matchAll({ type: 'window', includeUncontrolled: true })
            .then((clientList) => {
                for (const client of clientList) {
                    if (client.url === url && 'focus' in client) {
                        return client.focus();
                    }
                }
                return self.clients.openWindow(url);
            })
    );
});
