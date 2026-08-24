(() => {
  const input = document.getElementById('code');
  const box = document.getElementById('preview-box');
  if (!input) return;

  async function lookup() {
    const code = input.value.trim();
    if (!code) return;
    const res = await fetch('/CheckOut/Lookup?code=' + encodeURIComponent(code));
    const data = await res.json();
    if (!data.found) {
      box.classList.remove('d-none');
      document.getElementById('p-name').textContent = 'ไม่พบรายการ';
      document.getElementById('p-meta').textContent = '';
      return;
    }
    box.classList.remove('d-none');
    document.getElementById('p-name').textContent = data.name + ' · ' + data.visitNumber;
    document.getElementById('p-meta').textContent = (data.company || '') + ' · มาหา ' + data.host + ' · เข้า ' + (data.checkInAt || '-') + (data.onSite ? '' : ' · ไม่อยู่ในพื้นที่');
  }

  input.addEventListener('change', lookup);
  input.addEventListener('blur', lookup);

  if (window.Html5Qrcode) {
    const scanner = new Html5Qrcode('reader');
    Html5Qrcode.getCameras().then(cameras => {
      if (!cameras.length) return;
      scanner.start(cameras[0].id, { fps: 8, qrbox: 220 }, decoded => {
        input.value = decoded;
        lookup();
      }).catch(() => {});
    }).catch(() => {});
  }
})();
