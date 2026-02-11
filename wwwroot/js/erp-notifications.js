function showNotification(title, message, type = 'success') {
    const container = document.getElementById('notificationContainer');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `erp-toast ${type}`;

    let icon = 'bi-check-circle-fill';
    if (type === 'error') icon = 'bi-x-circle-fill';
    if (type === 'warning') icon = 'bi-exclamation-triangle-fill';
    if (type === 'info') icon = 'bi-info-circle-fill';

    toast.innerHTML = `
        <i class="bi ${icon}"></i>
        <div class="toast-content">
            <div class="toast-title">${title}</div>
            <div class="toast-message">${message}</div>
        </div>
        <i class="bi bi-x toast-close" onclick="this.parentElement.remove()"></i>
    `;

    container.appendChild(toast);

    setTimeout(() => {
        toast.classList.add('out');
        setTimeout(() => toast.remove(), 300);
    }, 5000);
}

function showConfirm(title, message, onConfirm, type = 'danger') {
    const overlay = document.createElement('div');
    overlay.className = 'custom-prompt-overlay';

    let btnClass = 'btn-danger';
    if (type === 'primary') btnClass = 'btn-primary';
    if (type === 'success') btnClass = 'btn-success';

    overlay.innerHTML = `
        <div class="custom-prompt-card">
            <h5 class="fw-bold mb-2">${title}</h5>
            <p class="text-muted mb-4">${message}</p>
            <div class="d-flex justify-content-end gap-2">
                <button class="btn-erp-secondary" onclick="this.closest('.custom-prompt-overlay').remove()">Cancelar</button>
                <button class="btn ${btnClass} px-4" id="btnConfirmAction">Confirmar</button>
            </div>
        </div>
    `;
    document.body.appendChild(overlay);
    overlay.querySelector('#btnConfirmAction').onclick = () => {
        onConfirm();
        overlay.remove();
    };
}
