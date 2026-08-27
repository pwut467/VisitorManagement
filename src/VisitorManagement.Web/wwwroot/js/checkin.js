(() => {
  const form = document.getElementById('checkin-form');
  if (!form) return;

  const photoInput = document.getElementById('PhotoDataUrl');
  const cardPhotoInput = document.getElementById('CardPhotoDataUrl');
  const video = document.getElementById('cam');
  const preview = document.getElementById('preview');
  const cardPreview = document.getElementById('card-preview');
  const cardPhotoEmpty = document.getElementById('card-photo-empty');
  const alertBox = document.getElementById('id-alert');
  const readerBadge = document.getElementById('reader-badge');
  const readerDetail = document.getElementById('reader-detail');
  const agentUrl = (document.getElementById('card-reader-config')?.dataset.url || '/api/card-reader').replace(/\/$/, '');
  let stream = null;
  let waitingForCard = false;
  const nationalIdHidden = document.getElementById('NationalId');
  const nationalIdDisplay = document.getElementById('NationalIdDisplay');

  function isDirectAgentUrl(url) {
    return /^https?:\/\//i.test(url);
  }

  /** Build status/thcard URL for either same-origin proxy or direct CardReader agent. */
  function cardEndpoint(resource, query) {
    const path = isDirectAgentUrl(agentUrl)
      ? agentUrl + '/api/' + resource
      : agentUrl + '/' + resource;
    return query ? path + '?' + query : path;
  }

  async function readJsonResponse(res) {
    const text = await res.text();
    if (!text || !text.trim()) {
      const err = new Error(res.status === 404
        ? 'ไม่พบ API อ่านบัตร — รีเฟรชหน้าแล้วลองใหม่'
        : 'โปรแกรมอ่านบัตรตอบกลับว่าง');
      err.code = 'empty_response';
      throw err;
    }
    try {
      return JSON.parse(text);
    } catch {
      const err = new Error('โปรแกรมอ่านบัตรตอบกลับไม่ใช่ JSON');
      err.code = 'bad_json';
      throw err;
    }
  }

  function digitsOnly(value) {
    return (value || '').replace(/\D/g, '');
  }

  function isMaskedNationalId(value) {
    return /^\d-xxxx-xxxxx-\d{2}-\d$/.test(value || '');
  }

  function maskNationalId(value) {
    const id = digitsOnly(value);
    if (id.length !== 13) {
      return value || '';
    }
    return id[0] + '-xxxx-xxxxx-' + id.slice(10, 12) + '-' + id[12];
  }

  function setNationalId(raw, options) {
    const id = digitsOnly(raw).slice(0, 13);
    if (!nationalIdHidden || !nationalIdDisplay) return id;
    nationalIdHidden.value = id;
    if (id.length === 13) {
      nationalIdDisplay.value = maskNationalId(id);
      if (options && options.lookup) {
        lookupVisitor(options.lookup === true ? undefined : options.lookup);
      }
    } else {
      nationalIdDisplay.value = id;
    }
    return id;
  }

  function syncNationalIdFromDisplay() {
    if (!nationalIdDisplay) return '';
    if (isMaskedNationalId(nationalIdDisplay.value) && digitsOnly(nationalIdHidden?.value).length === 13) {
      return nationalIdHidden.value;
    }
    return setNationalId(nationalIdDisplay.value);
  }

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

  function setCardPhoto(dataUrl) {
    if (!cardPhotoInput) return;
    cardPhotoInput.value = dataUrl || '';
    if (dataUrl && cardPreview) {
      cardPreview.src = dataUrl;
      cardPreview.classList.remove('d-none');
      cardPhotoEmpty?.classList.add('d-none');
    } else if (cardPreview) {
      cardPreview.removeAttribute('src');
      cardPreview.classList.add('d-none');
      cardPhotoEmpty?.classList.remove('d-none');
    }
  }

  document.getElementById('btn-start-cam')?.addEventListener('click', async () => {
    try {
      stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: 'user', width: 640, height: 480 } });
      video.srcObject = stream;
      video.classList.remove('d-none');
      preview.classList.add('d-none');
    } catch {
      showAlert('warning', 'เปิดกล้องไม่ได้ — อนุญาตกล้องในเบราว์เซอร์ หรือใช้รูปจากบัตรประชาชนแยกต่างหาก');
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

  document.getElementById('btn-clear-card-photo')?.addEventListener('click', () => {
    setCardPhoto('');
  });

  async function lookupVisitor(options) {
    const keepIdentity = options && options.keepIdentity;
    const id = digitsOnly(nationalIdHidden?.value || '');
    if (id.length !== 13) {
      return;
    }
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
        if (document.getElementById('Address')) document.getElementById('Address').value = v.address || '';
      }
      if (!document.getElementById('Phone').value) document.getElementById('Phone').value = v.phone || '';
      if (!document.getElementById('CompanyName').value) document.getElementById('CompanyName').value = v.companyName || '';
      if (!keepIdentity) {
        showAlert('success', 'พบประวัติผู้มาติดต่อ — เติมข้อมูลอัตโนมัติแล้ว');
      }
    } else if (!keepIdentity) {
      showAlert('info', 'ไม่พบประวัติ — กรอกข้อมูลใหม่ได้เลย');
    }
  }

  document.getElementById('btn-lookup')?.addEventListener('click', () => {
    syncNationalIdFromDisplay();
    lookupVisitor();
  });
  nationalIdDisplay?.addEventListener('keydown', (event) => {
    if (!isMaskedNationalId(nationalIdDisplay.value)) return;
    if (event.key === 'Backspace' || event.key === 'Delete') {
      event.preventDefault();
      setNationalId('');
    }
  });
  nationalIdDisplay?.addEventListener('input', () => {
    if (isMaskedNationalId(nationalIdDisplay.value)) return;
    const id = setNationalId(nationalIdDisplay.value);
    if (id.length === 13) {
      lookupVisitor();
    }
  });
  nationalIdDisplay?.addEventListener('blur', () => {
    const id = syncNationalIdFromDisplay();
    if (id.length === 13) lookupVisitor();
  });
  if (digitsOnly(nationalIdHidden?.value).length === 13) {
    setNationalId(nationalIdHidden.value);
  }

  async function refreshReaderStatus() {
    try {
      const res = await fetch(cardEndpoint('status'), {
        signal: AbortSignal.timeout(3000),
        cache: 'no-store',
        credentials: 'same-origin'
      });
      const data = await readJsonResponse(res);
      if (!res.ok || data.ok === false) {
        setBadge('bg-danger', 'ยังไม่เปิดโปรแกรมอ่านบัตร', data.message || 'รัน VisitorManagement.CardReader (พอร์ต 5001)');
        return null;
      }
      if (data.pcscAvailable === false) {
        setBadge('bg-warning text-dark', 'PC/SC ยังไม่พร้อม', data.message || 'เปิดบริการ Smart Card / pcscd');
      } else if (!data.hasReader) {
        setBadge('bg-warning text-dark', 'ไม่พบเครื่องอ่าน', data.message || 'เสียบเครื่องอ่าน USB ที่เครื่องนี้');
      } else if (!data.hasCard) {
        setBadge('bg-info text-dark', 'พร้อมอ่าน — ยังไม่มีบัตร', (data.readers || []).join(', '));
      } else {
        setBadge('bg-success', 'พบบัตรในเครื่องอ่าน', (data.readers || []).join(', '));
      }
      return data;
    } catch (err) {
      setBadge('bg-danger', 'ยังไม่เปิดโปรแกรมอ่านบัตร', err.message || 'รัน VisitorManagement.CardReader (พอร์ต 5001) แล้วรีเฟรชหน้า');
      return null;
    }
  }

  async function readCardOnce() {
    const res = await fetch(cardEndpoint('thcard', 'photo=true'), {
      signal: AbortSignal.timeout(25000),
      cache: 'no-store',
      credentials: 'same-origin'
    });
    const card = await readJsonResponse(res);
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
      const status = await refreshReaderStatus();
      if (!status) {
        showAlert(
          'danger',
          'ติดต่อโปรแกรมอ่านบัตรไม่ได้ — รัน src/VisitorManagement.CardReader ที่พอร์ต 5001 บนเครื่องที่รันเว็บนี้ แล้วรีเฟรชหน้า'
        );
        return;
      }
      if (status.pcscAvailable === false) {
        showAlert('danger', status.message || 'บริการ PC/SC ยังไม่พร้อม');
        return;
      }
      if (!status.hasReader) {
        showAlert('danger', status.message || 'ไม่พบเครื่องอ่านบัตร USB ที่เครื่องนี้');
        return;
      }

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
        showAlert(
          'danger',
          'ติดต่อโปรแกรมอ่านบัตรไม่ได้ — รัน CardReader ที่พอร์ต 5001 บนเครื่องที่รันเว็บ แล้วรีเฟรชหน้า'
        );
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
    setNationalId(card.nationalId || card.NationalId || '');
    ensureTitleOption(card.title || card.Title || 'นาย');
    document.getElementById('FirstName').value = card.firstName || card.FirstName || '';
    document.getElementById('LastName').value = card.lastName || card.LastName || '';
    if (document.getElementById('Address')) {
      document.getElementById('Address').value = card.address || card.Address || '';
    }
    const photo = card.photo || card.PhotoDataUrl;
    if (photo) {
      setCardPhoto(photo);
    }
  }

  form.addEventListener('submit', () => {
    syncNationalIdFromDisplay();
  });
  if (window.jQuery) {
    const validator = window.jQuery('#checkin-form').data('validator');
    if (validator) {
      validator.settings.ignore = ':hidden:not(#NationalId)';
    }
  }
  document.getElementById('btn-checkin')?.addEventListener('click', () => {
    document.getElementById('SubmitAction').value = 'checkin';
  });

  refreshReaderStatus();
  setInterval(refreshReaderStatus, 4000);
})();
