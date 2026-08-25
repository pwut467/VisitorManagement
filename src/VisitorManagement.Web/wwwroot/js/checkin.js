(() => {
  const form = document.getElementById('checkin-form');
  if (!form) return;

  const photoInput = document.getElementById('PhotoDataUrl');
  const video = document.getElementById('cam');
  const preview = document.getElementById('preview');
  const alertBox = document.getElementById('id-alert');
  const readerBadge = document.getElementById('reader-badge');
  const readerDetail = document.getElementById('reader-detail');
  const agentUrl = (document.getElementById('card-reader-config')?.dataset.url || 'http://127.0.0.1:5001').replace(/\/$/, '');
  let stream = null;
  let waitingForCard = false;

  function showAlert(type, message) {
    alertBox.className = 'alert alert-' + type;
    alertBox.textContent = message;
  }

  function setBadge(css, text, detail) {
    if (!readerBadge) return;
    readerBadge.className = 'badge ' + css;
    readerBadge.textContent = text;
    if (readerDetail) readerDetail.textContent = detail || '';
  }

  document.getElementById('btn-start-cam')?.addEventListener('click', async () => {
    try {
      stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'user', width: 640, height: 480 } });
      video.srcObject = stream;
      video.classList.remove('d-none');
      preview.classList.add('d-none');
    } catch {
      showAlert('warning', 'เปิดกล้องไม่ได้ — อนุญาตกล้องในเบราว์เซอร์ หรือใช้รูปจากบัตรประชาชน');
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

  async function lookupVisitor(options) {
    const keepIdentity = options && options.keepIdentity;
    const id = document.getElementById('NationalId').value;
    const res = await fetch('/api/visitors/by-national-id?id=' + encodeURIComponent(id));
    const data = await res.json();
    if (!data.valid) {
      showAlert('warning', 'เลขบัตรประชาชนไม่ผ่านการตรวจ checksum');
    } else if (data.blocked) {
      showAlert('danger', 'บัญชีดำ: ' + data.blockReason);
    } else if (data.found) {
      const v = data.visitor;
      if (!keepIdentity) {
        document.getElementById('Title').value = v.title || 'นาย';
        document.getElementById('FirstName').value = v.firstName || '';
        document.getElementById('LastName').value = v.lastName || '';
        document.getElementById('Address').value = v.address || '';
      }
      if (!document.getElementById('Phone').value) document.getElementById('Phone').value = v.phone || '';
      if (!document.getElementById('Email').value) document.getElementById('Email').value = v.email || '';
      if (!document.getElementById('CompanyName').value) document.getElementById('CompanyName').value = v.companyName || '';
      if (!keepIdentity) {
        showAlert('success', 'พบประวัติผู้มาติดต่อ — เติมข้อมูลอัตโนมัติแล้ว');
      }
    } else if (!keepIdentity) {
      showAlert('info', 'ไม่พบประวัติ — กรอกข้อมูลใหม่ได้เลย');
    }
  }

  document.getElementById('btn-lookup')?.addEventListener('click', () => lookupVisitor());
  document.getElementById('NationalId')?.addEventListener('blur', function () {
    if (this.value.length === 13) lookupVisitor();
  });

  async function refreshReaderStatus() {
    try {
      const res = await fetch(agentUrl + '/api/status', { signal: AbortSignal.timeout(1500) });
      const data = await res.json();
      if (!data.hasReader) {
        setBadge('bg-warning text-dark', 'ไม่พบเครื่องอ่าน', 'เสียบเครื่องอ่าน USB ที่เครื่องนี้');
      } else if (!data.hasCard) {
        setBadge('bg-info text-dark', 'พร้อมอ่าน — ยังไม่มีบัตร', (data.readers || []).join(', '));
      } else {
        setBadge('bg-success', 'พบบัตรในเครื่องอ่าน', (data.readers || []).join(', '));
      }
      return data;
    } catch {
      setBadge('bg-danger', 'ยังไม่เปิดโปรแกรมอ่านบัตร', 'เปิด VisitorManagement.CardReader บนเครื่องนี้ (พอร์ต 5001)');
      return null;
    }
  }

  async function readCardOnce() {
    const res = await fetch(agentUrl + '/api/thcard?photo=true', { signal: AbortSignal.timeout(20000) });
    const card = await res.json();
    if (!res.ok || card.ok === false) {
      const err = new Error(card.message || 'อ่านบัตรไม่สำเร็จ');
      err.code = card.error;
      throw err;
    }
    return card;
  }

  document.getElementById('btn-read-card')?.addEventListener('click', async () => {
    const btn = document.getElementById('btn-read-card');
    btn.disabled = true;
    waitingForCard = true;
    showAlert('info', 'กำลังอ่านบัตรจากเครื่องอ่านจริง...');
    const started = Date.now();
    try {
      while (waitingForCard && Date.now() - started < 30000) {
        try {
          const card = await readCardOnce();
          fillCard(card);
          await lookupVisitor({ keepIdentity: true });
          showAlert('success', 'อ่านบัตรประชาชนจากเครื่องอ่านแล้ว' + (card.reader ? ' (' + card.reader + ')' : ''));
          await refreshReaderStatus();
          return;
        } catch (err) {
          if (err.code === 'no_card') {
            showAlert('info', 'กรุณาเสียบบัตรประชาชนที่เครื่องอ่าน...');
            await new Promise(r => setTimeout(r, 1000));
            continue;
          }
          throw err;
        }
      }
      showAlert('warning', 'ยังไม่พบบัตรในเครื่องอ่าน — เสียบบัตรแล้วกดอ่านอีกครั้ง');
    } catch (err) {
      if (err.name === 'TimeoutError' || err.name === 'AbortError' || err.message === 'Failed to fetch') {
        showAlert('danger', 'เปิดโปรแกรมอ่านบัตรบนเครื่องนี้ไม่สำเร็จ รัน src/VisitorManagement.CardReader แล้วเสียบเครื่องอ่าน USB');
      } else {
        showAlert('danger', err.message || 'อ่านบัตรไม่สำเร็จ');
      }
      await refreshReaderStatus();
    } finally {
      waitingForCard = false;
      btn.disabled = false;
    }
  });

  function ensureTitleOption(title) {
    const select = document.getElementById('Title');
    if (!select || !title) return;
    const exists = Array.from(select.options).some(o => o.value === title);
    if (!exists) {
      select.add(new Option(title, title, true, true));
    }
    select.value = title;
  }

  function fillCard(card) {
    document.getElementById('NationalId').value = card.nationalId || card.NationalId || '';
    ensureTitleOption(card.title || card.Title || 'นาย');
    document.getElementById('FirstName').value = card.firstName || card.FirstName || '';
    document.getElementById('LastName').value = card.lastName || card.LastName || '';
    document.getElementById('Address').value = card.address || card.Address || '';
    const dob = document.getElementById('DateOfBirth');
    if (dob) dob.value = card.dateOfBirth || card.DateOfBirth || '';
    const photo = card.photo || card.PhotoDataUrl;
    if (photo) {
      photoInput.value = photo;
      preview.src = photo;
      preview.classList.remove('d-none');
      video.classList.add('d-none');
    }
  }

  document.getElementById('btn-checkin')?.addEventListener('click', () => {
    document.getElementById('SubmitAction').value = 'checkin';
  });
  document.getElementById('btn-prereg')?.addEventListener('click', () => {
    document.getElementById('SubmitAction').value = 'preregister';
  });

  refreshReaderStatus();
  setInterval(refreshReaderStatus, 4000);
})();
