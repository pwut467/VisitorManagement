(() => {
  const form = document.getElementById('checkin-form');
  if (!form) return;

  const photoInput = document.getElementById('PhotoDataUrl');
  const video = document.getElementById('cam');
  const preview = document.getElementById('preview');
  const alertBox = document.getElementById('id-alert');
  let stream = null;

  function showAlert(type, message) {
    alertBox.className = 'alert alert-' + type;
    alertBox.textContent = message;
  }

  document.getElementById('btn-start-cam')?.addEventListener('click', async () => {
    try {
      stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'user', width: 640, height: 480 } });
      video.srcObject = stream;
      video.classList.remove('d-none');
      preview.classList.add('d-none');
    } catch {
      showAlert('warning', 'เปิดกล้องไม่ได้ — อนุญาตกล้องในเบราว์เซอร์ หรือกรอกข้อมูลต่อไปโดยไม่มีรูป');
    }
  });

  document.getElementById('btn-snap')?.addEventListener('click', () => {
    if (!video.srcObject) return;
    const canvas = document.createElement('canvas');
    canvas.width = video.videoWidth || 480;
    canvas.height = video.videoHeight || 480;
    canvas.getContext('2d').drawImage(video, 0, 0);
    photoInput.value = canvas.toDataURL('image/jpeg', 0.85);
    preview.src = photoInput.value;
    preview.classList.remove('d-none');
    video.classList.add('d-none');
  });

  document.getElementById('btn-retake')?.addEventListener('click', () => {
    photoInput.value = '';
    preview.classList.add('d-none');
    video.classList.remove('d-none');
  });

  async function lookupVisitor() {
    const id = document.getElementById('NationalId').value;
    const res = await fetch('/api/visitors/by-national-id?id=' + encodeURIComponent(id));
    const data = await res.json();
    if (!data.valid) {
      showAlert('warning', 'เลขบัตรประชาชนไม่ผ่านการตรวจ checksum');
    } else if (data.blocked) {
      showAlert('danger', 'บัญชีดำ: ' + data.blockReason);
    } else if (data.found) {
      showAlert('success', 'พบประวัติผู้มาติดต่อ — เติมข้อมูลอัตโนมัติแล้ว');
      const v = data.visitor;
      document.getElementById('Title').value = v.title || 'นาย';
      document.getElementById('FirstName').value = v.firstName || '';
      document.getElementById('LastName').value = v.lastName || '';
      document.getElementById('Phone').value = v.phone || '';
      document.getElementById('Email').value = v.email || '';
      document.getElementById('CompanyName').value = v.companyName || '';
      document.getElementById('Address').value = v.address || '';
    } else {
      showAlert('info', 'ไม่พบประวัติ — กรอกข้อมูลใหม่ได้เลย');
    }
  }

  document.getElementById('btn-lookup')?.addEventListener('click', lookupVisitor);
  document.getElementById('NationalId')?.addEventListener('blur', function () {
    if (this.value.length === 13) lookupVisitor();
  });

  document.getElementById('btn-read-card')?.addEventListener('click', async () => {
    try {
      const local = await fetch('http://127.0.0.1:5001/api/thcard', { signal: AbortSignal.timeout(1500) });
      if (local.ok) {
        const card = await local.json();
        fillCard(card);
        showAlert('success', 'อ่านบัตรประชาชนจากเครื่องอ่านแล้ว');
        return;
      }
    } catch {
      // local reader agent is optional
    }
    const demo = await fetch('/api/demo/mock-id-card');
    if (demo.ok) {
      fillCard(await demo.json());
      showAlert('info', 'โหมดสาธิต: เติมข้อมูลบัตรตัวอย่าง (ต่อเครื่องอ่านที่พอร์ต 5001 เพื่อใช้ของจริง)');
    }
  });

  function fillCard(card) {
    document.getElementById('NationalId').value = card.nationalId || '';
    document.getElementById('Title').value = card.title || 'นาย';
    document.getElementById('FirstName').value = card.firstName || '';
    document.getElementById('LastName').value = card.lastName || '';
    document.getElementById('Address').value = card.address || '';
    lookupVisitor();
  }

  document.getElementById('btn-checkin')?.addEventListener('click', () => {
    document.getElementById('SubmitAction').value = 'checkin';
  });
  document.getElementById('btn-prereg')?.addEventListener('click', () => {
    document.getElementById('SubmitAction').value = 'preregister';
  });
})();
