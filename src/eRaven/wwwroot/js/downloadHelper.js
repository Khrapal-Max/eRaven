window.downloadFile = (filename, contentType, base64) => {
    // decode base64 -> Uint8Array
    const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    const blob = new Blob([bytes], {type: contentType });

    // IE 11 / старий Edge (опційно, можна прибрати якщо не треба)
    if (window.navigator && window.navigator.msSaveOrOpenBlob) {
        window.navigator.msSaveOrOpenBlob(blob, filename || 'download');
    return;
    }

    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename || 'download';
    // a.style.display = 'none'; // не обовʼязково
    document.body.appendChild(a);
    a.click();
    // приберемо лінк і звільнимо URL
    setTimeout(() => {
        URL.revokeObjectURL(url);
    a.remove();
    }, 0);
  };
