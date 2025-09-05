 // Глобальний тумблер теми (викликай із Blazor через IJSRuntime: window.toggleTheme())
        window.toggleTheme = () => {
          const html = document.documentElement;
          const next = (html.getAttribute('data-bs-theme') || 'dark') === 'dark' ? 'light' : 'dark';
          html.setAttribute('data-bs-theme', next);
        };

        // Увімкнути тултіп-и Bootstrap для елементів з data-bs-toggle="tooltip"
        document.addEventListener('DOMContentLoaded', () => {
          document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(el => new bootstrap.Tooltip(el));
          const ui = document.getElementById('blazor-error-ui');
          ui?.querySelector('.dismiss')?.addEventListener('click', () => ui.classList.add('d-none'));
        });

        // Показати error UI з довільним повідомленням (можна викликати з Blazor)
        window.showBlazorError = (msg) => {
          const ui = document.getElementById('blazor-error-ui');
          if (!ui) return;
          ui.classList.remove('d-none');
          if (msg) ui.querySelector('.msg').textContent = msg;
        };