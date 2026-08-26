(() => {
  const input = document.getElementById('code');
  const box = document.getElementById('preview-box');
  if (!input) return;

  // Kedmanee Thai → QWERTY English (เมื่อคีย์บอร์ดค้างภาษาไทยแล้วพิมพ์รหัสบัตร)
  const thaiToEn = {
    'ๅ': '1', '๑': '1', '/': '2', '๒': '2', '-': '3', '๓': '3', 'ภ': '4', '๔': '4',
    'ถ': '5', '๕': '5', 'ุ': '6', '๖': '6', 'ึ': '7', '๗': '7', 'ค': '8', '๘': '8',
    'ต': '9', '๙': '9', 'จ': '0', '๐': '0', 'ข': '-', 'ช': '=',
    'ๆ': 'q', 'ไ': 'w', 'ำ': 'e', 'พ': 'r', 'ะ': 't', 'ั': 'y', 'ี': 'u', 'ร': 'i',
    'น': 'o', 'ย': 'p', 'บ': '[', 'ล': ']', 'ฃ': '\\',
    'ฟ': 'a', 'ห': 's', 'ก': 'd', 'ด': 'f', 'เ': 'g', '้': 'h', '่': 'j', 'า': 'k',
    'ส': 'l', 'ว': ';', 'ง': "'",
    'ผ': 'z', 'ป': 'x', 'แ': 'c', 'อ': 'v', 'ิ': 'b', 'ื': 'n', 'ท': 'm', 'ม': ',',
    'ใ': '.', 'ฝ': '/',
    '๋': 'Q', 'ฎ': 'W', '"': 'E', 'ฑ': 'R', 'ธ': 'T', 'ํ': 'Y', '๊': 'U', 'ณ': 'I',
    'ฯ': 'O', 'ญ': 'P', 'ฐ': '{', ',': '}', 'ฅ': '|',
    'ฤ': 'A', 'ฆ': 'S', 'ฏ': 'D', 'โ': 'F', 'ฌ': 'G', '็': 'H', '๋': 'J', 'ษ': 'K',
    'ศ': 'L', 'ซ': ':', '.': '"',
    '(': 'Z', ')': 'X', 'ฉ': 'C', 'ฮ': 'V', 'ฺ': 'B', '์': 'N', '?': 'M', 'ฒ': '<',
    'ฬ': '>', 'ฦ': '?'
  };

  function toEnglishKeys(value) {
    let out = '';
    for (const ch of value) {
      out += Object.prototype.hasOwnProperty.call(thaiToEn, ch) ? thaiToEn[ch] : ch;
    }
    return out;
  }

  function preferEnglishInput() {
    input.setAttribute('lang', 'en');
    input.setAttribute('inputmode', 'latin');
    input.setAttribute('spellcheck', 'false');
    input.setAttribute('autocomplete', 'off');
    input.setAttribute('autocapitalize', 'off');
    // บางเบราว์เซอร์/IME ใช้ค่านี้เป็นคำใบ้ให้ใช้ Latin
    input.style.imeMode = 'disabled';
  }

  function normalizeCodeField() {
    const start = input.selectionStart;
    const end = input.selectionEnd;
    const before = input.value;
    const after = toEnglishKeys(before);
    if (after === before) return false;
    input.value = after;
    if (typeof start === 'number' && typeof end === 'number') {
      const delta = after.length - before.length;
      input.setSelectionRange(Math.max(0, start + delta), Math.max(0, end + delta));
    }
    return true;
  }

  async function lookup() {
    normalizeCodeField();
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

  preferEnglishInput();

  input.addEventListener('focus', () => {
    preferEnglishInput();
    normalizeCodeField();
  });

  input.addEventListener('click', () => {
    preferEnglishInput();
  });

  input.addEventListener('input', () => {
    if (normalizeCodeField()) {
      // keep lookup responsive after remap
    }
  });

  input.addEventListener('paste', () => {
    setTimeout(normalizeCodeField, 0);
  });

  input.addEventListener('change', lookup);
  input.addEventListener('blur', lookup);

  // Enter จากเครื่องสแกน → normalize ก่อน submit
  input.form?.addEventListener('submit', () => {
    normalizeCodeField();
  });
})();
