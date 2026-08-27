(() => {
  const input = document.getElementById('code');
  const box = document.getElementById('preview-box');
  if (!input) return;

  // Physical key → English (ไม่สนว่า OS ตั้งภาษาไทย/อังกฤษ)
  const digit = {
    Digit0: '0', Digit1: '1', Digit2: '2', Digit3: '3', Digit4: '4',
    Digit5: '5', Digit6: '6', Digit7: '7', Digit8: '8', Digit9: '9'
  };
  const digitShift = {
    Digit0: ')', Digit1: '!', Digit2: '@', Digit3: '#', Digit4: '$',
    Digit5: '%', Digit6: '^', Digit7: '&', Digit8: '*', Digit9: '('
  };
  const punct = {
    Minus: '-', Equal: '=', BracketLeft: '[', BracketRight: ']',
    Backslash: '\\', Semicolon: ';', Quote: "'", Comma: ',',
    Period: '.', Slash: '/', Backquote: '`'
  };
  const punctShift = {
    Minus: '_', Equal: '+', BracketLeft: '{', BracketRight: '}',
    Backslash: '|', Semicolon: ':', Quote: '"', Comma: '<',
    Period: '>', Slash: '?', Backquote: '~'
  };

  // แปลงอักขระไทยที่หลุดเข้ามา (เช่น paste) เป็นปุ่มอังกฤษ
  const thaiToEn = {
    'ๅ': '1', '๑': '1', '๒': '2', '๓': '3', '๔': '4', '๕': '5',
    '๖': '6', '๗': '7', '๘': '8', '๙': '9', '๐': '0',
    'ๆ': 'q', 'ไ': 'w', 'ำ': 'e', 'พ': 'r', 'ะ': 't', 'ั': 'y', 'ี': 'u', 'ร': 'i',
    'น': 'o', 'ย': 'p', 'บ': '[', 'ล': ']', 'ฃ': '\\',
    'ฟ': 'a', 'ห': 's', 'ก': 'd', 'ด': 'f', 'เ': 'g', '้': 'h', '่': 'j', 'า': 'k',
    'ส': 'l', 'ว': ';', 'ง': "'",
    'ผ': 'z', 'ป': 'x', 'แ': 'c', 'อ': 'v', 'ิ': 'b', 'ื': 'n', 'ท': 'm', 'ม': ',',
    'ใ': '.', 'ฝ': '/',
    'ฎ': 'W', 'ฑ': 'R', 'ธ': 'T', 'ํ': 'Y', '๊': 'U', 'ณ': 'I',
    'ฯ': 'O', 'ญ': 'P', 'ฐ': '{', 'ฅ': '|',
    'ฤ': 'A', 'ฆ': 'S', 'ฏ': 'D', 'โ': 'F', 'ฌ': 'G', '็': 'H', '๋': 'J', 'ษ': 'K',
    'ศ': 'L', 'ซ': ':',
    'ฉ': 'C', 'ฮ': 'V', 'ฺ': 'B', '์': 'N', 'ฒ': '<', 'ฬ': '>', 'ฦ': '?',
    'ภ': '4', 'ถ': '5', 'ุ': '6', 'ึ': '7', 'ค': '8', 'ต': '9', 'จ': '0', 'ข': '-', 'ช': '='
  };

  function preferEnglishInput() {
    input.setAttribute('lang', 'en');
    input.setAttribute('inputmode', 'latin');
    input.setAttribute('spellcheck', 'false');
    input.setAttribute('autocomplete', 'off');
    input.setAttribute('autocapitalize', 'characters');
    input.style.imeMode = 'disabled';
  }

  function insertText(text) {
    const start = input.selectionStart ?? input.value.length;
    const end = input.selectionEnd ?? input.value.length;
    const next = input.value.slice(0, start) + text + input.value.slice(end);
    input.value = next;
    const pos = start + text.length;
    input.setSelectionRange(pos, pos);
    input.dispatchEvent(new Event('input', { bubbles: true }));
  }

  function englishFromCode(e) {
    const { code, shiftKey } = e;
    if (code.startsWith('Key') && code.length === 4) {
      const letter = code.slice(3);
      return shiftKey ? letter : letter.toLowerCase();
    }
    if (Object.prototype.hasOwnProperty.call(digit, code)) {
      return shiftKey ? digitShift[code] : digit[code];
    }
    if (Object.prototype.hasOwnProperty.call(punct, code)) {
      return shiftKey ? punctShift[code] : punct[code];
    }
    if (code === 'Space') return ' ';
    return null;
  }

  function toEnglishKeys(value) {
    let out = '';
    for (const ch of value) {
      out += Object.prototype.hasOwnProperty.call(thaiToEn, ch) ? thaiToEn[ch] : ch;
    }
    return out;
  }

  function normalizeCodeField() {
    const before = input.value;
    const after = toEnglishKeys(before);
    if (after === before) return false;
    const start = input.selectionStart;
    const end = input.selectionEnd;
    input.value = after;
    if (typeof start === 'number' && typeof end === 'number') {
      input.setSelectionRange(start, end);
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

  input.addEventListener('click', preferEnglishInput);

  // บังคับอักขระอังกฤษจากปุ่มจริง แม้คีย์บอร์ด OS เป็นไทย
  input.addEventListener('keydown', (e) => {
    if (e.ctrlKey || e.metaKey || e.altKey || e.isComposing) return;
    if (e.key === 'Enter' || e.key === 'Tab' || e.key === 'Escape') return;
    if (e.key === 'Backspace' || e.key === 'Delete'
      || e.key === 'ArrowLeft' || e.key === 'ArrowRight'
      || e.key === 'ArrowUp' || e.key === 'ArrowDown'
      || e.key === 'Home' || e.key === 'End') return;

    const en = englishFromCode(e);
    if (en == null) return;
    e.preventDefault();
    insertText(en);
  });

  input.addEventListener('input', () => {
    normalizeCodeField();
  });

  input.addEventListener('paste', () => {
    setTimeout(() => {
      normalizeCodeField();
      lookup();
    }, 0);
  });

  input.addEventListener('change', lookup);
  input.addEventListener('blur', lookup);

  input.form?.addEventListener('submit', () => {
    normalizeCodeField();
  });
})();
