// service-worker.js (development)
// ������ �� �������������. ��� ������-����.

self.addEventListener('install', () => self.skipWaiting());

self.addEventListener('activate', event => {
    event.waitUntil(self.clients.claim());
});

// ������ fetch-��������� � ������� ��� ������� � ������.
