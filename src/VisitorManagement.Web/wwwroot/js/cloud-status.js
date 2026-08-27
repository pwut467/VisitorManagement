document.addEventListener('DOMContentLoaded', () => {
  const badge = document.getElementById('cloud-status-badge');
  if (!badge) {
    return;
  }

  const label = document.getElementById('cloud-status-label');
  const detail = document.getElementById('cloud-status-detail');

  async function refresh() {
    try {
      const res = await fetch('/api/cloud/status', { headers: { 'Accept': 'application/json' } });
      if (!res.ok) {
        throw new Error('status ' + res.status);
      }
      const data = await res.json();
      badge.classList.remove('cloud-online', 'cloud-offline', 'cloud-disabled', 'cloud-pending');
      if (!data.enabled) {
        badge.classList.add('cloud-disabled');
      } else if (!data.configured) {
        badge.classList.add('cloud-offline');
      } else if (data.online) {
        badge.classList.add(data.pendingSyncCount > 0 ? 'cloud-pending' : 'cloud-online');
      } else {
        badge.classList.add('cloud-offline');
      }
      if (label) {
        label.textContent = data.label || 'Cloud';
      }
      if (detail) {
        const parts = [];
        if (data.server) {
          parts.push(data.server);
        }
        if (data.database) {
          parts.push(data.database);
        }
        if (data.lastCheckedAt) {
          parts.push('ตรวจ ' + data.lastCheckedAt);
        }
        if (!data.configured) {
          parts.push('ตั้งค่า Username/Password ที่เมนูตั้งค่าบริษัท');
        } else if (!data.online && data.lastError) {
          parts.push(data.lastError);
        }
        detail.textContent = parts.join(' · ');
        detail.title = parts.join(' · ');
      }
    } catch {
      badge.classList.remove('cloud-online', 'cloud-pending', 'cloud-disabled');
      badge.classList.add('cloud-offline');
      if (label) {
        label.textContent = 'Cloud ไม่ทราบสถานะ';
      }
    }
  }

  refresh();
  setInterval(refresh, 15000);
});
