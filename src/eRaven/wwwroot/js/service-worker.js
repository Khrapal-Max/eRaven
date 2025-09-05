// service-worker.js (development)
// Мережа за замовчуванням. Без офлайн-кешу.

self.addEventListener('install', () => self.skipWaiting());

self.addEventListener('activate', event => {
    event.waitUntil(self.clients.claim());
});

// ЖОДНИХ fetch-обробників — браузер іде напряму в мережу.
