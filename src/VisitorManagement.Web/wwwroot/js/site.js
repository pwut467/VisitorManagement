document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('.toast').forEach(el => {
    const delay = Number(el.getAttribute('data-bs-delay')) || 4000;
    const toast = bootstrap.Toast.getOrCreateInstance(el, { autohide: true, delay });
    toast.show();
  });
});
