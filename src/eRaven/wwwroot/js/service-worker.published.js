// service-worker.published.js
// Caution! See https://aka.ms/blazor-offline-considerations

self.importScripts('./service-worker-assets.js');

const cacheNamePrefix = 'offline-cache-';
const cacheName = `${cacheNamePrefix}${self.assetsManifest.version}`;

const offlineAssetsInclude = [
    /\.dll$/, /\.pdb$/, /\.wasm$/, /\.html$/, /\.js$/, /\.json$/, /\.css$/,
    /\.woff2?$/, /\.png$/, /\.jpe?g$/, /\.gif$/, /\.ico$/, /\.blat$/, /\.dat$/
];
const offlineAssetsExclude = [/^service-worker\.js$/, /^service-worker-assets\.js$/];

// ���� ������ � ������� � ���� base � �� ������ ���� � ����.
const base = "/";
const baseUrl = new URL(base, self.origin);
const manifestUrlList = self.assetsManifest.assets.map(a => new URL(a.url, baseUrl).href);

self.addEventListener('install', event => {
    event.waitUntil((async () => {
        const assetsRequests = self.assetsManifest.assets
            .filter(a => offlineAssetsInclude.some(rx => rx.test(a.url)))
            .filter(a => !offlineAssetsExclude.some(rx => rx.test(a.url)))
            .map(a => new Request(a.url, { integrity: a.hash, cache: 'no-cache' }));

        const cache = await caches.open(cacheName);
        await cache.addAll(assetsRequests);
    })());

    // ������� �������� ����� SW
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        // ������� ���� ����
        const keys = await caches.keys();
        await Promise.all(
            keys.filter(k => k.startsWith(cacheNamePrefix) && k !== cacheName)
                .map(k => caches.delete(k))
        );

        // ������ �������� ��� ���� �볺�����
        await self.clients.claim();
    })());
});

self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET') return;

    event.respondWith((async () => {
        const cache = await caches.open(cacheName);

        // ��� �������� ������ index.html � ����, ���� URL �� � ��������� �������� � ��������
        const shouldServeIndexHtml = event.request.mode === 'navigate'
            && !manifestUrlList.includes(event.request.url);

        const request = shouldServeIndexHtml ? 'index.html' : event.request;

        // offline-first
        const cached = await cache.match(request);
        if (cached) return cached;

        try {
            const network = await fetch(event.request);
            return network;
        } catch {
            if (shouldServeIndexHtml) {
                const fallback = await cache.match('index.html');
                if (fallback) return fallback;
            }
            throw;
        }
    })());
});
